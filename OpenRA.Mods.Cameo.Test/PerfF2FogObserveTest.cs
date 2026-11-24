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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-F2a/b: BotFogMemory.Observe serves the ActorInfo-static part of a sighting from a per-type cache and reads the frozen layer once
	// per rebuild. The cache must give the record Classify gives, field for field, for every target-type mix; the rest is pinned in source.
	[TestFixture]
	public sealed class PerfF2FogObserveTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MasterAiBotModuleInfo Info(bool cache = true)
		{
			var info = new MasterAiBotModuleInfo();
			info.AircraftQueueNames.Add("AircraftSQ");
			typeof(MasterAiBotModuleInfo).GetField(nameof(MasterAiBotModuleInfo.UseFogClassifyCache)).SetValue(info, cache);
			return info;
		}

		static ValuedInfo Valued(int cost)
		{
			var valued = new ValuedInfo();
			typeof(ValuedInfo).GetField("Cost").SetValue(valued, cost);
			return valued;
		}

		static ProductionInfo Producing(params string[] queues)
		{
			var production = new ProductionInfo();
			typeof(ProductionInfo).GetField("Produces").SetValue(production, queues.ToImmutableArray());
			return production;
		}

		static ActorInfo[] Types() => new[]
		{
			new ActorInfo("tower", new BuildingInfo(), new AttackFrontalInfo(), Valued(700)),
			new ActorInfo("wall", new BuildingInfo(), Valued(50)),
			new ActorInfo("yard", new BuildingInfo(), new BaseBuildingInfo(), Valued(2000)),
			new ActorInfo("helipad", new BuildingInfo(), Producing("AircraftSQ"), Valued(500)),
			new ActorInfo("barracks", new BuildingInfo(), Producing("InfantrySQ")),
			new ActorInfo("refinery", new BuildingInfo(), new RefineryInfo()),
			new ActorInfo("harv", new MobileInfo(), new HarvesterInfo(), Valued(1100)),
			new ActorInfo("tank", new MobileInfo(), new AttackFrontalInfo(), Valued(800)),
			new ActorInfo("orca", new AircraftInfo(), new AttackFrontalInfo()),
			new ActorInfo("spawner", Producing("AircraftSQ")),
			new ActorInfo("bare"),
		};

		static BitSet<TargetableType>[] TargetMixes() => new[]
		{
			default,
			new BitSet<TargetableType>("Infantry"),
			new BitSet<TargetableType>("Vehicle"),
			new BitSet<TargetableType>("Ship"),
			new BitSet<TargetableType>("Defense"),
			new BitSet<TargetableType>("Infantry", "Defense"),
			new BitSet<TargetableType>("Vehicle", "Ship", "Defense"),
		};

		static void AssertSame(ObservedActor expected, ObservedActor actual, string what)
		{
			foreach (var field in typeof(ObservedActor).GetFields(BindingFlags.Public | BindingFlags.Instance))
				Assert.That(field.GetValue(actual), Is.EqualTo(field.GetValue(expected)), $"{what}: {field.Name}");
		}

		[Test]
		public void TheCachedRecordIsTheClassifyRecordFieldForField()
		{
			var info = Info();
			var memory = new BotFogMemory(null, info);
			foreach (var type in Types())
				foreach (var mix in TargetMixes())
					for (var pass = 0; pass < 2; pass++)
					{
						var expected = BotFogMemory.Classify(type, 7, new CPos(3, 4), mix, 100 + pass, info);
						var actual = memory.ClassifyObserved(type, 7, new CPos(3, 4), mix, 100 + pass);
						AssertSame(expected, actual, $"{type.Name} pass {pass}");
					}
		}

		[Test]
		public void TwoSightingsOfOneTypeAreTwoRecords()
		{
			var info = Info();
			var memory = new BotFogMemory(null, info);
			var type = Types()[0];
			var a = memory.ClassifyObserved(type, 1, new CPos(1, 1), default, 10);
			var b = memory.ClassifyObserved(type, 2, new CPos(9, 9), default, 20);
			Assert.That(a, Is.Not.SameAs(b));
			Assert.That(a.ActorID, Is.EqualTo(1u));
			Assert.That(b.ActorID, Is.EqualTo(2u));
			Assert.That(a.LastSeenTick, Is.EqualTo(10));
			a.Cloaked = true;
			Assert.That(b.Cloaked, Is.False, "the cached template is never handed out or shared");
		}

		[Test]
		public void SwitchedOffItIsTheOldClassify()
		{
			var info = Info(false);
			var memory = new BotFogMemory(null, info);
			foreach (var type in Types())
				AssertSame(BotFogMemory.Classify(type, 3, new CPos(2, 2), default, 5, info), memory.ClassifyObserved(type, 3, new CPos(2, 2), default, 5), type.Name);
		}

		[Test]
		public void ObserveUsesTheCacheAndTheSnapshotOnlyThroughTheirSwitches()
		{
			var fog = Source("mods/Cameo/Traits/BotModules/BotFogMemory.cs");
			Assert.That(fog, Does.Contain("!info.UseFogClassifyCache || ablateClassifyCache.Off(viewer, AblationIds.FogClassifyCache)"));
			// 7cb1a7b97 wraps this condition over two lines (the enemies.Count gate comes first).
			Assert.That(fog, Does.Contain("|| !info.UseSharedFrozenSnapshot"));
			Assert.That(fog, Does.Contain("|| ablateFrozenSnapshot.Off(viewer, AblationIds.FogFrozenSnapshot)"));
			var observe = fog.Substring(fog.IndexOf("public int Observe(", StringComparison.Ordinal), 2600);
			Assert.That(observe, Does.Not.Contain("Classify(actor.Info"), "both sightings go through ClassifyObserved");
			Assert.That(observe.Split("ClassifyObserved(").Length - 1, Is.EqualTo(2));
			Assert.That(observe, Does.Contain("fa.Owner != enemy"), "the owner check stays: the snapshot is an optimisation, not the filter");
			Assert.That(observe, Does.Contain("frozenSnapshot.Serves(viewer)"));
		}

		[Test]
		public void TheRebuildReadsTheFrozenLayerOncePerRebuildAndOnlyWhenFogged()
		{
			var situation = Source("mods/Cameo/Traits/BotModules/BotSituation.cs");
			Assert.That(situation, Does.Contain("var frozenSnapshot = fogged ? fogMemory.SnapshotFrozen(enemies) : null;"));
			Assert.That(situation, Does.Contain("fogMemory.Observe(enemy, enemyActors, tick, frozenSnapshot)"));
			Assert.That(situation.Split("SnapshotFrozen(").Length - 1, Is.EqualTo(1));
		}

		[Test]
		public void TheSnapshotKeepsWalkOrderPerOwnerAndTheSameValidityFilter()
		{
			var fog = Source("mods/Cameo/Traits/BotModules/BotFogMemory.cs");
			var snapshot = fog.Substring(fog.IndexOf("public FrozenSnapshot SnapshotFrozen(", StringComparison.Ordinal), 1100);
			Assert.That(snapshot, Does.Contain("frozen.FrozenActorsInRegion(viewer.World.Map.AllCells)"));
			Assert.That(snapshot, Does.Contain("enemies.Count < 2"));
			Assert.That(snapshot, Does.Contain("!enemies.Contains(owner)"));
			Assert.That(snapshot, Does.Contain("if (!fa.IsValid)"));
			Assert.That(snapshot, Does.Contain("list.Add(fa);"));
			Assert.That(snapshot, Does.Not.Contain("Sort").And.Not.Contain("OrderBy"));
		}

		[Test]
		public void TheSwitchesAreOnByDefaultAndAblatable()
		{
			var info = new MasterAiBotModuleInfo();
			Assert.That(info.UseFogClassifyCache, Is.True);
			Assert.That(info.UseSharedFrozenSnapshot, Is.True);
			Assert.That(AblationIds.FogClassifyCache, Is.EqualTo("pf_fog_classify"));
			Assert.That(AblationIds.FogFrozenSnapshot, Is.EqualTo("pf_fog_frozen"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pf_fog_classify").And.Contain("pf_fog_frozen"));
		}
	}
}
