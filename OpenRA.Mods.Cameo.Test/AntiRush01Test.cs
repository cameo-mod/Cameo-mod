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

namespace OpenRA.Mods.Cameo.Test
{
	// EARLY-DEF-01 (USER 11-24 via Opus): every bot builds 2-3 anti-rush defences right after its first production building. They are essentials at low cash and exempt from the
	// opening "defence only on a seen threat" rule and from af2_defence_need_gate (those always meant EXTRA defences). The AF2 cap and RATIO-2X still count them.
	[TestFixture]
	public sealed class AntiRush01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[TestCase(-1, 2)]
		[TestCase(500, 2)]
		[TestCase(625, 3)] // r = 5: 2 + 0.5 rounds up
		[TestCase(750, 3)]
		public void TheSetSizeScalesWithTheTier(int permille, int expected) =>
			Assert.That(AntiRushCA.SetSize(permille, 2, 3), Is.EqualTo(expected));

		[Test]
		public void TheSetIsWantedOnlyAfterAProductionBuildingAndWhileBelowItsSize()
		{
			Assert.That(AntiRushCA.Wants(false, 0, 2), Is.False, "no production building yet");
			Assert.That(AntiRushCA.Wants(true, 0, 2), Is.True);
			Assert.That(AntiRushCA.Wants(true, 1, 2), Is.True);
			Assert.That(AntiRushCA.Wants(true, 2, 2), Is.False, "the set is complete; extras keep the gates");
			Assert.That(AntiRushCA.Wants(true, 0, 0), Is.False);
		}

		[Test]
		public void TheLeastOwnedThenCheapestTypeIsPicked()
		{
			var c = new List<(string, int, int)> { ("gun", 600, 1), ("mg", 300, 0), ("flame", 400, 0) };
			Assert.That(AntiRushCA.Pick(c), Is.EqualTo(1), "fewest owned, then cheapest");
			c = [("b", 300, 0), ("a", 300, 0)];
			Assert.That(AntiRushCA.Pick(c), Is.EqualTo(1), "name breaks the last tie");
			Assert.That(AntiRushCA.Pick([]), Is.EqualTo(-1));
		}

		[Test]
		public void WithABankOf600AndOnlyBarracksAndRefineryTheSetIsQueuedPastTheLowCashGates()
		{
			// Barracks + refinery stand, no defence yet, bank 600: the set is wanted and a 500-cost defence is picked.
			var size = AntiRushCA.SetSize(500, 2, 3);
			Assert.That(AntiRushCA.Wants(true, 0, size), Is.True);
			Assert.That(AntiRushCA.Pick([("turret", 500, 0), ("tower", 700, 0)]), Is.EqualTo(0));

			// The defence cash floor (2000) would hold it; as an essential-class pick it rides at its own cost.
			Assert.That(BaseBuilderQueueEvalCA.FloorBlocks(600, 2000, false, false, 500), Is.True, "an ordinary defence is held at 600");
			Assert.That(BaseBuilderQueueEvalCA.FloorBlocks(600, 2000, false, true, 500), Is.False, "the base set is not");
			Assert.That(BaseBuilderQueueEvalCA.BlockedByCash(600, 2000, false, true, 500, false), Is.False);

			// The low-cash hold (only essentials below the tier line) would hold a non-first-copy defence; the base set is exempt.
			Assert.That(EssentialsLowCashCA.Holds(true, 600, 3000, false, false, false, false, false, false), Is.True);
			Assert.That(EssentialsLowCashCA.Holds(true, 600, 3000, false, false, false, false, false, true), Is.False);
		}

		[Test]
		public void TheRungSitsBeforeTheOpeningOrderAndTheRidesAreWiredIntoTickQueue()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(q.IndexOf("ChooseAntiRushBase(queue, buildableThings, power, availableCash)", StringComparison.Ordinal),
				Is.LessThan(q.IndexOf("var openingStep = ChooseOpeningStandard(", StringComparison.Ordinal)), "right after the first production building, above the opening order");
			Assert.That(q, Does.Contain("\t\t\t\t\t|| antiRushPick\n"), "floor exempt");
			Assert.That(q, Does.Contain("MobileSpendBlocks(cashNow, cost, reserveFloor, itemEssential || antiRushPick)"), "mobile-spend reserve exempt");
			Assert.That(q, Does.Contain("|| (antiRushPickName != null && item.Name == antiRushPickName)); // C-04"), "RATIO first-copy exemption");

			// the extras keep both gates: the roll still holds ground defences before a threat is seen
			Assert.That(q, Does.Contain("BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(defenceCandidate), DefenceThreatSeen())"));

			// the AF2 cap vote still sees the item as non-essential: the pause call is made with itemEssential only
			Assert.That(q, Does.Contain("pauseBuilding.Any(p => p.PausesBuilding(item, itemEssential, buildingClass, cost))"));
			Assert.That(q, Does.Contain("pauseBuilding.Any(p => p.PausesBuilding(item, essential, buildingClass, cost))"));
		}

		[Test]
		public void OnlyGenericbotArmsTheRowAndTheRowIsRegistered()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.EarlyDef.cs"), Does.Contain("\"bb_antirush_base\""));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(generic.Single(n => n.Key == "AntiRushBase").Value.Value, Is.EqualTo("true"));

			foreach (var o in player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "AntiRushBase"), Is.False, o.Key);
		}
	}
}
