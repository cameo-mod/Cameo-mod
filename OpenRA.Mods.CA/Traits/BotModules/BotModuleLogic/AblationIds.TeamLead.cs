#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// TEAM-LEAD-01 row (USER P0, 2026-11-23). Owner: Frontrunner.
	// Off = the pre-change behaviour of every bot of the team, byte for byte (the Info switch is the release control, the row is the DEV ablation).
	public static partial class AblationIds
	{
		public const string TeamLead = "tl_team_lead";

		// The joint guerrilla/harass group (all bots of the team hit one shared harass target) has its own row; the Info switch is TeamLeadEnabled.
		public const string TeamLeadJointHarass = "tl_joint_harass";

		// TEAM-LEAD-02 (P0 FIX, 2026-11-24): the leader publishes the push on cadence from its own team check (tl_leader_cadence), and after
		// the late-escalation start the push and harass target is the weakest known enemy seat until it dies (tl_finish_weakest). Info switches:
		// TeamLeadCadenceEnabled, TeamLeadFinishWeakestEnabled.
		public const string TeamLeadCadence = "tl_leader_cadence";
		public const string TeamLeadFinishWeakest = "tl_finish_weakest";

		// TEAM-PINCER-01 (USER P0, 2026-11-24): a team push whose blob holds at least twice the WAVE-MIN tier splits into two prongs that
		// stage on opposite sides of the same target and push together. Info switch: TeamLeadPincerEnabled.
		public const string TeamLeadPincer = "tl_pincer";

		// TEAM-BLOB-01 (USER P0, V02 RCA 2026-11-24): the no-order guarantee (tl_fallback_publish; Info TeamLeadFallbackPublishEnabled) and the order that
		// survives a leader re-election (tl_order_survive; Info TeamLeadOrderSurviveEnabled).
		public const string TeamLeadFallbackPublish = "tl_fallback_publish";
		public const string TeamLeadOrderSurvive = "tl_order_survive";

		// HARASS-CAP-01 (USER P0, V02 RCA 2026-11-24): raiders at most a percent of the seat's combat units (tl_harass_cap; Info HarassCapEnabled), a raid launches
		// with at least HarassMinUnits (tl_harass_min; Info HarassMinEnabled), and from the late start the raiders join the blob and the open order (tl_raider_recall;
		// Info TeamLeadRaiderRecallEnabled).
		public const string TeamLeadHarassCap = "tl_harass_cap";
		public const string TeamLeadHarassMin = "tl_harass_min";
		public const string TeamLeadRaiderRecall = "tl_raider_recall";
	}
}
