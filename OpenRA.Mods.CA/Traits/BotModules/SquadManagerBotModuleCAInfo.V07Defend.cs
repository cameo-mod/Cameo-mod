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
	// V07 TEAM-DEFEND, unit side (USER master replay 2026-11-24, via Opus). Every value is a named yaml parameter; none is read while the switch is false.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("TEAM-DEFEND (release switch; the DEV row is tl_defend_team). On (needs TeamLeadEnabled and two living team bots): a serious attack on an own or allied actor raises ONE team alarm; every other teammate then sends its pool units, its idle squads and its idle home-guard squad to the attacker. A team blob that already pushed keeps its order unless the ally would die (attack value above its defence). Off: DEFEND-ALLY-01 only, as before.")]
		public readonly bool TeamLeadDefendTeamEnabled = false;

		[Desc("TEAM-DEFEND: the armed enemies seen around the attacked actor must cost at least this much to raise the alarm (a lone scout is not an alarm).")]
		public readonly int TeamLeadDefendTeamMinAttackValue = 600;

		[Desc("TEAM-DEFEND: the alarm stays in force this many ticks after the last sighting; the attacker's side refreshes it while the attack goes on.")]
		public readonly int TeamLeadDefendTeamAlarmTicks = 450;

		[Desc("TEAM-DEFEND: a teammate sends units at most once per this many ticks.")]
		public readonly int TeamLeadDefendTeamSendTicks = 50;

		[Desc("TEAM-DEFEND: a bot looks for a serious attack at most once per this many ticks (the only periodic work of the feature).")]
		public readonly int TeamLeadDefendTeamRaiseTicks = 50;

		[Desc("TEAM-DEFEND: the ally would die when the attack value is above this percent (25-400) of its defence value (armed units and defence structures around it).")]
		public readonly int TeamLeadDefendTeamDiePercent = 100;

		[Desc("TEAM-DEFEND-02 (row tm_defend_two_tier; needs TeamLeadDefendTeamEnabled and DefendAllyEnabled): the two-tier ally defence. T1 (the local 25-cell response) is unchanged. T2: the team alarm is answered by EVERY ally from its pool, idle squads and home guard,",
			"with no radius: the team sends 1.25 x the SEEN attack value minus the defence value already there (TeamDefendMarginPercent), split over the allies by their spare value; a pushed blob keeps its order unless the ally would die; defenders are released after the threat clears.",
			"A damaged base asset (building, harvester, base builder) alarms at any attack size; an unseen attacker raises a blind alarm at the damaged location (TeamDefendBlindValue). Only attackers this player can view count, whatever FoggedScans says. Off in code and classic.")]
		public readonly bool TeamDefendTwoTierEnabled = false;

		[Desc("TEAM-DEFEND-02: the total presence sent to an attacked ally, percent (100-400) of the seen attack value (125 = the Hard outmatch factor); the defence already there counts toward it.")]
		public readonly int TeamDefendMarginPercent = 125;

		[Desc("TEAM-DEFEND-02: the value the team sends to a base asset that is hit by an attacker nobody sees (blind alarm), minus the defence already there. 600 = the old Serious floor, a probe-sized force.")]
		public readonly int TeamDefendBlindValue = 600;
	}
}
