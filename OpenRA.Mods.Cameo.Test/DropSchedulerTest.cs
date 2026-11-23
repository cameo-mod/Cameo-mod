#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion


using System;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class DropSchedulerTest
	{
		[Test]
		public void TierVectorsAndMatchStartDeadlines()
		{
			for (var rank = 0; rank <= 10; rank++)
			{
				var scheduler = DropScheduler.ForTier(rank);
				Assert.That(scheduler.ChancePercent, Is.EqualTo(50 + 5 * rank).And.GreaterThan(0));
				Assert.That(scheduler.IntervalTicks, Is.EqualTo(525 - 30 * rank).And.GreaterThan(0));
				Assert.That(scheduler.InitialDelayTicks, Is.EqualTo(675 - 60 * rank).And.GreaterThan(0));
				Assert.That(scheduler.Start(10), Is.EqualTo(new DropScheduleState(10, 685 - 60 * rank)));
			}
		}

		[TestCase(-1)]
		[TestCase(11)]
		public void InvalidTierIsRejected(int rank) =>
			Assert.Throws<ArgumentOutOfRangeException>(() => DropScheduler.ForTier(rank));

		[Test]
		public void OpportunityOnlyScanRequiresNoRoll()
		{
			var scheduler = DropScheduler.ForTier(5);
			var first = scheduler.Visit(scheduler.Start(0), 0);
			Assert.That(first.ScanOpportunity, Is.True);
			Assert.That(first.DrawRegular, Is.False);
			Assert.That(first.TryRegular(null, scheduler.ChancePercent, true), Is.False);
			Assert.Throws<ArgumentException>(() => first.TryRegular(0, scheduler.ChancePercent, true));
			Assert.That(scheduler.Visit(first.State, 24).ScanOpportunity, Is.False);
			Assert.That(scheduler.Visit(first.State, 25).ScanOpportunity, Is.True);
		}

		[TestCase(74, false, true)]
		[TestCase(75, false, false)]
		[TestCase(74, true, false)]
		public void CoincidentTriggersConsumeOneRegularRollAndGiveOpportunityPriority(int roll, bool opportunityAdmitted, bool expected)
		{
			var scheduler = DropScheduler.ForTier(5);
			var visit = scheduler.Visit(scheduler.Start(0), 375);
			Assert.That(visit.ScanOpportunity, Is.True);
			Assert.That(visit.DrawRegular, Is.True);
			Assert.Throws<ArgumentException>(() => visit.TryRegular(null, scheduler.ChancePercent, opportunityAdmitted));
			Assert.That(visit.TryRegular(roll, scheduler.ChancePercent, opportunityAdmitted), Is.EqualTo(expected));
		}

		[Test]
		public void LateSlotCoalescesAndRestoredStateKeepsAbsoluteDeadlines()
		{
			var scheduler = DropScheduler.ForTier(5);
			var late = scheduler.Visit(scheduler.Start(0), 1130);
			Assert.That(late.State, Is.EqualTo(new DropScheduleState(1155, 1505)));
			Assert.That(late.TryRegular(99, scheduler.ChancePercent, false), Is.False);
			Assert.That(scheduler.Visit(late.State, 1130).DrawRegular, Is.False);
			var restored = new DropScheduleState(late.State.NextOpportunityTick, late.State.NextRegularTick);
			Assert.That(scheduler.Visit(restored, 1504).DrawRegular, Is.False);
			Assert.That(scheduler.Visit(restored, 1505).DrawRegular, Is.True);
		}
	}
}
