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
		const string PipelineFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs";
		const string QmFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs";
		const string SovietBuildings = "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml";

		static CoreOnceInput T(string name, int depth = 1, int role = 100, int cost = 1000, int count = 0, bool buildable = true, bool inFractions = true,
			bool firstCopy = false, bool ownRule = false) =>
			new(name, buildable, inFractions, firstCopy, ownRule, count, depth, role, cost);

		static IReadOnlyDictionary<string, IReadOnlyList<string>> Map(params (string Key, string[] Values)[] rows) =>
			rows.ToDictionary(r => r.Key, r => (IReadOnlyList<string>)r.Values, StringComparer.Ordinal);

		sealed class FakeFeasibility : IBaseBuildFeasibility
		{
			public readonly HashSet<string> Infeasible = new(StringComparer.Ordinal);
			public readonly List<string> Tested = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => true;

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;
		}

		[Test]
		public void TheOrderIsDepthThenOpeningRoleThenCostThenName()
		{
			var order = CoreOnceCA.Order(
			[
				T("deep", depth: 3, role: 1, cost: 100),
				T("shallowExpensive", depth: 1, role: 100, cost: 900),
				T("shallowRadar", depth: 1, role: CoreOnceCA.RoleRank(false, false, false, false, true, false, false), cost: 1500),
				T("shallowBarracks", depth: 1, role: CoreOnceCA.RoleRank(false, true, false, false, false, false, false), cost: 2000),
				T("b", depth: 2, cost: 500),
				T("a", depth: 2, cost: 500),
				T("c", depth: 2, cost: 400),
			]);

			// depth 1: barracks (role 2), radar (role 5), then the role-less one; depth 2: cheaper first, then the ordinal name; depth 3 last.
			Assert.That(order, Is.EqualTo(new[] { "shallowBarracks", "shallowRadar", "shallowExpensive", "c", "a", "b", "deep" }));
		}

		[Test]
		public void TheOrderIsDeterministicWhateverTheInputOrder()
		{
			var set = new[] { T("z", 1, 100, 10), T("y", 1, 100, 10), T("x", 2, 100, 10), T("w", 0, 100, 10) };
			var expected = CoreOnceCA.Order(set);
			Assert.That(expected, Is.EqualTo(new[] { "w", "y", "z", "x" }));
			Assert.That(CoreOnceCA.Order(set.Reverse()), Is.EqualTo(expected));
			Assert.That(CoreOnceCA.Order(null), Is.Empty);
		}

		[Test]
		public void RoleRanksFollowTheOpeningOrderAndAnythingElseComesLast()
		{
			Assert.That(CoreOnceCA.RoleRank(true, false, false, false, false, false, false), Is.EqualTo((int)OpeningRole.Power));
			Assert.That(CoreOnceCA.RoleRank(false, true, false, false, false, false, false), Is.EqualTo((int)OpeningRole.Barracks));
			Assert.That(CoreOnceCA.RoleRank(false, false, true, false, false, false, false), Is.EqualTo((int)OpeningRole.Refinery));
			Assert.That(CoreOnceCA.RoleRank(false, false, false, true, false, false, false), Is.EqualTo((int)OpeningRole.WarFactory));
			Assert.That(CoreOnceCA.RoleRank(false, false, false, false, true, false, false), Is.EqualTo((int)OpeningRole.Radar));
			Assert.That(CoreOnceCA.RoleRank(false, false, false, false, false, true, false), Is.EqualTo((int)OpeningRole.Airfield));
			Assert.That(CoreOnceCA.RoleRank(false, false, false, false, false, false, true), Is.EqualTo((int)OpeningRole.PassiveIncome));
			Assert.That(CoreOnceCA.RoleRank(false, false, false, false, false, false, false), Is.EqualTo(100));
		}

		[Test]
		public void OnceOnlyAnOwnedOrInFlightTypeIsExcluded()
		{
			var order = CoreOnceCA.Order([T("none", count: 0), T("owned", count: 1), T("inflightAndOwned", count: 3), T("also", count: 0, inFractions: false, firstCopy: true)]);
			Assert.That(order, Is.EquivalentTo(new[] { "none", "also" }));
		}

		[Test]
		public void DefenceWallAndProxyTypesAreInTheSetOnlyThroughBuildingFractions()
		{
			// FirstCopyKind is false for a defence, wall or proxy (UnlistedOnceCA.Eligible); a BuildingFractions row still lists the type.
			Assert.That(CoreOnceCA.InSet(T("turret", inFractions: false, firstCopy: false)), Is.False);
			Assert.That(CoreOnceCA.InSet(T("wall", inFractions: false, firstCopy: false)), Is.False);
			Assert.That(CoreOnceCA.InSet(T("proxy", inFractions: false, firstCopy: false)), Is.False);
			Assert.That(CoreOnceCA.InSet(T("turret", inFractions: true, firstCopy: false)), Is.True);
			Assert.That(CoreOnceCA.InSet(T("lab", inFractions: false, firstCopy: true)), Is.True);

			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, true, "turret"), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, true, false, false, "wall"), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, "ra1_proxy_thing"), Is.False);
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, "lab"), Is.True);
		}

		[Test]
		public void TypesWithTheirOwnStageAndNamelessTypesAreNotInTheSet()
		{
			Assert.That(CoreOnceCA.InSet(T("proc", ownRule: true)), Is.False, "construction yard and refinery keep S2 and S7");
			Assert.That(CoreOnceCA.InSet(T("name", buildable: true)), Is.True);
			Assert.That(CoreOnceCA.InSet(new CoreOnceInput(null, true, true, true, false, 0, 0, 0, 0)), Is.False);
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
		public void TheCoreStageComesBeforeFractionExtras()
		{
			var s = new BaseBuildSnapshot
			{
				Bank = 5000,
				Owned = new Dictionary<string, int>(),
				InFlight = new Dictionary<string, int>(),
				BuildingCount = 10,
				Fractions = new[] { new KeyValuePair<string, int>("extra", 50) },
				StageSource = stage => stage == BaseBuildStage.S5CoreOnce ? new[] { "core" } : null,
			};

			var pick = BaseBuildPlanCA.Plan(s, new FakeFeasibility(), new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("core"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
		}

		[Test]
		public void TheWiringIsPinnedAndTheOldLadderStillRuns()
		{
			var stages = Source(StagesFile);
			Assert.That(stages, Does.Contain("case BaseBuildStage.S5CoreOnce: return PipelineCoreOnceStage(buildable, snapshot);"));
			Assert.That(stages, Does.Contain("CoreOnceCA.Order(inputs)"));
			Assert.That(stages, Does.Contain("TechDepthCA.Compute(prereqsOf, providersOf, baseBuilder.Info.ConstructionYardTypes)"));
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
