using System.IO;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class EconomyAcceptedTickTest
	{
		[Test]
		public void FloodAboveOldRecordCapPreservesEveryCreditAndWriterCompleteness()
		{
			using var stream = new MemoryStream();
			using var writer = new AiEconomyHealthLogWriter(stream);
			var aggregate = new EconomyAcceptedTick();
			long total = 0;
			var sequence = 0;
			// 270,006 callbacks alone previously exceeded 200,000 records.
			for (var tick = 0; tick <= 45000; tick++)
			{
				Drain(tick, false);
				for (var n = 0; n < 6; n++)
					Assert.That(aggregate.Add(tick, (uint)n, "ore", 10, 100), Is.True);
				Assert.That(aggregate.TryTakeBefore(tick, false, out _, out _), Is.False);
			}
			Drain(45000, true);
			Assert.That(aggregate.TryTakeBefore(45000, true, out _, out _), Is.False);
			Assert.That(writer.Records, Is.EqualTo(45001));
			Assert.That(writer.Complete, Is.True);
			Assert.That(total, Is.EqualTo(27000600L));
			Assert.That(writer.Bytes, Is.LessThan(AiEconomyHealthLogWriter.MaximumFileBytes));

			void Drain(int tick, bool terminal)
			{
				if (!aggregate.TryTakeBefore(tick, terminal, out var emittedTick, out var evidence))
					return;
				var json = AiEconomyHealthSchema.Raw(sequence++, emittedTick, "flood", "hard",
					"resource-accepted-tick", evidence);
				using var row = JsonDocument.Parse(json);
				var value = row.RootElement.GetProperty("evidence");
				total += value.GetProperty("credited_value").GetInt64();
				Assert.That(value.GetProperty("event_count").GetInt64(), Is.EqualTo(6));
				Assert.That(value.GetProperty("sample_count").GetInt32(), Is.EqualTo(EconomyAcceptedTick.MaximumSamples));
				Assert.That(value.GetProperty("detail_truncated").GetBoolean(), Is.True);
				Assert.That(writer.TryWrite(json), Is.True);
			}
		}

		[Test]
		public void MixedValuesAndCappedTextKeepExactTotalsAndUnknownSender()
		{
			var aggregate = new EconomyAcceptedTick();
			Assert.That(aggregate.Add(10, 1, new string('<', 10000), 3, int.MaxValue), Is.True);
			Assert.That(aggregate.Add(10, 2, "ore", 4, int.MaxValue), Is.True);
			Assert.That(aggregate.Add(10, 3, "ore", 5, -7), Is.True);
			Assert.That(aggregate.Add(10, 4, "ore", 6, 0), Is.True);
			Assert.That(aggregate.Add(11, 5, "ore", 1, 1), Is.False);
			Assert.That(aggregate.TryTakeBefore(11, false, out var tick, out var evidence), Is.True);
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Raw(0, tick, "g", "p", "resource-accepted-tick", evidence));
			var value = row.RootElement.GetProperty("evidence");
			Assert.That(tick, Is.EqualTo(10));
			Assert.That(value.GetProperty("value").GetInt64(), Is.EqualTo(2L * int.MaxValue - 7));
			Assert.That(value.GetProperty("credited_value").GetInt64(), Is.EqualTo(2L * int.MaxValue));
			Assert.That(value.GetProperty("accepted_event_count").GetInt64(), Is.EqualTo(2));
			Assert.That(value.GetProperty("count").GetInt64(), Is.EqualTo(18));
			Assert.That(value.GetProperty("harvester").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(value.GetProperty("samples")[0].GetProperty("resource_type").GetString().Length,
				Is.EqualTo(EconomyAcceptedTick.MaximumTypeCharacters));
			Assert.That(value.GetProperty("detail_truncated").GetBoolean(), Is.True);
			Assert.That(aggregate.Add(11, 5, "ore", 1, 1), Is.True);
		}
	}
}
