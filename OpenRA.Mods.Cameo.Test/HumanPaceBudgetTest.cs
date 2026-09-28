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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class HumanPaceBudgetTest
	{
		// Maintainer 2026-09-28: 24..240 orders per game minute in steps of 24; a 5-second window of 125 ticks
		// divides every tier exactly (APM / 12), the way AlphaStar's cap counted 22 actions per 5 seconds.
		[TestCase(24, 2)]
		[TestCase(48, 4)]
		[TestCase(120, 10)]
		[TestCase(240, 20)]
		public void EveryTierDividesIntoTheFiveSecondWindow(int perMinute, int perWindow)
		{
			Assert.That(HumanPaceBotModule.BudgetPerWindow(perMinute, 125, 1500), Is.EqualTo(perWindow));
		}

		[Test]
		public void ATinyRateStillAdmitsOneOrder()
		{
			Assert.That(HumanPaceBotModule.BudgetPerWindow(1, 125, 1500), Is.EqualTo(1));
		}
	}
}
