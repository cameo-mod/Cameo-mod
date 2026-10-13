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
		public void AcceptedDeliveriesFoldByRefineryAndTypeIntoOneBoundedRollup()
		{
			var window = new AcceptedDeliveryWindow();
			Assert.That(window.Add(17, "ore", 100, 50, 1000), Is.True);
			Assert.That(window.Add(17, "ore", 100, 50, 1000), Is.True);
			Assert.That(window.Add(17, "ore", 50, 0, 1000), Is.True);
			Assert.That(window.Add(23, "gems", 10, 200, 1000), Is.True);
			Assert.That(window.FirstTick, Is.EqualTo(1000));
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Raw(3, window.FirstTick, "roundtrip", "hard",
				"resource-accepted", window.RollupEvidence()));
			var root = row.RootElement;
			Assert.That(root.GetProperty("kind").GetString(), Is.EqualTo("resource-accepted"));
			Assert.That(root.GetProperty("seq").GetInt32(), Is.EqualTo(3));
			Assert.That(root.GetProperty("tick").GetInt32(), Is.EqualTo(1000));
			var evidence = root.GetProperty("evidence");
			Assert.That(evidence.GetProperty("deliveries").GetInt32(), Is.EqualTo(4));
			Assert.That(evidence.GetProperty("credited").GetInt64(), Is.EqualTo(300));
			Assert.That(evidence.GetProperty("gross_value").GetInt64(), Is.EqualTo(300));
			Assert.That(evidence.GetProperty("accepted_credit").GetBoolean(), Is.True);
			Assert.That(evidence.GetProperty("folded_deliveries").GetInt32(), Is.Zero);
			Assert.That(evidence.GetProperty("last_tick").GetInt32(), Is.EqualTo(1000));
			Assert.That(evidence.GetProperty("harvester").ValueKind, Is.EqualTo(JsonValueKind.Null));
			var refineries = evidence.GetProperty("refineries");
			Assert.That(refineries.GetArrayLength(), Is.EqualTo(2));
			// Ordinal (refinery, resource_type) ordering keeps bytes deterministic.
			Assert.That(refineries[0].GetProperty("refinery").GetUInt32(), Is.EqualTo(17u));
			Assert.That(refineries[0].GetProperty("resource_type").GetString(), Is.EqualTo("ore"));
			Assert.That(refineries[0].GetProperty("events").GetInt32(), Is.EqualTo(3));
			Assert.That(refineries[0].GetProperty("count").GetInt32(), Is.EqualTo(250));
			Assert.That(refineries[0].GetProperty("value").GetInt64(), Is.EqualTo(100));
			Assert.That(refineries[1].GetProperty("refinery").GetUInt32(), Is.EqualTo(23u));
		}

		[Test]
		public void ZeroAndNegativeCreditDeliveriesKeepAcceptedCreditFalse()
		{
			var window = new AcceptedDeliveryWindow();
			window.Add(17, "ore", 100, 0, 5);
			window.Add(17, "ore", 100, -40, 5);
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Raw(0, 5, "g", "p",
				"resource-accepted", window.RollupEvidence()));
			var evidence = row.RootElement.GetProperty("evidence");
			Assert.That(evidence.GetProperty("accepted_credit").GetBoolean(), Is.False);
			Assert.That(evidence.GetProperty("deliveries").GetInt32(), Is.EqualTo(2));
			Assert.That(evidence.GetProperty("credited").GetInt64(), Is.Zero);
			Assert.That(evidence.GetProperty("gross_value").GetInt64(), Is.EqualTo(-40));
		}

		[Test]
		public void DistinctKeyOverflowFoldsIntoTotalsAndReportsTruncation()
		{
			var window = new AcceptedDeliveryWindow();
			for (var i = 0; i < AcceptedDeliveryWindow.MaximumKeys; i++)
				Assert.That(window.Add((uint)i, "ore", 1, 1, 7), Is.True);
			Assert.That(window.Add(9999, "ore", 1, 5, 7), Is.False);
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Raw(0, 7, "g", "p",
				"resource-accepted", window.RollupEvidence()));
			var evidence = row.RootElement.GetProperty("evidence");
			Assert.That(evidence.GetProperty("deliveries").GetInt32(),
				Is.EqualTo(AcceptedDeliveryWindow.MaximumKeys + 1));
			Assert.That(evidence.GetProperty("credited").GetInt64(),
				Is.EqualTo(AcceptedDeliveryWindow.MaximumKeys + 5));
			Assert.That(evidence.GetProperty("refineries").GetArrayLength(),
				Is.EqualTo(AcceptedDeliveryWindow.MaximumKeys));
			Assert.That(evidence.GetProperty("folded_deliveries").GetInt32(), Is.EqualTo(1));
		}

		[Test]
		public void AcceptedFloodStaysOneRecordPerWindowUnderWriterBound()
		{
			// Regression pin for the 200000-record raw bound: the retired per-event shape
			// emitted one record per delivery (3/player/tick serialized to 271808 records
			// over 45001 ticks). The rollup emits at most one record per observe window.
			var window = new AcceptedDeliveryWindow();
			const int ticks = 45001;
			var deliveries = 0;
			var records = 0;
			for (var tick = 0; tick < ticks; tick++)
			{
				for (var n = 0; n < 3; n++)
				{
					window.Add(17 + (uint)(n % 2), "ore", 100, 100, tick);
					deliveries++;
				}
				if (window.HasPending)
				{
					records++;
					window.RollupEvidence();
					window.Clear();
				}
			}
			Assert.That(deliveries, Is.EqualTo(3 * ticks));
			Assert.That(records, Is.EqualTo(ticks));
			Assert.That(records, Is.LessThan(200000));
		}

		[Test]
		public void ClearedAcceptedWindowStartsAFreshRollup()
		{
			var window = new AcceptedDeliveryWindow();
			window.Add(17, "ore", 1, 1, 10);
			window.RollupEvidence();
			window.Clear();
			Assert.That(window.HasPending, Is.False);
			window.Add(20, "gems", 2, 4, 11);
			Assert.That(window.FirstTick, Is.EqualTo(11));
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Raw(1, window.FirstTick, "g", "p",
				"resource-accepted", window.RollupEvidence()));
			var evidence = row.RootElement.GetProperty("evidence");
			Assert.That(evidence.GetProperty("deliveries").GetInt32(), Is.EqualTo(1));
			Assert.That(evidence.GetProperty("credited").GetInt64(), Is.EqualTo(4));
			Assert.That(evidence.GetProperty("last_tick").GetInt32(), Is.EqualTo(11));
			Assert.That(evidence.GetProperty("refineries").GetArrayLength(), Is.EqualTo(1));
		}

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

		[Test]
		public void CleanCoverageEmitsTheAcceptedCompletenessTuple()
		{
			// Mirrors AiEconomyHealthRecorder.Identity: dropped=0 requires all three
			// completeness flags. Activation/census/terminal ordering was validated by
			// admitted run C (game uid 25060589-136e-46b8-9d78-4c4ad7828688, 2026-10-12).
			// The consumer UNKNOWNs unless the whole tuple holds: dropped=0 on every
			// record, queues_complete=true on pulses, complete=true on the end record.
			var state = new AiEconomyHealthState();
			for (var t = 0; t <= 50; t++)
				state.Observe(t, Array.Empty<EconomyQueueObservation>());
			Assert.That(state.Complete, Is.True);
			using var healthStream = new MemoryStream();
			using var rawStream = new MemoryStream();
			using var health = new AiEconomyHealthLogWriter(healthStream);
			using var raw = new AiEconomyHealthLogWriter(rawStream);
			var dropped = state.Complete && health.Complete && raw.Complete ? 0 : 1;
			using var pulse = JsonDocument.Parse(AiEconomyHealthSchema.Pulse(
				new EconomyHealthIdentity(0, 0, "pulse", "g", "p", "map", "td_nod", "hard", true, dropped),
				true, state.Complete, Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100));
			var root = pulse.RootElement;
			Assert.That(root.GetProperty("dropped").GetInt32(), Is.Zero);
			Assert.That(root.GetProperty("queues_complete").GetBoolean(), Is.True);
			Assert.That(root.GetProperty("profile_supported").GetBoolean(), Is.True);
			using var end = JsonDocument.Parse(AiEconomyHealthSchema.End(
				new EconomyHealthIdentity(1, 50, "end", "g", "p", "map", "td_nod", "hard", true, dropped),
				state.Complete && health.Complete && raw.Complete));
			Assert.That(end.RootElement.GetProperty("dropped").GetInt32(), Is.Zero);
			Assert.That(end.RootElement.GetProperty("complete").GetBoolean(), Is.True);
		}

		[Test]
		public void WriterFailureFlipsTheIdentityTupleFailClosed()
		{
			// Representative forced failure: a writer-level failure poisons the emitted
			// identity even while observation state stays complete. Census overflow and
			// unclassified transitions are pinned in AiEconomyHealthStateTest and
			// AiEconomyQueueEvidenceTest respectively; all share the one-way latch.
			var state = new AiEconomyHealthState();
			state.Observe(0, Array.Empty<EconomyQueueObservation>());
			Assert.That(state.Complete, Is.True);
			using var brokenStream = new MemoryStream();
			using var broken = new AiEconomyHealthLogWriter(brokenStream, byteLimit: 4);
			Assert.That(broken.TryWrite("12345"), Is.False);
			Assert.That(broken.Complete, Is.False);
			var dropped = state.Complete && broken.Complete ? 0 : 1;
			using var end = JsonDocument.Parse(AiEconomyHealthSchema.End(
				new EconomyHealthIdentity(1, 50, "end", "g", "p", "map", "td_nod", "hard", true, dropped),
				state.Complete && broken.Complete));
			Assert.That(end.RootElement.GetProperty("dropped").GetInt32(), Is.EqualTo(1));
			Assert.That(end.RootElement.GetProperty("complete").GetBoolean(), Is.False);
		}

	internal static void WriteCliFixture(string name, IEnumerable<string> rows)
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
