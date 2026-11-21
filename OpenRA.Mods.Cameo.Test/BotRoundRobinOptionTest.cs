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
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// STAGGER-06 user requirement: the lobby option and the DEV per-player override spec.
	[TestFixture]
	public sealed class BotRoundRobinOptionTest
	{
		[Test]
		public void OverrideSpecNamesPlayersByInternalNameOrName()
		{
			const string spec = "Multi0:on, Multi1:OFF ,Alice:off";
			Assert.That(BotRoundRobinScheduler.ParseOverride(spec, "Multi0", "Bob"), Is.True);
			Assert.That(BotRoundRobinScheduler.ParseOverride(spec, "Multi1", null), Is.False);
			Assert.That(BotRoundRobinScheduler.ParseOverride(spec, "Multi2", "alice"), Is.False);
			Assert.That(BotRoundRobinScheduler.ParseOverride(spec, "Multi3", "Carol"), Is.Null);
		}

		[Test]
		public void EmptyOrMalformedSpecNamesNobody()
		{
			Assert.That(BotRoundRobinScheduler.ParseOverride(null, "Multi0"), Is.Null);
			Assert.That(BotRoundRobinScheduler.ParseOverride("", "Multi0"), Is.Null);
			Assert.That(BotRoundRobinScheduler.ParseOverride("Multi0", "Multi0"), Is.Null);
			Assert.That(BotRoundRobinScheduler.ParseOverride("Multi0:maybe", "Multi0"), Is.Null);
			Assert.That(BotRoundRobinScheduler.ParseOverride(":on", "Multi0"), Is.Null);
		}
	}
}
