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
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-01 c2 (CONSOLIDATE_01_DESIGN.md 3.1, row bb_pipeline_01): the pure staged planner BaseBuildPlanCA. A candidate that fails its
	// stage caps or A1-A7 falls through to the next candidate, then to the next stage; at most K = 8 tests per producer per sweep; the same
	// snapshot and seed give the same pick and the same number of random draws. The switch is off by default and no yaml arms it (C7 does).
	[TestFixture]
	public sealed class Consolidate01C2Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string QmFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs";
		const string PipelineFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs";
		const string PlanFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuildPlanCA.cs";

		// A1-A7 stand-in: names in Infeasible fail A1-A6, names in CapHeld fail their stage caps, names in ClaimRefused fail A7.
		sealed class FakeFeasibility : IBaseBuildFeasibility
		{
			public readonly HashSet<string> Infeasible = new(StringComparer.Ordinal);
			public readonly HashSet<string> CapHeld = new(StringComparer.Ordinal);
			public readonly HashSet<string> ClaimRefused = new(StringComparer.Ordinal);
			public readonly Dictionary<string, string> PowerSwap = new(StringComparer.Ordinal);
			public readonly List<string> Tested = new();
			public readonly List<string> ClaimTried = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => !CapHeld.Contains(candidate.Name);

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : PowerSwap.TryGetValue(candidate.Name, out var swap) ? swap : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted)
			{
				ClaimTried.Add(admitted);
				return !ClaimRefused.Contains(admitted);
			}
		}

		static BaseBuildSnapshot Snapshot(params string[] fractions) => new()
		{
			Bank = 5000,
			// Each listed type already stands once: S5 (first copies) offers none of them, so these tests exercise the S10 roll.
			Owned = fractions.Distinct().ToDictionary(f => f, _ => 1),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = fractions.Select(f => new KeyValuePair<string, int>(f, 10)).ToArray(),
		};

		static void PowerShortNow(BaseBuildSnapshot s)
		{
			s.HasPowerManager = true;
			s.ExcessPower = -50;
			s.PowerMargin = 100;
			s.PowerType = "powr";
			s.PowerTypeAmount = 100;
		}

		// Power 1 (owned), Barracks 1, Refinery 1, War factory 1 with the given picks (null = the faction has none).
		static void Opening(BaseBuildSnapshot s, string barracks, string refinery, string factory, List<string> stuckAsked = null)
		{
			var status = new Dictionary<OpeningRole, (string, int)>
			{
				[OpeningRole.Power] = ("powr", 1),
				[OpeningRole.Barracks] = (barracks, 0),
				[OpeningRole.Refinery] = (refinery, 0),
				[OpeningRole.WarFactory] = (factory, 0),
			};

			s.OpeningActive = true;
			s.OpeningOrder = [new(OpeningRole.Power, 1), new(OpeningRole.Barracks, 1), new(OpeningRole.Refinery, 1), new(OpeningRole.WarFactory, 1)];
			s.OpeningStatus = role => status.TryGetValue(role, out var st) ? st : (null, 0);
			s.OpeningStuck = (role, required) =>
			{
				stuckAsked?.Add(role.ToString());
				return false;
			};
		}

		[Test]
		public void AHeldOpeningStepFallsThroughToTheNextStepInsteadOfStallingTheSweep()
		{
			// X2: the old ladder returned the barracks, a post-pick veto held it and the sweep returned false. Now the next open step goes.
			var s = Snapshot();
			Opening(s, "barr", "proc", "weap");
			var f = new FakeFeasibility();
			f.Infeasible.Add("barr");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("proc"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S3Opening));
			Assert.That(pick.Tests, Is.EqualTo(2));
			Assert.That(f.Tested, Is.EqualTo(new[] { "barr", "proc" }));
		}

		[Test]
		public void AFailedStageFallsThroughToTheNextStage()
		{
			var s = Snapshot("barr");
			PowerShortNow(s);
			var f = new FakeFeasibility();
			f.Infeasible.Add("powr");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
			Assert.That(f.Tested, Is.EqualTo(new[] { "powr", "barr" }));
		}

		[Test]
		public void AStageCapHoldCostsNoTestAndARefusedClaimFallsThrough()
		{
			var s = Snapshot();
			Opening(s, "barr", "proc", "weap");
			var f = new FakeFeasibility();
			f.CapHeld.Add("barr");
			f.ClaimRefused.Add("proc");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("weap"));
			Assert.That(f.Tested, Is.EqualTo(new[] { "proc", "weap" }), "the cap-held barracks is never tested");
			Assert.That(f.ClaimTried, Is.EqualTo(new[] { "proc", "weap" }), "A7 runs only after A1-A6 passed");
			Assert.That(pick.Tests, Is.EqualTo(2));
		}

		[Test]
		public void AnInfeasibleCandidateIsNeverClaimed()
		{
			var s = Snapshot("gtwr");
			var f = new FakeFeasibility();
			f.Infeasible.Add("gtwr");

			Assert.That(BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1)).IsNone, Is.True);
			Assert.That(f.ClaimTried, Is.Empty);
		}

		[Test]
		public void APowerSwapStartsThePlantButReportsTheCandidate()
		{
			var s = Snapshot("fix");
			var f = new FakeFeasibility();
			f.PowerSwap["fix"] = "powr";

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("powr"));
			Assert.That(pick.Candidate, Is.EqualTo("fix"));
			Assert.That(f.ClaimTried, Is.EqualTo(new[] { "powr" }), "the claim is for the type that starts");
		}

		[Test]
		public void AtMostEightTestsPerProducerPerSweep()
		{
			Assert.That(BaseBuildPlanCA.MaxFeasibilityTests, Is.EqualTo(8));
			var names = Enumerable.Range(0, 20).Select(i => "b" + i.ToString("D2")).ToArray();
			var s = Snapshot(names);
			var f = new FakeFeasibility();
			f.Infeasible.UnionWith(names);

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(7));
			Assert.That(pick.IsNone, Is.True);
			Assert.That(pick.Tests, Is.EqualTo(8));
			Assert.That(f.Tested, Has.Count.EqualTo(8));
		}

		[Test]
		public void TheBoundCountsTestsAcrossStagesAndATypeThatFailedIsNotTestedAgain()
		{
			var s = Snapshot(Enumerable.Range(0, 10).Select(i => "x" + i).ToArray());
			PowerShortNow(s);
			Opening(s, "barr", "proc", "weap");
			s.OpeningStatus = role => role == OpeningRole.Power ? ("powr", 0) : role == OpeningRole.Barracks ? ("barr", 0) : (null, 0);
			var f = new FakeFeasibility();
			f.Infeasible.UnionWith(new[] { "powr", "barr" });
			f.Infeasible.UnionWith(Enumerable.Range(0, 10).Select(i => "x" + i));

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(3));
			Assert.That(pick.IsNone, Is.True);
			Assert.That(f.Tested.Count(n => n == "powr"), Is.EqualTo(1), "S3's power step reuses S1's failed answer");
			Assert.That(f.Tested, Has.Count.EqualTo(8));
			Assert.That(f.Tested.Take(2), Is.EqualTo(new[] { "powr", "barr" }), "S1, then S3, then the roll");
		}

		[Test]
		public void TheSameSnapshotAndSeedGiveTheSamePickAndTheSameDraws()
		{
			var names = new[] { "barr", "weap", "proc", "dome", "fix", "hpad", "atek", "stek", "gap", "tsla" };

			(BaseBuildPick Pick, string[] Tested, int Draws) Run(string[] order, int seed)
			{
				var s = Snapshot(order);
				var f = new FakeFeasibility();
				f.Infeasible.UnionWith(new[] { "barr", "dome", "gap", "atek" });
				var rng = new MersenneTwister(seed);
				var pick = BaseBuildPlanCA.Plan(s, f, rng);
				return (pick, f.Tested.ToArray(), rng.TotalCount);
			}

			var a = Run(names, 1234);
			var b = Run(names, 1234);
			var reversed = Run(names.Reverse().ToArray(), 1234);
			Assert.That(a.Pick.IsNone, Is.False);
			Assert.That(b, Is.EqualTo(a));
			Assert.That(a.Draws, Is.GreaterThan(0));
			Assert.That(reversed.Pick, Is.EqualTo(a.Pick), "keys are sorted ordinally: the table's enumeration order does not matter");
			Assert.That(reversed.Tested, Is.EqualTo(a.Tested));
			Assert.That(reversed.Draws, Is.EqualTo(a.Draws));
		}

		[Test]
		public void PowerComesFirstWhenShortAndDrawsNothing()
		{
			var s = Snapshot("barr", "weap");
			PowerShortNow(s);
			Opening(s, "barr", "proc", "weap");
			var f = new FakeFeasibility();
			var rng = new MersenneTwister(5);

			var pick = BaseBuildPlanCA.Plan(s, f, rng);
			Assert.That(pick.Name, Is.EqualTo("powr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S1Power));
			Assert.That(f.Tested, Is.EqualTo(new[] { "powr" }));
			Assert.That(rng.TotalCount, Is.EqualTo(0), "the roll is never reached, so no random draw");
		}

		[Test]
		public void PowerIsShortOnlyWithAManagerAPlantAndExcessUnderTheMargin()
		{
			var s = Snapshot();
			PowerShortNow(s);
			Assert.That(BaseBuildPlanCA.PowerShort(s), Is.True);
			s.ExcessPower = 100;
			Assert.That(BaseBuildPlanCA.PowerShort(s), Is.False, "at the margin");
			s.ExcessPower = -50;
			s.PowerTypeAmount = 0;
			Assert.That(BaseBuildPlanCA.PowerShort(s), Is.False, "a plant that gives no power");
			s.PowerTypeAmount = 100;
			s.HasPowerManager = false;
			Assert.That(BaseBuildPlanCA.PowerShort(s), Is.False);
		}

		[Test]
		public void StubStagesReadTheStageSourceLazilyAndInOrder()
		{
			var s = Snapshot();
			s.TierLine = 1; // c6: S8 runs only from a tier line (BaseBuildTiersCA.Open); the bank (5000) is above it
			var asked = new List<BaseBuildStage>();
			s.StageSource = stage =>
			{
				asked.Add(stage);
				return stage == BaseBuildStage.S2ExpansionRefinery ? new[] { "proc" } : stage == BaseBuildStage.S9ExtraDefence ? new[] { "gtwr" } : null;
			};

			Opening(s, "barr", null, null);
			var f = new FakeFeasibility();
			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("proc"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S2ExpansionRefinery), "S2 ranks above the opening");
			Assert.That(asked, Is.EqualTo(new[] { BaseBuildStage.S2ExpansionRefinery }), "a later stage's source is never run");

			f.Infeasible.UnionWith(new[] { "proc", "barr" });
			asked.Clear();
			pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("gtwr"));
			Assert.That(asked, Is.EqualTo(new[]
			{
				BaseBuildStage.S2ExpansionRefinery, BaseBuildStage.S4AntiRush, BaseBuildStage.S5CoreOnce, BaseBuildStage.S6PassiveIncome,
				BaseBuildStage.S7RefineryPerField, BaseBuildStage.S8ProductionCycle, BaseBuildStage.S9ExtraDefence,
			}));

			Assert.That(BaseBuildPlanCA.Stages, Is.EqualTo(Enumerable.Range(1, 10).Select(i => (BaseBuildStage)i)));
		}

		[Test]
		public void TheOpeningStageUsesTheStuckTimerOnlyForStepsItReaches()
		{
			var s = Snapshot();
			var stuckAsked = new List<string>();
			Opening(s, "barr", "proc", "weap", stuckAsked);
			s.OpeningStuck = (role, required) =>
			{
				stuckAsked.Add(role.ToString());
				return role == OpeningRole.Barracks;
			};

			var pick = BaseBuildPlanCA.Plan(s, new FakeFeasibility(), new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("proc"), "a stuck step is skipped (BaseFix01CA.NextRole)");
			Assert.That(stuckAsked, Is.EqualTo(new[] { "Barracks", "Refinery" }));
		}

		[Test]
		public void CashCountsAndFractionHelpers()
		{
			Assert.That(BaseBuildPlanCA.ReserveFor(BaseBuildStage.S1Power, 1800), Is.EqualTo(0));
			Assert.That(BaseBuildPlanCA.ReserveFor(BaseBuildStage.S7RefineryPerField, 1800), Is.EqualTo(0));
			Assert.That(BaseBuildPlanCA.ReserveFor(BaseBuildStage.S8ProductionCycle, 1800), Is.EqualTo(1800));
			Assert.That(BaseBuildPlanCA.ReserveFor(BaseBuildStage.S10Fractions, -1), Is.EqualTo(0), "unset reserve");
			Assert.That(BaseBuildPlanCA.Affordable(1000, 1000, 0), Is.True);
			Assert.That(BaseBuildPlanCA.Affordable(999, 1000, 0), Is.False);
			Assert.That(BaseBuildPlanCA.Affordable(2800, 1000, 1800), Is.True);

			var s = Snapshot();
			s.Owned = new Dictionary<string, int> { ["gtwr"] = 2, ["gtwr.east"] = 1, ["barr"] = 1 };
			s.InFlight = new Dictionary<string, int> { ["gtwr"] = 1 };
			s.VariantsOf = name => name == "gtwr" ? new[] { "gtwr.east", "gtwr.east", "gtwr" } : null;
			Assert.That(BaseBuildPlanCA.CountOf(s, "gtwr"), Is.EqualTo(4), "2 owned + 1 variant (listed twice, counted once) + 1 in flight");
			Assert.That(BaseBuildPlanCA.CountOf(s, "none"), Is.EqualTo(0));

			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "barr", 10), Is.True, "1 x 100 <= 10% of 10 buildings");
			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "gtwr", 10), Is.False, "4 of 10 is over a 10% share");
			s.IntervalActive = name => name == "barr";
			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "barr", 10), Is.False, "the build interval");
		}

		[Test]
		public void TheSwitchIsOffByDefaultRegisteredAndNoYamlArmsIt()
		{
			Assert.That(new BaseBuilderBotModuleCAInfo().BbPipeline01, Is.False);
			Assert.That(AblationIds.BbPipeline01, Is.EqualTo("bb_pipeline_01"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("bb_pipeline_01"));

			// C7 arms it in BaseBuilderBotModuleCA@generic only (Consolidate01C7Test); no content pack arms it.
			var packs = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "source", "packs"), "ai.yaml", SearchOption.AllDirectories);
			Assert.That(packs, Is.Not.Empty);
			foreach (var pack in packs)
				Assert.That(File.ReadAllText(pack), Does.Not.Contain("BbPipeline01"), pack);

			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool Pipeline01Armed => Info.BbPipeline01 && switchesActive && !ablatePipeline01.Off(player, AblationIds.BbPipeline01);"));
		}

		[Test]
		public void TheSwitchOffPathStillRunsTheOldLadder()
		{
			var q = Source(QmFile);
			Assert.That(q, Does.Contain("\tpartial class BaseBuilderQueueManagerCA\n"));
			Assert.That(q, Does.Contain(
				"\t\t\t\tpendingDemandPick = default;\n\n"
				+ "\t\t\t\t// CONSOLIDATE-01 c2 (bb_pipeline_01): armed, the staged planner picks and admits (BaseBuilderQueueManagerCA.Pipeline01.cs).\n"
				+ "\t\t\t\t// Off (and classic): the ladder and vetoes below, unchanged.\n"
				+ "\t\t\t\tif (PipelineOn)\n"
				+ "\t\t\t\t\treturn TickQueuePlanned(bot, queue);\n\n"
				+ "\t\t\t\tvar item = ChooseBuildingToBuild(queue);\n"), "the only hook: an early return before the old ladder call");
			Assert.That(q.Split("TickQueuePlanned(").Length - 1, Is.EqualTo(1));
			Assert.That(q.Split("ChooseBuildingToBuild(queue)").Length - 1, Is.EqualTo(1));
			Assert.That(q, Does.Not.Contain("BaseBuildPlanCA"), "the planner is reached only through the hook");

			// The off path keeps the old ladder's first rungs and the post-pick vetoes.
			Assert.That(q, Does.Contain("if (openingBarracksPolicyActive && (baseBuilder.HasQueuedPowerPlant() || baseBuilder.HasQueuedBarracks()))"));
			Assert.That(q, Does.Contain("if (BaseBuilderQueueEvalCA.BlockedByCash(cashNow, minCashRequirement, isRefinery, exempt, cost, queuedBlocks, starved))"));
			Assert.That(q, Does.Contain("foreach (var frac in baseBuilder.Info.BuildingFractions.OrderBy(kv => kv.Key).Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA))))"));
		}

		[Test]
		public void ThePlannedPathTestsBeforeItCommitsAndUsesTheBotRandom()
		{
			var p = Source(PipelineFile);
			Assert.That(p, Does.Contain("bool PipelineOn => baseBuilder.Pipeline01Armed;"));
			Assert.That(p, Does.Contain("var pick = BaseBuildPlanCA.Plan(snapshot, pipelineFeasibility, BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));"));

			// The claims live only in A7, the order and the ratio record only in the admitted tail.
			var feasible = p.Substring(p.IndexOf("bool PipelineFeasible(", StringComparison.Ordinal));
			feasible = feasible.Substring(0, feasible.IndexOf("bool PipelineTryClaim(", StringComparison.Ordinal));
			Assert.That(feasible, Does.Not.Contain("TryClaim"));
			Assert.That(feasible, Does.Not.Contain("QueueOrder"));
			Assert.That(feasible, Does.Not.Contain("StaticRatio.Record"));

			var admit = p.Substring(p.IndexOf("void AdmitPlanned(", StringComparison.Ordinal));
			foreach (var statement in new[]
			{
				"var produceOrder = Order.StartProduction(queue.Actor, item.Name, 1);",
				"baseBuilder.StaticRatio.Record(world.WorldTick, item.Name, ValuedCost(item));",
				"p.BuildingAdmitted(item, cost);",
				"baseBuilder.RecordProducerOrder(queue.Actor, item.Name);",
				"orderedCostThisSweep += Math.Max(0, cost);",
				"SetBuildingInterval(item.Name);",
			})
			{
				Assert.That(admit, Does.Contain(statement));
				Assert.That(Source(QmFile), Does.Contain(statement), "the same tail as TickQueue's admission");
			}

			var plan = Source(PlanFile);
			// V08E re-pin (a3bdd1cd split the test into feasible + the claim, adding the OfferRejected reason; the order and short-circuit are unchanged)
			Assert.That(plan, Does.Contain("var feasible = feasibility.Feasible(candidate, out var admitted) && admitted != null;\n\t\t\t\t\tif (feasible && feasibility.TryClaim(candidate, admitted))"));
			Assert.That(plan, Does.Contain(".OrderBy(kv => kv.Key, StringComparer.Ordinal).Shuffle(rng)"));
		}
	}
}
