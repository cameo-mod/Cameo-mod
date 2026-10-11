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

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TF-2 (FINDINGS_2026-10-11_turtle_regression increment 2): the unit side's mobile-production
	/// reservation, published for the building queues. While production is feasible, a mobile
	/// queue has a free slot, and the eligible offensive pool needs replenishment, the provider
	/// holds a reviewed share of spendable cash — plus the still-unpaid remainder of in-flight
	/// unit production — as <see cref="MobileSpendFloor"/>. Building-queue admission refuses any
	/// NON-essential pick that would spend into it; the army-first essentials (construction yard,
	/// refinery, power, first production building) keep their old floor and always pass.
	/// Default floor 0: classic and switch-off providers leave admission unchanged.
	/// </summary>
	public interface IBotMobileSpendReserve
	{
		/// <summary>
		/// Cash the mobile-production side is holding right now — the standing share while its
		/// conditions hold, plus the unpaid remainder of tracked in-flight unit items. Building
		/// admission must leave this much unspent for non-essential items.
		/// </summary>
		int MobileSpendFloor { get; }

		/// <summary>
		/// Ledger metric: the share of observed PAID production spend (settled plus in-flight)
		/// that went to mobile unit production, 0–100. Reviewed against the trace's starvation
		/// signature — a reservation that works should raise it.
		/// </summary>
		int PaidUnitSharePct { get; }

		/// <summary>
		/// The unit side's reconcile feed, one call per mobile-producing queue per sweep:
		/// the queue's in-flight items by name with total/remaining cost. Items an owner stops
		/// reporting settle exactly once — produced, refunded, cancelled or producer-eliminated
		/// all release the same way; their paid-so-far portion counts, the rest frees.
		/// </summary>
		void ReconcileUnitSpend(object owner, IReadOnlyDictionary<string, (int Total, int Remaining)> items) { }

		/// <summary>
		/// The building side's paid-spend feed for the share metric — every in-flight building
		/// item with its army-first essential flag. Same settle-exactly-once rule per owner.
		/// </summary>
		void ReconcileBuildingSpend(object owner, IReadOnlyDictionary<string, (int Total, int Remaining, bool Essential)> items) { }
	}
}
