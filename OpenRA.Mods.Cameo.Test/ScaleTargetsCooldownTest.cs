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
	public sealed class ScaleTargetsCooldownTest
	{
		[Test]
		public void RecomputeCooldownIsInitiallyReadyAndReloadsWithoutCatchUp()
		{
			var cooldown = new BotCooldown();
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(false, cooldown, 0), Is.True);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 0), Is.True);

			ScaleTargetsBotModule.ReloadRecompute(ref cooldown, 0, 125);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 124), Is.False);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 125), Is.True);

			// A late visit reloads from its actual tick, matching the former absolute-deadline check.
			ScaleTargetsBotModule.ReloadRecompute(ref cooldown, 130, 125);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 254), Is.False);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 255), Is.True);
		}

		[Test]
		public void RecomputeCooldownUsesAbsoluteWorldTickDeadlineAcrossDisabledTicks()
		{
			var cooldown = new BotCooldown();
			ScaleTargetsBotModule.ReloadRecompute(ref cooldown, 0, 125);

			// ConditionalTrait suppresses IBotTick while disabled, but the legacy deadline is absolute WorldTick.
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 31), Is.False);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 124), Is.False);
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 125), Is.True);

			// If the gap passes the deadline, the next enabled call is immediately ready.
			Assert.That(ScaleTargetsBotModule.ShouldRecompute(true, cooldown, 131), Is.True);
		}
	}
}
