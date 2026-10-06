#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// AI_ARCHITECTURE hotspot #5: the extracted queue/placement decision seams of
	// BaseBuilderQueueManagerCA (BaseBuilderQueueEvalCA) — branch-complete.
	[TestFixture]
	public class BaseBuilderQueueEvalTest
	{
		static readonly CPos Base = new(10, 10);

		// BlockedByCash

		[Test]
		public void QueuedThisTickAlwaysBlocks()
			=> Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(99999, 100, false, false, 0, true), Is.True);

		[Test]
		public void CashAboveFloorNeverBlocks()
			=> Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(100, 100, false, false, 0, false), Is.False);

		[Test]
		public void LowCashBlocksOrdinaryButNotRefinery()
		{
			Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(50, 100, false, false, 0, false), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(50, 100, true, false, 0, false), Is.False);
		}

		[Test]
		public void PlannerWantRidesAtItsOwnCostFloor()
		{
			Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(50, 100, false, true, 50, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(49, 100, false, true, 50, false), Is.True);
		}

		// ClassifyPlacement

		[Test]
		public void LawLinkClassifiesAsCrawlBeforeEveryRule()
			=> Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(true, true, true, true, true, true), Is.EqualTo(BuildingType.BaseCrawl));

		[Test]
		public void RefineryAndFragileBeatDefenseAndCrawl()
		{
			Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(false, true, true, true, true, true), Is.EqualTo(BuildingType.Refinery));
			Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(false, false, true, true, true, true), Is.EqualTo(BuildingType.Fragile));
		}

		[Test]
		public void AttackNeedsTheRollAndOrdinaryNeedsTheCrawlRoll()
		{
			Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(false, false, false, true, true, true), Is.EqualTo(BuildingType.Defense));
			Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(false, false, false, true, false, true), Is.EqualTo(BuildingType.Building));
			Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(false, false, false, false, false, true), Is.EqualTo(BuildingType.BaseCrawl));
			Assert.That(BaseBuilderQueueEvalCA.ClassifyPlacement(false, false, false, false, false, false), Is.EqualTo(BuildingType.Building));
		}

		// CrawlHold

		[Test]
		public void CrawlHoldNeedsAllFourConditions()
		{
			Assert.That(BaseBuilderQueueEvalCA.CrawlHold(BuildingType.BaseCrawl, true, false, false), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.CrawlHold(BuildingType.BaseCrawl, true, false, true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.CrawlHold(BuildingType.BaseCrawl, true, true, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.CrawlHold(BuildingType.BaseCrawl, false, false, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.CrawlHold(BuildingType.Building, true, false, false), Is.False);
		}

		// OpeningBarracksPolicy

		[Test]
		public void OpeningPolicyNeedsLimitsFactionAndUnfinished()
		{
			Assert.That(BaseBuilderQueueEvalCA.OpeningBarracksPolicy(true, true, true, false), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.OpeningBarracksPolicy(false, true, true, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.OpeningBarracksPolicy(true, false, true, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.OpeningBarracksPolicy(true, true, false, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.OpeningBarracksPolicy(true, true, true, true), Is.False);
		}

		// PickOrPower

		[Test]
		public void PickOrPowerDecidesOnlyOnAPick()
		{
			var pick = new object();
			var power = new object();
			Assert.That(BaseBuilderQueueEvalCA.PickOrPower<object>(null, power, _ => true), Is.Null);
			Assert.That(BaseBuilderQueueEvalCA.PickOrPower(pick, power, _ => true), Is.SameAs(pick));
			Assert.That(BaseBuilderQueueEvalCA.PickOrPower(pick, power, _ => false), Is.SameAs(power));
			Assert.That(BaseBuilderQueueEvalCA.PickOrPower<object>(pick, null, _ => false), Is.Null);
		}

		// FractionAdmits

		[Test]
		public void FractionBoundaryAdmitsAtEquality()
		{
			// count*100*1000 vs frac*milli*buildings: 2*100*1000 = 200000 vs 10*1000*20 = 200000.
			Assert.That(BaseBuilderQueueEvalCA.FractionAdmits(2, 10, 1000, 20), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.FractionAdmits(3, 10, 1000, 20), Is.False);
		}

		// StillDelayed / IntervalBlocks / LimitReached / LimitAdmits

		[Test]
		public void DelayBlocksUntilTheTickPasses()
		{
			Assert.That(BaseBuilderQueueEvalCA.StillDelayed(10, 9), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.StillDelayed(10, 10), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.StillDelayed(10, 11), Is.False);
		}

		[Test]
		public void IntervalBlocksOnlyWhenConfiguredAndCooling()
		{
			Assert.That(BaseBuilderQueueEvalCA.IntervalBlocks(true, true), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.IntervalBlocks(true, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.IntervalBlocks(false, true), Is.False);
		}

		[Test]
		public void LimitsCompareAgainstStandingAndProducing()
		{
			Assert.That(BaseBuilderQueueEvalCA.LimitReached(2, 2), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.LimitReached(1, 2), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.LimitAdmits(false, 0, 99, 99), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.LimitAdmits(true, 3, 1, 1), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.LimitAdmits(true, 3, 2, 1), Is.False);
		}

		// NavalBlocked — the area scan stays lazy

		[Test]
		public void NavalScanRunsOnlyWhenNavalAndWatered()
		{
			var scanned = false;
			bool Scan() => scanned = true;

			Assert.That(BaseBuilderQueueEvalCA.NavalBlocked(false, true, Scan), Is.False);
			Assert.That(scanned, Is.False);

			Assert.That(BaseBuilderQueueEvalCA.NavalBlocked(true, true, Scan), Is.True);
			Assert.That(scanned, Is.False);

			Assert.That(BaseBuilderQueueEvalCA.NavalBlocked(true, false, () => true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.NavalBlocked(true, false, () => false), Is.True);
		}

		// LowPowerBlocked

		[Test]
		public void LowPowerBlocksWithoutManagerOrFloorOrSufficiency()
		{
			Assert.That(BaseBuilderQueueEvalCA.LowPowerBlocked(false, 0, 100, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.LowPowerBlocked(true, 50, 100, true), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.LowPowerBlocked(true, 100, 100, false), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.LowPowerBlocked(true, 100, 100, true), Is.False);
		}

		// ExpansionBalance

		[Test]
		public void ExpansionBalanceComparesNetEconomyToRefineries()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionBalance(3, 1, 1, 0, 3), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.ExpansionBalance(3, 1, 1, 0, 4), Is.False);
		}

		// BetterOpeningCandidate

		[Test]
		public void OpeningCandidateWinsOnFractionThenOrdinalName()
		{
			Assert.That(BaseBuilderQueueEvalCA.BetterOpeningCandidate(5, 4, "b", "a"), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.BetterOpeningCandidate(4, 5, "a", "b"), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.BetterOpeningCandidate(5, 5, "a", "b"), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.BetterOpeningCandidate(5, 5, "b", "a"), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.BetterOpeningCandidate(5, 5, "a", "a"), Is.False);
		}

		// ResourcePickCenter / ClaimFieldOrTarget

		[Test]
		public void ResourceScanCentresHomeWhileFailing()
		{
			var req = new CPos(20, 20);
			var res = new CPos(30, 30);
			Assert.That(BaseBuilderQueueEvalCA.ResourcePickCenter(1, Base, req, res), Is.EqualTo(Base));
			Assert.That(BaseBuilderQueueEvalCA.ResourcePickCenter(0, Base, req, res), Is.EqualTo(req));
			Assert.That(BaseBuilderQueueEvalCA.ResourcePickCenter(0, Base, null, res), Is.EqualTo(res));
			Assert.That(BaseBuilderQueueEvalCA.ResourcePickCenter(0, Base, null, null), Is.EqualTo(Base));
		}

		[Test]
		public void ClaimFieldOverridesTheCrawlAim()
		{
			var claim = new CPos(40, 40);
			var target = new CPos(50, 50);
			Assert.That(BaseBuilderQueueEvalCA.ClaimFieldOrTarget(claim, target), Is.EqualTo(claim));
			Assert.That(BaseBuilderQueueEvalCA.ClaimFieldOrTarget(null, target), Is.EqualTo(target));
		}

		// PickFacingVariant — the 1024-circle quadrant arc-sine; facings [0, 256, 512, 768]

		static readonly ImmutableArray<WAngle> CardinalFacings =
			ImmutableArray.Create(new WAngle(0), new WAngle(256), new WAngle(512), new WAngle(768));

		[Test]
		public void MissingOrEmptyFacingsKeepVariantZero()
		{
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(1, 0, 0), default), Is.EqualTo(0));
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(1, 0, 0), ImmutableArray<WAngle>.Empty), Is.EqualTo(0));
		}

		[Test]
		public void FacingFollowsTheDirectionAndFoldRules()
		{
			// East: arcSin(1024)=256, folded to 512-256=256 -> facing 256.
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(1, 0, 0), CardinalFacings), Is.EqualTo(1));
			// West: folded to 512+256=768 -> facing 768.
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(-1, 0, 0), CardinalFacings), Is.EqualTo(3));
			// Southwest: folded to -256 (angularly 768) -> facing 768.
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(-1, -1, 0), CardinalFacings), Is.EqualTo(3));
			// North and the zero-length guard (anchor aim) both resolve to facing 0.
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(0, 1, 0), CardinalFacings), Is.EqualTo(0));
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(0, 0, 0), CardinalFacings), Is.EqualTo(0));
			// South keeps the raw arcSin(0)=0 — the upstream quirk, preserved.
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(0, -1, 0), CardinalFacings), Is.EqualTo(0));
		}

		[Test]
		public void FacingTiesKeepTheEarliestIndex()
		{
			var duplicated = ImmutableArray.Create(new WAngle(256), new WAngle(256));
			Assert.That(BaseBuilderQueueEvalCA.PickFacingVariant(new WVec(1, 0, 0), duplicated), Is.EqualTo(0));
		}

		// PicksRandomVariant — variants without facings draw; either half alone does not

		[Test]
		public void RandomVariantNeedsVariantsWithoutFacings()
		{
			Assert.That(BaseBuilderQueueEvalCA.PicksRandomVariant(false, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.PicksRandomVariant(false, true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.PicksRandomVariant(true, true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.PicksRandomVariant(true, false), Is.True);
		}

		// PlacementCellAdmitted — short-circuit order is part of the contract

		[Test]
		public void AdmitNeedsAllFourChecks()
		{
			Assert.That(BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => true, () => true, () => true, () => true), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => false, () => true, () => true, () => true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => true, () => false, () => true, () => true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => true, () => true, () => false, () => true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => true, () => true, () => true, () => false), Is.False);
		}

		[Test]
		public void AdmitShortCircuitsInOrder()
		{
			var calls = new List<int>();
			bool Track(int n, bool v) { calls.Add(n); return v; }

			BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => Track(0, false), () => Track(1, true), () => Track(2, true), () => Track(3, true));
			Assert.That(calls, Is.EqualTo(new[] { 0 }));

			calls.Clear();
			BaseBuilderQueueEvalCA.PlacementCellAdmitted(() => Track(0, true), () => Track(1, false), () => Track(2, true), () => Track(3, true));
			Assert.That(calls, Is.EqualTo(new[] { 0, 1 }));
		}

		// AdvisorPick

		[Test]
		public void AdvisorChoiceStandsOnlyWhenItWasOffered()
		{
			var a = new CPos(1, 1);
			var b = new CPos(2, 2);
			var offered = new List<CPos> { a };
			Assert.That(BaseBuilderQueueEvalCA.AdvisorPick(a, offered), Is.EqualTo(a));
			Assert.That(BaseBuilderQueueEvalCA.AdvisorPick(b, offered), Is.EqualTo(a));
			Assert.That(BaseBuilderQueueEvalCA.AdvisorPick(null, offered), Is.EqualTo(a));
		}

		// FootprintGap

		[Test]
		public void FootprintGapClassifiesFlushThenRingThenIllegal()
		{
			var zone0 = new HashSet<CPos> { new CPos(1, 1) };
			var zone1 = new HashSet<CPos> { new CPos(2, 2) };
			Assert.That(BaseBuilderQueueEvalCA.FootprintGap(new[] { new CPos(1, 1) }, zone0, zone1), Is.EqualTo(0));
			Assert.That(BaseBuilderQueueEvalCA.FootprintGap(new[] { new CPos(2, 2) }, zone0, zone1), Is.EqualTo(1));
			Assert.That(BaseBuilderQueueEvalCA.FootprintGap(new[] { new CPos(9, 9) }, zone0, zone1), Is.EqualTo(-1));
		}

		// DockHasExit

		[Test]
		public void DockNeedsAnOnMapExitOutsideTheFootprint()
		{
			var dock = new CPos(5, 5);
			var open = new HashSet<CPos> { dock };
			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(dock, open, _ => true), Is.True);

			var walled = new HashSet<CPos>();
			for (var dy = -1; dy <= 1; dy++)
				for (var dx = -1; dx <= 1; dx++)
					walled.Add(new CPos(dock.X + dx, dock.Y + dy));

			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(dock, walled, _ => true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(dock, open, n => n != new CPos(6, 5)), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(dock, walled, _ => false), Is.False);
		}

		// FirstByOrder / FirstRequestedRefinery (F-CBL1/F4)

		[Test]
		public void FirstByOrderPicksTheLowestKeyNotInsertionOrder()
		{
			var requests = new Dictionary<string, int> { ["alpha"] = 1, ["beta"] = 2, ["gamma"] = 3 };
			Assert.That(BaseBuilderQueueEvalCA.FirstByOrder(requests, s => s switch { "beta" => 7, "gamma" => 3, _ => 9 }), Is.EqualTo("gamma"));
		}

		[Test]
		public void FirstByOrderIsStableUnderKeyEnumerationOrder()
		{
			var forward = new Dictionary<string, int> { ["alpha"] = 1, ["beta"] = 2 };
			var reverse = new Dictionary<string, int> { ["beta"] = 2, ["alpha"] = 1 };
			Assert.That(BaseBuilderQueueEvalCA.FirstByOrder(forward, s => s == "beta" ? 1 : 2), Is.EqualTo("beta"));
			Assert.That(BaseBuilderQueueEvalCA.FirstByOrder(reverse, s => s == "beta" ? 1 : 2), Is.EqualTo("beta"));
		}

		[Test]
		public void FirstByOrderYieldsDefaultOnNullOrEmpty()
		{
			Assert.That(BaseBuilderQueueEvalCA.FirstByOrder<string, int>(null, _ => 0), Is.Null);
			Assert.That(BaseBuilderQueueEvalCA.FirstByOrder(new Dictionary<string, int>(), _ => 0), Is.Null);
		}

		// ECON-A (SPEC_2026-10-05_econ_logistics Part A) — expansion demand scheduling
		// ExpansionDue

		[Test]
		public void DemandItemQueuesOnlyInsideItsEtaWindow()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionDue(100, 30, 69), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.ExpansionDue(100, 30, 70), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.ExpansionDue(100, 30, 200), Is.True);
		}

		[Test]
		public void UnreachableEtaIsNeverDue()
			=> Assert.That(BaseBuilderQueueEvalCA.ExpansionDue(int.MaxValue, 30, 200000), Is.False);

		// ExpansionTravelTicks

		[Test]
		public void TravelTicksDividePathLengthBySpeed()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(1000, 100), Is.EqualTo(10));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(999, 100), Is.EqualTo(9));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(0, 100), Is.EqualTo(0));
		}

		[Test]
		public void StalledOrPathlessTravelIsFarFuture()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(1000, 0), Is.EqualTo(int.MaxValue));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(1000, -5), Is.EqualTo(int.MaxValue));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(long.MaxValue, 1), Is.EqualTo(int.MaxValue));
		}

		// DefenceHoldPermitted — the sole-producer slack gate

		[Test]
		public void DefenceHoldAlwaysAllowedWithMultipleProducers()
		{
			Assert.That(BaseBuilderQueueEvalCA.DefenceHoldPermitted(false, 90000, 500), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.DefenceHoldPermitted(false, 0, 0), Is.True);
		}

		[Test]
		public void SoleProducerHoldsOnlyInsideTheSlackWindow()
		{
			Assert.That(BaseBuilderQueueEvalCA.DefenceHoldPermitted(true, 501, 500), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.DefenceHoldPermitted(true, 500, 500), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.DefenceHoldPermitted(true, 0, 500), Is.True);
		}

		// ChooseExpansionDefence — strength ordering, affordability at ETA, deterministic ties

		static readonly ActorInfo GunA = new("guna");
		static readonly ActorInfo GunB = new("gunb");
		static readonly ActorInfo GunC = new("gunc");

		[Test]
		public void StrongestAffordableDefenceWins()
		{
			var candidates = new List<(ActorInfo Info, int Strength, int Cost)>
			{
				(GunA, 100, 500),
				(GunB, 300, 900),
				(GunC, 50, 100),
			};

			Assert.That(BaseBuilderQueueEvalCA.ChooseExpansionDefence(candidates, 1000), Is.SameAs(GunB));
		}

		[Test]
		public void UnaffordableDefenceIsSkippedAtEta()
		{
			var candidates = new List<(ActorInfo Info, int Strength, int Cost)>
			{
				(GunA, 100, 500),
				(GunB, 300, 900),
			};

			// Projected cash covers the weak gun only — strength does not buy past the ETA price.
			Assert.That(BaseBuilderQueueEvalCA.ChooseExpansionDefence(candidates, 600), Is.SameAs(GunA));
			Assert.That(BaseBuilderQueueEvalCA.ChooseExpansionDefence(candidates, 100), Is.Null);
		}

		[Test]
		public void DefenceTiesBreakOnCostThenOrdinalName()
		{
			var byCost = new List<(ActorInfo Info, int Strength, int Cost)>
			{
				(GunB, 200, 800),
				(GunA, 200, 500),
			};

			Assert.That(BaseBuilderQueueEvalCA.ChooseExpansionDefence(byCost, 1000), Is.SameAs(GunA));

			var byName = new List<(ActorInfo Info, int Strength, int Cost)>
			{
				(GunC, 200, 500),
				(GunB, 200, 500),
				(GunA, 200, 500),
			};

			Assert.That(BaseBuilderQueueEvalCA.ChooseExpansionDefence(byName, 1000), Is.SameAs(GunA));
		}

		// ECON-A-FIX (REVIEW_2026-10-06_econ_a) — the six findings' seams.

		// R2: ExpansionPathLength — the pathfinder contract returns target→source; the walk must not
		// prepend a source→first-cell chord (the review's probe: 10-cell route misread as 20 cells).

		static WPos FakeCellCenter(CPos c) => new(c.X * 1024 + 512, c.Y * 1024 + 512, 0);

		[Test]
		public void ReversedPathHasNoSourceToTargetChord()
		{
			// Review probe: source (0,0), path returned [target (10,0), (5,0), source (0,0)] —
			// 10 cells x 1024 = 10240 WDist, travel 102 ticks at speed 100 — not 20480/204.
			var path = new[] { new CPos(10, 0), new CPos(5, 0), new CPos(0, 0) };
			var distance = BaseBuilderQueueEvalCA.ExpansionPathLength(path, FakeCellCenter);
			Assert.That(distance, Is.EqualTo(10240));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTravelTicks(distance, 100), Is.EqualTo(102));
		}

		[Test]
		public void PathLengthIsZeroForDegeneratePaths()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionPathLength(null, FakeCellCenter), Is.EqualTo(0));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionPathLength(new CPos[0], FakeCellCenter), Is.EqualTo(0));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionPathLength(new[] { new CPos(4, 4) }, FakeCellCenter), Is.EqualTo(0));
		}

		[Test]
		public void AdjacentPathCountsOnlyItsOwnSegment()
		{
			// A two-cell path [target, source] is one 1024-length step — nothing is added.
			var path = new[] { new CPos(5, 0), new CPos(4, 0) };
			Assert.That(BaseBuilderQueueEvalCA.ExpansionPathLength(path, FakeCellCenter), Is.EqualTo(1024));
		}

		// R1: EtaOccupantBlocks — the fog-honest blocker verdict. `known` = ours, on a visible cell, or
		// remembered; everything else mirrors IsBlockedBy under BlockedByActor.Immovable.

		[Test]
		public void HiddenOccupantsNeverBlockTheEta()
		{
			// The R1 invariance: an unseen enemy building can neither block nor unblock the estimate —
			// every flag combination with known=false must return false.
			foreach (var movable in new[] { false, true })
			foreach (var allied in new[] { false, true })
			foreach (var moving in new[] { false, true })
			foreach (var removable in new[] { false, true })
			foreach (var transitOnly in new[] { false, true })
			foreach (var crushable in new[] { false, true })
				Assert.That(
					BaseBuilderQueueEvalCA.EtaOccupantBlocks(false, movable, allied, moving, removable, transitOnly, crushable),
					Is.False, $"hidden occupant must never block (movable {movable}, allied {allied}, moving {moving})");
		}

		[Test]
		public void KnownImmovableBlocksButKnownPassablesDoNot()
		{
			// A standing building (immovable, uncrushable) blocks — same as the live Immovable check.
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, false, false, false, false, false, false), Is.True);

			// The Immovable exemptions carry over: crushable by our locomotor, a removable gate, a
			// transit-only footprint cell, a movable ally, or anything currently moving.
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, false, false, false, false, false, true), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, false, false, false, true, false, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, false, false, false, false, true, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, true, true, false, false, false, false), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, true, false, true, false, false, false), Is.False);

			// A stationary enemy vehicle blocks under Immovable (it cannot be ordered aside).
			Assert.That(BaseBuilderQueueEvalCA.EtaOccupantBlocks(true, true, false, false, false, false, false), Is.True);
		}

		// R3: ExpansionTransform — the ReplacedByActor edge is a one-shot transition.

		[Test]
		public void DeployTransitionFiresExactlyOnce()
		{
			var deploy = BaseBuilderQueueEvalCA.ExpansionTransformTransition.Deploy;
			var waiting = BaseBuilderQueueEvalCA.ExpansionTransformTransition.Waiting;
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(false, true, true, true), Is.EqualTo(deploy));

			// The consumed edge: once Deployed, the still-readable ReplacedByActor never re-fires —
			// this is the sweep's queue-cadence and cleanup invariant.
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(true, true, true, true), Is.EqualTo(waiting));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(true, true, false, true), Is.EqualTo(waiting));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(true, true, true, false), Is.EqualTo(waiting));
		}

		[Test]
		public void TransformWithoutReplacementOrWaitingKeepsWaiting()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(false, false, false, false), Is.EqualTo(
				BaseBuilderQueueEvalCA.ExpansionTransformTransition.Waiting));
		}

		[Test]
		public void UnusableOrCapturedReplacementFailsTheDemand()
		{
			// replacementUsable bundles alive + in-world + ours — a disposed or captured yard ends the journey.
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(false, true, false, true), Is.EqualTo(
				BaseBuilderQueueEvalCA.ExpansionTransformTransition.Fail));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(false, true, false, false), Is.EqualTo(
				BaseBuilderQueueEvalCA.ExpansionTransformTransition.Fail));
		}

		[Test]
		public void OwnNonYardReplacementRelocatesTheDemand()
			=> Assert.That(BaseBuilderQueueEvalCA.ExpansionTransform(false, true, true, false), Is.EqualTo(
				BaseBuilderQueueEvalCA.ExpansionTransformTransition.Relocate));

		// R4: ExpansionJourneyCommitted — silence is indeterminate, a foreign destination lapses.

		[Test]
		public void JourneyLapsesOnRedirectButNotOnSilence()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourneyCommitted(false, false), Is.True);   // no targets: transform/idle — the window decides
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourneyCommitted(true, true), Is.True);     // last target near the deploy cell
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourneyCommitted(true, false), Is.False);   // aiming elsewhere: a redirect took it
		}

		// R5: DemandItemUnambiguous — the (producer, name) token must be exclusive.

		[Test]
		public void DemandBindingNeedsAnExclusiveName()
		{
			Assert.That(BaseBuilderQueueEvalCA.DemandItemUnambiguous(false, false), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.DemandItemUnambiguous(true, false), Is.False);   // same name queued or in flight on the producer
			Assert.That(BaseBuilderQueueEvalCA.DemandItemUnambiguous(false, true), Is.False);   // another demand bound it
			Assert.That(BaseBuilderQueueEvalCA.DemandItemUnambiguous(true, true), Is.False);
		}

		// R6: RefineryAnchorReservations — the demand-linked anchor hold lifecycle.

		[Test]
		public void AHeldAnchorRefusesOtherDemands()
		{
			var table = new RefineryAnchorReservations();
			var anchor = new CPos(5, 5);
			var first = new object();
			var second = new object();

			Assert.That(table.TryReserve(anchor, first, now: 100, until: 200, taken: _ => false), Is.True);
			Assert.That(table.TryReserve(anchor, second, now: 110, until: 300, taken: _ => false), Is.False);
			Assert.That(table.LiveAt(anchor, 150), Is.True);
			Assert.That(table.LiveFor(anchor, first, 150), Is.True);
			Assert.That(table.LiveFor(anchor, second, 150), Is.False);
		}

		[Test]
		public void TheOwningDemandRefreshesItsHold()
		{
			var table = new RefineryAnchorReservations();
			var anchor = new CPos(1, 1);
			var owner = new object();

			Assert.That(table.TryReserve(anchor, owner, 0, 50, _ => false), Is.True);
			Assert.That(table.TryReserve(anchor, owner, 40, 500, _ => false), Is.True);   // own refresh extends
			Assert.That(table.LiveAt(anchor, 400), Is.True);
			Assert.That(table.LiveAt(anchor, 600), Is.False);
		}

		[Test]
		public void ExpiredAndReleasedHoldsFreeTheAnchor()
		{
			var table = new RefineryAnchorReservations();
			var a = new CPos(2, 2);
			var b = new CPos(3, 3);
			var first = new object();
			var second = new object();

			table.TryReserve(a, first, 0, 10, _ => false);
			table.TryReserve(b, first, 0, 1000, _ => false);

			Assert.That(table.TryReserve(a, second, 50, 500, _ => false), Is.True);   // expired → re-taken
			Assert.That(table.Release(b, second), Is.False);                          // not the owner
			Assert.That(table.Release(b, first), Is.True);
			Assert.That(table.LiveAt(b, 50), Is.False);
			Assert.That(table.TryReserve(b, second, 60, 200, _ => false), Is.True);
		}

		[Test]
		public void TakenAnchorsCannotBeReservedOrSurvivePrune()
		{
			// `taken` is the caller's probe for served/parked/pending — ground truth over a hold.
			var table = new RefineryAnchorReservations();
			var anchor = new CPos(4, 4);
			var owner = new object();

			Assert.That(table.TryReserve(anchor, owner, 0, 100, c => c == anchor), Is.False);   // already taken
			Assert.That(table.TryReserve(anchor, owner, 0, 100, _ => false), Is.True);
			Assert.That(table.Prune(10, c => c == anchor), Is.EqualTo(1));                      // taken under the hold → pruned
			Assert.That(table.LiveAt(anchor, 10), Is.False);
		}

		[Test]
		public void CommitClearsAReservation()
		{
			var table = new RefineryAnchorReservations();
			var anchor = new CPos(6, 6);
			var owner = new object();

			table.TryReserve(anchor, owner, 0, 100, _ => false);
			table.Clear(anchor);                                                                // RefineryClaimCommitted promotes it
			Assert.That(table.LiveAt(anchor, 10), Is.False);
			Assert.That(table.TryReserve(anchor, new object(), 10, 50, _ => false), Is.True);
		}
	}
}
