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

namespace OpenRA.Mods.Cameo.Traits
{
	// STAGGER_DESIGN_06 sections 4-6 and 8: the world-free scheduling core. Everything here is a pure
	// function of synced integers (world tick, spawn key, player index, win state through the liveness
	// callback, sequence numbers). No engine type, no clock, no hash-ordered collection.
	public enum DispatchPhase { Off, Draining, Running, Done }

	public enum PostOutcome { DeliverNow, Queued, DiscardedNotLive }

	public sealed class BotSeat
	{
		// Seat identity is Player.InternalName, never ClientIndex (every map-side bot shares the host's).
		public readonly string Name;
		public readonly int SpawnKey;
		public int PlayerIndex;

		// Opaque binding owned by the host trait (the player and the bot instance).
		public object Bot;
		public bool Gated;
		public DispatchPhase Phase;
		public int VisitTick = -1;
		public int LastVisitTick;
		public int Elapsed = 1;
		public long TrackingSerial;
		public int Visits;
		public readonly Queue<BotEvent> Fifo = new();

		public BotSeat(string name, int spawnKey, int playerIndex)
		{
			Name = name;
			SpawnKey = spawnKey;
			PlayerIndex = playerIndex;
		}
	}

	public sealed class BotRoundRobinCore
	{
		public const int NoSpawnKey = int.MaxValue;

		readonly List<BotSeat> registered = [];
		readonly Func<BotSeat, bool> isLive;
		readonly Func<BotSeat, int> playerIndexOf;
		BotSeat[] roster = [];
		int cycleStart = -1;
		int advancedTick = -1;
		int epoch;
		long nextSeq;

		// DEV telemetry counters (section 13). They never feed decisions.
		public long EventsQueued { get; private set; }
		public long EventsDelivered { get; private set; }
		public long DiscardedCase1 { get; private set; }
		public long DiscardedCase2 { get; private set; }
		public int MaxFifoDepth { get; private set; }
		public long EmptySlots { get; private set; }

		public BotRoundRobinCore(Func<BotSeat, bool> isLive, Func<BotSeat, int> playerIndexOf = null)
		{
			this.isLive = isLive;
			this.playerIndexOf = playerIndexOf ?? (s => s.PlayerIndex);
		}

		public IReadOnlyList<BotSeat> Roster => roster;
		public IReadOnlyList<BotSeat> Registered => registered;
		public int CycleStart => cycleStart;
		public int Epoch => epoch;
		public long NextSeq => nextSeq;

		public int Pending => registered.Sum(s => s.Fifo.Count);

		public BotSeat Find(string name) => registered.Find(s => s.Name == name);

		// 5.4: the same seat re-activated keeps its registration position and its spawn slot, but its events
		// were addressed to the old bot instance (D-3 case 1) and its clock baseline restarts.
		public BotSeat Register(string name, int spawnKey, int playerIndex, bool gated, object bot, int r)
		{
			var seat = Find(name);
			if (seat == null)
			{
				seat = new BotSeat(name, spawnKey, playerIndex) { Bot = bot, Gated = gated, LastVisitTick = r };
				registered.Add(seat);
				return seat;
			}

			DiscardPending(seat);
			seat.Bot = bot;
			seat.Gated = gated;
			seat.LastVisitTick = r;
			seat.VisitTick = -1;
			seat.Phase = DispatchPhase.Off;
			seat.PlayerIndex = playerIndex;
			return seat;
		}

		// 5.1: idempotent; from the scheduler tick and lazily from every query.
		public void Advance(int t)
		{
			if (t == advancedTick)
				return;

			advancedTick = t;
			if (cycleStart < 0 || t - cycleStart >= roster.Length)
			{
				// A seat that is no longer live can never be visited again: its pending events are D-3 case 1.
				foreach (var s in registered)
					if (s.Fifo.Count > 0 && !isLive(s))
						DiscardPending(s);

				foreach (var s in registered)
					s.PlayerIndex = playerIndexOf(s);

				roster = registered.Where(isLive).OrderBy(s => s.SpawnKey).ThenBy(s => s.PlayerIndex).ToArray();
				cycleStart = t;
				epoch++;
			}

			if (roster.Length > 0 && !isLive(roster[t - cycleStart]))
				EmptySlots++;
		}

		public BotSeat SlotOwner(int t)
		{
			Advance(t);
			return roster.Length == 0 ? null : roster[t - cycleStart];
		}

		// 5.2: pure query, any number of calls, no side effect beyond the idempotent Advance.
		public bool IsTurn(BotSeat seat, int t) => SlotOwner(t) == seat && isLive(seat);

