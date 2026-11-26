#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>The spending class a cash read belongs to. Each class is its own budget (designer-separate-queues-independent-2026-11-25).</summary>
	public enum CashClass { Building, Defence, Unit, Upgrade }

	/// <summary>Why a cash admission passed or failed.</summary>
	public enum CashReason { Affordable, Paygo, BelowReserve, BankEmpty, DepthCash }

	/// <summary>A cash admission: the verdict, its reason, the bank the gate read and the reserve it kept.</summary>
	public readonly record struct CashAdmission(bool Ok, CashReason Why, long Bank, int Reserve);

	// CONSOLIDATE-03 P2 (a) (CONSOLIDATE_03_P2_DESIGN_02.md, Sol item 5): the ONE owner of the bot's cash figures and reserve lines, for buildings, defences and units.
	// ACCOUNTING ONLY. No class ever waits for another class's commitments (designer-separate-queues-independent-2026-11-25: "a design that looks shared,
	// the cash pool, is not a reason to add a gate"). Each gate keeps counting its own class exactly as before.
	// (a) is a pure fold: every member returns the number its old helper returned, from the live bank the caller passes in. Nothing is snapshotted here
	// (Sonnet condition 1): the queues pay inside ProductionQueue.Tick, so the bank can move between two modules in one tick. The latches stay where the old
	// code latched them: the base-builder sweep (orderedCostThisSweep) and the unit pass (ProducerServiceBatch).
	public static class CashLedger
	{
		// ---- Buildings and defences (the staged planner, one ledger per BaseBuilderQueueManagerCA, keyed by the manager, not the class) ----

		/// <summary>The bank a building or defence manager reads: the live bank less this manager's own starts of this sweep (when every producer is served).</summary>
		public static int BuildingBank(bool allProducers, int rawNow, int orderedThisManager) =>
			ProdBusyCA.SweepBank(allProducers, rawNow, orderedThisManager);

		/// <summary>A6: the reserve a building start keeps. 0 for S1-S7, the tier ProductionCashReserve (unset = 0) for S8-S10.</summary>
		public static int BuildingReserve(BaseBuildStage stage, int productionCashReserve) =>
			BaseBuildPlanCA.ReserveFor(stage, productionCashReserve);

		/// <summary>A6: the full-price test with the reserve kept. A pay-as-you-go waiver (TechChainPaygo, BuildAllListed) is the caller's next step.</summary>
		public static CashAdmission AdmitBuilding(int bank, int cost, int reserve) =>
			BaseBuildPlanCA.Affordable(bank, cost, reserve)
				? new CashAdmission(true, CashReason.Affordable, bank, reserve)
				: new CashAdmission(false, bank > 0 ? CashReason.BelowReserve : CashReason.BankEmpty, bank, reserve);

		// ---- Units (the fair producer pass) ----

		/// <summary>K5: the unit reserve line. The tier ProductionCashReserve scaled by the personality lean; 0 when upgrades leave the unit figure
		/// (pb_unit_reserve_excl_upgrades); an unset line (-1) stays -1 and fails closed in the policy.</summary>
		public static int UnitReserve(int productionCashReserve, double lean, bool exclUpgrades) =>
			ProdBusyCA.UnitReserveLine(exclUpgrades, RecoverySpendPolicyCA.EffectiveReserve(productionCashReserve, lean));

		/// <summary>K5: the general gate on the pass bank net of every unpaid unit-queue item (the caller's per-tick scan, its own scope).</summary>
		public static bool UnitReserveAdmits(int bankLeft, long unpaidUnitQueues, int cost, int reserveLine) =>
			RecoverySpendPolicyCA.Admits(SpendCommitmentsCA.Available(bankLeft, unpaidUnitQueues), cost, 0, 0, 0, false,
				RecoverySpendPolicyCA.Admission.General, reserveLine);

		/// <summary>PR-2 depth: an extra item on a queue that already holds one is paid from spare cash only (the unpaid figure latched once per pass).</summary>
		public static bool UnitDepthAdmits(long bankLeft, long unpaidLatched, long cost) =>
			QueueDepthCA.CashAdmits(bankLeft, unpaidLatched, cost);
	}
}
