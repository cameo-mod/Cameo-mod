#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-BUSY-01 (USER P0 11-23: "never stop producing; keep all queues busy at all times"): pure decisions, yaml arms, rows.
	[TestFixture]
	public class ProdBusy01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai")
			.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string Field(MiniYamlNode node, string key) => node.Value.Nodes.Single(n => n.Key == key).Value.Value.Trim();

		[Test]
		public void AllFactoriesServesEveryQueueElseTheOldWidth()
		{
			Assert.That(UnitProducerServiceCA.OfferBudget(3, ProdBusyCA.QueuesPerCategory(true, false, 0)), Is.EqualTo(3));
			Assert.That(UnitProducerServiceCA.OfferBudget(1, ProdBusyCA.QueuesPerCategory(true, true, 0)), Is.EqualTo(1));
			Assert.That(ProdBusyCA.QueuesPerCategory(false, false, 0), Is.EqualTo(1), "off = one factory per category");
			Assert.That(ProdBusyCA.QueuesPerCategory(false, true, 0), Is.EqualTo(2), "off = two while saturated");
			Assert.That(ProdBusyCA.QueuesPerCategory(false, false, 4), Is.EqualTo(4), "off = the provider width");
		}

		[Test]
		public void AllCategoriesStopsOnlyUnderTheCashFloor()
		{
			// On: the MaximiseProductionCash bar no longer ends the sweep; only the bank left after this pass's orders under the floor does.
			Assert.That(ProdBusyCA.StopCategorySweep(true, false, 2000, 5000, 2000, 1000), Is.False);
			Assert.That(ProdBusyCA.StopCategorySweep(true, false, 2000, 5000, 999, 1000), Is.True, "the reserve exception");
			Assert.That(ProdBusyCA.StopCategorySweep(true, false, 2000, 5000, 1000, 1000), Is.False, "at the floor it serves on");

			// Off: the old break below MaximiseProductionCash unless saturated.
			Assert.That(ProdBusyCA.StopCategorySweep(false, false, 2000, 5000, 2000, 1000), Is.True);
			Assert.That(ProdBusyCA.StopCategorySweep(false, true, 2000, 5000, 2000, 1000), Is.False);
			Assert.That(ProdBusyCA.StopCategorySweep(false, false, 6000, 5000, 6000, 1000), Is.False);
		}

		[Test]
		public void BaseBuilderDecisions()
		{
			Assert.That(ProdBusyCA.QueuedThisSweepBlocks(false, true), Is.True, "off = one start per category per sweep");
			Assert.That(ProdBusyCA.QueuedThisSweepBlocks(true, true), Is.False);
			Assert.That(ProdBusyCA.QueuedThisSweepBlocks(false, false), Is.False);
			Assert.That(ProdBusyCA.ShortSweepDelay(false, false), Is.False, "off = an idle sweep waits the inactive delay");
			Assert.That(ProdBusyCA.ShortSweepDelay(false, true), Is.True);
			Assert.That(ProdBusyCA.ShortSweepDelay(true, false), Is.True);
		}

		[Test]
		public void SwitchesAreArmedOnTheGenericInstancesOnly()
		{
			var unit = PlayerNode("UnitBuilderBotModuleCA@generic");
			Assert.That(Field(unit, "SwitchCondition"), Is.EqualTo("genericbot"));
			Assert.That(Field(unit, "ServeAllFactories"), Is.EqualTo("true"));
			Assert.That(Field(unit, "ServeAllCategories"), Is.EqualTo("true"));

			var bases = PlayerNode("BaseBuilderBotModuleCA@generic");
			Assert.That(Field(bases, "SwitchCondition"), Is.EqualTo("genericbot"));
			Assert.That(Field(bases, "ServeAllBuildingProducers"), Is.EqualTo("true"));
			Assert.That(Field(bases, "BuildingIdleSweepActive"), Is.EqualTo("true"));
		}

		[Test]
		public void EachSwitchHasItsOwnRowAndIsSwitchGated()
		{
			var ids = AblationSpec.RegistryIds();
			foreach (var id in new[] { "pb_all_factories", "pb_all_categories", "pb_all_building_producers", "pb_building_idle_sweep" })
				Assert.That(ids, Does.Contain(id));

			var unit = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("bool AllFactoriesOn => switchesActive && Info.ServeAllFactories && !ablateAllFactories.Off(player, AblationIds.ProdBusyAllFactories);"));
			Assert.That(unit, Does.Contain("bool AllCategoriesOn => switchesActive && Info.ServeAllCategories && !ablateAllCategories.Off(player, AblationIds.ProdBusyAllCategories);"));
			Assert.That(unit, Does.Not.Contain("ProductionSaturationCA.StopCategorySweep("), "the sweep stop goes through ProdBusyCA");

			var bases = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bases, Does.Contain("public bool AllBuildingProducersArmed => Info.ServeAllBuildingProducers && switchesActive;"));
			Assert.That(bases, Does.Contain("public bool BuildingIdleSweepArmed => Info.BuildingIdleSweepActive && switchesActive;"));

			var queue = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(queue, Does.Contain("ablateAllProducers.Off(player, AblationIds.ProdBusyAllBuildingProducers)"));
			Assert.That(queue, Does.Contain("ablateIdleSweep.Off(player, AblationIds.ProdBusyBuildingIdleSweep)"));
			Assert.That(queue, Does.Not.Contain("cost, itemQueuedThisTick, starved)"), "the cash gate reads the switch-aware block");
		}
	}
}
