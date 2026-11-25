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
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// crawl_sticky_expiry and crawl_pause_bound (Opus V08G rulings a and b, BS-03 c7 semantics). Pure rules by tables, the wiring by pins, the arm by yaml.
	[TestFixture]
	public sealed class CrawlExpiryPauseTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static int Count(string text, string needle) => text.Split(needle).Length - 1;

		[Test]
		public void ProgressIsAStrictlyCloserFrontierAndAStallIsTheFullWindowWithoutIt()
		{
			Assert.That(ExpansionPlannerBotModule.CrawlProgressed(40, int.MaxValue), Is.True, "the first distance after the commit");
			Assert.That(ExpansionPlannerBotModule.CrawlProgressed(34, 40), Is.True);
			Assert.That(ExpansionPlannerBotModule.CrawlProgressed(40, 40), Is.False, "same distance is no progress");
			Assert.That(ExpansionPlannerBotModule.CrawlProgressed(47, 40), Is.False, "a farther frontier (a lost link) is no progress");

			Assert.That(ExpansionPlannerBotModule.CrawlStalled(2999, 0, 3000), Is.False);
			Assert.That(ExpansionPlannerBotModule.CrawlStalled(3000, 0, 3000), Is.True, "exactly the window");
			Assert.That(ExpansionPlannerBotModule.CrawlStalled(9000, 6500, 3000), Is.False, "the window restarts at the last progress");
			Assert.That(ExpansionPlannerBotModule.CrawlStalled(99999, 0, 0), Is.False, "0 = never");
		}

		[Test]
		public void ThePauseStartsAtTheFirstThreatRefusalSurvivesAndEndsWithTheBound()
		{
			Assert.That(ExpansionPlannerBotModule.CrawlPauseStart(true, -1, 500), Is.EqualTo(500), "starts now");
			Assert.That(ExpansionPlannerBotModule.CrawlPauseStart(true, 500, 750), Is.EqualTo(500), "an ongoing pause keeps its start");
			Assert.That(ExpansionPlannerBotModule.CrawlPauseStart(false, 500, 750), Is.EqualTo(-1), "admitted or refused for another reason: reset");

			Assert.That(ExpansionPlannerBotModule.CrawlPauseExpired(-1, 99999, 900), Is.False, "not paused");
			Assert.That(ExpansionPlannerBotModule.CrawlPauseExpired(500, 1399, 900), Is.False);
			Assert.That(ExpansionPlannerBotModule.CrawlPauseExpired(500, 1400, 900), Is.True, "exactly the bound");
			Assert.That(ExpansionPlannerBotModule.CrawlPauseExpired(500, 99999, 0), Is.False, "0 = unbounded");
		}

		[Test]
		public void TheLadderIs900Plus150RFromNoobToGodAndTheEscortValueIsOnlyAFloor()
		{
			var info = new ExpansionPlannerBotModuleInfo();
			Assert.That(info.CrawlMaxPauseEasiest, Is.EqualTo(900));
			Assert.That(info.CrawlMaxPauseHardest, Is.EqualTo(2250));
			Assert.That(info.CrawlEscortValueEasiest, Is.EqualTo(900));
			Assert.That(info.CrawlEscortValueHardest, Is.EqualTo(2250));
			Assert.That(info.CrawlMaxPauseUnranked, Is.EqualTo(900 + 150 * 4), "an unranked owner gets the hard value");

			for (var r = -1; r <= 9; r++)
			{
				Assert.That(BotDifficultyLadder.InterpolateByRank(info.CrawlMaxPauseEasiest, info.CrawlMaxPauseHardest, r, BotDifficultyLadder.Difficulties.Length), Is.EqualTo(900 + 150 * r), "pause rank " + r);
				Assert.That(BotDifficultyLadder.InterpolateByRank(info.CrawlEscortValueEasiest, info.CrawlEscortValueHardest, r, BotDifficultyLadder.Difficulties.Length), Is.EqualTo(900 + 150 * r), "escort rank " + r);
			}

			Assert.That(ExpansionPlannerBotModule.EscortRequestValue(false, 400, 150, 1500), Is.EqualTo(400), "row off: max(threat, link cost) as before");
			Assert.That(ExpansionPlannerBotModule.EscortRequestValue(false, 40, 150, 1500), Is.EqualTo(150));
			Assert.That(ExpansionPlannerBotModule.EscortRequestValue(true, 400, 150, 1500), Is.EqualTo(1500), "row on: at least the ladder value");
			Assert.That(ExpansionPlannerBotModule.EscortRequestValue(true, 3000, 150, 1500), Is.EqualTo(3000), "a bigger threat still wins");
		}

		[Test]
		public void TheRowsAreOffByDefaultWiredAndArmedForTheGenericPlanner()
		{
			var info = new ExpansionPlannerBotModuleInfo();
			Assert.That(info.CrawlStickyExpiry, Is.False);
			Assert.That(info.CrawlPauseBound, Is.False);
			Assert.That(info.CrawlStickyExpiryTicks, Is.EqualTo(3000));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("crawl_sticky_expiry"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("crawl_pause_bound"));

			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("bool CrawlStickyExpiryOn => CrawlTargetStickyOn && Info.CrawlStickyExpiry && (Info.UseCrawlSafety || CrawlAdvanceOnlyOn) && !ablateCrawlExpiry.Off(player, AblationIds.CrawlStickyExpiry);"));
			Assert.That(planner, Does.Contain("bool CrawlPauseBoundOn => Info.CrawlPauseBound && Info.UseCrawlSafety && Info.DriveBaseCrawl && LawActive && !ablateCrawlPause.Off(player, AblationIds.CrawlPauseBound);"));
			Assert.That(planner, Does.Contain("if (sticky && CrawlStickyExpiryOn && crawlCommitIndex >= 0)\n\t\t\t\tExpireStalledCrawl(scores);"), "expiry runs before the choice");
			Assert.That(planner, Does.Contain("\t\t\t\t\tCommitCrawl(Target.Value);"), "the commit goes through CommitCrawl");
			Assert.That(planner, Does.Contain("crawlOwnCells = buildingCells;\n\t\t\tTarget = ChooseTarget(scores);"), "the own cells are set before the choice");
			Assert.That(Count(planner, "safetyThreatRefused = true;"), Is.EqualTo(2), "uncovered and contested front only; parity, scout and farmed route are not a threat pause");
			Assert.That(planner, Does.Contain("crawlPauseSince = CrawlPauseStart(!mandatoryPower && !safetyAdmits && safetyThreatRefused, pauseCarry, pauseTick);"), "mandatory power never pauses");
			Assert.That(planner, Does.Contain("var pauseCarry = crawlPauseSince;\n\t\t\tcrawlPauseSince = -1;\n\t\t\twantedLinkBuilding = null;"),
				"the pause clock is reset at the top of UpdateCrawlWant and carried only into the pause block, so an early return (no link wanted, beyond the cap) breaks the run");
			Assert.That(Count(planner, "crawlPauseSince = -1;"), Is.GreaterThanOrEqualTo(3), "the top reset, the commit reset and the expiry reset");
			Assert.That(planner, Does.Contain("EscortRequestValue(CrawlPauseBoundOn, threat, link.Cost, CrawlPauseBoundOn ? CrawlEscortValue() : 0)"));

			var node = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(node.Value.Nodes.Any(n => n.Key == "CrawlStickyExpiry" && n.Value.Value == "true"), Is.True);
			Assert.That(node.Value.Nodes.Any(n => n.Key == "CrawlPauseBound" && n.Value.Value == "true"), Is.True);
			Assert.That(node.Value.Nodes.Any(n => n.Key == "UseCrawlSafety" && n.Value.Value == "true"), Is.True, "the pause row needs the safety gate");
		}
	}
}
