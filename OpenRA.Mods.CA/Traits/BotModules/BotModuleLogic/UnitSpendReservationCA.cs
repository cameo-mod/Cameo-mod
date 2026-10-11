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
		sealed class Tracked
		{
			public int Total;
			public int Remaining;
			public bool Essential;
			public int Paid => Math.Max(0, Total - Remaining);
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
		public int UnitOutstanding => units.Values.Sum(t => t.Remaining);

		public int UnitItemsInFlight => units.Count;
		public int BuildingItemsInFlight => buildings.Count;

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
		/// floor. Items reported again just update; a repeated settle can never double-count.
		/// </summary>
		public void ReconcileUnits(object owner, IReadOnlyDictionary<string, (int Total, int Remaining)> items)
		{
			foreach (var key in units.Keys.Where(k => ReferenceEquals(k.Owner, owner)).ToList())
			{
				if (!items.ContainsKey(key.Item))
				{
					PaidUnitCost = SaturateAdd(PaidUnitCost, units[key].Paid);
					units.Remove(key);
				}
			}

			foreach (var kv in items)
			{
				var key = (owner, kv.Key);
				if (!units.TryGetValue(key, out var tracked))
					units[key] = tracked = new Tracked();
				tracked.Total = Math.Max(0, kv.Value.Total);
				tracked.Remaining = Math.Max(0, kv.Value.Remaining);
			}
		}

		/// <summary>
		/// The building side's per-sweep reconcile for one producer queue — the metric feed, split
		/// by the army-first essential flag at settle time. Same settle-exactly-once rule.
		/// </summary>
		public void ReconcileBuildings(object owner, IReadOnlyDictionary<string, (int Total, int Remaining, bool Essential)> items)
		{
			foreach (var key in buildings.Keys.Where(k => ReferenceEquals(k.Owner, owner)).ToList())
			{
				if (!items.ContainsKey(key.Item))
				{
					var paid = buildings[key].Paid;
					if (buildings[key].Essential)
						PaidEssentialCost = SaturateAdd(PaidEssentialCost, paid);
					else
						PaidOptionalCost = SaturateAdd(PaidOptionalCost, paid);
					buildings.Remove(key);
				}
			}

			foreach (var kv in items)
			{
				var key = (owner, kv.Key);
				if (!buildings.TryGetValue(key, out var tracked))
					buildings[key] = tracked = new Tracked();
				tracked.Total = Math.Max(0, kv.Value.Total);
				tracked.Remaining = Math.Max(0, kv.Value.Remaining);
				tracked.Essential = kv.Value.Essential;
			}
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
