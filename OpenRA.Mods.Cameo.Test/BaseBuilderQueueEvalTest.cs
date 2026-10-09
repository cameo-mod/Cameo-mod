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

		// REPLAY-HEALTH-LOGGER seam (ACK-2 / task 01a12023) — BotQueueEpisodeTracker dedupe.

		[Test]
		public void HeldEmitsOncePerEpisode()
		{
			var tracker = new BotQueueEpisodeTracker();
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True, "first sighting emits");
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.False, "steady hold must not re-emit per tick");
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.CrawlHold), Is.True, "a changed reason is a new episode");
		}

		[Test]
		public void HeldEpisodeEndsOnTerminalTransition()
		{
			var tracker = new BotQueueEpisodeTracker();
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True);
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Placed, BotQueueTransitionReason.None), Is.True);
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True,
				"the same hold after a terminal transition is a new episode");
		}

		[Test]
		public void ReadyEmitsOncePerProductionEpisode()
		{
			var tracker = new BotQueueEpisodeTracker();
			Assert.That(tracker.EmitGate(9, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.True, "first Done observation emits the ready timestamp");
			Assert.That(tracker.EmitGate(9, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.False, "a held item stays Done — no re-emit");
			Assert.That(tracker.EmitGate(9, "proc", BotQueueTransitionKind.Placed, BotQueueTransitionReason.None), Is.True);
			Assert.That(tracker.EmitGate(9, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.True, "a re-queued item announces a fresh episode");
		}

		[Test]
		public void CancelledShadowsInFlightReadyUntilStarted()
		{
			var tracker = new BotQueueEpisodeTracker();

			// Done item cancelled — CancelProduction is an order, so the item can sit at the
			// queue head still Done for a tick or two. The sweep must not re-announce Ready.
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.True);
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Cancelled, BotQueueTransitionReason.NoRefinerySite), Is.True);
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.False,
				"phantom Ready after Cancelled would invert the pair in the log");
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Cancelled, BotQueueTransitionReason.NoRefinerySite), Is.False,
				"an in-flight cancel re-swept next tick must not re-emit");
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Started, BotQueueTransitionReason.None), Is.True);
			Assert.That(tracker.EmitGate(7, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.True,
				"a re-queued same-name item clears the shadow and announces fresh");
		}

		[Test]
		public void EpisodesArePerProducer()
		{
			var tracker = new BotQueueEpisodeTracker();
			tracker.EmitGate(3, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold);
			Assert.That(tracker.EmitGate(4, "proc", BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold), Is.True, "a different producer is its own episode");
			Assert.That(tracker.EmitGate(3, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.True);
			Assert.That(tracker.EmitGate(4, "proc", BotQueueTransitionKind.Ready, BotQueueTransitionReason.None), Is.True, "ready dedupe never bleeds across producers");
		}

		[Test]
		public void TransitionRecordCarriesSchema2Fields()
		{
			var t = new BotQueueTransition(1234, "proc", "Building", BuildingType.Refinery, 42,
				BotQueueTransitionKind.Cancelled, BotQueueTransitionReason.NoRefinerySite,
				true, null, BotQueueCancellationClass.Production);

			Assert.That(t.Tick, Is.EqualTo(1234));
			Assert.That(t.ItemId, Is.EqualTo("proc"));
			Assert.That(t.Queue, Is.EqualTo("Building"));
			Assert.That(t.Category, Is.EqualTo(BuildingType.Refinery));
			Assert.That(t.ProducerActorId, Is.EqualTo(42u));
			Assert.That(t.Kind, Is.EqualTo(BotQueueTransitionKind.Cancelled));
			Assert.That(t.Reason, Is.EqualTo(BotQueueTransitionReason.NoRefinerySite));
			Assert.That(t.PlayerActive, Is.True);
			Assert.That(t.ProducerLive, Is.Null, "no producer bound -> UNKNOWN, not a guessed bool");
			Assert.That(t.CancellationClass, Is.EqualTo(BotQueueCancellationClass.Production));
		}
	}
}
