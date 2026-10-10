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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// Runtime regression for GroundUnitsConcaveStateCA: a placement planned while a
	// member was alive keeps that Actor reference after the member is destroyed —
	// SquadCA's unit list is pruned by the manager, but the state's `placed` list is
	// not. The commit checks then read the stale actor's traits
	// (HpOf -> TraitOrDefault -> CheckDestroyed) and the engine throws
	// InvalidOperationException; the PT7 all-arm hit it as a fatal "trait from
	// destroyed object". The fix prunes dead placements before the commit checks.
	// These tests drive the real Tick on a real SquadCA: the world shells and the
	// spatial index are substituted, the state and its members are real — the same
	// harness shape as SuperweaponPlugLimitLifecycleTest. Internal state classes
	// have no InternalsVisibleTo here, so state construction/seeding goes through
	// reflection; the behaviour under test is the production path, not a stub.
	[TestFixture]
	public class ConcaveDeadMemberRuntimeTest
	{
		static readonly WPos Anchor = new(50000, 50000, 0);

		static void Set(object obj, string name, object value)
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var type = obj.GetType();
			while (type != null)
			{
				var field = type.GetField(name, flags) ?? type.GetField($"<{name}>k__BackingField", flags);
				if (field != null)
				{
					field.SetValue(obj, value);
					return;
				}

				type = type.BaseType;
			}

			throw new MissingFieldException($"{obj.GetType().Name}.{name}");
		}

		static object Get(object obj, string name)
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var type = obj.GetType();
			while (type != null)
			{
				var field = type.GetField(name, flags) ?? type.GetField($"<{name}>k__BackingField", flags);
				if (field != null)
					return field.GetValue(obj);
				type = type.BaseType;
			}

			throw new MissingFieldException($"{obj.GetType().Name}.{name}");
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

		// Actor ctor is internal to OpenRA.Game and this assembly has no
		// InternalsVisibleTo — same reflection route as the plug-limit fixture.
		static readonly ConstructorInfo ActorCtor = typeof(Actor).GetConstructor(
			BindingFlags.Instance | BindingFlags.NonPublic, null,
			new[] { typeof(World), typeof(string), typeof(TypeDictionary) }, null);

		static Actor NewActor(World world, string name, Player owner)
		{
			return (Actor)ActorCtor.Invoke(new object[]
			{
				world, name, new TypeDictionary { new OwnerInit(owner) },
			});
		}

		// The merged roles provider is genericbot-gated and absent on classic —
		// production treats a missing provider as "feature off", so a null-map
		// stub is the faithful stand-in.
		sealed class NoRoles : IBotUnitRoles
		{
			public IReadOnlyDictionary<string, HashSet<string>> RoleMembers => null;
			public IReadOnlyDictionary<string, HashSet<string>> ActorRoles => null;
			public string PrimaryRoleOf(string actorName) => null;
		}

		sealed class FakeBot : IBot
		{
			public readonly List<Order> Orders = new();
			readonly Player player;

			public FakeBot(Player player) { this.player = player; }
			public void Activate(Player p) { }
			public void QueueOrder(Order order) { Orders.Add(order); }
			public IBotInfo Info => null;
			public Player Player => player;
		}

		sealed class Fixture
		{
			public readonly World World;
			public readonly Player Player;
			public readonly SquadManagerBotModuleCA Manager;
			public readonly FakeBot Bot;
			public readonly SquadCA Squad;
			public readonly object State;
			public readonly Type StateType;
			public readonly Type PlacedType;
			public readonly IList PlacedList;

			public Fixture(int assemblePercent)
			{
				Log.AddChannel("debug", null);

				World = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
				var traitDictType = typeof(World).Assembly.GetType("OpenRA.TraitDictionary");
				Set(World, "TraitDict", Activator.CreateInstance(traitDictType));
				Set(World, "frameEndActions", new Queue<Action<World>>());
				Set(World, "SharedRandom", new MersenneTwister(17));

				var orderManager = RuntimeHelpers.GetUninitializedObject(typeof(OrderManager));
				Set(orderManager, "LobbyInfo", new Session());
				Set(World, "OrderManager", orderManager);

				// One armed ground member type: Mobile passes the eligibility gate,
				// an Armament with a resolvable weapon gives a positive MaxRange.
				// The 'walk' locomotor lives on the world actor, as real rules do;
				// RulesetLoaded resolves both it and the weapon reference.
				var warhead = Info<TargetDamageWarhead>(("Damage", 100));
				var weapon = Info<WeaponInfo>(
					("Range", new WDist(4 * 1024)),
					("Warheads", ImmutableArray.Create<IWarhead>(warhead)));
				var memberInfo = new ActorInfo("member",
					Info<MobileInfo>(("Locomotor", "walk")),
					Info<AttackOmniInfo>(),
					Info<ArmamentInfo>(("Weapon", "mgun")));

				var actors = new Dictionary<string, ActorInfo>
				{
					["member"] = memberInfo,
					["world"] = new ActorInfo("world", Info<LocomotorInfo>(("Name", "walk"))),
				};
				var weapons = new Dictionary<string, WeaponInfo> { ["mgun"] = weapon };
				var rules = new Ruleset(actors, weapons, new Dictionary<string, SoundInfo>(),
					new Dictionary<string, SoundInfo>(), new Dictionary<string, MusicInfo>(),
					new Dictionary<string, PlaylistDefinition>(), null, null);
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

				// The squad manager with just the fields the concave tick reads:
				// the orderability predicate (mirrors the ctor's real one), the
				// shell World/Player and Info (AttackScanRadius default is enough).
				Manager = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
				Set(Manager, "World", World);
				Set(Manager, "Player", Player);
				Set(Manager, "Info", new SquadManagerBotModuleCAInfo());
				Set(Manager, "unitCannotBeOrdered", (Predicate<Actor>)(a =>
					a == null || a.Owner != Player || a.IsDead || !a.IsInWorld || a.CurrentActivity is Enter));
				Set(Manager, "unitRoles", new NoRoles());

				Bot = new FakeBot(Player);
				Squad = (SquadCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadCA));
				Set(Squad, "Units", new List<UnitWposWrapper>());
				Set(Squad, "NewUnits", new HashSet<Actor>());
				Set(Squad, "Type", SquadCAType.Rush);
				Set(Squad, "Bot", Bot);
				Set(Squad, "World", World);
				Set(Squad, "SquadManager", Manager);
				Set(Squad, "Target", Target.FromPos(Anchor));

				var asm = typeof(SquadCA).Assembly;
				StateType = asm.GetType("OpenRA.Mods.CA.Traits.BotModules.Squads.GroundUnitsConcaveStateCA");
				var shapeType = asm.GetType("OpenRA.Mods.CA.Traits.BotModules.Squads.DeployShape");
				State = StateType.GetConstructor(new[] { shapeType })
					.Invoke(new object[] { Enum.Parse(shapeType, "Army") });
				PlacedType = StateType.GetNestedType("Placed", BindingFlags.NonPublic);
				PlacedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(PlacedType));
				Set(State, "placed", PlacedList);

				// Seed as "planned this tick at the current anchor" so the replan
				// gate is closed and the seed list is what the commit checks see.
				Set(State, "lastPlanTick", 0);
				Set(State, "formStartTick", 0);
				Set(State, "planAnchor", Anchor);
				Set(State, "settings", new AssaultFormationSettings(
					0, 25, 0, 0, 0, 0, 0, 0, 0, 0, assemblePercent, int.MaxValue));
			}

			public Actor AddMember(WPos pos)
			{
				var a = NewActor(World, "member", Player);
				SetProp(a, "IsInWorld", true);
				SetProp(a.Trait<Mobile>(), "CenterPosition", pos);
				((List<UnitWposWrapper>)Get(Squad, "Units")).Add(new UnitWposWrapper(a));
				return a;
			}

			public Actor MakeDead()
			{
				var a = NewActor(World, "member", Player);

				// IsDead derives from Disposed; setting it is what makes
				// TraitOrDefault throw 'trait from destroyed object'.
				SetProp(a, "Disposed", true);
				return a;
			}

			public object MakePlaced(Actor actor, WPos slot)
			{
				var p = Activator.CreateInstance(PlacedType, true);
				Set(p, "Actor", actor);
				Set(p, "Range", 512);
				Set(p, "Speed", 10);
				Set(p, "Slot", slot);
				Set(p, "Orders", 0);
				Set(p, "LastHp", 0);
				return p;
			}

			public void SeedPlaced(params object[] entries)
			{
				PlacedList.Clear();
				foreach (var p in entries)
					PlacedList.Add(p);
			}

			public IList Pending => (IList)Get(State, "pending");

			public void Tick()
			{
				StateType.GetMethod("Tick").Invoke(State, new object[] { Squad });
			}
		}

		// The invariant the crash rests on: a trait lookup on a destroyed actor
		// throws before any null-defaulting can run.
		[Test]
		public void TraitLookupOnDestroyedActorThrows()
		{
			var f = new Fixture(50);
			var dead = f.MakeDead();
			Assert.Throws<InvalidOperationException>(() => dead.TraitOrDefault<IHealth>());
		}

		// A destroyed member's stale slot must be pruned before the commit checks:
		// the survivor keeps its slot, no orders go out, nothing touches the dead
		// actor's traits.
		[Test]
		public void DeadPlacementPrunedBeforeCommitChecks()
		{
			var f = new Fixture(101);
			var live = f.AddMember(Anchor);
			var dead = f.MakeDead();
			f.SeedPlaced(f.MakePlaced(dead, Anchor), f.MakePlaced(live, live.CenterPosition));

			Assert.That(() => f.Tick(), Throws.Nothing);
			Assert.That(f.PlacedList, Has.Count.EqualTo(1));
			Assert.That(Get(f.PlacedList[0], "Actor"), Is.SameAs(live));
			Assert.That(f.Bot.Orders, Has.Count.EqualTo(0));
		}

		// Once the plan is committed the members the prune kept still deploy: the
		// unplaced joiner and the far member push at once, the near member waits on
		// its staggered delay, and no order is issued to the dead member.
		[Test]
		public void SurvivorsCommitWithStaggeredDelays()
		{
			var f = new Fixture(50);
			var joiner = f.AddMember(Anchor + new WVec(20000, 0, 0));
			var near = f.AddMember(Anchor + new WVec(256, 0, 0));
			var far = f.AddMember(Anchor + new WVec(30000, 0, 0));
			var dead = f.MakeDead();
			f.SeedPlaced(
				f.MakePlaced(dead, Anchor),
				f.MakePlaced(near, near.CenterPosition),
				f.MakePlaced(far, far.CenterPosition));

			Assert.That(() => f.Tick(), Throws.Nothing);
			Assert.That(f.PlacedList, Has.Count.EqualTo(2));
			Assert.That(f.Bot.Orders, Has.Count.EqualTo(2));
			Assert.That(f.Bot.Orders.All(o => o.OrderString == "AttackMove"), Is.True);
			Assert.That(f.Bot.Orders.Select(o => o.Subject).ToHashSet().SetEquals(new[] { joiner, far }), Is.True);
			Assert.That(f.Pending, Has.Count.EqualTo(1));
			Assert.That(Get(f.Pending[0], "Actor"), Is.SameAs(near));
		}
	}
}
