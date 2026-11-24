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
	// INFANTRY-IDLE-01 (USER P0, V03): the infantry queues sat idle with ~10k banked while Vehicle stayed busy. An empty infantry queue that the normal
	// pick (role mix / share table / composition) leaves without a unit takes the best buildable unit; every admission gate after the pick still applies.
	[TestFixture]
	public sealed class InfantryIdle01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheFloorPicksTheHeaviestWeightThenTheCheapestThenTheName()
		{
			var c = new[] { ("e1", 100, 10), ("e3", 300, 30), ("e2", 150, 30), ("e6", 150, 30) };
			Assert.That(c[IdleQueueFloorCA.Pick(c)].Item1, Is.EqualTo("e2"), "weight 30 ties: cheapest 150, then ordinal name");
		}

		[Test]
		public void AnEmptyCandidateListGivesNoPick()
		{
			Assert.That(IdleQueueFloorCA.Pick(Array.Empty<(string, int, int)>()), Is.EqualTo(-1));
		}

		[Test]
		public void AZeroWeightUnitStillBeatsNothing()
		{
			var c = new[] { ("e3", 300, 0), ("e1", 100, 0) };
			Assert.That(c[IdleQueueFloorCA.Pick(c)].Item1, Is.EqualTo("e1"));
		}

		[Test]
		public void TheFairLoopFloorRunsOnlyForAnEmptyQueueAfterTheNormalPickAndBeforeTheAdmissionGates()
		{
			var s = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var pick = s.IndexOf("unit == null && held == 0 && IdleQueueFloorOn", StringComparison.Ordinal);
			var normal = s.IndexOf(": ChooseUnitToBuild(queue, excludeLimited, saturated, shares, skip));", StringComparison.Ordinal);
			var nullBreak = s.IndexOf("RevertToBaselineComposition();", pick, StringComparison.Ordinal);
			var gate = s.IndexOf("ShouldBuild(name, baselineTried || fromIdleFloor, queue.Info.Type)", StringComparison.Ordinal);
			Assert.That(pick, Is.GreaterThan(normal));
			Assert.That(pick, Is.LessThan(nullBreak), "the floor runs before the empty-pick break");
			Assert.That(gate, Is.GreaterThan(pick), "delays, intervals, limits, caps and the afford gates still run on the floor pick");
			Assert.That(s, Does.Contain("Array.IndexOf(Info.IdleQueueFloorQueues, queue.Info.Type) >= 0"));
			Assert.That(s, Does.Contain("bool IdleQueueFloorOn => switchesActive && Info.IdleQueueFloor && !ablateIdleQueueFloor.Off(player, AblationIds.ProdBusyIdleQueueFloor);"));
		}

		[Test]
		public void TheFloorCandidatesExcludeRefusedNamesAndStillCheckTheLimitsAndAircraftCap()
		{
			var s = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(s, Does.Contain("if (skip.Contains(item.Name) || !ShouldBuild(item.Name, true, queue.Info.Type) || !CanBuildMoreOfAircraft(item))"));
		}

		[Test]
		public void TheRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.ProdBusy.cs"), Does.Contain("ProdBusyIdleQueueFloor = \"pb_idle_queue_floor\""));
			var yaml = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var armed = yaml.Where(n => n.Key.StartsWith("UnitBuilderBotModuleCA@", StringComparison.Ordinal)
				&& n.Value.Nodes.Any(x => x.Key == "IdleQueueFloor" && x.Value.Value == "true")).ToArray();
			Assert.That(armed.Length, Is.EqualTo(1));
			Assert.That(armed[0].Key, Is.EqualTo("UnitBuilderBotModuleCA@generic"));
			Assert.That(armed[0].Value.Nodes.Single(x => x.Key == "RequiresCondition").Value.Value, Does.Contain("genericbot"));
		}
	}
}
