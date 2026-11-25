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
	/// PUSHBAR-RCA-01 (row sq_guard_lend). V08D 6v6: 3 team orders, 0 team_push. The team's army sat in the seats' home guards (Protection squads: 90 to 126 units against 15 in
	/// the pool and the quiet wave squads), so an order found at most 3 joinable pool units per seat. A QUIET home guard lends its spare units to the open, unpushed team order.
	/// Pure: no world access, no RNG.
	/// </summary>
	public static class GuardLend
	{
		/// <summary>The most a guard lends, percent of its units. The guard always keeps at least one unit (it never empties).</summary>
		public const int MaxPercent = 90;

		/// <summary>
		/// The guard is quiet: it has no target (its own state sets one for any enemy near it), holds no rally for a predicted attack, answers no live protection request and
		/// no perceived or predicted threat stands against the base. Anything else and it keeps every unit (defence first).
		/// </summary>
		public static bool Quiet(bool hasTarget, bool rallyHeld, bool requestLive, bool threatened) =>
			!hasTarget && !rallyHeld && !requestLive && !threatened;

		/// <summary>
		/// The units a quiet guard of <paramref name="guardUnits"/> lends: <paramref name="lendPercent"/> of them (at most <see cref="MaxPercent"/>), never more than the
		/// <paramref name="joinable"/> ones (ground, armed, at home), and only as a batch of at least <paramref name="floorUnits"/> (a squad needs the join floor). 0 = nothing.
		/// </summary>
		public static int Units(int guardUnits, int joinable, int lendPercent, int floorUnits)
		{
			if (guardUnits <= 0 || joinable <= 0)
				return 0;

			var share = (int)((long)guardUnits * Math.Clamp(lendPercent, 0, MaxPercent) / 100);
			var lend = Math.Min(share, joinable);
			return lend >= Math.Max(1, floorUnits) ? lend : 0;
		}
	}
}
