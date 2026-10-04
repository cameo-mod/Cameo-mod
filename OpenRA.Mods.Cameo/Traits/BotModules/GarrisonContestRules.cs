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

using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// The pure decisions inside <see cref="GarrisonContestBotModule.TickContests"/>, free of World/Actor so every
	/// branch is unit-testable (AI_ARCHITECTURE hotspot #7). Behaviour is bit-identical to the inline conditions
	/// these replace — including the lease heartbeat inside the walker-done predicate, which is a side effect
	/// (renewal) embedded in a filter and must stay lazy, and the stand-down/timeout ordering.
	/// </summary>
	public static class GarrisonContestRules
	{
		/// <summary>A walking claim is superseded when an outranking ally claims the recorded cell — the claim
		/// set may be absent (flag off / 1v1) and the cell lookup may miss, so the cell read stays lazy.</summary>
		public static bool SupersedesClaim(bool hasClaimSet, bool hasCell, Func<bool> cellClaimed) =>
			hasClaimSet && hasCell && cellClaimed();

		/// <summary>A claim times out when its recorded start is older than the timeout — the start tick must exist
		/// before the subtraction, and the timeout only counts while a claim is not superseded.</summary>
		public static bool ClaimTimedOut(bool hasStartTick, int elapsedTicks, int timeoutTicks) =>
			hasStartTick && elapsedTicks > timeoutTicks;

		/// <summary>A walker leaves the claim when it is inside, dead, gone, stolen, or the lease no longer holds.
		/// The lease check is the heartbeat renewal — a side effect that must only run for a live walker still
		/// outside, so it arrives lazy.</summary>
		public static bool WalkerDone(bool inside, bool dead, bool inWorld, bool ownedByUs, Func<bool> leaseLost) =>
			inside || dead || !inWorld || !ownedByUs || leaseLost();

		/// <summary>A neutral garrisonable may be contested when it is alive, neutral-owned, has room, is not already
		/// claimed by us, is not inside its block window, is not claimed by an outranking ally, has been scouted
		/// (fog-honest), and sits inside the frontier radius.</summary>
		public static bool IsContestable(bool alive, bool neutralOwned, bool hasSpace, bool alreadyClaimed,
			bool blocked, bool claimedByAlly, bool explored, bool withinRadius) =>
			alive && neutralOwned && hasSpace && !alreadyClaimed && !blocked && !claimedByAlly && explored && withinRadius;

		/// <summary>A spare idle infantry may walk when it can actually fight from a port and no other module owns it.</summary>
		public static bool IsContestWalker(bool alive, bool idle, bool canFight, bool claimedByOther) =>
			alive && idle && canFight && !claimedByOther;

		/// <summary>The claimed set's capacity gate — the pass stops once the claims, the lease pool, or the spare
		/// infantry run out.</summary>
		public static bool CapacityReached(int activeClaims, int maxClaims, int leasedWalkers, int maxLeased, int sparePool) =>
			activeClaims >= maxClaims || leasedWalkers >= maxLeased || sparePool <= 0;

		/// <summary>Occupied weight to aim for per claimed garrison, clamped to the building's own capacity.</summary>
		public static int DesiredWalkers(int garrisonMaxWeight, int desiredOccupancyWeight) =>
			Math.Min(garrisonMaxWeight, Math.Max(1, desiredOccupancyWeight));

		/// <summary>Contest ordering: nearest first, but distance is normalized by how much the building can hold —
		/// a big garrison justifies a longer walk. MaxWeight 0 sorts as weight 1, never divides by zero.</summary>
		public static long ContestRankKey(long distanceSquared, int garrisonMaxWeight) =>
			distanceSquared / Math.Max(1, garrisonMaxWeight);

		/// <summary>The claims cap on its own — the early-out when the claim book is already full.</summary>
		public static bool ClaimsFull(int activeClaims, int maxClaims) => activeClaims >= maxClaims;

		/// <summary>One more walker fits the claim when the occupancy target is unmet, the pool and the lease
		/// budget are not exhausted, and the garrison has room for the running total.</summary>
		public static bool FitsOneMore(int usedWeight, int desiredWeight, int poolCount, int leased, int maxLeased, bool hasSpace) =>
			usedWeight < desiredWeight && poolCount > 0 && leased < maxLeased && hasSpace;
	}
}
