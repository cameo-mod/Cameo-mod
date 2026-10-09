#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.GameRules;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// Real-world lifecycle regressions for SuperweaponPlugLimit. Builds a minimal
	// World shell the same way AttackGarrisonedTest does: real Actor instances on
	// a reflection-initialized World with a synthetic Ruleset, a real TechTree,
	// ProductionQueue, ProvidesPrerequisite and Pluggable trait graph — no mocks
	// of the traits under test. Only the outer World/OrderManager/Map shells and
	// the spatial index are substituted; no renderer, assets, or server required.
	[TestFixture]
	public class SuperweaponPlugLimitLifecycleTest
	{
		const string PlugItem = "td_gdi_ioncannonuplink";
		const string PlugToken = "ionc";

		static void Set(object obj, string name, object value)
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var field = obj.GetType().GetField(name, flags)
				?? obj.GetType().GetField($"<{name}>k__BackingField", flags);
			field.SetValue(obj, value);
		}

		static void SetProp(object obj, string name, object value) => obj.GetType().GetProperty(name,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);

		static T Info<T>(params (string Key, object Value)[] fields) where T : new()
		{
			var info = new T();
			foreach (var (key, value) in fields)
				Set(info, key, value);
			return info;
		}

		public class SpatialIndex : DispatchProxy
		{
			public readonly List<Actor> Targets = [];
			protected override object Invoke(MethodInfo method, object[] args) => method.Name == "ActorsInBox"
				? Targets : throw new NotSupportedException(method.Name);
		}

		// The Actor ctor, Initialize and IsInWorld are internal to OpenRA.Game; this
		// test assembly does not have InternalsVisibleTo, so they are invoked by
		// reflection (the AttackGarrisonedTest harness in OpenRA.Test uses them
		// directly). Reflection on engine internals is confined to this fixture.
		static readonly ConstructorInfo ActorCtor = typeof(Actor).GetConstructor(
			BindingFlags.Instance | BindingFlags.NonPublic, null,
			new[] { typeof(World), typeof(string), typeof(TypeDictionary) }, null);

		static readonly MethodInfo ActorInitialize = typeof(Actor).GetMethod("Initialize",
			BindingFlags.Instance | BindingFlags.NonPublic);

		static Actor NewActor(World world, string name, Player owner)
		{
			return (Actor)ActorCtor.Invoke(new object[]
			{
				world, name, new TypeDictionary { new OwnerInit(owner) },
			});
		}

		public sealed class Fixture
		{
			public readonly World World;
			public readonly Player Player;
			public readonly Actor PlayerActor;
			public readonly Actor Host;
			public readonly ProductionQueue Queue;
			public readonly PlayerResources Resources;
			public readonly SuperweaponPlugLimit Limit = new();
			public readonly Queue<Action<World>> FrameEnd = new();
			public readonly ActorInfo PlugActorInfo;

			public Fixture(bool lobbyCap = true)
			{
				Log.AddChannel("debug", null);

				World = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
				var traitDictType = typeof(World).Assembly.GetType("OpenRA.TraitDictionary");
				Set(World, "TraitDict", Activator.CreateInstance(traitDictType));
				Set(World, "frameEndActions", FrameEnd);
				Set(World, "SharedRandom", new MersenneTwister(17));

				var orderManager = RuntimeHelpers.GetUninitializedObject(typeof(OrderManager));
				Set(orderManager, "LobbyInfo", new Session());
				Set(World, "OrderManager", orderManager);

				// Player actor carries TechTree + the resource account the refund
				// path writes to + the queue + (when lobbyCap) the global-swlimit
				// prerequisite that Limited mode provides in real rules.
				var playerTraits = new List<TraitInfo>
				{
					new TechTreeInfo(),
					Info<PlayerResourcesInfo>(("DefaultCash", 5000)),
					Info<DeveloperModeInfo>(),
					Info<ProductionQueueInfo>(("Type", "BuildingAddons")),
				};
				if (lobbyCap)
					playerTraits.Add(Info<ProvidesPrerequisiteInfo>(("Prerequisite", "global-swlimit")));

				var hostInfo = new ActorInfo("td_gdi_advancedcommunicationscenter",
					Info<ProvidesPrerequisiteInfo>(
						("Prerequisite", (object)PlugToken),
						("RequiresCondition", new BooleanExpression(PlugToken)),
						("RequiresPrerequisites", new[] { "global-swlimit" }.ToImmutableArray())),
					Info<PluggableInfo>(
						("Conditions", new Dictionary<string, string> { ["td_gdi_advancedcommunicationscenter"] = PlugToken }.ToFrozenDictionary())));

				PlugActorInfo = new ActorInfo(PlugItem,
					Info<PlugInfo>(("Type", "td_gdi_advancedcommunicationscenter")),
					Info<BuildableInfo>(
						("Prerequisites", new[] { "!ionc" }.ToImmutableArray()),
						("Queue", new[] { "BuildingAddons" }.ToFrozenSet())));

				var actors = new Dictionary<string, ActorInfo>
				{
					["playeractor"] = new("playeractor", playerTraits.ToArray()),
					["td_gdi_advancedcommunicationscenter"] = hostInfo,
					[PlugItem] = PlugActorInfo,
				};

				var rules = new Ruleset(actors, new Dictionary<string, WeaponInfo>(),
					new Dictionary<string, SoundInfo>(), new Dictionary<string, SoundInfo>(),
					new Dictionary<string, MusicInfo>(), new Dictionary<string, PlaylistDefinition>(),
					null, null);
				var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
				typeof(Map).GetProperty("Rules").SetValue(map, rules);
				Set(World, "Map", map);
				Set(World, "ActorMap", DispatchProxy.Create<IActorMap, SpatialIndex>());

				Player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
				Set(Player, "Playable", true);
				Set(Player, "IsBot", true);
				Set(Player, "Faction", new FactionInfo());
				Set(Player, "InternalName", "own");
				Set(Player, "World", World);
				Set(World, "Players", new[] { Player });

				// PlayerActor must be registered on the Player before Initialize:
				// ProductionQueue/ProvidesPrerequisite resolve it during Created.
				PlayerActor = NewActor(World, "playeractor", Player);
				Set(Player, "PlayerActor", PlayerActor);
				ActorInitialize.Invoke(PlayerActor, new object[] { false });
				SetProp(PlayerActor, "IsInWorld", true);
				Queue = PlayerActor.Trait<ProductionQueue>();
				Resources = PlayerActor.Trait<PlayerResources>();

				// Refund assertions need headroom — in-game capacity comes from silos.
				Resources.ResourceCapacity = 100000;

				Host = NewActor(World, "td_gdi_advancedcommunicationscenter", Player);
				ActorInitialize.Invoke(Host, new object[] { false });
				SetProp(Host, "IsInWorld", true);
			}

			public List<ProductionItem> Items =>
				(List<ProductionItem>)typeof(ProductionQueue).GetField("Queue",
					BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Queue);

			public ProductionItem Enqueue(int totalCost = 1000, int remainingCost = 0,
				int resourcesPaid = 0, bool done = false, bool infinite = false)
			{
				var item = new ProductionItem(Queue, PlugItem, totalCost, null, null)
				{
					RemainingCost = remainingCost,
					ResourcesPaid = resourcesPaid,
					Infinite = infinite,
				};
				if (done)
					SetProp(item, "Done", true);
				Items.Add(item);
				return item;
			}

			public void Install() => Host.Trait<Pluggable>().EnablePlug(Host, "td_gdi_advancedcommunicationscenter");

			public void Tick()
			{
				Limit.Tick(PlayerActor);
				while (FrameEnd.Count > 0)
					FrameEnd.Dequeue()(World);
			}

			public bool Validate(string targetString, uint extraData = 1)
			{
				var order = new Order("StartProduction", PlayerActor, false)
				{
					TargetString = targetString,
					ExtraData = extraData,
				};
				return Limit.OrderValidation(null, World, 0, order);
			}
		}

		// ----------------------------------------------------------------
		// Admission gate over real queue state
		// ----------------------------------------------------------------

		[Test]
		public void OrderGate_FirstPlug_Admits()
		{
			var f = new Fixture();
			Assert.That(f.Validate(PlugItem), Is.True);
		}

		[Test]
		public void OrderGate_PendingSecondPlug_Rejects()
		{
			var f = new Fixture();
			f.Enqueue();
			Assert.That(f.Validate(PlugItem), Is.False);
		}

		[Test]
		public void OrderGate_InstalledPlug_Rejects()
		{
			var f = new Fixture();
			f.Install();
			Assert.That(f.Validate(PlugItem), Is.False);
		}

		[Test]
		public void OrderGate_BatchOfTwo_Rejects()
		{
			var f = new Fixture();
			Assert.That(f.Validate(PlugItem, extraData: 2), Is.False);
		}

		[Test]
		public void OrderGate_NoLobbyCap_NeverGates()
		{
			var f = new Fixture(lobbyCap: false);
			f.Enqueue();
			Assert.That(f.Validate(PlugItem), Is.True);
		}

		[Test]
		public void OrderGate_NonSwItem_Passes()
		{
			var f = new Fixture();
			Assert.That(f.Validate("td_gdi_riflesoldier"), Is.True);
		}

		// ----------------------------------------------------------------
		// Installed-only CanBuild wall (the PlacePlug install boundary)
		// ----------------------------------------------------------------

		[Test]
		public void Install_FlipsCanBuild_SameFrame()
		{
			// The prerequisite chain must drop CanBuild synchronously on install —
			// this is the engine mechanism that prevents a second same-token
			// PlacePlug in the same frame-end drain.
			var f = new Fixture();
			Assert.Multiple(() =>
			{
				Assert.That(f.Queue.CanBuild(f.PlugActorInfo), Is.True);
				Assert.That(f.Player.PlayerActor.Trait<TechTree>().HasPrerequisites(new[] { PlugToken }), Is.False);
			});

			f.Install();

			Assert.Multiple(() =>
			{
				Assert.That(f.Player.PlayerActor.Trait<TechTree>().HasPrerequisites(new[] { PlugToken }), Is.True);
				Assert.That(f.Queue.CanBuild(f.PlugActorInfo), Is.False);
			});
		}

		// ----------------------------------------------------------------
		// Frame-end reconciliation
		// ----------------------------------------------------------------

		[Test]
		public void Sweep_OverCapPending_RemovesLatestOnly()
		{
			var f = new Fixture();
			var first = f.Enqueue();
			var second = f.Enqueue();

			f.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(f.Items, Does.Contain(first));
				Assert.That(f.Items, Does.Not.Contain(second));
				Assert.That(f.Items.Count, Is.EqualTo(1));
			});
		}

		[Test]
		public void Sweep_Installed_RemovesAllPending()
		{
			var f = new Fixture();
			var pending = f.Enqueue();
			f.Install();

			f.Tick();

			Assert.That(f.Items, Does.Not.Contain(pending));
		}

		[Test]
		public void Sweep_UnderCap_KeepsPending()
		{
			var f = new Fixture();
			var pending = f.Enqueue();

			f.Tick();

			Assert.That(f.Items, Does.Contain(pending));
		}

		[Test]
		public void Sweep_NoLobbyCap_KeepsOverCapPending()
		{
			// Unlimited mode must never reconcile (ordinary infinite/unlimited
			// queues preserved).
			var f = new Fixture(lobbyCap: false);
			var a = f.Enqueue();
			var b = f.Enqueue();

			f.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(f.Items, Does.Contain(a));
				Assert.That(f.Items, Does.Contain(b));
			});
		}

		[Test]
		public void Sweep_InfiniteItem_RemovedWithoutReplenish()
		{
			var f = new Fixture();
			var first = f.Enqueue();
			var infinite = f.Enqueue(infinite: true);

			f.Tick();

			// The excess infinite item is removed and NOT re-added by
			// EndProduction's infinite-replenishment (Infinite cleared first).
			Assert.Multiple(() =>
			{
				Assert.That(f.Items, Does.Contain(first));
				Assert.That(f.Items, Does.Not.Contain(infinite));
				Assert.That(f.Items.Count, Is.EqualTo(1));
				Assert.That(infinite.Infinite, Is.False);
			});
		}

		[Test]
		public void Sweep_ProgressedDoneMigration_RefundsAndRemoves()
		{
			// The integrator's required regression: an item migrated into the
			// queue already Done and paid (progressed/Done incoming migration)
			// must be swept and fully refunded — 600 resources + 400 cash, not
			// 600 + 1000 (the F1 double-credit bug).
			var f = new Fixture();
			var legit = f.Enqueue();
			var migrated = f.Enqueue(totalCost: 1000, remainingCost: 0, resourcesPaid: 600, done: true);

			var cashBefore = f.Resources.Cash;
			var resBefore = f.Resources.Resources;

			f.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(f.Items, Does.Contain(legit));
				Assert.That(f.Items, Does.Not.Contain(migrated));
				Assert.That(f.Resources.Resources - resBefore, Is.EqualTo(600));
				Assert.That(f.Resources.Cash - cashBefore, Is.EqualTo(400));
			});
		}

		[Test]
		public void Sweep_InstalledCountNeverExceedsCap_PlusRefund()
		{
			// Installed occupancy plus pending never exceeds cap after the drain:
			// installed plug stays installed (1 == cap), pending item is removed.
			var f = new Fixture();
			f.Install();
			var migrated = f.Enqueue(totalCost: 1000, remainingCost: 0, resourcesPaid: 0, done: true);

			var cashBefore = f.Resources.Cash;
			f.Tick();

			Assert.Multiple(() =>
			{
				Assert.That(f.Items, Does.Not.Contain(migrated));
				Assert.That(f.Player.PlayerActor.Trait<TechTree>().HasPrerequisites(new[] { PlugToken }), Is.True);
				Assert.That(f.Resources.Cash - cashBefore, Is.EqualTo(1000));
			});
		}

		[Test]
		public void Tick_EnqueuesSingleSweepPerTick()
		{
			var f = new Fixture();
			f.Enqueue();
			f.Enqueue();

			f.Limit.Tick(f.PlayerActor);
			f.Limit.Tick(f.PlayerActor);
			Assert.That(f.FrameEnd.Count, Is.EqualTo(1));

			// After the drain a fresh tick schedules a fresh sweep.
			while (f.FrameEnd.Count > 0)
				f.FrameEnd.Dequeue()(f.World);
			f.Limit.Tick(f.PlayerActor);
			Assert.That(f.FrameEnd.Count, Is.EqualTo(1));
		}
	}

	// Pins the real shipped yaml: exactly four plug hosts may carry a
	// global-swlimit-gated @swlimit ProvidesPrerequisite, and each SW plug item
	// must negate its occupancy token in Buildable.Prerequisites. A fifth wired
	// host or a missing negated token fails this test — it is the repo-level
	// counterpart of TokenMap_ExactlyFourActiveMappings.
	[TestFixture]
	public class SuperweaponPlugLimitYamlTest
	{
		static readonly (string Fixture, string Host, string Token, string Item)[] Expected =
		[
			("td_gdi_buildings.yaml", "td_gdi_advancedcommunicationscenter", "ionc", "td_gdi_ioncannonuplink"),
			("td_nod_buildings.yaml", "td_nod_templeofnod", "nodnuke", "td_nod_nuclearmissilesilo"),
			("cabal_buildings.yaml", "cabal_core", "cabalnuke_swlimit", "cabal_missilesilo"),
			("ts_gdi_buildings.yaml", "ts_gdi_upgradecenter", "tsionc", "ts_gdi_ioncannonuplink"),
		];

		static string FixturePath(string name) =>
			Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "swcap", name);

		// Collect (host, @swlimit-block-lines) pairs: actor headers sit at column
		// 0, trait instances at one tab, their fields deeper.
		static List<(string Host, List<string> Block)> SwlimitBlocks(string path)
		{
			var blocks = new List<(string, List<string>)>();
			string actor = null;
			List<string> current = null;
			var currentIndent = 0;
			foreach (var raw in File.ReadLines(path))
			{
				var line = raw.TrimEnd();
				if (line.Length == 0 || line.TrimStart() == "#")
					continue;

				var indent = line.TakeWhile(c => c == '\t').Count();
				if (indent == 0)
				{
					actor = line.Split(':')[0].Trim();
					current = null;
					continue;
				}

				if (indent == 1)
				{
					current = line.TrimStart() == "ProvidesPrerequisite@swlimit:" ? [] : null;
					currentIndent = indent;
					if (current != null)
						blocks.Add((actor, current));
					continue;
				}

				if (current != null && indent <= currentIndent)
					current = null;

				current?.Add(line.Trim());
			}

			return blocks;
		}

		[Test]
		public void Yaml_ExactlyFourSwlimitGatedHosts()
		{
			// Scan every fixture file (the four host files plus the file that
			// defines the CABAL plug): a fifth lobby-cap-gated @swlimit provider
			// anywhere in these files trips the exact-four assertion.
			var files = Expected.Select(e => e.Fixture)
				.Concat(new[] { "cabal_defenses.yaml" }).Distinct();
			var gated = new Dictionary<string, string>();
			foreach (var file in files)
				foreach (var (blockHost, block) in SwlimitBlocks(FixturePath(file)))
					if (block.Any(l => l.StartsWith("RequiresPrerequisites:") && l.Contains("global-swlimit")))
						gated[blockHost] = block
							.Where(l => l.StartsWith("Prerequisite:"))
							.Select(l => l.Split(':')[1].Trim())
							.FirstOrDefault();

			Assert.Multiple(() =>
			{
				Assert.That(gated.Count, Is.EqualTo(4));
				foreach (var (_, host, token, _) in Expected)
					Assert.That(gated[host], Is.EqualTo(token), $"missing or wrong token on {host}");
			});
		}

		[Test]
		public void Yaml_EachSwPlugItemNegatesOccupancyToken()
		{
			// cabal_missilesilo lives in defenses.yaml; the rest are colocated.
			var itemFile = new Dictionary<string, string>
			{
				["td_gdi_ioncannonuplink"] = "td_gdi_buildings.yaml",
				["td_nod_nuclearmissilesilo"] = "td_nod_buildings.yaml",
				["cabal_missilesilo"] = "cabal_defenses.yaml",
				["ts_gdi_ioncannonuplink"] = "ts_gdi_buildings.yaml",
			};

			foreach (var (_, _, token, item) in Expected)
			{
				var lines = File.ReadAllLines(FixturePath(itemFile[item]));
				var inActor = false;
				var negates = false;
				foreach (var raw in lines)
				{
					var line = raw.TrimEnd();
					if (line.Length == 0)
						continue;

					var indent = line.TakeWhile(c => c == '\t').Count();
					if (indent == 0)
					{
						inActor = line.Split(':')[0].Trim() == item;
						continue;
					}

					if (inActor && line.Contains("Prerequisites:") && line.Contains("!" + token))
						negates = true;
				}

				Assert.That(negates, Is.True, $"{item} does not negate !{token}");
			}
		}
	}
}
