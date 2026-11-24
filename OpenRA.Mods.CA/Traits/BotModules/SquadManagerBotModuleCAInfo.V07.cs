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
	// V07 (USER master replay 2026-11-24, via Opus). Every value is a named yaml parameter; none is read while its switch is false.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("PINCER-02 (release switch; the DEV row is tl_pincer_wide). On (needs TeamLeadPincerEnabled): any approach separation of TeamLeadPincerMinAngleDegrees to 180 counts, the staging search fans out to more angles when the preferred spread has no legal point, and the two-times-WAVE-MIN gate is read per arm (TeamLeadPincerArmPercent). Off: TEAM-PINCER-01 exactly as before.")]
		public readonly bool TeamLeadPincerWideEnabled = false;

		[Desc("PINCER-02: the smallest angle between the two approaches, as seen from the target, in degrees. Range 60-180; 90 is the user rule (90-180 counts).")]
		public readonly int TeamLeadPincerMinAngleDegrees = 90;

		[Desc("PINCER-02: each arm needs this percent of the WAVE-MIN bars (units and value, each arm on its own); 100 = a full WAVE-MIN per arm. Range 50-100. Below 100 the pincer fires with a smaller blob (the pair still holds at least twice the arm bar, so never less than this share of two waves).")]
		public readonly int TeamLeadPincerArmPercent = 100;

		[Desc("SOLO-LEAD (release switch; the DEV row is tl_solo_lead). On: a team of ONE living bot (a 1v1, an FFA seat, the last survivor of a team) is engaged and leads itself, so TEAM-LEAD, ALL-IN and PINCER work without allies. Off: TEAM-LEAD needs two living bots, as before.")]
		public readonly bool TeamLeadSoloEnabled = false;

		[Desc("ALL-IN-01 (release switch; the DEV row is tl_all_in). On (needs TeamLeadEnabled and the cadence publish): the team leader marks an order ALL-IN when a member's idle army is at its tier limit, the team idle army has TeamLeadAllInIdleUnits units, or the team has nearly won (TeamLeadAllInWinPercent). The order publishes at once, every member sends every joinable unit (raiders included, new units while the order is open), and an order without a known target hunts. Defending squads stay.")]
		public readonly bool TeamLeadAllInEnabled = false;

		[Desc("ALL-IN-01: the team's idle army (pools and quiet wave squads) of at least this many units triggers an all-in. 0 = this trigger is off. The tier idle limits (25 Noob .. 75 God) trigger on their own.")]
		public readonly int TeamLeadAllInIdleUnits = 60;

		[Desc("ALL-IN-01: nearly won = the team army value is at least this percent of the value of the enemy structures the leader sees or remembers (never below 100).")]
		public readonly int TeamLeadAllInWinPercent = 400;

		[Desc("ALL-IN-01: the nearly-won trigger waits for this world tick (a scouted early outpost is not a won game).")]
		public readonly int TeamLeadAllInStartTick = 15000;

		[Desc("ALL-IN-01: the nearly-won scan (every enemy structure the leader sees or remembers) runs at most once per this many ticks; the last result is reused in between. Range 100-1000.")]
		public readonly int TeamLeadAllInScanIntervalTicks = 250;

		[Desc("ALL-IN-01: on an all-in order a member sends its idle units again whenever at least this many wait in its pool (one unit would walk alone).")]
		public readonly int TeamLeadAllInJoinMinUnits = 2;
	}
}
