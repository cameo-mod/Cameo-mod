#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using BotRng = OpenRA.Mods.CA.BotRng;
using OpenRA.Mods.Common;
using AIUtils = OpenRA.Mods.Common.AIUtils;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// Merged from CA CaptureManagerBotModuleCA (Cameo's copy of CAmod's, itself from OpenRA.Mods.AS) and AS
	// CncEngineerManagerBotModule (engine d5d8b2a685) under DESIGN §19.3 (one module per decision). AI_MASTER_PLAN ENG.
	//   from CA: capture — a priority target by chance (nearest to the base first), otherwise the most valuable
	//            capturable of a random enemy/neutral player, nearest per engineer; SafePath was meant to route the
	//            engineer around enemy fire but never did (its goal predicate matched the start cell and the path was
	//            discarded) — SafeRoute below does it for real when MaxExposedRouteCells >= 0.
	//   from AS: bridge-hut repair and instant building repair (nearest path-reachable target, one engineer per
	//            evaluation, the repair jobs taking turns); the stuck check (moving, not moved since the last sample).
	//            DESIGN §19.5 lets the whole module see through fog (maintainer 2026-09-30); both visibility switches
	//            remain, so a fog-honest engineer owner is one yaml line each.
	//   new:     the ONE owner of every engineer (review P0b, "IsIdle is not ownership"): an LC1 claim before every
	//            order, renewed while the engineer works and released when it is idle again; its own in-flight set,
	//            so the capture and repair passes never order one engineer twice in a tick (the lease lets the same
	//            owner re-claim, so it cannot catch that); a stuck engineer is stopped, released and retried after
	//            StuckRetryTicks (the AS module benched it for the rest of the match); an optional visibility check
	//            for bridge huts (the AS module had none).
	//   sync:    ONE-WAY — upstream CA/AS edits land in the parents via re-vendor/engine-pin bumps and
	//            audit_merged_bot_modules fails until ported here. Fixes made here NEVER go back into the
	//            vendored parents: classic runs the CA copy verbatim, so classic-only capture bugs are
	//            part of the frozen baseline by design.
	[TraitLocation(SystemActors.Player)]
	[Desc("The ONE owner of the bot's engineers (DESIGN §19.3): replaces CaptureManagerBotModuleCA + CncEngineerBotModule,",
		"which ordered the same idle engineers side by side. classic keeps running the CA copy alone.")]
	public class EngineerBotModuleInfo : ConditionalTraitInfo
	{
		// The three id lists below: empty = read from CaptureManagerBotModuleCA on the same Player actor (classic's copy,
		// defined whatever its condition), so the ids live in ai.yaml once (audit_central_ids is lower-only).
		[Desc("Actor types that capture (via `Captures`). Empty: CaptureManagerBotModuleCA's list on this actor.")]
		public readonly HashSet<string> CapturingActorTypes = [];

		[Desc("Percentage chance of trying a priority capture.")]
		public readonly int PriorityCaptureChance = 75;

		[Desc("Actor types captured first, nearest to the base first. Empty: CaptureManagerBotModuleCA's list.")]
		public readonly HashSet<string> PriorityCapturableActorTypes = [];

		[Desc("Actor types that may be captured; empty on both modules includes all actors. Empty: CaptureManagerBotModuleCA's list.")]
		public readonly HashSet<string> CapturableActorTypes = [];

		[Desc("Avoid enemy actors this close to the path when routing a capturer. Near the maximum weapon range.")]
		public readonly WDist EnemyAvoidanceRadius = WDist.FromCells(8);

		[Desc("Route engineers for real (maintainer 2026-09-30: if three engineers die at one target, the path was not",
			"safe enough). The parents' SafePath never scored a route: its goal predicate matched the engineer's own cell,",
			"and the order ignored the result, so engineers walked the default shortest path. With this >= 0 the module",
			"searches the least-exposed path (cost: nearness of ARMED enemies within EnemyAvoidanceRadius), skips a target",
			"whose safest path still has more than this many exposed cells outside the final approach, and walks the",
			"engineer along it through waypoints. -1 = the parents' behaviour, the default until its A/B.")]
		public readonly int MaxExposedRouteCells = -1;

		[Desc("Re-check a routed capture while the engineer walks (v2): every this many ticks the REMAINING route cells (ahead",
			"of the engineer, outside ApproachCells) are scored; if more than MaxExposedRouteCells are exposed a fresh safe route",
			"from the engineer's cell is issued, and if none passes the engineer is pulled back to the base and the attempt is",
			"released (outmatched). Needs MaxExposedRouteCells >= 0; runs on its own countdown in BotTick (independent of",
			"MinimumCaptureDelay). 0 = off, the default.")]
		public readonly int RouteRecheckTicks = 0;

		[Desc("ENG-ROUTE-01 (V08 RCA 8c37f899: one SafeRoute visit cost 2549 ms): bound the safe-route search. Each SafeRoute call may spend at most SafeRouteExpansionBudget distinct cells whose path cost is evaluated",
			"(roughly the map area the search covers); on overrun the search is abandoned and the route counts as unsafe. At most SafeRouteCallsPerTick calls run per world tick across the capture ranking, the",
			"recheck and the route gate (a deferred call is unsafe for this visit; the next visit tries again), and the danger scan is cached for DangerCacheTicks ticks instead of one.",
			"Needs MaxExposedRouteCells >= 0. Gated by ablation row er_route_budget. false = the unbounded search.")]
		public readonly bool SafeRouteBudget = false;

		[Desc("ENG-ROUTE-01: distinct cells whose path cost one SafeRoute call may evaluate (0 = unlimited).")]
		public readonly int SafeRouteExpansionBudget = 2000;

		[Desc("ENG-ROUTE-01: SafeRoute searches per world tick (0 = unlimited).")]
		public readonly int SafeRouteCallsPerTick = 3;

		[Desc("ENG-ROUTE-01: ticks a cell's danger value is reused (1 = recomputed every tick).")]
		public readonly int DangerCacheTicks = 8;

		[Desc("Stealth means unguarded (v2): a target that is not escort-eligible (an enemy-base building) is skipped when",
			"more than this many armed enemy actors stand within EnemyAvoidanceRadius of it. -1 = off, the default.")]
		public readonly int StealthMaxDefenders = -1;

		[Desc("Rank capture candidates by safety and value (v2) instead of pure distance: score = target value / (1 + distance",
			"in cells + ExposureWeight x exposed route cells); only the nearest CaptureTargetTries x 2 are scored. false = off.")]
		public readonly bool RankTargetsBySafety = false;

		[Desc("Cost of one exposed route cell, in cells of distance, for RankTargetsBySafety.")]
		public readonly int ExposureWeight = 10;

		[Desc("Cells around the target excluded from the exposure count: the final approach to a defended target is the",
			"escort's question (EscortDefendedCaptures), not the route's.")]
		public readonly int ApproachCells = 6;

		[Desc("Cells between the waypoints of a safe route (MaxExposedRouteCells >= 0).")]
		public readonly int RouteWaypointSpacing = 5;

		[Desc("Targets tried per engineer, nearest first, before it waits for the next evaluation. A target that is full",
			"(MaxEngineersPerTarget), dormant or unsafe is passed over so the engineer goes to a DIFFERENT target. 1 = the",
			"parents' single nearest target, the default until its A/B.")]
		public readonly int CaptureTargetTries = 1;

		[Desc("Ticks between capture evaluations.")]
		public readonly int MinimumCaptureDelay = 375;

		[Desc("Most capture targets considered per evaluation (at least 1).")]
		public readonly int MaximumCaptureTargetOptions = 10;

		[Desc("Consider visibility (shroud, fog, cloak) when choosing capture targets. DESIGN §19.5 allows false for this module.")]
		public readonly bool CheckCaptureTargetsForVisibility = true;

		[Desc("FOG-ENGCRATE-01: generic-bot capture and bridge-hut targets are those seen now or remembered in the frozen layer, whatever the Check*ForVisibility flags say. Row fog_engcrate.")]
		public readonly bool FogEngCrate = false;

		[Desc("Player relationships whose actors capturers target.")]
		public readonly PlayerRelationship CapturableRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("FB2 (Frans SpecOps demand-capturers): with zero live capturers the capture pipeline never",
			"even enumerates targets, so the first specialist only ever appears by unit-mix luck. On each",
			"capture evaluation, count the safe capturable targets (not full, not dormant, not escort-blocked);",
			"while live capturers < min(MaximumDemandCapturers, safe/DemandTargetsPerCapturer), request one",
			"more capturer type from the unit builders. The route/escort checks still gate the dispatch itself.")]
		public readonly bool UseDemandCapturers = false;

		[Desc("FB2 demand-capturers: never keep more live capturers than this.")]
		public readonly int MaximumDemandCapturers = 1;

		[Desc("FB2 demand-capturers: how many safe targets justify one specialist (Frans DemandTargetsPerCapturer).")]
		public readonly int DemandTargetsPerCapturer = 1;

		[Desc("Actor types that repair: bridge huts (via `RepairsBridges`) and buildings (via `InstantlyRepairs`).")]
		public readonly HashSet<string> RepairingActorTypes = [];

		[Desc("Allied building types an engineer repairs instantly (via `InstantlyRepairable`). Empty disables it.")]
		public readonly HashSet<string> RepairableActorTypes = [];

		[Desc("Repair an allied building at this damage state or worse.")]
		public readonly DamageState RepairableDamageState = DamageState.Heavy;

		[Desc("Bridge hut types an engineer repairs once their bridge is down. Empty disables it.")]
		public readonly HashSet<string> RepairableHutActorTypes = [];

		[Desc("Ticks between repair evaluations; also the stuck-check sample interval.")]
		public readonly int AssignRoleDelay = 120;

		[Desc("Only repair bridge huts the player can see. DESIGN §19.5 allows false for this module.")]
		public readonly bool CheckRepairTargetsForVisibility = true;

		[Desc("LC1 lease length in ticks, renewed at every evaluation while the engineer works.")]
		public readonly int LeaseTicks = 3000;

		[Desc("Ticks after an order before an idle engineer counts as done (orders reach the unit a few ticks late).")]
		public readonly int OrderGraceTicks = 50;

		[Desc("Ticks a stuck engineer is left alone before it may be ordered again.")]
		public readonly int StuckRetryTicks = 1500;

		[Desc("Most engineers sent at one capture target at a time (one mission, one live attempt). 0 = no limit,",
			"the parents' behaviour and the default until its A/B: a smoke match sent three engineers at one oil derrick",
			"within 400 ticks; the candidate sets 1 (AI_MASTER_PLAN §1.2 step 6).")]
		public readonly int MaxEngineersPerTarget = 0;

		[Desc("A capture mission whose attempts fail this many times in a row (the engineer died) goes dormant: its target",
			"is skipped for CaptureDormantTicks (fransotto's dormant shelf; AI_MISSION_CARDS §2.2). 0 disables. A smoke match",
			"lost five engineers one after another at one defended derrick. Off until its A/B; the candidate sets 2.")]
		public readonly int CaptureFailuresBeforeDormant = 0;

		[Desc("Ticks a dormant capture mission rests before its target may be tried again.")]
		public readonly int CaptureDormantTicks = 3000;

		[Desc("S1 (FINDINGS_2026-10-12 §3): how many dormant episodes may stack on one mission — each shelve",
			"costs the next episode one more CaptureDormantTicks (episode N rests N ticks of shelf), so a target",
			"that keeps killing or repelling engineers backs off instead of churning on the flat shelf. 1 = flat;",
			"caps the multiplier, never the date — a far-future shelf still expires.")]
		public readonly int CaptureDormantEscalationMax = 1;

		[Desc("Consecutive released/outmatched retreats on the same capture mission that shelve it for",
			"CaptureDormantTicks. The death streak (CaptureFailuresBeforeDormant) is unchanged — a voluntary",
			"retreat is still not a failure — but a target whose route stays hot through every recheck produces",
			"commit-retreat churn the shelf otherwise never sees (armed 2v2 smoke: one defended airstrip committed",
			"4x by the same bot across ~28k ticks). Higher than the death threshold on purpose. 0 disables.")]
		public readonly int CaptureRetreatsBeforeDormant = 0;

		[Desc("Escort as ONE mission (maintainer 2026-09-30): a TECH building (neutral, or a PriorityCapturableActorTypes",
			"entry) defended by enemy armed units (within EnemyAvoidanceRadius) is not attempted solo; a building in the",
			"enemy base is never escorted — the engineer sneaks in alone (SafePath), an escort would give it away. The mission is PUBLISHED and a protection request is raised at the",
			"target (IBotProtectionRequestProvider, the squad manager's escort seam — it needs UseProtectionRequests); the",
			"engineer goes once our armed value there reaches EscortSuperiority percent of the defenders'. Off until its A/B.")]
		public readonly bool EscortDefendedCaptures = false;

		[Desc("Percent of the defenders' value our armed units near the target must reach before the engineer goes.")]
		public readonly int EscortSuperiority = 100;

		[Desc("The engineer also waits until the defenders' value near the target has fallen to this percent of their value",
			"when the mission was published: the escort must have thinned them out, not merely arrived (#693's first A/B",
			"smoke: the escort arrived, the engineer went into the firefight and died twice). 100 = no thinning required.")]
		public readonly int EscortThinnedPercent = 100;

		[Desc("Ticks an escorted capture waits for its escort before the mission is DENIED (no_units) and goes dormant.")]
		public readonly int EscortWaitTicks = 3000;

		[Desc("Ticks a published protection request stays valid; it is re-published while the mission lives.")]
		public readonly int EscortRequestTicks = 250;

		[Desc("ENG-T (maintainer 2026-09-30): percent of stealth infiltrations (a building in the enemy base) that ask an",
			"IBotCaptureTransportProvider to carry the engineer in (APC, transport helicopter) along a route around the enemy;",
			"the rest go on foot. 0 = never (and no random number is drawn). The A/B candidate sets 25.")]
		public readonly int TransportChance = 0;

		[Desc("ENG-T: most engineers (and buildings) in one infiltration run — one per stop.")]
		public readonly int TransportRunMax = 5;

		[Desc("ENGDROP (ENGDROP_DESIGN_01): drive transport drops from the two-trigger scheduler instead of the per-candidate",
			"TransportChance roll. Trigger A (every 25 ticks) launches at once when a visible, unguarded building, a free crew and",
			"an EXISTING craft are at hand and never queues a transport. Trigger B draws one per-bot number per due visit.",
			"With the ed_engdrop ablation off (DEV only) the legacy TransportChance roll runs instead.")]
		public readonly bool UseDropScheduler = false;

		[Desc("ENGDROP Trigger B: percent chance per due visit, easiest .. hardest tier (50 + 5 r, r = 0 noob .. 10 cameogod).")]
		public readonly int DropChancePctEasy = 55;
		public readonly int DropChancePctHard = 100;

		[Desc("ENGDROP Trigger B: ticks from one due visit to the next, easiest .. hardest tier (525 - 30 r).")]
		public readonly int DropIntervalTicksEasy = 495;
		public readonly int DropIntervalTicksHard = 225;

		[Desc("ENGDROP Trigger B: ticks from match start to the first due visit, easiest .. hardest tier (675 - 60 r).")]
		public readonly int DropInitialDelayTicksEasy = 615;
		public readonly int DropInitialDelayTicksHard = 75;

		[Desc("ENGDROP Trigger A: how old (world ticks) the newest visibility stamp of every sampled cell of the target area, drop route and landing",
			"may be for the area to count as KNOWN. Unknown or stale coverage is never undefended. The same on every tier.")]
		public readonly int DropFreshTicks = 2500;

		[Desc("ENGDROP: ticks between re-checks of an in-flight Trigger A run against the owner's memory (a contradicted run is aborted once).")]
		public readonly int DropRiskRecheckTicks = 125;

		[Desc("ENGDROP: margin in cells added to a remembered threat's reach when it becomes a hard exclusion disc for the run.")]
		public readonly int DropExclusionMarginCells = 2;

		[Desc("ENGDROP Trigger A: most cells sampled, evenly, along the route the provider plans for the craft (the area is unknown when the",
			"target area, route and landing samples do not fit the 256-cell table).")]
		public readonly int DropRouteSamples = 64;

		[Desc("ENGDROP Trigger A: a target whose route was planned (or refused) is not planned again for this many ticks (the plan runs a path finder).")]
		public readonly int DropPlanRetryTicks = 100;

		[Desc("TC-2e (AI_ARCHITECTURE §12.17): yield a capture target an outranking allied bot already claims",
			"(published on the team blackboard — lower ClientIndex wins), and stand an in-flight capture down",
			"when an outranking ally's claim lands on its cell. Inert in 1v1 — no allied broadcasts exist.")]
		public readonly bool UseTeamCaptureClaims = false;

		[Desc("BF-2 (AI_ARCHITECTURE §12.26): prefer capture targets in the caller's deterministic",
			"shard (hash of the target cell mod team size) — two allies picking the same building",
			"inside one snapshot interval can't be arbitrated by an unpublished claim. Orders, never",
			"filters: out-of-shard targets stay eligible once the own tier is exhausted.")]
		public readonly bool PreferShardCaptureTargets = false;

		public override object Create(ActorInitializer init) { return new EngineerBotModule(init.Self, this); }
	}

	public enum EngineerJob { Capture, RepairBridge, RepairBuilding }

	public enum EngineerCheck { Working, Done, Stuck, Gone }

	public class EngineerBotModule : ConditionalTrait<EngineerBotModuleInfo>, IBotTick, IBotPositionsUpdated, IGameSaveTraitData,
		IBotProtectionRequestProvider, IBotCaptureClaimSource, IBotCapturerTargetAllowance
	{
		const string LeaseOwner = nameof(EngineerBotModule);

		sealed class Assignment
		{
			public EngineerJob Job;
			public int OrderedTick;
			public int SampleTick;
			public WPos SamplePos;
			public Actor Target;
			public string MissionId;
			public int Attempt;
			public bool ViaTransport;
			public string RunId; // ENGDROP: the provider's run identity (RequestAbort)
			public bool Opportunity; // ENGDROP: launched by Trigger A
			public IReadOnlyList<CPos> RouteSamples; // ENGDROP: the sampled planned route Trigger A vetted (re-checked against memory)
			public bool AbortRequested;
			public List<CPos> Route;
		}

		readonly World world;
		readonly OpenRA.Player player;
		AblationLatch ablateFogEngCrate; // FOG-ENGCRATE-01 hook fog_engcrate (off = the omniscient scans)
		AblationLatch ablateDemandCapturers; // ABLATE-01 hook pi_demand_capturers (DEV provider only; release = on)
		AblationLatch ablateEngDrop; // ABLATE-01 hook ed_engdrop (DEV provider only; release = on): off = the legacy TransportChance roll
		AblationLatch ablateRouteBudget; // ABLATE-01 hook er_route_budget (ENG-ROUTE-01; DEV provider only; release = on): off = the unbounded SafeRoute search

		// ENG-ROUTE-01: the bounded safe-route search. routeCalls counts the searches started in world tick routeCallsTick.
		bool RouteBudgetOn => Info.SafeRouteBudget && Info.MaxExposedRouteCells >= 0 && !ablateRouteBudget.Off(player, AblationIds.EngRouteBudget);
		int routeCallsTick = -1;

		/// <summary>ENG-ROUTE-01: the last SafeRoute call returned null because of the budget (call cap, overrun memory or overrun), not because the route is unsafe or missing; callers that would punish a null must not.</summary>
		bool RouteRefused;
		int routeCalls;

		/// <summary>ENG-ROUTE-01: SafeRoute searches abandoned at the expansion budget / refused at the per-tick call cap.</summary>
		public int RouteBudgetOverruns { get; private set; }
		public int RouteCallsDeferred { get; private set; }

		sealed class RouteBudgetExceeded : Exception
		{
		}

		// ENG-ROUTE-01: the throw is caught inside SafeRoute and never leaves it (no exception reaches the logs). A pair that overran is remembered, so a stuck target costs one throw per memory window.
		const int RouteOverrunMemoryTicks = 300;
		readonly Dictionary<(uint Capturer, uint Target), int> routeOverrunUntil = [];
		readonly int maximumCaptureTargetOptions;
		readonly EngineerJob[] repairJobs;
		readonly HashSet<string> capturingTypes;
		readonly HashSet<string> priorityCapturableTypes;
		readonly HashSet<string> capturableTypes;
		readonly Dictionary<Actor, Assignment> assigned = [];
		readonly Dictionary<Actor, int> stuckUntil = [];
		readonly IBotRequestUnitProduction[] unitBuilders;
		readonly Dictionary<string, int> missionAttempts = [];
		readonly Dictionary<string, int> missionFailStreak = [];
		readonly Dictionary<string, int> missionRetreatStreak = [];
		readonly Dictionary<string, int> dormantUntil = [];
		readonly Dictionary<string, int> dormantEpisodes = [];

		// The one escorted capture in progress: the mission waits (no attempt) until the escort holds the target area.
		sealed class EscortPlan
		{
			public BotClaimEpisode Episode;
			public Actor Target;
			public string MissionId;
			public int SinceTick;
			public int DefenceValue;
			public int InitialDefenceValue;
			public bool Committed;
		}

		EscortPlan escort;
		readonly BotClaimEpisodeCounter escortEpisodes;

		// The squad manager serves a protection request only at or above its PrepositionMinThreatValue: a request valued at
		// a few riflemen (the first flag-on match published 440-1000) is dropped in silence. Read the bar from the squad
		// managers on this player (Info-level, fixed for the match) instead of copying the number.
		int escortRequestFloor = -1;

		int EscortRequestFloor() =>
			escortRequestFloor >= 0 ? escortRequestFloor
				: escortRequestFloor = player.PlayerActor.Info.TraitInfos<SquadManagerBotModuleCAInfo>()
					.Select(i => i.PrepositionMinThreatValue).DefaultIfEmpty(0).Max();

		/// <summary>The request value, free of world state so it can be tested: the defenders' value, never below the bar
		/// the squad manager serves requests at.</summary>
		public static int EscortRequestValue(int defenceValue, int floor) => Math.Max(Math.Max(1, defenceValue), floor);

		BotPeriod captureTicks; // STAGGER-06 F1

		// ENGDROP: the caller-persisted deadlines (IssueTraitData/ResolveTraitData). Start is match start + the tier's initial delay.
		DropScheduleState dropState;
		bool dropStarted;
		DropScheduler? dropScheduler;

		/// <summary>ENGDROP: runs launched by Trigger A (opportunity) and Trigger B (regular), for the DEV summary and tests.</summary>
		public int OpportunityDrops { get; private set; }
		public int OpportunityAborts { get; private set; }

		// ENGDROP Trigger A's evidence: scouted memory only (visibility stamps + the fogged memory of enemy defences, army and anti-air).
		EngineerAreaIntel areaIntel;
		readonly List<AreaThreat> areaThreats = [];
		int areaThreatTick = -1;
		IBotRememberedDefenceProvider[] defenceProviders;
		BotCooldown dropRecheck;
		public int RegularDrops { get; private set; }

#if CAMEO_DEV_LOGGING
		// ENGDROP-LOG-01 (DEV only; observer: no RNG, no order, no world write). Scans = visits that ran Trigger A's scan, tries = targets
		// Trigger A / B actually tried this game; the summary line is cumulative, so the last one in the log is the game-end summary.
		int dropScans;
		int opportunityTries;
		int regularTries;
		int dropSummaryTick = -1;
		string dropSummaryLast = "";

		void LogDropSummary(int tick, bool force)
		{
			if (!force && dropSummaryTick >= 0 && tick - dropSummaryTick < 750)
				return;

			var line = $"A={OpportunityDrops} B={RegularDrops} aborts={OpportunityAborts} scans={dropScans} tries_A={opportunityTries} tries_B={regularTries}";
			if (line == dropSummaryLast)
				return;

			dropSummaryTick = tick;
			dropSummaryLast = line;
			Log.Write("debug", $"AI ({player.ClientIndex}): ENGDROP summary {line} (tick {tick})");
		}
#endif

		DropScheduler DropTiers => dropScheduler ??= new DropScheduler(
			BotDifficultyLadder.Scale(Info.DropChancePctEasy, Info.DropChancePctHard, player, Info.DropChancePctHard),
			BotDifficultyLadder.Scale(Info.DropIntervalTicksEasy, Info.DropIntervalTicksHard, player, Info.DropIntervalTicksHard),
			BotDifficultyLadder.Scale(Info.DropInitialDelayTicksEasy, Info.DropInitialDelayTicksHard, player, Info.DropInitialDelayTicksHard));

		/// <summary>The scheduler owns transport drops; false = the legacy roll (the DEV ed_engdrop Disabled arm, or a module without the flag).</summary>
		bool UsesDropScheduler => Info.UseDropScheduler && !ablateEngDrop.Off(player, AblationIds.EngDrop);
		BotPeriod routeRecheck; // STAGGER-06 E070: call-counted F1 K=RouteRecheckTicks x0=0; frozen across a disabled gap (lastSeen), not reset on enable
		BotCooldown repairCooldown;
		int nextRepairJob;
		int housekeptTick = -1;
		CPos initialBaseCenter;

		// Telemetry (debug.log). SkippedClaimed counts evaluations that left an idle engineer alone because another
		// module holds it — each is a chance for the order pair that used to collide.
		public int CaptureOrders { get; private set; }
		public int BridgeOrders { get; private set; }
		public int BuildingRepairOrders { get; private set; }
		public int StuckStops { get; private set; }
		public int SkippedClaimed { get; private set; }

		/// <summary>Capture targets passed over because even the safest route was too exposed.</summary>
		public int UnsafeRoutes { get; private set; }

		/// <summary>Routed captures whose remaining route turned exposed and were given a fresh safe route.</summary>
		public int Reroutes { get; private set; }

		/// <summary>Routed captures pulled back to the base because no safe route remained.</summary>
		public int Pullbacks { get; private set; }

		/// <summary>Stealth targets passed over because more than StealthMaxDefenders armed enemies guard them.</summary>
		public int GuardedSkips { get; private set; }

		/// <summary>Capture attempts stood down to an outranking ally's published claim, plus denied escort plans (TC-2e).</summary>
		public int SupersededCaptures { get; private set; }

		// TC-2e (AI_ARCHITECTURE §12.17): the live capture missions' target cell centres — repair and
		// bridge jobs are not claims. A waiting escort plan counts: it already holds its target.
		public IReadOnlyList<WPos> CaptureClaimPositions
		{
			get
			{
				var positions = new List<WPos>();
				foreach (var job in assigned.Values)
					if (job.Job == EngineerJob.Capture && job.Target != null)
						positions.Add(world.Map.CenterOfCell(job.Target.Location));

				if (escort?.Target != null)
					positions.Add(world.Map.CenterOfCell(escort.Target.Location));

				return positions;
			}
		}

		// ENGINEER-CAP-10PCT (maintainer 2026-10-12): +1 per KNOWN capturable target this bot has
		// fog-fairly seen and not captured — neutral tech buildings plus priority-listed enemy
		// structures. Reported raw; the unit builder clamps it into the capturer cap. §19.5's fog
		// exception does NOT apply here: the allowance only counts honestly-visible targets.
		int capturerAllowanceTick = -1, capturerAllowanceCached;

		public int CapturerTargetAllowance
		{
			get
			{
				if (IsTraitDisabled)
					return 0;

				if (capturerAllowanceTick == world.WorldTick)
					return capturerAllowanceCached;

				capturerAllowanceTick = world.WorldTick;
				var count = 0;
				foreach (var a in world.Actors)
				{
					if (a.IsDead || !a.IsInWorld || a.Owner == player)
						continue;

					if (a.TraitOrDefault<CaptureManager>() == null || !a.CanBeViewedByPlayer(player))
						continue;

					var name = a.Info.Name.ToLowerInvariant();
					var relationship = player.RelationshipWith(a.Owner);
					if (!CountsTowardCapturerAllowance(
							relationship == PlayerRelationship.Neutral,
							relationship == PlayerRelationship.Enemy && priorityCapturableTypes.Contains(name),
							capturableTypes.Count == 0 || capturableTypes.Contains(name)))
						continue;

					count++;
				}

				return capturerAllowanceCached = count;
			}
		}

		/// <summary>Pure seam: a target feeds the engineer allowance only as a neutral tech building
		/// or a priority-listed enemy structure, inside the capturable whitelist when one exists.</summary>
		public static bool CountsTowardCapturerAllowance(bool neutralOwned, bool enemyPriorityListed, bool capturableWhitelistOk)
			=> capturableWhitelistOk && (neutralOwned || enemyPriorityListed);

		public EngineerBotModule(Actor self, EngineerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			routeRecheck = BotPeriod.F1(world.WorldTick, 0, Info.RouteRecheckTicks);
			escortEpisodes = new BotClaimEpisodeCounter(player.InternalName, nameof(EngineerBotModule),
				self.Info.TraitInfos<EngineerBotModuleInfo>().ToList().IndexOf(info));
			maximumCaptureTargetOptions = Math.Max(1, info.MaximumCaptureTargetOptions);

			var shared = self.Info.TraitInfos<CaptureManagerBotModuleCAInfo>().FirstOrDefault();
			capturingTypes = info.CapturingActorTypes.Count > 0 || shared == null ? info.CapturingActorTypes : shared.CapturingActorTypes;
			priorityCapturableTypes = info.PriorityCapturableActorTypes.Count > 0 || shared == null ? info.PriorityCapturableActorTypes : shared.PriorityCapturableActorTypes;
			capturableTypes = info.CapturableActorTypes.Count > 0 || shared == null ? info.CapturableActorTypes : shared.CapturableActorTypes;

			var jobs = new List<EngineerJob>();

			// One owner per decision (architecture audit): when the dedicated CN bridge module is
			// enabled on this actor (cn3_bridge_repair), it owns hut repair — Engineer keeps the job
			// only as its fallback. TraitsImplementing is safe here; unitBuilders below does the same.
			if (info.RepairableHutActorTypes.Count > 0
				&& !self.TraitsImplementing<BridgeRepairBotModule>().Any(t => !t.IsTraitDisabled))
				jobs.Add(EngineerJob.RepairBridge);
			if (info.RepairableActorTypes.Count > 0)
				jobs.Add(EngineerJob.RepairBuilding);
			repairJobs = jobs.ToArray();

			unitBuilders = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating on the same tick (both parents do this).
			captureTicks = BotPeriod.F1(player.World.WorldTick, BotRng.For(player, nameof(EngineerBotModule)).Next(Info.MinimumCaptureDelay), Info.MinimumCaptureDelay);
			var repairDelay = BotRng.For(player, nameof(EngineerBotModule)).Next(Info.AssignRoleDelay);
			repairCooldown = new BotCooldown { Due = player.World.WorldTick + Math.Max(1, repairDelay) };

			// ENGDROP: the schedule starts once (a re-enable keeps its deadlines, like routeRecheck). No random number here:
			// Trigger B's draw happens only at a due visit.
			if (!dropStarted && Info.UseDropScheduler)
			{
				dropState = DropTiers.Start(player.World.WorldTick);
				dropStarted = true;
			}
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation) { initialBaseCenter = newLocation; }

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		/// <summary>What happened to an engineer this module ordered. Free of world state so it can be tested.</summary>
		public static EngineerCheck Check(bool gone, bool idle, bool moving, bool movedSinceSample,
			int ticksSinceOrder, int ticksSinceSample, int graceTicks, int sampleTicks)
		{
			if (gone)
				return EngineerCheck.Gone;

			// A queued order reaches the unit a few ticks late: an engineer idle inside the grace is still on its way.
			if (idle)
				return ticksSinceOrder >= graceTicks ? EngineerCheck.Done : EngineerCheck.Working;

			// The AS stuck rule: moving, yet at the same spot a whole sample interval later.
			if (moving && !movedSinceSample && ticksSinceSample >= sampleTicks && ticksSinceOrder >= graceTicks)
				return EngineerCheck.Stuck;

			return EngineerCheck.Working;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (player.WinState != WinState.Undefined)
				return;

			var visitTick = BotVisitClock.Of(bot).VisitTick;
			if (repairCooldown.Ready(visitTick))
			{
				repairCooldown.Start(visitTick, Info.AssignRoleDelay);
				if (repairJobs.Length > 0)
				{
					Housekeep(bot);
					var job = repairJobs[nextRepairJob];
					nextRepairJob = (nextRepairJob + 1) % repairJobs.Length;
					if (job == EngineerJob.RepairBridge)
						QueueRepairBridgeOrder(bot);
					else
						QueueRepairBuildingOrder(bot);
				}
			}

			if (Info.RouteRecheckTicks > 0 && Info.MaxExposedRouteCells >= 0 && routeRecheck.TryFire(BotVisitClock.Of(bot)))
			{
				var tick = world.WorldTick;
				var leases = BotUnitLeases.Of(player);
				foreach (var (a, job) in assigned.ToList())
					if (job.Route != null && !job.ViaTransport && job.Job == EngineerJob.Capture && !IsGone(a))
						Recheck(bot, leases, a, job, tick);
			}

			// ENGDROP: before the ordinary capture planning, so a drop takes its crew first (A commits before B).
			if (dropStarted && UsesDropScheduler)
				RunDropScheduler(bot, visitTick);

			if (captureTicks.TryFire(BotVisitClock.Of(bot)))
			{
				Housekeep(bot);
				QueueCaptureOrders(bot);
			}
		}

		/// <summary>
		/// ENGDROP: one bot visit. The scheduler says which triggers are due; late visits coalesce (no catch-up draws). A due B
		/// visit consumes exactly one draw from this module's bot stream and reloads from this visit whether it succeeds, is
		/// refused or finds nothing; if A launched a run in the same visit the draw is still consumed but B does not run.
		/// </summary>
		void RunDropScheduler(IBot bot, int visitTick)
		{
			if (dropRecheck.Ready(visitTick))
			{
				dropRecheck.Start(visitTick, Info.DropRiskRecheckTicks);
				RecheckDropRuns(bot);
			}

			var tiers = DropTiers;
			var visit = tiers.Visit(dropState, visitTick);
			dropState = visit.State;
#if CAMEO_DEV_LOGGING
			LogDropSummary(visitTick, false);
#endif
			if (!visit.ScanOpportunity && !visit.DrawRegular)
				return;

			int? roll = visit.DrawRegular ? BotRng.For(player, nameof(EngineerBotModule)).Next(100) : null;
#if CAMEO_DEV_LOGGING
			if (visit.ScanOpportunity)
				dropScans++;
#endif
			var admitted = visit.ScanOpportunity && TryScheduledDrop(bot, true);
			if (visit.TryRegular(roll, tiers.ChancePercent, admitted) && TryScheduledDrop(bot, false))
			{
				RegularDrops++;
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ENGDROP B launch #{RegularDrops} (roll {roll} < chance {tiers.ChancePercent}; A admitted this visit: {admitted}) (tick {visitTick})");
				LogDropSummary(visitTick, true);
#endif
			}
		}

		bool IsGone(Actor a) => a.IsDead || !a.IsInWorld || a.Owner != player;

		/// <summary>Renew the leases of working engineers, release finished ones, stop stuck ones. Once per tick.</summary>
		void Housekeep(IBot bot)
		{
			var tick = world.WorldTick;
			if (housekeptTick == tick)
				return;

			housekeptTick = tick;
			HousekeepEscort(tick);
			var leases = BotUnitLeases.Of(player);
			foreach (var (a, job) in assigned.ToList())
			{
				// ENG-T: a passenger is out of the world while it rides, so the normal checks would read it as gone.
				if (job.ViaTransport)
				{
					var provider = TransportProvider();
					if (provider != null && provider.TryConsumeDelivered(a, out var delivered))
					{
						job.ViaTransport = false;
						job.OrderedTick = job.SampleTick = tick;
						job.SamplePos = a.CenterPosition;
						var t = delivered ?? job.Target;
						bot.QueueOrder(new Order("CaptureActor", a, Target.FromActor(t), true));
						BotMissionLog.Write(new BotMissionRecord
						{
							Player = player, MissionId = job.MissionId, Attempt = job.Attempt, State = BotMissionAttemptState.Progressing,
							Executor = "Engineers", MissionType = "capture", TargetCell = t.Location, Units = 1
						});
						BotUnitLeases.TryClaim(leases, a, LeaseOwner, BotLeasePurpose.Capture, Info.LeaseTicks);
						continue;
					}

					if (provider != null && provider.IsHandlingPassenger(a))
					{
						// §19.6: the run hands each passenger's lease to the provider for the ride —
						// renew ours only once it is back or free, so housekeeping isn't a refused
						// claim against the transport module on every pass.
						if (!BotUnitLeases.IsClaimedByOther(leases, a, LeaseOwner))
							BotUnitLeases.TryClaim(leases, a, LeaseOwner, BotLeasePurpose.Capture, Info.LeaseTicks);
						continue;
					}

					// Dropped by the provider (or no provider any more): from here the normal checks decide.
					job.ViaTransport = false;
					job.OrderedTick = tick;
				}

				var moving = !IsGone(a) && a.CurrentActivity?.ChildActivity?.ActivityType == ActivityType.Move;
				var check = Check(IsGone(a), !IsGone(a) && a.IsIdle, moving, IsGone(a) || a.CenterPosition != job.SamplePos,
					tick - job.OrderedTick, tick - job.SampleTick, Info.OrderGraceTicks, Info.AssignRoleDelay);

				switch (check)
				{
					case EngineerCheck.Gone:
						assigned.Remove(a);
						EndMission(a, job, check);
						break;
					case EngineerCheck.Done:
						assigned.Remove(a);
						leases?.Release(a, LeaseOwner);
						EndMission(a, job, check);
						break;
					case EngineerCheck.Stuck:
						assigned.Remove(a);
						EndMission(a, job, check);
						stuckUntil[a] = tick + Info.StuckRetryTicks;
						StuckStops++;

						// Order before release: a Stop issued while the lease is still held reads as the
						// claim's own last act at the order gate; releasing first would leave the Stop
						// unattributed and the next module's order counted crossed (GC-1 StandDownWalkers).
						bot.QueueOrder(new Order("Stop", a, false));
						leases?.Release(a, LeaseOwner);
#if CAMEO_DEV_LOGGING
						Log.Write("debug", $"AI ({player.ClientIndex}): ENG stuck {a.Info.Name} {a.ActorID} on {job.Job}, stopped and released (tick {tick})");
#endif
						break;
					default:
						BotUnitLeases.TryClaim(leases, a, LeaseOwner, PurposeOf(job.Job), Info.LeaseTicks);
						if (tick - job.SampleTick >= Info.AssignRoleDelay)
						{
							job.SampleTick = tick;
							job.SamplePos = a.CenterPosition;
						}

						break;
				}
			}

			foreach (var a in stuckUntil.Where(kv => kv.Value <= tick || IsGone(kv.Key)).Select(kv => kv.Key).ToList())
				stuckUntil.Remove(a);
		}

		/// <summary>Re-scores the remaining route of a walking engineer. Returns true when the attempt ended (pull back).</summary>
		bool Recheck(IBot bot, IBotUnitLeases leases, Actor a, Assignment job, int tick)
		{
			var target = job.Target;
			if (target == null || target.IsDead || !target.IsInWorld || job.Job != EngineerJob.Capture)
				return false;

			var remaining = RemainingExposure(job.Route, a.Location, target.Location, Info.ApproachCells, c => Danger(a, c));
			if (remaining <= Info.MaxExposedRouteCells)
				return false;

			var fresh = SafeRoute(a, target, "recheck");
			if (fresh == null && RouteRefused)
				return false; // a budget refusal is not "exposed": keep the route and re-check at the next interval
			if (fresh != null && ExposedCells(fresh.Select(c => (c, Danger(a, c))), target.Location, Info.ApproachCells) <= Info.MaxExposedRouteCells)
			{
				var waypoints = Waypoints(fresh, Info.RouteWaypointSpacing, Info.ApproachCells, target.Location);
				for (var i = 0; i < waypoints.Count; i++)
					bot.QueueOrder(new Order("Move", a, Target.FromCell(world, waypoints[i]), i > 0));

				bot.QueueOrder(new Order("CaptureActor", a, Target.FromActor(target), waypoints.Count > 0));
				job.Route = fresh;
				job.OrderedTick = job.SampleTick = tick;
				job.SamplePos = a.CenterPosition;
				Reroutes++;
				BotUnitLeases.TryClaim(leases, a, LeaseOwner, BotLeasePurpose.Capture, Info.LeaseTicks);
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ENG reroute {a.Info.Name} {a.ActorID} -> {target.Info.Name} {target.ActorID}: {remaining} exposed cells ahead > {Info.MaxExposedRouteCells} (tick {tick}; reroutes {Reroutes}, pullbacks {Pullbacks})");
#endif
				return false;
			}

			assigned.Remove(a);
			bot.QueueOrder(new Order("Move", a, Target.FromCell(world, initialBaseCenter), false));
			leases?.Release(a, LeaseOwner);
			EndMission(a, job, EngineerCheck.Working, true);
			Pullbacks++;
#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI ({player.ClientIndex}): ENG pullback {a.Info.Name} {a.ActorID} from {target.Info.Name} {target.ActorID}: {remaining} exposed cells ahead, no safe route (tick {tick}; reroutes {Reroutes}, pullbacks {Pullbacks})");
#endif
			return true;
		}

		/// <summary>Exposed cells still AHEAD of the engineer: the route cells after the one nearest its cell, outside the
		/// approach of the target. Free of world state so it can be tested.</summary>
		public static int RemainingExposure(IReadOnlyList<CPos> route, CPos engineerCell, CPos target, int approachCells, Func<CPos, int> danger)
		{
			if (route == null || route.Count == 0)
				return 0;

			var at = 0;
			for (var i = 1; i < route.Count; i++)
				if ((route[i] - engineerCell).LengthSquared < (route[at] - engineerCell).LengthSquared)
					at = i;

			var count = 0;
			for (var i = at + 1; i < route.Count; i++)
				if ((route[i] - target).LengthSquared > approachCells * approachCells && danger(route[i]) > 0)
					count++;

			return count;
		}

		/// <summary>The stealth gate, free of world state so it can be tested: guarded when more than `max` defenders (-1 = off).</summary>
		public static bool StealthGuarded(int defenders, int max) => max >= 0 && defenders > max;

		/// <summary>RankTargetsBySafety's score (higher is better), free of world state so it can be tested: value per cell
		/// of effective cost, each exposed cell costing `exposureWeight` cells of walking.</summary>
		public static double RankScore(int value, int distanceCells, int exposedCells, int exposureWeight) =>
			(double)Math.Max(0, value) / (1 + Math.Max(0, distanceCells) + (long)Math.Max(0, exposureWeight) * Math.Max(0, exposedCells));

		// FOG-ENGCRATE-01: generic-bot capture and bridge-hut scans read the fog. Off = the Check*ForVisibility flags, as before.
		bool FogOn => Info.FogEngCrate && !ablateFogEngCrate.Off(player, AblationIds.FogEngCrate);
		bool CaptureFilter => FogOn || Info.CheckCaptureTargetsForVisibility;
		bool CaptureVisible(Actor a) => FogOn ? FogEngCrateGate.Admits(player, a) : a.CanBeViewedByPlayer(player);

		int GuardingEnemies(Actor capturer, Actor target) =>
			world.FindActorsInCircle(target.CenterPosition, Info.EnemyAvoidanceRadius)
				.Count(u => !u.IsDead && u.IsInWorld && player.RelationshipWith(u.Owner) == PlayerRelationship.Enemy
					&& (!CaptureFilter || CaptureVisible(u))
					&& u.Info.HasTraitInfo<AttackBaseInfo>() && capturer.IsTargetableBy(u));

		/// <summary>Orders the try-candidates by RankScore over the nearest CaptureTargetTries x 2 (the rest are dropped).</summary>
		List<Actor> RankBySafety(Actor capturer, IEnumerable<Actor> candidates)
		{
			var tries = Math.Max(1, Info.CaptureTargetTries);
			return candidates.OrderBy(t => (t.CenterPosition - capturer.CenterPosition).LengthSquared)
				.Take(tries * 2)
				.Select(t =>
				{
					var route = SafeRoute(capturer, t, "rank");
					var exposed = route == null ? int.MaxValue / 2 : ExposedCells(route.Select(c => (c, Danger(capturer, c))), t.Location, Info.ApproachCells);
					return (Target: t, Score: RankScore(t.GetSellValue(), (t.Location - capturer.Location).Length, exposed, Info.ExposureWeight));
				})
				.OrderByDescending(x => x.Score)
				.Take(tries)
				.Select(x => x.Target)
				.ToList();
		}

		/// <summary>FB2: how many capturers the safe-target count justifies (Frans
		/// DemandTargetsPerCapturer / MaximumDemandCapturers). Static for tests.</summary>
		public static int DemandCapturersDesired(int safeTargets, int maximum, int targetsPerCapturer) =>
			Math.Min(Math.Max(0, maximum), Math.Max(0, safeTargets) / Math.Max(1, targetsPerCapturer));

		// FB2 (Frans SpecOps, DESIGN §19.5 omniscient-owner exemption applies to the same scans the
		// assign path already runs): count the safe capturable targets across EVERY capturable owner —
		// the assign path picks one random owner because one attempt needs one target, but demand is a
		// production question and must see the whole opportunity set deterministically. "Safe" reuses
		// the assign path's own gates (full / dormant / escort-blocked); route and guard checks still
		// apply when a real engineer is dispatched.
		void DemandCapturers(IBot bot, int liveCapturers)
		{
			if (liveCapturers >= Math.Max(0, Info.MaximumDemandCapturers))
				return;

			var targets = world.Actors.Where(a => !a.IsDead && a.IsInWorld
				&& Info.CapturableRelationships.HasRelationship(player.RelationshipWith(a.Owner))
				&& a.TraitOrDefault<CaptureManager>() != null);
			if (CaptureFilter)
				targets = targets.Where(a => CaptureVisible(a));
			if (capturableTypes.Count > 0)
				targets = targets.Where(t => capturableTypes.Contains(t.Info.Name.ToLowerInvariant()));

			var safeTargets = targets.Count(t => !TargetFull(t) && !Dormant(t) && !BlockedByEscort(t));
			if (liveCapturers < DemandCapturersDesired(safeTargets, Info.MaximumDemandCapturers, Info.DemandTargetsPerCapturer))
				RequestCapturer(bot);
		}

		// ScoutBotModule.RequestScout's contract verbatim: ordinal-sorted names, skip anything a builder
		// already queued or no queue of this faction can build, request the first buildable capturer type.
		void RequestCapturer(IBot bot)
		{
			foreach (var name in capturingTypes.OrderBy(n => n, StringComparer.Ordinal))
			{
				var builder = unitBuilders.FirstOrDefault(b => b.RequestedProductionCount(bot, name) == 0);
				if (builder == null)
					continue;

				// Unloaded ContentPacks leave their actor names out of Rules.Actors; TryGetValue is load-bearing.
				if (!world.Map.Rules.Actors.TryGetValue(name, out var actorInfo))
					continue;
				if (actorInfo?.TraitInfoOrDefault<BuildableInfo>() is not { } buildable)
					continue;
				if (!buildable.Queue.Any(q => OpenRA.Mods.CA.AIUtils.FindQueues(player, q).Any(pq => pq.BuildableItems().Any(b => b.Name == name))))
					continue;

				builder.RequestUnitProduction(bot, name);
				return;
			}
		}

		static BotLeasePurpose PurposeOf(EngineerJob job) => job == EngineerJob.Capture ? BotLeasePurpose.Capture : BotLeasePurpose.Engineer;

		/// <summary>An engineer this module may order now: ours, idle, not already on a job, not benched, not held by another module.</summary>
		bool Available(Actor a, IBotUnitLeases leases, bool allowFlyIdle)
		{
			if (IsGone(a) || assigned.ContainsKey(a) || stuckUntil.ContainsKey(a))
				return false;

			if (!(a.IsIdle || (allowFlyIdle && a.CurrentActivity is FlyIdle)))
				return false;

			if (BotUnitLeases.IsClaimedByOther(leases, a, LeaseOwner))
			{
				SkippedClaimed++;
				return false;
			}

			return true;
		}

		bool Assign(IBot bot, IBotUnitLeases leases, Actor engineer, EngineerJob job, Order order, Actor target,
			IReadOnlyList<CPos> waypoints = null, List<CPos> route = null)
		{
			if (!BotUnitLeases.TryClaim(leases, engineer, LeaseOwner, PurposeOf(job), Info.LeaseTicks))
				return false;

			var tick = world.WorldTick;
			var assignment = new Assignment { Job = job, OrderedTick = tick, SampleTick = tick, SamplePos = engineer.CenterPosition, Target = target, Route = route };
			assigned[engineer] = assignment;
			if (waypoints != null && waypoints.Count > 0)
			{
				// Walk the safe route: the first leg replaces whatever the engineer was doing, the rest queue behind it,
				// and the capture order (queued) follows the last waypoint.
				for (var i = 0; i < waypoints.Count; i++)
					bot.QueueOrder(new Order("Move", engineer, Target.FromCell(world, waypoints[i]), i > 0));
			}

			bot.QueueOrder(order);
			if (job == EngineerJob.Capture)
			{
				StartMission(assignment, engineer);
				if (escort != null && escort.Target == target)
					escort.Committed = true;
			}

			switch (job)
			{
				case EngineerJob.Capture: CaptureOrders++; break;
				case EngineerJob.RepairBridge: BridgeOrders++; break;
				default: BuildingRepairOrders++; break;
			}

#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI ({player.ClientIndex}): ENG {job} {engineer.Info.Name} {engineer.ActorID} -> {target.Info.Name} {target.ActorID} (tick {tick}; captures {CaptureOrders}, bridges {BridgeOrders}, repairs {BuildingRepairOrders}, stuck {StuckStops}, skipped-claimed {SkippedClaimed})");
#endif
			return true;
		}

		void QueueCaptureOrders(IBot bot)
		{
			if (capturingTypes.Count == 0)
				return;

			var leases = BotUnitLeases.Of(player);

			// TC-2e (AI_ARCHITECTURE §12.17): the cells outranking allied bots already claim, off the team
			// blackboard — one read per evaluation. Flag off or 1v1 leaves null and every gate below is the
			// no-op it is today.
			HashSet<WPos> claimsAhead = null;
			if (Info.UseTeamCaptureClaims)
				claimsAhead = TeamBlackboard.ClaimsAheadOf(TeamBlackboard.CollectBroadcasts(player), player.InternalName);

			bool ClaimedByOutrankingAlly(Actor target) =>
				claimsAhead != null && claimsAhead.Contains(world.Map.CenterOfCell(target.Location));

			// BF-2 (AI_ARCHITECTURE §12.26): the deterministic shard tier — in-shard targets sort
			// first, out-of-shard stays eligible after them. Size 1 makes every tier 0, the no-op.
			var shardSize = 1;
			var shardRank = 0;
			if (Info.PreferShardCaptureTargets)
				(shardRank, shardSize) = TeamBlackboard.ClaimRank(player);

			int ShardTier(Actor target) =>
				CaptureRules.CaptureShardTier(shardRank, shardSize, TeamBlackboard.CaptureShard(target.Location, shardSize));

			if (claimsAhead != null && claimsAhead.Count > 0)
			{
				// Stand down an in-flight capture whose target an outranking ally now claims — the same
				// release path as a finished job (unassigned, mission ended, lease out) plus a Stop.
				foreach (var (a, job) in assigned.ToList())
				{
					if (!CaptureRules.SupersedesCapture(job.Job == EngineerJob.Capture, job.Target != null, () => ClaimedByOutrankingAlly(job.Target)))
						continue;

					assigned.Remove(a);
					EndMission(a, job, EngineerCheck.Working, superseded: true);
					SupersededCaptures++;

					// Order before release (same shape as the Stuck path above).
					bot.QueueOrder(new Order("Stop", a, false));
					leases?.Release(a, LeaseOwner);
#if CAMEO_DEV_LOGGING
					Log.Write("debug", $"AI ({player.ClientIndex}): ENG capture superseded {a.Info.Name} {a.ActorID} -> {job.Target.Info.Name} {job.Target.ActorID} (tick {world.WorldTick}; superseded {SupersededCaptures})");
#endif
				}

				// The waiting escort plan is the same claim without a walking engineer — deny it too.
				// (A committed plan is ended by the stand-down above through its MissionId.)
				if (escort != null && CaptureRules.SupersedesCapture(true, escort.Target != null, () => ClaimedByOutrankingAlly(escort.Target)))
				{
					BotMissionLog.Write(new BotMissionRecord
					{
						Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Denied, Reason = BotMissionReasons.Superseded,
						Executor = "Engineers", MissionType = "capture", TargetCell = escort.Target.Location, Value = escort.DefenceValue
					});
					SupersededCaptures++;
					escort = null;
				}
			}

			var capturers = world.ActorsHavingTrait<Captures>()
				.Where(a => a.Owner == player && capturingTypes.Contains(a.Info.Name.ToLowerInvariant()))
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null)
				.ToList();
			var liveCapturers = capturers.Count;
			capturers = capturers.Where(tp => Available(tp.Actor, leases, false)).ToList();

			// FB2: safe targets justify building a specialist even when none is available — without
			// this, the first capturer only ever appears by unit-mix luck (the enumeration above is
			// the module's own-units scan; the demand count includes assigned and riding engineers).
			if (Info.UseDemandCapturers && !ablateDemandCapturers.Off(player, AblationIds.DemandCapturers))
				DemandCapturers(bot, liveCapturers);

			if (capturers.Count == 0)
				return;

			var next = 0;
			var baseCenter = world.Map.CenterOfCell(initialBaseCenter);

			if (CaptureRules.UsesPriorityPass(BotRng.For(player, nameof(EngineerBotModule)).Next(100), Info.PriorityCaptureChance))
			{
				var priorityTargets = world.Actors.Where(a =>
					!a.IsDead && a.IsInWorld && Info.CapturableRelationships.HasRelationship(player.RelationshipWith(a.Owner))
					&& priorityCapturableTypes.Contains(a.Info.Name.ToLowerInvariant()));

				// DESIGN §19.5: the engineer owner's fog exception.
				if (CaptureFilter)
					priorityTargets = priorityTargets.Where(a => CaptureVisible(a));

				var candidates = priorityTargets.Where(t => !TargetFull(t) && !Dormant(t) && !ClaimedByOutrankingAlly(t)).OrderBy(ShardTier).ThenBy(a => (a.CenterPosition - baseCenter).LengthSquared).ToList();
				ConsiderEscort(candidates);
				var ordered = candidates.Where(t => !BlockedByEscort(t)).ToList();

				// As the CA parent: each attempt uses up a target; a capturer is used up only when it is sent.
				var attempts = Math.Min(capturers.Count, ordered.Count);
				for (var i = 0; i < attempts && next < capturers.Count; i++)
				{
					var capturer = capturers[next];
					var target = ordered[i];
					var captureManager = target.TraitOrDefault<CaptureManager>();
					if (captureManager != null && capturer.Trait.CanTarget(captureManager) && TryRoute(capturer.Actor, target, out var waypoints, out var fullRoute)
						&& Assign(bot, leases, capturer.Actor, EngineerJob.Capture, new Order("CaptureActor", capturer.Actor, Target.FromActor(target), true), target, waypoints, fullRoute))
						next++;
				}

				if (next >= capturers.Count)
					return;
			}

			var randPlayers = world.Players.Where(p => !p.Spectating
				&& Info.CapturableRelationships.HasRelationship(player.RelationshipWith(p))).ToList();
			if (randPlayers.Count == 0)
				return;

			var randPlayer = randPlayers.Random(BotRng.For(player, nameof(EngineerBotModule)));
			var remaining = capturers.Skip(next).ToList();

			var options = world.Actors.Where(a => a.Owner == randPlayer && !a.IsDead && a.IsInWorld);
			if (CaptureFilter)
				options = options.Where(a => CaptureVisible(a));

			var capturable = options
				.Where(target =>
				{
					var captureManager = target.TraitOrDefault<CaptureManager>();
					return captureManager != null && remaining.Any(tp => tp.Trait.CanTarget(captureManager));
				})
				.OrderBy(ShardTier).ThenByDescending(target => target.GetSellValue())
				.Take(maximumCaptureTargetOptions);

			if (capturableTypes.Count > 0)
				capturable = capturable.Where(target => capturableTypes.Contains(target.Info.Name.ToLowerInvariant()));

			var targets = capturable.Where(t => !ClaimedByOutrankingAlly(t)).ToList();
			if (targets.Count == 0)
				return;

			for (var ci = 0; ci < remaining.Count; ci++)
			{
				var capturer = remaining[ci];
				ConsiderEscort(targets.Where(t => !TargetFull(t) && !Dormant(t)).OrderBy(ShardTier).ThenByDescending(t => t.GetSellValue()));

				// Nearest first. A full, dormant, escort-blocked or unsafe target passes the engineer on to the next one, so
				// with MaxEngineersPerTarget 1 engineers spread over DIFFERENT targets (TargetFull is re-read per engineer:
				// the previous engineer's Assign already counts). CaptureTargetTries 1 = the parents' single nearest target.
			var open = targets.Where(t => CaptureRules.IsOpenTarget(TargetFull(t), Dormant(t), () => BlockedByEscort(t)));
				var tries = Info.RankTargetsBySafety
					? RankBySafety(capturer.Actor, open)
					: open.OrderBy(ShardTier).ThenBy(t => (t.CenterPosition - capturer.Actor.CenterPosition).LengthSquared)
						.Take(Math.Max(1, Info.CaptureTargetTries))
						.ToList();
				foreach (var target in tries)
				{
					if (StealthTargetGuarded(capturer.Actor, target))
						continue;

					// ENG-T: sometimes a stealth infiltration rides in. Transport answers a hot or missing ground route,
					// so the roll precedes the route check; the provider plans its own way in, and a refused run falls
					// through to the foot path. The roll is drawn only when it can matter, so a chance of 0 leaves
					// LocalRandom's sequence — and every later random choice — exactly as before.
					// ENGDROP: with the scheduler on, drops come from RunDropScheduler and this roll (and its draw) never runs.
					if (!UsesDropScheduler && CaptureRules.ShouldRollForTransport(Info.TransportChance, EscortEligible(target))
						&& WantsTransport(true, BotRng.For(player, nameof(EngineerBotModule)).Next(100), Info.TransportChance)
						&& TransportProvider() is IBotCaptureTransportProvider provider)
					{
						var used = TryTransportRun(bot, leases, remaining.Skip(ci).Select(tp => tp.Actor).ToList(), target, targets, provider);
						if (used > 0)
						{
							ci += used - 1;
							break;
						}
					}

					if (!TryRoute(capturer.Actor, target, out var waypoints, out var fullRoute))
						continue;

					Assign(bot, leases, capturer.Actor, EngineerJob.Capture, new Order("CaptureActor", capturer.Actor, Target.FromActor(target), true), target, waypoints, fullRoute);
					break;
				}
			}
		}

		/// <summary>ENG-T's gate, free of world state so it can be tested.</summary>
		public static bool WantsTransport(bool stealthTarget, int roll, int chancePct) => stealthTarget && chancePct > 0 && roll < chancePct;

		int AvoidanceCells => Math.Max(1, (Info.EnemyAvoidanceRadius.Length + 1023) / 1024);

		EngineerAreaIntel AreaMemory => areaIntel ??= new EngineerAreaIntel(player, AreaThreats);

		/// <summary>What the bot knows of the area around one target, the route the craft will take and the landing ring (scouted memory only).</summary>
		AreaIntel DropArea(Actor target, IReadOnlyList<CPos> routeSamples) =>
			AreaMemory.Assess(target.Location, AvoidanceCells, routeSamples, null, world.WorldTick, Info.DropFreshTicks);

		/// <summary>Enemy defences, army, anti-air and detectors the bot has seen (visible now or remembered), with their reach. Cached per tick.</summary>
		IReadOnlyList<AreaThreat> AreaThreats()
		{
			if (areaThreatTick == world.WorldTick)
				return areaThreats;

			areaThreatTick = world.WorldTick;
			areaThreats.Clear();
			var situation = player.PlayerActor.TraitOrDefault<MasterAiBotModule>()?.Situation;
			if (situation == null)
				return areaThreats;

			defenceProviders ??= player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>().ToArray();
			var ranges = new Dictionary<CPos, int>();
			foreach (var provider in defenceProviders)
				foreach (var d in provider.RememberedDefences())
					ranges[d.Cell] = Math.Max(ranges.GetValueOrDefault(d.Cell), d.MaxRangeCells);

			foreach (var enemy in world.Players)
			{
				if (enemy == player || enemy.NonCombatant || player.RelationshipWith(enemy) != PlayerRelationship.Enemy)
					continue;

				foreach (var o in situation.Remembered(enemy).OrderBy(o => o.ActorID))
				{
					if (!o.Combat && !o.Defence && !o.AntiAir && !o.Detector)
						continue;

					// A static defence reaches as far as its longest weapon plus the margin; a unit as far as the module's own
					// avoidance radius (the same notion GuardingEnemies uses) plus the margin.
					var reach = (o.Defence && ranges.TryGetValue(o.Location, out var range) ? range : AvoidanceCells) + Info.DropExclusionMarginCells;
					areaThreats.Add(new AreaThreat(o.Location, o.Value, reach, o.Defence, o.Combat && !o.Building && !o.Defence, o.AntiAir,
						o.Detector, o.DetectorRangeCells));
				}
			}

			return areaThreats;
		}

		/// <summary>The run's hard exclusion geometry from the same memory: anti-air discs for aircraft, ground discs for every landing and target.</summary>
		DropConstraints BuildDropConstraints() => ConstraintsFor(AreaThreats(), world.Map.CenterOfCell, world.WorldTick);

		/// <summary>Most exclusion zones a run carries. More remembered threats than this and the geometry would be clipped (a clipped zone is a hole
		/// the craft may cross), so there are NO constraints: null, and Trigger A refuses.</summary>
		internal const int MaxDropZones = 256;

		internal static DropConstraints ConstraintsFor(IReadOnlyList<AreaThreat> threats, Func<CPos, WPos> centreOf, int tick) =>
			threats.Count > MaxDropZones
				? null
				: new DropConstraints(threats.Select(th => new DropExclusion(centreOf(th.Cell),
					WDist.FromCells(Math.Max(1, th.RangeCells)).Length, th.AntiAir ? DropExclusionKind.AntiAir : DropExclusionKind.Ground)), tick);

		/// <summary>Trigger A may plan a target's route again only DropPlanRetryTicks after the last plan (or refusal).</summary>
		internal static bool PlanDue(int now, int lastPlanTick, int retryTicks) => lastPlanTick <= 0 || now - lastPlanTick >= retryTicks;

		/// <summary>Trigger A's candidates: the ranked targets that pass BOTH the threat prefilter and the plan throttle, cut to the try count AFTER
		/// filtering, so a throttled or defended target never uses up a try that a later, plannable one could have had.</summary>
		internal static IEnumerable<T> TriggerATries<T>(IEnumerable<T> ranked, Func<T, bool> noThreat, Func<T, bool> planDue, int tries) =>
			ranked.Where(t => noThreat(t) && planDue(t)).Take(Math.Max(1, tries));

		readonly Dictionary<uint, int> dropPlanTick = [];

		/// <summary>DESIGN 19.5: the engineer owner is omniscient as a whole, so target choice reads the world as the legacy loop does.</summary>
		List<Actor> DropTargets(List<Actor> capturers)
		{
			HashSet<WPos> claimsAhead = null;
			if (Info.UseTeamCaptureClaims)
				claimsAhead = TeamBlackboard.ClaimsAheadOf(TeamBlackboard.CollectBroadcasts(player), player.InternalName);

			return world.ActorsHavingTrait<CaptureManager>()
				.Where(t => !t.IsDead && t.IsInWorld && t.Owner != null && Info.CapturableRelationships.HasRelationship(player.RelationshipWith(t.Owner))
					&& (!CaptureFilter || CaptureVisible(t))
					&& !EscortEligible(t) && !TargetFull(t) && !Dormant(t)
					&& (capturableTypes.Count == 0 || capturableTypes.Contains(t.Info.Name.ToLowerInvariant()))
					&& (claimsAhead == null || !claimsAhead.Contains(world.Map.CenterOfCell(t.Location)))
					&& (capturers == null || capturers.Any(c => c.TraitOrDefault<CaptureManager>().CanTarget(t.TraitOrDefault<CaptureManager>()))))
				.OrderBy(t => t.ActorID)
				.ToList();
		}

		/// <summary>
		/// ENGDROP: try to start one transport run now. opportunity = Trigger A, see <see cref="TryOpportunityRun"/>. Otherwise Trigger B: any
		/// stealth building the foot path's own guard test accepts, up to TransportRunMax stops, and the provider may queue a transport for the next
		/// try. Targets are ranked value per cell of distance, actor id as the tie-break.
		/// </summary>
		bool TryScheduledDrop(IBot bot, bool opportunity)
		{
			if (capturingTypes.Count == 0 || TransportProvider() is not { } provider)
				return false;

			var leases = BotUnitLeases.Of(player);
			var capturers = world.ActorsHavingTrait<Captures>()
				.Where(a => a.Owner == player && capturingTypes.Contains(a.Info.Name.ToLowerInvariant())
					&& a.TraitOrDefault<CaptureManager>() != null && Available(a, leases, false))
				.OrderBy(a => a.ActorID)
				.ToList();
			if (capturers.Count == 0)
				return false;

			var pool = DropTargets(capturers)
				.Where(t => opportunity || !StealthTargetGuarded(capturers[0], t))
				.ToList();
			if (pool.Count == 0)
				return false;

			// Trigger A: a target whose area holds remembered defence, army or anti-air cannot qualify, so it must not use up one of the few tries.
			var ranked = pool
				.OrderByDescending(t => RankScore(t.GetSellValue(), (t.Location - capturers[0].Location).Length, 0, 0))
				.ThenBy(t => t.ActorID)
				.AsEnumerable();
			// A target whose area is not yet Known still passes the threat filter (the stamps only exist once its route has been planned, see
			// TryOpportunityRun). The plan throttle rotates them: a target planned (or refused) in the last DropPlanRetryTicks drops out here, so the
			// next scan's CaptureTargetTries window reaches the next unknown target instead of re-planning the same few.
			var tries = opportunity
				? TriggerATries(ranked,
					t => AreaMemory.Assess(t.Location, AvoidanceCells, [], null, world.WorldTick, Info.DropFreshTicks).ThreatValue == 0,
					t => !dropPlanTick.TryGetValue(t.ActorID, out var lastPlan) || PlanDue(world.WorldTick, lastPlan, Info.DropPlanRetryTicks),
					Info.CaptureTargetTries)
				: ranked.Take(Math.Max(1, Info.CaptureTargetTries));

			foreach (var first in tries)
			{
				var crew = capturers.OrderBy(c => (c.CenterPosition - first.CenterPosition).LengthSquared).ThenBy(c => c.ActorID).ToList();
#if CAMEO_DEV_LOGGING
				if (opportunity)
					opportunityTries++;
				else
					regularTries++;
#endif
				if (opportunity)
				{
					if (TryOpportunityRun(bot, leases, crew, first, provider))
					{
						OpportunityDrops++;
#if CAMEO_DEV_LOGGING
						Log.Write("debug", $"AI ({player.ClientIndex}): ENGDROP A launch #{OpportunityDrops} -> {first.Info.Name} {first.ActorID} (tick {world.WorldTick})");
						LogDropSummary(world.WorldTick, true);
#endif
						return true;
					}
				}
				else if (TryTransportRun(bot, leases, crew, first, pool, provider) > 0)
					return true;
			}

			return false;
		}

		/// <summary>
		/// ENGDROP Trigger A for one target: the route the transport will ACTUALLY fly or drive is the provider's own planned journey for the first
		/// startable existing craft (no planned route at decision time = not Trigger A; the target waits for Trigger B). The cells the bot sees now
		/// along it are stamped; the run qualifies only when the target area, that planned route and the landing ring are KNOWN (fresh stamps) and
		/// hold zero remembered or observed defence, army and anti-air. One engineer, one vetted stop; existing craft only, never a production
		/// request, with the memory's hard exclusion constraints.
		/// </summary>
		bool TryOpportunityRun(IBot bot, IBotUnitLeases leases, List<Actor> crew, Actor target, IBotCaptureTransportProvider provider)
		{
			if (dropPlanTick.TryGetValue(target.ActorID, out var lastPlan) && !PlanDue(world.WorldTick, lastPlan, Info.DropPlanRetryTicks))
				return false;

			if (dropPlanTick.Count > 64)
				dropPlanTick.Clear();

			dropPlanTick[target.ActorID] = Math.Max(1, world.WorldTick);

			var constraints = BuildDropConstraints();
			if (constraints == null || !provider.TryPlanCaptureRoute([crew[0]], [target], constraints, out var journey))
				return false;

			var samples = VisibilityStamps.SampleRoute(journey, Info.DropRouteSamples);
			AreaMemory.Observe(target.Location, AvoidanceCells, samples, world.WorldTick);
			if (!DropArea(target, samples).Undefended)
				return false;

			return TryTransportRun(bot, leases, crew, target, [target], provider, allowProduction: false, constraints, opportunity: true, samples, journey) > 0;
		}

		/// <summary>Re-check each in-flight Trigger A run against the memory: a gone target or newly seen defence/army/anti-air at the target, on the
		/// corridor or at the landing aborts the run once through the provider's own safe retreat path.</summary>
		void RecheckDropRuns(IBot bot)
		{
			if (TransportProvider() is not { } provider)
				return;

			foreach (var (a, job) in assigned.ToList())
			{
				if (!job.ViaTransport || !job.Opportunity || job.RunId == null || job.AbortRequested || job.Target == null)
					continue;

				string reason = null;
				if (job.Target.IsDead || !job.Target.IsInWorld || job.Target.Owner == null
					|| !Info.CapturableRelationships.HasRelationship(player.RelationshipWith(job.Target.Owner)))
					reason = "target_gone";
				else if (DropArea(job.Target, job.RouteSamples ?? []).ThreatValue > 0)
					reason = "evidence_contradicted";

				if (reason == null)
					continue;

				job.AbortRequested = true;
				if (provider.RequestAbort(bot, job.RunId, a, reason))
				{
					OpportunityAborts++;
#if CAMEO_DEV_LOGGING
					Log.Write("debug", $"AI ({player.ClientIndex}): ENGDROP A abort #{OpportunityAborts} run {job.RunId} engineer {a.ActorID} -> {job.Target.Info.Name} {job.Target.ActorID}: {reason} (tick {world.WorldTick})");
					LogDropSummary(world.WorldTick, true);
#endif
				}
			}
		}

		/// <summary>The enabled transport provider, resolved at use (never cached: LC4's bug class).</summary>
		IBotCaptureTransportProvider TransportProvider() =>
			player.PlayerActor.TraitsImplementing<IBotCaptureTransportProvider>().FirstOrDefault(p => p is not IDisabledTrait d || !d.IsTraitDisabled);

		/// <summary>The visiting order of a run, free of world state so it can be tested: start at `start`, then always the
		/// nearest unvisited stop (squared cell distance), at most `max` stops.</summary>
		public static List<int> GreedyRoute(IReadOnlyList<CPos> stops, int start, int max)
		{
			var route = new List<int> { start };
			var left = Enumerable.Range(0, stops.Count).Where(i => i != start).ToList();
			while (route.Count < max && left.Count > 0)
			{
				var from = stops[route[^1]];
				var next = left.MinBy(i => (stops[i] - from).LengthSquared);
				route.Add(next);
				left.Remove(next);
			}

			return route;
		}

		/// <summary>ENG-T: one run of up to TransportRunMax engineers, one per stealth building; returns the engineers used.</summary>
		int TryTransportRun(IBot bot, IBotUnitLeases leases, List<Actor> candidates, Actor first, List<Actor> pool, IBotCaptureTransportProvider provider,
			bool allowProduction = true, DropConstraints constraints = null, bool opportunity = false, IReadOnlyList<CPos> routeSamples = null,
			IReadOnlyList<CPos> vettedRoute = null)
		{
			// The stops: the chosen building, then other stealth buildings that are free for an attempt.
			var stops = new List<Actor> { first };
			stops.AddRange(pool.Where(t => t != first && !TargetFull(t) && !Dormant(t) && !EscortEligible(t)));
			var order = GreedyRoute(stops.Select(t => t.Location).ToList(), 0, Math.Min(Info.TransportRunMax, candidates.Count));

			var passengers = new List<Actor>();
			var targets = new List<Actor>();
			foreach (var engineer in candidates)
			{
				if (passengers.Count == order.Count)
					break;

				if (!BotUnitLeases.TryClaim(leases, engineer, LeaseOwner, BotLeasePurpose.Capture, Info.LeaseTicks))
					continue;

				passengers.Add(engineer);
				targets.Add(stops[order[passengers.Count - 1]]);
			}

			// ENGDROP: Trigger A vetted the route for crew[0] alone; any other passenger set is a different run.
			var bindBroken = opportunity && (passengers.Count != 1 || passengers[0] != candidates[0]);

			// ENGDROP: Trigger A asks the side-effect-free readiness first; only a ready run reaches the commit request.
			string runId = null;
			if (passengers.Count == 0 || bindBroken || (opportunity && !provider.CanStartCaptureRun(passengers, targets, constraints ?? DropConstraints.None))
				|| !(constraints != null
					? provider.TryRequestConstrainedCaptureRun(bot, passengers, targets, constraints, allowProduction, opportunity ? vettedRoute : null, out runId)
					: provider.TryRequestCaptureRun(bot, passengers, targets)))
			{
				foreach (var p in passengers)
					leases?.Release(p, LeaseOwner);

				return 0;
			}

			var tick = world.WorldTick;
			for (var k = 0; k < passengers.Count; k++)
			{
				var job = new Assignment
				{
					Job = EngineerJob.Capture, OrderedTick = tick, SampleTick = tick, SamplePos = passengers[k].CenterPosition,
					Target = targets[k], ViaTransport = true, RunId = runId, Opportunity = opportunity, RouteSamples = routeSamples
				};
				assigned[passengers[k]] = job;
				CaptureOrders++;
				StartMission(job, passengers[k], "Transport");
			}

#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI ({player.ClientIndex}): ENG transport run: {passengers.Count} engineer(s) -> {string.Join(", ", targets.Select(t => $"{t.Info.Name} {t.ActorID}"))} (tick {tick})");
#endif

			// The remaining candidates list may skip engineers whose claim failed; the caller advances by the scanned count.
			return candidates.IndexOf(passengers[^1]) + 1;
		}


		/// <summary>
		/// The route gate for one capture. MaxExposedRouteCells &lt; 0: the parents' check (a path exists; no waypoints).
		/// Otherwise the least-exposed path, rejected when too exposed; its waypoints are returned for the order.
		/// </summary>
		/// <summary>The stealth-guard veto, shared by the foot and transport paths: a stealth target ringed by more
		/// than StealthMaxDefenders armed guards is skipped entirely — escorted targets never gate on it.</summary>
		bool StealthTargetGuarded(Actor capturer, Actor target)
		{
			if (Info.StealthMaxDefenders < 0 || EscortEligible(target))
				return false;

			var defenders = GuardingEnemies(capturer, target);
			if (!StealthGuarded(defenders, Info.StealthMaxDefenders))
				return false;

			GuardedSkips++;
#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI ({player.ClientIndex}): ENG stealth target {target.Info.Name} {target.ActorID} guarded by {defenders} > {Info.StealthMaxDefenders} (tick {world.WorldTick}; guarded {GuardedSkips})");
#endif
			return true;
		}

		bool TryRoute(Actor capturer, Actor target, out IReadOnlyList<CPos> waypoints, out List<CPos> fullRoute)
		{
			waypoints = null;
			fullRoute = null;
			if (Info.MaxExposedRouteCells < 0)
				return SafePath(capturer, target).Type != TargetType.Invalid;

			var route = SafeRoute(capturer, target, "gate");
			if (route == null)
				return false;

			var exposed = ExposedCells(route.Select(c => (c, Danger(capturer, c))), target.Location, Info.ApproachCells);
			if (exposed > Info.MaxExposedRouteCells)
			{
				UnsafeRoutes++;
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ENG route to {target.Info.Name} {target.ActorID} too exposed: {exposed} cells under fire > {Info.MaxExposedRouteCells} (tick {world.WorldTick}; unsafe {UnsafeRoutes})");
#endif
				return false;
			}

			waypoints = Waypoints(route, Info.RouteWaypointSpacing, Info.ApproachCells, target.Location);
			fullRoute = route;
			return true;
		}

		/// <summary>Cells of the route within reach of armed enemies, the final approach (within `approachCells` of the
		/// target) excluded. Free of world state so it can be tested.</summary>
		public static int ExposedCells(IEnumerable<(CPos Cell, int Danger)> route, CPos target, int approachCells) =>
			route.Count(c => c.Danger > 0 && (c.Cell - target).LengthSquared > approachCells * approachCells);

		/// <summary>Every `spacing`-th cell of a source-first route, stopping before the final approach (the capture
		/// order walks that part). Free of world state so it can be tested.</summary>
		public static List<CPos> Waypoints(IReadOnlyList<CPos> route, int spacing, int approachCells, CPos target)
		{
			var result = new List<CPos>();
			spacing = Math.Max(1, spacing);
			for (var i = spacing; i < route.Count; i += spacing)
			{
				if ((route[i] - target).LengthSquared <= approachCells * approachCells)
					break;

				result.Add(route[i]);
			}

			return result;
		}

		readonly Dictionary<CPos, int> dangerCache = [];
		int dangerCacheTick = -1;

		// DESIGN §19.5: the engineer owner is omniscient as a whole, so this scan sees through fog. Only ARMED enemies
		// count (the parent counted every actor that could target an engineer's type, harvesters and MCVs included).
		// Cached per tick: one evaluation routes several engineers over the same cells.
		int Danger(Actor capturer, CPos loc)
		{
			// ENG-ROUTE-01: with the budget on a cell's value is reused for DangerCacheTicks ticks (1 = the old per-tick cache; the tick delta is >= 1 whenever the tick changed).
			if (world.WorldTick - dangerCacheTick >= (RouteBudgetOn ? Math.Max(1, Info.DangerCacheTicks) : 1))
			{
				dangerCache.Clear();
				dangerCacheTick = world.WorldTick;
			}

			if (dangerCache.TryGetValue(loc, out var d))
				return d;

			var center = world.Map.CenterOfCell(loc);
			var sum = 0L;
			foreach (var u in world.FindActorsInCircle(center, Info.EnemyAvoidanceRadius))
				if (!u.IsDead && capturer.Owner.RelationshipWith(u.Owner) == PlayerRelationship.Enemy
					&& (!CaptureFilter || CaptureVisible(u))
					&& u.Info.HasTraitInfo<AttackBaseInfo>() && capturer.IsTargetableBy(u))
					sum += Math.Max(0, Info.EnemyAvoidanceRadius.Length - (center - u.CenterPosition).Length);

			// Bounded so a long route through a crowded base cannot overflow the path cost.
			return dangerCache[loc] = (int)Math.Min(sum, 1 << 20);
		}

		/// <summary>The least-exposed path from the engineer to a cell next to the target, source first; null when none (or, with ENG-ROUTE-01 on, when the search was deferred or over budget: unsafe for this visit).</summary>
		List<CPos> SafeRoute(Actor capturer, Actor target, string caller = "")
		{
			RouteRefused = false;
			var mobile = capturer.TraitOrDefault<Mobile>();
			if (mobile == null || !mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, capturer.Location, target.Location))
				return null;

			// ENG-ROUTE-01: at most SafeRouteCallsPerTick searches per world tick across the ranking, the recheck and the route gate; a deferred one is unsafe for this visit.
			var budget = RouteBudgetOn;
			if (budget && Info.SafeRouteCallsPerTick > 0)
			{
				if (routeCallsTick != world.WorldTick)
				{
					routeCallsTick = world.WorldTick;
					routeCalls = 0;
				}

				if (routeCalls >= Info.SafeRouteCallsPerTick)
				{
					RouteCallsDeferred++;
					RouteRefused = true;
#if CAMEO_DEV_LOGGING
					Log.Write("debug", $"AI ({player.ClientIndex}): ENG saferoute caller={caller} deferred (call cap {Info.SafeRouteCallsPerTick}) tick {world.WorldTick}");
#endif
					return null;
				}

				routeCalls++;
			}

			// A pair whose search overran is unsafe without searching again for RouteOverrunMemoryTicks: no repeated 2000-evaluation search and no repeated throw for the same unreachable ring.
			var pair = (capturer.ActorID, target.ActorID);
			if (budget && routeOverrunUntil.TryGetValue(pair, out var until))
			{
				if (world.WorldTick < until)
				{
					RouteRefused = true;
					return null;
				}

				routeOverrunUntil.Remove(pair);
			}

			var footprint = target.OccupiesSpace?.OccupiedCells().Select(c => c.Cell).ToHashSet() ?? [target.Location];
			var ring = new HashSet<CPos>();
			foreach (var c in footprint)
				for (var dx = -1; dx <= 1; dx++)
					for (var dy = -1; dy <= 1; dy++)
					{
						var n = new CPos(c.X + dx, c.Y + dy);
						if (!footprint.Contains(n) && world.Map.Contains(n))
							ring.Add(n);
					}

			// ENG-ROUTE-01: the cost callback is the search's expansion counter; past the budget it throws and the search is abandoned (the engine disposes its pooled state).
			// Distinct cells, not callbacks: the callback runs for every relaxed neighbour (several per expanded cell) and repeats cells, while the expensive part (Danger) is paid once per cell.
			var seen = budget ? new HashSet<CPos>() : null;
			var evaluations = 0;
			var limit = budget ? Info.SafeRouteExpansionBudget : 0;
