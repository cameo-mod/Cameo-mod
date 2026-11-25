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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;
using V = OpenRA.Mods.CA.Traits.RefineryDemand.Verdict;

namespace OpenRA.Mods.Cameo.Test
{
	// REF-DEMAND-01 (switch ref_demand, Opus ruling + user final spec 2026-11-25): the refinery law's anchors are resource zones; a zone near any own yard
	// and reachable from it asks for up to RefineryPerZoneCap refineries; friendly refineries keep RefinerySpacingCells apart (the fallback spacing when a
	// zone has no other site). The pure rule is tested directly, the planner and placement wiring and the DEV rows by pins, the arm by the yaml.
	[TestFixture]
	public sealed class RefineryDemandTest
	{
		const string Planner = "mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs";
		const string PlannerDemand = "mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs";
		const string Logic = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static CPos C(int x, int y) => new(x, y);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static List<MiniYamlNode> PlayerNodes() =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.ToList();

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		static List<CPos> Strip(int width, int height)
		{
			var cells = new List<CPos>();
			for (var y = 0; y < height; y++)
				for (var x = 0; x < width; x++)
					cells.Add(C(10 + x, 10 + y));

			return cells;
		}

		[Test]
		public void TheVerdictCoversEveryCombination()
		{
			foreach (var inRadius in new[] { false, true })
				foreach (var reachable in new[] { false, true })
					foreach (var allied in new[] { false, true })
						for (var count = 0; count <= 3; count++)
						{
							var v = RefineryDemand.Classify(inRadius, reachable, allied, count, 2);
							var expected = allied ? V.AlliedServed : count >= 2 ? V.ZoneCap : !inRadius ? V.OutOfRadius : !reachable ? V.Unreachable : V.Demand;
							Assert.That(v, Is.EqualTo(expected), $"{inRadius} {reachable} {allied} {count}");
							Assert.That(RefineryDemand.InReach(v), Is.EqualTo(v == V.Demand));
							Assert.That(RefineryDemand.Covers(v), Is.EqualTo(v == V.ZoneCap || v == V.AlliedServed));
							Assert.That(RefineryDemand.Demand(inRadius, reachable, allied, count, 2), Is.EqualTo(v == V.Demand ? 2 - count : 0));
						}

			Assert.That(RefineryDemand.Reason(V.OutOfRadius), Is.EqualTo("out_of_radius"));
			Assert.That(RefineryDemand.Reason(V.Unreachable), Is.EqualTo("unreachable"));
			Assert.That(RefineryDemand.Reason(V.AlliedServed), Is.EqualTo("allied_served"));
			Assert.That(RefineryDemand.Reason(V.ZoneCap), Is.EqualTo("zone_cap"));
		}

		[Test]
		public void AZoneTakesAtMostTwoRefineries()
		{
			Assert.That(RefineryDemand.Classify(true, true, false, 0, 2), Is.EqualTo(V.Demand));
			Assert.That(RefineryDemand.Classify(true, true, false, 1, 2), Is.EqualTo(V.Demand), "one friendly refinery: one more is wanted");
			Assert.That(RefineryDemand.Classify(true, true, false, 2, 2), Is.EqualTo(V.ZoneCap));
			Assert.That(RefineryDemand.Demand(true, true, false, 1, 2), Is.EqualTo(1));
			Assert.That(RefineryDemand.ZoneCap(0), Is.EqualTo(1), "a zone always takes its first refinery");

			Assert.That(RefineryDemand.ZoneCapFor(2, 26, 40), Is.EqualTo(1), "a small zone takes one refinery");
			Assert.That(RefineryDemand.ZoneCapFor(2, 40, 40), Is.EqualTo(2));
			Assert.That(RefineryDemand.ZoneCapFor(2, 3, 0), Is.EqualTo(2), "min 0 = always the cap");
			Assert.That(RefineryDemand.ZoneCapFor(0, 100, 40), Is.EqualTo(1));

			// Two refineries 10+ apart both count at an anchor between them (spacing 10 measured from the anchor).
			var anchor = C(50, 50);
			Assert.That(RefineryDemand.CountWithin(anchor, new[] { C(45, 50), C(56, 50), C(70, 50) }, 10), Is.EqualTo(2));
		}

