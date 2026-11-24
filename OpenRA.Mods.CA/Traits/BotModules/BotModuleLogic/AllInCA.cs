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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ALL-IN-01 (V07, USER 2026-11-24, master replay: "hundreds of idle units sit in the base while the enemy has a few buildings left, and the lag gets heavy").
	/// Pure rules, no world access; the wiring is SquadManagerBotModuleCA.V07.cs and the TEAM-LEAD code of SquadManagerBotModuleCA.TeamLead.cs. Behind the switch
	/// TeamLeadAllInEnabled (DEV row tl_all_in) the team leader marks an order ALL-IN when the team army is about the cap, or the idle army is very large, or the
	/// team has nearly won: the order is published at once (no interval wait) and every member sends every joinable unit - the pool and its quiet squads, raiders
	/// included - on it, keeps sending the units that arrive while the order is open, and a target-less order hunts. Squads that defend stay (threat-matched
	/// home defence). The pincer of the order is planned by TEAM-PINCER-01 / PINCER-02 as for every order. Observation only: the enemy value is what the bot
	/// sees or remembers of enemy structures, nothing else.
	/// </summary>
	public static class AllInCA
	{
		public const string WhyCap = "cap";
		public const string WhyIdle = "idle";
		public const string WhyWon = "won";

		/// <summary>The team's idle army is very large: at least <paramref name="threshold"/> units (0 or less = this trigger is off).</summary>
		public static bool IdleHuge(int blobUnits, int threshold) => threshold > 0 && blobUnits >= threshold;

		/// <summary>
		/// The team has nearly won: past the start tick, at least one enemy structure is known (a blind bot has nothing to compare), and the team army
		/// is at least <paramref name="winPercent"/> percent (never below 100) of the value of the known enemy structures.
		/// </summary>
		public static bool NearlyWon(int now, int startTick, long ourValue, long enemyStructureValue, int enemyStructures, int winPercent) =>
			now >= startTick && enemyStructures > 0 && ourValue > 0 && ourValue * 100 >= enemyStructureValue * Math.Max(100, winPercent);

		/// <summary>The trigger that fired (cap, then idle, then won), or null when none did.</summary>
		public static string Why(bool anyAtCap, bool idleHuge, bool nearlyWon) => anyAtCap ? WhyCap : idleHuge ? WhyIdle : nearlyWon ? WhyWon : null;

		/// <summary>The units a member needs in its pool to send them: the all-in minimum on an all-in order (never below 1), else the order's ordinary minimum.</summary>
		public static int JoinMinUnits(bool allIn, int ordinaryMin, int allInMin) => allIn ? Math.Max(1, allInMin) : ordinaryMin;

		/// <summary>A member that already joined this order asks again: only an all-in order takes the units that arrived since (the others join once).</summary>
		public static bool JoinsAgain(bool allIn, int joinedSeq, int orderSeq) => allIn || joinedSeq != orderSeq;
	}
}
