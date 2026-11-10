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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>BOTPASSIVE F3/F4 (design _06 §4-§5.4, _07 L2, _08 M3/N4/N5): the switch-ON urgency pieces.</summary>
	[TestFixture]
	public sealed class StalemateUrgencyTest
	{
		static readonly (int X, int Y) Start = (10, 10);

		[TestCase(1000, 0, 2000, 60, true)]
		[TestCase(1000, 200, 2000, 60, false)]
		[TestCase(0, 0, 0, 60, false)]
		[TestCase(5000, 0, 0, 60, false)]
		[TestCase(1199, 0, 2000, 60, true)]
		[TestCase(1200, 0, 2000, 60, false)]
		public void TeamRatioPressureCountsMutualAllies(long own, long allied, long enemy, int ratio, bool expected)
		{
			Assert.That(MasterAiEval.TeamRatioPressure(own, allied, enemy, ratio), Is.EqualTo(expected));
		}

		[Test]
		public void TeamRatioWithoutAlliesEqualsTheLegacyRatio()
		{
			for (var own = 0; own <= 3000; own += 37)
				foreach (var enemy in new[] { 0, 1, 999, 2000, 5000 })
				{
					var legacy = MasterAiEval.ClassifyUrgency(BotUrgency.Normal, false, enemy, own, 60) == BotUrgency.Pressured;
					Assert.That(MasterAiEval.TeamRatioPressure(own, 0, enemy, 60), Is.EqualTo(legacy), $"own={own} enemy={enemy}");
				}
		}

		[TestCase(false, false, false, false, false)]
		[TestCase(true, false, false, false, true)]
		[TestCase(false, true, false, false, false)]
		[TestCase(false, true, true, false, true)]
		[TestCase(false, true, false, true, true)]
		[TestCase(false, false, true, true, false)]
		public void EmergencyNeedsAHomeLossOrTheLossBranchAtHome(bool homeLoss, bool lossBranch, bool threat, bool latch, bool expected)
		{
			Assert.That(MasterAiEval.StalemateEmergency(homeLoss, lossBranch, threat, latch), Is.EqualTo(expected));
		}

		[Test]
		public void AnOutpostFightIsNotPressuredWhileTheTeamRatioHolds()
		{
			// Legacy: any enemy pressure (a far outpost) makes the bot Pressured.
			Assert.That(MasterAiEval.ClassifyUrgency(BotUrgency.Normal, true, 1000, 2000, 60), Is.EqualTo(BotUrgency.Pressured));

			// ON: no home threat, no latch, ratio fine.
			Assert.That(MasterAiEval.ClassifyUrgencyStalemate(BotUrgency.Normal, false, false, false), Is.EqualTo(BotUrgency.Normal));
			Assert.That(MasterAiEval.ClassifyUrgencyStalemate(BotUrgency.Normal, true, false, false), Is.EqualTo(BotUrgency.Pressured));
			Assert.That(MasterAiEval.ClassifyUrgencyStalemate(BotUrgency.Normal, false, true, false), Is.EqualTo(BotUrgency.Pressured));
			Assert.That(MasterAiEval.ClassifyUrgencyStalemate(BotUrgency.Normal, false, false, true), Is.EqualTo(BotUrgency.Pressured));
			Assert.That(MasterAiEval.ClassifyUrgencyStalemate(BotUrgency.Emergency, false, false, false), Is.EqualTo(BotUrgency.Emergency));
		}

		[Test]
		public void AnchorTestIsSquaredEuclidean()
		{
			var anchors = new List<(int X, int Y)> { (0, 0) };
			Assert.That(MasterAiEval.WithinAnyAnchor(20, 0, anchors, 20), Is.True);
			Assert.That(MasterAiEval.WithinAnyAnchor(12, 16, anchors, 20), Is.True);
			Assert.That(MasterAiEval.WithinAnyAnchor(13, 16, anchors, 20), Is.False);
			Assert.That(MasterAiEval.WithinAnyAnchor(21, 0, anchors, 20), Is.False);
			Assert.That(MasterAiEval.WithinAnyAnchor(0, 0, new List<(int X, int Y)>(), 20), Is.False);
		}

		[TestCase(0, 20)]
		[TestCase(18 * 1024, 20)]
		[TestCase(19 * 1024, 21)]
		[TestCase(19 * 1024 + 1, 22)]
		[TestCase(-5, 20)]
		public void HomeThreatRadiusIsTwentyOrRangePlusTwo(int rangeWorld, int expected)
		{
			Assert.That(MasterAiEval.HomeThreatRadiusCells(rangeWorld), Is.EqualTo(expected));
		}

		[Test]
		public void WithoutCyOrProductionTheStartCellIsTheAnchor()
		{
			Assert.That(StalemateUrgencyState.Anchors(Enumerable.Empty<(int X, int Y)>(), Start), Is.EqualTo(new[] { Start }));
			Assert.That(StalemateUrgencyState.Anchors(new[] { (50, 50) }, Start), Is.EqualTo(new[] { (50, 50) }));

			var state = new StalemateUrgencyState();
			Assert.That(state.StepHomeProductionLoss(25, new Dictionary<uint, (int X, int Y, bool Cy)>(), Start, 750, true), Is.False);
			Assert.That(state.PrevAnchors.Single(), Is.EqualTo(((uint)0, 10, 10)));
		}

		[Test]
		public void LosingTheCyIsAHomeLossEvenWithABarracksLeft()
		{
			var state = new StalemateUrgencyState();
			var live = new Dictionary<uint, (int X, int Y, bool Cy)> { [1] = (10, 10, true), [2] = (14, 10, false) };
			Assert.That(state.StepHomeProductionLoss(25, live, Start, 750, true), Is.False);
			live.Remove(1);
			Assert.That(state.StepHomeProductionLoss(50, live, Start, 750, true), Is.True);
			Assert.That(state.StepHomeProductionLoss(800, live, Start, 750, true), Is.True);
			Assert.That(state.StepHomeProductionLoss(801, live, Start, 750, true), Is.False);
		}

		[Test]
		public void AFarOutpostFactoryLossIsNotAHomeLoss()
		{
			var state = new StalemateUrgencyState();
			var live = new Dictionary<uint, (int X, int Y, bool Cy)>
			{
				[1] = (10, 10, true), [2] = (14, 10, false), [3] = (80, 80, false)
			};
			state.StepHomeProductionLoss(25, live, Start, 750, true);
			live.Remove(3);
			Assert.That(state.StepHomeProductionLoss(50, live, Start, 750, true), Is.False);
			live.Remove(2);
			Assert.That(state.StepHomeProductionLoss(75, live, Start, 750, true), Is.True, "a barracks 4 cells from the CY is home");
		}

		[Test]
		public void StateRoundTripsThroughSaveAndCopyFrom()
		{
			var state = new StalemateUrgencyState { HomeThreat = true, LegacyEmergency = true, LegacyUrgency = BotUrgency.Pressured, LastAppliedSwitchTick = 1450 };
			state.HomeLatch.Observe(1500, true, true);
			state.HomeLatch.Observe(1650, true, true);
			state.StepHomeProductionLoss(1675, new Dictionary<uint, (int X, int Y, bool Cy)> { [7] = (-3, 12, true), [9] = (5, 6, false) }, Start, 750, true);
			state.StepHomeProductionLoss(1700, new Dictionary<uint, (int X, int Y, bool Cy)> { [9] = (5, 6, false) }, Start, 750, true);

			var text = new List<MiniYamlNode> { new("S", "", state.Save()) }.WriteToString();
			var copy = new StalemateUrgencyState();
			copy.CopyFrom(StalemateUrgencyState.Load(MiniYaml.FromString(text, "test").Single().Value));

			Assert.That(new List<MiniYamlNode> { new("S", "", copy.Save()) }.WriteToString(), Is.EqualTo(text));
			Assert.That(copy.HomeLatch.Active, Is.True);
			Assert.That(copy.LastAppliedSwitchTick, Is.EqualTo(1450));
			Assert.That(copy.HomeProductionLossTicks, Is.EqualTo(new[] { 1700 }));
			Assert.That(copy.PrevAnchors, Is.EqualTo(new[] { ((uint)9, 5, 6) }));
		}

		[Test]
		public void RememberedTargetsExpireFifteenHundredTicksAfterTheLastSighting()
		{
			var seat = new OpenRA.Mods.CA.Traits.BotModules.Squads.StalemateBreakerSeatCA();
			Assert.That(seat.RememberedFresh(7, 100), Is.False, "never seen: not a legal remembered target");
			seat.TargetSeen[7] = 1000;
			Assert.That(seat.RememberedFresh(7, 2500), Is.True);
			Assert.That(seat.RememberedFresh(7, 2501), Is.False);
			Assert.That(seat.RememberedFresh(7, 999), Is.False);
			seat.PruneSightings(2500);
			Assert.That(seat.TargetSeen.ContainsKey(7), Is.True);
			seat.PruneSightings(2501);
			Assert.That(seat.TargetSeen.ContainsKey(7), Is.False);

			seat.TargetSeen[9] = 4000;
			var text = new List<MiniYamlNode> { new("S", "", seat.Save()) }.WriteToString();
			var loaded = OpenRA.Mods.CA.Traits.BotModules.Squads.StalemateBreakerSeatCA.Load(MiniYaml.FromString(text, "test").Single().Value);
			Assert.That(loaded.TargetSeen[9], Is.EqualTo(4000));
		}

		[Test]
		public void DwellHoldsThreeThousandTicksUnlessHomeEmergency()
		{
			// The initial personality counts as applied at tick 0 (design _03 §1).
			var p = new StalemateUrgencyState();
			Assert.That(OpenRA.Mods.CA.Traits.BotModules.Squads.StalemateBreakerPolicyCA.MaySwitchPersonality(true, false, 2999, p.LastAppliedSwitchTick), Is.False);
			Assert.That(OpenRA.Mods.CA.Traits.BotModules.Squads.StalemateBreakerPolicyCA.MaySwitchPersonality(true, false, 3000, p.LastAppliedSwitchTick), Is.True);
			Assert.That(OpenRA.Mods.CA.Traits.BotModules.Squads.StalemateBreakerPolicyCA.MaySwitchPersonality(true, true, 10, p.LastAppliedSwitchTick), Is.True);
		}

		[Test]
		public void OffSaveBytesAreTheLegacyBytesAndOnAppendsOneNode()
		{
			var saved = new MasterAiBotSavedState
			{
				CostCountersInitialized = true,
				PreviousDeathsCost = 100,
				CurrentUrgency = BotUrgency.Pressured,
				ProductionLossTicks = new[] { 40 },
				ProductionBuildings = new uint[] { 3 }
			};

			var off = new List<MiniYamlNode> { new("State", "", MasterAiBotModule.SerializeState(saved)) }.WriteToString();
			var offExplicit = new List<MiniYamlNode> { new("State", "", MasterAiBotModule.SerializeState(saved, null)) }.WriteToString();
			Assert.That(offExplicit, Is.EqualTo(off));
			Assert.That(off, Does.Not.Contain("Stalemate"));

			var onNodes = MasterAiBotModule.SerializeState(saved, new StalemateUrgencyState());
			var offNodes = MasterAiBotModule.SerializeState(saved);
			Assert.That(onNodes.Count, Is.EqualTo(offNodes.Count + 1));
			Assert.That(onNodes.Last().Key, Is.EqualTo("StalemateUrgency"));
			Assert.That(new List<MiniYamlNode> { new("State", "", onNodes.Take(offNodes.Count).ToList()) }.WriteToString(), Is.EqualTo(off));

			// The legacy loader ignores the extra node.
			var onText = new List<MiniYamlNode> { new("State", "", onNodes) }.WriteToString();
			var restored = MasterAiBotModule.DeserializeState(MiniYaml.FromString(onText, "test").Single().Value);
			Assert.That(restored.CurrentUrgency, Is.EqualTo(BotUrgency.Pressured));
		}
	}
}
