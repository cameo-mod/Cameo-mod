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
	/// PROD-IDLE_PLAN_02 section 6 (PR-2) queue depth for the fair unit path, pure and world-free.
	/// A single-slot queue is topped up while the build time already loaded in it is shorter than the
	/// gap until this bot's next pass, so the queue never runs dry between visits (round-robin makes
	/// that gap long). An empty queue always takes one item (the existing rule). The depth is bounded
	/// by <see cref="DepthCap"/>, a perf/safety cap and not a difficulty value, and every extra item is
	/// paid from spare cash only: the unpaid remainder of what is already queued is protected.
	/// </summary>
	public static class QueueDepthCA
	{
		/// <summary>Most items one single-slot queue may hold under the depth rule (PLAN_02 Q4, proposed 4).</summary>
		public const int DepthCap = 4;

		/// <summary>Upper bound on the items a non-parallel queue may take this visit: the room left under the cap
		/// when it is empty or its loaded build time is still shorter than the gap to the next pass; otherwise 0.
		/// The fill loop stops earlier through <see cref="Covered"/> once the gap is covered, so an empty queue
		/// takes one item when that item alone covers the gap (the old rule), more only when it does not.</summary>
		public static int FreeSlots(int queuedItems, long loadedTicks, int gapTicks)
		{
			var room = DepthCap - Math.Max(0, queuedItems);
			return room > 0 && (queuedItems <= 0 || loadedTicks < gapTicks) ? room : 0;
		}

		/// <summary>True once the items held (queued plus ordered this pass) already cover the gap, so the fill stops.
		/// An empty queue is never covered: it always takes its first item.</summary>
		public static bool Covered(int itemsHeld, long loadedTicks, int gapTicks) =>
			itemsHeld > 0 && loadedTicks >= gapTicks;

		/// <summary>An item beyond the first draws on cash that earlier queued items have not yet paid: it needs the
		/// bank left after this pass's orders AND the unpaid remainder of everything already queued.</summary>
		public static bool CashAdmits(long bankLeft, long unpaidQueuedCost, long cost) =>
			bankLeft - unpaidQueuedCost >= cost;
	}
}
