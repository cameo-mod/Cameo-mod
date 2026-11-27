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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// BASE-FIX-01 (AUDIT_BASE_01 + AUDIT_CONFIG_01, Opus 11-24): opening war-factory step, essentials above sustained, vote-filtered picks, defence need gate,
	// power margin cap, barracks-first on every tier, first-copy defences counted, essentials exempt from the building cash floor. Each row is genericbot only.
	[TestFixture]
	public sealed class BaseFix01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Queue() => Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");

		// Simulates the opening: every sweep builds the role NextRole names (a Refinery result is built by the refinery rung), counts grow, until nothing is left.
		static System.Collections.Generic.List<OpeningRole> Simulate(bool secondBarracksFirst, System.Collections.Generic.ISet<OpeningRole> lacking)
		{
			var have = Enum.GetValues<OpeningRole>().ToDictionary(r => r, _ => 0);
			var built = new System.Collections.Generic.List<OpeningRole>();
			var order = BaseFix01CA.StandardOrder(secondBarracksFirst);
			for (var i = 0; i < 40; i++)
			{
				var next = BaseFix01CA.NextRole(order, r => (!lacking.Contains(r), have[r]), (r, n) => false);
				if (next == OpeningRole.None)
					break;

				have[next]++;
				built.Add(next);
			}

			return built;
		}

		[Test]
		public void TheStandardOrderIsFollowedForAFactionWithEveryRole()
		{
			var built = Simulate(false, new System.Collections.Generic.HashSet<OpeningRole>());
			Assert.That(built, Is.EqualTo(new[]
			{
				OpeningRole.Power, OpeningRole.Barracks, OpeningRole.Refinery, OpeningRole.WarFactory, OpeningRole.Refinery, OpeningRole.WarFactory, OpeningRole.Radar,
				OpeningRole.Airfield, OpeningRole.Barracks, OpeningRole.TechCenter, OpeningRole.Barracks, OpeningRole.WarFactory, OpeningRole.PassiveIncome,
			}));

			var swapped = Simulate(true, new System.Collections.Generic.HashSet<OpeningRole>());
			Assert.That(swapped[5], Is.EqualTo(OpeningRole.Barracks), "a barracks-first faction builds its 2nd barracks before the 2nd war factory");
			Assert.That(swapped[8], Is.EqualTo(OpeningRole.WarFactory));
		}

		[Test]
		public void AWarFactoryIsBuiltWithinTheFirstFiveBuildingsForEveryFactionShape()
		{
			// Any subset of the optional roles a faction may lack (no barracks, no radar, no airfield, no tech chain, no trickler) and both second-production orders.
			var optional = new[] { OpeningRole.Barracks, OpeningRole.Refinery, OpeningRole.Radar, OpeningRole.Airfield, OpeningRole.TechCenter, OpeningRole.PassiveIncome };
			for (var mask = 0; mask < 1 << optional.Length; mask++)
			{
				var lacking = new System.Collections.Generic.HashSet<OpeningRole>(optional.Where((_, i) => (mask & (1 << i)) != 0));
				foreach (var barracksFirst in new[] { false, true })
				{
					var built = Simulate(barracksFirst, lacking);
					var warFactory = built.IndexOf(OpeningRole.WarFactory);
					Assert.That(warFactory, Is.InRange(0, 4), $"mask {mask} barracksFirst {barracksFirst}: {string.Join(",", built)}");
				}
			}
		}

		[Test]
		public void AClosedRefinerySlotIsSkippedNotWaitedOn()
		{
			// CM FIX: when the wanted-refinery rung has nothing to pick (adequate count, max, unsafe) the slot is closed and the war factory still comes within the first 5.
			foreach (var barracksFirst in new[] { false, true })
			{
				var built = Simulate(barracksFirst, new System.Collections.Generic.HashSet<OpeningRole> { OpeningRole.Refinery });
				Assert.That(built, Does.Not.Contain(OpeningRole.Refinery));
				Assert.That(built.IndexOf(OpeningRole.WarFactory), Is.InRange(0, 4));
			}
		}

		[Test]
		public void TheRefinerySlotMirrorsTheWantedRefineryRungAndASatisfiedStepForgetsItsTimer()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(q, Does.Contain("pick = ChooseWantedRefinery(queue, buildable, power);"));
			Assert.That(q, Does.Contain("(role, required) => openingStepSince.Remove((role, required))"));
			var cleared = new System.Collections.Generic.List<(OpeningRole, int)>();
			var order = BaseFix01CA.StandardOrder(false);
			BaseFix01CA.NextRole(order, r => (true, r == OpeningRole.Power ? 1 : 0), (r, n) => false, (r, n) => cleared.Add((r, n)));
			Assert.That(cleared, Is.EqualTo(new[] { (OpeningRole.Power, 1) }));
		}

		[Test]
		public void AStepStuckTooLongIsSkippedSoTheOrderNeverDeadlocks()
		{
			var order = BaseFix01CA.StandardOrder(false);
			var next = BaseFix01CA.NextRole(order, r => (true, r == OpeningRole.Power ? 1 : 0), (r, n) => r == OpeningRole.Barracks);
			Assert.That(next, Is.EqualTo(OpeningRole.Refinery), "the stuck barracks is skipped");
			Assert.That(BaseFix01CA.NextRole(order, r => (false, 0), (r, n) => false), Is.EqualTo(OpeningRole.None), "a faction with nothing buildable");
		}

		[Test]
		public void ThePowerMarginIsCappedButNeverBelowTheBaseMargin()
		{
			Assert.That(BaseFix01CA.CapPowerMargin(true, 1000, 100, 300), Is.EqualTo(300));
			Assert.That(BaseFix01CA.CapPowerMargin(true, 200, 100, 300), Is.EqualTo(200));
			Assert.That(BaseFix01CA.CapPowerMargin(true, 1000, 500, 300), Is.EqualTo(500), "never below MinimumExcessPower");
			Assert.That(BaseFix01CA.CapPowerMargin(false, 1000, 100, 300), Is.EqualTo(1000), "switch off");
		}

		[Test]
		public void AGroundDefenceWaitsForAThreatOnlyWhenTheGateIsOn()
		{
			Assert.That(BaseFix01CA.DefenceNeedHolds(true, true, false), Is.True);
			Assert.That(BaseFix01CA.DefenceNeedHolds(true, true, true), Is.False, "threat seen");
			Assert.That(BaseFix01CA.DefenceNeedHolds(true, false, false), Is.False, "not a ground defence");
			Assert.That(BaseFix01CA.DefenceNeedHolds(false, true, false), Is.False, "switch off");
		}

		[Test]
		public void TheOpeningRungSitsAboveTheRefineryPickAndTheSustainedRungKeepsEssentialsFirst()
		{
			var q = Queue();
			var opening = q.IndexOf("var openingStep = ChooseOpeningStandard(queue, buildableThings, power);", StringComparison.Ordinal);
			Assert.That(opening, Is.GreaterThan(0));
			Assert.That(opening, Is.LessThan(q.IndexOf("var wantedRefinery = ChooseWantedRefinery(queue, buildableThings, power);", StringComparison.Ordinal)));
			var kept = q.IndexOf("var keptEssential = ChooseFirstCopyOnce(queue, buildableThings, power, true);", StringComparison.Ordinal);
			Assert.That(kept, Is.GreaterThan(q.IndexOf("var expansionRefinery = ChooseWantedRefinery", StringComparison.Ordinal)));
			Assert.That(kept, Is.LessThan(q.IndexOf("var sustained = ChooseCycledProduction(", StringComparison.Ordinal)), "essentials before the sustained production pick");
			Assert.That(q, Does.Contain("if (includeListed && (BuildingStillDelayed(item.Name)"), "a techrush-guarded building never comes early through the sustained rung");
		}

		[Test]
		public void TheOverflowScalingThresholdNavalSiloAndRadarPicksAreVoteFilteredBeforeTheyAreReturned()
		{
			var q = Queue();
			Assert.That(q, Does.Contain("overflowCandidates.RemoveAll(c => !Admitted(queue, c));"));
			foreach (var pick in new[] { "scalingPick", "pick", "navalPick", "siloPick", "radarPick" })
				Assert.That(q, Does.Contain($"if ({pick} != null && Admitted(queue, {pick}))"), pick);

			Assert.That(q, Does.Contain("pick == null || !PrefilterOn || VotesAdmit(queue, pick, RecoverySpendPolicyCA.Admission.General)"));
		}

		[Test]
		public void TheDefenceGateSitsInTheFractionRollAndSpareTheAntiAirGate()
		{
			var q = Queue();
			Assert.That(q, Does.Contain("BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(defenceCandidate), DefenceThreatSeen())"));
			Assert.That(q, Does.Contain("if (b.AntiAirTypes.Contains(info.Name))\n\t\t\t\treturn false;"), "anti-air has its own intel gate");
			Assert.That(q, Does.Contain("PowerCapOn, baseBuilder.Info.PowerMarginCap, BuildOrderPowerMilli());"));
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/PowerArbiter.cs"), Does.Contain("BaseFix01CA.CapPowerMargin(capOn, margin, min, cap);"));
		}

		[Test]
		public void TheFloorExemptsEssentialsAndRatioCountsFirstCopyDefencesUnlessAThreatIsSeen()
		{
			var q = Queue();
			Assert.That(q, Does.Contain("|| (FloorExemptOn && itemEssential);"));
			Assert.That(q.IndexOf("var itemEssential = IsArmyFirstEssential(item);", StringComparison.Ordinal),
				Is.LessThan(q.IndexOf("|| (FloorExemptOn && itemEssential);", StringComparison.Ordinal)), "itemEssential is known before exempt");
			Assert.That(q, Does.Contain("&& (!DefenceFirstCopyOn || DefenceThreatSeen())"));

			var af = Source("mods/Cameo/Traits/BotModules/ArmyFirstBotModule.cs");
			Assert.That(af, Does.Contain("if (isFirstCopy && isDefence && info.DefenceFirstCopyCounts && !ablateDefenceFirstCopy.Off(player, AblationIds.Af2DefenceFirstCopy))\n\t\t\t\t\tisFirstCopy = false;"));
		}

		[Test]
		public void EveryRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			var ids = Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.BaseFix01.cs");
			foreach (var id in new[] { "opening_standard_order", "pb_sustained_keeps_essentials", "pb_prefilter_picks", "af2_defence_need_gate", "pb_power_margin_cap",
				"af2_defence_first_copy", "pb_floor_essentials_exempt" })
				Assert.That(ids, Does.Contain($"\"{id}\""), id);

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			foreach (var key in new[] { "OpeningStandardOrder", "SustainedKeepsEssentials", "PrefilterBuildingPicks", "DefenceNeedGate", "PowerMarginCapEnabled",
				"DefenceFirstCopyCounts", "FloorEssentialsExempt" })
				Assert.That(generic.Single(n => n.Key == key).Value.Value, Is.EqualTo("true"), key);

			// no other base builder instance (classic) carries any of them
			var others = player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic");
			foreach (var o in others)
				Assert.That(o.Value.Nodes.Any(n => n.Key is "OpeningStandardOrder" or "SustainedKeepsEssentials" or "PrefilterBuildingPicks" or "DefenceNeedGate"
					or "PowerMarginCapEnabled" or "DefenceFirstCopyCounts" or "FloorEssentialsExempt"), Is.False, o.Key);

			var af02 = player.Single(n => n.Key == "ArmyFirstBotModule@af02").Value.Nodes;
			Assert.That(af02.Single(n => n.Key == "DefenceFirstCopyCounts").Value.Value, Is.EqualTo("true"));
		}

		static System.Collections.Generic.IEnumerable<MiniYamlNode> Flatten(MiniYamlNode n) => new[] { n }.Concat(n.Value.Nodes.SelectMany(Flatten));

		[Test]
		public void EveryTierOpensBarracksBeforeTheFirstRefinery()
		{
			var limits = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").SelectMany(Flatten).Where(n => n.Key.StartsWith("BotLimits@", StringComparison.Ordinal)).ToArray();
			foreach (var tier in new[] { "noob", "easiest", "veryeasy", "easy", "medium", "hard", "veryhard", "brutal", "challenger", "unbeatable", "god" })
			{
				var block = limits.Single(n => n.Key == "BotLimits@" + tier);
				Assert.That(block.Value.Nodes.Single(n => n.Key == "PrioritizeBarracksBeforeRefinery").Value.Value, Is.EqualTo("true"), tier);
			}
		}
	}
}