#if CAMEO_DEV_LOGGING
			var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
			List<CPos> path;
			try
			{
				path = mobile.PathFinder.FindPathToTargetCellByPredicate(
					capturer, [capturer.Location], ring.Contains, BlockedByActor.Stationary,
					loc =>
					{
						if (seen != null)
						{
							if (seen.Add(loc))
								evaluations++;
						}
						else
							evaluations++;

						if (limit > 0 && evaluations > limit)
							throw new RouteBudgetExceeded();

						return Danger(capturer, loc);
					});
			}
			catch (RouteBudgetExceeded)
			{
				RouteBudgetOverruns++;
				routeOverrunUntil[pair] = world.WorldTick + RouteOverrunMemoryTicks;
				RouteRefused = true;
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ENG saferoute caller={caller} OVERRUN {capturer.Info.Name}->{target.Info.Name} evaluations>{limit} ring={ring.Count} {watch.ElapsedMilliseconds} ms tick {world.WorldTick} (overruns {RouteBudgetOverruns}, deferred {RouteCallsDeferred})");
#endif
				return null;
			}

#if CAMEO_DEV_LOGGING
			if (watch.ElapsedMilliseconds >= 50)
				Log.Write("debug", $"AI ({player.ClientIndex}): ENG saferoute caller={caller} SLOW {capturer.Info.Name}->{target.Info.Name} evaluations={evaluations} ring={ring.Count} path={path.Count} {watch.ElapsedMilliseconds} ms tick {world.WorldTick}");
