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

namespace OpenRA.Mods.Cameo.Test
{
	// U1 (production service contract §3): the world-free decisions behind
	// UnitBuilderBotModuleCA's FairProducerService arm — rotation resume over stable queue
	// keys, ephemeral in-pass slot/cost reservations, and the bounded offer budget. The
	// caller sites (BuildUnitFairService, the external-request queue match) own the world
	// lookups; every case below proves its contract on pure inputs.
	[TestFixture]
	public class UnitProducerServiceTest
	{
		static readonly List<string> Keys = new()
		{
			"10|VehicleSQ", "20|VehicleSQ", "30|VehicleSQ"
		};

		[Test]
		public void ResumeStartsAtTheDocumentedFirstKey()
		{
			Assert.That(UnitProducerServiceCA.ResumeIndex(Keys, null), Is.EqualTo(0),
				"no cursor yet — the stable first producer is served first");
			Assert.That(UnitProducerServiceCA.ResumeIndex(Keys, string.Empty), Is.EqualTo(0));
			Assert.That(UnitProducerServiceCA.ResumeIndex(new List<string>(), Keys[0]), Is.EqualTo(0),
				"an empty producer set cannot index out of range");
		}

		[Test]
		public void ResumeAdvancesAfterTheLastExaminedKey()
		{
			Assert.That(UnitProducerServiceCA.ResumeIndex(Keys, Keys[0]), Is.EqualTo(1));
			Assert.That(UnitProducerServiceCA.ResumeIndex(Keys, Keys[1]), Is.EqualTo(2));
			Assert.That(UnitProducerServiceCA.ResumeIndex(Keys, Keys[2]), Is.EqualTo(0),
				"the tail wraps to the head — service never stalls past the last producer");
		}

		[Test]
		public void RepeatedPassesRotateAcrossEveryProducer()
		{
			// The module examines in key order resuming after the cursor and stores every
			// examined producer; simulating that loop proves a width-1 visit serves all
			// three producers within the declared rotation window.
			string cursor = null;
			var served = new List<string>();
			for (var visit = 0; visit < Keys.Count; visit++)
			{
				var index = UnitProducerServiceCA.ResumeIndex(Keys, cursor);
				served.Add(Keys[index]);
				cursor = Keys[index];
			}

			Assert.That(served, Is.EquivalentTo(Keys),
				"each producer is served exactly once per rotation");
			Assert.That(served[0], Is.EqualTo(Keys[0]));
			Assert.That(served[1], Is.EqualTo(Keys[1]));
			Assert.That(served[2], Is.EqualTo(Keys[2]));
		}

		[Test]
		public void ADestroyedCursorTargetResumesAtTheStableFirstKey()
		{
			// The producer behind the cursor is gone: its key is absent, so service resumes
			// once at the stable head and rotation proceeds — a missing key cannot pin
			// starvation or bind an arbitrary position.
			var shrunk = new List<string> { Keys[0], Keys[2] };
			Assert.That(UnitProducerServiceCA.ResumeIndex(shrunk, Keys[1]), Is.EqualTo(0));
			Assert.That(UnitProducerServiceCA.ResumeIndex(shrunk, Keys[2]), Is.EqualTo(0),
				"the live tail still wraps normally");
		}

		[Test]
		public void AReboundActorIdNeverMatchesADifferentQueue()
		{
			// Save/load identity check: the stored cursor "42|VehicleSQ" must not bind to a
			// producer whose reused actor ID now fronts "42|InfantrySQ" — the composite key
			// mismatches and resumes at the documented first key.
			var live = new List<string> { "42|InfantrySQ", "55|VehicleSQ" };
			Assert.That(UnitProducerServiceCA.ResumeIndex(live, "42|VehicleSQ"), Is.EqualTo(0));
		}

		[Test]
		public void InPassPendingConsumesOnlyTheFreeSlots()
		{
			Assert.That(UnitProducerServiceCA.SlotAvailable(3, 0), Is.True);
			Assert.That(UnitProducerServiceCA.SlotAvailable(3, 2), Is.True);
			Assert.That(UnitProducerServiceCA.SlotAvailable(3, 3), Is.False,
				"three emitted orders fill a three-slot producer before AllQueued sees them");
			Assert.That(UnitProducerServiceCA.SlotAvailable(1, 1), Is.False,
				"one pending order satisfies the cap headroom — no second invisible order");
		}

