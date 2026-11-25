#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// CRAWL-LONG-01 (Opus 11-25, user issue: over long distances the crawl wastes power plants). V08D 6v6: 117 crawl power plants; seat_5 placed 20 of them in 3200 ticks around an aim whose
	// distance only grew (6.7 -> 21.8 cells), scattered up to 39 cells apart. Row crawl_advance_only: a crawl placement must strictly reduce the frontier-to-aim distance.
	[TestFixture]
	public sealed class CrawlLong01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		[Test]
		public void TheFrontierIsTheOwnTileNearestTheAim()
		{
			var tiles = new[] { new CPos(10, 10), new CPos(14, 12), new CPos(12, 30) };
			Assert.That(BaseBuilderQueueEvalCA.NearestTile(tiles, new CPos(30, 12)), Is.EqualTo(new CPos(14, 12)));
			Assert.That(BaseBuilderQueueEvalCA.NearestTile(tiles, new CPos(12, 40)), Is.EqualTo(new CPos(12, 30)));
			Assert.That(BaseBuilderQueueEvalCA.NearestTile(Array.Empty<CPos>(), new CPos(1, 1)), Is.Null);
			Assert.That(BaseBuilderQueueEvalCA.NearestTile(null, new CPos(1, 1)), Is.Null);

			// ties keep enumeration order (the planner's FrontierAnchor rule)
			Assert.That(BaseBuilderQueueEvalCA.NearestTile(new[] { new CPos(0, 5), new CPos(10, 5) }, new CPos(5, 5)), Is.EqualTo(new CPos(0, 5)));
		}

		[Test]
		public void APlacementAdvancesOnlyWhenItsFootprintIsStrictlyCloserToTheAimThanTheFrontier()
		{
			var frontier = new[] { new CPos(10, 10), new CPos(8, 8) };
			var aim = new CPos(30, 10);

			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(12, 10) }, frontier, aim), Is.True, "two cells nearer");
			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(10, 10) }, frontier, aim), Is.False, "the same distance is no advance");
			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(10, 12) }, frontier, aim), Is.False, "sideways: farther");
			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(2, 10) }, frontier, aim), Is.False, "backwards");
			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(10, 14), new CPos(11, 10) }, frontier, aim), Is.True, "one footprint tile nearer is enough (the planner measures building tiles)");

			// seat_5 t=22656..25840: the aim sat 6.7..21.8 cells from each plant while the frontier already stood near it: none of those cells would pass
			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(36, 57) }, new[] { new CPos(49, 72), new CPos(50, 67) }, new CPos(51, 72)), Is.False);

			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(1, 1) }, Array.Empty<CPos>(), aim), Is.True, "no frontier evidence: no veto");
			Assert.That(BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { new CPos(1, 1) }, null, aim), Is.True);
		}

		[Test]
		public void ALongCrawlStepsStrictlyCloserEachTimeAndStopsWhenNothingAdvances()
		{
			// simulate the placement rule: each plant lands on the legal cell nearest the aim; 60 cells at a 6-cell reach = ten strictly decreasing steps, then a blocked front stops it
			var aim = new CPos(100, 0);
			var frontier = new System.Collections.Generic.List<CPos> { new CPos(40, 0) };
			var last = int.MaxValue;
			var steps = 0;
			while (steps < 50)
			{
				var nearest = BaseBuilderQueueEvalCA.NearestTile(frontier, aim).Value;
				var candidate = new CPos(nearest.X + 6, 0);
				if (candidate.X > 70 || !BaseBuilderQueueEvalCA.AdvancesFrontier(new[] { candidate }, frontier, aim))
					break;

				frontier.Add(candidate);
				var d = (candidate - aim).LengthSquared;
				Assert.That(d, Is.LessThan(last), "each step is strictly closer");
				last = d;
				steps++;
			}

			Assert.That(steps, Is.EqualTo(5), "legal ground ends at x=70: five steps, then the front is blocked and the rule refuses a sixth");
		}

		[Test]
		public void TheRowWiringIsPinned()
		{
			var manager = Source(Dir + "BaseBuilderQueueManagerCA.cs");
			Assert.That(manager, Does.Contain("var advanceOnly = crawlLaw != null && crawlLaw.CrawlAdvanceOnly;"));
			Assert.That(manager, Does.Contain("advanceOnly ? 0 : baseBuilder.Info.MinBaseRadius, baseBuilder.Info.BaseCrawlRadius, bypassAdvisor: crawlLaw != null);"));
			Assert.That(manager, Does.Contain("if (crawlLaw.WantedLinkBuilding == actorType)\n\t\t\t\t\t\t\t\tcrawlLaw.CrawlLinkNoAdvance();"), "only the wanted link parks the target");
			Assert.That(manager, Does.Contain("actorInfo.HasTraitInfo<GivesBuildableAreaInfo>()"), "a placement that gives no buildable area is no advance");

			// the no-advance branch returns BEFORE the random-resource and toward-the-enemy fallbacks
			var noAdvance = manager.IndexOf("CRAWL-ADVANCE {actorType} has no forward cell", StringComparison.Ordinal);
			var randomResource = manager.IndexOf("// Try and place the refinery near a resource field", StringComparison.Ordinal);
			Assert.That(noAdvance, Is.GreaterThan(0));
			Assert.That(randomResource, Is.GreaterThan(noAdvance), "the fallbacks are unreachable from the no-advance branch");

			var provider = Source("mods/CA/Traits/BotModules/IBotExpansionTargetProvider.cs");
			Assert.That(provider, Does.Contain("bool CrawlAdvanceOnly => false;"), "default off: fakes and the classic provider are unchanged");
			Assert.That(provider, Does.Contain("void CrawlLinkNoAdvance()\n\t\t{\n\t\t}"));

			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("public readonly bool CrawlAdvanceOnly = false;"), "off by default");
			Assert.That(planner, Does.Contain("bool CrawlAdvanceOnlyOn => Info.CrawlAdvanceOnly && Info.DriveBaseCrawl && LawActive && !ablateCrawlAdvance.Off(player, AblationIds.CrawlAdvanceOnly);"));
			Assert.That(planner, Does.Contain("crawlParkedUntil[Target.Value.Index] = world.WorldTick + Info.CrawlNoAdvanceParkTicks;"));
			Assert.That(planner, Does.Contain("if ((Info.UseCrawlSafety || CrawlAdvanceOnlyOn) && crawlParkedUntil.TryGetValue(i, out var lossUntil)"), "the target choice honours the park");

			Assert.That(AblationSpec.RegistryIds(), Does.Contain("crawl_advance_only"));
			Assert.That(Source(Dir + "AblationIds.CrawlLong.cs"), Does.Contain("\"crawl_advance_only\""));

			var node = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(node.Value.Nodes.Any(n => n.Key == "CrawlAdvanceOnly" && n.Value.Value == "true"), Is.True, "armed for the generic planner");
		}
	}
}
