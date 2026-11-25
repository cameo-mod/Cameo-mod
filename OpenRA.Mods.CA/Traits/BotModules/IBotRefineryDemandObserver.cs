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
	/// <summary>
	/// REF-DEMAND-01, DEV only: record-only observer of the refinery law's demand (sibling of <see cref="IBotBuildingPickObserver"/>). Implemented by the
	/// DEV player telemetry (OpenRA.Mods.Cameo.Dev); every call site is compiled out of a release build (Conditional CAMEO_DEV_LOGGING) and the planner
	/// never reads anything back, so an absent observer leaves behaviour bit-identical. The observer rate-limits per (anchor, row) on change or every
	/// 750 ticks. Zone rows carry the zone id, the zone centre (its anchor cell) and the zone radius.
	/// </summary>
	public interface IBotRefineryDemandObserver
	{
		/// <summary>The zone model was built (once per field model): <paramref name="zones"/> zones over <paramref name="fields"/> fields.</summary>
		void RefineryZonesBuilt(Player owner, int tick, int zones, int fields, int points, int radius);

		/// <summary>One zone's demand verdict (<see cref="RefineryDemand.Reason"/>, or "zone_gate" when REF-FIELD-02 blocks a demand zone).</summary>
		void RefineryZoneVerdict(Player owner, int tick, int zone, CPos center, int radius, string reason, int refineries, int demand, int capRemaining, int yardDistance);

		/// <summary>The per-bot sum of the last anchor update: <paramref name="demand"/> = the eligible zone count (as gameplay counts it), <paramref name="capRemaining"/> = the refineries the demand zones may still take before their caps.</summary>
		void RefineryDemandSummary(Player owner, int tick, int zones, int zonesInDemand, int demand, int capRemaining, int ownRefineries, int friendlyRefineries,
			int outOfRadius, int unreachable, int capped, int alliedServed);

		/// <summary>An anchor was parked (<paramref name="reason"/> claim_fail or stuck) until <paramref name="untilTick"/>.</summary>
		void RefineryAnchorParked(Player owner, int tick, int zone, CPos center, string reason, int untilTick);

		/// <summary>An anchor went pending (a committed refinery on its way) until <paramref name="untilTick"/>.</summary>
		void RefineryAnchorPending(Player owner, int tick, int zone, CPos center, int untilTick);

		/// <summary>A law refinery was committed at <paramref name="site"/> for <paramref name="zone"/>; <paramref name="fallbackSpacing"/> = the site is
		/// only legal at the fallback spacing. <paramref name="nearestFriendly"/> = cells to the nearest friendly refinery (-1 = none).</summary>
		void RefineryPlaced(Player owner, int tick, int zone, CPos center, CPos site, bool fallbackSpacing, int nearestFriendly);
	}
}
