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
	// PUSHBAR-RCA-01 S1 (row sq_report_window; V08D 6v6: orders 1 and 2 were cancelled no_reports after 527 and 1195 ticks, before the 1500-tick gather window that can push them).
	// Read only behind the one launcher (SquadPipeline02Enabled); with the DEV row off the no_reports clause is the old one.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqReportWindow;

		/// <summary>PUSHBAR-RCA-01 S1 (row sq_report_window; needs the one launcher): an order nobody joined is not cancelled for no reports before its join window is over.</summary>
		internal bool ArmyReportWindowOn => ArmyLaunchArbiterOn && !ablateSqReportWindow.Off(Player, AblationIds.SqReportWindow);

		// True while the order's join window (TeamLeadGatherMaxTicks from its issue) is open and the row is on: TeamLeadMaintainOrder then keeps an order with no report.
		bool ArmyReportWindowOpen(TeamLeadOrder order, int now) => ArmyReportWindowOn && ReportWindow.Open(now, order.IssueTick, Info.TeamLeadGatherMaxTicks);
	}
}
