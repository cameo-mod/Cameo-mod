#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Immutable;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// S2 (FINDINGS_2026-10-12 §4): the fog-honest air intel behind IBotEnemyAirIntelProvider —
	// a remembered enemy aircraft OR a remembered enemy aircraft-producing structure ungates
	// dedicated anti-air. ProducesAircraft is derived from ProductionInfo.Produces intersecting
	// the ruleset's aircraft queue names (AircraftQueueNames), never a hand-list.
	[TestFixture]
	public sealed class BotFogMemoryAirTest
	{
		static MasterAiBotModuleInfo InfoWithAircraftQueues()
		{
			var info = new MasterAiBotModuleInfo();
			info.AircraftQueueNames.Add("AircraftSQ");
			return info;
		}

		static ProductionInfo Producing(params string[] queues)
		{
			var production = new ProductionInfo();
			typeof(ProductionInfo).GetField("Produces").SetValue(production, queues.ToImmutableArray());
			return production;
		}

		static ObservedActor Seen(ActorInfo actorInfo, MasterAiBotModuleInfo info) =>
			BotFogMemory.Classify(actorInfo, 1, new CPos(4, 4), new BitSet<TargetableType>(), 100, info);

		[Test]
		public void AnAirProducerIsRememberedAsOne()
		{
			var info = InfoWithAircraftQueues();
			var helipad = new ActorInfo("helipad", new BuildingInfo(), Producing("AircraftSQ"));
			Assert.That(Seen(helipad, info).ProducesAircraft, Is.True);
		}

		[Test]
		public void AGroundProducerIsNot()
		{
			var info = InfoWithAircraftQueues();
			var barracks = new ActorInfo("barracks", new BuildingInfo(), Producing("InfantrySQ"));
			Assert.That(Seen(barracks, info).ProducesAircraft, Is.False);
		}

		[Test]
		public void OnlyBuildingsProduce()
		{
			// A mobile spawner (drop-pod, queen) can feed an aircraft queue without being a
			// building — it is still not "aircraft production": the intel means the enemy
			// invested in an air plant.
			var info = InfoWithAircraftQueues();
			var spawner = new ActorInfo("spawner", Producing("AircraftSQ"));
			Assert.That(Seen(spawner, info).ProducesAircraft, Is.False);
		}

		[Test]
		public void AnAircraftIsAnAircraft()
		{
			var info = InfoWithAircraftQueues();
			var orca = new ActorInfo("orca", new AircraftInfo());
			Assert.That(Seen(orca, info).Aircraft, Is.True);
			Assert.That(Seen(orca, info).ProducesAircraft, Is.False);
		}
	}
}
