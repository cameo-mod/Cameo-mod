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
using OpenRA.Mods.Common.Traits;

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

		// FIX-RA-REFINERY: real refinery shapes — the '=' bib is passable and must not wall the dock.
		// Shapes follow the resolved yaml footprints (and AUDIT_2026-10-09_all_faction_docks.md for
		// families whose actor defs live outside this tree); dock = CellContaining(CenterOfCell(topLeft)
		// + CenterOffset(world) + DockOffset) is map-grid dependent, so each family is covered two ways:
		// every passable footprint cell keeps an exit, and audit-verified or exactly-projected dock
		// cells keep theirs. The ^RAPROC enclosure (all nine scan cells inside the full footprint) was
		// the confirmed defect; WC2/OP2's inherited zero-offset docks shared it under Tiles semantics.

		static Dictionary<CVec, FootprintCellType> Shape(params string[] rows)
		{
			var cells = new Dictionary<CVec, FootprintCellType>();
			for (var y = 0; y < rows.Length; y++)
				for (var x = 0; x < rows[y].Length; x++)
					cells[new CVec(x, y)] = (FootprintCellType)rows[y][x];

			return cells;
		}

		// Every passable cell ('='/ '+') of every resolved refinery shape must keep an on-map exit at an
		// ordinary interior site — a dock landing on any bib cell is reachable. Under the old
		// BuildingInfo.Tiles footprint these cells were walls, which enclosed RAPROC outright.
		[TestCase("RAPROC_RA1_Allies_Soviets_Japan", "_X_", "xxx", "X==", "===")]
		[TestCase("TDPROC_TD_GDI_Nod_TS_Forgotten_Cabal", "_x_", "xxx", "===", "===")]
		[TestCase("TS_GDI_Nod", "xxx_", "xxx=", "_===")]
		[TestCase("D2K_five_factions", "=xx", "xx=", "===")]
		[TestCase("SC_Terran_Protoss_Zerg", "_x_", "xxx", "===")]
		[TestCase("SC_Zerg_Terran_Protoss_townhall", "xxx", "xxx", "===")]
		[TestCase("WC2_Humans_Orcs_lumber", "xxx", "xxx", "===")]
		[TestCase("RA2_Allies_Soviets", "xxx=", "xxx=", "x+==")]
		[TestCase("RA2Mod_Consortium", "xxx=", "xxxx", "=xx=")]
		[TestCase("OP2_Eden_Plymouth_smelter", "xxx", "x==", "===")]
		public void EveryPassableCellOfRealRefineryShapeKeepsAnExit(string name, params string[] rows)
		{
			var shape = Shape(rows);
			var blocking = new HashSet<CPos>(BaseBuilderQueueEvalCA.DockBlockingFootprint(shape, CPos.Zero));
			foreach (var kv in shape)
			{
				if (kv.Value != FootprintCellType.OccupiedPassable
					&& kv.Value != FootprintCellType.OccupiedPassableTransitOnly)
					continue;

				var dock = new CPos(kv.Key.X, kv.Key.Y);
				Assert.That(BaseBuilderQueueEvalCA.DockHasExit(dock, blocking, _ => true),
					Is.True, $"{name}: passable cell ({dock.X},{dock.Y}) must keep an on-map exit");
			}
		}

		// Dock cells confirmed by the audit (RAPROC/TDPROC) or by exact integer projection:
		// zero-offset inherited ^Refinery docks (WC2 lumber, OP2 smelters) land on centre (1,1) of a
		// 3x3 — WC2's is even an impassable 'x' cell, which the dock check must tolerate; RA2 Allies
		// (DockOffset 1086,1086, no LocalCenterOffset) projects to (3,1); D2K's (1c5,0c5) projects
		// outside the footprint at (3,2).
		[TestCase("RAPROC", new[] { "_X_", "xxx", "X==", "===" }, 1, 2)]
		[TestCase("TDPROC", new[] { "_x_", "xxx", "===", "===" }, 0, 2)]
		[TestCase("WC2_OP2_inherited_zero_offset", new[] { "xxx", "xxx", "===" }, 1, 1)]
		[TestCase("OP2_smelter_zero_offset", new[] { "xxx", "x==", "===" }, 1, 1)]
		[TestCase("RA2_Allies", new[] { "xxx=", "xxx=", "x+==" }, 3, 1)]
		[TestCase("D2K_off_footprint_dock", new[] { "=xx", "xx=", "===" }, 3, 2)]
		public void RealRefineryDockCellKeepsAnExit(string name, string[] rows, int dockX, int dockY)
		{
			var blocking = new HashSet<CPos>(BaseBuilderQueueEvalCA.DockBlockingFootprint(Shape(rows), CPos.Zero));
			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(new CPos(dockX, dockY), blocking, _ => true),
				Is.True, $"{name}: dock ({dockX},{dockY}) must keep an on-map exit outside the impassable footprint");
		}

		[Test]
		public void RaRefineryDockCellsThatWereWallsArePassable()
		{
			// ^RAPROC bib cells that BuildingInfo.Tiles reported as footprint walls.
			var blocking = new HashSet<CPos>(BaseBuilderQueueEvalCA.DockBlockingFootprint(
				Shape("_X_", "xxx", "X==", "==="), CPos.Zero));

			Assert.That(blocking, Does.Not.Contain(new CPos(1, 2)), "the dock's own bib cell is passable");
			Assert.That(blocking, Does.Not.Contain(new CPos(2, 3)), "'=' bib cells are not walls");
			Assert.That(blocking, Does.Contain(new CPos(1, 1)), "'x' cells stay blocking");
			Assert.That(blocking, Does.Contain(new CPos(0, 2)), "'X' cells stay blocking");
		}

		[Test]
		public void ImpassableCellsStillWallTheDock()
		{
			// x/X stay blocking — a dock ringed by real occupied cells has no exit.
			var blocking = new HashSet<CPos>(BaseBuilderQueueEvalCA.DockBlockingFootprint(
				Shape("xxx", "x=x", "xxx"), CPos.Zero));

			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(new CPos(1, 1), blocking, _ => true), Is.False);

			// '+' transit-only lanes are the dock's own egress path — they don't wall it either.
			var transit = new HashSet<CPos>(BaseBuilderQueueEvalCA.DockBlockingFootprint(
				Shape("+++", "+x+", "+++"), CPos.Zero));
			Assert.That(BaseBuilderQueueEvalCA.DockHasExit(new CPos(1, 1), transit, _ => true), Is.True);
		}

		[Test]
		public void LawRefineryNoSiteDefersInsteadOfFailing()
		{
			// Deferral is law+refinery only — every other class keeps the ordinary failure path.
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Refinery), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(false, BuildingType.Refinery), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Building), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Defense), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Fragile), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.BaseCrawl), Is.False);
		}

		// FIX-RA-REFINERY R1: the saturated-placement latch — the recovery arm covers every case
		// the expansion nudge can't act on, probes on a per-episode timer, and releases only on a
		// real world change against the baseline captured at saturation.

		[Test]
		public void LatchRecoveryCoversEveryUnNudgeableCase()
		{
			Assert.Multiple(() =>
			{
				Assert.That(BaseBuilderQueueEvalCA.LatchRecoveryApplies(0, true, false), Is.True, "no expansion modules");
				Assert.That(BaseBuilderQueueEvalCA.LatchRecoveryApplies(0, false, false), Is.True, "no modules, no centre");
				Assert.That(BaseBuilderQueueEvalCA.LatchRecoveryApplies(2, false, false), Is.True, "null failing centre");
				Assert.That(BaseBuilderQueueEvalCA.LatchRecoveryApplies(2, true, true), Is.True, "relocation hold live");
				Assert.That(BaseBuilderQueueEvalCA.LatchRecoveryApplies(2, false, true), Is.True, "hold + null centre");
				Assert.That(BaseBuilderQueueEvalCA.LatchRecoveryApplies(2, true, false), Is.False,
					"modules + centre + no hold = the nudge arm's own case");
			});
		}

		[Test]
		public void LatchProbeReleasesOnlyOnWorldChange()
		{
			// Snapshot at saturation: 10 buildings, 2 providers.
			Assert.Multiple(() =>
			{
				Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(9, 10, 2, 2), Is.True, "a building was lost — room freed");
				Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(10, 10, 3, 2), Is.True, "a provider appeared — new build area");
				Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(9, 10, 3, 2), Is.True, "either change suffices");
				Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(10, 10, 2, 2), Is.False, "unchanged world holds the latch");
				Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(11, 10, 2, 2), Is.False, "more buildings don't free space");
				Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(10, 10, 1, 2), Is.False, "fewer providers don't help");
			});
		}

		[Test]
		public void LatchProbeTimerTicksDownAndRearms()
		{
			// delay 3: three ticks per probe window, rearmed on expiry.
			var timer = 3;
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeDue(ref timer, 3), Is.False);
			Assert.That(timer, Is.EqualTo(2));
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeDue(ref timer, 3), Is.False);
			Assert.That(timer, Is.EqualTo(1));
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeDue(ref timer, 3), Is.True);
			Assert.That(timer, Is.EqualTo(3), "expiry re-arms a full delay");
		}

		[Test]
		public void LatchRecoveryEpisodeReleasesOnlyAfterFreshProbe()
		{
			// Control-flow regression mirroring Tick's latch: an un-nudgeable saturation (null
			// failing centre) holds across static probes, releases when a provider appears, and
			// the NEXT episode starts on a fresh snapshot+timer — never on the stale one that
			// previously let a water-check-cached baseline release or starve the latch.
			const int delay = 2;
			var failRetryTicks = delay;         // reset at saturation
			var latchedBuildings = 10;          // snapshot at saturation
			var latchedProviders = 2;

			var released = false;
			for (var i = 0; i < 20 && !released; i++)
			{
				if (!BaseBuilderQueueEvalCA.LatchProbeDue(ref failRetryTicks, delay))
					continue;

				released = BaseBuilderQueueEvalCA.LatchProbeReleases(10, latchedBuildings, 2, latchedProviders);
			}

			Assert.That(released, Is.False, "static world never releases the latch");

			// A new base provider arrives — the next due probe releases.
			for (var i = 0; i < delay && !BaseBuilderQueueEvalCA.LatchProbeDue(ref failRetryTicks, delay); i++) { }
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(10, latchedBuildings, 3, latchedProviders), Is.True);

			// Second episode: fresh snapshot (12 buildings, 1 provider) and a fresh timer —
			// the prior episode's countdown must not bleed through.
			failRetryTicks = delay;
			latchedBuildings = 12;
			latchedProviders = 1;
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeDue(ref failRetryTicks, delay), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeDue(ref failRetryTicks, delay), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.LatchProbeReleases(12, latchedBuildings, 1, latchedProviders), Is.False,
				"the new baseline holds until the world actually changes again");
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

		// R1-FIX2: EtaOccupantBlocks — the fog-honest blocker verdict. `known` = ours or legally
		// visible to us (CanBeViewedByPlayer — a lit cell does not expose a cloaked actor); everything
		// else mirrors IsBlockedBy under BlockedByActor.Immovable.

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

		// R4-FIX2: ExpansionJourney — silence is its own verdict now, not a silent pass; and
		// JourneyRenewsDemand — only a provably ongoing journey extends the window.
		// (REREVIEW_2026-10-06_econ_a: a targetless WaitFor demand renewed expiry 200→300→1100.)

		[Test]
		public void JourneyClassifiesRedirectSilenceAndCommitment()
		{
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourney(false, false), Is.EqualTo(BaseBuilderQueueEvalCA.ExpansionJourneyState.Indeterminate));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourney(false, true), Is.EqualTo(BaseBuilderQueueEvalCA.ExpansionJourneyState.Indeterminate));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourney(true, true), Is.EqualTo(BaseBuilderQueueEvalCA.ExpansionJourneyState.Committed));
			Assert.That(BaseBuilderQueueEvalCA.ExpansionJourney(true, false), Is.EqualTo(BaseBuilderQueueEvalCA.ExpansionJourneyState.Redirected));
		}

		[Test]
		public void TargetlessActivityNeverRenewsTheDemand()
		{
			// Sol's R4 probe row: an in-world mobile MCV redirected onto WaitFor(() => false) —
			// non-idle, but no positional target. The window must NOT renew; the demand dies at its
			// outstanding ExpiresTick.
			Assert.That(BaseBuilderQueueEvalCA.JourneyRenewsDemand(
				BaseBuilderQueueEvalCA.ExpansionJourneyState.Indeterminate, idle: false, hasMobile: true), Is.False);

			// Silence on an idle traveller doesn't renew either — the idle window itself decides.
			Assert.That(BaseBuilderQueueEvalCA.JourneyRenewsDemand(
				BaseBuilderQueueEvalCA.ExpansionJourneyState.Indeterminate, idle: true, hasMobile: true), Is.False);

			// A committed chain on a moving traveller renews — the journey is provably ongoing.
			Assert.That(BaseBuilderQueueEvalCA.JourneyRenewsDemand(
				BaseBuilderQueueEvalCA.ExpansionJourneyState.Committed, idle: false, hasMobile: true), Is.True);

			// A committed chain on an idle traveller doesn't — stopped is stopped; expiry decides.
			Assert.That(BaseBuilderQueueEvalCA.JourneyRenewsDemand(
				BaseBuilderQueueEvalCA.ExpansionJourneyState.Committed, idle: true, hasMobile: true), Is.False);

			// A redirected chain never renews (the sweep already lapses it — defence in depth).
			Assert.That(BaseBuilderQueueEvalCA.JourneyRenewsDemand(
				BaseBuilderQueueEvalCA.ExpansionJourneyState.Redirected, idle: false, hasMobile: true), Is.False);

			// A unit-less traveller can't idle — its existence is the in-flight relocation.
			Assert.That(BaseBuilderQueueEvalCA.JourneyRenewsDemand(
				BaseBuilderQueueEvalCA.ExpansionJourneyState.Indeterminate, idle: false, hasMobile: false), Is.True);
		}

		// R1-FIX2: the remembered-facts verdicts behind FrozenBlockedCells. The adapter itself takes
		// no live-actor input — invariance under hidden-state change is structural (nothing remains
		// to dereference), so the world-free tests pin the fact-level rule.

		[Test]
		public void FrozenFootprintBlocksUnlessRememberedPassable()
		{
			Assert.That(BaseBuilderQueueEvalCA.FrozenOccupantBlocks(false, false, false), Is.True);   // remembered enemy building blocks
			Assert.That(BaseBuilderQueueEvalCA.FrozenOccupantBlocks(true, false, false), Is.False);   // transit-only cell
			Assert.That(BaseBuilderQueueEvalCA.FrozenOccupantBlocks(false, true, false), Is.False);   // remembered gate/DoesNotBlock admits us
			Assert.That(BaseBuilderQueueEvalCA.FrozenOccupantBlocks(false, false, true), Is.False);   // remembered crushable
			Assert.That(BaseBuilderQueueEvalCA.FrozenOccupantBlocks(true, true, true), Is.False);
		}

		[Test]
		public void RememberedCrushableNeedsOverlapAndANonAlliedOwner()
		{
			Assert.That(BaseBuilderQueueEvalCA.RememberedCrushable(true, false, false), Is.True);    // enemy crushable wall: crush
			Assert.That(BaseBuilderQueueEvalCA.RememberedCrushable(true, false, true), Is.False);    // remembered ally: don't crush
			Assert.That(BaseBuilderQueueEvalCA.RememberedCrushable(true, true, true), Is.True);      // CrushedByFriendlies record
			Assert.That(BaseBuilderQueueEvalCA.RememberedCrushable(false, false, false), Is.False);  // no class overlap
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

		// REPAIR-B3 (SPEC_2026-10-09 §3): the atomic multi-anchor reservation — a queued refinery's
		// covered set binds or refuses as one; a partially owned set is never left behind.

		[Test]
		public void ReserveAllCommitsTheWholeSetOrNothing()
		{
			var table = new RefineryAnchorReservations();
			var set = new[] { new CPos(1, 1), new CPos(2, 2), new CPos(3, 3) };
			var owner = new object();
			var other = new object();

			Assert.That(table.TryReserveAll(set, owner, 0, 100, _ => false), Is.True);
			Assert.That(table.LiveSetFor(set, owner, 10), Is.True);

			// A foreign claim on any member refuses the whole second set — nothing half-taken.
			var overlap = new[] { new CPos(3, 3), new CPos(4, 4) };
			Assert.That(table.TryReserveAll(overlap, other, 10, 200, _ => false), Is.False);
			Assert.That(table.LiveFor(new CPos(4, 4), other, 10), Is.False);
			Assert.That(table.LiveAt(new CPos(4, 4), 10), Is.False);
		}

		[Test]
		public void ReserveAllRefusesTakenMembersWithoutPartialWrites()
		{
			var table = new RefineryAnchorReservations();
			var set = new[] { new CPos(1, 1), new CPos(2, 2) };
			var owner = new object();

			// The taken probe rejects member 2 — member 1 must not be left holding.
			Assert.That(table.TryReserveAll(set, owner, 0, 100, c => c == new CPos(2, 2)), Is.False);
			Assert.That(table.LiveAt(new CPos(1, 1), 10), Is.False);
			Assert.That(table.Count, Is.EqualTo(0));
		}

		[Test]
		public void ReserveAllRefreshesTheOwnersExistingHolds()
		{
			var table = new RefineryAnchorReservations();
			var owner = new object();
			var set = new[] { new CPos(1, 1), new CPos(2, 2) };

			table.TryReserve(new CPos(1, 1), owner, 0, 50, _ => false);
			Assert.That(table.TryReserveAll(set, owner, 10, 500, _ => false), Is.True);
			Assert.That(table.LiveAt(new CPos(1, 1), 400), Is.True);   // refreshed, not stuck at 50
			Assert.That(table.LiveAt(new CPos(2, 2), 400), Is.True);
		}

		[Test]
		public void ReleaseAllIsIdempotentAndClearsOnlyTheOwnersMembers()
		{
			var table = new RefineryAnchorReservations();
			var owner = new object();
			var other = new object();
			var set = new[] { new CPos(1, 1), new CPos(2, 2) };

			table.TryReserveAll(set, owner, 0, 100, _ => false);
			table.TryReserve(new CPos(9, 9), other, 0, 100, _ => false);

			Assert.That(table.ReleaseAll(set, owner), Is.EqualTo(2));
			Assert.That(table.ReleaseAll(set, owner), Is.EqualTo(0));  // idempotent
			Assert.That(table.LiveAt(new CPos(9, 9), 10), Is.True);    // other's hold untouched
		}

		[Test]
		public void ClearAllDropsAWholeCommittedSet()
		{
			var table = new RefineryAnchorReservations();
			var set = new[] { new CPos(1, 1), new CPos(2, 2), new CPos(3, 3) };

			table.TryReserveAll(set, new object(), 0, 100, _ => false);
			table.ClearAll(set);
			Assert.That(table.Count, Is.EqualTo(0));
		}

		[Test]
		public void ReleaseAllForOwnerFreesTheWholeDriftedSet()
		{
			// R3: teardown must not recompute the reserved set — between admission and release a
			// member may be taken/covered/re-modelled elsewhere, so a recomputed set could exclude
			// a live hold and leak it. Owner-keyed release frees everything the owner holds.
			var table = new RefineryAnchorReservations();
			var owner = new object();
			var other = new object();
			var set = new[] { new CPos(1, 1), new CPos(2, 2), new CPos(3, 3) };

			table.TryReserveAll(set, owner, 0, 100, _ => false);
			table.TryReserve(new CPos(9, 9), other, 0, 100, _ => false);

			Assert.That(table.ReleaseAllForOwner(owner), Is.EqualTo(3));
			Assert.That(table.LiveAt(new CPos(1, 1), 10), Is.False);
			Assert.That(table.LiveAt(new CPos(3, 3), 10), Is.False);
			Assert.That(table.LiveAt(new CPos(9, 9), 10), Is.True);    // other's hold untouched
			Assert.That(table.ReleaseAllForOwner(owner), Is.EqualTo(0)); // idempotent

			// And the freed anchors are reservable by someone else.
			Assert.That(table.TryReserve(new CPos(2, 2), other, 10, 50, _ => false), Is.True);
		}

		// REPAIR-B3 production lifecycle (VP rereview eafdfb6): the five wired call sites run
		// through the BaseBuilderQueueEvalCA seams below — exercised end-to-end against a
		// recording provider backed by the real RefineryAnchorReservations table, mirroring
		// ExpansionPlannerBotModule's version guard, taken probe and commit-clear semantics.

		sealed class RecordingRefineryLaw : IBotExpansionTargetProvider
		{
			public readonly RefineryAnchorReservations Reservations = new();
			public readonly HashSet<CPos> Taken = new();
			public readonly HashSet<CPos> Parked = new();
			public readonly List<(CPos Anchor, CPos Site)> Committed = new();
			public readonly List<CPos> PlacementFailed = new();
			public readonly List<int> ReservedVersions = new();
			public readonly List<int> ReservedUntilTicks = new();
			public IReadOnlyList<CPos> Covered = new CPos[0];
			public int Version = 1;
			public int Now;
			public int CoveredCalls;
			public int SetReleaseCalls;

			public CPos? ExpansionTarget => null;
			public bool WantsRefineryAtExpansionTarget => false;
			public int ExpansionTargetClaimRadius => 0;
			public CPos? RefineryClaimTarget => null;
			public int RefineryCoverageModelVersion => Version;

			public IReadOnlyList<CPos> RefineryClaimCoveredAnchors(CPos anchor)
			{
				CoveredCalls++;
				return Covered;
			}

			public bool TryReserveRefineryAnchors(CPos site, IReadOnlyCollection<CPos> anchors, object owner, int untilTick, int modelVersion = -1)
			{
				ReservedVersions.Add(modelVersion);
				ReservedUntilTicks.Add(untilTick);
				if (modelVersion >= 0 && modelVersion != Version)
					return false;
				return Reservations.TryReserveAll(anchors, owner, Now, untilTick, a => Taken.Contains(a) || Parked.Contains(a));
			}

			public void RefineryClaimCommitted(CPos anchor, CPos site)
			{
				Committed.Add((anchor, site));
				Reservations.ClearAll(Covered);
			}

			public void RefineryClaimPlacementFailed(CPos anchor)
			{
				PlacementFailed.Add(anchor);
				Parked.Add(anchor);
			}

			public int ReleaseRefineryAnchors(object owner) => Reservations.ReleaseAllForOwner(owner);

			public int ReleaseRefineryAnchors(IReadOnlyCollection<CPos> anchors, object owner)
			{
				SetReleaseCalls++;
				return Reservations.ReleaseAll(anchors, owner);
			}
		}

		static RefineryAnchorClaim ClaimAt(CPos anchor) =>
			new(anchor, anchor + new CVec(3, 0), fieldId: 1, tier: 1, resourceCells: new[] { anchor });

		[Test]
		public void AdmissionConflictRefusesTheWholeSetAndLeavesNoHold()
		{
			// Queue-admission gate: a contested member refuses the whole reservation — the
			// caller treats false as "never queue or bind", so no partial holds and no claim.
			var law = new RecordingRefineryLaw { Covered = new[] { new CPos(1, 1), new CPos(2, 2), new CPos(3, 3) } };
			var owner = new object();
			var foreign = new object();
			law.Reservations.TryReserve(new CPos(2, 2), foreign, 0, 500, _ => false);

			Assert.That(BaseBuilderQueueEvalCA.RefineryReservationAdmits(
				law, ClaimAt(new CPos(1, 1)), owner, untilTick: 100), Is.False);
			Assert.That(law.Reservations.LiveSetFor(law.Covered, owner, 10), Is.False);
			Assert.That(law.Reservations.LiveAt(new CPos(1, 1), 10), Is.False); // no partial hold
			Assert.That(law.Reservations.LiveAt(new CPos(3, 3), 10), Is.False);
		}

		[Test]
		public void NullClaimRefusesAdmissionBeforeAnyReserveCall()
		{
			var law = new RecordingRefineryLaw();
			Assert.That(BaseBuilderQueueEvalCA.RefineryReservationAdmits(law, null, new object(), 100), Is.False);
			Assert.That(law.ReservedVersions, Is.Empty);
			Assert.That(law.CoveredCalls, Is.EqualTo(0));
		}

		[Test]
		public void AdmissionBindsTheWholeCoveredSetAtomically()
		{
			// Successful admission reserves every covered member under the live model version —
			// the set the provider computed, owner-keyed to the demand, TTL'd to the demand's
			// idle window.
			var covered = new CPos[] { new(1, 1), new(2, 2), new(3, 3) };
			var law = new RecordingRefineryLaw { Covered = covered, Version = 7, Now = 20 };
			var demand = new ExpansionDemand(null);

			Assert.That(BaseBuilderQueueEvalCA.RefineryReservationAdmits(
				law, ClaimAt(new CPos(1, 1)), demand, untilTick: 300), Is.True);
			Assert.That(law.Reservations.LiveSetFor(covered, demand, 21), Is.True);
			Assert.That(law.ReservedVersions, Is.EqualTo(new[] { 7 }));   // live version forwarded
			Assert.That(law.ReservedUntilTicks, Is.EqualTo(new[] { 300 }));
			Assert.That(law.CoveredCalls, Is.EqualTo(1));                 // one set, computed once
		}

		[Test]
		public void CommittedPlacementPublishesTheSelectedSiteAndClearsHolds()
		{
			// The production commit call passes the ACTUAL selected placement — the anchor is
			// the claim; the site is wherever the footprint legally landed. The provider
			// publishes the whole-set pending coverage and clears the demand's reservations.
			var covered = new CPos[] { new(1, 1), new(2, 2), new(3, 3) };
			var law = new RecordingRefineryLaw { Covered = covered };
			var owner = new object();
			var claim = ClaimAt(new CPos(1, 1));
			law.Reservations.TryReserveAll(covered, owner, 0, 100, _ => false);
			var selected = new CPos(4, 5); // the legal site — deliberately not the anchor

			Assert.That(BaseBuilderQueueEvalCA.CommitRefineryClaimOrPark(law, claim, selected), Is.True);
			Assert.That(law.Committed, Is.EqualTo(new[] { (new CPos(1, 1), selected) }));
			Assert.That(law.PlacementFailed, Is.Empty);
			Assert.That(law.Reservations.Count, Is.EqualTo(0)); // commit consumed the holds
		}

		[Test]
		public void FailedPlacementParksTheOfferedAnchorAndCommitsNothing()
		{
			// No legal site for the offered claim — the manager signals placement failure so
			// the provider bounds the retry (parked = taken here), and no commit fires.
			var law = new RecordingRefineryLaw();
			var claim = ClaimAt(new CPos(6, 6));

			Assert.That(BaseBuilderQueueEvalCA.CommitRefineryClaimOrPark(law, claim, null), Is.False);
			Assert.That(law.PlacementFailed, Is.EqualTo(new[] { new CPos(6, 6) }));
			Assert.That(law.Committed, Is.Empty);

			// The parked anchor now refuses fresh reservation — it is off the market for the
			// bounded cooldown instead of looping an immediate re-offer.
			Assert.That(law.TryReserveRefineryAnchors(new CPos(6, 6), new[] { new CPos(6, 6) },
				new object(), 100), Is.False);
		}

		[Test]
		public void TeardownReleasesByOwnerKeyWithoutRecomputingTheSet()
		{
			// Both production teardown paths (ExpireExpansionDemand lapse and the
			// VerifyDemandBinding unwind) share this seam. The admission-time set has drifted:
			// the provider's recomputation would now answer a different, smaller set — a
			// set-keyed release would leak (3,3). Owner-keyed release frees all three holds
			// and never consults the covered set or the set-overload release.
			var coveredAtAdmission = new CPos[] { new(1, 1), new(2, 2), new(3, 3) };
			var law = new RecordingRefineryLaw { Covered = coveredAtAdmission };
			var owner = new object();
			var other = new object();
			law.Reservations.TryReserveAll(coveredAtAdmission, owner, 0, 100, _ => false);
			law.Reservations.TryReserve(new CPos(9, 9), other, 0, 100, _ => false);
			law.Covered = new[] { new CPos(1, 1) }; // the model drifted — (3,3) no longer covered

			Assert.That(BaseBuilderQueueEvalCA.ReleaseRefineryReservations(law, owner), Is.EqualTo(3));
			Assert.That(law.Reservations.LiveAt(new CPos(3, 3), 10), Is.False); // no leaked member
			Assert.That(law.Reservations.LiveAt(new CPos(9, 9), 10), Is.True);  // foreign hold kept
			Assert.That(law.CoveredCalls, Is.EqualTo(0));   // zero set computation at teardown
			Assert.That(law.SetReleaseCalls, Is.EqualTo(0)); // and never the set-keyed overload

			// The freed anchors are immediately claimable again — the lapse path depends on
			// this so a still-Ready refinery can re-adopt the very same anchor.
			Assert.That(law.TryReserveRefineryAnchors(new CPos(2, 2), new[] { new CPos(2, 2) },
				other, 200), Is.True);
		}

		[Test]
		public void RenewalRefreshesUnderTheLiveModelAndRefusalClearsTheClaim()
		{
			// Sweep-time renewal reserves the CURRENT covered set under the provider's live
			// version — the demand keeps its claim; a contested renewal clears it so the
			// placement falls back to a fresh claim (the law's one-refinery rule stands).
			var covered = new CPos[] { new(1, 1), new(2, 2) };
			var law = new RecordingRefineryLaw { Covered = covered, Version = 4 };
			var demand = new ExpansionDemand(null) { ReservedClaim = ClaimAt(new CPos(1, 1)), RefineryItem = "proc" };

			BaseBuilderQueueEvalCA.RenewRefineryReservation(law, demand, untilTick: 500);
			Assert.That(demand.ReservedClaim, Is.Not.Null);
			Assert.That(law.Reservations.LiveSetFor(covered, demand, 10), Is.True);
			Assert.That(law.ReservedVersions, Is.EqualTo(new[] { 4 }));

			// A foreign owner contests a member between sweeps — the renewal refuses and the
			// stale claim is cleared (its surviving holds lapse at their own expiry).
			var foreign = new object();
			law.Reservations.TryReserve(new CPos(5, 5), foreign, 10, 900, _ => false);
			law.Covered = new[] { new CPos(1, 1), new CPos(5, 5) };
			BaseBuilderQueueEvalCA.RenewRefineryReservation(law, demand, untilTick: 800);
			Assert.That(demand.ReservedClaim, Is.Null);
		}

		[Test]
		public void RenewalDropsTheClaimWhenTheLawOrTheBindingIsGone()
		{
			var demand = new ExpansionDemand(null) { ReservedClaim = ClaimAt(new CPos(1, 1)), RefineryItem = "proc" };

			// Provider gone (law switched off / module missing) — the claim cannot stand.
			BaseBuilderQueueEvalCA.RenewRefineryReservation(null, demand, 100);
			Assert.That(demand.ReservedClaim, Is.Null);

			// The bound refinery item already unwound — a stale claim is dropped without
			// consulting the provider at all.
			var law = new RecordingRefineryLaw();
			var stale = new ExpansionDemand(null) { ReservedClaim = ClaimAt(new CPos(2, 2)) };
			BaseBuilderQueueEvalCA.RenewRefineryReservation(law, stale, 100);
			Assert.That(stale.ReservedClaim, Is.Null);
			Assert.That(law.ReservedVersions, Is.Empty);
		}

		[Test]
		public void StaleModelVersionRefusesAndTheFreshReadRetries()
		{
			// The provider refuses a version that no longer matches its coverage model —
			// the protocol the seams implement by forwarding the live version every call.
			var law = new RecordingRefineryLaw { Version = 5 };
			var owner = new object();
			Assert.That(law.TryReserveRefineryAnchors(new CPos(1, 1), new[] { new CPos(1, 1) },
				owner, 100, modelVersion: 4), Is.False, "a stashed version is refused");

			law.Covered = new[] { new CPos(1, 1) };
			Assert.That(BaseBuilderQueueEvalCA.RefineryReservationAdmits(
				law, ClaimAt(new CPos(1, 1)), owner, 100), Is.True);
			Assert.That(law.ReservedVersions, Is.EqualTo(new[] { 4, 5 }),
				"the seam re-reads the provider's version — never a cached one");
		}

		// REPLAY-HEALTH-LOGGER seam (ACK-2 / task 01a12023) — BotQueueEpisodeTracker dedupe,
		// episode ids, and per-instance Ready (Sol review 22861e442 F1/F3/F5).

		[Test]
		public void HeldEmitsOncePerEpisode()
		{
			var tracker = new BotQueueEpisodeTracker();
			var item = new object();
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True, "first sighting emits");
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.False, "steady hold must not re-emit per tick");
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.CrawlHold), Is.True, "a changed reason is a new episode");
		}

		[Test]
		public void HeldEpisodeEndsOnTerminalTransition()
		{
			var tracker = new BotQueueEpisodeTracker();
			var item = new object();
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True);
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Placed, BotQueueTransitionReason.None), Is.True);
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True,
				"the same hold after a terminal transition is a new episode");
		}

		[Test]
		public void ReadyEmitsOncePerItemInstance()
		{
			var tracker = new BotQueueEpisodeTracker();
			var item = new object();
			tracker.BeginEpisode(item);
			Assert.That(tracker.AnnounceReady(item), Is.True, "first Done observation emits the ready timestamp");
			Assert.That(tracker.AnnounceReady(item), Is.False, "the item stays Done until it leaves — no per-tick re-emit");
			Assert.That(tracker.AnnounceReady(item), Is.False, "even an in-flight cancel leaves it announced — no phantom Ready after Cancelled");

			tracker.EndEpisode(item);
			var requeued = new object();
			tracker.BeginEpisode(requeued);
			Assert.That(tracker.AnnounceReady(requeued), Is.True, "a re-queued same-name item is a new instance and announces fresh");
		}

		[Test]
		public void EpisodeIdsArePerInstance()
		{
			var tracker = new BotQueueEpisodeTracker();
			var first = new object();
			var ep1 = tracker.BeginEpisode(first);
			Assert.That(tracker.EpisodeOf(first), Is.EqualTo(ep1));

			tracker.EndEpisode(first);
			var second = new object();
			var ep2 = tracker.BeginEpisode(second);
			Assert.That(ep2, Is.Not.EqualTo(ep1), "re-queued same-name items must be distinguishable (F1)");
			Assert.That(tracker.EpisodeOf(first), Is.EqualTo(0u), "a departed item resolves to UNKNOWN");
			Assert.That(tracker.EpisodeOf(null), Is.EqualTo(0u), "an unbound emit resolves to UNKNOWN");
		}

		[Test]
		public void EpisodesArePerProducer()
		{
			var tracker = new BotQueueEpisodeTracker();
			var item3 = new object();
			var item4 = new object();
			tracker.EmitGate(3, item3, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold);
			Assert.That(tracker.EmitGate(4, item4, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True, "a different producer is its own episode");
			Assert.That(tracker.AnnounceReady(item3), Is.True);
			Assert.That(tracker.AnnounceReady(item4), Is.True, "ready dedupe never bleeds across producers");
		}

		[Test]
		public void ReadyCannotInterleaveWithHeld()
		{
			// Sol review: a stable held item sweeping Ready->Held->Ready->Held would churn both
			// records forever under last-tuple dedupe. Ready is once per item instance, so the
			// interleaved Held emits can never resurrect it.
			var tracker = new BotQueueEpisodeTracker();
			var item = new object();
			tracker.BeginEpisode(item);
			Assert.That(tracker.AnnounceReady(item), Is.True);
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True);
			Assert.That(tracker.AnnounceReady(item), Is.False, "Held must not resurrect an announced Ready");
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.False);
			Assert.That(tracker.AnnounceReady(item), Is.False, "still suppressed on every later sweep");
		}

		[Test]
		public void HeldReasonAlternationIsATransition()
		{
			// A real oscillation (hold reason changing between sweeps) is a transition and must
			// emit — dedupe only suppresses the identical consecutive tuple.
			var tracker = new BotQueueEpisodeTracker();
			var item = new object();
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True);
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.CrawlHold), Is.True);
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True, "back to the first reason is a real change, not noise");
			Assert.That(tracker.EmitGate(7, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.False, "then the repeat dedupes");
		}

		[Test]
		public void PendingTerminalExpiresRejectedRequests()
		{
			// Requests and confirmed lifecycle stay separate: a pending older than the bound
			// whose item is still queued is a rejected/lost order, dropped before reconcile.
			var pending = new BotQueuePendingTerminal { IssuedTick = 100 };
			Assert.That(pending.Stale(108), Is.False, "inside the bound the request may still resolve");
			Assert.That(pending.Stale(109), Is.True, "past MaxInFlightTicks it never resolved");
		}

		[Test]
		public void PendingMatchesBoundInstanceNotName()
		{
			// Sol review: CancelProduction resolves the LAST same-name item, so a request bound
			// to an instance must never resolve against a different same-name removal — that
			// would attribute another item's fate to this request.
			var first = new object();
			var second = new object();
			var bound = new BotQueuePendingTerminal { Item = second, ItemName = "proc" };
			Assert.That(bound.Matches(second, "proc"), Is.True, "its own instance resolves the request");
			Assert.That(bound.Matches(first, "proc"), Is.False, "a same-name sibling leaving first must not consume it");

			var nameBound = new BotQueuePendingTerminal { ItemName = "proc" };
			Assert.That(nameBound.Matches(first, "proc"), Is.True, "a name-only request resolves the next same-name removal");
			Assert.That(nameBound.Matches(first, "warfactory"), Is.False, "different names never match");
		}

		[Test]
		public void DroppedProducerLosesDedupeHistory()
		{
			var tracker = new BotQueueEpisodeTracker();
			var item = new object();
			Assert.That(tracker.EmitGate(5, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True);
			Assert.That(tracker.EmitGate(5, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.False);
			tracker.DropProducer(5);
			Assert.That(tracker.EmitGate(5, item, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True,
				"a pruned producer's next transition is fresh — and its history is gone");
		}

		[Test]
		public void TransitionRecordCarriesSchema2Fields()
		{
			var t = new BotQueueTransition(1234, "proc", "Building", BuildingType.Refinery, 42, 7,
				BotQueueTransitionKind.Cancelled, BotQueueTransitionReason.NoRefinerySite,
				true, null, BotQueueCancellationClass.Production);

			Assert.That(t.Tick, Is.EqualTo(1234));
			Assert.That(t.ItemId, Is.EqualTo("proc"));
			Assert.That(t.Queue, Is.EqualTo("Building"));
			Assert.That(t.Category, Is.EqualTo(BuildingType.Refinery));
			Assert.That(t.ProducerActorId, Is.EqualTo(42u));
			Assert.That(t.EpisodeId, Is.EqualTo(7u));
			Assert.That(t.Kind, Is.EqualTo(BotQueueTransitionKind.Cancelled));
			Assert.That(t.Reason, Is.EqualTo(BotQueueTransitionReason.NoRefinerySite));
			Assert.That(t.PlayerActive, Is.True);
			Assert.That(t.ProducerLive, Is.Null, "no producer bound -> UNKNOWN, not a guessed bool");
			Assert.That(t.CancellationClass, Is.EqualTo(BotQueueCancellationClass.Production));
		}

		// FIX-QUEUE-OBSERVER-TERMINAL-CAUSALITY (01a121b6): request intent and proven
		// outcome are separate records. Queue disappearance is correlation, never proof —
		// engine cleanup removes items too. These drive the world-free resolution map the
		// probe executes; every case below enumerates what the emit site can produce.

		[Test]
		public void PlacementRemovalWithoutProofArmsProofWindow()
		{
			// VP consumer-fit: a pending placement followed by the item leaving a live
			// queue within the window is correlation — a competing cleanup or rejected
			// order removes the item the same way. It must NOT emit Placed.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(pending.ResolveOnRemoval(101, queueSeen: true, placementProven: false),
				Is.EqualTo(BotQueuePendingResolution.KeepAwaitingProof),
				"unproven removal keeps the request armed — it never promotes to Placed on disappearance");
			Assert.That(pending.ResolveOnRemoval(108, queueSeen: true, placementProven: false),
				Is.EqualTo(BotQueuePendingResolution.KeepAwaitingProof), "still inside the bound, still not Placed");
		}

		[Test]
		public void StalePlacementRequestEndsRemovedAtRemoval()
		{
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(pending.ResolveOnRemoval(109, queueSeen: true, placementProven: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved),
				"a request older than the bound is a lost order — Removed, never the proof window");
		}

		[Test]
		public void ProvenPlacementEmitsPlacedAtRemoval()
		{
			// Normal placement: the actor lands inside the same frame-end task the item
			// leaves in — site proof already holds at the next probe tick.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(pending.ResolveOnRemoval(101, queueSeen: true, placementProven: true),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested), "actor on the ordered cell -> Placed");
			Assert.That(pending.ResolveOnRemoval(101, queueSeen: false, placementProven: true),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested),
				"lifecycle evidence beats queue staleness — the building exists");
		}

		[Test]
		public void BuilderUnitPlacementProvesOnLateActorArrival()
		{
			// Lead ruling: a builder-unit order removes the item immediately (EndProduction
			// before BuildOnSite runs), and the actor lands ticks later — Placed resolves
			// at actor-lifecycle proof inside the bounded window, never at queue removal.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(pending.ResolveOnRemoval(101, queueSeen: true, placementProven: false),
				Is.EqualTo(BotQueuePendingResolution.KeepAwaitingProof), "item gone, actor not yet landed");

			// The probe arms it and restarts the window at the removal tick.
			pending.AwaitingProof = true;
			pending.IssuedTick = 101;
			Assert.That(pending.ResolveWhileAwaiting(105, placementProven: true, playerEliminated: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested), "BuildOnSite landed the actor -> Placed");
		}

		[Test]
		public void BuilderUnitPlacementFailureExpiresToRemoved()
		{
			// BuildOnSite can fail after the item left (unit cancelled/dead, cell gone
			// invalid, CanQueue refused) — no actor ever lands: the armed window expires
			// to Removed carrying the intent reason, schema-2 UNKNOWN, never Placed.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(pending.ResolveOnRemoval(101, queueSeen: true, placementProven: false),
				Is.EqualTo(BotQueuePendingResolution.KeepAwaitingProof));

			pending.AwaitingProof = true;
			pending.IssuedTick = 101;
			Assert.That(pending.ResolveWhileAwaiting(104, placementProven: false, playerEliminated: false),
				Is.EqualTo(BotQueuePendingResolution.Unresolved), "inside the window — no record yet");
			Assert.That(pending.ResolveWhileAwaiting(110, placementProven: false, playerEliminated: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved),
				"window expired without the actor -> Removed + intent reason");
		}

		[Test]
		public void CompetingRemovalAfterPlacementNeverConfirms()
		{
			// The required cleanup/competing-removal regression, end to end: request issued,
			// the bound item is removed by something else (automatic unbuildable cleanup, a
			// second cancel, a dead-producer flush) — every unproven path ends Removed.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(pending.ResolveOnRemoval(101, queueSeen: true, placementProven: false),
				Is.Not.EqualTo(BotQueuePendingResolution.EmitRequested), "correlation is not Placed");

			// queueSeen=false path: a disabled queue flushing its contents.
			var stale = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(3, 4)
			};
			Assert.That(stale.ResolveOnRemoval(101, queueSeen: false, placementProven: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved));
		}

		[Test]
		public void CancelRemovalNeverConfirmsCancelled()
		{
			// VP: a pending cancel followed by ANY removal — ours, a sibling's, or
			// automatic cleanup — is indistinguishable on this engine pin; every case
			// resolves Removed carrying the intent reason (schema-2 non-pass), never
			// an invented Cancelled.
			foreach (var (seen, age) in new[] { (true, 1), (true, 8), (false, 1), (true, 9) })
			{
				var pending = new BotQueuePendingTerminal
				{
					Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Cancelled,
					Reason = BotQueueTransitionReason.DemandCancel, Class = BotQueueCancellationClass.Production,
					IssuedTick = 100
				};
				Assert.That(pending.ResolveOnRemoval(100 + age, seen, placementProven: false),
					Is.EqualTo(BotQueuePendingResolution.EmitRemoved),
					$"seen={seen} age={age}: disappearance never proves a cancel");
			}
		}

		[Test]
		public void InfiniteCancelProvenOnlyByBoundFlagFlip()
		{
			// CancelProductionInner's unique signature: Infinite flips false on the bound
			// item while it stays queued. Bound-item + flip together — a sibling or an
			// unrelated infinite item flipping cannot satisfy it.
			var bound = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Cancelled,
				Reason = BotQueueTransitionReason.DemandCancel, IssuedTick = 100, WasInfinite = true
			};
			Assert.That(bound.CancelProvenByFlagFlip(infiniteNow: false), Is.True, "the bound item flipped -> proven Cancelled");
			Assert.That(bound.CancelProvenByFlagFlip(infiniteNow: true), Is.False, "no flip -> not proven");

			var finite = new BotQueuePendingTerminal
			{
				Item = new object(), Kind = BotQueueTransitionKind.Cancelled, WasInfinite = false
			};
			Assert.That(finite.CancelProvenByFlagFlip(infiniteNow: false), Is.False,
				"a finite item has no flag transition — no proof exists");

			var nameOnly = new BotQueuePendingTerminal
			{
				ItemName = "proc", Kind = BotQueueTransitionKind.Cancelled, WasInfinite = true
			};
			Assert.That(nameOnly.CancelProvenByFlagFlip(infiniteNow: false), Is.False,
				"an unbound request cannot verify its item's flag — honest UNKNOWN");

			var notCancel = new BotQueuePendingTerminal
			{
				Item = new object(), Kind = BotQueueTransitionKind.Placed, WasInfinite = true
			};
			Assert.That(notCancel.CancelProvenByFlagFlip(infiniteNow: false), Is.False);
		}

		[Test]
		public void AwaitingProofEntryCannotBindSiblingRemoval()
		{
			// An armed entry's item already left; a later same-name sibling removal must
			// not re-consume it, and it can never re-resolve against its own departed ref.
			var first = new object();
			var second = new object();
			var pending = new BotQueuePendingTerminal
			{
				Item = first, ItemName = "proc", Kind = BotQueueTransitionKind.Placed, AwaitingProof = true
			};
			Assert.That(pending.Matches(second, "proc"), Is.False, "awaiting entries are unbound from queue removals");
			Assert.That(pending.Matches(first, "proc"), Is.False, "it resolves through the proof scan only");
		}

		[Test]
		public void EliminationDuringAwaitingResolvesRemoved()
		{
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, AwaitingProof = true
			};
			Assert.That(pending.ResolveWhileAwaiting(102, placementProven: false, playerEliminated: true),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved), "elimination closes the proof window early");
			Assert.That(pending.ResolveWhileAwaiting(102, placementProven: true, playerEliminated: true),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested),
				"a landed building still proves — elimination changes classification, not fact");
		}

		// VP re-review of ae259554d: !AcceptsPlug is not install proof — the engine's
		// AcceptsPlug is false for unknown types and failed dynamic Requirements too.
		// These drive the real predicate the manager wires from the Pluggable trait.

		[Test]
		public void PlugInstallProofRequiresPositiveSlotState()
		{
			// True install: the slot defines the type, it has no dynamic Requirements,
			// and it now refuses — for requirement-free types AcceptsPlug is exactly
			// `active == null`, so false here is the engine's own installed state.
			Assert.That(BaseBuilderQueueEvalCA.PlugInstallProven(
					slotDefinesType: true, requirementKeyed: false, acceptsNow: false),
				Is.True, "requirement-free slot refusing its own type = active plug installed");

			// Requirement flip: a requirement-keyed refusal is availability state —
			// no EnablePlug ran. Never proof.
			Assert.That(BaseBuilderQueueEvalCA.PlugInstallProven(true, requirementKeyed: true, acceptsNow: false),
				Is.False, "requirement-keyed refusal is a requirement state, never install proof");
			Assert.That(BaseBuilderQueueEvalCA.PlugInstallProven(true, true, acceptsNow: true),
				Is.False, "availability true means the slot would still accept — not installed");

			// Unknown type: the slot refuses by definition, not by installation.
			Assert.That(BaseBuilderQueueEvalCA.PlugInstallProven(slotDefinesType: false, false, false),
				Is.False, "a slot that never defined the type cannot 'refuse' it");

			// Still accepting: no install.
			Assert.That(BaseBuilderQueueEvalCA.PlugInstallProven(true, false, acceptsNow: true),
				Is.False, "an empty accepting slot is not installed");
		}

		[Test]
		public void RequirementFlipPlugPendingExpiresToRemoved()
		{
			// The VP scenario end to end through the production predicate: a plug order
			// issued, then the host's dynamic Requirement fails before PlacePlug resolves —
			// no EnablePlug runs, the slot reports refusal. With the corrected predicate
			// that is NOT proof, so the armed pending expires to Removed + intent reason.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "plug", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(5, 6), PlugType = "adv", AwaitingProof = true
			};

			var requirementFlipped = BaseBuilderQueueEvalCA.PlugInstallProven(
				slotDefinesType: true, requirementKeyed: true, acceptsNow: false);
			Assert.That(pending.ResolveWhileAwaiting(110, placementProven: requirementFlipped, playerEliminated: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved),
				"requirement flip -> no install -> expires Removed, never Placed");
		}

		[Test]
		public void TruePlugInstallProvesPlaced()
		{
			// Same pending, true install: requirement-free slot now refusing the type it
			// accepted at order-selection — the engine's install state -> Placed.
			var pending = new BotQueuePendingTerminal
			{
				Item = new object(), ItemName = "plug", Kind = BotQueueTransitionKind.Placed,
				IssuedTick = 100, Site = new CPos(5, 6), PlugType = "adv", AwaitingProof = true
			};

			var installed = BaseBuilderQueueEvalCA.PlugInstallProven(
				slotDefinesType: true, requirementKeyed: false, acceptsNow: false);
			Assert.That(pending.ResolveWhileAwaiting(104, placementProven: installed, playerEliminated: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested),
				"slot occupied by EnablePlug -> Placed");
		}
	}
}
