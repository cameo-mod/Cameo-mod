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
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P1 (a) (CONSOLIDATE_03_P1_DESIGN_01.md): the ONE building-pick arbiter. Equivalence first: recording the refusals changes neither the pick, nor the tests
	// run, nor the random draws. Then every refusal names its reason and rule, and the early-defence plan is the prior (AntiRushCA) exactly.
	[TestFixture]
	public sealed class BuildingPickArbiterTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		sealed class FakeFeasibility : IBaseBuildFeasibility
		{
			public readonly HashSet<string> Infeasible = new(StringComparer.Ordinal);
			public readonly HashSet<string> ClaimRefused = new(StringComparer.Ordinal);
			public readonly HashSet<string> CashOnly = new(StringComparer.Ordinal);
			public readonly HashSet<BaseBuildStage> StagesHeld = new();
			public readonly List<string> Tested = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => !StagesHeld.Contains(candidate.Stage);

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => !ClaimRefused.Contains(candidate.Name);

			public bool CashOnlyBlocked(BaseBuildCandidate candidate, int minBankPercent) => CashOnly.Contains(candidate.Name);
		}

		// Three listed fractions, each owned once of ten buildings, so the S10 roll (one draw per entry reached) offers them in a seeded order.
		static BaseBuildSnapshot Snapshot(Dictionary<BaseBuildStage, string[]> sources, bool sustained = false, bool fallthrough = false, int bank = 5000) => new()
		{
			WorldTick = 1000,
			Bank = bank,
			TierLine = 3500,
			LowCashCut = true,
			SustainedActive = sustained,
			S8RejectedFallthrough = fallthrough,
			Owned = new Dictionary<string, int> { ["barr"] = 1, ["weap"] = 1, ["dome"] = 1 },
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = new[] { new KeyValuePair<string, int>("barr", 10), new KeyValuePair<string, int>("weap", 10), new KeyValuePair<string, int>("dome", 10) },
			StageSource = stage => sources.TryGetValue(stage, out var names) ? names : null,
		};

		static Dictionary<BaseBuildStage, string[]> Sources(params (BaseBuildStage Stage, string[] Names)[] entries) => entries.ToDictionary(e => e.Stage, e => e.Names);

		[Test]
		public void RecordingTheRefusalsChangesNeitherThePickNorTheTestsNorTheDraws()
		{
			var grid = new List<(Dictionary<BaseBuildStage, string[]> Sources, bool Sustained, bool Fallthrough, int Bank, string[] Infeasible)>
			{
				(Sources(), false, false, 5000, Array.Empty<string>()),
				(Sources(), false, false, 5000, new[] { "barr", "weap" }),
				(Sources(), false, false, 1000, Array.Empty<string>()),
				(Sources((BaseBuildStage.S8ProductionCycle, new[] { "tent" })), true, false, 5000, new[] { "tent" }),
				(Sources((BaseBuildStage.S8ProductionCycle, new[] { "tent" })), true, true, 5000, new[] { "tent" }),
				(Sources((BaseBuildStage.S8ProductionCycle, new[] { "tent" }), (BaseBuildStage.S9ExtraDefence, new[] { "gtwr", "pbox" })), true, true, 5000, new[] { "tent", "gtwr" }),
				(Sources((BaseBuildStage.S4AntiRush, new[] { "pbox", "gtwr" }), (BaseBuildStage.S5CoreOnce, new[] { "fix" })), false, false, 5000, new[] { "pbox" }),
				(Sources((BaseBuildStage.S9ExtraDefence, new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" })), false, false, 5000, new[] { "a", "b", "c", "d", "e", "f", "g", "h" }),
			};

			foreach (var (sources, sustained, fallthrough, bank, infeasible) in grid)
			{
				for (var seed = 1; seed <= 25; seed++)
				{
					var f0 = new FakeFeasibility();
					var f1 = new FakeFeasibility();
					f0.Infeasible.UnionWith(infeasible);
					f1.Infeasible.UnionWith(infeasible);
					var r0 = new MersenneTwister(seed);
					var r1 = new MersenneTwister(seed);

					var old = BaseBuildPlanCA.Plan(Snapshot(sources, sustained, fallthrough, bank), f0, r0);
					var refusals = new List<BuildingRefusal>();
					var decision = BuildingPickArbiter.Decide(Snapshot(sources, sustained, fallthrough, bank), f1, r1, refusals);

					Assert.That(decision.Pick, Is.EqualTo(old), $"seed {seed}: the same pick");
					Assert.That(f1.Tested, Is.EqualTo(f0.Tested), $"seed {seed}: the same tests in the same order");
					Assert.That(r1.Next(), Is.EqualTo(r0.Next()), $"seed {seed}: the same number of random draws");
					Assert.That(decision.Decider == BuildingRule.None, Is.EqualTo(old.IsNone), "a pick has a deciding rule, no pick has none");
					Assert.That(refusals.All(r => r.Rule != BuildingRule.None), Is.True, "every refusal names a rule");
				}
			}
		}

		[Test]
		public void ARefusedCandidateNamesItsReasonAndRule()
		{
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			f.ClaimRefused.Add("pbox");
			f.StagesHeld.Add(BaseBuildStage.S6PassiveIncome);
			var refusals = new List<BuildingRefusal>();
			var decision = BuildingPickArbiter.Decide(Snapshot(Sources(
				(BaseBuildStage.S4AntiRush, new[] { "pbox" }),
				(BaseBuildStage.S6PassiveIncome, new[] { "silo" }),
				(BaseBuildStage.S7RefineryPerField, new[] { "tent", "tent" }),
				(BaseBuildStage.S9ExtraDefence, new[] { "gtwr" }))), f, new MersenneTwister(1), refusals);

			Assert.That(decision.Pick.Candidate, Is.EqualTo("gtwr"));
			Assert.That(decision.Decider, Is.EqualTo(BuildingRule.ScaledDefence));
			Assert.That(refusals, Is.EqualTo(new[]
			{
				new BuildingRefusal("pbox", BaseBuildStage.S4AntiRush, BuildingReason.ClaimRefused, BuildingRule.EarlyDefence),
				new BuildingRefusal("silo", BaseBuildStage.S6PassiveIncome, BuildingReason.StageCap, BuildingRule.PassiveIncome),
				new BuildingRefusal("tent", BaseBuildStage.S7RefineryPerField, BuildingReason.Infeasible, BuildingRule.RefineryPerField),
				new BuildingRefusal("tent", BaseBuildStage.S7RefineryPerField, BuildingReason.AlreadyFailed, BuildingRule.RefineryPerField),
			}));
		}

		[Test]
		public void AClosedStageNamesTheTierCutTheCycleHoldOrTheQueueSplit()
		{
			// Below the tier line: S8, S9 (no threat seen) and S10 are cut.
			var refusals = new List<BuildingRefusal>();
			var pick = BuildingPickArbiter.Decide(Snapshot(Sources(), bank: 1000), new FakeFeasibility(), new MersenneTwister(1), refusals).Pick;
			Assert.That(pick.IsNone, Is.True);
			Assert.That(refusals.Select(r => (r.Stage, r.Reason)), Is.EqualTo(new[]
			{
				(BaseBuildStage.S8ProductionCycle, BuildingReason.TierCut),
				(BaseBuildStage.S9ExtraDefence, BuildingReason.TierCut),
				(BaseBuildStage.S10Fractions, BuildingReason.TierCut),
			}));

			// SUSTAINED, the old offer-only hold: a rejected S8 offer holds S9 and S10 (the cycle hold, not a cut).
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			refusals = new List<BuildingRefusal>();
			BuildingPickArbiter.Decide(Snapshot(Sources((BaseBuildStage.S8ProductionCycle, new[] { "tent" })), sustained: true), f, new MersenneTwister(1), refusals);
			Assert.That(refusals.Where(r => r.Name == null).Select(r => (r.Stage, r.Reason)), Is.EqualTo(new[]
			{
				(BaseBuildStage.S9ExtraDefence, BuildingReason.CycleHold),
				(BaseBuildStage.S10Fractions, BuildingReason.CycleHold),
			}));

			// QUEUE-SPLIT on the building queue: S4 and S9 belong to the defence queue.
			var s = Snapshot(Sources());
			s.QueueSplit = true;
			refusals = new List<BuildingRefusal>();
			BuildingPickArbiter.Decide(s, new FakeFeasibility(), new MersenneTwister(1), refusals);
			Assert.That(refusals.Where(r => r.Reason == BuildingReason.QueueSplit).Select(r => r.Stage),
				Is.EqualTo(new[] { BaseBuildStage.S4AntiRush, BaseBuildStage.S9ExtraDefence }));
		}

		[Test]
		public void TheS8CashHoldIsANamedInputOfTheArbiter()
		{
			var s = Snapshot(Sources((BaseBuildStage.S8ProductionCycle, new[] { "tent" })), sustained: true, fallthrough: true);
			s.S8CashHold = true;
			s.S8HoldMaxTicks = 750;
			s.S8HoldMinBankPercent = 75;
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			f.CashOnly.Add("tent");
			var refusals = new List<BuildingRefusal>();
			var decision = BuildingPickArbiter.Decide(s, f, new MersenneTwister(1), refusals);

			Assert.That(decision.Pick.IsNone, Is.True, "the S10 roll waits for the factory");
			Assert.That(refusals, Does.Contain(new BuildingRefusal("tent", BaseBuildStage.S8ProductionCycle, BuildingReason.S8CashOnly, BuildingRule.ProductionCycle)));
			Assert.That(refusals, Does.Contain(new BuildingRefusal(null, BaseBuildStage.S10Fractions, BuildingReason.S8CashHold, BuildingRule.Fractions)));
		}

		[Test]
		public void TheKStopIsTheLastRefusal()
		{
			var names = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" };
			var f = new FakeFeasibility();
			f.Infeasible.UnionWith(names);
			var refusals = new List<BuildingRefusal>();
			var pick = BuildingPickArbiter.Decide(Snapshot(Sources((BaseBuildStage.S9ExtraDefence, names))), f, new MersenneTwister(1), refusals).Pick;

			Assert.That(pick.IsNone, Is.True);
			Assert.That(pick.Tests, Is.EqualTo(BaseBuildPlanCA.MaxFeasibilityTests));
			Assert.That(refusals[^1], Is.EqualTo(new BuildingRefusal(null, BaseBuildStage.S9ExtraDefence, BuildingReason.TestCap, BuildingRule.ScaledDefence)));
			Assert.That(refusals.Count(r => r.Reason == BuildingReason.Infeasible), Is.EqualTo(BaseBuildPlanCA.MaxFeasibilityTests));
		}

		[Test]
		public void AFirstCopyNamesItsReasonListedThenCaptureUnlockThenTechDepth()
		{
			var s = Snapshot(Sources());
			Assert.That(BuildingPickArbiter.RuleOf(BaseBuildStage.S5CoreOnce, s, "barr"), Is.EqualTo(BuildingRule.FirstCopyListed));
			Assert.That(BuildingPickArbiter.RuleOf(BaseBuildStage.S5CoreOnce, s, "fix"), Is.EqualTo(BuildingRule.FirstCopyTechDepth), "no capture provider = tech depth");

			s.CaptureUnlockOf = name => name == "npower";
			Assert.That(BuildingPickArbiter.RuleOf(BaseBuildStage.S5CoreOnce, s, "npower"), Is.EqualTo(BuildingRule.FirstCopyCaptureUnlock));
			Assert.That(BuildingPickArbiter.RuleOf(BaseBuildStage.S5CoreOnce, s, "fix"), Is.EqualTo(BuildingRule.FirstCopyTechDepth));

			// A listed entry with weight 0 is not a listed first copy (the roll never offers it).
			s.Fractions = new[] { new KeyValuePair<string, int>("fix", 0) };
			Assert.That(BuildingPickArbiter.RuleOf(BaseBuildStage.S5CoreOnce, s, "fix"), Is.EqualTo(BuildingRule.FirstCopyTechDepth));
		}

		[Test]
		public void EveryStageHasItsNamedRule()
		{
			var s = Snapshot(Sources());
			var rules = BaseBuildPlanCA.Stages.Select(stage => BuildingPickArbiter.RuleOf(stage, s, null)).ToList();
			Assert.That(rules, Does.Not.Contain(BuildingRule.None));
			Assert.That(rules.Distinct().Count(), Is.EqualTo(rules.Count), "one rule per stage");
		}

		[Test]
		public void TheEarlyDefencePlanIsThePriorExactly()
		{
			for (var permille = -1; permille <= 800; permille += 25)
				for (var min = 0; min <= 3; min++)
					for (var max = 0; max <= 4; max++)
						Assert.That(EarlyDefencePlan.Count(permille, min, max), Is.EqualTo(AntiRushCA.SetSize(permille, min, max)));

			foreach (var production in new[] { false, true })
				for (var have = 0; have <= 4; have++)
					for (var count = 0; count <= 4; count++)
						Assert.That(EarlyDefencePlan.Wanted(production, have, count), Is.EqualTo(AntiRushCA.Wants(production, have, count)));
		}

		[Test]
		public void TheThreeArgumentPlanFillsTheSnapshotListOnlyWhenOneIsSet()
		{
			var f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			var s = Snapshot(Sources((BaseBuildStage.S7RefineryPerField, new[] { "tent" })));
			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(s.Refusals, Is.Null, "release: nothing is collected");

			s = Snapshot(Sources((BaseBuildStage.S7RefineryPerField, new[] { "tent" })));
			s.Refusals = new List<BuildingRefusal>();
			Assert.That(BaseBuildPlanCA.Plan(s, new FakeFeasibility { Infeasible = { "tent" } }, new MersenneTwister(1)), Is.EqualTo(pick), "the same pick with the DEV list");
			Assert.That(s.Refusals, Does.Contain(new BuildingRefusal("tent", BaseBuildStage.S7RefineryPerField, BuildingReason.Infeasible, BuildingRule.RefineryPerField)));
		}

		[Test]
		public void TheSummaryListsStageNameAndReasonThenTheRest()
		{
			Assert.That(BuildingPickArbiter.Summary(null, 3), Is.EqualTo(""));
			var refusals = new List<BuildingRefusal>
			{
				new("tent", BaseBuildStage.S8ProductionCycle, BuildingReason.Infeasible, BuildingRule.ProductionCycle),
				new(null, BaseBuildStage.S9ExtraDefence, BuildingReason.CycleHold, BuildingRule.ScaledDefence),
				new(null, BaseBuildStage.S10Fractions, BuildingReason.CycleHold, BuildingRule.Fractions),
			};
			Assert.That(BuildingPickArbiter.Summary(refusals, 3), Is.EqualTo("S8ProductionCycle:tent:Infeasible;S9ExtraDefence:-:CycleHold;S10Fractions:-:CycleHold"));
			Assert.That(BuildingPickArbiter.Summary(refusals, 1), Is.EqualTo("S8ProductionCycle:tent:Infeasible;+2"));
		}

		[Test]
		public void TheDecideRowIsDevOnlyAndDeduped()
		{
			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RefusalsCollect(BaseBuildSnapshot s)"));
			Assert.That(p1, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid DecideObserved(BaseBuildSnapshot s, BaseBuildPick pick)"));
			Assert.That(p1, Does.Contain("RefusalsCollect(snapshot); // bb_decide_row, DEV only"));
			Assert.That(p1, Does.Contain("DecideObserved(snapshot, pick); // bb_decide_row, DEV only"));
			Assert.That(p1, Does.Contain("if (now - decideLogged < (signature == decideSignature ? 750 : 25))"), "a changed decision at most every 25 ticks, an unchanged one every 750");
			Assert.That(p1.Split("s.Refusals = ").Length - 1, Is.EqualTo(1), "the list is set only by the DEV collector");

			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			Assert.That(dev, Does.Contain("ObserverRow(owner, \"bb_decide\", b =>"));
		}

		[Test]
		public void TheReleasePathRecordsNothingAndOneTestFeedsS4AndModeBias()
		{
			var plan = Source(Dir + "BaseBuildPlanCA.cs");
			Assert.That(plan, Does.Contain("public static BaseBuildPick Plan(BaseBuildSnapshot snapshot, IBaseBuildFeasibility feasibility, MersenneTwister rng) => Plan(snapshot, feasibility, rng, snapshot.Refusals);"), "P1 (b): the DEV list rides on the snapshot, null in release");
			Assert.That(plan, Does.Contain("if (refusals != null)\n\t\t\t\t\tBuildingPickArbiter.NoteClosed(refusals, stage, snapshot, cycleOffered);"));

			var arbiter = Source(Dir + "BuildingPickArbiter.cs");
			Assert.That(arbiter, Does.Contain("refusals?.Add(new BuildingRefusal(candidate.Name, candidate.Stage, reason, RuleOf(candidate.Stage, s, candidate.Name)));"), "null = no work");

			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("var pick = BaseBuildPlanCA.Plan(snapshot, pipelineFeasibility, BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));"), "P1 (a) leaves the live call as it was");

			var defence = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Defence.cs");
			var mode = Source(Dir + "BaseBuilderQueueManagerCA.ModeBias.cs");
			Assert.That(defence, Does.Contain("if (!EarlyDefenceWanted())"));
			Assert.That(mode, Does.Contain("bool AntiRushWantedNow() => EarlyDefenceWanted();"));
			Assert.That(defence + mode, Does.Not.Contain("AntiRushCA.SetSize("), "the set size is computed in one place only");
		}
	}
}
