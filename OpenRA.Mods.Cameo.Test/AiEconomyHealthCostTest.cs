using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AiEconomyHealthCostTest
	{
		[Test, Explicit("Isolated core/serialization bound measurement; not a runtime cost approval.")]
		public void MaximumCensusFor45000TicksStaysWithinCaptureBudget()
		{
			var observations = new EconomyQueueObservation[AiEconomyHealthState.MaximumQueues];
			for (var i = 0; i < observations.Length; i++)
				observations[i] = new EconomyQueueObservation(new object(), new object(),
					$"{i}:Building:Building", "ra1_soviets_orerefinery", "ready", true, "observed");
			var state = new AiEconomyHealthState();
			var snapshots = new List<EconomyQueueSnapshot>();
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			var sequence = 0;
			var before = GC.GetAllocatedBytesForCurrentThread();
			var stopwatch = Stopwatch.StartNew();
			for (var tick = 0; tick <= 45000; tick++)
			{
				state.Observe(tick, observations);
				if (!AiEconomyHealthState.PulseDue(tick))
					continue;
				state.CopySnapshots(snapshots);
				var identity = new EconomyHealthIdentity(sequence++, tick, "pulse", "cost",
					"hard", "map", "ra1_soviets", "hard", true, 0);
				Assert.That(writer.TryWrite(AiEconomyHealthSchema.Pulse(identity, true, true,
					snapshots, 5000, 0, 1000, 0)), Is.True);
			}
			stopwatch.Stop();
			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.That(state.Complete, Is.True);
			Assert.That(state.QueueCount, Is.EqualTo(AiEconomyHealthState.MaximumQueues));
			Assert.That(writer.Complete, Is.True);
			Assert.That(writer.Records, Is.EqualTo(901));
			var receipt = JsonSerializer.Serialize(new
			{
				scope = "one-player core and serializer with MemoryStream; excludes World scans, callbacks, disk and engine observer",
				ticks = 45001, queues = observations.Length, pulses = writer.Records,
				elapsed_ms = stopwatch.Elapsed.TotalMilliseconds, allocated_bytes = allocated,
				output_bytes = writer.Bytes, adoption_approved = false
			});
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, "economy-health-core-cost.json"), receipt);
			TestContext.Progress.WriteLine(receipt);
		}
	}
}
