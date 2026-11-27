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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// STALEMATE-SCOUT-01 (row ss_scout_push; USER decision D5 on audit A7). A push that is ready with no known target scouts the stale regions of the tactical map and then attacks what it finds;
	/// only a genuinely unreachable target excuses a push that does not attack. The decisions are pure; the wiring keeps the old text of every call site behind the row, reads only the bot's own
	/// region memory and shroud (fog-safe), and ships OFF in code, in classic and in the six blocks.
	/// </summary>
	[TestFixture]
	public class StalemateScoutTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		// ---- the three cases of the decision ----

		[Test]
		public void NoKnownTargetScoutsWhenAStaleRegionCanBeWalkedTo()
		{
			Assert.That(StalemateScoutCA.Classify(false, true, 1), Is.EqualTo(StalemateScoutCA.Verdict.Scout));
			Assert.That(StalemateScoutCA.Classify(false, true, 16), Is.EqualTo(StalemateScoutCA.Verdict.Scout), "after several refused centres the next one still scouts");
			Assert.That(StalemateScoutCA.Excused(StalemateScoutCA.Verdict.Scout), Is.False, "a push that scouts is not excused from anything, it is working");
		}

		[Test]
		public void OnlyAnUnreachableTargetIsTheException()
		{
			var verdict = StalemateScoutCA.Classify(false, false, 5);
			Assert.That(verdict, Is.EqualTo(StalemateScoutCA.Verdict.Unreachable), "stale regions exist and none can be walked to");
			Assert.That(StalemateScoutCA.Excused(verdict), Is.True);

			var explored = StalemateScoutCA.Classify(false, false, 0);
			Assert.That(explored, Is.EqualTo(StalemateScoutCA.Verdict.Explored), "nothing stale left: not a reason to idle");
			Assert.That(StalemateScoutCA.Excused(explored), Is.False);
			Assert.That(StalemateScoutCA.Excused(StalemateScoutCA.Verdict.Attack), Is.False);
		}

		[Test]
		public void AFoundTargetIsAttackedWhateverTheScoutPickSays()
		{
			foreach (var found in new[] { true, false })
				foreach (var checks in new[] { 0, 1, 16 })
					Assert.That(StalemateScoutCA.Classify(true, found, checks), Is.EqualTo(StalemateScoutCA.Verdict.Attack));

			// The target tiers of the leader come before any hunt, so a target the bot sees or remembers is always taken before a cell is scouted.
			var order = TargetPick.Order(TargetPurpose.Main);
			Assert.That(order.ToList().IndexOf(TargetTier.Hunt), Is.GreaterThan(order.ToList().IndexOf(TargetTier.Visible)));
			Assert.That(order.ToList().IndexOf(TargetTier.Hunt), Is.GreaterThan(order.ToList().IndexOf(TargetTier.Frozen)));
			Assert.That(order.ToList().IndexOf(TargetTier.Hunt), Is.GreaterThan(order.ToList().IndexOf(TargetTier.Focus)));
		}

		[Test]
		public void TheHuntGateIsTheOldOneWithTheRowOffAndOpensFromTheStartTickWithItOn()
		{
			foreach (var now in new[] { 0, 100, 25000 })
				foreach (var existing in new[] { true, false })
					Assert.That(StalemateScoutCA.Hunts(existing, false, now, 0), Is.EqualTo(existing), "row off = the old gate");

			Assert.That(StalemateScoutCA.Hunts(false, true, 0, 0), Is.True, "from the first ready push with the default start");
			Assert.That(StalemateScoutCA.Hunts(false, true, 11999, 12000), Is.False);
			Assert.That(StalemateScoutCA.Hunts(false, true, 12000, 12000), Is.True);
			Assert.That(StalemateScoutCA.Hunts(true, true, 0, 12000), Is.True, "the old reasons stay");
		}

		[Test]
		public void ARefusedRegionCentreRestsForTheHoldAndThenIsAskedAgain()
		{
			Assert.That(StalemateScoutCA.Rejected(-1, 5000), Is.False, "never refused");
			Assert.That(StalemateScoutCA.Rejected(1000, 1000 + StalemateScoutCA.RejectHoldTicks - 1), Is.True);
			Assert.That(StalemateScoutCA.Rejected(1000, 1000 + StalemateScoutCA.RejectHoldTicks), Is.False);
			Assert.That(StalemateScoutCA.Reached(true, 1_000_000, 4), Is.True, "seen is reached");
			Assert.That(StalemateScoutCA.Reached(false, 16, 4), Is.True);
			Assert.That(StalemateScoutCA.Reached(false, 17, 4), Is.False);
		}

		// ---- the tactical-map side: the scout module offers stale regions, best first ----

		[Test]
		public void TheScoutPickOffersTheStalestRegionFirstAndNeverASeenOne()
		{
			var regions = new RegionMemory(new CPos(0, 0), new CPos(63, 63), 8);
			var stale = new Dictionary<int, int> { { 3, 5000 }, { 9, 9000 }, { 20, 2000 } };
			var offered = new List<CPos>();

			var found = ScoutBotModule.PickScoutCell(regions, new CPos(0, 0), 16, c => { offered.Add(c); return false; }, i => stale.GetValueOrDefault(i), i => 0, i => 0, out _, out var checks);

			Assert.That(found, Is.False);
			Assert.That(checks, Is.EqualTo(3), "only the three stale regions are offered; every seen or fresh region is skipped");
			Assert.That(offered[0], Is.EqualTo(regions.CenterOf(9)), "the stalest first");
			Assert.That(offered.Select(regions.IndexOf).ToList(), Is.EquivalentTo(new[] { 3, 9, 20 }));
		}

		[Test]
		public void TheScoutPickTakesTheNextRegionWhenTheBestCannotBeWalkedTo()
		{
			var regions = new RegionMemory(new CPos(0, 0), new CPos(63, 63), 8);
			var stale = new Dictionary<int, int> { { 3, 5000 }, { 9, 9000 }, { 20, 2000 } };
			var unreachable = regions.CenterOf(9);

			var found = ScoutBotModule.PickScoutCell(regions, new CPos(0, 0), 16, c => c != unreachable, i => stale.GetValueOrDefault(i), i => 0, i => 0, out var cell, out var checks);

			Assert.That(found, Is.True);
			Assert.That(checks, Is.EqualTo(2));
			Assert.That(regions.IndexOf(cell), Is.EqualTo(3), "the best region that can be walked to");
		}

		[Test]
		public void TheScoutPickIsBoundedAndReportsWhetherAnythingWasStale()
		{
			var regions = new RegionMemory(new CPos(0, 0), new CPos(63, 63), 8);
			var all = Enumerable.Range(0, regions.CellCount).ToDictionary(i => i, i => 1000 + i);

			var bounded = ScoutBotModule.PickScoutCell(regions, new CPos(0, 0), 4, _ => false, i => all[i], i => 0, i => 0, out _, out var checks);
			Assert.That(bounded, Is.False);
			Assert.That(checks, Is.EqualTo(4), "never more than maxChecks centres are offered (one path query each)");

			var none = ScoutBotModule.PickScoutCell(regions, new CPos(0, 0), 16, _ => true, i => 0, i => 0, i => 0, out _, out var noneChecks);
			Assert.That(none, Is.False);
			Assert.That(noneChecks, Is.EqualTo(0), "nothing stale = nothing to scout (Explored), distinct from stale regions that cannot be reached (Unreachable)");
			Assert.That(StalemateScoutCA.Classify(false, bounded, checks), Is.EqualTo(StalemateScoutCA.Verdict.Unreachable));
			Assert.That(StalemateScoutCA.Classify(false, none, noneChecks), Is.EqualTo(StalemateScoutCA.Verdict.Explored));
		}

		// ---- the wiring: row off = the old text; fog-safe; one row, off everywhere ----

		[Test]
		public void TheRowIsOffInCodeAndInClassicAndInTheSixBlocksAndHasItsOwnAblationId()
		{
			Assert.That(AblationIds.SsScoutPush, Is.EqualTo("ss_scout_push"));
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.StalemateScout.cs");
			Assert.That(info, Does.Contain("public readonly bool StalemateScoutEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int StalemateScoutStartTick = 0;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(yaml[classic..], Does.Not.Contain("StalemateScout"), "classic untouched");
			Assert.That(Count(yaml[..classic], "\n\t\tStalemateScoutEnabled: false\n"), Is.EqualTo(6));
			Assert.That(Count(yaml[..classic], "\n\t\tStalemateScoutStartTick: 0\n"), Is.EqualTo(6));
			Assert.That(Regex.IsMatch(yaml, "StalemateScoutEnabled: true"), Is.False, "ships OFF until its A/B passes");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.StalemateScout.cs");
			Assert.That(wiring, Does.Contain("internal bool StalemateScoutOn => ArmyLaunchArbiterOn && Info.StalemateScoutEnabled && !ablateSsScoutPush.Off(Player, AblationIds.SsScoutPush);"));
			Assert.That(wiring, Does.Contain("internal bool StalemateScoutHunts => StalemateScoutCA.Hunts(false, StalemateScoutOn, World.WorldTick, Info.StalemateScoutStartTick);"));
		}

		[Test]
		public void TheCallSitesKeepTheirOldTextBehindTheRow()
		{
			var hunt = RepoFile(Bots + "SquadManagerBotModuleCA.Stalemate01.cs");
			var cell = Between(hunt, "internal CPos? FinishHuntCell(Actor leader, bool ownPick = false)", "huntCells = new List<CPos>();");
			Assert.That(cell, Does.Contain("if (mobile == null || World.WorldTick < huntRetryAt)\n\t\t\t\treturn null;\n\n\t\t\t// STALEMATE-SCOUT-01"));
			Assert.That(cell, Does.Contain("if (StalemateScoutOn && StalemateScoutCell(leader, mobile) is { } scouted)\n\t\t\t\treturn scouted;"), "the tactical map first, the plain grid below as the fallback");
			Assert.That(Count(hunt, "StalemateScout"), Is.EqualTo(2), "one hook, nothing else in the finish-mode file");

			var team = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(team, Does.Contain("if (((TeamLeadHuntViaOrderOn && FinishModeActive) || teamLeadAllInPicking || StalemateScoutHunts)"));
			Assert.That(team, Does.Contain("teamLeadAllInPicking = false;\n\t\t\tStalemateScoutIdle(picked, now, allIn != null, blob.Units, blob.Value);\n\t\t\tif (!picked)\n\t\t\t\treturn;"), "the old pick text is intact, the DEV row is a call before it");
			Assert.That(team, Does.Contain("var order = TeamLeadIssue(home, target, targetId, now);\n\t\t\tStalemateScoutPublished(order, targetId, target, now);"));

			var pick = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			Assert.That(pick, Does.Contain("var hunts = (TeamLeadHuntViaOrderOn && FinishModeActive) || teamLeadAllInPicking || StalemateScoutHunts;"));
		}

		[Test]
		public void TheScoutReadsOnlyTheBotsOwnMemoryAndTheRecordsAreDevOnly()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.StalemateScout.cs");
			foreach (var banned in new[] { "World.Actors", "ActorMap", "WinState", "Trait<", "TraitsImplementing<Frozen", "Random", "BotRng", "Personality", "HiddenActor" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);

			Assert.That(wiring, Does.Contain("Player.Shroud.IsVisible"), "the reached evidence is the bot's own shroud");
			Assert.That(Count(wiring, "TraitsImplementing<"), Is.EqualTo(1), "the provider lookup, once, cached");

			// Only the pick, never a tick: the DEV rows are behind the writer and the 250-tick throttle, the pick itself has no loop over the world.
			Assert.That(wiring, Does.Contain("if (picked || !StalemateScoutOn || DiagnosticWriter == null || now - scoutIdleLogTick < 250)"));
			Assert.That(wiring, Does.Contain("if (!StalemateScoutOn || DiagnosticWriter == null)"));
			Assert.That(wiring, Does.Not.Contain("BotTick"));
			Assert.That(wiring, Does.Not.Contain("foreach (var actor"));

			// The DEV rows carry what audit A7 asks for: the all-in reason, the order state, eligible vs committed force, whether the target was reached.
			foreach (var field in new[] { "why=", "state=", "eligible=", "committed=", "arrived=", "prev_reached=", "cell_seen=", "verdict=", "excused=" })
				Assert.That(wiring, Does.Contain(field), field);

			var provider = RepoFile("OpenRA.Mods.Cameo/Traits/BotModules/ScoutBotModule.cs");
			var offer = Between(provider, "bool IBotScoutCellProvider.TryPickScoutCell", "internal static int PickScoutRegion(");
			foreach (var banned in new[] { "World.Actors", "ActorMap", "WinState", "Trait<", "BotRng" })
				Assert.That(offer, Does.Not.Contain(banned), banned);
		}
	}
}
