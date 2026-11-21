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
		public void SeatArmLineNamesTheArmAndTheVisitResidue()
		{
			// ARMLINE-01 (O-7): three gated seats, roster cycle starting at tick 10: slot i is visited at 10 + i, so its residue is
			// (10 + i) mod 3 and the seat really is the slot owner on every tick of that residue.
			var core = new BotRoundRobinCore(_ => true);
			var a = core.Register("Multi0", 1, 0, true, null, 0);
			var b = core.Register("Multi1", 2, 1, true, null, 0);
			var c = core.Register("Multi2", 3, 2, true, null, 0);
			core.Advance(10);

			Assert.That(core.CycleStart, Is.EqualTo(10));
			Assert.That(BotRoundRobinScheduler.VisitResidue(core.CycleStart, 0, 3), Is.EqualTo(1));
			Assert.That(BotRoundRobinScheduler.VisitResidue(core.CycleStart, 1, 3), Is.EqualTo(2));
			Assert.That(BotRoundRobinScheduler.VisitResidue(core.CycleStart, 2, 3), Is.EqualTo(0));
			Assert.That(BotRoundRobinScheduler.VisitResidue(core.CycleStart, -1, 3), Is.EqualTo(-1));

			for (var t = 10; t < 40; t++)
			{
				var owner = core.SlotOwner(t);
				var slot = owner == a ? 0 : owner == b ? 1 : 2;
				Assert.That(t % 3, Is.EqualTo(BotRoundRobinScheduler.VisitResidue(core.CycleStart, slot, 3)), $"t={t}");
			}

			// Rank 1 of 3 seats on the 75-tick reference period: floor(1 x 75 / 3) = 25.
			Assert.That(BotRoundRobinScheduler.FormatSeatArm("Multi1", "Bot B", "allies", true, true, 3, 2, 1, 3, 10),
				Is.EqualTo("BotRoundRobinSeat seat=Multi1 player=Bot B faction=allies rr_enabled=true gated=true n=3 visit_residue=2 slot_rank=1 slot_family=3 slot_phase=25 phase_period=75 tick=10"));

			// An ungated seat acts every tick: n 1, residue 0, whatever the roster says; rank 0 of one seat has phase 0.
			Assert.That(BotRoundRobinScheduler.FormatSeatArm("Multi3", "Bot D", "soviet", false, false, 3, -1, 0, 1, 10),
				Is.EqualTo("BotRoundRobinSeat seat=Multi3 player=Bot D faction=soviet rr_enabled=false gated=false n=1 visit_residue=0 slot_rank=0 slot_family=1 slot_phase=0 phase_period=75 tick=10"));
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
