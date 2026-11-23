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

using System;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class DevCashBonusTest
	{
		[Test]
		public void ParsePercentMatchesSlotIdOrPlayerNameCaseInsensitive()
		{
			Assert.That(DevCashBonus.ParsePercent("Multi0:10,Multi1:0", "Multi0", "Alice"), Is.EqualTo(10));
			Assert.That(DevCashBonus.ParsePercent("Multi0:10,Multi1:25", "Multi1", "Bob"), Is.EqualTo(25));
			Assert.That(DevCashBonus.ParsePercent("alice:40", "Multi2", "Alice"), Is.EqualTo(40));
		}

		[Test]
		public void ParsePercentIsZeroWhenAbsentOrNobodyIsNamed()
		{
			Assert.That(DevCashBonus.ParsePercent(null, "Multi0"), Is.EqualTo(0));
			Assert.That(DevCashBonus.ParsePercent("", "Multi0"), Is.EqualTo(0));
			Assert.That(DevCashBonus.ParsePercent("Multi1:10", "Multi0"), Is.EqualTo(0));
		}

		[Test]
		public void ParsePercentRejectsMalformedOrOutOfRangeEntries()
		{
			Assert.Throws<FormatException>(() => DevCashBonus.ParsePercent("Multi0", "Multi0"));
			Assert.Throws<FormatException>(() => DevCashBonus.ParsePercent("Multi0:101", "Multi0"));
			Assert.Throws<FormatException>(() => DevCashBonus.ParsePercent("Multi0:-5", "Multi0"));
			Assert.Throws<FormatException>(() => DevCashBonus.ParsePercent("Multi0:ten", "Multi0"));
		}

		[Test]
		public void TopUpIsIntegerPercentAndZeroIsByteIdentical()
		{
			Assert.That(DevCashBonus.TopUp(700, 10), Is.EqualTo(70));
			Assert.That(DevCashBonus.TopUp(25, 10), Is.EqualTo(2), "rounds toward zero");
			Assert.That(DevCashBonus.TopUp(700, 0), Is.EqualTo(0));
			Assert.That(DevCashBonus.TopUp(0, 50), Is.EqualTo(0));
			Assert.That(DevCashBonus.TopUp(int.MaxValue, 100), Is.EqualTo(int.MaxValue), "no overflow");
		}
	}
}
