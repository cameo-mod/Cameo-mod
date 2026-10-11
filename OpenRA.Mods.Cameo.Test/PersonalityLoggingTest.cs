using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class PersonalityLoggingTest
	{
		static IReadOnlyDictionary<string, int> Conditions(string personality) =>
			new Dictionary<string, int> { ["personality-" + personality] = 1 };

		static JsonDocument Serialize(string seat, AiMatchLogRecorder recorder)
		{
			var b = new StringBuilder();
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "seat", seat, true);
			AiMatchLogWriter.AppendPersonalitySummary(b, recorder);
			AiMatchLogWriter.AppendTimeline(b, recorder?.PersonalityTimeline);
			b.Append('}');
			return JsonDocument.Parse(b.ToString());
		}

		[Test]
		public void RecorderCoversEveryDefaultControllerPersonality()
		{
			Assert.That(new AiMatchLogRecorderInfo().PersonalityConditions,
				Is.EquivalentTo(new BotPersonalityControllerInfo().Conditions));
		}

		[TestCase("rush")]
		[TestCase("turtle")]
		[TestCase("tech")]
		[TestCase("expansion")]
		[TestCase("steamroller")]
		[TestCase("guerrilla")]
		public void FinalSummaryRetainsObservedPersonalityAfterLiveConditionsRevoke(string personality)
		{
			var recorder = new AiMatchLogRecorder(new AiMatchLogRecorderInfo());
			recorder.ObservePersonalityConditions(0, Conditions(personality));
			recorder.ObservePersonalityConditions(100, new Dictionary<string, int>());
			Assert.That(recorder.CurrentPersonality, Is.Empty, "live consumers must not see a revoked condition");
			using var row = Serialize("seat_3", recorder);
			Assert.That(row.RootElement.GetProperty("personality").GetString(), Is.EqualTo(personality));
			Assert.That(row.RootElement.GetProperty("personality_timeline")[0].GetProperty("personality").GetString(), Is.EqualTo(personality));
			Assert.That(row.RootElement.GetProperty("personality_switches").GetInt32(), Is.Zero);
		}

		[Test]
		public void DistinctSeatHistoriesRemainPairedWhenTheLoserSwitchesToGuerrilla()
		{
			var loser = new AiMatchLogRecorder(new AiMatchLogRecorderInfo());
			var winner = new AiMatchLogRecorder(new AiMatchLogRecorderInfo());
			loser.ObservePersonalityConditions(0, Conditions("rush"));
			winner.ObservePersonalityConditions(0, Conditions("tech"));
			loser.ObservePersonalityConditions(500, Conditions("guerrilla"));
			winner.ObservePersonalityConditions(700, Conditions("steamroller"));
			loser.ObservePersonalityConditions(900, new Dictionary<string, int>());
			foreach (var (seat, recorder, expected, ticks) in new[]
			{
				("seat_4", winner, new[] { "tech", "steamroller" }, new[] { 0, 700 }),
				("seat_3", loser, new[] { "rush", "guerrilla" }, new[] { 0, 500 })
			})
			{
				using var row = Serialize(seat, recorder);
				Assert.That(row.RootElement.GetProperty("seat").GetString(), Is.EqualTo(seat));
				Assert.That(row.RootElement.GetProperty("personality").GetString(), Is.EqualTo(expected[^1]));
				var entries = row.RootElement.GetProperty("personality_timeline").EnumerateArray().ToArray();
				Assert.That(entries.Select(e => e.GetProperty("personality").GetString()), Is.EqualTo(expected));
				Assert.That(entries.Select(e => e.GetProperty("tick").GetInt32()), Is.EqualTo(ticks));
				Assert.That(row.RootElement.GetProperty("personality_switches").GetInt32(), Is.EqualTo(1));
			}
		}

		[Test]
		public void MissingPersonalityEvidenceIsNotCopiedFromAnotherSeat()
		{
			var known = new AiMatchLogRecorder(new AiMatchLogRecorderInfo());
			known.ObservePersonalityConditions(0, Conditions("turtle"));
			foreach (var recorder in new[] { new AiMatchLogRecorder(new AiMatchLogRecorderInfo()), null })
			{
				using var row = Serialize("seat_4", recorder);
				Assert.That(row.RootElement.GetProperty("personality").GetString(), Is.Empty);
				Assert.That(row.RootElement.GetProperty("personality_timeline").GetArrayLength(), Is.Zero);
			}
		}
	}
}
