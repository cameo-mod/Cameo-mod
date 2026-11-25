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
	// CONSOLIDATE-01 c5 (CONSOLIDATE_01_DESIGN.md 3.1, section 4 row C5): stage S4 (the anti-rush base set, rulings D1 and D2) and stage S9 (the extra defences) of the staged
	// planner, and the threat-scaled defence cap as a pure function of the CA assembly. The pure rules are tested directly (the cap against the ArmyFirstEval it copies),
	// the stage order and fall-through through BaseBuildPlanCA with a stand-in for A1-A7, and the world-dependent wiring by pins. The switches bb_antirush_set and
	// bb_extra_defence are off by default and no yaml arms them (C7 does).
	[TestFixture]
	public sealed class Consolidate01C5Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string DefenceFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Defence.cs";
		const string StagesFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Stages.cs";
		const string PipelineFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs";

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
			// Each listed type already stands once: S5 (first copies) offers none of them, so these tests exercise the S10 roll.
			Owned = fractions.Distinct().ToDictionary(f => f, _ => 1),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = fractions.Select(f => new KeyValuePair<string, int>(f, 10)).ToArray(),
			StageSource = stage => sources.TryGetValue(stage, out var names) ? names : null,
		};

		// ---- DefenceCapCA: the same numbers as ArmyFirstEval.DefenceCap ----

		[Test]
		public void TheDefenceCapIsTheArmyFirstCapNumberForNumber()
		{
			Assert.That(DefenceCapCA.Cap(-1, 6, 1.0, false, 200), Is.EqualTo(-1), "negative base = no cap");
			Assert.That(DefenceCapCA.Cap(3, 6, 0.0, false, 200), Is.EqualTo(3), "quiet");
			Assert.That(DefenceCapCA.Cap(3, 6, 0.5, false, 200), Is.EqualTo(6));
			Assert.That(DefenceCapCA.Cap(3, 6, 0.49, false, 200), Is.EqualTo(5), "rounded down");
			Assert.That(DefenceCapCA.Cap(3, 6, 1.0, false, 200), Is.EqualTo(9), "full threat");
			Assert.That(DefenceCapCA.Cap(3, 6, 7.0, false, 200), Is.EqualTo(9), "intensity clamps to 1");
			Assert.That(DefenceCapCA.Cap(3, 6, -1.0, false, 200), Is.EqualTo(3), "intensity clamps to 0");
			Assert.That(DefenceCapCA.Cap(3, 6, 0.0, true, 200), Is.EqualTo(6), "turtle early-mid: 2x");
			Assert.That(DefenceCapCA.Cap(3, 6, 1.0, true, 200), Is.EqualTo(18));
			Assert.That(DefenceCapCA.Cap(3, -4, 1.0, false, 100), Is.EqualTo(3), "a negative per-threat term adds nothing");

			// Parity with the copied function over a grid (ArmyFirstBotModule is untouched, so the two must never drift apart).
			foreach (var baseCap in new[] { -1, 0, 3, 5 })
			foreach (var perThreat in new[] { -2, 0, 6 })
			foreach (var intensity in new[] { -0.5, 0.0, 0.25, 0.49, 0.5, 1.0, 3.0 })
			foreach (var turtle in new[] { false, true })
			foreach (var percent in new[] { 0, 100, 200 })
				Assert.That(DefenceCapCA.Cap(baseCap, perThreat, intensity, turtle, percent),
					Is.EqualTo(OpenRA.Mods.Cameo.Traits.ArmyFirstEval.DefenceCap(baseCap, perThreat, intensity, turtle, percent)),
					$"{baseCap}/{perThreat}/{intensity}/{turtle}/{percent}");

			foreach (var cap in new[] { -1, 0, 3 })
			foreach (var count in new[] { 0, 2, 3, 4 })
				Assert.That(DefenceCapCA.Holds(cap, count), Is.EqualTo(OpenRA.Mods.Cameo.Traits.ArmyFirstEval.DefenceCapHolds(cap, count)), $"{cap}/{count}");

			Assert.That(DefenceCapCA.Holds(-1, 99), Is.False, "no cap never holds");
		}

		[Test]
		public void TheCapReadsTheHighestThreatRoleAndTheTurtleBonusEndsAtItsTick()
		{
			Assert.That(DefenceCapCA.Intensity(0.1f, 0.7f, 0.3f), Is.EqualTo(0.7).Within(1e-6));
			Assert.That(DefenceCapCA.Intensity(0f, 0f, 0f), Is.EqualTo(0.0));

			Assert.That(DefenceCapCA.TurtleBonus(100, 27000, "turtle"), Is.True);
			Assert.That(DefenceCapCA.TurtleBonus(26999, 27000, "turtle"), Is.True);
			Assert.That(DefenceCapCA.TurtleBonus(27000, 27000, "turtle"), Is.False, "the bonus is early to mid game only");
			Assert.That(DefenceCapCA.TurtleBonus(100, 27000, "rush"), Is.False);
			Assert.That(DefenceCapCA.TurtleBonus(100, 27000, null), Is.False);
			Assert.That(DefenceCapCA.TurtleBonus(100, 0, "turtle"), Is.False, "unset end tick = no bonus");

			Assert.That(DefenceCapCA.IsArmedDefence(true, true), Is.True);
			Assert.That(DefenceCapCA.IsArmedDefence(true, false), Is.False, "walls and unarmed support are not defences");
			Assert.That(DefenceCapCA.IsArmedDefence(false, true), Is.False);
		}

		// ---- S4: the anti-rush set ----

		[Test]
		public void TheAntiRushOrderIsThePickFirstThenTheNextBestAndBounded()
		{
			var candidates = new List<(string Name, int Cost, int OwnedOrQueued)>
			{
				("pbox", 600, 1), ("gtwr", 500, 0), ("hbox", 400, 1), ("agun", 300, 0), ("bnkr", 300, 2),
			};

			var order = AntiRushCA.Order(candidates, 10);
			Assert.That(order, Is.EqualTo(new[] { "agun", "gtwr", "hbox", "pbox", "bnkr" }), "fewest copies, then cheapest, then name");
			Assert.That(order[0], Is.EqualTo(candidates[AntiRushCA.Pick(candidates)].Name), "the first is exactly what Pick takes");
			Assert.That(AntiRushCA.Order(candidates, 3), Is.EqualTo(new[] { "agun", "gtwr", "hbox" }), "bounded");
			Assert.That(AntiRushCA.Order(candidates, 0), Is.Empty);
			Assert.That(AntiRushCA.Order(new List<(string Name, int Cost, int OwnedOrQueued)>(), 3), Is.Empty);

			var ties = new List<(string Name, int Cost, int OwnedOrQueued)> { ("b", 100, 0), ("a", 100, 0) };
			Assert.That(AntiRushCA.Order(ties, 2), Is.EqualTo(new[] { "a", "b" }), "equal cost and count: ordinal name");
		}

		// D2: N = 1 at the lowest tiers up to 3 at the top on the straight line, Hard (rung 5, permille 625) = 2.
		[Test]
		public void TheSetSizeRunsOneToThreeOnTheTierLineAndHardIsTwo()
		{
			var sizes = Enumerable.Range(0, 11).Select(r => AntiRushCA.SetSize(500 + 25 * r, 1, 3)).ToArray();
			Assert.That(sizes, Is.EqualTo(new[] { 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3 }));
			Assert.That(AntiRushCA.SetSize(625, 1, 3), Is.EqualTo(2), "Hard");
			Assert.That(AntiRushCA.SetSize(-1, 1, 3), Is.EqualTo(1), "an unset tier line gives the minimum");
		}

		// X3: bank 1800 right after the opening, no enemy seen: the set comes before the core and the roll.
		[Test]
		public void TheAntiRushSetComesBeforeTheCoreAndTheFractionRollAndFallsThroughToTheNextType()
		{
			var f = new FakeFeasibility();
			var s = Snapshot(new()
			{
				[BaseBuildStage.S4AntiRush] = ["gtwr", "pbox"],
				[BaseBuildStage.S5CoreOnce] = ["radar"],
			}, "barr");
			s.Bank = 1800;

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Name, Is.EqualTo("gtwr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S4AntiRush));
			Assert.That(f.Tested, Is.EqualTo(new[] { "gtwr" }));

			f = new FakeFeasibility();
			f.Infeasible.Add("gtwr");
			pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Candidate, Is.EqualTo("pbox"), "a refused first type falls through to the next type of the set, not to a stall");
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S4AntiRush));

			f = new FakeFeasibility();
			f.Infeasible.Add("gtwr");
			f.Infeasible.Add("pbox");
			pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Candidate, Is.EqualTo("radar"), "an unaffordable set leaves the core stage its turn");
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S5CoreOnce));
		}

		// ---- S9: the extra defences ----

		[Test]
		public void TheExtraDefenceStageSitsAfterTheCycleAndBeforeTheFractionRoll()
		{
			var s = Snapshot(new()
			{
				[BaseBuildStage.S9ExtraDefence] = ["gtwr"],
				[BaseBuildStage.S8ProductionCycle] = ["tent"],
			}, "barr");

			var pick = BaseBuildPlanCA.Plan(s, new FakeFeasibility(), new MersenneTwister(1));
			Assert.That(pick.Candidate, Is.EqualTo("tent"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S8ProductionCycle), "the cycle comes first");

			s = Snapshot(new() { [BaseBuildStage.S9ExtraDefence] = ["gtwr"] }, "barr");
			pick = BaseBuildPlanCA.Plan(s, new FakeFeasibility(), new MersenneTwister(1));
			Assert.That(pick.Candidate, Is.EqualTo("gtwr"));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S9ExtraDefence), "a defence before the fraction roll");
		}

		[Test]
		public void AHeldExtraDefenceCostsNoTestAndTheRollGoesOn()
		{
			var f = new FakeFeasibility();
			f.StagesHeld.Add(BaseBuildStage.S9ExtraDefence);
			var s = Snapshot(new() { [BaseBuildStage.S9ExtraDefence] = ["gtwr", "pbox"] }, "barr");

			var pick = BaseBuildPlanCA.Plan(s, f, new MersenneTwister(1));
			Assert.That(pick.Stage, Is.EqualTo(BaseBuildStage.S10Fractions), "cap, need gate or ratio held the defences: the roll goes on");
			Assert.That(f.Tested, Is.EqualTo(new[] { "barr" }), "the held defences were never tested (stage caps cost no test)");
		}

		// ---- wiring pins ----

		[Test]
		public void TheStageSourcesAndTheStageCapsAreWired()
		{
			var stages = Source(StagesFile);
			Assert.That(stages, Does.Contain("case BaseBuildStage.S4AntiRush: return PipelineAntiRushStage(queue, buildable);"));
			Assert.That(stages, Does.Contain("case BaseBuildStage.S9ExtraDefence: return PipelineExtraDefenceStage(buildable);"));
			Assert.That(stages, Does.Contain("default: return null;"));

			Assert.That(Source(PipelineFile), Does.Contain("public bool StageAdmits(BaseBuildCandidate candidate) => qm.PipelineStageAdmits(queue, buildableNames, candidate);"));

			var d = Source(DefenceFile);
			var admits = Between(d, "bool PipelineStageAdmits(", "bool PipelineExtraDefenceAdmits(");
			Assert.That(admits, Does.Contain("case BaseBuildStage.S9ExtraDefence:\n\t\t\t\t\treturn PipelineExtraDefenceAdmits(buildableNames, candidate.Name);"));
			Assert.That(admits, Does.Contain("return PipelineFractionCapsAdmit(queue, buildableNames, candidate.Name)"), "S10 keeps its old caps");
			Assert.That(admits, Does.Contain("&& IsArmedDefenceInfo(info) && PipelineDefenceCapHolds()"), "and the cap for a defence");
			Assert.That(admits, Does.Contain("default:\n\t\t\t\t\treturn true;"), "S4 and every other stage: no world cap (D1)");
			Assert.That(admits, Does.Not.Contain("S4AntiRush"), "no S4 case: RATIO-2X, need gate and cap never veto the set");
		}

		// D1 + the V07 crash requirement: the S4 source holds nothing back, claims nothing and never dereferences a PickOrPower result.
		[Test]
		public void TheAntiRushStageIsAQueryWithoutRatioNeedGateOrPickOrPower()
		{
			var d = Source(DefenceFile);
			var s4 = Between(d, "IEnumerable<string> PipelineAntiRushStage(", "IEnumerable<string> PipelineExtraDefenceStage(");

			Assert.That(s4, Does.Contain("if (!AntiRushSetOn || playerBuildings == null)\n\t\t\t\tyield break;"));
			Assert.That(s4, Does.Contain("AntiRushCA.SetSize(botLimits != null ? botLimits.Info.ArmyShareTargetPermille : -1, b.AntiRushBaseMin, b.AntiRushBaseMax)"));
			Assert.That(s4, Does.Contain("AntiRushCA.Wants(playerBuildings.Any(a => b.ProductionTypes.Contains(a.Info.Name)), have, size)"));
			Assert.That(s4, Does.Contain("var bank = ProdBusyCA.SweepBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep);"));
			Assert.That(s4, Does.Contain("queue.GetProductionCost(info) > bank"), "only a set member the bank pays now: the K bound is not spent on unaffordable types");
			Assert.That(s4, Does.Contain("AntiRushCA.Order(candidates, AntiRushStageMaxCandidates)"));
			Assert.That(d, Does.Contain("const int AntiRushStageMaxCandidates = 3;"));

			Assert.That(s4, Does.Not.Contain("PickOrPower"), "no PickOrPower result to dereference (V07 NRE); A5 swaps the power plant and is null-safe");
			Assert.That(s4, Does.Not.Contain("RatioGate"), "D1: RATIO-2X never vetoes the set");
			Assert.That(s4, Does.Not.Contain("DefenceNeedHolds"), "no need gate");
			Assert.That(s4, Does.Not.Contain("DefenceThreatSeen"), "no seen-threat rule");
			Assert.That(s4, Does.Not.Contain("antiRushPickName"), "no exemption marker on the pipeline path");
			Assert.That(s4, Does.Not.Contain("TryClaim"));
			Assert.That(s4, Does.Not.Contain("StaticRatio.Record"));

			// The planner's A5 swap, which replaces PickOrPower here, is null-safe for a base with no power plant to build.
			Assert.That(Source(PipelineFile), Does.Contain("if (!HasSufficientPowerForActor(info) && power != null && power != info"));
		}

		[Test]
		public void TheExtraDefenceStageAndItsCapsFollowTheDesign()
		{
			var d = Source(DefenceFile);
			var s9 = Between(d, "IEnumerable<string> PipelineExtraDefenceStage(", "bool PipelineStageAdmits(");
			Assert.That(s9, Does.Contain("if (!ExtraDefenceOn || baseBuilder.Info.BuildingFractions == null || !baseBuilder.Info.DefenseQueues.Contains(Category) || PipelineDefenceCapHolds())"),
				"defence queue only, a fraction table (null on a bot without BuildingFractions), nothing while the cap is reached");
			Assert.That(s9, Does.Contain(".OrderBy(kv => kv.Key, StringComparer.Ordinal);"));
			Assert.That(s9, Does.Contain("defences.Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA)))"), "the synced bot random, ordinal keys first");
			Assert.That(s9, Does.Not.Contain("PickOrPower"));

			var caps = Between(d, "bool PipelineExtraDefenceAdmits(", "static bool IsArmedDefenceInfo(");
			Assert.That(caps, Does.Contain("BaseBuilderQueueEvalCA.AirDefenceAdmits(baseBuilder.IsGatedAirDefence(name), baseBuilder.DedicatedAntiAirAllowed)"));
			Assert.That(caps, Does.Contain("DefenceNeedOn && BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(info), DefenceThreatSeen())"), "the need gate, extras only");
			Assert.That(caps, Does.Contain("if (PipelineDefenceCapHolds())"));
			Assert.That(caps, Does.Contain("return !MobileStaticRatioCA.Holds(RatioGate(info, out _));"), "RATIO-2X with its FLOAT exemption");
			Assert.That(caps, Does.Not.Contain("VotesAdmit"), "the reserve is A6, the army-share cap is S10 only");

			Assert.That(d, Does.Contain("bool PipelineDefenceCapHolds() => baseBuilder.DefenceCapArmed && DefenceCapCA.Holds(PipelineDefenceCap(), PipelineArmedDefenceCount());"));
			Assert.That(d, Does.Contain("DefenceCapCA.Cap(b.DefenceCapBase, b.DefenceCapPerThreat, intensity, turtle, b.TurtleDefenceCapPercent)"));
			Assert.That(d, Does.Contain("defenceThreatAnalysis.GetThreatIntensity(BotThreatRoles.AntiAir, saturation)"));
			Assert.That(d, Does.Contain("DefenceCapCA.TurtleBonus(now, b.TurtleDefenceUntilTick, pipelinePersonality?.CurrentPersonality)"));
			Assert.That(d, Does.Contain("static bool IsArmedDefenceInfo(ActorInfo info) => DefenceCapCA.IsArmedDefence(info.HasTraitInfo<BuildingInfo>(), info.HasTraitInfo<AttackBaseInfo>());"));
		}

		[Test]
		public void TheSwitchesAreOffByDefaultTheCapIsOffAndNoYamlArmsThem()
		{
			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(module, Does.Contain("public readonly bool BbAntiRushSet = false;"));
			Assert.That(module, Does.Contain("public readonly bool BbExtraDefence = false;"));
			Assert.That(module, Does.Contain("public readonly int DefenceCapBase = -1;"), "no cap until C7 arms it");
			Assert.That(module, Does.Contain("public readonly int DefenceCapPerThreat = 0;"));
			Assert.That(module, Does.Contain("public readonly int DefenceThreatSaturationPercent = 300;"));
			Assert.That(module, Does.Contain("public readonly int TurtleDefenceCapPercent = 100;"));
			Assert.That(module, Does.Contain("public readonly int TurtleDefenceUntilTick = 0;"));
			Assert.That(module, Does.Contain("public bool AntiRushSetArmed => Info.BbAntiRushSet && switchesActive && !ablateAntiRushSet.Off(player, AblationIds.BbAntiRushSet);"));
			Assert.That(module, Does.Contain("public bool ExtraDefenceArmed => Info.BbExtraDefence && switchesActive && !ablateExtraDefence.Off(player, AblationIds.BbExtraDefence);"));
			Assert.That(module, Does.Contain("public bool DefenceCapArmed => Info.DefenceCapBase >= 0 && switchesActive && !ablateDefenceCap.Off(player, AblationIds.Af2DefenceCap);"));

			Assert.That(AblationIds.BbAntiRushSet, Is.EqualTo("bb_antirush_set"));
			Assert.That(AblationIds.BbExtraDefence, Is.EqualTo("bb_extra_defence"));
			Assert.That(AblationIds.Af2DefenceCap, Is.EqualTo("af2_defence_cap"), "the cap keeps the existing row");
		}

		[Test]
		public void ThePersonalityControllerPublishesItsNameToTheCaAssembly()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/IBotCurrentPersonality.cs"), Does.Contain("string CurrentPersonality { get; }"));

			var controller = Source("mods/Cameo/Traits/BotPersonalityController.cs");
			Assert.That(controller, Does.Contain("public class BotPersonalityController : ConditionalTrait<BotPersonalityControllerInfo>, IResolveOrder, OpenRA.Mods.CA.Traits.IBotCurrentPersonality"));
			Assert.That(controller, Does.Contain("public string CurrentPersonality { get; private set; } = \"\";"), "the existing property satisfies the interface");
		}
	}
}
