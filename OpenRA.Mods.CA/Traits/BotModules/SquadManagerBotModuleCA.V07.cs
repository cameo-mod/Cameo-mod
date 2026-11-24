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
	// V07 (USER master replay 2026-11-24, via Opus). One switch + one DEV row per item; each is read only behind its own switch, so with it off
	// the bot plays exactly as before.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablatePincerWide;

		// PINCER-02: read by the leader (it plans the prongs and decides the push); the members follow the order's own prong data.
		internal bool TeamLeadPincerWideOn => TeamLeadPincerOn && Info.TeamLeadPincerWideEnabled && !ablatePincerWide.Off(Player, AblationIds.TeamLeadPincerWide);

		// PINCER-02: a pincer that is due to push collapses into the single blob unless BOTH arms hold the arm bar (units and value of each arm on its own).
		bool TeamLeadPincerArmCollapse(TeamLeadOrder order, int now)
		{
			if (!WaveMinActive)
				return false;

			var (minUnits, minValue) = WaveMinBars();
			var (value0, _) = order.Totals(0);
			var (value1, _) = order.Totals(1);
			var units0 = order.ProngUnits(0);
			var units1 = order.ProngUnits(1);
			if (PincerWideCA.ArmsHold(units0, value0, units1, value1, minUnits, minValue, Info.TeamLeadPincerArmPercent))
				return false;

			order.PincerCollapsed = true;
			DiagnosticTeamEvent("team_pincer_collapse", "seq=" + order.Seq + " why=arm arms=" + units0 + "/" + value0 + "," + units1 + "/" + value1
				+ " bars=" + PincerWideCA.ArmBar(minUnits, Info.TeamLeadPincerArmPercent) + "/" + PincerWideCA.ArmBar(minValue, Info.TeamLeadPincerArmPercent)
				+ " age=" + (now - order.IssueTick));
			return true;
		}

		// PINCER-02 DEV record (the writer is null in a release build): a pincer was published or released, with the approach angle of its two staging points.
		void TeamLeadPincerRecord(string evt, TeamLeadOrder order, string detail)
		{
			if (DiagnosticWriter == null)
				return;

			var angle = PincerWideCA.SeparationDegrees(order.Target, order.ProngRally(0), order.ProngRally(1));
			DiagnosticTeamEvent(evt, "seq=" + order.Seq + " angle=" + angle + " wide=" + (TeamLeadPincerWideOn ? 1 : 0) + " " + detail);
		}
	}
}
