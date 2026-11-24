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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// BLOB-LAUNCH-01 (USER 11-24: BIG blob pushes, not frequent trickles). Pure decisions.
	public static class BlobLaunch01CA
	{
		/// <summary>The team blob's launch minimum: the WAVE-MIN unit tier, and at least half of the team's idle army units (rounded up).</summary>
		public static int MinUnits(int waveMinUnits, int teamIdleUnits) =>
			Math.Max(Math.Max(0, waveMinUnits), (Math.Max(0, teamIdleUnits) + 1) / 2);

		/// <summary>Attack gate: a wave of waveUnits holds below MinUnits(waveMinUnits, idleUnits).</summary>
		public static bool GateHolds(int waveUnits, int waveMinUnits, int idleUnits) => waveUnits < MinUnits(waveMinUnits, idleUnits);

		/// <summary>The attack gate with the idle-pool term clamped to the reachable units, so unreachable idle units never hold a wave.</summary>
		public static bool ReachableGateHolds(int waveUnits, int waveMinUnits, int idleUnits, int reachableUnits) =>
			GateHolds(waveUnits, waveMinUnits, Math.Min(Math.Max(0, idleUnits), Math.Max(0, reachableUnits)));

		/// <summary>Whether the team leader's own attack-force check forms no wave: switch on, it is the engaged leader, cadence is on,
		/// and no team order is open (an open order is joined, not launched alone). Safety fallback (Opus 11-24): after fallbackTicks
		/// without any team order, with the team pool at the blob minimum, the leader may launch (its squad then issues the team order).</summary>
		public static bool LeaderHolds(bool active, bool isTeamLeader, bool cadenceOn, bool orderOpen, int ticksSinceOrder, int fallbackTicks,
			int teamPoolUnits, int minUnits) =>
			active && isTeamLeader && cadenceOn && !orderOpen
				&& !(fallbackTicks > 0 && ticksSinceOrder >= fallbackTicks && teamPoolUnits >= minUnits);

		/// <summary>Ticks since the team's last order; before the first one, since the initial attack delay ended (never negative).</summary>
		public static int TicksSinceOrder(int now, int lastOrderTick, int initialAttackDelay) =>
			Math.Max(0, now - (lastOrderTick >= 0 ? lastOrderTick : initialAttackDelay));
	}
}
