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
	[TestFixture]
	public class RecoverySpendPolicyTest
	{
		[Test]
		public void GeneralSpendRespectsMaximumFloorAndSeparateRecoveryCharge()
		{
			var kind = RecoverySpendPolicyCA.Admission.General;
			Assert.That(RecoverySpendPolicyCA.Admits(4000, 1000, 2000, 1500, 1000, false, kind), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(3999, 1000, 2000, 1500, 1000, false, kind), Is.False);
			Assert.That(RecoverySpendPolicyCA.Admits(4000, 1500, 2000, 1500, 1000, true, kind), Is.True);
		}

		[Test]
		public void OnlySelectedRecoveryMayUseMobileFloorButItLeavesEssentialFloor()
		{
			Assert.That(RecoverySpendPolicyCA.Admits(2500, 1000, 2000, 1500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(2499, 1000, 2000, 1500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.False);
			Assert.That(RecoverySpendPolicyCA.Admits(2500, 1000, 2000, 1500, 1000, false,
				RecoverySpendPolicyCA.Admission.General), Is.False);
		}

		[Test]
		public void EssentialMayUseReservesButNeverSpendMoreThanAvailable()
		{
			var kind = RecoverySpendPolicyCA.Admission.OpeningOrEssential;
			Assert.That(RecoverySpendPolicyCA.Admits(1000, 1000, 2000, 1000, 2000, false, kind), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(999, 1000, 0, 0, 0, false, kind), Is.False);
			Assert.That(RecoverySpendPolicyCA.Admits(int.MaxValue, 0, int.MaxValue, 0, int.MaxValue, false,
				RecoverySpendPolicyCA.Admission.General), Is.False);
		}

		[Test]
		public void PendingChargesAndExactRecoveryIdentityPreventDoubleSpending()
		{
			var ledger = new RecoverySpendLedgerCA();
			Assert.That(ledger.TryAllocateGeneration(out var generation), Is.True);
			Assert.That(ledger.TryAllocateGeneration(out var generalGeneration), Is.True);
			var selected = new RecoverySpendKey(generation, 1, 0, "harvester");
			var wrongQueue = new RecoverySpendKey(generation, 2, 0, "harvester");
			var general = new RecoverySpendKey(generalGeneration, 2, 0, "tank");
			ledger.SelectRecovery(selected);
			Assert.That(ledger.TryReserve(wrongQueue, 3000, 1000, 1500, 500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.False);
			Assert.That(ledger.TryReserve(selected, 3000, 1000, 1500, 500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.True);
			Assert.That(ledger.TryReserve(general, 3000, 1, 1500, 500, 1000, false,
				RecoverySpendPolicyCA.Admission.General), Is.False);
			Assert.That(ledger.Settle(general), Is.False);
			Assert.That(ledger.Settle(wrongQueue), Is.False);
			Assert.That(ledger.TransferToQueue(wrongQueue, 1000), Is.False);
			Assert.That(ledger.TransferToQueue(selected, 1000), Is.True);
			Assert.That(ledger.CommittedCost, Is.EqualTo(1000));
			Assert.That(ledger.ObserveRemaining(selected, 1001), Is.False);
			Assert.That(ledger.ObserveRemaining(selected, 750), Is.True);
			Assert.That(ledger.CommittedCost, Is.EqualTo(750));
			Assert.That(ledger.Settle(selected), Is.True);
			Assert.That(ledger.Settle(selected), Is.False);
			Assert.That(ledger.TryReserve(selected, 3000, 1000, 1500, 500, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.False);
		}

		[Test]
		public void FloorLeaseSelectsLowestEligibleProducerAndDoesNotSkipBusyWinner()
		{
			var ledger = new RecoverySpendLedgerCA();
			var eligible = new[] { (Actor: 9U, Queue: 1), (Actor: 3U, Queue: 2), (Actor: 3U, Queue: 1) };
			Assert.That(ledger.TryAcquireFloorLease(9, 1, true, eligible), Is.False);
			Assert.That(ledger.TryAcquireFloorLease(3, 1, false, eligible), Is.False);
			Assert.That(ledger.TryAcquireFloorLease(3, 1, true, eligible), Is.True);
			Assert.That(ledger.TryAcquireFloorLease(3, 1, true, eligible), Is.False);
			Assert.That(ledger.ReleaseFloorLease(9, 1), Is.False);
			Assert.That(ledger.ReleaseFloorLease(3, 1), Is.True);
		}

		[Test]
		public void ReloadRetainsUnknownCommitmentsAndRejectsCorruptSnapshot()
		{
			var original = new RecoverySpendLedgerCA();
			Assert.That(original.HasState, Is.False);
			Assert.That(original.TryAllocateGeneration(out var generation), Is.True);
			Assert.That(original.HasState, Is.True);
			var key = new RecoverySpendKey(generation, 4, 0, "harvester");
			original.SelectRecovery(key);
			Assert.That(original.TryReserve(key, 2000, 1000, 0, 0, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.True);
			var restored = new RecoverySpendLedgerCA();
			Assert.That(restored.Restore(original.Save()), Is.True);
			Assert.That(restored.CommittedCost, Is.EqualTo(1000));
			Assert.That(restored.TryAllocateGeneration(out var next), Is.True);
			Assert.That(next, Is.GreaterThan(generation));
			Assert.That(restored.TryReserve(key, 2000, 1000, 0, 0, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.False);
			var corrupt = original.Save() with { UsedGenerations = new uint[0] };
			Assert.That(restored.Restore(corrupt), Is.False);
			Assert.That(restored.CommittedCost, Is.EqualTo(1000));
			var decoded = new RecoverySpendLedgerCA();
			Assert.That(decoded.RestoreText(original.SaveText()), Is.True);
			Assert.That(decoded.CommittedCost, Is.EqualTo(1000));
			Assert.That(decoded.RestoreText("{broken"), Is.False);
			Assert.That(decoded.CommittedCost, Is.EqualTo(1000));
		}

		[Test]
		public void SelectionUsesRecoveryPriorityCostAndStableQueueIdentity()
		{
			var choices = new[]
			{
				new RecoverySpendPolicyCA.RecoveryCandidate(RecoverySpendPolicyCA.RecoveryLane.Refinery, 1, 0, "refinery", 100),
				new RecoverySpendPolicyCA.RecoveryCandidate(RecoverySpendPolicyCA.RecoveryLane.ConstructionMcv, 4, 0, "mcv-b", 2000),
				new RecoverySpendPolicyCA.RecoveryCandidate(RecoverySpendPolicyCA.RecoveryLane.ConstructionMcv, 2, 0, "mcv-a", 2000),
			};
			var selected = RecoverySpendPolicyCA.SelectRecovery(choices);
			Assert.That(selected.HasValue, Is.True);
			Assert.That(selected.Value.Item, Is.EqualTo("mcv-a"));
			Assert.That(RecoverySpendPolicyCA.SelectRecovery(new RecoverySpendPolicyCA.RecoveryCandidate[0]), Is.Null);
		}

		[Test]
		public void DeliveryCoverageOutlivesCashAndOnlyRetiredStartsTimeout()
		{
			var ledger = new RecoverySpendLedgerCA();
			Assert.That(ledger.TryAllocateGeneration(out var generation), Is.True);
			var key = new RecoverySpendKey(generation, 4, 0, "harvester");
			ledger.SelectRecovery(key);
			Assert.That(ledger.TryReserve(key, 2000, 1000, 0, 0, 1000, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.True);
			Assert.That(ledger.RegisterDelivery(key, 100), Is.True);
			Assert.That(ledger.DeliveryCovered(key, 10000), Is.True, "unresolved has no timeout");
			Assert.That(ledger.NoteRetired(key, 200), Is.True);
			Assert.That(ledger.Settle(key), Is.True);
			Assert.That(ledger.CommittedCost, Is.Zero);
			Assert.That(ledger.DeliveryCovered(key, 1699), Is.True);
			Assert.That(ledger.DeliveryCovered(key, 1700), Is.False);
			Assert.That(ledger.ReleaseDelivery(key, 1700), Is.True);
		}

		[Test]
		public void OneProducedActorCoversAtMostOneGenerationAcrossSave()
		{
			var ledger = new RecoverySpendLedgerCA();
			Assert.That(ledger.TryAllocateGeneration(out var first), Is.True);
			Assert.That(ledger.TryAllocateGeneration(out var second), Is.True);
			var one = new RecoverySpendKey(first, 1, 0, "harvester");
			var two = new RecoverySpendKey(second, 2, 0, "harvester");
			ledger.SelectRecovery(one);
			Assert.That(ledger.TryReserve(one, 5000, 1000, 0, 0, 0, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.True);
			Assert.That(ledger.RegisterDelivery(one, 10), Is.True);
			ledger.SelectRecovery(two);
			Assert.That(ledger.TryReserve(two, 5000, 1000, 0, 0, 0, false,
				RecoverySpendPolicyCA.Admission.SelectedRecovery), Is.True);
			Assert.That(ledger.RegisterDelivery(two, 10), Is.True);
			Assert.That(ledger.NoteProduced("harvester", 100, 10), Is.False);
			Assert.That(ledger.NoteProduced("harvester", 100, 11), Is.True);
			Assert.That(ledger.NoteProduced("harvester", 100, 11), Is.False);
			Assert.That(ledger.DeliveryCovered(one, 11), Is.True, "an unresolved order still covers");
			Assert.That(ledger.NoteRetired(one, 12), Is.True);
			Assert.That(ledger.NoteRetired(two, 12), Is.True);
			var restored = new RecoverySpendLedgerCA();
			Assert.That(restored.RestoreText(ledger.SaveText()), Is.True);
			Assert.That(restored.DeliveryCovered(one, 12), Is.False);
			Assert.That(restored.DeliveryCovered(two, 12), Is.True);
			Assert.That(restored.NoteProduced("harvester", 100, 13), Is.False);
		}
	}
}
