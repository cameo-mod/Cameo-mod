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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// U2 (service-contract §5, Sol dispositions 0CB8EF18/6919D6E4): the world-free
	// objects behind the intent lifecycle — BuildIntentLedger (Waiting/EmittedPending/
	// Unresolved, counted once), BuildIntentOrderBindings (order→intent resolution
	// with terminal consume), and BotOrderDispositionTracker (the registration seam
	// ModularBot calls). Each case drives the SAME objects the production call sites
	// drive, in the same order — the FIFO/gate mechanics in ModularBot remain static
	// call-site evidence, not re-implemented here.
	[TestFixture]
	public class BuildIntentLedgerTest
	{
		sealed class RecordingObserver : IBotOrderDisposition
		{
			public readonly List<(Order Order, BotOrderOutcome Outcome)> Calls = new();
			public void OrderDisposition(Order order, BotOrderOutcome outcome) { Calls.Add((order, outcome)); }
		}

		[Test]
		public void RequestStartsWaitingAndCountsOnce()
		{
			var ledger = new BuildIntentLedger();
			var intent = ledger.Request("mcv");
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.Waiting));
			Assert.That(ledger.Count("mcv"), Is.EqualTo(1), "one intent counts once");
			Assert.That(ledger.HasWaiting, Is.True);
		}

		[Test]
		public void DuplicateRequestsCountIndependently()
		{
			var ledger = new BuildIntentLedger();
			ledger.Request("harv");
			ledger.Request("harv");
			Assert.That(ledger.Count("harv"), Is.EqualTo(2));
		}

		[Test]
		public void EmittedPendingKeepsCoverageWithheld()
		{
			var ledger = new BuildIntentLedger();
			var intent = ledger.Request("mcv");
			intent.State = BuildIntentState.EmittedPending;
			Assert.That(ledger.Count("mcv"), Is.EqualTo(1),
				"an emitted-but-unproven intent still covers the demand — the caller cannot re-request it");
			Assert.That(ledger.HasWaiting, Is.False);
			Assert.That(ledger.WaitingNames(), Is.Empty);
			Assert.That(ledger.PendingNames(), Is.EqualTo(new[] { "mcv" }));
		}

		[Test]
		public void RefusalReturnsIntentToWaiting()
		{
			var intent = new BuildIntent("mcv", BuildIntentState.EmittedPending);
			intent.NoteDisposition(BotOrderOutcome.RefusedLocally);
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.Waiting),
				"the refused order instance never issues — retry is legal");
			Assert.That(intent.LastDisposition, Is.EqualTo(BotOrderOutcome.RefusedLocally));
		}

		[Test]
		public void RoutingNotesOnlyAnnotate()
		{
			var intent = new BuildIntent("mcv", BuildIntentState.EmittedPending);
			foreach (var note in new[] { BotOrderOutcome.LocallyQueued, BotOrderOutcome.Deferred, BotOrderOutcome.IssuedToWorld })
			{
				intent.NoteDisposition(note);
				Assert.That(intent.State, Is.EqualTo(BuildIntentState.EmittedPending),
					$"{note} is a routing note, not a settlement — IssuedToWorld included");
				Assert.That(intent.LastDisposition, Is.EqualTo(note));
			}
		}

		[Test]
		public void BindingsConsumeOnTerminalOutcomes()
		{
			var bindings = new BuildIntentOrderBindings();
			var intent = new BuildIntent("mcv", BuildIntentState.EmittedPending);
			var order = new Order("StartProduction", null, false);
			bindings.Bind(order, intent);

			bindings.NoteDisposition(order, BotOrderOutcome.IssuedToWorld);
			Assert.That(intent.LastDisposition, Is.EqualTo(BotOrderOutcome.IssuedToWorld));
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.EmittedPending));

			var before = intent.LastDisposition;
			bindings.NoteDisposition(order, BotOrderOutcome.RefusedLocally);
			Assert.That(intent.LastDisposition, Is.EqualTo(before),
				"a consumed binding delivers nothing twice");
		}

		[Test]
		public void BindingsRefusalRequeuesAndUnbinds()
		{
			var bindings = new BuildIntentOrderBindings();
			var intent = new BuildIntent("mcv", BuildIntentState.EmittedPending);
			var order = new Order("StartProduction", null, false);
			bindings.Bind(order, intent);

			bindings.NoteDisposition(order, BotOrderOutcome.RefusedLocally);
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.Waiting));

			// The binding is gone — a stale later note cannot resurrect it.
			bindings.NoteDisposition(order, BotOrderOutcome.IssuedToWorld);
			Assert.That(intent.LastDisposition, Is.EqualTo(BotOrderOutcome.RefusedLocally));
		}

		[Test]
		public void BindingsIgnoreUntrackedOrders()
		{
			var bindings = new BuildIntentOrderBindings();
			bindings.NoteDisposition(new Order("StartProduction", null, false), BotOrderOutcome.RefusedLocally);
			bindings.NoteDisposition(null, BotOrderOutcome.RefusedLocally);
		}

		[Test]
		public void WaitingSnapshotSkipsPendingAndPreservesOrder()
		{
			var ledger = new BuildIntentLedger();
			ledger.Request("a");
			var emitted = ledger.Request("b");
			ledger.Request("c");
			emitted.State = BuildIntentState.EmittedPending;

			var waiting = ledger.WaitingSnapshot();
			Assert.That(waiting.Select(i => i.Name), Is.EqualTo(new[] { "a", "c" }),
				"request order preserved; emitted intents are not waiting");
			Assert.That(ledger.NextWaiting().Name, Is.EqualTo("a"));
		}

		[Test]
		public void MigrationConsumesTheOldStringListOnce()
		{
			var ledger = new BuildIntentLedger();
			ledger.MigrateWaiting(new[] { "e1", "harv", "e1" });
			Assert.That(ledger.Count("e1"), Is.EqualTo(2));
			Assert.That(ledger.Count("harv"), Is.EqualTo(1));
			Assert.That(ledger.WaitingNames(), Is.EqualTo(new[] { "e1", "harv", "e1" }),
				"the ledger replaces the list — migrated strings are never double-counted");
		}

		[Test]
		public void LoadPendingBecomesUnresolvedCoverage()
		{
			var ledger = new BuildIntentLedger();
			ledger.LoadPending(new[] { "mcv", "harv" });
			Assert.That(ledger.Entries.All(i => i.State == BuildIntentState.Unresolved), Is.True,
				"emitted-at-save loads Unresolved — loss is never assumed (Sol 6919D6E4)");
			Assert.That(ledger.Count("mcv"), Is.EqualTo(1), "coverage withheld — no re-request");
			Assert.That(ledger.HasWaiting, Is.False);
		}

		[Test]
		public void SaveRoundTripPreservesCoverageExactlyOnce()
		{
			var ledger = new BuildIntentLedger();
			ledger.Request("waiting1");
			var emitted = ledger.Request("emitted1");
			emitted.State = BuildIntentState.EmittedPending;
			ledger.LoadPending(new[] { "old_unresolved" });

			// The save nodes the module emits.
			var savedWaiting = ledger.WaitingNames();
			var savedPending = ledger.PendingNames();
			Assert.That(savedWaiting, Is.EqualTo(new[] { "waiting1" }));
			Assert.That(savedPending.OrderBy(n => n), Is.EqualTo(new[] { "emitted1", "old_unresolved" }));
			Assert.That(savedWaiting.Intersect(savedPending), Is.Empty, "the two nodes are disjoint — nothing counts twice");

			// The load path applies them to a fresh ledger.
			var loaded = new BuildIntentLedger();
			loaded.MigrateWaiting(savedWaiting);
			loaded.LoadPending(savedPending);
			Assert.That(loaded.Count("waiting1"), Is.EqualTo(1));
			Assert.That(loaded.Count("emitted1"), Is.EqualTo(1));
			Assert.That(loaded.Count("old_unresolved"), Is.EqualTo(1));
			Assert.That(loaded.Entries.Count(), Is.EqualTo(3));
			Assert.That(loaded.Entries.Single(i => i.Name == "waiting1").State, Is.EqualTo(BuildIntentState.Waiting));
			Assert.That(loaded.Entries.Where(i => i.Name != "waiting1").All(i => i.State == BuildIntentState.Unresolved), Is.True);
		}

		[Test]
		public void RemoveDropsOnlyTheNamedIntent()
		{
			var ledger = new BuildIntentLedger();
			var a = ledger.Request("e1");
			ledger.Request("e1");
			Assert.That(ledger.Remove(a), Is.True);
			Assert.That(ledger.Count("e1"), Is.EqualTo(1),
				"the exact record leaves — a same-name sibling survives");
		}

		[Test]
		public void TrackerRejectsNullGroupedAndDuplicateRegistration()
		{
			var tracker = new BotOrderDispositionTracker();
			var observer = new RecordingObserver();
			Assert.That(tracker.Register(null, observer), Is.False);
			Assert.That(tracker.Register(new Order("StartProduction", null, false), null), Is.False);
			Assert.That(tracker.Register(new Order("StartProduction", null, false, null, new Actor[0]), observer), Is.False,
				"a grouped order rebuilds into a different instance — identity cannot be kept");

			var order = new Order("StartProduction", null, false);
			Assert.That(tracker.Register(order, observer), Is.True);
			Assert.That(tracker.Register(order, new RecordingObserver()), Is.False,
				"a second observer on the same instance never overwrites the first");
		}

		[Test]
		public void TrackerResidentNotesKeepRegistration()
		{
			var tracker = new BotOrderDispositionTracker();
			var observer = new RecordingObserver();
			var order = new Order("StartProduction", null, false);
			Assert.That(tracker.Register(order, observer), Is.True);

			tracker.NotifyResident(order, BotOrderOutcome.LocallyQueued);
			tracker.NotifyResident(order, BotOrderOutcome.Deferred);
			tracker.NotifyResident(order, BotOrderOutcome.Deferred);
			Assert.That(observer.Calls.Count, Is.EqualTo(3));
			Assert.That(observer.Calls.Select(c => c.Order), Is.All.EqualTo(order));

			tracker.NotifyTerminal(order, BotOrderOutcome.IssuedToWorld);
			Assert.That(observer.Calls.Count, Is.EqualTo(4));
		}

		[Test]
		public void TrackerTerminalDeliversExactlyOnce()
		{
			var tracker = new BotOrderDispositionTracker();
			var observer = new RecordingObserver();
			var order = new Order("StartProduction", null, false);
			Assert.That(tracker.Register(order, observer), Is.True);

			tracker.NotifyTerminal(order, BotOrderOutcome.RefusedLocally);
			tracker.NotifyTerminal(order, BotOrderOutcome.IssuedToWorld);
			tracker.NotifyResident(order, BotOrderOutcome.Deferred);
			Assert.That(observer.Calls.Count, Is.EqualTo(1),
				"terminal consumes the registration — nothing leaks, nothing double-delivers");
			Assert.That(observer.Calls[0].Outcome, Is.EqualTo(BotOrderOutcome.RefusedLocally));

			Assert.That(tracker.Register(order, observer), Is.True,
				"a consumed registration may be re-bound by a fresh queue");
		}

		[Test]
		public void TrackerIgnoresUnregisteredTraffic()
		{
			var tracker = new BotOrderDispositionTracker();
			var order = new Order("StartProduction", null, false);
			tracker.NotifyResident(order, BotOrderOutcome.LocallyQueued);
			tracker.NotifyTerminal(order, BotOrderOutcome.IssuedToWorld);
		}

		[Test]
		public void SimulatedEmitSequenceMatchesProductionCallOrder()
		{
			// The exact sequence EmitIntentProduction + ModularBot drive: ledger
			// request → intent emitted → binding → tracker registration →
			// LocallyQueued (synchronous inside the enqueue) → IssuedToWorld at drain.
			var ledger = new BuildIntentLedger();
			var bindings = new BuildIntentOrderBindings();
			var tracker = new BotOrderDispositionTracker();
			var observer = new RoutedObserver(bindings);
			var intent = ledger.Request("mcv");
			intent.State = BuildIntentState.EmittedPending;
			var order = new Order("StartProduction", null, false);
			bindings.Bind(order, intent);
			Assert.That(tracker.Register(order, observer), Is.True);

			// AddLast then NotifyResident(LocallyQueued) — Enqueue's order.
			tracker.NotifyResident(order, BotOrderOutcome.LocallyQueued);
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.EmittedPending));
			Assert.That(intent.LastDisposition, Is.EqualTo(BotOrderOutcome.LocallyQueued));
			Assert.That(ledger.Count("mcv"), Is.EqualTo(1));

			// world.IssueOrder then NotifyTerminal(IssuedToWorld) — the drain's order.
			tracker.NotifyTerminal(order, BotOrderOutcome.IssuedToWorld);
			Assert.That(intent.LastDisposition, Is.EqualTo(BotOrderOutcome.IssuedToWorld));
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.EmittedPending),
				"issued is not delivered — coverage stays withheld");
			Assert.That(ledger.Count("mcv"), Is.EqualTo(1));
		}

		[Test]
		public void SimulatedDeferredThenRefusedRestoresWaiting()
		{
			var ledger = new BuildIntentLedger();
			var bindings = new BuildIntentOrderBindings();
			var tracker = new BotOrderDispositionTracker();
			var observer = new RoutedObserver(bindings);

			var intent = ledger.Request("harv");
			intent.State = BuildIntentState.EmittedPending;
			var order = new Order("StartProduction", null, false);
			bindings.Bind(order, intent);
			Assert.That(tracker.Register(order, observer), Is.True);

			// Budget deferral keeps the registration; a later gate refusal is terminal.
			tracker.NotifyResident(order, BotOrderOutcome.Deferred);
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.EmittedPending));
			tracker.NotifyTerminal(order, BotOrderOutcome.RefusedLocally);
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.Waiting),
				"the refused instance never issues — the intent may legally re-emit");
			Assert.That(ledger.HasWaiting, Is.True);

			// A stale terminal on the consumed binding cannot flip it back.
			tracker.NotifyTerminal(order, BotOrderOutcome.IssuedToWorld);
			Assert.That(intent.State, Is.EqualTo(BuildIntentState.Waiting));
		}

		// Routes tracker callbacks through the same bindings object the module uses.
		sealed class RoutedObserver : IBotOrderDisposition
		{
			readonly BuildIntentOrderBindings bindings;
			public RoutedObserver(BuildIntentOrderBindings bindings) { this.bindings = bindings; }
			public void OrderDisposition(Order order, BotOrderOutcome outcome) { bindings.NoteDisposition(order, outcome); }
		}
	}
}