		[Test]
		public void ABigFieldGivesSeveralZonesAndEveryPointOnce()
		{
			var big = Strip(40, 3);
			var zones = RefineryDemand.Zones(big, 10);
			Assert.That(zones.Count, Is.GreaterThan(1), "a 40-cell strip at radius 10");
			Assert.That(zones.SelectMany(z => z).OrderBy(c => c.Y).ThenBy(c => c.X), Is.EqualTo(big.OrderBy(c => c.Y).ThenBy(c => c.X)), "a cover, each point once");
			foreach (var zone in zones)
				foreach (var c in zone)
					Assert.That(RefineryDemand.DistanceSq(zone[0], c), Is.LessThanOrEqualTo(100), "every point within the radius of its seed");

			Assert.That(RefineryDemand.Zones(Strip(5, 3), 10).Count, Is.EqualTo(1), "a small field is one zone");

			// Deterministic: the input order does not matter.
			var shuffled = big.OrderByDescending(c => c.X * 7 + c.Y * 13 % 5).ToList();
			Assert.That(RefineryDemand.Zones(shuffled, 10), Is.EqualTo(zones));

			// The anchor is the zone point nearest the integer centroid.
			Assert.That(RefineryDemand.ZoneAnchor(new[] { C(0, 0), C(2, 0), C(4, 0) }), Is.EqualTo(C(2, 0)));
		}

		[Test]
		public void AnOwnRefineryWithinTenCoversAZoneAndBlocksASite()
		{
			var own = new[] { C(30, 30) };
			Assert.That(RefineryDemand.Covered(RefineryDemand.DistanceSq(C(30, 38), own[0]), 10), Is.True);
			Assert.That(RefineryDemand.CountWithin(C(30, 38), own, 10), Is.EqualTo(1));
			Assert.That(RefineryDemand.SiteAllowed(C(30, 38), own, 10), Is.False);
			Assert.That(RefineryDemand.SiteAllowed(C(30, 41), own, 10), Is.True, "11 cells away");
			Assert.That(RefineryDemand.SpacingTier(C(30, 33), own, 10, 5), Is.EqualTo(RefineryDemand.SpacingRefused), "inside the fallback spacing too");
		}

		[Test]
		public void AnAlliedRefineryBlocksASiteAndServesItsZone()
		{
			var allied = new[] { C(80, 20) };
			Assert.That(RefineryDemand.SiteAllowed(C(84, 24), allied, 10), Is.False, "sqrt(32) < 10");
			Assert.That(RefineryDemand.SpacingTier(C(84, 24), allied, 10, 5), Is.EqualTo(RefineryDemand.SpacingFallbackOnly));
			Assert.That(RefineryDemand.Classify(true, true, true, 1, 2), Is.EqualTo(V.AlliedServed), "an allied refinery serving the zone takes it off the demand");
		}

		[Test]
		public void TheFallbackSpacingIsUsedOnlyWhenTheFullSpacingLeavesNoSite()
		{
			var friendly = new[] { C(0, 0) };

			// Every candidate within 10; one beyond 5: the fallback triggers.
			var site = RefineryDemand.PickSpaced(new[] { C(3, 0), C(7, 0), C(0, 4) }, friendly, 10, 5, out var fallback);
			Assert.That(site, Is.EqualTo(C(7, 0)));
			Assert.That(fallback, Is.True);

			// A candidate beyond 10 wins over an earlier fallback-only one.
			site = RefineryDemand.PickSpaced(new[] { C(7, 0), C(11, 0) }, friendly, 10, 5, out fallback);
			Assert.That(site, Is.EqualTo(C(11, 0)));
			Assert.That(fallback, Is.False);

			// All within 5: no site at either spacing.
			Assert.That(RefineryDemand.PickSpaced(new[] { C(3, 0), C(0, 5) }, friendly, 10, 5, out fallback), Is.Null);
			Assert.That(fallback, Is.False);
		}

		[Test]
		public void TheYardCacheRefreshesOnAYardChangeOrTheInterval()
		{
			Assert.That(RefineryDemand.ReachRefreshDue(100, -1, 750, false), Is.True, "never refreshed");
			Assert.That(RefineryDemand.ReachRefreshDue(800, 100, 750, false), Is.False);
			Assert.That(RefineryDemand.ReachRefreshDue(850, 100, 750, false), Is.True);
			Assert.That(RefineryDemand.ReachRefreshDue(200, 100, 750, true), Is.True, "a new or lost yard");
		}

