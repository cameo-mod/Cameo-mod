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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TIER-MIX-01 (row ub_tech_lean; V08D 6v6: the roll rolled vehicles at 1033 growing to 1890 per unit, what was built stayed 900 to 990): a unit order must have its FULL price in the bank
	/// and a refused pick is re-rolled among the cheaper rest, so a low bank (median 334 to 1164 from tick 5000) built tier 1 whatever the roll said. Only the FIRST build of a type was pay-as-you-go
	/// (UnitRoll.FirstAdmits). The lean gives the same pay-as-you-go admission to a LATE pick from the TOP of its queue's pool, on an EMPTY queue, with a share of its price already in the bank.
	/// Pure integer math, no game state and no random draw: the weights are untouched, no queue is held, and the order still reserves its full price (batch.Reserve).
	/// </summary>
	public static class TechLean
	{
		/// <summary>The percent inputs are clamped to [0, 100]: a bad yaml value can neither disable the funding test (below 0) nor demand more than the whole price (above 100).</summary>
		public const int MaxPct = 100;

		/// <summary>
		/// True when the pick is admitted pay-as-you-go: lean on (topPct above 0), a priced pick that is at least topPct percent of the dearest type of its pool, late (tick at or after startTick),
		/// the queue holds nothing (at most one lean item in flight per queue) and the bank left already covers bankPct percent of the price (and is above 0).
		/// </summary>
		public static bool Admits(int cost, int poolMaxCost, int held, int tick, int startTick, int bankLeft, int topPct, int bankPct)
		{
			if (topPct <= 0 || cost <= 0 || poolMaxCost <= 0 || held != 0 || tick < startTick || bankLeft <= 0)
				return false;

			if ((long)cost * 100 < (long)poolMaxCost * Math.Min(MaxPct, topPct))
				return false;

			return (long)bankLeft * 100 >= (long)cost * Math.Clamp(bankPct, 0, MaxPct);
		}

		/// <summary>The dearest price of the pool this fill started from: a fill's first roll (nothing refused yet) starts over, a re-roll after a refusal keeps the earlier top, so a refused expensive pick does not lower the bar for the next one.</summary>
		public static int PoolMax(int refusedSoFar, int previousMax, int currentMax) =>
			Math.Max(refusedSoFar == 0 ? 0 : Math.Max(0, previousMax), Math.Max(0, currentMax));
	}
}
