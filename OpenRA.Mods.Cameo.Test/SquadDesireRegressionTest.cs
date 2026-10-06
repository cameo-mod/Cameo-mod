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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadDesireRegressionTest
	{
		static SquadDesireSignals Signals(int ratio, int tick, int health = 1000) =>
			new(3000, 1500, 0, ratio, health, 0, true, false, tick);

		[Test]
		public void LosingRushCannotAttackEvenWhenDesireAndDwellRetainAttack()
		{
			var state = new SquadDesireMemory();
			var winning = Signals(4000, 0);
			Assert.That(state.Evaluate(in winning, "rush", 100, 150, 100, 200), Is.EqualTo(SquadDesireStance.Attack));
			var losing = Signals(500, 75);
			var retained = state.Evaluate(in losing, "rush", 100, 150, 100, 200);
			Assert.That(retained, Is.EqualTo(SquadDesireStance.Attack), "the historical desire is intentionally retained");
			var safe = SquadDesireOrders.CanEngage(losing.PredictedRatioMilli, 50, 150);
			Assert.That(safe, Is.False, "the old margin rejected ratio 0.5; desire cannot grant permission");
			Assert.That(SquadDesireOrders.Plan(retained, safe, true),
				Is.EqualTo(new SquadDesireOrder("Move", SquadDesireDestination.Home)));
			var fresh = new SquadDesireMemory();
			Assert.That(fresh.Evaluate(in losing, "rush", 100, 150, 100, 200), Is.EqualTo(SquadDesireStance.Attack));
			Assert.That(SquadDesireOrders.CanEngage(999, 50, 150), Is.False, "no losing square-law commit");
			Assert.That(SquadDesireOrders.CanEngage(1000, 50, 150), Is.True);
		}

		[Test]
		public void SixStancesHaveDistinctOrderRoutesAndHarassHasAConsumer()
		{
			var routes = Enum.GetValues<SquadDesireStance>().Select(s => SquadDesireOrders.Plan(s, true, true)).ToArray();
			Assert.That(routes.Distinct().Count(), Is.EqualTo(6));
			Assert.That(routes[(int)SquadDesireStance.Defend], Is.EqualTo(new SquadDesireOrder("AttackMove", SquadDesireDestination.Home)));
			Assert.That(routes[(int)SquadDesireStance.Regroup].Destination, Is.EqualTo(SquadDesireDestination.Assembly));
			Assert.That(routes[(int)SquadDesireStance.Harass].Destination, Is.EqualTo(SquadDesireDestination.Raid));
			Assert.That(routes[(int)SquadDesireStance.Reinforce].Destination, Is.EqualTo(SquadDesireDestination.Reinforcement));
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Harass), Is.True);
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Protection), Is.True);
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Rush), Is.True);
			Assert.That(SquadDesireOrders.Supports(SquadCAType.Air), Is.False);
		}

		static readonly BitSet<TargetableType> Ground = new("Ground");
		static IntegerCombatPredictor.Unit Unit(int hp, int damage, int cycle = 1, string armor = null,
			Dictionary<string, int> versus = null) => new(hp, armor, Ground,
			[new(damage, 1, cycle, Ground, default, versus)]);

		[Test]
		public void IntegerPredictionPinsBoundaryAndIsIndependentOfEnumerationAndOverflow()
		{
			var own = Unit(1000, 3, 7);
			var enemy = Unit(1000, 6, 7);
			Assert.That(IntegerCombatPredictor.RatioMilli([(own, 1)], [(enemy, 1)]), Is.EqualTo(500));
			Assert.That(IntegerCombatPredictor.RatioMilli([(own, 2)], [(own, 1)]), Is.EqualTo(4000));
			var antiHeavy = Unit(500, 300, 11, versus: new() { ["Heavy"] = 200 });
			var heavy = Unit(int.MaxValue, int.MaxValue, 13, "Heavy");
			var a = new[] { (antiHeavy, int.MaxValue), (own, 43) };
			var b = new[] { (heavy, 51), (enemy, int.MaxValue) };
			var ratio = IntegerCombatPredictor.RatioMilli(a, b);
			for (var i = 0; i < 10; i++)
				Assert.That(IntegerCombatPredictor.RatioMilli(a.Reverse().ToArray(), b.Reverse().ToArray()), Is.EqualTo(ratio));
			Assert.That(SquadDesireOrders.CanEngage(1499, 100, 150), Is.False);
			Assert.That(SquadDesireOrders.CanEngage(1500, 100, 150), Is.True);
		}

		[Test]
		public void RegularEvaluationsChangeAnActiveAttackAndDuplicateTickDoesNotAccelerateLeak()
		{
			var a = new SquadDesireMemory();
			var b = new SquadDesireMemory();
			var winning = Signals(4000, 0);
			a.Evaluate(in winning, "rush", 100, 150, 100, 200);
			b.Evaluate(in winning, "rush", 100, 150, 100, 200);
			SquadDesireStance stance = SquadDesireStance.Attack;
			for (var tick = 75; tick < 2500; tick += 75)
			{
				var signals = Signals(0, tick, 0);
				stance = a.Evaluate(in signals, "rush", 100, 150, 100, 200);
				Assert.That(b.Evaluate(in signals, "rush", 100, 150, 100, 200), Is.EqualTo(stance));
				for (var repeat = 0; repeat < 5; repeat++)
					Assert.That(b.Evaluate(in signals, "rush", 100, 150, 100, 200), Is.EqualTo(stance));
			}
			Assert.That(stance, Is.EqualTo(SquadDesireStance.Retreat));
		}
	}
}
