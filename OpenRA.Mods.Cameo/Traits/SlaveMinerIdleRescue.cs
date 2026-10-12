#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.Cameo.Traits
{
	public enum SlaveIdleRescueAction
	{
		None,
		Harvest,
		DeliverThenHarvest,
	}

	// Pure decision logic for the free-will slave miner idle rescue. Free-will slaves
	// are never reassigned by the master's AssignSlaveActivity, so any path that empties
	// a slave's activity queue would otherwise leave it standing still for good.
	// Deterministic and RNG-free: the outcome depends only on the arguments.
	public static class SlaveIdleRescue
	{
		public const int MinCadenceTicks = 100;

		public static int Cadence(int idleCheckTick) => Math.Max(MinCadenceTicks, idleCheckTick);

		// Returns true (and re-arms the countdown) once per cadence period.
		public static bool Due(ref int countdown, int idleCheckTick)
		{
			if (--countdown > 0)
				return false;

			countdown = Cadence(idleCheckTick);
			return true;
		}

		// The [MoveToDock, FindAndDeliverResources] pair is appended from every harvest and dock
		// notification; without this guard the queue grows by a pair per cycle. One pending harvest
		// after the current activity already guarantees the slave delivers and goes back to work.
		public static bool ShouldAppendDeliveryPair(bool hasMasterAndDock, bool harvestAlreadyQueued)
		{
			return hasMasterAndDock && !harvestAlreadyQueued;
		}

		public static SlaveIdleRescueAction Decide(
			bool isIdle, bool isInWorld, bool isDead, bool sameOwnerAsMaster,
			bool isFreed, bool harvesterEnabled, bool isFull)
		{
			if (!isIdle || !isInWorld || isDead || !sameOwnerAsMaster || isFreed || !harvesterEnabled)
				return SlaveIdleRescueAction.None;

			return isFull ? SlaveIdleRescueAction.DeliverThenHarvest : SlaveIdleRescueAction.Harvest;
		}
	}
}
