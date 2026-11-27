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
	// LATEPUSH-01 P1 (rows lp_age_min and lp_commit; design 01 D1 and D2, design 02). Only read behind the one launcher (and for lp_commit behind the guard floor), so @classic and every block without the keys play as before.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("LATEPUSH-01 P1 (row lp_age_min; needs the one launcher and WAVE-MIN): the leader's team attack minimum is the WAVE-MIN bar x (1 + tick / LatePushAgeTicks) x map context x the learned multiplier, never above LatePushCapPercent",
			"percent of what the team can send and never below WAVE-MIN. An open all-in keeps WAVE-MIN. Off in code and classic.")]
		public readonly bool LatePushAgeMinEnabled = false;

		[Desc("LATEPUSH-01 P1 (row lp_commit; needs sq_guard_lend and sq_guard_floor, inert with a DEV line without the floor): an order commits a share of the eligible army (free units plus the guard units above the floor) that grows",
			"with the tick and with the observed army ratio; the guard lends exactly what that share needs instead of the fixed lend share. Off in code and classic.")]
		public readonly bool LatePushCommitEnabled = false;

		[Desc("LATEPUSH-01 P1: the age factor of the team minimum is 1 + tick / LatePushAgeTicks (fixed, not capped).")]
		public readonly int LatePushAgeTicks = 30000;

		[Desc("LATEPUSH-01 P1: the age-scaled minimum is never above this percent of the team's eligible army (a shrinking team can still push); never below WAVE-MIN.")]
		public readonly int LatePushCapPercent = 60;

		[Desc("LATEPUSH-01 P1: the PRIOR of the committed share at tick 0, percent of the eligible army; it grows with the age factor (LatePushCommitCapPercent is the ceiling before the army ratio matters). Learnable (key latepush.lcommit).")]
		public readonly int LatePushCommitBasePercent = 30;

		[Desc("LATEPUSH-01 P1: the committed share never exceeds this percent from time alone; the observed army ratio (ArmyAheadArmyPercent and above) raises it to 100.")]
		public readonly int LatePushCommitCapPercent = 85;

		[Desc("LATEPUSH-01 P1: the PRIOR of the map context's reference distance in cells (the distance of the leader's last picked target over this, clamped to 0.8 - 1.4). Learnable (key latepush.kref).")]
		public readonly int LatePushMapRefCells = 64;

		[Desc("LATEPUSH-01 P1: the PRIOR of the one learned multiplier L on the age-scaled minimum, percent (rush and guerrilla 85, tech and expansion 100, steamroller 110, turtle 120). Learnable (key latepush.l).")]
		public readonly int LatePushPersonalityPercent = 100;
	}
}
