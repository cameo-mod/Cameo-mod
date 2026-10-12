#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using BotRng = OpenRA.Mods.CA.BotRng;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using CAAIUtils = OpenRA.Mods.CA.AIUtils;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[Desc("Phase EX-0 of the expansion planner (docs/design/AI_ARCHITECTURE.md §12.13): scores every resource field",
		"we hold no refinery at by value x safety / time-until-it-pays and keeps the best one as the target field.",
		"Telemetry only: nothing reads the target yet (EX-1 makes the base builder walk toward it).")]
	public class ExpansionPlannerBotModuleInfo : ConditionalTraitInfo, NotBefore<ResourceMapBotModuleInfo>
	{
		[Desc("Ticks between two re-plans.")]
		public readonly int ReplanTicks = 250;

		[Desc("How far (cells) one new building extends the base: the step of the building line toward a field.")]
		public readonly int LinkStepCells = 4;

		[Desc("How far (cells) a field's resource edge may be from the tiles of a building that gives buildable area for",
			"its anchor to be claimable — the law's refinery sits flush to the field, so the field edge, not the spreader,",
			"is what must be in reach. A field farther than this needs link buildings (the base crawl) first.")]
		public readonly int ReachCells = 6;

		[Desc("Radius (cells) around a field's resource centre in which our own combat units count as its guard.")]
		public readonly int GuardRadiusCells = 12;

		[Desc("Smoothing (ticks) added to the time-until-it-pays, so a field next to the yard does not divide by ~0.")]
		public readonly int TauTicks = 750;

		[Desc("Window (ticks) over which the income used to turn costs into time is measured.")]
		public readonly int IncomeWindowTicks = 1500;

		[Desc("EX-1: publish the target field to the base builder, whose BaseCrawl placements then walk toward it.",
			"False = telemetry only (EX-0).")]
		public readonly bool DriveBaseCrawl = false;

		[Desc("TF-1 (FINDINGS_2026-10-11_turtle_regression increment 1): bound OPTIONAL crawl-link supply to a",
			"per-target allowance that only paid link admissions spend, gated on deterministic frontier progress",
			"toward the aim — a stalled route parks until the target moves. Mandatory power and refinery",
			"deployment never consult it. False = the traced unbounded supply (default off).")]
		public readonly bool BoundCrawlLinkSupply = false;

		[Desc("TF-1: paid cost an optional-link episode may supply before the route parks. 0 = no cost bound.")]
		public readonly int OptionalLinkCostAllowance = 6000;

		[Desc("TF-1: paid links an optional-link episode may supply before the route parks. 0 = no count bound.")]
		public readonly int OptionalLinkCountAllowance = 4;

		[Desc("TF-1: re-plans an outstanding paid link may go without advancing the frontier before the route",
			"parks. At ReplanTicks=250, 6 re-plans is ~1500 ticks — a slow build's produce-and-place window.")]
		public readonly int OptionalLinkNonprogressReplans = 6;

		/// <summary>Measured re-plans a pending queue intent may go unobserved in production before it drops uncharged.</summary>
		public readonly int OptionalLinkPendingReplans = 2;

		[Desc("CRAWL-SAFETY (FINDINGS_2026-10-12 S3): scout, escort, defend and army-parity gates on the",
			"optional crawl link. An unexplored aim is scouted first (the want waits ScoutWaitReplans while",
			"publishing a scout-interest cell); a contested frontier (remembered enemy value at the aim)",
			"publishes a front defence until covered and an escort request until our guard outweighs the",
			"threat; while the remembered own-vs-enemy army ratio sits below the difficulty-scaled floor no",
			"optional link is wanted at all (army production keeps priority); N links lost near the frontier",
			"park the target's route for LossParkTicks. Mandatory power bypasses every gate. False = the want",
			"rides bare (default off).")]
		public readonly bool UseCrawlSafety = false;

		[Desc("CS: remembered own-vs-enemy army ratio below which no optional link is wanted, on the",
			"BotSituation.CombatRatioPct scale (100 = even, larger = we win) — the EASY end of the",
			"difficulty line, interpolated to the HARD end by bot rank. 0 disables the gate.")]
		public readonly int ArmyParityPctEasy = 40;

		public readonly int ArmyParityPctHard = 80;

		[Desc("CS: links lost within LossCellRadiusCells of one another park the target's route — the",
			"farmed-cell bound at the EASY end of the difficulty line (easier bots tolerate more farming).",
			"0 disables loss parking.")]
		public readonly int LossesToParkEasy = 5;

		public readonly int LossesToParkHard = 2;

		[Desc("CS: own building cells within this distance (cells) of the frontier->aim segment belong to",
			"the watched corridor — losses there count toward the park.")]
		public readonly int LossWatchRadiusCells = 14;

		[Desc("CS: lost cells this close together count as the same farmed cell.")]
		public readonly int LossCellRadiusCells = 4;

		[Desc("CS: ticks a loss-farmed target field stays out of the candidates.")]
		public readonly int LossParkTicks = 7500;

		[Desc("CS: re-plans an unexplored aim may hold the link want while scouts close in. 0 = hold until",
			"explored without bound (the interest cell keeps drawing scouts either way).")]
		public readonly int ScoutWaitReplans = 4;

		[Desc("CS: own guard value must reach this percent of the remembered enemy value at the front",
			"before a contested link admits — below it the frontier publishes an escort request instead.")]
		public readonly int FrontGuardPercent = 110;

		[Desc("CS: own armed-building value within FrontCoverRadiusCells of the frontier that a contested",
			"front demands before the next link — below it the planner publishes a front-defence want",
			"instead. 0 disables the defence want.")]
		public readonly int FrontCoverMinValue = 600;

		[Desc("CS: radius (cells) around the frontier anchor inside which own armed buildings count as",
			"front cover.")]
		public readonly int FrontCoverRadiusCells = 10;

		[Desc("CS: ticks a published escort request stays live between re-plans (re-emitted each plan).")]
		public readonly int EscortRequestTicks = 600;

		[Desc("EX-2: while the target field is in reach and unclaimed, ask the base builder for a refinery there, beyond",
			"its fixed optimum (every field in reach gets one). Needs DriveBaseCrawl, which publishes the target.")]
		public readonly bool DriveRefineries = false;

		[Desc("EX-2: an own refinery this close (cells) to a field's resource centre claims it.")]
		public readonly int ClaimRadiusCells = 8;

		[Desc("EX-2: refineries built while a field stayed unclaimed before the field is parked (a placement that keeps",
			"missing it must not turn into a refinery loop).")]
		public readonly int MaxClaimAttempts = 2;

		[Desc("EX-2: ticks a parked field is left out of the candidates.")]
		public readonly int ParkTicks = 3000;

		[Desc("EX-3: tell McvExpansionManagerBotModule where an MCV should found its next base (it still decides when).")]
		public readonly bool DriveMcvSite = false;

		[Desc("EX-3: only fields at least this many link buildings away are MCV sites; nearer ones the building line reaches.")]
		public readonly int McvMinHops = 3;

		[Desc("EX-3: smoothing (cells) added to the MCV's distance to a site, so the nearest field does not divide by ~0.")]
		public readonly int McvTauCells = 10;

		[Desc("LC3 (AI_REVIEW_FRANSOTTO P1a): the same field handed out this many times in a row is parked for ParkTicks.",
			"The MCV module only asks again for an IDLE MCV, so a repeat means the last attempt there failed (no deploy",
			"cell, blocked, or rejected); the module records its own checkspot, not ours, so without this the field comes",
			"back forever. 0 disables.")]
		public readonly int McvMaxSiteHandouts = 3;

		[Desc("Greedy expansion: the planner requests construction-MCV production itself while a free field beyond the",
			"building line's reach (McvMinHops) exists, instead of waiting for the MCV module's high cash trigger.",
			"The engine module still decides where to send it (EX-3) and placement/dedup is unchanged.")]
		public readonly bool DriveMcvRequests = false;

		[Desc("Greedy expansion: DEPRECATED as a request gate (REF-1 B2 — the request is free and rides under the",
			"reserve; production is cash-gated at the queue). Kept for yaml compatibility; currently unused.")]
		public readonly int McvRequestReserve = 1500;

		[Desc("Greedy expansion: construction yards + construction MCVs + queued MCVs the driver aims for.",
			"The engine module's own count/cash gates still apply to its requests on top of this.")]
		public readonly int McvTargetCount = 3;

		[Desc("UT-4 (AI_ARCHITECTURE §12.13): the TechRush<->Expansion utility axis scales the appetite —",
			"an Expansion-leaning personality raises the effective McvTargetCount, a TechRush-leaning one",
			"lowers it. Needs DriveMcvRequests. genericbot-only module, so classic is unaffected.")]
		public readonly bool UseUtilityExpansionAppetite = false;

		[Desc("UT-4: at the full Expansion pole the effective McvTargetCount grows by this many.")]
		public readonly int ExpansionAxisBonusMcvs = 2;

		[Desc("UT-4: at the full TechRush pole the effective McvTargetCount shrinks by this many (floor 1).")]
		public readonly int TechRushAxisMinusMcvs = 1;

		[Desc("EX-4 (AI_ARCHITECTURE §12.19): the appetite tracks the map, not a flat cap — while any reachable far",
			"field stays free, the driver keeps up to CoverAllFieldsMaxInflight construction MCVs in the queue on top",
			"of however many yards/MCVs already exist, so expansion only stops when every field is claimed (or",
			"unreachable/parked). Difficulty still paces it through BotLimits production intervals and the cash",
			"reserve — speed changes, the ceiling does not. Needs DriveMcvRequests.")]
		public readonly bool CoverAllFields = false;

		[Desc("EX-4: queued construction MCVs the cover-the-map driver holds in flight at once. Bounds the",
			"commitment, not the map: finished MCVs leave the queue and a new one is requested while fields remain.")]
		public readonly int CoverAllFieldsMaxInflight = 2;

		[Desc("BZ_production_scaling: on top of the active MCV target, admit up to ScalingMcvMaxExtra",
			"extra construction MCVs while windowed income holds above the difficulty line, a reachable",
			"far field stays free, and the army-parity floor admits expansion. Also makes MCV site",
			"handouts skip fields whose deploy annulus holds no placeable cell (MCV-EXPAND-IDLE).",
			"Armed by the increment switch; false keeps today's behaviour bit-identical.")]
		public readonly bool UseProductionScaling = false;

		[Desc("BZ: windowed income per tick admitting the extra-MCV arm (easiest pole; difficulty line via BotDifficultyLadder).")]
		public readonly int ScalingMcvIncomeEasy = 60;

		[Desc("BZ: windowed income bar (hardest pole).")]
		public readonly int ScalingMcvIncomeHard = 30;

		[Desc("BZ: concurrent extra MCVs over the active target (easiest pole).")]
		public readonly int ScalingMcvMaxExtraEasy = 1;

		[Desc("BZ: concurrent extra MCVs (hardest pole).")]
		public readonly int ScalingMcvMaxExtraHard = 3;

		[Desc("TC-2c (AI_ARCHITECTURE §12.17): yield a free field to an allied bot's published expansion claim",
			"when it outranks us (lower ClientIndex). Deterministic precedence, so contested fields converge",
			"instead of oscillating. Inert in 1v1 — no allied broadcasts exist.")]
		public readonly bool UseTeamExpansionClaims = false;

		[Desc("TC-2c: an allied claim this close (cells) to a field's resource centre contests it.")]
		public readonly int AllyClaimRadiusCells = 10;

		[Desc("TC-3 / BD (AI_ARCHITECTURE §12.18): score the Voronoi sector anchored at our own spawn",
			"first — a field belongs to the coalition sector anchor nearest it; foreign-sector fields keep",
			"CoalitionForeignSectorPercent of their score (reachable, deprioritized, never forbidden).",
			"Needs the master's UseCoalitionPlan publishing the anchors; inert in 1v1 (the only anchor",
			"is our own) and bit-identical when no provider publishes.")]
		public readonly bool UseCoalitionSectors = false;

		[Desc("TC-3 / BD: percent of its score a foreign-sector field keeps. 0 would still leave the",
			"argmax free to pick a foreign field when no own-sector field remains; higher values shrink",
			"the home bias.")]
		public readonly int CoalitionForeignSectorPercent = 35;

		[Desc("BEV (AI_MASTER_PLAN §3; DESIGN §19.4 keeps BevManagerBotModule unloaded because this owner covers it):",
			"a vehicle in the MCV module's McvTypes that is NOT a construction MCV and does not deploy into a refinery is",
			"a base-building vehicle (Japan's cores) and deploys next to the base, not at a far field. Construction MCVs",
			"(EX-3) and field refineries (Yuri's slave miner, Japan's core refinery) keep going to fields.")]
		public readonly bool BaseVehiclesAtBase = true;

		[Desc("REF-1 (AI_ARCHITECTURE §12.24 v2, DESIGN §19.1b; switch group AJ_field_coverage, default-on ruling",
			"2026-10-04): one refinery per ANCHOR (a resource spreader, or the centre of a field that has none), bound",
			"1:1 — a refinery is allowed only while a specific anchor in building reach is unclaimed (unserved, not",
			"parked, not pending), never by comparing totals. Anchors are grouped into FIELDS (8-connected components",
			"of the map's valuable resource cells): the first anchor of every unserved field in reach claims first,",
			"extra spreaders of covered fields only when no field is still waiting. Placement is the placeable cell",
			"nearest the anchor whose footprint touches the field's resource cells (gap 0; gap 1 only when 0 is",
			"unplaceable; never more; no home fallback). Needs DriveRefineries.")]
		public readonly bool FieldCoverage = false;

		[Desc("REF-1: ticks an anchor stays pending after a produced refinery was committed to it — covers the window",
			"between the placement order and the building landing so a second refinery cannot claim the same anchor.")]
		public readonly int AnchorClaimPendingTicks = 2000;

		[Desc("FE-1: a field with a spreader within this many cells of its resource centre is represented by the spreader;",
			"a field without one is its own anchor.")]
		public readonly int SpreaderFieldRadiusCells = 12;

		[Desc("FE-1: an own refinery within this many cells of an anchor serves it (0 = ClaimRadiusCells).")]
		public readonly int AnchorServeRadiusCells = 0;

		[Desc("FE-1: replans an anchor may stay wanted with no refinery gained before it is parked for ParkTicks",
			"(a claim whose placement keeps failing must not retry forever).")]
		public readonly int AnchorStuckReplans = 12;

		[Desc("REPAIR-B3: re-offers of the same uncommitted claim before the anchor is parked for",
			"RefineryClaimFailParkTicks (a claim whose site search keeps returning null must not churn).")]
		public readonly int RefineryClaimFailAttempts = 1;

		[Desc("REPAIR-B3: the bounded cooldown a claim-failed anchor is parked — shorter than ParkTicks so a",
			"transiently unplaceable anchor re-enters the market before the planner's terminal park fires.")]
		public readonly int RefineryClaimFailParkTicks = 1500;

		[Desc("REPAIR-B3: coverage-refresh ceilings — candidate site evaluations and directed pathfinder",
			"probes bound ONE WHOLE anchor-sweep (spanning ticks), never re-armed mid-refresh (R1).")]
		public readonly int CoverageSiteLimit = 32;

		public readonly int CoverageProbeLimit = 64;

		[Desc("REPAIR-B3: per-tick pacing for the same refresh — bounds the pathfinder spend in ONE world",
			"tick without touching the refresh ceilings above (a stopped anchor resumes next tick).")]
		public readonly int CoverageProbesPerTick = 8;

		[Desc("REPAIR-B3: route bound per directed leg in cells (milli-tiles = x1000) — a patch counts covered",
			"only when the outbound dock->patch AND the return patch->dock route each stay within it.")]
		public readonly int CoverageRouteLimitCells = 10;

		[Desc("REPAIR-B3: patch cells a (anchor, refinery) pair's route-probe walk advances per refresh —",
			"the walk resumes across refreshes until the field is exhausted, so an Unserved verdict only",
			"ever follows a fully-examined candidate set; each cell and each leg consumes budget.")]
		public readonly int CoveragePatchCellSample = 4;

		[Desc("FE-1: the factor a site/field keeps when its bearing from the main base lies within CrawlSeparationDegrees of",
			"the crawl target, an own yard or another in-flight MCV site (deprioritised, never forbidden).")]
		public readonly double MinSeparationFactor = 0.25;

		[Desc("FE-1: the bearing window (degrees) inside which two directions count as the same direction.")]
		public readonly int CrawlSeparationDegrees = 35;

		[Desc("FE-1: score x (1 + SpreadBonus x distance to our nearest building / map diagonal), for the MCV site and the crawl target.")]
		public readonly double SpreadBonus = 1.0;

		[Desc("FE-1: a building closer than this (cells) to the main base has no usable bearing and is ignored by the separation factor.")]
		public readonly int MinBearingDistanceCells = 6;

		[Desc("Building queues searched for the refinery and the cheapest link building. Empty = the enabled base",
			"builder's own BuildingQueues (Cameo's classic mode builds from the player-level RABuilding queue).")]
		public readonly HashSet<string> BuildingQueues = new();

		public override object Create(ActorInitializer init) { return new ExpansionPlannerBotModule(init.Self, this); }
	}

	public enum McvRole { Expansion, FieldRefinery, BaseBuilding }

	/// <summary>
	/// REF-1 (§12.24 v2): one resource FIELD — an 8-connected component of the map's initial valuable resource cells
	/// (public map data, frozen when the model is built; depletion under the fog is never re-read). Anchors carry the
	/// field id; the claim tier is 1 while no refinery serves the field, 2 for its additional spreaders.
	/// </summary>
	internal sealed class RefineryField
	{
		public readonly int Id;
		public readonly List<CPos> Cells;
		public readonly HashSet<CPos> CellSet;
		public readonly CPos Center;

		public RefineryField(int id, List<CPos> cells)
		{
			Id = id;
			Cells = cells;
			CellSet = new HashSet<CPos>(cells);
			Center = cells.Count == 0 ? CPos.Zero : new CPos((int)cells.Average(c => c.X), (int)cells.Average(c => c.Y));
		}
	}

	public class ExpansionPlannerBotModule : ConditionalTrait<ExpansionPlannerBotModuleInfo>, IBotTick, IBotExpansionTargetProvider,
		IBotMcvExpansionSiteProvider, IBotPositionsUpdated, IBotExpansionAssistProvider,
		IBotProtectionRequestProvider, IBotScoutInterestProvider
	{
		public readonly struct FieldScore
		{
			public readonly int Index;
			public readonly CPos Center;
			public readonly int Value;
			public readonly int Hops;
			public readonly int PaybackTicks;
			public readonly int Threat;
			public readonly double Score;
			public readonly double Safety;

			public FieldScore(int index, CPos center, int value, int hops, int paybackTicks, int threat, double score, double safety = 1)
			{
				Safety = safety;
				Index = index;
				Center = center;
				Value = value;
				Hops = hops;
				PaybackTicks = paybackTicks;
				Threat = threat;
				Score = score;
			}
		}

		/// <summary>
		/// TF-1 (FINDINGS_2026-10-11_turtle_regression increment 1): the optional crawl-link episode
		/// ledger. Episodes key on the target field's centre; the bound spends only PAID link
		/// admissions — a queue's StartProduction intent is a pending CLAIM and becomes paid only
		/// when the item is seen in its production queue — and the route lives only while the
		/// frontier keeps closing on the aim. World-free: owners, item names, distances and aims are
		/// supplied, so the whole lifecycle — claim, acceptance, drop, progress, park, reset — is
		/// directly testable.
		/// </summary>
		public sealed class CrawlLinkAllowance
		{
			/// <summary>
			/// One queue's live admission: pending at order intent, paid once the item is observed in
			/// its production queue, removed once it leaves — accepted charges stand either way.
			/// </summary>
			public sealed class Claim
			{
				public readonly object Owner;
				public readonly string Item;
				public readonly int Cost;
				public int Age;
				public bool Paid;

				public Claim(object owner, string item, int cost)
				{
					Owner = owner;
					Item = item;
					Cost = cost;
				}
			}

			readonly Dictionary<object, Claim> claims = new();

			public CPos? Target { get; private set; }
			public int PaidCost { get; private set; }
			public int PaidCount { get; private set; }

			/// <summary>Outstanding unsettled admissions — pending intents and accepted-but-unprogressed claims.</summary>
			public int AwaitingProgress => claims.Count;

			public int NonprogressReplans { get; private set; }
			public bool Parked { get; private set; }
			public int BestDistanceSq { get; private set; } = -1;
			public CPos? LastAim { get; private set; }
			public IReadOnlyDictionary<object, Claim> Claims => claims;
			public bool HasClaims => claims.Count > 0;

			/// <summary>
			/// A changed target opens a fresh episode: the whole ledger, claims included, resets. A
			/// want that blinks off and back on under the SAME target must not re-open the
			/// allowance, so callers key on the field's centre and never pass null mid-route.
			/// Returns whether a new episode started — the caller logs the reset once.
			/// </summary>
			public bool ForTarget(CPos target)
			{
				if (Target == target)
					return false;

				Target = target;
				PaidCost = 0;
				PaidCount = 0;
				NonprogressReplans = 0;
				Parked = false;
				BestDistanceSq = -1;
				LastAim = null;
				claims.Clear();
				return true;
			}

			/// <summary>
			/// The per-replan frontier→aim measure. A moved aim re-baselines without counting; a
			/// strictly smaller distance is progress (every outstanding admission settles); anything
			/// else counts a stalled re-plan while claims are outstanding and `maxNonprogress` of
			/// them park the route. `distanceSq` &lt; 0 means the frontier could not be measured —
			/// nothing counts.
			/// </summary>
			public void NoteDistance(int distanceSq, CPos? aim, int maxNonprogress)
			{
				if (distanceSq < 0 || Parked)
					return;

				if (LastAim != aim)
				{
					LastAim = aim;
					BestDistanceSq = distanceSq;
					return;
				}

				if (distanceSq < BestDistanceSq)
				{
					BestDistanceSq = distanceSq;
					claims.Clear();
					NonprogressReplans = 0;
					return;
				}

				if (claims.Count > 0 && ++NonprogressReplans >= maxNonprogress)
					Parked = true;
			}

			/// <summary>
			/// An optional link is supplied only with progress evidence (a measured distance), no
			/// outstanding claim, headroom under the paid bounds, and a live route.
			/// `maxCost`/`maxCount` of 0 leave that bound uncapped.
			/// </summary>
			public bool OptionalLinkAdmissible(int cost, int maxCost, int maxCount, int distanceSq) =>
				!Parked && distanceSq >= 0 && claims.Count == 0
				&& (maxCost <= 0 || PaidCost + cost <= maxCost)
				&& (maxCount <= 0 || PaidCount < maxCount);

			/// <summary>
			/// The atomic admission gate — the queue calls it at the StartProduction order: the
			/// first queue through holds the pending claim, any second queue in the same window is
			/// refused (the published want alone admits nothing). A mandatory-power claim bypasses
			/// every bound — a real deficit outranks the allowance and any outstanding claim.
			/// Charges NOTHING: the claim becomes paid only when Reconcile sees it in production.
			/// </summary>
			public bool TryClaim(object owner, string item, int cost, bool mandatory, int maxCost, int maxCount)
			{
				if (claims.ContainsKey(owner))
					return true;

				if (!mandatory
					&& (Parked || claims.Count > 0
						|| (maxCost > 0 && PaidCost + cost > maxCost)
						|| (maxCount > 0 && PaidCount >= maxCount)))
					return false;

				claims[owner] = new Claim(owner, item, Math.Max(0, cost));
				return true;
			}

			/// <summary>
			/// Per-replan reconcile against what actually entered production: a pending claim seen
			/// in its queue becomes PAID — the charge lands here, never at intent; an unseen pending
			/// intent ages out dropped, uncharged; a paid item that left production keeps its
			/// charges — it consumed the spend window either way, and the route's progress check is
			/// what judges it.
			/// </summary>
			public void Reconcile(IReadOnlyDictionary<object, HashSet<string>> inflightByOwner, int maxPendingAge)
			{
				foreach (var claim in claims.Values.ToList())
				{
					var seen = inflightByOwner.TryGetValue(claim.Owner, out var items) && items.Contains(claim.Item);
					if (seen)
					{
						if (!claim.Paid)
						{
							claim.Paid = true;
							PaidCost = SaturateAdd(PaidCost, claim.Cost);
							PaidCount = SaturateAdd(PaidCount, 1);
						}

						continue;
					}

					if (claim.Paid || ++claim.Age >= maxPendingAge)
						claims.Remove(claim.Owner);
				}
			}

			static int SaturateAdd(int a, int b) => (int)Math.Min(int.MaxValue, (long)a + b);
		}

		/// <summary>
		/// CRAWL-SAFETY (FINDINGS_2026-10-12 S3): the crawl front ledger — the corridor watch that counts
		/// lost link buildings per cell and the scout-wait counter. World-free: watch sets, radii and
		/// re-plan bounds are supplied, so the whole lifecycle — watch diff, per-cell loss merge, park
		/// trigger, scout hold — is directly testable.
		/// </summary>
		public sealed class CrawlFrontSafety
		{
			readonly Dictionary<CPos, int> losses = new();
			HashSet<CPos> lastWatch = new();
			bool watching;

			public int ScoutWaitReplans { get; private set; }
			public int TotalLosses => losses.Values.Sum();

			/// <summary>
			/// Squared distance of <paramref name="p"/> to the segment a-b — the corridor the loss watch
			/// keeps is every own building cell within LossWatchRadiusCells of the frontier→aim line.
			/// The projection lands inside the segment by the dot product; the distance itself is
			/// |AP|² − dot²/|AB|² — pure integer math, deterministic on every client.
			/// </summary>
			public static long PointToSegmentDistanceSq(CPos p, CPos a, CPos b)
			{
				var abx = (long)b.X - a.X;
				var aby = (long)b.Y - a.Y;
				var denom = abx * abx + aby * aby;
				var apx = (long)p.X - a.X;
				var apy = (long)p.Y - a.Y;
				if (denom == 0)
					return apx * apx + apy * apy;

				var dot = apx * abx + apy * aby;
				if (dot <= 0)
					return apx * apx + apy * apy;

				if (dot >= denom)
				{
					var bpx = (long)p.X - b.X;
					var bpy = (long)p.Y - b.Y;
					return bpx * bpx + bpy * bpy;
				}

				return apx * apx + apy * apy - dot * dot / denom;
			}

			/// <summary>
			/// The per-replan corridor watch: cells gone since the last set count a loss at their own
			/// cell; cells within <paramref name="mergeRadiusCells"/> of each other pool their counts so
			/// a link rebuilt on the same farmed spot accumulates one count. A disappeared cell counts
			/// only while <paramref name="stillInCorridor"/> says it is — an advancing frontier slides
			/// cells out of the corridor without losing them. Returns the worst pooled count at a
			/// newly-lost cell and that cell — the caller compares it against the park bound.
			/// </summary>
			public int NoteWatch(IReadOnlyCollection<CPos> watch, int mergeRadiusCells, out CPos hottest,
				Predicate<CPos> stillInCorridor = null)
			{
				hottest = default;
				var worst = 0;
				if (watching)
					foreach (var cell in lastWatch)
						if (!watch.Contains(cell) && (stillInCorridor == null || stillInCorridor(cell)))
						{
							losses[cell] = losses.GetValueOrDefault(cell) + 1;
							var near = LossesNear(cell, mergeRadiusCells);
							if (near > worst)
							{
								worst = near;
								hottest = cell;
							}
						}

				lastWatch = new HashSet<CPos>(watch);
				watching = true;
				return worst;
			}

			/// <summary>The pooled loss count within <paramref name="radiusCells"/> of <paramref name="cell"/>.</summary>
			public int LossesNear(CPos cell, int radiusCells)
			{
				var radiusSq = (long)radiusCells * radiusCells;
				var sum = 0;
				foreach (var kv in losses)
					if ((kv.Key - cell).LengthSquared <= radiusSq)
						sum += kv.Value;

				return sum;
			}

			/// <summary>
			/// The scout-wait bound: an unexplored aim may hold the want while the counter stays under
			/// the bound — true while it still holds; a bound of 0 holds until explored forever.
			/// </summary>
			public bool ScoutHolds(int maxReplans) => maxReplans <= 0 || ++ScoutWaitReplans < maxReplans;

			public void ResetScoutWait() => ScoutWaitReplans = 0;

			/// <summary>The corridor ended (target or want gone) — the next watch diffs from nothing.</summary>
			public void ResetCorridor() => watching = false;
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly Dictionary<int, int> initialCells = new();
		readonly Dictionary<int, CPos> initialCenters = new();
		readonly Queue<(int Tick, int Earned)> income = new();

		ResourceMapBotModule resourceMap;
		PlayerResources resources;
		IBotRegionThreatProvider[] threatProviders;
		BaseBuilderBotModuleCA[] baseBuilders;
		int ticks;
		string lastIdleReason;
		readonly Dictionary<int, int> claimAttempts = new();
		readonly Dictionary<int, int> parkedUntil = new();

		// LC3: the field last handed to an MCV and how many times in a row; fields an MCV's locomotor cannot reach
		// (kept as their own class: a future transport objective, FB1).
		(int Field, int Count) mcvHandout = (-1, 0);
		readonly HashSet<int> landUnreachableLogged = new();

		// BZ_production_scaling (MCV-EXPAND-IDLE H1): deployability is checked once per field while it is
		// parked — the yard profile per MCV type (transform target + footprint + offset) and the union of
		// the engine manager's deploy annuli, both resolved lazily.
		readonly HashSet<int> deployRejectedLogged = new();
		readonly Dictionary<string, (ActorInfo Info, BuildingInfo Building, CVec Offset)?> deployProfiles = new();
		(int Min, int Max)? deployAnnulus;
		IPathFinder pathFinder;
		Actor self;
		(int Field, int Refineries) wanting = (-1, 0);
		bool wantsRefinery;
		FieldScore? claimField;

		// FE-1 (§12.24): the anchors and the claim of the last re-plan, own-building cells for the spread factor, and the
		// MCV sites handed out whose MCV is still on the way (actor id -> site).
		List<CPos> anchors = new();
		int unservedInReach;
		CPos? anchorClaim;
		CPos? anchorClaimFieldCenter;
		(CPos? Anchor, int Replans, int Refineries) anchorStuck = (null, 0, 0);

		// REF-1 (§12.24 v2): the field model — connected components of the map's initial valuable resource cells, built
		// once — the field id of every anchor, the fields' cells indexed by field id (synthetic single-spreader fields
		// have none), the claimable anchors of the last re-plan in claim order, and the pending commits (a produced
		// refinery bound to an anchor that has not landed yet). Refinery cells and building TILES of the last re-plan
		// (the frontier IsCloseEnoughToBase measures against, not the top-lefts) are kept so NextRefineryClaim
		// re-ranks live at placement time.
		List<RefineryField> fields;
		int[] anchorFieldIds = Array.Empty<int>();
		List<IReadOnlyCollection<CPos>> fieldCellsById = new();
		int spreaderAnchorCount;
		readonly Dictionary<CPos, int> anchorPendingUntil = new();
		List<CPos> lastRefineryCells = new();
		List<CPos> lastBuildingTiles = new();

		// ECON-A-FIX (R6): anchor holds owned by expansion demands — a demand's refinery takes its anchor
		// off every other claim while the item is still queued. The table is world-free; the probes below
		// wire it to served/parked/pending state.
		readonly RefineryAnchorReservations anchorReservations = new();

		// F1: the anchor model also feeds gate B's anchor-granular taken test — the assignment of the
		// last model build and the tick it ran on (built once per re-plan, before the scores loop).
		int[] lastAssigned = Array.Empty<int>();
		int anchorModelTick = -1;
		int unclaimedAnchorsInReach;
		int unservedFieldsInReach;
		int claimableAnchorsInReach;
		int unservedBeyondReach;

		// REF-1 B1 (§12.24 v2): the planner's own crawl supply — the cheapest crawl-eligible building it wants
		// produced while the target field is out of reach, and the aim refined to the field's resource edge.
		string wantedLinkBuilding;
		bool wantWasMandatory;
		CPos? crawlTargetEdge;

		// TF-1 (bounded optional supply): the episode ledger, the frontier anchor the queue searches around,
		// and the last refusal reason — logged once per change, not per re-plan.
		readonly CrawlLinkAllowance crawlAllowance = new();
		CPos? crawlFrontierAnchor;
		string lastCrawlRefusal;

		// CRAWL-SAFETY (S3): the front ledger and what it publishes per re-plan — the escort request for
		// the squad manager, the scout-interest cell for the scout module, the front-defence want with
		// its atomic in-flight claim for the base builder, and the per-target loss park.
		readonly CrawlFrontSafety frontSafety = new();
		readonly List<BotProtectionRequest> protectionRequests = new(1);
		readonly List<CPos> scoutInterestCells = new(1);
		readonly Dictionary<int, int> crawlParkedUntil = new();
		string wantedFrontDefence;
		CPos? frontDefenceCell;
		(object Owner, string Item)? frontDefenceClaim;
		string safetyRefusal;

		// REF-1 B2: the cheapest buildable provider of a due MCV's missing prerequisite (td_gdi: the repair
		// facility). REF-1 B3: the last refinery estimate seen, kept so a transiently unbuildable refinery no
		// longer silences Target, the anchor claim and RequestMcv.
		string wantedMcvPrerequisite;
		(ActorInfo Info, int Cost, int BuildTicks) lastRefineryEstimate;

		// REPAIR-B3 (lead ruling 2026-10-10): the park records the owner whose placement failed —
		// the cooldown binds that demand's re-reserve only; a foreign demand can bind the freed
		// anchor at once (a contested anchor recycles fast). Null owner = a provider-side park
		// (offer-streak, stuck-anchor) with no failing demand.
		readonly Dictionary<CPos, (int Until, object Owner)> anchorParkedUntil = new();
		readonly Dictionary<uint, CPos> inflightMcvSites = new();
		List<CPos> ownBuildingCells = new();
		List<CPos> ownYardCells = new();

		// REPAIR-B3 (shared coverage, SPEC_2026-10-09): the route-verified coverage model. Directed-leg
		// witnesses are keyed by (dock cell, patch cell, locomotor, leg) and carry the topology version
		// that produced them; a changed blocker cell inside a probed route invalidates that witness while
		// unrelated ones survive the version bump. Per-anchor verdicts ride a persistent cursor — a
		// refresh resumes where the per-tick budget stopped instead of restarting at the first anchor,
		// so an exhausted budget cannot starve the tail of the anchor list.
		readonly Dictionary<(CPos Dock, CPos Patch, string Locomotor, bool Outbound), RefineryRouteWitness> routeWitnesses = new();
		readonly Dictionary<(CPos Dock, CPos Patch, string Locomotor, bool Outbound), List<CPos>> routeWitnessPaths = new();
		readonly Dictionary<CPos, int> anchorOfferStreaks = new();
		RefineryProbeBudget coverageBudget = new(0, 0, 0);
		byte[] anchorCoverageVerdict = Array.Empty<byte>();
		readonly Dictionary<(int Anchor, int Refinery), long> coverageRanks = new();

		// REPAIR-B3 (R2): how far each (anchor, refinery) pair has walked its field's ordered patch
		// cells — the window advances CoveragePatchCellSample cells per refresh and persists across
		// refreshes under the same model version, so "unserved" is only ever declared after the whole
		// candidate set was examined (budget- or window-deferred pairs stay UNKNOWN).
		readonly Dictionary<(int Anchor, int Refinery), (int Version, int Index)> patchProbeProgress = new();
		readonly Dictionary<int, (int Version, CPos Anchor, CPos[] Ordered)> patchCellsCache = new();
		readonly HashSet<(int Anchor, int Refinery)> evaluatedSites = new();
		int coverageVersion;
		int coverageCursor;
		int coveragePendingAnchors;
		int coverageModelSignature;
		int coverageSweepScanned;
		int coverageFirstBudgetDeferred = -1;
		HashSet<CPos> coverageBlockerCells;
		List<(CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled)> lastRefineryDocks = new();
		(Locomotor Locomotor, BitSet<DockType> DockType, MobileInfo Mobile)[] harvesterSpecs;

		// BEV: the base centre the base builder publishes (the parent BevManagerBotModule used the same signal), and the
		// construction MCVs of every MCV module on this player (Info-level: fixed for the match, safe to cache).
		CPos? baseCenter;
		readonly HashSet<string> constructionMcvTypes;
		readonly HashSet<string> constructionYardTypes;
		readonly Dictionary<string, McvRole> mcvRoles = new();
		IBotRequestUnitProduction[] unitBuilders;
		IBotUtilityAxes[] utilityAxesProviders;
		IBotScaleTargets[] scaleTargetProviders;

		public ExpansionPlannerBotModule(Actor self, ExpansionPlannerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			constructionMcvTypes = self.Info.TraitInfos<McvExpansionManagerBotModuleInfo>()
				.SelectMany(i => i.ConstructionMcvTypes).ToHashSet();
			constructionYardTypes = self.Info.TraitInfos<McvExpansionManagerBotModuleInfo>()
				.SelectMany(i => i.ConstructionYardTypes).ToHashSet();
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation) { baseCenter = newLocation; }

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		/// <summary>BEV's rule, free of world state so it can be tested.</summary>
		public static McvRole ClassifyMcv(bool constructionMcv, bool deploysIntoRefinery) =>
			constructionMcv ? McvRole.Expansion : deploysIntoRefinery ? McvRole.FieldRefinery : McvRole.BaseBuilding;

		McvRole RoleOf(ActorInfo vehicle)
		{
			if (mcvRoles.TryGetValue(vehicle.Name, out var role))
				return role;

			var into = vehicle.TraitInfos<TransformsInfo>().FirstOrDefault()?.IntoActor;
			var intoInfo = into != null && world.Map.Rules.Actors.TryGetValue(into, out var ai) ? ai : null;
			role = ClassifyMcv(constructionMcvTypes.Contains(vehicle.Name), intoInfo?.HasTraitInfo<RefineryInfo>() ?? false);
			mcvRoles[vehicle.Name] = role;
			return role;
		}

		/// <summary>The best field of the last re-plan; null when there is none (EX-1 reads it).</summary>
		public FieldScore? Target { get; private set; }

		public IReadOnlyList<FieldScore> LastScores { get; private set; } = Array.Empty<FieldScore>();

		/// <summary>Telemetry only (12.24 FE-0): the last field handed to an MCV as its site. Never read by a decision.</summary>
		public CPos? LastMcvSite { get; private set; }

		CPos? IBotExpansionTargetProvider.ExpansionTarget => IsTraitDisabled || !Info.DriveBaseCrawl ? null : Target?.Center;

		bool IBotExpansionTargetProvider.WantsRefineryAtExpansionTarget => !IsTraitDisabled && Info.DriveRefineries && wantsRefinery;

		int IBotExpansionTargetProvider.ExpansionTargetClaimRadius => Info.ClaimRadiusCells;

		CPos? IBotExpansionTargetProvider.RefineryClaimTarget => IsTraitDisabled || !Info.DriveRefineries ? null
			: LawActive ? anchorClaim : claimField?.Center;

		bool LawActive => !IsTraitDisabled && Info.FieldCoverage && Info.DriveRefineries && anchors.Count > 0;

		bool IBotExpansionTargetProvider.RefineryLawActive => LawActive;

		int IBotExpansionTargetProvider.RefineryAnchorCount => LawActive ? anchors.Count : 0;

		int IBotExpansionTargetProvider.UnservedAnchorsInReach => LawActive ? unservedInReach : 0;

		CPos? IBotExpansionTargetProvider.RefineryClaimFieldCenter => LawActive ? anchorClaimFieldCenter : null;

		// REF-1 (§12.24 v2): the claim surface — the queue takes a full claim (anchor, field, tier, resource cells),
		// commits the anchor when the placement order issues, and gates production on the claimable count.
		RefineryAnchorClaim? IBotExpansionTargetProvider.NextRefineryClaim(CPos? near)
		{
			return LawActive ? ComputeClaim(near) : null;
		}

		void IBotExpansionTargetProvider.RefineryClaimCommitted(CPos anchor) => CommitRefineryClaim(anchor, anchor);

		void IBotExpansionTargetProvider.RefineryClaimCommitted(CPos anchor, CPos site) => CommitRefineryClaim(anchor, site);

		void CommitRefineryClaim(CPos anchor, CPos site)
		{
			if (!LawActive)
				return;

			var tick = world.WorldTick;
			var until = tick + Info.AnchorClaimPendingTicks;
			anchorPendingUntil[anchor] = until;
			anchorOfferStreaks.Remove(anchor);

			// ECON-A-FIX (R6): a commit is ground truth — the demand's hold (if any) promoted to the
			// pending commit; the demand releases its side when the binding unwinds anyway.
			anchorReservations.Clear(anchor);

			// REPAIR-B3 (SPEC §3): the placed refinery covers every anchor inside its serve radius —
			// mark the whole unserved/unresolved set pending atomically at commit so a second queue
			// cannot admit a duplicate refinery for a sibling while this one is still in flight. Each
			// member's pending clears exactly when the route oracle verifies it covered, or on the
			// pending expiry if the site's docks never reach it — a false mark self-heals.
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			for (var a = 0; a < anchors.Count && a < anchorCoverageVerdict.Length; a++)
			{
				if (anchors[a] == anchor
					|| anchorCoverageVerdict[a] == (byte)RefineryCoverageVerdict.Covered
					|| !RefineryCoverageOracle.WithinServeRadius(site, anchors[a], serve))
					continue;

				anchorPendingUntil[anchors[a]] = until;
				anchorOfferStreaks.Remove(anchors[a]);
				anchorReservations.Clear(anchors[a]);
			}
		}

		void IBotExpansionTargetProvider.RefineryClaimPlacementFailed(CPos anchor)
		{
			if (LawActive)
				ParkFailedClaim(anchor, world.WorldTick, "placement failed (queue signal)");
		}

		void IBotExpansionTargetProvider.RefineryClaimPlacementFailed(CPos anchor, object owner)
		{
			if (LawActive)
				ParkFailedClaim(anchor, world.WorldTick, "placement failed (queue signal)", owner);
		}

		// ECON-A-FIX (R6): the claim surface's three states, evaluated uniformly for the claim order and
		// the reservation probes — blocked: off the market right now; committed: the field counts as
		// covered (a pending commit or a live reservation); taken: ground truth owns it (served, parked
		// or pending-committed) — a reservation cannot attach to or survive a taken anchor.
		bool AnchorBlockedForClaims(int i, int tick)
		{
			var anchor = anchors[i];
			return (anchorParkedUntil.TryGetValue(anchor, out var park) && tick < park.Until)
				|| (anchorPendingUntil.TryGetValue(anchor, out var pend) && tick < pend)
				|| anchorReservations.LiveAt(anchor, tick);
		}

		bool AnchorCommittedForClaims(int i, int tick)
		{
			var anchor = anchors[i];
			return (anchorPendingUntil.TryGetValue(anchor, out var pend) && tick < pend)
				|| anchorReservations.LiveAt(anchor, tick);
		}

		bool AnchorTaken(CPos anchor, int tick)
		{
			var i = anchors.IndexOf(anchor);
			return (i >= 0 && lastAssigned[i] >= 0)
				|| (anchorParkedUntil.TryGetValue(anchor, out var park) && tick < park.Until)
				|| (anchorPendingUntil.TryGetValue(anchor, out var pend) && tick < pend);
		}

		// REPAIR-B3 (lead ruling 2026-10-10): the reservation-channel taken probe — the fail
		// cooldown binds only the owner that failed, so a freed contested anchor re-binds to a
		// foreign demand immediately while the parked demand's own re-reserve waits it out.
		// Null-owner parks stay GLOBAL (VP rereview 560a413): the offer-channel churn bounds —
		// uncommitted-offer streaks and ParkTicks — carry no owner and must refuse every
		// demand's reservation exactly as the pre-scoping AnchorTaken did. The offer channel
		// itself stays globally parked (AnchorTaken above), which is what bounds the re-offer
		// churn.
		bool AnchorTakenForOwner(CPos anchor, int tick, object owner)
		{
			var i = anchors.IndexOf(anchor);
			return (i >= 0 && lastAssigned[i] >= 0)
				|| (anchorPendingUntil.TryGetValue(anchor, out var pend) && tick < pend)
				|| (anchorParkedUntil.TryGetValue(anchor, out var park) && tick < park.Until
					&& (park.Owner == null || ReferenceEquals(park.Owner, owner)));
		}

		// Park-agnostic "taken" for the reservation sweep: a hold on a foreign-parked anchor is
		// still valid (it can activate for its owner during the cooldown), so only ground truth —
		// served or pending-committed — retires a live hold.
		bool AnchorServedOrPending(CPos anchor, int tick)
		{
			var i = anchors.IndexOf(anchor);
			return (i >= 0 && lastAssigned[i] >= 0)
				|| (anchorPendingUntil.TryGetValue(anchor, out var pend) && tick < pend);
		}

		// ECON-A-FIX (R6): the demand's own claim — ranked from the outpost with its future yard
		// footprint counting as part of the frontier, so an expansion beyond today's buildable reach
		// still reserves its anchor (the reach-only quota must not gate the field the yard opens).
		RefineryAnchorClaim? IBotExpansionTargetProvider.DemandRefineryClaim(CPos near, IReadOnlyCollection<CPos> futureProviderTiles)
		{
			if (!LawActive)
				return null;

			var tick = world.WorldTick;
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			var tiles = futureProviderTiles == null || futureProviderTiles.Count == 0
				? lastBuildingTiles
				: lastBuildingTiles.Concat(futureProviderTiles).ToList();
			var order = ClaimOrder(anchors, anchorFieldIds, fieldCellsById, lastRefineryCells, tiles,
				serve, Info.ReachCells,
				i => AnchorBlockedForClaims(i, tick),
				i => AnchorCommittedForClaims(i, tick),
				near, out _, out _, out var tiers, out _, out _,
				RouteRank, CoverageUnknown);
			if (order.Count == 0)
				return null;

			var best = order[0];
			var fieldId = anchorFieldIds[best];
			var center = fieldId < fields.Count ? fields[fieldId].Center : anchors[best];
			return new RefineryAnchorClaim(anchors[best], center, fieldId, tiers[best], fieldCellsById[fieldId]);
		}

		bool IBotExpansionTargetProvider.TryReserveRefineryAnchor(CPos anchor, object owner, int untilTick)
		{
			var tick = world.WorldTick;
			return LawActive && anchorReservations.TryReserve(anchor, owner, tick, untilTick, a => AnchorTakenForOwner(a, tick, owner));
		}

		bool IBotExpansionTargetProvider.RefineryAnchorReserved(CPos anchor, object owner) =>
			LawActive && anchorReservations.LiveFor(anchor, owner, world.WorldTick)
				&& !AnchorTakenForOwner(anchor, world.WorldTick, owner);

		void IBotExpansionTargetProvider.ReleaseRefineryAnchor(CPos anchor, object owner) =>
			anchorReservations.Release(anchor, owner);

		// REPAIR-B3: the atomic set surface — a queued refinery's covered anchors bind as one
		// transaction (SPEC §3), so two parallel building queues can never split the set and each
		// admit a duplicate for a different half.
		bool IBotExpansionTargetProvider.TryReserveRefineryAnchors(CPos site, IReadOnlyCollection<CPos> set, object owner, int untilTick, int modelVersion)
		{
			var tick = world.WorldTick;
			if (!LawActive || (modelVersion >= 0 && modelVersion != coverageVersion))
				return false;

			// The table refreshes an existing owner's hold without consulting its taken probe.
			// Revalidate the provider park/pending/served state on renewal as well as admission.
			if (set.Any(a => AnchorTakenForOwner(a, tick, owner)))
				return false;

			var ok = anchorReservations.TryReserveAll(set, owner, tick, untilTick, a => AnchorTakenForOwner(a, tick, owner));
			Log.Write("debug", $"AI ({player.ClientIndex}): REPAIR-B3 reserve {set.Count} anchors for site {site} v{modelVersion}: {(ok ? "committed" : "refused")} at tick {tick}");
			return ok;
		}

		int IBotExpansionTargetProvider.RefineryCoverageModelVersion => LawActive ? coverageVersion : -1;

		int IBotExpansionTargetProvider.ReleaseRefineryAnchors(IReadOnlyCollection<CPos> set, object owner) =>
			anchorReservations.ReleaseAll(set, owner);

		int IBotExpansionTargetProvider.ReleaseRefineryAnchors(object owner) =>
			anchorReservations.ReleaseAllForOwner(owner);

		bool IBotExpansionTargetProvider.RefineryAnchorsReserved(IReadOnlyCollection<CPos> set, object owner)
		{
			var tick = world.WorldTick;
			return LawActive && anchorReservations.LiveSetFor(set, owner, tick) && !set.Any(a => AnchorTakenForOwner(a, tick, owner));
		}

		int IBotExpansionTargetProvider.RefineryCoveragePendingAnchors => LawActive ? coveragePendingAnchors : 0;

		IReadOnlyList<CPos> IBotExpansionTargetProvider.RefineryClaimCoveredAnchors(CPos anchor)
		{
			if (!LawActive)
				return Array.Empty<CPos>();

			var tick = world.WorldTick;
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			var set = new List<CPos>();
			for (var a = 0; a < anchors.Count; a++)
			{
				if ((a < anchorCoverageVerdict.Length && anchorCoverageVerdict[a] == (byte)RefineryCoverageVerdict.Covered)
					|| AnchorTaken(anchors[a], tick)
					|| !RefineryCoverageOracle.WithinServeRadius(anchor, anchors[a], serve))
					continue;

				set.Add(anchors[a]);
			}

			return set;
		}

		int IBotExpansionTargetProvider.UnclaimedAnchorsInReach => LawActive ? unclaimedAnchorsInReach : 0;

		// REF-1 B1/B2/B4 (§12.24 v2): the planner's crawl-supply want, the refined edge aim, the due MCV's
		// missing prerequisite, and the expansion-nudge metric — published only while the law runs, so classic
		// and switch-off see the interface defaults (unchanged behaviour).
		string IBotExpansionTargetProvider.WantedLinkBuilding =>
			LawActive && Info.DriveBaseCrawl ? wantedLinkBuilding : null;

		CPos? IBotExpansionTargetProvider.CrawlTargetEdge =>
			LawActive && Info.DriveBaseCrawl ? crawlTargetEdge : null;

		// TF-1: the frontier anchor a bounded placement searches around (its own building cell nearest
		// the aim) and the queue's atomic admission gate — the first queue through claims the pending
		// link, a second queue in the same window is refused. Both ride the interface defaults
		// (null / admit) under classic and the switch-off path.
		CPos? IBotExpansionTargetProvider.CrawlFrontierAnchor =>
			LawActive && Info.DriveBaseCrawl && Info.BoundCrawlLinkSupply ? crawlFrontierAnchor : null;

		bool IBotExpansionTargetProvider.TryClaimCrawlLink(object owner, string item, int cost)
		{
			if (!LawActive || !Info.DriveBaseCrawl || !Info.BoundCrawlLinkSupply || item != wantedLinkBuilding)
				return true;

			return crawlAllowance.TryClaim(owner, item, cost, wantWasMandatory,
				Info.OptionalLinkCostAllowance, Info.OptionalLinkCountAllowance);
		}

		// CRAWL-SAFETY (S3): the front-defence want, its aim cell (the frontier anchor) and the same
		// single-in-flight admission the crawl link uses — the first queue through claims it, a second
		// queue in the same window is refused. All ride the interface defaults (null / admit) under
		// classic and switch-off.
		string IBotExpansionTargetProvider.WantedFrontDefence =>
			LawActive && Info.DriveBaseCrawl && Info.UseCrawlSafety ? wantedFrontDefence : null;

		CPos? IBotExpansionTargetProvider.FrontDefenceCell =>
			LawActive && Info.DriveBaseCrawl && Info.UseCrawlSafety ? frontDefenceCell : null;

		bool IBotExpansionTargetProvider.TryClaimFrontDefence(object owner, string item, int cost)
		{
			if (item != wantedFrontDefence)
				return true;

			if (frontDefenceClaim is { } claim)
				return ReferenceEquals(claim.Owner, owner);

			frontDefenceClaim = (owner, item);
			return true;
		}

		// CRAWL-SAFETY (S3): the escort request for the squad manager and the scout-interest cell for
		// the scout module — re-published each re-plan while the front needs them, masked off-law like
		// the other provider surfaces (a lapsed law must not leave a stale front guarded or scouted).
		IReadOnlyList<BotProtectionRequest> IBotProtectionRequestProvider.ProtectionRequests =>
			LawActive && Info.DriveBaseCrawl && Info.UseCrawlSafety ? protectionRequests : Array.Empty<BotProtectionRequest>();

		IReadOnlyList<CPos> IBotScoutInterestProvider.ScoutInterestCells =>
			LawActive && Info.DriveBaseCrawl && Info.UseCrawlSafety ? scoutInterestCells : Array.Empty<CPos>();

		string IBotExpansionTargetProvider.WantedMcvPrerequisite =>
			LawActive && Info.DriveMcvRequests ? wantedMcvPrerequisite : null;

		int IBotExpansionTargetProvider.UnservedAnchorsBeyondReach => LawActive ? unservedBeyondReach : 0;

		// REF-1: the field model for the telemetry (same assembly, record-only reads) — the provider's model so the
		// logged field ids are exactly the ones the law claimed with. Null until the first re-plan builds it.
		internal IReadOnlyList<RefineryField> ComponentFields => fields;
		internal IReadOnlyList<CPos> AnchorCells => anchors;
		internal IReadOnlyList<int> AnchorFieldIds => anchorFieldIds;
		internal IReadOnlyList<IReadOnlyCollection<CPos>> FieldCellsById => fieldCellsById;
		internal int SpreaderAnchorCount => spreaderAnchorCount;

		/// <summary>
		/// TC-3 (§12.18): the claim that wants a bodyguard — the current target field while its
		/// evaluated threat is non-trivial. Threat is the remembered-enemy sum the region threat
		/// providers publish at the field's centre (the same term Safety() folds into the score),
		/// so "contested" is the fog-honest memory, not a live peek. An uncontested target needs
		/// no escort and no target publishes nothing — null, never a behaviour change.
		/// </summary>
		WPos? IBotExpansionAssistProvider.ExpansionAssistTarget
		{
			get
			{
				if (IsTraitDisabled || !(Target is FieldScore target) || target.Threat <= 0)
					return null;

				return world.Map.CenterOfCell(target.Center);
			}
		}

		/// <summary>EX-2: a field counts as ours when an own refinery stands within the claim radius of its resource centre.</summary>
		public static bool Claimed(CPos center, IEnumerable<CPos> refineries, int claimRadiusCells)
		{
			return refineries.Any(r => (r - center).LengthSquared <= claimRadiusCells * claimRadiusCells);
		}

		/// <summary>
		/// EX-2 loop guard: while we want a refinery at `field`, every refinery we gain without the field turning ours is a
		/// missed claim. Returns the updated state and whether the field must now be parked.
		/// </summary>
		public static ((int Field, int Refineries) Wanting, int Attempts, bool Park) TrackClaim(
			(int Field, int Refineries) wanting, int field, int refineries, int attempts, int maxAttempts)
		{
			if (wanting.Field != field)
				return ((field, refineries), attempts, false);

			if (refineries > wanting.Refineries)
				attempts += refineries - wanting.Refineries;

			return ((field, refineries), attempts, attempts >= maxAttempts);
		}

		protected override void TraitEnabled(Actor self)
		{
			this.self = self;
			pathFinder = self.World.WorldActor.TraitOrDefault<IPathFinder>();
			resourceMap = self.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());
			resources = self.TraitOrDefault<PlayerResources>();
			threatProviders = self.TraitsImplementing<IBotRegionThreatProvider>().ToArray();
			baseBuilders = self.TraitsImplementing<BaseBuilderBotModuleCA>().ToArray();
			unitBuilders = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			utilityAxesProviders = self.TraitsImplementing<IBotUtilityAxes>().ToArray();
			scaleTargetProviders = self.TraitsImplementing<IBotScaleTargets>().ToArray();
		}

		/// <summary>
		/// §12.13: score = V x S / (T + tau), S = 1 / (1 + threat / max(guard, 1)),
		/// T = cost / income + hops x link build time + refinery build time. Pure, for the tests.
		/// </summary>
		public static double Score(int value, int threat, int guard, int costCredits, double incomePerTick,
			int hops, int linkBuildTicks, int refineryBuildTicks, int tauTicks, out int paybackTicks)
		{
			// No income yet: every credit costs a tick, so fields still rank by cost, distance and value.
			var costTicks = incomePerTick > 0 ? costCredits / incomePerTick : costCredits;
			paybackTicks = (int)Math.Min(int.MaxValue, costTicks + (long)hops * linkBuildTicks + refineryBuildTicks);
			var safety = Safety(threat, guard);
			return value * safety / (paybackTicks + Math.Max(tauTicks, 1));
		}

		public static double Safety(int threat, int guard) => 1.0 / (1.0 + threat / (double)Math.Max(guard, 1));

		/// <summary>
		/// EX-3: the MCV site among the far fields (at least `minHops` links away): value x safety / (distance from the
		/// MCV + tau). Nearer fields are the building line's job (EX-1/EX-2). Null when no far field is free.
		/// </summary>
		public static FieldScore? McvSite(IEnumerable<FieldScore> fields, CPos mcv, int minHops, int tauCells,
			Func<FieldScore, bool> eligible = null, Func<FieldScore, double> weight = null)
		{
			FieldScore? best = null;
			var bestScore = double.MinValue;
			foreach (var f in fields)
			{
				if (f.Hops < minHops || (eligible != null && !eligible(f)))
					continue;

				var score = f.Value * f.Safety / ((f.Center - mcv).Length + Math.Max(tauCells, 1));

				// FE-1: separation x spread (null = master behaviour, bit-identical).
				if (weight != null)
					score *= weight(f);

				if (score > bestScore)
				{
					bestScore = score;
					best = f;
				}
			}

			return best;
		}

		// ---- FE-1 (§12.24): pure helpers, free of world state so they can be tested ----

		/// <summary>
		/// FE-1: the anchors - every resource spreader (deduplicated, input order), then the centre of every field with no
		/// spreader within `spreaderRadiusCells` of it. One refinery per anchor.
		/// </summary>
		public static List<CPos> BuildAnchors(IEnumerable<CPos> spreaders, IEnumerable<CPos> fieldCenters, int spreaderRadiusCells)
		{
			var result = new List<CPos>();
			foreach (var s in spreaders)
				if (!result.Contains(s))
					result.Add(s);

			var spreaderCount = result.Count;
			var r2 = (long)spreaderRadiusCells * spreaderRadiusCells;
			foreach (var f in fieldCenters)
			{
				var covered = false;
				for (var i = 0; i < spreaderCount && !covered; i++)
					covered = (result[i] - f).LengthSquared <= r2;

				if (!covered && !result.Contains(f))
					result.Add(f);
			}

			return result;
		}

		/// <summary>
		/// FE-1: which refinery serves which anchor - a result per anchor, the refinery index or -1. Every
		/// refinery may serve EVERY anchor it reaches (REPAIR-B3 shared coverage, SPEC_2026-10-09 §2 —
		/// the retired 1:1 binding counted a cluster of spreaders beside one refinery as starving while the
		/// harvesters all docked there). Per anchor the best candidate wins by route rank (when supplied),
		/// then squared distance, then refinery index — deterministic across clients.
		/// R1 (SPEC §30 "a far same-field anchor does not inherit coverage"): the retired flush exception
		/// is gone — eligibility is the geometric radius from the refinery's own location, nothing else;
		/// the route oracle still gates whether in-radius coverage is actually usable.
		/// REPAIR-B3: <paramref name="routeRank"/> is the coverage oracle — (anchor, refinery) -&gt; the
		/// route-verified round-trip travel rank in milli-ticks, or null while unproven (UNKNOWN defers
		/// the anchor rather than counting it either way). Null oracle = pure geometric assignment.
		/// </summary>
		public static int[] AssignRefineries(IReadOnlyList<CPos> anchorCells, IReadOnlyList<CPos> refineries, int serveRadiusCells,
			Func<int, int, long?> routeRank = null)
		{
			var result = new int[anchorCells.Count];
			var r2 = (long)serveRadiusCells * serveRadiusCells;
			for (var a = 0; a < anchorCells.Count; a++)
			{
				var best = -1;
				var bestRank = long.MaxValue;
				var bestD = long.MaxValue;
				for (var r = 0; r < refineries.Count; r++)
				{
					var d = (anchorCells[a] - refineries[r]).LengthSquared;
					if (d > r2)
						continue;

					var rank = routeRank == null ? 0L : routeRank(a, r) ?? long.MaxValue;
					if (rank == long.MaxValue)
						continue;

					if (rank < bestRank || (rank == bestRank && (d < bestD || (d == bestD && r < best))))
					{
						best = r;
						bestRank = rank;
						bestD = d;
					}
				}

				result[a] = best;
			}

			return result;
		}

		/// <summary>
		/// REF-1 (v2): the field each refinery sits flush to — the field id whose resource cells come within
		/// Chebyshev 2 of a refinery tile (the law's own placement bound: footprint within gap 1 of the field),
		/// -1 when none. Distances tie-break to the lowest field id, matching the fields' own ordering.
		/// </summary>
		public static int[] RefineryFlushFields(IReadOnlyList<IReadOnlyCollection<CPos>> refineryTiles,
			IReadOnlyList<IReadOnlyCollection<CPos>> fieldCells)
		{
			var result = new int[refineryTiles.Count];
			for (var r = 0; r < refineryTiles.Count; r++)
			{
				result[r] = -1;
				var tiles = refineryTiles[r];
				if (tiles == null)
					continue;

				var best = -1;
				var bestD = int.MaxValue;
				for (var f = 0; f < fieldCells.Count; f++)
				{
					var cells = fieldCells[f];
					if (cells == null || cells.Count == 0)
						continue;

					foreach (var t in tiles)
						foreach (var c in cells)
						{
							var d = Math.Max(Math.Abs(t.X - c.X), Math.Abs(t.Y - c.Y));
							if (d < bestD)
							{
								bestD = d;
								best = f;
							}
						}
				}

				if (bestD <= 2)
					result[r] = best;
			}

			return result;
		}

		/// <summary>
		/// REF-1 (§12.24 v2): one FIELD = one 8-connected component of valuable resource cells. Deterministic: each
		/// component's cells are sorted (X then Y) and the components are ordered by their smallest cell, so a field's
		/// id — its position in the result — is stable for the whole match.
		/// </summary>
		public static List<List<CPos>> ResourceFields(IEnumerable<CPos> resourceCells)
		{
			var remaining = new HashSet<CPos>(resourceCells);
			var fields = new List<List<CPos>>();
			while (remaining.Count > 0)
			{
				var seed = remaining.OrderBy(c => c.X).ThenBy(c => c.Y).First();
				var cells = new List<CPos>();
				var queue = new Queue<CPos>();
				queue.Enqueue(seed);
				remaining.Remove(seed);
				while (queue.Count > 0)
				{
					var c = queue.Dequeue();
					cells.Add(c);
					for (var dy = -1; dy <= 1; dy++)
						for (var dx = -1; dx <= 1; dx++)
						{
							if (dx == 0 && dy == 0)
								continue;

							var n = new CPos(c.X + dx, c.Y + dy);
							if (remaining.Remove(n))
								queue.Enqueue(n);
						}
				}

				cells.Sort((a, b) => a.X != b.X ? a.X - b.X : a.Y - b.Y);
				fields.Add(cells);
			}

			fields.Sort((a, b) => a[0].X != b[0].X ? a[0].X - b[0].X : a[0].Y - b[0].Y);
			return fields;
		}

		/// <summary>
		/// REF-1: the field id of every anchor. The first <paramref name="spreaderCount"/> anchors are spreaders — each maps to
		/// the field whose nearest valuable cell lies within <paramref name="mergeRadiusCells"/> of it; later anchors are the
		/// centres of spreaderless fields and map back to their own field. A spreader with no field in range gets a synthetic
		/// single-anchor field (id &gt;= fieldCells.Count, assigned in anchor order) — a field that keeps its own anchor.
		/// </summary>
		public static int[] AssignAnchorFields(IReadOnlyList<CPos> anchorCells, int spreaderCount,
			IReadOnlyList<IReadOnlyCollection<CPos>> fieldCells, IReadOnlyList<CPos> fieldCenters, int mergeRadiusCells)
		{
			var result = new int[anchorCells.Count];
			var centerToField = new Dictionary<CPos, int>();
			for (var f = 0; f < fieldCenters.Count; f++)
				if (!centerToField.ContainsKey(fieldCenters[f]))
					centerToField[fieldCenters[f]] = f;

			var r2 = (long)mergeRadiusCells * mergeRadiusCells;
			var orphan = 0;
			for (var a = 0; a < anchorCells.Count; a++)
			{
				if (a >= spreaderCount && centerToField.TryGetValue(anchorCells[a], out var fieldId))
				{
					result[a] = fieldId;
					continue;
				}

				var best = -1;
				long bestD = long.MaxValue;
				for (var f = 0; f < fieldCells.Count; f++)
				{
					var cells = fieldCells[f];
					if (cells == null || cells.Count == 0)
						continue;

					foreach (var c in cells)
					{
						var d = (anchorCells[a] - c).LengthSquared;
						if (d < bestD)
						{
							bestD = d;
							best = f;
						}
					}
				}

				result[a] = best >= 0 && bestD <= r2 ? best : fieldCells.Count + orphan++;
			}

			return result;
		}

		/// <summary>
		/// REF-1: an anchor's coverage of its field — the count of the field's resource cells nearest to it among the
		/// field's own anchors. The first refinery of a field goes to the spreader covering the most cells.
		/// </summary>
		static int AnchorCoverage(int anchor, IEnumerable<int> fieldAnchors, IReadOnlyCollection<CPos> cells, IReadOnlyList<CPos> anchorCells)
		{
			if (cells == null || cells.Count == 0)
				return 0;

			var mates = fieldAnchors.ToArray();
			var coverage = 0;
			foreach (var c in cells)
			{
				var nearest = mates[0];
				var bestD = (anchorCells[nearest] - c).LengthSquared;
				foreach (var m in mates)
				{
					var d = (anchorCells[m] - c).LengthSquared;
					if (d < bestD || (d == bestD && m < nearest))
					{
						bestD = d;
						nearest = m;
					}
				}

				if (nearest == anchor)
					coverage++;
			}

			return coverage;
		}

		/// <summary>
		/// REF-1 (§12.24 v2) refinery-claim order: the anchors the next refineries should claim, best first — tier 1: the
		/// representative anchor of every field with no refinery yet (of the field's claimable anchors, the one covering
		/// the most of the field's resource cells; ties: nearest our buildings, then lowest index), fields ordered home
		/// first; then tier 2: the still-unserved anchors of covered fields, each field's anchors farthest from its own
		/// refineries first. <paramref name="blocked"/> excludes parked or pending anchors from candidacy (they still
		/// count in the unserved metrics); <paramref name="committed"/> marks pending anchors — a field with a pending
		/// claim counts as covered, its other spreaders are tier 2. <paramref name="near"/> (the yard of an MCV-requested
		/// refinery) ranks within each tier by distance to it instead of to our buildings.
		/// <paramref name="anchorTier"/> returns each claimable anchor's tier (0 for the rest).
		/// </summary>
		public static List<int> ClaimOrder(
			IReadOnlyList<CPos> anchorCells, IReadOnlyList<int> anchorField, IReadOnlyList<IReadOnlyCollection<CPos>> fieldCells,
			IReadOnlyList<CPos> refineries, IReadOnlyList<CPos> buildingTiles,
			int serveRadiusCells, int reachCells,
			Func<int, bool> blocked, Func<int, bool> committed, CPos? near,
			out int unservedAnchorsInReach, out int unservedFieldsInReach, out int[] anchorTier,
			out int claimableAnchorsInReach, out int unservedAnchorsBeyondReach,
			Func<int, int, long?> routeRank = null, Func<int, bool> coverageUnknown = null)
		{
			unservedAnchorsInReach = 0;
			unservedFieldsInReach = 0;
			claimableAnchorsInReach = 0;
			unservedAnchorsBeyondReach = 0;
			var order = new List<int>();
			var n = anchorCells.Count;
			anchorTier = new int[n];
			if (n == 0)
				return order;

			var assigned = AssignRefineries(anchorCells, refineries, serveRadiusCells, routeRank);
			var anchorsByField = new Dictionary<int, List<int>>();
			for (var a = 0; a < n; a++)
			{
				var field = anchorField[a];
				if (!anchorsByField.TryGetValue(field, out var list))
					anchorsByField[field] = list = new List<int>();
				list.Add(a);
			}

			// Synthetic single-spreader field ids sit beyond fieldCells.Count — the callers' per-id list spans them,
			// but a bare field list does not; size lookups by the highest id used and give synthetic fields no cells.
			var fieldCount = fieldCells.Count;
			foreach (var f in anchorField)
				fieldCount = Math.Max(fieldCount, f + 1);
			IReadOnlyCollection<CPos> CellsOf(int field) => field < fieldCells.Count ? fieldCells[field] : null;

			// buildingTiles are the footprint cells of our GivesBuildableArea buildings — the frontier the placement
			// test (IsCloseEnoughToBase) measures against. An anchor is "in reach" when its FIELD's resource edge is:
			// the law's refinery sits flush to the field, not to the spreader, so a spreader a few cells beyond the
			// frontier is still claimable while its field's edge is in reach (a spreader with no recorded field cells
			// — an orphan — keeps its own cell as the measure).
			var anchorDist = new int[n];
			for (var a = 0; a < n; a++)
			{
				var d = int.MaxValue;
				foreach (var b in buildingTiles)
					d = Math.Min(d, (b - anchorCells[a]).Length);
				anchorDist[a] = d;
			}

			var fieldReach = new int[fieldCount];
			Array.Fill(fieldReach, int.MaxValue);
			for (var f = 0; f < fieldCount; f++)
			{
				var cells = CellsOf(f);
				if (cells == null || cells.Count == 0)
					continue;

				foreach (var b in buildingTiles)
				{
					var d = int.MaxValue;
					foreach (var c in cells)
						d = Math.Min(d, (b - c).Length);
					fieldReach[f] = Math.Min(fieldReach[f], d);
				}
			}

			Func<int, int> rank = near.HasValue
				? a => (anchorCells[a] - near.Value).Length
				: a => anchorDist[a];

			var reachDist = new int[n];
			var served = new bool[n];
			var unknown = new bool[n];
			var fieldCovered = new bool[fieldCount];
			for (var a = 0; a < n; a++)
			{
				served[a] = assigned[a] >= 0;
				unknown[a] = coverageUnknown != null && coverageUnknown(a);
				var field = anchorField[a];
				reachDist[a] = CellsOf(field) is { Count: > 0 }
					? fieldReach[field]
					: anchorDist[a];

				// REPAIR-B3: an UNKNOWN anchor defers — it is neither served nor claimable nor
				// backlog until the route oracle resolves it within budget.
				var inReach = reachDist[a] <= reachCells;
				if (!served[a] && !unknown[a] && inReach)
					unservedAnchorsInReach++;
				if (!served[a] && !unknown[a] && !inReach)
					unservedAnchorsBeyondReach++;
				if (served[a] || (committed != null && committed(a)))
					fieldCovered[field] = true;
			}

			// Tier-1 backlog metric: a field in reach with no serving and no pending refinery still waits for its first
			// one — parked anchors count too (the backlog is honest even while it cannot be claimed right now).
			foreach (var kv in anchorsByField)
				if (!fieldCovered[kv.Key] && kv.Value.Any(a => !served[a] && !unknown[a] && reachDist[a] <= reachCells))
					unservedFieldsInReach++;

			Func<int, bool> claimable = a => !served[a] && !unknown[a] && reachDist[a] <= reachCells && (blocked == null || !blocked(a));
			for (var a = 0; a < n; a++)
				if (claimable(a))
					claimableAnchorsInReach++;

			// Tier 1: one representative per field that has no refinery yet — the claimable anchor covering the most of
			// the field's cells (ties: rank, then lowest index). Fields ordered by their rep's rank, then field id.
			var tier1 = new List<(int Anchor, int Rank, int Field)>();
			foreach (var kv in anchorsByField.OrderBy(kv => kv.Key))
			{
				if (fieldCovered[kv.Key])
					continue;

				var rep = -1;
				var bestCoverage = -1;
				var bestRank = int.MaxValue;
				foreach (var a in kv.Value.Where(claimable))
				{
					var coverage = AnchorCoverage(a, kv.Value, CellsOf(kv.Key), anchorCells);
					var r = rank(a);
					if (coverage > bestCoverage || (coverage == bestCoverage && r < bestRank))
					{
						rep = a;
						bestCoverage = coverage;
						bestRank = r;
					}
				}

				if (rep >= 0)
				{
					anchorTier[rep] = 1;
					tier1.Add((rep, bestRank, kv.Key));
				}
			}

			order.AddRange(tier1.OrderBy(t => t.Rank).ThenBy(t => t.Field).Select(t => t.Anchor));

			// Tier 2: the still-unserved anchors of covered fields — each field's claimable anchors farthest from the
			// refineries already serving it first; fields ordered by their best anchor's rank, then field id.
			var tier2Fields = new List<(int Rank, int Field, List<int> Anchors)>();
			foreach (var kv in anchorsByField.OrderBy(kv => kv.Key))
			{
				if (!fieldCovered[kv.Key])
					continue;

				var serving = kv.Value.Where(a => assigned[a] >= 0).Select(a => refineries[assigned[a]]).ToList();
				var cand = kv.Value.Where(claimable)
					.OrderByDescending(a => serving.Count == 0 ? 0 : serving.Min(r => (anchorCells[a] - r).LengthSquared))
					.ThenBy(rank)
					.ToList();
				if (cand.Count == 0)
					continue;

				var fieldRank = cand.Min(rank);
				foreach (var a in cand)
					anchorTier[a] = 2;
				tier2Fields.Add((fieldRank, kv.Key, cand));
			}

			foreach (var f in tier2Fields.OrderBy(t => t.Rank).ThenBy(t => t.Field))
				order.AddRange(f.Anchors);

			// The first refinery is never hostage to the reach test: with none built and none committed,
			// claim the unblocked unserved anchor nearest our base (or the requested yard) even beyond reach —
			// placement may still fail and park the anchor, and the next call walks to the next field.
			if (order.Count == 0 && refineries.Count == 0)
			{
				var anyCommitted = false;
				for (var a = 0; a < n; a++)
					if (committed != null && committed(a))
					{
						anyCommitted = true;
						break;
					}

				if (!anyCommitted)
				{
					var first = -1;
					var firstRank = int.MaxValue;
					for (var a = 0; a < n; a++)
					{
						if (served[a] || (blocked != null && blocked(a)))
							continue;

						var r = rank(a);
						if (r < firstRank)
						{
							first = a;
							firstRank = r;
						}
					}

					if (first >= 0)
					{
						anchorTier[first] = 1;
						order.Add(first);
					}
				}
			}

			return order;
		}

		/// <summary>FE-1: the bearing of `to` as seen from `from`, integer (WAngle.ArcTan).</summary>
		public static WAngle Bearing(CPos from, CPos to) => WAngle.ArcTan(to.Y - from.Y, to.X - from.X);

		/// <summary>FE-1: the smaller angle between two bearings, in WAngle units (0..512).</summary>
		public static int BearingDelta(WAngle a, WAngle b)
		{
			var d = Math.Abs(a.Angle - b.Angle) % 1024;
			return d > 512 ? 1024 - d : d;
		}

		/// <summary>
		/// FE-1: the separation factor of a site - `minFactor` when its bearing from `origin` (the main base) lies within
		/// `degrees` of the bearing of any `avoid` position (the crawl target, own yards, other in-flight MCV sites), else 1.
		/// A position closer than `minDistanceCells` to the origin has no usable bearing and is skipped.
		/// </summary>
		public static double SeparationFactor(CPos site, CPos origin, IEnumerable<CPos> avoid, int degrees, double minFactor, int minDistanceCells)
		{
			if ((site - origin).Length < 1)
				return 1;

			var siteBearing = Bearing(origin, site);
			var window = WAngle.FromDegrees(degrees).Angle;
			foreach (var other in avoid)
			{
				if ((other - origin).Length < Math.Max(minDistanceCells, 1))
					continue;

				if (BearingDelta(siteBearing, Bearing(origin, other)) <= window)
					return minFactor;
			}

			return 1;
		}

		/// <summary>FE-1: 1 + bonus x distance-to-our-nearest-building / map diagonal (unexplored ground wins ties).</summary>
		public static double SpreadFactor(int distanceToNearestBuildingCells, int mapDiagonalCells, double bonus)
		{
			return 1 + bonus * Math.Max(distanceToNearestBuildingCells, 0) / Math.Max(mapDiagonalCells, 1);
		}

		/// <summary>FE-1: is the field's centre within `radiusCells` of a site an MCV is already heading to?</summary>
		public static bool TakenByMcvSite(CPos center, IEnumerable<CPos> sites, int radiusCells)
		{
			return sites.Any(s => (s - center).LengthSquared <= (long)radiusCells * radiusCells);
		}

		/// <summary>
		/// EX-2c: the field the next refinery should claim — the best-scoring free field already in buildable
		/// reach (`scores` is score-sorted, parked and claimed fields never reach it). Any in-reach field
		/// qualifies, not only the crawl target: an outpost yard draws its refinery the moment it can place
		/// one. Pure, for the tests.
		/// </summary>
		public static FieldScore? BestClaimField(IReadOnlyList<FieldScore> scores)
		{
			for (var i = 0; i < scores.Count; i++)
				if (scores[i].Hops == 0)
					return scores[i];

			return null;
		}

		/// <summary>
		/// TC-2c: does an allied claim outrank ours on `field`? The lower ClientIndex wins — a stable,
		/// arbitrary precedence both bots compute identically, so a contested field converges instead of
		/// both allies yielding forever. Pure, for the tests.
		/// </summary>
		public static bool AllyClaimWins(WPos field, IEnumerable<(int ClientIndex, WPos Claim)> allyClaims,
			int myClientIndex, int radiusCells)
		{
			var radiusW = 1024L * radiusCells;
			foreach (var (index, claim) in allyClaims)
				if (index < myClientIndex && (field - claim).HorizontalLengthSquared <= radiusW * radiusW)
					return true;

			return false;
		}

		/// <summary>
		/// TC-3 / BD (§12.18): the Voronoi score percent — a field belongs to the coalition sector
		/// anchor nearest it (min HorizontalLengthSquared, the same horizontal measure AllyClaimWins
		/// uses, so terrain height cannot tilt a border). The anchor holding our own participant id
		/// keeps 100, a foreign anchor keeps `foreignPercent`; a distance tie goes to the ordinally
		/// smallest id so every member computes the identical partition. Pure, for the tests.
		/// </summary>
		public static int SectorScorePercent(WPos field, IReadOnlyDictionary<string, WPos> anchors,
			string myId, int foreignPercent)
		{
			if (anchors == null || anchors.Count == 0)
				return 100;

			string nearest = null;
			var best = long.MaxValue;
			foreach (var kv in anchors)
			{
				var d = (kv.Value - field).HorizontalLengthSquared;
				if (d < best || (d == best && string.CompareOrdinal(kv.Key, nearest) < 0))
				{
					best = d;
					nearest = kv.Key;
				}
			}

			return nearest == myId ? 100 : Math.Clamp(foreignPercent, 0, 100);
		}

		/// <summary>LC3: one more hand-out of `field`; returns the new streak and whether the field must now be parked.</summary>
		public static ((int Field, int Count) Streak, bool Park) TrackMcvHandout((int Field, int Count) streak, int field, int maxHandouts)
		{
			var count = streak.Field == field ? streak.Count + 1 : 1;
			if (maxHandouts > 0 && count > maxHandouts)
				return ((-1, 0), true);

			return ((field, count), false);
		}

		/// <summary>
		/// Greedy expansion: an extra construction MCV is worth its queue slot while a field beyond the building
		/// line's reach is free and we still hold the cash reserve. Pure, for the tests.
		/// </summary>
		public static bool ShouldRequestMcv(int cash, int reserve, bool farFieldFree, int activePlusQueued, int targetCount)
		{
			return farFieldFree && cash >= reserve && activePlusQueued < targetCount;
		}

		/// <summary>
		/// REF-1 B2: an MCV request is due when a far field is free and the pipeline has room — cash does NOT gate
		/// the request itself (production is cash-gated at the queue; a standing request rides under the reserve
		/// instead of waiting for a re-plan with high cash). Pure, for the tests.
		/// </summary>
		public static bool McvDue(bool farFieldFree, int activePlusQueued, int targetCount) =>
			farFieldFree && activePlusQueued < targetCount;

		/// <summary>
		/// REF-1 B2: the prerequisite entries of <paramref name="prerequisites"/> currently unmet AND fixable by
		/// building — '~' (any-provider group) and plain entries count; '!' entries are satisfied by absence, so
		/// nothing can build them away. Pure, for the tests.
		/// </summary>
		public static IEnumerable<string> MissingPrerequisiteTokens(IEnumerable<string> prerequisites, Func<string, bool> isMet)
		{
			foreach (var raw in prerequisites)
			{
				var token = raw.Replace("~", string.Empty);
				if (token.StartsWith("!", StringComparison.Ordinal) || isMet(raw))
					continue;

				yield return token;
			}
		}

		/// <summary>
		/// REF-1 B2: a construction MCV is due but no production queue offers it — find its unmet prerequisites
		/// and return the name of the cheapest building an enabled building queue could produce that provides one.
		/// Null = nothing actionable (all prereqs met yet still unproducible, or no buildable provider).
		/// </summary>
		string MissingMcvPrerequisite()
		{
			var techTree = player.PlayerActor.TraitOrDefault<TechTree>();
			if (techTree == null)
				return null;

			var missing = MissingPrerequisiteTokens(
					constructionMcvTypes
						.Select(n => world.Map.Rules.Actors.TryGetValue(n, out var ai) ? ai : null)
						.Where(ai => ai != null)
						.SelectMany(ai => ai.TraitInfos<BuildableInfo>().SelectMany(bi => bi.Prerequisites)),
					raw => techTree.HasPrerequisites(new[] { raw }))
				.ToHashSet();
			if (missing.Count == 0)
				return null;

			// Cheapest provider currently buildable on an enabled building queue — its own prereqs are met, so the
			// want can actually be produced. Ties by name keep the pick deterministic.
			return BuildingQueueTypes()
				.SelectMany(t => CAAIUtils.FindQueues(player, t))
				.Distinct()
				.Where(q => q.Enabled)
				.SelectMany(q => q.BuildableItems())
				.Where(b => b.HasTraitInfo<BuildingInfo>()
					&& b.TraitInfos<ITechTreePrerequisiteInfo>().Any(i => i.Prerequisites(b).Any(missing.Contains)))
				.OrderBy(b => b.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? int.MaxValue)
				.ThenBy(b => b.Name, StringComparer.Ordinal)
				.Select(b => b.Name)
				.FirstOrDefault();
		}

		/// <summary>
		/// UT-4: the effective MCV appetite under the TechRush&lt;-&gt;Expansion axis — Expansion leans add up to
		/// +expansionBonus at the pole, TechRush leans subtract up to -techrushMinus (floor 1: never zero appetite).
		/// Neutral (50) returns the base count verbatim. Pure, for the tests.
		/// </summary>
		public static int EffectiveMcvTargetCount(int baseTarget, int axis, int expansionBonus, int techrushMinus)
		{
			if (axis > IBotUtilityAxes.Neutral)
				return baseTarget + (int)Math.Round((axis - IBotUtilityAxes.Neutral) / (double)IBotUtilityAxes.Neutral * expansionBonus);

			return Math.Max(1, baseTarget - (int)Math.Round((IBotUtilityAxes.Neutral - Math.Max(0, axis)) / (double)IBotUtilityAxes.Neutral * techrushMinus));
		}

		// BZ_production_scaling: difficulty-line bars (identical Scale() call shape as the crawl-safety floor).
		int ScalingMcvIncomeBar => BotDifficultyLadder.Scale(Info.ScalingMcvIncomeEasy, Info.ScalingMcvIncomeHard, player, Info.ScalingMcvIncomeHard);
		int ScalingMcvMaxExtra => BotDifficultyLadder.Scale(Info.ScalingMcvMaxExtraEasy, Info.ScalingMcvMaxExtraHard, player, Info.ScalingMcvMaxExtraHard);
		int ScalingParityFloor => BotDifficultyLadder.Scale(Info.ArmyParityPctEasy, Info.ArmyParityPctHard, player, Info.ArmyParityPctHard);

		/// <summary>Windowed income per tick from the rolling sample queue (0 before the first sample).</summary>
		double CurrentIncomePerTick()
		{
			var first = income.Count > 0 ? income.Peek() : (Tick: world.WorldTick, Earned: resources.Earned);
			var span = Math.Max(1, world.WorldTick - first.Tick);
			return (resources.Earned - first.Earned) / (double)span;
		}

		void RequestMcv(IBot bot)
		{
			wantedMcvPrerequisite = null;
			if (unitBuilders == null || resources == null)
				return;

			// A construction MCV pays only if the building line cannot reach a free field soon.
			var farFieldFree = LastScores.Any(f => f.Hops >= Info.McvMinHops
				&& !(parkedUntil.TryGetValue(f.Index, out var until) && world.WorldTick < until));

			// Yards + construction MCVs in the field + MCVs already in a queue (factory-level and player-level).
			var active = world.Actors.Count(a => a.Owner == player && !a.IsDead
				&& (constructionMcvTypes.Contains(a.Info.Name) || constructionYardTypes.Contains(a.Info.Name)));
			var queued = world.ActorsWithTrait<ProductionQueue>()
				.Where(tp => tp.Actor.Owner == player && tp.Trait.Enabled)
				.SelectMany(tp => tp.Trait.AllQueued())
				.Concat(player.PlayerActor.TraitsImplementing<ProductionQueue>()
					.Where(t => t.Enabled)
					.SelectMany(t => t.AllQueued()))
				.Count(q => constructionMcvTypes.Contains(q.Item));

			// UT-4: the TechRush<->Expansion axis scales the appetite — Expansion personalities keep more
			// MCVs flowing, TechRush ones hold back (the engine's own gates still apply on top either way).
			// Scale targets (DESIGN 19.10): with an enabled provider the construction-yard target IS this count, and UT-4's
			// axis lean lives inside the provider (one multiplier, never two), so the planner applies neither fixed number.
			var targetCount = Info.McvTargetCount;
			if (scaleTargetProviders.TryTarget("conyard", out var scaledYards))
				targetCount = scaledYards;
			else if (Info.UseUtilityExpansionAppetite)
			{
				var axis = utilityAxesProviders?.FirstEnabledTraitOrDefault()?.UtilityTechRushExpansion ?? IBotUtilityAxes.Neutral;
				targetCount = EffectiveMcvTargetCount(Info.McvTargetCount, axis, Info.ExpansionAxisBonusMcvs, Info.TechRushAxisMinusMcvs);
			}

			// EX-4: cover the whole map. The flat McvTargetCount ceiling is what made the stack stop at one
			// expansion — active yards count against it, so three yards meant "never request again". With
			// CoverAllFields the target becomes "as many active construction assets as exist, plus a bounded
			// inflight pipeline whenever a reachable far field is still free". freeFarFields==0 keeps a one-MCV
			// replacement allowance so a lost yard can be refounded.
			if (Info.CoverAllFields)
			{
				var tick = world.WorldTick;
				var freeFarFields = LastScores.Count(f => f.Hops >= Info.McvMinHops
					&& !(parkedUntil.TryGetValue(f.Index, out var until) && tick < until));
				var inflightCap = Math.Min(Math.Max(freeFarFields, 1), Math.Max(1, Info.CoverAllFieldsMaxInflight));
				targetCount = Math.Max(targetCount, active + inflightCap);
			}

			// BZ_production_scaling: while windowed income holds over the difficulty line and the
			// army-parity floor admits expansion, the target carries up to ScalingMcvMaxExtra extra
			// construction MCVs (a missing situation snapshot admits, same as CRAWL-SAFETY). McvDue
			// still requires a free far field and pipeline headroom, and production stays cash-gated
			// at the queue — this raises the ceiling only.
			if (Info.UseProductionScaling)
			{
				var ratio = player.PlayerActor.TraitOrDefault<MasterAiBotModule>()?.Situation?.CombatRatioPct;
				targetCount += ProductionScalingCA.McvAllowance(
					ProductionScalingCA.IncomeMet(CurrentIncomePerTick(), ScalingMcvIncomeBar),
					farFieldFree,
					ratio == null || ArmyParityAdmits(ratio.Value, ScalingParityFloor),
					ScalingMcvMaxExtra);
			}

			// REF-1 B2: the request rides under the cash reserve — queueing the MCV costs nothing until
			// production starts, so the standing want must not wait for a re-plan that happens to see cash.
			if (!McvDue(farFieldFree, active + queued, targetCount))
				return;

			var unitBuilder = unitBuilders.FirstEnabledTraitOrDefault();
			if (unitBuilder == null)
				return;

			var producible = world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Enabled)
				.SelectMany(a => a.Trait.BuildableItems())
				.Select(i => i.Name)
				.Where(constructionMcvTypes.Contains)
				.Distinct()
				.ToArray();
			if (producible.Length == 0)
			{
				// REF-1 B2: the MCV is due but not producible — its missing prerequisite becomes a building
				// want (td_gdi: the repair facility gating the MCV for ~10k ticks of the trace).
				wantedMcvPrerequisite = MissingMcvPrerequisite();
				if (wantedMcvPrerequisite != null)
					Log.Write("debug", $"AI ({player.ClientIndex}): REF-1 MCV due but not producible — wants prerequisite building {wantedMcvPrerequisite} at tick {world.WorldTick}");
				return;
			}

			var mcvType = producible.Random(BotRng.For(player, nameof(ExpansionPlannerBotModule)));
			if (unitBuilder.RequestedProductionCount(bot, mcvType) > 0)
				return;

			unitBuilder.RequestUnitProduction(bot, mcvType);
			var line = $"AI ({player.ClientIndex}): greedy MCV request {mcvType}: {active} active yards/MCVs, {queued} queued, cash {resources.GetCashAndResources()}, at tick {world.WorldTick}";
			Log.Write("debug", line);
			AIUtils.BotDebug(line);
		}

		CPos? IBotMcvExpansionSiteProvider.McvExpansionSite(Actor mcv)
		{
			// A deploying yard relocation (no Mobile) keeps the MCV module's own choice.
			if (IsTraitDisabled || mcv == null || !mcv.Info.HasTraitInfo<MobileInfo>())
				return null;

			// BEV: a base-building vehicle deploys at home; the MCV module's deploy search takes the cells 2-20 around it.
			if (Info.BaseVehiclesAtBase && baseCenter.HasValue && RoleOf(mcv.Info) == McvRole.BaseBuilding)
			{
				Log.Write("debug", $"AI ({player.ClientIndex}): BEV {mcv.Info.Name} {mcv.ActorID} deploys at the base {baseCenter.Value} (not a construction MCV, not a refinery) at tick {world.WorldTick}");
				return baseCenter;
			}

			if (!Info.DriveMcvSite)
				return null;

			var tick = world.WorldTick;
			var locomotor = mcv.TraitOrDefault<Mobile>()?.Locomotor;
			bool Eligible(FieldScore f)
			{
				if (parkedUntil.TryGetValue(f.Index, out var until) && tick < until)
					return false;

				// LC3: never send an MCV to a field its locomotor cannot reach (the MCV module's deploy search would
				// reject it and record its own checkspot, not this field).
				if (pathFinder != null && locomotor != null
					&& !pathFinder.PathMightExistForLocomotorBlockedByImmovable(locomotor, mcv.Location, f.Center))
				{
					if (landUnreachableLogged.Add(f.Index))
						Log.Write("debug", $"AI ({player.ClientIndex}): LC3 field {f.Index} at {f.Center} is not reachable for {mcv.Info.Name} ({locomotor.Info.Name}) from {mcv.Location} at tick {tick}");

					return false;
				}

				// BZ_production_scaling (MCV-EXPAND-IDLE H1): a field whose deploy annulus holds no cell the
				// yard can legally occupy is a dead handout — the MCV drives there and never deploys. Park it
				// like a stale handout; the park makes the re-check cheap while blockers may clear.
				if (Info.UseProductionScaling && !ScalingDeployCellExists(mcv, f.Center))
				{
					parkedUntil[f.Index] = tick + Info.ParkTicks;
					if (deployRejectedLogged.Add(f.Index))
						Log.Write("debug", $"AI ({player.ClientIndex}): BZ field {f.Index} at {f.Center} parked {Info.ParkTicks} ticks — no deployable cell in the annulus for {mcv.Info.Name} at tick {tick}");

					return false;
				}

				return true;
			}

			// FE-1 (§12.24): MCV and crawl cover different ground. A site scores x SeparationFactor (its bearing from the main
			// base must differ from the crawl target's, any own yard's and any other in-flight MCV site's) x SpreadFactor
			// (far from everything we own). Null weight (switch off) = master behaviour.
			Func<FieldScore, double> weight = null;
			if (Info.FieldCoverage)
			{
				var origin = baseCenter ?? (ownYardCells.Count > 0 ? ownYardCells[0] : mcv.Location);
				var avoid = new List<CPos>(ownYardCells);
				if (Target is FieldScore crawl)
					avoid.Add(crawl.Center);

				foreach (var kv in inflightMcvSites)
					if (kv.Key != mcv.ActorID)
						avoid.Add(kv.Value);

				var diagonal = MapDiagonalCells();
				weight = f => SeparationFactor(f.Center, origin, avoid, Info.CrawlSeparationDegrees, Info.MinSeparationFactor, Info.MinBearingDistanceCells)
					* SpreadFactor(NearestOwnBuildingDistance(f.Center), diagonal, Info.SpreadBonus);
			}

			// At most one park per request: a parked field falls out and the next best is offered.
			for (var attempt = 0; attempt < 2; attempt++)
			{
				if (McvSite(LastScores, mcv.Location, Info.McvMinHops, Info.McvTauCells, Eligible, weight) is not FieldScore s)
					return null;

				(mcvHandout, var park) = TrackMcvHandout(mcvHandout, s.Index, Info.McvMaxSiteHandouts);
				if (park)
				{
					parkedUntil[s.Index] = tick + Info.ParkTicks;
					Log.Write("debug", $"AI ({player.ClientIndex}): LC3 parked field {s.Index} at {s.Center} for {Info.ParkTicks} ticks: handed to an idle MCV more than {Info.McvMaxSiteHandouts} times in a row, no yard founded, at tick {tick}");
					continue;
				}

				if (Info.FieldCoverage)
					inflightMcvSites[mcv.ActorID] = s.Center;

				Log.Write("debug", $"AI ({player.ClientIndex}): EX-3 MCV {mcv.Info.Name} at {mcv.Location} sent to field {s.Index} at {s.Center}: value {s.Value}, hops {s.Hops}, safety {s.Safety:F2}, hand-out {mcvHandout.Count} at tick {tick}");
				LastMcvSite = s.Center;
				return s.Center;
			}

			return null;
		}

		/// <summary>
		/// BZ_production_scaling: does the yard this MCV transforms into have at least one legally placeable
		/// cell inside the engine manager's deploy annulus around the field centre? Mirrors the engine's
		/// FindDeployCell predicate verbatim (CanPlaceBuilding at cell + transform offset, the MCV ignored
		/// as a blocker). Null profile = nothing to validate — never reject on missing data.
		/// </summary>
		bool ScalingDeployCellExists(Actor mcv, CPos fieldCenter)
		{
			var profile = DeployProfile(mcv);
			if (profile == null)
				return true;

			var (info, bi, offset) = profile.Value;
			var annulus = deployAnnulus ??= DeployAnnulusRadii();
			return world.Map.FindTilesInAnnulus(fieldCenter, annulus.Min, annulus.Max)
				.Any(cell => world.CanPlaceBuilding(cell + offset, info, bi, mcv));
		}

		(ActorInfo Info, BuildingInfo Building, CVec Offset)? DeployProfile(Actor mcv)
		{
			if (deployProfiles.TryGetValue(mcv.Info.Name, out var cached))
				return cached;

			(ActorInfo Info, BuildingInfo Building, CVec Offset)? profile = null;
			var transforms = mcv.Info.TraitInfos<TransformsInfo>().FirstOrDefault();
			if (transforms != null && world.Map.Rules.Actors.TryGetValue(transforms.IntoActor, out var into)
				&& into.TraitInfoOrDefault<BuildingInfo>() is { } bi)
				profile = (into, bi, transforms.Offset);

			return deployProfiles[mcv.Info.Name] = profile;
		}

		/// <summary>
		/// The union of every deploy annulus the engine's McvExpansionManager can search under any mode —
		/// the site provider cannot see the live mode, so a field only parks when NO mode's annulus holds a
		/// placeable cell (conservative: never reject a field a live mode could still deploy on).
		/// </summary>
		(int Min, int Max) DeployAnnulusRadii()
		{
			var infos = self.Info.TraitInfos<McvExpansionManagerBotModuleInfo>().ToArray();
			if (infos.Length == 0)
				return (2, 20);

			return (infos.Min(i => Math.Min(i.CRmodeMinDeployRadius, i.CBmodeMinDeployRadius)),
				infos.Max(i => Math.Max(i.CRmodeMaxDeployRadius, i.CBmodeMaxDeployRadius)));
		}

		/// <summary>
		/// EX-2d: the centre to score/claim for a field — the live centre while resource cells exist;
		/// a depleted field's live centre collapses (the resource map recomputes it every scan and an
		/// empty field has none), so fall back to the remembered first-seen centre instead of aiming
		/// refineries at the map corner. Pure, for the tests.
		/// </summary>
		public static CPos EffectiveCenter(int liveCellCount, CPos liveCenter, CPos rememberedCenter)
		{
			return liveCellCount > 0 ? liveCenter : rememberedCenter;
		}

		/// <summary>Buildings needed to bring the base within reach of a field `distance` cells away.</summary>
		public static int Hops(int distanceCells, int reachCells, int stepCells)
		{
			var gap = distanceCells - reachCells;
			return gap <= 0 ? 0 : (gap + Math.Max(stepCells, 1) - 1) / Math.Max(stepCells, 1);
		}

		/// <summary>
		/// Field gate A: a live indice (its own cells) or a remembered one stays in play — regrowth keeps a
		/// depleted field worth revisiting. Pure, for the tests.
		/// </summary>
		public static bool FieldPresent(bool hasIndice, int liveResourceCells, bool remembered) =>
			hasIndice && (liveResourceCells > 0 || remembered);

		/// <summary>
		/// Field gate B: already ours (a refinery in the indice or within claim radius) or worthless —
		/// `value` is the first-seen cell count, so a live field never reaches this gate with zero.
		/// Under the refinery law the take is anchor-granular (F1): an indice that still holds an
		/// unserved anchor is not "ours" no matter how many refineries its other anchors already have —
		/// one refinery per ANCHOR means a multi-spreader single-indice field must stay claimable.
		/// Classic keeps the old shape: `lawActive` false makes the extra terms fall away entirely.
		/// Pure, for the tests.
		/// </summary>
		public static bool FieldTaken(int playerRefineries, bool claimed, int value, bool lawActive = false, bool indiceHasUnservedAnchor = false) =>
			value <= 0 || ((claimed || playerRefineries > 0) && !(lawActive && indiceHasUnservedAnchor));

		/// <summary>
		/// TC-2c (§12.17): yield to an outranking ally's broadcast claim; null or empty claims never yield.
		/// Pure, for the tests.
		/// </summary>
		public static bool YieldToAllyClaim(IReadOnlyCollection<(int ClientIndex, WPos Claim)> allyClaims,
			WPos field, int myClientIndex, int radiusCells) =>
			allyClaims != null && allyClaims.Count > 0 && AllyClaimWins(field, allyClaims, myClientIndex, radiusCells);

		/// <summary>
		/// The post-score multipliers in their fixed order — coalition-sector percent (TC-3), unexplored-spread
		/// bonus and MCV separation (FE-1). A null factor means "not applicable this plan". Pure, for the tests.
		/// </summary>
		public static double ApplyFieldScoreModifiers(double score, int? sectorPercent, double? spreadFactor, double? separationFactor)
		{
			if (sectorPercent.HasValue)
				score *= sectorPercent.Value / 100.0;

			if (spreadFactor.HasValue)
				score *= spreadFactor.Value;

			if (separationFactor.HasValue)
				score *= separationFactor.Value;

			return score;
		}

		/// <summary>
		/// The field-score ordering (F4): best score first; an exact tie goes to the lowest indice
		/// index. List.Sort is unstable, so a bare score comparison could rank two tied fields
		/// differently across runtimes — the index tie-break keeps every peer's pick identical.
		/// Pure, for the tests.
		/// </summary>
		public static int CompareFieldScore(FieldScore a, FieldScore b)
		{
			var c = b.Score.CompareTo(a.Score);
			return c != 0 ? c : a.Index.CompareTo(b.Index);
		}

		/// <summary>An in-flight MCV's site claim releases when the holder is gone, dead or no longer ours.</summary>
		public static bool SiteHolderGone(bool found, bool dead, bool inWorld, bool owned) =>
			!found || dead || !inWorld || !owned;

		int MapDiagonalCells()
		{
			var size = world.Map.MapSize;
			return (int)Math.Sqrt((long)size.Width * size.Width + (long)size.Height * size.Height);
		}

		int NearestOwnBuildingDistance(CPos cell)
		{
			var best = int.MaxValue;
			foreach (var b in ownBuildingCells)
				best = Math.Min(best, (b - cell).Length);

			return best == int.MaxValue ? 0 : best;
		}

		void IBotTick.BotTick(IBot bot)
		{
			// LC4 (AI_REVIEW_FRANSOTTO P1c): TraitEnabled may run before the resource map's own condition is granted in
			// the same batch, so a null / disabled cache is re-resolved here instead of idling for the whole match.
			if (resourceMap == null || !resourceMap.IsTraitEnabled())
				resourceMap = self.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());

			if (resourceMap == null || resources == null)
			{
				if (++ticks % Info.ReplanTicks == 0)
					Idle(resourceMap == null ? "no enabled ResourceMapBotModule" : "no PlayerResources");
				return;
			}

			income.Enqueue((world.WorldTick, resources.Earned));
			while (income.Count > 1 && world.WorldTick - income.Peek().Tick > Info.IncomeWindowTicks)
				income.Dequeue();

			// REPAIR-B3: the coverage oracle's per-tick budget step — decoupled from the replan cadence
			// so a refresh interrupted by the probe budget resumes where it stopped instead of starving
			// the tail of the anchor list for ReplanTicks at a time.
			if (Info.FieldCoverage && Info.DriveRefineries)
				CoverageTick();

			if (++ticks % Info.ReplanTicks != 0)
				return;

			Replan(bot);
		}

		void Replan(IBot bot)
		{
			var (refinery, link, crawlLink, defence, queues) = CheapestBuildables();

			// REF-1 B3 (§12.24 v2): a transiently unbuildable refinery (prerequisite missing, queue disabled)
			// must not silence the planner — the estimate only feeds field scoring's payback term, so the last
			// known buildable, then any rules-listed refinery, stands in. Target, the anchor claim and
			// RequestMcv all keep running.
			if (refinery.Info != null)
				lastRefineryEstimate = refinery;
			refinery = RefineryEstimateOrFallback(refinery, lastRefineryEstimate, RulesRefineryEstimate);

			if (refinery.Info == null)
			{
				// No refinery exists at all (a mod without one). The last target and scores stay published —
				// a stale crawl aim beats none — and a standing MCV want still fires: the request is free and
				// production is cash-gated downstream.
				if (Info.DriveMcvRequests)
					RequestMcv(bot);
				Idle($"no refinery buildable ({queues} building queue(s) searched)");
				return;
			}

			// One pass over OUR OWN actors (always visible to us: fog-honest): the cells of the buildings that extend the
			// base (GivesBuildableArea; a captured derrick or a garrisoned house does not), and guard units.
			var buildingCells = new List<CPos>();
			var refineryCells = new List<CPos>();

			// REF-1: the frontier/tiles the law actually measures — a refinery's footprint binds its anchor and a
			// building's tiles are the cells IsCloseEnoughToBase tests for adjacency; top-lefts would read too tight.
			var refineryTiles = new List<IReadOnlyCollection<CPos>>();
			var refineryDocks = new List<(CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled)>();
			var buildingTiles = new List<CPos>();
			var guards = new List<(CPos Cell, int Value)>();
			var defences = Info.UseCrawlSafety ? new List<(CPos Cell, int Value)>() : null;
			var allBuildingCells = new List<CPos>();
			var yardCells = new List<CPos>();
			foreach (var a in world.Actors)
			{
				if (a.Owner != player || a.IsDead || !a.IsInWorld)
					continue;

				// The Refinery trait, from rules (the by-name lists are filled by the role rollout, §2.8).
				if (a.Info.HasTraitInfo<RefineryInfo>())
				{
					refineryCells.Add(a.Location);
					if (Info.FieldCoverage)
					{
						refineryTiles.Add(a.Info.TraitInfos<BuildingInfo>().FirstOrDefault()?.Tiles(a.Location).ToList());
						refineryDocks.Add(RefineryDockGeometry(a));
					}
				}

				if (Info.FieldCoverage)
				{
					if (a.Info.HasTraitInfo<BuildingInfo>())
						allBuildingCells.Add(a.Location);

					if (constructionYardTypes.Contains(a.Info.Name))
						yardCells.Add(a.Location);
				}

				// CRAWL-SAFETY: armed own buildings count as front cover — collected for both GBA
				// branches, so a defence tower keeps scoring even without buildable area.
				if (defences != null && a.Info.HasTraitInfo<AttackBaseInfo>() && a.Info.HasTraitInfo<BuildingInfo>())
					defences.Add((a.Location, a.Info.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? 0));

				if (a.Info.HasTraitInfo<GivesBuildableAreaInfo>())
				{
					buildingCells.Add(a.Location);
					if (Info.FieldCoverage && a.Info.TraitInfos<BuildingInfo>().FirstOrDefault() is { } abi)
						buildingTiles.AddRange(abi.Tiles(a.Location));
				}
				else if (a.Info.HasTraitInfo<BuildingInfo>())
					continue;
				else if (a.Info.HasTraitInfo<AttackBaseInfo>() && !a.Info.HasTraitInfo<HarvesterInfo>())
					guards.Add((a.Location, a.Info.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? 0));
			}

			if (buildingCells.Count == 0)
			{
				Idle("no own building gives buildable area");
				return;
			}

			if (Info.FieldCoverage)
			{
				ownBuildingCells = allBuildingCells;
				ownYardCells = yardCells;

				// An MCV that is gone (deployed into a yard, or dead) no longer holds its site.
				foreach (var id in inflightMcvSites.Keys.ToArray())
				{
					var heading = world.GetActorById(id);
					if (SiteHolderGone(heading != null, heading?.IsDead ?? false, heading?.IsInWorld ?? false, heading?.Owner == player))
						inflightMcvSites.Remove(id);
				}
			}

			var diagonalCells = Info.FieldCoverage ? MapDiagonalCells() : 1;
			var incomePerTick = CurrentIncomePerTick();
			var reach = Info.ReachCells;
			var scores = new List<FieldScore>();
			var fieldIndexCount = resourceMap.GetIndicesLength();
			var owned = 0;

			// TC-2c (§12.17): allied expansion claims off the team blackboard — only the ones that
			// outrank us (lower ClientIndex). No allies → empty → every field behaves as before.
			List<(int ClientIndex, WPos Claim)> allyClaims = null;
			if (Info.UseTeamExpansionClaims)
			{
				allyClaims = TeamBlackboard.CollectBroadcasts(player)
					.Where(b => b != null && b.ExpansionClaim != WPos.Zero)
					.Select(b => (b.ClientIndex, b.ExpansionClaim))
					.ToList();
			}

			// TC-3 / BD (§12.18): the coalition's Voronoi partition — every allied spawn anchor owns
			// the fields nearest it. Resolved once per re-plan; a missing provider or an empty anchor
			// map leaves every score untouched (1v1, flag off — bit-identical).
			IReadOnlyDictionary<string, WPos> sectorAnchors = null;
			if (Info.UseCoalitionSectors)
			{
				var anchors = player.PlayerActor.TraitsImplementing<IBotCoalition>()
					.FirstEnabledTraitOrDefault()?.Coalition?.SectorAnchors;
				if (anchors != null && anchors.Count > 0)
					sectorAnchors = anchors;
			}

			// F1 (lead ruling): the law's service is per ANCHOR, so gate B's indice-level refinery count
			// must not starve the second anchor of a multi-spreader single-indice field — the anchor
			// model builds before the scores loop and every indice still holding an unserved anchor
			// stays in play (crawl aim and MCV pipeline keep driving at it).
			var lawClaims = false;
			HashSet<ResourceIndice> unservedAnchorIndices = null;
			if (Info.FieldCoverage && Info.DriveRefineries)
			{
				EnsureAnchorModel(refineryCells, refineryTiles, buildingTiles, refineryDocks);
				lawClaims = LawActive;
				if (lawClaims && fieldIndexCount > 0)
					for (var a = 0; a < this.anchors.Count; a++)
						if (lastAssigned[a] < 0)
							(unservedAnchorIndices ??= new HashSet<ResourceIndice>())
								.Add(resourceMap.FindClosestIndiceFromCPos(this.anchors[a]));
			}

			for (var i = 0; i < resourceMap.GetIndicesLength(); i++)
			{
				var field = resourceMap.GetIndice(i);
				if (!FieldPresent(field != null, field?.ResourceCellsCount ?? 0, initialCells.ContainsKey(i)))
					continue;

				// Ruling (c): the field's size at match start is public map data. The first scan of a field
				// fixes its value; depletion under the fog is never read (EX-1 adds depletion that was seen).
				if (!initialCells.TryGetValue(i, out var value))
				{
					initialCells[i] = value = field.ResourceCellsCount;
					initialCenters[i] = field.ResourceCellsCenter;
				}

				// The centre drifts as the field depletes (the resource map recomputes it every scan), so a refinery near
				// either the current or the first-seen centre claims it; otherwise a claimed field could look free again.
				// EX-2d: fully depleted fields report a degenerate live centre — score/aim at the remembered
				// first-seen centre instead (the field regrows; its location does not move).
				var center = EffectiveCenter(field.ResourceCellsCount, field.ResourceCellsCenter, initialCenters[i]);
				var claimed = Claimed(center, refineryCells, Info.ClaimRadiusCells)
					|| Claimed(initialCenters[i], refineryCells, Info.ClaimRadiusCells);
				if (FieldTaken(field.PlayerRefineryCount, claimed, value, lawClaims,
						unservedAnchorIndices != null && unservedAnchorIndices.Contains(field)))
				{
					owned += value > 0 ? 1 : 0;
					continue;
				}

				if (parkedUntil.TryGetValue(i, out var until) && world.WorldTick < until)
					continue;

				// CRAWL-SAFETY (S3): a route that kept losing links at the same cells is parked —
				// another frontier gets the crawl until the farm expires.
				if (Info.UseCrawlSafety && crawlParkedUntil.TryGetValue(i, out var lossUntil) && world.WorldTick < lossUntil)
					continue;

				// TC-2c: an outranking ally already aims at this field — yield instead of stacking.
				if (YieldToAllyClaim(allyClaims, world.Map.CenterOfCell(center), player.ClientIndex, Info.AllyClaimRadiusCells))
					continue;

				var distance = buildingCells.Min(c => (c - center).Length);
				var hops = Hops(distance, reach, Info.LinkStepCells);
				var threat = threatProviders.MergedThreatAt(center);
				var guardValue = guards.Where(g => (g.Cell - center).LengthSquared <= Info.GuardRadiusCells * Info.GuardRadiusCells)
					.Sum(g => g.Value);
				var cost = refinery.Cost + hops * link.Cost;
				var score = Score(value, threat, guardValue, cost, incomePerTick, hops, link.BuildTicks, refinery.BuildTicks,
					Info.TauTicks, out var payback);

				// TC-3 / BD: own-sector fields keep their full score, foreign-sector fields keep
				// CoalitionForeignSectorPercent of it — deprioritized, never forbidden: the argmax
				// still picks a foreign field when no own-sector field is left.
				// FE-1: unexplored ground wins ties (spread), and the crawl prefers fields no MCV is already heading to.
				score = ApplyFieldScoreModifiers(score,
					sectorAnchors != null ? SectorScorePercent(world.Map.CenterOfCell(center), sectorAnchors,
						player.InternalName ?? "#" + player.ClientIndex, Info.CoalitionForeignSectorPercent) : (int?)null,
					Info.FieldCoverage ? SpreadFactor(allBuildingCells.Count > 0 ? allBuildingCells.Min(c => (c - center).Length) : distance,
						diagonalCells, Info.SpreadBonus) : (double?)null,
					Info.FieldCoverage && inflightMcvSites.Count > 0 && TakenByMcvSite(center, inflightMcvSites.Values, Info.ClaimRadiusCells)
						? Info.MinSeparationFactor : (double?)null);

				scores.Add(new FieldScore(i, center, value, hops, payback, threat, score, Safety(threat, guardValue)));
			}

			scores.Sort(CompareFieldScore);
			LastScores = scores;
			var previous = Target?.Index;
			Target = scores.Count > 0 ? scores[0] : null;
			if (Target == null)
				Idle($"no free field ({fieldIndexCount} map indices, {initialCells.Count} with resources, {owned} already ours)");
			else
				lastIdleReason = null;

			// EX-2/EX-2c: want a refinery at the best free field already in reach — any in-reach field counts,
			// not only the crawl target, so an outpost yard claims its local field right away (EX-2c). Park a
			// field that keeps being missed.
			// FE-1 (§12.24): with the switch on, one refinery per anchor replaces this field-based claim.
			if (Info.FieldCoverage && Info.DriveRefineries)
			{
				UpdateAnchorClaim(refineryCells, refineryTiles, buildingTiles, refineryDocks);
				UpdateCrawlWant(crawlLink, defence, buildingTiles, buildingCells, guards, defences);
			}
			else
			{
				wantedLinkBuilding = null;
				crawlTargetEdge = null;
				frontSafety.ResetCorridor();
			}

			(claimField, wantsRefinery) = ClaimSelection(lawClaims, wantsRefinery,
				lawClaims ? (FieldScore?)null : BestClaimField(scores));

			if (lawClaims)
			{
				// The anchor claim (UpdateAnchorClaim) carries its own loop guard; TrackClaim is the field claim's.
			}
			else if (wantsRefinery && claimField is FieldScore want)
			{
				claimAttempts.TryGetValue(want.Index, out var attempts);
				var (state, now, park) = TrackClaim(wanting, want.Index, refineryCells.Count, attempts, Info.MaxClaimAttempts);
				wanting = state;
				claimAttempts[want.Index] = now;
				if (park)
				{
					parkedUntil[want.Index] = world.WorldTick + Info.ParkTicks;
					claimAttempts.Remove(want.Index);
					claimField = null;
					wantsRefinery = false;
					var parked = $"AI ({player.ClientIndex}): EX-2 parked field {want.Index} at {want.Center} for {Info.ParkTicks} ticks: {now} refinery(ies) built without claiming it, at tick {world.WorldTick}";
					Log.Write("debug", parked);
					AIUtils.BotDebug(parked);
				}
			}
			else
				wanting = (-1, refineryCells.Count);

			// Telemetry (EX-0): a debug.log line whenever the target field changes, so a batch shows where each bot
			// wanted to expand and why. BotDebug alone only reaches the in-game chat.
			if (Target is FieldScore t && t.Index != previous)
			{
				var line = string.Format("AI ({0}): EX-0 target field {1} at {2}: value {3}, hops {4}, payback {5} ticks, threat {6}, score {7:F4} ({8} fields) at tick {9}",
					player.ClientIndex, t.Index, t.Center, t.Value, t.Hops, t.PaybackTicks, t.Threat, t.Score, scores.Count, world.WorldTick);
				Log.Write("debug", line);
				AIUtils.BotDebug(line);
			}

			// Greedy expansion: ask for the next construction MCV right away; the engine module would wait for its
			// high cash trigger first. LastScores is this tick's fresh list, so a freed field is seen immediately.
			if (Info.DriveMcvRequests)
				RequestMcv(bot);
		}

		/// <summary>
		/// REF-1 B1: the planner's own crawl-supply want — while the crawl target's field sits beyond reach and no
		/// anchor is claimable in reach, the cheapest crawl-eligible building should be produced and placed to close
		/// the gap. Power demand alone was the only supply before, and it retired the moment a bigger power plant
		/// unlocked (the trace's frontier freeze). Pure, for the tests.
		/// </summary>
		public static bool LinkBuildingWanted(bool driveBaseCrawl, int? targetHops, int claimableAnchorsInReach, bool linkAvailable) =>
			driveBaseCrawl && linkAvailable && targetHops > 0 && claimableAnchorsInReach == 0;

		/// <summary>
		/// REF-1: a pending anchor commit clears on expiry or the moment its refinery landed (the anchor is
		/// served). Pure, for the tests.
		/// </summary>
		public static bool PendingCommitCleared(int tick, int until, bool served) => tick >= until || served;

		/// <summary>
		/// REF-1: a refinery is wanted only while claimable anchors outnumber refineries already in production —
		/// never by comparing totals (a duplicate stacked at home must not spend a forward anchor's quota).
		/// Pure, for the tests.
		/// </summary>
		public static bool AnchorRefineryWanted(int claimable, int queued) => claimable > queued;

		/// <summary>
		/// REF-1 loop guard: the same anchor wanted replan after replan with no refinery gained and none in
		/// production counts a stuck re-plan; anything else restarts the count. `max` of 0 disables parking.
		/// Pure, for the tests.
		/// </summary>
		public static (int Replans, bool Park) AnchorStuckNext(bool sameAnchor, int refineryCount,
			int previousRefineries, int queued, int previousReplans, int maxReplans)
		{
			var replans = sameAnchor && refineryCount <= previousRefineries && queued == 0 ? previousReplans + 1 : 0;
			return (replans, maxReplans > 0 && replans >= maxReplans);
		}

		/// <summary>
		/// R7: under the anchor law the field claim is always null and the law's want carries through; classic
		/// claims the best in-reach field and wants a refinery exactly when one exists. Pure, for the tests.
		/// </summary>
		public static (FieldScore? Claim, bool Want) ClaimSelection(bool lawActive, bool lawWanted, FieldScore? bestClaim) =>
			lawActive ? (null, lawWanted) : (bestClaim, bestClaim.HasValue);

		void UpdateCrawlWant((ActorInfo Info, int Cost, int BuildTicks) link, (ActorInfo Info, int Cost, int BuildTicks) defence,
			List<CPos> buildingTiles, List<CPos> buildingCells, List<(CPos Cell, int Value)> guards, List<(CPos Cell, int Value)> defences)
		{
			wantedLinkBuilding = null;
			wantWasMandatory = false;
			crawlTargetEdge = null;
			crawlFrontierAnchor = null;
			wantedFrontDefence = null;
			frontDefenceCell = null;
			protectionRequests.Clear();
			scoutInterestCells.Clear();

			// One queue scan serves the claim reconciles below (TF-1's and the front defence's).
			IReadOnlyDictionary<object, HashSet<string>> inflightByOwner = null;
			if ((Info.BoundCrawlLinkSupply && crawlAllowance.HasClaims) || frontDefenceClaim != null)
				inflightByOwner = QueuedItemNamesByProducer();

			if (Info.BoundCrawlLinkSupply && crawlAllowance.HasClaims)
			{
				// TF-1: reconcile outstanding claims against the real queues every re-plan — a pending
				// intent that entered production becomes paid; one that never arrived drops uncharged.
				crawlAllowance.Reconcile(inflightByOwner, Info.OptionalLinkPendingReplans);
			}

			// CRAWL-SAFETY: the front-defence claim frees the moment its item leaves the claimant's
			// queue — it landed (cover decides next plan) or was cancelled (the want re-admits).
			if (frontDefenceClaim is { } claimedDefence
				&& !(inflightByOwner.TryGetValue(claimedDefence.Owner, out var claimedItems) && claimedItems.Contains(claimedDefence.Item)))
				frontDefenceClaim = null;

			if (!LinkBuildingWanted(Info.DriveBaseCrawl, Target?.Hops, claimableAnchorsInReach, link.Info != null))
			{
				// TF-1: the ledger survives a want that blinks off — only a different target field
				// opens a fresh episode, so an off/on flap can never re-open the allowance.
				frontSafety.ResetCorridor();
				return;
			}

			crawlTargetEdge = LinkTargetEdge(fields, Target.Value, buildingTiles);

			// Mandatory power never consults a gate: a real deficit admits the (power-first) link
			// outright; under TF-1 its paid admission still charges the episode.
			var mandatoryPower = player.PlayerActor.TraitOrDefault<PowerManager>()?.ExcessPower < 0;

			// CRAWL-SAFETY (S3): the front ledger evaluates the corridor — a farmed route parks the
			// target, the parity/scout/cover/escort gates may refuse the optional want; the published
			// escort request, scout cell and front-defence want ride as side effects either way.
			var frontier = FrontierAnchor(buildingTiles, crawlTargetEdge);
			var safetyAdmits = !Info.UseCrawlSafety
				|| UpdateCrawlSafety(link, defence, frontier, buildingCells, guards, defences);

			if (!Info.BoundCrawlLinkSupply)
			{
				// REF-1 B1 legacy path: the want rides unbounded (switch-off keeps the traced behaviour);
				// CRAWL-SAFETY still gates it when armed, mandatory power aside.
				if (OptionalLinkAdmitted(mandatoryPower, safetyAdmits))
					wantedLinkBuilding = link.Info.Name;
				return;
			}

			// TF-1: the episode keys on the target field's centre; the frontier→aim distance is the
			// route-progress measure (the same argmin LinkTargetEdge uses, summed over building tiles).
			crawlAllowance.ForTarget(Target.Value.Center);
			var distanceSq = crawlTargetEdge.HasValue && buildingTiles.Count > 0
				? buildingTiles.Min(b => (b - crawlTargetEdge.Value).LengthSquared) : -1;
			crawlAllowance.NoteDistance(distanceSq, crawlTargetEdge, Info.OptionalLinkNonprogressReplans);

			// Mandatory power never consults the allowance: a real deficit admits the (power-first)
			// link outright; its paid admission still charges the episode.
			var admissible = crawlAllowance.OptionalLinkAdmissible(link.Cost,
				Info.OptionalLinkCostAllowance, Info.OptionalLinkCountAllowance, distanceSq) && safetyAdmits;
			if (!OptionalLinkAdmitted(mandatoryPower, admissible))
			{
				var reason = !safetyAdmits ? safetyRefusal
					: crawlAllowance.Parked ? "route parked"
					: distanceSq < 0 ? "no frontier evidence"
					: crawlAllowance.AwaitingProgress > 0 ? "awaiting frontier progress"
					: "allowance spent";
				if (lastCrawlRefusal != reason)
				{
					lastCrawlRefusal = reason;
					var refusal = string.Format(
						"AI ({0}): TF-1 crawl link {1} REFUSED ({2}) target {3} aim {4} distSq {5} paid {6}/{7} count {8}/{9} stall {10} at tick {11}",
						player.ClientIndex, link.Info.Name, reason, Target.Value.Center, crawlTargetEdge, distanceSq,
						crawlAllowance.PaidCost, Info.OptionalLinkCostAllowance, crawlAllowance.PaidCount,
						Info.OptionalLinkCountAllowance, crawlAllowance.NonprogressReplans, world.WorldTick);
					Log.Write("debug", refusal);
					AIUtils.BotDebug(refusal);
				}

				return;
			}

			lastCrawlRefusal = null;
			wantedLinkBuilding = link.Info.Name;
			wantWasMandatory = mandatoryPower;
			crawlFrontierAnchor = FrontierAnchor(buildingTiles, crawlTargetEdge);
			var admitted = string.Format(
				"AI ({0}): TF-1 crawl link {1} ADMITTED{2} target {3} aim {4} distSq {5} paid {6}/{7} count {8}/{9} at tick {10}",
				player.ClientIndex, link.Info.Name, mandatoryPower ? " (mandatory power)" : "",
				Target.Value.Center, crawlTargetEdge, distanceSq, crawlAllowance.PaidCost,
				Info.OptionalLinkCostAllowance, crawlAllowance.PaidCount, Info.OptionalLinkCountAllowance, world.WorldTick);
			Log.Write("debug", admitted);
			AIUtils.BotDebug(admitted);
		}

		/// <summary>
		/// CRAWL-SAFETY (FINDINGS_2026-10-12 S3): the per-replan front evaluation for the live corridor.
		/// Counts lost links near the frontier->aim segment (a farmed cell parks this target's route),
		/// applies the fog-fair army-parity gate, demands a scouted aim (bounded hold, the cell rides to
		/// the scout module as interest), and on a contested front demands defence cover then an escort
		/// before the optional link may be wanted. Publishes the escort request, the scout cell and the
		/// front-defence want as side effects; returns whether the OPTIONAL link admits — mandatory
		/// power bypasses every gate at the caller.
		/// </summary>
		bool UpdateCrawlSafety((ActorInfo Info, int Cost, int BuildTicks) link,
			(ActorInfo Info, int Cost, int BuildTicks) defence, CPos? frontier,
			List<CPos> buildingCells, List<(CPos Cell, int Value)> guards, List<(CPos Cell, int Value)> defences)
		{
			var tick = world.WorldTick;
			safetyRefusal = null;

			// (d) the corridor watch — own buildable-area cells near the frontier->aim segment whose
			// disappearance since the last re-plan is a lost link; a farmed cell parks this target's
			// route so another frontier gets the crawl (S3's rebuilt-15x farm).
			if (frontier.HasValue && crawlTargetEdge.HasValue)
			{
				var radiusSq = (long)Info.LossWatchRadiusCells * Info.LossWatchRadiusCells;
				var watch = new List<CPos>();
				foreach (var c in buildingCells)
					if (CrawlFrontSafety.PointToSegmentDistanceSq(c, frontier.Value, crawlTargetEdge.Value) <= radiusSq)
						watch.Add(c);

				var bound = Info.LossesToParkEasy <= 0 ? 0
					: BotDifficultyLadder.Scale(Info.LossesToParkEasy, Info.LossesToParkHard, player, Info.LossesToParkHard);
				var worst = frontSafety.NoteWatch(watch, Info.LossCellRadiusCells, out var hottest,
					c => CrawlFrontSafety.PointToSegmentDistanceSq(c, frontier.Value, crawlTargetEdge.Value) <= radiusSq);
				if (bound > 0 && worst >= bound)
				{
					crawlParkedUntil[Target.Value.Index] = tick + Info.LossParkTicks;
					var parked = string.Format(
						"AI ({0}): CRAWL-SAFETY parked target field {1} for {2} ticks — {3} links lost near {4} at tick {5}",
						player.ClientIndex, Target.Value.Index, Info.LossParkTicks, worst, hottest, tick);
					Log.Write("debug", parked);
					AIUtils.BotDebug(parked);
					return RefuseSafety("route farmed");
				}
			}
			else
				frontSafety.ResetCorridor();

			// (e) the army-parity gate — remembered own army vs the enemy army as this bot has SEEN it:
			// while the predicted combat ratio sits under the difficulty-scaled floor a crawl starves
			// the army it needs to survive, so no optional link is wanted and unit production keeps
			// priority. A missing snapshot or a floor of 0 never blocks.
			var situation = player.PlayerActor.TraitOrDefault<MasterAiBotModule>()?.Situation;
			var parityFloor = BotDifficultyLadder.Scale(Info.ArmyParityPctEasy, Info.ArmyParityPctHard, player, Info.ArmyParityPctHard);
			if (situation != null && !ArmyParityAdmits(situation.CombatRatioPct, parityFloor))
				return RefuseSafety($"army parity {situation.CombatRatioPct}<{parityFloor}");

			// (a) the aim must be explored ground — while it isn't, the want waits the bounded hold
			// and the aim cell rides to the scout module as interest (scouting first, then the link).
			if (crawlTargetEdge.HasValue && player.Shroud != null && !player.Shroud.IsExplored(crawlTargetEdge.Value))
			{
				scoutInterestCells.Add(crawlTargetEdge.Value);
				if (frontSafety.ScoutHolds(Info.ScoutWaitReplans))
					return RefuseSafety("aim unscouted");
			}
			else
				frontSafety.ResetScoutWait();

			// (b)+(c) the contested front — remembered enemy value at the aim or frontier means the
			// next link waits for defence cover, then for an escort that outweighs the threat.
			if (frontier.HasValue)
			{
				var threat = threatProviders.MergedThreatAt(frontier.Value);
				if (crawlTargetEdge.HasValue)
					threat = Math.Max(threat, threatProviders.MergedThreatAt(crawlTargetEdge.Value));

				if (threat > 0)
				{
					var coverRadiusSq = (long)Info.FrontCoverRadiusCells * Info.FrontCoverRadiusCells;
					var cover = defences.Where(d => (d.Cell - frontier.Value).LengthSquared <= coverRadiusSq).Sum(d => d.Value);
					if (Info.FrontCoverMinValue > 0 && cover < Info.FrontCoverMinValue && defence.Info != null)
					{
						wantedFrontDefence = defence.Info.Name;
						frontDefenceCell = frontier.Value;
						return RefuseSafety($"front uncovered ({cover}<{Info.FrontCoverMinValue})");
					}

					var guardRadiusSq = (long)Info.GuardRadiusCells * Info.GuardRadiusCells;
					var guard = guards.Where(g => (g.Cell - frontier.Value).LengthSquared <= guardRadiusSq).Sum(g => g.Value);
					if (!FrontGuardAdmits(guard, threat, Info.FrontGuardPercent))
					{
						protectionRequests.Add(new BotProtectionRequest(frontier.Value,
							Math.Max(threat, link.Cost), tick + Info.EscortRequestTicks));
						return RefuseSafety($"front contested (guard {guard} vs {threat})");
					}
				}
			}

			return true;
		}

		bool RefuseSafety(string reason)
		{
			safetyRefusal = reason;
			return false;
		}

		/// <summary>
		/// CRAWL-SAFETY: the army-parity gate — a missing floor never blocks; the remembered
		/// own-vs-enemy ratio must reach it (an unseen enemy scores MaxRatio and passes).
		/// Pure, for the tests.
		/// </summary>
		public static bool ArmyParityAdmits(int combatRatioPct, int floorPct) =>
			floorPct <= 0 || combatRatioPct >= floorPct;

		/// <summary>
		/// CRAWL-SAFETY: the contested-front escort gate — an uncontested front always admits;
		/// contested ground admits only while our guard reaches the required percent of the
		/// remembered threat (a stronger local force still crawls; a weaker one waits for escort).
		/// Pure, for the tests.
		/// </summary>
		public static bool FrontGuardAdmits(int guardValue, int threat, int percent) =>
			threat <= 0 || guardValue * 100L >= threat * (long)percent;

		/// <summary>
		/// TF-1: the bound publishes the want when mandatory power (a real deficit) or the episode
		/// admits it — the only two reasons an optional link may be supplied. Pure, for the tests.
		/// </summary>
		public static bool OptionalLinkAdmitted(bool mandatoryPower, bool admissible) => mandatoryPower || admissible;

		/// <summary>
		/// TF-1: what each building-queue producer actually has in production right now — the
		/// reconcile evidence. Claims key on the queue's actor, so a pending intent is "accepted"
		/// only when its item is seen in that actor's queue.
		/// </summary>
		Dictionary<object, HashSet<string>> QueuedItemNamesByProducer()
		{
			var inFlight = new Dictionary<object, HashSet<string>>();
			foreach (var queue in BuildingQueueTypes().SelectMany(t => CAAIUtils.FindQueues(player, t)).Distinct())
			{
				if (!inFlight.TryGetValue(queue.Actor, out var items))
					inFlight[queue.Actor] = items = new HashSet<string>(StringComparer.Ordinal);
				items.UnionWith(queue.AllQueued().Select(i => i.Item));
			}

			return inFlight;
		}

		/// <summary>
		/// TF-1: the frontier anchor — our own building cell nearest the published aim. A bounded crawl
		/// placement searches its annulus around this cell instead of the yard centre, so an admitted
		/// link must land forward of the frontier. Pure (argmin, ties keep enumeration order), for
		/// the tests.
		/// </summary>
		internal static CPos? FrontierAnchor(IReadOnlyList<CPos> buildingTiles, CPos? aim)
		{
			if (!aim.HasValue || buildingTiles == null || buildingTiles.Count == 0)
				return null;

			var best = buildingTiles[0];
			var bestSq = (best - aim.Value).LengthSquared;
			for (var i = 1; i < buildingTiles.Count; i++)
			{
				var sq = (buildingTiles[i] - aim.Value).LengthSquared;
				if (sq >= bestSq)
					continue;

				best = buildingTiles[i];
				bestSq = sq;
			}

			return best;
		}

		/// <summary>
		/// REF-1 B1: the target field's resource cell nearest our frontier — the aim every crawl placement closes
		/// the gap toward (the maintainer's "every building placed to close the gap"). The field models differ
		/// (ResourceMap field vs the law's map-true component), so the law field nearest the target's centre supplies
		/// the cells; with no usable cells the centre stays the aim.
		/// </summary>
		internal static CPos? LinkTargetEdge(IReadOnlyList<RefineryField> fields, FieldScore target, IReadOnlyList<CPos> buildingTiles)
		{
			if (fields == null || fields.Count == 0 || buildingTiles == null || buildingTiles.Count == 0)
				return null;

			var field = fields.OrderBy(f => (f.Center - target.Center).LengthSquared).First();
			if (field.Cells == null || field.Cells.Count == 0)
				return null;

			CPos? best = null;
			var bestD = int.MaxValue;
			foreach (var c in field.Cells)
			{
				var d = buildingTiles.Min(b => (b - c).LengthSquared);
				if (d < bestD)
				{
					bestD = d;
					best = c;
				}
			}

			return best;
		}

		/// <summary>
		/// REF-1 (§12.24 v2): the field model — the map's valuable resource cells grouped into 8-connected components,
		/// scanned once (public map data: the initial geometry, never re-read for depletion — DESIGN §19.1b).
		/// </summary>
		void EnsureFieldModel()
		{
			if (fields != null)
				return;

			fields = new List<RefineryField>();
			var layer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			var valuable = resourceMap?.Info.ValuableResourceTypes;
			if (layer == null || valuable == null || valuable.Count == 0)
				return;

			var cells = new List<CPos>();
			foreach (var cell in world.Map.AllCells)
				if (valuable.Contains(layer.GetResource(cell).Type))
					cells.Add(cell);

			fields = ResourceFields(cells).Select((component, i) => new RefineryField(i, component)).ToList();
		}

		/// <summary>
		/// REF-1: the claim a produced refinery must serve, ranked live (pending commits are honoured at once).
		/// <paramref name="near"/> is the MCV-requested refinery's yard: within each tier the anchor nearest it wins.
		/// </summary>
		RefineryAnchorClaim? ComputeClaim(CPos? near)
		{
			var tick = world.WorldTick;
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			var order = ClaimOrder(anchors, anchorFieldIds, fieldCellsById, lastRefineryCells, lastBuildingTiles,
				serve, Info.ReachCells,
				i => AnchorBlockedForClaims(i, tick),
				i => AnchorCommittedForClaims(i, tick),
				near, out _, out _, out var tiers, out _, out _,
				RouteRank, CoverageUnknown);
			var oi = 0;
			while (oi < order.Count)
			{
				var best = order[oi];
				var anchor = anchors[best];
				var offers = (anchorOfferStreaks.TryGetValue(anchor, out var s) ? s : 0) + 1;

				// REPAIR-B3: an offer that returns uncommitted is a failed placement attempt. Re-offered
				// past RefineryClaimFailAttempts, the anchor's site search is stuck — park it for the
				// bounded claim-fail cooldown and hand the next claim instead of re-running the same
				// cancel/requeue cycle on it forever.
				if (offers > Info.RefineryClaimFailAttempts)
				{
					ParkFailedClaim(anchor, tick, "offered " + offers + " times uncommitted");
					order.RemoveAt(oi);
					continue;
				}

				anchorOfferStreaks[anchor] = offers;
				var fieldId = anchorFieldIds[best];
				var center = fieldId < fields.Count ? fields[fieldId].Center : anchor;
				return new RefineryAnchorClaim(anchor, center, fieldId, tiers[best], fieldCellsById[fieldId]);
			}

			return null;
		}

		void ParkFailedClaim(CPos anchor, int tick, string reason, object owner = null)
		{
			anchorParkedUntil[anchor] = (tick + Info.RefineryClaimFailParkTicks, owner);
			anchorOfferStreaks.Remove(anchor);
			var parked = $"AI ({player.ClientIndex}): REPAIR-B3 parked claim anchor {anchor} for {Info.RefineryClaimFailParkTicks} ticks ({reason}), at tick {tick}";
			Log.Write("debug", parked);
			AIUtils.BotDebug(parked);
		}

		/// <summary>
		/// REF-1: rebuild the per-tick anchor model — the fields (once), the spreader anchors, each
		/// anchor's field, the field cells by id, the field every refinery sits flush to and the anchor
		/// every refinery serves. Runs at most once per world tick: the scores loop's anchor-granular
		/// gate-B test (F1) and the claim update share the same build.
		/// </summary>
		void EnsureAnchorModel(List<CPos> refineryCells, List<IReadOnlyCollection<CPos>> refineryTiles, List<CPos> buildingTiles,
			List<(CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled)> refineryDocks = null)
		{
			if (anchorModelTick == world.WorldTick)
				return;

			anchorModelTick = world.WorldTick;
			EnsureFieldModel();
			lastRefineryCells = refineryCells;
			lastBuildingTiles = buildingTiles;

			// Fog honesty: spreaders are neutral map actors placed by the map author - public map data like the spawn points
			// (DESIGN 19.1b), never an enemy's state. Manifested in tools/audit/fog_honesty_manifest.json.
			var spreaders = new List<CPos>();
			foreach (var tp in world.ActorsWithTrait<ISeedableResource>())
			{
				if (tp.Actor.IsDead || !tp.Actor.IsInWorld || (tp.Trait is IDisabledTrait d && d.IsTraitDisabled))
					continue;

				spreaders.Add(tp.Actor.Location);
			}

			anchors = BuildAnchors(spreaders, fields.Select(f => f.Center), Info.SpreaderFieldRadiusCells);
			spreaderAnchorCount = spreaders.Distinct().Count();
			anchorFieldIds = AssignAnchorFields(anchors, spreaderAnchorCount,
				fields.Select(f => (IReadOnlyCollection<CPos>)f.Cells).ToList(),
				fields.Select(f => f.Center).ToList(), Info.SpreaderFieldRadiusCells);

			fieldCellsById = new List<IReadOnlyCollection<CPos>>();
			var maxField = anchorFieldIds.Length == 0 ? -1 : anchorFieldIds.Max();
			for (var f = 0; f <= maxField; f++)
				fieldCellsById.Add(f < fields.Count ? fields[f].Cells : (IReadOnlyCollection<CPos>)Array.Empty<CPos>());

			// R1: the flush-fields pass is gone — spec §30 forbids same-field inheritance beyond the
			// radius, so coverage eligibility is the geometric radius alone. RefineryFlushFields stays
			// a public helper (the placement side may still reason about gap-0/1), but nothing in the
			// coverage model consumes it.
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			lastRefineryDocks = refineryDocks ?? new List<(CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled)>();
			while (lastRefineryDocks.Count < refineryCells.Count)
				lastRefineryDocks.Add((new[] { refineryCells[lastRefineryDocks.Count] }, null, new[] { true }));

			// REPAIR-B3: the coverage topology version — anchors, refineries (cells+docks) and own
			// blockers (footprint cells + building top-lefts) fold into one order-independent signature.
			// A changed signature re-opens every verdict; a witness survives only when none of its
			// recorded path cells changed. Docks are footprint-adjacent, so buildingTiles already covers
			// the cells a harvester actually enters.
			var blockers = new HashSet<CPos>(buildingTiles);
			foreach (var cells in refineryTiles)
				if (cells != null)
					blockers.UnionWith(cells);
			foreach (var r in lastRefineryDocks)
				if (r.Cells != null)
					blockers.UnionWith(r.Cells);

			var newSignature = anchors.Count * 31 + blockers.Count;
			foreach (var a in anchors)
				newSignature = newSignature * 31 + a.X * 1024 + a.Y;
			foreach (var c in refineryCells)
				newSignature ^= c.X * 32768 + c.Y;

			// The blocker cells themselves, not just their count — a same-count swap must re-open
			// the verdicts. XOR over the set is order-independent (HashSet order is an impl detail).
			foreach (var c in blockers)
				newSignature ^= unchecked(c.X * 73856093 + c.Y * 19349663);

			// R1: dock enabled state is part of the eligibility contract — a dock that wakes or dies
			// must re-open the verdicts it could never satisfy while disabled.
			foreach (var r in lastRefineryDocks)
				if (r.Enabled != null)
					foreach (var e in r.Enabled)
						newSignature = newSignature * 31 + (e ? 1 : 0);

			if (coverageBlockerCells == null || newSignature != coverageModelSignature)
			{
				// First build, or the model changed: every verdict re-opens and the cursor restarts.
				// Witnesses keep only when their recorded route avoids every changed blocker cell.
				var changed = coverageBlockerCells == null
					? null
					: new HashSet<CPos>(blockers.Where(c => !coverageBlockerCells.Contains(c))
						.Concat(coverageBlockerCells.Where(c => !blockers.Contains(c))));
				coverageVersion++;
				coverageModelSignature = newSignature;
				coverageBlockerCells = blockers;
				anchorCoverageVerdict = new byte[anchors.Count];
				coverageRanks.Clear();
				patchProbeProgress.Clear();
				patchCellsCache.Clear();
				evaluatedSites.Clear();
				coverageCursor = 0;
				coveragePendingAnchors = anchors.Count;
				coverageSweepScanned = 0;
				coverageFirstBudgetDeferred = -1;
				coverageBudget = new RefineryProbeBudget(Info.CoverageSiteLimit, Info.CoverageProbeLimit, Info.CoverageProbesPerTick);

				if (changed != null && routeWitnesses.Count > 0)
				{
					foreach (var kv in routeWitnesses.ToList())
					{
						// A stored path survives only when it avoids every changed cell. An
						// unreachable verdict stores no cells at all, so it can never observe a
						// REMOVED blocker — it must re-probe on any blocker change.
						if (!routeWitnessPaths.TryGetValue(kv.Key, out var path)
							|| (path.Count == 0 && changed.Count > 0)
							|| path.Any(changed.Contains))
							routeWitnesses.Remove(kv.Key);
						else
							routeWitnesses[kv.Key] = new RefineryRouteWitness(kv.Value.Reachable, kv.Value.RouteMilli, kv.Value.TravelMilli, coverageVersion);
					}

					foreach (var kv in routeWitnessPaths.ToList())
						if (!routeWitnesses.ContainsKey(kv.Key))
							routeWitnessPaths.Remove(kv.Key);
				}
				else if (routeWitnesses.Count > 0)
				{
					// Witnesses exist but nothing changed for them — adopt them under the new version.
					foreach (var kv in routeWitnesses.ToList())
						routeWitnesses[kv.Key] = new RefineryRouteWitness(kv.Value.Reachable, kv.Value.RouteMilli, kv.Value.TravelMilli, coverageVersion);
				}

				// A dock that no longer exists can never be probed again — drop its stale keys so the
				// cache stays bounded by the live refinery set.
				var liveDocks = new HashSet<CPos>();
				foreach (var r in lastRefineryDocks)
					if (r.Cells != null)
						liveDocks.UnionWith(r.Cells);

				if (routeWitnesses.Count > 0)
				{
					foreach (var kv in routeWitnesses.ToList())
						if (!liveDocks.Contains(kv.Key.Dock))
							routeWitnesses.Remove(kv.Key);
					foreach (var kv in routeWitnessPaths.ToList())
						if (!liveDocks.Contains(kv.Key.Dock) || !routeWitnesses.ContainsKey(kv.Key))
							routeWitnessPaths.Remove(kv.Key);
				}
			}

			lastAssigned = AssignRefineries(anchors, refineryCells, serve, RouteRank);
		}

		/// <summary>REPAIR-B3: the route-verified pair rank for <see cref="AssignRefineries"/> — null while
		/// the oracle has not proven the pair (the anchor then reads as UNKNOWN, not unserved).</summary>
		long? RouteRank(int a, int r) =>
			coverageRanks.TryGetValue((a, r), out var rank) ? rank : (long?)null;

		bool CoverageUnknown(int a) =>
			a < anchorCoverageVerdict.Length && anchorCoverageVerdict[a] == (byte)RefineryCoverageVerdict.Unknown;

		/// <summary>
		/// REPAIR-B3: a refinery's dock geometry — every DockHost's dock cell (the trait's own
		/// DockPosition, never a geometric guess), its type set, and whether it can actually serve
		/// clients right now (R1: enabled+in-world is part of the entry/exit contract — a disabled
		/// or selling dock cannot unload a harvester). A refinery without a dock trait keeps its
		/// top-left as the route origin with wildcard compatibility (exotic mod).
		/// </summary>
		static (CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled) RefineryDockGeometry(Actor a)
		{
			var docks = a.TraitsImplementing<DockHost>().ToArray();
			if (docks.Length == 0)
				return (new[] { a.Location }, null, new[] { true });

			return (docks.Select(d => a.World.Map.CellContaining(d.DockPosition)).ToArray(),
				docks.Select(d => d.Info.Type).ToArray(),
				docks.Select(d => d.IsEnabledAndInWorld).ToArray());
		}

		/// <summary>
		/// REPAIR-B3: the faction's harvester locomotors and dock types, resolved once from the resource
		/// map's HarvesterTypes (the role rollout's name list). Empty list — a mod that never lists them —
		/// falls back to every rules-listed harvester so the probe still has a vehicle to measure with.
		/// Deterministic: names sorted, locomotors deduped, rules immutable per match.
		/// </summary>
		(Locomotor Locomotor, BitSet<DockType> DockType, MobileInfo Mobile)[] HarvesterSpecs()
		{
			if (harvesterSpecs != null)
				return harvesterSpecs;

			var names = resourceMap?.Info.HarvesterTypes;
			var infos = (names == null || names.Count == 0
					? world.Map.Rules.Actors.Values.Where(ai => ai.HasTraitInfo<HarvesterInfo>())
					: names.Where(n => world.Map.Rules.Actors.TryGetValue(n, out var ai) && ai.HasTraitInfo<HarvesterInfo>())
						.Select(n => world.Map.Rules.Actors[n]))
				.OrderBy(ai => ai.Name, StringComparer.Ordinal);

			var locos = world.WorldActor.TraitsImplementing<Locomotor>().ToArray();
			var list = new List<(Locomotor Locomotor, BitSet<DockType> DockType, MobileInfo Mobile)>();
			foreach (var ai in infos)
			{
				var mobile = ai.TraitInfoOrDefault<MobileInfo>();
				if (mobile?.Locomotor == null)
					continue;

				var loco = locos.FirstOrDefault(l => l.Info.Name == mobile.Locomotor);
				if (loco == null || list.Any(s => ReferenceEquals(s.Locomotor, loco)))
					continue;

				list.Add((loco, ai.TraitInfoOrDefault<HarvesterInfo>()?.Type ?? default, mobile));
			}

			return harvesterSpecs = list.ToArray();
		}

		/// <summary>
		/// REPAIR-B3: the patch cells a harvester would mine at anchor <paramref name="a"/>, ordered
		/// nearest-first — the anchor cell itself when the field records none. R2: the FULL candidate
		/// set, not just the nearest CoveragePatchCellSample — a pair's walk advances only the knob's
		/// window per refresh and resumes where it stopped, so a refinery is never declared unserving
		/// while unexamined cells could still prove coverage. Deterministic order: distance, then X,
		/// then Y. R4b: the sorted list is cached per (anchor, model version) — the ordering is
		/// fixed while the topology is, so a field is sorted at most once per refresh generation
		/// rather than once per pass (the VP's materialization-profile note).
		/// </summary>
		IReadOnlyList<CPos> PatchCells(int a)
		{
			var anchor = anchors[a];
			if (patchCellsCache.TryGetValue(a, out var cached)
				&& cached.Version == coverageVersion && cached.Anchor == anchor)
				return cached.Ordered;

			var fieldId = a < anchorFieldIds.Length ? anchorFieldIds[a] : -1;
			var cells = fieldId >= 0 && fieldId < fieldCellsById.Count ? fieldCellsById[fieldId] : null;
			var ordered = cells == null || cells.Count == 0
				? new[] { anchor }
				: cells.OrderBy(c => (c - anchor).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).ToArray();
			patchCellsCache[a] = (coverageVersion, anchor, ordered);
			return ordered;
		}

		/// <summary>
		/// REPAIR-B3: the witness cache — a stored verdict under the current topology version is free
		/// evidence; anything else costs probe budget. Returns null when the budget refused the probe —
		/// the pair is then deferred (UNKNOWN), never counted as failure. R1: the witness also carries
		/// the estimated simulation travel time — the harvester spec's MobileInfo.Speed adjusted by the
		/// locomotor's terrain speed percentage, the same product Mobile.MovementSpeedForCell computes
		/// minus actor-bound modifiers (those are transient by design exclusion).
		/// </summary>
		RefineryRouteWitness? WitnessOrProbe(CPos dock, CPos patch,
			(Locomotor Locomotor, BitSet<DockType> DockType, MobileInfo Mobile) spec, bool outbound)
		{
			var key = (dock, patch, spec.Locomotor.Info.Name, outbound);
			if (routeWitnesses.TryGetValue(key, out var w))
			{
				if (w.Version == coverageVersion)
				{
					coverageBudget.CacheHits++;
					return w;
				}

				routeWitnesses.Remove(key);
				routeWitnessPaths.Remove(key);
			}

			if (!coverageBudget.TryConsumeProbe())
				return null;

			var path = RefineryRouteProbe.ProbeLeg(world, spec.Locomotor, outbound ? dock : patch, outbound ? patch : dock);
			var travel = RefineryCoverageOracle.RouteTravelMilli(path,
				c => Util.ApplyPercentageModifiers(spec.Mobile.Speed, new[] { spec.Locomotor.MovementSpeedForCell(c) }));
			w = new RefineryRouteWitness(path.Count > 0, RefineryCoverageOracle.RouteLengthMilli(path), travel, coverageVersion);
			routeWitnesses[key] = w;
			routeWitnessPaths[key] = path;
			return w;
		}

		/// <summary>
		/// REPAIR-B3: the per-tick coverage step — rebuilds the anchor/refinery model inputs from the
		/// trait-indexed actor sets (same predicates as the replan's own-actor pass, so whichever ran
		/// first this tick wins) and resumes the persistent-cursor refresh under the probe budget.
		/// </summary>
		void CoverageTick()
		{
			var refineryCells = new List<CPos>();
			var refineryTiles = new List<IReadOnlyCollection<CPos>>();
			var refineryDocks = new List<(CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled)>();
			var buildingTiles = new List<CPos>();
			foreach (var tp in world.ActorsWithTrait<Refinery>())
			{
				var a = tp.Actor;
				if (a.Owner != player || a.IsDead || !a.IsInWorld)
					continue;

				refineryCells.Add(a.Location);
				refineryTiles.Add(a.Info.TraitInfos<BuildingInfo>().FirstOrDefault()?.Tiles(a.Location).ToList());
				refineryDocks.Add(RefineryDockGeometry(a));
			}

			foreach (var tp in world.ActorsWithTrait<GivesBuildableArea>())
			{
				var a = tp.Actor;
				if (a.Owner != player || a.IsDead || !a.IsInWorld)
					continue;

				var bi = a.Info.TraitInfos<BuildingInfo>().FirstOrDefault();
				if (bi != null)
					buildingTiles.AddRange(bi.Tiles(a.Location));
			}

			EnsureAnchorModel(refineryCells, refineryTiles, buildingTiles, refineryDocks);
			EvaluateCoverageTick();
		}

		/// <summary>
		/// REPAIR-B3 (SPEC §4, R1+R2): the bounded refresh sweep — anchors resume at the persistent
		/// cursor, each (anchor, refinery) candidate pair is gated by the geometric radius alone (R1:
		/// no same-field inheritance — a far flush refinery does not serve), then requires a DockHost
		/// that is enabled, in-world, and type-compatible (the loaded-return contract), then probes
		/// both directed legs under the stationary-obstacle model. R2: a pair walks its anchor's whole
		/// field in CoveragePatchCellSample-cell windows across refreshes (per-pair cursor persisted
		/// under the model version) — Unserved is only ever declared after every patch candidate was
		/// examined, and a window- or budget-deferred pair keeps the anchor UNKNOWN. Site/probe
		/// counters cap one PASS over the anchors; a ceiling-cut pass re-arms at the boundary so
		/// deferred work resumes instead of starving. The per-tick probe limit only paces the spend
		/// (a tick-stopped anchor rewinds one cursor step and resumes next tick — paid witnesses
		/// persist). An anchor with any verified pair is Covered at the best pair's travel-time rank.
		/// </summary>
		void EvaluateCoverageTick()
		{
			if (anchorCoverageVerdict.Length != anchors.Count)
			{
				// Defensive: the signature block normally reallocates verdicts and clears ranks
				// together; a stray length mismatch clears all index-keyed state.
				anchorCoverageVerdict = new byte[anchors.Count];
				coverageRanks.Clear();
				patchProbeProgress.Clear();
				patchCellsCache.Clear();
				evaluatedSites.Clear();
			}

			if (anchors.Count == 0)
			{
				coveragePendingAnchors = 0;
				return;
			}

			var specs = HarvesterSpecs();
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			var r2 = (long)serve * serve;
			var legLimit = Info.CoverageRouteLimitCells * RefineryCoverageOracle.MilliPerCell;
			var hadFirstBudgetDeferred = coverageFirstBudgetDeferred;
			coverageBudget.NewTick();

			var n = anchors.Count;
			var scanned = 0;
			var rearmCoverageBudget = false;
			while (scanned < n && coverageBudget.TickProbesOpen)
			{
				var a = coverageCursor % n;
				coverageCursor++;
				scanned++;
				coverageSweepScanned++;
				if (coverageCursor % n == 0 && coverageFirstBudgetDeferred >= 0)
					rearmCoverageBudget = true;
				if (anchorCoverageVerdict[a] != (byte)RefineryCoverageVerdict.Unknown)
					continue;

				var anyCovered = false;
				var anyDeferred = false;
				var tickCapHit = false;
				var patches = PatchCells(a);
				for (var r = 0; r < lastRefineryCells.Count && r < lastRefineryDocks.Count; r++)
				{
					var d = (anchors[a] - lastRefineryCells[r]).LengthSquared;
					if (d > r2)
						continue;

					// Whole-refresh ceiling (R1): the site cap charges a pair once per refresh —
					// a pair already admitted stays free to re-derive from cached witnesses on a
					// tick-paced revisit, so pacing can never drain the site budget twice.
					if (!evaluatedSites.Contains((a, r)))
					{
						if (!coverageBudget.TryConsumeSite())
						{
							anyDeferred = true;
							if (coverageFirstBudgetDeferred < 0)
								coverageFirstBudgetDeferred = a;

							break;
						}

						evaluatedSites.Add((a, r));
					}

					var pairCovered = false;
					var pairRank = long.MaxValue;
					var (cells, types, enabled) = lastRefineryDocks[r];

					// R2+R3: patch-outer walk with a persisted per-pair cursor — the cell index may
					// only advance after every eligible (dock, spec) examined it, and the window
					// moves at most CoveragePatchCellSample cells per refresh. Any interruption —
					// tick pacing or a probe-cap refusal — leaves the cursor ON the unexamined cell
					// (R3: an unconditional for-update would skip it into a false Unserved). A pair
					// whose window ends before the field does stays UNKNOWN; only a fully-walked
					// field contributes to Unserved.
					var pi = patchProbeProgress.TryGetValue((a, r), out var progress) && progress.Version == coverageVersion
						? progress.Index : 0;
					var walk = new PatchWalk(pi, patches.Count, Info.CoveragePatchCellSample);
					while (walk.HasCell && !pairCovered && !tickCapHit && !coverageBudget.ProbeCapSpent)
					{
						var patch = patches[walk.Index];
						var cellExamined = true;
						for (var di = 0; cells != null && di < cells.Length && !pairCovered && !tickCapHit && cellExamined; di++)
						{
							// R1: real DockHost eligibility — enabled and in-world, type-overlapping.
							// A null Types array (no DockHost — exotic mod) is wildcard-compatible with
							// the synthetic "enabled" flag RefineryDockGeometry fills for it.
							var dockEnabled = enabled != null && di < enabled.Length && enabled[di];
							BitSet<DockType>? dockType = types != null && di < types.Length ? types[di] : (BitSet<DockType>?)null;
							foreach (var spec in specs)
							{
								if (pairCovered || tickCapHit)
									break;

								if (!RefineryCoverageOracle.DockEligible(dockEnabled, spec.DockType, dockType))
									continue;

								var outbound = WitnessOrProbe(cells[di], patch, spec, true);
								var inbound = outbound == null ? null : WitnessOrProbe(cells[di], patch, spec, false);
								if (outbound == null || inbound == null)
								{
									// Probe-cap refusal = deferred for this refresh; tick-cap
									// refusal = pacing — rewind so this anchor resumes next tick
									// (paid witnesses persist across both). The cell stays
									// unexamined either way: pi must NOT advance (R3).
									anyDeferred = true;
									cellExamined = false;
									if (coverageBudget.ProbeCapSpent)
									{
										if (coverageFirstBudgetDeferred < 0)
											coverageFirstBudgetDeferred = a;
									}
									else
										tickCapHit = true;

									break;
								}

								if (!RefineryCoverageOracle.BothLegsCover(outbound.Value, inbound.Value, legLimit))
									continue;

								pairCovered = true;
								pairRank = RefineryCoverageOracle.LegPairRank(outbound.Value, inbound.Value);
								break;
							}
						}

						if (!cellExamined)
							break;

						walk.CompleteCell();
					}

					patchProbeProgress[(a, r)] = (coverageVersion, walk.Index);
					if (RefineryCoverageOracle.PairDeferredAfterWalk(pairCovered, walk, patches.Count))
					{
						// R4: unexamined cells defer the pair no matter WHAT stopped the walk —
						// window end, tick pacing (already marked above, idempotent), or a probe
						// ceiling the final SUCCESSFUL probe landed exactly on. The R3 form also
						// required !ProbeCapSpent, so a clean cap exit suppressed the defer →
						// false Unserved that cascaded: every later anchor exited its walk at
						// entry, aggregated Unserved, and no deferred marker meant no re-arm.
						anyDeferred = true;
						if (coverageBudget.ProbeCapSpent && coverageFirstBudgetDeferred < 0)
							coverageFirstBudgetDeferred = a;
					}

					if (pairCovered)
					{
						anyCovered = true;
						coverageRanks[(a, r)] = pairRank;
					}
					else
						coverageRanks.Remove((a, r));

					if (tickCapHit)
						break;
				}

				anchorCoverageVerdict[a] = (byte)RefineryCoverageOracle.Aggregate(anyCovered, anyDeferred);
				if (tickCapHit)
				{
					coverageCursor--;
					break;
				}
			}

			// One pass = one refresh (SPEC §54): the site/probe ceilings bound each pass, so a pass
			// the ceiling cut short re-arms at the boundary — deferred anchors resume in the next
			// pass instead of starving UNKNOWN on a permanently spent budget. The per-tick pace is
			// untouched (NewTick runs at sweep start; this budget starts clean for next tick).
			if (rearmCoverageBudget)
			{
				coverageBudget = new RefineryProbeBudget(Info.CoverageSiteLimit, Info.CoverageProbeLimit, Info.CoverageProbesPerTick);
				evaluatedSites.Clear();
				coverageFirstBudgetDeferred = -1;
				Log.Write("debug", $"AI ({player.ClientIndex}): REPAIR-B3 coverage pass ceiling hit at tick {world.WorldTick}: refresh budget re-armed for the next pass");
			}

			var hadPending = coveragePendingAnchors;
			coveragePendingAnchors = anchorCoverageVerdict.Count(v => v == (byte)RefineryCoverageVerdict.Unknown);

			// SPEC §56 (R1): the refresh telemetry now carries what a reviewer needs to audit the
			// spend — candidate sites evaluated, directed probes consumed, deferred candidates,
			// witness cache hits — plus the first anchor a refresh ceiling refused. Logged on the
			// transition to fully resolved, once when a refresh ceiling first defers an anchor,
			// and whenever the per-tick pacing stops the sweep (a deferred anchor is retried,
			// never folded into Unserved).
			if (coveragePendingAnchors == 0 && hadPending > 0)
			{
				var line = $"AI ({player.ClientIndex}): REPAIR-B3 coverage refresh complete at tick {world.WorldTick}: {anchors.Count} anchors, {coverageSweepScanned} anchor-scans, {coverageBudget.SitesEvaluated} sites, {coverageBudget.ProbesUsed} probes, {coverageBudget.DeferredCandidates} deferred candidates, {coverageBudget.CacheHits} witness cache hits";
				Log.Write("debug", line);
				AIUtils.BotDebug(line);
			}

			if (coverageFirstBudgetDeferred >= 0 && hadFirstBudgetDeferred < 0)
			{
				var line = $"AI ({player.ClientIndex}): REPAIR-B3 coverage refresh ceiling reached at tick {world.WorldTick}: anchor {coverageFirstBudgetDeferred} first deferred, {coverageBudget.SitesEvaluated}/{coverageBudget.SiteLimit} sites, {coverageBudget.ProbesUsed}/{coverageBudget.ProbeLimit} probes spent; {coveragePendingAnchors} anchors stay Unknown until the next refresh";
				Log.Write("debug", line);
				AIUtils.BotDebug(line);
			}
			else if (!coverageBudget.TickProbesOpen && coveragePendingAnchors > 0)
				Log.Write("debug", $"AI ({player.ClientIndex}): REPAIR-B3 coverage tick pacing at tick {world.WorldTick}: {coveragePendingAnchors} anchors pending, resume at cursor {coverageCursor}");

			// Verified pairs may have moved the assignment since the model build — recompute so served
			// and the pending-commit clears read the live verdict, not this morning's.
			var serveR = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			lastAssigned = AssignRefineries(anchors, lastRefineryCells, serveR, RouteRank);
		}

		/// <summary>
		/// REF-1 (§12.24 v2): rebuild the anchors (spreaders are public map data, like the spawn points, plus the centres of
		/// spreaderless fields), attach each to its resource field, and publish the claim order — tier 1: every unserved
		/// field's best spreader in reach, home first; tier 2: covered fields' extra spreaders. A claim that keeps failing
		/// is parked; a committed one is pending until its refinery lands.
		/// </summary>
		void UpdateAnchorClaim(List<CPos> refineryCells, List<IReadOnlyCollection<CPos>> refineryTiles, List<CPos> buildingTiles,
			List<(CPos[] Cells, BitSet<DockType>[] Types, bool[] Enabled)> refineryDocks = null)
		{
			EnsureAnchorModel(refineryCells, refineryTiles, buildingTiles, refineryDocks);

			anchorClaim = null;
			anchorClaimFieldCenter = null;
			wantsRefinery = false;
			unservedInReach = 0;
			unservedFieldsInReach = 0;
			unclaimedAnchorsInReach = 0;
			claimableAnchorsInReach = 0;
			unservedBeyondReach = 0;
			if (anchors.Count == 0)
				return;

			var tick = world.WorldTick;
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;

			// Pending commits clear when their refinery landed (the anchor is served) or the commit expired.
			foreach (var kv in anchorPendingUntil.ToList())
			{
				var index = anchors.IndexOf(kv.Key);
				if (PendingCommitCleared(tick, kv.Value, index >= 0 && lastAssigned[index] >= 0))
					anchorPendingUntil.Remove(kv.Key);
			}

			// ECON-A-FIX (R6): the same sweep retires demand reservations — expired holds, and holds whose
			// anchor got served or pending-committed under them. A park no longer retires a live hold
			// (lead ruling 2026-10-10): the failing owner's holds are released at the failure point, and
			// a foreign hold on a parked anchor stays valid so the anchor recycles to its owner.
			anchorReservations.Prune(tick, a => AnchorServedOrPending(a, tick));

			var queued = 0;
			var builder = baseBuilders.FirstOrDefault(t => t.IsTraitEnabled());
			if (builder?.BuildingsBeingProduced != null)
				foreach (var r in builder.Info.RefineryTypes)
					if (builder.BuildingsBeingProduced.TryGetValue(r, out var n))
						queued += n;

			var order = ClaimOrder(anchors, anchorFieldIds, fieldCellsById, refineryCells, buildingTiles,
				serve, Info.ReachCells,
				i => AnchorBlockedForClaims(i, tick),
				i => AnchorCommittedForClaims(i, tick),
				null, out unservedInReach, out unservedFieldsInReach, out _,
				out claimableAnchorsInReach, out unservedBeyondReach,
				RouteRank, CoverageUnknown);
			unclaimedAnchorsInReach = order.Count;

			// A refinery is wanted only while more anchors are claimable than refineries already in flight — never by
			// comparing totals (a duplicate stacked at home must not spend a forward anchor's quota).
			wantsRefinery = AnchorRefineryWanted(order.Count, queued);
			if (order.Count == 0)
			{
				anchorStuck = (null, 0, refineryCells.Count);
				return;
			}

			var anchor = anchors[order[0]];

			// Loop guard: the same anchor wanted replan after replan with no refinery gained and none in production.
			var (replans, parkAnchor) = AnchorStuckNext(anchorStuck.Anchor == anchor, refineryCells.Count,
				anchorStuck.Refineries, queued, anchorStuck.Replans, Info.AnchorStuckReplans);
			anchorStuck = (anchor, replans, refineryCells.Count);
			if (parkAnchor)
			{
				anchorParkedUntil[anchor] = (tick + Info.ParkTicks, null);
				anchorStuck = (null, 0, refineryCells.Count);
				var parked = $"AI ({player.ClientIndex}): REF-1 parked anchor {anchor} for {Info.ParkTicks} ticks: wanted {replans} re-plans with no refinery placed, at tick {tick}";
				Log.Write("debug", parked);
				AIUtils.BotDebug(parked);
				return;
			}

			anchorClaim = anchor;
			var claimFieldId = anchorFieldIds[order[0]];
			anchorClaimFieldCenter = claimFieldId < fields.Count ? fields[claimFieldId].Center : anchor;
		}

		void Idle(string reason)
		{
			if (reason == lastIdleReason)
				return;

			lastIdleReason = reason;
			var line = $"AI ({player.ClientIndex}): EX-0 no target at tick {world.WorldTick}: {reason}";
			Log.Write("debug", line);
			AIUtils.BotDebug(line);
		}

		/// <summary>The planner's building-queue types, resolved per call: the base builder's condition may
		/// switch on after ours.</summary>
		IEnumerable<string> BuildingQueueTypes() =>
			Info.BuildingQueues.Count > 0 ? Info.BuildingQueues
				: baseBuilders.FirstOrDefault(t => t.IsTraitEnabled())?.Info.BuildingQueues ?? (IEnumerable<string>)new[] { "Building" };

		/// <summary>
		/// REF-1 B3: the refinery estimate for field scoring — the live buildable, else the remembered one, else the
		/// rules-listed fallback (evaluated lazily). A tuple with a null Info marks "none". Pure, for the tests.
		/// </summary>
		public static (ActorInfo Info, int Cost, int BuildTicks) RefineryEstimateOrFallback(
			(ActorInfo Info, int Cost, int BuildTicks) buildable, (ActorInfo Info, int Cost, int BuildTicks) remembered,
			Func<(ActorInfo Info, int Cost, int BuildTicks)> rulesFallback) =>
			buildable.Info != null ? buildable : remembered.Info != null ? remembered : rulesFallback();

		/// <summary>
		/// REF-1 B3: a refinery the faction could field once its prerequisites are met — the estimate only feeds
		/// field scoring's payback term, never a build decision. Cheapest rules-listed refinery wins; ties by name
		/// keep it deterministic.
		/// </summary>
		(ActorInfo Info, int Cost, int BuildTicks) RulesRefineryEstimate()
		{
			var cheapest = world.Map.Rules.Actors.Values
				.Where(a => a.HasTraitInfo<RefineryInfo>() && a.HasTraitInfo<BuildableInfo>() && a.HasTraitInfo<BuildingInfo>())
				.OrderBy(a => a.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? int.MaxValue)
				.ThenBy(a => a.Name, StringComparer.Ordinal)
				.FirstOrDefault();
			if (cheapest == null)
				return default;

			return (cheapest, cheapest.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? 0,
				Math.Max(0, cheapest.TraitInfos<BuildableInfo>().FirstOrDefault()?.BuildDuration ?? 0));
		}

		/// <summary>
		/// REF-1 B1 (maintainer ruling): a crawl link must EXTEND the buildable area (GivesBuildableArea — a silo or
		/// other non-provider placed forward closes no gap) and should be useful — power plants are preferred since
		/// they also feed defences. No cost cap: the advanced plant is still a valid link when it is the only one.
		/// </summary>
		public static bool IsCrawlLink(ActorInfo a) =>
			a.HasTraitInfo<BuildableInfo>() && a.HasTraitInfo<BuildingInfo>()
			&& a.HasTraitInfo<GivesBuildableAreaInfo>() && !a.HasTraitInfo<RefineryInfo>();

		public static bool IsPowerPlant(ActorInfo a) => a.TraitInfos<PowerInfo>().Any(p => p.Amount > 0);

		public static (ActorInfo Info, int Cost, int BuildTicks) PickCrawlLink(
			(ActorInfo Info, int Cost, int BuildTicks) power, (ActorInfo Info, int Cost, int BuildTicks) other) =>
			power.Info != null ? power : other;

		((ActorInfo Info, int Cost, int BuildTicks) Refinery, (ActorInfo Info, int Cost, int BuildTicks) Link,
			(ActorInfo Info, int Cost, int BuildTicks) CrawlLink, (ActorInfo Info, int Cost, int BuildTicks) Defence,
			int Queues) CheapestBuildables()
		{
			(ActorInfo Info, int Cost, int BuildTicks) refinery = default, link = default, linkPower = default, linkOther = default, defence = default;
			var queues = 0;
			foreach (var queue in BuildingQueueTypes().SelectMany(type => CAAIUtils.FindQueues(player, type)).Distinct())
			{
				queues++;
				foreach (var item in queue.BuildableItems())
				{
					var bi = item.TraitInfos<BuildableInfo>().FirstOrDefault();
					if (bi == null || !item.HasTraitInfo<BuildingInfo>())
						continue;

					var cost = item.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? 0;
					var entry = (item, cost, queue.GetBuildTime(item, bi));
					// The Refinery trait, from rules: the by-name lists were emptied by the role rollout (§2.8).
					if (item.HasTraitInfo<RefineryInfo>())
					{
						if (refinery.Info == null || cost < refinery.Cost)
							refinery = entry;
					}
					else
					{
						if (cost > 0 && (link.Info == null || cost < link.Cost))
							link = entry;

						// REF-1 B1: the law's crawl link is the cheapest GBA structure, power plants first.
						if (IsCrawlLink(item))
						{
							if (IsPowerPlant(item))
							{
								if (linkPower.Info == null || cost < linkPower.Cost)
									linkPower = entry;
							}
							else if (linkOther.Info == null || cost < linkOther.Cost)
								linkOther = entry;
						}

						// CRAWL-SAFETY: the front defence is the cheapest armed structure — a turret
						// standing at the frontier, not a wall (walls carry no AttackBase).
						if (cost > 0 && item.HasTraitInfo<AttackBaseInfo>()
							&& (defence.Info == null || cost < defence.Cost))
							defence = entry;
					}
				}
			}

			return (refinery, link, PickCrawlLink(linkPower, linkOther), defence, queues);
		}
	}
}