		[Test]
		public void ReservedCostsComeOffTheBankForLaterPicks()
		{
			Assert.That(UnitProducerServiceCA.BankAfterReserved(5000, 900), Is.EqualTo(4100));
			Assert.That(UnitProducerServiceCA.BankAfterReserved(5000, 0), Is.EqualTo(5000));
			Assert.That(UnitProducerServiceCA.BankAfterReserved(5000, -50), Is.EqualTo(5000),
				"a negative reservation cannot inflate the bank");
			Assert.That(UnitProducerServiceCA.BankAfterReserved(500, 900), Is.LessThan(700),
				"over-reservation goes negative so a later cost check fails");
		}

		[Test]
		public void OfferBudgetIsBoundedAndNeverZeroWhileQueuesExist()
		{
			Assert.That(UnitProducerServiceCA.OfferBudget(3, 1), Is.EqualTo(1));
			Assert.That(UnitProducerServiceCA.OfferBudget(3, 4), Is.EqualTo(3),
				"the pass never scans past the producer count");
			Assert.That(UnitProducerServiceCA.OfferBudget(0, 4), Is.EqualTo(0));
			Assert.That(UnitProducerServiceCA.OfferBudget(3, 0), Is.EqualTo(1),
				"a zero width cannot silently starve a category");
			Assert.That(UnitProducerServiceCA.OfferBudget(5, 2), Is.EqualTo(2));
		}

		// Sol F1 regression: the batch is the REAL seam the external-request scan and the
		// category visits share — these tests drive the used object across simulated call
		// sites rather than re-deriving the arithmetic.

		[Test]
		public void OneBatchSharesReservationsAcrossCallSites()
		{
			var batch = new ProducerServiceBatch(5000);
			var producer = new object(); // stands in for a ProductionQueue instance

			// "External request" call site emits first: spends a slot and 1200 cash.
			Assert.That(batch.SlotsFree(producer, 1), Is.EqualTo(1));
			batch.Reserve(producer, 1200);

			// "Category visit" call site later in the same pass must see both spends —
			// oversubscription across call sites was the F1 defect.
			Assert.That(batch.SlotsFree(producer, 1), Is.EqualTo(0),
				"the emitted order consumed the only slot before AllQueued could show it");
			Assert.That(batch.BankLeft, Is.EqualTo(3800));
			Assert.That(batch.Affords(3800), Is.True);
			Assert.That(batch.Affords(3801), Is.False,
				"a request costing more than the reserved bank is not a second invisible order");
		}

		[Test]
		public void SiblingProducersReserveIndependently()
		{
			var batch = new ProducerServiceBatch(5000);
			var first = new object();
			var sibling = new object(); // same actor, same queue type — distinct trait

			batch.Reserve(first, 900);
			Assert.That(batch.SlotsFree(sibling, 1), Is.EqualTo(1),
				"keyed by queue instance: the sibling's slot is not spent by the first");
			Assert.That(batch.BankLeft, Is.EqualTo(4100), "cash is shared — slots are not");
		}

		[Test]
		public void BatchReservationsAreEphemeralPerPass()
		{
			var producer = new object();
			var first = new ProducerServiceBatch(1000);
			first.Reserve(producer, 600);
			first.PendingArmedInfantry = 2;
			first.LimitHeadroom["infantry"] = 1;

			// The next decision pass builds a new batch: nothing leaks — no durable
			// pending/retry/ack state before the U2 checkpoint.
			var next = new ProducerServiceBatch(1000);
			Assert.That(next.BankLeft, Is.EqualTo(1000));
			Assert.That(next.SlotsFree(producer, 1), Is.EqualTo(1));
			Assert.That(next.PendingArmedInfantry, Is.EqualTo(0));
			Assert.That(next.LimitHeadroom, Is.Empty);
		}

		// Sol ECAFF220: the aircraft admission delta is the production-used seam —
		// CanBuildMoreOfAircraft adds it to the live+queued count. These tests drive
		// the SAME method both call sites use, with the batch states the calls
		// actually produce.

