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

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-01 c1 (B07/X11, CONSOLIDATE_01_DESIGN.md aa041141): armed, BaseBuilder recounts the buildings in flight every bot tick whatever the
	// bank. Off (and classic) = the old recount, which ran only at bank >= BuildingProductionMinCashRequirement.
	[TestFixture]
	public sealed class Consolidate01C1Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// The loop body of BaseBuilderBotModuleCA's recount, over the head items of each building queue (null = an idle queue).
		static Dictionary<string, int> Recount(bool armed, int bank, int minCash, params string[] heads)
		{
			var counts = new Dictionary<string, int>();
			if (InFlightRecountCA.Recounts(armed, bank, minCash))
				foreach (var head in heads)
					if (head != null)
						InFlightRecountCA.Record(counts, head);

			return counts;
		}

		[Test]
		public void TwoProducersAtBank1200AreCountedOnlyWhenArmed()
		{
			var armed = Recount(true, 1200, 1500, "powr", "barr");
			Assert.That(armed, Is.EquivalentTo(new Dictionary<string, int> { ["powr"] = 1, ["barr"] = 1 }));
			Assert.That(Recount(false, 1200, 1500, "powr", "barr"), Is.Empty, "row off = the old rule: below the floor nothing in flight is seen (B07)");
		}

		[Test]
		public void TwoProducersOfOneTypeCountTwiceAndIdleQueuesNothing()
		{
			Assert.That(Recount(true, 0, 1500, "proc", null, "proc"), Is.EquivalentTo(new Dictionary<string, int> { ["proc"] = 2 }));
		}

		[Test]
		public void AtOrAboveTheFloorBothPathsAgree()
		{
			foreach (var bank in new[] { 1500, 1501, 9000 })
				Assert.That(Recount(false, bank, 1500, "powr", "barr"), Is.EquivalentTo(Recount(true, bank, 1500, "powr", "barr")), bank.ToString());
		}

		[Test]
		public void TheRecountUsesTheHelperAndCountsPowerUnderTheSameGate()
		{
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("if (InFlightRecountCA.Recounts(RecountInFlightArmed, playerResources.GetCashAndResources(), Info.BuildingProductionMinCashRequirement))"));
			Assert.That(bb, Does.Not.Contain("if (playerResources.GetCashAndResources() >= Info.BuildingProductionMinCashRequirement)"));
			var block = bb.Substring(bb.IndexOf("if (InFlightRecountCA.Recounts(", StringComparison.Ordinal), 900);
			Assert.That(block, Does.Contain("InFlightRecountCA.Record(BuildingsBeingProduced, producing.Item);"));
			Assert.That(block, Does.Contain("ExcessPower += producing.ActorInfo.TraitInfos<PowerInfo>()"), "in-flight power is counted under the same gate");
			Assert.That(bb, Does.Contain("Info.RecountInFlightAlways && switchesActive && !ablateRecountInFlight.Off(player, AblationIds.RecountInFlight)"));
		}

		[Test]
		public void TheSwitchIsOffByDefaultRegisteredAndOnlyGenericbotArmsIt()
		{
			Assert.That(new BaseBuilderBotModuleCAInfo().RecountInFlightAlways, Is.False);
			Assert.That(AblationIds.RecountInFlight, Is.EqualTo("bb_recount_in_flight"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("bb_recount_in_flight"));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(generic.Single(n => n.Key == "RecountInFlightAlways").Value.Value, Is.EqualTo("true"));
			foreach (var o in player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "RecountInFlightAlways"), Is.False, o.Key);
		}
	}
}
