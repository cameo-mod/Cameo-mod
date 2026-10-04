#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Test.TestFixtures;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// AR-9 review (P3): LoadGarrisoner's lease lifecycle seams — per-tick release of gone/idle
	// marchers, per-scan renewal, Stop-before-release, disable teardown and heartbeat lapse.
	// The seams are public statics taking IBotUnitLeases/Predicate<Actor> so a fake table and
	// identity-only Actors pin the contract without a World.
	[TestFixture]
	public sealed class LoadGarrisonerLeaseTest
	{
		// One shared log for order-queue and lease-release calls, so the test sees the
		// sequence between them, not just that both happened.
		sealed class EventLeases : IBotUnitLeases
		{
			public bool Allow = true;
			public readonly List<string> Log;
			public int ClaimCalls;
			public int ReleaseCount;

			public EventLeases(List<string> log) { Log = log; }

			public bool TryClaim(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks)
			{
				ClaimCalls++;
				return Allow;
			}

			public void Release(Actor actor, string owner)
			{
				ReleaseCount++;
				Log.Add("release");
			}
			public bool IsClaimedByOther(Actor actor, string owner) => false;
			public BotLease? LeaseOf(Actor actor) => null;
			public bool Preempt(Actor actor, string owner, BotLeasePurpose purpose, int durationTicks) => Allow;
			public bool Transfer(Actor actor, string newOwner, BotLeasePurpose purpose, int durationTicks) => Allow;
		}

		sealed class LogBot : IBot
		{
			readonly List<string> log;
			public readonly List<Order> Queued = new();
			public LogBot(List<string> l) { log = l; }
			public Player Player { get; private set; }
			public IBotInfo Info { get; set; }
			public void Activate(Player p) { Player = p; }
			public void QueueOrder(Order order)
			{
				Queued.Add(order);
				log.Add("queue:" + order.OrderString);
			}
		}

		static List<UnitWposWrapper> Marchers(params Actor[] actors)
		{
			var list = new List<UnitWposWrapper>();
			foreach (var a in actors)
				list.Add(new UnitWposWrapper(a));
			return list;
		}

		[Test]
		public void TheStopIsQueuedBeforeTheClaimIsReleased()
		{
			var log = new List<string>();
			var bot = new LogBot(log);
			var leases = new EventLeases(log);
			var unit = Uninitialized.Actor();

			LoadGarrisonerBotModuleCA.StopAndRelease(bot, leases, unit, "loader");

			Assert.That(log, Is.EqualTo(new[] { "queue:Stop", "release" }),
				"GC-1 order-before-release: the Stop must precede the release call");
			Assert.That(bot.Queued[0].OrderString, Is.EqualTo("Stop"));
			Assert.That(bot.Queued[0].Subject, Is.SameAs(unit));
		}

		[Test]
		public void GoneOrIdleMarchersAreReleasedAndDroppedEveryTick()
		{
			var log = new List<string>();
			var leases = new EventLeases(log);
			var gone = Uninitialized.Actor();
			var dead = Uninitialized.Actor();
			var captured = Uninitialized.Actor();
			var stillMarching = Uninitialized.Actor();
			var active = Marchers(gone, dead, captured, stillMarching);

			// The production predicate is unitCannotBeOrderedOrIsIdle (dead, out of world,
			// captured, idle); a stand-in marks the same three cases here.
			Predicate<Actor> goneOrIdle = a => a != stillMarching;
			var released = LoadGarrisonerBotModuleCA.ReleaseGoneOrIdle(active, goneOrIdle, leases, "loader");

			Assert.That(released, Is.EqualTo(3));
			Assert.That(leases.ReleaseCount, Is.EqualTo(3));
			Assert.That(active, Has.Count.EqualTo(1));
			Assert.That(active[0].Actor, Is.SameAs(stillMarching));
		}

		[Test]
		public void ALostRenewalReleasesAndDropsTheUnit()
		{
			var log = new List<string>();
			var unit = Uninitialized.Actor();

			var denied = new EventLeases(log) { Allow = false };
			Assert.That(LoadGarrisonerBotModuleCA.LostRenewal(denied, unit, "loader", 400), Is.True);
			Assert.That(denied.ReleaseCount, Is.EqualTo(1), "a refused renewal ends our stale handle");

			var allowed = new EventLeases(log) { Allow = true };
			Assert.That(LoadGarrisonerBotModuleCA.LostRenewal(allowed, unit, "loader", 400), Is.False);
			Assert.That(allowed.ReleaseCount, Is.EqualTo(0));
		}

		[Test]
		public void ADeniedClaimChargesNoCapacity()
		{
			var log = new List<string>();
			var unit = Uninitialized.Actor();

			var denied = new EventLeases(log) { Allow = false };
			Assert.That(LoadGarrisonerBotModuleCA.ClaimedWeight(denied, unit, 2, "loader", 400), Is.EqualTo(0),
				"a unit another module took mid-scan must not eat garrison capacity");

			var allowed = new EventLeases(log) { Allow = true };
			Assert.That(LoadGarrisonerBotModuleCA.ClaimedWeight(allowed, unit, 2, "loader", 400), Is.EqualTo(2));
		}

		[Test]
		public void TraitDisabledStopsMarchersAndReleasesEverything()
		{
			var log = new List<string>();
			var bot = new LogBot(log);
			var leases = new EventLeases(log);
			var marcher1 = Uninitialized.Actor();
			var marcher2 = Uninitialized.Actor();
			var alreadyGone = Uninitialized.Actor();
			var active = Marchers(marcher1, marcher2, alreadyGone);

			// orderable = unitCannotBeOrdered stand-in: the gone unit releases without a Stop.
			Predicate<Actor> orderable = a => a != alreadyGone;
			LoadGarrisonerBotModuleCA.DisableRelease(bot, leases, active, "loader", orderable);

			Assert.That(log.FindAll(e => e == "queue:Stop"), Has.Count.EqualTo(2));
			Assert.That(log.FindAll(e => e == "release"), Has.Count.EqualTo(3),
				"every claim ends on disable, including the unit that no longer needs a Stop");
			Assert.That(log.IndexOf("queue:Stop"), Is.LessThan(log.IndexOf("release")));
		}

		[Test]
		public void ClassicDisableStopsNothingAndReleasesNothing()
		{
			var log = new List<string>();
			var bot = new LogBot(log);
			var active = Marchers(Uninitialized.Actor(), Uninitialized.Actor());
			Predicate<Actor> orderable = _ => true;

			// leases == null is classicbot: no Stop, no release — the march continues as before.
			LoadGarrisonerBotModuleCA.DisableRelease(bot, null, active, "loader", orderable);

			Assert.That(log, Is.Empty);
			Assert.That(bot.Queued, Is.Empty);
		}

		[Test]
		public void TheRealLeaseTableLapsesAStaleClaim()
		{
			// Heartbeat model on the production table: a holder that stops renewing loses the
			// unit without any release call, the rival claims it, and the stale holder's next
			// renewal is refused — the LostRenewal drop path's table-side truth.
			var table = new BotLeaseTable<Actor>();
			var unit = Uninitialized.Actor();

			Assert.That(table.TryClaim(unit, "loader", BotLeasePurpose.Garrison, 100, 50), Is.True);
			Assert.That(table.IsClaimedByOther(unit, "squad", 149), Is.True);

			Assert.That(table.TryClaim(unit, "squad", BotLeasePurpose.Squad, 160, 400), Is.True,
				"the lapsed claim frees the unit for a rival");

			Assert.That(table.TryClaim(unit, "loader", BotLeasePurpose.Garrison, 457, 1828), Is.False,
				"the stale holder's next renewal is refused — LostRenewal reports it lost");
		}
	}
}
