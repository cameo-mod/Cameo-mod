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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadDesireEvalTest
	{
		// LEARN-P6 (BP_squad_desire, SPEC 2026-10-05 §11): per-squad, per-stance leaky integrator
		// over urgency + capped personality bias; a stance switch needs best > current + margin
		// AND MinDwellTicks elapsed. Integer thousandths only, MP-deterministic.

		static SquadDesireSignals Signals(int own = 3000, int enemy = 3000, int baseEnemy = 0,
			int ratio = 1000, int health = 1000, int scatter = 0, bool target = true,
			bool harassCapable = false, int tick = 0)
		{
			return new SquadDesireSignals(own, enemy, baseEnemy, ratio, health, scatter, target, harassCapable, tick);
		}

		static int[] Urgencies(SquadDesireSignals s)
		{
			var u = new int[SquadDesireEval.StanceCount];
			SquadDesireEval.Urgencies(in s, u);
			return u;
		}

		[Test]
		public void StepConvergesTowardUrgency()
		{
			var d = 0;
			for (var i = 0; i < 100; i++)
				d = SquadDesireEval.Step(d, 1000, 100);

			// Truncating integer division asymptotes — the integrator settles one step short of
			// the urgency (the last milli's delta is < 1), never overshoots it.
			Assert.That(d, Is.InRange(990, 1000), "a 10%/eval leak converges to a constant urgency");
		}

		[Test]
		public void StepZeroRateHoldsAndFullRateSnaps()
		{
			Assert.That(SquadDesireEval.Step(500, 1000, 0), Is.EqualTo(500));
			Assert.That(SquadDesireEval.Step(500, 1000, 1000), Is.EqualTo(1000));
			Assert.That(SquadDesireEval.Step(500, 0, 1000), Is.EqualTo(0));
		}

		[Test]
		public void UrgenciesStayInsideMilliBoundsOnExtremes()
		{
			foreach (var s in new[]
			{
				Signals(1, int.MaxValue / 2, int.MaxValue / 2, 4000, 0, 1000),
				Signals(int.MaxValue / 2, 0, 0, 0, 1000, 0, false),
			})
			{
				var u = Urgencies(s);
				for (var i = 0; i < SquadDesireEval.StanceCount; i++)
					Assert.That(u[i], Is.InRange(0, 1000), $"stance {i} must stay in thousandths bounds");
			}
		}

		[Test]
		public void AttackDominatesAWinnableTarget()
		{
			// Even odds, a live target, nobody home-threatened: attack 816, reinforce 300, rest ~0.
			var u = Urgencies(Signals());
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Attack));
			Assert.That(u[(int)SquadDesireStance.Attack], Is.GreaterThan(u[(int)SquadDesireStance.Reinforce]));
		}

		[Test]
		public void ReinforceDominatesWhenOutgunned()
		{
			// Enemy 2x our value at a live target: reinforce 800 > attack 416 > retreat 333.
			var u = Urgencies(Signals(enemy: 6000, ratio: 400, health: 900));
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Reinforce));
		}

		[Test]
		public void DefendDominatesWhenBaseIsThreatened()
		{
			// Parity enemy at home while the squad fights even odds: defend 900 > attack 700.
			var u = Urgencies(Signals(enemy: 1500, baseEnemy: 3000, ratio: 1200));
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Defend));
		}

		[Test]
		public void RetreatDominatesWhenLosingAndHurt()
		{
			// A losing call on a squad at 20% health, enemy only 1.5x (not overwhelming):
			// retreat 516 > attack 483 > harass 300 — flee, don't wait for help that isn't coming.
			var u = Urgencies(Signals(enemy: 1500, ratio: 500, health: 200));
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Retreat));
		}

		[Test]
		public void HarassWinsForRaiderAtParityWithGuerrillaBias()
		{
			// Parity enemy value but the predictor calls it a losing fight (comp counter): a
			// guerrilla chassis raiding, not committing — harass 650 > attack 641 under the
			// guerrilla bias (+150 harass, +25 attack), while an unbiased squad still attacks.
			var u = Urgencies(Signals(ratio: 700, harassCapable: true));
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Attack),
				"without the bias the plain urgencies still prefer attack");

			SquadDesireEval.AddBias(u, "guerrilla", 200);
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Harass));
		}

		[Test]
		public void RegroupRisesWithScatter()
		{
			var tight = Urgencies(Signals(scatter: 50));
			var loose = Urgencies(Signals(scatter: 900));
			Assert.That(loose[(int)SquadDesireStance.Regroup],
				Is.GreaterThan(tight[(int)SquadDesireStance.Regroup]));
		}

		[Test]
		public void PickBootstrapsToArgmax()
		{
			var d = new[] { 10, 700, 300, 0, 50, 20 };
			Assert.That(SquadDesireEval.Pick(d, -1, 150, false), Is.EqualTo(1),
				"no incumbent = plain argmax, dwell irrelevant on the first pick");
		}

		[Test]
		public void PickHysteresisBlocksSmallGaps()
		{
			var d = new[] { 600, 700, 0, 0, 0, 0 };
			Assert.That(SquadDesireEval.Pick(d, 0, 150, true), Is.EqualTo(0),
				"challenger beats the incumbent but not by the margin");

			d[1] = 800;
			Assert.That(SquadDesireEval.Pick(d, 0, 150, true), Is.EqualTo(1),
				"past the margin and dwell elapsed = switch");
		}

		[Test]
		public void PickDwellBlocksSwitch()
		{
			var d = new[] { 100, 900, 0, 0, 0, 0 };
			Assert.That(SquadDesireEval.Pick(d, 0, 150, false), Is.EqualTo(0),
				"a decisive challenger still waits out MinDwellTicks");
		}

		[Test]
		public void PickTieKeepsIncumbent()
		{
			var d = new[] { 500, 500, 0, 0, 0, 0 };
			Assert.That(SquadDesireEval.Pick(d, 1, 0, true), Is.EqualTo(1),
				"equal desires never dislodge the incumbent");
		}

		[Test]
		public void BiasIsCappedPerStance()
		{
			var u = new int[SquadDesireEval.StanceCount];
			SquadDesireEval.AddBias(u, "rush", 50);
			Assert.That(u[(int)SquadDesireStance.Attack], Is.EqualTo(50),
				"rush's +150 attack bias clamps to the +50 cap");
			Assert.That(u[(int)SquadDesireStance.Defend], Is.EqualTo(0),
				"rush's -100 defend bias clamps low to the -50 cap, then urgencies floor at 0");
		}

		[Test]
		public void BiasCannotOverrideClearlyStrongerEvidence()
		{
			// A crushing win: attack saturates. Turtle's capped -100 attack / +150 defend bias
			// cannot open a defend channel when nothing threatens home.
			var u = Urgencies(Signals(enemy: 500, ratio: 4000));
			SquadDesireEval.AddBias(u, "turtle", 200);
			Assert.That(SquadDesireEval.Pick(u, -1, 150, true), Is.EqualTo((int)SquadDesireStance.Attack),
				"the cap keeps temperament from drowning clearly stronger evidence");
		}

		[Test]
		public void UnknownPersonalityAddsNoBias()
		{
			var u = Urgencies(Signals());
			var copy = (int[])u.Clone();
			SquadDesireEval.AddBias(u, "", 200);
			SquadDesireEval.AddBias(u, "not-a-personality", 200);
			Assert.That(u, Is.EqualTo(copy));
		}

		[Test]
		public void StanceFlapsAreBoundedByDwell()
		{
			// Alternating winnable/outgunned worlds through the full provider math — the stance
			// can never switch more than once per MinDwellTicks: switches <= 1 + span/dwell.
			const int dwell = 200;
			const int evalStep = 50;
			const int evals = 80; // 4000 ticks
			var desires = new int[SquadDesireEval.StanceCount];
			var urgencies = new int[SquadDesireEval.StanceCount];
			var current = -1;
			var lastSwitch = 0;
			var switches = 0;
			var primed = false;

			for (var i = 0; i < evals; i++)
			{
				var tick = i * evalStep;
				var s = i % 2 == 0
					? Signals(tick: tick)                                   // winnable
					: Signals(enemy: 6000, ratio: 400, health: 900, tick: tick); // outgunned
				SquadDesireEval.Urgencies(in s, urgencies);
				SquadDesireEval.AddBias(urgencies, "rush", 200);

				for (var j = 0; j < SquadDesireEval.StanceCount; j++)
					desires[j] = primed ? SquadDesireEval.Step(desires[j], urgencies[j], 100) : urgencies[j];
				primed = true;

				var next = SquadDesireEval.Pick(desires, current, 150, tick - lastSwitch >= dwell);
				if (next != current)
				{
					current = next;
					lastSwitch = tick;
					switches++;
				}
			}

			var bound = 1 + (evals * evalStep - 1) / dwell;
			Assert.That(switches, Is.LessThanOrEqualTo(bound),
				$"stance flaps must stay inside the dwell bound ({switches} > {bound})");
			Assert.That(switches, Is.LessThanOrEqualTo(12),
				"alternating urgency must not flip the stance every eval");
		}

		[Test]
		public void IntegratorSmoothsFlickeringUrgency()
		{
			// The AR-S lesson: an alternating 1000/0 urgency must not produce an alternating
			// 1000/0 desire — the leak keeps it bounded mid-range instead of snapping.
			var d = 0;
			var lo = int.MaxValue;
			var hi = int.MinValue;
			for (var i = 0; i < 60; i++)
			{
				d = SquadDesireEval.Step(d, i % 2 == 0 ? 1000 : 0, 100);
				if (i > 30)
				{
					lo = System.Math.Min(lo, d);
					hi = System.Math.Max(hi, d);
				}
			}

			Assert.That(hi - lo, Is.LessThan(150),
				$"flickering urgency settled into a tight band ({lo}..{hi}), not a full-swing flap");
		}
	}
}
