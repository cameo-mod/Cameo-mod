#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// E1 observational slice (lead-accepted 2026-10-17, DESIGN_2026-10-17_e1_bounded_storage_impl_map_devin.md
	// v4.1): records storage-capacity commitments (P = P_queued + P_pending) and emits would-be
	// admission verdicts to the bot debug log. NOTHING here gates, orders, or mutates world state —
	// admissions are unchanged either flag-state and classic/all-off is byte-identical.
	//
	// PERMANENT DOUBLE-COUNT (documented limitation, not a bug to hide): a successfully emitted
	// reservation never settles on queue-item-arrival evidence — no proven order→item correlation
	// seam exists, and actor/name matching can consume a reservation against a foreign same-name
	// item (Sol C4198CC4 + v3-delta). Once the emitted item lands in its producer's queue the same
	// capacity is therefore counted TWICE for the reservation's lifetime — once in P_queued, once
	// in P_pending. This is conservative overcounting: it can overstate would-be refusals but can
	// never affect actual behavior in observational mode. Any future authoritative mode must keep
	// this limitation until a reviewed correlation mechanism exists.
	//
	// Settlement is positive-evidence only: a reservation releases on an exact-instance
	// RefusedLocally disposition (U2 IBotOrderTracking seam — Cameo ModularBot only; under the
	// engine classic/fransbot ModularBot the interface is absent and every reservation stays
	// pending, purely conservative) or on producer death/removal/owner-change at the per-pass
	// reconcile. IssuedToWorld keeps the reservation pending by design (arrival is not settlement).
	//
	// In-memory only: not persisted — the ledger resets on save/load (documented; observational
	// state must not reach save output since it feeds no decisions).
	sealed class StorageAdmissionObservation
	{
		public sealed class Reservation : IBotOrderDisposition
		{
			public readonly StorageAdmissionObservation Ledger;
			public readonly ProductionQueue Queue;   // exact trait instance — sibling queues stay distinct
			public readonly string ItemName;
			public readonly int Capacity;

			public bool Released { get; private set; }
			public bool Added { get; internal set; }

			public Reservation(StorageAdmissionObservation ledger, ProductionQueue queue, string itemName, int capacity)
			{
				Ledger = ledger;
				Queue = queue;
				ItemName = itemName;
				Capacity = capacity;
			}

			public void Release()
			{
				if (Released)
					return;

				Released = true;

				// A refusal may arrive before the caller's NoteEmitted (registration precedes the
				// enqueue — the reservation is only ledgered after the order call returns).
				if (Ledger.pending.Remove(this))
					Ledger.pendingCapacity -= Capacity;
			}

			void IBotOrderDisposition.OrderDisposition(Order order, BotOrderOutcome outcome)
			{
				// Exact-instance local refusal is the only disposition evidence that settles a
				// reservation. IssuedToWorld is routing release, NOT queue arrival — keep pending.
				if (outcome == BotOrderOutcome.RefusedLocally)
					Release();
			}
		}

		readonly Player player;
		readonly List<Reservation> pending = new();
		readonly Dictionary<string, int> capacityOf = new();
		int pendingCapacity;
		int lastStorageEmitTick = -1;

		public StorageAdmissionObservation(Player player)
		{
			this.player = player;
		}

		public int PendingCapacity => pendingCapacity;
		public int PendingCount => pending.Count;

		// Capacity an actor type adds to the owner's resource store. 0 = not a storage building.
		// Resolved from the (frozen at match start) ruleset — static ActorInfo data, no live trait needed.
		public int CapacityOf(World world, string actorName)
		{
			if (capacityOf.TryGetValue(actorName, out var cached))
				return cached;

			var cap = world.Map.Rules.Actors.TryGetValue(actorName, out var ai)
				? ai.TraitInfos<StoresPlayerResourcesInfo>().Sum(t => t.Capacity)
				: 0;

			capacityOf[actorName] = cap;
			return cap;
		}

		// P_queued: committed storage capacity sitting in EVERY owned ProductionQueue trait —
		// enabled, disabled or paused; head, tail and done-but-unplaced items all count (the
		// authoritative enumeration, deliberately NOT BuildingsBeingProduced which is rebuilt
		// each pass, cash-gated and head-only). Queue-type agnostic: committed capacity does
		// not depend on which category produced it.
		public int QueuedCapacity(World world)
		{
			var sum = 0;
			foreach (var tp in world.ActorsWithTrait<ProductionQueue>())
			{
				var actor = tp.Actor;
				if (actor.Owner != player || actor.IsDead || !actor.IsInWorld)
					continue;

				foreach (var item in tp.Trait.AllQueued())
					sum += CapacityOf(world, item.Item);
			}

			return sum;
		}

		// Per-pass reconcile: a dead, removed or captured producer's queued items die with it —
		// exact evidence, so its reservations release. Disabled-but-live producers keep theirs
		// (their items stay committed in the queue). Ambiguity stays counted.
		public void ReconcilePass()
		{
			foreach (var r in pending.ToList())
				if (r.Queue.Actor.IsDead || !r.Queue.Actor.IsInWorld || r.Queue.Actor.Owner != player)
					r.Release();
		}

		public static int ResolveTarget(int baseTarget, int ceiling, int stored, int hysteresis)
		{
			var target = baseTarget <= 0 ? 0 : System.Math.Max(baseTarget, stored + System.Math.Max(0, hysteresis));
			return ceiling > 0 && target > 0 ? System.Math.Min(target, ceiling) : target;
		}

		// Would-be verdicts — pure arithmetic for tests. Target admission is STRICT (<): at the
		// exact target the candidate is refused (Sol C4198CC4). The hard ceiling admits AT the
		// bound (<=): it caps what the committed sum may reach, target-vs-ceiling kept distinct.
		public static bool? VerdictTarget(int committedPlusCandidate, int target)
			=> target > 0 ? committedPlusCandidate < target : null;

		public static bool? VerdictCeiling(int committedPlusCandidate, int ceiling)
			=> ceiling > 0 ? committedPlusCandidate <= ceiling : null;

		// Called BEFORE the emit — the decision context is the committed set before this item
		// joins it. Returns the reservation to bind to the emitted order, or null when the item
		// carries no storage capacity (non-storage emits leave no observation).
		public Reservation ObserveEmitting(World world, PlayerResources resources, ProductionQueue queue,
			string itemName, int itemCost, BaseBuilderBotModuleCAInfo info)
		{
			var candCap = CapacityOf(world, itemName);
			if (candCap <= 0)
				return null;

			var c = resources.ResourceCapacity;
			var s = resources.Resources;
			var pQueued = QueuedCapacity(world);
			var committed = c + pQueued + pendingCapacity + candCap;
			var target = ResolveTarget(info.StorageAdmissionTarget, info.StorageAdmissionCeiling, s, info.StorageAdmissionHysteresis);
			var gap = lastStorageEmitTick < 0 ? -1 : world.WorldTick - lastStorageEmitTick;
			var cooldownOk = info.StorageAdmissionCooldownTicks <= 0 || gap < 0 || gap >= info.StorageAdmissionCooldownTicks;
			var verdictT = VerdictTarget(committed, target);
			var verdictC = VerdictCeiling(committed, info.StorageAdmissionCeiling);
			var verdict = verdictT == null ? (bool?)null : verdictT == true && cooldownOk;

			AIUtils.BotDebug("E1_STORAGE_OBS emit item={0} producer={1} c={2} s={3} p_queued={4} p_pending={5} cand_cap={6} "
				+ "cand_cost={7} committed_plus_cand={8} target={9} ceiling={10} gap_ticks={11} cooldown_ok={12} "
				+ "verdict_target={13} verdict_ceiling={14} verdict={15} mode=observational(no-gate)",
				itemName, queue.Actor.ActorID, c, s, pQueued, pendingCapacity, candCap, itemCost, committed,
				target, info.StorageAdmissionCeiling, gap, cooldownOk,
				verdictT?.ToString() ?? "unset", verdictC?.ToString() ?? "unset", verdict?.ToString() ?? "unset");

			return new Reservation(this, queue, itemName, candCap);
		}

		// Called AFTER the order call path — the reservation joins P_pending. From the next pass the
		// same capacity also shows in P_queued (the item is in its producer's queue): the permanent
		// double-count, deliberately retained for the reservation's lifetime.
		public void NoteEmitted(Reservation reservation, int worldTick)
		{
			// Already settled (refused between registration and here) or already ledgered — do not count it twice.
			if (reservation.Released || reservation.Added)
				return;

			reservation.Added = true;
			pending.Add(reservation);
			pendingCapacity += reservation.Capacity;
			lastStorageEmitTick = worldTick;
		}
	}
}
