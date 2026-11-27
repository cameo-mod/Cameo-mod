#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-01 c3 (CONSOLIDATE_01_DESIGN.md 3.1 stage S5, section 4 row C3): S5 "every listed building once" and the tech depth derived from the
	// prerequisite graph. The pure rules (CoreOnceCA, TechDepthCA) are tested directly, the stage through BaseBuildPlanCA with a stand-in for A1-A7, and the
	// world-dependent wiring by pins. The switch bb_pipeline_01 stays off by default; the old ladder is untouched.
	[TestFixture]
	public sealed class Consolidate01C3Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		const string StagesFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Stages.cs";
		const string PlanFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuildPlanCA.cs";
		const string DefenceFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Defence.cs";
		const string PipelineFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs";
		const string QmFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs";
		const string SovietBuildings = "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml";

		// Weight: -1 = not in BuildingFractions. The default is the fallback's: a first-copy kind that is not listed.
		static CoreOnceInput T(string name, int depth = 1, int cost = 1000, int count = 0, bool buildable = true, int weight = -1,
			bool firstCopy = true, bool ownRule = false) =>
			new(name, buildable, weight, firstCopy, ownRule, count, depth, cost);

		static BaseBuildSnapshot Snap(IEnumerable<KeyValuePair<string, int>> fractions, Func<BaseBuildStage, IEnumerable<string>> source = null,
			int buildingCount = 10, bool defence = false) => new()
		{
			Bank = 5000,
			Owned = new Dictionary<string, int>(),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = buildingCount,
			DefenceQueue = defence,
			Fractions = fractions?.ToArray(),
			StageSource = source,
		};

		static KeyValuePair<string, int> W(string name, int weight) => new(name, weight);

		static List<string> S5Names(BaseBuildSnapshot s, int seed) =>
			BaseBuildPlanCA.StageCandidates(BaseBuildStage.S5CoreOnce, s, new MersenneTwister(seed)).Select(c => c.Name).ToList();

		static IReadOnlyDictionary<string, IReadOnlyList<string>> Map(params (string Key, string[] Values)[] rows) =>
			rows.ToDictionary(r => r.Key, r => (IReadOnlyList<string>)r.Values, StringComparer.Ordinal);

		sealed class FakeFeasibility : IBaseBuildFeasibility
		{
			public readonly HashSet<string> Infeasible = new(StringComparer.Ordinal);
			public readonly List<string> Tested = new();

			// The types this player can build (null = all): a doctrine alternative outside it is skipped by the stage caps, at no test cost.
			public HashSet<string> Buildable;

			public bool StageAdmits(BaseBuildCandidate candidate) => Buildable == null || Buildable.Contains(candidate.Name);

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;
		}

		[Test]
		public void TheFallbackOrderIsDepthThenCostThenName()
		{
			var order = CoreOnceCA.Order(
			[
				T("deep", depth: 3, cost: 100),
				T("shallowExpensive", depth: 1, cost: 900),
				T("shallowCheap", depth: 1, cost: 300),
				T("b", depth: 2, cost: 500),
				T("a", depth: 2, cost: 500),
				T("c", depth: 2, cost: 400),
			]);

			Assert.That(order, Is.EqualTo(new[] { "shallowCheap", "shallowExpensive", "c", "a", "b", "deep" }));
		}

		[Test]
		public void TheFallbackOrderIsDeterministicWhateverTheInputOrder()
		{
			var set = new[] { T("z", 1, 10), T("y", 1, 10), T("x", 2, 10), T("w", 0, 10) };
			var expected = CoreOnceCA.Order(set);
			Assert.That(expected, Is.EqualTo(new[] { "w", "y", "z", "x" }));
			Assert.That(CoreOnceCA.Order(set.Reverse()), Is.EqualTo(expected));
			Assert.That(CoreOnceCA.Order(null), Is.Empty);
		}

		[Test]
		public void OnceOnlyAnOwnedOrInFlightTypeIsExcluded()
		{
			var order = CoreOnceCA.Order([T("none", count: 0), T("owned", count: 1), T("inflightAndOwned", count: 3), T("zero", count: 0, weight: 0, firstCopy: true)]);
			Assert.That(order, Is.EquivalentTo(new[] { "none", "zero" }));
		}

		[Test]
		public void TheFallbackTakesOnlyWhatTheRollCannotOffer()
		{
			// Weight 0 (listed, never rolled into S5) and an unlisted type are the fallback's only when they are a first-copy kind; a positive weight is the roll's.
			Assert.That(CoreOnceCA.InSet(T("zeroWeight", weight: 0, firstCopy: false)), Is.False, "a listed weight-0 wall or defence is not built once by S5");
			Assert.That(CoreOnceCA.InSet(T("zeroWeightFirstCopy", weight: 0, firstCopy: true)), Is.True);
			Assert.That(CoreOnceCA.InSet(T("unlisted", weight: -1, firstCopy: true)), Is.True);
			Assert.That(CoreOnceCA.InSet(T("positive", weight: 5, firstCopy: true)), Is.False, "no duplicate of the roll");
			Assert.That(CoreOnceCA.InSet(T("positive", weight: 1, firstCopy: false)), Is.False);

			// FirstCopyKind is false for a defence, wall or proxy (UnlistedOnceCA.Eligible): unlisted, they are in no set.
			Assert.That(CoreOnceCA.InSet(T("turret", weight: -1, firstCopy: false)), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, true, "turret"), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, true, false, false, "wall"), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, "ra1_proxy_thing"), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, "lab"), Is.True);
		}

		[Test]
		public void TypesWithTheirOwnStageAndNamelessTypesAreNotInTheFallback()
		{
			Assert.That(CoreOnceCA.InSet(T("proc", ownRule: true)), Is.False, "construction yard and refinery keep S2 and S7");
			Assert.That(CoreOnceCA.InSet(T("name", buildable: true)), Is.True);
			Assert.That(CoreOnceCA.InSet(new CoreOnceInput(null, true, 0, true, false, 0, 0, 0)), Is.False);
		}

		[Test]
		public void AnUnbuiltFractionTypeIsAlwaysOfferedByS5WhateverTheBuildingCount()
		{
			foreach (var count in new[] { 0, 10, 500 })
			{
				foreach (var weight in new[] { 1, 5, 100 })
				{
					var s = Snap([W("lab", weight)], buildingCount: count);
					Assert.That(S5Names(s, 1), Is.EqualTo(new[] { "lab" }), "count " + count + ", weight " + weight);
				}
			}
		}

		[Test]
		public void TheFractionWeightsDecideS5Eligibility()
		{
			var s = Snap([W("rolled", 10), W("zero", 0), W("built", 10)], stage => stage == BaseBuildStage.S5CoreOnce ? CoreOnceCA.Order([T("zero", weight: 0), T("late", depth: 5)]) : null);
			s.Owned = new Dictionary<string, int> { ["built"] = 1 };

			var names = S5Names(s, 3);
			Assert.That(names, Is.EqualTo(new[] { "rolled", "zero", "late" }), "the positive weight goes to the roll, weight 0 only to the depth fallback after it");
			Assert.That(names, Does.Not.Contain("built"), "a type with a first copy is in neither");

			s.InFlight = new Dictionary<string, int> { ["rolled"] = 1 };
			Assert.That(S5Names(s, 3), Is.EqualTo(new[] { "zero", "late" }), "in flight counts as built");
		}

		[Test]
		public void TheDoctrinePairCountsAsOne()
		{
			// Both twins are listed; only the one this player can build passes StageAdmits (buildableNames), so exactly one of the pair is picked and,
			// once it stands, the other is never offered as a missing core building.
			var pair = new[] { W("ra1_soviets_largefactory", 10), W("ra1_soviets_warfactory", 10) };
			foreach (var buildable in new[] { "ra1_soviets_largefactory", "ra1_soviets_warfactory" })
			{
				var s = Snap(pair);
				var f = new FakeFeasibility { Buildable = new HashSet<string>(StringComparer.Ordinal) { buildable } };
				var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(2));
				Assert.That(pick.Name, Is.EqualTo(buildable));
				Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
				Assert.That(f.Tested, Is.EqualTo(new[] { buildable }), "the unbuildable twin costs no test");

				s.Owned = new Dictionary<string, int> { [buildable] = 1 };
				f = new FakeFeasibility { Buildable = new HashSet<string>(StringComparer.Ordinal) { buildable } };
				Assert.That(BaseBuildPlanCA.Plan(s, f, new MersenneTwister(2)).Stage, Is.Not.EqualTo(BaseBuildStage.S5CoreOnce));
				Assert.That(f.Tested, Does.Not.Contain(pair.Select(p => p.Key).Single(k => k != buildable)));
			}
		}

		[Test]
		public void S5AndS10DrawFromTheSameRollCode()
		{
			var plan = Source(PlanFile);
			Assert.That(plan, Does.Contain("return RollCandidates(s, rng, BaseBuildStage.S10Fractions);"));
			Assert.That(plan, Does.Contain("foreach (var c in RollCandidates(s, rng, BaseBuildStage.S5CoreOnce))"));
			Assert.That(plan, Does.Contain("static IEnumerable<BaseBuildCandidate> RollCandidates(BaseBuildSnapshot s, MersenneTwister rng, BaseBuildStage stage)"));
			Assert.That(plan, Does.Not.Contain("FractionCandidates"));

			var shuffles = plan.Split(new[] { "s.Fractions.OrderBy" }, StringSplitOptions.None).Length - 1;
			Assert.That(shuffles, Is.EqualTo(1), "one shuffle of s.Fractions in BaseBuildPlanCA, inside RollCandidates");
			Assert.That(plan.Split(new[] { ".Shuffle(rng)" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
		}

		[Test]
		public void S5OffersNothingOnTheDefenceQueue()
		{
			var s = Snap([W("lab", 10)], _ => new[] { "fallback" }, defence: true);
			Assert.That(S5Names(s, 1), Is.Empty);
			s.DefenceQueue = false;
			Assert.That(S5Names(s, 1), Is.EqualTo(new[] { "lab", "fallback" }));
		}

		[Test]
		public void TheSameSeedGivesTheSameS5Order()
		{
			var fractions = Enumerable.Range(0, 12).Select(i => W("b" + i.ToString("D2"), 5 + i)).ToArray();
			var a = S5Names(Snap(fractions), 77);
			Assert.That(a, Has.Count.EqualTo(12));
			Assert.That(S5Names(Snap(fractions), 77), Is.EqualTo(a));
			Assert.That(S5Names(Snap(fractions.Reverse()), 77), Is.EqualTo(a), "keys are sorted ordinally before the shuffle");
			Assert.That(a, Is.EquivalentTo(fractions.Select(x => x.Key)));
		}

		[Test]
		public void ADoctrineAlternativeThePlayerCannotBuildIsAbsentNotMissing()
		{
			// Industrial efficiency picked: the large factory is buildable, the war factory is not (and the other way round). Either way exactly one is in
			// the set and the unbuildable twin is neither offered nor reported.
			var picked = CoreOnceCA.Order([T("ra1_soviets_largefactory", depth: 2), T("ra1_soviets_warfactory", depth: 2, buildable: false)]);
			Assert.That(picked, Is.EqualTo(new[] { "ra1_soviets_largefactory" }));

			var notPicked = CoreOnceCA.Order([T("ra1_soviets_largefactory", depth: 2, buildable: false), T("ra1_soviets_warfactory", depth: 2)]);
			Assert.That(notPicked, Is.EqualTo(new[] { "ra1_soviets_warfactory" }));

			var airfield = CoreOnceCA.Order([T("ra1_soviets_largesovietairfield", buildable: false), T("ra1_soviets_airfield")]);
			Assert.That(airfield, Is.EqualTo(new[] { "ra1_soviets_airfield" }));
		}

		[Test]
		public void TheSovietDoctrineGateIsPinnedInTheYaml()
		{
			var yaml = RepoFile(SovietBuildings);
			const string gate = "ra1_soviets_doctrine_industrialefficiency";
			Assert.That(PrerequisitesOf(yaml, "ra1_soviets_largefactory"), Does.Contain("~" + gate).And.Not.Contain("~!"));
			Assert.That(PrerequisitesOf(yaml, "ra1_soviets_largesovietairfield"), Does.Contain("~" + gate).And.Not.Contain("~!"));
			Assert.That(PrerequisitesOf(yaml, "ra1_soviets_warfactory"), Does.Contain("~!" + gate));
			Assert.That(PrerequisitesOf(yaml, "ra1_soviets_airfield"), Does.Contain("~!" + gate));

			// The depth rule on exactly those tokens: the negation adds no depth, the positive doctrine token is not provided by any building.
			Assert.That(TechDepthCA.Positive("~!" + gate), Is.Null);
			Assert.That(TechDepthCA.Positive("~" + gate), Is.EqualTo(gate));
		}

		// The first "Prerequisites:" line of one actor block (up to the next top-level key).
		static string PrerequisitesOf(string yaml, string actor)
		{
			var start = yaml.IndexOf("\n" + actor + ":\n", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), actor);
			var rest = yaml[(start + actor.Length + 3)..];
			var block = new List<string>();
			foreach (var line in rest.Split('\n'))
			{
				if (line.Length > 0 && !char.IsWhiteSpace(line[0]) && line[0] != '#')
					break;

				block.Add(line);
			}

			var prereq = block.FirstOrDefault(l => l.TrimStart().StartsWith("Prerequisites:", StringComparison.Ordinal));
			Assert.That(prereq, Is.Not.Null, actor + " has a Prerequisites line");
			return prereq;
		}

		[Test]
		public void PrerequisiteTokensStripTheTildeAndIgnoreNegationsAndBlanks()
		{
			Assert.That(TechDepthCA.Positive("~ra1_soviets_constructionyard"), Is.EqualTo("ra1_soviets_constructionyard"));
			Assert.That(TechDepthCA.Positive("RA1_PowerPlant"), Is.EqualTo("ra1_powerplant"));
			Assert.That(TechDepthCA.Positive("!x"), Is.Null);
			Assert.That(TechDepthCA.Positive("~!x"), Is.Null);
			Assert.That(TechDepthCA.Positive("~"), Is.Null);
			Assert.That(TechDepthCA.Positive(""), Is.Null);
			Assert.That(TechDepthCA.Positive(null), Is.Null);
		}

		[Test]
		public void TheIxianChainOutpostResearchCentreHighTechDeepensByOneEach()
		{
			var prereqs = Map(
				("yard", ["~rafact"]),
				("power", ["~yard"]),
				("outpost", ["~yard", "power"]),
				("research", ["outpost"]),
				("hightech", ["research", "power"]));
			var providers = Map(
				("yard", ["yard"]),
				("power", ["power"]),
				("outpost", ["outpost"]),
				("research", ["research"]),
				("hightech", ["hightech"]));

			var depth = TechDepthCA.Compute(prereqs, providers, ["yard"]);
			Assert.That(depth["yard"], Is.EqualTo(0));
			Assert.That(depth["power"], Is.EqualTo(1));
			Assert.That(depth["outpost"], Is.EqualTo(2));
			Assert.That(depth["research"], Is.EqualTo(3));
			Assert.That(depth["hightech"], Is.EqualTo(4));

			var order = CoreOnceCA.Order(new[] { "hightech", "research", "outpost", "power" }.Select(n => T(n, depth: depth[n])));
			Assert.That(order, Is.EqualTo(new[] { "power", "outpost", "research", "hightech" }));
		}

		[Test]
		public void ADepthTakesTheNearestProviderAndIgnoresNegationsAndUnprovidedTokens()
		{
			var prereqs = Map(
				("yard", []),
				("tech", ["yard"]),
				("lab", ["tech"]),
				("a", ["~!doctrine", "~doctrine", "~anything"]),
				("b", ["vehicleproduction"]),
				("factorySmall", ["yard"]),
				("factoryBig", ["lab"]));
			var providers = Map(
				("yard", ["yard"]),
				("tech", ["tech"]),
				("lab", ["lab"]),
				("vehicleproduction", ["factoryBig", "factorySmall"]));

			var depth = TechDepthCA.Compute(prereqs, providers, ["yard"]);
			Assert.That(depth["a"], Is.EqualTo(0), "doctrine tokens: negation ignored, upgrade not provided by a building");
			Assert.That(depth["factorySmall"], Is.EqualTo(1));
			Assert.That(depth["factoryBig"], Is.EqualTo(3));
			Assert.That(depth["b"], Is.EqualTo(2), "either provider meets it: the nearest one counts");
		}

		[Test]
		public void ACycleAndASelfProviderTerminate()
		{
			var prereqs = Map(("x", ["y"]), ("y", ["x"]), ("self", ["self"]));
			var providers = Map(("x", ["x"]), ("y", ["y"]), ("self", ["self"]));
			var depth = TechDepthCA.Compute(prereqs, providers, []);
			Assert.That(depth.Keys, Is.EquivalentTo(new[] { "x", "y", "self" }));
			Assert.That(depth["self"], Is.EqualTo(0));
			Assert.That(TechDepthCA.Compute(prereqs, providers, []), Is.EqualTo(depth), "deterministic");
		}

		[Test]
		public void TheCoreStageOffersTheOrderedSetAndFallsThroughARefusedType()
		{
			var s = new BaseBuildSnapshot
			{
				Bank = 5000,
				Owned = new Dictionary<string, int>(),
				InFlight = new Dictionary<string, int>(),
				BuildingCount = 10,
				StageSource = stage => stage == BaseBuildStage.S5CoreOnce ? CoreOnceCA.Order([T("deep", depth: 3), T("near", depth: 1), T("mid", depth: 2)]) : null,
			};
			var f = new FakeFeasibility();
			f.Infeasible.Add("near");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("mid"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
			Assert.That(f.Tested, Is.EqualTo(new[] { "near", "mid" }));
		}

		[Test]
		public void TheRollFirstThenTheFallbackAndBothBeforeTheS10Roll()
		{
			var s = Snap([W("extra", 50)], stage => stage == BaseBuildStage.S5CoreOnce ? new[] { "core" } : null);
			var f = new FakeFeasibility();
			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("extra"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));

			f.Infeasible.Add("extra");
			pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("core"), "the depth fallback follows the roll");
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
		}

		[Test]
		public void S5HandsOverToTheS10RollOnceEveryListedTypeIsBuiltOrRefused()
		{
			// "a" is built once, "b" is not: S5 offers only b. b refused: the S10 roll offers a (count 1 passes the share), b is not tested again.
			var s = Snap([W("a", 50), W("b", 50)]);
			s.Owned = new Dictionary<string, int> { ["a"] = 1 };
			var f = new FakeFeasibility();
			f.Infeasible.Add("b");
			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("a"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
			Assert.That(f.Tested, Is.EqualTo(new[] { "b", "a" }));

			// Every listed type built: S5 offers nothing, the pick is the S10 roll's.
			s.Owned = new Dictionary<string, int> { ["a"] = 1, ["b"] = 1 };
			var g = new FakeFeasibility();
			pick = BaseBuildPlanCA.Plan(s, g, new MersenneTwister(1));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
			Assert.That(S5Names(s, 1), Is.Empty);
		}

		[Test]
		public void TheWiringIsPinnedAndTheOldLadderStillRuns()
		{
			var stages = Source(StagesFile);
			Assert.That(stages, Does.Contain("case BaseBuildStage.S5CoreOnce: return PipelineCoreOnceStage(queue, buildable, snapshot);"));
			Assert.That(stages, Does.Contain("CoreOnceCA.Order(inputs)"));
			Assert.That(stages, Does.Not.Contain("RoleRank"));
			Assert.That(stages, Does.Contain("weight = Math.Max(0, listed);"));

			var defence = Source(DefenceFile);
			Assert.That(defence, Does.Contain("case BaseBuildStage.S5CoreOnce:\n"));
			Assert.That(defence, Does.Contain("return buildableNames.Contains(candidate.Name)"));
			Assert.That(defence, Does.Contain("&& !baseBuilder.Info.ConstructionYardTypes.Contains(candidate.Name)").And.Contain("(!baseBuilder.Info.RefineryTypes.Contains(candidate.Name) || (baseBuilder.CaptureUnlockArmed && captureUnlockOffered.Contains(candidate.Name)))"));

			var plan = Source(PlanFile);
			Assert.That(plan, Does.Contain("case BaseBuildStage.S5CoreOnce: return CoreOnceCandidates(s, rng);"));
			Assert.That(plan, Does.Contain("if (stage == BaseBuildStage.S5CoreOnce && (frac.Value <= 0 || CountOf(s, frac.Key) != 0))"));
			Assert.That(stages, Does.Contain("baseBuilder.RuleMetadata.CoreDepths(player.Faction.InternalName, baseBuilder.Info.ConstructionYardTypes)"));
			var metadata = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderRuleMetadataCA.cs");
			Assert.That(metadata, Does.Contain("TechDepthCA.Compute(prereqsOf, providersOf, constructionYardTypes)"));
			Assert.That(stages, Does.Contain("if (snapshot.DefenceQueue)"), "the defence queue gets no core set");
			Assert.That(stages, Does.Contain("FirstCopyKind(item)"));
			Assert.That(stages, Does.Not.Contain("TechChainTypes.Contains"), "the pipeline reads no TechChainTypes; the derived depth replaces it");

			var pipeline = Source(PipelineFile);
			Assert.That(pipeline, Does.Contain("s.StageSource = stage => PipelineStageNames(stage, queue, buildable, power, law, s);"));
			Assert.That(pipeline, Does.Contain("!BuildingStillDelayed(name)"), "A4: BuildingDelays apply to every stage, S5 included (X14)");

			// Pipeline off: the ladder and its folded rules are still there, called as before.
			var qm = Source(QmFile);
			Assert.That(qm, Does.Contain("return TickQueuePlanned(bot, queue);"));
			Assert.That(qm, Does.Contain("var unlisted = ChooseUnlistedOnce(queue, buildableThings, power);"));
			Assert.That(qm, Does.Contain("var keptEssential = ChooseFirstCopyOnce(queue, buildableThings, power, true);"));
			Assert.That(qm, Does.Contain("bool TechChainExempt(string name) =>"));
			Assert.That(qm, Does.Contain("EssentialsLowCashCA.Holds("));
			Assert.That(qm, Does.Contain("bool FirstCopyKind(ActorInfo info)"));
			Assert.That(qm, Does.Not.Contain("CoreOnceCA"), "the classic ladder does not know S5");

			Assert.That(Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs"), Does.Contain("public IReadOnlyDictionary<string, int> CoreTechDepth;"));
		}
	}
}
