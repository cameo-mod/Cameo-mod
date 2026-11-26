#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P2 (b), DEV row cash_ledger (CONSOLIDATE_03_P2_DESIGN_02.md): what the cash ledger saw, per seat, at most every 250 ticks. Observation only: it
	// gates nothing and draws no random number. Compiled out of a release build (the calls are [Conditional], so their arguments are not evaluated there).
	// The fields: the live bank; the unpaid remainder of every queued item, split unit / building+defence / upgrade-service; the unit K5 line and the building S8
	// line the gates use; over = how far all unpaid commitments exceed the bank (classes are independent, so this is a measure, not a limit); the unit cash
	// refusals since the last row by reason (afford, the K5 reserve, the depth spare cash); and unpaid_cache_diff = the per-tick cached unit figure minus a fresh
	// scan (Sonnet condition 1: must be 0 before the cache is trusted).
	partial class UnitBuilderBotModuleCA
	{
		const int CashLedgerRowTicks = 250;

#if CAMEO_DEV_LOGGING
		int cashLedgerRowTick = -1;
		int cashRefusedAfford;
		int cashRefusedReserve;
		int cashRefusedDepth;
#endif

		// The reason of a unit cash refusal, in the order the gate tests them.
		[Conditional("CAMEO_DEV_LOGGING")]
		void CashRefused(ProducerServiceBatch batch, int cost, bool depthTested, long unpaidLatched)
		{
#if CAMEO_DEV_LOGGING
			if (batch == null)
				return;

			if (!batch.Affords(cost))
				cashRefusedAfford++;
			else if (!ReserveAdmits(batch, cost))
				cashRefusedReserve++;
			else if (depthTested && !CashLedger.UnitDepthAdmits(batch.BankLeft, unpaidLatched, cost))
				cashRefusedDepth++;
#endif
		}

		[Conditional("CAMEO_DEV_LOGGING")]
		void CashLedgerRow()
		{
#if CAMEO_DEV_LOGGING
			var now = world.WorldTick;
			if (cashLedgerRowTick >= 0 && now - cashLedgerRowTick < CashLedgerRowTicks)
				return;

			var observers = UnitObservers;
			if (observers.Length == 0)
				return;

			cashLedgerRowTick = now;
			long bank = playerResources.GetCashAndResources();
			var all = SpendCommitmentsCA.UnpaidQueued(world, player);
			var withoutUpgrades = SpendCommitmentsCA.UnpaidQueued(world, player, t => Array.IndexOf(Info.UpgradeServiceQueues, t) >= 0);
			var withoutBase = baseBuilder != null
				? SpendCommitmentsCA.UnpaidQueued(world, player, t => baseBuilder.Info.BuildingQueues.Contains(t) || baseBuilder.Info.DefenseQueues.Contains(t))
				: all;
			var unpaidUpgrade = all - withoutUpgrades;
			var unpaidBase = all - withoutBase;
			var unpaidUnit = all - unpaidUpgrade - unpaidBase;

			var cached = UnpaidQueuedCost();
			var cacheDiff = cached - UnitUnpaidScan();

			var reserve = botLimits != null ? botLimits.Info.ProductionCashReserve : -1;
			var unitLine = botLimits != null
				? CashLedger.UnitReserve(reserve, BotPersonalityLeads.Lean(leadProviders, "steamroller"), UnitReserveExclOn)
				: -1;
			var buildingLine = baseBuilder != null
				? CashLedger.BuildingReserve(BaseBuildStage.S8ProductionCycle, reserve, baseBuilder.CashOneReserveArmed ? baseBuilder.ReserveLean() : 1.0)
				: CashLedger.BuildingReserve(BaseBuildStage.S8ProductionCycle, reserve);
			var over = Math.Max(0, all - bank);

			foreach (var o in observers)
				o.CashLedgerObserved(player, now, bank, unpaidUnit, unpaidBase, unpaidUpgrade, unitLine, buildingLine, over,
					cashRefusedAfford, cashRefusedReserve, cashRefusedDepth, cacheDiff);

			cashRefusedAfford = cashRefusedReserve = cashRefusedDepth = 0;
#endif
		}
	}
}
