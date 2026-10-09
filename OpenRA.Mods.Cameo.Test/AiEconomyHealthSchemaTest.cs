using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AiEconomyHealthSchemaTest
	{
		static EconomyHealthIdentity Identity(int sequence, int tick, string kind)
			=> new(sequence, tick, kind, "roundtrip", "hard", "map", "td_nod", "hard", true, 0);

		[Test]
		public void SignedRefundableNetSpendNeverClaimsGrossSpend()
		{
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Pulse(Identity(0, 0, "pulse"),
				true, true, Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, -123));
			var root = row.RootElement;
			Assert.That(root.GetProperty("net_spent").GetInt32(), Is.EqualTo(-123));
			Assert.That(root.GetProperty("gross_spent").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(root.GetProperty("gross_spend_complete").GetBoolean(), Is.False);
			Assert.That(root.GetProperty("schema").GetInt32(), Is.EqualTo(2));
		}

		[Test]
		public void QueueAgeAndUnknownProducerEvidenceArePreserved()
		{
			var queues = new[] { new EconomyQueueSnapshot("producer:queue", "episode", "refinery",
				"ready", 17, false, "producer-disabled") };
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Pulse(Identity(1, 50, "pulse"),
				false, false, queues, 5000, 0, 1000, 2));
			var root = row.RootElement;
			Assert.That(root.GetProperty("queues_complete").GetBoolean(), Is.False);
			Assert.That(root.GetProperty("player_active").GetBoolean(), Is.False);
			Assert.That(root.GetProperty("queues")[0].GetProperty("state_since_tick").GetInt32(), Is.EqualTo(17));
			Assert.That(root.GetProperty("queues")[0].GetProperty("producer_live").GetBoolean(), Is.False);
		}

		[Test]
		public void EmitConsumerRoundTripWithRefundAndCoveredTerminal()
		{
			var rows = new[]
			{
				AiEconomyHealthSchema.Pulse(Identity(0, 0, "pulse"), true, true,
					Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 123),
				AiEconomyHealthSchema.Pulse(Identity(1, 50, "pulse"), true, true,
					Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, -123),
				AiEconomyHealthSchema.End(Identity(2, 50, "end"), true)
			};
			using var end = JsonDocument.Parse(rows[2]);
			Assert.That(end.RootElement.GetProperty("complete").GetBoolean(), Is.True);
			// Generated test artifact, consumed by the independent Python analyzer command.
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
			Directory.CreateDirectory(directory);
			WriteCliFixture("schema", rows);
		}

		[Test]
		public void EmitReadyThresholdAndIncompleteConsumerFixtures()
		{
			var rows = new List<string>();
			var queue = new[] { new EconomyQueueSnapshot("yard:Building", "one", "refinery",
				"ready", 0, true, "observed") };
			for (var tick = 0; tick <= 250; tick += 50)
				rows.Add(AiEconomyHealthSchema.Pulse(Identity(rows.Count, tick, "pulse"),
					true, true, queue, 5000, 0, 1000, 100));
			rows.Add(AiEconomyHealthSchema.End(Identity(rows.Count, 250, "end"), true));
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
			Directory.CreateDirectory(directory);
			WriteCliFixture("ready", rows);
			rows[^1] = AiEconomyHealthSchema.End(Identity(rows.Count - 1, 250, "end"), false);
			WriteCliFixture("incomplete", rows);
			using var end = JsonDocument.Parse(rows[^1]);
			Assert.That(end.RootElement.GetProperty("complete").GetBoolean(), Is.False);
		}
		static void WriteCliFixture(string name, IEnumerable<string> rows)
		{
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "TestResults");
			var support = Path.Combine(directory, "economy-cli-" + name + "-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(Path.Combine(support, "Logs"));
			using (var writer = new AiEconomyHealthLogWriter(Path.Combine(support, "Logs", "cameo-ai-economy-health.jsonl")))
				foreach (var row in rows)
					Assert.That(writer.TryWrite(row), Is.True);
			// Only this test-discovery pointer is replaceable; evidence roots are always new.
			File.WriteAllText(Path.Combine(directory, "economy-health-" + name + "-cli-input.txt"), support);
		}

	}
}
