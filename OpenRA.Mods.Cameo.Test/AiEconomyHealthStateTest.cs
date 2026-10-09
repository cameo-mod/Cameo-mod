using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AiEconomyHealthStateTest
	{
		static EconomyQueueObservation Queue(object queue, object head, string state, string id = "1:Building")
			=> new(queue, head, id, head == null ? "" : "refinery", state, true, "observed");

		[Test]
		public void SameNameReadyReplacementStartsNewEpisode()
		{
			var state = new AiEconomyHealthState();
			var queue = new object();
			var first = new object();
			var snapshots = new List<EconomyQueueSnapshot>();
			state.Observe(0, new[] { Queue(queue, first, "ready") });
			state.CopySnapshots(snapshots);
			var firstId = snapshots[0].ItemId;
			state.Observe(1, new[] { Queue(queue, first, "ready") });
			state.CopySnapshots(snapshots);
			Assert.That(snapshots[0].StateSinceTick, Is.Zero);
			state.Observe(2, new[] { Queue(queue, new object(), "ready") });
			state.CopySnapshots(snapshots);
			Assert.That(snapshots[0].StateSinceTick, Is.EqualTo(2));
			Assert.That(snapshots[0].ItemId, Is.Not.EqualTo(firstId));
			Assert.That(state.Complete, Is.True);
		}

		[Test]
		public void IdleAndPausedTransitionsResetOnlyTheirEpisode()
		{
			var state = new AiEconomyHealthState();
			var queue = new object();
			var snapshots = new List<EconomyQueueSnapshot>();
			state.Observe(0, new[] { Queue(queue, null, "idle") });
			state.Observe(1, new[] { Queue(queue, null, "idle") });
			state.CopySnapshots(snapshots);
			Assert.That(snapshots[0].StateSinceTick, Is.Zero);
			state.Observe(2, new[] { Queue(queue, null, "paused") });
			state.Observe(3, new[] { Queue(queue, null, "idle") });
			state.CopySnapshots(snapshots);
			Assert.That(snapshots[0].StateSinceTick, Is.EqualTo(3));
		}

		[Test]
		public void DisappearedQueuesDoNotRetainProducerHistory()
		{
			var state = new AiEconomyHealthState();
			for (var tick = 0; tick < 1000; tick++)
				state.Observe(tick, new[] { Queue(new object(), null, "idle", tick.ToString()) });
			Assert.That(state.QueueCount, Is.EqualTo(1));
			Assert.That(state.Complete, Is.True);
		}

		[Test]
		public void FullCensusTurnoverPrunesBeforeAddingReplacements()
		{
			var state = new AiEconomyHealthState();
			for (var tick = 0; tick < 3; tick++)
			{
				var queues = new List<EconomyQueueObservation>();
				for (var i = 0; i < AiEconomyHealthState.MaximumQueues; i++)
					queues.Add(Queue(new object(), null, "idle", i.ToString()));
				state.Observe(tick, queues);
				Assert.That(state.QueueCount, Is.EqualTo(AiEconomyHealthState.MaximumQueues));
			}

			Assert.That(state.Complete, Is.True);
		}

		[Test]
		public void InvalidStateAndMissedTickCannotBeCertified()
		{
			var invalid = new AiEconomyHealthState();
			invalid.Observe(0, new[] { Queue(new object(), null, "unknown") });
			Assert.That(invalid.Complete, Is.False);
			var missed = new AiEconomyHealthState();
			missed.Observe(0, System.Array.Empty<EconomyQueueObservation>());
			missed.Observe(2, System.Array.Empty<EconomyQueueObservation>());
			Assert.That(missed.Complete, Is.False);
		}

		[TestCase(1)]
		[TestCase(50)]
		public void MissingTickZeroCannotBeCertified(int firstTick)
		{
			var state = new AiEconomyHealthState();
			state.Observe(firstTick, System.Array.Empty<EconomyQueueObservation>());
			Assert.That(state.Complete, Is.False);
		}

		[Test]
		public void CensusBoundAndUnknownStateAreStickyIncomplete()
		{
			var state = new AiEconomyHealthState();
			var queues = new List<EconomyQueueObservation>();
			for (var i = 0; i <= AiEconomyHealthState.MaximumQueues; i++)
				queues.Add(Queue(new object(), null, "idle", i.ToString()));
			state.Observe(0, queues);
			Assert.That(state.QueueCount, Is.LessThanOrEqualTo(AiEconomyHealthState.MaximumQueues));
			Assert.That(state.Complete, Is.False);
			state.Observe(1, System.Array.Empty<EconomyQueueObservation>());
			Assert.That(state.Complete, Is.False);
		}

		[Test]
		public void PulseAndDeliveryRulesUseWorldTicksAndPositiveAcceptedValue()
		{
			Assert.That(AiEconomyHealthState.PulseDue(0), Is.True);
			Assert.That(AiEconomyHealthState.PulseDue(50), Is.True);
			Assert.That(AiEconomyHealthState.PulseDue(750), Is.True);
			Assert.That(AiEconomyHealthState.PulseDue(49), Is.False);
			Assert.That(AiEconomyHealthState.AcceptedDelivery(0), Is.False);
			Assert.That(AiEconomyHealthState.AcceptedDelivery(1), Is.True);
		}
	}
}
