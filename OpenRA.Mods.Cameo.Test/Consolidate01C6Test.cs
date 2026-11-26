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
	// CONSOLIDATE-01 c6 (CONSOLIDATE_01_DESIGN.md 3.1, section 4 row C6): the cash tiers (R9: below the tier line L the walk stops after S7, S9 only with a threat seen),
	// stage S8 (R7: the production cycle, bank at or above L) and SUSTAINED-CASH (S8 moves above S7, S9 and S10 are held back while the cycle still offers a type; S1-S5
	// stay above). The pure rules are tested directly, the walk through BaseBuildPlanCA with a stand-in for A1-A7, and the world-dependent wiring by pins.
	[TestFixture]
	public sealed class Consolidate01C6Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string LogicDir = "mods/CA/Traits/BotModules/BotModuleLogic/";
		const string CycleFile = LogicDir + "BaseBuilderQueueManagerCA.Pipeline01Cycle.cs";
		const string StagesFile = LogicDir + "BaseBuilderQueueManagerCA.Pipeline01Stages.cs";
		const string PipelineFile = LogicDir + "BaseBuilderQueueManagerCA.Pipeline01.cs";
		const string PlanFile = LogicDir + "BaseBuildPlanCA.cs";
		const string QmFile = LogicDir + "BaseBuilderQueueManagerCA.cs";
		const string DefenceFile = LogicDir + "BaseBuilderQueueManagerCA.Pipeline01Defence.cs";

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		sealed class FakeFeasibility : IBaseBuildFeasibility
		{
			public readonly HashSet<string> Infeasible = new(StringComparer.Ordinal);
			public readonly HashSet<BaseBuildStage> StagesHeld = new();
			public readonly List<string> Tested = new();

			public bool StageAdmits(BaseBuildCandidate candidate) => !StagesHeld.Contains(candidate.Stage);

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				Tested.Add(candidate.Name);
				admitted = Infeasible.Contains(candidate.Name) ? null : candidate.Name;
				return admitted != null;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;
		}

		static BaseBuildSnapshot Snapshot(Dictionary<BaseBuildStage, string[]> sources, params string[] fractions) => new()
		{
			Bank = 5000,
			TierLine = 3500,
			LowCashCut = true,
			// The listed types are owned once, so S5 (the roll on first copies, c3c) offers none of them and the stage tests reach S8-S10.
			Owned = fractions.Distinct().ToDictionary(f => f, _ => 1),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = fractions.Select(f => new KeyValuePair<string, int>(f, 10)).ToArray(),
			StageSource = stage => sources.TryGetValue(stage, out var names) ? names : null,
		};

		static BaseBuildPick Plan(BaseBuildSnapshot s, FakeFeasibility f) => BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));

		// ---- CycleProductionCA.Order ----

		[Test]
		public void TheCycleOrderIsTheRepeatedPickFewestFirstThenTheNextNameAfterTheLastType()
		{
			var counted = new List<(string Name, int Count)> { ("barracks", 2), ("warfactory", 1), ("airfield", 1), ("naval", 0) };

			var order = CycleProductionCA.Order(counted, null);
			Assert.That(order, Is.EqualTo(new[] { "naval", "airfield", "warfactory", "barracks" }));
			Assert.That(order[0], Is.EqualTo(counted[CycleProductionCA.Pick(counted, null)].Name), "the first is exactly what Pick takes");

			order = CycleProductionCA.Order(counted, "airfield");
			Assert.That(order, Is.EqualTo(new[] { "naval", "warfactory", "airfield", "barracks" }), "among equals the next name after the last cycled type goes first");
			Assert.That(order[0], Is.EqualTo(counted[CycleProductionCA.Pick(counted, "airfield")].Name));

			Assert.That(CycleProductionCA.Order(new List<(string Name, int Count)>(), "x"), Is.Empty);
			Assert.That(CycleProductionCA.Order(new List<(string Name, int Count)> { ("barracks", 3) }, "barracks"), Is.EqualTo(new[] { "barracks" }), "only one type left: it keeps going");
		}

		// ---- BaseBuildTiersCA ----

		[Test]
		public void TheStageOrderIsNormalAndSustainedPutsTheCycleDirectlyAboveTheRefineryStage()
		{
			var normal = Enumerable.Range(1, 10).Select(i => (BaseBuildStage)i).ToArray();
			Assert.That(BaseBuildTiersCA.Order(false, 3500), Is.EqualTo(normal));
			Assert.That(BaseBuildTiersCA.Order(true, 0), Is.EqualTo(normal), "sustained needs a tier line");

			var sustained = BaseBuildTiersCA.Order(true, 3500);
			Assert.That(sustained, Is.EqualTo(new[]
			{
				BaseBuildStage.S1Power, BaseBuildStage.S2ExpansionRefinery, BaseBuildStage.S3Opening, BaseBuildStage.S4AntiRush, BaseBuildStage.S5CoreOnce,
				BaseBuildStage.S6PassiveIncome, BaseBuildStage.S8ProductionCycle, BaseBuildStage.S7RefineryPerField, BaseBuildStage.S9ExtraDefence, BaseBuildStage.S10Fractions,
			}));

			var s8 = sustained.ToList().IndexOf(BaseBuildStage.S8ProductionCycle);
			foreach (var kept in new[] { BaseBuildStage.S1Power, BaseBuildStage.S2ExpansionRefinery, BaseBuildStage.S5CoreOnce })
				Assert.That(sustained.ToList().IndexOf(kept), Is.LessThan(s8), kept + " stays above the cycle");
		}

		[Test]
		public void TheTierCutStopsAfterStageSevenBelowTheLineAndStageNineNeedsAThreat()
		{
			// Open(stage, line, bank, lowCashCut, sustained, threatSeen, cycleOffered)
			foreach (var stage in new[] { BaseBuildStage.S1Power, BaseBuildStage.S2ExpansionRefinery, BaseBuildStage.S3Opening, BaseBuildStage.S4AntiRush,
				BaseBuildStage.S5CoreOnce, BaseBuildStage.S6PassiveIncome, BaseBuildStage.S7RefineryPerField })
				Assert.That(BaseBuildTiersCA.Open(stage, 3500, 0, true, false, false, false), Is.True, stage + " runs at any bank");

			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S8ProductionCycle, 3500, 1800, true, false, false, false), Is.False, "the cycle needs the line");
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S8ProductionCycle, 3500, 3499, true, false, true, false), Is.False, "one under the line, even with a threat");
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S8ProductionCycle, 3500, 3500, true, false, false, false), Is.True, "at the line");
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S8ProductionCycle, 0, 99999, true, false, false, false), Is.False, "no tier line = no cycle");

			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S10Fractions, 3500, 1800, true, false, true, false), Is.False, "below the line the roll stops, threat or not");
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S10Fractions, 3500, 3500, true, false, false, false), Is.True);
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S9ExtraDefence, 3500, 1800, true, false, false, false), Is.False, "no extra defence below the line while no threat is seen");
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S9ExtraDefence, 3500, 1800, true, false, true, false), Is.True, "a seen threat opens S9 below the line");
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S9ExtraDefence, 3500, 5000, true, false, false, false), Is.True);

			// R9 ablated: no cut (the roll and the extra defences run at any bank); no tier line: no cut either.
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S10Fractions, 3500, 1800, false, false, false, false), Is.True);
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S9ExtraDefence, 3500, 1800, false, false, false, false), Is.True);
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S10Fractions, 0, 0, true, false, false, false), Is.True);
			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S9ExtraDefence, 0, 0, true, false, false, false), Is.True);
		}

		[Test]
		public void SustainedHoldsBackTheExtrasOnlyWhileTheCycleStillOffersAType()
		{
			foreach (var stage in new[] { BaseBuildStage.S9ExtraDefence, BaseBuildStage.S10Fractions })
			{
				Assert.That(BaseBuildTiersCA.Open(stage, 3500, 9000, true, true, true, true), Is.False, stage + " is suppressed while the cycle offers a type");
				Assert.That(BaseBuildTiersCA.Open(stage, 3500, 9000, true, true, true, false), Is.True, stage + ": an exhausted cycle lets normal building resume");
				Assert.That(BaseBuildTiersCA.Open(stage, 3500, 9000, true, false, true, true), Is.True, stage + ": not sustained, nothing is suppressed");
			}

			Assert.That(BaseBuildTiersCA.Open(BaseBuildStage.S7RefineryPerField, 3500, 9000, true, true, true, true), Is.True, "S7 is never suppressed");
		}

		// ---- the walk ----

		// X10 / R9: bank 1800 after the opening: only S1-S7 run, the cycle, the extras and the roll are never even tested.
		[Test]
		public void BelowTheLineOnlyTheEssentialStagesAreTested()
		{
			var s = Snapshot(new()
			{
				[BaseBuildStage.S5CoreOnce] = ["radar"],
				[BaseBuildStage.S7RefineryPerField] = ["proc"],
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			}, "barr");
			s.Bank = 1800;
			var f = new FakeFeasibility();
			f.Infeasible.Add("radar");
			f.Infeasible.Add("proc");

			var pick = Plan(s, f);
			Assert.That(pick.IsNone, Is.True);
			Assert.That(f.Tested, Is.EqualTo(new[] { "radar", "proc" }), "S8, S9 and S10 cost no test below the line");

			s.Bank = 1800;
			f = new FakeFeasibility();
			pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("radar"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce), "the core still builds below the line");
		}

		[Test]
		public void ASeenThreatKeepsTheExtraDefenceStageOpenBelowTheLine()
		{
			var s = Snapshot(new()
			{
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
			}, "barr");
			s.Bank = 1800;
			s.DefenceThreatSeen = true;

			var f = new FakeFeasibility();
			var pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("gtwr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S9ExtraDefence));
			Assert.That(f.Tested, Is.EqualTo(new[] { "gtwr" }));
		}

		[Test]
		public void TheCycleRunsFromTheLineAndAfterTheRefineryStageWhileNotSustained()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S7RefineryPerField] = ["proc"],
				[BaseBuildStage.S8ProductionCycle] = ["tent", "wf"],
			};

			var s = Snapshot(new(sources), "barr");
			var pick = Plan(s, new FakeFeasibility());
			Assert.That(pick.Candidate, Is.EqualTo("proc"), "S7 before S8 in the normal order");

			var f = new FakeFeasibility();
			f.Infeasible.Add("proc");
			pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("tent"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S8ProductionCycle));

			f = new FakeFeasibility();
			f.Infeasible.Add("proc");
			f.Infeasible.Add("tent");
			pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("wf"), "a refused first type falls through to the next type of the cycle");

			s.Bank = 3499;
			f = new FakeFeasibility();
			f.Infeasible.Add("proc");
			pick = Plan(s, f);
			Assert.That(pick.IsNone, Is.True, "one under the line: nothing of S8-S10");
		}

		[Test]
		public void WithTheCutAblatedOrNoLineTheRollRunsAtAnyBankAndTheCycleNeverRuns()
		{
			var s = Snapshot(new() { [BaseBuildStage.S8ProductionCycle] = ["tent"] }, "barr");
			s.Bank = 1800;
			s.LowCashCut = false;
			var pick = Plan(s, new FakeFeasibility());
			Assert.That(pick.Candidate, Is.EqualTo("barr"), "R9 ablated: the roll runs below the line, the cycle still needs it");
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));

			s = Snapshot(new() { [BaseBuildStage.S8ProductionCycle] = ["tent"] }, "barr");
			s.TierLine = 0;
			pick = Plan(s, new FakeFeasibility());
			Assert.That(pick.Candidate, Is.EqualTo("barr"), "no tier line: no cycle, no cut");
		}

		[Test]
		public void SustainedCashMovesTheCycleAboveTheRefineryStageAndHoldsTheExtrasBack()
		{
			var sources = new Dictionary<BaseBuildStage, string[]>
			{
				[BaseBuildStage.S7RefineryPerField] = ["proc"],
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
			};

			var s = Snapshot(new(sources), "barr");
			s.SustainedActive = true;
			var f = new FakeFeasibility();
			var pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("tent"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S8ProductionCycle), "S8 above S7");
			Assert.That(f.Tested, Is.EqualTo(new[] { "tent" }));

			f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("proc"), "a refused cycle type falls through to S7");
			Assert.That(f.Tested, Is.EqualTo(new[] { "tent", "proc" }));

			f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			f.Infeasible.Add("proc");
			pick = Plan(s, f);
			Assert.That(pick.IsNone, Is.True, "S9 and S10 are held back while the cycle offered a type");
			Assert.That(f.Tested, Is.EqualTo(new[] { "tent", "proc" }), "the roll was never tested");

			// X12: the same walk without sustained cash reaches the roll after the failed stages.
			s.SustainedActive = false;
			f = new FakeFeasibility();
			f.Infeasible.Add("tent");
			f.Infeasible.Add("proc");
			pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
		}

		[Test]
		public void SustainedWithAnExhaustedCycleLetsNormalBuildingResume()
		{
			var s = Snapshot(new() { [BaseBuildStage.S7RefineryPerField] = ["proc"] }, "barr");
			s.SustainedActive = true;
			var f = new FakeFeasibility();
			f.Infeasible.Add("proc");

			var pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("barr"), "every production type at its limit: the roll resumes (the old 'none left' fall-through)");
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
			Assert.That(f.Tested, Is.EqualTo(new[] { "proc", "barr" }));
		}

		// X1: pb_sustained_keeps_essentials: the opening, the anti-rush set and the core once stay above the sustained cycle.
		[Test]
		public void SustainedKeepsTheEssentialsAboveTheCycle()
		{
			var s = Snapshot(new()
			{
				[BaseBuildStage.S2ExpansionRefinery] = ["proc2"],
				[BaseBuildStage.S4AntiRush] = ["gtwr"],
				[BaseBuildStage.S5CoreOnce] = ["radar"],
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
			}, "barr");
			s.SustainedActive = true;

			var f = new FakeFeasibility();
			Assert.That(Plan(s, f).Candidate, Is.EqualTo("proc2"));

			f = new FakeFeasibility();
			f.Infeasible.Add("proc2");
			Assert.That(Plan(s, f).Candidate, Is.EqualTo("gtwr"));

			f = new FakeFeasibility();
			f.Infeasible.Add("proc2");
			f.Infeasible.Add("gtwr");
			var pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("radar"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
		}

		// ---- wiring pins ----

		[Test]
		public void TheCycleSourceIsAQueryThatOffersTheProductionTypesInCycleOrder()
		{
			var cycle = Source(CycleFile);
			var s8 = cycle.Substring(cycle.IndexOf("IEnumerable<string> PipelineProductionCycleStage(", StringComparison.Ordinal));
			s8 = s8.Substring(0, s8.IndexOf("ActorInfo PipelineNavalCycleCandidate(", StringComparison.Ordinal));

			Assert.That(s8, Does.Contain("if (!(CycleOn || SustainedActive) || playerBuildings == null)\n\t\t\t\tyield break;"), "one stage, two triggers (pb_cycle_production, pb_sustained_cash_production)");
			Assert.That(s8, Does.Contain("var candidates = OverflowProductionCandidates(buildable, null, false);"), "the production types unresolved: A5 swaps the power plant in");
			Assert.That(s8, Does.Contain("var naval = PipelineNavalCycleCandidate(buildable);"));
			Assert.That(s8, Does.Contain("if (naval != null && candidates.All(c => c.Name != naval.Name))"), "a naval type already in the production list is not offered twice");
			Assert.That(s8, Does.Contain("foreach (var name in CycleProductionCA.Order(counted, lastCycledProduction))"));
			Assert.That(s8, Does.Contain("yield return name;"));

			// The source code only: not the file's comments (they name what the source does not do), and not the stage cap PipelineCycleAdmits (it IS the votes).
			var capFrom = cycle.IndexOf("bool PipelineCycleAdmits(", StringComparison.Ordinal);
			var capTo = cycle.IndexOf("ActorInfo PipelineNavalCycleCandidate(", StringComparison.Ordinal);
			Assert.That(capFrom, Is.GreaterThan(0));
			Assert.That(capTo, Is.GreaterThan(capFrom));
			var code = string.Join("\n", (cycle.Substring(0, capFrom) + cycle.Substring(capTo)).Split('\n').Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
			foreach (var forbidden in new[] { "lastCycledProduction =", "PickOrPower", "VotesAdmit", "cashClock", "sustainedPickName", "TryClaim", "StaticRatio", "Random", "BotRng", "RatioGate" })
				Assert.That(code, Does.Not.Contain(forbidden), "the S8 source is a query: " + forbidden);

			Assert.That(cycle, Does.Contain("BaseBuilderQueueEvalCA.LimitAdmits(baseBuilder.TryGetBuildingLimit(naval.Name, out var limit), limit,"));
			Assert.That(cycle, Does.Contain("BaseBuilderQueueEvalCA.OverflowTypeAdmits(standing, producing, ProductionTypeLimit)"));
		}

		// A vote-held cycle type is a stage cap: it costs no feasibility test and the walk goes on (to S7 or the roll, as the vote would have held it in the old ladder).
		[Test]
		public void AVoteHeldCycleTypeCostsNoTestAndTheWalkGoesOn()
		{
			var s = Snapshot(new() { [BaseBuildStage.S8ProductionCycle] = ["tent", "wf"] }, "barr");
			var f = new FakeFeasibility();
			f.StagesHeld.Add(BaseBuildStage.S8ProductionCycle);

			var pick = Plan(s, f);
			Assert.That(pick.Candidate, Is.EqualTo("barr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions));
			Assert.That(f.Tested, Is.EqualTo(new[] { "barr" }), "the held cycle types were never tested");
		}

		// Opus pin (Sonnet's C7 review): the cycle and the extra defences must run the admission votes (the pause votes and the mobile spend floor), as the old ladder did.
		[Test]
		public void TheCycleAndTheExtraDefencesRunTheAdmissionVotesAsTheOldLadderDid()
		{
			var d = Source(DefenceFile);
			var admits = Between(d, "bool PipelineStageAdmits(", "bool PipelineExtraDefenceAdmits(");
			Assert.That(admits, Does.Contain("case BaseBuildStage.S8ProductionCycle:\n\t\t\t\t\treturn PipelineCycleAdmits(queue, candidate.Name);"));
			Assert.That(admits, Does.Contain("case BaseBuildStage.S9ExtraDefence:\n\t\t\t\t\treturn PipelineExtraDefenceAdmits(queue, buildableNames, candidate.Name);"));

			var cap = Between(Source(CycleFile), "bool PipelineCycleAdmits(", "ActorInfo PipelineNavalCycleCandidate(");
			Assert.That(cap, Does.Contain("VotesAdmit(queue, info, SustainedActive ? RecoverySpendPolicyCA.Admission.EssentialBuilding : RecoverySpendPolicyCA.Admission.General)"));

			// The classes are the old ladder's: the sustained rung an essential building, the plain cycle General; the extra defence General (a non-essential roll pick).
			var qm = Source(QmFile);
			Assert.That(qm, Does.Contain("var sustained = ChooseCycledProduction(buildableThings, power, c => VotesAdmit(queue, c, RecoverySpendPolicyCA.Admission.EssentialBuilding));"));
			Assert.That(qm, Does.Contain("var cycled = ChooseCycledProduction(buildableThings, power, c => VotesAdmit(queue, c, RecoverySpendPolicyCA.Admission.General));"));
			Assert.That(Between(d, "bool PipelineExtraDefenceAdmits(", "static bool IsArmedDefenceInfo("),
				Does.Contain("&& VotesAdmit(queue, info, RecoverySpendPolicyCA.Admission.General);"));

			// VotesAdmit still carries both: the pause votes and the mobile spend floor.
			var votes = Between(qm, "bool VotesAdmit(", "bool EssentialsHolds(");
			Assert.That(votes, Does.Contain("p.PausesBuilding(item, essential, admission, cost)"));
			Assert.That(votes, Does.Contain("BaseBuilderQueueEvalCA.MobileSpendBlocks(playerResources.GetCashAndResources(), cost, mobileReserve?.MobileSpendFloor ?? 0, essential)"));

			// S4 stays exempt (D1: reserve 0, no vote), as the old anti-rush pick was floor-exempt.
			Assert.That(Between(d, "IEnumerable<string> PipelineAntiRushStage(", "IEnumerable<string> PipelineExtraDefenceStage("), Does.Not.Contain("VotesAdmit"));
		}

		[Test]
		public void OnlyTheAdmittedCycleCandidateMovesTheCycleAndTheOldLadderIsUntouched()
		{
			var p = Source(PipelineFile);
			Assert.That(p, Does.Contain("AdmitPlanned(bot, queue, item);\n\n\t\t\t// c6:"));
			Assert.That(p, Does.Contain("if (pick.Stage == BaseBuildStage.S8ProductionCycle)\n\t\t\t\tlastCycledProduction = pick.Candidate;"));
			Assert.That(Count(p, "lastCycledProduction ="), Is.EqualTo(1), "one write on the planned path, after the admission");

			var qm = Source(QmFile);
			Assert.That(Count(qm, "lastCycledProduction ="), Is.EqualTo(1), "the old ladder keeps its single write in ChooseCycledProduction");
			Assert.That(qm, Does.Contain("lastCycledProduction = candidates[index].Name;"));
			Assert.That(qm, Does.Contain("ActorInfo ChooseCycledProduction(IEnumerable<ActorInfo> buildableThings, ActorInfo power, Func<ActorInfo, bool> admits = null)"));
			Assert.That(qm, Does.Contain("var candidates = OverflowProductionCandidates(buildableThings, power);"), "the old caller still resolves PickOrPower (default)");
			Assert.That(qm, Does.Contain("List<ActorInfo> OverflowProductionCandidates(IEnumerable<ActorInfo> buildableThings, ActorInfo power, bool resolvePower = true)"));
			Assert.That(qm, Does.Contain("var resolved = resolvePower ? BaseBuilderQueueEvalCA.PickOrPower(actor, power, HasSufficientPowerForActor) : actor;"));
		}

		[Test]
		public void ThePlannerWalksTheTierOrderAndTheStageSourceAndSnapshotCarryTheTierInputs()
		{
			var plan = Source(PlanFile);
			Assert.That(plan, Does.Contain("foreach (var stage in BaseBuildTiersCA.Order(snapshot.SustainedActive, snapshot.TierLine))"));
			Assert.That(plan, Does.Contain("if (!BaseBuildTiersCA.Open(stage, snapshot.TierLine, snapshot.Bank, snapshot.LowCashCut, snapshot.SustainedActive, snapshot.DefenceThreatSeen, BaseBuildTiersCA.CycleHolds(cycleOffered, snapshot.S8RejectedFallthrough)))\n\t\t\t\t\tcontinue;"));
			Assert.That(plan, Does.Contain("cycleOffered |= stage == BaseBuildStage.S8ProductionCycle && candidate.Name != null;"));
			Assert.That(plan, Does.Contain("public bool LowCashCut;"));

			Assert.That(Source(StagesFile), Does.Contain("case BaseBuildStage.S8ProductionCycle: return PipelineProductionCycleStage(buildable);"));

			var p = Source(PipelineFile);
			Assert.That(p, Does.Contain("TierLine = TierLine,\n\t\t\t\tLowCashCut = EssentialsOn,\n\t\t\t\tSustainedActive = SustainedActive,"));
			Assert.That(p, Does.Contain("CashLedger.BuildingReserve(c.Stage, s.ProductionCashReserve)"), "A6: reserve 0 for S1-S7, the tier reserve for S8-S10 (P2 (a): the ledger line = ReserveFor)");
			Assert.That(p, Does.Contain("ProductionCashReserve = botLimits != null ? botLimits.Info.ProductionCashReserve : -1,"));
		}

		static int Count(string text, string needle)
		{
			var n = 0;
			for (var i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
				n++;

			return n;
		}
	}
}
