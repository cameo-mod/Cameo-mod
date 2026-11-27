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
	// DEF-RATIO-PERS-01 (rulebook amendment 3, user 11-27) and audit A4 (the early-defence waiver): the per-personality RATIO-2X factor, its off row,
	// the clamp, and the waiver boundaries (rulebook line 50: the ratio starts only after BOTH legs expire). Pure state, no world.
	[TestFixture]
	public class DefenceRatioPersonalityTest
	{
		static int Prior(string personality) => DefenceRatioPersonality.PriorFor(personality, 2000, 1750, 1500, 1500, 1250, 1000);

		[Test]
		public void PersonalityPriorsAreTheRuledValues()
		{
			Assert.That(Prior("rush"), Is.EqualTo(2000));
			Assert.That(Prior("guerrilla"), Is.EqualTo(1750));
			Assert.That(Prior("steamroller"), Is.EqualTo(1500));
			Assert.That(Prior("expansion"), Is.EqualTo(1500));
			Assert.That(Prior("tech"), Is.EqualTo(1250));
			Assert.That(Prior("turtle"), Is.EqualTo(1000));
		}

		[Test]
		public void ClassicAndAnUnknownPersonalityReadTheCodeDefault()
		{
			Assert.That(DefenceRatioPersonality.CodeDefault, Is.EqualTo(2000));
			Assert.That(Prior("classic"), Is.EqualTo(2000));
			Assert.That(Prior(null), Is.EqualTo(2000));
		}

		[Test]
		public void SwitchOffIsTwoPointZeroForEveryPersonality()
		{
			foreach (var personality in new[] { "rush", "guerrilla", "steamroller", "expansion", "tech", "turtle", "classic", null })
				Assert.That(DefenceRatioPersonality.Resolve(false, Prior(personality), true, 1600), Is.EqualTo(2000), personality ?? "null");
		}

		[Test]
		public void ArmedPriorIsUsedUntilALearnedValueIsHeld()
		{
			Assert.That(DefenceRatioPersonality.Resolve(true, 1250, false, 0), Is.EqualTo(1250));
			Assert.That(DefenceRatioPersonality.Resolve(true, 1250, true, 1600), Is.EqualTo(1600));
		}

		[Test]
		public void LearnedAndPriorValuesAreClampedToTheAcceptedDomain()
		{
			Assert.That(DefenceRatioPersonality.Resolve(true, 1000, true, 100), Is.EqualTo(750));
			Assert.That(DefenceRatioPersonality.Resolve(true, 1000, true, 9000), Is.EqualTo(3000));
			Assert.That(DefenceRatioPersonality.Resolve(true, 5000, false, 0), Is.EqualTo(3000), "a prior outside the domain is clamped too");
		}

		[Test]
		public void TheRatioTestUsesThePersonalityFactor()
		{
			// need = static 400 + cost 600 = 1000, so the ratio reads directly as mobile value.
			Assert.That(MobileStaticRatioCA.Admits(1750, 400, 600, 1750), Is.True, "exactly 1.75 x 1000");
			Assert.That(MobileStaticRatioCA.Admits(1749, 400, 600, 1750), Is.False, "one below 1.75");
			Assert.That(MobileStaticRatioCA.Admits(1000, 400, 600, 1000), Is.True, "turtle 1.0");
			Assert.That(MobileStaticRatioCA.Admits(1999, 400, 600, 2000), Is.False, "the code default 2.0 is unchanged");
			Assert.That(MobileStaticRatioCA.Admits(2000, 400, 600, 2000), Is.True);
		}

		[Test]
		public void TheDefaultRatioIsTheOldTwoXAndTheOldFormStillAgrees()
		{
			Assert.That(MobileStaticRatioCA.RatioPermille, Is.EqualTo(2000));
			Assert.That(MobileStaticRatioCA.Admits(1199, 0, 600), Is.False);
			Assert.That(MobileStaticRatioCA.Admits(1200, 0, 600), Is.True);
			Assert.That(MobileStaticRatioCA.Admits(1200, 0, 600), Is.EqualTo(MobileStaticRatioCA.Admits(1200, 0, 600, 2000)));
		}

		[Test]
		public void TheWaiverHoldsWhileEitherLegIsOpen()
		{
			// Rulebook line 50 (approved): the ratio starts only after BOTH waivers expire.
			Assert.That(EarlyDefencePlan.WaiverActive(have: 1, count: 3, firstProductionTick: 100, now: 100 + 2999, waiverTicks: 3000), Is.True, "both legs open");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 3, count: 3, firstProductionTick: 100, now: 100 + 2999, waiverTicks: 3000), Is.True, "tick leg open alone");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 1, count: 3, firstProductionTick: 100, now: 100 + 3000, waiverTicks: 3000), Is.True, "count leg open alone");
		}

		[Test]
		public void TheWaiverEndsOnlyWhenBothLegsAreExhausted()
		{
			Assert.That(EarlyDefencePlan.WaiverActive(have: 2, count: 2, firstProductionTick: 100, now: 150, waiverTicks: 3000), Is.True, "count met, tick still open");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 2, count: 2, firstProductionTick: 100, now: 100 + 3000, waiverTicks: 3000), Is.False, "both exhausted at the tick boundary");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 2, count: 2, firstProductionTick: 100, now: 100 + 3500, waiverTicks: 3000), Is.False, "both exhausted, later");
		}

		[Test]
		public void TheCountBoundaryIsExactlyTheLearnedCount()
		{
			Assert.That(EarlyDefencePlan.WaiverActive(have: 2, count: 3, firstProductionTick: 100, now: 100 + 3000, waiverTicks: 3000), Is.True, "one short, tick closed");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 3, count: 3, firstProductionTick: 100, now: 100 + 3000, waiverTicks: 3000), Is.False, "exactly the count, tick closed");
		}

		[Test]
		public void TheTickBoundaryIsExactlyTheWaiverTicks()
		{
			Assert.That(EarlyDefencePlan.WaiverActive(have: 3, count: 3, firstProductionTick: 100, now: 100 + 3000, waiverTicks: 3000), Is.False, "exactly the waiver ticks");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 3, count: 3, firstProductionTick: 100, now: 100 + 2999, waiverTicks: 3000), Is.True, "one tick short");
		}

		[Test]
		public void WithNoProductionStandingThereIsNoTickLeg()
		{
			Assert.That(EarlyDefencePlan.WaiverActive(have: 0, count: 3, firstProductionTick: -1, now: 999999, waiverTicks: 3000), Is.True, "count leg open");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 3, count: 3, firstProductionTick: -1, now: 999999, waiverTicks: 3000), Is.False, "no tick clock and count met");
		}

		[Test]
		public void AZeroWaiverWindowLeavesOnlyTheCountLeg()
		{
			Assert.That(EarlyDefencePlan.WaiverActive(have: 3, count: 3, firstProductionTick: 100, now: 100, waiverTicks: 0), Is.False, "zero window, count met");
			Assert.That(EarlyDefencePlan.WaiverActive(have: 0, count: 3, firstProductionTick: 100, now: 100, waiverTicks: 0), Is.True, "zero window, count open");
		}
	}
}
