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
	// STAGGER_DESIGN_06 9.5 TimerSim: the old call-counted forms stepped literally against BotPeriod / BotCooldown.
	[TestFixture]
	public sealed class BotTimersTest
	{
		const int Horizon = 10000;

		static readonly int[] Ks = [0, 1, 2, 25, 50, 125, 150, 250, 750];
		static readonly int[] Rs = [0, 1, 7];

		static IEnumerable<int> X0s(int k) => new[] { -3, -1, 0, 1, 2, k - 1, k, k + 1, 249 }.Distinct();

		// Old forms: the k-th call is at tick r + k.
		static List<int> OldF1(int r, int x0, int k)
		{
			var fired = new List<int>();
			var x = x0;
			for (var t = r + 1; t <= r + Horizon; t++)
			{
				if (--x > 0)
					continue;
				x = k;
				fired.Add(t);
			}

			return fired;
		}

		static List<int> OldF2(int r, int x0, int k)
		{
			var fired = new List<int>();
			var x = x0;
			for (var t = r + 1; t <= r + Horizon; t++)
			{
				if (x-- > 0)
					continue;
				x = k;
				fired.Add(t);
			}

			return fired;
		}

		static List<int> OldF3(int r, int c0, int k)
		{
			var fired = new List<int>();
			var c = c0;
			for (var t = r + 1; t <= r + Horizon; t++)
			{
				if (++c < k)
					continue;
				c = 0;
				fired.Add(t);
			}

			return fired;
		}

		static List<int> OldF4(int r, int k, int res)
		{
			var fired = new List<int>();
			for (var t = r + 1; t <= r + Horizon; t++)
				if (t % Math.Max(1, k) == res)
					fired.Add(t);

			return fired;
		}

		// New side with a visit every tick (N = 1).
		static List<int> NewEveryTick(BotPeriod p, int r)
		{
			var fired = new List<int>();
			for (var t = r + 1; t <= r + Horizon; t++)
				if (p.TryFire(t))
					fired.Add(t);

			return fired;
		}

		[Test]
		public void F1IdentityAtN1()
		{
			foreach (var k in Ks)
				foreach (var r in Rs)
					foreach (var x0 in X0s(k))
						Assert.That(NewEveryTick(BotPeriod.F1(r, x0, k), r), Is.EqualTo(OldF1(r, x0, k)), $"F1 K={k} x0={x0} r={r}");
		}

		[Test]
		public void F2IdentityAtN1()
		{
			foreach (var k in Ks)
				foreach (var r in Rs)
					foreach (var x0 in X0s(k))
						Assert.That(NewEveryTick(BotPeriod.F2(r, x0, k), r), Is.EqualTo(OldF2(r, x0, k)), $"F2 K={k} x0={x0} r={r}");
		}

		[Test]
		public void F3IdentityAtN1()
		{
			foreach (var k in Ks)
				foreach (var r in Rs)
					foreach (var c0 in new[] { -1, 0, 1, k - 1, k })
						Assert.That(NewEveryTick(BotPeriod.F3(r, c0, k), r), Is.EqualTo(OldF3(r, c0, k)), $"F3 K={k} c0={c0} r={r}");
		}

		[Test]
		public void F4IdentityAtN1()
		{
			foreach (var k in Ks.Where(k => k >= 1))
				foreach (var r in Rs)
					foreach (var res in new[] { 0, 1, k - 1 }.Where(v => v >= 0 && v < k))
						Assert.That(NewEveryTick(BotPeriod.F4(r, k, res), r), Is.EqualTo(OldF4(r, k, res)), $"F4 K={k} res={res} r={r}");
		}

		[Test]
		public void ResidueWindowMatchesOldModuloAtN1AndKeepsGridAtVisitSpacingN()
		{
			foreach (var k in new[] { 1, 25, 75, 100, 250, 750 })
				foreach (var res in new[] { 0, 1, k - 1 }.Where(v => v >= 0 && v < k))
				{
					for (var t = 1; t <= 3000; t++)
						Assert.That(BotPeriod.ResidueHit(t, 1, k, res), Is.EqualTo(t % k == res), $"N=1 K={k} res={res} t={t}");

					// N = 4: a grid tick falling between visits is taken at the first visit at or after it, none lost.
					var hits = 0;
					for (var t = 4; t <= 3000; t += 4)
						if (BotPeriod.ResidueHit(t, 4, k, res))
							hits++;

					var grid = Enumerable.Range(1, 3000).Count(t => t % k == res && t <= 3000);
					var expected = k >= 4 ? grid : (3000 / 4);
					Assert.That(hits, Is.EqualTo(expected).Within(1), $"N=4 K={k} res={res}");
				}
		}

		[Test]
		public void ExpansionDemandEtaReloadMatchesItsN1AbsoluteTickCadence()
		{
			var oldDue = 100;
			var cooldown = new BotCooldown { Due = 0 };
			var oldRefreshes = new List<int>();
			var newRefreshes = new List<int>();

			for (var now = 100; now <= 250; now++)
			{
				if (now >= oldDue)
				{
					oldRefreshes.Add(now);
					oldDue = now + 25;
				}

				if (cooldown.Ready(now))
				{
					newRefreshes.Add(now);
					cooldown.Start(now, 25);
				}
			}

			Assert.That(newRefreshes, Is.EqualTo(oldRefreshes));
			Assert.That(newRefreshes, Is.EqualTo(new[] { 100, 125, 150, 175, 200, 225, 250 }));
		}

		[Test]
		public void RetreatRallyRescoreAndOrderCooldownsMatchTheirN1Gates()
		{
			const int interval = 100;
			const int pickTick = 7;
			var lastRescoreTick = pickTick;
			var rescoreCooldown = BotCooldown.FromRemaining(pickTick, interval);
			var lastOrderTick = int.MinValue / 2;
			var orderCooldown = new BotCooldown { Due = int.MinValue / 2 };
			var oldRescores = new List<int>();
			var newRescores = new List<int>();
			var oldOrders = new List<int>();
			var newOrders = new List<int>();

			for (var now = 1; now <= 400; now++)
			{
				if (now - lastRescoreTick >= interval)
				{
					oldRescores.Add(now);
					lastRescoreTick = now;
				}

				if (rescoreCooldown.Ready(now))
				{
					newRescores.Add(now);
					rescoreCooldown.Start(now, interval);
				}

				if (now - lastOrderTick >= interval)
				{
					oldOrders.Add(now);
					lastOrderTick = now;
				}

				if (orderCooldown.Ready(now))
				{
					newOrders.Add(now);
					orderCooldown.Start(now, interval);
				}
			}

			Assert.That(newRescores, Is.EqualTo(oldRescores));
			Assert.That(newOrders, Is.EqualTo(oldOrders));
		}

		[Test]
		public void RetreatRallyCooldownsRebaseFromTheLastEventWhenTheResolvedIntervalChanges()
		{
			static int IntervalAt(int tick) => tick < 60 ? 100 : tick < 90 ? 25 : tick < 150 ? 80 : 40;
			var oldRescoreTick = 7;
			var cooldownRescoreTick = 7;
			var rescoreCooldown = new BotCooldown();
			var oldRescores = new List<int>();
			var newRescores = new List<int>();
			var oldOrderTick = int.MinValue / 2;
			var cooldownOrderTick = int.MinValue / 2;
			var orderCooldown = new BotCooldown { Due = int.MinValue / 2 };
			var hasOrder = false;
			var oldOrders = new List<int>();
			var newOrders = new List<int>();

			for (var now = 1; now <= 300; now++)
			{
				var interval = IntervalAt(now);
				if (now - oldRescoreTick >= interval)
				{
					oldRescores.Add(now);
					oldRescoreTick = now;
				}

				rescoreCooldown.Due = cooldownRescoreTick + interval;
				if (rescoreCooldown.Ready(now))
				{
					newRescores.Add(now);
					cooldownRescoreTick = now;
					rescoreCooldown.Start(now, interval);
				}

				if (!hasOrder || now - oldOrderTick >= interval)
				{
					oldOrders.Add(now);
					oldOrderTick = now;
					hasOrder = true;
				}

				orderCooldown.Due = cooldownOrderTick + interval;
				if (!hasOrder || orderCooldown.Ready(now))
				{
					newOrders.Add(now);
					cooldownOrderTick = now;
					orderCooldown.Start(now, interval);
				}
			}

			Assert.That(newRescores, Is.EqualTo(oldRescores));
			Assert.That(newOrders, Is.EqualTo(oldOrders));
		}

		[Test]
		public void AssaultFormationRefanCooldownMatchesItsN1GateAndPruneBoundary()
		{
			const int recordTick = 100;
			const int cooldownTicks = 750;
			var cooldown = new BotCooldown();
			cooldown.Start(recordTick, cooldownTicks);

			foreach (var now in new[] { recordTick, recordTick + cooldownTicks - 1, recordTick + cooldownTicks, recordTick + cooldownTicks + 1 })
			{
				Assert.That(!cooldown.Ready(now), Is.EqualTo(now - recordTick < cooldownTicks), $"same-ground gate at {now}");
				Assert.That(now > cooldown.Due, Is.EqualTo(now - recordTick > cooldownTicks), $"prune gate at {now}");
			}
		}

		[Test]
		public void ObjectiveFanCooldownIsReadyAtItsOriginalWorldTickDeadline()
		{
			var cooldown = new BotCooldown { Due = int.MinValue / 2 };
			Assert.That(cooldown.Ready(0), Is.True, "the initial sentinel does not block the first fan");
			cooldown.Start(100, 750);
			Assert.That(cooldown.Ready(849), Is.False);
			Assert.That(cooldown.Ready(850), Is.True);
		}

		[Test]
		public void BeaconResponderReleaseCooldownIsReadyAtTheOriginalTickBoundary()
		{
			var cooldown = new BotCooldown();
			cooldown.Start(100, 50);

			Assert.That(cooldown.Ready(149), Is.False);
			Assert.That(cooldown.Ready(150), Is.True);
		}

		[Test]
		public void DeployAbilityAndActionCooldownsMatchTheirN1Boundaries()
		{
			var ability = new BotCooldown();
			ability.Start(100, 100);
			Assert.That(ability.Ready(199), Is.False);
			Assert.That(ability.Ready(200), Is.True);

			var action = new BotCooldown();
			action.Start(100, 150);
			Assert.That(action.Ready(249), Is.False);
			Assert.That(action.Ready(250), Is.True);
		}

		[Test]
		public void DeployCooldownUsesTheIntervalOfTheReadingGroup()
		{
			const int actionTick = 100;
			var longGroupCooldown = new BotCooldown();
			longGroupCooldown.Start(actionTick, 150);
			var shortGroupCooldown = new BotCooldown();
			shortGroupCooldown.Start(actionTick, 0);

			Assert.That(longGroupCooldown.Ready(actionTick), Is.False);
			Assert.That(shortGroupCooldown.Ready(actionTick), Is.True);
			Assert.That(longGroupCooldown.Ready(actionTick + 150), Is.True);
		}

		[Test]
		public void RuledOutFirstDeadlineFormsFail()
		{
			// Positive control (9.5): the withdrawn due0 = r + x0 reproduces Sol's tick-25-against-26 defect at x0 = 0.
			var old = OldF1(0, 0, 25);
			var withdrawn = new List<int>();
			var due = 0 + 0;
			for (var t = 1; t <= Horizon; t++)
				if (t >= due)
				{
					withdrawn.Add(t);
					due += 25;
				}

			Assert.That(withdrawn, Is.Not.EqualTo(old));
			Assert.That(old[0], Is.EqualTo(1));
			Assert.That(old[1], Is.EqualTo(26));
		}

		[Test]
		public void FreezeKeepsTheOldCounterIdentity()
		{
			// R2: the old code does not count on a frozen call; the new due moves back by Elapsed.
			foreach (var k in new[] { 25, 150 })
				foreach (var x0 in new[] { 0, 5, k })
				{
					bool Frozen(int t) => t % 7 < 3;

					var old = new List<int>();
					var x = x0;
					for (var t = 1; t <= Horizon; t++)
					{
						if (Frozen(t))
							continue;
						if (--x > 0)
							continue;
						x = k;
						old.Add(t);
					}

					var p = BotPeriod.F1(0, x0, k);
					var now = new List<int>();
					for (var t = 1; t <= Horizon; t++)
					{
						if (Frozen(t))
						{
							p.Freeze(1);
							continue;
						}

						if (p.TryFire(t))
							now.Add(t);
					}

					Assert.That(now, Is.EqualTo(old), $"freeze K={k} x0={x0}");
				}
		}

		[Test]
		public void CooldownIdentityAtN1()
		{
			// Event ticks injected; old: next = t + K set where the event happens; new: Start(v, K), v = t at N = 1.
			foreach (var k in new[] { 1, 25, 150 })
			{
				var oldNext = 0;
				var c = new BotCooldown();
				for (var t = 1; t <= 2000; t++)
				{
					var ev = t % 13 == 0 || t % 41 == 0;
					var oldReady = t >= oldNext;
					Assert.That(c.Ready(t), Is.EqualTo(oldReady), $"cooldown K={k} t={t}");
					if (ev && oldReady)
					{
						oldNext = t + k;
						c.Start(t, k);
					}
				}
			}
		}

		[Test]
		public void PeriodIsKeptAtVisitSpacingN()
		{
			// T3-b/T3-c: with a visit every N ticks the carry form loses no deadline and keeps the period K on average.
			foreach (var n in new[] { 2, 4, 8, 12 })
			{
				var p = BotPeriod.F1(0, 0, 25);
				var fired = 0;
				for (var t = n; t <= Horizon; t += n)
					if (p.TryFire(t))
						fired++;

				Assert.That(fired + p.Coalesced, Is.EqualTo((Horizon - 1) / 25 + 1).Within(1), $"carry N={n}");
			}

			// Positive control: a RELOAD (due = v + K) P site at K = 25, N = 8 stretches the period to 32 and must differ.
			var due = 0;
			var reload = 0;
			for (var t = 8; t <= Horizon; t += 8)
				if (t >= due)
				{
					reload++;
					due = t + 25;
				}

			Assert.That(reload, Is.LessThan(Horizon / 25 - 10));
		}

		[Test]
		public void SaveLoadRoundTripKeepsThePhase()
		{
			var p = BotPeriod.F1(0, 30, 25);
			for (var t = 1; t <= 40; t++)
				p.TryFire(t);

			var saved = p.SavedRemaining(40);
			var loaded = BotPeriod.Restore(40, saved, 25, true);
			Assert.That(loaded.Due, Is.EqualTo(p.Due));

			// Legacy number: a fresh x0 at r = loadTick, first deadline loadTick + max(1, x) (F1).
			Assert.That(BotPeriod.Restore(40, 0, 25, false).Due, Is.EqualTo(41));
			Assert.That(BotPeriod.Restore(40, 7, 25, false).Due, Is.EqualTo(47));
			// An overdue (negative) remaining is restored exactly.
			Assert.That(BotPeriod.Restore(40, -3, 25, true).Due, Is.EqualTo(37));

			Assert.That(BotPeriod.ReadTimerFormat("M", null), Is.False);
			Assert.That(BotPeriod.ReadTimerFormat("M", "1"), Is.True);
			Assert.That(() => BotPeriod.ReadTimerFormat("M", "2"), Throws.InvalidOperationException);
		}

		[Test]
		public void DisabledGapFreezesACallCountedTimerLikeTheOldCounter()
		{
			// The module is not ticked while disabled (RunModules skips it): the old counter does not move.
			bool Disabled(int t) => (t >= 100 && t < 190) || (t >= 400 && t < 403);

			foreach (var k in new[] { 25, 150 })
				foreach (var x0 in new[] { 0, 7, k })
				{
					var old = new List<int>();
					var x = x0;
					for (var t = 1; t <= 2000; t++)
					{
						if (Disabled(t))
							continue;
						if (--x > 0)
							continue;
						x = k;
						old.Add(t);
					}

					var p = BotPeriod.F1(0, x0, k);
					var now = new List<int>();
					for (var t = 1; t <= 2000; t++)
					{
						if (Disabled(t))
							continue;
						if (p.TryFire(t, 1))
							now.Add(t);
					}

					Assert.That(now, Is.EqualTo(old), $"gap K={k} x0={x0}");
				}
		}

		[Test]
		public void ResidueHitRejectsAResidueOutsideThePeriod()
		{
			Assert.That(() => BotPeriod.ResidueHit(10, 1, 25, 25), Throws.InstanceOf<ArgumentOutOfRangeException>());
			Assert.That(() => BotPeriod.ResidueHit(10, 1, 25, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void CarryDueMatchesTheOldDueFormAtN1()
		{
			foreach (var k in new[] { 0, 1, 30, 50, 250 })
			{
				var oldNext = 0;
				var due = 0;
				for (var t = 1; t <= 3000; t++)
				{
					var oldFire = t >= oldNext;
					if (oldFire)
						oldNext = t + Math.Max(1, k);

					Assert.That(BotPeriod.CarryDue(ref due, t, k), Is.EqualTo(oldFire), $"K={k} t={t}");
				}
			}
		}

		[Test]
		public void UnarmedPeriodAndBadResidueThrow()
		{
			Assert.That(() => default(BotPeriod).TryFire(5), Throws.InvalidOperationException);
			Assert.That(() => BotPeriod.F4(0, 25, 25), Throws.InstanceOf<ArgumentOutOfRangeException>());
			Assert.That(() => BotPeriod.F4(0, 25, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void CoalescedMissesAreCountedNotLost()
		{
			var p = BotPeriod.F1(0, 0, 10);
			Assert.That(p.TryFire(1), Is.True);
			Assert.That(p.TryFire(45), Is.True); // due 11, 21, 31, 41 elapsed in one visit: one run, 3 coalesced
			Assert.That(p.Coalesced, Is.EqualTo(3));
			Assert.That(p.Due, Is.EqualTo(51));
		}
	}
}
