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

using System;
using System.Collections.Generic;
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

		// E1 (lead ruling 2026-10-12): the per-enemy latch — once an enemy's air was seen the
		// gate stays open against that enemy for the match; stale sightings never close it.

		[Test]
		public void AStaleSightingKeepsTheLatchOpen()
		{
			var latched = new HashSet<string>(StringComparer.Ordinal);
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => true), Is.True,
				"the live sighting opens the gate");
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => false), Is.True,
				"the expired sighting must not close it again");
		}

		[Test]
		public void TheLatchIsPerEnemyPlayer()
		{
			var latched = new HashSet<string>(StringComparer.Ordinal);
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => true), Is.True);
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy2", () => false), Is.False,
				"enemy2 never showed air — its gate stays closed");
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => false), Is.True);
		}

		[Test]
		public void ANeverSightedEnemyStaysClosedAndTheScanStaysLazy()
		{
			var latched = new HashSet<string>(StringComparer.Ordinal);
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => false), Is.False);
			Assert.That(latched, Is.Empty, "a closed query must not record a latch");

			MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => true);
			var scanned = false;
			Assert.That(MasterAiBotModule.NoteEnemyAirSighting(latched, "enemy1", () => { scanned = true; return false; }), Is.True);
			Assert.That(scanned, Is.False, "a latched enemy skips the fog scan entirely");
		}
	}
}
