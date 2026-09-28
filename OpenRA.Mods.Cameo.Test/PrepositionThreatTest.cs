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
	[TestFixture]
	public class PrepositionThreatTest
	{
		static BotPredictedThreat T(int eta, int value) => new(new CPos(eta, value), eta, value, 0);

		[Test]
		public void TheBiggestThreatInTimeAndLargeEnoughIsMet()
		{
			var pick = SquadManagerBotModuleCA.SelectPrepositionThreat(new[] { T(900, 3000), T(400, 8000), T(3000, 20000), T(200, 500) }, 1500, 1500);
			Assert.That(pick?.Value, Is.EqualTo(8000));
		}

		[Test]
		public void NothingSmallOrFarIsMet()
		{
			Assert.That(SquadManagerBotModuleCA.SelectPrepositionThreat(new[] { T(3000, 20000), T(200, 500) }, 1500, 1500), Is.Null);
		}
	}
}
