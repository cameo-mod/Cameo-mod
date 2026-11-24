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
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// SUSTAINED-FIX-01 (P0 RCA of V02, seed 100774): the bank sat at 32.5k and production buildings stayed at 3 per seat for 40k ticks. The sustained rung
	// returned a second production building (a K6 General order); TickQueue then asked the army/building share vote, which held it, and returned false
	// WITHOUT trying the rest of the order, so the tech chain, research and upgrades starved behind it too (max tier 6 -> 5, research queue disabled).
	[TestFixture]
	public sealed class SustainedFix01Test
	{
		static string Queue() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs")).Replace("\r\n", "\n");

		[Test]
		public void TheV02ShareVoteHoldsAGeneralProductionBuildingButNotAnEssentialBuildingClassOrder()
		{
			// The logged V02 state: army spend 1800 against building spend 18600, target 625 permille, a 1000-cost barracks while 30k+ floats.
			var share = new ArmyShareCA();
			share.Refresh(1800, 0, 18600, 0);
			Assert.That(ArmyFirstEval.ShareHolds(false, RecoverySpendPolicyCA.Admission.General, share, 1000, 625), Is.True, "V02: the extra production building is held");
			Assert.That(ArmyFirstEval.ShareHolds(false, RecoverySpendPolicyCA.Admission.EssentialBuilding, share, 1000, 625), Is.False,
				"the sustained pick rides in the essential-building class, like a planner want");
		}

		[Test]
		public void WhenEveryProductionCandidateIsVetoedThePickIsNullSoTheOrderFallsThroughToTheTechChain()
		{
			// Three types at three buildings each (flat at 3 in V02). The queue manager removes the vetoed ones before Pick (pinned in source below);
			// an empty list gives no pick (the order falls through) and a single admitted type is still cycled.
			var counted = new[] { ("barracks", 3), ("warfactory", 3), ("airfield", 3) };
			Assert.That(CycleProductionCA.Pick(counted.Where(c => false).ToList(), null), Is.EqualTo(-1));

			// One admitted type is still cycled as before.
			var some = counted.Where(c => c.Item1 == "warfactory").ToList();
			Assert.That(some[CycleProductionCA.Pick(some, null)].Item1, Is.EqualTo("warfactory"));
		}

		[Test]
		public void TheSustainedAndCycleRungsFilterTheirCandidatesByTheVotesAndFallThrough()
		{
			var q = Queue();
			Assert.That(q, Does.Contain("ActorInfo ChooseCycledProduction(IEnumerable<ActorInfo> buildableThings, ActorInfo power, Func<ActorInfo, bool> admits = null)"));
			Assert.That(q, Does.Contain("if (admits != null)\n\t\t\t\tcandidates.RemoveAll(c => !admits(c));\n\n\t\t\tif (candidates.Count == 0)\n\t\t\t\treturn null;"),
				"the filter runs before the empty check, so every candidate vetoed = null = the order goes on");
			Assert.That(q, Does.Contain("ChooseCycledProduction(buildableThings, power, c => VotesAdmit(queue, c, RecoverySpendPolicyCA.Admission.EssentialBuilding))"));
			Assert.That(q, Does.Contain("ChooseCycledProduction(buildableThings, power, c => VotesAdmit(queue, c, RecoverySpendPolicyCA.Admission.General))"));
			Assert.That(q, Does.Contain("p.PausesBuilding(item, essential, admission, cost)"), "the same pause votes TickQueue asks");
			Assert.That(q, Does.Contain("EssentialsHolds(item, isRefinery, exempt && item.Name != sustainedPickName, cashNow)"), "the sustained pick is not exempt from the low-cash hold");
		}

		[Test]
		public void TheSustainedPickRidesAtItsOwnCostFloorInTheEssentialBuildingClass()
		{
			var q = Queue();
			var choose = q.IndexOf("ActorInfo ChooseBuildingToBuild(ProductionQueue queue)\n\t\t{\n\t\t\tsustainedPickName = null;", StringComparison.Ordinal);
			Assert.That(choose, Is.GreaterThan(0), "the flag is reset at the top of every choice");
			Assert.That(q, Does.Contain("sustainedPickName = sustained.Name;"));
			Assert.That(q, Does.Contain("var exempt = plannerWant || demandPick.Demand != null || (sustainedPickName != null && item.Name == sustainedPickName);"));
			Assert.That(q.IndexOf("sustainedPickName = sustained.Name;", StringComparison.Ordinal), Is.GreaterThan(choose));
		}
	}
}
