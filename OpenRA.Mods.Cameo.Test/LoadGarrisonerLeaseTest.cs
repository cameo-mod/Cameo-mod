#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Test.TestFixtures;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// AR-9 review (P3) + re-review (P1): LoadGarrisoner's lease lifecycle on the module's
	// OWN predicates — the seams under test (SweepTick/DisableAll) own the real
	// CannotBeOrdered/IsIdle/CanBeOrdered gates, so an inverted or premature predicate at
	// the callsite fails these tests rather than slipping past stand-ins.
	[TestFixture]
	public sealed class LoadGarrisonerLeaseTest
	{
		const int Grace = 50;
		const int Now = 1000;

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

		static void Set(object o, string field, object v) =>
			typeof(Actor).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(o, v);

		// Identity-only Actor with the fields the module's real predicates read pinned
		// directly: Owner/IsInWorld/Disposed are backing fields, CurrentActivity a field.
		static Actor Unit(Player owner, bool dead = false, bool inWorld = true, bool marching = false)
		{
			var a = Uninitialized.Actor();
			Set(a, "<Owner>k__BackingField", owner);
			Set(a, "<IsInWorld>k__BackingField", inWorld);
			Set(a, "<Disposed>k__BackingField", dead);
			if (marching)
				Set(a, "currentActivity", Uninitialized.Of<Wait>());
			return a;
		}

		static List<UnitWposWrapper> Marchers(params Actor[] actors)
		{
			var list = new List<UnitWposWrapper>();
			foreach (var a in actors)
				list.Add(new UnitWposWrapper(a));
			return list;
		}

		[Test]
		public void TheModulePredicatesSeeAFabricatedUnit()
		{
			// Sanity that the fixtures read through the real gates: a live, in-world,
			// owned unit is orderable; dead/captured/absent ones are not; a unit with no
			// CurrentActivity is idle and a marching one is not.
			var me = Uninitialized.Player();
			var other = Uninitialized.Player();

			Assert.That(LoadGarrisonerBotModuleCA.CanBeOrdered(Unit(me, marching: true), me), Is.True);
			Assert.That(LoadGarrisonerBotModuleCA.CannotBeOrdered(Unit(me, dead: true), me), Is.True);
			Assert.That(LoadGarrisonerBotModuleCA.CannotBeOrdered(Unit(me, inWorld: false), me), Is.True);
			Assert.That(LoadGarrisonerBotModuleCA.CannotBeOrdered(Unit(other), me), Is.True);
			Assert.That(LoadGarrisonerBotModuleCA.CannotBeOrdered(null, me), Is.True);
			Assert.That(LoadGarrisonerBotModuleCA.IsIdle(Unit(me)), Is.True);
			Assert.That(LoadGarrisonerBotModuleCA.IsIdle(Unit(me, marching: true)), Is.False);
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
		public void AFreshClaimKeepsItsLeaseWhileTheOrderIsInFlight()
		{
			// P1 regression: a just-claimed unit is still idle while its orders ride the
			// bot queue -> net frame -> apply path. The sweep must not drop it.
			var log = new List<string>();
			var leases = new EventLeases(log);
			var me = Uninitialized.Player();
			var fresh = Unit(me);                     // owned, alive, in-world, no activity
			var active = Marchers(fresh);
			var pending = new Dictionary<Actor, int> { [fresh] = Now };

			var released = LoadGarrisonerBotModuleCA.SweepTick(active, pending, leases, "loader", me, Now + 5, Grace);

			Assert.That(released, Is.EqualTo(0));
			Assert.That(leases.ReleaseCount, Is.EqualTo(0), "the lease survives order latency");
			Assert.That(active, Has.Count.EqualTo(1));
			Assert.That(pending, Does.ContainKey(fresh), "still waiting for the launch to be seen");
		}

		[Test]
		public void AnIdlePendingUnitIsFreedWhenTheOrderNeverLands()
		{
			var log = new List<string>();
			var leases = new EventLeases(log);
			var me = Uninitialized.Player();
			var stale = Unit(me);
			var active = Marchers(stale);
			var pending = new Dictionary<Actor, int> { [stale] = Now };

			var released = LoadGarrisonerBotModuleCA.SweepTick(active, pending, leases, "loader", me, Now + Grace + 1, Grace);

			Assert.That(released, Is.EqualTo(1), "a launch that never happened gives up after the grace");
			Assert.That(leases.ReleaseCount, Is.EqualTo(1));
			Assert.That(active, Is.Empty);
			Assert.That(pending, Is.Empty);
		}

		[Test]
		public void ALaunchedUnitGoingIdleIsDone()
		{
			// First sweep sees the unit non-idle (order landed) -> pending cleared.
			// Next sweep sees it idle again -> march confirmed over -> released.
			var log = new List<string>();
			var leases = new EventLeases(log);
			var me = Uninitialized.Player();
			var unit = Unit(me, marching: true);
			var active = Marchers(unit);
			var pending = new Dictionary<Actor, int> { [unit] = Now };

			Assert.That(LoadGarrisonerBotModuleCA.SweepTick(active, pending, leases, "loader", me, Now + 5, Grace),
				Is.EqualTo(0));
			Assert.That(pending, Is.Empty, "launch observed — pending bookkeeping cleared");

			Set(unit, "currentActivity", null);   // march finished without boarding
			Assert.That(LoadGarrisonerBotModuleCA.SweepTick(active, pending, leases, "loader", me, Now + 6, Grace),
				Is.EqualTo(1));
			Assert.That(leases.ReleaseCount, Is.EqualTo(1));
			Assert.That(active, Is.Empty);
		}

		[Test]
		public void GoneUnitsDropImmediatelyEvenWithinTheGrace()
		{
			var log = new List<string>();
			var leases = new EventLeases(log);
			var me = Uninitialized.Player();
			var other = Uninitialized.Player();
			var dead = Unit(me, dead: true);
			var captured = Unit(other);
			var gone = Unit(me, inWorld: false);      // boarded
			var active = Marchers(dead, captured, gone);
			var pending = new Dictionary<Actor, int> { [dead] = Now, [captured] = Now, [gone] = Now };

			var released = LoadGarrisonerBotModuleCA.SweepTick(active, pending, leases, "loader", me, Now + 2, Grace);

			Assert.That(released, Is.EqualTo(3), "dead/captured/boarded never wait for launch");
			Assert.That(leases.ReleaseCount, Is.EqualTo(3));
			Assert.That(pending, Is.Empty);
		}

		[Test]
		public void ClassicSweepIsAStrictNoOp()
		{
			// leases == null is classicbot: the per-tick sweep must not touch the tracked
			// list at all — INC-g behaviour is bit-identical.
			var me = Uninitialized.Player();
			var dead = Unit(me, dead: true);
			var idle = Unit(me);
			var active = Marchers(dead, idle);
			var pending = new Dictionary<Actor, int> { [idle] = Now };

			var released = LoadGarrisonerBotModuleCA.SweepTick(active, pending, null, "loader", me, Now + Grace + 1, Grace);

			Assert.That(released, Is.EqualTo(0));
			Assert.That(active, Has.Count.EqualTo(2), "classic leaves tracking untouched per tick");
			Assert.That(pending, Does.ContainKey(idle));
		}

		[Test]
		public void DisableStopsOnlyLiveMarchers()
		{
			// P1 regression: the old callsite passed unitCannotBeOrdered as `orderable` —
			// Stops went to dead units while live marchers were released silently.
			// DisableAll owns the real gate, so the inversion cannot hide.
			var log = new List<string>();
			var bot = new LogBot(log);
			var leases = new EventLeases(log);
			var me = Uninitialized.Player();
			var other = Uninitialized.Player();
			var marching = Unit(me, marching: true);
			var waiting = Unit(me);                    // live + orderable but idle — still ours to stop
			var dead = Unit(me, dead: true);
			var captured = Unit(other);
			var active = Marchers(marching, waiting, dead, captured);

			LoadGarrisonerBotModuleCA.DisableAll(bot, leases, active, "loader", me);

			Assert.That(log.FindAll(e => e == "queue:Stop"), Has.Count.EqualTo(2),
				"Stops go to live orderable units only");
			Assert.That(bot.Queued.FindAll(o => o.Subject == marching), Has.Count.EqualTo(1));
			Assert.That(bot.Queued.FindAll(o => o.Subject == waiting), Has.Count.EqualTo(1));
			Assert.That(bot.Queued.FindAll(o => o.Subject == dead || o.Subject == captured), Is.Empty,
				"dead/captured units must not receive orders");
			Assert.That(leases.ReleaseCount, Is.EqualTo(4), "every claim ends on disable");
		}

		[Test]
		public void ClassicDisableStopsNothingAndReleasesNothing()
		{
			var log = new List<string>();
			var bot = new LogBot(log);
			var me = Uninitialized.Player();
			var active = Marchers(Unit(me, marching: true), Unit(me));

			// leases == null is classicbot: no Stop, no release — the march continues as before.
			LoadGarrisonerBotModuleCA.DisableAll(bot, null, active, "loader", me);

			Assert.That(log, Is.Empty);
			Assert.That(bot.Queued, Is.Empty);
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
