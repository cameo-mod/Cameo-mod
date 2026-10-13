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
	// TF-2 (FINDINGS_2026-10-11_turtle_regression increment 2): the mobile-production
	// reservation — a reviewed share held only while production is feasible, capacity is free
	// and the offensive pool needs replenishment; optional spend cannot consume it; every
	// terminal path (produced, refunded, cancelled, producer eliminated) releases exactly once.
	[TestFixture]
	public class UnitSpendReservationTest
	{
		static List<(string Item, int Total, int Remaining)> Units(params (string Name, int Total, int Remaining)[] items) =>
			items.Select(i => (i.Name, i.Total, i.Remaining)).ToList();

		static List<(string Item, int Total, int Remaining, bool Essential)> Buildings(params (string Name, int Total, int Remaining, bool Essential)[] items) =>
			items.Select(i => (i.Name, i.Total, i.Remaining, i.Essential)).ToList();

		[Test]
		public void StandingHoldsOnlyWhileAllThreeConditionsHold()
		{
			var r = new UnitSpendReservation();
			r.SetStanding(feasible: true, capacity: true, poolNeedsReplenish: true, sharePct: 25, spendable: 4000);
			Assert.That(r.Standing, Is.EqualTo(1000), "25% of spendable while every condition holds");

			r.SetStanding(false, true, true, 25, 4000);
			Assert.That(r.Standing, Is.EqualTo(0), "no feasible production — nothing reserved");
			r.SetStanding(true, false, true, 25, 4000);
			Assert.That(r.Standing, Is.EqualTo(0), "no free mobile capacity — nothing reserved");
			r.SetStanding(true, true, false, 25, 4000);
			Assert.That(r.Standing, Is.EqualTo(0), "pool already replenished — nothing reserved");
			r.SetStanding(true, true, true, 0, 4000);
			Assert.That(r.Standing, Is.EqualTo(0), "share 0 keeps the whole feature off");
		}

		[Test]
		public void FloorIsStandingPlusCommittedUnitSpendCappedAtSpendable()
		{
			var r = new UnitSpendReservation();
			r.SetStanding(true, true, true, 25, 4000);
			Assert.That(r.Floor(4000), Is.EqualTo(1000));

			var factory = new object();
			r.ReconcileUnits(factory, Units(("tank", 900, 600)));
			Assert.That(r.Floor(4000), Is.EqualTo(1600),
				"the in-flight unit's unpaid remainder protects committed production too");

			Assert.That(r.Floor(1200), Is.EqualTo(1200), "the floor never exceeds what there is to spend");
			Assert.That(r.Floor(-50), Is.EqualTo(0));
		}

		[Test]
		public void ASettledUnitItemReleasesExactlyOnce()
		{
			var r = new UnitSpendReservation();
			var factory = new object();
			r.SetStanding(true, true, true, 25, 4000);

			r.ReconcileUnits(factory, Units(("tank", 900, 600)));
			r.ReconcileUnits(factory, Units(("tank", 900, 300))); // it paid 300 more
			Assert.That(r.Floor(4000), Is.EqualTo(1300), "outstanding tracks the remaining cost");

			r.ReconcileUnits(factory, Units()); // produced: the item left the queue
			Assert.That(r.PaidUnitCost, Is.EqualTo(600), "paid-so-far counts at settle");
			Assert.That(r.Floor(4000), Is.EqualTo(1000), "the released remainder leaves only the share");

			r.ReconcileUnits(factory, Units());
			Assert.That(r.PaidUnitCost, Is.EqualTo(600),
				"a second empty reconcile cannot settle the same item twice");
		}

		[Test]
		public void ACancelledOrRefundedItemCountsOnlyItsPaidPart()
		{
			var r = new UnitSpendReservation();
			var factory = new object();
			r.SetStanding(true, true, true, 25, 4000);
			r.ReconcileUnits(factory, Units(("tank", 900, 500)));

			// Cancelled after paying 400: the remaining 500 refunds and frees off the floor.
			r.ReconcileUnits(factory, Units());
			Assert.That(r.PaidUnitCost, Is.EqualTo(400));
			Assert.That(r.UnitOutstanding, Is.EqualTo(0), "the refunded remainder holds nothing");
		}

		[Test]
		public void CompetingQueuesKeepIndependentClaimsAndReleases()
		{
			var r = new UnitSpendReservation();
			var warFactory = new object();
			var airfield = new object();
			r.SetStanding(true, true, true, 25, 8000);

			r.ReconcileUnits(warFactory, Units(("tank", 900, 900)));
			r.ReconcileUnits(airfield, Units(("heli", 1200, 600)));
			Assert.That(r.Floor(8000), Is.EqualTo(2000 + 1500));
			Assert.That(r.UnitItemsInFlight, Is.EqualTo(2));

			// The airfield's producer dies: only its claim settles; the war factory's survives.
			r.ReconcileUnits(airfield, Units());
			Assert.That(r.PaidUnitCost, Is.EqualTo(600));
			Assert.That(r.UnitItemsInFlight, Is.EqualTo(1));
			Assert.That(r.Floor(8000), Is.EqualTo(2000 + 900),
				"elimination releases exactly that producer's hold");
		}

		[Test]
		public void BuildingFeedSplitsEssentialAndOptionalPaidSpend()
		{
			var r = new UnitSpendReservation();
			var conyard = new object();
			r.ReconcileBuildings(conyard, Buildings(
				("refinery", 2000, 1000, true), ("turret", 800, 400, false)));

			r.ReconcileBuildings(conyard, Buildings()); // both left the queue
			Assert.That(r.PaidEssentialCost, Is.EqualTo(1000));
			Assert.That(r.PaidOptionalCost, Is.EqualTo(400));
			Assert.That(r.BuildingItemsInFlight, Is.EqualTo(0));
		}

		[Test]
		public void PaidSharePctCountsSettledAndInFlightSpend()
		{
			var r = new UnitSpendReservation();
			var factory = new object();
			var conyard = new object();
			Assert.That(r.PaidSharePct(), Is.EqualTo(0), "no spend observed — no share");

			r.ReconcileUnits(factory, Units(("tank", 1000, 500)));         // 500 paid, in flight
			r.ReconcileBuildings(conyard, Buildings(("turret", 1000, 0, false))); // 1000 paid, in flight
			Assert.That(r.PaidSharePct(), Is.EqualTo(33), "live spend counts toward the share");

			r.ReconcileUnits(factory, Units());
			r.ReconcileBuildings(conyard, Buildings());
			Assert.That(r.PaidSharePct(), Is.EqualTo(33), "settled spend keeps the share");
		}

		[Test]
		public void SettledCountersSaturateInsteadOfWrapping()
		{
			var r = new UnitSpendReservation();
			var factory = new object();
			r.ReconcileUnits(factory, Units(("a", int.MaxValue - 10, 0)));
			r.ReconcileUnits(factory, Units());
			r.ReconcileUnits(factory, Units(("b", int.MaxValue - 10, 0)));
			r.ReconcileUnits(factory, Units());
			Assert.That(r.PaidUnitCost, Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void RestoreSettledRehydratesTheMetric()
		{
			var r = new UnitSpendReservation();
			r.RestoreSettled(1500, 500, 1000);
			Assert.That(r.PaidSharePct(), Is.EqualTo(50));
		}

		// F1 (REVIEW_2026-10-11_turtle_fix2_devin.md): five same-name units on one producer are
		// five instances, not one — a name-keyed feed collapsed them and undercounted both the
		// in-flight remainder and every settled paid portion.

		[Test]
		public void SameNameQueuedUnitsCountPerInstance()
		{
			var r = new UnitSpendReservation();
			var factory = new object();
			r.SetStanding(true, true, true, 25, 8000);

			r.ReconcileUnits(factory, Units(
				("tank", 900, 900), ("tank", 900, 900), ("tank", 900, 900)));
			Assert.That(r.UnitItemsInFlight, Is.EqualTo(3));
			Assert.That(r.UnitOutstanding, Is.EqualTo(2700),
				"three queued tanks hold three remainders, not one collapsed entry");

			// The front tank pays down; the queue still reports three.
			r.ReconcileUnits(factory, Units(
				("tank", 900, 900), ("tank", 900, 900), ("tank", 900, 400)));
			Assert.That(r.UnitOutstanding, Is.EqualTo(2200));

			// It finishes: two remain, its last-observed paid portion settles.
			r.ReconcileUnits(factory, Units(("tank", 900, 900), ("tank", 900, 900)));
			Assert.That(r.UnitItemsInFlight, Is.EqualTo(2));
			Assert.That(r.PaidUnitCost, Is.EqualTo(500),
				"the completed front item settles its paid progress, the others keep flying");
		}

		[Test]
		public void CompletionAndRefillInTheSameSweepStillSettleTheFinishedItem()
		{
			var r = new UnitSpendReservation();
			var factory = new object();

			r.ReconcileUnits(factory, Units(("tank", 900, 400)));
			// One sweep later the finished tank is gone and a fresh one queued behind it.
			r.ReconcileUnits(factory, Units(("tank", 900, 900)));
			Assert.That(r.PaidUnitCost, Is.EqualTo(500),
				"a remainder that grows means a new instance — the old one settles");
			Assert.That(r.UnitOutstanding, Is.EqualTo(900));
		}

		[Test]
		public void ATailCancelReleasesNothingWhileProgressedItemsSurvive()
		{
			var r = new UnitSpendReservation();
			var factory = new object();
			r.ReconcileUnits(factory, Units(
				("tank", 900, 900), ("tank", 900, 900), ("tank", 900, 400)));

			// The unstarted tail is cancelled: the vanished entry is the unprogressed one —
			// it settles paid 0, the front keeps its progress.
			r.ReconcileUnits(factory, Units(("tank", 900, 900), ("tank", 900, 350)));
			Assert.That(r.PaidUnitCost, Is.EqualTo(0));
			Assert.That(r.UnitOutstanding, Is.EqualTo(1250));
		}

		// F2: a producer that leaves FindQueues (destroyed or disabled) can never call reconcile —
		// the sweep-end flush settles its tracked items so the unpaid remainder does not pin the
		// floor and the paid-so-far part still credits.

		[Test]
		public void AVanishedProducerFlushesItsClaimsAndCreditsThePaidPart()
		{
			var r = new UnitSpendReservation();
			var warFactory = new object();
			var airfield = new object();
			r.SetStanding(true, true, true, 25, 8000);
			r.ReconcileUnits(warFactory, Units(("tank", 900, 900), ("tank", 900, 300)));
			r.ReconcileUnits(airfield, Units(("heli", 1200, 600)));

			// The airfield dies: it never reconciles again — the flush settles it.
			r.FlushUnseenUnits(new HashSet<object> { warFactory });
			Assert.That(r.PaidUnitCost, Is.EqualTo(600));
			Assert.That(r.UnitOutstanding, Is.EqualTo(1200),
				"only the living producer's remainder holds");
			Assert.That(r.UnitItemsInFlight, Is.EqualTo(2));

			// And the flush is idempotent for sibling managers sharing the ledger.
			r.FlushUnseenUnits(new HashSet<object> { warFactory });
			Assert.That(r.PaidUnitCost, Is.EqualTo(600));
			Assert.That(r.UnitItemsInFlight, Is.EqualTo(2));
		}

		[Test]
		public void ADeadConyardSettlesEssentialAndOptionalSeparately()
		{
			var r = new UnitSpendReservation();
			var conyard = new object();
			var defense = new object();
			r.ReconcileBuildings(conyard, Buildings(("refinery", 2000, 1000, true)));
			r.ReconcileBuildings(defense, Buildings(("turret", 800, 400, false)));

			// The conyard is destroyed; the defence producer still reports next sweep.
			r.FlushUnseenBuildings(new HashSet<object> { defense });
			Assert.That(r.PaidEssentialCost, Is.EqualTo(1000));
			Assert.That(r.PaidOptionalCost, Is.EqualTo(0),
				"the live producer's optional item keeps flying");
			Assert.That(r.BuildingItemsInFlight, Is.EqualTo(1));
		}
	}
}