#endif
			if (path.Count == 0)
				return null;

			// The engine returns paths goal-first.
			if ((path[0] - capturer.Location).LengthSquared > (path[^1] - capturer.Location).LengthSquared)
				path.Reverse();

			return path;
		}

		// CA parent verbatim, kept for MaxExposedRouteCells < 0 (the default until its A/B). It is only a reachability
		// check: the goal predicate `loc => true` matches the start cell, so the danger cost is never scored along any
		// route, and the caller discards the path anyway (maintainer's question, 2026-09-30). SafeRoute is the real one.
		Target SafePath(Actor capturer, Actor target)
		{
			var mobile = capturer.TraitOrDefault<Mobile>();
			if (mobile == null)
				return Target.Invalid;

			var locomotor = mobile.Locomotor;
			if (!mobile.PathFinder.PathExistsForLocomotor(locomotor, capturer.Location, target.Location))
				return Target.Invalid;

			var path = mobile.PathFinder.FindPathToTargetCellByPredicate(
				capturer, new[] { capturer.Location }, loc => true, BlockedByActor.Stationary,
				loc => world.FindActorsInCircle(world.Map.CenterOfCell(loc), Info.EnemyAvoidanceRadius)
					.Where(u => !u.IsDead && capturer.Owner.RelationshipWith(u.Owner) == PlayerRelationship.Enemy
						&& (!CaptureFilter || CaptureVisible(u)) && capturer.IsTargetableBy(u))
					.Sum(u => Math.Max(WDist.Zero.Length, Info.EnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(loc) - u.CenterPosition).Length)));

			return path.Count == 0 ? Target.Invalid : Target.FromActor(target);
		}

		void QueueRepairBridgeOrder(IBot bot)
		{
			if (Info.RepairingActorTypes.Count == 0)
				return;

			var leases = BotUnitLeases.Of(player);
			var repairers = world.ActorsHavingTrait<RepairsBridges>()
				.Where(a => a.Owner == player && Info.RepairingActorTypes.Contains(a.Info.Name) && Available(a, leases, true))
				.ToList();

			if (repairers.Count == 0)
				return;

			// Few bridge huts per map, so a list is fine (AS parent).
			var huts = world.ActorsWithTrait<BridgeHut>()
				.Where(at => Info.RepairableHutActorTypes.Contains(at.Actor.Info.Name) && at.Trait.BridgeDamageState >= DamageState.Dead)
				.Select(at => at.Actor).ToList();
			huts.AddRange(world.ActorsWithTrait<LegacyBridgeHut>()
				.Where(at => Info.RepairableHutActorTypes.Contains(at.Actor.Info.Name) && at.Trait.BridgeDamageState >= DamageState.Dead)
				.Select(at => at.Actor));

			if (FogOn || Info.CheckRepairTargetsForVisibility)
				huts.RemoveAll(h => !(FogOn ? FogEngCrateGate.Admits(player, h) : h.CanBeViewedByPlayer(player)));

			SendOneRepairer(bot, leases, repairers, huts, EngineerJob.RepairBridge, "RepairBridge");
		}

		void QueueRepairBuildingOrder(IBot bot)
		{
			if (Info.RepairingActorTypes.Count == 0)
				return;

			var leases = BotUnitLeases.Of(player);
			var repairers = world.ActorsHavingTrait<InstantlyRepairs>()
				.Where(a => a.Owner == player && Info.RepairingActorTypes.Contains(a.Info.Name) && Available(a, leases, true))
				.ToList();

			if (repairers.Count == 0)
				return;

			// Allied buildings only: the bot knows its own and its allies' base, no fog involved.
			var targets = world.ActorsHavingTrait<InstantlyRepairable>().Where(t =>
			{
				if (!Info.RepairableActorTypes.Contains(t.Info.Name) || t.Owner.RelationshipWith(player) != PlayerRelationship.Ally)
					return false;

				var health = t.TraitOrDefault<IHealth>();
				return health != null && health.DamageState >= Info.RepairableDamageState;
			}).ToList();

			SendOneRepairer(bot, leases, repairers, targets, EngineerJob.RepairBuilding, "InstantRepair");
		}

		/// <summary>AS parent: the first repairer that reaches its nearest target goes; one per evaluation.</summary>
		void SendOneRepairer(IBot bot, IBotUnitLeases leases, List<Actor> repairers, List<Actor> targets, EngineerJob job, string orderString)
		{
			if (targets.Count == 0)
				return;

			foreach (var r in repairers)
			{
				foreach (var target in targets.OrderBy(a => (r.Location - a.Location).LengthSquared))
				{
					if (!AIUtils.PathExist(r, target.Location, target))
						continue;

					if (Assign(bot, leases, r, job, new Order(orderString, r, Target.FromActor(target), true), target))
						return;

					break;
				}
			}
		}

		/// <summary>The escort rule, free of world state so it can be tested: an undefended target needs no escort;
		/// otherwise our armed value near it must reach `superiorityPct` percent of the defenders'.</summary>
		public static bool EscortReady(int ownValue, int defenceValue, int superiorityPct) =>
			defenceValue <= 0 || (long)ownValue * 100 >= (long)defenceValue * superiorityPct;

		/// <summary>Superiority AND thinning: the defenders must also have fallen to `thinnedPct` percent of their value
		/// at publish. Free of world state so it can be tested.</summary>
		public static bool EscortReady(int ownValue, int defenceValue, int initialDefenceValue, int superiorityPct, int thinnedPct) =>
			defenceValue <= 0 || (EscortReady(ownValue, defenceValue, superiorityPct)
				&& (long)defenceValue * 100 <= (long)Math.Max(defenceValue, initialDefenceValue) * thinnedPct);

		static int CostOf(Actor a) => a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

		// DESIGN §19.5: the engineer owner is omniscient as a whole (maintainer 2026-09-30) — these scans see through fog.
		int DefenceValue(Actor target) =>
			world.FindActorsInCircle(target.CenterPosition, Info.EnemyAvoidanceRadius)
				.Where(u => !u.IsDead && u.IsInWorld && player.RelationshipWith(u.Owner) == PlayerRelationship.Enemy
					&& u.Info.HasTraitInfo<AttackBaseInfo>())
				.Sum(CostOf);

		int OwnArmedValueNear(Actor target) =>
			world.FindActorsInCircle(target.CenterPosition, Info.EnemyAvoidanceRadius)
				.Where(u => !u.IsDead && u.IsInWorld && u.Owner == player && u.Info.HasTraitInfo<AttackBaseInfo>()
					&& !u.Info.HasTraitInfo<BuildingInfo>())
				.Sum(CostOf);

		/// <summary>
		/// Maintainer 2026-09-30: escorts are ONLY for tech buildings (neutral, or a PriorityCapturableActorTypes
		/// entry) in an unsafe area. A building in the enemy's base is taken by stealth — an escort would give the
		/// engineer away — so it is never escorted: the engineer sneaks in alone along SafePath.
		/// </summary>
		public static bool EscortEligible(bool enemyOwned, bool priorityType) => priorityType || !enemyOwned;

		bool EscortEligible(Actor target) =>
			EscortEligible(player.RelationshipWith(target.Owner) == PlayerRelationship.Enemy,
				priorityCapturableTypes.Contains(target.Info.Name.ToLowerInvariant()));

		/// <summary>True when a defended tech target may not be attempted yet: escorts on, defenders present, and no
		/// ready escort plan for exactly this target. Enemy-base buildings are never blocked (stealth).</summary>
		bool BlockedByEscort(Actor target)
		{
			if (!Info.EscortDefendedCaptures || !EscortEligible(target))
				return false;

			var defence = DefenceValue(target);
			if (defence <= 0)
				return false;

			if (escort == null || escort.Target != target)
				return true;

			escort.DefenceValue = defence;
			return !EscortReady(OwnArmedValueNear(target), defence, escort.InitialDefenceValue, Info.EscortSuperiority, Info.EscortThinnedPercent);
		}

		/// <summary>Opens the one escort plan for the first defended candidate (the candidates arrive best first).</summary>
		void ConsiderEscort(IEnumerable<Actor> candidates)
		{
			if (!Info.EscortDefendedCaptures || escort != null)
				return;

			foreach (var t in candidates)
			{
				if (!EscortEligible(t))
					continue;

				var defence = DefenceValue(t);
				if (defence <= 0)
					continue;

				escort = new EscortPlan
				{
					Episode = escortEpisodes.Admit(),
					Target = t, MissionId = CaptureMissionId(t.Info.Name, t.ActorID), SinceTick = world.WorldTick, DefenceValue = defence,
					InitialDefenceValue = defence
				};

				BotMissionLog.Write(new BotMissionRecord
				{
					Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Published,
					Executor = "Engineers", MissionType = "capture", TargetCell = t.Location, Value = defence
				});
				return;
			}
		}

		/// <summary>Ends a plan whose target is gone or ours, and denies one whose escort never came.</summary>
		void HousekeepEscort(int tick)
		{
			if (escort == null || escort.Committed)
				return;

			var t = escort.Target;
			if (t.IsDead || !t.IsInWorld || t.Owner == player)
			{
				BotMissionLog.Write(new BotMissionRecord
				{
					Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Denied, Reason = BotMissionReasons.TargetGone,
					Executor = "Engineers", MissionType = "capture"
				});
				escort = null;
				return;
			}

			if (tick - escort.SinceTick < Info.EscortWaitTicks)
				return;

			// No escort in time: no execution attempt ever existed — mission feedback, then the shelf.
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Denied, Reason = BotMissionReasons.NoUnits,
				Executor = "Engineers", MissionType = "capture", TargetCell = t.Location, Value = escort.DefenceValue
			});
			Shelve(escort.MissionId, tick);
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Dormant, Reason = BotMissionReasons.NoUnits,
				Executor = "Engineers", MissionType = "capture", TargetCell = t.Location
			});
			escort = null;
		}

		/// <summary>The escort seam: one standing guard request at the escorted target while its mission lives.</summary>
		IReadOnlyList<BotProtectionRequest> IBotProtectionRequestProvider.ProtectionRequests =>
			escort == null || escort.Target.IsDead || !escort.Target.IsInWorld
				? []
				: [new BotProtectionRequest(escort.Target.Location, EscortRequestValue(escort.DefenceValue, EscortRequestFloor()), world.WorldTick + Info.EscortRequestTicks, escort.Episode)];

		/// <summary>A target already carrying `max` live capture attempts takes no more (max ≤ 0: no limit).</summary>
		public static bool TargetFull(int liveAttempts, int max) => max > 0 && liveAttempts >= max;

		bool TargetFull(Actor target) =>
			TargetFull(assigned.Values.Count(j => j.Job == EngineerJob.Capture && j.Target == target), Info.MaxEngineersPerTarget);

		// MC1 (docs/design/AI_MISSION_CARDS.md): every capture is a mission card. The mission is the building (its
		// strategic reason survives a lost engineer, and a change of owner); each engineer sent at it is one attempt.
		public static string CaptureMissionId(string actorType, uint actorId) => $"capture:{actorType}:{actorId}";

		/// <summary>The dormant shelf's trigger, free of world state so it can be tested.</summary>
		public static bool GoesDormant(int failStreak, int threshold) => threshold > 0 && failStreak >= threshold;

		/// <summary>S1: the escalating shelf. Episode 1 rests CaptureDormantTicks; every further shelve of the
		/// same mission adds another, capped at CaptureDormantEscalationMax × base. Free of world state.</summary>
		public static int DormantWindowTicks(int baseTicks, int episode, int escalationMax)
		{
			if (baseTicks <= 0)
				return 0;
			var multiplier = Math.Clamp(episode, 1, Math.Max(1, escalationMax));
			return (int)Math.Min(int.MaxValue, (long)baseTicks * multiplier);
		}

		/// <summary>Parks a mission on the dormant shelf for this episode's escalated window.</summary>
		int Shelve(string missionId, int tick)
		{
			var episode = dormantEpisodes.GetValueOrDefault(missionId) + 1;
			dormantEpisodes[missionId] = episode;
			var until = tick + DormantWindowTicks(Info.CaptureDormantTicks, episode, Info.CaptureDormantEscalationMax);
			dormantUntil[missionId] = until;
			return until;
		}

		bool Dormant(Actor target) =>
			dormantUntil.TryGetValue(CaptureMissionId(target.Info.Name, target.ActorID), out var until) && world.WorldTick < until;

		/// <summary>How a capture attempt ended, free of world state so it can be tested. Order matters: an engineer
		/// consumed by a successful capture is also "dead" (Actor.IsDead includes Disposed).</summary>
		public static (BotMissionAttemptState State, string Reason) CaptureVerdict(bool targetOurs, bool stuck, bool engineerDead, bool targetGone)
		{
			if (targetOurs)
				return (BotMissionAttemptState.Success, BotMissionReasons.Done);

			if (stuck)
				return (BotMissionAttemptState.Released, BotMissionReasons.Stuck);

			if (engineerDead)
				return (BotMissionAttemptState.Failed, BotMissionReasons.LostUnits);

			if (targetGone)
				return (BotMissionAttemptState.Released, BotMissionReasons.TargetGone);

			return (BotMissionAttemptState.Released, BotMissionReasons.Dropped);
		}

		void StartMission(Assignment job, Actor engineer, string executor = "Engineers")
		{
			var target = job.Target;
			job.MissionId = CaptureMissionId(target.Info.Name, target.ActorID);

			// A card leaving the shelf is its own event, before the attempt that follows it.
			if (dormantUntil.Remove(job.MissionId))
				BotMissionLog.Write(new BotMissionRecord
				{
					Player = player, MissionId = job.MissionId, Event = BotMissionEvent.Reopened, Reason = BotMissionReasons.Timeout,
					Executor = "Engineers", MissionType = "capture", TargetCell = target.Location
				});

			job.Attempt = missionAttempts.GetValueOrDefault(job.MissionId) + 1;
			missionAttempts[job.MissionId] = job.Attempt;
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = job.MissionId, Attempt = job.Attempt, State = BotMissionAttemptState.Committed,
				Executor = "Engineers", MissionType = "capture", TargetCell = target.Location, Units = 1
			});
		}

		void EndMission(Actor engineer, Assignment job, EngineerCheck check, bool pulledBack = false, bool superseded = false)
		{
			if (job.MissionId == null)
				return;

			// The escorted attempt is over either way: the escort's job ends with it.
			if (escort != null && escort.MissionId == job.MissionId)
				escort = null;

			var target = job.Target;
			var targetGone = target == null || target.IsDead || !target.IsInWorld;
			var (state, reason) = CaptureVerdict(!targetGone && target.Owner == player, check == EngineerCheck.Stuck,
				engineer.IsDead, targetGone);

							// A voluntary retreat (RouteRecheckTicks): released, outmatched; it does not feed the dormant shelf.
							if (pulledBack && state == BotMissionAttemptState.Released && reason == BotMissionReasons.Dropped)
								reason = BotMissionReasons.Outmatched;

							// TC-2e: a stand-down to an outranking ally's published claim — released, superseded;
							// like the voluntary retreat, it does not feed the dormant shelf.
							if (superseded && state == BotMissionAttemptState.Released && reason == BotMissionReasons.Dropped)
								reason = BotMissionReasons.Superseded;
							BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = job.MissionId, Attempt = job.Attempt, State = state, Reason = reason,
				Executor = "Engineers", MissionType = "capture", TargetCell = target?.Location, Units = 1,

				// Where the attempt ended: for a lost engineer, where it fell (on the route or at the target).
				UnitCell = world.Map.CellContaining(engineer.CenterPosition)
			});

			// The dormant shelf: a success clears both streaks; consecutive losses rest the mission.
			// A consecutive `Released/outmatched` run feeds the retreat shelf instead — a voluntary
			// retreat is not a failure, but sustained commit-retreat churn still rests the target.
			if (state == BotMissionAttemptState.Success)
			{
				missionFailStreak.Remove(job.MissionId);
				missionRetreatStreak.Remove(job.MissionId);
			}
			else if (state == BotMissionAttemptState.Failed)
			{
				missionRetreatStreak.Remove(job.MissionId);
				var streak = missionFailStreak.GetValueOrDefault(job.MissionId) + 1;
				missionFailStreak[job.MissionId] = streak;
				if (GoesDormant(streak, Info.CaptureFailuresBeforeDormant))
				{
					missionFailStreak.Remove(job.MissionId);
					Shelve(job.MissionId, world.WorldTick);
					BotMissionLog.Write(new BotMissionRecord
					{
						Player = player, MissionId = job.MissionId, Event = BotMissionEvent.Dormant, Reason = BotMissionReasons.Outmatched,
						Executor = "Engineers", MissionType = "capture", TargetCell = target?.Location
					});
				}
			}
			else if (state == BotMissionAttemptState.Released && reason == BotMissionReasons.Outmatched)
			{
				var retreats = missionRetreatStreak.GetValueOrDefault(job.MissionId) + 1;
				missionRetreatStreak[job.MissionId] = retreats;
				if (GoesDormant(retreats, Info.CaptureRetreatsBeforeDormant))
				{
					missionRetreatStreak.Remove(job.MissionId);
					Shelve(job.MissionId, world.WorldTick);
					BotMissionLog.Write(new BotMissionRecord
					{
						Player = player, MissionId = job.MissionId, Event = BotMissionEvent.Dormant, Reason = BotMissionReasons.Outmatched,
						Executor = "Engineers", MissionType = "capture", TargetCell = target?.Location
					});
				}
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			var nodes = new List<MiniYamlNode> { new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)) };

			// ENGDROP: the scheduler's two deadlines (world ticks fit an int). The bot RNG streams are not saved by any module.
			if (dropStarted)
			{
				nodes.Add(new("DropNextOpportunityTick", FieldSaver.FormatValue((int)dropState.NextOpportunityTick)));
				nodes.Add(new("DropNextRegularTick", FieldSaver.FormatValue((int)dropState.NextRegularTick)));
			}

			return nodes;
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			// Existing saves contain no escort admission history. Identity remains UNKNOWN after load;
			// do not change legacy save fields, orders or restore behavior to manufacture provenance.
			escortEpisodes.Invalidate();
			if (escort != null)
				escort.Episode = default;
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();
			if (nodes.TryGetValue("InitialBaseCenter", out var node))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", node.Value);

			if (Info.UseDropScheduler && nodes.TryGetValue("DropNextOpportunityTick", out var opportunityNode)
				&& nodes.TryGetValue("DropNextRegularTick", out var regularNode))
			{
				dropState = new DropScheduleState(
					FieldLoader.GetValue<int>("DropNextOpportunityTick", opportunityNode.Value),
					FieldLoader.GetValue<int>("DropNextRegularTick", regularNode.Value));
				dropStarted = true;
			}
		}
	}
}
