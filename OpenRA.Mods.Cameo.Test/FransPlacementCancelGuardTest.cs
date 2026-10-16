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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// Frans placement-cancel guard regressions (task 01a14475, C6-C14). Real
	// World shell + real ProductionQueue/ProductionItem in the
	// SuperweaponPlugLimitLifecycleTest harness style — no mocks of the traits
	// under test. The shared CA policy helper seam is exercised via the
	// cancelPermitted boolean parameter TryIssue already receives.
	[TestFixture]
	public class FransPlacementCancelGuardTest
	{
		const string Item = "td_gdi_tiberiumrefinery";

		sealed class RecordingBot : IBot
		{
			public readonly List<Order> Orders = [];
			public void QueueOrder(Order order) => Orders.Add(order);
		}

		public class SpatialIndex : DispatchProxy
		{
			public readonly List<Actor> Targets = [];
			protected override object Invoke(MethodInfo method, object[] args) => method.Name == "ActorsInBox"
				? Targets : throw new NotSupportedException(method.Name);
		}

		static void Set(object obj, string name, object value)
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var field = obj.GetType().GetField(name, flags)
				?? obj.GetType().GetField($"<{name}>k__BackingField", flags);
			field.SetValue(obj, value);
		}

		static T Info<T>(params (string Key, object Value)[] fields) where T : new()
		{
			var info = new T();
			foreach (var (key, value) in fields)
				Set(info, key, value);
			return info;
		}

		static readonly ConstructorInfo ActorCtor = typeof(Actor).GetConstructor(
			BindingFlags.Instance | BindingFlags.NonPublic, null,
			new[] { typeof(World), typeof(string), typeof(TypeDictionary) }, null);

		static readonly MethodInfo ActorInitialize = typeof(Actor).GetMethod("Initialize",
			BindingFlags.Instance | BindingFlags.NonPublic);

		public sealed class Fixture
		{
			public readonly World World;
			public readonly Player Player;
			public readonly Actor PlayerActor;
			public readonly ProductionQueue Queue;
			public readonly RecordingBot Bot = new();

			public Fixture()
			{
				Log.AddChannel("debug", null);

				World = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
				var traitDictType = typeof(World).Assembly.GetType("OpenRA.TraitDictionary");
				Set(World, "TraitDict", Activator.CreateInstance(traitDictType));
				Set(World, "SharedRandom", new MersenneTwister(17));

				var orderManager = RuntimeHelpers.GetUninitializedObject(typeof(OrderManager));
				Set(orderManager, "LobbyInfo", new Session());
				Set(World, "OrderManager", orderManager);

				var playerTraits = new List<TraitInfo>
				{
					new TechTreeInfo(),
					Info<PlayerResourcesInfo>(("DefaultCash", 5000)),
					Info<DeveloperModeInfo>(),
					Info<ProductionQueueInfo>(("Type", "Building")),
				};

				var actors = new Dictionary<string, ActorInfo>
				{
					["playeractor"] = new("playeractor", playerTraits.ToArray()),
					[Item] = new(Item,
						Info<BuildableInfo>(("Queue", new[] { "Building" }.ToFrozenSet())),
						Info<ValuedInfo>(("Cost", 800))),
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
				Set(Player, "InternalName", "frans");
				Set(Player, "World", World);
				Set(World, "Players", new[] { Player });

				PlayerActor = (Actor)ActorCtor.Invoke(new object[]
				{
					World, "playeractor", new TypeDictionary { new OwnerInit(Player) },
				});
				Set(Player, "PlayerActor", PlayerActor);
				ActorInitialize.Invoke(PlayerActor, new object[] { false });
				PlayerActor.GetType().GetProperty("IsInWorld",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(PlayerActor, true);
				Queue = PlayerActor.Trait<ProductionQueue>();
			}

			public List<ProductionItem> Items =>
				(List<ProductionItem>)typeof(ProductionQueue).GetField("Queue",
					BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Queue);

			public ProductionItem Enqueue(string name = Item)
			{
				var item = new ProductionItem(Queue, name, 800, null, null);
				Items.Add(item);
				return item;
			}
		}

		// ----------------------------------------------------------------
		// Engine-law target resolution: LastOrDefault same-name (tail-first)
		// ----------------------------------------------------------------

		[Test]
		public void EngineTarget_LastSameNameEntry()
		{
			var f = new Fixture();
			var first = f.Enqueue();
			f.Enqueue("other");
			var last = f.Enqueue();

			Assert.That(FransPlacementCancelGuard.EngineCancelTarget(f.Queue, Item), Is.SameAs(last));
			Assert.That(FransPlacementCancelGuard.EngineCancelTarget(f.Queue, Item), Is.Not.SameAs(first));
		}

		[Test]
		public void EngineTarget_AbsentName_Null()
		{
			var f = new Fixture();
			f.Enqueue("other");

			Assert.That(FransPlacementCancelGuard.EngineCancelTarget(f.Queue, Item), Is.Null);
		}

		// ----------------------------------------------------------------
		// TryIssue: permitted + unambiguous intended -> order issued
		// ----------------------------------------------------------------

		[Test]
		public void TryIssue_IssuesWhenPermittedAndIntendedIsLast()
		{
			var f = new Fixture();
			var item = f.Enqueue();

			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, true, out var hold);

			Assert.Multiple(() =>
			{
				Assert.That(issued, Is.True);
				Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(1));
				Assert.That(f.Bot.Orders[0].OrderString, Is.EqualTo("CancelProduction"));
			});
		}

		// ----------------------------------------------------------------
		// Ambiguous same-name target: reserved first vs actual engine last
		// ----------------------------------------------------------------

		[Test]
		public void TryIssue_IntendedFirstVsEngineLast_HoldsAmbiguous()
		{
			var f = new Fixture();
			var reservedFirst = f.Enqueue();
			var engineLast = f.Enqueue();

			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, reservedFirst, true, out var hold);

			Assert.Multiple(() =>
			{
				Assert.That(issued, Is.False);
				Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.AmbiguousTarget));
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items, Does.Contain(reservedFirst));
				Assert.That(f.Items, Does.Contain(engineLast));
			});
		}

		[Test]
		public void TryIssue_IntendedLast_IssuesDespiteEarlierSameName()
		{
			// The intent is the LAST same-name entry itself: identical to what the
			// engine would remove — no ambiguity, cancel proceeds.
			var f = new Fixture();
			var first = f.Enqueue();
			var intendedLast = f.Enqueue();

			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, intendedLast, true, out var hold);

			Assert.Multiple(() =>
			{
				Assert.That(issued, Is.True);
				Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(1));
			});
		}

		// ----------------------------------------------------------------
		// Protected / unresolvable classification -> hold, no order
		// ----------------------------------------------------------------

		[Test]
		public void TryIssue_NotPermitted_HoldsProtected()
		{
			var f = new Fixture();
			var item = f.Enqueue();

			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, false, out var hold);

			Assert.Multiple(() =>
			{
				Assert.That(issued, Is.False);
				Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.Protected));
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items, Does.Contain(item));
			});
		}

		// ----------------------------------------------------------------
		// Absent engine target: caller treats as done, no hold
		// ----------------------------------------------------------------

		[Test]
		public void TryIssue_NoEngineTarget_ReturnsFalseNoHold()
		{
			var f = new Fixture();
			f.Enqueue("other");

			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, null, true, out var hold);

			Assert.Multiple(() =>
			{
				Assert.That(issued, Is.False);
				Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders, Is.Empty);
			});
		}

		// ----------------------------------------------------------------
		// Name-only cancel (null intended): engine tail is authoritative
		// ----------------------------------------------------------------

		[Test]
		public void TryIssue_NullIntended_UsesEngineTail()
		{
			var f = new Fixture();
			f.Enqueue();
			var last = f.Enqueue();

			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, null, true, out var hold);

			Assert.Multiple(() =>
			{
				Assert.That(issued, Is.True);
				Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(1));
			});
		}
	}
}
