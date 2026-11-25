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
	/// <summary>
	/// CONSOLIDATE-02 Q8 (row sq_hunt_one_cell under the master switch sq_pipeline_02; design 4.1 Step 3 "one team hunt cell"): the team hunts ONE unseen cell at a time. Every
	/// squad of the team that hunts (the order's own hunt, a raider, a squad the order left) takes the cell on the team board, instead of every seat spreading its squads over its own
	/// least-recently-assigned cells. Pure rule, no world access: the cell stays until a bot of the team SEES it (it is empty: the hunt moves on) or it is held longer than the hold
	/// (a cell nobody reaches is not a hunt). Fog-honest: "seen" is the asking bot's own vision.
	/// </summary>
	public static class TeamHuntCell
	{
		/// <summary>
		/// The board cell is still the team's hunt cell: one is set, it is not explored (seen by the asker, or the asker stands at it: <see cref="Reached"/>), and it was taken at most
		/// <paramref name="holdTicks"/> ago (a tick in the future = not held).
		/// </summary>
		public static bool Keep(bool hasCell, bool explored, int sinceTick, int now, int holdTicks) =>
			hasCell && !explored && sinceTick >= 0 && now >= sinceTick && now - sinceTick <= Math.Max(1, holdTicks);

		/// <summary>
		/// The asker stands at the cell: within twice the arrival radius of the hunting squads (the asker is the squad's first unit, the arrival test reads its pathfind leader), so
		/// a squad that reached a cell it could not see never gets the same cell again.
		/// </summary>
		public static bool Reached(long distSq, int arriveCells) => distSq <= 4L * arriveCells * arriveCells;
	}
}
