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
	[TestFixture]
	public class SlaveMinerIdleRescueTest
	{
		static SlaveIdleRescueAction Decide(
			bool isIdle = true, bool isInWorld = true, bool isDead = false, bool sameOwner = true,
			bool isFreed = false, bool harvesterEnabled = true, bool isFull = false)
		{
			return SlaveIdleRescue.Decide(isIdle, isInWorld, isDead, sameOwner, isFreed, harvesterEnabled, isFull);
		}

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		[Test]
		public void IdleEmptySlaveGoesBackToHarvesting()
		{
			Assert.That(Decide(), Is.EqualTo(SlaveIdleRescueAction.Harvest));
		}

		[Test]
		public void IdleFullSlaveDeliversBeforeHarvesting()
		{
			Assert.That(Decide(isFull: true), Is.EqualTo(SlaveIdleRescueAction.DeliverThenHarvest));
		}

		[Test]
		public void BusyDeadAbsentForeignFreedOrDisabledSlavesAreLeftAlone()
		{
			Assert.That(Decide(isIdle: false), Is.EqualTo(SlaveIdleRescueAction.None), "busy slave");
			Assert.That(Decide(isInWorld: false), Is.EqualTo(SlaveIdleRescueAction.None), "not in world");
			Assert.That(Decide(isDead: true), Is.EqualTo(SlaveIdleRescueAction.None), "dead");
			Assert.That(Decide(sameOwner: false), Is.EqualTo(SlaveIdleRescueAction.None), "owned by someone else");
			Assert.That(Decide(isFreed: true), Is.EqualTo(SlaveIdleRescueAction.None), "freed");
			Assert.That(Decide(harvesterEnabled: false), Is.EqualTo(SlaveIdleRescueAction.None), "harvester disabled");
		}

		[Test]
		public void DecisionIsDeterministic()
		{
			for (var i = 0; i < 256; i++)
			{
				var idle = (i & 1) != 0;
				var world = (i & 2) != 0;
				var dead = (i & 4) != 0;
				var same = (i & 8) != 0;
				var freed = (i & 16) != 0;
				var enabled = (i & 32) != 0;
				var full = (i & 64) != 0;
				Assert.That(
					SlaveIdleRescue.Decide(idle, world, dead, same, freed, enabled, full),
					Is.EqualTo(SlaveIdleRescue.Decide(idle, world, dead, same, freed, enabled, full)));
			}
		}

		[Test]
		public void CadenceNeverScansFasterThanOncePerHundredTicks()
		{
			Assert.That(SlaveIdleRescue.Cadence(103), Is.EqualTo(103), "default IdleCheckTick is kept");
			Assert.That(SlaveIdleRescue.Cadence(1), Is.EqualTo(100));
			Assert.That(SlaveIdleRescue.Cadence(0), Is.EqualTo(100));
			Assert.That(SlaveIdleRescue.Cadence(-5), Is.EqualTo(100));
		}

		[Test]
		public void DueFiresOncePerCadenceAndRearms()
		{
			var countdown = SlaveIdleRescue.Cadence(1);
			var fired = 0;
			var lastFire = -1;
			for (var tick = 0; tick < 1000; tick++)
			{
				if (SlaveIdleRescue.Due(ref countdown, 1))
				{
					fired++;
					if (lastFire >= 0)
						Assert.That(tick - lastFire, Is.EqualTo(100), "constant spacing between scans");
					lastFire = tick;
				}
			}

			Assert.That(fired, Is.EqualTo(10), "exactly one scan per 100 ticks, never per tick");
		}

		[Test]
		public void DeliveryPairIsNeverAppendedWhileAHarvestIsAlreadyQueued()
		{
			Assert.That(SlaveIdleRescue.ShouldAppendDeliveryPair(true, false), Is.True);
			Assert.That(SlaveIdleRescue.ShouldAppendDeliveryPair(true, true), Is.False, "already queued: no growth");
			Assert.That(SlaveIdleRescue.ShouldAppendDeliveryPair(false, false), Is.False, "no master dock");

			// Simulate a slave cycling 1000 times: each harvest start and dock start asks for the
			// pair; the pending count must stay bounded at one pair.
			var pendingPairs = 0;
			for (var cycle = 0; cycle < 1000; cycle++)
			{
				for (var request = 0; request < 2; request++)
					if (SlaveIdleRescue.ShouldAppendDeliveryPair(true, pendingPairs > 0))
						pendingPairs++;

				Assert.That(pendingPairs, Is.LessThanOrEqualTo(1));
				pendingPairs = 0; // the pair ran
			}
		}

		[Test]
		public void DeliverToMasterConsultsTheQueueGuard()
		{
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			var start = slave.IndexOf("bool DeliverToMaster(Actor self)", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			var body = slave.Substring(start, slave.IndexOf("static bool HasQueuedHarvest", start, StringComparison.Ordinal) - start);
			Assert.That(body, Does.Contain("SlaveIdleRescue.ShouldAppendDeliveryPair(true, HasQueuedHarvest(self))"));
			Assert.That(body, Does.Contain("return true;"));
		}

		[Test]
		public void RescuePathUsesNoRandomness()
		{
			var rescue = Source("SlaveMinerIdleRescue.cs");
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			var start = slave.IndexOf("public void RescueIdle(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			var rescueMethod = slave.Substring(start, slave.IndexOf("public bool IsMoving(", start, StringComparison.Ordinal) - start);

			foreach (var token in new[] { "Random", "MersenneTwister", "BotRng", "world.SharedRandom" })
			{
				Assert.That(rescue, Does.Not.Contain(token), $"policy must not use {token}");
				Assert.That(rescueMethod, Does.Not.Contain(token), $"RescueIdle must not use {token}");
			}
		}

		[Test]
		public void MasterOnlyRescuesFreeWillSlavesOnTheBoundedCadence()
		{
			var master = Source("SlaveMinerSpawnerMaster.cs");
			var tick = master.IndexOf("void ITick.Tick(Actor self)", StringComparison.Ordinal);
			Assert.That(tick, Is.GreaterThanOrEqualTo(0));

			var freeWill = master.IndexOf("if (Info.SlavesHaveFreeWill)", tick, StringComparison.Ordinal);
			var due = master.IndexOf("SlaveIdleRescue.Due(ref idleRescueCountdown, Info.IdleCheckTick)", tick, StringComparison.Ordinal);
			var rescue = master.IndexOf(".RescueIdle(se.Actor, self)", tick, StringComparison.Ordinal);
			var assign = master.IndexOf("AssignSlaveActivity(self);", tick, StringComparison.Ordinal);

			Assert.That(freeWill, Is.GreaterThan(tick));
			Assert.That(due, Is.GreaterThan(freeWill), "the scan is gated by the free-will branch and the cadence");
			Assert.That(rescue, Is.GreaterThan(due), "no per-tick rescue outside the cadence gate");
			Assert.That(assign, Is.GreaterThan(rescue), "non-free-will masters keep their AssignSlaveActivity path");
		}
	}
}
