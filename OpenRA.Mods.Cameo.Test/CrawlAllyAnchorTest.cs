#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// crawl_ally_anchor (BS-03 c9 port, light team crawl; Opus V08G). The dedupe of the BS-03 team crawl already exists as the TC-2c ally expansion claims
	// (YieldToAllyClaim), so this row only adds the designer's allied anchor: allied yards next to our own in the cap, allied area-giving buildings in the frontier.
	[TestFixture]
	public sealed class CrawlAllyAnchorTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		[Test]
		public void AnAlliedYardMakesAFieldNearItWithinTheCapAndNoYardsStillMeansNoCrawl()
		{
			var own = new[] { new CPos(0, 0) };
			var ally = new[] { new CPos(100, 0) };
			var field = new CPos(110, 0);

			Assert.That(ExpansionPlannerBotModule.CrawlBeyondCap(field, own, 30), Is.True, "own yards only: 110 away");
			var merged = ExpansionPlannerBotModule.CrawlAnchors(own, ally);
			Assert.That(merged.Count, Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.CrawlBeyondCap(field, merged, 30), Is.False, "10 from the allied yard");
			Assert.That(ExpansionPlannerBotModule.CrawlBeyondCap(new CPos(150, 0), merged, 30), Is.True, "beyond the cap of both");

			Assert.That(ExpansionPlannerBotModule.CrawlAnchors(own, null), Is.SameAs(own), "no allied yard: the same list (row off or no ally)");
			Assert.That(ExpansionPlannerBotModule.CrawlAnchors(own, Array.Empty<CPos>()), Is.SameAs(own));
			Assert.That(ExpansionPlannerBotModule.CrawlBeyondCap(field, ExpansionPlannerBotModule.CrawlAnchors(Array.Empty<CPos>(), Array.Empty<CPos>()), 30), Is.True, "no yard at all = no crawl");
		}

		[Test]
		public void TheTargetChoicePrefersAFieldWithinTheCapOfAnAlliedYard()
		{
			static ExpansionPlannerBotModule.FieldScore F(int index, int x, int hops) =>
				new ExpansionPlannerBotModule.FieldScore(index, new CPos(x, 0), 1000, hops, 1000, 0, 1.0 / (index + 1));

			var merged = ExpansionPlannerBotModule.CrawlAnchors(new[] { new CPos(0, 0) }, new[] { new CPos(100, 0) });
			var far = F(1, 60, 4);
			var nearAlly = F(2, 110, 4);
			Assert.That(ExpansionPlannerBotModule.ChooseCrawlTarget(new[] { far, nearAlly }, -1, 30, merged, out _), Is.EqualTo(1), "the field beside the ally is within the cap");
			Assert.That(ExpansionPlannerBotModule.ChooseCrawlTarget(new[] { far, nearAlly }, -1, 30, new[] { new CPos(0, 0) }, out _), Is.EqualTo(0), "own yards only: both beyond, the best stays");
		}

		[Test]
		public void TheRowIsOffByDefaultNeedsAllybuildAndTheFrontierGainsAlliedTiles()
		{
			Assert.That(new ExpansionPlannerBotModuleInfo().CrawlAllyAnchor, Is.False);
			Assert.That(AblationIds.CrawlAllyAnchor, Is.EqualTo("crawl_ally_anchor"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("crawl_ally_anchor"));

			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("bool CrawlAllyAnchorOn => Info.CrawlAllyAnchor && CrawlAdvanceOnlyOn && !ablateCrawlAllyAnchor.Off(player, AblationIds.CrawlAllyAnchor)"));
			Assert.That(planner, Does.Contain("&& (allyBuildEnabled ??= world.WorldActor.TraitOrDefault<MapBuildRadius>()?.AllyBuildRadiusEnabled ?? false);"), "allybuild lobby option");
			Assert.That(planner.Split("CrawlAnchorCells()").Length - 1, Is.EqualTo(3), "the definition, the target choice and the start gate");
			Assert.That(planner, Does.Not.Contain("CrawlBeyondCap(Target.Value.Center, ownYardCells"), "the start gate no longer reads own yards directly");
			Assert.That(planner, Does.Contain("bool IBotExpansionTargetProvider.CrawlAllyAnchor => CrawlAllyAnchorOn;"));

			var qm = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(qm, Does.Contain("var frontierTiles = advanceOnly ? OwnBuildableAreaTiles(crawlLaw.CrawlAllyAnchor) : null;"));
			Assert.That(qm, Does.Contain("List<CPos> OwnBuildableAreaTiles(bool withAllied = false)"));
			Assert.That(qm, Does.Contain("g.Info.OnlyAllowPlacementFromSelf"), "the engine's own rule for an allied front");

			var node = PlayerNode("ExpansionPlannerBotModule");
			Assert.That(node.Value.Nodes.Any(n => n.Key == "CrawlAllyAnchor" && n.Value.Value == "true"), Is.True);
		}
	}
}
