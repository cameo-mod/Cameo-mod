#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

// PLAYTEST-PORT (Opus ruling, 2026-11-10): the yaml-OFF guard assertions in this file were flipped to the ON values
// that the ported mods/cameo/ai/ai.yaml carries (switches armed on the six personalities; classic unchanged).
// Test names are unchanged. The fixture mcv_deploy_ai.yaml is the build-time copy of mods/cameo/ai/ai.yaml (csproj Link).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	// IMPL02-AGG-TEAM (A4 allied attack intents, A5 home-reserve cap and max-strength push). Switch-ON path only; the OFF arm is pinned
	// by the guard tests at the bottom (every entry point sits behind Info.AggTeamEnabled and the OFF broadcast carries no intent).
	[TestFixture]
	public sealed class AggTeamTest
	{
		const int Cell = AggTeamPolicyCA.CellUnits;

		static WPos P(int cellsX, int cellsY = 0) => new(cellsX * Cell, cellsY * Cell, 0);

		static TeamAttackIntent Intent(WPos rendezvous, int publish = 1000, int eta = 1200, long value = 5000, int rendezvousTick = 1100) =>
			new(P(100), rendezvous, rendezvousTick, eta, publish, value, 7);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
			"tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		// ---- A4: rendezvous geometry and the intent a publisher builds ----

		[Test]
		public void TheRendezvousPointIsBackCellsFromTheTargetAlongTheApproach()
		{
			var rv = AggTeamPolicyCA.RendezvousPoint(P(100), P(50), 14);
			Assert.That(rv.X, Is.EqualTo(P(86).X).Within(2));
			Assert.That(rv.Y, Is.EqualTo(0).Within(2));
		}

		[Test]
		public void ARendezvousNeverLiesBehindThePublisher()
		{
			// The publisher is already closer than backCells to the target: the rendezvous is the publisher's own position.
			Assert.That(AggTeamPolicyCA.RendezvousPoint(P(100), P(95), 14), Is.EqualTo(P(95)));
			Assert.That(AggTeamPolicyCA.RendezvousPoint(P(100), P(100), 14), Is.EqualTo(P(100)));
		}

		[Test]
		public void TheBuiltIntentCarriesTravelTimesFromTheTickRate()
		{
			var i = AggTeamPolicyCA.BuildIntent(P(50), P(100), 2000, 6000, 9, 14, 18);
			Assert.That(i.EtaTick, Is.EqualTo(2000 + 50 * 18));
			Assert.That(i.RendezvousTick, Is.EqualTo(2000 + 36 * 18));
			Assert.That(i.PublishTick, Is.EqualTo(2000));
			Assert.That(i.Value, Is.EqualTo(6000));
			Assert.That(i.TargetActorId, Is.EqualTo(9u));
			Assert.That(i.Target, Is.EqualTo(P(100)));
		}

		// ---- A4: liveness and the choice of intent ----

		[Test]
		public void AnIntentIsLiveWhileFreshAndBeforeTheGraceAfterItsEta()
		{
			var i = Intent(P(60), publish: 1000, eta: 1200);
			Assert.That(AggTeamPolicyCA.IntentLive(i, 1320, 320, 200), Is.True, "age at the limit");
			Assert.That(AggTeamPolicyCA.IntentLive(i, 1321, 320, 200), Is.False, "one tick stale");
			Assert.That(AggTeamPolicyCA.IntentLive(i, 1400, 600, 200), Is.True, "ETA grace at the limit");
			Assert.That(AggTeamPolicyCA.IntentLive(i, 1401, 600, 200), Is.False, "the attack is over");
			Assert.That(AggTeamPolicyCA.IntentLive(null, 1000, 320, 200), Is.False);
		}

		[Test]
		public void ASquadJoinsOnlyAnIntentWhoseRendezvousIsInRange()
		{
			var near = new AggTeamPolicyCA.Candidate("a", Intent(P(60)));
			var far = new AggTeamPolicyCA.Candidate("b", Intent(P(200)));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { far }, P(40), 1100, 320, 200, 45), Is.Null);
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { far, near }, P(40), 1100, 320, 200, 45)?.Key, Is.EqualTo("a"));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { near }, P(40), 1100, 320, 200, 19), Is.Null, "20 cells away, range 19");
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { near }, P(40), 1100, 320, 200, 20)?.Key, Is.EqualTo("a"), "range is inclusive");
			Assert.That(AggTeamPolicyCA.ChooseIntent(null, P(40), 1100, 320, 200, 45), Is.Null);
		}

		[Test]
		public void AStaleIntentIsNeverChosen()
		{
			var stale = new AggTeamPolicyCA.Candidate("a", Intent(P(60), publish: 100, eta: 5000));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { stale }, P(40), 1100, 320, 200, 45), Is.Null);
		}

		[Test]
		public void TheNearestIntentWinsThenTheLargerSquadThenTheLowerKey()
		{
			var closer = new AggTeamPolicyCA.Candidate("z", Intent(P(55), value: 1000));
			var farther = new AggTeamPolicyCA.Candidate("a", Intent(P(70), value: 9000));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { farther, closer }, P(40), 1100, 320, 200, 45)?.Key, Is.EqualTo("z"));

			var big = new AggTeamPolicyCA.Candidate("z", Intent(P(60), value: 9000));
			var small = new AggTeamPolicyCA.Candidate("a", Intent(P(60), value: 1000));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { small, big }, P(40), 1100, 320, 200, 45)?.Key, Is.EqualTo("z"));

			var keyB = new AggTeamPolicyCA.Candidate("b", Intent(P(60), value: 5000));
			var keyA = new AggTeamPolicyCA.Candidate("a", Intent(P(60), value: 5000));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { keyB, keyA }, P(40), 1100, 320, 200, 45)?.Key, Is.EqualTo("a"));
			Assert.That(AggTeamPolicyCA.ChooseIntent(new[] { keyA, keyB }, P(40), 1100, 320, 200, 45)?.Key, Is.EqualTo("a"), "order independent");
		}

		// ---- A4: start together ----

		[Test]
		public void AJoinerStartsOnlyWhenAssembledAndThePublisherHasReachedTheRendezvous()
		{
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1000, 100, 70, 1100, 900, 250), Is.False, "assembled early: wait for the publisher");
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1100, 100, 70, 1100, 900, 250), Is.True, "assembled and the publisher is there");
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1100, 69, 70, 1100, 900, 250), Is.False, "not assembled yet");
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1100, 70, 70, 1100, 900, 250), Is.True, "the assembly bar is inclusive");
		}

		[Test]
		public void AJoinerNeverWaitsPastTheStartWindow()
		{
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1349, 0, 70, 1100, 900, 250), Is.False);
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1350, 0, 70, 1100, 900, 250), Is.True, "timeout at rendezvousTick + wait");
			// A joiner that joined after the publisher passed the rendezvous waits from its own join tick.
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1500, 0, 70, 1100, 1400, 250), Is.False);
			Assert.That(AggTeamPolicyCA.RendezvousCommit(1650, 0, 70, 1100, 1400, 250), Is.True);
		}

		// ---- A5: the home-reserve cap ----

		[Test]
		public void TheReserveIsTheShareOfTheOwnCombatValue()
		{
			Assert.That(AggTeamPolicyCA.ReserveValue(100000, 15), Is.EqualTo(15000), "steamroller");
			Assert.That(AggTeamPolicyCA.ReserveValue(100000, 10), Is.EqualTo(10000), "rush");
			Assert.That(AggTeamPolicyCA.ReserveValue(100000, 25), Is.EqualTo(25000), "tech");
			Assert.That(AggTeamPolicyCA.ReserveValue(100000, 40), Is.EqualTo(40000), "turtle");
			Assert.That(AggTeamPolicyCA.ReserveValue(0, 40), Is.EqualTo(0));
			Assert.That(AggTeamPolicyCA.ReserveValue(100000, 0), Is.EqualTo(0));
			Assert.That(AggTeamPolicyCA.ReserveValue(100000, 250), Is.EqualTo(100000), "clamped to the whole army");
		}

		[Test]
		public void AHomeBlockOrAReserveShortfallKeepsTheSquadHome()
		{
			Assert.That(AggTeamPolicyCA.MayLeaveHome(20000, 5000, 10000, homeBlocked: true), Is.False, "home threat or latch");
			Assert.That(AggTeamPolicyCA.MayLeaveHome(20000, 5000, 10000, homeBlocked: false), Is.True);
			Assert.That(AggTeamPolicyCA.MayLeaveHome(15000, 5000, 10000, homeBlocked: false), Is.True, "exactly the reserve stays");
			Assert.That(AggTeamPolicyCA.MayLeaveHome(14999, 5000, 10000, homeBlocked: false), Is.False, "one short of the reserve");
		}

		[Test]
		public void ThePoolAboveTheReserveLaunchesOnceTheExcessReachesTheMinimum()
		{
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(11500, 0, 10000, 1500, false), Is.True, "excess exactly the minimum");
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(11499, 0, 10000, 1500, false), Is.False);
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(11500, 0, 10000, 1500, true), Is.False, "never under a home block");
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(0, 50000, 10000, 1500, false), Is.False, "empty pool");
		}

		[Test]
		public void QuietSquadsAtHomeCoverTheReserveBeforeThePoolDoes()
		{
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(2000, 10000, 10000, 1500, false), Is.True, "quiet squads already hold the reserve");
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(2000, 9000, 10000, 1500, false), Is.False, "pool must cover the 1000 shortfall");
		}

		[Test]
		public void TheStrongestPoolUnitsAreDraftedAndTheRestStayHome()
		{
			var pool = new (uint, int)[] { (1, 300), (2, 900), (3, 600), (4, 900), (5, 100) };
			var keep = AggTeamPolicyCA.KeepForReserve(pool, 700);
			var keptValue = pool.Where(u => keep.Contains(u.Item1)).Sum(u => u.Item2);
			Assert.That(keptValue, Is.GreaterThanOrEqualTo(700));
			Assert.That(keep.Contains(2) && keep.Contains(4), Is.False, "the two strongest are drafted first");
			Assert.That(keep, Is.EquivalentTo(new uint[] { 3, 5 }), "kept value is exactly the need: 600 + 100");
		}

		[Test]
		public void AZeroNeedKeepsNobodyAndAHugeNeedKeepsEveryone()
		{
			var pool = new (uint, int)[] { (1, 300), (2, 900) };
			Assert.That(AggTeamPolicyCA.KeepForReserve(pool, 0), Is.Empty);
			Assert.That(AggTeamPolicyCA.KeepForReserve(pool, 5000), Is.EquivalentTo(new uint[] { 1, 2 }));
		}

		[Test]
		public void EqualValuesAreDraftedByLowerIdFirstSoTheKeepSetIsDeterministic()
		{
			var pool = new (uint, int)[] { (9, 500), (3, 500), (7, 500) };
			Assert.That(AggTeamPolicyCA.KeepForReserve(pool, 500), Is.EquivalentTo(new uint[] { 9 }));
			Assert.That(AggTeamPolicyCA.KeepForReserve(pool.Reverse(), 500), Is.EquivalentTo(new uint[] { 9 }));
		}

		// ---- A5: the max-strength push (the turtle rule) ----

		[Test]
		public void ThePushFiresOnTheArmyValueOrOnTheLateTick()
		{
			Assert.That(AggTeamPolicyCA.PushDue(19999, 100, 20000, 20000), Is.False);
			Assert.That(AggTeamPolicyCA.PushDue(20000, 100, 20000, 20000), Is.True, "army trigger");
			Assert.That(AggTeamPolicyCA.PushDue(0, 20000, 20000, 20000), Is.True, "late-tick trigger");
			Assert.That(AggTeamPolicyCA.PushDue(0, 19999, 20000, 20000), Is.False);
		}

		[Test]
		public void AZeroTriggerIsDisabledNotAlwaysOn()
		{
			Assert.That(AggTeamPolicyCA.PushDue(999999, 999999, 0, 0), Is.False);
			Assert.That(AggTeamPolicyCA.PushDue(30000, 0, 20000, 0), Is.True);
			Assert.That(AggTeamPolicyCA.PushDue(0, 30000, 0, 20000), Is.True);
		}

		[Test]
		public void APushDropsTheReserveToZero()
		{
			Assert.That(AggTeamPolicyCA.EffectiveReserve(true, 100000, 40), Is.EqualTo(0));
			Assert.That(AggTeamPolicyCA.EffectiveReserve(false, 100000, 40), Is.EqualTo(40000));
			Assert.That(AggTeamPolicyCA.ExcessLaunchDue(1500, 0, AggTeamPolicyCA.EffectiveReserve(true, 100000, 40), 1500, false), Is.True,
				"a push launches the whole pool");
		}

		// ---- the squad manager's real states and the carried intent ----

		[Test]
		public void AttackAndQuietAreDecidedOverTheRealStateClasses()
		{
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(new GroundUnitsAttackMoveStateCA()), Is.True);
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(new GroundUnitsAttackState()), Is.True);
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(new GroundUnitsIdleStateCA()), Is.False);
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(new GroundUnitsFleeStateCA()), Is.False, "a fleeing squad publishes nothing");
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(new AggTeamRendezvousStateCA(Intent(P(60)), 0)), Is.False, "joiners do not chain");
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(null), Is.False);
			Assert.That(SquadManagerBotModuleCA.AggTeamIsQuietState(new GroundUnitsIdleStateCA()), Is.True);
			Assert.That(SquadManagerBotModuleCA.AggTeamIsQuietState(new GroundUnitsAttackMoveStateCA()), Is.False);
			Assert.That(SquadManagerBotModuleCA.AggTeamIsQuietState(new AggTeamRendezvousStateCA(Intent(P(60)), 0)), Is.False);
		}

		[Test]
		public void TheBroadcastCarriesTheIntentAndDefaultsToNone()
		{
			Assert.That(TeamBroadcast.Empty.AttackIntent, Is.Null);
			var intent = Intent(P(60));
			var b = new TeamBroadcast(1, 0, 0, 0, DirectorPhase.BuildUp, null, false, WPos.Zero, attackIntent: intent);
			Assert.That(b.AttackIntent, Is.SameAs(intent));
			Assert.That(new TeamBroadcast(1, 0, 0, 0, DirectorPhase.BuildUp, null, false, WPos.Zero).AttackIntent, Is.Null);
		}

		// ---- OFF identity ----

		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static SquadManagerBotModuleCA Shell(bool enabled)
		{
			var info = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(nameof(SquadManagerBotModuleCAInfo.AggTeamEnabled)).SetValue(info, enabled);
			var m = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			typeof(SquadManagerBotModuleCA).BaseType.GetField("Info", Any).SetValue(m, info);
			return m;
		}

		[Test]
		public void OffPublishesNoIntentWithoutTouchingTheWorld()
		{
			var m = Shell(false);
			Assert.That(((IBotAttackIntentSource)m).AttackIntent, Is.Null);
			Assert.That(new SquadManagerBotModuleCAInfo().AggTeamEnabled, Is.False, "the code default is off");
		}

		[Test]
		public void EveryWorldFacingEntryPointSitsBehindTheFlag()
		{
			var text = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(text, Does.Contain("if (Info.AggTeamEnabled)\n\t\t\t\tAggTeamTick(bot);"));
			Assert.That(text, Does.Contain("if (Info.AggTeamEnabled)\n\t\t\t\t\t\t\tAggTeamPrepare(s);"));
			Assert.That(text, Does.Contain("var aggKeep = aggForce ? AggTeamTakeReserve() : Info.AggTeamEnabled ? AggTeamTakeDetached() : null;"));
			Assert.That(text.Split("AggTeamTick(bot)").Length - 1, Is.EqualTo(1), "one call site");
			Assert.That(text.Split("AggTeamPrepare(s)").Length - 1, Is.EqualTo(1), "one call site");
			Assert.That(text.Split("CreateAttackForce(bot").Length - 1, Is.EqualTo(1), "the ordinary call never passes aggForce");
			var wiring = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs");
			Assert.That(wiring, Does.Contain("Info.AggTeamEnabled && !IsTraitDisabled ? AggTeamComputeIntent() : null"));
			Assert.That(wiring, Does.Contain("CreateAttackForce(bot, aggForce: true)"));
		}

		[Test]
		public void AForcedLaunchIsTheOnlyPathThatBypassesTheOrdinaryBars()
		{
			var text = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(text, Does.Contain("void CreateAttackForce(IBot bot, bool aggForce = false)"));
			Assert.That(text, Does.Contain("if (aggForce || AttackForceEvalCA.ShouldLaunch("));
			Assert.That(text, Does.Contain("if (aggKeep != null)\n\t\t\t\t\tunitsHangingAroundTheBase.AddRange(aggKeep);"), "the reserve is put back");
		}

		[Test]
		public void TheBroadcastReadsTheIntentThroughTheSeamOnly()
		{
			var text = Source("mods/Cameo/Traits/BotModules/BotSituation.cs");
			Assert.That(text, Does.Contain("TraitsImplementing<IBotAttackIntentSource>().FirstEnabledTraitOrDefault()?.AttackIntent"));
			Assert.That(text, Does.Contain("attackIntent: attackIntent"));
		}

		[Test]
		public void TheJoinStateIsEnteredFromOutsideAndLeavesGroundStatesUntouched()
		{
			var states = Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Not.Contain("AggTeam"));
		}

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static string Wiring() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs");

		[Test]
		public void TheJoinChecksTheTypeTheStateTheBreakerSquadTheCooldownTheHomeBlockAndTheReserve()
		{
			// The world wiring needs a live World (the test project has none): pin each guard of AggTeamPrepare in source, in order.
			var prep = Section(Wiring(), "void AggTeamPrepare(SquadCA s)", "internal void AggTeamNoteJoinFailed");
			foreach (var guard in new[]
			{
				"s.Type != SquadCAType.Rush", "ReferenceEquals(s, stalemateSquad)", "!AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState)",
				"s.Units.Count < (aggFinishActive ? AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishJoinMinUnits)) : AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamJoinMinUnits)))", "now < cooldown.Value", "var blocked = provider == null || defence.RecallAll || AggTeamSquadDefends(s);", "if (blocked)",
				"AggTeamPolicyCA.MayLeaveHome(AggTeamHomeValue(), SquadValueOf(s), reserve, blocked)", "AggTeamPolicyCA.ChooseIntent(AggTeamCandidates()",
			})
				Assert.That(prep, Does.Contain(guard), guard);
			Assert.That(prep.IndexOf("if (!AggTeamResolveTarget(s,", StringComparison.Ordinal), Is.LessThan(prep.IndexOf("ChangeState(s, new AggTeamRendezvousStateCA", StringComparison.Ordinal)),
				"the target is resolved before the squad is sent");
		}

		[Test]
		public void TheIntentIsBuiltOnlyFromAnAttackingRushSquadWithAValidTargetAndEnoughUnits()
		{
			var compute = Section(Wiring(), "TeamAttackIntent AggTeamComputeIntent()", "List<AggTeamPolicyCA.Candidate> AggTeamCandidates()");
			foreach (var guard in new[] { "s.Type != SquadCAType.Rush", "s.Units.Count < AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamIntentMinUnits))", "!AggTeamIsAttackState(s.FuzzyStateMachine?.CurrentState)", "!s.IsTargetValid", "value > bestValue" })
				Assert.That(compute, Does.Contain(guard), guard);
		}

		[Test]
		public void TheReserveCapTickRespectsTheInitialDelayTheHomeBlockAndTheQuietSquads()
		{
			var tick = Section(Wiring(), "void AggTeamTick(IBot bot)", "List<UnitWposWrapper> AggTeamTakeReserve()");
			foreach (var guard in new[]
			{
				"if (now < initialAttackDelay)", "var blocked = provider == null || defence.RecallAll;", "AggTeamUpdateFinish(provider);", "if (blocked)",
				"AggTeamPolicyCA.ExcessLaunchDue(poolValue, AggTeamQuietSquadValue(), reserve, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamMinExcessValue)), blocked)",
				"AggTeamPolicyCA.EffectiveReserve(AggTeamPushActive(now), aggFinishActive, provider.StalemateOwnArmy, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamReserveSharePercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishMinReservePercent)))",
			})
				Assert.That(tick, Does.Contain(guard), guard);
			Assert.That(tick.IndexOf("AggTeamUpdateFinish(provider);", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("AggTeamPolicyCA.ExcessLaunchDue(", StringComparison.Ordinal)), "the finish latch is updated before the launch decision");
		}

		// ---- A7: the finishing push ----

		[Test]
		public void TheAllInStartsAtTheFinishRatioAndNeedsAKnownEnemyAndAnArmyFloor()
		{
			Assert.That(AggTeamPolicyCA.FinishStep(false, 10000, 10000, 10000, 200, 130, 6000, false), Is.True, "20000 vs 10000 is exactly 2.0");
			Assert.That(AggTeamPolicyCA.FinishStep(false, 10000, 9999, 10000, 200, 130, 6000, false), Is.False, "one short of the ratio");
			Assert.That(AggTeamPolicyCA.FinishStep(false, 5000, 0, 1000, 200, 130, 6000, false), Is.False, "below the army floor");
			Assert.That(AggTeamPolicyCA.FinishStep(false, 50000, 0, 0, 200, 130, 6000, false), Is.False, "an empty enemy memory is not a win");
			Assert.That(AggTeamPolicyCA.FinishStep(false, 6000, 0, 3000, 200, 130, 6000, false), Is.True, "the floor is inclusive");
		}

		[Test]
		public void AnAlliedArmyCountsTowardsTheRatio()
		{
			Assert.That(AggTeamPolicyCA.FinishStep(false, 6000, 14000, 10000, 200, 130, 6000, false), Is.True);
			Assert.That(AggTeamPolicyCA.FinishStep(false, 6000, 0, 10000, 200, 130, 6000, false), Is.False);
		}

		[Test]
		public void TheAllInHoldsUntilTheRatioFallsBelowTheExitPercent()
		{
			// 14000 vs 10000 = 140%: not enough to enter, enough to stay.
			Assert.That(AggTeamPolicyCA.FinishStep(false, 14000, 0, 10000, 200, 130, 6000, false), Is.False);
			Assert.That(AggTeamPolicyCA.FinishStep(true, 14000, 0, 10000, 200, 130, 6000, false), Is.True, "hysteresis keeps it on");
			Assert.That(AggTeamPolicyCA.FinishStep(true, 12900, 0, 10000, 200, 130, 6000, false), Is.False, "below the exit percent it ends");
			Assert.That(AggTeamPolicyCA.FinishStep(true, 14000, 0, 10000, 120, 150, 6000, false), Is.True, "the exit percent never exceeds the entry percent (120)");
		}

		[Test]
		public void AnEnemySeatWithoutProductionStartsTheAllInRegardlessOfTheRatio()
		{
			Assert.That(AggTeamPolicyCA.FinishStep(false, 0, 0, 999999, 200, 130, 6000, true), Is.True);
			Assert.That(AggTeamPolicyCA.FinishStep(true, 0, 0, 999999, 200, 130, 6000, true), Is.True);
		}

		[Test]
		public void TheAllInDropsTheReserveToTheFinishMinimumAndAHomeBlockStillWins()
		{
			Assert.That(AggTeamPolicyCA.EffectiveReserve(false, true, 100000, 40, 3), Is.EqualTo(3000), "turtle 40% -> 3%");
			Assert.That(AggTeamPolicyCA.EffectiveReserve(false, false, 100000, 40, 3), Is.EqualTo(40000));
			Assert.That(AggTeamPolicyCA.EffectiveReserve(false, true, 100000, 2, 3), Is.EqualTo(2000), "never above the personality share");
			Assert.That(AggTeamPolicyCA.EffectiveReserve(true, true, 100000, 40, 3), Is.EqualTo(0), "a push is zero");
			Assert.That(AggTeamPolicyCA.MayLeaveHome(100000, 5000, 3000, homeBlocked: true), Is.False);
		}

		// ---- A8: target value scoring ----

		static AggTeamPolicyCA.TargetCandidate Target(uint id, int value, int defence, int weight, bool key, int distCells, long seatArmy = 0) =>
			new(id, value, defence, weight, key, (long)distCells * Cell * distCells * Cell, seatArmy);

		[Test]
		public void AnUndefendedKeyBuildingBeatsADefendedOneEvenWhenFarther()
		{
			var defendedNear = Target(1, 2000, 4000, 300, true, 10);
			var undefendedFar = Target(2, 1000, 0, 100, true, 55);
			Assert.That(AggTeamPolicyCA.PickTarget(new[] { defendedNear, undefendedFar }, 40, 0, 60), Is.EqualTo(2u));
		}

		[Test]
		public void AnUndefendedBuildingBeyondReachDoesNotOverrideTheScore()
		{
			var defendedNear = Target(1, 20000, 0, 100, false, 10);
			var undefendedFar = Target(2, 1000, 0, 100, true, 61);
			Assert.That(AggTeamPolicyCA.PickTarget(new[] { defendedNear, undefendedFar }, 40, 0, 60), Is.EqualTo(1u));
		}

		[Test]
		public void ADefendedNonKeyTargetNeverCountsAsUndefendedKey()
		{
			var plainUndefended = Target(1, 100, 0, 100, false, 10);
			var keyDefended = Target(2, 30000, 5, 300, true, 10);
			Assert.That(AggTeamPolicyCA.PickTarget(new[] { plainUndefended, keyDefended }, 40, 0, 60), Is.EqualTo(2u), "no tier-1 member: best score wins");
		}

		[Test]
		public void ScoreFallsWithDefenceAndDistanceAndRisesWithValueAndWeight()
		{
			var baseline = AggTeamPolicyCA.TargetScore(Target(1, 1000, 0, 100, false, 40), 40, 0, 0);
			Assert.That(AggTeamPolicyCA.TargetScore(Target(1, 1000, 1000, 100, false, 40), 40, 0, 0), Is.LessThan(baseline), "defence");
			Assert.That(AggTeamPolicyCA.TargetScore(Target(1, 1000, 0, 100, false, 80), 40, 0, 0), Is.LessThan(baseline), "distance");
			Assert.That(AggTeamPolicyCA.TargetScore(Target(1, 2000, 0, 100, false, 40), 40, 0, 0), Is.GreaterThan(baseline), "value");
			Assert.That(AggTeamPolicyCA.TargetScore(Target(1, 1000, 0, 300, false, 40), 40, 0, 0), Is.GreaterThan(baseline), "class weight");
			Assert.That(AggTeamPolicyCA.TargetScore(Target(1, 1000, 0, 100, false, 40), 40, 0, 0), Is.EqualTo(1000L * 100 * 100 * 500 / 1), "half distance = factor 0.5");
		}

		[Test]
		public void TheWeakestSeatGetsTheBonusAndTheStrongestGetsNone()
		{
			var strong = Target(1, 1000, 0, 100, false, 20, seatArmy: 10000);
			var weak = Target(2, 1000, 0, 100, false, 20, seatArmy: 0);
			Assert.That(AggTeamPolicyCA.TargetScore(strong, 40, 50, 10000), Is.EqualTo(AggTeamPolicyCA.TargetScore(strong, 40, 0, 10000)));
			Assert.That(AggTeamPolicyCA.TargetScore(weak, 40, 50, 10000), Is.GreaterThan(AggTeamPolicyCA.TargetScore(weak, 40, 0, 10000)));
			Assert.That(AggTeamPolicyCA.PickTarget(new[] { strong, weak }, 40, 50, 60), Is.EqualTo(2u));
		}

		[Test]
		public void EqualScoresGoToTheNearerThenTheLowerId()
		{
			var a = Target(5, 1000, 0, 100, false, 20);
			var b = Target(3, 1000, 0, 100, false, 20);
			Assert.That(AggTeamPolicyCA.PickTarget(new[] { a, b }, 40, 0, 60), Is.EqualTo(3u));
			Assert.That(AggTeamPolicyCA.PickTarget(new[] { b, a }, 40, 0, 60), Is.EqualTo(3u));
			Assert.That(AggTeamPolicyCA.PickTarget(Array.Empty<AggTeamPolicyCA.TargetCandidate>(), 40, 0, 60), Is.Null);
			Assert.That(AggTeamPolicyCA.PickTarget(null, 40, 0, 60), Is.Null);
		}

		[Test]
		public void TheTargetHookIsTheLegacyNearestPickWhileOffAndOnlyForRushSquads()
		{
			var text = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(text.Split("AggTeamPickVisible(visible, sourceActor, owner)").Length - 1, Is.EqualTo(4), "both squad overloads, both fog branches");
			var pick = Section(Wiring(), "Actor AggTeamPickVisible(", "\t}\n}");
			Assert.That(pick, Does.Contain("var nearest = visible.ClosestToIgnoringPath(source.CenterPosition);"));
			Assert.That(pick, Does.Contain("if (!Info.AggTeamEnabled || owner == null || owner.Type != SquadCAType.Rush || visible.Count < 2 || AggTeamSquadDefends(owner))\n\t\t\t\treturn nearest;"));
			Assert.That(pick, Does.Contain("AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetWeakSeatBonusPercent)) * (aggFinishActive ? Math.Max(1, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishWeakSeatMultiplier))) : 1)"));
		}

		[Test]
		public void TheAllInAssignsIdleSquadsEvenWithNoAttackToJoin()
		{
			var prep = Section(Wiring(), "void AggTeamPrepare(SquadCA s)", "internal void AggTeamNoteJoinFailed");
			Assert.That(prep, Does.Contain("else if (aggFinishActive && AggTeamPushIntent(s, now) is { } pushIntent)"));
			Assert.That(prep, Does.Contain("aggFinishActive ? AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishJoinMinUnits)) : AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamJoinMinUnits))"));
			var update = Section(Wiring(), "void AggTeamUpdateFinish(", "bool AggTeamEnemySeatWithoutProduction()");
			Assert.That(update, Does.Contain("provider.StalemateOwnArmy, provider.StalemateAlliedArmy, provider.StalemateEnemyArmy"));
			Assert.That(update, Does.Contain("AggTeamEnemySeatWithoutProduction()"));
			var tick = Section(Wiring(), "void AggTeamTick(IBot bot)", "List<UnitWposWrapper> AggTeamTakeReserve()");
			Assert.That(tick.IndexOf("AggTeamUpdateFinish(provider);", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("ExcessLaunchDue", StringComparison.Ordinal)), "the latch is updated before the launch decision");
		}

		[Test]
		public void FinishIsOffWhileTheFlagIsOffAndExposedToTheGroundStates()
		{
			var m = Shell(false);
			var prop = typeof(SquadManagerBotModuleCA).GetProperty("AggTeamFinishActive", Any);
			Assert.That(prop, Is.Not.Null);
			Assert.That((bool)prop.GetValue(m), Is.False);
			typeof(SquadManagerBotModuleCA).GetField("aggFinishActive", Any).SetValue(m, true);
			Assert.That((bool)prop.GetValue(m), Is.False, "a latched value never leaks while the flag is off");
		}

		// ---- A9: the parameter registry ----

		[Test]
		public void EveryAggTeamValueIsANamedYamlFieldWithADocumentedRange()
		{
			var fields = typeof(SquadManagerBotModuleCAInfo).GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(f => f.Name.StartsWith("AggTeam", StringComparison.Ordinal) && f.FieldType == typeof(int)).ToList();
			Assert.That(fields.Count, Is.GreaterThanOrEqualTo(30));
			foreach (var f in fields)
			{
				var desc = f.GetCustomAttribute<DescAttribute>();
				Assert.That(desc, Is.Not.Null, f.Name);
				Assert.That(string.Join(" ", desc.Lines), Does.Match(@"Range -?\d+-\d+"), f.Name + " documents its range");
			}
		}

		[Test]
		public void EveryShippedDefaultLiesInsideItsDocumentedRange()
		{
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var f in typeof(SquadManagerBotModuleCAInfo).GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(f => f.Name.StartsWith("AggTeam", StringComparison.Ordinal) && f.FieldType == typeof(int)))
			{
				var m = System.Text.RegularExpressions.Regex.Match(string.Join(" ", f.GetCustomAttribute<DescAttribute>().Lines), @"Range (-?\d+)-(\d+)");
				var value = (int)f.GetValue(info);
				Assert.That(value, Is.InRange(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)), f.Name);
			}
		}

		[Test]
		public void ThePersonalityValuesLieInsideTheirRanges()
		{
			foreach (var kv in Personalities())
				Assert.That(kv.Value.AggTeamReserveSharePercent, Is.InRange(0, 60), kv.Key);
			var turtle = Personalities()["turtle"];
			Assert.That(turtle.AggTeamPushArmyValue, Is.InRange(0, 100000));
			Assert.That(turtle.AggTeamPushLateTick, Is.InRange(0, 60000));
		}

		// ---- A9: the layered resolver (Opus ruling D1912510) ----

		const string Reserve = nameof(SquadManagerBotModuleCAInfo.AggTeamReserveSharePercent);

		static SquadManagerBotModuleCAInfo InfoWith(int reserve, Action<SquadManagerBotModuleCAInfo> tables = null)
		{
			var info = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(Reserve).SetValue(info, reserve);
			tables?.Invoke(info);
			return info;
		}

		[Test]
		public void WithEmptyTablesTheResolverReturnsTheFieldValueForEveryFactionPair()
		{
			var info = InfoWith(40);
			Assert.That(AggressionParams.Resolve(info, Reserve, null, null), Is.EqualTo(40));
			Assert.That(AggressionParams.Resolve(info, Reserve, "gdi", "nod"), Is.EqualTo(40));
			Assert.That(AggressionParams.Resolve(info, Reserve, "Soviet", "ALLIES"), Is.EqualTo(40));
		}

		[Test]
		public void EveryAggTeamParameterResolvesToItsDefaultWithNoTables()
		{
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var f in typeof(SquadManagerBotModuleCAInfo).GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(f => f.Name.StartsWith("AggTeam", StringComparison.Ordinal) && f.FieldType == typeof(int)))
				Assert.That(AggressionParams.Resolve(info, f.Name, "gdi", "nod"), Is.EqualTo((int)f.GetValue(info)), f.Name);
		}

		[Test]
		public void ThePersonalityFactionAndMatchupLayersMultiply()
		{
			var info = InfoWith(40, i =>
			{
				i.AggMultPersonality[Reserve] = 1500;
				i.AggMultFaction["gdi/" + Reserve] = 800;
				i.AggMultMatchup["gdi>nod/" + Reserve] = 500;
			});
			// 40 x 1.5 x 0.8 x 0.5 = 24
			Assert.That(AggressionParams.Resolve(info, Reserve, "gdi", "nod"), Is.EqualTo(24));
			// another enemy: the matchup layer is 1.0 -> 40 x 1.5 x 0.8 = 48
			Assert.That(AggressionParams.Resolve(info, Reserve, "gdi", "allies"), Is.EqualTo(48));
			// another own faction: faction and matchup layers are 1.0 -> 40 x 1.5 = 60
			Assert.That(AggressionParams.Resolve(info, Reserve, "nod", "nod"), Is.EqualTo(60));
			// no enemy: the matchup layer is skipped
			Assert.That(AggressionParams.Resolve(info, Reserve, "gdi", null), Is.EqualTo(48));
		}

		[Test]
		public void TheMatchupLayerPrefersTheExactRowThenAnyOwnThenAnyEnemy()
		{
			var info = InfoWith(40, i =>
			{
				i.AggMultMatchup["gdi>nod/" + Reserve] = 500;
				i.AggMultMatchup["*>nod/" + Reserve] = 2000;
				i.AggMultMatchup["gdi>*/" + Reserve] = 1500;
			});
			Assert.That(AggressionParams.Resolve(info, Reserve, "gdi", "nod"), Is.EqualTo(20), "exact");
			Assert.That(AggressionParams.Resolve(info, Reserve, "allies", "nod"), Is.EqualTo(60), "any own faction versus nod (x2.0 = 80, clamped to 60)");
			Assert.That(AggressionParams.Resolve(info, Reserve, "gdi", "soviet"), Is.EqualTo(60), "gdi versus any (x1.5)");
			Assert.That(AggressionParams.Resolve(info, Reserve, "allies", "soviet"), Is.EqualTo(40), "no row");
		}

		[Test]
		public void TheResultIsClampedToTheDocumentedRange()
		{
			var high = InfoWith(40, i => i.AggMultPersonality[Reserve] = 5000);
			Assert.That(AggressionParams.Resolve(high, Reserve, null, null), Is.EqualTo(60), "max of Range 0-60");
			var low = InfoWith(40, i => i.AggMultPersonality[Reserve] = 100);
			Assert.That(AggressionParams.Resolve(low, Reserve, null, null), Is.EqualTo(4), "x0.1 stays inside the range");
			var edge = InfoWith(60, i => i.AggMultPersonality[Reserve] = 1000);
			Assert.That(AggressionParams.Resolve(edge, Reserve, null, null), Is.EqualTo(60));
			Assert.That(AggressionParams.Apply(10, 20, 30, 1000, 1000, 1000), Is.EqualTo(20), "below the min is raised");
			Assert.That(AggressionParams.Apply(100, 20, 30, 1000, 1000, 1000), Is.EqualTo(30), "above the max is cut");
		}

		[Test]
		public void AnOutOfBoundsMultiplierIsClampedBeforeItIsApplied()
		{
			Assert.That(AggressionParams.Apply(100, 0, 100000, 0, 1000, 1000), Is.EqualTo(10), "a zero row acts as 0.1");
			Assert.That(AggressionParams.Apply(100, 0, 100000, 99999, 1000, 1000), Is.EqualTo(500), "a huge row acts as 5.0");
			Assert.That(AggressionParams.Apply(100, 0, 100000, -5, 1000, 1000), Is.EqualTo(10), "a negative row acts as 0.1");
		}

		[Test]
		public void TheLayeringRoundsToTheNearestInteger()
		{
			Assert.That(AggressionParams.Apply(15, 0, 100, 1000, 1000, 1500), Is.EqualTo(23), "22.5 rounds up");
			Assert.That(AggressionParams.Apply(15, 0, 100, 1000, 1000, 1400), Is.EqualTo(21), "21.0");
			Assert.That(AggressionParams.Apply(15, 0, 100, 1000, 1000, 1030), Is.EqualTo(15), "15.45 rounds down");
		}

		[Test]
		public void AnUnknownOrUnrangedNameThrowsInsteadOfResolvingToZero()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.Throws<ArgumentException>(() => AggressionParams.Resolve(info, "AggTeamNoSuchParameter", "gdi", "nod"));
			Assert.Throws<ArgumentException>(() => AggressionParams.Resolve(info, null, "gdi", "nod"));
			Assert.Throws<ArgumentException>(() => AggressionParams.Resolve(info, nameof(SquadManagerBotModuleCAInfo.AggTeamEnabled), "gdi", "nod"), "a bool is not a parameter");
			Assert.Throws<ArgumentException>(() => AggressionParams.Resolve(info, nameof(SquadManagerBotModuleCAInfo.SquadSize), "gdi", "nod"), "no documented range");
		}

		[Test]
		public void TheRangeIsReadFromTheDescription()
		{
			Assert.That(AggressionParams.RangeOf(Reserve), Is.EqualTo((0, 60)));
			Assert.That(AggressionParams.RangeOf(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishRatioPercent)), Is.EqualTo((120, 400)));
		}

		[Test]
		public void TheTablesDefaultToEmptyAndLoadFromYamlChildNodes()
		{
			var defaults = new SquadManagerBotModuleCAInfo();
			Assert.That(defaults.AggMultPersonality, Is.Empty);
			Assert.That(defaults.AggMultFaction, Is.Empty);
			Assert.That(defaults.AggMultMatchup, Is.Empty);
			var yaml = "SquadManagerBotModuleCA:\n\tAggMultFaction:\n\t\tgdi/AggTeamReserveSharePercent: 800\n\tAggMultMatchup:\n\t\tgdi>nod/AggTeamReserveSharePercent: 500\n";
			var node = MiniYaml.FromString(yaml, "t").Single().Value;
			var info = FieldLoader.Load<SquadManagerBotModuleCAInfo>(node);
			Assert.That(info.AggMultFaction["gdi/AggTeamReserveSharePercent"], Is.EqualTo(800));
			Assert.That(info.AggMultMatchup["gdi>nod/AggTeamReserveSharePercent"], Is.EqualTo(500));
		}

		[Test]
		public void NoShippedPersonalityCarriesAMultiplierRow()
		{
			foreach (var kv in Personalities())
			{
				Assert.That(kv.Value.AggMultPersonality, Is.Empty, kv.Key);
				Assert.That(kv.Value.AggMultFaction, Is.Empty, kv.Key);
				Assert.That(kv.Value.AggMultMatchup, Is.Empty, kv.Key);
			}
		}

		[Test]
		public void EveryAggTeamValueTheWiringReadsGoesThroughTheResolver()
		{
			foreach (var file in new[] { "mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs", "mods/CA/Traits/BotModules/Squads/States/AggTeamStatesCA.cs" })
			{
				var text = Source(file);
				var raw = System.Text.RegularExpressions.Regex.Matches(text, @"(?<![A-Za-z])[Ii]nfo\.AggTeam(?!Enabled\b)\w+");
				Assert.That(raw.Count, Is.EqualTo(0), file + " reads a raw AggTeam field: " + string.Join(",", raw.Select(m => m.Value)));
			}
		}

		[Test]
		public void TheManagerHelperResolvesWithTheSeatsOwnFactionAndTheEnemysFaction()
		{
			var wiring = Wiring();
			Assert.That(wiring, Does.Contain("AggressionParams.Resolve(Info, name, Player.Faction?.InternalName, enemy?.Faction?.InternalName)"));
			Assert.That(wiring, Does.Contain("internal int AggP(string name, Player enemy = null)"));
		}

		[Test]
		public void TheMatchupLayerReceivesTheTargetSeatForTheTargetWeights()
		{
			var pick = Section(Wiring(), "Actor AggTeamPickVisible(", "\t}\n}");
			Assert.That(pick, Does.Contain("AggTeamTargetKeyWeightPercent), a.Owner)"));
			Assert.That(pick, Does.Contain("AggTeamTargetEconomyWeightPercent), a.Owner)"));
			Assert.That(pick, Does.Contain("AggTeamTargetOtherWeightPercent), a.Owner)"));
		}

		// ---- A9 strategic form: weighted geometric mean over the enemy seats of M(own coalition, f_e) ----

		static AggressionParams.EnemySeat Seat(string faction, long weight) => new(faction, weight);

		[Test]
		public void TheStrategicAggregateIsExactlyTheFieldValueAtDefaults()
		{
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var f in typeof(SquadManagerBotModuleCAInfo).GetFields(BindingFlags.Instance | BindingFlags.Public)
				.Where(f => f.Name.StartsWith("AggTeam", StringComparison.Ordinal) && f.FieldType == typeof(int)))
			{
				var want = (int)f.GetValue(info);
				Assert.That(AggressionParams.ResolveStrategic(info, f.Name, new[] { "gdi", "nod" }, new[] { Seat("allies", 5000), Seat("soviet", 100) }), Is.EqualTo(want), f.Name);
				Assert.That(AggressionParams.ResolveStrategic(info, f.Name, null, null), Is.EqualTo(want), f.Name + " (no coalition, no enemy)");
			}
		}

		[Test]
		public void TheStrategicAggregateIsTheWeightedGeometricMeanForTwoEnemies()
		{
			// base 1000-permille field; value 100 inside Range 0-5000 would clamp, so use the reserve (Range 0-60) at 40 and test the multiplier through Apply bounds.
			var info = InfoWith(40, i =>
			{
				i.AggMultMatchup["gdi>nod/" + Reserve] = 2000;
				i.AggMultMatchup["gdi>soviet/" + Reserve] = 500;
			});
			// equal weights: exp((ln2 + ln0.5) / 2) = 1.0 -> 40
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi" }, new[] { Seat("nod", 100), Seat("soviet", 100) }), Is.EqualTo(40));
			// weights 3:1 -> exp(0.75 ln2 + 0.25 ln0.5) = 2^0.5 = 1.41421 -> 40 x 1.41421 = 56.57 -> 57
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi" }, new[] { Seat("nod", 300), Seat("soviet", 100) }), Is.EqualTo(57));
			// weights 1:3 -> 2^-0.5 = 0.70711 -> 28.28 -> 28
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi" }, new[] { Seat("nod", 100), Seat("soviet", 300) }), Is.EqualTo(28));
			// a single enemy is the plain matchup: 40 x 2.0 = 80, clamped to 60
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi" }, new[] { Seat("nod", 100) }), Is.EqualTo(60));
		}

		[Test]
		public void TheOwnCoalitionIsTheGeometricMeanOfTheMembersAndWeightlessSeatsAreIgnored()
		{
			var info = InfoWith(40, i =>
			{
				i.AggMultFaction["gdi/" + Reserve] = 2000;
				i.AggMultFaction["nod/" + Reserve] = 500;
				i.AggMultMatchup["gdi>soviet/" + Reserve] = 1500;
			});
			// gdi + nod: faction layers 2.0 and 0.5 -> geometric mean 1.0; the matchup row belongs to gdi only: exp(ln1.5 / 2) = 1.2247 -> 49
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi", "nod" }, new[] { Seat("soviet", 100) }), Is.EqualTo(49));
			// own faction alone, no enemy seat: faction layer only (2.0 -> 80, clamped to 60)
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi" }, null), Is.EqualTo(60));
			// zero-weight and unnamed seats are dropped, leaving the soviet seat only
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi", "nod" },
				new[] { Seat("soviet", 100), Seat("allies", 0), Seat(null, 500) }), Is.EqualTo(49));
		}

		[Test]
		public void TheStrategicAggregateAppliesThePersonalityLayerAndTheRangeClamp()
		{
			var info = InfoWith(40, i => i.AggMultPersonality[Reserve] = 1500);
			Assert.That(AggressionParams.ResolveStrategic(info, Reserve, new[] { "gdi" }, new[] { Seat("nod", 100) }), Is.EqualTo(60));
			var low = InfoWith(40, i => i.AggMultPersonality[Reserve] = 500);
			Assert.That(AggressionParams.ResolveStrategic(low, Reserve, new[] { "gdi" }, new[] { Seat("nod", 100) }), Is.EqualTo(20));
			Assert.Throws<ArgumentException>(() => AggressionParams.ResolveStrategic(low, "AggTeamNoSuchParameter", new[] { "gdi" }, null));
		}

		[Test]
		public void TheStrategicParametersGoThroughTheAggregateAndTacticalOnesDoNot()
		{
			var text = Wiring();
			foreach (var name in new[] { "ReserveSharePercent", "FinishMinReservePercent", "MinExcessValue", "PushArmyValue", "PushLateTick",
				"PushHoldTicks", "PushCooldownTicks", "FinishRatioPercent", "FinishExitRatioPercent", "FinishMinArmyValue" })
			{
				Assert.That(text, Does.Not.Contain("AggP(nameof(SquadManagerBotModuleCAInfo.AggTeam" + name + ")"), name + " must be strategic");
				Assert.That(text, Does.Contain("AggS(nameof(SquadManagerBotModuleCAInfo.AggTeam" + name + "))"), name);
			}

			Assert.That(text, Does.Contain("AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetKeyWeightPercent), a.Owner)"), "A8 stays per-engagement");
			Assert.That(text, Does.Not.Contain("AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamTarget"), "A8 target weights are tactical");
			Assert.That(text, Does.Not.Contain("AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamJoin"), "A4 joins are tactical");
		}

		[Test]
		public void TheStrategicHelperUsesTheCoalitionTheEnemySeatsTheWeightFloorAndRefreshesPerTick()
		{
			var helper = Section(Wiring(), "internal int AggS(string name)", "Dictionary<Player, long> AggTeamSeatArmies()");
			foreach (var guard in new[]
			{
				"if (aggStrategicTick != now)", "aggStrategicValues.Clear();", "aggCoalition = new List<string> { Player.Faction?.InternalName };",
				"p == Player || p.NonCombatant || p.WinState != WinState.Undefined", "if (Player.IsAlliedWith(p))", "aggCoalition.Add(p.Faction?.InternalName);",
				"PlayerRelationship.Enemy", "armies.GetValueOrDefault(p) + floor", "AggTeamEnemyWeightFloorValue",
				"AggressionParams.ResolveStrategic(Info, name, aggCoalition, aggEnemySeats)",
			})
				Assert.That(helper, Does.Contain(guard), guard);
		}

		// ---- A3 merge: a quiet remnant goes back to the pool ----

		[Test]
		public void ARemnantIsBelowTheUnitFloorOrTheValueFloor()
		{
			Assert.That(AggTeamPolicyCA.IsTinyRemnant(2, 5000, 3, 1000), Is.True, "units");
			Assert.That(AggTeamPolicyCA.IsTinyRemnant(5, 999, 3, 1000), Is.True, "value");
			Assert.That(AggTeamPolicyCA.IsTinyRemnant(3, 1000, 3, 1000), Is.False, "at the floors is not tiny");
		}

		[Test]
		public void ARemnantMergesOnlyAfterItHasStayedOneForTheMergeTicks()
		{
			Assert.That(AggTeamPolicyCA.TinyMergeDue(true, 100, 699, 600), Is.False);
			Assert.That(AggTeamPolicyCA.TinyMergeDue(true, 100, 700, 600), Is.True);
			Assert.That(AggTeamPolicyCA.TinyMergeDue(false, 100, 5000, 600), Is.False, "a squad that is no longer tiny never merges");
			Assert.That(AggTeamPolicyCA.TinyMergeDue(true, -1, 5000, 600), Is.False, "never seen tiny");
			Assert.That(AggTeamPolicyCA.TinyMergeDue(true, 100, 100, 0), Is.True, "zero ticks merges at once");
		}

		[Test]
		public void TheMergeTouchesOnlyQuietRushSquadsNeverTheBreakerSquadAndRunsAfterTheHomeBlock()
		{
			var merge = Section(Wiring(), "void AggTeamMergeTiny(int now)", "void AggTeamUpdatePush(");
			foreach (var guard in new[]
			{
				"s.Type != SquadCAType.Rush", "ReferenceEquals(s, stalemateSquad)", "!AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState)",
				"AggTeamPolicyCA.IsTinyRemnant(s.Units.Count, SquadValueOf(s),", "AggTeamPolicyCA.TinyMergeDue(true, since, now,", "DismissSquad(s);", "return;",
			})
				Assert.That(merge, Does.Contain(guard), guard);
			var tick = Section(Wiring(), "void AggTeamTick(IBot bot)", "List<UnitWposWrapper> AggTeamTakeReserve()");
			Assert.That(tick.IndexOf("if (blocked)", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("AggTeamMergeTiny(now);", StringComparison.Ordinal)), "merge runs after the home block");
		}

		// ---- the yaml values (named parameters, per personality) ----

		static IReadOnlyDictionary<string, SquadManagerBotModuleCAInfo> Personalities()
		{
			var nodes = MiniYaml.FromString(Fixture("mcv_deploy_ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			return nodes.Where(n => n.Key.StartsWith("SquadManagerBotModuleCA@", StringComparison.Ordinal))
				.ToDictionary(n => n.Key.Split('@')[1], n => FieldLoader.Load<SquadManagerBotModuleCAInfo>(n.Value));
		}

		[Test]
		public void EachPersonalityCarriesItsNamedReserveShare()
		{
			var p = Personalities();
			Assert.That(p["steamroller"].AggTeamReserveSharePercent, Is.EqualTo(15));
			Assert.That(p["rush"].AggTeamReserveSharePercent, Is.EqualTo(10));
			Assert.That(p["tech"].AggTeamReserveSharePercent, Is.EqualTo(25));
			Assert.That(p["turtle"].AggTeamReserveSharePercent, Is.EqualTo(40));
		}

		[Test]
		public void OnlyTheTurtleCarriesAPushTrigger()
		{
			var p = Personalities();
			Assert.That(p["turtle"].AggTeamPushArmyValue, Is.GreaterThan(0));
			Assert.That(p["turtle"].AggTeamPushLateTick, Is.GreaterThan(0));
			foreach (var name in new[] { "rush", "tech", "expansion", "steamroller", "guerrilla" })
			{
				Assert.That(p[name].AggTeamPushArmyValue, Is.EqualTo(0), name);
				Assert.That(p[name].AggTeamPushLateTick, Is.EqualTo(0), name);
			}
		}

		[Test]
		public void NoPersonalityEnablesTheFlagInTheShippedYaml()
		{
			foreach (var kv in Personalities())
				Assert.That(kv.Value.AggTeamEnabled, Is.EqualTo(kv.Key != "classic"), kv.Key);
			Assert.That(Fixture("mcv_deploy_ai.yaml"), Does.Contain("AggTeamEnabled: true"), "only the switch group arms it (ported ON values)");
		}
	}
}
