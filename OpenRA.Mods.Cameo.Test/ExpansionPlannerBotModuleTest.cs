#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class ExpansionPlannerBotModuleTest
	{
		// AI_ARCHITECTURE §12.13: score = V x S / (T + tau), S = 1 / (1 + threat / max(guard, 1)).
		static double Score(int value, int threat = 0, int guard = 0, int cost = 2000, int income = 10, int hops = 0)
		{
			return ExpansionPlannerBotModule.Score(value, threat, guard, cost, income, hops, 300, 900, 750, out _);
		}

		[Test]
		public void MoreValueRanksHigher()
		{
			Assert.That(Score(80), Is.GreaterThan(Score(40)));
		}

		[Test]
		public void ShorterDistanceRanksHigher()
		{
			Assert.That(Score(60, hops: 0), Is.GreaterThan(Score(60, hops: 3)));
		}

		[Test]
		public void FewerRememberedEnemiesRankSafer()
		{
			Assert.That(Score(60, threat: 0), Is.GreaterThan(Score(60, threat: 3000)));

			// Our own guard at the field offsets the threat.
			Assert.That(Score(60, threat: 3000, guard: 6000), Is.GreaterThan(Score(60, threat: 3000, guard: 0)));
		}

		[Test]
		public void PaybackTurnsCostIntoTicksAtTheCurrentIncome()
		{
			ExpansionPlannerBotModule.Score(60, 0, 0, 2000, 10, 2, 300, 900, 750, out var payback);

			// 2000 / 10 per tick + 2 links x 300 + the refinery's 900.
			Assert.That(payback, Is.EqualTo(200 + 600 + 900));
		}

		[Test]
		public void NoIncomeMakesEveryFieldSlowButStillRanksThem()
		{
			Assert.That(Score(80, income: 0), Is.GreaterThan(Score(40, income: 0)));
		}

		[Test]
		public void HopsCountTheBuildingsNeededToReachAField()
		{
			Assert.That(ExpansionPlannerBotModule.Hops(10, 12, 4), Is.EqualTo(0));
			Assert.That(ExpansionPlannerBotModule.Hops(13, 12, 4), Is.EqualTo(1));
			Assert.That(ExpansionPlannerBotModule.Hops(20, 12, 4), Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.Hops(21, 12, 4), Is.EqualTo(3));
		}
	}
}
