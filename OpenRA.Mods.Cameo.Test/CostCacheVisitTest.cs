#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-BOTTICK-01 FIX 4 (row cost_cache_visit, Opus 11-26): the unit builder reads every production cost through a cache that lives for one bot visit only. Off = live reads.
	[TestFixture]
	public sealed class CostCacheVisitTest
	{
		const string Unit = "mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs";
		const string Harvest = "mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.HarvesterTarget.cs";
		const string BaseBuilder = "mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs";
		const string Cache = "mods/CA/Traits/BotModules/BotModuleLogic/ProductionCostCache.cs";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string ReleaseView(string source) => Regex.Replace(source, @"#if CAMEO_DEV_LOGGING\n.*?#endif\n", "", RegexOptions.Singleline);

		[Test]
		public void OffInCodeAndUnarmedInTheYaml()
		{
			var u = Source(Unit);
			Assert.That(AblationIds.CostCacheVisit, Is.EqualTo("cost_cache_visit"));
			Assert.That(u, Does.Contain("public readonly bool CostCacheVisit = false;"));
			Assert.That(u, Does.Contain("bool CostCacheOn => switchesActive && Info.CostCacheVisit && !ablateCostCache.Off(player, AblationIds.CostCacheVisit);"));
			// the gate commit arms it in the @generic unit builder only (Opus 11-26: the push candidate = exact commits + FIX 4 armed)
			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml.Split("CostCacheVisit: true").Length - 1, Is.EqualTo(1));
			var generic = yaml.IndexOf("\tUnitBuilderBotModuleCA@generic:", StringComparison.Ordinal);
			var armed = yaml.IndexOf("\t\tCostCacheVisit: true", StringComparison.Ordinal);
			var next = generic + 1 + Regex.Match(yaml.Substring(generic + 1), "\n\t[^\t\n]").Index;
			Assert.That(generic, Is.GreaterThan(0));
			Assert.That(armed, Is.GreaterThan(generic), "inside the @generic block");
			Assert.That(armed, Is.LessThan(next), "before the next trait block");
		}

		[Test]
		public void TheCacheLivesForOneVisitAndEveryUnitBuilderCostReadUsesIt()
		{
			var u = Source(Unit);
			Assert.That(u, Does.Contain("UbVisitBegin();\n\t\t\tcostCache.BeginVisit(CostCacheOn);\n\t\t\tBotTickVisit(bot);\n\t\t\tcostCache.EndVisit();\n\t\t\tUbVisitEnd();"));
			Assert.That(u, Does.Contain("int Cost(ProductionQueue queue, ActorInfo unit) => costCache.Get(queue, unit);"));
			Assert.That(u, Does.Not.Contain("GetProductionCost("), "the unit builder has no live cost read left");
			Assert.That(Source(Harvest), Does.Not.Contain("GetProductionCost("));

			var c = Source(Cache);
			Assert.That(c, Does.Contain("public void BeginVisit(bool on)\n\t\t{\n\t\t\tcosts.Clear();\n\t\t\tactive = on;\n\t\t}"), "a visit starts empty");
			Assert.That(c, Does.Contain("public void EndVisit()\n\t\t{\n\t\t\tcosts.Clear();\n\t\t\tactive = false;\n\t\t}"), "after a visit every read is live");
			Assert.That(c, Does.Contain("if (!active)\n\t\t\t\treturn queue.GetProductionCost(unit);"), "off or outside a visit = the live call");
			Assert.That(c, Does.Contain("var key = (queue.Actor.ActorID, queue.Info.Type, unit.Name);"));
			Assert.That(c, Does.Contain("return costs[key] = queue.GetProductionCost(unit);"), "a miss stores the live value");
		}

		[Test]
		public void BaseBuilderCacheBeginsAndEndsAroundEachModuleVisit()
		{
			var baseBuilder = Source(BaseBuilder);
			Assert.That(baseBuilder, Does.Contain("void IBotTick.BotTick(IBot bot)\n\t\t{\n\t\t\tcostCache.BeginVisit(true);\n\t\t\ttry\n\t\t\t{\n\t\t\t\tBotTickVisit(bot);\n\t\t\t}\n\t\t\tfinally\n\t\t\t{\n\t\t\t\tcostCache.EndVisit();\n\t\t\t}\n\t\t}"));
			Assert.That(baseBuilder, Does.Contain("internal int ProductionCost(ProductionQueue queue, ActorInfo actorInfo) => costCache.Get(queue, actorInfo);"));
			Assert.That(baseBuilder, Does.Not.Contain("GetProductionCost("));

			foreach (var file in new[]
			{
				"mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs",
				"mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs",
				"mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Defence.cs",
				"mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Stages.cs"
			})
			{
				var source = Source(file);
				Assert.That(source, Does.Not.Contain("GetProductionCost("), file + " uses the visit-scoped BaseBuilder owner");
				Assert.That(source, Does.Contain("baseBuilder.ProductionCost("), file + " routes costs through the cache");
			}
		}

		[Test]
		public void TheShadowCheckIsDevOnly()
		{
			var c = Source(Cache);
			Assert.That(c, Does.Contain("PERF-BOTTICK-01 SHADOW-MISMATCH production-cost"));
			var release = ReleaseView(c);
			foreach (var devOnly in new[] { "ShadowEvery", "shadowCount", "SHADOW-MISMATCH" })
				Assert.That(release, Does.Not.Contain(devOnly), devOnly + " is compiled out of Release");
		}
	}
}
