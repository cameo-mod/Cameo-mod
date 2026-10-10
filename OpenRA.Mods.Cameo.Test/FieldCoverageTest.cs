#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// AI_ARCHITECTURE 12.24 FE-1 + REF-1 v2: refineries per anchor, field grouping, claim tiers, gap. Pure helpers only.
	[TestFixture]
	public sealed class FieldCoverageTest
	{
		static CPos C(int x, int y) => new(x, y);

		static IReadOnlyList<IReadOnlyCollection<CPos>> Fields(params CPos[][] fields) =>
			fields.Select(f => (IReadOnlyCollection<CPos>)f.ToList()).ToList();

		[Test]
		public void AnchorsAreSpreadersPlusSpreaderlessFieldCenters()
		{
			var anchors = ExpansionPlannerBotModule.BuildAnchors(
				new[] { C(10, 10), C(10, 10), C(50, 50) },
				new[] { C(12, 12), C(80, 80), C(52, 49) }, 12);

			// duplicate spreader dropped; the two fields near a spreader are represented by it; (80,80) has none
			Assert.That(anchors, Is.EqualTo(new[] { C(10, 10), C(50, 50), C(80, 80) }));
		}

		[Test]
		public void ThreeSpreadersOfOneFieldStayThreeAnchors()
		{
			// REF-1 v2 (correction A): spreaders are not merged — one refinery per spreader, never per field.
			var anchors = ExpansionPlannerBotModule.BuildAnchors(
				new[] { C(10, 10), C(14, 10), C(18, 10) },
				new[] { C(14, 12) }, 12);
			Assert.That(anchors, Is.EqualTo(new[] { C(10, 10), C(14, 10), C(18, 10) }));
		}

		[Test]
		public void OneRefineryServesEveryAnchorInRadius()
		{
			// REPAIR-B3 shared coverage (SPEC_2026-10-09 §2): one usable refinery serves EVERY anchor in
			// range — the retired 1:1 binding left the second spreader falsely unserved.
			var anchors = new[] { C(10, 10), C(14, 10) };
			var assigned = ExpansionPlannerBotModule.AssignRefineries(anchors, new[] { C(12, 10) }, 8);

			Assert.That(assigned, Is.EqualTo(new[] { 0, 0 }));
		}

		[Test]
		public void AssignRefineriesRadiusTenDiagonalBoundary()
		{
			// SPEC radius-10 boundary: (7,7) away is inside (d^2=98 <= 100); one cell past on both axes is out.
			var refinery = new[] { C(30, 10) };
			Assert.That(ExpansionPlannerBotModule.AssignRefineries(new[] { C(37, 17) }, refinery, 10),
				Is.EqualTo(new[] { 0 }));
			Assert.That(ExpansionPlannerBotModule.AssignRefineries(new[] { C(38, 18) }, refinery, 10),
				Is.EqualTo(new[] { -1 }));
		}

		[Test]
		public void RefineryOutsideServeRadiusServesNothing()
		{
			var assigned = ExpansionPlannerBotModule.AssignRefineries(new[] { C(10, 10) }, new[] { C(30, 10) }, 8);
			Assert.That(assigned, Is.EqualTo(new[] { -1 }));
		}

		[Test]
		public void ResourceFieldsGroupsEightConnectedComponents()
		{
			var cells = new[]
			{
				C(10, 10), C(11, 11), C(12, 12), // diagonally connected: one field
				C(50, 50), C(51, 50),
				C(30, 30), // alone
			};
			var fields = ExpansionPlannerBotModule.ResourceFields(cells);
			Assert.That(fields.Count, Is.EqualTo(3));
			Assert.That(fields[0], Is.EqualTo(new[] { C(10, 10), C(11, 11), C(12, 12) })); // smallest first cell first
			Assert.That(fields[1], Is.EqualTo(new[] { C(30, 30) }));
			Assert.That(fields[2], Is.EqualTo(new[] { C(50, 50), C(51, 50) }));

			// a one-cell gap splits the field
			var split = ExpansionPlannerBotModule.ResourceFields(new[] { C(10, 10), C(12, 10) });
			Assert.That(split.Count, Is.EqualTo(2));
		}

		[Test]
		public void AssignAnchorFieldsMapsSpreadersToNearestField()
		{
			var fieldCells = Fields(
				new[] { C(10, 10), C(11, 10), C(12, 10) },
				new[] { C(60, 60), C(61, 60) });
			var centers = new[] { C(11, 10), C(60, 60) };
			var anchors = new[] { C(10, 12), C(61, 60), C(30, 30), C(11, 10) };

			// spreaders 0/1 near fields 0/1; spreader 2 is an orphan (synthetic id 2); anchor 3 is field 0's own centre
			var ids = ExpansionPlannerBotModule.AssignAnchorFields(anchors, 3, fieldCells, centers, 12);
			Assert.That(ids, Is.EqualTo(new[] { 0, 1, 2, 0 }));
		}

		[Test]
		public void ClaimOrderServesAFreeFieldBeforeACoveredFieldsSecondSpreader()
		{
			// REF-1 v2 spec test: field A has three spreaders and one refinery already; field B has one spreader,
			// none — both in reach. B's spreader must be claimed next (tier 1), not A's second (tier 2).
			// REPAIR-B3: the extra spreaders sit beyond the serve radius so the existing refinery does NOT
			// cover them — in-radius siblings would be served outright and never reach the claim order.
			var anchors = new[] { C(20, 20), C(32, 20), C(36, 20), C(60, 20) };
			var fieldOf = new[] { 0, 0, 0, 1 };
			var fieldCells = Fields(
				new[] { C(19, 19), C(20, 20), C(21, 20), C(25, 20), C(29, 20) },
				new[] { C(59, 19), C(60, 20) });
			var buildings = new[] { C(10, 20) };

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new[] { C(21, 21) }, buildings, 8, 80, null, null, null,
				out var unservedAnchors, out var unservedFields, out var tiers, out _, out _);

			Assert.That(order[0], Is.EqualTo(3));
			Assert.That(tiers[3], Is.EqualTo(1));
			Assert.That(tiers[1], Is.EqualTo(2));
			Assert.That(tiers[2], Is.EqualTo(2));
			Assert.That(order.Skip(1), Is.EquivalentTo(new[] { 1, 2 }));
			Assert.That(unservedFields, Is.EqualTo(1));
			Assert.That(unservedAnchors, Is.EqualTo(3));
		}

		[Test]
		public void ClaimOrderTierTwoPicksTheSpreaderFarthestFromTheFieldsRefinery()
		{
			// Once the only other field is served, field A's second refinery goes to the spreader farthest from
			// A's first refinery (at 21,21 serving the (20,20) spreader). REPAIR-B3: the candidates sit beyond
			// serve radius — shared coverage would serve them outright inside it.
			var anchors = new[] { C(20, 20), C(32, 20), C(38, 20) };
			var fieldOf = new[] { 0, 0, 0 };
			var fieldCells = Fields(new[] { C(19, 19), C(20, 20), C(21, 20), C(25, 20), C(29, 20) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new[] { C(21, 21) }, new[] { C(10, 20) }, 8, 80, null, null, null,
				out var unservedAnchors, out var unservedFields, out var tiers, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
			Assert.That(tiers[2], Is.EqualTo(2));
			Assert.That(unservedFields, Is.EqualTo(0));
			Assert.That(unservedAnchors, Is.EqualTo(2));
		}

		[Test]
		public void ClaimOrderTierOnePicksTheSpreaderCoveringMostOfTheField()
		{
			// A line of four resource cells; the spreader on the line covers all 4, the off-line one none.
			var anchors = new[] { C(10, 15), C(12, 10) };
			var fieldOf = new[] { 0, 0 };
			var fieldCells = Fields(new[] { C(10, 10), C(11, 10), C(12, 10), C(13, 10) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new CPos[0], new[] { C(8, 10) }, 8, 80, null, null, null,
				out _, out _, out var tiers, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 1 }));
			Assert.That(tiers[1], Is.EqualTo(1));
		}

		[Test]
		public void ClaimOrderPendingCommitCoversItsField()
		{
			// A committed (produced, not yet placed) refinery marks its anchor's field covered: the other field's
			// first refinery still wins (tier 1), the pending field's second spreader falls to tier 2.
			var anchors = new[] { C(20, 20), C(24, 20), C(60, 20) };
			var fieldOf = new[] { 0, 0, 1 };
			var fieldCells = Fields(new[] { C(20, 19), C(25, 20) }, new[] { C(60, 19) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new CPos[0], new[] { C(10, 20) }, 8, 80, i => i == 0, i => i == 0, null,
				out var unservedAnchors, out var unservedFields, out var tiers, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 2, 1 }));
			Assert.That(tiers[2], Is.EqualTo(1));
			Assert.That(tiers[1], Is.EqualTo(2));
			Assert.That(unservedFields, Is.EqualTo(1)); // field 1 still waits; field 0 counts as covered
			Assert.That(unservedAnchors, Is.EqualTo(3)); // the pending anchor still counts in the honest metric
		}

		[Test]
		public void ClaimOrderNewAnchorInReachIsWantedImmediately()
		{
			// The home spreader is served; a forward spreader that just entered reach tops the order at once.
			var anchors = new[] { C(12, 10), C(50, 10) };
			var fieldOf = new[] { 0, 1 };
			var fieldCells = Fields(new[] { C(12, 9) }, new[] { C(50, 9) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new[] { C(13, 10) }, new[] { C(10, 10), C(45, 10) }, 8, 10, null, null, null,
				out var unservedAnchors, out var unservedFields, out _, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 1 }));
			Assert.That(unservedAnchors, Is.EqualTo(1));
			Assert.That(unservedFields, Is.EqualTo(1));
		}

		[Test]
		public void ClaimOrderStackedRefineriesDoNotBlockTheForwardAnchor()
		{
			// Three refineries stacked on the home spreader serve one anchor only; the forward anchor is still
			// wanted first — duplicates never eat a field's quota.
			var anchors = new[] { C(10, 10), C(50, 10) };
			var fieldOf = new[] { 0, 1 };
			var fieldCells = Fields(new[] { C(10, 9) }, new[] { C(50, 9) });
			var stack = new[] { C(9, 10), C(11, 10), C(12, 10) };

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				stack, new[] { C(5, 10), C(55, 10) }, 8, 10, null, null, null,
				out var unservedAnchors, out var unservedFields, out _, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 1 }));
			Assert.That(unservedAnchors, Is.EqualTo(1));
			Assert.That(unservedFields, Is.EqualTo(1));
		}

		[Test]
		public void ClaimOrderBlockedAnchorStaysOutOfCandidacyButCounts()
		{
			// A parked/pending anchor is blocked from claiming but still counts in the unserved metrics.
			var anchors = new[] { C(10, 10), C(18, 10) };
			var fieldOf = new[] { 0, 1 };
			var fieldCells = Fields(new[] { C(10, 9) }, new[] { C(18, 9) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new CPos[0], new[] { C(12, 10) }, 8, 10, i => i == 0, null, null,
				out var unservedAnchors, out var unservedFields, out var tiers, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 1 }));
			Assert.That(tiers[0], Is.EqualTo(0));
			Assert.That(unservedAnchors, Is.EqualTo(2));
			Assert.That(unservedFields, Is.EqualTo(2));
		}

		[Test]
		public void ClaimOrderNearRanksWithinEachTier()
		{
			// An MCV-requested refinery ranks its yard's nearest anchors first inside each tier: the far field's
			// rep beats the nearer one for the requesting yard, tier order untouched.
			var anchors = new[] { C(20, 20), C(60, 20) };
			var fieldOf = new[] { 0, 1 };
			var fieldCells = Fields(new[] { C(20, 19) }, new[] { C(60, 19) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new CPos[0], new[] { C(10, 20) }, 8, 80, null, null, C(58, 22),
				out _, out _, out _, out _, out _);

			Assert.That(order[0], Is.EqualTo(1));
		}

		[Test]
		public void ResourceGapCountsEmptyCellsBetweenFootprintAndField()
		{
			var footprint = new[] { C(20, 20) };
			Assert.That(ExpansionMath.ResourceGap(footprint, new[] { C(21, 21) }), Is.EqualTo(0)); // on/adjacent
			Assert.That(ExpansionMath.ResourceGap(footprint, new[] { C(22, 20) }), Is.EqualTo(1)); // one empty cell — the law's maximum
			Assert.That(ExpansionMath.ResourceGap(footprint, new[] { C(23, 20) }), Is.EqualTo(2)); // the placement must reject this
			Assert.That(ExpansionMath.ResourceGap(footprint, new CPos[0]), Is.EqualTo(-1)); // a lone spreader: no known cells
			Assert.That(ExpansionMath.ResourceGap(new CPos[0], new[] { C(21, 21) }), Is.EqualTo(-1));
		}

		[Test]
		public void ParkedAnchorIsSkippedButStillCountedUnserved()
		{
			// The module-side blocked predicate covers this; kept as the ClaimOrder-level pin.
			var anchors = new[] { C(10, 10), C(18, 10) };
			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, new[] { 0, 1 },
				Fields(new[] { C(10, 9) }, new[] { C(18, 9) }),
				new CPos[0], new[] { C(12, 10) }, 8, 10, i => i == 0, null, null,
				out var unserved, out _, out _, out _, out _);
			Assert.That(order, Is.EqualTo(new[] { 1 }));
			Assert.That(unserved, Is.EqualTo(2));
		}

		[Test]
		public void ClaimOrderFieldEdgeInReachClaimsTheAnchor()
		{
			// The spreader sits 8 cells from our frontier (beyond ReachCells) but its field's edge is only 3
			// away: the law's refinery goes flush to the FIELD, so the anchor is claimable. Under the old
			// anchor-to-building-top-left measure this anchor starved (the smoke match's losing side).
			var anchors = new[] { C(52, 20) };
			var fieldOf = new[] { 0 };
			var fieldCells = Fields(new[] { C(47, 20), C(48, 20), C(52, 20) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new CPos[0], new[] { C(44, 20) }, 8, 6, null, null, null,
				out var unservedAnchors, out var unservedFields, out _, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 0 }));
			Assert.That(unservedAnchors, Is.EqualTo(1));
			Assert.That(unservedFields, Is.EqualTo(1));
		}

		[Test]
		public void ClaimOrderOrphanAnchorKeepsItsOwnCellAsTheReachMeasure()
		{
			// A spreader with no recorded field cells (synthetic id 1 beyond the single real field) measures
			// reach from its own cell — 8 away is out of reach even though a field exists nearer. A refinery
			// already exists, so the first-refinery fallback does not fire and reach alone decides.
			var anchors = new[] { C(52, 20) };
			var fieldCells = Fields(new[] { C(47, 20), C(48, 20) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, new[] { 1 }, fieldCells,
				new[] { C(40, 20) }, new[] { C(44, 20) }, 8, 6, null, null, null,
				out var unservedAnchors, out var unservedFields, out _, out _, out _);

			Assert.That(order, Is.Empty);
			Assert.That(unservedAnchors, Is.EqualTo(0));
			Assert.That(unservedFields, Is.EqualTo(0));
		}

		[Test]
		public void ClaimOrderFirstRefineryIgnoresTheReachTest()
		{
			// The first refinery is never hostage to the reach test: with nothing in reach and no refinery
			// anywhere, the unblocked unserved anchor nearest our buildings is claimed anyway — a far home
			// field cannot starve the law (the smoke match's losing seat built zero refineries all game).
			var anchors = new[] { C(52, 20), C(60, 20) };
			var fieldCells = Fields(new[] { C(53, 20) }, new[] { C(61, 20) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, new[] { 0, 1 }, fieldCells,
				new CPos[0], new[] { C(44, 20) }, 8, 6, null, null, null,
				out _, out _, out var tiers, out _, out _);

			Assert.That(order, Is.EqualTo(new[] { 0 }));
			Assert.That(tiers[0], Is.EqualTo(1));
		}

		[Test]
		public void ClaimOrderCommittedAnchorSuppressesTheFirstRefineryFallback()
		{
			// One refinery already claimed and in production: the fallback must not queue a second.
			var anchors = new[] { C(52, 20), C(60, 20) };

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, new[] { 0, 1 },
				Fields(new[] { C(53, 20) }, new[] { C(61, 20) }),
				new CPos[0], new[] { C(44, 20) }, 8, 6, i => i == 0, i => i == 0, null,
				out _, out _, out _, out _, out _);

			Assert.That(order, Is.Empty);
		}

		[Test]
		public void RefineryFlushBeyondServeRadiusBindsNothing()
		{
			// R1 (SPEC §30 "a far same-field anchor does not inherit coverage"): a refinery flush to the
			// field's far edge but beyond the serve radius serves NOTHING — the retired flush exception
			// used to bind the whole field's anchors to it.
			var anchors = new[] { C(10, 10), C(14, 10) };
			var assigned = ExpansionPlannerBotModule.AssignRefineries(anchors, new[] { C(30, 10) }, 8);

			Assert.That(assigned, Is.EqualTo(new[] { -1, -1 }));
		}

		[Test]
		public void RefineryFlushFieldsFindsTheFieldWithinTheGapBound()
		{
			var fields = Fields(new[] { C(20, 20), C(21, 20) }, new[] { C(60, 20) });
			var tiles = new IReadOnlyCollection<CPos>[] { new List<CPos> { C(23, 20), C(23, 21) }, new List<CPos> { C(40, 40) } };
			var flush = ExpansionPlannerBotModule.RefineryFlushFields(tiles, fields);

			// tile (23,20) is Chebyshev 2 from field cell (21,20) — the law's gap-1 bound; the far refinery: none
			Assert.That(flush, Is.EqualTo(new[] { 0, -1 }));
		}

		[Test]
		public void SiteInTheCrawlDirectionIsPenalised()
		{
			var origin = C(50, 50);
			var crawl = new[] { C(70, 50) };

			// same bearing (east) -> min factor; a perpendicular site keeps 1
			Assert.That(ExpansionPlannerBotModule.SeparationFactor(C(90, 54), origin, crawl, 35, 0.25, 6), Is.EqualTo(0.25));
			Assert.That(ExpansionPlannerBotModule.SeparationFactor(C(50, 10), origin, crawl, 35, 0.25, 6), Is.EqualTo(1.0));
		}

		[Test]
		public void SeparationUsesYardsAndInflightSitesAndWrapsAround()
		{
			var origin = C(50, 50);

			// bearings straddling the 0/360 seam are still close
			Assert.That(ExpansionPlannerBotModule.SeparationFactor(C(90, 46), origin, new[] { C(90, 54) }, 35, 0.25, 6), Is.EqualTo(0.25));

			// a yard at the base itself has no bearing and never penalises
			Assert.That(ExpansionPlannerBotModule.SeparationFactor(C(90, 50), origin, new[] { C(52, 50) }, 35, 0.25, 6), Is.EqualTo(1.0));
		}

		[Test]
		public void BearingDeltaIsTheShorterArc()
		{
			Assert.That(ExpansionPlannerBotModule.BearingDelta(new WAngle(10), new WAngle(1014)), Is.EqualTo(20));
			Assert.That(ExpansionPlannerBotModule.BearingDelta(new WAngle(0), new WAngle(512)), Is.EqualTo(512));
		}

		[Test]
		public void SpreadFactorGrowsWithDistanceFromOwnBuildings()
		{
			Assert.That(ExpansionPlannerBotModule.SpreadFactor(0, 100, 1.0), Is.EqualTo(1.0));
			Assert.That(ExpansionPlannerBotModule.SpreadFactor(50, 100, 1.0), Is.EqualTo(1.5).Within(1e-9));
			Assert.That(ExpansionPlannerBotModule.SpreadFactor(50, 100, 0.0), Is.EqualTo(1.0));
		}

		[Test]
		public void McvSiteAvoidsTheCrawlDirectionWhenWeighted()
		{
			var origin = C(50, 50);
			var crawl = new[] { C(70, 50) };
			var east = new ExpansionPlannerBotModule.FieldScore(1, C(80, 50), 100, 5, 0, 0, 1);
			var north = new ExpansionPlannerBotModule.FieldScore(2, C(50, 0), 100, 5, 0, 0, 1);
			var fields = new[] { east, north };

			// east is nearer the MCV, so unweighted it wins; weighted by separation the north site wins
			Assert.That(ExpansionPlannerBotModule.McvSite(fields, origin, 3, 10)?.Index, Is.EqualTo(1));
			var weighted = ExpansionPlannerBotModule.McvSite(fields, origin, 3, 10, null,
				f => ExpansionPlannerBotModule.SeparationFactor(f.Center, origin, crawl, 35, 0.25, 6));
			Assert.That(weighted?.Index, Is.EqualTo(2));
		}

		[Test]
		public void FieldTakenByAnMcvSiteIsDetected()
		{
			Assert.That(ExpansionPlannerBotModule.TakenByMcvSite(C(100, 50), new[] { C(98, 50) }, 8), Is.True);
			Assert.That(ExpansionPlannerBotModule.TakenByMcvSite(C(100, 50), new[] { C(50, 5) }, 8), Is.False);
			Assert.That(ExpansionPlannerBotModule.TakenByMcvSite(C(100, 50), Enumerable.Empty<CPos>(), 8), Is.False);
		}

		[Test]
		public void LinkBuildingWantedOnlyWhileTargetOutOfReachAndNothingClaimable()
		{
			// REF-1 B1: a far target + zero claimable anchors in reach -> the crawl wants its supply building.
			Assert.That(ExpansionPlannerBotModule.LinkBuildingWanted(true, 3, 0, true), Is.True);

			// ...and only then: drive off, no target, target already in reach, an anchor claimable, or no link
			// buildable each silence it.
			Assert.That(ExpansionPlannerBotModule.LinkBuildingWanted(false, 3, 0, true), Is.False);
			Assert.That(ExpansionPlannerBotModule.LinkBuildingWanted(true, null, 0, true), Is.False);
			Assert.That(ExpansionPlannerBotModule.LinkBuildingWanted(true, 0, 0, true), Is.False);
			Assert.That(ExpansionPlannerBotModule.LinkBuildingWanted(true, 3, 1, true), Is.False);
			Assert.That(ExpansionPlannerBotModule.LinkBuildingWanted(true, 3, 0, false), Is.False);
		}

		[Test]
		public void ClaimOrderCountsClaimableInReachAndUnservedBeyond()
		{
			// B1's gate metric and B4's nudge metric: field A in reach and unserved is claimable; field B's
			// anchor sits beyond reach and unserved.
			var anchors = new[] { C(10, 10), C(60, 10) };
			var fieldOf = new[] { 0, 1 };
			var fieldCells = Fields(new[] { C(11, 10) }, new[] { C(61, 10) });

			ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new CPos[0], new[] { C(10, 12) }, 8, 6, null, null, null,
				out _, out _, out _, out var claimable, out var beyond);

			Assert.That(claimable, Is.EqualTo(1));
			Assert.That(beyond, Is.EqualTo(1));

			// A refinery serving A: nothing claimable remains, B stays unserved beyond reach.
			ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new[] { C(11, 10) }, new[] { C(10, 12) }, 8, 6, null, null, null,
				out _, out _, out _, out var claimableServed, out var beyondServed);

			Assert.That(claimableServed, Is.EqualTo(0));
			Assert.That(beyondServed, Is.EqualTo(1));
		}

		[Test]
		public void McvDueRidesUnderTheReserve()
		{
			// REF-1 B2: the request is free — no cash term at all; a far free field and pipeline room decide.
			Assert.That(ExpansionPlannerBotModule.McvDue(true, 1, 3), Is.True);
			Assert.That(ExpansionPlannerBotModule.McvDue(false, 1, 3), Is.False);
			Assert.That(ExpansionPlannerBotModule.McvDue(true, 3, 3), Is.False);
		}

		[Test]
		public void MissingPrerequisiteTokensSkipsMetAndForbiddenEntries()
		{
			var met = new HashSet<string> { "a" };
			var missing = ExpansionPlannerBotModule.MissingPrerequisiteTokens(
				new[] { "~b", "a", "!c", "d" }, raw => met.Contains(raw.Replace("~", string.Empty)));

			// "~b" unmet but fixable; "a" met; "!c" is satisfied by absence (nothing builds it away); "d" missing.
			Assert.That(missing.ToList(), Is.EqualTo(new[] { "b", "d" }));
		}

		[Test]
		public void RefineryEstimateFallsBackInsteadOfGoingSilent()
		{
			// REF-1 B3: a transiently unbuildable refinery keeps the remembered estimate, then the rules-listed
			// one — only a mod with no refinery at all yields none.
			var live = (new ActorInfo("refinery.live"), 800, 100);
			var remembered = (new ActorInfo("refinery.remembered"), 900, 110);
			var rules = (new ActorInfo("refinery.rules"), 700, 90);
			var none = ((ActorInfo)null, 0, 0);

			Assert.That(ExpansionPlannerBotModule.RefineryEstimateOrFallback(live, remembered, () => rules).Info.Name,
				Is.EqualTo("refinery.live"));
			Assert.That(ExpansionPlannerBotModule.RefineryEstimateOrFallback(none, remembered, () => rules).Info.Name,
				Is.EqualTo("refinery.remembered"));
			Assert.That(ExpansionPlannerBotModule.RefineryEstimateOrFallback(none, none, () => rules).Info.Name,
				Is.EqualTo("refinery.rules"));
			Assert.That(ExpansionPlannerBotModule.RefineryEstimateOrFallback(none, none, () => none).Info, Is.Null);
		}

		[Test]
		public void RefineryLawNudgeReplacesTheRefineryCount()
		{
			// Classic / switch-off (no provider): the old refinery total decides verbatim.
			Assert.That(RefineryLawNudge.Due(null, 3, 3), Is.True);
			Assert.That(RefineryLawNudge.Due(null, 2, 3), Is.False);

			// REF-1 B4 under the law: coverage decides — all anchors in reach served AND unserved anchors beyond
			// reach. The raw refinery count is irrelevant in both directions.
			Assert.That(RefineryLawNudge.Due(new LawStub(0, 2), 0, 3), Is.True);
			Assert.That(RefineryLawNudge.Due(new LawStub(1, 2), 5, 3), Is.False);
			Assert.That(RefineryLawNudge.Due(new LawStub(0, 0), 5, 3), Is.False);
		}

		static PowerInfo Power(int amount)
		{
			var info = new PowerInfo();
			typeof(PowerInfo).GetField("Amount").SetValue(info, amount);
			return info;
		}

		[Test]
		public void CrawlLinkRequiresBuildableAreaAndPrefersPowerPlants()
		{
			// REF-1 B1 (maintainer correction): a link must extend the buildable area — a silo placed forward
			// closes no gap — and power plants are preferred because they feed defences too. Cost never excludes
			// a power plant: the advanced plant is a valid link when it is the only one.
			var silo = new ActorInfo("silo", new BuildableInfo(), new BuildingInfo());
			var barracks = new ActorInfo("rax", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo());
			var nuke = new ActorInfo("nuke", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo(),
				Power(100));
			var nuk2 = new ActorInfo("nuk2", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo(),
				Power(250));
			var refinery = new ActorInfo("ref", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo(),
				new RefineryInfo());

			Assert.That(ExpansionPlannerBotModule.IsCrawlLink(silo), Is.False);
			Assert.That(ExpansionPlannerBotModule.IsCrawlLink(barracks), Is.True);
			Assert.That(ExpansionPlannerBotModule.IsCrawlLink(nuke), Is.True);
			Assert.That(ExpansionPlannerBotModule.IsCrawlLink(refinery), Is.False);
			Assert.That(ExpansionPlannerBotModule.IsPowerPlant(nuk2), Is.True);
			Assert.That(ExpansionPlannerBotModule.IsPowerPlant(barracks), Is.False);

			var none = ((ActorInfo)null, 0, 0);

			// A power plant wins over a cheaper non-power link; the expensive advanced plant is still picked
			// when it is the only power plant.
			Assert.That(ExpansionPlannerBotModule.PickCrawlLink((nuk2, 1000, 200), (barracks, 100, 5)).Info.Name,
				Is.EqualTo("nuk2"));
			Assert.That(ExpansionPlannerBotModule.PickCrawlLink(none, (barracks, 100, 5)).Info.Name,
				Is.EqualTo("rax"));
			Assert.That(ExpansionPlannerBotModule.PickCrawlLink(none, none).Info, Is.Null);
		}

		[Test]
		public void MultiInstanceTraitInfosDoNotCrashThePlanner()
		{
			// Packs model conditional power as second instances (Power@upgraded: ~190 actors carry
			// two or more Power lines). TraitInfoOrDefault throws on multi-instance TypeDictionary
			// entries; the planner's buildable-item scans must tolerate them — observed live crash
			// on "A Nuclear Winter" (EMBER flag, 2026-10-04).
			var variantPlant = new ActorInfo("pp", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo(),
				Power(100), Power(40));
			Assert.DoesNotThrow(() => ExpansionPlannerBotModule.IsPowerPlant(variantPlant));
			Assert.That(ExpansionPlannerBotModule.IsPowerPlant(variantPlant), Is.True);
			Assert.That(ExpansionPlannerBotModule.IsCrawlLink(variantPlant), Is.True);

			// All-negative variant (a sink, not a plant) and single-instance still behave.
			var sink = new ActorInfo("sink", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo(),
				Power(-50), Power(-20));
			Assert.That(ExpansionPlannerBotModule.IsPowerPlant(sink), Is.False);
		}

		[Test]
		public void SiloOverrideThrottledUnderTheLaw()
		{
			// Law on: 85% capacity wants nothing, 96% wants a silo, 96% with one already in
			// production still wants nothing (the queue spends on production instead).
			Assert.That(RefineryLawSilo.Wanted(true, 85, 100, false), Is.False);
			Assert.That(RefineryLawSilo.Wanted(true, 96, 100, false), Is.True);
			Assert.That(RefineryLawSilo.Wanted(true, 96, 100, true), Is.False);
			Assert.That(RefineryLawSilo.Wanted(true, 95, 100, false), Is.False);

			// Law off / classic: the plain 80% test is unchanged.
			Assert.That(RefineryLawSilo.Wanted(false, 85, 100, false), Is.True);
			Assert.That(RefineryLawSilo.Wanted(false, 85, 100, true), Is.True);
			Assert.That(RefineryLawSilo.Wanted(false, 80, 100, false), Is.False);
		}

		[Test]
		public void CrawlRollGateBlocksNonGbaWithoutDrawingRandom()
		{
			var silo = new ActorInfo("silo", new BuildableInfo(), new BuildingInfo());
			var rax = new ActorInfo("rax", new BuildableInfo(), new BuildingInfo(), new GivesBuildableAreaInfo());

			Assert.That(RefineryLawCrawlRoll.LegalLink(true, silo), Is.False);
			Assert.That(RefineryLawCrawlRoll.LegalLink(true, rax), Is.True);
			Assert.That(RefineryLawCrawlRoll.LegalLink(false, silo), Is.True);
			Assert.That(RefineryLawCrawlRoll.LegalLink(false, rax), Is.True);

			// The gate precedes the roll in the queue's && chain, so a law-blocked building consumes no
			// randoms — the deterministic stream must not depend on which building the queue produced.
			var rng = new MersenneTwister(42);
			var rollTaken = RefineryLawCrawlRoll.LegalLink(true, silo) && rng.Next(100) < 50;
			Assert.That(rollTaken, Is.False);
			Assert.That(rng.TotalCount, Is.EqualTo(0));

			// A legal link takes the roll exactly once.
			_ = RefineryLawCrawlRoll.LegalLink(true, rax) && rng.Next(100) < 50;
			Assert.That(rng.TotalCount, Is.EqualTo(1));

			// Classic path is unfiltered: a non-GBA building still reaches the draw when the law is off.
			_ = RefineryLawCrawlRoll.LegalLink(false, silo) && rng.Next(100) < 50;
			Assert.That(rng.TotalCount, Is.EqualTo(2));
		}

		[Test]
		public void RefineryLifecycleClassifiesTheDiff()
		{
			// Owner change in world: a capture — the victim logs a loss and the capturer an acquisition.
			var capture = RefineryLifecycle.Classify(true, true, false);
			Assert.That(capture.LostCause, Is.EqualTo("captured"));
			Assert.That(capture.Acquired, Is.True);

			// Same owner in world: nothing happened.
			var quiet = RefineryLifecycle.Classify(false, true, false);
			Assert.That(quiet.LostCause, Is.Null);
			Assert.That(quiet.Acquired, Is.False);

			// Gone from the world: the death flag splits killed from sold.
			var killed = RefineryLifecycle.Classify(false, false, true);
			Assert.That(killed.LostCause, Is.EqualTo("killed"));
			Assert.That(killed.Acquired, Is.False);
			var sold = RefineryLifecycle.Classify(false, false, false);
			Assert.That(sold.LostCause, Is.EqualTo("sold"));
			Assert.That(sold.Acquired, Is.False);
		}

		// REPAIR-B3 (SPEC_2026-10-09): the coverage oracle — deterministic route accounting, leg-pair
		// verdicts, site ranking, and the bounded probe budget. Pure functions; no World.

		[Test]
		public void RouteLengthMilliCountsDiagonalStepsDearly()
		{
			Assert.That(RefineryCoverageOracle.RouteLengthMilli(new List<CPos> { C(0, 0) }), Is.EqualTo(0));
			Assert.That(RefineryCoverageOracle.RouteLengthMilli(new List<CPos> { C(0, 0), C(1, 0) }), Is.EqualTo(1000));
			Assert.That(RefineryCoverageOracle.RouteLengthMilli(new List<CPos> { C(0, 0), C(1, 1) }), Is.EqualTo(1414));
			Assert.That(RefineryCoverageOracle.RouteLengthMilli(new List<CPos> { C(0, 0), C(1, 1), C(2, 1) }), Is.EqualTo(2414));
		}

		[Test]
		public void RouteLengthMilliIsDirectionSymmetric()
		{
			var forward = new List<CPos> { C(0, 0), C(1, 1), C(2, 1), C(3, 1) };
			var reverse = forward.AsEnumerable().Reverse().ToList();
			Assert.That(RefineryCoverageOracle.RouteLengthMilli(reverse),
				Is.EqualTo(RefineryCoverageOracle.RouteLengthMilli(forward)));
		}

		[Test]
		public void WithinServeRadiusIsInclusive()
		{
			Assert.That(RefineryCoverageOracle.WithinServeRadius(C(0, 0), C(10, 0), 10), Is.True);
			Assert.That(RefineryCoverageOracle.WithinServeRadius(C(0, 0), C(7, 7), 10), Is.True);    // 98 <= 100
			Assert.That(RefineryCoverageOracle.WithinServeRadius(C(0, 0), C(8, 8), 10), Is.False);   // 128 > 100
		}

		[Test]
		public void BothLegsCoverRequiresBothLegsInsideTheBound()
		{
			const int limit = 10_000;
			var okOut = new RefineryRouteWitness(true, 9_000, 6_000, 1);
			var okIn = new RefineryRouteWitness(true, 8_000, 5_000, 1);
			var longIn = new RefineryRouteWitness(true, 12_000, 8_000, 1);
			var deadOut = new RefineryRouteWitness(false, 0, int.MaxValue, 1);

			Assert.That(RefineryCoverageOracle.BothLegsCover(okOut, okIn, limit), Is.True);
			// Asymmetric legs (spec F9): a covered outbound + over-long inbound does not cover.
			Assert.That(RefineryCoverageOracle.BothLegsCover(okOut, longIn, limit), Is.False);
			Assert.That(RefineryCoverageOracle.BothLegsCover(deadOut, okIn, limit), Is.False);
		}

		[Test]
		public void VerdictAggregationTreatsDeferredAsUnknown()
		{
			Assert.That(RefineryCoverageOracle.Aggregate(true, true), Is.EqualTo(RefineryCoverageVerdict.Covered));
			Assert.That(RefineryCoverageOracle.Aggregate(true, false), Is.EqualTo(RefineryCoverageVerdict.Covered));
			// Budget cut before every candidate failed: Unknown, not Unserved — the anchor is retried.
			Assert.That(RefineryCoverageOracle.Aggregate(false, true), Is.EqualTo(RefineryCoverageVerdict.Unknown));
			Assert.That(RefineryCoverageOracle.Aggregate(false, false), Is.EqualTo(RefineryCoverageVerdict.Unserved));
		}

		[Test]
		public void CompareSitesIsDeterministic()
		{
			// Newly covered desc, then cost asc, then X then Y.
			Assert.That(RefineryCoverageOracle.CompareSites(2, 5000, C(1, 1), 1, 1000, C(0, 0)), Is.LessThan(0));
			Assert.That(RefineryCoverageOracle.CompareSites(1, 1000, C(0, 0), 1, 5000, C(0, 0)), Is.LessThan(0));
			Assert.That(RefineryCoverageOracle.CompareSites(1, 1000, C(0, 0), 1, 1000, C(1, 0)), Is.LessThan(0));
			Assert.That(RefineryCoverageOracle.CompareSites(1, 1000, C(0, 0), 1, 1000, C(0, 1)), Is.LessThan(0));
			Assert.That(RefineryCoverageOracle.CompareSites(1, 1000, C(0, 0), 1, 1000, C(0, 0)), Is.EqualTo(0));
		}

		[Test]
		public void ProbeBudgetCapsSpanTheWholeRefresh()
		{
			// R1 (SPEC "in one coverage refresh"): the site/probe ceilings bound the entire sweep —
			// nothing re-arms them mid-refresh; NewTick only re-opens the pacing window.
			var budget = new RefineryProbeBudget(siteLimit: 2, probeLimit: 3, tickProbeLimit: 2);

			Assert.That(budget.TryConsumeSite(), Is.True);
			Assert.That(budget.TryConsumeSite(), Is.True);
			Assert.That(budget.SiteCapSpent, Is.True);               // the ceiling reads spent at 2/2
			Assert.That(budget.TryConsumeSite(), Is.False);          // ... and a 3rd site is refused
			Assert.That(budget.RefreshSpent, Is.True);
			Assert.That(budget.DeferredCandidates, Is.EqualTo(1));

			budget.NewTick();                                        // a new tick does NOT re-arm caps
			Assert.That(budget.TryConsumeSite(), Is.False);
			Assert.That(budget.SitesEvaluated, Is.EqualTo(2));
		}

		[Test]
		public void ProbeBudgetTickPacingIsIndependentOfRefreshCaps()
		{
			// The per-tick limit only paces the spend: probes exhaust the tick window without
			// touching the refresh ceiling, and NewTick re-opens the window for the same refresh.
			var budget = new RefineryProbeBudget(siteLimit: 8, probeLimit: 8, tickProbeLimit: 2);

			Assert.That(budget.TryConsumeProbe(), Is.True);
			Assert.That(budget.TryConsumeProbe(), Is.True);
			Assert.That(budget.TryConsumeProbe(), Is.False);         // tick window spent (2/8 refresh)
			Assert.That(budget.TickProbesOpen, Is.False);
			Assert.That(budget.ProbeCapSpent, Is.False);

			budget.NewTick();
			Assert.That(budget.TickProbesOpen, Is.True);
			Assert.That(budget.TryConsumeProbe(), Is.True);          // 3/8 refresh — still open
			Assert.That(budget.ProbeCapSpent, Is.False);
		}

		[Test]
		public void RouteTravelMilliUsesTheCellSpeed()
		{
			// R1 (SPEC "estimated simulation travel time using movement/terrain speed"): the same
			// path costs half the time at double the speed — milli-ticks = milli-cells x 1024 / speed.
			var path = new List<CPos> { C(0, 0), C(1, 0), C(2, 0) };   // two orthogonal steps, 2000 milli-cells
			Assert.That(RefineryCoverageOracle.RouteTravelMilli(path, _ => 100), Is.EqualTo(2_000 * 1024 / 100));
			Assert.That(RefineryCoverageOracle.RouteTravelMilli(path, _ => 200), Is.EqualTo(2_000 * 1024 / 200));
			Assert.That(RefineryCoverageOracle.RouteTravelMilli(new List<CPos> { C(0, 0) }, _ => 100), Is.EqualTo(0));
		}

		[Test]
		public void RouteTravelMilliRanksZeroSpeedCellsInfinite()
		{
			// A zero-speed cell contradicts the path that found it — the route can never win.
			var path = new List<CPos> { C(0, 0), C(1, 0), C(2, 0) };
			Assert.That(RefineryCoverageOracle.RouteTravelMilli(path, c => c.X == 1 ? 0 : 100), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void LegPairRankPrefersTheFasterRoundTrip()
		{
			// R1: ranking follows simulation travel time, not raw route length — a longer fast-terrain
			// route outranks a shorter slow one.
			var fastLong = new RefineryRouteWitness(true, 20_000, 10_000, 1);
			var fastLongBack = new RefineryRouteWitness(true, 20_000, 10_000, 1);
			var slowShort = new RefineryRouteWitness(true, 12_000, 30_000, 1);
			var slowShortBack = new RefineryRouteWitness(true, 12_000, 30_000, 1);

			Assert.That(RefineryCoverageOracle.LegPairRank(fastLong, fastLongBack),
				Is.LessThan(RefineryCoverageOracle.LegPairRank(slowShort, slowShortBack)));

			// The slower leg bounds the round trip; ties break by the other leg.
			var asym = new RefineryRouteWitness(true, 5_000, 2_000, 1);
			Assert.That(RefineryCoverageOracle.LegPairRank(slowShort, asym),
				Is.GreaterThan(RefineryCoverageOracle.LegPairRank(fastLong, asym)));
		}

		[Test]
		public void DockEligibleRequiresAnEnabledTypeCompatibleHost()
		{
			var harvester = new BitSet<DockType>("Unload");
			var matching = new BitSet<DockType>("Unload");
			var wrong = new BitSet<DockType>("Dock");

			Assert.That(RefineryCoverageOracle.DockEligible(true, harvester, matching), Is.True);
			Assert.That(RefineryCoverageOracle.DockEligible(true, harvester, null), Is.True);      // wildcard host
			Assert.That(RefineryCoverageOracle.DockEligible(false, harvester, matching), Is.False); // disabled/dead dock
			Assert.That(RefineryCoverageOracle.DockEligible(true, harvester, wrong), Is.False);     // type mismatch
			Assert.That(RefineryCoverageOracle.DockEligible(true, default, matching), Is.False);    // no client type
		}

		[Test]
		public void AssignRefineriesRouteGateHonoursTheWitness()
		{
			// A geometrically in-range refinery only serves the anchor when the probe witness covers it —
			// unreachable or over-long routes stay -1 even inside the radius. The oracle returns the
			// round-trip rank for covered pairs and null for failed/unproven ones.
			var anchors = new[] { C(14, 10) };
			var refineries = new[] { C(12, 10) };

			Assert.That(ExpansionPlannerBotModule.AssignRefineries(anchors, refineries, 8,
				routeRank: (a, r) => null), Is.EqualTo(new[] { -1 }));
			Assert.That(ExpansionPlannerBotModule.AssignRefineries(anchors, refineries, 8,
				routeRank: (a, r) => 4_000), Is.EqualTo(new[] { 0 }));
		}

		[Test]
		public void AssignRefineriesPicksTheLowerRankedRefinery()
		{
			// Both refineries cover the anchor; the oracle's rank (not raw distance) decides.
			var anchors = new[] { C(14, 10) };
			var refineries = new[] { C(20, 10), C(12, 10) };   // r1 is geometrically nearer
			var assigned = ExpansionPlannerBotModule.AssignRefineries(anchors, refineries, 8,
				routeRank: (a, r) => r == 0 ? 3_000 : 9_000);

			Assert.That(assigned, Is.EqualTo(new[] { 0 }));   // r0 wins on rank despite the longer geometry
		}

		[Test]
		public void PatchWalkProbeCapRefusalKeepsTheUnexaminedCell()
		{
			// R3 regression: a final allowed outbound probe followed by a budget-refused
			// inbound must leave the cursor ON the cell — the R2 for-loop advanced past
			// it anyway, letting a later field exhaustion publish a false Unserved.
			var budget = new RefineryProbeBudget(siteLimit: 8, probeLimit: 3, tickProbeLimit: 8);
			var walk = new PatchWalk(0, count: 10, window: 4);

			Assert.That(budget.TryConsumeProbe(), Is.True);   // cell 0 outbound
			Assert.That(budget.TryConsumeProbe(), Is.True);   // cell 0 inbound — complete
			walk.CompleteCell();
			Assert.That(walk.Index, Is.EqualTo(1));

			Assert.That(budget.TryConsumeProbe(), Is.True);   // cell 1 outbound — last of cap (3/3)
			Assert.That(budget.TryConsumeProbe(), Is.False);  // cell 1 inbound refused at the ceiling
			Assert.That(budget.ProbeCapSpent, Is.True);

			// The module's interruption path never calls CompleteCell — the cursor must still
			// point at cell 1 so the next pass re-examines it instead of skipping to Unserved.
			Assert.That(walk.Index, Is.EqualTo(1));
			Assert.That(walk.FieldExhausted(10), Is.False);
		}

		[Test]
		public void PatchWalkTickCapInterruptionLeavesTheCellPending()
		{
			// R3 regression: tick pacing interrupting a cell mid-examination must not advance
			// the cursor either — the same for-loop skipped it before tickCapHit could stop it.
			var budget = new RefineryProbeBudget(siteLimit: 8, probeLimit: 8, tickProbeLimit: 1);
			var walk = new PatchWalk(0, count: 10, window: 4);

			Assert.That(budget.TryConsumeProbe(), Is.True);    // outbound (tick window 1/1)
			Assert.That(budget.TryConsumeProbe(), Is.False);   // inbound refused — pacing only
			Assert.That(budget.ProbeCapSpent, Is.False);
			Assert.That(budget.TickProbesOpen, Is.False);
			Assert.That(walk.Index, Is.EqualTo(0));            // cell 0 still pending

			budget.NewTick();
			Assert.That(budget.TryConsumeProbe(), Is.True);    // the resumed inbound probe
			walk.CompleteCell();
			Assert.That(walk.Index, Is.EqualTo(1));
		}

		[Test]
		public void PatchWalkSiteCapRefusalLeavesThePairUntouched()
		{
			// Cap-before-probe: when the site ceiling refuses admission the pair's walk never
			// runs — the stored cursor resumes unchanged next pass, zero probes consumed.
			var budget = new RefineryProbeBudget(siteLimit: 1, probeLimit: 8, tickProbeLimit: 8);
			var walk = new PatchWalk(0, count: 10, window: 4);

			Assert.That(budget.TryConsumeSite(), Is.True);    // another pair took the only slot
			Assert.That(budget.TryConsumeSite(), Is.False);   // admission refused for this pair
			Assert.That(budget.SiteCapSpent, Is.True);
			Assert.That(budget.ProbeCapSpent, Is.False);

			Assert.That(walk.Index, Is.EqualTo(0));
			Assert.That(walk.HasCell, Is.True);
		}

		[Test]
		public void PatchWalkWindowEndIsNotExhaustion()
		{
			// A pair whose window ends before the field does is UNKNOWN, never Unserved:
			// unexamined cells beyond the window could still prove coverage.
			var walk = new PatchWalk(0, count: 7, window: 4);
			for (var i = 0; i < 4; i++)
				walk.CompleteCell();

			Assert.That(walk.Index, Is.EqualTo(4));
			Assert.That(walk.HasCell, Is.False);               // this refresh's window spent
			Assert.That(walk.FieldExhausted(7), Is.False);     // cells 4-6 unexamined -> not Unserved

			// The next refresh resumes where it stopped and completes the field — only then
			// may the pair contribute to an Unserved verdict.
			var resumed = new PatchWalk(walk.Index, 7, 4);
			resumed.CompleteCell();
			resumed.CompleteCell();
			resumed.CompleteCell();
			Assert.That(resumed.FieldExhausted(7), Is.True);
		}

		[Test]
		public void PatchWalkResumeHonoursTheStoredIndex()
		{
			// The persisted (version, index) cursor is the resume point — a stale-version
			// cursor restarts at 0 (the caller's version check), a live one continues.
			var stored = new PatchWalk(4, count: 6, window: 4);
			Assert.That(stored.Index, Is.EqualTo(4));
			Assert.That(stored.HasCell, Is.True);              // window = cells 4-5

			stored.CompleteCell();
			Assert.That(stored.Index, Is.EqualTo(5));
			Assert.That(stored.FieldExhausted(6), Is.False);

			stored.CompleteCell();
			Assert.That(stored.Index, Is.EqualTo(6));
			Assert.That(stored.FieldExhausted(6), Is.True);    // fully walked — honest Unserved input
		}

		[Test]
		public void ProbeCapSpentAtCompletedCellBoundaryStillDefers()
		{
			// R4 regression (VP boundary check): the final ALLOWED probe lands the count
			// exactly on the ceiling — no refusal ever runs. The cell completes, the walk
			// exits on ProbeCapSpent with cells remaining, and the pair MUST still defer;
			// R3 suppressed the defer behind !ProbeCapSpent → false Unserved.
			var budget = new RefineryProbeBudget(siteLimit: 8, probeLimit: 2, tickProbeLimit: 8);
			var walk = new PatchWalk(0, count: 3, window: 8);

			Assert.That(budget.TryConsumeProbe(), Is.True);   // cell 0 outbound
			Assert.That(budget.TryConsumeProbe(), Is.True);   // cell 0 inbound — 2/2, cap spent by SUCCESS
			Assert.That(budget.ProbeCapSpent, Is.True);
			walk.CompleteCell();                              // cell 0 fully examined

			// The module's while exits on ProbeCapSpent with cells 1-2 unexamined...
			Assert.That(walk.HasCell, Is.True);
			Assert.That(walk.FieldExhausted(3), Is.False);

			// ...and the production predicate defers the pair — spent cap is not permission.
			Assert.That(RefineryCoverageOracle.PairDeferredAfterWalk(false, walk, 3), Is.True);
			Assert.That(RefineryCoverageOracle.Aggregate(false, true),
				Is.EqualTo(RefineryCoverageVerdict.Unknown)); // never Unserved
		}

		[Test]
		public void SpentProbeCapAtPairEntryDefersWithoutTouchingTheWalk()
		{
			// R4 cascade case: the NEXT anchor enters with the ceiling already spent —
			// the while never admits the walk, yet the pair must still defer rather than
			// aggregate Unserved (and the deferred marker is what re-arms the next pass).
			var budget = new RefineryProbeBudget(siteLimit: 8, probeLimit: 1, tickProbeLimit: 8);
			Assert.That(budget.TryConsumeProbe(), Is.True);   // an earlier pair spent the cap
			Assert.That(budget.ProbeCapSpent, Is.True);

			var walk = new PatchWalk(0, count: 5, window: 8);
			// while (walk.HasCell && !pairCovered && !tickCapHit && !ProbeCapSpent) never runs:
			Assert.That(walk.Index, Is.EqualTo(0));
			Assert.That(walk.HasCell, Is.True);
			Assert.That(RefineryCoverageOracle.PairDeferredAfterWalk(false, walk, 5), Is.True);

			// And a covered pair is exempt — exhaustion with a witness stays Covered.
			Assert.That(RefineryCoverageOracle.PairDeferredAfterWalk(true, walk, 5), Is.False);
		}

		sealed class LawStub : IBotExpansionTargetProvider
		{
			readonly int unservedInReach;
			readonly int beyond;

			public LawStub(int unservedInReach, int beyond)
			{
				this.unservedInReach = unservedInReach;
				this.beyond = beyond;
			}

			public CPos? ExpansionTarget => null;
			public bool WantsRefineryAtExpansionTarget => false;
			public int ExpansionTargetClaimRadius => 0;
			public CPos? RefineryClaimTarget => null;
			public int UnservedAnchorsInReach => unservedInReach;
			public int UnservedAnchorsBeyondReach => beyond;
		}
	}
}
