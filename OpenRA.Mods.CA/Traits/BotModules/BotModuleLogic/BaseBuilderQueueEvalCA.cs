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
using OpenRA.Mods.Common.Warheads;

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

		/// <summary>
		/// ECON-A (SPEC_2026-10-05 Part A §3): a demand item queues when it can still reach Ready by
		/// the MCV's ETA — start when `etaTick - buildTime &lt;= now`; an already-late item starts now.
		/// </summary>
		public static bool ExpansionDue(int etaTick, int buildTimeTicks, int now) => etaTick - buildTimeTicks <= now;

		/// <summary>
		/// ECON-A (§2): travel time for the expansion ETA — path length in WDist over the MCV's
		/// nominal speed (WDist per tick), integer division. A pathless or stalled estimate is a
		/// far-future tick (never due) so the caller expires the demand instead of pre-building blind.
		/// </summary>
		public static int ExpansionTravelTicks(long distanceWDist, int speedWDistPerTick) =>
			speedWDistPerTick <= 0 ? int.MaxValue : (int)Math.Min(int.MaxValue, distanceWDist / speedWDistPerTick);

		/// <summary>
		/// ECON-A (§4): the defence pre-build is skipped while it would hold a Ready item on the
		/// bot's only building producer — unless the deploy is already close (within holdSlack).
		/// The refinery never takes this gate: it gates the expansion's economy.
		/// </summary>
		public static bool DefenceHoldPermitted(bool soleProducer, int ticksToEta, int holdSlack) =>
			!soleProducer || ticksToEta <= holdSlack;

		/// <summary>
		/// ECON-A (§1): a defence's strength ordering — the same damage model the squad fuzzy uses
		/// (AttackOrFleeFuzzyCA): Damage * Burst / totalReloadDelay * 100 per DamageWarhead, with the
		/// first BurstDelay counted once per extra shot and the total reload window clamped to
		/// [1, 200] so slow one-shot weapons are not undervalued. 0 for a non-AttackBase actor or one
		/// with no damage warheads.
		/// </summary>
		public static int DefenceStrength(ActorInfo info)
		{
			if (!info.HasTraitInfo<AttackBaseInfo>())
				return 0;

			var sum = 0;
			foreach (var arm in info.TraitInfos<ArmamentInfo>())
			{
				var weapon = arm.WeaponInfo;
				if (weapon == null)
					continue;

				var burst = weapon.Burst;
				var burstDelay = weapon.BurstDelays.IsDefaultOrEmpty ? 0 : weapon.BurstDelays[0];
				var totalReloadDelay = weapon.ReloadDelay + (burstDelay * (burst - 1)).Clamp(1, 200);
				foreach (var warhead in weapon.Warheads.OfType<DamageWarhead>())
					sum += warhead.Damage * burst / Math.Max(1, totalReloadDelay) * 100;
			}

			return sum;
		}

		/// <summary>
		/// ECON-A (§1): the strongest affordable defence — highest strength wins; ties break on the
		/// lowest ActorInfo cost, then the actor name (ordinal). Nothing affordable yields null and
		/// the demand proceeds refinery-only. Inputs arrive precomputed so the rule stays World-free.
		/// </summary>
		public static ActorInfo ChooseExpansionDefence(IReadOnlyList<(ActorInfo Info, int Strength, int Cost)> candidates, int projectedCash)
		{
			ActorInfo best = null;
			var bestStrength = 0;
			var bestCost = 0;
			foreach (var candidate in candidates)
			{
				if (candidate.Cost > projectedCash)
					continue;

				if (best == null || candidate.Strength > bestStrength
					|| (candidate.Strength == bestStrength
						&& (candidate.Cost < bestCost
							|| (candidate.Cost == bestCost && string.CompareOrdinal(candidate.Info.Name, best.Name) < 0))))
				{
					best = candidate.Info;
					bestStrength = candidate.Strength;
					bestCost = candidate.Cost;
				}
			}

			return best;
		}

		// ECON-A-FIX (REVIEW_2026-10-06_econ_a): the review's six findings each get a world-free seam so
		// the regression tests drive the rule, not a harness.

		/// <summary>
		/// ECON-A R1-FIX2: one occupant's verdict on the fog-honest ETA path — Locomotor.IsBlockedBy
		/// under BlockedByActor.Immovable, minus the hidden state: an occupant that is neither ours,
		/// nor legally visible to us (Actor.CanBeViewedByPlayer — a revealed CELL is not a revealed
		/// ACTOR; cloaked, disguised or otherwise hidden actors on lit ground stay unknown), nor
		/// remembered (a frozen footprint handled by <see cref="FrozenOccupantBlocks"/>) is not KNOWN
		/// and never blocks, so adding or removing an unseen enemy actor cannot change the estimate.
		/// Known + immovable and not movable-allied/moving/removable/transit-only/crushable blocks.
		/// </summary>
		public static bool EtaOccupantBlocks(bool known, bool movable, bool allied, bool moving, bool removable, bool transitOnly, bool crushable) =>
			known && !(movable && allied) && !moving && !removable && !transitOnly && !crushable;

		/// <summary>
		/// ECON-A R2: the length of a PathFinder result — the contract returns the path target-to-source
		/// (<see cref="IPathFinder.FindPathToTargetCell"/>), so the path length is the sum of its own
		/// consecutive cell-to-cell segments. There is no source-to-first-cell chord to prepend:
		/// seeding the walk at the source position double-counts the whole route.
		/// </summary>
		public static long ExpansionPathLength(IReadOnlyList<CPos> targetToSource, Func<CPos, WPos> center)
		{
			if (targetToSource == null || targetToSource.Count < 2)
				return 0;

			var previous = center(targetToSource[0]);
			long distance = 0;
			for (var i = 1; i < targetToSource.Count; i++)
			{
				var cell = center(targetToSource[i]);
				distance += (cell - previous).Length;
				previous = cell;
			}

			return distance;
		}

		/// <summary>
		/// ECON-A R4-FIX2: where the traveller's activity chain leaves the journey — the chain's last
		/// positional target decides. A chain with no positional target at all (a WaitFor, a turn in
		/// place, a transform in flight) is Indeterminate: not a redirect, but also not provably still
		/// travelling — <see cref="JourneyRenewsDemand"/> bounds its grace.
		/// </summary>
		public enum ExpansionJourneyState
		{
			/// <summary>The chain's last positional target aims elsewhere — a redirect took the unit.</summary>
			Redirected,

			/// <summary>No positional target on the chain — silence; the existing window decides.</summary>
			Indeterminate,

			/// <summary>The last positional target is still inside the deploy slack — travelling.</summary>
			Committed,
		}

		public static ExpansionJourneyState ExpansionJourney(bool sawAnyTarget, bool lastTargetNearDeploy) =>
			!sawAnyTarget ? ExpansionJourneyState.Indeterminate
				: lastTargetNearDeploy ? ExpansionJourneyState.Committed : ExpansionJourneyState.Redirected;

		/// <summary>
		/// ECON-A R4-FIX2: the idle window renews only for a provably ongoing journey — a Committed
		/// chain on a non-idle traveller, or a unit-less actor (a conyard mid-relocation, whose
		/// continued existence IS the in-flight relocation). Indeterminate chains and idle travellers
		/// do not renew: the demand's ExpiresTick is the bounded silence grace, so an unrelated
		/// targetless activity (WaitFor with a false predicate) can no longer keep a demand alive
		/// forever.
		/// </summary>
		public static bool JourneyRenewsDemand(ExpansionJourneyState state, bool idle, bool hasMobile) =>
			!hasMobile || (state == ExpansionJourneyState.Committed && !idle);

		/// <summary>
		/// ECON-A R1-FIX2: a remembered frozen footprint cell blocks iff it isn't a transit-only cell,
		/// isn't removable by us, and isn't crushable — evaluated on remembered facts only.
		/// </summary>
		public static bool FrozenOccupantBlocks(bool transitOnly, bool removable, bool crushable) =>
			!transitOnly && !removable && !crushable;

		/// <summary>
		/// ECON-A R1-FIX2: remembered crushability — the record's static crush classes overlap the
		/// locomotor's and the remembered owner relationship permits it (enemy owner, or friendly
		/// when the record's static flag allows friendly crushing).
		/// </summary>
		public static bool RememberedCrushable(bool classesOverlap, bool friendliesCrush, bool rememberedAllied) =>
			classesOverlap && (friendliesCrush || !rememberedAllied);

		/// <summary>
		/// ECON-A R5: the demand binding's (producer, item-name) token is unambiguous only while no
		/// same-name item shares the producer — already queued, ordered and still in flight, or bound
		/// to another demand. CancelProduction resolves the LAST same-name item on the queue; a
		/// duplicate would refund or free the wrong item.
		/// </summary>
		public static bool DemandItemUnambiguous(bool sameNameOnProducer, bool boundByOtherDemand) =>
			!sameNameOnProducer && !boundByOtherDemand;

		/// <summary>
		/// ECON-A R3: what the sweep does with a live <c>Actor.ReplacedByActor</c> — the transform edge is a
		/// one-shot transition, consumed by the caller's Deployed flag (the field stays readable on the
		/// disposed MCV forever, so gating is the only thing keeping it from re-firing every sweep).
		/// </summary>
		public enum ExpansionTransformTransition
		{
			/// <summary>No live transform edge (or one already consumed) — keep waiting.</summary>
			Waiting,

			/// <summary>The replacement is an own, live construction yard — the MCV deployed.</summary>
			Deploy,

			/// <summary>The replacement is an own, live non-yard — the journey continues on it.</summary>
			Relocate,

			/// <summary>The replacement is unusable or foreign-owned — the journey died.</summary>
			Fail,
		}

		/// <summary>The transition described above; <paramref name="replacementUsable"/> bundles the
		/// sweep's "alive, in world, ours" check so a disposed or captured yard can never deploy.</summary>
		public static ExpansionTransformTransition ExpansionTransform(
			bool alreadyDeployed, bool hasReplacement, bool replacementUsable, bool replacementYard)
		{
			if (alreadyDeployed || !hasReplacement)
				return ExpansionTransformTransition.Waiting;
			if (!replacementUsable)
				return ExpansionTransformTransition.Fail;
			return replacementYard ? ExpansionTransformTransition.Deploy : ExpansionTransformTransition.Relocate;
		}
	}
}
