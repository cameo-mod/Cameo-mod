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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// HANS-PROFILE-01 row 2 (ub_core_focus; Hans' top 3 unit types are 48 to 64 percent of his unit requests, the build-everything roll spreads over all of them). Once a type has been built once
	/// (coverage is done for it), the few dearest types of a queue's pool that the bank can pay for form the CORE. The core's weights are multiplied so that the core holds about the target share
	/// of the weight of the types built once; every other weight is left exactly as the roll made it (the first-build pull of a never-built type keeps coverage, and the roll's own floor chance is
	/// not lowered, only diluted by the larger total). A bias only: it never removes a candidate. Pure arithmetic over the numbers it is handed, no game state and no random draw.
	/// </summary>
	public static class CoreFocus
	{
		/// <summary>The target share is clamped below 100 so the multiplier formula always has a rest to compare against.</summary>
		public const int MaxSharePct = 99;

		/// <summary>The bank bar is a percent of the price, clamped to [0, 100] like TechLean.</summary>
		public const int MaxPct = 100;

		/// <summary>True when the bank already holds bankPct percent of a priced candidate (and is above 0): the same funding bar as the tech lean, so the core never names a unit the order gates could not start.</summary>
		public static bool Affordable(int price, int bankLeft, int bankPct) =>
			price > 0 && bankLeft > 0 && (long)bankLeft * 100 >= (long)price * Math.Clamp(bankPct, 0, MaxPct);

		/// <summary>
		/// The indices of the count dearest eligible candidates, dearest first; equal prices keep the lower index first (the pool arrives in a fixed order), so the set is deterministic.
		/// Fewer eligible than count = all of them; count 0 or less = none.
		/// </summary>
		public static List<int> Core(IReadOnlyList<int> price, IReadOnlyList<bool> eligible, int count)
		{
			var picked = new List<int>();
			if (count <= 0)
				return picked;

			for (var i = 0; i < price.Count; i++)
				if (eligible[i] && price[i] > 0)
					picked.Add(i);

			picked.Sort((a, b) => price[a] != price[b] ? price[b].CompareTo(price[a]) : a.CompareTo(b));
			if (picked.Count > count)
				picked.RemoveRange(count, picked.Count - count);

			return picked;
		}

		/// <summary>
		/// The weight multiplier in percent (100 = none) that lifts the core to the target share of the weight of the SETTLED candidates (built once): m = share x rest / ((100 - share) x core).
		/// 100 when the row is off (share 0 or less), when there is no core or no rest among the settled, or when the core already holds the share; never above maxPct (and never below 100).
		/// Candidates that are not settled are not counted at all: the first-build boost of a never-built type is untouched.
		/// </summary>
		public static int MultiplierPct(IReadOnlyList<double> weights, IReadOnlyList<bool> settled, IReadOnlyList<bool> core, int sharePct, int maxPct)
		{
			if (sharePct <= 0)
				return 100;

			var share = Math.Min(MaxSharePct, sharePct);
			double coreSum = 0, restSum = 0;
			for (var i = 0; i < weights.Count; i++)
			{
				if (!settled[i])
					continue;

				var w = double.IsFinite(weights[i]) && weights[i] > 0 ? weights[i] : 0;
				if (core[i])
					coreSum += w;
				else
					restSum += w;
			}

			if (coreSum <= 0 || restSum <= 0)
				return 100;

			var pct = share * restSum * 100.0 / ((100 - share) * coreSum);
			if (!(pct > 100))
				return 100;

			return (int)Math.Min(Math.Max(100, maxPct), Math.Floor(pct));
		}

		/// <summary>The share (percent, rounded down) of the settled candidates' weight that the core holds; 0 when the settled weight is 0.</summary>
		public static int SharePct(IReadOnlyList<double> weights, IReadOnlyList<bool> settled, IReadOnlyList<bool> core)
		{
			double coreSum = 0, all = 0;
			for (var i = 0; i < weights.Count; i++)
			{
				if (!settled[i])
					continue;

				var w = double.IsFinite(weights[i]) && weights[i] > 0 ? weights[i] : 0;
				all += w;
				if (core[i])
					coreSum += w;
			}

			return all <= 0 ? 0 : (int)Math.Floor(coreSum * 100.0 / all);
		}

		/// <summary>
		/// The weights with ONLY the core multiplied by multiplierPct/100 (capped at the roll's weight cap); every other weight, never-built types included, is returned exactly as it came, so no weight
		/// is ever lowered or lifted off the roll's own floor. A multiplier of 100 or less returns the weights unchanged.
		/// </summary>
		public static double[] Apply(IReadOnlyList<double> weights, IReadOnlyList<bool> core, int multiplierPct)
		{
			var result = new double[weights.Count];
			for (var i = 0; i < result.Length; i++)
				result[i] = multiplierPct > 100 && core[i] ? Math.Min(UnitRoll.WeightCap, weights[i] * multiplierPct / 100.0) : weights[i];

			return result;
		}
	}
}
