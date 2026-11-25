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
	/// PUSHBAR-RCA-01 S1 (row sq_report_window). V08D 6v6: order 1 was cancelled no_reports at age 527 and order 2 at 1195, while the order's gather window is 1500 ticks
	/// (TeamLeadGatherMaxTicks): the 500-tick no-report grace (5 x TeamLeadCheckTicks) ended the order before the members had their join window, so no push (the window push at
	/// age 1500) could ever fire for an order nobody joined at once. With the row on, no_reports waits for the join window. Pure: no world access, no RNG.
	/// </summary>
	public static class ReportWindow
	{
		/// <summary>The order's join window is still open: fewer than <paramref name="windowTicks"/> ticks (at least 1) since it was issued. No_reports cannot cancel it yet.</summary>
		public static bool Open(int now, int issueTick, int windowTicks) => now - issueTick < Math.Max(1, windowTicks);
	}
}
