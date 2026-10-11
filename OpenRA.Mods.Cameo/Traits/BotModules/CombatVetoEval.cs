#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModuleLogic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// The pure verdict math of the combat veto (AI_ARCHITECTURE §12.31): the same HP-share damage assembly
	/// <c>BotCombatPredictor</c> uses, with the optional tier-1 prior applied per (attacker, target) pair before the
	/// Lanchester core runs. The formula never leaves <c>BotCombatPredictor.Predict</c> — one authority.
	/// FIXED POINT: thresholds and corrections are integers (percent / thousandths).
	/// </summary>
	public static class CombatVetoEval
	{
		/// <summary>Predict like <see cref="BotCombatPredictor.Predict(IReadOnlyList{ValueTuple{BotUnitProfile, int}}, IReadOnlyList{ValueTuple{BotUnitProfile, int}})"/>,
		/// multiplying each attacker's per-pair damage by <paramref name="priors"/>' correction (null = neutral).</summary>
		public static BotCombatPredictor.Prediction Predict(
			IReadOnlyList<(BotUnitProfile Unit, int Count)> own,
			IReadOnlyList<(BotUnitProfile Unit, int Count)> enemy,
			IBotEngagementPriors priors,
			bool useEffective = false)
		{
			var ownHp = SumHp(own);
			var enemyHp = SumHp(enemy);
			// The fitted file measures OUR faction's trades: it corrects own-side damage only. The enemy
			// direction stays unfitted (null = neutral), so a uniform prior can't cancel itself out.
			var pred = BotCombatPredictor.Predict(
				DamagePerTick(own, enemy, enemyHp, priors, useEffective), ownHp,
				DamagePerTick(enemy, own, ownHp, null, useEffective), enemyHp);

			// The fitted attrition exponent warps the aggregate ratio (the fitter's AttritionExponentMilli);
			// fractions re-derive from the warped ratio so the Lanchester invariant holds. Neutral = 1000.
			var alpha = priors?.AttritionExponentMilli ?? 1000;
			if (alpha == 1000)
				return pred;

			var ratio = Math.Min(BotCombatPredictor.MaxRatio, Math.Pow(pred.Ratio, alpha / 1000.0));
			return ratio >= 1
				? new BotCombatPredictor.Prediction(ratio, Math.Sqrt(1 - 1 / ratio), 0)
				: new BotCombatPredictor.Prediction(ratio, 0, Math.Sqrt(1 - ratio));
		}

		static double SumHp(IReadOnlyList<(BotUnitProfile Unit, int Count)> force)
		{
			var hp = 0.0;
			foreach (var (unit, count) in force)
				hp += (double)unit.Hp * count;
			return hp;
		}

		static double DamagePerTick(IReadOnlyList<(BotUnitProfile Unit, int Count)> attackers,
			IReadOnlyList<(BotUnitProfile Unit, int Count)> targets, double targetHp, IBotEngagementPriors priors,
			bool useEffective)
		{
			if (targetHp <= 0)
				return 0;

			var total = 0.0;
			foreach (var (attacker, count) in attackers)
				foreach (var (target, targetCount) in targets)
				{
					var dpt = attacker.DamagePerTickAgainst(target, useEffective);
					if (priors != null)
						dpt = dpt * priors.CorrectionMilli(attacker, target) / 1000.0;

					total += count * dpt * target.Hp * targetCount / targetHp;
				}

			return total;
		}

		/// <summary>Engage-veto hysteresis: an uncommitted squad is vetoed below <paramref name="engageRatioPct"/>;
		/// an already-committed one only below the lower <paramref name="abortRatioPct"/> (enter ≥, exit &lt;).</summary>
		public static bool EngageVetoed(int ratioPct, bool alreadyCommitted, int engageRatioPct, int abortRatioPct) =>
			ratioPct < (alreadyCommitted ? abortRatioPct : engageRatioPct);

		/// <summary>True when the seen pursuers outrun the force: mean speed (count-weighted) of the pursuers
		/// reaches <paramref name="speedMarginPct"/> percent of the force's mean. A pursuer-free list cannot outrun.</summary>
		public static bool CannotOutrun(IReadOnlyList<(BotUnitProfile Unit, int Count)> force,
			IReadOnlyList<(BotUnitProfile Unit, int Count)> pursuers, int speedMarginPct)
		{
			var ownSpeed = MeanSpeed(force);
			var pursuerSpeed = MeanSpeed(pursuers);
			return pursuerSpeed > 0 && ownSpeed > 0 && pursuerSpeed * 100 >= ownSpeed * speedMarginPct;
		}

		static double MeanSpeed(IReadOnlyList<(BotUnitProfile Unit, int Count)> force)
		{
			long speedSum = 0, countSum = 0;
			foreach (var (unit, count) in force)
			{
				if (unit.Speed <= 0)
					continue;

				speedSum += (long)unit.Speed * count;
				countSum += count;
			}

			return countSum == 0 ? 0 : (double)speedSum / countSum;
		}
	}
}
