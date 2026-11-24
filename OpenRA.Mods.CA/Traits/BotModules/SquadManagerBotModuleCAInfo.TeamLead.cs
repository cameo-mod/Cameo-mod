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
	// TEAM-LEAD-01 (USER rule, 2026-11-23). Every value is a named yaml parameter; none is read while TeamLeadEnabled is false.
	// It builds on the AggTeam intent and target code, so it also needs AggTeamEnabled.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("TEAM-LEAD-01 master flag (release switch; the DEV row is tl_team_lead). On: each team of bots elects one leader by score; followers launch only on its order and the whole team gathers at one rally point, then pushes one target together. Off = every bot plays as before. Needs AggTeamEnabled.")]
		public readonly bool TeamLeadEnabled = false;

		[Desc("Ticks between team checks (election, order upkeep, published pools). The only periodic work of the feature. Range 100-300.")]
		public readonly int TeamLeadCheckTicks = 100;

		[Desc("A challenger takes the leader post only with this many percent more score than the leader. Range 5-50.")]
		public readonly int TeamLeadLeadPercent = 15;

		[Desc("A new leader keeps the post at least this many ticks (a dead leader is replaced at once). Range 500-6000.")]
		public readonly int TeamLeadHoldTicks = 1500;

		[Desc("The gathering never pushes earlier than this many ticks after the order. Range 100-900.")]
		public readonly int TeamLeadGatherMinTicks = 300;

		[Desc("The gathering pushes at the latest this many ticks after the order, whoever is still on the way. Range 600-3000.")]
		public readonly int TeamLeadGatherMaxTicks = 1500;

		[Desc("Percent of the committed army value that must stand at the rally point for an early push. Range 50-100.")]
		public readonly int TeamLeadArrivePercent = 70;

		[Desc("A unit counts as at the rally point within this many cells of it. Range 4-16.")]
		public readonly int TeamLeadArriveCells = 8;

		[Desc("Cells the rally point lies ahead of the leader's wave toward the target when the army staging has no plan. Range 4-24.")]
		public readonly int TeamLeadRallyForwardCells = 12;

		[Desc("A bot contributes a wave to an order only with at least this many idle units. Range 1-10.")]
		public readonly int TeamLeadJoinMinUnits = 3;

		[Desc("The leader forms the team wave with at least this many idle units of its own (the launch bars measure the whole team's pool). Range 1-10.")]
		public readonly int TeamLeadLeaderMinUnits = 1;

		[Desc("A leader that issued no order for this many ticks (counted from its election, the last order and the first attack tick) hands the post to the seat with the largest idle pool, so a leader that cannot form a wave never stalls the team. Range 1500-9000.")]
		public readonly int TeamLeadStallTicks = 4500;

		[Desc("A follower may still form and send a wave this many ticks after the push. Range 0-1200.")]
		public readonly int TeamLeadLateJoinTicks = 600;

		[Desc("An order that has not pushed after this many ticks is cancelled. Range 1500-6000.")]
		public readonly int TeamLeadOrderMaxTicks = 3000;

		[Desc("The joint guerrilla/harass target of the team is kept this many ticks (or until it dies), then the next squad without a target publishes a new one. Range 600-3000.")]
		public readonly int TeamLeadHarassTicks = 1500;

		[Desc("A published idle-pool count older than this many ticks is ignored in the combined blob. Range 200-600.")]
		public readonly int TeamLeadPoolMaxAgeTicks = 300;

		[Desc("TEAM-LEAD-02 (release switch; the DEV row is tl_leader_cadence). On: the leader publishes the team push from its own team check, no formed leader squad needed, whenever the combined blob (every member's idle army, quiet squads included) reaches the WAVE-MIN tier, at least as often as the old per-bot attack cadence (the MinimumAttackForceDelay reset, late-escalated) and at the latest every TeamLeadFallbackTicks; every member forms its wave for the open order at once, and a push whose target cannot be resolved attack-moves to the order's position instead of standing down. Off = the TEAM-LEAD-01 behaviour. Needs TeamLeadEnabled.")]
		public readonly bool TeamLeadCadenceEnabled = false;

		[Desc("TEAM-LEAD-02: the team has no order for this many ticks with a ready blob = the leader publishes now, whatever its own cadence says. Range 600-3000.")]
		public readonly int TeamLeadFallbackTicks = 1500;

		[Desc("TEAM-LEAD-02 (release switch; the DEV row is tl_finish_weakest). On, from LateEscalationStartTick: the team's push target (and the joint harass target) is the WEAKEST known enemy seat - the fewest known buildings, from what the bot sees and remembers - and it stays until that seat is dead. Off = the nearest known enemy as before. Needs TeamLeadEnabled.")]
		public readonly bool TeamLeadFinishWeakestEnabled = false;

		[Desc("TEAM-LEAD-02: a finish focus held this long without killing the seat (unreachable) rotates to the next weakest seat (and may come back to this one after the next such period). Range 6000-30000.")]
		public readonly int TeamLeadFocusMaxTicks = 12000;

		[Desc("TEAM-PINCER-01 (release switch; the DEV row is tl_pincer). On: when the leader publishes a team push and the combined blob holds at least twice the WAVE-MIN tier (and twice the join floor), the push splits into two prongs with the SAME target. Each prong stages on its own side of the target, the two staging points at least TeamLeadPincerMinSeparationCells apart; the members' squads are shared out lightest-prong-first; the prongs hold at their staging points until both are ready (at most TeamLeadPincerHoldTicks after the first one is), then both push together. A blob that is too small, a target too close, or no two reachable staging points = the single blob as before. Needs TeamLeadEnabled.")]
		public readonly bool TeamLeadPincerEnabled = false;

		[Desc("TEAM-PINCER-01: distance of a prong's staging point from the target, in cells. Never less than AttackScanRadius + 4: a gathering squad fights any enemy inside its scan radius and leaves the order, so a staging point inside it would break the pincer at the target itself.")]
		public readonly int TeamLeadPincerStandOffCells = 24;

		[Desc("TEAM-PINCER-01: angle between the direct line to the target and each prong's staging point as seen from the target, in degrees (the two prongs are twice this apart). Range 45-90.")]
		public readonly int TeamLeadPincerSpreadDegrees = 70;

		[Desc("TEAM-PINCER-01: no pincer when the target is closer than this to the leader's home, in cells (nothing to flank).")]
		public readonly int TeamLeadPincerMinTargetCells = 30;

		[Desc("TEAM-PINCER-01: the two staging points must be at least this far apart, in cells; closer means one blob.")]
		public readonly int TeamLeadPincerMinSeparationCells = 16;

		[Desc("TEAM-PINCER-01: the longest the first ready prong waits for the other at its staging point, in ticks (the AssaultSyncHoldTicks pattern, max 750). Then both push.")]
		public readonly int TeamLeadPincerHoldTicks = 750;

		[Desc("TEAM-PINCER-01: the gather window of a pincer order, in ticks (the way round takes longer than one rally). Keep it below TeamLeadOrderMaxTicks.")]
		public readonly int TeamLeadPincerGatherMaxTicks = 2250;

		[Desc("TEAM-PINCER-01: the lighter prong must carry at least this percent of the committed value when the gather minimum is over; below that the pincer collapses into one blob (both prongs push at once).")]
		public readonly int TeamLeadPincerMinSharePercent = 25;

		[Desc("TEAM-BLOB-01 (release switch; the DEV row is tl_fallback_publish). On: the leader's no-order guarantee. After TeamLeadFallbackTicks without any team order and a blob below the WAVE-MIN bars (the ready path publishes the attack when it is above them) it re-publishes the RALLY order: a gather-only order whose squads walk to the rally point and wait; it becomes the attack order only when the gathered squads pass the WAVE-MIN bars (USER rule: nothing below WAVE-MIN ever attacks). Needs TeamLeadCadenceEnabled.")]
		public readonly bool TeamLeadFallbackPublishEnabled = false;

		[Desc("TEAM-BLOB-01 push bar (release switch; the DEV row is tl_push_bar). On (USER rule: bots never trickle in small groups): a team push that is due (the window or the arrival) is held back while the squads standing at the rally are below the blob launch minimum (the WAVE-MIN unit tier or half of the team's idle army, whichever is more; AND the WAVE-MIN value). Every share goes to the rally and none advances alone; all advance on one tick, a share formed after the push does not follow it (TeamLeadLateJoinTicks is ignored). The order stays at the rally and keeps merging new groups, and is dissolved (squads back to the pool) after TeamLeadPushBarMaxTicks of hold. Off = the push goes with whatever has arrived. Needs TeamLeadEnabled.")]
		public readonly bool TeamLeadPushBarEnabled = false;

		[Desc("TEAM-BLOB-01 push bar: the hard cap of the hold, in ticks after the first held push. Range 1500-6000.")]
		public readonly int TeamLeadPushBarMaxTicks = 3000;

		[Desc("TEAM-GATHER-01 (release switch; the DEV row is tl_gather_hold). On: a squad gathering for an order that has not pushed yet never leaves the order for a fight. It reports to the order the moment it joins; when an enemy stands inside TeamLeadGatherFightCells it stops (its units fire at what is in weapon range), fights it out, then walks on to the rally; a fight is no state change, so its report stays. Off = a contact sends the squad into its own attack and out of the order as before.")]
		public readonly bool TeamLeadGatherHoldEnabled = false;

		[Desc("TEAM-GATHER-01 tl_gather_hold: the distance in cells inside which a gathering squad stops to fight an enemy (about the weapon range of an infantry or tank squad).")]
		public readonly int TeamLeadGatherFightCells = 6;

		[Desc("TEAM-GATHER-01 tl_gather_hold: a fight (a stand-off with an enemy nobody can reach) longer than this many ticks is dropped: the squad walks on to the rally and ignores contacts for the same time.")]
		public readonly int TeamLeadGatherFightMaxTicks = 450;

		[Desc("TEAM-GATHER-01 (release switch; the DEV row is tl_grace_hold). On: the no-report grace never cancels an order whose push is held or extending (the dissolve at TeamLeadPushBarMaxTicks after the first hold stays); each held push also refreshes the last report tick. Off = no_reports can cancel a held order as before.")]
		public readonly bool TeamLeadGraceHoldEnabled = false;

		[Desc("TEAM-GATHER-01 (release switch; the DEV row is tl_route_all). On: while a team order is open, the liveness (M13) urgent attack and the stalemate breaker wave do not launch alone; their units stay in the pool and join the order. Defence and ally defence are not affected. Off = they launch as before (behind the BLOB-LAUNCH-01 attack gate).")]
		public readonly bool TeamLeadRouteAllEnabled = false;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_defend_respects_order). On: ally defence (DEFEND-ALLY) never takes a squad that gathers for the open team order or was formed less than TeamLeadFreshSquadTicks ago (its first team pass has not run yet); it uses free squads and pool units only, inside its radius. Off = any idle or gathering squad can be sent.")]
		public readonly bool TeamLeadDefendRespectsOrderEnabled = false;

		[Desc("TEAM-GATHER-02 tl_defend_respects_order: ticks after its formation during which a squad is left to its first team pass (join or stand down).")]
		public readonly int TeamLeadFreshSquadTicks = 100;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is gt_bounded_retarget). On: a team Rush squad that lost its target looks for the next enemy only inside AttackScanRadius around its leader; with none there it goes back to idle, where the team pass re-joins the open order or stands it down. Off = it marches to the nearest enemy anywhere.")]
		public readonly bool TeamLeadBoundedRetargetEnabled = false;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_join_at_form). On: a wave squad that is formed while a team order is open joins it in the same pass, before ally defence or anything else can take it.")]
		public readonly bool TeamLeadJoinAtFormEnabled = false;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_bar_joinable). On: the team idle army (the push bar, the ready blob and the rally publish floor) counts only joinable units: live, armed, ground (Mobile) units that are not artillery or fire support. Off = every pool unit counts.")]
		public readonly bool TeamLeadBarJoinableEnabled = false;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_join_merge). On: for a rally or held order, joinable pool units of any number merge into the seat's gathering squad (no new one-unit squads; a new squad forms only from TeamLeadJoinMinUnits joinable units), the join counts only when a squad with units was formed, and the leader publishes a rally order only when the team idle army reaches the WAVE-MIN units (BlobLaunch01CA.MinUnits) and value.")]
		public readonly bool TeamLeadJoinMergeEnabled = false;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_grace_rally). On: a rally (gather-only) order is not cancelled for missing reports (it ends by promotion or at TeamLeadOrderMaxTicks), and no order is published within TeamLeadRepublishHoldTicks of a cancel (a leader change excepted).")]
		public readonly bool TeamLeadGraceRallyEnabled = false;

		[Desc("TEAM-GATHER-02 tl_grace_rally: ticks after a cancel during which the leader publishes no new order.")]
		public readonly int TeamLeadRepublishHoldTicks = 1500;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_gather_range). On: a gathering squad stops to fight an enemy inside its own maximum weapon range (at least TeamLeadGatherFightCells, at most AttackScanRadius) instead of the fixed TeamLeadGatherFightCells.")]
		public readonly bool TeamLeadGatherRangeEnabled = false;

		[Desc("TEAM-GATHER-02 (release switch; the DEV row is tl_hunt_via_order). On: from the late start a team Rush squad does not hunt its own grid cell; with nothing known the leader's order target is one hunt cell for the whole blob.")]
		public readonly bool TeamLeadHuntViaOrderEnabled = false;

		[Desc("TEAM-GATHER-02 c2 (release switch; the DEV row is tl_issue_floor). On: a leader squad below the WAVE-MIN bars issues a team order only when the team joinable army (pool plus quiet wave squads of every seat) reaches WAVE-MIN units and value.")]
		public readonly bool TeamLeadIssueFloorEnabled = false;

		[Desc("TEAM-GATHER-02 c2 (release switch; the DEV row is tl_rally_mid). On: the rally of a new order lies TeamLeadRallyMidPercent of the way from the centroid of the allied bases to the target (on the team side), so followers do not walk to the leader staging cell. Off = the leader staging cell.")]
		public readonly bool TeamLeadRallyMidEnabled = false;

		[Desc("TEAM-GATHER-02 c2 tl_rally_mid: how far from the allied base centroid toward the target the rally lies, in percent.")]
		public readonly int TeamLeadRallyMidPercent = 35;

		[Desc("TEAM-GATHER-02 c3 tl_rally_mid: the army staging cell is used for the rally only when it lies within this many cells of the mid point; it is the leader own staging cell and must not pull the rally home.")]
		public readonly int TeamLeadRallyMidStagingCells = 10;

		[Desc("TEAM-GATHER-02 c2 (release switch; the DEV row is tl_pincer_double). On: a pincer pushes only when the gathered blob is at least twice the WAVE-MIN units and value (each prong meets WAVE-MIN); a thinner one collapses into the single blob.")]
		public readonly bool TeamLeadPincerDoubleEnabled = false;

		[Desc("TEAM-BLOB-01 (release switch; the DEV row is tl_order_survive). On: when the elected leader changes while an order lives, the new leader takes the order over instead of cancelling it (the squads gathering for it are not thrown back to the pool). Off = the order is cancelled on a leader change as before.")]
		public readonly bool TeamLeadOrderSurviveEnabled = false;
	}
}
