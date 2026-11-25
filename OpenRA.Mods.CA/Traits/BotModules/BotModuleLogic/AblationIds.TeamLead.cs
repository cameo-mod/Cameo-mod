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

		// TEAM-BLOB-01 push bar (USER rule 2026-11-24, nothing below WAVE-MIN attacks): a due push waits while the squads at the rally are below the bars (tl_push_bar;
		// Info TeamLeadPushBarEnabled), dissolving the order after TeamLeadPushBarMaxTicks.
		public const string TeamLeadPushBar = "tl_push_bar";

		// HARASS-CAP-01 (USER P0, V02 RCA 2026-11-24): raiders at most a percent of the seat's combat units (tl_harass_cap; Info HarassCapEnabled), a raid launches
		// with at least HarassMinUnits (tl_harass_min; Info HarassMinEnabled), and from the late start the raiders join the blob and the open order (tl_raider_recall;
		// Info TeamLeadRaiderRecallEnabled).
		public const string TeamLeadHarassCap = "tl_harass_cap";
		public const string TeamLeadHarassMin = "tl_harass_min";
		public const string TeamLeadRaiderRecall = "tl_raider_recall";

		// TEAM-GATHER-01 (USER order, V04 RCA 2026-11-24): a gathering squad keeps its order on contact (tl_gather_hold; Info TeamLeadGatherHoldEnabled), a held or
		// extending order is never cancelled for missing reports (tl_grace_hold; Info TeamLeadGraceHoldEnabled), and the liveness (M13) and stalemate-breaker
		// launches join an open team order instead of launching alone (tl_route_all; Info TeamLeadRouteAllEnabled).
		public const string TeamLeadGatherHold = "tl_gather_hold";
		public const string TeamLeadGraceHold = "tl_grace_hold";
		public const string TeamLeadRouteAll = "tl_route_all";

		// TEAM-GATHER-02 (USER order via Opus, AUDIT_SQUAD_01 / AUDIT_CONFIG_01): ally defence leaves the order's squads alone (tl_defend_respects_order), a squad that lost its
		// target retargets only around its fight (gt_bounded_retarget), a formed squad joins at once (tl_join_at_form), the push bar and the publish floor count joinable units
		// (tl_bar_joinable), units merge into the gathering squad (tl_join_merge), a rally order keeps its grace and is not re-published at once (tl_grace_rally), the gather
		// fight uses the weapon range (tl_gather_range), and the late hunt goes through the order (tl_hunt_via_order). Info switches: TeamLead<Key>Enabled.
		public const string TeamLeadDefendRespectsOrder = "tl_defend_respects_order";
		public const string TeamLeadBoundedRetarget = "gt_bounded_retarget";
		public const string TeamLeadJoinAtForm = "tl_join_at_form";
		public const string TeamLeadBarJoinable = "tl_bar_joinable";
		public const string TeamLeadJoinMerge = "tl_join_merge";
		public const string TeamLeadGraceRally = "tl_grace_rally";
		public const string TeamLeadGatherRange = "tl_gather_range";
		public const string TeamLeadHuntViaOrder = "tl_hunt_via_order";

		// TEAM-GATHER-02 c2 (Opus audit FIX): a leader squad below WAVE-MIN issues an order only when the team joinable army reaches WAVE-MIN (tl_issue_floor), the rally lies
		// between the team bases and the target (tl_rally_mid), and a pincer pushes only with two WAVE-MIN prongs gathered (tl_pincer_double).
		public const string TeamLeadIssueFloor = "tl_issue_floor";
		public const string TeamLeadRallyMid = "tl_rally_mid";
		public const string TeamLeadPincerDouble = "tl_pincer_double";
	}
}
