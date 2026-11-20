#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// QW-B (STAGGER_DESIGN_06 section 15): the shared-phase rule and the site arithmetic around it. World-free: the
	// phase is a pure function of (rank, N_fam, period), the rank of (SpawnKey, PlayerIndex), and each converted site
	// reduces to IsDue / NextDue / a countdown start. T1a (flag off) and the N = 1 identity are the first group.
	[TestFixture]
	public sealed class BotSlotPhaseTest
	{
		// The periods the converted sites use: lease prune / InMatchAdapt / RegionRoles / TacticalMap / ExpansionPlanner /
		// Stalemate diagnostics 250, BotSituation snapshot 150, BotSituation emergency + Radar 25, SquadManager roles +
		// EngagementLog sample 50.
		static readonly int[] Periods = [25, 50, 150, 250];

		// ---- T1a / N = 1 identity -------------------------------------------------------------------------------

		[Test]
		public void FlagIsOffByDefault()
		{
			Assert.That(new ModularBotInfo().SlotPhasedTimers, Is.False, "default off: T1a no-op");
			Assert.That(new ModularBotInfo().RoundRobinFamily, Is.False);
		}

		[Test]
		public void OneBotKeepsPhaseZeroForEveryPeriod()
		{
			foreach (var k in Periods)
			{
				Assert.That(BotSlotPhase.Phase(0, 1, k), Is.EqualTo(0), $"K={k}");
				Assert.That(BotSlotPhase.Phase(3, 1, k), Is.EqualTo(0), "a single seat never offsets, whatever rank it is handed");
			}
		}

		[Test]
		public void RankZeroAlwaysKeepsPhaseZero()
		{
			for (var n = 1; n <= 12; n++)
				foreach (var k in Periods)
					Assert.That(BotSlotPhase.Phase(0, n, k), Is.EqualTo(0), $"N={n} K={k}");
		}

		[Test]
		public void PhaseZeroIsTheAbsoluteModulo()
		{
			foreach (var k in Periods)
				for (var t = 0; t < 1000; t++)
					Assert.That(BotSlotPhases.IsDue(t, k, 0), Is.EqualTo(t % k == 0), $"K={k} t={t}");
		}

		[Test]
		public void PeriodOfOneIsAlwaysDue()
		{
			for (var t = 0; t < 10; t++)
			{
				Assert.That(BotSlotPhases.IsDue(t, 1, 0), Is.True);
				Assert.That(BotSlotPhases.NextDue(t, 1, 0), Is.EqualTo(t));
			}
		}

		// ---- the phase rule -------------------------------------------------------------------------------------

		[Test]
		public void PhaseIsFloorOfRankTimesPeriodOverCount()
		{
			for (var n = 1; n <= 12; n++)
				foreach (var k in Periods)
					for (var rank = 0; rank < n; rank++)
					{
						var phase = BotSlotPhase.Phase(rank, n, k);
						Assert.That(phase, Is.EqualTo(rank * k / n), $"N={n} K={k} rank={rank}");
						Assert.That(phase, Is.InRange(0, k - 1));
					}
		}

		[Test]
		public void SixBotsOnA250PeriodSpreadEvenly()
		{
			var phases = Enumerable.Range(0, 6).Select(i => BotSlotPhase.Phase(i, 6, 250)).ToArray();
			Assert.That(phases, Is.EqualTo(new[] { 0, 41, 83, 125, 166, 208 }));
		}

		[Test]
		public void PhasesAreDistinctAndOrderedWhenThePeriodCoversTheCount()
		{
			// N_max is 12 and the shortest period is 25, so every site separates every bot.
			for (var n = 1; n <= 12; n++)
				foreach (var k in Periods)
				{
					var phases = Enumerable.Range(0, n).Select(i => BotSlotPhase.Phase(i, n, k)).ToArray();
					Assert.That(phases, Is.Ordered.Ascending.And.Unique, $"N={n} K={k}");
				}
		}

		[Test]
		public void NoTwoBotsFireOnOneTickWhereTodayAllDo()
		{
			// The spike: today every bot's 250-tick work lands on tick % 250 == 0. With phases, at most one bot per tick.
			const int Bots = 6;
			const int K = 250;
			var phases = Enumerable.Range(0, Bots).Select(i => BotSlotPhase.Phase(i, Bots, K)).ToArray();

			var worst = 0;
			var today = 0;
			for (var t = 0; t < 5 * K; t++)
			{
				worst = System.Math.Max(worst, phases.Count(p => BotSlotPhases.IsDue(t, K, p)));
				today = System.Math.Max(today, Enumerable.Repeat(0, Bots).Count(p => BotSlotPhases.IsDue(t, K, p)));
			}

			Assert.That(today, Is.EqualTo(Bots), "control: with every phase 0 all bots fire together");
			Assert.That(worst, Is.EqualTo(1), "phase-offset: never two bots on one tick");
		}

		[Test]
		public void AnOutOfRangeRankIsClamped()
		{
			Assert.That(BotSlotPhase.Phase(99, 4, 250), Is.EqualTo(3 * 250 / 4));
			Assert.That(BotSlotPhase.Phase(-1, 4, 250), Is.EqualTo(0));
		}

		// ---- ranks ----------------------------------------------------------------------------------------------

		[Test]
		public void RanksFollowSpawnPointThenPlayerIndex()
		{
			Assert.That(BotSlotPhase.Ranks([(3, 0), (1, 1), (2, 2)]), Is.EqualTo(new[] { 2, 0, 1 }));
			Assert.That(BotSlotPhase.Ranks([(1, 5), (1, 2)]), Is.EqualTo(new[] { 1, 0 }), "a spawn tie falls to the player index");
		}

		[Test]
		public void SeatsWithoutASpawnPointRankLast()
		{
			Assert.That(BotSlotPhase.Ranks([(BotSlotPhase.NoSpawnKey, 0), (4, 3), (BotSlotPhase.NoSpawnKey, 1)]), Is.EqualTo(new[] { 1, 0, 2 }));
		}

		[Test]
		public void RanksAreAPermutationAndIndependentOfInputOrder()
		{
			var seats = new List<(int SpawnKey, int PlayerIndex)> { (5, 7), (2, 3), (BotSlotPhase.NoSpawnKey, 9), (2, 1), (8, 0), (5, 2) };
			var ranks = BotSlotPhase.Ranks(seats);
			Assert.That(ranks.OrderBy(r => r), Is.EqualTo(Enumerable.Range(0, seats.Count)));

			var reversed = seats.AsEnumerable().Reverse().ToList();
			var reversedRanks = BotSlotPhase.Ranks(reversed);
			for (var i = 0; i < seats.Count; i++)
				Assert.That(reversedRanks[seats.Count - 1 - i], Is.EqualTo(ranks[i]), $"seat {seats[i]}");
		}

		// ---- the site arithmetic --------------------------------------------------------------------------------

		[Test]
		public void IsDueFiresOncePerPeriodOnThePhase()
		{
			const int K = 250;
			const int Phase = 83;
			var fired = Enumerable.Range(0, 10 * K).Where(t => BotSlotPhases.IsDue(t, K, Phase)).ToArray();
			Assert.That(fired, Has.Length.EqualTo(10));
			Assert.That(fired.All(t => t % K == Phase));
			Assert.That(fired[0], Is.EqualTo(Phase), "never before the phase: the first run waits at most one period");
		}

		[Test]
		public void IsDueMatchesTheDesignFormula()
		{
			// design: (WorldTick - phase) % K == 0, read for ticks at or after the phase.
			foreach (var k in Periods)
				foreach (var phase in new[] { 0, 1, k / 3, k - 1 })
					for (var t = phase; t < phase + 4 * k; t++)
						Assert.That(BotSlotPhases.IsDue(t, k, phase), Is.EqualTo((t - phase) % k == 0));
		}

		[Test]
		public void NextDueIsTheFirstOnPhaseTickAtOrAfterNow()
		{
			foreach (var k in new[] { 25, 250 })
				foreach (var phase in new[] { 0, 1, k / 2, k - 1 })
					for (var now = 0; now < 3 * k; now++)
					{
						var due = BotSlotPhases.NextDue(now, k, phase);
						Assert.That(due, Is.GreaterThanOrEqualTo(now));
						Assert.That(due - now, Is.LessThan(k), "the first, not a later one");
						Assert.That(BotSlotPhases.IsDue(due, k, phase), Is.True, $"K={k} phase={phase} now={now}");
					}
		}

		// Countdown sites (lease prune, BridgeRepair): `if (--c > 0) return; c = K; run`. Returns the 1-based calls that run.
		static List<int> CountdownRuns(int initial, int reload, int calls)
		{
			var runs = new List<int>();
			var c = initial;
			for (var call = 1; call <= calls; call++)
			{
				if (--c > 0)
					continue;

				c = reload;
				runs.Add(call);
			}

			return runs;
		}

		[Test]
		public void LeasePruneCountdownAtPhaseZeroIsTheOldFirstCallPrune()
		{
			// Old initial countdown 0; QW-B seeds phase + 1 = 1. Same run calls, forever.
			Assert.That(CountdownRuns(1, 250, 2000), Is.EqualTo(CountdownRuns(0, 250, 2000)));
			Assert.That(CountdownRuns(1, 250, 2000).Take(3), Is.EqualTo(new[] { 1, 251, 501 }));
		}

		[Test]
		public void LeasePruneCountdownAtAPhaseRunsPhasePlusOneCallsLater()
		{
			var runs = CountdownRuns(83 + 1, 250, 1000);
			Assert.That(runs, Is.EqualTo(new[] { 84, 334, 584, 834 }));
		}

		[Test]
		public void ExpansionReplanAtPhaseZeroIsTheOldModulo()
		{
			for (var calls = 1; calls <= 2000; calls++)
				Assert.That(ExpansionPlannerBotModule.IsReplanCall(calls, 250, 0), Is.EqualTo(calls % 250 == 0), $"call {calls}");
		}

		[Test]
		public void ExpansionReplanAtAPhaseWaitsAndThenKeepsThePhase()
		{
			var runs = Enumerable.Range(1, 1200).Where(c => ExpansionPlannerBotModule.IsReplanCall(c, 250, 83)).ToArray();
			Assert.That(runs, Is.EqualTo(new[] { 333, 583, 833, 1083 }), "never before call 250, then every 250 on the phase");
		}

		// ---- TacticalMap: the BotRng draw stays in the stream -----------------------------------------------------

		// One ResetPerBaseCaches territory/region-ownership step as the module runs it: the draw is made at the call site
		// (always, flag or not) and handed to NextRefreshTick.
		static int ResetStep(MersenneTwister rng, int now, int interval, bool phased, int phase) =>
			TacticalMapBotModule.NextRefreshTick(now, interval, rng.Next(interval), phased, phase);

		[Test]
		public void TacticalMapFlagOffIsTodaysDrawExactly()
		{
			const int Seed = 0x5EED;
			var site = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(Seed, 2), nameof(TacticalMapBotModule)));
			var reference = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(Seed, 2), nameof(TacticalMapBotModule)));

			for (var i = 0; i < 40; i++)
			{
				var now = 100 + 37 * i;
				Assert.That(ResetStep(site, now, 250, false, 0), Is.EqualTo(now + reference.Next(250)));
			}
		}

		[Test]
		public void TacticalMapStreamPositionIsTheSameWithTheFlagOnOrOff()
		{
			// The identity the design asks for is a stream-position identity, not a tick identity: after any number of
			// resets, the next draw on the module's stream is the same value whether the phase was used or discarded.
			const int Seed = 0x5EED;
			var key = nameof(TacticalMapBotModule);
			var off = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(Seed, 4), key));
			var on = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(Seed, 4), key));

			for (var i = 0; i < 25; i++)
			{
				var now = 1 + 53 * i;
				var ticks = ResetStep(on, now, 250, true, 41);
				ResetStep(off, now, 250, false, 0);
				Assert.That(BotSlotPhases.IsDue(ticks, 250, 41), Is.True, "phased: lands on the phase");
				Assert.That(ticks, Is.GreaterThanOrEqualTo(now));
			}

			for (var i = 0; i < 16; i++)
				Assert.That(on.Next(), Is.EqualTo(off.Next()), $"draw {i} after the resets");
		}

		[Test]
		public void TacticalMapOneBotUsesTheAbsolutePhaseZeroNotTheDraw()
		{
			// N = 1 gives phase 0, which is NOT today's random offset (only if the old draw had been 0): so the tick differs,
			// the stream position does not (previous test).
			Assert.That(TacticalMapBotModule.NextRefreshTick(1000, 250, 77, true, 0), Is.EqualTo(1000));
			Assert.That(TacticalMapBotModule.NextRefreshTick(1001, 250, 77, true, 0), Is.EqualTo(1250));
			Assert.That(TacticalMapBotModule.NextRefreshTick(1001, 250, 77, false, 0), Is.EqualTo(1078));
		}
	}
}
