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

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA;

namespace OpenRA.Mods.Cameo.Test
{
	// STAGGER_DESIGN_06 9.5 TimerSim for the never-reset call counter with a freeze (TIMER_TABLE_06 C001/C002): the old
	// form `if (++ticks % K == 0)` stepped literally against BotCallPeriod.
	[TestFixture]
	public sealed class BotCallPeriodTest
	{
		const int Horizon = 6000;

		static readonly int[] Ks = [1, 2, 25, 150, 250];

		// The old form, literally: one counter, one increment per CALL, never reset; no call, no increment (the freeze).
		static List<int> Legacy(int k, Func<int, bool> called)
		{
			var fired = new List<int>();
			var ticks = 0;
			for (var t = 1; t <= Horizon; t++)
			{
				if (!called(t))
					continue;

				if (++ticks % k == 0)
					fired.Add(t);
			}

			return fired;
		}

		// The new form: the bot is visited every n ticks starting at firstVisit; the module is called on those visits where
		// `called` is true. Elapsed is the bot's own visit gap, n.
		static List<int> New(int k, int n, int firstVisit, Func<int, bool> called)
		{
			var fired = new List<int>();
			var period = new BotCallPeriod();
			for (var t = firstVisit; t <= Horizon; t += n)
				if (called(t) && period.Call(t, n, k))
					fired.Add(t);

			return fired;
		}

		// A fixed pseudo-random disabled pattern: stretches of 1..40 ticks, about one per 50 ticks.
		static bool[] RandomPattern(int seed)
		{
			var on = new bool[Horizon + 2];
			var state = (uint)seed;
			var off = 0;
			for (var t = 1; t < on.Length; t++)
			{
				state = unchecked(state * 1103515245u + 12345u);
				if (off == 0 && (state >> 16) % 50 == 0)
					off = 1 + (int)((state >> 8) % 40);

				on[t] = off == 0;
				if (off > 0)
					off--;
			}

			return on;
		}

		[Test]
		public void N1_EveryCall_MatchesTheModulo()
		{
			foreach (var k in Ks)
				Assert.That(New(k, 1, 1, _ => true), Is.EqualTo(Legacy(k, _ => true)), $"K={k}");
		}

		[Test]
		public void N1_DisabledStretch_FreezesLikeTheCounter()
		{
			Func<int, bool> called = t => t < 101 || t > 150;
			foreach (var k in Ks)
				Assert.That(New(k, 1, 1, called), Is.EqualTo(Legacy(k, called)), $"K={k}");
		}

		[Test]
		public void N1_RandomDisabledStretches_MatchTheCounter()
		{
			foreach (var seed in new[] { 1, 7, 12345 })
			{
				var on = RandomPattern(seed);
				Func<int, bool> called = t => on[t];
				foreach (var k in Ks)
					Assert.That(New(k, 1, 1, called), Is.EqualTo(Legacy(k, called)), $"seed={seed} K={k}");
			}
		}

		[Test]
		public void N1_FirstCallIsCallOne()
		{
			// Enabled from tick 10: call 1 is at tick 10, so call 250 is at 259 and call 500 at 509.
			Func<int, bool> called = t => t >= 10;
			Assert.That(New(250, 1, 10, called).Take(2).ToArray(), Is.EqualTo(new[] { 259, 509 }));
			Assert.That(Legacy(250, called).Take(2).ToArray(), Is.EqualTo(new[] { 259, 509 }));
		}

		[Test]
		public void Gated_PeriodStaysKTicks_LateByAtMostNMinus1()
		{
			// k = 250 is at least N_max (12), so no deadline is ever coalesced.
			const int k = 250;
			foreach (var n in new[] { 2, 3, 4, 8, 12 })
			{
				var lastVisit = Horizon / n * n;
				var expected = new List<int>();
				for (var j = 1; ; j++)
				{
					var due = n - 1 + k * j; // armed at the first visit n: due0 = (n - 1) + K, then += K
					var visit = (due + n - 1) / n * n; // the first visit at or after the due
					if (visit > lastVisit)
						break;

					expected.Add(visit);
				}

				var fired = New(k, n, n, _ => true);
				Assert.That(fired, Is.EqualTo(expected), $"N={n}");
				for (var i = 0; i < fired.Count; i++)
					Assert.That(fired[i] - (n - 1 + k * (i + 1)), Is.InRange(0, n - 1), $"N={n} firing {i}");
			}
		}

