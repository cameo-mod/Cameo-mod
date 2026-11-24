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
	// STALEMATE-01 (USER BINDING 11-23: no stalemates; games end decisively). Both switches default off; classic never arms them.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("STALEMATE-01 late escalation (row st_late_escalation): from LateEscalationStartTick every personality, Turtle included,",
			"attacks more often (the MinimumAttackForceDelay reset scaled by LateEscalationDelayPercent, stepped). The launch bar is never",
			"lowered (r5, USER 11-24). Off = unchanged.")]
		public readonly bool LateEscalationEnabled = false;

		[Desc("STALEMATE-01: first tick of late escalation and of finish mode.")]
		public readonly int LateEscalationStartTick = 25000;

		[Desc("STALEMATE-01: percent of the attack-delay reset while late escalation is on (0-100).")]
		public readonly int LateEscalationDelayPercent = 50;

		[Desc("STALEMATE-01 r4 (V01 timeout): every LateEscalationStepTicks after LateEscalationStartTick the attack-delay percent is",
			"multiplied again by LateEscalationStepPercent, never below LateEscalationDelayFloorPercent. 0 ticks = no steps.")]
		public readonly int LateEscalationStepTicks = 5000;

		[Desc("STALEMATE-01 r4: per-step multiplier of the attack-delay percent (0-100).")]
		public readonly int LateEscalationStepPercent = 75;

		[Desc("STALEMATE-01 r4: lowest attack-delay percent the steps reach (0-100).")]
		public readonly int LateEscalationDelayFloorPercent = 20;

		[Desc("BLOB-LAUNCH-01 (USER 11-24, row bl_leader_order_only): with TEAM-LEAD cadence on, the team leader forms no wave of its own",
			"outside an open team order; it attacks only by publishing the order and joining it. Off = the leader may launch its own wave.")]
		public readonly bool BlobLeaderOrderOnly = false;

		[Desc("BLOB-LAUNCH-01 safety fallback (Opus 11-24): after this many ticks without any team order, with the team pool at the blob",
			"minimum (BlobLaunch01CA.MinUnits), the leader may launch its own wave; its squad then issues the team order. 0 = never.")]
		public readonly int BlobLeaderFallbackTicks = 3000;

		[Desc("STALEMATE-01 finish mode (row st_finish_mode): from LateEscalationStartTick a ground squad with no visible and no",
			"remembered enemy target hunts instead of holding: it attack-moves to the least recently assigned map grid cell the bot",
			"cannot see now (fog-honest) that its leader can reach, and on arrival takes the next. Off = it holds, as before.")]
		public readonly bool FinishModeEnabled = false;

		[Desc("STALEMATE-01: hunt grid spacing in cells.")]
		public readonly int FinishHuntGridCells = 16;

		[Desc("STALEMATE-01: a hunt cell counts as reached within this many cells of the leader.")]
		public readonly int FinishHuntArriveCells = 4;

		[Desc("STALEMATE-01: at most this many reachability checks per hunt pick.")]
		public readonly int FinishHuntMaxChecks = 16;
	}
}