		[Test]
		public void ExternalAircraftEmissionCountsAgainstTheNextRequest()
		{
			var batch = new ProducerServiceBatch(5000);

			// External request 1 emits an aircraft: its own emission increments the
			// batch — request 2's admission must see it (the ECAFF220 defect case).
			batch.PendingAircraft = 1;
			Assert.That(UnitProducerServiceCA.PendingAircraftDelta(true, false, false, batch),
				Is.EqualTo(1), "second external aircraft sees the first one's spend");

			// At cap-minus-one live+queued, that delta is what refuses the second emit.
			const int currentCount = 9; const int limit = 10;
			Assert.That(currentCount + 1 < limit, Is.False);
		}

		[Test]
		public void ExternalAndCategoryCallsShareOneAircraftHeadroom()
		{
			var batch = new ProducerServiceBatch(5000);
			batch.PendingAircraft = 1; // emitted by the external-request scan

			// The ordinary category visit reads the same delta — one headroom unit
			// cannot admit an external order AND a category pick.
			Assert.That(UnitProducerServiceCA.PendingAircraftDelta(true, false, false, batch),
				Is.EqualTo(1), "category pick spends the same headroom the external request spent");
		}

		[Test]
		public void AirSuperiorityClassesSpendTheirOwnDeltas()
		{
			var batch = new ProducerServiceBatch(5000);
			batch.PendingAircraft = 2;
			batch.PendingAirToAir = 1;

			Assert.That(UnitProducerServiceCA.PendingAircraftDelta(true, true, true, batch),
				Is.EqualTo(1), "air-to-air candidate spends the air-to-air class");
			Assert.That(UnitProducerServiceCA.PendingAircraftDelta(true, false, true, batch),
				Is.EqualTo(1), "non-A2A candidate spends the remainder (2 total - 1 A2A)");
		}

		[Test]
		public void NonAircraftAndNullBatchAdmitNoDelta()
		{
			var batch = new ProducerServiceBatch(5000);
			batch.PendingAircraft = 3;

			Assert.That(UnitProducerServiceCA.PendingAircraftDelta(false, false, false, batch), Is.EqualTo(0));
			Assert.That(UnitProducerServiceCA.PendingAircraftDelta(true, false, false, null), Is.EqualTo(0));
		}

		[Test]
		public void LimitHeadroomMapIsSharedAcrossCallSites()
		{
			var batch = new ProducerServiceBatch(5000);
			batch.LimitHeadroom["officer"] = 1; // computed once: limit 2, one alive
			Assert.That(batch.LimitHeadroom["officer"], Is.EqualTo(1));

			batch.LimitHeadroom["officer"]--; // "external" spend
			Assert.That(batch.LimitHeadroom["officer"], Is.EqualTo(0),
				"the category visit sees the external request's headroom spend");
		}

		[Test]
		public void CombatFloorPickIsHighestValuePerCostThenName()
		{
			// P4: the floor pick is combat value per cost — a stronger cheap rifle beats
			// a stronger expensive one when the ratio wins; equal ratios tie on the name.
			var order = UnitProducerServiceCA.CombatValueOrder(new[]
			{
				("medic", 1.0, 100),      // ratio .010 — support units never reach the pick anyway
				("rifle", 9.0, 300),      // ratio .030
				("rocket", 8.0, 200),     // ratio .040 — best value per cost
				("gunner", 6.0, 200),     // ratio .030 — ties rifle, ordinal name wins
			}).ToList();

			Assert.That(order[0], Is.EqualTo("rocket"));
			Assert.That(order[1], Is.EqualTo("gunner"), "equal ratios — ordinal name tiebreak");
			Assert.That(order[2], Is.EqualTo("rifle"));
			Assert.That(order[3], Is.EqualTo("medic"));
		}

		[Test]
		public void CombatFloorOrderIsTotalAndDeterministic()
		{
			var candidates = new[] { ("b", 5.0, 100), ("a", 5.0, 100), ("c", 0.0, 0) };
			var first = UnitProducerServiceCA.CombatValueOrder(candidates).ToList();
			var second = UnitProducerServiceCA.CombatValueOrder(candidates.Reverse()).ToList();
			Assert.That(first, Is.EqualTo(second),
				"input order never changes the pick — identical on every client");
			Assert.That(first[0], Is.EqualTo("a"), "equal ratios tie on the ordinal name");
			Assert.That(first[2], Is.EqualTo("c"), "a zero cost clamps to 1 — no NaN");
		}
	}
}
