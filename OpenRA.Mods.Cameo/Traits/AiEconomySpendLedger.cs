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

namespace OpenRA.Mods.Cameo.Traits
{
	// One production item at one observation. TotalCost is fixed; RemainingCost falls as cash is staged in.
	internal readonly record struct SpendItemSample(object Key, int TotalCost, int RemainingCost);

	// SPEND-LEDGER-01 Step 1, record-only. Attributes staged production payments: for each tracked item,
	// paid = TotalCost - RemainingCost, and every increase of paid between observations is production spend.
	// Removal (completion or cancellation) settles the unseen increase. A cancellation refund goes through
	// GiveCash without isRefund, so it never lowers Spent and is not subtracted here.
	// Not observed here, so they stay in the residual: build-on-site (BuilderUnit only; no TD actor has it),
	// repair, resupply/rearm, transfers and steals. Pure logic: no engine types, unit-tested directly.
	internal sealed class AiEconomySpendLedger
	{
		internal const int MaximumTrackedItems = 256;

		readonly Dictionary<object, int> paid = new(ReferenceEqualityComparer.Instance);
		long production;
		bool complete = true;

		internal bool Complete => complete;

		internal void MarkIncomplete() => complete = false;

		// current: every own tracked item at this observation. removed: final state of items seen last time and gone now.
		internal void Observe(IEnumerable<SpendItemSample> current, IEnumerable<SpendItemSample> removed)
		{
			var count = 0;
			foreach (var item in current)
			{
				if (++count > MaximumTrackedItems)
					complete = false;
				Settle(item);
			}

			foreach (var item in removed)
				SettleRemoved(item);
		}

		// Attributed production since the previous call, then reset: one pulse window.
		internal long TakePulse()
		{
			var value = production;
			production = 0;
			return value;
		}

		// A removed item may have had its ore portion given back into RemainingCost before removal (engine
		// CancelProduction), so a lower paid amount is expected here: keep the attributed total, never flag.
		void SettleRemoved(SpendItemSample item)
		{
			var now = item.TotalCost - item.RemainingCost;
			paid.TryGetValue(item.Key, out var before);
			if (now > before)
				production += now - before;
			paid.Remove(item.Key);
		}

		void Settle(SpendItemSample item)
		{
			var now = item.TotalCost - item.RemainingCost;
			paid.TryGetValue(item.Key, out var before);

			// A lowered paid amount (a refund inside the queue) cannot be attributed: fail closed, keep the total.
			if (now < before)
				complete = false;
			else
				production += now - before;
			paid[item.Key] = now;
		}

		// Record body for the raw log. The residual is everything Spent counts that the ledger did not observe.
		internal static object Evidence(int windowFromTick, int windowToTick, long productionAttributed,
			long netSpentDelta, bool ledgerComplete) => new
		{
			window_from_tick = windowFromTick,
			window_to_tick = windowToTick,
			observed = new[] { "production" },
			production_attributed = productionAttributed,
			net_spent_delta = netSpentDelta,
			residual = netSpentDelta - productionAttributed,
			unobserved = new[] { "build_on_site", "repair", "resupply", "transfer_cash", "steal_resource" },
			ledger_complete = ledgerComplete,
		};
	}
}
