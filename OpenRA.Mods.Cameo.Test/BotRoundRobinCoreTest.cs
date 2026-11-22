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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// STAGGER_DESIGN_06 T0: the world-free scheduling core (sections 5, 6.2, 7.3, 7.5, 8).
	[TestFixture]
	public sealed class BotRoundRobinCoreTest
	{
		sealed class Fixture
		{
			public readonly HashSet<string> Dead = [];
			public readonly BotRoundRobinCore Core;
			public readonly List<BotSeat> Seats = [];

			public Fixture()
			{
				Core = new BotRoundRobinCore(s => !Dead.Contains(s.Name));
			}

			public BotSeat Add(string name, int spawn, int index, bool gated = true, int r = 0)
			{
				var seat = Core.Register(name, spawn, index, gated, null, r);
				Seats.Add(seat);
				return seat;
			}

			public static Fixture WithSeats(int n)
			{
				var f = new Fixture();
				for (var i = 0; i < n; i++)
					f.Add("p" + i, i + 1, i);

				return f;
			}
		}

		static BeaconEvent Ev(string kind) => new() { BeaconKind = kind, SourceSeat = "x" };

		[Test]
		public void RosterFollowsSpawnIndexThenPlayerIndexWithUnspawnedLast()
		{
			var f = new Fixture();
			f.Add("a", 3, 0);
			f.Add("b", 1, 1);
			f.Add("c", BotRoundRobinCore.NoSpawnKey, 3);
			f.Add("d", BotRoundRobinCore.NoSpawnKey, 2);

			f.Core.Advance(0);
			Assert.That(f.Core.Roster.Select(s => s.Name), Is.EqualTo(new[] { "b", "a", "d", "c" }));
			for (var t = 0; t < 4; t++)
				Assert.That(f.Core.SlotOwner(t).Name, Is.EqualTo(new[] { "b", "a", "d", "c" }[t]));
		}

		[Test]
		public void EveryBotGetsExactlyOneConsumedVisitPerCycleForNOneToTwelve()
		{
			for (var n = 1; n <= 12; n++)
			{
				var f = Fixture.WithSeats(n);
				for (var t = 0; t < 3 * n; t++)
				{
					var owner = f.Core.SlotOwner(t);
					Assert.That(owner.Name, Is.EqualTo("p" + (t % n)), $"N={n} t={t}");
					foreach (var s in f.Seats)
					{
						var expected = s == owner;
						Assert.That(f.Core.BeginVisit(s, t), Is.EqualTo(expected), $"N={n} t={t} {s.Name}");
						Assert.That(f.Core.BeginVisit(s, t), Is.False, "consumed-visit guard");
					}
				}

				Assert.That(f.Seats.Select(s => s.Visits), Is.All.EqualTo(3), $"N={n}");
			}
		}

		[Test]
		public void RosterRepackedFiresAtEveryBoundaryWithTheSlotOrderedRoster()
		{
			var f = Fixture.WithSeats(3);
			var seen = new List<string[]>();
			f.Core.RosterRepacked += roster => seen.Add(roster.Select(s => s.Name).ToArray());

			for (var t = 0; t < 7; t++)
			{
				if (t == 4)
					f.Dead.Add("p1");

				f.Core.Advance(t);
			}

			// Boundaries at t=0, 3 and 6; p1 is gone from the t=6 roster (it died inside the second cycle).
			Assert.That(seen.Count, Is.EqualTo(3));
			Assert.That(seen[0], Is.EqualTo(new[] { "p0", "p1", "p2" }));
			Assert.That(seen[1], Is.EqualTo(new[] { "p0", "p1", "p2" }));
			Assert.That(seen[2], Is.EqualTo(new[] { "p0", "p2" }));
		}

		[Test]
		public void SingleBotIsVisitedEveryTick()
		{
			var f = Fixture.WithSeats(1);
			for (var t = 0; t < 10; t++)
				Assert.That(f.Core.BeginVisit(f.Seats[0], t), Is.True);
		}

		[Test]
		public void DefeatMidCycleLeavesAnEmptySlotAndRepacksAtTheNextBoundary()
		{
			var f = Fixture.WithSeats(4);
			Assert.That(f.Core.BeginVisit(f.Seats[0], 0), Is.True);
			Assert.That(f.Core.BeginVisit(f.Seats[1], 1), Is.True);
			f.Dead.Add("p2");

			// t=2 is p2's slot: empty, and the cycle is not shortened.
			Assert.That(f.Core.IsTurn(f.Seats[2], 2), Is.False);
			Assert.That(f.Core.BeginVisit(f.Seats[2], 2), Is.False);
			Assert.That(f.Core.BeginVisit(f.Seats[3], 3), Is.True);
			Assert.That(f.Core.EmptySlots, Is.EqualTo(1));

			// t=4 is the boundary: the roster re-packs to three bots in spawn order.
			Assert.That(f.Core.BeginVisit(f.Seats[0], 4), Is.True);
			Assert.That(f.Core.Roster.Select(s => s.Name), Is.EqualTo(new[] { "p0", "p1", "p3" }));
			Assert.That(f.Core.BeginVisit(f.Seats[1], 5), Is.True);
			Assert.That(f.Core.BeginVisit(f.Seats[3], 6), Is.True);
			Assert.That(f.Seats.Select(s => s.Visits), Is.EqualTo(new[] { 2, 2, 0, 2 }));
		}

		[Test]
		public void LateRegistrationJoinsAtTheNextBoundaryAndNeverEarlier()
		{
			var f = Fixture.WithSeats(2);
			f.Core.Advance(0);
			var late = f.Add("late", 3, 2, r: 1);

			Assert.That(f.Core.IsTurn(late, 1), Is.False);
			Assert.That(f.Core.BeginVisit(f.Seats[1], 1), Is.True);

			// Boundary at t=2: roster of three, slot 2 belongs to "late" at t=4.
			Assert.That(f.Core.BeginVisit(f.Seats[0], 2), Is.True);
			Assert.That(f.Core.Roster.Count, Is.EqualTo(3));
			Assert.That(f.Core.BeginVisit(f.Seats[1], 3), Is.True);
			Assert.That(f.Core.BeginVisit(late, 4), Is.True);
			Assert.That(late.Elapsed, Is.EqualTo(3), "clock baseline is the registration tick");
		}

		[Test]
		public void ElapsedIsTheTrueTimeSincePreviousVisit()
		{
			var f = Fixture.WithSeats(3);
			var s = f.Seats[0];
			Assert.That(f.Core.BeginVisit(s, 0), Is.True);
			f.Core.EndVisit(s);
			f.Core.BeginVisit(f.Seats[1], 1);
			f.Core.BeginVisit(f.Seats[2], 2);
			Assert.That(f.Core.BeginVisit(s, 3), Is.True);
			Assert.That(s.Elapsed, Is.EqualTo(3));
		}

		[Test]
		public void ClockOfAnUngatedSeatIsTheWorldClock()
		{
			var f = new Fixture();
			var control = f.Add("control", 1, 0, gated: false);
			f.Add("treatment", 2, 1);
			Assert.That(f.Core.ClockOf(control, 41), Is.EqualTo((41, 1, true)));
			Assert.That(f.Core.ClockOf(null, 7), Is.EqualTo((7, 1, true)));
		}

		[Test]
		public void ClockOfAGatedSeatIsInVisitOnlyBetweenBeginAndEnd()
		{
			var f = Fixture.WithSeats(2);
			var s = f.Seats[0];
			Assert.That(f.Core.ClockOf(s, 0).InVisit, Is.False);
			f.Core.BeginVisit(s, 0);
			Assert.That(f.Core.ClockOf(s, 0).InVisit, Is.True);
			f.Core.EndVisit(s);
			Assert.That(f.Core.ClockOf(s, 0).InVisit, Is.False);
			Assert.That(f.Core.PhaseAt(s, 1), Is.EqualTo(DispatchPhase.Off));
		}

		[Test]
		public void PostToAnUngatedOrUnknownSeatDeliversNow()
		{
			var f = new Fixture();
			var control = f.Add("control", 1, 0, gated: false);
			Assert.That(f.Core.Post(control, Ev("a"), 5), Is.EqualTo(PostOutcome.DeliverNow));
			Assert.That(f.Core.Post(null, Ev("a"), 5), Is.EqualTo(PostOutcome.DeliverNow));
			Assert.That(f.Core.EventsQueued, Is.EqualTo(0));
		}

		[Test]
		public void PostOffSlotQueuesAndTheNextVisitDeliversInFifoOrder()
		{
			var f = Fixture.WithSeats(3);
			var s = f.Seats[2];
			Assert.That(f.Core.Post(s, Ev("a"), 0), Is.EqualTo(PostOutcome.Queued));
			Assert.That(f.Core.Post(s, Ev("b"), 1), Is.EqualTo(PostOutcome.Queued));
			Assert.That(f.Core.BeginVisit(s, 2), Is.True);

			var kinds = new List<string>();
			while (f.Core.TryDequeue(s, out var ev))
				kinds.Add(((BeaconEvent)ev).BeaconKind);

			Assert.That(kinds, Is.EqualTo(new[] { "a", "b" }));
			Assert.That(f.Core.ConservationHolds(), Is.True);
		}

		[Test]
		public void EventsGetAscendingSequenceNumbersAndTheArrivalTick()
		{
			var f = Fixture.WithSeats(2);
			var a = Ev("a");
			var b = Ev("b");
			f.Core.Post(f.Seats[1], a, 0);
			f.Core.Post(f.Seats[0], b, 1);
			Assert.That(a.Seq, Is.EqualTo(0));
			Assert.That(b.Seq, Is.EqualTo(1));
			Assert.That(a.ArrivalTick, Is.EqualTo(0));
			Assert.That(b.ArrivalTick, Is.EqualTo(1));
			Assert.That(f.Core.NextSeq, Is.EqualTo(2));
		}

		[Test]
		public void PostDuringDrainAppendsToTheTailSoNestingGivesABC()
		{
			var f = Fixture.WithSeats(2);
			var s = f.Seats[0];
			f.Core.Post(s, Ev("A"), 0);
			f.Core.Post(s, Ev("B"), 0);
			Assert.That(f.Core.BeginVisit(s, 0), Is.True);

			var order = new List<string>();
			while (f.Core.TryDequeue(s, out var ev))
			{
				var kind = ((BeaconEvent)ev).BeaconKind;
				order.Add(kind);
				if (kind == "A")
					Assert.That(f.Core.Post(s, Ev("C"), 0), Is.EqualTo(PostOutcome.Queued));
			}

			Assert.That(order, Is.EqualTo(new[] { "A", "B", "C" }));
		}

		[Test]
		public void PostWhileRunningDeliversNow()
		{
			var f = Fixture.WithSeats(2);
			var s = f.Seats[0];
			f.Core.BeginVisit(s, 0);
			f.Core.BeginRunning(s);
			Assert.That(s.Fifo.Count, Is.EqualTo(0));
			Assert.That(f.Core.Post(s, Ev("a"), 0), Is.EqualTo(PostOutcome.DeliverNow));
			Assert.That(s.Fifo.Count, Is.EqualTo(0));
		}

		[Test]
		public void PostAfterTheVisitOnTheSlotTickGoesToTheNextVisit()
		{
			var f = Fixture.WithSeats(2);
			var s = f.Seats[0];
			f.Core.BeginVisit(s, 0);
			f.Core.BeginRunning(s);
			f.Core.EndVisit(s);

			Assert.That(f.Core.Post(s, Ev("late"), 0), Is.EqualTo(PostOutcome.Queued));
			Assert.That(f.Core.BeginVisit(s, 0), Is.False, "no second visit on the same tick");
			f.Core.BeginVisit(f.Seats[1], 1);
			Assert.That(f.Core.BeginVisit(s, 2), Is.True);
			Assert.That(f.Core.TryDequeue(s, out _), Is.True);
		}

		[Test]
		public void SingleBotBaselineDeliversNowWhenTheFifoIsEmpty()
		{
			var f = Fixture.WithSeats(1);
			var s = f.Seats[0];
			Assert.That(f.Core.Post(s, Ev("a"), 3), Is.EqualTo(PostOutcome.DeliverNow));
			Assert.That(f.Core.EventsQueued, Is.EqualTo(0));
		}

		[Test]
		public void SingleBotWithAPendingEventKeepsFifoOrderUntilTheDrainCompletes()
		{
			var f = Fixture.WithSeats(2);
			var s = f.Seats[0];
			f.Core.Post(s, Ev("old"), 0);

			// The other bot is defeated: N becomes one at the next boundary.
			f.Dead.Add("p1");
			Assert.That(f.Core.BeginVisit(s, 2), Is.True);
			Assert.That(f.Core.Roster.Count, Is.EqualTo(1));
			Assert.That(f.Core.Post(s, Ev("new"), 2), Is.EqualTo(PostOutcome.Queued));
			var kinds = new List<string>();
			while (f.Core.TryDequeue(s, out var ev))
				kinds.Add(((BeaconEvent)ev).BeaconKind);

			Assert.That(kinds, Is.EqualTo(new[] { "old", "new" }));
			f.Core.BeginRunning(s);
			f.Core.EndVisit(s);
			Assert.That(f.Core.Post(s, Ev("after"), 2), Is.EqualTo(PostOutcome.DeliverNow));
		}

		[Test]
		public void PostToADefeatedBotIsDiscardedAndCounted()
		{
			var f = Fixture.WithSeats(2);
			f.Dead.Add("p0");
			Assert.That(f.Core.Post(f.Seats[0], Ev("a"), 0), Is.EqualTo(PostOutcome.DiscardedNotLive));
			Assert.That(f.Core.DiscardedCase1, Is.EqualTo(1));
			Assert.That(f.Core.ConservationHolds(), Is.True);
		}

		[Test]
		public void PendingEventsOfADefeatedBotAreDiscardedAtTheBoundaryAndCounted()
		{
			var f = Fixture.WithSeats(3);
			f.Core.Advance(0);
			f.Core.Post(f.Seats[2], Ev("a"), 0);
			f.Core.Post(f.Seats[2], Ev("b"), 0);
			f.Dead.Add("p2");

			f.Core.Advance(1);
			f.Core.Advance(2);
			Assert.That(f.Seats[2].Fifo.Count, Is.EqualTo(2), "still inside the cycle: nothing discarded yet");
			f.Core.Advance(3);
			Assert.That(f.Seats[2].Fifo.Count, Is.EqualTo(0));
			Assert.That(f.Core.DiscardedCase1, Is.EqualTo(2));
			Assert.That(f.Core.ConservationHolds(), Is.True);
		}

		[Test]
		public void ReRegisteringTheSameSeatDiscardsItsPendingEventsAndKeepsItsSlot()
		{
			var f = Fixture.WithSeats(3);
			f.Core.Advance(0);
			var seat = f.Seats[1];
			f.Core.Post(seat, Ev("a"), 0);

			var again = f.Core.Register("p1", 2, 1, true, null, 2);
			Assert.That(again, Is.SameAs(seat));
			Assert.That(seat.Fifo.Count, Is.EqualTo(0));
			Assert.That(f.Core.DiscardedCase1, Is.EqualTo(1));
			Assert.That(f.Core.Registered.Count, Is.EqualTo(3));
			Assert.That(f.Core.ConservationHolds(), Is.True);
		}

		[Test]
		public void NoGatedSeatStillKeepsTheRosterAndNeverQueues()
		{
			var f = new Fixture();
			f.Add("c1", 1, 0, gated: false);
			f.Add("c2", 2, 1, gated: false);
			for (var t = 0; t < 6; t++)
			{
				f.Core.Advance(t);
				Assert.That(f.Core.Post(f.Seats[0], Ev("a"), t), Is.EqualTo(PostOutcome.DeliverNow));
			}

			Assert.That(f.Core.Roster.Count, Is.EqualTo(2));
			Assert.That(f.Core.Pending, Is.EqualTo(0));
		}

		[Test]
		public void RestoreDispatchRefusesAnUnknownSeatByName()
		{
			var f = Fixture.WithSeats(2);
			Assert.That(f.Core.RestoreDispatch(4, 2, 9, new[] { "p0", "ghost" }), Is.EqualTo("ghost"));
			Assert.That(f.Core.RestoreDispatch(4, 2, 9, new[] { "p1", "p0" }), Is.Null);
			Assert.That(f.Core.Roster.Select(s => s.Name), Is.EqualTo(new[] { "p1", "p0" }));
			Assert.That(f.Core.CycleStart, Is.EqualTo(4));
			Assert.That(f.Core.NextSeq, Is.EqualTo(9));
			Assert.That(f.Core.SlotOwner(4).Name, Is.EqualTo("p1"));
			Assert.That(f.Core.SlotOwner(5).Name, Is.EqualTo("p0"));
		}

		[Test]
		public void RestoredCycleContinuesTheSameOrderAsTheContinuousGame()
		{
			var continuous = Fixture.WithSeats(4);
			var order = new List<string>();
			for (var t = 0; t < 12; t++)
				order.Add(continuous.Core.SlotOwner(t).Name);

			var loaded = Fixture.WithSeats(4);
			var ok = loaded.Core.RestoreDispatch(4, 2, 0, new[] { "p0", "p1", "p2", "p3" });
			Assert.That(ok, Is.Null);
			for (var t = 5; t < 12; t++)
				Assert.That(loaded.Core.SlotOwner(t).Name, Is.EqualTo(order[t]), $"t={t}");
		}

		[Test]
		public void ResetCycleStartsANewCycleAtTheNextTick()
		{
			var f = Fixture.WithSeats(3);
			f.Core.Advance(0);
			f.Core.ResetCycle();
			Assert.That(f.Core.SlotOwner(7).Name, Is.EqualTo("p0"));
			Assert.That(f.Core.CycleStart, Is.EqualTo(7));
		}

		// 7.1: the event hierarchy is closed and every kind round-trips through its saved form; none carries a delegate.
		static IEnumerable<Type> EventTypes() =>
			typeof(BotEvent).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(BotEvent)));

		[Test]
		public void EveryEventKindRoundTripsThroughItsSavedForm()
		{
			var types = EventTypes().ToList();
			Assert.That(types.Count, Is.GreaterThanOrEqualTo(6));

			foreach (var type in types)
			{
				var ev = (BotEvent)Activator.CreateInstance(type);
				ev.Seq = 41;
				ev.ArrivalTick = 1234;
				ev.Recipient = new ModuleKey("Multi0", "HarvesterBotModule@0");

				var node = ev.Save();
				var back = BotEvent.Load(node.Value);
				Assert.That(back, Is.Not.Null, type.Name);
				Assert.That(back.GetType(), Is.EqualTo(type), type.Name);
				Assert.That(back.Kind, Is.EqualTo(ev.Kind));
				Assert.That(back.Seq, Is.EqualTo(41));
				Assert.That(back.ArrivalTick, Is.EqualTo(1234));
				Assert.That(back.Recipient, Is.EqualTo(ev.Recipient));

				var again = back.Save();
				Assert.That(again.Value.Nodes.Select(n => n.Key + "=" + n.Value.Value),
					Is.EqualTo(node.Value.Nodes.Select(n => n.Key + "=" + n.Value.Value)), type.Name);
			}
		}

		[Test]
		public void AnUnknownEventKindIsNotLoadable()
		{
			var node = new BeaconEvent().Save();
			Assert.That(BotEvent.Load(node.Value.WithValue("ActionEvent")), Is.Null);
		}

		[Test]
		public void NoEventKindCarriesADelegate()
		{
			foreach (var type in EventTypes())
			{
				var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
					| System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
				foreach (var field in fields)
					Assert.That(typeof(Delegate).IsAssignableFrom(field.FieldType), Is.False, $"{type.Name}.{field.Name}");
			}
		}

		[Test]
		public void ModuleKeyParseInvertsToString()
		{
			var key = new ModuleKey("Multi3", "SquadManagerBotModuleCA@0");
			Assert.That(ModuleKey.Parse(key.ToString()), Is.EqualTo(key));
			Assert.That(ModuleKey.Parse("Multi3"), Is.EqualTo(new ModuleKey("Multi3", "")));
		}

		// HOTFIX-RR-01: AttackEvent, LeaseLostEvent and OrderDispositionEvent are queued with an unset (default) Recipient.
		// ModularBot.Deliver reads Recipient.Module.Length at the drain (ModularBot.cs:753); a null Module threw an NRE at
		// tick 8 of every RR-on game (m6 RRa, exception-2026-11-22T000558Z.log).
		[Test]
		public void HotfixRr01AnUnsetRecipientDrainsAsTheBotItself()
		{
			var f = Fixture.WithSeats(3);
			var s = f.Seats[2];
			var queued = new BotEvent[] { AttackEvent.From(null, null), new LeaseLostEvent(), new OrderDispositionEvent() };
			foreach (var ev in queued)
				Assert.That(f.Core.Post(s, ev, 0), Is.EqualTo(PostOutcome.Queued));

			Assert.That(f.Core.BeginVisit(s, 2), Is.True);
			var drained = 0;
			while (f.Core.TryDequeue(s, out var ev))
			{
				Assert.That(ev.Recipient.Module, Is.Not.Null, ev.Kind);
				Assert.That(ev.Recipient.Module.Length, Is.EqualTo(0), ev.Kind);
				Assert.That(ev.Recipient.Seat, Is.EqualTo(""), ev.Kind);
				drained++;
			}

			Assert.That(drained, Is.EqualTo(queued.Length));
			Assert.That(f.Core.ConservationHolds(), Is.True);
		}

		[Test]
		public void HotfixRr01TheDefaultKeyIsTheEmptyKey()
		{
			var unset = default(ModuleKey);
			Assert.That(unset.Seat, Is.EqualTo(""));
			Assert.That(unset.Module, Is.EqualTo(""));
			Assert.That(unset, Is.EqualTo(new ModuleKey("", "")));
			Assert.That(unset, Is.EqualTo(new ModuleKey(null, null)));
			Assert.That(unset.GetHashCode(), Is.EqualTo(new ModuleKey("", "").GetHashCode()));
			Assert.That(unset.ToString(), Is.EqualTo("|"));
			Assert.That(new Dictionary<string, int> { [""] = 1 }.TryGetValue(unset.Module, out _), Is.True);
		}

		[Test]
		public void HotfixRr01EveryEventKindRoundTripsAnUnsetRecipient()
		{
			foreach (var type in EventTypes())
			{
				var ev = (BotEvent)Activator.CreateInstance(type);
				var node = ev.Save();
				Assert.That(node.Value.Nodes.First(n => n.Key == "Recipient").Value.Value, Is.EqualTo("|"), type.Name);

				var back = BotEvent.Load(node.Value);
				Assert.That(back, Is.Not.Null, type.Name);
				Assert.That(back.Recipient, Is.EqualTo(ev.Recipient), type.Name);
				Assert.That(back.Recipient.Module, Is.EqualTo(""), type.Name);
				Assert.That(back.Save().Value.Nodes.Select(n => n.Key + "=" + n.Value.Value),
					Is.EqualTo(node.Value.Nodes.Select(n => n.Key + "=" + n.Value.Value)), type.Name);
			}
		}
	}
}
