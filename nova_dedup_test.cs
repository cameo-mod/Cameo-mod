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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadOrderDedupTest
	{
		// AR-S (2026-10-04): identical per-tick re-issues cancel the in-flight activity — the
		// maintainer's "move one tile, stand, move" report. The rule must preserve micro:
		// re-tasking is fine while a unit is in motion or attacking; only a byte-identical
		// repeat of the in-flight order is suppressed.

		[Test]
		public void UnarmedAlwaysIssues()
		{
			// The pre-change stream, bit-identical — every call queues.
			Assert.That(SquadOrderDedup.ShouldIssue(armed: false, hasPrev: true, sameKey: true, idle: false, terminal: false), Is.True);
			Assert.That(SquadOrderDedup.ShouldIssue(armed: false, hasPrev: true, sameKey: true, idle: true, terminal: true), Is.True);
			Assert.That(SquadOrderDedup.ShouldIssue(armed: false, hasPrev: false, sameKey: false, idle: true, terminal: false), Is.True);
		}

		[Test]
		public void FirstOrderAlwaysIssues()
		{
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: false, sameKey: false, idle: false, terminal: false), Is.True);
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: false, sameKey: false, idle: false, terminal: true), Is.True);
		}

		[Test]
		public void ChangedKeyAlwaysIssues()
		{
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: true, sameKey: false, idle: false, terminal: false), Is.True);
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: true, sameKey: false, idle: true, terminal: false), Is.True);
		}

		[Test]
		public void SameKeyInFlightSuppresses()
		{
			// The stutter kill: identical order while the unit is still executing it.
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: true, sameKey: true, idle: false, terminal: false), Is.False);
		}

		[Test]
		public void SameKeyIdleMobileReissues()
		{
			// The earlier Move completed (idle member) — re-issue is a real retry, not a cancel.
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: true, sameKey: true, idle: true, terminal: false), Is.True);
		}

		[Test]
		public void SameKeyTerminalNeverRepeats()
		{
			// A completed Stop/ReturnToBase needs no re-issue whether the member is idle or not.
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: true, sameKey: true, idle: true, terminal: true), Is.False);
			Assert.That(SquadOrderDedup.ShouldIssue(armed: true, hasPrev: true, sameKey: true, idle: false, terminal: true), Is.False);
		}

		[Test]
		public void KeysCompareOnOrderCellAndTarget()
		{
			var a = SquadOrderKey.ForCell("Move", new CPos(3, 4));
			var b = SquadOrderKey.ForCell("Move", new CPos(3, 4));
			var c = SquadOrderKey.ForCell("Move", new CPos(3, 5));
			var d = SquadOrderKey.ForCell("AttackMove", new CPos(3, 4));

			Assert.That(a.Equals(b), Is.True);
			Assert.That(a.Equals(c), Is.False, "one-cell target drift must re-issue");
			Assert.That(a.Equals(d), Is.False, "order-string change must re-issue");
			Assert.That(SquadOrderKey.Plain("Stop").Equals(SquadOrderKey.Plain("Stop")), Is.True);
		}
	}
}
