#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class BaseFrontBackPlannerTest
	{
		static readonly CPos Center = new(50, 50);
		const double Merge45 = 0.7072; // a hair under cos(45) so a 45-degree split still merges
		const double Cone45 = 0.7071;

		static ActorInfo Info(string name, params TraitInfo[] traits)
		{
			return new ActorInfo(name, traits);
		}

		static T With<T>(T info, string field, string value)
		{
			FieldLoader.LoadFieldOrProperty(info, field, value);
			return info;
		}

		static ProductionInfo Producing(params string[] queues)
		{
			return With(new ProductionInfo(), "Produces", string.Join(",", queues));
		}

		static ProductionQueueInfo QueueOf(string type, string group)
		{
			var q = new ProductionQueueInfo();
			FieldLoader.LoadFieldOrProperty(q, "Type", type);
			FieldLoader.LoadFieldOrProperty(q, "Group", group);
			return q;
		}

		static BaseFront EastFront(int frontProj, int rearProj)
		{
			return new BaseFront
			{
				Id = 0,
				Kind = FrontAnchorKind.EnemySpawn,
				Anchor = new CPos(90, 50),
				Centre = Center,
				DirX = 1,
				DirY = 0,
				FrontProj = frontProj,
				RearProj = rearProj,
				LineCells = { new CPos(50 + rearProj, 50), new CPos(50 + frontProj, 50) },
			};
		}

		// --- fronts ---

		[Test]
		public void AnchorsWithinMergeAngleShareAFront()
		{
			var fronts = BaseFrontBackPlannerBotModule.BuildFronts(Center,
				new List<(CPos, FrontAnchorKind)> { (new CPos(90, 50), FrontAnchorKind.EnemySpawn), (new CPos(95, 55), FrontAnchorKind.Expansion) },
				new List<CPos>(), new List<CPos>(), Merge45, 3);
			Assert.That(fronts.Count, Is.EqualTo(1));
			Assert.That(fronts[0].Kind, Is.EqualTo(FrontAnchorKind.EnemySpawn)); // the earlier, higher-priority anchor keeps the front
		}

		[Test]
		public void AnchorsBeyondMergeAngleSplitIntoFrontsInOrder()
		{
			var fronts = BaseFrontBackPlannerBotModule.BuildFronts(Center,
				new List<(CPos, FrontAnchorKind)> { (new CPos(90, 50), FrontAnchorKind.EnemySpawn), (new CPos(50, 90), FrontAnchorKind.Expansion), (new CPos(10, 50), FrontAnchorKind.LastAttack) },
				new List<CPos>(), new List<CPos>(), Merge45, 3);
			Assert.That(fronts.Count, Is.EqualTo(3));
			Assert.That(fronts[0].Anchor, Is.EqualTo(new CPos(90, 50)));
			Assert.That(fronts[1].Anchor, Is.EqualTo(new CPos(50, 90)));
		}

		[Test]
		public void MaxFrontsCapsTheAnchors()
		{
			var fronts = BaseFrontBackPlannerBotModule.BuildFronts(Center,
				new List<(CPos, FrontAnchorKind)> { (new CPos(90, 50), FrontAnchorKind.EnemySpawn), (new CPos(50, 90), FrontAnchorKind.Expansion), (new CPos(10, 50), FrontAnchorKind.LastAttack), (new CPos(50, 10), FrontAnchorKind.MapCentre) },
				new List<CPos>(), new List<CPos>(), Merge45, 2);
			Assert.That(fronts.Count, Is.EqualTo(2));
		}

		[Test]
		public void DefencesJoinTheirBestAlignedFrontAndSetTheLine()
		{
			var defences = new List<CPos> { new(60, 50), new(62, 48), new(52, 58) };
			var fronts = BaseFrontBackPlannerBotModule.BuildFronts(Center,
				new List<(CPos, FrontAnchorKind)> { (new CPos(90, 50), FrontAnchorKind.EnemySpawn), (new CPos(50, 90), FrontAnchorKind.LastAttack) },
				defences, new List<CPos> { new(52, 50), new(55, 50) }, Merge45, 3);

			var east = fronts[0];
			var south = fronts[1];
			Assert.That(east.LineCells, Is.EquivalentTo(new[] { new CPos(60, 50), new CPos(62, 48) }));
			Assert.That(south.LineCells, Is.EquivalentTo(new[] { new CPos(52, 58) }));
			Assert.That(east.FrontProj, Is.EqualTo(12)); // the foremost defence
			Assert.That(east.RearProj, Is.EqualTo(10)); // the rearmost defence
			Assert.That(east.ArcEdgeProj, Is.EqualTo(5)); // foremost own building in the arc (55,50)
		}

		// --- approach ---

		[Test]
		public void ApproachNeedsADefenceLine()
		{
			var front = EastFront(10, 10);
			front.LineCells.Clear();
			var space = Enumerable.Range(55, 30).SelectMany(x => Enumerable.Range(45, 11).Select(y => new CPos(x, y)));
			Assert.That(BaseFrontBackPlannerBotModule.ApproachCells(space, Center, front, 4, Cone45), Is.Empty);
		}

		[Test]
		public void ApproachIsTheBandAheadOfTheLineInsideTheCone()
		{
			var front = EastFront(10, 10);
			var space = Enumerable.Range(40, 40).SelectMany(x => Enumerable.Range(40, 30).Select(y => new CPos(x, y)));
			var approach = BaseFrontBackPlannerBotModule.ApproachCells(space, Center, front, 4, Cone45).ToHashSet();

			Assert.That(approach, Does.Contain(new CPos(60, 50))); // on the line = start of the approach
			Assert.That(approach, Does.Contain(new CPos(64, 50))); // end of the band
			Assert.That(approach, Does.Not.Contain(new CPos(65, 50))); // past the approach depth
			Assert.That(approach, Does.Not.Contain(new CPos(59, 50))); // behind the line
			Assert.That(approach, Does.Not.Contain(new CPos(62, 63))); // outside the 45-degree cone
			Assert.That(approach, Does.Contain(new CPos(62, 60))); // inside the cone
		}

		[Test]
		public void AnnulusBoundsCoverTheWholeApproachBand()
		{
			// Unclamped passthrough: band [10,24] in a 45-degree cone needs
			// ceil(24/0.7071)+1 = 35 cells of Euclidean reach.
			Assert.That(BaseFrontBackPlannerBotModule.AnnulusForApproach(10, 14, Cone45, 50),
				Is.EqualTo((9, 35)));
		}

		[Test]
		public void AnnulusOuterEdgeClampsToTheEngineCap()
		{
			// Playtest crash C1 (Imminent Destruction): a line crawled 40 cells out
			// asked for maxRange 78 — the engine throws past MaximumTileSearchRange.
			Assert.That(BaseFrontBackPlannerBotModule.AnnulusForApproach(40, 14, Cone45, 50),
				Is.EqualTo((39, 50)));
		}

		[Test]
		public void AnnulusBeyondTheCapIsUnmeasurableNotACrash()
		{
			// inner must never exceed outer (engine throws on that too): a band
			// starting past the cap returns null — the approach reads as no cells.
			Assert.That(BaseFrontBackPlannerBotModule.AnnulusForApproach(52, 14, Cone45, 50),
				Is.Null);
			// The exactly-at-cap ring is still legal.
			Assert.That(BaseFrontBackPlannerBotModule.AnnulusForApproach(51, 14, Cone45, 50),
				Is.EqualTo((50, 50)));
		}

		[Test]
		public void AnnulusDegenerateInputsNeverProduceAnIllegalRange()
		{
			// coneCos <= 0 (cone half-angle >= 90 in yaml) saturates the outer
			// edge at the cap rather than producing a negative range.
			var degenerate = BaseFrontBackPlannerBotModule.AnnulusForApproach(10, 14, 0, 50);
			Assert.That(degenerate.HasValue && degenerate.Value.Outer <= 50
				&& degenerate.Value.Inner <= degenerate.Value.Outer);
			// An unset front projection (int.MinValue sentinel) must not overflow
			// into a legal-looking range either.
			Assert.That(BaseFrontBackPlannerBotModule.AnnulusForApproach(int.MinValue, 14, Cone45, 50),
				Is.Null);
		}

		// --- radar ---

		[Test]
		public void RadarStaysStrictlyBehindTheRearmostDefence()
		{
			var front = EastFront(10, 10); // the line sits at proj 10
			var candidates = new List<CPos>
			{
				new(61, 50), // beyond the line
				new(60, 50), // ON the line
				new(59, 50), // setback 1 < RadarMinSetbackCells
				new(57, 50), // setback 3: the only legal cell
			};
			var pick = BaseFrontBackPlannerBotModule.ChooseRadarCell(candidates, Center, front,
				new List<CPos>(), new HashSet<CPos>(), 18, 2, 8);
			Assert.That(pick.Cell, Is.EqualTo(new CPos(57, 50)));
			Assert.That(pick.SetbackCells, Is.EqualTo(3));
		}

		[Test]
		public void RadarNeverGoesForward()
		{
			var front = EastFront(10, 10);
			var candidates = new List<CPos> { new(61, 50), new(65, 50), new(60, 50) };
			var pick = BaseFrontBackPlannerBotModule.ChooseRadarCell(candidates, Center, front,
				new List<CPos>(), new HashSet<CPos>(), 18, 2, 8);
			Assert.That(pick.Cell, Is.Null);
			Assert.That(pick.Hold, Is.False); // the caller (module) decides wait-vs-back; the static just refuses
		}

		[Test]
		public void RadarPickScoredByNewlyCoveredApproachCells()
		{
			var front = EastFront(10, 10);
			var approach = new List<CPos> { new(64, 50), new(66, 50), new(74, 50), new(76, 50) };
			var covered = new HashSet<CPos> { new(64, 50), new(66, 50) }; // an existing radar owns the near cells
			var candidates = new List<CPos> { new(56, 50), new(58, 50), new(56, 52) };

			var pick = BaseFrontBackPlannerBotModule.ChooseRadarCell(candidates, Center, front,
				approach, covered, 18, 2, 8);

			// (58,50): all four approach cells in range -> 2 new + 2 overlap beats
			// (56,50): 1 new + 2 overlap and (56,52): 1 overlap only.
			Assert.That(pick.Cell, Is.EqualTo(new CPos(58, 50)));
			Assert.That(pick.NewCoverageCells, Is.EqualTo(2));
			Assert.That(pick.OverlapCells, Is.EqualTo(2));
			Assert.That(pick.SetbackCells, Is.EqualTo(2));
		}

		[Test]
		public void RadarTieBreaksBySmallerSetbackThenCell()
		{
			var front = EastFront(10, 10);
			var approach = new List<CPos> { new(74, 50) };
			var covered = new HashSet<CPos>();
			// Both cover the same new cell; (58,50) sits closer to the line (setback 2 < 3) and wins.
			var pick = BaseFrontBackPlannerBotModule.ChooseRadarCell(
				new List<CPos> { new(57, 50), new(58, 50) }, Center, front, approach, covered, 18, 2, 8);
			Assert.That(pick.Cell, Is.EqualTo(new CPos(58, 50)));
		}

		[Test]
		public void RadarPickIsDeterministicUnderInputOrder()
		{
			var front = EastFront(10, 10);
			var approach = new List<CPos> { new(74, 50) };
			var a = new List<CPos> { new(57, 50), new(58, 50), new(56, 50) };
			var b = a.OrderByDescending(c => c.X).ToList();
			Assert.That(
				BaseFrontBackPlannerBotModule.ChooseRadarCell(a, Center, front, approach, new HashSet<CPos>(), 18, 2, 8).Cell,
				Is.EqualTo(BaseFrontBackPlannerBotModule.ChooseRadarCell(b, Center, front, approach, new HashSet<CPos>(), 18, 2, 8).Cell));
		}

		// --- production / valuable ---

		[Test]
		public void ProductionTakesTheForemostCellAtOrBehindTheLimit()
		{
			var front = EastFront(10, 10);
			var candidates = new List<CPos> { new(55, 50), new(59, 50), new(60, 50), new(61, 50) };
			var pick = BaseFrontBackPlannerBotModule.ChooseProductionCell(candidates, Center, front, 10, 0);
			Assert.That(pick.Cell, Is.EqualTo(new CPos(60, 50))); // proj 10 = the line itself, never beyond
		}

		[Test]
		public void ProductionBeyondTheLimitKeepsTheCallersFallback()
		{
			var front = EastFront(10, 10);
			var candidates = new List<CPos> { new(61, 50), new(63, 50) };
			Assert.That(BaseFrontBackPlannerBotModule.ChooseProductionCell(candidates, Center, front, 10, 0).Cell, Is.Null);
		}

		[Test]
		public void ValuableGoesFarthestFromEveryFront()
		{
			var east = EastFront(10, 10);
			var south = new BaseFront { Id = 1, Centre = Center, DirX = 0, DirY = 1, Anchor = new CPos(50, 90) };
			var fronts = new List<BaseFront> { east, south };
			var candidates = new List<CPos> { new(45, 50), new(50, 45), new(55, 55), new(40, 40) };
			var pick = BaseFrontBackPlannerBotModule.ChooseValuableCell(candidates, Center, fronts);
			Assert.That(pick.Cell, Is.EqualTo(new CPos(40, 40))); // proj -10 on BOTH axes
		}

		[Test]
		public void ValuableWithNoFrontsAnswersNothing()
		{
			Assert.That(BaseFrontBackPlannerBotModule.ChooseValuableCell(
				new List<CPos> { new(40, 40) }, Center, new List<BaseFront>()).Cell, Is.Null);
		}

		// --- classification ---

		[Test]
		public void ClassifyOrderRadarBeatsDefenceBeatsProduction()
		{
			var radarWithGun = Info("test_radar_tower", new RangedGpsProviderInfo(), new AttackOmniInfo());
			Assert.That(BaseFrontBackPlannerBotModule.Classify(radarWithGun, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Radar));

			var defenceProducer = Info("test_defence_factory", new AttackOmniInfo(), Producing("Vehicle"));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(defenceProducer, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Defence));
		}

		[Test]
		public void ClassifyGroundAndNavalProductionGoFront()
		{
			var factory = Info("test_factory", Producing("Vehicle"), QueueOf("Vehicle", "Vehicle"));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(factory, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Production));

			var naval = Info("test_navalyard", Producing("Ship"), QueueOf("Ship", "Ship"));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(naval, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Production));
		}

		[Test]
		public void AirOnlyProducersAreExemptFromTheFront()
		{
			var helipad = Info("test_helipad", Producing("Aircraft"), QueueOf("Aircraft", "Aircraft"));
			Assert.That(BaseFrontBackPlannerBotModule.IsAirOnlyProducer(helipad), Is.True);
			Assert.That(BaseFrontBackPlannerBotModule.Classify(helipad, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Building));

			// A mixed producer (vehicles AND aircraft) is NOT exempt.
			var mixed = Info("test_mixed", Producing("Vehicle", "Aircraft"), QueueOf("Vehicle", "Vehicle"), QueueOf("Aircraft", "Aircraft"));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(mixed, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Production));
		}

		[Test]
		public void ValuableMeansTechSuperweaponOrPassiveIncomeOnly()
		{
			var byName = Info("test_lab");
			Assert.That(BaseFrontBackPlannerBotModule.Classify(byName, 1000,
				new HashSet<string> { "test_lab" }, null), Is.EqualTo(FrontBackClass.Valuable));

			var byCategory = Info("test_temple");
			Assert.That(BaseFrontBackPlannerBotModule.Classify(byCategory, 1000,
				new HashSet<string>(), _ => BuildOrderCategory.Superweapon), Is.EqualTo(FrontBackClass.Valuable));

			var income = Info("test_derrick", new CashTricklerInfo());
			Assert.That(BaseFrontBackPlannerBotModule.Classify(income, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Valuable));
		}

		[Test]
		public void CheapIsCrawlResidueIsBuilding()
		{
			var plant = Info("test_plant", With(new ValuedInfo(), "Cost", "500"));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(plant, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Crawl));

			var pricey = Info("test_pricey", With(new ValuedInfo(), "Cost", "1500"));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(pricey, 1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Building));
		}

		[Test]
		public void RefineryAndRadarLabels()
		{
			Assert.That(BaseFrontBackPlannerBotModule.Classify(Info("test_refinery", new RefineryInfo()),
				1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Refinery));
			Assert.That(BaseFrontBackPlannerBotModule.Classify(Info("test_radardome", new ProvidesRadarInfo()),
				1000, new HashSet<string>(), null), Is.EqualTo(FrontBackClass.Radar));
		}

		// --- radar provider choice ---

		[Test]
		public void RadarProviderPrefersNonCollidingQueues()
		{
			var commcenter = Info("test_commcenter", Producing("Upgrades"), With(new ValuedInfo(), "Cost", "1500"));
			var dome = Info("test_dome", With(new ValuedInfo(), "Cost", "1800"));
			var owned = new HashSet<string> { "Upgrades" }; // we already produce it - a second commcenter would double it
			var pick = BaseFrontBackPlannerBotModule.ChooseRadarProvider(new List<ActorInfo> { commcenter, dome }, owned);
			Assert.That(pick, Is.SameAs(dome));
		}

		[Test]
		public void RadarProviderFallsBackToCheapestWhenAllCollide()
		{
			var cheap = Info("test_cheap", Producing("Upgrades"), With(new ValuedInfo(), "Cost", "1200"));
			var pricey = Info("test_pricey", Producing("Upgrades"), With(new ValuedInfo(), "Cost", "2000"));
			var pick = BaseFrontBackPlannerBotModule.ChooseRadarProvider(
				new List<ActorInfo> { pricey, cheap }, new HashSet<string> { "Upgrades" });
			Assert.That(pick, Is.SameAs(cheap));
		}

		[Test]
		public void RadarRangeFloorsToWholeCells()
		{
			var info = Info("test_radar", With(new RangedGpsProviderInfo(), "Range", "20000"));
			Assert.That(BaseFrontBackPlannerBotModule.RadarRangeCells(info), Is.EqualTo(19)); // 20000 / 1024 = 19.5
		}

		// --- boss_review 2026-10-04 corrections B1-B4 ---

		[Test]
		public void RadarTargetIsAbsoluteNotDeficit()
		{
			// B1: a defended front's target does not shrink with providers already serving it —
			// the caller subtracts owned/planned itself.
			Assert.That(BaseFrontBackPlannerBotModule.WantedForFront(true, 0, 30, 2), Is.EqualTo(1));
			Assert.That(BaseFrontBackPlannerBotModule.WantedForFront(true, 29, 30, 2), Is.EqualTo(1));
			Assert.That(BaseFrontBackPlannerBotModule.WantedForFront(true, 30, 30, 2), Is.EqualTo(2));
			Assert.That(BaseFrontBackPlannerBotModule.WantedForFront(true, 100, 30, 2), Is.EqualTo(2)); // per-front cap
			Assert.That(BaseFrontBackPlannerBotModule.WantedForFront(true, 30, 30, 1), Is.EqualTo(1)); // maxPerFront
			Assert.That(BaseFrontBackPlannerBotModule.WantedForFront(false, 500, 30, 2), Is.EqualTo(0)); // no line
		}

		[Test]
		public void AggregateTargetChargesEachFrontsOwnUnmetNeed()
		{
			// B1 rev-2 (boss re-review): the caller compares the ABSOLUTE target against ALL owned
			// providers, so the target must equal owned + every front's unmet need — per-front
			// allocation, not a flat sum of wants.
			Assert.That(BaseFrontBackPlannerBotModule.AggregateRadarTarget(2, new[] { 1, 1 }, new[] { 2, 0 }),
				Is.EqualTo(3), "A over-served 2/1 + B empty and defended: deficit = B's first radar");
			Assert.That(BaseFrontBackPlannerBotModule.AggregateRadarTarget(1, new[] { 1, 1 }, new[] { 1, 0 }),
				Is.EqualTo(2), "one served + one empty defended front");
			Assert.That(BaseFrontBackPlannerBotModule.AggregateRadarTarget(2, new[] { 1, 1 }, new[] { 1, 1 }),
				Is.EqualTo(2), "all wants met: nothing new");
			Assert.That(BaseFrontBackPlannerBotModule.AggregateRadarTarget(1, new[] { 2, 1 }, new[] { 1, 0 }),
				Is.EqualTo(3), "A's justified extra AND B's first both sit in the deficit");
			Assert.That(BaseFrontBackPlannerBotModule.AggregateRadarTarget(3, new int[0], new int[0]),
				Is.EqualTo(3), "no fronts at all: owned providers alone are the target");
		}

		[Test]
		public void StrayProvidersNeverConsumeAnotherFrontsFirstSlot()
		{
			// B1 rev-2: providers surviving at a now-undefended base (assigned to a want-0 front,
			// or unassigned entirely) still count as owned but must not suppress a new defended
			// front's required first radar — the target exceeds owned by exactly one.
			const int Owned = 2;
			var assignedToDeadBase = BaseFrontBackPlannerBotModule.AggregateRadarTarget(Owned, new[] { 0, 1 }, new[] { 2, 0 });
			Assert.That(assignedToDeadBase - Owned, Is.EqualTo(1));

			var unassigned = BaseFrontBackPlannerBotModule.AggregateRadarTarget(Owned, new[] { 0, 1 }, new[] { 0, 0 });
			Assert.That(unassigned - Owned, Is.EqualTo(1));

			// The same guarantee with A over-served: no under-served defended front may request
			// zero new radars because another front already carries extras.
			var overServedA = BaseFrontBackPlannerBotModule.AggregateRadarTarget(Owned, new[] { 1, 1 }, new[] { 2, 0 });
			Assert.That(overServedA - Owned, Is.EqualTo(1));
		}

		[Test]
		public void PickRequiresFirstReachAndExtraThreshold()
		{
			// B4: a first provider must positively reach the approach; an extra must meet the threshold.
			Assert.That(BaseFrontBackPlannerBotModule.RequiredNewCoverage(0, 30), Is.EqualTo(1));
			Assert.That(BaseFrontBackPlannerBotModule.RequiredNewCoverage(1, 30), Is.EqualTo(30));
			Assert.That(BaseFrontBackPlannerBotModule.RequiredNewCoverage(2, 30), Is.EqualTo(30));
		}

		[Test]
		public void ClusterDefencesKeepsOnlyOwnTowers()
		{
			// B2: the perimeter handed to a cluster's fronts is membership-filtered.
			var cluster = new HashSet<CPos> { new(50, 50), new(51, 50), new(52, 50) };
			var defences = new List<CPos> { new(51, 50), new(90, 90), new(52, 50) };
			Assert.That(BaseFrontBackPlannerBotModule.ClusterDefences(defences, cluster),
				Is.EqualTo(new List<CPos> { new(51, 50), new(52, 50) }));
		}

		[Test]
		public void RemoteTowersWouldMoveTheLineIfNotFiltered()
		{
			// B2 regression shape: feeding remote towers into the same BuildFronts call moves the line —
			// the cluster filter is what prevents the polluted input ever reaching here.
			var anchors = new List<(CPos, FrontAnchorKind)> { (new CPos(90, 50), FrontAnchorKind.EnemySpawn) };
			var cluster = new List<CPos> { new(50, 50), new(51, 50) };
			var home = BaseFrontBackPlannerBotModule.BuildFronts(Center, anchors,
				new List<CPos> { new(58, 50) }, cluster, Merge45, 3)[0];
			var polluted = BaseFrontBackPlannerBotModule.BuildFronts(Center, anchors,
				new List<CPos> { new(58, 50), new(140, 50) }, cluster, Merge45, 3)[0];
			Assert.That(home.RearProj, Is.EqualTo(8));
			Assert.That(home.FrontProj, Is.EqualTo(8));
			Assert.That(polluted.FrontProj, Is.GreaterThan(home.FrontProj));
		}

		[Test]
		public void FallbackNeverReturnsAForwardRadar()
		{
			// B3: defended front — only cells strictly behind the rearmost defence are safe;
			// an all-forward candidate set yields nothing (the caller then Holds).
			var front = EastFront(10, 5);
			Assert.That(BaseFrontBackPlannerBotModule.SafeBackCells(
				new List<CPos> { new(60, 50), new(70, 50) }, Center, front), Is.Empty);
			Assert.That(BaseFrontBackPlannerBotModule.SafeBackCells(
				new List<CPos> { new(53, 50), new(60, 50), new(48, 50) }, Center, front),
				Is.EqualTo(new List<CPos> { new(53, 50), new(48, 50) }));
		}

		[Test]
		public void UndefendedFallbackStaysAtOrBehindCentre()
		{
			// B3: a front with no line — cells ahead of centre (proj > 0) are forward, not back.
			var front = new BaseFront { Id = 0, Centre = Center, DirX = 1, DirY = 0, Anchor = new CPos(90, 50) };
			Assert.That(BaseFrontBackPlannerBotModule.SafeBackCells(
				new List<CPos> { new(55, 50), new(51, 50) }, Center, front), Is.Empty);
			Assert.That(BaseFrontBackPlannerBotModule.SafeBackCells(
				new List<CPos> { new(50, 50), new(45, 50) }, Center, front),
				Is.EqualTo(new List<CPos> { new(50, 50), new(45, 50) }));
		}

		[Test]
		public void ExtraThresholdComparesCandidateDeltaNotUncoveredTotal()
		{
			// B4: 40 uncovered approach cells does not licence an extra whose best band pick
			// adds fewer than the threshold — NewCoverageCells is what gets compared.
			var front = EastFront(10, 5);
			var approach = new List<CPos>();
			for (var i = 0; i < 40; i++)
				approach.Add(new CPos(60 + i % 10, 46 + i / 10));

			var pick = BaseFrontBackPlannerBotModule.ChooseRadarCell(
				new List<CPos> { new(53, 50) }, Center, front, approach, new HashSet<CPos>(), 2, 2, 8);
			Assert.That(pick.Cell, Is.Not.Null);
			Assert.That(pick.NewCoverageCells, Is.EqualTo(0));
			Assert.That(pick.NewCoverageCells, Is.LessThan(30));
		}
	}
}
