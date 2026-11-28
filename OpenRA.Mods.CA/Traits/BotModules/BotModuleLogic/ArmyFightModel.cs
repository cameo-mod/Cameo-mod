#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ARMY-BRAIN-01 S1 (design: ARMY_BRAIN_DESIGN_02.md, 3.4): the fight model behind the formula counter. It keeps the square-law core of
	/// BotCombatPredictor (which is not changed) and adds two terms:
	/// 1. can-target: a unit spreads its damage only over the enemy types it may hit, and deals none when it can hit nothing;
	/// 2. range: a unit that outranges a target fires unanswered while the target closes the gap (capped; a target that cannot move gives
	///    the cap);
	/// 3. a type the other side cannot hit at all fires on the survivors for a fixed time, in proportion to its count and damage.
	/// Pure and bot-side (unsynced), from type profiles only. No learning.
	/// </summary>
	/// <summary>
	/// The damage per tick and the free-fire time of one unit type against another, remembered per pair. Type profiles are one object per
	/// type and ruleset (BotUnitProfiles), so the pair is the key. One cache per bot module; the cap is fixed for its lifetime.
	/// </summary>
	public sealed class ArmyPairCache
	{
		readonly Dictionary<(BotUnitProfile, BotUnitProfile), (double Damage, double FreeFire)> pairs = [];
		readonly int freeFireCapTicks;

		public ArmyPairCache(int freeFireCapTicks) { this.freeFireCapTicks = freeFireCapTicks; }

		public (double Damage, double FreeFire) Of(BotUnitProfile shooter, BotUnitProfile target)
		{
			if (!pairs.TryGetValue((shooter, target), out var pair))
				pairs[(shooter, target)] = pair = (shooter.DamagePerTickAgainst(target), ArmyFightModel.FreeFireTicks(shooter, target, freeFireCapTicks));

			return pair;
		}
	}

	public static class ArmyFightModel
	{
		/// <summary>Value destroyed and value lost, in cost units.</summary>
		public readonly record struct Outcome(double Destroyed, double Lost)
		{
			public double Net => Destroyed - Lost;
		}

		/// <summary>The longest range (world units) of the weapons of <paramref name="shooter"/> that may hit <paramref name="target"/>;
		/// 0 = none can. Passenger (port) weapons are left out, as in the type-table damage term.</summary>
		public static int RangeAgainst(BotUnitProfile shooter, BotUnitProfile target)
		{
			var best = 0;
			foreach (var w in shooter.Weapons)
				if (w.PortArmament == null && w.CanTarget(target.TargetTypes))
					best = Math.Max(best, w.Range.Length);

			return best;
		}

		/// <summary>
		/// Ticks <paramref name="shooter"/> fires on <paramref name="target"/> before the target can answer: the range advantage divided by
		/// the target's speed, at most <paramref name="capTicks"/>. A target that cannot move, or cannot hit the shooter at all, gives the
		/// cap. No advantage, or no weapon that reaches, gives 0.
		/// </summary>
		public static double FreeFireTicks(BotUnitProfile shooter, BotUnitProfile target, int capTicks)
		{
			if (capTicks <= 0)
				return 0;

			var reach = RangeAgainst(shooter, target);
			if (reach <= 0)
				return 0;

			var answer = RangeAgainst(target, shooter);
			if (answer <= 0)
				return capTicks;

			if (reach <= answer)
				return 0;

			if (target.Speed <= 0)
				return capTicks;

			return Math.Min(capTicks, (double)(reach - answer) / target.Speed);
		}

		/// <summary>
		/// One fight of <paramref name="own"/> against <paramref name="enemy"/>, in three phases. Counts may be fractional (an enemy mix is
		/// given by value).
		/// 1. Free fire from the range advantages (capped by <paramref name="freeFireCapTicks"/>).
		/// 2. The square law between the units that can be hit by the other side.
		/// 3. Unanswered fire: a type the other side cannot hit at all fires on what is left for <paramref name="unansweredTicks"/>
		///    (negative = the free-fire cap). It scales with the count and the damage of that type: one helicopter costs a tank army a
		///    little, thirty cost it everything.
		/// </summary>
		public static Outcome Evaluate(IReadOnlyList<(BotUnitProfile Unit, double Count)> own, IReadOnlyList<(BotUnitProfile Unit, double Count)> enemy,
			int freeFireCapTicks, ArmyPairCache cache = null, int unansweredTicks = -1)
		{
			if (own.Count == 0 || enemy.Count == 0)
				return default;

			var ownLeft = new double[own.Count];
			var enemyLeft = new double[enemy.Count];
			for (var i = 0; i < own.Count; i++)
				ownLeft[i] = Math.Max(0, own[i].Count);
			for (var j = 0; j < enemy.Count; j++)
				enemyLeft[j] = Math.Max(0, enemy[j].Count);

			// Phase 1: free fire. Both sides fire from the starting counts.
			var enemyHit = new double[enemy.Count];
			var ownHit = new double[own.Count];
			Spread(own, ownLeft, enemy, enemyLeft, null, true, -1, freeFireCapTicks, enemyHit, cache);
			Spread(enemy, enemyLeft, own, ownLeft, null, true, -1, freeFireCapTicks, ownHit, cache);
			Remove(enemy, enemyLeft, enemyHit);
			Remove(own, ownLeft, ownHit);

			// Phase 2: the square law on aggregates, each side's damage spread over what it can hit. Only the types the other side can hit
			// take part: a type nobody can hit is not destroyed by the square law, and it fires in phase 3 instead.
			var ownReachable = Reachable(enemy, enemyLeft, own, ownLeft, cache);
			var enemyReachable = Reachable(own, ownLeft, enemy, enemyLeft, cache);
			var ownDamage = Spread(own, ownLeft, enemy, enemyLeft, ownReachable, true, 0, 0, null, cache);
			var enemyDamage = Spread(enemy, enemyLeft, own, ownLeft, enemyReachable, true, 0, 0, null, cache);
			var prediction = BotCombatPredictor.Predict(ownDamage, Hp(own, ownLeft, ownReachable), enemyDamage, Hp(enemy, enemyLeft, enemyReachable));
			for (var i = 0; i < own.Count; i++)
				if (ownReachable[i])
					ownLeft[i] *= prediction.OwnSurvivingFraction;
			for (var j = 0; j < enemy.Count; j++)
				if (enemyReachable[j])
					enemyLeft[j] *= prediction.EnemySurvivingFraction;

			// Phase 3: unanswered fire of the types that could not be hit, on what is left, for a fixed time.
			var horizon = unansweredTicks < 0 ? freeFireCapTicks : unansweredTicks;
			if (horizon > 0)
			{
				Array.Clear(enemyHit);
				Array.Clear(ownHit);
				Spread(own, ownLeft, enemy, enemyLeft, ownReachable, false, horizon, 0, enemyHit, cache);
				Spread(enemy, enemyLeft, own, ownLeft, enemyReachable, false, horizon, 0, ownHit, cache);
				Remove(enemy, enemyLeft, enemyHit);
				Remove(own, ownLeft, ownHit);
			}

			double destroyed = 0, lost = 0;
			for (var j = 0; j < enemy.Count; j++)
				destroyed += (Math.Max(0, enemy[j].Count) - enemyLeft[j]) * enemy[j].Unit.Cost;
			for (var i = 0; i < own.Count; i++)
				lost += (Math.Max(0, own[i].Count) - ownLeft[i]) * own[i].Unit.Cost;

			return new Outcome(destroyed, lost);
		}

		static double Damage(BotUnitProfile shooter, BotUnitProfile target, ArmyPairCache cache) =>
			cache != null ? cache.Of(shooter, target).Damage : shooter.DamagePerTickAgainst(target);

		// Per target type: some attacker that is left can hit it.
		static bool[] Reachable(IReadOnlyList<(BotUnitProfile Unit, double Count)> attackers, double[] attackersLeft,
			IReadOnlyList<(BotUnitProfile Unit, double Count)> targets, double[] targetsLeft, ArmyPairCache cache)
		{
			var reachable = new bool[targets.Count];
			for (var j = 0; j < targets.Count; j++)
			{
				if (targetsLeft[j] <= 0)
					continue;

				for (var i = 0; i < attackers.Count && !reachable[j]; i++)
					reachable[j] = attackersLeft[i] > 0 && Damage(attackers[i].Unit, targets[j].Unit, cache) > 0;
			}

			return reachable;
		}

		static double Hp(IReadOnlyList<(BotUnitProfile Unit, double Count)> side, double[] left, bool[] counted)
		{
			var total = 0.0;
			for (var i = 0; i < side.Count; i++)
				if (counted[i])
					total += left[i] * side[i].Unit.Hp;

			return total;
		}

		// Damage of the attackers, each one's damage spread by HP share over the targets it can hit. Only the attackers whose `include`
		// entry equals `includeValue` fire (include null = all).
		// hit == null: returns the damage per tick. hit != null: adds the hit points taken per target; over `ticks` when that is >= 0, else
		// over each pair's free-fire time (capped by capTicks).
		static double Spread(IReadOnlyList<(BotUnitProfile Unit, double Count)> attackers, double[] attackersLeft,
			IReadOnlyList<(BotUnitProfile Unit, double Count)> targets, double[] targetsLeft, bool[] include, bool includeValue,
			double ticks, int capTicks, double[] hit, ArmyPairCache cache)
		{
			var total = 0.0;
			var freeFire = hit != null && ticks < 0;
			var perTick = new double[targets.Count];
			var free = new double[targets.Count];
			for (var i = 0; i < attackers.Count; i++)
			{
				if (attackersLeft[i] <= 0 || (include != null && include[i] != includeValue))
					continue;

				var reachable = 0.0;
				for (var j = 0; j < targets.Count; j++)
				{
					perTick[j] = 0;
					if (targetsLeft[j] <= 0)
						continue;

					if (cache != null)
						(perTick[j], free[j]) = cache.Of(attackers[i].Unit, targets[j].Unit);
					else
					{
						perTick[j] = attackers[i].Unit.DamagePerTickAgainst(targets[j].Unit);
						free[j] = freeFire && perTick[j] > 0 ? FreeFireTicks(attackers[i].Unit, targets[j].Unit, capTicks) : 0;
					}

					if (perTick[j] > 0)
						reachable += targetsLeft[j] * targets[j].Unit.Hp;
				}

				if (reachable <= 0)
					continue;

				for (var j = 0; j < targets.Count; j++)
				{
					if (perTick[j] <= 0)
						continue;

					var damage = attackersLeft[i] * perTick[j] * targetsLeft[j] * targets[j].Unit.Hp / reachable;
					if (hit == null)
						total += damage;
					else
						hit[j] += damage * (freeFire ? free[j] : ticks);
				}
			}

			return total;
		}

		static void Remove(IReadOnlyList<(BotUnitProfile Unit, double Count)> side, double[] left, double[] hit)
		{
			for (var i = 0; i < side.Count; i++)
				if (hit[i] > 0 && side[i].Unit.Hp > 0)
					left[i] = Math.Max(0, left[i] - hit[i] / side[i].Unit.Hp);
		}

		/// <summary>How many units of one type make one test step: about <paramref name="stepCost"/> worth, 1..<paramref name="maxCount"/>.
		/// A cheap type is tested as several units, a dear one as one, so the gain per cost compares like with like.</summary>
		public static int StepCount(int stepCost, int unitCost, int maxCount)
		{
			if (unitCost <= 0)
				return 1;

			return Math.Clamp((stepCost + unitCost / 2) / unitCost, 1, Math.Max(1, maxCount));
		}

		/// <summary>
		/// The marginal gain per cost of adding one test step of <paramref name="candidate"/> to the army: [Net(own + n x candidate, enemy) -
		/// <paramref name="baseNet"/>] / (n x cost). Marginal on the CURRENT army, so a screen is worth more once fragile damage dealers
		/// exist and a type the army already has enough of is worth less. A candidate without a cost gives 0.
		/// </summary>
		public static double Gain(BotUnitProfile candidate, IReadOnlyList<(BotUnitProfile Unit, double Count)> own,
			IReadOnlyList<(BotUnitProfile Unit, double Count)> enemy, double baseNet, int stepCost, int maxCount, int freeFireCapTicks,
			ArmyPairCache cache = null, int unansweredTicks = -1)
		{
			if (candidate.Cost <= 0)
				return 0;

			var n = StepCount(stepCost, candidate.Cost, maxCount);
			var with = new List<(BotUnitProfile Unit, double Count)>(own.Count + 1);
			var merged = false;
			foreach (var (unit, count) in own)
			{
				if (!merged && ReferenceEquals(unit, candidate))
				{
					with.Add((unit, count + n));
					merged = true;
				}
				else
					with.Add((unit, count));
			}

			if (!merged)
				with.Add((candidate, n));

			return (Evaluate(with, enemy, freeFireCapTicks, cache, unansweredTicks).Net - baseNet) / ((double)n * candidate.Cost);
		}

		/// <summary>
		/// The counter multipliers (percent, 100 = neutral) from the gains per cost of the candidates:
		/// 100 + gainPercent x z x band / 1000, clamped to minPercent..maxPercent. z = the RANK of the gain, -1000..1000 (ties share a rank),
		/// so the cost scale of the ruleset cannot dominate. band (0..1) is the dead band: the spread of the gains (max - min) over
		/// bandPermille of their mean size; gains that are almost equal stay near 100 and equal gains give exactly 100.
		/// </summary>
		public static void Multipliers(IReadOnlyList<double> gains, int gainPercent, int minPercent, int maxPercent, int bandPermille, int[] percents)
		{
			var count = gains.Count;
			for (var i = 0; i < count; i++)
				percents[i] = 100;

			if (count < 2)
				return;

			double min = double.MaxValue, max = double.MinValue, size = 0;
			foreach (var g in gains)
			{
				min = Math.Min(min, g);
				max = Math.Max(max, g);
				size += Math.Abs(g);
			}

			var spread = max - min;
			size /= count;
			if (!(spread > 0) || !(size > 0))
				return;

			var band = bandPermille <= 0 ? 1.0 : Math.Min(1.0, spread / (size * bandPermille / 1000.0));
			for (var i = 0; i < count; i++)
			{
				// Rank with ties averaged: (number below) + (number equal, without itself) / 2.
				int below = 0, equal = 0;
				for (var k = 0; k < count; k++)
				{
					if (gains[k] < gains[i])
						below++;
					else if (k != i && gains[k] == gains[i])
						equal++;
				}

				var z = (2.0 * (below + equal / 2.0) / (count - 1) - 1.0) * 1000.0;
				var percent = (int)Math.Round(100 + gainPercent * z * band / 1000.0);
				percents[i] = Math.Clamp(percent, Math.Min(minPercent, maxPercent), Math.Max(minPercent, maxPercent));
			}
		}
	}
}
