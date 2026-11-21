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
	// PROD-IDLE K5/K6 production-reserve gate (PLAN_03 section 2.2/2.3, PLAN_04 row 4): C - x >= P_eff with C the cash
	// less every unpaid commitment counted once. Written with the code; not run (no build in the PERF WINDOW).
	[TestFixture]
	public class ReserveGateTest
	{
		const RecoverySpendPolicyCA.Admission General = RecoverySpendPolicyCA.Admission.General;

		[TestCase(5000L, 1000L, 500, 3300, false)] // 5000 - 1000 - 500 = 3500 >= 3300
		[TestCase(5000L, 1000L, 700, 3300, false)] // exactly 3300 passes
		[TestCase(5000L, 1000L, 701, 3300, true)]  // one cash below the reserve holds
		[TestCase(3000L, 0L, 100, 3300, true)]     // cash below the reserve holds everything with a cost
		[TestCase(800L, 1000L, 100, 300, true)]    // commitments above the cash: C = 0, held
		[TestCase(800L, 1000L, 0, 0, false)]       // nothing to spend, no reserve: a zero-cost order still passes
		public void GeneralOrderMustLeaveTheReserveNetOfCommitments(long cash, long committed, int cost, int reserve, bool held)
		{
			Assert.That(ArmyFirstEval.ReserveHolds(false, General, cash, committed, cost, reserve), Is.EqualTo(held));
		}

		[Test]
		public void OnlyTheGeneralClassIsTested()
		{
			// Same numbers that hold a General order: 1000 cash, 900 cost, reserve 3300.
			Assert.That(ArmyFirstEval.ReserveHolds(false, General, 1000, 0, 900, 3300), Is.True);
			foreach (var passing in new[]
			{
				RecoverySpendPolicyCA.Admission.OpeningOrEssential,
				RecoverySpendPolicyCA.Admission.SelectedRecovery,
				RecoverySpendPolicyCA.Admission.EssentialBuilding,
			})
				Assert.That(ArmyFirstEval.ReserveHolds(false, passing, 1000, 0, 900, 3300), Is.False, passing.ToString());

			Assert.That(ArmyFirstEval.ReserveHolds(true, General, 1000, 0, 900, 3300), Is.False, "the base builder's essential flag passes");
		}

		[Test]
		public void UnsetReserveLineHoldsGeneralClosed()
		{
			Assert.That(ArmyFirstEval.ReserveHolds(false, General, 100000, 0, 1, -1), Is.True);
		}

		[Test]
		public void AvailableIsNeverNegativeAndClampsToTheIntRange()
		{
			Assert.That(SpendCommitmentsCA.Available(500, 900), Is.EqualTo(0));
			Assert.That(SpendCommitmentsCA.Available(900, 500), Is.EqualTo(400));
			Assert.That(SpendCommitmentsCA.Available(long.MaxValue, 0), Is.EqualTo(int.MaxValue));
		}

		[TestCase(3300, 1.0, 3300)]
		[TestCase(3300, 1.5, 3300)]  // a lean at or above 1 leaves P untouched
		[TestCase(3300, 0.5, 1650)]
		[TestCase(1000, 0.75, 750)]
		[TestCase(301, 0.5, 150)]    // integer truncation, like every other leaned knob
		[TestCase(3300, 0.0, 0)]
		public void SteamrollerLeanScalesTheReserveOnly(int reserve, double lean, int expected)
		{
			Assert.That(RecoverySpendPolicyCA.EffectiveReserve(reserve, lean), Is.EqualTo(expected));
		}

		[TestCase(-1, 1.0)]
		[TestCase(-1, 0.5)]
		[TestCase(-1, 0.0)]
		public void TheLeanCanNeverTurnAnUnsetLineIntoNoReserve(int reserve, double lean)
		{
			Assert.That(RecoverySpendPolicyCA.EffectiveReserve(reserve, lean), Is.LessThan(0));
		}

		[Test]
		public void LeanedReserveAdmitsWhatTheFullReserveHolds()
		{
			// 4000 cash, 600 cost: 3400 left. Full reserve 3600 holds; a 0.5 lean (1800) admits.
			Assert.That(ArmyFirstEval.ReserveHolds(false, General, 4000, 0, 600, 3600), Is.True);
			Assert.That(ArmyFirstEval.ReserveHolds(false, General, 4000, 0, 600, RecoverySpendPolicyCA.EffectiveReserve(3600, 0.5)), Is.False);
		}
	}
}
