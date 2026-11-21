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
	// PROD-IDLE PR-2 (PLAN_02 section 6): queue depth for the fair unit path. Written with the code; not run
	// (no build in the PERF WINDOW).
	[TestFixture]
	public class QueueDepthTest
	{
		// The same loop shape as BuildUnitFairService: bounded by FreeSlots, stopped early by Covered.
		static int Fill(int queued, long loaded, int gap, int itemTicks)
		{
			var free = QueueDepthCA.FreeSlots(queued, loaded, gap);
			var added = 0;
			while (added < free)
			{
				if (QueueDepthCA.Covered(queued + added, loaded, gap))
					break;

				added++;
				loaded += itemTicks;
			}

			return added;
		}

		[Test]
		public void CapIsAPerfConstantNotADifficultyValue()
		{
			Assert.That(QueueDepthCA.DepthCap, Is.EqualTo(4));
		}

		[TestCase(0, 0, 360, 4)]       // empty: up to the cap, the loop stops earlier once the gap is covered
		[TestCase(1, 100, 360, 3)]     // loaded below the gap: room left under the cap
		[TestCase(1, 360, 360, 0)]     // loaded exactly the gap: covered
		[TestCase(3, 100, 360, 1)]
		[TestCase(4, 0, 360, 0)]       // at the cap, whatever is loaded
		[TestCase(5, 0, 360, 0)]
		[TestCase(-2, 0, 360, 4)]      // negative item count clamps to empty
		public void FreeSlotsBoundary(int queued, long loaded, int gap, int expected)
		{
			Assert.That(QueueDepthCA.FreeSlots(queued, loaded, gap), Is.EqualTo(expected));
		}

		[Test]
		public void AnEmptyQueueIsNeverCoveredAndALoadedOneCoversAtTheGap()
		{
			Assert.That(QueueDepthCA.Covered(0, 99999, 360), Is.False);
			Assert.That(QueueDepthCA.Covered(1, 359, 360), Is.False);
			Assert.That(QueueDepthCA.Covered(1, 360, 360), Is.True);
		}

		[Test]
		public void FillLoadsEnoughItemsToCoverTheGapAndNoMore()
		{
			Assert.That(Fill(0, 0, 360, 150), Is.EqualTo(3), "150 + 150 + 150 = 450 >= 360");
			Assert.That(Fill(0, 0, 100, 150), Is.EqualTo(1), "one item alone covers a short gap: the old rule");
			Assert.That(Fill(0, 0, 10000, 50), Is.EqualTo(4), "the cap bounds a very long gap");
			Assert.That(Fill(1, 150, 360, 150), Is.EqualTo(2), "150 queued + 2 more = 450 >= 360");
			Assert.That(Fill(2, 500, 360, 150), Is.Zero, "already covers the gap");
		}

		[Test]
		public void ExtraItemsLeaveTheUnpaidRemainderOfQueuedItemsUntouched()
		{
			// Bank 1000 after this pass's orders, 400 still unpaid on queued items: a 600 item fits, 601 does not.
			Assert.That(QueueDepthCA.CashAdmits(1000, 400, 600), Is.True);
			Assert.That(QueueDepthCA.CashAdmits(1000, 400, 601), Is.False);
			Assert.That(QueueDepthCA.CashAdmits(300, 400, 0), Is.False, "unpaid cash already exceeds the bank");
		}
	}
}
