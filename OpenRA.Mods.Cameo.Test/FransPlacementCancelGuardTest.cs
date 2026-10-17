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

		public sealed class RecordingBot : IBot
		{
			public readonly List<Order> Orders = [];
			public void Activate(Player p) { }
			public void QueueOrder(Order order) => Orders.Add(order);
			public IBotInfo Info => null;
			public Player Player => null;
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

				// The serviced held-cancel tracker logs through FransBotLog.BotDebug ->
				// AIUtils.BotDebug -> Game.Settings.Debug.BotDebug. Provide real default
				// settings (missing file -> built-in sections) so the service path can
				// run inside the bare harness; BotDebug stays false so nothing writes.
				Game.Settings = new Settings(
					System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cameo-guard-test-no-such-settings.yaml"),
					Arguments.Empty);

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
					["other"] = new("other",
						Info<BuildableInfo>(("Queue", new[] { "Building" }.ToFrozenSet())),
						Info<ValuedInfo>(("Cost", 400))),
					["other_item"] = new("other_item",
						Info<BuildableInfo>(("Queue", new[] { "Building" }.ToFrozenSet())),
						Info<ValuedInfo>(("Cost", 400))),
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

		// ----------------------------------------------------------------
		// Retry semantics (review 7CE00E73 finding F1): a held cancel must not
		// consume or latch queue state — the same exact item is re-askable on
		// the next sweep after placement conditions change.
		// ----------------------------------------------------------------

		[Test]
		public void TryIssue_HeldCancelLeavesQueueUntouchedAndReaskable()
		{
			var f = new Fixture();
			var item = f.Enqueue();

			var firstIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, false, out var firstHold);
			Assert.Multiple(() =>
			{
				Assert.That(firstIssued, Is.False);
				Assert.That(firstHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.Protected));
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items.Count, Is.EqualTo(1));
				Assert.That(f.Items[0], Is.SameAs(item));
				Assert.That(FransPlacementCancelGuard.EngineCancelTarget(f.Queue, Item), Is.SameAs(item));
			});

			// Re-ask after "space opened" (a later sweep): identical hold, still
			// nothing queued as an order, item still live — the guard consumed no
			// state the first time, so callers may retry placement freely.
			var secondIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, false, out var secondHold);
			Assert.Multiple(() =>
			{
				Assert.That(secondIssued, Is.False);
				Assert.That(secondHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.Protected));
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items.Count, Is.EqualTo(1));
			});
		}

		[Test]
		public void TryIssue_AmbiguityResolvesWhenTailSameNameLeaves()
		{
			var f = new Fixture();
			var reservedFirst = f.Enqueue();
			var engineLast = f.Enqueue();

			var firstIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, reservedFirst, true, out var firstHold);
			Assert.Multiple(() =>
			{
				Assert.That(firstIssued, Is.False);
				Assert.That(firstHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.AmbiguousTarget));
				Assert.That(f.Bot.Orders, Is.Empty);
			});

			// The tail same-name item leaves by other means (placed / externally
			// cancelled): the reserved FIRST is now the engine target and the
			// re-asked guard issues on the exact intended item.
			f.Items.Remove(engineLast);

			var secondIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, reservedFirst, true, out var secondHold);
			Assert.Multiple(() =>
			{
				Assert.That(secondIssued, Is.True);
				Assert.That(secondHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(1));
				Assert.That(f.Items, Does.Contain(reservedFirst));
			});
		}

		// ----------------------------------------------------------------
		// Caller outcome mapping (review 7CE00E73 finding F2): the Mcv
		// expansion manager maps (issued, hold) onto its tri-state — absent
		// engine target settles as "already absent", a nonzero hold must be
		// propagated as Held (ownership retained), and an issued cancel is the
		// only outcome that releases the ownership record.
		// ----------------------------------------------------------------

		[Test]
		public void CallerOutcome_AbsentSettlesHeldRetainsIssuedSettles()
		{
			var f = new Fixture();

			// Already absent: (false, None) -> caller's Settled/NoOwnership path.
			var absentIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, null, true, out var absentHold);
			Assert.Multiple(() =>
			{
				Assert.That(absentIssued, Is.False);
				Assert.That(absentHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
			});

			// Present but denied: (false, Protected) -> caller's Held path, item
			// still live so ownership teardown must not proceed as if cancelled.
			var item = f.Enqueue();
			var heldIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, false, out var heldHold);
			Assert.Multiple(() =>
			{
				Assert.That(heldIssued, Is.False);
				Assert.That(heldHold, Is.Not.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Items, Does.Contain(item));
				Assert.That(f.Bot.Orders, Is.Empty);
			});

			// Present and permitted: (true, None) -> caller's Settled path.
			var issuedIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, true, out var issuedHold);
			Assert.Multiple(() =>
			{
				Assert.That(issuedIssued, Is.True);
				Assert.That(issuedHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(1));
			});
		}

		// ----------------------------------------------------------------
		// CancelPermitted evidence seam (no-Allow decision per plan 4AD696C8): the seam is
		// no-Allow/fail-closed this iteration — every classification returns
		// false — but the positive-evidence contract holds: resolved cost/ticks
		// come only from engine resolution, unknown/nonpositive evidence is
		// never cheap-fast proof.
		// ----------------------------------------------------------------

		[Test]
		public void CancelPermitted_UnknownItemName_UnresolvedReturnsFalse()
		{
			var f = new Fixture();
			f.Enqueue();

			Assert.That(FransPlacementCancelGuard.CancelPermitted(f.World, f.Queue, "not_a_rule", null, null), Is.False);
		}

		[Test]
		public void CancelPermitted_ProtectedRoleMember_ReturnsFalse()
		{
			var f = new Fixture();
			f.Enqueue();

			Assert.That(FransPlacementCancelGuard.CancelPermitted(f.World, f.Queue, Item,
				new[] { Item }, null), Is.False);
		}

		[Test]
		public void CancelPermitted_CheapFastMember_StillFailClosed()
		{
			// Even with positive resolved evidence (ValuedInfo 800, resolvable
			// build time) and cheap-fast role membership, the no-Allow seam holds.
			var f = new Fixture();
			f.Enqueue();

			Assert.That(FransPlacementCancelGuard.CancelPermitted(f.World, f.Queue, Item,
				null, new[] { Item }), Is.False);
		}

		[Test]
		public void CancelPermitted_NullQueue_ReturnsFalse()
		{
			var f = new Fixture();

			Assert.That(FransPlacementCancelGuard.CancelPermitted(f.World, null, Item, null, null), Is.False);
		}

		// ----------------------------------------------------------------
		// Serviced held-cancel tracker (review F2 corrective successor).
		// Every held caller now transfers the EXACT queue item into a
		// HeldQueueCancelTracker — the explicit fallback owner — and BotTick
		// drives ServiceHeldQueueCancels over it every sweep. These fixtures
		// exercise that real service routine end-to-end: entry retained while
		// held, re-asked on schedule against the real guard, settled honestly
		// when the item leaves the queue by other means (the post-placement
		// outcome), and cleared when its queue actor dies. No imitation of the
		// transition — this is the production code path the callers take.
		// ----------------------------------------------------------------

		static FransPlacementCancelGuard.HeldQueueCancelEntry HeldEntry(Fixture f, ProductionItem item, int nextReaskTick = 0)
		{
			return new FransPlacementCancelGuard.HeldQueueCancelEntry
			{
				Queue = f.Queue,
				ItemName = item.Item,
				Item = item,
				Reason = "TestCallerHandoff",
				HeldSinceTick = f.World.WorldTick,
				NextReaskTick = nextReaskTick
			};
		}

		[Test]
		public void ServiceHeldCancels_ProtectedHold_RetainsEntryReschedulesLeavesQueueUntouched()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			var entry = HeldEntry(f, item);
			tracker.Entries.Add(entry);

			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);

			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1), "held entry must survive the sweep — the live retry owner");
				Assert.That(entry.ReaskCount, Is.EqualTo(1), "a due entry is re-asked against the real guard");
				Assert.That(entry.NextReaskTick, Is.EqualTo(FransPlacementCancelGuard.HeldQueueCancelTracker.DefaultReaskIntervalTicks));
				Assert.That(f.Bot.Orders, Is.Empty, "fail-closed seam: no cancel order may be emitted");
				Assert.That(f.Items, Does.Contain(item), "the exact queue item stays queued");
				Assert.That(FransPlacementCancelGuard.EngineCancelTarget(f.Queue, Item), Is.SameAs(item));
			});
		}

		[Test]
		public void ServiceHeldCancels_NotYetDue_DoesNotReask()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			var entry = HeldEntry(f, item, nextReaskTick: 1000);
			tracker.Entries.Add(entry);

			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);

			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1));
				Assert.That(entry.ReaskCount, Is.EqualTo(0), "re-asks are interval-bounded, not every sweep");
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items, Does.Contain(item));
			});
		}

		[Test]
		public void ServiceHeldCancels_ItemLeftQueueByOtherMeans_SettlesHonestly()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			tracker.Entries.Add(HeldEntry(f, item));

			// The retained item left the queue by other means — a PlaceBuilding
			// order consumed it, or it was cancelled elsewhere. The tracker must
			// settle the entry WITHOUT recording a cancellation.
			f.Items.Remove(item);

			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);

			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Is.Empty, "a departed item is resolved, not re-asked forever");
				Assert.That(f.Bot.Orders, Is.Empty, "settling a departed item must not emit a cancel");
			});
		}

		[Test]
		public void ServiceHeldCancels_QueueActorGone_SettlesEntry()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			tracker.Entries.Add(HeldEntry(f, item));

			// Queue actor (the ProductionQueue's host) is gone — a dead producer
			// must not freeze the entry forever.
			typeof(Actor).GetProperty("IsInWorld",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.SetValue(f.PlayerActor, false);

			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);

			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Is.Empty, "a dead queue resolves the entry honestly");
				Assert.That(f.Bot.Orders, Is.Empty);
			});
		}

		[Test]
		public void ServiceHeldCancels_AmbiguityResolves_StillProtectedUnderFailClosedSeam()
		{
			var f = new Fixture();
			var reservedFirst = f.Enqueue();
			var engineLast = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			var entry = HeldEntry(f, reservedFirst);
			tracker.Entries.Add(entry);

			// Sweep 1: intended FIRST vs engine LAST -> AmbiguousTarget hold.
			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1));
				Assert.That(entry.ReaskCount, Is.EqualTo(1));
				Assert.That(f.Bot.Orders, Is.Empty);
			});

			// The tail same-name item leaves by other means; the intended FIRST
			// is now the unambiguous engine target. The fail-closed seam still
			// denies the cancel — the service must NOT turn an identity-resolved
			// retry into an unauthorized issue.
			f.Items.Remove(engineLast);
			entry.NextReaskTick = 0;

			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1));
				Assert.That(entry.ReaskCount, Is.EqualTo(2));
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items, Does.Contain(reservedFirst));
			});
		}

		[Test]
		public void ServiceHeldCancels_PersistentHold_LogsHardBlockerOnce()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			var entry = HeldEntry(f, item);
			tracker.Entries.Add(entry);

			var limit = FransPlacementCancelGuard.HeldQueueCancelTracker.HardBlockerReaskLimit;
			for (var i = 0; i < limit + 2; i++)
			{
				Set(f.World, "WorldTick", i * FransPlacementCancelGuard.HeldQueueCancelTracker.DefaultReaskIntervalTicks);
				FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			}

			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1), "a persistent hold keeps the entry — it is never dropped");
				Assert.That(entry.ReaskCount, Is.EqualTo(limit + 2));
				Assert.That(entry.HardBlockerLogged, Is.True, "the current hard blocker is recorded once");
				Assert.That(f.Bot.Orders, Is.Empty);
				Assert.That(f.Items, Does.Contain(item));
			});
		}

		[Test]
		public void ServiceHeldCancels_MultipleEntries_ServicedIndependently()
		{
			var f = new Fixture();
			var first = f.Enqueue();
			var second = f.Enqueue("other_item");
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			var entryFirst = HeldEntry(f, first);
			var entrySecond = HeldEntry(f, second);
			tracker.Entries.Add(entryFirst);
			tracker.Entries.Add(entrySecond);

			// Both retained entries are serviced — the tracker is a list, so a
			// later handoff cannot overwrite an earlier retained item.
			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(2));
				Assert.That(entryFirst.ReaskCount, Is.EqualTo(1));
				Assert.That(entrySecond.ReaskCount, Is.EqualTo(1));
			});

			// One leaves the queue; the other keeps retrying on schedule.
			f.Items.Remove(first);
			Set(f.World, "WorldTick", FransPlacementCancelGuard.HeldQueueCancelTracker.DefaultReaskIntervalTicks);
			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1));
				Assert.That(tracker.Entries[0], Is.SameAs(entrySecond));
				Assert.That(entrySecond.ReaskCount, Is.EqualTo(2));
				Assert.That(f.Bot.Orders, Is.Empty);
			});
		}

		[Test]
		public void Track_SameExactItemTwice_SingleEntry()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();

			// Two teardown paths can hand off the same item in overlapping sweeps;
			// the tracker must keep exactly one intent per (queue, item).
			var first = tracker.Track(f.Queue, Item, item, "PathA", f.World.WorldTick);
			var second = tracker.Track(f.Queue, Item, item, "PathB", f.World.WorldTick);
			var differentItem = tracker.Track(f.Queue, Item, f.Enqueue(), "PathC", f.World.WorldTick);

			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(2), "same exact item merges, a different same-name item is its own entry");
				Assert.That(second, Is.SameAs(first));
				Assert.That(differentItem, Is.Not.SameAs(first));
			});
		}

		[Test]
		public void TryIssue_SameTickDedupe_SecondAskEmitsNoSecondOrder()
		{
			var f = new Fixture();
			var item = f.Enqueue();

			// Order.CancelProduction removes the LAST same-name entry at dispatch —
			// a second order for the same queue+name while the first is pending
			// would cancel the wrong item. The guard suppresses the duplicate ask.
			var firstIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, true, out var firstHold);
			var secondIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, true, out var secondHold);
			Assert.Multiple(() =>
			{
				Assert.That(firstIssued, Is.True);
				Assert.That(secondIssued, Is.False, "same-tick re-ask is suppressed");
				Assert.That(secondHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None),
					"suppression reports the honest settle outcome, not a hold");
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(1), "exactly one order reaches the queue");
			});

			// Next world tick: a fresh ask may issue again (the pending order has
			// dispatched by then in the real flow, so this is not a duplicate).
			Set(f.World, "WorldTick", 1);
			var thirdIssued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, true, out var thirdHold);
			Assert.Multiple(() =>
			{
				Assert.That(thirdIssued, Is.True);
				Assert.That(thirdHold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.None));
				Assert.That(f.Bot.Orders.Count, Is.EqualTo(2));
			});
		}

		// ----------------------------------------------------------------
		// Dead-FACT demotion → live placement admission (final review: a
		// retained item must not merely retry-cancel — it must reach a real
		// legal placement). The dead-FACT callers (SecureProducerFactLost,
		// RemoteProducerFactLost, ExpansionPlacementFactUnavailable) transfer
		// the exact item via Track(...).DeadFactDemoted = true; the placement
		// sweep then consults tracker.IsDeadFactDemoted — the same production
		// member — to bypass the expansion-only park gate and to pass
		// deadFactDemoted into ChooseBuildLocation so the no-expansion-FACT
		// case falls through to ordinary base placement. These fixtures drive
		// the real caller sequence end-to-end: TryIssue hold -> caller
		// Track+demote -> ServiceHeldQueueCancels -> admission token
		// lifecycle -> honest settle when placement consumes the queue item.
		// ----------------------------------------------------------------

		[Test]
		public void DeadFactDemotion_CallerHandoff_AdmitsRetainedItemToPlacement()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var unrelated = f.Enqueue("other_item");
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();

			// Real caller sequence: the first ask goes through the actual
			// guard — protected under the fail-closed seam -> held -> the
			// dead-FACT caller hands the exact item to the tracker WITH the
			// demotion flag, verbatim caller code.
			var issued = FransPlacementCancelGuard.TryIssue(f.Bot, f.Queue, Item, item, false, out var hold);
			Assert.That(issued, Is.False);
			Assert.That(hold, Is.EqualTo(FransPlacementCancelGuard.FransCancelHold.Protected));

			tracker.Track(f.Queue, Item, item, "SecureProducerFactLost", f.World.WorldTick).DeadFactDemoted = true;

			Assert.Multiple(() =>
			{
				// The admission token the placement sweep consults: bypasses
				// the expansion-only park gate and feeds ChooseBuildLocation's
				// deadFactDemoted flag — the retained item reaches a real
				// legal placement instead of parking ahead of it.
				Assert.That(tracker.IsDeadFactDemoted(item), Is.True,
					"demoted retained item must be admitted to the ordinary placement path");
				Assert.That(tracker.IsDeadFactDemoted(unrelated), Is.False,
					"demotion never transfers to an unrelated queued item");
			});
		}

		[Test]
		public void DeadFactDemotion_AdmissionPersistsWhileHeld_SettlesOnPlacementDeparture()
		{
			var f = new Fixture();
			var item = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			tracker.Track(f.Queue, Item, item, "RemoteProducerFactLost", f.World.WorldTick).DeadFactDemoted = true;

			// While the cancel intent retries, admission must stay live —
			// the item is in the placement path every sweep, not parked.
			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Has.Count.EqualTo(1));
				Assert.That(tracker.IsDeadFactDemoted(item), Is.True,
					"admission persists across serviced re-asks — placement stays reachable");
				Assert.That(f.Bot.Orders, Is.Empty);
			});

			// A second teardown path re-tracking the same item dedupes; the
			// caller re-asserts the flag — admission is idempotent, never
			// duplicated or lost.
			var reentry = tracker.Track(f.Queue, Item, item, "RemoteProducerFactLost", f.World.WorldTick);
			reentry.DeadFactDemoted = true;
			Assert.That(tracker.Entries, Has.Count.EqualTo(1));
			Assert.That(tracker.IsDeadFactDemoted(item), Is.True);

			// Placement consumed the queue item — the honest settle. The
			// entry leaves and the demotion with it: no stale admission can
			// survive into a later same-name item.
			f.Items.Remove(item);
			Set(f.World, "WorldTick", FransPlacementCancelGuard.HeldQueueCancelTracker.DefaultReaskIntervalTicks);
			FransPlacementCancelGuard.ServiceHeldQueueCancels(f.World, f.Bot, f.Player, tracker, null, null);
			Assert.Multiple(() =>
			{
				Assert.That(tracker.Entries, Is.Empty, "a placed item settles the held entry honestly");
				Assert.That(tracker.IsDeadFactDemoted(item), Is.False,
					"demotion is bounded to the item's queue lifetime");
				Assert.That(f.Bot.Orders, Is.Empty);
			});
		}

		[Test]
		public void DeadFactDemotion_SameNameDifferentItem_IsNotAdmitted()
		{
			var f = new Fixture();
			var demoted = f.Enqueue();
			var sameNameTail = f.Enqueue();
			var tracker = new FransPlacementCancelGuard.HeldQueueCancelTracker();
			tracker.Track(f.Queue, Item, demoted, "ExpansionPlacementFactUnavailable", f.World.WorldTick).DeadFactDemoted = true;

			Assert.Multiple(() =>
			{
				Assert.That(tracker.IsDeadFactDemoted(demoted), Is.True);
				Assert.That(tracker.IsDeadFactDemoted(sameNameTail), Is.False,
					"exact-item retention: a same-name tail is NOT demoted — demotion cannot widen to an unrelated item");
			});
		}

		// ----------------------------------------------------------------
		// Option-B consumption seams (task 01a14676, spec
		// SPEC_2026-10-17_frans_world_harness_devin): the two sites that were
		// tracker-only now resolve through verbatim FransPlacementCancelGuard
		// predicates — the park-gate bypass in the completed-item sweep and
		// the routing decision inside ChooseBuildLocation. These fixtures
		// drive the exact methods the call sites invoke, covering the full
		// decision matrix the inline code encoded.
		// ----------------------------------------------------------------

		[Test]
		public void ExpansionParkGateHolds_DecisionMatrix_OnlyParksNonDemotedWithoutFact()
		{
			Assert.Multiple(() =>
			{
				Assert.That(FransPlacementCancelGuard.ExpansionParkGateHolds(true, false, true), Is.False,
					"demoted + no FACT: admitted past the park gate");
				Assert.That(FransPlacementCancelGuard.ExpansionParkGateHolds(true, false, false), Is.True,
					"non-demoted + no FACT: gate holds — the park outcome");
				Assert.That(FransPlacementCancelGuard.ExpansionParkGateHolds(true, true, true), Is.False,
					"demoted + live FACT: gate irrelevant");
				Assert.That(FransPlacementCancelGuard.ExpansionParkGateHolds(true, true, false), Is.False,
					"non-demoted + live FACT: expansion routing applies, gate never parks");
				Assert.That(FransPlacementCancelGuard.ExpansionParkGateHolds(false, false, true), Is.False,
					"non-expansion item: gate never holds");
				Assert.That(FransPlacementCancelGuard.ExpansionParkGateHolds(false, true, false), Is.False);
			});
		}

		[Test]
		public void ExpansionRouting_DecisionMatrix_DemotedFallsThroughOnlyWithoutFact()
		{
			Assert.Multiple(() =>
			{
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(false, false, false),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Fallthrough),
					"ordinary item always falls through to base placement");
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(false, true, true),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Fallthrough),
					"non-expansion item ignores both FACT presence and demotion");
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(true, true, false),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Expansion),
					"expansion-routed item with a live FACT routes to it");
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(true, true, true),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Expansion),
					"demotion is fallback-only: a live FACT still routes to expansion");
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(true, false, true),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Fallthrough),
					"demoted + no FACT: the legal base-placement fallback");
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(true, false, false),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Hold),
					"non-demoted + no FACT: the parked item stays held");
			});
		}

		[Test]
		public void ExpansionGateAndRouting_DemotedTransition_AgreeOnAdmission()
		{
			// The two consumption predicates consult the same demotion token —
			// an item the park gate admits (holds==false) must never resolve to
			// Hold downstream; a gate-held non-demoted item stays consistently
			// held at routing too. One park decision, two consults, no drift.
			Assert.Multiple(() =>
			{
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(true, false, true),
					Is.Not.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Hold),
					"gate-admitted demoted item cannot resolve to Hold downstream");
				Assert.That(FransPlacementCancelGuard.ExpansionRouting(true, false, false),
					Is.EqualTo(FransPlacementCancelGuard.FransExpansionRouting.Hold),
					"gate-held non-demoted item stays held at routing — consistent park");
			});
		}
	}
}
