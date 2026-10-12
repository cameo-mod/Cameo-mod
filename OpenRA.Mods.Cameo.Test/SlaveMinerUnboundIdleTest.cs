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
	// appended a bare MoveToDock with no harvest after it. Pins the fix and the slave-side rescue, and the
	// engine interaction (Harvester.OnDockCompleted guard + FindAndDeliverResources interrupt) that made it fatal.
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

		// --- The Harvester.OnDockCompleted guard interaction -------------------------------------
		// Engine commit 35b13f077b ("Make chrono miners remember resource fields") restored a guard in
		// Harvester.OnDockCompleted: a fresh FindAndDeliverResources is queued only when the current activity
		// is null, or is not FindAndDeliverResources and has no NextActivity. FindAndDeliverResources
		// itself stops after its first delivered load whenever a NextActivity exists. In the playtest-20260709
		// release the guard was commented out, so every dock completion re-queued harvesting and the bare
		// MoveToDock this mod appended for unbound slaves was harmless. With the guard back, that stray
		// MoveToDock is the NextActivity that makes the harvest stop and nothing restarts it.
		enum Act
		{
			FindAndDeliver,
			MoveToDock,
		}

		// Mirrors Harvester.OnDockCompleted (engine_Harvester.cs, pinned by the source contract below).
		static bool DockCompletionQueuesHarvest(Act? current, bool currentHasNext) =>
			current == null || (current != Act.FindAndDeliver && !currentHasNext);

		// Mirrors FindAndDeliverResources.Tick: with a NextActivity queued, it stops once a load was delivered.
		static bool HarvestStopsAfterDelivery(bool harvestHasNext) => harvestHasNext;

		// What the slave does after its first delivery, given the activities queued behind the
		// FindAndDeliverResources that was running when the dock finished. True = still harvesting.
		static bool KeepsHarvesting(Act[] queuedBehindHarvest)
		{
			if (!HarvestStopsAfterDelivery(queuedBehindHarvest.Length > 0))
				return true;

			// The harvest stopped; the dock completion saw it as the current activity, so the guard did not fire.
			if (DockCompletionQueuesHarvest(Act.FindAndDeliver, queuedBehindHarvest.Length > 0))
				return true;

			// Only a harvest queued behind the stopped one brings the slave back to work.
			return Array.IndexOf(queuedBehindHarvest, Act.FindAndDeliver) >= 0;
		}

		[Test]
		public void StrayQueuedDockTripSuppressesTheRequeueAndIdlesTheSlave()
		{
			// Before the fix: MovingToDock appended a bare MoveToDock behind the running harvest.
			Assert.That(KeepsHarvesting(new[] { Act.MoveToDock }), Is.False, "bare MoveToDock behind the harvest idles the slave");
			Assert.That(KeepsHarvesting(new[] { Act.MoveToDock, Act.MoveToDock }), Is.False, "repeated notifications do not help");
		}

		[Test]
		public void NothingQueuedBehindTheHarvestKeepsTheSlaveWorking()
		{
			// After the fix: the unbound branch appends nothing.
			Assert.That(KeepsHarvesting(Array.Empty<Act>()), Is.True);
		}

		[Test]
		public void BoundDeliveryPairKeepsTheSlaveWorking()
		{
			// DeliverToMaster appends [MoveToDock, FindAndDeliverResources]; the trailing harvest restarts work.
			Assert.That(KeepsHarvesting(new[] { Act.MoveToDock, Act.FindAndDeliver }), Is.True);
		}

		[Test]
		public void GuardOnlyRequeuesWhenNothingElseIsQueued()
		{
			Assert.That(DockCompletionQueuesHarvest(null, false), Is.True, "no activity");
			Assert.That(DockCompletionQueuesHarvest(Act.MoveToDock, false), Is.True, "lone dock trip");
			Assert.That(DockCompletionQueuesHarvest(Act.MoveToDock, true), Is.False, "a queued next activity suppresses it");
			Assert.That(DockCompletionQueuesHarvest(Act.FindAndDeliver, false), Is.False, "harvest itself is current");
		}

		[Test]
		public void EngineStillHasTheGuardAndTheHarvestInterrupt()
		{
			// The model above is only valid while the engine behaves this way; an engine bump that changes either
			// rule must revisit the slave code, so fail here instead of idling slaves in a playtest.
			var harvester = Source("engine_Harvester.cs");
			var guard = Section(harvester, "public override void OnDockCompleted(", "public bool CanHarvestCell(");
			Assert.That(guard, Does.Contain("currentActivity == null || (currentActivity is not FindAndDeliverResources && currentActivity.NextActivity == null)"));

			var far = Source("engine_FindAndDeliverResources.cs");
			var tick = Section(far, "public override bool Tick(Actor self)", "Are we full or have nothing more to gather");
			Assert.That(tick, Does.Contain("if (NextActivity != null)"));
			Assert.That(tick, Does.Contain("if (hasDeliveredLoad || harv.IsFull)"));
		}

		[Test]
		public void NoOtherModCodeQueuesActivitiesBehindAHarvestAfterADock()
		{
			// Audit pin (2026-10-12): in OpenRA.Mods.Cameo the only code that queues a MoveToDock is the
			// slave's guarded delivery pair, which always carries its own FindAndDeliverResources.
			var slave = Source("SlaveMinerSpawnerSlave.cs");
			var pair = Section(slave, "if (SlaveIdleRescue.ShouldAppendDeliveryPair(", "return true;");
			Assert.That(pair, Does.Contain("new MoveToDock("));
			Assert.That(pair, Does.Contain("new FindAndDeliverResources(self)"));
			Assert.That(slave.Split(new[] { "new MoveToDock(" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
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
