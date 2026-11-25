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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// PROD-IDLE_PLAN_03 section 2.2: C = cash and resources LESS the outstanding unpaid commitments, each counted once.
	/// A queued item draws cash progressively, so its unpaid part is exactly <c>RemainingCost</c> (the same figure TF-2's
	/// UpdateMobileReserve counts per item). Read-only: no orders, no RNG.
	/// </summary>
	public static class SpendCommitmentsCA
	{
		/// <summary>Cash the player's already queued items (every queue: units, buildings, defences) have not paid yet.</summary>
		public static long UnpaidQueued(World world, Player player) => UnpaidQueued(world, player, null);

		/// <summary>PROD-FIX-01 F-A / UNIT-MIX-01 (X5): as above, but queues whose type <paramref name="exclude"/> accepts are not counted (null = none).</summary>
		public static long UnpaidQueued(World world, Player player, Func<string, bool> exclude)
		{
			long unpaid = 0;
			foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
			{
				if (pair.Actor.Owner != player || pair.Actor.IsDead || !pair.Trait.Enabled)
					continue;

				if (exclude != null && exclude(pair.Trait.Info.Type))
					continue;

				foreach (var item in pair.Trait.AllQueued())
					unpaid += item.RemainingCost;
			}

			return unpaid;
		}

		/// <summary>C for <see cref="RecoverySpendPolicyCA.Admits"/>: never negative, clamped to the int range.</summary>
		public static int Available(long cash, long committed) =>
			(int)Math.Max(0L, Math.Min(int.MaxValue, cash - committed));
	}
}
