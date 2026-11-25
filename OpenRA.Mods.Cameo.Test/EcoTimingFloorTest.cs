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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// ECO-TIMING-FLOOR (row eco_timing_floor, HANS_PROFILE_DESIGN_01 row 1, Opus V08G): the pure floor rule, the ladder, and the wiring pins.
	[TestFixture]
	public sealed class EcoTimingFloorTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheTargetStepsFromNothingToTwoToThreeWithTheLead()
		{
			Assert.That(EcoFloorRules.RefineryTarget(0, 3700, 5000, 500), Is.EqualTo(0), "no floor early: the opening order decides");
			Assert.That(EcoFloorRules.RefineryTarget(3199, 3700, 5000, 500), Is.EqualTo(0));
			Assert.That(EcoFloorRules.RefineryTarget(3200, 3700, 5000, 500), Is.EqualTo(2), "T2 - lead");
			Assert.That(EcoFloorRules.RefineryTarget(4499, 3700, 5000, 500), Is.EqualTo(2));
			Assert.That(EcoFloorRules.RefineryTarget(4500, 3700, 5000, 500), Is.EqualTo(3), "T3 - lead");
			Assert.That(EcoFloorRules.RefineryTarget(99999, 3700, 5000, 500), Is.EqualTo(3), "never more than 3 (the zone demand and the caps decide beyond it)");
		}

		[Test]
		public void TheFloorLiftsOnlyWhileTheOwnCountIsBelowTheTargetAndTheRowIsOn()
		{
			Assert.That(EcoFloorRules.Lifts(true, 1, 2), Is.True);
			Assert.That(EcoFloorRules.Lifts(true, 2, 2), Is.False, "the floor is met");
			Assert.That(EcoFloorRules.Lifts(true, 2, 3), Is.True);
			Assert.That(EcoFloorRules.Lifts(true, 5, 3), Is.False);
			Assert.That(EcoFloorRules.Lifts(true, 0, 0), Is.False, "no floor yet");
			Assert.That(EcoFloorRules.Lifts(false, 0, 3), Is.False, "row off");
		}

		[Test]
		public void TheFloorLiftsTheZoneCapButNeverTheOtherVerdicts()
		{
			// a zone at its cap (1 friendly of cap 1): the plain verdict is ZoneCap, the lifted cap (2) asks for one more
			Assert.That(RefineryDemand.Classify(true, true, false, 1, 1), Is.EqualTo(RefineryDemand.Verdict.ZoneCap));
			Assert.That(RefineryDemand.Classify(true, true, false, 1, 1 + 1), Is.EqualTo(RefineryDemand.Verdict.Demand));
			Assert.That(RefineryDemand.Classify(true, true, false, 2, 1 + 1), Is.EqualTo(RefineryDemand.Verdict.ZoneCap), "the lift is one refinery, not unbounded");

			// ally-served, out of radius and unreachable zones stay as they are
			Assert.That(RefineryDemand.Classify(true, true, true, 1, 1 + 1), Is.EqualTo(RefineryDemand.Verdict.AlliedServed));
			Assert.That(RefineryDemand.Classify(false, true, false, 1, 1 + 1), Is.EqualTo(RefineryDemand.Verdict.OutOfRadius));
			Assert.That(RefineryDemand.Classify(true, false, false, 1, 1 + 1), Is.EqualTo(RefineryDemand.Verdict.Unreachable));
		}

		[Test]
		public void TheTicksFollowTheLadderAroundTheHardValue()
		{
			var info = new ExpansionPlannerBotModuleInfo();
			Assert.That(info.EcoFloorRefinery2Tick, Is.EqualTo(3700));
			Assert.That(info.EcoFloorRefinery3Tick, Is.EqualTo(5000));
			Assert.That(info.EcoFloorStepTicks, Is.EqualTo(150));

			var n = BotDifficultyLadder.Difficulties.Length;
			var min = 3700 + EcoFloorRules.HardRank * info.EcoFloorStepTicks;
			var max = 3700 - (n - 1 - EcoFloorRules.HardRank) * info.EcoFloorStepTicks;
			for (var r = -1; r <= n - 1; r++)
				Assert.That(BotDifficultyLadder.InterpolateByRank(min, max, r, n), Is.EqualTo(3700 + (EcoFloorRules.HardRank - r) * 150), "rank " + r);

			Assert.That(BotDifficultyLadder.InterpolateByRank(min, max, EcoFloorRules.HardRank, n), Is.EqualTo(3700), "the Hans value is the Hard tier");
			Assert.That(Array.IndexOf(BotDifficultyLadder.Difficulties, "hard"), Is.EqualTo(EcoFloorRules.HardRank), "Hard is rank 4 on the ladder");
		}

		[Test]
		public void TheRowIsOffByDefaultWiredIntoBothCapReadsAndArmedForTheGenericPlanner()
		{
			Assert.That(new ExpansionPlannerBotModuleInfo().EcoTimingFloor, Is.False);
			Assert.That(AblationIds.EcoTimingFloor, Is.EqualTo("eco_timing_floor"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("eco_timing_floor"));

			var rd = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs");
			Assert.That(rd, Does.Contain("bool EcoFloorOn => Info.EcoTimingFloor && RefDemandArmed && !ablateEcoFloor.Off(player, AblationIds.EcoTimingFloor);"));
			Assert.That(rd, Does.Contain("RefineryDemand.Classify(inRadius, reachable, allied, count, ZoneCapAt(z) + EcoFloorLift());"));
			Assert.That(rd, Does.Contain("if (friendlyRefineryOwn[r])"), "only OWN refineries count toward the floor");

			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("RefDemandZones ? ZoneCapAt(field) + EcoFloorLift() : Info.RefineryFieldCap"), "the field gate reads the same lifted cap, or it would still bar the claim");

			var node = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(node.Value.Nodes.Any(n => n.Key == "EcoTimingFloor" && n.Value.Value == "true"), Is.True);
			Assert.That(node.Value.Nodes.Any(n => n.Key == "RefDemand" && n.Value.Value == "true"), Is.True, "the floor needs the zones");
		}
	}
}
