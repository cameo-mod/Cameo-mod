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
	public class CombatAnalysisCooldownTest
	{
		[Test]
		public void DecayCooldownPreservesRemainingDelayAcrossDisabledVisits()
		{
			var cooldown = new BotCooldown();
			Assert.That(CombatAnalysisCooldown.Ready(cooldown, 0), Is.True);
			CombatAnalysisCooldown.Start(ref cooldown, 0, 1000);
			var disabledFrom = CombatAnalysisCooldown.DisabledFromTick(1, 0);

			// ModularBot skips IBotTick while the conditional trait is disabled.
			CombatAnalysisCooldown.Resume(ref cooldown, 31, disabledFrom);
			Assert.That(CombatAnalysisCooldown.Ready(cooldown, 1029), Is.False);
			Assert.That(CombatAnalysisCooldown.Ready(cooldown, 1030), Is.True);
		}
	}
}
