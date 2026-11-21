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
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-IDLE PR-3 (PLAN_03 section 3): the projected army-share state and the K6 vote.
	// Written with the code; not run (no build in the PERF WINDOW).
	[TestFixture]
	public class ArmyShareTest
	{
		const RecoverySpendPolicyCA.Admission General = RecoverySpendPolicyCA.Admission.General;

		[TestCase(true, true, false, ArmyShareSide.Building)]    // a defence is in B, not A
		[TestCase(true, false, false, ArmyShareSide.Building)]
		[TestCase(false, true, false, ArmyShareSide.Army)]
		[TestCase(false, true, true, ArmyShareSide.None)]        // a harvester with a weapon is outside A
		[TestCase(false, false, false, ArmyShareSide.None)]      // MCV, attack-less support
		public void ClassifySidesFollowTheShareDefinition(bool building, bool attack, bool harvester, ArmyShareSide expected)
		{
			Assert.That(ArmyShareCA.Classify(building, attack, harvester), Is.EqualTo(expected));
		}

		[Test]
		public void SecondBuildingInOnePassSeesTheFirstOnesCost()
		{
			var share = new ArmyShareCA();
			share.Refresh(1000, 0, 900, 0);

			// A' = 1000, B' = 900 + 100: exactly at T = 500.
			Assert.That(share.BuildingAdmits(100, 500), Is.True);
			share.Record(ArmyShareSide.Building, 100);

			// The first order is recorded before the second is tested.
			Assert.That(share.Buildings, Is.EqualTo(1000));
			Assert.That(share.BuildingAdmits(100, 500), Is.False);
		}

		[Test]
		public void RecordedUnitOrdersRaiseTheArmyBeforeTheNextScan()
		{
			var share = new ArmyShareCA();
			share.Refresh(0, 0, 1000, 0);
			Assert.That(share.BuildingAdmits(100, 500), Is.False);

			share.Record(ArmyShareSide.Army, 1200);
			Assert.That(share.Army, Is.EqualTo(1200));
			Assert.That(share.BuildingAdmits(100, 500), Is.True);
		}

		[Test]
		public void RefreshReplacesTheSnapshotAndDropsRecordedOrders()
		{
			var share = new ArmyShareCA();
			share.Refresh(100, 50, 300, 20);
			share.Record(ArmyShareSide.Building, 500);
			share.Record(ArmyShareSide.Army, 700);
			Assert.That(share.Army, Is.EqualTo(850));
			Assert.That(share.Buildings, Is.EqualTo(820));

			// The next scan sees those orders as in-flight items, so they are not counted twice.
			share.Refresh(100, 750, 300, 520);
			Assert.That(share.Army, Is.EqualTo(850));
			Assert.That(share.Buildings, Is.EqualTo(820));
		}

		[Test]
		public void NegativeInputsClampAndNoneRecordsNothing()
		{
			var share = new ArmyShareCA();
			share.Refresh(-5, -5, -5, -5);
			share.Record(ArmyShareSide.Army, -100);
			share.Record(ArmyShareSide.None, 1000);
			Assert.That(share.Army, Is.Zero);
			Assert.That(share.Buildings, Is.Zero);
		}

		[TestCase(500, 500, 0, true)]     // T(noob) = 500, exactly at target
		[TestCase(500, 500, 1, false)]    // one cost unit below
		[TestCase(750, 200, 50, true)]    // T(cameogod) = 750: 750 x 1000 >= 750 x 1000
		[TestCase(750, 200, 51, false)]
		public void TargetBoundaryAtTheLineEnds(int target, long buildingLive, long cost, bool expected)
		{
			var share = new ArmyShareCA();
			share.Refresh(target == 500 ? 500 : 750, 0, buildingLive, 0);
			Assert.That(share.BuildingAdmits(cost, target), Is.EqualTo(expected));
		}

		[Test]
		public void UnsetTargetFailsClosedAndEmptyStateIsBlocked()
		{
			var share = new ArmyShareCA();
			share.Refresh(1000000, 0, 0, 0);
			Assert.That(share.BuildingAdmits(1, -1), Is.False, "unset BotLimits target is a configuration error");
			Assert.That(share.BuildingAdmits(1, 0), Is.True, "target 0 admits any non-empty state");

			share.Refresh(0, 0, 0, 0);
			Assert.That(share.BuildingAdmits(0, 500), Is.False, "A' + B' = 0 is share 0, below target");
		}

		[Test]
		public void OnlyGeneralClassOrdersAreRatioTested()
		{
			var share = new ArmyShareCA();
			share.Refresh(0, 0, 1000, 0);

			// K6 below target is held.
			Assert.That(ArmyFirstEval.ShareHolds(false, General, share, 100, 500), Is.True);

			// K1/K2 essentials, K4 essential buildings and the base builder's essential flag pass untested.
			Assert.That(ArmyFirstEval.ShareHolds(false, RecoverySpendPolicyCA.Admission.OpeningOrEssential, share, 100, 500), Is.False);
			Assert.That(ArmyFirstEval.ShareHolds(false, RecoverySpendPolicyCA.Admission.EssentialBuilding, share, 100, 500), Is.False);
			Assert.That(ArmyFirstEval.ShareHolds(true, General, share, 100, 500), Is.False);

			// K6 at or above target passes.
			share.Refresh(1000, 0, 900, 0);
			Assert.That(ArmyFirstEval.ShareHolds(false, General, share, 100, 500), Is.False);
		}
	}
}
