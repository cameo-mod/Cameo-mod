#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class NavyStateCooldownTest
	{
		[Test]
		public void MultiTickStateActionsKeepTheirN1LengthAndUseWorldTickDeadlines()
		{
			var kickStuck = new BotCooldown();
			Assert.That(NavyStateCooldown.IsDue(true, kickStuck, 0), Is.True);
			NavyStateCooldown.Start(ref kickStuck, 0, 75, 4);
			Assert.That(NavyStateCooldown.IsActive(true, kickStuck, 75), Is.True);
			Assert.That(NavyStateCooldown.IsActive(true, kickStuck, 225), Is.True);
			Assert.That(NavyStateCooldown.IsActive(true, kickStuck, 300), Is.False);
			Assert.That(NavyStateCooldown.IsDue(true, kickStuck, 300), Is.True);

			var makeWay = new BotCooldown();
			NavyStateCooldown.Start(ref makeWay, 10, 75, 3);
			Assert.That(NavyStateCooldown.IsActive(true, makeWay, 85), Is.True);
			Assert.That(NavyStateCooldown.IsDue(true, makeWay, 235), Is.True);
			Assert.That(NavyStateCooldown.IsDue(true, makeWay, 400), Is.True);
		}
	}
}
