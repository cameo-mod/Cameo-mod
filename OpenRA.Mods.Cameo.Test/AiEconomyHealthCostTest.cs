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
		[TestCase(1), TestCase(2), Explicit("Isolated core/serialization bound measurement; not a runtime cost approval.")]
		public void MaximumCensusFor45000TicksStaysWithinCaptureBudget(int players)
		{
			var observations = new EconomyQueueObservation[AiEconomyHealthState.MaximumQueues];
			for (var i = 0; i < observations.Length; i++)
				observations[i] = new EconomyQueueObservation(new object(), new object(),
					$"{i}:Building:Building", "ra1_soviets_orerefinery", "ready", true, "observed");
			var states = new AiEconomyHealthState[players];
			for (var player = 0; player < players; player++)
				states[player] = new AiEconomyHealthState();
			var snapshots = new List<EconomyQueueSnapshot>();
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			var sequence = new int[players];
			var before = GC.GetAllocatedBytesForCurrentThread();
			var stopwatch = Stopwatch.StartNew();
			for (var tick = 0; tick <= 45000; tick++)
			{
				for (var player = 0; player < players; player++)
				{
					states[player].Observe(tick, observations);
					if (!AiEconomyHealthState.PulseDue(tick))
						continue;
					states[player].CopySnapshots(snapshots);
					var identity = new EconomyHealthIdentity(sequence[player]++, tick, "pulse", "cost",
						"hard-" + player, "map", "ra1_soviets", "hard", true, 0);
					Assert.That(writer.TryWrite(AiEconomyHealthSchema.Pulse(identity, true, true,
						snapshots, 5000, 0, 1000, 0)), Is.True);
				}
			}
			stopwatch.Stop();
			var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			foreach (var state in states)
			{
				Assert.That(state.Complete, Is.True);
				Assert.That(state.QueueCount, Is.EqualTo(AiEconomyHealthState.MaximumQueues));
			}
			Assert.That(writer.Complete, Is.True);
			Assert.That(writer.Records, Is.EqualTo(901 * players));
			if (players == 2)
				Assert.That(writer.Bytes, Is.GreaterThan(32 * 1024 * 1024));
			var receipt = JsonSerializer.Serialize(new
			{
				players,
				scope = "core and serializer with MemoryStream; excludes World scans, callbacks, disk and engine observer",
				ticks = 45001, queues = observations.Length, pulses = writer.Records,
				elapsed_ms = stopwatch.Elapsed.TotalMilliseconds, allocated_bytes = allocated,
				output_bytes = writer.Bytes, adoption_approved = false
			});
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, $"economy-health-core-cost-{players}p.json"), receipt);
			TestContext.Progress.WriteLine(receipt);
		}
	}
}
