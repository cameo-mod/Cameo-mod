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
	}
}
