#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 Q8b (row sq_hunt_follow under sq_pipeline_02; Opus 2026-11-25 approved). A pushed order that hunts (no target actor) used to stay at its first hunt cell: ArmyRetargetEngaged
	// returns on a hunt-to-hunt pick, so the blob idled on an explored cell until the order ran out (ArmyEngagedMaxTicks). With the team's one hunt cell (Q8) the pick changes only when the old cell
	// was explored (a bot of the team saw it, or the squad stood at it) or its hold ran out, so following it is safe: the order moves on, its squads are re-pointed by the order's TargetSeq.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqHuntFollow;

		/// <summary>Q8b (row sq_hunt_follow): needs the team's one hunt cell (it is what makes a changed pick mean "explored") and the persistent order.</summary>
		internal bool ArmyHuntFollowOn => ArmyHuntOneCellOn && ArmyOrderEngagedOn && !ablateSqHuntFollow.Off(Player, AblationIds.SqHuntFollow);

		/// <summary>
		/// Leader only, from ArmyRetargetEngaged: the pushed order hunts and the leader's pick (<paramref name="target"/>, no actor) is another cell than the order's: the order moves on to it.
		/// False = it stays (row off, an order with a target actor, the same cell, or too soon after its push or last move).
		/// </summary>
		bool ArmyHuntFollowMoves(TeamLeadOrder order, WPos target, int now)
		{
			if (!ArmyHuntFollowOn || order.TargetActorId != 0)
				return false;

			var cell = World.Map.CellContaining(target);
			if (!TeamHuntCell.Follow(cell == World.Map.CellContaining(order.Target), System.Math.Max(order.PushTick, order.HuntFollowTick), now, Info.ArmyHuntFollowMinTicks))
				return false;

			order.Retarget(target, 0);
			order.HuntFollowTick = now;
			if (DiagnosticWriter != null)
				DiagnosticTeamEvent("team_hunt_follow", "seq=" + order.Seq + " tseq=" + order.TargetSeq + " cell=" + cell.X + "," + cell.Y);

			return true;
		}
	}
}
