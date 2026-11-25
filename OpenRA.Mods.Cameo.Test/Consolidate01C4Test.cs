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
	// CONSOLIDATE-01 c4 (CONSOLIDATE_01_DESIGN.md 3.1, section 4 row C4): stages S2 (expansion refinery), S2b (planner wants), S6 (passive income, silo)
	// and S7 (refinery per field) of the staged planner. The pure rules are tested directly, the stage order and fall-through through BaseBuildPlanCA with
	// a stand-in for A1-A7, and the world-dependent wiring by pins. The switch bb_passive_income is off by default and no yaml arms it (C7 does).
	[TestFixture]
	public sealed class Consolidate01C4Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string StagesFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Stages.cs";
		const string PipelineFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs";

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

		static BaseBuildSnapshot Snapshot(Dictionary<BaseBuildStage, string[]> sources, List<BaseBuildStage> read = null, params string[] fractions) => new()
		{
			Bank = 5000,
			Owned = new Dictionary<string, int>(),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = fractions.Select(f => new KeyValuePair<string, int>(f, 10)).ToArray(),
			StageSource = stage =>
			{
				read?.Add(stage);
				return sources.TryGetValue(stage, out var names) ? names : null;
			},
		};

		static void OpeningBarracks(BaseBuildSnapshot s)
		{
			s.OpeningActive = true;
			s.OpeningOrder = [new(OpeningRole.Barracks, 1)];
			s.OpeningStatus = role => role == OpeningRole.Barracks ? ("barr", 0) : (null, 0);
			s.OpeningStuck = (_, _) => false;
		}

		[Test]
		public void PassiveIncomeOffersTypesUnderTheirLimitCheapestFirst()
		{
			var order = PassiveIncomeCA.Order(
			[
				("derrick", 800, 1, 4),
				("hospital", 500, 0, -1),
				("market", 500, 1, -1),
				("bank", 400, 2, 2),
				("antique", 400, 0, 0),
				("fund", 300, 3, 5),
			]);

			// fund (300, 3 < 5) first, then derrick (800, 1 < 4) vs hospital (500, no limit, none owned): 500 before 800. market is owned once with no limit,
			// bank is at its limit, antique's limit is 0.
			Assert.That(order, Is.EqualTo(new[] { "fund", "hospital", "derrick" }));
		}

		[Test]
		public void PassiveIncomeTiesBreakByOrdinalNameAndEmptyInputsGiveNothing()
		{
			Assert.That(PassiveIncomeCA.Order([("b", 100, 0, -1), ("a", 100, 0, -1), ("B", 100, 0, -1)]), Is.EqualTo(new[] { "B", "a", "b" }));
			Assert.That(PassiveIncomeCA.Order([]), Is.Empty);
			Assert.That(PassiveIncomeCA.Order(null), Is.Empty);
		}

		[Test]
		public void PlannerWantsGoMcvPrerequisiteThenFrontDefenceThenLinkAndNullsAndRepeatsAreDropped()
		{
			Assert.That(PlannerWantsCA.Order("tech", "turret", "link"), Is.EqualTo(new[] { "tech", "turret", "link" }));
			Assert.That(PlannerWantsCA.Order(null, "turret", "link"), Is.EqualTo(new[] { "turret", "link" }));
			Assert.That(PlannerWantsCA.Order("link", "turret", "link"), Is.EqualTo(new[] { "link", "turret" }));
			Assert.That(PlannerWantsCA.Order(null, null, null), Is.Empty);
		}

		[Test]
		public void TheExpansionRefineryComesBeforeTheOpeningOrder()
		{
			var s = Snapshot(new() { [BaseBuildStage.S2ExpansionRefinery] = ["proc"] });
			OpeningBarracks(s);
			var f = new FakeFeasibility();

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("proc"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S2ExpansionRefinery));
			Assert.That(f.Tested, Is.EqualTo(new[] { "proc" }));
		}

		[Test]
		public void ARefusedExpansionRefineryFallsThroughToTheOpeningOrderNotToASilentStall()
		{
			var s = Snapshot(new() { [BaseBuildStage.S2ExpansionRefinery] = ["proc"] });
			OpeningBarracks(s);
			var f = new FakeFeasibility();
			f.Infeasible.Add("proc");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S3Opening));
			Assert.That(f.Tested, Is.EqualTo(new[] { "proc", "barr" }));
		}

		[Test]
		public void PlannerWantsFollowTheExpansionRefineryInStageTwoInTheirOwnOrder()
		{
			var s = Snapshot(new() { [BaseBuildStage.S2ExpansionRefinery] = ["proc", "tech", "turret", "link"] });
			var f = new FakeFeasibility();
			f.Infeasible.Add("proc");
			f.Infeasible.Add("tech");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("turret"));
			Assert.That(f.Tested, Is.EqualTo(new[] { "proc", "tech", "turret" }));
		}

		[Test]
		public void PassiveIncomeSitsAfterTheCoreStageAndBeforeTheRefineryPerField()
		{
			var s = Snapshot(new()
			{
				[BaseBuildStage.S6PassiveIncome] = ["fund"],
				[BaseBuildStage.S7RefineryPerField] = ["proc"],
				[BaseBuildStage.S5CoreOnce] = ["radar"],
			});
			var f = new FakeFeasibility();
			Assert.That(BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1)).Name, Is.EqualTo("radar"));

			f = new FakeFeasibility();
			f.Infeasible.Add("radar");
			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("fund"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S6PassiveIncome));

			f = new FakeFeasibility();
			f.Infeasible.Add("radar");
			f.Infeasible.Add("fund");
			pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("proc"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S7RefineryPerField));
		}

		[Test]
		public void ARefineryAnEarlierStageOfferedAndAFailedIsNotTestedAgainInStageSeven()
		{
			var s = Snapshot(new()
			{
				[BaseBuildStage.S2ExpansionRefinery] = ["proc"],
				[BaseBuildStage.S7RefineryPerField] = ["proc"],
			}, null, "barr");
			var f = new FakeFeasibility();
			f.Infeasible.Add("proc");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
			Assert.That(f.Tested, Is.EqualTo(new[] { "proc", "barr" }), "one test for the refinery, not two");
		}

		[Test]
		public void LaterStageSourcesAreNotReadAfterAnAdmission()
		{
			var read = new List<BaseBuildStage>();
			var s = Snapshot(new()
			{
				[BaseBuildStage.S2ExpansionRefinery] = ["proc"],
				[BaseBuildStage.S6PassiveIncome] = ["fund"],
				[BaseBuildStage.S7RefineryPerField] = ["proc2"],
			}, read);

			var pick = BaseBuildPlanCA.Plan(s, new FakeFeasibility(), new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("proc"));
			Assert.That(read, Is.EqualTo(new[] { BaseBuildStage.S2ExpansionRefinery }), "S4..S7 helpers never ran");
		}

		[Test]
		public void AtMostEightTestsRunOverTheStagesOfOneSweep()
		{
			var many = Enumerable.Range(0, 12).Select(i => "t" + i).ToArray();
			var s = Snapshot(new()
			{
				[BaseBuildStage.S6PassiveIncome] = many.Take(6).ToArray(),
				[BaseBuildStage.S7RefineryPerField] = many.Skip(6).ToArray(),
			});
			var f = new FakeFeasibility();
			foreach (var n in many)
				f.Infeasible.Add(n);

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.IsNone, Is.True);
			Assert.That(pick.Tests, Is.EqualTo(BaseBuildPlanCA.MaxFeasibilityTests));
			Assert.That(f.Tested.Count, Is.EqualTo(BaseBuildPlanCA.MaxFeasibilityTests));
		}

		[Test]
		public void TheStageSourcesAreWiredToTheWorldHelpersAndTheSwitchIsOffByDefault()
		{
			var stages = Source(StagesFile);
			Assert.That(stages, Does.Contain("case BaseBuildStage.S2ExpansionRefinery: return PipelineExpansionStage(queue, buildable, power, law);"));
			Assert.That(stages, Does.Contain("case BaseBuildStage.S6PassiveIncome: return PipelinePassiveIncomeStage(queue, buildable, power, law);"));
			Assert.That(stages, Does.Contain("case BaseBuildStage.S7RefineryPerField: return PipelineRefineryStage(queue, buildable, power);"));
			Assert.That(stages, Does.Contain("if (baseBuilder.YardWithoutRefinery())"), "S2 only for a yard with no refinery");
			Assert.That(stages, Does.Contain("PlannerWantsCA.Order(law.WantedMcvPrerequisite, law.WantedFrontDefence, law.WantedLinkBuilding)"));
			Assert.That(stages, Does.Contain("if (!PassiveIncomeOn)"));
			Assert.That(stages, Does.Contain("PassiveIncomeCA.Order(tricklers)"));
			Assert.That(stages, Does.Contain("RefineryLawSilo.Wanted(law != null, playerResources.Resources, playerResources.ResourceCapacity, siloInProduction)"));
			Assert.That(stages, Does.Contain("var refinery = ChooseWantedRefinery(queue, buildable, power);"), "S2 and S7 share the existing wanted-refinery helper");

			Assert.That(Source(PipelineFile), Does.Contain("s.StageSource = stage => PipelineStageNames(stage, queue, buildable, power, law, s);"));

			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(module, Does.Contain("public readonly bool BbPassiveIncome = false;"));
			Assert.That(module, Does.Contain("public bool PassiveIncomeArmed => Info.BbPassiveIncome && switchesActive && !ablatePassiveIncome.Off(player, AblationIds.BbPassiveIncome);"));
			Assert.That(AblationIds.BbPassiveIncome, Is.EqualTo("bb_passive_income"));
		}
	}
}
