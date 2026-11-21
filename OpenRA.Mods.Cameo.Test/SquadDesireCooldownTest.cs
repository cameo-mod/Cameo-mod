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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class SquadDesireCooldownTest
	{
		[Test]
		public void PruneCooldownUsesAbsoluteWorldTickDeadlineWithoutCatchUp()
		{
			var cooldown = new BotCooldown();
			Assert.That(SquadDesireBotModule.ShouldPrune(cooldown, 0), Is.True);

			SquadDesireBotModule.ReloadPrune(ref cooldown, 0, 500);
			Assert.That(SquadDesireBotModule.ShouldPrune(cooldown, 499), Is.False);
			Assert.That(SquadDesireBotModule.ShouldPrune(cooldown, 500), Is.True);

			// Disabled traits skip IBotTick, but the stored deadline continues to age in world time.
			Assert.That(SquadDesireBotModule.ShouldPrune(cooldown, 501), Is.True);
			SquadDesireBotModule.ReloadPrune(ref cooldown, 501, 500);
			Assert.That(SquadDesireBotModule.ShouldPrune(cooldown, 1000), Is.False);
			Assert.That(SquadDesireBotModule.ShouldPrune(cooldown, 1001), Is.True);
		}
	}
}
