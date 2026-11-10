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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// NOOB-TIER-01 (user ruling, final): noob sits one linear step below easiest (rank -1, extrapolated).
	// Existing tiers (easiest..god) keep their ranks and values byte-identical to 8a0297b7.
	[TestFixture]
	public class NoobTierTest
	{
		[Test]
		public void NoobIsRankMinusOneOutsideTheLadder()
		{
			Assert.That(BotDifficultyLadder.NoobRank, Is.EqualTo(-1));
			Assert.That(BotDifficultyLadder.Difficulties.Length, Is.EqualTo(10));
			Assert.That(BotDifficultyLadder.Difficulties[0], Is.EqualTo("easiest"));
			Assert.That(BotDifficultyLadder.IsNoob(null), Is.False);
		}

		[Test]
		public void ScaleTargetsNoobIsOneStepBelowEasiest()
		{
			// Base Min..Max lines on 10 tiers; noob is tier -1 on the same line.
			Assert.That(ScaleTargetsEval.Lerp(3000, 30000, -1, 10), Is.EqualTo(0));     // harvester
			Assert.That(ScaleTargetsEval.Lerp(1500, 7500, -1, 10), Is.EqualTo(834));    // production
			Assert.That(ScaleTargetsEval.Lerp(1000, 3250, -1, 10), Is.EqualTo(750));    // tech
			Assert.That(ScaleTargetsEval.Lerp(600, 1400, -1, 10), Is.EqualTo(512));     // ratio
		}

		[Test]
		public void ScaleTargetsExistingTiersAreUnchanged()
		{
			Assert.That(ScaleTargetsEval.Lerp(3000, 30000, 0, 10), Is.EqualTo(3000));   // easiest
			Assert.That(ScaleTargetsEval.Lerp(3000, 30000, 9, 10), Is.EqualTo(30000));  // god
			Assert.That(ScaleTargetsEval.Lerp(1500, 7500, 1, 10), Is.EqualTo(2166));    // veryeasy, base value
			Assert.That(ScaleTargetsEval.Lerp(1000, 3250, 0, 10), Is.EqualTo(1000));
		}

		[Test]
		public void LadderKnobNoobIsOneStepBelowEasiest()
		{
			// Scale knobs (min = easiest, max = cameogod): noob = InterpolateByRank at NoobRank.
			Assert.That(BotDifficultyLadder.InterpolateByRank(6000, 2500, -1, 10), Is.EqualTo(6388)); // ProductionScalingCash
			Assert.That(BotDifficultyLadder.InterpolateByRank(40, 15, -1, 10), Is.EqualTo(42));       // ProductionScalingSpare
			Assert.That(BotDifficultyLadder.InterpolateByRank(40, 80, -1, 10), Is.EqualTo(36));       // ArmyParityPct
		}
	}
}
