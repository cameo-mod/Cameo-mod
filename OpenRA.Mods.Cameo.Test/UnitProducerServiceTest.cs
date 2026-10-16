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
	}
}
