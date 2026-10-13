#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TF-2 (FINDINGS_2026-10-11_turtle_regression increment 2): the mobile-production spend
	/// reservation ledger. The unit side publishes a standing share of spendable cash while its
	/// three conditions hold — production feasible, mobile capacity free, the offensive pool
	/// needing replenishment — plus the still-unpaid remainder of every in-flight unit item, so
	/// optional building spend can neither consume the reserved share nor starve committed unit
	/// production. World-free: owners, item names and costs are supplied per reconcile, so the
	/// whole lifecycle — settle exactly once on produced/refunded/cancelled/eliminated, the
	/// paid-share metric — is directly testable.
	/// </summary>
	public sealed class UnitSpendReservation
	{
		// F1: a same-name queue is a MULTISET — queueing five tanks reports five entries, so the
		// tracked remainder is a descending list of per-instance remaining costs, never one
		// collapsed record.
		sealed class Tracked
		{
			public int Total;
			public bool Essential;
			public readonly List<int> Remaining = new();
			public int Outstanding => Remaining.Sum();
			public int Paid => Math.Max(0, Remaining.Count * Total - Outstanding);
		}

		readonly Dictionary<(object Owner, string Item), Tracked> units = new();
		readonly Dictionary<(object Owner, string Item), Tracked> buildings = new();
		int standing;

		public int Standing => standing;

		/// <summary>Settled paid spend — units, optional buildings, army-first-essential buildings.</summary>
		public int PaidUnitCost { get; private set; }

		public int PaidOptionalCost { get; private set; }
		public int PaidEssentialCost { get; private set; }

		/// <summary>Still-unpaid cash committed to in-flight unit items — freed when they leave.</summary>
		public int UnitOutstanding => units.Values.Sum(t => t.Outstanding);

		public int UnitItemsInFlight => units.Values.Sum(t => t.Remaining.Count);
		public int BuildingItemsInFlight => buildings.Values.Sum(t => t.Remaining.Count);

		/// <summary>
		/// The standing pool: `sharePct` of spendable, held only while production is feasible, a
		/// mobile queue has capacity, and the offensive pool needs replenishment. Recomputed each
		/// call — a condition that drops removes the standing hold immediately.
		/// </summary>
		public void SetStanding(bool feasible, bool capacity, bool poolNeedsReplenish, int sharePct, int spendable)
		{
			standing = feasible && capacity && poolNeedsReplenish && sharePct > 0
				? (int)Math.Min(Math.Max(0, spendable), (long)spendable * sharePct / 100)
				: 0;
		}

		/// <summary>
		/// What non-essential building spend must leave unspent: the standing share plus committed
		/// unit production, capped at what there is to spend.
		/// </summary>
		public int Floor(int spendable) => Math.Min(Math.Max(0, spendable), standing + UnitOutstanding);

		/// <summary>
		/// Paid-unit share of observed production spend, settled plus in-flight, 0–100.
		/// </summary>
		public int PaidSharePct()
		{
			var paidUnit = PaidUnitCost + units.Values.Sum(t => t.Paid);
			var paidBuildings = PaidOptionalCost + PaidEssentialCost + buildings.Values.Sum(t => t.Paid);
			var total = paidUnit + paidBuildings;
			return total > 0 ? (int)(100L * paidUnit / total) : 0;
		}

		/// <summary>
		/// The unit side's per-sweep reconcile for one producer queue: items the owner no longer
		/// reports settle exactly once — produced, refunded, cancelled or producer-eliminated all
		/// end the same claim — their paid-so-far portion counts and the unpaid rest frees off the
		/// floor. Same-name entries reconcile as a multiset: duplicate queued units are tracked
		/// per instance instead of collapsing to one. Items reported again just update; a repeated
		/// settle can never double-count.
		/// </summary>
		public void ReconcileUnits(object owner, IReadOnlyList<(string Item, int Total, int Remaining)> items) =>
			ReconcileSide(units, owner, items.Select(i => (i.Item, i.Total, i.Remaining, false)).ToList(), true);

		/// <summary>
		/// The building side's per-sweep reconcile for one producer queue — the metric feed, split
		/// by the army-first essential flag at settle time. Same multiset and settle-exactly-once
		/// rule as the unit side.
		/// </summary>
		public void ReconcileBuildings(object owner, IReadOnlyList<(string Item, int Total, int Remaining, bool Essential)> items) =>
			ReconcileSide(buildings, owner, items, false);

		void ReconcileSide(Dictionary<(object Owner, string Item), Tracked> map, object owner,
			IReadOnlyList<(string Item, int Total, int Remaining, bool Essential)> items, bool unitSide)
		{
			var reported = items.GroupBy(i => i.Item, StringComparer.Ordinal)
				.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

			foreach (var key in map.Keys.Where(k => ReferenceEquals(k.Owner, owner) && !reported.ContainsKey(k.Item)).ToList())
				SettleGroup(map, key, unitSide);

			foreach (var kv in reported)
				ReconcileGroup(map, (owner, kv.Key), kv.Value, unitSide);
		}

		/// <summary>
		/// Multiset reconcile of one (owner, item) group: each reported remainder adopts the closest
		/// tracked remainder that is not smaller than it — a live item's remaining never grows, so a
		/// reported remainder LARGER than every tracked one is a fresh instance and a tracked entry
		/// nobody adopted vanished (a completed front, a cancel, a refund). Every vanished entry
		/// settles its last-observed paid portion exactly once.
		/// </summary>
		void ReconcileGroup(Dictionary<(object Owner, string Item), Tracked> map,
			(object Owner, string Item) key, List<(string Item, int Total, int Remaining, bool Essential)> reported, bool unitSide)
		{
			if (!map.TryGetValue(key, out var tracked))
				map[key] = tracked = new Tracked();

			tracked.Total = Math.Max(0, reported.Max(r => r.Total));
			tracked.Essential = reported[0].Essential;

			var wants = reported.Select(r => Math.Max(0, r.Remaining)).OrderByDescending(r => r).ToList();
			var candidates = new List<int>(tracked.Remaining);
			foreach (var want in wants)
			{
				var best = -1;
				for (var i = 0; i < candidates.Count; i++)
					if (candidates[i] >= want && (best < 0 || candidates[i] < candidates[best]))
						best = i;

				if (best >= 0)
					candidates.RemoveAt(best);
			}

			foreach (var vanished in candidates)
				SettleEntry(tracked, vanished, unitSide);

			tracked.Remaining.Clear();
			tracked.Remaining.AddRange(wants);
		}

		/// <summary>
		/// Sweep-end flush (the F2 dead-producer fix): every tracked owner absent from the sweep's
		/// live producer set settles its whole remainder — a destroyed or disabled producer can
		/// never reconcile itself, and without the flush its unpaid items would hold the floor
		/// forever. The caller passes the producers it observed this sweep.
		/// </summary>
		public void FlushUnseenUnits(IReadOnlyCollection<object> liveOwners) =>
			FlushUnseen(units, liveOwners, true);

		public void FlushUnseenBuildings(IReadOnlyCollection<object> liveOwners) =>
			FlushUnseen(buildings, liveOwners, false);

		void FlushUnseen(Dictionary<(object Owner, string Item), Tracked> map, IReadOnlyCollection<object> liveOwners, bool unitSide)
		{
			foreach (var key in map.Keys.Where(k => !liveOwners.Contains(k.Owner)).ToList())
				SettleGroup(map, key, unitSide);
		}

		void SettleGroup(Dictionary<(object Owner, string Item), Tracked> map, (object Owner, string Item) key, bool unitSide)
		{
			var tracked = map[key];
			foreach (var remaining in tracked.Remaining)
				SettleEntry(tracked, remaining, unitSide);

			map.Remove(key);
		}

		void SettleEntry(Tracked tracked, int remaining, bool unitSide)
		{
			var paid = Math.Max(0, tracked.Total - Math.Min(tracked.Total, Math.Max(0, remaining)));
			if (unitSide)
				PaidUnitCost = SaturateAdd(PaidUnitCost, paid);
			else if (tracked.Essential)
				PaidEssentialCost = SaturateAdd(PaidEssentialCost, paid);
			else
				PaidOptionalCost = SaturateAdd(PaidOptionalCost, paid);
		}

		/// <summary>Save/load: the settled counters persist; in-flight items re-track from live queue state.</summary>
		public void RestoreSettled(int unit, int optional, int essential)
		{
			PaidUnitCost = Math.Max(0, unit);
			PaidOptionalCost = Math.Max(0, optional);
			PaidEssentialCost = Math.Max(0, essential);
		}

		static int SaturateAdd(int a, int b) => (int)Math.Min(int.MaxValue, (long)a + b);
	}
}