		[Test]
		public void Gated_DisabledVisits_MoveTheDueBackByTheSkippedTime()
		{
			// N = 4, K = 250: first visit 4 arms due 253. Not called on the visits 300..396 (25 visits = 100 ticks).
			Func<int, bool> called = t => t < 300 || t > 396;
			var noGap = New(250, 4, 4, _ => true);
			var gap = New(250, 4, 4, called);

			var lastVisit = Horizon / 4 * 4;
			var expected = noGap.Where(f => f < 300)
				.Concat(noGap.Where(f => f >= 300).Select(f => f + 100).Where(f => f <= lastVisit))
				.ToList();
			Assert.That(gap, Is.EqualTo(expected));
			Assert.That(gap.First(f => f > 300) - noGap.First(f => f > 300), Is.EqualTo(100));
		}

		// B044 / B045: the UnitBuilder BotTick state machine, the two old counters against two BotCallPeriod. Each tick is in
		// one of three states (0 free, 1 base-builder pause, 2 foreign pause); a state lasts 1..120 ticks. The foreign pause
		// returns before either counter; the base-builder pause counts the episode counter; a free call resets the episode
		// counter (`= default` for the new form) and counts the build counter.
		[Test]
		public void N1_UnitBuilderPauseStateMachine_MatchesTheOldCounters()
		{
			foreach (var seed in new[] { 3, 11, 999 })
			{
				foreach (var k in new[] { 30, 35, 40 })
				{
					var oldOpen = new List<int>();
					var oldBuild = new List<int>();
					var newOpen = new List<int>();
					var newBuild = new List<int>();
					var openingDefenseTicks = 0;
					var ticks = 0;
					var openCalls = new BotCallPeriod();
					var buildCalls = new BotCallPeriod();
					var state = (uint)seed;
					var mode = 0;
					var left = 0;
					for (var t = 1; t <= Horizon; t++)
					{
						if (left == 0)
						{
							state = unchecked(state * 1103515245u + 12345u);
							mode = (int)((state >> 16) % 3);
							left = 1 + (int)((state >> 8) % 120);
						}

						left--;
						if (mode == 2)
							continue;

						if (mode == 1)
						{
							if (++openingDefenseTicks % k == 0)
								oldOpen.Add(t);

							if (openCalls.Call(t, 1, k))
								newOpen.Add(t);

							continue;
						}

						openingDefenseTicks = 0;
						openCalls = default;
						ticks++;
						if (ticks % k == 0)
							oldBuild.Add(t);

						if (buildCalls.Call(t, 1, k))
							newBuild.Add(t);
					}

					Assert.That(newOpen, Is.EqualTo(oldOpen), $"opening defence seed={seed} K={k}");
					Assert.That(newBuild, Is.EqualTo(oldBuild), $"build sweep seed={seed} K={k}");
					Assert.That(oldOpen.Count, Is.GreaterThan(0), $"the test must exercise the opening-defence counter, seed={seed} K={k}");
					Assert.That(oldBuild.Count, Is.GreaterThan(0), $"the test must exercise the build counter, seed={seed} K={k}");
				}
			}
		}

		[Test]
		public void DefaultStruct_IsUnarmedUntilTheFirstCall()
		{
			var period = new BotCallPeriod();
			Assert.That(period.Call(500, 1, 250), Is.False);
			Assert.That(period.Call(501, 1, 250), Is.False);

			// Armed at v = 500: call 250 is at tick 749.
			var tick = 501;
			var fired = -1;
			while (fired < 0 && tick < 2000)
				if (period.Call(++tick, 1, 250))
					fired = tick;

			Assert.That(fired, Is.EqualTo(749));
		}
	}
}
