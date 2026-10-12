#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// Barracks-built (unbound) naxis slaves went idle after their first delivery: MovingToDock
	// appended a bare MoveToDock with no harvest after it. Pins the fix and the slave-side rescue.
	[TestFixture]
	public class SlaveMinerUnboundIdleTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		[Test]
		public void UnboundMovingToDockAppendsNothing()
		{
			var body = Section(Source("SlaveMinerSpawnerSlave.cs"),
				"void INotifyDockClientMoving.MovingToDock(", "void INotifyDockClientMoving.MovementCancelled(");
			Assert.That(body, Does.Not.Contain("new MoveToDock("), "no bare dock trip may be appended for an unbound slave");
			Assert.That(body, Does.Contain("if (DeliverToMaster(self))"), "bound slaves still use the guarded delivery pair");
		}

		[Test]
		public void SlaveChecksItselfOnlyWhenItHasNoMaster()
		{
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			Assert.That(slave, Does.Contain("INotifyAddedToWorld, ITick"));
			var tick = Section(slave, "void ITick.Tick(Actor self)", "void Rescue(Actor self");
			Assert.That(tick, Does.Contain("masterActor != null"), "bound slaves are the master's job");
			Assert.That(tick, Does.Contain("SlaveIdleRescue.Due(ref idleRescueCountdown, Info.IdleCheckTick)"), "same bounded cadence");
			Assert.That(tick, Does.Contain("Rescue(self, true)"));
		}

		[Test]
		public void MasterAndSelfRescueShareOnePolicyPath()
		{
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			var master = Section(slave, "public void RescueIdle(", "void ITick.Tick(");
			Assert.That(master, Does.Contain("Rescue(self, self.Owner == master.Owner)"));
			var rescue = Section(slave, "void Rescue(Actor self", "public bool IsMoving(");
			Assert.That(rescue, Does.Contain("SlaveIdleRescue.Decide("));
			Assert.That(slave.Split(new[] { "SlaveIdleRescue.Decide(" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1),
				"one Decide call site, so master and self rescue cannot drift apart");
		}

		[Test]
		public void SelfRescueCountdownIsArmedOnCreationAndNeverScansFasterThanTheFloor()
		{
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			Assert.That(slave, Does.Contain("idleRescueCountdown = SlaveIdleRescue.Cadence(Info.IdleCheckTick);"));
			Assert.That(slave, Does.Contain("public readonly int IdleCheckTick = 103;"), "same default as the master");

			var countdown = SlaveIdleRescue.Cadence(1);
			var fired = 0;
			for (var tick = 0; tick < 1000; tick++)
				if (SlaveIdleRescue.Due(ref countdown, 1))
					fired++;

			Assert.That(fired, Is.EqualTo(10));
		}

		[Test]
		public void UnboundIdleEmptySlaveIsSentBackToHarvest()
		{
			// The slave-side tick passes sameOwnerAsMaster = true: with no master there is nothing to differ from.
			Assert.That(SlaveIdleRescue.Decide(true, true, false, true, false, true, false), Is.EqualTo(SlaveIdleRescueAction.Harvest));
			Assert.That(SlaveIdleRescue.Decide(true, true, false, true, false, true, true), Is.EqualTo(SlaveIdleRescueAction.DeliverThenHarvest));
			Assert.That(SlaveIdleRescue.Decide(false, true, false, true, false, true, false), Is.EqualTo(SlaveIdleRescueAction.None), "busy");
			Assert.That(SlaveIdleRescue.Decide(true, true, false, true, true, true, false), Is.EqualTo(SlaveIdleRescueAction.None), "freed");
		}

		[Test]
		public void SelfRescueUsesNoRandomness()
		{
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			var section = Section(slave, "public void RescueIdle(", "public bool IsMoving(");
			Assert.That(section, Does.Not.Contain("World.SharedRandom"));
			Assert.That(section, Does.Not.Contain("Random"));
		}
	}
}
