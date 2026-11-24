#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// ESSENTIALS-01 (USER 11-24: no spam at low cash). Pure decision: below the tier's MaximiseProductionCash line the base builder admits ONLY essentials.
	public static class EssentialsLowCashCA
	{
		/// <summary>
		/// True when the base builder must hold this start. Held only while <paramref name="armed"/> and the bank is under the tier
		/// <paramref name="line"/> (line &lt;= 0 = no tier line, never held), and the item is no essential: a first copy of its kind (each available
		/// building once, counting orders still in flight), a tech-chain prerequisite, a refinery (its own per-field law bounds the count),
		/// a power plant while power is short, a passive-income generator (always built up to its limit), or a planner/demand-driven start
		/// (threat-driven defence, crawl link, due MCV prerequisite). Extras (a second copy) wait for the bank to reach the line.
		/// Threat-driven defence stays bounded by the AF2 vote and RATIO-2X, which run after this gate.
		/// </summary>
		public static bool Holds(bool armed, int bank, int line, bool firstCopy, bool techChain, bool refinery, bool powerWhileShort,
			bool passiveIncome, bool plannerOrDemand)
		{
			if (!armed || line <= 0 || bank >= line)
				return false;

			return !(firstCopy || techChain || refinery || powerWhileShort || passiveIncome || plannerOrDemand);
		}
	}
}
