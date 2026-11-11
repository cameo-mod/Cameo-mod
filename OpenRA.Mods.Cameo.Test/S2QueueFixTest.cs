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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// S2 successor (RCA S2_DROPPED_TELEMETRY_RCA_01): the three observer-only fixes of the queue-transition
	// evidence ladder. F1 plug install proof for requirement-keyed slots, F2 stale-watch grace and
	// still-queued guard, F3 one observer per queue category. Everything drives world-free seams.
	[TestFixture]
	public class S2QueueFixTest
	{
		// ---- F1: plug install proof truth table ---------------------------------------------------------------

		// slotDefinesType, requirementKeyed, acceptsNow, acceptedAtOrder -> proven
		[TestCase(true, false, false, false, true, TestName = "F1_free_refused_notAcceptedAtOrder")]
		[TestCase(true, false, false, true, true, TestName = "F1_free_refused_acceptedAtOrder")]
		[TestCase(true, false, true, false, false, TestName = "F1_free_stillAccepting_notAcceptedAtOrder")]
		[TestCase(true, false, true, true, false, TestName = "F1_free_stillAccepting_acceptedAtOrder")]
		[TestCase(true, true, false, false, false, TestName = "F1_keyed_refused_notAcceptedAtOrder")]
		[TestCase(true, true, false, true, true, TestName = "F1_keyed_refused_acceptedAtOrder")]
		[TestCase(true, true, true, false, false, TestName = "F1_keyed_stillAccepting_notAcceptedAtOrder")]
		[TestCase(true, true, true, true, false, TestName = "F1_keyed_stillAccepting_acceptedAtOrder")]
		[TestCase(false, false, false, false, false, TestName = "F1_undefined_free_refused_notAccepted")]
		[TestCase(false, false, false, true, false, TestName = "F1_undefined_free_refused_accepted")]
		[TestCase(false, false, true, false, false, TestName = "F1_undefined_free_accepting_notAccepted")]
		[TestCase(false, false, true, true, false, TestName = "F1_undefined_free_accepting_accepted")]
		[TestCase(false, true, false, false, false, TestName = "F1_undefined_keyed_refused_notAccepted")]
		[TestCase(false, true, false, true, false, TestName = "F1_undefined_keyed_refused_accepted")]
		[TestCase(false, true, true, false, false, TestName = "F1_undefined_keyed_accepting_notAccepted")]
		[TestCase(false, true, true, true, false, TestName = "F1_undefined_keyed_accepting_accepted")]
		public void PlugInstallProofTruthTable(bool slotDefinesType, bool requirementKeyed, bool acceptsNow, bool acceptedAtOrder, bool expected)
		{
			Assert.That(BaseBuilderQueueEvalCA.PlugInstallProven(slotDefinesType, requirementKeyed, acceptsNow, acceptedAtOrder),
				Is.EqualTo(expected));
		}

		[TestCase(true, true, true, true)]
		[TestCase(false, true, true, false)]
		[TestCase(true, false, true, false)]
		[TestCase(true, true, false, false)]
		[TestCase(false, false, false, false)]
		public void PlugHostEligibleNeedsPresentLiveOwnedHost(bool found, bool live, bool owned, bool expected)
		{
			Assert.That(BaseBuilderQueueEvalCA.PlugHostEligible(found, live, owned), Is.EqualTo(expected));
		}

		static BotQueuePendingTerminal PlugPending(bool acceptedAtOrder, uint host = 4) => new()
		{
			Item = new object(), ItemName = "td_gdi_ioncannonuplink", Kind = BotQueueTransitionKind.Placed,
			IssuedTick = 13507, Site = new CPos(5, 6), PlugType = "td_gdi_advancedcommunicationscenter",
			PlugHostId = host, PlugAcceptedAtOrder = acceptedAtOrder
		};

		// ---- F1: pending seam ---------------------------------------------------------------------------------

		[Test]
		public void KeyedHostAcceptedAtOrderThenRefusedResolvesPlaced()
		{
			var pending = PlugPending(acceptedAtOrder: true);
			var proven = pending.PlugProven(hostEligible: true, slotDefinesType: true, requirementKeyed: true, acceptsNow: false);
			Assert.That(proven, Is.True);

			// The item left the queue and the proof is already readable: Placed at the removal tick.
			Assert.That(pending.ResolveOnRemoval(13517, queueSeen: true, placementProven: proven),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested));

			// Or the proof lands while the request is armed.
			pending.AwaitingProof = true;
			Assert.That(pending.ResolveWhileAwaiting(13520, placementProven: proven, playerEliminated: false),
				Is.EqualTo(BotQueuePendingResolution.EmitRequested));
		}

		[Test]
		public void KeyedHostNotAcceptedAtOrderStillResolvesRemoved()
		{
			var pending = PlugPending(acceptedAtOrder: false);
			var proven = pending.PlugProven(true, true, true, acceptsNow: false);
			Assert.That(proven, Is.False);

			// Armed for the bounded window, then Removed + intent reason (UNKNOWN), exactly as before S2.
			Assert.That(pending.ResolveOnRemoval(13517, queueSeen: true, placementProven: proven),
				Is.EqualTo(BotQueuePendingResolution.KeepAwaitingProof));
			pending.AwaitingProof = true;
			pending.IssuedTick = 13517;
			Assert.That(pending.ResolveWhileAwaiting(13517 + BotQueuePendingTerminal.MaxInFlightTicks + 1, proven, false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved));
			Assert.That(pending.ResolveOnRemoval(13517, queueSeen: false, placementProven: proven),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved));
		}

		[TestCase(false, true, true)]
		[TestCase(true, false, true)]
		[TestCase(true, true, false)]
		public void DeadDisownedOrMissingHostNeverProvesAndResolvesRemoved(bool found, bool live, bool owned)
		{
			// The host was accepted at order time and now "refuses" — but it is dead, disposed, missing
			// or owned by someone else, so its slot state says nothing about an install: stays UNKNOWN.
			var pending = PlugPending(acceptedAtOrder: true);
			var eligible = BaseBuilderQueueEvalCA.PlugHostEligible(found, live, owned);
			var proven = pending.PlugProven(eligible, true, true, acceptsNow: false);
			Assert.That(proven, Is.False);
			Assert.That(pending.ResolveOnRemoval(13517, queueSeen: false, placementProven: proven),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved));
			pending.AwaitingProof = true;
			Assert.That(pending.ResolveWhileAwaiting(13517 + BotQueuePendingTerminal.MaxInFlightTicks + 1, proven, false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved));
		}

		[Test]
		public void RequirementFreeSlotIsUnchanged()
		{
			// Requirement-free: the refusal alone is the install, independent of acceptedAtOrder.
			Assert.That(PlugPending(acceptedAtOrder: false).PlugProven(true, true, requirementKeyed: false, acceptsNow: false), Is.True);
			Assert.That(PlugPending(acceptedAtOrder: true).PlugProven(true, true, requirementKeyed: false, acceptsNow: false), Is.True);
			Assert.That(PlugPending(acceptedAtOrder: true).PlugProven(true, true, requirementKeyed: false, acceptsNow: true), Is.False);
		}

		[Test]
		public void NonPlugRequestIsNeverPlugProven()
		{
			var pending = new BotQueuePendingTerminal { Item = new object(), ItemName = "proc", Kind = BotQueueTransitionKind.Placed };
			Assert.That(pending.PlugProven(true, true, true, false), Is.False);
		}

		// ---- F2: stale-watch grace and the still-queued guard ----------------------------------------------

		sealed class StaleModel
		{
			// Mirrors the manager's stale loop: sweep, resolve through ResolveRemovedItem (class from the same ladder).
			public readonly BotQueueWatch Watch = new();
			public readonly HashSet<object> Queued = [];
			public bool Live = true;
			public bool Owned = true;
			public bool Eliminated;
			public int Now; // lets a Bind closure read the sweep tick
			public Func<object, bool> Bind; // S2-RECEIPT-01: models the manager's first-gone receipt bind (null = none)

			public object AddItem()
			{
				var item = new object();
				Queued.Add(item);
				Watch.Items.Add(item);
				return item;
			}

			public List<(object Item, BotQueueCancellationClass Class)> Sweep(int now)
			{
				var result = new List<(object, BotQueueCancellationClass)>();
				var scratch = Live && Owned && !Eliminated ? Queued : new HashSet<object>();
				var resolve = Watch.StaleSweep(o => scratch.Contains(o), Eliminated, Live, Owned, now, Bind);
				if (resolve != null)
					foreach (var item in resolve)
					{
						var evicted = Watch.EvictedAtFirstGone.Remove(item); // as ResolveRemovedItem reads it
						result.Add((item, BaseBuilderQueueEvalCA.RemovalCauseClass(Eliminated, Live, Owned,
							queueSeen: false, itemBuildable: false, unbuildableEvicted: evicted)));
						Watch.Items.Remove(item);
						Watch.GoneSince.Remove(item);
					}

				return result;
			}
		}

		[Test]
		public void StaleWatchWithLiveProducerEmitsNothingInsideTheWindow()
		{
			var m = new StaleModel();
			var item = m.AddItem();
			m.Queued.Remove(item); // the queue was cleared (sell or transform) while the actor reads live

			for (var tick = 100; tick < 100 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks; tick += 7)
				Assert.That(m.Sweep(tick), Is.Empty, $"tick {tick} is inside the grace window");

			Assert.That(m.Watch.GoneSince[item], Is.EqualTo(100), "the window starts at the first stale sweep that saw it gone");
			Assert.That(m.Watch.Items, Does.Contain(item));
		}

		[Test]
		public void DisposedInsideTheWindowResolvesAsDestruction()
		{
			var m = new StaleModel();
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(100), Is.Empty);
			Assert.That(m.Sweep(130), Is.Empty);

			m.Live = false; // the sold or transformed actor is disposed when its animation ends
			var resolved = m.Sweep(160);
			Assert.That(resolved, Has.Count.EqualTo(1));
			Assert.That(resolved[0].Item, Is.SameAs(item));
			Assert.That(resolved[0].Class, Is.EqualTo(BotQueueCancellationClass.Destruction));
			Assert.That(m.Watch.Items, Is.Empty);
		}

		[Test]
		public void LiveAtExpiryResolvesAsHonestUnknown()
		{
			var m = new StaleModel();
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(100), Is.Empty);
			Assert.That(m.Sweep(100 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks - 1), Is.Empty);

			var resolved = m.Sweep(100 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks);
			Assert.That(resolved, Has.Count.EqualTo(1));
			Assert.That(resolved[0].Class, Is.EqualTo(BotQueueCancellationClass.None),
				"a live producer at expiry is as unproven as today: class None = UNKNOWN");
		}

		// ---- S2-RECEIPT-01: the eviction receipt (64 ticks) is bound at first-gone, not at the 600-tick resolve ----

		// Engine eviction receipt lifetime (ProductionQueue.UnbuildableEvictionReceiptLifetime).
		const int ReceiptLifetime = 64;

		[Test]
		public void ConyardLostQueuedBuildingEvictedGraceResolvesAsEvictedUnbuildable()
		{
			// The stage S S2 / VERIFY G3 sequence: the conyard dies, the queue is disabled (left FindQueues), the engine
			// evicts the queued building and writes a receipt; the bot sweeps every 25 ticks, holds the item for the
			// grace window and resolves it 600 ticks later with a producer (the Player actor) that still reads live.
			const int evictedAt = 22115;
			var receiptLive = true;
			var bindCalls = 0;
			var m = new StaleModel();
			var item = m.AddItem();
			m.Bind = o =>
			{
				bindCalls++;
				var valid = receiptLive && m.Now - evictedAt < ReceiptLifetime;
				receiptLive = false; // bind-and-consume: a receipt answers exactly once
				return valid;
			};

			m.Queued.Remove(item); // evicted at 22115
			List<(object Item, BotQueueCancellationClass Class)> resolved = null;
			for (var tick = 22125; resolved == null || resolved.Count == 0; tick += 25)
			{
				m.Now = tick;
				resolved = m.Sweep(tick);
				Assert.That(tick, Is.LessThan(22125 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks + 50), "never resolves");
			}

			Assert.That(bindCalls, Is.EqualTo(1), "bound once, at the first sweep that saw the item gone");
			Assert.That(resolved, Has.Count.EqualTo(1));
			Assert.That(resolved[0].Item, Is.SameAs(item));
			Assert.That(resolved[0].Class, Is.EqualTo(BotQueueCancellationClass.EvictedUnbuildable),
				"the cause was bound while the receipt was alive, so the 600-tick resolve is receipt-backed, not None");
			Assert.That(m.Watch.EvictedAtFirstGone, Is.Empty);
		}

		[Test]
		public void FirstGoneWithoutAReceiptStaysAnHonestUnknown()
		{
			// Sell or transform clear the queue with no engine eviction: no receipt, so nothing is invented.
			var m = new StaleModel { Bind = o => false };
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(100), Is.Empty);
			Assert.That(m.Watch.EvictedAtFirstGone, Is.Empty);

			var resolved = m.Sweep(100 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks);
			Assert.That(resolved, Has.Count.EqualTo(1));
			Assert.That(resolved[0].Class, Is.EqualTo(BotQueueCancellationClass.None));
		}

		[Test]
		public void DisposedProducerStillOutranksAStoredReceipt()
		{
			// The ladder order is unchanged: a producer that is disposed inside the window is a proven Destruction
			// even when a receipt was bound at first-gone.
			var m = new StaleModel { Bind = o => true };
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(100), Is.Empty);
			Assert.That(m.Watch.EvictedAtFirstGone, Does.Contain(item));

			m.Live = false;
			var resolved = m.Sweep(160);
			Assert.That(resolved, Has.Count.EqualTo(1));
			Assert.That(resolved[0].Class, Is.EqualTo(BotQueueCancellationClass.Destruction));
		}

		[Test]
		public void ReceiptIsBoundOncePerGoneEpisodeAndNeverWhileStillQueued()
		{
			var binds = 0;
			var m = new StaleModel { Bind = o => { binds++; return true; } };
			var item = m.AddItem();
			for (var tick = 100; tick < 400; tick += 25)
				Assert.That(m.Sweep(tick), Is.Empty);

			Assert.That(binds, Is.EqualTo(0), "an intact queue is unobserved: nothing is bound");

			m.Queued.Remove(item);
			for (var tick = 400; tick < 500; tick += 25)
				Assert.That(m.Sweep(tick), Is.Empty);

			Assert.That(binds, Is.EqualTo(1), "one bind for the whole gone episode");
			Assert.That(m.Watch.EvictedAtFirstGone, Does.Contain(item));

			m.Queued.Add(item); // the queue is observed intact again: the episode is over
			Assert.That(m.Sweep(500), Is.Empty);
			Assert.That(m.Watch.EvictedAtFirstGone, Is.Empty);
			Assert.That(m.Watch.GoneSince, Is.Empty);
		}

		[Test]
		public void ItemStillQueuedIsNeverRemoved()
		{
			var m = new StaleModel();
			var item = m.AddItem(); // the queue is disabled but its contents are intact
			for (var tick = 100; tick < 5000; tick += 11)
				Assert.That(m.Sweep(tick), Is.Empty, $"tick {tick}: an intact queue is unobserved, not emptied");

			Assert.That(m.Watch.Items, Does.Contain(item), "the watch entry is kept");
			Assert.That(m.Watch.GoneSince, Is.Empty);
		}

		[Test]
		public void ItemThatLeavesLaterStartsItsOwnWindow()
		{
			var m = new StaleModel();
			var item = m.AddItem();
			Assert.That(m.Sweep(100), Is.Empty);
			Assert.That(m.Sweep(400), Is.Empty);
			m.Queued.Remove(item); // cleared at tick 401
			Assert.That(m.Sweep(410), Is.Empty);
			Assert.That(m.Watch.GoneSince[item], Is.EqualTo(410));
			Assert.That(m.Sweep(410 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks - 1), Is.Empty);
			Assert.That(m.Sweep(410 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks), Has.Count.EqualTo(1));
		}

		[Test]
		public void KillPathIsUnchangedAndResolvesAtOnce()
		{
			// A dead or disposed producer resolves on the first stale sweep exactly as before S2, and so
			// does an item that is still listed in a dead producer's queue (the watch must not leak).
			foreach (var stillListed in new[] { false, true })
			{
				var m = new StaleModel { Live = false };
				var item = m.AddItem();
				if (!stillListed)
					m.Queued.Remove(item);

				var resolved = m.Sweep(100);
				Assert.That(resolved, Has.Count.EqualTo(1));
				Assert.That(resolved[0].Class, Is.EqualTo(BotQueueCancellationClass.Destruction));
				Assert.That(m.Watch.Items, Is.Empty);
				Assert.That(m.Watch.GoneSince, Is.Empty);
			}
		}

		[Test]
		public void CapturedAndEliminatedResolveAtOnce()
		{
			var captured = new StaleModel { Owned = false };
			captured.Queued.Remove(captured.AddItem());
			Assert.That(captured.Sweep(100).Single().Class, Is.EqualTo(BotQueueCancellationClass.Captured));

			var eliminated = new StaleModel { Eliminated = true };
			eliminated.AddItem();
			Assert.That(eliminated.Sweep(100).Single().Class, Is.EqualTo(BotQueueCancellationClass.Elimination));
		}

		static BotQueueTransition RemovedTransition(int tick, BotQueueCancellationClass cls)
			=> new(tick, "td_gdi_tiberiumrefinery", "Building", BuildingType.Refinery, 3, 37,
				BotQueueTransitionKind.Removed, BotQueueTransitionReason.None, true, true, cls);

		[Test]
		public void ItemInGraceWhenTheSeatEndsResolvesAsProvenEliminationNotUnknown()
		{
			// N3 end-of-game path: a seat whose queue was cleared by a sell/transform (item gone, producer
			// still live) is held inside the grace window; when the game ends for that seat (WinState set,
			// ModularBot keeps ticking the manager) the very next sweep resolves the held item at once as
			// Elimination, which the evidence seam admits as Observation: no Unknown, no incomplete census.
			var m = new StaleModel();
			m.Queued.Remove(m.AddItem());
			Assert.That(m.Sweep(1000), Is.Empty, "first stale tick: held, nothing emitted");
			Assert.That(m.Sweep(1000 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks / 2), Is.Empty, "still inside the window");

			m.Eliminated = true;
			var resolved = m.Sweep(1000 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks / 2 + 1).Single();
			Assert.That(resolved.Class, Is.EqualTo(BotQueueCancellationClass.Elimination));
			Assert.That(AiEconomyQueueEvidence.Classify(RemovedTransition(2000, resolved.Class)),
				Is.EqualTo(EconomyQueueEvidenceKind.Observation));
			Assert.That(m.Watch.Items, Is.Empty);
			Assert.That(m.Watch.GoneSince, Is.Empty);
		}

		[Test]
		public void ItemStillInGraceWhenTheGameStopsEmitsNothingAndLatchesNothing()
		{
			// The game (or the batch cap) stops while the item is held and the seat has no win state: no sweep
			// runs again, so no Removed exists at all. Nothing can reach the census as an unresolved removal
			// (the same as an item still queued at the end of S1), and the held entry stays bounded.
			var m = new StaleModel();
			m.Queued.Remove(m.AddItem());
			var emitted = new List<(object Item, BotQueueCancellationClass Class)>();
			for (var tick = 5000; tick < 5000 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks; tick += 25)
				emitted.AddRange(m.Sweep(tick));

			Assert.That(emitted, Is.Empty);
			Assert.That(m.Watch.Items.Count, Is.EqualTo(1));
			Assert.That(m.Watch.GoneSince.Count, Is.EqualTo(1));
		}

		[Test]
		public void QueueReturningRestartsTheWindowAndContinuesTheItems()
		{
			var m = new StaleModel();
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(100), Is.Empty);
			Assert.That(m.Watch.GoneSince, Has.Count.EqualTo(1));

			// The queue is back in FindQueues: the probe loop clears the stale bookkeeping
			// (BaseBuilderQueueManagerCA: watch.GoneSince.Clear() right after SeenTick) and diffs normally.
			m.Watch.GoneSince.Clear();

			// A later stale episode begins a fresh window instead of inheriting the old one.
			Assert.That(m.Sweep(600), Is.Empty);
			Assert.That(m.Watch.GoneSince[item], Is.EqualTo(600));
		}

		[Test]
		public void StaleBookkeepingStaysBoundedByTheItems()
		{
			var m = new StaleModel();
			var items = Enumerable.Range(0, 5).Select(_ => m.AddItem()).ToList();
			foreach (var item in items.Take(3))
				m.Queued.Remove(item);

			for (var tick = 100; tick < 2000; tick += 3)
			{
				m.Sweep(tick);
				Assert.That(m.Watch.GoneSince.Count, Is.LessThanOrEqualTo(m.Watch.Items.Count));
			}

			Assert.That(m.Watch.Items.Count, Is.EqualTo(2), "only the still-queued items remain");
		}

		[Test]
		public void StaleDecisionTableAndGraceConstant()
		{
			Assert.That(BaseBuilderQueueEvalCA.StaleResolveGraceTicks, Is.EqualTo(600));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(true, false, true, true, 0, 10_000), Is.EqualTo(BotStaleItemDecision.Keep));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(false, false, true, true, 0, 599), Is.EqualTo(BotStaleItemDecision.Hold));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(false, false, true, true, 0, 600), Is.EqualTo(BotStaleItemDecision.Resolve));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(false, false, false, true, 0, 0), Is.EqualTo(BotStaleItemDecision.Resolve));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(true, false, false, true, 0, 0), Is.EqualTo(BotStaleItemDecision.Resolve));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(false, false, true, false, 0, 0), Is.EqualTo(BotStaleItemDecision.Resolve));
			Assert.That(BaseBuilderQueueEvalCA.StaleItemDecision(true, true, true, true, 0, 0), Is.EqualTo(BotStaleItemDecision.Resolve));
		}

		// ---- F3: one observer per queue category -------------------------------------------------------------

		[Test]
		public void ObservationOwnerSlotPointsDuplicatesAtTheFirstManager()
		{
			var created = new List<string>();
			var owners = new List<int>();
			foreach (var category in new[] { "Building", "RABuilding", "BuildingAddons", "Defence", "RADefence", "BuildingAddons" })
			{
				owners.Add(BaseBuilderQueueEvalCA.ObservationOwnerSlot(created, category));
				created.Add(category);
			}

			Assert.That(owners, Is.EqualTo(new[] { -1, -1, -1, -1, -1, 2 }));
			Assert.That(BaseBuilderQueueEvalCA.ObservationOwnerSlot(new[] { "A", "A" }, "A"), Is.EqualTo(0), "the owner stays the first manager");
		}

		[Test]
		public void TheShippedAiYamlListsBuildingAddonsInBothQueueSets()
		{
			// The root cause of the doubled events: one category in both sets means two managers over one queue.
			var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n");
			var building = yaml.Split('\n').Single(l => l.Trim().StartsWith("BuildingQueues: Building, RABuilding", StringComparison.Ordinal))
				.Split(':')[1].Split(',').Select(s => s.Trim()).ToList();
			var defense = yaml.Split('\n').Single(l => l.Trim().StartsWith("DefenseQueues: Defence, RADefence", StringComparison.Ordinal))
				.Split(':')[1].Split(',').Select(s => s.Trim()).ToList();
			Assert.That(building, Does.Contain("BuildingAddons"));
			Assert.That(defense, Does.Contain("BuildingAddons"));

			var created = new List<string>();
			var duplicates = 0;
			foreach (var category in building.Concat(defense))
			{
				if (BaseBuilderQueueEvalCA.ObservationOwnerSlot(created, category) >= 0)
					duplicates++;
				created.Add(category);
			}

			Assert.That(duplicates, Is.EqualTo(1), "exactly the BuildingAddons defence slot borrows the building slot's observation");
		}

		[Test]
		public void SharedObservationStateEmitsEachItemOnce()
		{
			// Model of the shared state: two managers over one queue, one episode tracker and one watch.
			// Whichever manager ticks, a Queued is emitted once and a pending-backed Removed resolves once.
			var tracker = new BotQueueEpisodeTracker();
			var watch = new BotQueueWatch();
			var item = new object();
			var queuedEmits = 0;

			void ProbeBy(IEnumerable<object> now)
			{
				foreach (var it in now.Where(it => !watch.Items.Contains(it)))
				{
					tracker.BeginEpisode(it);
					queuedEmits++;
				}

				watch.Items.Clear();
				foreach (var it in now)
					watch.Items.Add(it);
			}

			ProbeBy([item]); // manager A (the owner)
			ProbeBy([item]); // manager B (the defence slot, same queue, shared watch)
			ProbeBy([item]);
			Assert.That(queuedEmits, Is.EqualTo(1));
			Assert.That(tracker.EpisodeOf(item), Is.EqualTo(1u));

			// A placement registered by either manager lands in the one shared pending list.
			watch.Pending.Add(PlugPending(acceptedAtOrder: true));
			Assert.That(watch.Pending, Has.Count.EqualTo(1));

			// Identical consecutive emits for one producer dedupe on the shared tracker (a doubled CancelOrdered).
			Assert.That(tracker.EmitGate(4, item, "x", BotQueueTransitionKind.CancelOrdered, BotQueueTransitionReason.DemandCancel), Is.True);
			Assert.That(tracker.EmitGate(4, item, "x", BotQueueTransitionKind.CancelOrdered, BotQueueTransitionReason.DemandCancel), Is.False);
		}

		// ---- Decision-path invariance pins (observer-only change) -------------------------------------------

		static string ManagerSource() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods", "CA", "Traits",
			"BotModules", "BotModuleLogic", "BaseBuilderQueueManagerCA.cs")).Replace("\r\n", "\n");

		static int Count(string text, string token)
		{
			var count = 0;
			for (var i = text.IndexOf(token, StringComparison.Ordinal); i >= 0; i = text.IndexOf(token, i + token.Length, StringComparison.Ordinal))
				count++;

			return count;
		}

		[Test]
		public void DecisionPathTokensAreUnchangedFromTheBase()
		{
			// Counts measured on base d4ac5f6a: the S2 change adds capture and observation code only. Any new
			// order, RNG draw or production call in this file changes one of these numbers.
			var src = ManagerSource();
			Assert.That(Count(src, "bot.QueueOrder("), Is.EqualTo(6));
			Assert.That(Count(src, "Order.StartProduction("), Is.EqualTo(1));
			Assert.That(Count(src, "Order.CancelProduction("), Is.EqualTo(4));
			Assert.That(Count(src, "new Order("), Is.EqualTo(1));
			Assert.That(Count(src, "BotRng.For("), Is.EqualTo(14));
			Assert.That(Count(src, ".Next("), Is.EqualTo(7));
			Assert.That(Count(src, "PlaceBuilding"), Is.EqualTo(17));
		}

		[Test]
		public void PlugHostChoiceAndItsSingleRngDrawAreUntouched()
		{
			var src = ManagerSource();
			Assert.That(Count(src, "var plugCandidates = world.ActorsWithTrait<Pluggable>()\n"
				+ "\t\t\t\t\t\t.Where(a => a.Actor.Owner == player && a.Trait.AcceptsPlug(plugInfo.Type))\n"
				+ "\t\t\t\t\t\t.OrderBy(a => a.Actor.ActorID)\n"
				+ "\t\t\t\t\t\t.ToArray();"), Is.EqualTo(1));
			Assert.That(Count(src, "var possibleBuilding = plugCandidates[BotRng.For(player).Next(plugCandidates.Length)];"), Is.EqualTo(1));
			Assert.That(Count(src, "orderString = \"PlacePlug\";\n\t\t\t\t\t\tlocation = possibleBuilding.Actor.Location + possibleBuilding.Trait.Info.Offset;"), Is.EqualTo(1));

			// The capture reads the choice AFTER the draw and only stores it.
			Assert.That(src.IndexOf("plugHostId = possibleBuilding.Actor.ActorID;", StringComparison.Ordinal),
				Is.GreaterThan(src.IndexOf("BotRng.For(player).Next(plugCandidates.Length)", StringComparison.Ordinal)));
		}

		[Test]
		public void ManagersStillTickAndDecideIndependently()
		{
			// F3 borrows observation state only. The module still creates one manager per listed category and
			// ticks builders[currentBuilderIndex]; the per-slot constructor path stays one `new` per slot.
			var module = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods", "CA", "Traits", "BotModules",
				"BaseBuilderBotModuleCA.cs")).Replace("\r\n", "\n");
			Assert.That(Count(module, "new BaseBuilderQueueManagerCA("), Is.EqualTo(1), "one creation site, in CreateManager");
			Assert.That(Count(module, "builders[i++] = CreateManager("), Is.EqualTo(2), "one per BuildingQueues and one per DefenseQueues entry");
			Assert.That(Count(module, "builders[currentBuilderIndex].Tick(bot);"), Is.EqualTo(1));

			var src = ManagerSource();
			Assert.That(src, Does.Contain("watch.SeenTick = tick;\n\t\t\t\twatch.GoneSince.Clear();"), "a re-observed queue clears the stale bookkeeping");
			Assert.That(src, Does.Contain("BaseBuilderQueueManagerCA observationOwner = null"));
		}

		// ---- S2 raw-log replay fixture ---------------------------------------------------------------------------

		// Real cameo-ai-economy-raw.jsonl rows of the S2 game (uid 166ca84d), verbatim except for the elided game_uid.
		const string BotBRaw =
			"{\"seq\":2294,\"tick\":11869,\"kind\":\"queue-observation\",\"player\":\"BotB\",\"evidence\":{\"item\":\"td_gdi_ioncannonuplink\",\"queue\":\"Addon\",\"producer\":4,\"episode\":1,\"kind\":\"Queued\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"None\",\"category\":\"Building\"}}\n"
			+ "{\"seq\":2775,\"tick\":13499,\"kind\":\"queue-observation\",\"player\":\"BotB\",\"evidence\":{\"item\":\"td_gdi_ioncannonuplink\",\"queue\":\"Addon\",\"producer\":4,\"episode\":1,\"kind\":\"Ready\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"None\",\"category\":\"Building\"}}\n"
			+ "{\"seq\":2778,\"tick\":13507,\"kind\":\"queue-observation\",\"player\":\"BotB\",\"evidence\":{\"item\":\"td_gdi_ioncannonuplink\",\"queue\":\"Addon\",\"producer\":4,\"episode\":1,\"kind\":\"PlacementOrdered\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"None\",\"category\":\"Building\"}}\n"
			+ "{\"seq\":2779,\"tick\":13517,\"kind\":\"queue-observation\",\"player\":\"BotB\",\"evidence\":{\"item\":\"td_gdi_ioncannonuplink\",\"queue\":\"Addon\",\"producer\":4,\"episode\":1,\"kind\":\"Removed\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"Unbuildable\",\"category\":\"Building\"}}\n"
			+ "{\"seq\":2783,\"tick\":13543,\"kind\":\"queue-observation\",\"player\":\"BotB\",\"evidence\":{\"item\":\"td_gdi_ioncannonuplink\",\"queue\":\"Addon\",\"producer\":4,\"episode\":1,\"kind\":\"Removed\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"Unbuildable\",\"category\":\"Building\"}}";

		const string BotARaw =
			"{\"seq\":4482,\"tick\":21311,\"kind\":\"queue-observation\",\"player\":\"BotA\",\"evidence\":{\"item\":\"td_gdi_tiberiumrefinery\",\"queue\":\"Building\",\"producer\":3,\"episode\":37,\"kind\":\"Queued\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"None\",\"category\":\"Refinery\"}}\n"
			+ "{\"seq\":4572,\"tick\":22156,\"kind\":\"queue-observation\",\"player\":\"BotA\",\"evidence\":{\"item\":\"td_gdi_tiberiumrefinery\",\"queue\":\"Building\",\"producer\":3,\"episode\":37,\"kind\":\"Removed\",\"reason\":\"None\",\"player_active\":true,\"producer_live\":true,\"cancellation_class\":\"None\",\"category\":\"Refinery\"}}";

		static List<BotQueueTransition> ParseRaw(string jsonl)
		{
			var list = new List<BotQueueTransition>();
			foreach (var line in jsonl.Split('\n'))
			{
				using var doc = JsonDocument.Parse(line);
				var root = doc.RootElement;
				var e = root.GetProperty("evidence");
				list.Add(new BotQueueTransition(root.GetProperty("tick").GetInt32(), e.GetProperty("item").GetString(),
					e.GetProperty("queue").GetString(), Enum.Parse<BuildingType>(e.GetProperty("category").GetString()),
					e.GetProperty("producer").GetUInt32(), e.GetProperty("episode").GetUInt32(),
					Enum.Parse<BotQueueTransitionKind>(e.GetProperty("kind").GetString()),
					Enum.Parse<BotQueueTransitionReason>(e.GetProperty("reason").GetString()),
					e.GetProperty("player_active").GetBoolean(), e.GetProperty("producer_live").GetBoolean(),
					Enum.Parse<BotQueueCancellationClass>(e.GetProperty("cancellation_class").GetString())));
			}

			return list;
		}

		sealed class Census
		{
			public bool Incomplete;
			public int FirstUnknownTick = -1;
			public readonly List<EconomyQueueEvidenceKind> Evidence = [];
		}

		// The same bookkeeping as AiEconomyHealthRecorder.OnQueueTransition (queue-observation path):
		// Classify with the episode-resolved flag, Unknown -> MarkIncomplete, and the resolved-episode set.
		static Census Replay(IEnumerable<BotQueueTransition> events)
		{
			var census = new Census();
			var resolved = new HashSet<uint>();
			foreach (var original in events)
			{
				var t = original;
				var evidence = AiEconomyQueueEvidence.Classify(in t, resolved.Contains(t.EpisodeId));
				census.Evidence.Add(evidence);
				if (evidence == EconomyQueueEvidenceKind.Unknown && !census.Incomplete)
				{
					census.Incomplete = true;
					census.FirstUnknownTick = t.Tick;
				}

				if (t.EpisodeId != 0)
				{
					if (t.Kind == BotQueueTransitionKind.Removed)
						resolved.Remove(t.EpisodeId);
					else if (t.Kind is BotQueueTransitionKind.CancelOrdered or BotQueueTransitionKind.Cancelled or BotQueueTransitionKind.Placed)
						resolved.Add(t.EpisodeId);
				}
			}

			return census;
		}

		// The pre-S2 predicate (pin d4ac5f6a): requirement-keyed refusals were never proof.
		static bool LegacyPlugInstallProven(bool slotDefinesType, bool requirementKeyed, bool acceptsNow)
			=> slotDefinesType && !requirementKeyed && !acceptsNow;

		static BotQueueTransition Emit(int tick, BotQueueTransitionKind kind, BotQueueCancellationClass cls,
			uint producer, uint episode, string item, string queue, BuildingType category, bool producerLive = true)
			=> new(tick, item, queue, category, producer, episode, kind, BotQueueTransitionReason.None, true, producerLive, cls);

		[Test]
		public void S2BotBIonCannonUplinkReplayBeforeIsUnknownAfterIsPlaced()
		{
			// BEFORE, the raw log as recorded: the second observer's pending-less Removed (13517) and the
			// pending's expiry Removed (13543) are both unproven Unbuildable -> Unknown -> MarkIncomplete.
			var raw = ParseRaw(BotBRaw);
			Assert.That(raw.Select(t => t.Tick), Is.EqualTo(new[] { 11869, 13499, 13507, 13517, 13543 }));
			var before = Replay(raw);
			Assert.That(before.Incomplete, Is.True);
			Assert.That(before.FirstUnknownTick, Is.EqualTo(13517));
			Assert.That(before.Evidence.Count(e => e == EconomyQueueEvidenceKind.Unknown), Is.EqualTo(2));

			// BEFORE through the old logic seam: the legacy predicate on the requirement-keyed slot (!build-incomplete
			// && !ionc, the plug just granted ionc) never proves, so the armed request expires Removed + Unbuildable.
			const string item = "td_gdi_ioncannonuplink";
			var legacy = PlugPending(acceptedAtOrder: true);
			var legacyProof = LegacyPlugInstallProven(slotDefinesType: true, requirementKeyed: true, acceptsNow: false);
			Assert.That(legacy.ResolveOnRemoval(13517, queueSeen: true, placementProven: legacyProof),
				Is.EqualTo(BotQueuePendingResolution.KeepAwaitingProof));
			legacy.AwaitingProof = true;
			legacy.IssuedTick = 13517;
			Assert.That(legacy.ResolveWhileAwaiting(13517 + BotQueuePendingTerminal.MaxInFlightTicks + 1, legacyProof, false),
				Is.EqualTo(BotQueuePendingResolution.EmitRemoved));
			var legacyEvents = new List<BotQueueTransition>(raw.Take(3))
			{
				Emit(13543, BotQueueTransitionKind.Removed, BotQueueCancellationClass.Unbuildable, 4, 1, item, "Addon", BuildingType.Building)
			};
			Assert.That(Replay(legacyEvents).Incomplete, Is.True);

			// AFTER: one observer (F3), the pending captured the host and acceptedAtOrder (F1), and at the
			// removal tick the host is live and owned and now refuses (the plug granted ionc).
			var pending = PlugPending(acceptedAtOrder: true);
			var proof = pending.PlugProven(hostEligible: true, slotDefinesType: true, requirementKeyed: true, acceptsNow: false);
			var resolution = pending.ResolveOnRemoval(13517, queueSeen: true, placementProven: proof);
			Assert.That(resolution, Is.EqualTo(BotQueuePendingResolution.EmitRequested), "the 13507 plug placement resolves as Placed");

			var after = new List<BotQueueTransition>(raw.Take(3))
			{
				Emit(13517, BotQueueTransitionKind.Placed, BotQueueCancellationClass.None, 4, 1, item, "Addon", BuildingType.Building)
			};
			var census = Replay(after);
			Assert.That(census.Incomplete, Is.False, "no Unknown, the census stays complete");
			Assert.That(census.Evidence.Last(), Is.EqualTo(EconomyQueueEvidenceKind.ProvenPlacement));
			Assert.That(census.Evidence, Does.Not.Contain(EconomyQueueEvidenceKind.Unknown));
		}

		[Test]
		public void S2BotARefineryReplayBeforeIsUnknownAfterDestructionIsObservation()
		{
			var raw = ParseRaw(BotARaw);
			var before = Replay(raw);
			Assert.That(before.Incomplete, Is.True, "BEFORE: the stale-watch removal on a live producer is class None = Unknown");
			Assert.That(before.FirstUnknownTick, Is.EqualTo(22156));

			// AFTER: the queue left the probe with the item gone (sold or transformed, actor still live) at 22156.
			var m = new StaleModel();
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(22156), Is.Empty, "held, nothing emitted at the old Unknown tick");
			Assert.That(m.Sweep(22200), Is.Empty);

			m.Live = false; // the sell/transform animation ends and the actor is disposed
			var resolved = m.Sweep(22260).Single();
			Assert.That(resolved.Class, Is.EqualTo(BotQueueCancellationClass.Destruction));

			var after = new List<BotQueueTransition>(raw.Take(1))
			{
				Emit(22260, BotQueueTransitionKind.Removed, resolved.Class, 3, 37, "td_gdi_tiberiumrefinery", "Building",
					BuildingType.Refinery, producerLive: false)
			};
			var census = Replay(after);
			Assert.That(census.Incomplete, Is.False);
			Assert.That(census.Evidence.Last(), Is.EqualTo(EconomyQueueEvidenceKind.Observation));
		}

		[Test]
		public void S2BotAStaysHonestUnknownIfTheProducerOutlivesTheWindow()
		{
			var m = new StaleModel();
			var item = m.AddItem();
			m.Queued.Remove(item);
			Assert.That(m.Sweep(22156), Is.Empty);
			var resolved = m.Sweep(22156 + BaseBuilderQueueEvalCA.StaleResolveGraceTicks).Single();
			Assert.That(resolved.Class, Is.EqualTo(BotQueueCancellationClass.None));
			var census = Replay(ParseRaw(BotARaw).Take(1).Append(
				Emit(22306, BotQueueTransitionKind.Removed, resolved.Class, 3, 37, "td_gdi_tiberiumrefinery", "Building", BuildingType.Refinery)));
			Assert.That(census.Incomplete, Is.True, "the fix adds proof, it never relaxes the ladder");
		}
	}
}
