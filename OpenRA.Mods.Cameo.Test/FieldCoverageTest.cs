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
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

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
		public void OneRefineryServesAtMostOneAnchor()
		{
			var anchors = new[] { C(10, 10), C(14, 10) };
			var assigned = ExpansionPlannerBotModule.AssignRefineries(anchors, new[] { C(12, 10) }, 8);

			// equidistant: the lower anchor index takes it, the other stays unserved
			Assert.That(assigned, Is.EqualTo(new[] { 0, -1 }));
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
			var anchors = new[] { C(20, 20), C(24, 20), C(28, 20), C(60, 20) };
			var fieldOf = new[] { 0, 0, 0, 1 };
			var fieldCells = Fields(
				new[] { C(19, 19), C(20, 20), C(21, 20), C(25, 20), C(29, 20) },
				new[] { C(59, 19), C(60, 20) });
			var buildings = new[] { C(10, 20) };

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new[] { C(21, 21) }, null, buildings, 8, 80, null, null, null,
				out var unservedAnchors, out var unservedFields, out var tiers);

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
			// A's first refinery (at 21,21 serving the (20,20) spreader): (28,20), not (24,20).
			var anchors = new[] { C(20, 20), C(24, 20), C(28, 20) };
			var fieldOf = new[] { 0, 0, 0 };
			var fieldCells = Fields(new[] { C(19, 19), C(20, 20), C(21, 20), C(25, 20), C(29, 20) });

			var order = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells,
				new[] { C(21, 21) }, null, new[] { C(10, 20) }, 8, 80, null, null, null,
				out var unservedAnchors, out var unservedFields, out var tiers);

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
				new CPos[0], null, new[] { C(8, 10) }, 8, 80, null, null, null,
				out _, out _, out var tiers);

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
				new CPos[0], null, new[] { C(10, 20) }, 8, 80, i => i == 0, i => i == 0, null,
				out var unservedAnchors, out var unservedFields, out var tiers);

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
				new[] { C(13, 10) }, null, new[] { C(10, 10), C(45, 10) }, 8, 10, null, null, null,
				out var unservedAnchors, out var unservedFields, out _);

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
				stack, null, new[] { C(5, 10), C(55, 10) }, 8, 10, null, null, null,
				out var unservedAnchors, out var unservedFields, out _);

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
				new CPos[0], null, new[] { C(12, 10) }, 8, 10, i => i == 0, null, null,
				out var unservedAnchors, out var unservedFields, out var tiers);

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
				new CPos[0], null, new[] { C(10, 20) }, 8, 80, null, null, C(58, 22),
				out _, out _, out _);

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
				new CPos[0], null, new[] { C(12, 10) }, 8, 10, i => i == 0, null, null,
				out var unserved, out _, out _);
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
				new CPos[0], null, new[] { C(44, 20) }, 8, 6, null, null, null,
				out var unservedAnchors, out var unservedFields, out _);

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
				new[] { C(40, 20) }, null, new[] { C(44, 20) }, 8, 6, null, null, null,
				out var unservedAnchors, out var unservedFields, out _);

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
				new CPos[0], null, new[] { C(44, 20) }, 8, 6, null, null, null,
				out _, out _, out var tiers);

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
				new CPos[0], null, new[] { C(44, 20) }, 8, 6, i => i == 0, i => i == 0, null,
				out _, out _, out _);

			Assert.That(order, Is.Empty);
		}

		[Test]
		public void RefineryFlushToTheFieldBindsItsAnchorBeyondServeRadius()
		{
			// A refinery placed flush to a field's far edge can land beyond the serve radius from the spreader;
			// it still is that field's refinery and binds the nearest of the field's anchors — without this the
			// anchor stays unserved and the law claims a duplicate.
			var anchors = new[] { C(10, 10), C(14, 10) };
			var assigned = ExpansionPlannerBotModule.AssignRefineries(anchors, new[] { C(30, 10) }, 8,
				new[] { 0, 0 }, new[] { 0 });

			Assert.That(assigned, Is.EqualTo(new[] { -1, 0 }));
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
	}
}
