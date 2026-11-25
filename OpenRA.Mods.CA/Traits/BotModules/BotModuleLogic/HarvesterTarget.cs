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
	// HARV-01 (Opus/user 2026-11-25, V08 seed 100775): harvesters per refinery in both directions. The V08 bots ran out of money with ore left (seat_9
	// GDI 0 harvesters on 1 refinery for 12000 ticks) while an Ixian bot ran 21 harvesters on 1 refinery. Target = at least 2 per refinery (the
	// HarvesterBotModuleCA HarvestersPerRefinery, never below 2); cap = target + the slack per refinery. The refinery count carries the field scaling (the
	// refinery law places one refinery per anchor field); a learned per-field value is later work. Pure, no world.
	public static class HarvesterTarget
	{
		/// <summary>The harvesters wanted: at least two per standing refinery; none without a refinery.</summary>
		public static int Target(int refineries, int perRefinery) => refineries <= 0 ? 0 : refineries * Math.Max(2, perRefinery);

		/// <summary>The most harvesters allowed (live + queued): the target plus <paramref name="slackPerRefinery"/> per refinery.</summary>
		public static int Cap(int refineries, int perRefinery, int slackPerRefinery) =>
			refineries <= 0 ? 0 : Target(refineries, perRefinery) + refineries * Math.Max(0, slackPerRefinery);

		/// <summary>
		/// The queued harvesters of one type at a refresh (Sol FIX of HARV-01): the queue's own count, unless an order started less than latencyTicks ago is not in the queue yet
		/// (cachedCount counts it): then the cached count stands until the queue shows it or the window passes (a refused order). A late outgoing order is never forgotten.
		/// </summary>
		public static int EffectiveQueued(int queueCount, int cachedCount, int ticksSinceStart, int latencyTicks) =>
			queueCount >= cachedCount || ticksSinceStart >= Math.Max(0, latencyTicks) ? queueCount : cachedCount;

		/// <summary>A replacement is due: live + queued below the target and the bank at the minimum.</summary>
		public static bool Due(int live, int queued, int target, int bank, int minBank) => live + queued < target && bank >= Math.Max(0, minBank);

		/// <summary>One more harvester would pass the cap.</summary>
		public static bool OverCap(int live, int queued, int cap) => live + queued >= cap;
	}
}
