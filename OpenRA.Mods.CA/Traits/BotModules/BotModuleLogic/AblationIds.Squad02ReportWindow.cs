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
	// PUSHBAR-RCA-01 S1. Row off = the old no-report grace (500 ticks from the issue or the last report), whatever the gather window.
	public static partial class AblationIds
	{
		// The no_reports cancel waits for the order's join window (TeamLeadGatherMaxTicks after the issue) before it may cancel an order nobody joined.
		public const string SqReportWindow = "sq_report_window";
	}
}