		[Test]
		public void TheDemandVerdictReplacesTheReachAndServedTestsOfTheClaimOrder()
		{
			// Two single-cell fields 40 cells from the only building: beyond ReachCells 6, so the old rule claims nothing (a refinery stands).
			var anchors = new[] { C(50, 10), C(80, 10) };
			var fieldOf = new[] { 0, 1 };
			var cells = new List<IReadOnlyCollection<CPos>> { new[] { C(50, 10) }, new[] { C(80, 10) } };
			var refineries = new[] { C(10, 40) };
			var tiles = new[] { C(10, 10) };

			var off = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, cells, refineries, tiles, 10, 6, null, null, null,
				out _, out _, out _, out _, out var beyond);
			Assert.That(off, Is.Empty);
			Assert.That(beyond, Is.EqualTo(2));

			var on = ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, cells, refineries, tiles, 10, 6, null, null, null,
				out var unserved, out _, out _, out _, out _, null, null, a => a == 0 ? V.Demand : V.ZoneCap);
			Assert.That(on, Is.EqualTo(new[] { 0 }), "the demand zone is in reach, the capped one is served");
			Assert.That(unserved, Is.EqualTo(1));
		}

		[Test]
		public void TheSwitchGatesTheAnchorsTheReachSetTheCapAndThePlacement()
		{
			var p = Source(Planner);
			Assert.That(p, Does.Contain("var inReach = inDemand != null ? inDemand[a] : reachDist[a] <= reachCells;"));
			Assert.That(p, Does.Contain("served[a] = RefineryDemand.Covers(verdict);"));
			Assert.That(p, Does.Contain("zonesActive = RefDemandOn && EnsureZoneModel(spreaders);"));
			Assert.That(p, Does.Contain("RefDemandZones ? ZoneCapAt(field) : Info.RefineryFieldCap"), "the REF-FIELD-02 cap per zone");
			Assert.That(p, Does.Contain("(exhaustionProof ? SpacingTierAt(topLeft) != RefineryDemand.SpacingRefused : SpacingTierAt(topLeft) == RefineryDemand.SpacingAllowed)"), "sites stay at the full spacing; the exhaustion proof counts fallback-spacing sites as legal");
			Assert.That(p, Does.Contain("FallbackLegal(actorInfo, bi, producer, distanceToBaseIsImportant, dockOffsets, valuable, layer, topLeft, true),"));
			Assert.That(p, Does.Contain("FallbackLegal(actorInfo, bi, producer, distanceToBaseIsImportant, dockOffsets, valuable, layer, topLeft, false));"));
			Assert.That(Source("mods/Cameo/Traits/BotModules/RefineryFallbackScan.cs"), Does.Contain("if (snapshot.PickLegal(pick))"), "a picked site passes the strict legality");
			Assert.That(p.Split("RouteRank, CoverageUnknown, DemandOverride);").Length - 1, Is.EqualTo(3));
			Assert.That(Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefinerySearch.cs"), Does.Contain("RouteRank, CoverageUnknown, DemandOverride);"));

			var d = Source(PlannerDemand);
			Assert.That(d, Does.Contain("Func<int, RefineryDemand.Verdict> DemandOverride => RefDemandArmed ? (demandVerdictFunc ??= DemandVerdict) : null;"));
			Assert.That(d, Does.Contain("bool RefDemandArmed => Info.RefDemand && zonesActive && LawActive;"));
			Assert.That(d, Does.Contain("pathFinder.PathMightExistForLocomotorBlockedByImmovable(locomotor, from, cell)"), "the domain check, no path search");
			Assert.That(d, Does.Not.Contain("FindPath"));
			Assert.That(d, Does.Contain("foreach (var yard in yards)"), "every own yard counts its own radius");

			var qm = Source(Logic + "BaseBuilderQueueManagerCA.cs");
			Assert.That(qm, Does.Contain("var spacingTier = spacing ? spacingLaw.RefinerySiteSpacingTier(cell) : RefineryDemand.SpacingAllowed;"));
			Assert.That(qm.IndexOf("if (gap1.HasValue)\n\t\t\t\treturn (gap1.Value, 1, 0);", StringComparison.Ordinal),
				Is.LessThan(qm.IndexOf("if (relaxed0.HasValue)", StringComparison.Ordinal)), "the fallback spacing only after every full-spacing site");

			var search = Source(Logic + "RefinerySiteSearch.cs");
			Assert.That(search, Does.Contain("return LegalCell(e, c.Cell) && SpacingAdmits(c.Cell, c.SpacingFallback);"));
			Assert.That(search, Does.Contain("Offer(e, cell, new RefinerySiteScore(0, tier, relaxed ? gap + 2 : gap, d2, cell.X, cell.Y), anchor, now, relaxed);"));
			Assert.That(search.Split("|| SpacingTier(cell) != RefineryDemand.SpacingAllowed)").Length - 1, Is.EqualTo(1), "the sampled sites keep the full spacing; the widened site may be fallback-spacing (relaxed, deferred)");
		}

		[Test]
		public void TheDemandRowsAreDevOnly()
		{
			var d = Source(PlannerDemand);
			foreach (var method in new[] { "void ZonesBuiltObserved(", "void RefDemandObserved(", "void AnchorParkedObserved(", "void AnchorPendingObserved(", "void RefineryPlacedObserved(" })
			{
				var at = d.IndexOf(method, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThan(0), method);
				Assert.That(d.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1), method);
			}

			var p = Source(Planner);
			Assert.That(p, Does.Contain("AnchorParkedObserved(anchor, \"claim_fail\", tick + Info.RefineryClaimFailParkTicks);"));
			Assert.That(p, Does.Contain("AnchorParkedObserved(anchor, \"stuck\", tick + Info.ParkTicks);"));
			Assert.That(p, Does.Contain("unclaimedAnchorsInReach = order.Count;\n\t\t\tRefDemandObserved(tick);"));

			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			foreach (var kind in new[] { "rd_zones", "rd_zone", "rd_demand", "rd_anchor_parked", "rd_anchor_pending", "rd_refinery_placed" })
				Assert.That(dev, Does.Contain("\"" + kind + "\""), kind);
			foreach (var field in new[] { "\"center\"", "\"radius\"", "\"zone\"", "\"cell\"", "\"fallback_spacing\"" })
				Assert.That(dev, Does.Contain(field), field);
			Assert.That(dev, Does.Contain("const int RefineryDemandRepeatTicks = 750;"));
		}

		[Test]
		public void OnlyTheGenericPlannerArmsItAndTheRowIsRegistered()
		{
			var nodes = PlayerNodes();
			var planner = nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(FieldOrNull(planner, "RequiresCondition"), Is.EqualTo("genericbot"));
			Assert.That(FieldOrNull(planner, "RefDemand"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(planner, "YardRadiusCells"), Is.EqualTo("15"));
			Assert.That(FieldOrNull(planner, "RefineryZoneRadius"), Is.EqualTo("10"));
			Assert.That(FieldOrNull(planner, "RefineryPerZoneCap"), Is.EqualTo("2"));
			Assert.That(FieldOrNull(planner, "RefinerySecondPerZoneMinCells"), Is.EqualTo("40"));
			Assert.That(FieldOrNull(planner, "RefinerySpacingCells"), Is.EqualTo("10"));
			Assert.That(FieldOrNull(planner, "RefinerySpacingFallbackCells"), Is.EqualTo("5"));
			Assert.That(nodes.Count(n => n.Value.Nodes.Any(c => c.Key == "RefDemand")), Is.EqualTo(1), "no other module arms it");
			Assert.That(FieldOrNull(nodes.Single(n => n.Key == "BaseBuilderBotModuleCA@classic"), "RefDemand"), Is.Null);

			Assert.That(AblationIds.RefDemand, Is.EqualTo("ref_demand"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.RefDemand));
		}

		[Test]
		public void APartiallyReachableZoneIsAccepted()
		{
			// Only the far cell of the zone shares the harvester domain of the yard; the nearest cell does not.
			var cells = new[] { C(0, 0), C(1, 0), C(50, 50) };
			Assert.That(RefineryDemand.AnyCellReachable(cells, true, c => c.X > 40), Is.True);
		}

		[Test]
		public void AZoneWithNoReachableCellOrNoHarvesterProfileIsRejected()
		{
			var cells = new[] { C(0, 0), C(1, 0), C(50, 50) };
			Assert.That(RefineryDemand.AnyCellReachable(cells, true, c => false), Is.False, "no cell in the domain");
			Assert.That(RefineryDemand.AnyCellReachable(cells, false, c => true), Is.False, "no harvester profile: never accepted");
			Assert.That(RefineryDemand.AnyCellReachable(Array.Empty<CPos>(), true, c => true), Is.False, "an empty zone");
			Assert.That(RefineryDemand.AnyCellReachable(cells, true, null), Is.False, "no domain query");
		}

		[Test]
		public void AStrictSearchNotFinishedDefersAndPlacesNoRelaxedSite()
		{
			// Two relaxed sites found, no strict site yet, the strict search still running (the deadline hit): deferred, nothing promoted.
			Assert.That(RefineryDemand.RelaxedGate(false, 0, 2), Is.EqualTo(RefineryDemand.RelaxedStep.Deferred));

			// The deferral also holds the certified cancel back (the Visit condition), and a strict site makes the relaxed ones moot.
			Assert.That(RefineryDemand.RelaxedGate(false, 1, 2), Is.EqualTo(RefineryDemand.RelaxedStep.NotNeeded));

			// The relaxed list is only ever filled by Offer(..., spacingFallback), never Best.
			var search = Source("mods/CA/Traits/BotModules/BotModuleLogic/RefinerySiteSearch.cs");
			Assert.That(search, Does.Contain("InsertBest(e.Relaxed, c, x => x.Score, x => x.Cell);"));
			Assert.That(search, Does.Contain("PromoteRelaxed(e, now);"));
		}

		[Test]
		public void ARelaxedSiteIsAllowedOnlyAfterTheStrictSearchIsExhausted()
		{
			Assert.That(RefineryDemand.RelaxedGate(true, 0, 1), Is.EqualTo(RefineryDemand.RelaxedStep.Promote));
			Assert.That(RefineryDemand.RelaxedGate(true, 0, 0), Is.EqualTo(RefineryDemand.RelaxedStep.NotNeeded), "nothing relaxed to use");
			Assert.That(RefineryDemand.RelaxedGate(true, 2, 1), Is.EqualTo(RefineryDemand.RelaxedStep.NotNeeded), "a strict site exists");
		}

		[Test]
		public void TheDevDemandIsTheGameplayDemand()
		{
			var verdicts = new[] { V.Demand, V.Demand, V.ZoneCap, V.Unreachable, V.Demand };
			var fieldOf = new[] { 0, 1, 2, 3, 4 };
			var anchors = new[] { C(20, 20), C(40, 20), C(60, 20), C(80, 20), C(100, 20) };
			var fieldCells = new IReadOnlyCollection<CPos>[]
			{
				new[] { C(20, 20) }, new[] { C(40, 20) }, new[] { C(60, 20) }, new[] { C(80, 20) }, new[] { C(100, 20) },
			};

			// Zone 1's coverage is unknown: gameplay does not count it, so neither may the DEV row.
			ExpansionPlannerBotModule.ClaimOrder(anchors, fieldOf, fieldCells, Array.Empty<CPos>(), new[] { C(10, 20) }, 8, 200, null, null, null,
				out var unservedInReach, out _, out _, out _, out _,
				coverageUnknown: i => i == 1, demand: i => verdicts[i]);

			var dev = RefineryDemand.EligibleZones(verdicts.Length, i => verdicts[i], i => i == 1);
			Assert.That(dev, Is.EqualTo(2));
			Assert.That(dev, Is.EqualTo(unservedInReach));

			// The remaining cap is its own number and never the demand.
			Assert.That(RefineryDemand.CapRemaining(V.Demand, 2, 1), Is.EqualTo(1));
			Assert.That(RefineryDemand.CapRemaining(V.ZoneCap, 2, 2), Is.EqualTo(0));
			Assert.That(Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs"),
				Does.Contain("RefineryDemand.EligibleZones(anchors.Count, i => demandVerdicts[i], CoverageUnknown)"));
		}

		[Test]
		public void TheSwitchIsOffByDefault()
		{
			var info = (ExpansionPlannerBotModuleInfo)Activator.CreateInstance(typeof(ExpansionPlannerBotModuleInfo), true);
			Assert.That(info.RefDemand, Is.False);
			Assert.That(info.RefineryZoneRadius, Is.EqualTo(10));
			Assert.That(info.RefineryPerZoneCap, Is.EqualTo(2));
			Assert.That(info.RefinerySecondPerZoneMinCells, Is.EqualTo(40));
			Assert.That(info.RefinerySpacingCells, Is.EqualTo(10));
			Assert.That(info.RefinerySpacingFallbackCells, Is.EqualTo(5));
			Assert.That(info.YardRadiusCells, Is.EqualTo(15));
			Assert.That(info.ReachRefreshTicks, Is.EqualTo(750));
		}
	}
}
