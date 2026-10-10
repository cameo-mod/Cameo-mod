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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Architecture review hotspot #5: the pure decision half of
	/// <see cref="BaseBuilderQueueManagerCA"/> — the queue/place gates and the placement
	/// predicates, stateless and World-free so tests drive every branch directly.
	/// World reads, order issuance and LocalRandom draws stay in the manager; every rule
	/// here is a verbatim extraction (identical inputs give the identical decision).
	/// </summary>
	public static class BaseBuilderQueueEvalCA
	{
		/// <summary>
		/// TQ2: a queued item waits when the tick already queued one, or when cash is short —
		/// except refineries (economy first) and the planner's own wants (the crawl link and the
		/// due MCV's prerequisite), which ride at their own cost floor under the law (REF-1 B1/B2).
		/// </summary>
		public static bool BlockedByCash(int cash, int minCashRequirement, bool isRefinery,
			bool plannerWant, int productionCost, bool queuedThisTick) =>
			queuedThisTick || (cash < minCashRequirement && !isRefinery && !(plannerWant && cash >= productionCost));

		/// <summary>
		/// TQ3: the placement-type ladder — the planner's crawl want and refinery/fragile rules
		/// classify first, then the defense roll (advisor-approved cells are placed by the caller
		/// and never reach this), then the organic crawl roll. The random draws themselves stay
		/// with the caller so draw order — and with it sim determinism — is unchanged.
		/// </summary>
		public static BuildingType ClassifyPlacement(bool lawLink, bool isRefinery, bool isFragile,
			bool hasAttackBase, bool defenseRollPassed, bool organicCrawlPassed)
		{
			if (lawLink)
				return BuildingType.BaseCrawl;

			if (isRefinery)
				return BuildingType.Refinery;

			if (isFragile)
				return BuildingType.Fragile;

			if (hasAttackBase)
				return defenseRollPassed ? BuildingType.Defense : BuildingType.Building;

			return organicCrawlPassed ? BuildingType.BaseCrawl : BuildingType.Building;
		}

		/// <summary>
		/// TQ3: under the law a crawl placement with no aim holds — the produced building stays
		/// queued (and spends no failure budget) instead of landing on an un-aimed fallback cell
		/// (REF-1 B1, crawl-trace §8).
		/// </summary>
		public static bool CrawlHold(BuildingType type, bool lawActive, bool hasCrawlEdge, bool hasExpansionTarget) =>
			type == BuildingType.BaseCrawl && lawActive && !hasCrawlEdge && !hasExpansionTarget;

		/// <summary>CB1: the opening's barracks-first policy window for this faction.</summary>
		public static bool OpeningBarracksPolicy(bool limitsPresent, bool prioritize, bool factionListed, bool completed) =>
			limitsPresent && prioritize && factionListed && !completed;

		/// <summary>
		/// The priority overrides' shared shape: the pick goes through when it does not starve
		/// power; otherwise a power plant wins instead. No pick means nothing is decided.
		/// </summary>
		public static T PickOrPower<T>(T pick, T power, Func<T, bool> sufficientPower)
			where T : class =>
			pick != null && sufficientPower(pick) ? pick : pick != null ? power : null;

		/// <summary>
		/// CB10: a building's share of all standing buildings stays under its fraction
		/// (per-100 in yaml, scaled by the knobs' milli — 1000 neutral).
		/// </summary>
		public static bool FractionAdmits(int count, int fractionPer100, int fractionMilli, int buildingCount) =>
			count * 100L * 1000 <= (long)fractionPer100 * fractionMilli * buildingCount;

		/// <summary>CB10: an opening delay has not elapsed yet.</summary>
		public static bool StillDelayed(int delayTicks, int worldTick) => delayTicks > worldTick;

		/// <summary>CB10: the building has a configured interval and it is still cooling down.</summary>
		public static bool IntervalBlocks(bool intervalDefined, bool coolingDown) => intervalDefined && coolingDown;

		/// <summary>Standing + in-flight count has reached a per-building or production-type cap.</summary>
		public static bool LimitReached(int count, int limit) => count >= limit;

		/// <summary>GetProducibleBuilding's admit test: no limit configured, or standing plus producing under it.</summary>
		public static bool LimitAdmits(bool hasLimit, int limit, int standing, int producing) =>
			!hasLimit || standing + producing < limit;

		/// <summary>CB10: a naval structure needs water inside the base perimeter and a nearby buildable edge —
		/// the area scan stays lazy: it is consulted only when the other clauses demand it.</summary>
		public static bool NavalBlocked(bool isNaval, bool notEnoughWater, Func<bool> areaAvailable) =>
			isNaval && (notEnoughWater || !areaAvailable());

		/// <summary>CB10: the pick would drop us below the power floor — build the plant instead.</summary>
		public static bool LowPowerBlocked(bool hasPowerManager, int excessPower, int minimumExcess, bool pickSufficient) =>
			hasPowerManager && (excessPower < minimumExcess || !pickSufficient);

		/// <summary>TQ5: the expansion nudge's economy balance — production plus tech, minus the tolerated slack.</summary>
		public static bool ExpansionBalance(int numProd, int numTech, int tolerance, int cashTolerance, int numRef) =>
			numProd + numTech - tolerance - cashTolerance >= numRef;

		/// <summary>CB6: a higher fraction wins; equal fractions take the ordinally smaller name — deterministic.</summary>
		public static bool BetterOpeningCandidate(int fraction, int bestFraction, string name, string bestName) =>
			fraction > bestFraction || (fraction == bestFraction && string.CompareOrdinal(name, bestName) < 0);

		/// <summary>CBL-Refinery: the scan centre — home while placing is failing, else the requesting yard's, else the resource conyard's.</summary>
		public static CPos ResourcePickCenter(int failCount, CPos baseCenter, CPos? requestedConyard, CPos? resourceConyard) =>
			failCount > 0 ? baseCenter : requestedConyard ?? resourceConyard ?? baseCenter;

		/// <summary>CBL-Refinery: the claim field overrides the crawl aim when the planner publishes one (EX-2c).</summary>
		public static CPos ClaimFieldOrTarget(CPos? claimTarget, CPos expansionTarget) => claimTarget ?? expansionTarget;

		/// <summary>
		/// FP2: the variant index whose facing best matches the centre→target direction — a 1024-circle
		/// arc-sine of |dx|/len, quadrant-folded; nearest configured facing wins, earliest index on ties.
		/// A zero-length direction keeps index 0 (anchor placements have no facing to match).
		/// </summary>
		public static int PickFacingVariant(WVec vector, ImmutableArray<WAngle> facings)
		{
			if (facings.IsDefaultOrEmpty)
				return 0;

			if (vector.Length == 0)
				vector = new WVec(0, 1, 0);

			var desireFacing = new WAngle(WAngle.ArcSin((int)((long)Math.Abs(vector.X) * 1024 / vector.Length)).Angle);
			if (vector.X > 0 && vector.Y >= 0)
				desireFacing = new WAngle(512) - desireFacing;
			else if (vector.X < 0 && vector.Y >= 0)
				desireFacing = new WAngle(512) + desireFacing;
			else if (vector.X < 0 && vector.Y < 0)
				desireFacing = -desireFacing;

			var best = 0;
			for (int i = 0, e = 1024; i < facings.Length; i++)
			{
				var minDelta = Math.Min((desireFacing - facings[i]).Angle, (facings[i] - desireFacing).Angle);
				if (e > minDelta)
				{
					e = minDelta;
					best = i;
				}
			}

			return best;
		}

		/// <summary>
		/// FP2/FB: a placement draws a random variant index exactly when the actor has variants
		/// but no configured facings — the aimed path uses PickFacingVariant instead; the front/back
		/// advisor uses the same gate (a front-side facing is the advisor's to encode later).
		/// </summary>
		public static bool PicksRandomVariant(bool hasVariants, bool hasFacings) => hasVariants && !hasFacings;

		/// <summary>
		/// FP6/FP7: a candidate cell survives placeable, base-distance, requirement-distance and
		/// own-building spacing checks — the identical gate both candidate loops apply, in the
		/// same short-circuit order (each check is consulted only when the previous passed).
		/// </summary>
		public static bool PlacementCellAdmitted(Func<bool> canPlace, Func<bool> closeEnough,
			Func<bool> withinRequirement, Func<bool> outsideBuffer) =>
			canPlace() && closeEnough() && withinRequirement() && outsideBuffer();

		/// <summary>FP6: a ranking advisor's answer stands only while it names one of the offered cells.</summary>
		public static CPos AdvisorPick(CPos? chosen, IReadOnlyList<CPos> candidates) =>
			chosen.HasValue && candidates.Contains(chosen.Value) ? chosen.Value : candidates[0];

		/// <summary>
		/// REF-1 (§12.24 v2): how flush a footprint sits to the claim field — 0 when a footprint cell
		/// lands on or beside a resource cell, 1 on the next ring out, -1 beyond that (illegal).
		/// </summary>
		public static int FootprintGap(IReadOnlyCollection<CPos> footprint, IReadOnlyCollection<CPos> zone0, IReadOnlyCollection<CPos> zone1) =>
			footprint.Any(zone0.Contains) ? 0 : footprint.Any(zone1.Contains) ? 1 : -1;

		/// <summary>
		/// REF-1 (§12.24 v2): the dock keeps an exit — at least one on-map 8-neighbour outside the
		/// footprint so a harvester can actually leave. A walled-in dock makes the refinery useless.
		/// </summary>
		public static bool DockHasExit(CPos dock, IReadOnlySet<CPos> footprint, Func<CPos, bool> onMap)
		{
			for (var dy = -1; dy <= 1; dy++)
				for (var dx = -1; dx <= 1; dx++)
				{
					// FIX-RA-REFINERY: the exit must be another cell — a dock on a passable bib cell
					// is no longer in the blocking set and would otherwise count as its own exit.
					if (dx == 0 && dy == 0)
						continue;

					var n = new CPos(dock.X + dx, dock.Y + dy);
					if (!footprint.Contains(n) && onMap(n))
						return true;
				}

			return false;
		}

		/// <summary>
		/// FIX-RA-REFINERY: the footprint cells that wall a dock in — the impassable cells only.
		/// BuildingInfo.Tiles also yields passable '=' bibs and transit-only '+' lanes; treating
		/// those as walls put the RA proc's dock — which lands on a bib cell — inside a neighbour
		/// ring of "occupied" cells, so the exit check failed at every legal site. The harvester
		/// leaving a dock is in transit, so '=' and '+' cells are both traversable here; only
		/// 'x'/'X' cells block egress.
		/// </summary>
		public static IEnumerable<CPos> DockBlockingFootprint(IReadOnlyDictionary<CVec, FootprintCellType> footprint, CPos topLeft) =>
			footprint
				.Where(kv => kv.Value == FootprintCellType.Occupied || kv.Value == FootprintCellType.OccupiedUntargetable)
				.Select(kv => topLeft + kv.Key);

		/// <summary>
		/// Deterministic dictionary pick (F-CBL1/F4): lowest tie-break key wins.
		/// `Dictionary.Keys.First()` order is unspecified and could diverge across runtimes; an
		/// explicit order keeps every peer's pick identical. Null/empty yields the default.
		/// </summary>
		public static TKey FirstByOrder<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> items, Func<TKey, long> order) =>
			items == null || items.Count == 0 ? default : items.Keys.OrderBy(order).First();

		/// <summary>
		/// The pending MCV refinery request the queue serves first — lowest ActorID wins (the Frans
		/// twin already picks by ActorID).
		/// </summary>
		public static Actor FirstRequestedRefinery<TValue>(IReadOnlyDictionary<Actor, TValue> requests) =>
			FirstByOrder(requests, a => a.ActorID);

		/// <summary>
		/// FIX-RA-REFINERY: under the refinery law a refinery that found no legal site defers — the
		/// produced item is cancelled for the refund and the standing request re-queues it on a
		/// later sweep (the law's own contract: a failed claim retries later). It must not spend
		/// the shared failCount budget: a siteless field would otherwise saturate the latch and
		/// stop every other structure placing. Non-refinery and law-less placements keep the
		/// ordinary failure path.
		/// </summary>
		public static bool RefineryDefers(bool lawActive, BuildingType type) =>
			lawActive && type == BuildingType.Refinery;

		/// <summary>
		/// FIX-RA-REFINERY: the saturated-placement recovery arm owns a tick whenever the
		/// expansion-nudge arm cannot act — no expansion modules, no recorded failing centre,
		/// or a relocation hold in progress. Gated on module count alone those cases latched
		/// the builder forever.
		/// </summary>
		public static bool LatchRecoveryApplies(int expansionModuleCount, bool hasFailingCenter, bool relocationHold) =>
			expansionModuleCount == 0 || !hasFailingCenter || relocationHold;

		/// <summary>
		/// FIX-RA-REFINERY: ticks the recovery probe timer — due exactly when it reaches zero,
		/// then re-arms to a full delay so each probe waits the same interval and a fresh
		/// episode (reset at saturation) always gets its own window.
		/// </summary>
		public static bool LatchProbeDue(ref int failRetryTicks, int resumeDelay)
		{
			if (--failRetryTicks > 0)
				return false;

			failRetryTicks = resumeDelay;
			return true;
		}

		/// <summary>
		/// FIX-RA-REFINERY: a latch probe releases only on real world change against the
		/// saturation-time baseline — fewer buildings (room freed) or more base providers
		/// (new build area). Equal or worse counts hold the latch for another delay.
		/// </summary>
		public static bool LatchProbeReleases(int currentBuildings, int latchedBuildings, int currentProviders, int latchedProviders) =>
			currentBuildings < latchedBuildings || currentProviders > latchedProviders;
	}
}