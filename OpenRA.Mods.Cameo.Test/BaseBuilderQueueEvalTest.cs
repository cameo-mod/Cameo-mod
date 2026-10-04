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
	}
}
