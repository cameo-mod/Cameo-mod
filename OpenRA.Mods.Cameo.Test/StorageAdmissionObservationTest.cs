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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// E1 observational slice (lead-accepted 2026-10-17): the world-free arithmetic behind
	// StorageAdmissionObservation — target/ceiling verdicts, reservation ledgering, and the
	// permanent double-count contract. World-bound seams (QueuedCapacity enumeration,
	// ObserveEmitting terms, QueueOrderTracked binding) remain call-site evidence.
	[TestFixture]
	public class StorageAdmissionObservationTest
	{
		[Test]
		public void TargetResolvesBaseStoredHysteresisAndCeiling()
		{
			// Ctarget = min(ceiling, max(base, S + H)); unset pieces stay unset.
			Assert.That(StorageAdmissionObservation.ResolveTarget(2000, 0, 1500, 0), Is.EqualTo(2000), "stored below base: base wins");
			Assert.That(StorageAdmissionObservation.ResolveTarget(2000, 0, 3000, 500), Is.EqualTo(3500), "stored + hysteresis above base");
			Assert.That(StorageAdmissionObservation.ResolveTarget(2000, 4000, 3000, 2000), Is.EqualTo(4000), "ceiling clamps");
			Assert.That(StorageAdmissionObservation.ResolveTarget(2000, 3000, 5000, 0), Is.EqualTo(3000), "ceiling clamps even when stored exceeds base");
			Assert.That(StorageAdmissionObservation.ResolveTarget(0, 0, 5000, 100), Is.EqualTo(0), "unset base stays unset — terms-only, no verdict");
			Assert.That(StorageAdmissionObservation.ResolveTarget(2000, 0, 1000, -50), Is.EqualTo(2000), "negative hysteresis clamps to none");
		}

		[Test]
		public void TargetVerdictIsStrictAtTheBound()
		{
			// Sol C4198CC4: committed == target must REFUSE — < not <=.
			Assert.That(StorageAdmissionObservation.VerdictTarget(2000, 2000), Is.False, "at the exact target is a refuse");
			Assert.That(StorageAdmissionObservation.VerdictTarget(1999, 2000), Is.True, "below the target admits");
			Assert.That(StorageAdmissionObservation.VerdictTarget(2001, 2000), Is.False, "above the target refuses");
			Assert.That(StorageAdmissionObservation.VerdictTarget(500, 0), Is.Null, "unset target emits no verdict");
		}

		[Test]
		public void CeilingVerdictAdmitsAtTheBound()
		{
			// The hard ceiling caps what committed may reach — at the bound is allowed (<=),
			// deliberately distinct from the strict target.
			Assert.That(StorageAdmissionObservation.VerdictCeiling(4000, 4000), Is.True, "at the ceiling admits");
			Assert.That(StorageAdmissionObservation.VerdictCeiling(4001, 4000), Is.False, "above the ceiling refuses");
			Assert.That(StorageAdmissionObservation.VerdictCeiling(500, 0), Is.Null, "unset ceiling emits no verdict");
		}

		[Test]
		public void EmittedReservationJoinsPendingOnce()
		{
			var ledger = new StorageAdmissionObservation(null);
			var r = new StorageAdmissionObservation.Reservation(ledger, null, "silox", 750);

			ledger.NoteEmitted(r, 100);
			Assert.That(ledger.PendingCapacity, Is.EqualTo(750));
			Assert.That(ledger.PendingCount, Is.EqualTo(1));

			ledger.NoteEmitted(r, 200);
			Assert.That(ledger.PendingCapacity, Is.EqualTo(750), "re-ledgering the same reservation never double-adds");
			Assert.That(ledger.PendingCount, Is.EqualTo(1));
		}

		[Test]
		public void ExactRefusalSettlesTheReservation()
		{
			var ledger = new StorageAdmissionObservation(null);
			var r = new StorageAdmissionObservation.Reservation(ledger, null, "silox", 750);
			ledger.NoteEmitted(r, 100);

			((IBotOrderDisposition)r).OrderDisposition(null, BotOrderOutcome.RefusedLocally);
			Assert.That(ledger.PendingCapacity, Is.EqualTo(0), "exact-instance refusal releases the commitment");
			Assert.That(ledger.PendingCount, Is.EqualTo(0));
		}

		[Test]
		public void IssuedToWorldKeepsTheReservationPending()
		{
			// THE PERMANENT DOUBLE-COUNT contract: routing release is not queue arrival —
			// the reservation stays pending while the item also counts in P_queued.
			var ledger = new StorageAdmissionObservation(null);
			var r = new StorageAdmissionObservation.Reservation(ledger, null, "silox", 750);
			ledger.NoteEmitted(r, 100);

			((IBotOrderDisposition)r).OrderDisposition(null, BotOrderOutcome.IssuedToWorld);
			Assert.That(ledger.PendingCapacity, Is.EqualTo(750), "issued keeps pending — arrival never settles");
			Assert.That(ledger.PendingCount, Is.EqualTo(1));

			((IBotOrderDisposition)r).OrderDisposition(null, BotOrderOutcome.Deferred);
			((IBotOrderDisposition)r).OrderDisposition(null, BotOrderOutcome.LocallyQueued);
			Assert.That(ledger.PendingCapacity, Is.EqualTo(750), "routing notes never settle either");
		}

		[Test]
		public void RefusalBeforeLedgeringNeverCounts()
		{
			// Registration precedes NoteEmitted — a synchronously refused order's reservation
			// must never be ledgered retroactively.
			var ledger = new StorageAdmissionObservation(null);
			var r = new StorageAdmissionObservation.Reservation(ledger, null, "silox", 750);
			((IBotOrderDisposition)r).OrderDisposition(null, BotOrderOutcome.RefusedLocally);

			ledger.NoteEmitted(r, 100);
			Assert.That(ledger.PendingCapacity, Is.EqualTo(0));
			Assert.That(ledger.PendingCount, Is.EqualTo(0));
		}
	}
}
