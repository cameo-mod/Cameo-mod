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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// BZ_production_scaling: the pure decisions behind the income-driven production arms —
	// every gate fails closed on a non-positive bar; nothing here admits on missing data.
	[TestFixture]
	public class ProductionScalingTest
	{
		// CashHeld — the banked-cash persistence window

		[Test]
		public void CashHeldRequiresTheWholeWindow()
		{
			Assert.That(ProductionScalingCA.CashHeld(500, 500), Is.True, "exactly a full window admits");
			Assert.That(ProductionScalingCA.CashHeld(499, 500), Is.False, "one tick short stays shut");
			Assert.That(ProductionScalingCA.CashHeld(1500, 500), Is.True, "longer runs still admit");
		}

		[Test]
		public void CashHeldFailsClosedOnNonPositiveBars()
		{
			Assert.That(ProductionScalingCA.CashHeld(99999, 0), Is.False, "persist bar 0 is disabled, not free");
			Assert.That(ProductionScalingCA.CashHeld(99999, -1), Is.False);
			Assert.That(ProductionScalingCA.CashHeld(0, 500), Is.False, "a just-crossed bank has no hold yet");
		}

		// AllBusy — every unit-production queue must hold work

		[Test]
		public void AllBusyNeedsEveryQueueBusy()
		{
			Assert.That(ProductionScalingCA.AllBusy(2, 2), Is.True, "both producers working");
			Assert.That(ProductionScalingCA.AllBusy(1, 2), Is.False, "an idle queue is slack, not starvation");
			Assert.That(ProductionScalingCA.AllBusy(0, 0), Is.False, "no queues at all is not starved");
		}

		// SpareIncomeMet — net income per producer above the line

		[Test]
		public void SpareIncomeMetScalesWithProducers()
		{
			Assert.That(ProductionScalingCA.SpareIncomeMet(31, 2, 15), Is.True, "31/tick over 2 producers beats the 15 line");
			Assert.That(ProductionScalingCA.SpareIncomeMet(30, 2, 15), Is.False, "exactly at the line does not admit");
			Assert.That(ProductionScalingCA.SpareIncomeMet(16, 1, 15), Is.True, "one producer needs only the bare line");
			Assert.That(ProductionScalingCA.SpareIncomeMet(61, 4, 15), Is.True, "61 over 4 beats 60");
			Assert.That(ProductionScalingCA.SpareIncomeMet(60, 4, 15), Is.False, "per-producer line, not total");
		}

		[Test]
		public void SpareIncomeMetFailsClosed()
		{
			Assert.That(ProductionScalingCA.SpareIncomeMet(99999, 0, 15), Is.False, "no producers, nothing to feed");
			Assert.That(ProductionScalingCA.SpareIncomeMet(99999, 2, 0), Is.False, "spare bar 0 is disabled");
			Assert.That(ProductionScalingCA.SpareIncomeMet(-5, 2, 15), Is.False, "negative net income never admits");
		}

		// IncomeMet — the extra-MCV arm's sustained-income gate

		[Test]
		public void IncomeMetReachesTheBar()
		{
			Assert.That(ProductionScalingCA.IncomeMet(30.0, 30), Is.True, "at the bar admits");
			Assert.That(ProductionScalingCA.IncomeMet(29.9, 30), Is.False);
			Assert.That(ProductionScalingCA.IncomeMet(100.0, 0), Is.False, "bar 0 is disabled, not free");
			Assert.That(ProductionScalingCA.IncomeMet(-3.0, 30), Is.False, "shrinking income never admits");
		}

		// McvAllowance — all three conditions bound the extra target

		[Test]
		public void McvAllowanceNeedsIncomeFieldAndParity()
		{
			Assert.That(ProductionScalingCA.McvAllowance(true, true, true, 2), Is.EqualTo(2), "all green raises the cap");
			Assert.That(ProductionScalingCA.McvAllowance(false, true, true, 2), Is.EqualTo(0), "thin income alone shuts it");
			Assert.That(ProductionScalingCA.McvAllowance(true, false, true, 2), Is.EqualTo(0), "no free far field shuts it");
			Assert.That(ProductionScalingCA.McvAllowance(true, true, false, 2), Is.EqualTo(0), "lost parity shuts it");
		}

		[Test]
		public void McvAllowanceNeverGoesNegative()
		{
			Assert.That(ProductionScalingCA.McvAllowance(true, true, true, 0), Is.EqualTo(0), "cap 0 admits nothing");
			Assert.That(ProductionScalingCA.McvAllowance(true, true, true, -3), Is.EqualTo(0), "a negative cap floors at 0");
		}
	}
}
