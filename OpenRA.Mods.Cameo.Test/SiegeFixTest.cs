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
using OpenRA.Primitives;
using OpenRA.Traits;

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
				(info.HysteresisBandPctEasy, info.HysteresisBandPctHard, 25, -1),
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
			Assert.That(block.Groups[1].Value, Does.Contain("HysteresisBandPctEasy: " + info.HysteresisBandPctEasy));
			Assert.That(block.Groups[1].Value, Does.Contain("HysteresisBandPctHard: " + info.HysteresisBandPctHard));
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

		// ---- S3: hysteresis ----

		static SiegeVerdict Serve(SiegeEvaluatorBotModule.SiegeHold hold, SiegeVerdict raw, int value = 2000, int band = 20, bool bypass = false)
			=> SiegeEvaluatorBotModule.Hysteresis(raw, value, band, bypass, hold);

		// ---- S1 ground-unit clause and the retreat-side hold cap (Opus ruling on Haiku 50B57A19) ----

		static OpenRA.Mods.CA.Traits.BotUnitProfile Shooter(int rangeCells, string validTargets, string invalidTargets = "")
		{
			var weapon = new BotWeaponProfile(1.0, WDist.FromCells(rangeCells), new BitSet<TargetableType>(validTargets),
				invalidTargets == "" ? default : new BitSet<TargetableType>(invalidTargets), null);
			return new OpenRA.Mods.CA.Traits.BotUnitProfile("shooter", 500, 100, "none", 100, false, false, default, new[] { weapon });
		}

		static OpenRA.Mods.CA.Traits.BotUnitProfile Tower() =>
			new("tower", 500, 400, "heavy", 0, false, true, new BitSet<TargetableType>("Structure"), new BotWeaponProfile[0]);

		[Test]
		public void S1_AGroundUnitThatOutranksTheCoveringDefenceSupportsTheStandOff()
		{
			var units = new[] { Shooter(9, "Structure") };
			Assert.That(SiegeEvaluatorBotModule.HasStandOffWeapon(units, Tower(), 9), Is.True, "range equal to the defence's");
			Assert.That(SiegeEvaluatorBotModule.HasStandOffWeapon(units, Tower(), 8), Is.True);
			Assert.That(SiegeEvaluatorBotModule.HasStandOffWeapon(units, Tower(), 10), Is.False, "shorter than the defence: it would be shot first");
		}

		[Test]
		public void S1_AWeaponThatCannotHitTheDefenceTypeDoesNotCount()
		{
			Assert.That(SiegeEvaluatorBotModule.HasStandOffWeapon(new[] { Shooter(20, "Air") }, Tower(), 9), Is.False, "anti-air with a long range");
			Assert.That(SiegeEvaluatorBotModule.HasStandOffWeapon(new[] { Shooter(20, "Structure", "Structure") }, Tower(), 9), Is.False, "invalid target set wins");
			Assert.That(SiegeEvaluatorBotModule.HasStandOffWeapon(new OpenRA.Mods.CA.Traits.BotUnitProfile[0], Tower(), 9), Is.False);
		}

		[Test]
		public void S1_TheSupportFlagDecidesStandOffVersusAdvance()
		{
			Assert.That(Decide(new SiegeEvaluatorBotModule.SiegeHold(), artillery: true), Is.EqualTo(SiegeVerdict.StandOff), "support: hold");
			Assert.That(Decide(new SiegeEvaluatorBotModule.SiegeHold(), artillery: false), Is.EqualTo(SiegeVerdict.Advance), "no support: take the fight");
			var module = Module();
			Assert.That(module, Does.Contain("var standOffSupport = artilleryAttached ||"));
			Assert.That(module, Does.Contain("HasStandOffWeapon(own.Select(o => o.Item1), longestProfile, longest.MaxRangeCells)"));
		}

		[Test]
		public void TheRetreatHoldIsCappedAtTheTierLineAndNeverHoldsForeverOnANeverGrowingSquad()
		{
			Assert.That(SiegeEvaluatorBotModule.HoldExpired(1000 + 2999, 1000, 3000), Is.False);
			Assert.That(SiegeEvaluatorBotModule.HoldExpired(1000 + 3000, 1000, 3000), Is.True);
			Assert.That(SiegeEvaluatorBotModule.HoldExpired(1000 + 999999, 1000, 0), Is.False, "no advisor: no cap");

			// The squad never grows: the cooldown keeps answering Retreat for as long as it is asked, so only the cap ends the hold.
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			SiegeEvaluatorBotModule.Decide(false, false, false, true, 2000, 0, Max, Reengage, false, hold, out _, out _);
			for (var t = 100; t <= 30 * Max; t += 100)
				Assert.That(SiegeEvaluatorBotModule.Decide(true, false, false, true, 2000, t, Max, Reengage, true, hold, out _, out _), Is.EqualTo(SiegeVerdict.Retreat), "t=" + t);

			var state = Section(GroundStates(), "class GroundUnitsSiegeRetreatStateCA", "class GroundUnitsFleeStateCA");
			var cap = state.IndexOf("SiegeEvaluatorBotModule.HoldExpired(owner.World.WorldTick, startTick, owner.SquadManager.SiegeHoldCapTicks())", StringComparison.Ordinal);
			var move = state.IndexOf("Target.FromCell(owner.World, hold)", StringComparison.Ordinal);
			Assert.That(cap, Is.GreaterThan(0));
			Assert.That(cap, Is.LessThan(move), "the cap is judged before any further pull-back order");
			Assert.That(state, Does.Contain("Retreat(owner, flee: true, rearm: true, repair: true);"), "released to the legacy regroup/rally-home path");
			Assert.That(state, Does.Contain("startTick = owner.World.WorldTick;"));
			Assert.That(state, Does.Not.Contain("DismissSquad"));
			Assert.That(Source("mods/CA/Traits/BotModules/IBotSiegeAdvisor.cs"), Does.Contain("int StandOffMaxTicks { get; }"));
		}

		[Test]
		public void S3_TheFirstEvaluationIsServedAtOnce()
		{
			Assert.That(Serve(new SiegeEvaluatorBotModule.SiegeHold(), SiegeVerdict.Retreat), Is.EqualTo(SiegeVerdict.Retreat));
		}

		[Test]
		public void S3_AChangedVerdictNeedsTwoAgreeingEvaluations()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Serve(hold, SiegeVerdict.StandOff);
			Assert.That(Serve(hold, SiegeVerdict.Retreat), Is.EqualTo(SiegeVerdict.StandOff), "one vote is not enough");
			Assert.That(Serve(hold, SiegeVerdict.Retreat), Is.EqualTo(SiegeVerdict.Retreat), "the second consecutive vote switches");
		}

		[Test]
		public void S3_AnAlternatingRawVerdictNeverFlipsTheServedOne()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Serve(hold, SiegeVerdict.StandOff);
			var flips = 0;
			var last = SiegeVerdict.StandOff;
			for (var i = 0; i < 20; i++)
			{
				var served = Serve(hold, i % 2 == 0 ? SiegeVerdict.Retreat : SiegeVerdict.StandOff);
				if (served != last)
					flips++;
				last = served;
			}

			Assert.That(flips, Is.EqualTo(0), "stand-off <-> retreat cycling on a stable squad is the RCA C4 symptom");
		}

		[Test]
		public void S3_AMaterialValueChangeSwitchesAtOnce()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Serve(hold, SiegeVerdict.StandOff, value: 2000);
			Assert.That(Serve(hold, SiegeVerdict.Retreat, value: 1601, band: 20), Is.EqualTo(SiegeVerdict.StandOff), "19.95% loss is inside the band");
			Assert.That(Serve(hold, SiegeVerdict.Advance, value: 1600, band: 20), Is.EqualTo(SiegeVerdict.Advance), "20% change is decisive");
			Assert.That(hold.ServedValue, Is.EqualTo(1600), "the band restarts from the new served value");
		}

		[Test]
		public void S3_ATimedOutHoldIsServedAtOnceAndAgreeingRawResetsThePendingVote()
		{
			var hold = new SiegeEvaluatorBotModule.SiegeHold();
			Serve(hold, SiegeVerdict.StandOff);
			Assert.That(Serve(hold, SiegeVerdict.Advance, bypass: true), Is.EqualTo(SiegeVerdict.Advance));

			Serve(hold, SiegeVerdict.Retreat);
			Assert.That(hold.PendingCount, Is.EqualTo(1));
			Serve(hold, SiegeVerdict.Advance);
			Assert.That(hold.PendingCount, Is.EqualTo(0), "the raw verdict came back: no stale vote");
		}

		[Test]
		public void S3_ARetreatTransitionIsJudgedOnTheRawVerdictNotTheHeldOne()
		{
			var module = Module();
			Assert.That(module, Does.Contain("var wasRetreating = hold.LastRaw == SiegeVerdict.Retreat;"));
			Assert.That(module, Does.Not.Contain("lastVerdictBySquad"), "the served verdict is not a transition source");
		}

		// ---- S4: one measure ----

		[Test]
		public void S4_TheCommitRatioPricesTheDefencesOnlyNotTheRegionalArmyOnTop()
		{
			var module = Module();
			Assert.That(module, Does.Contain("var obstacleValue = defenceValue * siegeMem / 100;"));
			Assert.That(module, Does.Not.Contain("(defenceValue + rememberedThreat)"));
			Assert.That(module, Does.Not.Contain("Personality moves this later"), "stale Desc (RCA F4)");
		}

		// ---- S5: defences that can reach the approach ----

		[Test]
		public void S5_ADefenceBesideTheApproachSegmentCoversItAndOneBehindTheSquadDoesNot()
		{
			var from = new CPos(0, 0);
			var to = new CPos(100, 0);
			Assert.That(SiegeEvaluatorBotModule.CoversApproach(new CPos(50, 9), 11, from, to), Is.True, "9 cells off the line, reach 11");
			Assert.That(SiegeEvaluatorBotModule.CoversApproach(new CPos(50, 12), 11, from, to), Is.False, "12 cells off the line");
			Assert.That(SiegeEvaluatorBotModule.CoversApproach(new CPos(-20, 0), 11, from, to), Is.False, "behind the squad: the segment is clamped at the squad");
			Assert.That(SiegeEvaluatorBotModule.CoversApproach(new CPos(-10, 0), 11, from, to), Is.True, "within reach of the squad itself");
			Assert.That(SiegeEvaluatorBotModule.CoversApproach(new CPos(105, 0), 11, from, to), Is.True, "beyond the target but within reach of it");
			Assert.That(SiegeEvaluatorBotModule.CoversApproach(new CPos(130, 0), 11, from, to), Is.False);
		}

		[Test]
		public void S5_ADegenerateSegmentIsAPoint()
		{
			var p = new CPos(10, 10);
			Assert.That(SiegeEvaluatorBotModule.DistanceSqToSegment(new CPos(13, 14), p, p), Is.EqualTo(25));
		}

		[Test]
		public void S5_TheTargetCentredRadiusIsGone()
		{
			Assert.That(Module(), Does.Not.Contain("RelevantDefenceRadiusCells"));
			Assert.That(typeof(SiegeEvaluatorBotModuleInfo).GetField("RelevantDefenceRadiusCells"), Is.Null);
			Assert.That(Module(), Does.Contain("CoversApproach(d.Cell, d.MaxRangeCells + Info.StandOffMarginCells, squadCell, targetCell)"));
		}

		[Test]
		public void TheEvaluatorKeepsItsPerSquadMemoryBounded()
		{
			var module = Module();
			Assert.That(module, Does.Contain("holds.Remove(stale);"), "dropped with the squad");
			Assert.That(module, Does.Contain("foreach (var stale in holds.Keys.Where(k => !verdicts.ContainsKey(k)).ToArray())"));
			Assert.That(module, Does.Contain("holds.Remove(squad);"), "dropped when no defence covers the target");
		}
	}
}
