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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 Q8 (design 4.1 Steps 2-3; rows sq_all_in_late and sq_hunt_one_cell under the master switch sq_pipeline_02). The all-in has ONE input list now: the cap, the
	// huge idle army, nearly won (ALL-IN-01), the stalemate breaker's signal (Q4) and the late tick (here, ArmyAllInLateTick). The breaker never launches a wave (Q4); the late-escalation clock
	// only paces the cadence (STALEMATE-01) and the late tick commits the whole army. The hunt has ONE cell for the team at a time, whoever asks.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqAllInLate, ablateSqHuntOneCell;

		/// <summary>
		/// Q8 (row sq_all_in_late; Opus: the decisive gate is tick 30000): from ArmyAllInLateTick (default 24000) the game is still on and every order is an all-in order, once, no steps.
		/// ALLIN-TRIGGER-01 (Opus ruling 2026-11-25, USER: a fixed tick is only a stagnation fallback): the tick answers only while the Q4 stagnation signal (the no-kill clock of the stalemate
		/// breaker) also holds, never on the tick alone; the dynamic ahead triggers come first (TeamLeadAllInWhy).
		/// Needs the one launcher (the order is the only attack); the all-in itself starts at TeamLeadAllInStartTick at the earliest.
		/// </summary>
		internal bool ArmyAllInLateOn(int now) => ArmyLaunchArbiterOn && !ablateSqAllInLate.Off(Player, AblationIds.SqAllInLate)
			&& now >= Info.TeamLeadAllInStartTick
			&& AllInCA.StalemateSignal(now, teamLeadTeam?.StalemateSignalTick ?? -1, Info.ArmyStalemateSignalTicks)
			&& AllInCA.LateTick(now, Info.ArmyAllInLateTick);

		/// <summary>Q8 (row sq_hunt_one_cell): the team hunts one unseen cell at a time (the board cell).</summary>
		internal bool ArmyHuntOneCellOn => ArmyPipelineOn && !ablateSqHuntOneCell.Off(Player, AblationIds.SqHuntOneCell);

		/// <summary>
		/// The hunt cell for a squad whose leader is <paramref name="leader"/>, under the row: the team's cell while it is held (nobody of the team sees it, not older than the hold), else
		/// the next cell by the plain pick (FinishHuntCell: unseen, least recently assigned, nearest), which becomes the team's cell. A cell this asker cannot walk to is not taken from
		/// the team: it gets its own pick and the board keeps the cell. Null = no reachable cell.
		/// </summary>
		CPos? TeamHuntCellFor(Actor leader)
		{
			var mobile = leader?.TraitOrDefault<Mobile>();
			var team = teamLeadTeam;
			if (mobile == null || team == null)
				return FinishHuntCell(leader, true);

			var now = World.WorldTick;
			if (team.HuntCell is { } shared
				&& TeamHuntCell.Keep(true, Player.Shroud.IsVisible(shared) || TeamHuntCell.Reached((leader.Location - shared).LengthSquared, Info.FinishHuntArriveCells), team.HuntTick, now,
					Info.ArmyHuntCellHoldTicks))
				return mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, leader.Location, shared) ? shared : FinishHuntCell(leader, true);

			var picked = FinishHuntCell(leader, true);
			if (picked is { } cell)
			{
				team.HuntCell = cell;
				team.HuntTick = now;
				DiagnosticTeamEvent("hunt_cell", "cell=" + cell.X + "," + cell.Y + " seat=" + teamLeadKey);
			}

			return picked;
		}
	}
}
