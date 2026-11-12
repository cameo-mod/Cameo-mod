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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// HVCAP-01: pure harvester production rule. BotLimits.HarvesterValueLimit caps the summed ValuedInfo cost of the
	/// owner's live harvesters; 0 or negative = off. The caller passes the ScaleTargets-resolved count limit and
	/// maximum, so the value cap is evaluated after any provider target and cannot be bypassed by it.
	/// </summary>
	public static class HarvesterValueCap
	{
		public static bool Active(int cap) => cap > 0;

		/// <summary>Sum of the rule costs of the live harvesters.</summary>
		public static int Sum(IEnumerable<int> liveCosts)
		{
			var total = 0;
			foreach (var cost in liveCosts)
				total += cost;

			return total;
		}

		/// <summary>True when one more harvester of <paramref name="nextCost"/> keeps the live total at or under the cap. Off = always true.</summary>
		public static bool Allows(int cap, int valueNow, int nextCost) => !Active(cap) || valueNow + nextCost <= cap;

		/// <summary>
		/// The full production decision. USER RULING (2026-11-08): when the value cap is on (&gt; 0) it REPLACES the count
		/// limit: neither the limit nor maxHarvesters applies, only the value gate. Off (cap 0): the count gate, where a
		/// limit of 0 or less means no count limit and maxHarvesters always applies.
		/// TIER-01 R1: with no live harvester the value gate always admits one, so a cap below one harvester's cost never
		/// leaves a seat without income.
		/// </summary>
		public static bool MayProduce(int numHarvesters, int limit, int maxHarvesters, int valueCap, int valueNow, int nextCost)
		{
			if (Active(valueCap))
				return numHarvesters <= 0 || Allows(valueCap, valueNow, nextCost);

			return !((limit > 0 && numHarvesters >= limit) || numHarvesters >= maxHarvesters);
		}
	}
}
