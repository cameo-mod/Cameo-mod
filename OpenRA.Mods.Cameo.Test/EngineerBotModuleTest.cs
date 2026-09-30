#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// ENG / DESIGN §19.3: the merged engineer owner keeps an engineer until it is done, gone or stuck, and one owner
	// at a time holds it (AI_REVIEW_FRANSOTTO P0b, "IsIdle is not ownership").
	[TestFixture]
	public sealed class EngineerBotModuleTest
	{
		const int Grace = 50;
		const int Sample = 120;

		static EngineerCheck Check(bool gone = false, bool idle = false, bool moving = false, bool moved = true,
			int sinceOrder = 500, int sinceSample = 500)
		{
			return EngineerBotModule.Check(gone, idle, moving, moved, sinceOrder, sinceSample, Grace, Sample);
		}

		[Test]
		public void AGoneEngineerIsDroppedWhateverElseHolds()
		{
			Assert.That(Check(gone: true, idle: true, moving: true, moved: false), Is.EqualTo(EngineerCheck.Gone));
		}

		[Test]
		public void AnIdleEngineerInsideTheGraceIsStillOnItsWay()
		{
			// The order was queued, not issued: ModularBot hands out ceil(n / MinOrderQuotientPerTick) orders a tick.
			Assert.That(Check(idle: true, sinceOrder: Grace - 1), Is.EqualTo(EngineerCheck.Working));
			Assert.That(Check(idle: true, sinceOrder: Grace), Is.EqualTo(EngineerCheck.Done));
		}

		[Test]
		public void MovingWithoutMovingForASampleIsStuck()
		{
			Assert.That(Check(moving: true, moved: false), Is.EqualTo(EngineerCheck.Stuck));
			Assert.That(Check(moving: true, moved: true), Is.EqualTo(EngineerCheck.Working));
		}

		[Test]
		public void NotStuckBeforeAWholeSampleOrInsideTheGrace()
		{
			Assert.That(Check(moving: true, moved: false, sinceSample: Sample - 1), Is.EqualTo(EngineerCheck.Working));
			Assert.That(Check(moving: true, moved: false, sinceOrder: Grace - 1), Is.EqualTo(EngineerCheck.Working));
		}

		[Test]
		public void StandingStillWhileNotMovingIsWorkNotStuck()
		{
			// e.g. repairing, or inside the capture's enter step: only a stalled MOVE counts.
			Assert.That(Check(moving: false, moved: false), Is.EqualTo(EngineerCheck.Working));
		}

		[Test]
		public void AnEngineerTheSquadsHoldIsRefusedToTheEngineerOwner()
		{
			var table = new BotLeaseTable<int>();
			Assert.That(table.TryClaim(7, "SquadManagerBotModuleCA", BotLeasePurpose.Squad, 0, 3000), Is.True);
			Assert.That(table.IsClaimedByOther(7, nameof(EngineerBotModule), 10), Is.True);
			Assert.That(table.TryClaim(7, nameof(EngineerBotModule), BotLeasePurpose.Capture, 10, 3000), Is.False);

			table.Release(7, "SquadManagerBotModuleCA");
			Assert.That(table.TryClaim(7, nameof(EngineerBotModule), BotLeasePurpose.Capture, 20, 3000), Is.True);
			Assert.That(table.LeaseOf(7, 20)?.Purpose, Is.EqualTo(BotLeasePurpose.Capture));
		}
	}
}
