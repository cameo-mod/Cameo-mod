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
	// RAID-HARV-01 P1 (row raid_harv; design 01 D7, design 02, deltas 01 and 02). P1 has no wiring: these keys are the PRIORS of the nine learned raid numbers and the switch. Switch OFF in code, classic and every block until the A/B passes.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("RAID-HARV-01 (row raid_harv): the native harvester raid. Off in code, classic and all six blocks until its A/B passes.")]
		public readonly bool RaidHarvEnabled = false;

		[Desc("RAID-HARV-01: PRIOR of raid.lost_recon, the ticks a lost-target recon lasts before the raid is released. Learnable (4-value rule, DEV only).")]
		public readonly int RaidHarvLostReconTicks = 400;

		[Desc("RAID-HARV-01: PRIOR of raid.move_refresh, the ticks between re-issued raid moves while the target moves. Learnable.")]
		public readonly int RaidHarvMoveRefreshTicks = 125;

		[Desc("RAID-HARV-01: PRIOR of raid.req_margin, the percent of the target value the package must reach. Learnable.")]
		public readonly int RaidHarvReqMarginPercent = 150;

		[Desc("RAID-HARV-01: PRIOR of raid.recon_radius, the cells of the explored ring probed after a loss. Learnable.")]
		public readonly int RaidHarvReconRadiusCells = 8;

		[Desc("RAID-HARV-01: PRIOR of raid.incident_loss, the percent of its value a raid squad loses inside the window before it retreats. Learnable.")]
		public readonly int RaidHarvIncidentLossPercent = 25;

		[Desc("RAID-HARV-01: PRIOR of raid.incident_window, the ticks of the incident window. Learnable.")]
		public readonly int RaidHarvIncidentWindowTicks = 300;

		[Desc("RAID-HARV-01: PRIOR of raid.incident_decay, the ticks an incident cell counts as a threat. Learnable.")]
		public readonly int RaidHarvIncidentDecayTicks = 3000;

		[Desc("RAID-HARV-01: PRIOR of raid.waypoint_spacing, the cells between the explored waypoints a raid move is issued through. Learnable.")]
		public readonly int RaidHarvWaypointSpacingCells = 8;

		[Desc("RAID-HARV-01: PRIOR of raid.defence_radius, the cells around the target in which visible armed enemy actors raise the required package value. Learnable.")]
		public readonly int RaidHarvDefenceRadiusCells = 6;
	}
}
