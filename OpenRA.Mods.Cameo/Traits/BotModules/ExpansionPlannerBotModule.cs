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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
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
		IBotMcvExpansionSiteProvider, IBotPositionsUpdated, IBotExpansionAssistProvider
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
		int[] lastRefineryFields = Array.Empty<int>();
		int unclaimedAnchorsInReach;
		int unservedFieldsInReach;
		int claimableAnchorsInReach;
		int unservedBeyondReach;

		// REF-1 B1 (§12.24 v2): the planner's own crawl supply — the cheapest crawl-eligible building it wants
		// produced while the target field is out of reach, and the aim refined to the field's resource edge.
		string wantedLinkBuilding;
		CPos? crawlTargetEdge;

		// REF-1 B2: the cheapest buildable provider of a due MCV's missing prerequisite (td_gdi: the repair
		// facility). REF-1 B3: the last refinery estimate seen, kept so a transiently unbuildable refinery no
		// longer silences Target, the anchor claim and RequestMcv.
		string wantedMcvPrerequisite;
		(ActorInfo Info, int Cost, int BuildTicks) lastRefineryEstimate;

		readonly Dictionary<CPos, int> anchorParkedUntil = new();
		readonly Dictionary<uint, CPos> inflightMcvSites = new();
		List<CPos> ownBuildingCells = new();
		List<CPos> ownYardCells = new();

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

			var into = vehicle.TraitInfoOrDefault<TransformsInfo>()?.IntoActor;
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

		void IBotExpansionTargetProvider.RefineryClaimCommitted(CPos anchor)
		{
			if (LawActive)
				anchorPendingUntil[anchor] = world.WorldTick + Info.AnchorClaimPendingTicks;
		}

		int IBotExpansionTargetProvider.UnclaimedAnchorsInReach => LawActive ? unclaimedAnchorsInReach : 0;

		// REF-1 B1/B2/B4 (§12.24 v2): the planner's crawl-supply want, the refined edge aim, the due MCV's
		// missing prerequisite, and the expansion-nudge metric — published only while the law runs, so classic
		// and switch-off see the interface defaults (unchanged behaviour).
		string IBotExpansionTargetProvider.WantedLinkBuilding =>
			LawActive && Info.DriveBaseCrawl ? wantedLinkBuilding : null;

		CPos? IBotExpansionTargetProvider.CrawlTargetEdge =>
			LawActive && Info.DriveBaseCrawl ? crawlTargetEdge : null;

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
		/// FE-1: which refinery serves which anchor - a result per anchor, the refinery index or -1. Greedy by ascending
		/// distance (ties by anchor then refinery index) within `serveRadiusCells`; every refinery serves at most one anchor
		/// and every anchor has at most one refinery, so two spreaders next to one refinery do not both count as served.
		/// REF-1 (v2 fix): <paramref name="refineryFields"/> gives the field each refinery's footprint sits flush to —
		/// a refinery on a field's far edge can land farther than `serveRadiusCells` from the spreader yet still be that
		/// field's refinery, so field-mates are eligible too; the distance order still binds the nearest anchor first.
		/// </summary>
		public static int[] AssignRefineries(IReadOnlyList<CPos> anchorCells, IReadOnlyList<CPos> refineries, int serveRadiusCells,
			IReadOnlyList<int> anchorField = null, IReadOnlyList<int> refineryFields = null)
		{
			var result = new int[anchorCells.Count];
			Array.Fill(result, -1);
			var r2 = (long)serveRadiusCells * serveRadiusCells;
			var pairs = new List<(long D, int A, int R)>();
			for (var a = 0; a < anchorCells.Count; a++)
				for (var r = 0; r < refineries.Count; r++)
				{
					var d = (anchorCells[a] - refineries[r]).LengthSquared;
					var flush = anchorField != null && refineryFields != null
						&& r < refineryFields.Count && refineryFields[r] >= 0 && anchorField[a] == refineryFields[r];
					if (d <= r2 || flush)
						pairs.Add((d, a, r));
				}

			pairs.Sort((x, y) => x.D != y.D ? x.D.CompareTo(y.D) : x.A != y.A ? x.A.CompareTo(y.A) : x.R.CompareTo(y.R));
			var used = new bool[refineries.Count];
			foreach (var (_, a, r) in pairs)
			{
				if (result[a] >= 0 || used[r])
					continue;

				result[a] = r;
				used[r] = true;
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
			IReadOnlyList<CPos> refineries, IReadOnlyList<int> refineryFields, IReadOnlyList<CPos> buildingTiles,
			int serveRadiusCells, int reachCells,
			Func<int, bool> blocked, Func<int, bool> committed, CPos? near,
			out int unservedAnchorsInReach, out int unservedFieldsInReach, out int[] anchorTier,
			out int claimableAnchorsInReach, out int unservedAnchorsBeyondReach)
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

			var assigned = AssignRefineries(anchorCells, refineries, serveRadiusCells, anchorField, refineryFields);
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
			var fieldCovered = new bool[fieldCount];
			for (var a = 0; a < n; a++)
			{
				served[a] = assigned[a] >= 0;
				var field = anchorField[a];
				reachDist[a] = CellsOf(field) is { Count: > 0 }
					? fieldReach[field]
					: anchorDist[a];

				var inReach = reachDist[a] <= reachCells;
				if (!served[a] && inReach)
					unservedAnchorsInReach++;
				if (!served[a] && !inReach)
					unservedAnchorsBeyondReach++;
				if (served[a] || (committed != null && committed(a)))
					fieldCovered[field] = true;
			}

			// Tier-1 backlog metric: a field in reach with no serving and no pending refinery still waits for its first
			// one — parked anchors count too (the backlog is honest even while it cannot be claimed right now).
			foreach (var kv in anchorsByField)
				if (!fieldCovered[kv.Key] && kv.Value.Any(a => !served[a] && reachDist[a] <= reachCells))
					unservedFieldsInReach++;

			Func<int, bool> claimable = a => !served[a] && reachDist[a] <= reachCells && (blocked == null || !blocked(a));
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
				.OrderBy(b => b.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? int.MaxValue)
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

			var mcvType = producible.Random(world.LocalRandom);
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

			if (++ticks % Info.ReplanTicks != 0)
				return;

			Replan(bot);
		}

		void Replan(IBot bot)
		{
			var (refinery, link, crawlLink, queues) = CheapestBuildables();

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
			var buildingTiles = new List<CPos>();
			var guards = new List<(CPos Cell, int Value)>();
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
						refineryTiles.Add(a.Info.TraitInfoOrDefault<BuildingInfo>()?.Tiles(a.Location).ToList());
				}

				if (Info.FieldCoverage)
				{
					if (a.Info.HasTraitInfo<BuildingInfo>())
						allBuildingCells.Add(a.Location);

					if (constructionYardTypes.Contains(a.Info.Name))
						yardCells.Add(a.Location);
				}

				if (a.Info.HasTraitInfo<GivesBuildableAreaInfo>())
				{
					buildingCells.Add(a.Location);
					if (Info.FieldCoverage && a.Info.TraitInfoOrDefault<BuildingInfo>() is BuildingInfo abi)
						buildingTiles.AddRange(abi.Tiles(a.Location));
				}
				else if (a.Info.HasTraitInfo<BuildingInfo>())
					continue;
				else if (a.Info.HasTraitInfo<AttackBaseInfo>() && !a.Info.HasTraitInfo<HarvesterInfo>())
					guards.Add((a.Location, a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0));
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
					if (heading == null || heading.IsDead || !heading.IsInWorld || heading.Owner != player)
						inflightMcvSites.Remove(id);
				}
			}

			var diagonalCells = Info.FieldCoverage ? MapDiagonalCells() : 1;
			var first = income.Count > 0 ? income.Peek() : (Tick: world.WorldTick, Earned: resources.Earned);
			var span = Math.Max(1, world.WorldTick - first.Tick);
			var incomePerTick = (resources.Earned - first.Earned) / (double)span;
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

			for (var i = 0; i < resourceMap.GetIndicesLength(); i++)
			{
				var field = resourceMap.GetIndice(i);
				if (field == null || (field.ResourceCellsCount <= 0 && !initialCells.ContainsKey(i)))
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
				if (field.PlayerRefineryCount > 0 || claimed || value <= 0)
				{
					owned += value > 0 ? 1 : 0;
					continue;
				}

				if (parkedUntil.TryGetValue(i, out var until) && world.WorldTick < until)
					continue;

				// TC-2c: an outranking ally already aims at this field — yield instead of stacking.
				if (allyClaims != null && allyClaims.Count > 0
					&& AllyClaimWins(world.Map.CenterOfCell(center), allyClaims, player.ClientIndex, Info.AllyClaimRadiusCells))
					continue;

				var distance = buildingCells.Min(c => (c - center).Length);
				var hops = Hops(distance, reach, Info.LinkStepCells);
				var threat = threatProviders.Sum(p => p.RememberedEnemyThreatAt(center));
				var guardValue = guards.Where(g => (g.Cell - center).LengthSquared <= Info.GuardRadiusCells * Info.GuardRadiusCells)
					.Sum(g => g.Value);
				var cost = refinery.Cost + hops * link.Cost;
				var score = Score(value, threat, guardValue, cost, incomePerTick, hops, link.BuildTicks, refinery.BuildTicks,
					Info.TauTicks, out var payback);

				// TC-3 / BD: own-sector fields keep their full score, foreign-sector fields keep
				// CoalitionForeignSectorPercent of it — deprioritized, never forbidden: the argmax
				// still picks a foreign field when no own-sector field is left.
				if (sectorAnchors != null)
					score *= SectorScorePercent(world.Map.CenterOfCell(center), sectorAnchors,
						player.InternalName ?? "#" + player.ClientIndex, Info.CoalitionForeignSectorPercent) / 100.0;

				// FE-1: unexplored ground wins ties (spread), and the crawl prefers fields no MCV is already heading to.
				if (Info.FieldCoverage)
				{
					score *= SpreadFactor(allBuildingCells.Count > 0 ? allBuildingCells.Min(c => (c - center).Length) : distance, diagonalCells, Info.SpreadBonus);
					if (inflightMcvSites.Count > 0 && TakenByMcvSite(center, inflightMcvSites.Values, Info.ClaimRadiusCells))
						score *= Info.MinSeparationFactor;
				}

				scores.Add(new FieldScore(i, center, value, hops, payback, threat, score, Safety(threat, guardValue)));
			}

			scores.Sort((a, b) => b.Score.CompareTo(a.Score));
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
				UpdateAnchorClaim(refineryCells, refineryTiles, buildingTiles);
				UpdateCrawlWant(crawlLink, buildingTiles);
			}
			else
			{
				wantedLinkBuilding = null;
				crawlTargetEdge = null;
			}

			var lawClaims = LawActive;
			claimField = lawClaims ? null : BestClaimField(scores);
			if (!lawClaims)
				wantsRefinery = claimField.HasValue;

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

		void UpdateCrawlWant((ActorInfo Info, int Cost, int BuildTicks) link, List<CPos> buildingTiles)
		{
			wantedLinkBuilding = null;
			crawlTargetEdge = null;
			if (!LinkBuildingWanted(Info.DriveBaseCrawl, Target?.Hops, claimableAnchorsInReach, link.Info != null))
				return;

			wantedLinkBuilding = link.Info.Name;
			crawlTargetEdge = LinkTargetEdge(Target.Value, buildingTiles);
		}

		/// <summary>
		/// REF-1 B1: the target field's resource cell nearest our frontier — the aim every crawl placement closes
		/// the gap toward (the maintainer's "every building placed to close the gap"). The field models differ
		/// (ResourceMap field vs the law's map-true component), so the law field nearest the target's centre supplies
		/// the cells; with no usable cells the centre stays the aim.
		/// </summary>
		CPos? LinkTargetEdge(FieldScore target, List<CPos> buildingTiles)
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
			var order = ClaimOrder(anchors, anchorFieldIds, fieldCellsById, lastRefineryCells, lastRefineryFields, lastBuildingTiles,
				serve, Info.ReachCells,
				i => (anchorParkedUntil.TryGetValue(anchors[i], out var until) && tick < until)
					|| (anchorPendingUntil.TryGetValue(anchors[i], out var pend) && tick < pend),
				i => anchorPendingUntil.TryGetValue(anchors[i], out var pend) && tick < pend,
				near, out _, out _, out var tiers, out _, out _);
			if (order.Count == 0)
				return null;

			var best = order[0];
			var fieldId = anchorFieldIds[best];
			var center = fieldId < fields.Count ? fields[fieldId].Center : anchors[best];
			return new RefineryAnchorClaim(anchors[best], center, fieldId, tiers[best], fieldCellsById[fieldId]);
		}

		/// <summary>
		/// REF-1 (§12.24 v2): rebuild the anchors (spreaders are public map data, like the spawn points, plus the centres of
		/// spreaderless fields), attach each to its resource field, and publish the claim order — tier 1: every unserved
		/// field's best spreader in reach, home first; tier 2: covered fields' extra spreaders. A claim that keeps failing
		/// is parked; a committed one is pending until its refinery lands.
		/// </summary>
		void UpdateAnchorClaim(List<CPos> refineryCells, List<IReadOnlyCollection<CPos>> refineryTiles, List<CPos> buildingTiles)
		{
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

			// Which field each refinery's footprint sits flush to — a legal gap-0/1 placement on a field's far edge
			// can land beyond the serve radius from the spreader, and it still is that field's refinery.
			lastRefineryFields = RefineryFlushFields(refineryTiles, fieldCellsById);

			// Pending commits clear when their refinery landed (the anchor is served) or the commit expired.
			var assigned = AssignRefineries(anchors, refineryCells, serve, anchorFieldIds, lastRefineryFields);
			foreach (var kv in anchorPendingUntil.ToList())
			{
				var index = anchors.IndexOf(kv.Key);
				if (tick >= kv.Value || (index >= 0 && assigned[index] >= 0))
					anchorPendingUntil.Remove(kv.Key);
			}

			var queued = 0;
			var builder = baseBuilders.FirstOrDefault(t => t.IsTraitEnabled());
			if (builder?.BuildingsBeingProduced != null)
				foreach (var r in builder.Info.RefineryTypes)
					if (builder.BuildingsBeingProduced.TryGetValue(r, out var n))
						queued += n;

			var order = ClaimOrder(anchors, anchorFieldIds, fieldCellsById, refineryCells, lastRefineryFields, buildingTiles,
				serve, Info.ReachCells,
				i => (anchorParkedUntil.TryGetValue(anchors[i], out var until) && tick < until)
					|| (anchorPendingUntil.TryGetValue(anchors[i], out var pend) && tick < pend),
				i => anchorPendingUntil.TryGetValue(anchors[i], out var pend) && tick < pend,
				null, out unservedInReach, out unservedFieldsInReach, out _,
				out claimableAnchorsInReach, out unservedBeyondReach);
			unclaimedAnchorsInReach = order.Count;

			// A refinery is wanted only while more anchors are claimable than refineries already in flight — never by
			// comparing totals (a duplicate stacked at home must not spend a forward anchor's quota).
			wantsRefinery = order.Count > queued;
			if (order.Count == 0)
			{
				anchorStuck = (null, 0, refineryCells.Count);
				return;
			}

			var anchor = anchors[order[0]];

			// Loop guard: the same anchor wanted replan after replan with no refinery gained and none in production.
			var replans = anchorStuck.Anchor == anchor && refineryCells.Count <= anchorStuck.Refineries && queued == 0 ? anchorStuck.Replans + 1 : 0;
			anchorStuck = (anchor, replans, refineryCells.Count);
			if (Info.AnchorStuckReplans > 0 && replans >= Info.AnchorStuckReplans)
			{
				anchorParkedUntil[anchor] = tick + Info.ParkTicks;
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
				.OrderBy(a => a.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? int.MaxValue)
				.ThenBy(a => a.Name, StringComparer.Ordinal)
				.FirstOrDefault();
			if (cheapest == null)
				return default;

			return (cheapest, cheapest.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0,
				Math.Max(0, cheapest.TraitInfoOrDefault<BuildableInfo>()?.BuildDuration ?? 0));
		}

		/// <summary>
		/// REF-1 B1 (maintainer ruling): a crawl link must EXTEND the buildable area (GivesBuildableArea — a silo or
		/// other non-provider placed forward closes no gap) and should be useful — power plants are preferred since
		/// they also feed defences. No cost cap: the advanced plant is still a valid link when it is the only one.
		/// </summary>
		public static bool IsCrawlLink(ActorInfo a) =>
			a.HasTraitInfo<BuildableInfo>() && a.HasTraitInfo<BuildingInfo>()
			&& a.HasTraitInfo<GivesBuildableAreaInfo>() && !a.HasTraitInfo<RefineryInfo>();

		public static bool IsPowerPlant(ActorInfo a) => (a.TraitInfoOrDefault<PowerInfo>()?.Amount ?? 0) > 0;

		public static (ActorInfo Info, int Cost, int BuildTicks) PickCrawlLink(
			(ActorInfo Info, int Cost, int BuildTicks) power, (ActorInfo Info, int Cost, int BuildTicks) other) =>
			power.Info != null ? power : other;

		((ActorInfo Info, int Cost, int BuildTicks) Refinery, (ActorInfo Info, int Cost, int BuildTicks) Link,
			(ActorInfo Info, int Cost, int BuildTicks) CrawlLink, int Queues) CheapestBuildables()
		{
			(ActorInfo Info, int Cost, int BuildTicks) refinery = default, link = default, linkPower = default, linkOther = default;
			var queues = 0;
			foreach (var queue in BuildingQueueTypes().SelectMany(type => CAAIUtils.FindQueues(player, type)).Distinct())
			{
				queues++;
				foreach (var item in queue.BuildableItems())
				{
					var bi = item.TraitInfoOrDefault<BuildableInfo>();
					if (bi == null || !item.HasTraitInfo<BuildingInfo>())
						continue;

					var cost = item.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
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
					}
				}
			}

			return (refinery, link, PickCrawlLink(linkPower, linkOther), queues);
		}
	}
}
