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
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// SIEGE-FIX-01 (RCA plan SIEGE_FIX_01_RCA_PLAN_02, Opus D1 + ratified S8): the verdict ladder is pure and tested directly (S1, S2, S6);
	// the state wiring and the yaml/source pins cover what has no headless world fixture. First real exercise: the M0 measurement run.
	[TestFixture]
	public sealed class SiegeFixTest
	{
		const int Max = 3000;
		const int Reengage = 40;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Module() => Source("mods/CA/Traits/BotModules/SiegeEvaluatorBotModule.cs");

		static string GroundStates() => Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");

		static string Section(string text, string from, string to)
		{
			var a = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(a, Is.GreaterThanOrEqualTo(0), from);
			var b = text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
			Assert.That(b, Is.GreaterThan(a), to);
			return text.Substring(a, b - a);
		}

		static SiegeVerdict Decide(SiegeEvaluatorBotModule.SiegeHold hold, bool ownWins = true, bool committed = false, bool artillery = true,
			int value = 2000, int now = 0, bool wasRetreating = false, bool inside = false, int max = Max, int pct = Reengage)
			=> SiegeEvaluatorBotModule.Decide(ownWins, committed, inside, artillery, value, now, max, pct, wasRetreating, hold, out _, out _);

		// ---- S1: a stand-off is held only with artillery attached ----

		[Test]
		public void S1_StandOffNeedsArtillery()
		{
			Assert.That(Decide(new SiegeEvaluatorBotModule.SiegeHold(), artillery: true), Is.EqualTo(SiegeVerdict.StandOff));
			Assert.That(Decide(new SiegeEvaluatorBotModule.SiegeHold(), artillery: false), Is.EqualTo(SiegeVerdict.Advance),
				"predicted-won fight, no artillery: take it, never park outside it");
		}

		[Test]
		public void S1_APredictedLossRetreatsWithOrWithoutArtillery()
		{
			foreach (var artillery in new[] { true, false })
				Assert.That(Decide(new SiegeEvaluatorBotModule.SiegeHold(), ownWins: false, artillery: artillery), Is.EqualTo(SiegeVerdict.Retreat));
		}

		[Test]
		public void S1_CommitIsAdvanceWhateverTheArtillery()
		{
			foreach (var artillery in new[] { true, false })
				Assert.That(Decide(new SiegeEvaluatorBotModule.SiegeHold(), committed: true, artillery: artillery), Is.EqualTo(SiegeVerdict.Advance));
		}

		[Test]
		public void S1_ArtilleryLostEndsTheHoldAndResetsTheTimer()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Assert.That(Decide(hold, now: 0), Is.EqualTo(SiegeVerdict.StandOff));
			Assert.That(Decide(hold, now: 100, artillery: false), Is.EqualTo(SiegeVerdict.Advance));
			Assert.That(hold.StandOffSince, Is.EqualTo(-1), "no artillery: no hold running");
			Assert.That(Decide(hold, now: 200), Is.EqualTo(SiegeVerdict.StandOff), "artillery back: a fresh hold starts");
			Assert.That(hold.StandOffSince, Is.EqualTo(200));
		}

		// ---- S2: the hold ends by decision ----

		[Test]
		public void S2_TheHoldTimesOutIntoACommit()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Assert.That(Decide(hold, now: 1000), Is.EqualTo(SiegeVerdict.StandOff));
			Assert.That(Decide(hold, now: 1000 + Max - 100), Is.EqualTo(SiegeVerdict.StandOff), "just before the limit");
			Assert.That(Decide(hold, now: 1000 + Max), Is.EqualTo(SiegeVerdict.Advance), "at the limit the predicted-won fight is taken");
			Assert.That(hold.TimedOut, Is.True);
		}

		[Test]
		public void S2_ATimedOutSquadStaysCommittedUntilARetreat()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Decide(hold, now: 0);
			Decide(hold, now: Max);
			for (var t = Max + 100; t < Max + 1000; t += 100)
				Assert.That(Decide(hold, now: t), Is.EqualTo(SiegeVerdict.Advance), "no second hold after the decision: t=" + t);

			Assert.That(Decide(hold, now: Max + 1000, ownWins: false), Is.EqualTo(SiegeVerdict.Retreat));
			Assert.That(hold.TimedOut, Is.False, "a retreat ends the latch");
		}

		[Test]
		public void S2_ALongerTierLimitHoldsLonger()
		{
			var easy = new SiegeEvaluatorBotModule.SiegeHold();
			var hard = new SiegeEvaluatorBotModule.SiegeHold();
			Decide(easy, now: 0, max: 3000);
			Decide(hard, now: 0, max: 1500);
			Assert.That(Decide(easy, now: 1500, max: 3000), Is.EqualTo(SiegeVerdict.StandOff));
			Assert.That(Decide(hard, now: 1500, max: 1500), Is.EqualTo(SiegeVerdict.Advance), "the stronger tier decides sooner");
		}

		// ---- S6: the re-engage cooldown ----

		[Test]
		public void S6_ARetreatWaitsForTheSquadToGrow()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Assert.That(SiegeEvaluatorBotModule.Decide(false, false, false, true, 2000, 0, Max, Reengage, false, hold, out _, out var first), Is.EqualTo(SiegeVerdict.Retreat));
			Assert.That(first, Is.True, "the transition is reported once (failure memory)");
			Assert.That(hold.ReengageBase, Is.EqualTo(2000));

			// The fight now reads as won (memory drifted), but the squad has not grown: still out.
			Assert.That(SiegeEvaluatorBotModule.Decide(true, false, false, true, 2000, 100, Max, Reengage, true, hold, out var label, out var again), Is.EqualTo(SiegeVerdict.Retreat));
			Assert.That(again, Is.False, "a pass spent pinned does not stack a second failure");
			Assert.That(label, Is.EqualTo("retreat(reengage-cooldown)"));
			Assert.That(Decide(hold, value: 2000 * 140 / 100 - 1, now: 200, wasRetreating: true), Is.EqualTo(SiegeVerdict.Retreat), "39.9% growth is not enough at 40%");

			Assert.That(Decide(hold, value: 2000 * 140 / 100, now: 300, wasRetreating: true), Is.EqualTo(SiegeVerdict.StandOff), "grown by 40%: the cooldown ends");
			Assert.That(hold.ReengageBase, Is.EqualTo(-1));
		}

		[Test]
		public void S6_AnOverwhelmingCommitBypassesTheCooldown()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold { ReengageBase = 2000 };
			Assert.That(Decide(hold, committed: true, value: 2000, wasRetreating: true), Is.EqualTo(SiegeVerdict.Advance));
			Assert.That(hold.ReengageBase, Is.EqualTo(-1));
		}

		[Test]
		public void S6_AStrongerTierReengagesOnASmallerGrowth()
		{
			var easy = new SiegeEvaluatorBotModule.SiegeHold { ReengageBase = 1000 };
			var hard = new SiegeEvaluatorBotModule.SiegeHold { ReengageBase = 1000 };
			Assert.That(Decide(easy, value: 1250, pct: 38, wasRetreating: true), Is.EqualTo(SiegeVerdict.Retreat));
			Assert.That(Decide(hard, value: 1250, pct: 20, wasRetreating: true), Is.EqualTo(SiegeVerdict.StandOff));
		}

		// ---- S8: the ladder and the flag ----

		[Test]
		public void S8_TheKnobsAreOneEqualStepLineWithNoZeroEndpoint()
		{
			var info = new SiegeEvaluatorBotModuleInfo();
			foreach (var (lo, hi, start, step) in new[]
			{
				(info.StandOffMaxTicksEasy, info.StandOffMaxTicksHard, 3000, -150),
				(info.SiegeReengagePctEasy, info.SiegeReengagePctHard, 40, -2),
			})
			{
				// noob = rank -1 .. cameogod = rank 9 on the shared ladder arithmetic (r = rank + 1).
				var tiers = Enumerable.Range(-1, 11).Select(rank => BotDifficultyLadder.InterpolateByRank(lo, hi, rank, BotDifficultyLadder.Difficulties.Length)).ToArray();
				Assert.That(tiers, Is.EqualTo(Enumerable.Range(0, 11).Select(r => start + step * r).ToArray()));
				Assert.That(tiers.Min(), Is.GreaterThan(0), "no tier reaches zero");
			}
		}

		[Test]
		public void S8_TheYamlWritesTheSameLineAsTheCodeDefaults()
		{
			var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n");
			var block = Regex.Match(yaml, @"^\tSiegeEvaluatorBotModule:\n((?:\t\t.*\n|[ \t]*\n)+)", RegexOptions.Multiline);
			Assert.That(block.Success, Is.True);
			var info = new SiegeEvaluatorBotModuleInfo();
			Assert.That(block.Groups[1].Value, Does.Contain("StandOffMaxTicksEasy: " + info.StandOffMaxTicksEasy));
			Assert.That(block.Groups[1].Value, Does.Contain("StandOffMaxTicksHard: " + info.StandOffMaxTicksHard));
			Assert.That(block.Groups[1].Value, Does.Contain("SiegeReengagePctEasy: " + info.SiegeReengagePctEasy));
			Assert.That(block.Groups[1].Value, Does.Contain("SiegeReengagePctHard: " + info.SiegeReengagePctHard));
		}

		[Test]
		public void S8_NoBehaviourFlagExistsAnywhere()
		{
			Assert.That(Module(), Does.Not.Contain("public readonly bool BehaviourEnabled"));
			Assert.That(Module(), Does.Not.Contain("Info.BehaviourEnabled"));
			var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml"));
			Assert.That(yaml, Does.Not.Contain("BehaviourEnabled"));
			Assert.That(typeof(SiegeEvaluatorBotModuleInfo).GetField("BehaviourEnabled"), Is.Null);
		}

		[Test]
		public void S8_TheVerdictIsServedWithoutASwitch()
		{
			var serve = Section(Module(), "public SiegeVerdict VerdictFor(SquadCA squad, out CPos standOffCell)", "return cached.Verdict;");
			Assert.That(serve, Does.Contain("if (IsTraitDisabled || !verdicts.TryGetValue(squad, out var cached))"));
		}

		// ---- S6 wiring: a siege retreat never dismisses ----

		[Test]
		public void S6_TheSiegeRetreatStateAsksTheRallyOnceAndNeverDismisses()
		{
			var state = Section(GroundStates(), "class GroundUnitsSiegeRetreatStateCA", "class GroundUnitsFleeStateCA");
			var ask = state.IndexOf("TryRetreatRally(owner, \"siege_retreat\", out var plan)", StringComparison.Ordinal);
			var note = state.IndexOf("RetreatRallyNoteFlight(owner);", StringComparison.Ordinal);
			var hold = state.IndexOf("Target.FromCell(owner.World, hold)", StringComparison.Ordinal);
			Assert.That(note, Is.GreaterThanOrEqualTo(0));
			Assert.That(ask, Is.GreaterThan(note), "mark first, then the guarded ask");
			Assert.That(hold, Is.GreaterThan(ask), "the pull-back follows the ask");
			Assert.That(state, Does.Contain("if (!decided)"), "one ask per entry");
			Assert.That(state, Does.Contain("ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);"));
			Assert.That(state, Does.Contain("ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);"), "any other verdict returns to the march");
			Assert.That(state, Does.Not.Contain("DismissSquad"));
			Assert.That(state, Does.Not.Contain("GoToRandomOwnBuilding"));
		}

		[Test]
		public void S6_TheMarchSendsASiegeRetreatToTheSiegeStateNotToTheFleeState()
		{
			var march = Section(GroundStates(), "class GroundUnitsAttackMoveStateCA", "// INC-N combat veto");
			Assert.That(march, Does.Contain("if (siegeVerdict == SiegeVerdict.Retreat && !AggressionHoldsMarch(owner))"));
			Assert.That(march, Does.Contain("new GroundUnitsSiegeRetreatStateCA(standOffCell)"));
			var retreat = Section(march, "if (siegeVerdict == SiegeVerdict.Retreat", "if (siegeVerdict == SiegeVerdict.StandOff");
			Assert.That(retreat, Does.Not.Contain("GroundUnitsFleeStateCA"), "the Flee state dismisses the squad");
		}

		[Test]
		public void S6_TheSiegeRetreatCountsAsARetreatForLiveness()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs"), Does.Contain("or GroundUnitsSiegeRetreatStateCA;"));
		}

		[Test]
		public void TheEvaluatorKeepsItsPerSquadMemoryBounded()
		{
			var module = Module();
			Assert.That(module, Does.Contain("holds.Remove(stale);"), "dropped with the squad");
			Assert.That(module, Does.Contain("holds.Remove(squad);"), "dropped when no defence covers the target");
		}
	}
}