		// 5.2: consumes the visit exactly once per (seat, tick).
		public bool BeginVisit(BotSeat seat, int t)
		{
			if (!IsTurn(seat, t) || seat.VisitTick == t)
				return false;

			seat.VisitTick = t;
			seat.Elapsed = t - seat.LastVisitTick;
			seat.LastVisitTick = t;
			seat.Phase = DispatchPhase.Draining;
			seat.Visits++;
			return true;
		}

		public void BeginRunning(BotSeat seat) => seat.Phase = DispatchPhase.Running;

		public void EndVisit(BotSeat seat) => seat.Phase = DispatchPhase.Done;

		// 6.1: Off is lazy: a seat whose last consumed visit is not this tick is Off.
		public DispatchPhase PhaseAt(BotSeat seat, int t) => seat.VisitTick == t ? seat.Phase : DispatchPhase.Off;

		// 8: the bot clock. An ungated bot (or one the scheduler never heard of) sees {WorldTick, 1, true}.
		public (int VisitTick, int Elapsed, bool InVisit) ClockOf(BotSeat seat, int t)
		{
			if (seat == null || !seat.Gated)
				return (t, 1, true);

			var inVisit = seat.VisitTick == t && seat.Phase != DispatchPhase.Done;
			return (seat.VisitTick, seat.Elapsed, inVisit);
		}

		// 6.2: the single, exhaustive Post rule. The event is created only when it is queued, so a delivery that
		// happens now (the N = 1 baseline and the Running nesting) allocates nothing.
		public PostOutcome Post(BotSeat seat, BotEvent ev, int t) => Post(seat, () => ev, t);

		public PostOutcome Post(BotSeat seat, Func<BotEvent> make, int t)
		{
			if (seat == null || !seat.Gated || !registered.Contains(seat))
				return PostOutcome.DeliverNow;

			Advance(t);
			if (!isLive(seat))
			{
				// Counted as an event in and a case-1 discard, so the 7.5 conservation invariant holds.
				EventsQueued++;
				DiscardedCase1++;
				return PostOutcome.DiscardedNotLive;
			}

			switch (PhaseAt(seat, t))
			{
				case DispatchPhase.Draining:
					Enqueue(seat, make(), t);
					return PostOutcome.Queued;

				case DispatchPhase.Running:
					// The FIFO is provably empty in Running (entered only with an empty FIFO; Running never appends).
					return PostOutcome.DeliverNow;

				default:
					if (roster.Length == 1 && roster[0] == seat && seat.Fifo.Count == 0)
						return PostOutcome.DeliverNow;

					Enqueue(seat, make(), t);
					return PostOutcome.Queued;
			}
		}

		void Enqueue(BotSeat seat, BotEvent ev, int t)
		{
			ev.Seq = nextSeq++;
			ev.ArrivalTick = t;
			seat.Fifo.Enqueue(ev);
			EventsQueued++;
			if (seat.Fifo.Count > MaxFifoDepth)
				MaxFifoDepth = seat.Fifo.Count;
		}

		// Strict FIFO. An append during delivery goes to the tail and is delivered later in the same drain.
		public bool TryDequeue(BotSeat seat, out BotEvent ev)
		{
			if (seat.Fifo.Count == 0)
			{
				ev = null;
				return false;
			}

			ev = seat.Fifo.Dequeue();
			EventsDelivered++;
			return true;
		}

		public void DiscardPending(BotSeat seat)
		{
			DiscardedCase1 += seat.Fifo.Count;
			seat.Fifo.Clear();
		}

		// D-3 case 2 (an actor ID that does not resolve after a load) is counted by the host that resolves IDs.
		public void NoteCase2Discard() => DiscardedCase2++;

		// 11.3/11.2: restore the dispatch position. An unknown seat name is a refused load (returns the name).
		public string RestoreDispatch(int savedCycleStart, int savedEpoch, long savedNextSeq, IReadOnlyList<string> rosterNames)
		{
			var restored = new List<BotSeat>();
			foreach (var name in rosterNames)
			{
				var seat = Find(name);
				if (seat == null)
					return name;

				restored.Add(seat);
			}

			roster = restored.ToArray();
			cycleStart = savedCycleStart;
			epoch = savedEpoch;
			nextSeq = savedNextSeq;
			advancedTick = -1;
			return null;
		}

		// A save from before the scheduler: start a new cycle at the first post-load tick.
		public void ResetCycle()
		{
			roster = [];
			cycleStart = -1;
			advancedTick = -1;
		}

		public void RestoreCounters(long queued, long delivered, long case1, long case2)
		{
			EventsQueued = queued;
			EventsDelivered = delivered;
			DiscardedCase1 = case1;
			DiscardedCase2 = case2;
		}

		// The invariant of 7.5: events in = delivered + discarded (both cases) + pending.
		public bool ConservationHolds() =>
			EventsQueued == EventsDelivered + DiscardedCase1 + DiscardedCase2 + Pending;
	}
}
