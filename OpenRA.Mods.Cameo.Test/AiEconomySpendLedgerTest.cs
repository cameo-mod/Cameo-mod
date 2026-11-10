using System;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AiEconomySpendLedgerTest
	{
		static readonly SpendItemSample[] None = Array.Empty<SpendItemSample>();

		static SpendItemSample Sample(object key, int total, int remaining) => new(key, total, remaining);

		[Test]
		public void StagedPaymentsAreAttributedUntilCompletion()
		{
			var ledger = new AiEconomySpendLedger();
			var item = new object();
			ledger.Observe(new[] { Sample(item, 100, 100) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(0));
			ledger.Observe(new[] { Sample(item, 100, 70) }, None);
			ledger.Observe(new[] { Sample(item, 100, 0) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(100));
			Assert.That(ledger.Complete, Is.True);
		}

		[Test]
		public void FirstSightOfAPartlyPaidItemCountsTheShownPaidPortion()
		{
			var ledger = new AiEconomySpendLedger();
			ledger.Observe(new[] { Sample(new object(), 100, 60) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(40));
		}

		[Test]
		public void CompletionBetweenObservationsSettlesTheUnseenRest()
		{
			var ledger = new AiEconomySpendLedger();
			var item = new object();
			ledger.Observe(new[] { Sample(item, 100, 100) }, None);
			ledger.Observe(None, new[] { Sample(item, 100, 0) });
			Assert.That(ledger.TakePulse(), Is.EqualTo(100));
		}

		[Test]
		public void CancellationKeepsOnlyThePaidPortionAndNeverReducesIt()
		{
			var ledger = new AiEconomySpendLedger();
			var item = new object();
			ledger.Observe(new[] { Sample(item, 100, 100) }, None);
			ledger.Observe(new[] { Sample(item, 100, 60) }, None);
			ledger.Observe(None, new[] { Sample(item, 100, 60) });
			Assert.That(ledger.TakePulse(), Is.EqualTo(40));
			Assert.That(ledger.Complete, Is.True);
		}

		[Test]
		public void LoweredPaidAmountFailsClosedAndKeepsTheTotal()
		{
			var ledger = new AiEconomySpendLedger();
			var item = new object();
			ledger.Observe(new[] { Sample(item, 100, 50) }, None);
			ledger.Observe(new[] { Sample(item, 100, 70) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(50));
			Assert.That(ledger.Complete, Is.False);
		}

		[Test]
		public void PulseWindowsAreDisjoint()
		{
			var ledger = new AiEconomySpendLedger();
			var item = new object();
			ledger.Observe(new[] { Sample(item, 100, 100) }, None);
			ledger.Observe(new[] { Sample(item, 100, 90) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(10));
			ledger.Observe(new[] { Sample(item, 100, 80) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(10));
			Assert.That(ledger.TakePulse(), Is.EqualTo(0));
		}

		[Test]
		public void TrackingOverflowFailsClosed()
		{
			var ledger = new AiEconomySpendLedger();
			var items = Enumerable.Range(0, AiEconomySpendLedger.MaximumTrackedItems + 1)
				.Select(_ => Sample(new object(), 10, 10)).ToArray();
			ledger.Observe(items, None);
			Assert.That(ledger.Complete, Is.False);
		}

		[Test]
		public void MarkIncompleteIsOneWay()
		{
			var ledger = new AiEconomySpendLedger();
			ledger.MarkIncomplete();
			ledger.Observe(new[] { Sample(new object(), 10, 10) }, None);
			Assert.That(ledger.Complete, Is.False);
		}

		[Test]
		public void CancelledOreItemKeepsTheLedgerCompleteAndTheTotal()
		{
			var ledger = new AiEconomySpendLedger();
			var item = new object();
			ledger.Observe(new[] { Sample(item, 100, 100) }, None);
			ledger.Observe(new[] { Sample(item, 100, 40) }, None);
			// Engine CancelProduction gives the ore portion back into RemainingCost before removal.
			ledger.Observe(None, new[] { Sample(item, 100, 100) });
			Assert.That(ledger.TakePulse(), Is.EqualTo(60));
			Assert.That(ledger.Complete, Is.True);
		}

		[Test]
		public void UnitAndBuildingQueueItemsAreAttributedTogether()
		{
			var ledger = new AiEconomySpendLedger();
			var building = new object();
			var unit = new object();
			ledger.Observe(new[] { Sample(building, 100, 50), Sample(unit, 200, 150) }, None);
			Assert.That(ledger.TakePulse(), Is.EqualTo(100));
			ledger.Observe(None, new[] { Sample(unit, 200, 0) });
			Assert.That(ledger.TakePulse(), Is.EqualTo(150));
			Assert.That(ledger.Complete, Is.True);
		}

		[Test]
		public void EvidenceResidualIsNetSpentDeltaMinusAttributedProduction()
		{
			var evidence = AiEconomySpendLedger.Evidence(50, 100, 120, 500, true);
			using var doc = JsonDocument.Parse(JsonSerializer.Serialize(evidence));
			var root = doc.RootElement;
			Assert.That(root.GetProperty("window_from_tick").GetInt32(), Is.EqualTo(50));
			Assert.That(root.GetProperty("window_to_tick").GetInt32(), Is.EqualTo(100));
			Assert.That(root.GetProperty("production_attributed").GetInt64(), Is.EqualTo(120));
			Assert.That(root.GetProperty("net_spent_delta").GetInt64(), Is.EqualTo(500));
			Assert.That(root.GetProperty("residual").GetInt64(), Is.EqualTo(380));
			Assert.That(root.GetProperty("ledger_complete").GetBoolean(), Is.True);
			Assert.That(root.GetProperty("unobserved").GetArrayLength(), Is.EqualTo(5));
		}
	}
}
