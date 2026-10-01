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
	// §12.14 (AI_ARCHITECTURE.md) Guerrilla map-control lead — the telemetry ratio
	// "regions we hold fresh intel on" per "region with remembered enemy presence".
	// The share denominators cancel, so the raw counts divide directly; an unseen
	// enemy reads as at-target (1.0), never as infinite or zero.
	[TestFixture]
	public class GuerrillaLeadTest
	{
		[Test]
		public void NoEnemyPresenceReadsAtTarget()
		{
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(0, 0), Is.EqualTo(1.0),
				"nothing seen, nothing remembered: at-target, not zero");
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(40, 0), Is.EqualTo(1.0),
				"full coverage with no remembered enemy is still at-target — scouting harder is pointless");
		}

		[Test]
		public void LeadIsFreshPerPresence()
		{
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(10, 10), Is.EqualTo(1.0),
				"fresh coverage matching enemy presence is parity");
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(5, 10), Is.EqualTo(0.5),
				"half the contested map fresh reads as half a lead");
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(20, 10), Is.EqualTo(2.0),
				"coverage beyond the contested regions reads above target — the lead is allowed to exceed 1");
		}

		[Test]
		public void EdgeCountsAreSane()
		{
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(0, 10), Is.EqualTo(0.0),
				"zero fresh intel on a remembered enemy is a zero lead, the worst case");
			Assert.That(MasterAiBotModule.GuerrillaLeadFor(0, 0), Is.EqualTo(1.0));
		}
	}
}
