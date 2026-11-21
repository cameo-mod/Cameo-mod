#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-IDLE_PLAN_03 sections 2.2, 2.3 and 3: the P(r) term, the class boundaries and the
	// projected share test. Written with the code; not run (no build in the PERF WINDOW).
	[TestFixture]
	public class ProdIdleAdmissionTest
	{
		const RecoverySpendPolicyCA.Admission General = RecoverySpendPolicyCA.Admission.General;

		[Test]
		public void GeneralGateUsesMaxOfMobileEssentialAndProductionNeverTheirSum()
		{
			// M 2000, E 1500, P 3000, R 0: floor is 3000, not 6500.
			Assert.That(RecoverySpendPolicyCA.Admits(4000, 1000, 2000, 1500, 0, false, General, 3000), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(3999, 1000, 2000, 1500, 0, false, General, 3000), Is.False);
			// P below the other terms changes nothing.
			Assert.That(RecoverySpendPolicyCA.Admits(3000, 1000, 2000, 1500, 0, false, General, 500), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(2999, 1000, 2000, 1500, 0, false, General, 500), Is.False);
		}

		[Test]
		public void RecoveryChargeIsAddedToTheMaximumNotFoldedIntoIt()
		{
			// floor = max(M, E, P) + R = 3000 + 1000.
			Assert.That(RecoverySpendPolicyCA.Admits(5000, 1000, 2000, 1500, 1000, false, General, 3000), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(4999, 1000, 2000, 1500, 1000, false, General, 3000), Is.False);
			// The same commitment as the essential keeps R out of the sum.
			Assert.That(RecoverySpendPolicyCA.Admits(4000, 1000, 2000, 1500, 1000, true, General, 3000), Is.True);
		}

		[Test]
		public void DefaultProductionReserveKeepsTheF2F3Behaviour()
		{
			Assert.That(RecoverySpendPolicyCA.Admits(4000, 1000, 2000, 1500, 1000, false, General),
				Is.EqualTo(RecoverySpendPolicyCA.Admits(4000, 1000, 2000, 1500, 1000, false, General, 0)));
		}

		[Test]
		public void OpeningAndSelectedRecoveryMayDipBelowTheProductionReserve()
		{
			Assert.That(RecoverySpendPolicyCA.Admits(1000, 1000, 2000, 0, 1000, false,
				RecoverySpendPolicyCA.Admission.OpeningOrEssential, 9000), Is.True);
			// K3: only C - x >= E, whatever P, M and R are.
			Assert.That(RecoverySpendPolicyCA.Admits(1500, 1000, 2000, 500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery, 9000), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(1499, 1000, 2000, 500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery, 9000), Is.False);
		}

		[Test]
		public void EssentialBuildingIgnoresPButCannotTakeTheFundsEHolds()
		{
			var k4 = RecoverySpendPolicyCA.Admission.EssentialBuilding;
			Assert.That(RecoverySpendPolicyCA.Admits(1500, 1000, 2000, 500, 1000, false, k4, 9000), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(1499, 1000, 2000, 500, 1000, false, k4, 9000), Is.False);
			// A unit (K5) with the same numbers is blocked by P.
			Assert.That(RecoverySpendPolicyCA.Admits(1500, 1000, 2000, 500, 1000, false, General, 9000), Is.False);
		}

		[Test]
		public void LedgerPassesTheProductionReserveToThePolicy()
		{
			var ledger = new RecoverySpendLedgerCA();
			Assert.That(ledger.TryAllocateGeneration(out var generation), Is.True);
			var key = new RecoverySpendKey(generation, 7, 0, "barracks");
			Assert.That(ledger.TryReserve(key, 3000, 1000, 0, 0, 0, false, General, 2500), Is.False);
			Assert.That(ledger.TryReserve(key, 3500, 1000, 0, 0, 0, false, General, 2500), Is.True);
		}

		[TestCase(500, 0, 500, 0, 0, true)]      // exactly at target: 500 x 1000 >= 500 x 1000
		[TestCase(499, 0, 500, 0, 0, false)]     // one below target
		[TestCase(500, 0, 400, 100, 0, true)]    // in-flight building counts in B'
		[TestCase(500, 0, 400, 100, 1, false)]   // one more cost unit crosses below target
		[TestCase(0, 500, 500, 0, 0, true)]      // in-flight army counts in A'
		public void ProjectedShareBoundary(long armyLive, long armyInflight, long buildingLive,
			long buildingInflight, long cost, bool expected)
		{
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(armyLive, armyInflight, buildingLive,
				buildingInflight, cost, 500), Is.EqualTo(expected));
		}

		[Test]
		public void TwoAdmissionsInOnePassCannotBothPassAnOldSnapshot()
		{
			// A' = 1000, B = 900, T = 500: one building of 100 passes (1000 vs 1000 total shares at target).
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(1000, 0, 900, 0, 100, 500), Is.True);
			// The first order is recorded before the second is tested, so the second sees B' = 1000 + 100.
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(1000, 0, 900, 100, 100, 500), Is.False);
		}

		[Test]
		public void ProjectedShareEdgesAndLargeValues()
		{
			// A' + B' = 0 is share 0, below target: blocked even at cost 0.
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(0, 0, 0, 0, 0, 500), Is.False);
			// 64-bit: values near int.MaxValue must not overflow.
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(int.MaxValue, int.MaxValue, 0, 0, 0, 750), Is.True);
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(int.MaxValue, 0, int.MaxValue, 0, 0, 750), Is.False);
			// Target 0 admits any non-empty state; negative inputs clamp to 0.
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(0, 0, 100, 0, 0, 0), Is.True);
			Assert.That(RecoverySpendPolicyCA.ProjectedShareAdmits(-5, 0, -5, 0, 0, 500), Is.False);
		}

		[Test]
		public void DeliveryTimeoutIsTheNamedSafetyCapAndStartsOnlyAfterRetired()
		{
			Assert.That(RecoverySpendPolicyCA.DeliveryTimeoutTicks, Is.EqualTo(1500));
			var ledger = new RecoverySpendLedgerCA();
			Assert.That(ledger.TryAllocateGeneration(out var generation), Is.True);
			var key = new RecoverySpendKey(generation, 7, 0, "harv");
			Assert.That(ledger.TryReserve(key, 2000, 1000, 0, 0, 0, false,
				RecoverySpendPolicyCA.Admission.General), Is.True);
			Assert.That(ledger.RegisterDelivery(key, 10), Is.True);
			// Not retired: covered at any tick, no timeout.
			Assert.That(ledger.DeliveryCovered(key, 10 + RecoverySpendPolicyCA.DeliveryTimeoutTicks + 5000), Is.True);
			Assert.That(ledger.NoteRetired(key, 100), Is.True);
			Assert.That(ledger.DeliveryCovered(key, 100 + RecoverySpendPolicyCA.DeliveryTimeoutTicks - 1), Is.True);
			Assert.That(ledger.DeliveryCovered(key, 100 + RecoverySpendPolicyCA.DeliveryTimeoutTicks), Is.False);
		}
	}
}
