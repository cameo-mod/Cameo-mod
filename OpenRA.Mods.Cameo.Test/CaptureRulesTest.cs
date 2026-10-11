#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// Hotspot #10 (ORDERS_2026-10-04b): QueueCaptureOrders' decision seams extracted pure —
	// every branch covered, and the laziness that guards a null target / an undrawn roll is asserted.
	[TestFixture]
	public sealed class CaptureRulesTest
	{
		[Test]
		public void NonCaptureJobsAreNeverSuperseded()
		{
			var probed = false;
			Assert.That(CaptureRules.SupersedesCapture(false, true, () => { probed = true; return true; }), Is.False);
			Assert.That(probed, Is.False, "the claim probe must not run for a non-capture job");
		}

		[Test]
		public void ATargetlessCaptureIsNeverSuperseded()
		{
			var probed = false;
			Assert.That(CaptureRules.SupersedesCapture(true, false, () => { probed = true; return true; }), Is.False);
			Assert.That(probed, Is.False, "the claim probe dereferences the target — it must not run on a null target");
		}

		[Test]
		public void AnUnclaimedTargetIsNotSuperseded()
		{
			Assert.That(CaptureRules.SupersedesCapture(true, true, () => false), Is.False);
		}

		[Test]
		public void AClaimedCaptureTargetIsSuperseded()
		{
			Assert.That(CaptureRules.SupersedesCapture(true, true, () => true), Is.True);
		}

		[Test]
		public void ShardSizeOneOrLessMakesEveryTargetTierZero()
		{
			Assert.That(CaptureRules.CaptureShardTier(0, 1, 0), Is.EqualTo(0));
			Assert.That(CaptureRules.CaptureShardTier(0, 1, 7), Is.EqualTo(0));
			Assert.That(CaptureRules.CaptureShardTier(3, 0, 9), Is.EqualTo(0));
		}

		[Test]
		public void InShardIsTierZeroOutOfShardIsTierOne()
		{
			Assert.That(CaptureRules.CaptureShardTier(2, 4, 2), Is.EqualTo(0));
			Assert.That(CaptureRules.CaptureShardTier(2, 4, 3), Is.EqualTo(1));
			Assert.That(CaptureRules.CaptureShardTier(2, 4, 0), Is.EqualTo(1));
		}

		[Test]
		public void NoRollIsDrawnWhenTransportIsOffOrEscorted()
		{
			Assert.That(CaptureRules.ShouldRollForTransport(0, false), Is.False);
			Assert.That(CaptureRules.ShouldRollForTransport(-5, false), Is.False);
			Assert.That(CaptureRules.ShouldRollForTransport(25, true), Is.False);
		}

		[Test]
		public void TheRollIsDrawnOnlyWhenATransportRunCanMatter()
		{
			Assert.That(CaptureRules.ShouldRollForTransport(1, false), Is.True);
			Assert.That(CaptureRules.ShouldRollForTransport(100, false), Is.True);
		}

		[Test]
		public void ThePriorityPassIsTheChanceRoll()
		{
			Assert.That(CaptureRules.UsesPriorityPass(0, 0), Is.False);
			Assert.That(CaptureRules.UsesPriorityPass(74, 75), Is.True);
			Assert.That(CaptureRules.UsesPriorityPass(75, 75), Is.False);
			Assert.That(CaptureRules.UsesPriorityPass(99, 100), Is.True);
		}

		[Test]
		public void FullOrDormantTargetsNeverRunTheEscortScan()
		{
			var probed = false;
			Assert.That(CaptureRules.IsOpenTarget(true, false, () => { probed = true; return false; }), Is.False);
			Assert.That(CaptureRules.IsOpenTarget(false, true, () => { probed = true; return false; }), Is.False);
			Assert.That(probed, Is.False, "BlockedByEscort can scan the map — it stays lazy behind the cheap checks");
		}

		[Test]
		public void AnOpenTargetIsDecidedByTheEscortCheck()
		{
			Assert.That(CaptureRules.IsOpenTarget(false, false, () => true), Is.False);
			Assert.That(CaptureRules.IsOpenTarget(false, false, () => false), Is.True);
		}
	}
}
