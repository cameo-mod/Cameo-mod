using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class InsuranceTelemetryTest
	{
		[Test]
		public void ActualCreditsAccumulateSeparatelyFromIncomeAndStartingCash()
		{
			var ledger = new InsuranceLedger(0, 0);
			Assert.That(ledger.Credit(20, 5000, 5020, 20, 5020), Is.EqualTo(20));
			ledger.Observe(100, 5100);
			Assert.That(ledger.Total, Is.EqualTo(20));
			Assert.That(ledger.Income, Is.EqualTo(100));
			// Refund adds cash but does not add Earned or insurance.
			ledger.Observe(100, 5200);
			Assert.That(ledger.Income, Is.EqualTo(100));
		}

		[TestCase(1, 0, 0)]
		[TestCase(0, int.MaxValue, int.MaxValue)]
		public void LateOrSaturatedBaselineIsUnknown(int tick, int earned, int cash)
		{
			var ledger = new InsuranceLedger(tick, earned);
			ledger.Observe(earned, cash);
			Assert.That(ledger.Income, Is.Null);
		}

		[Test]
		public void CapRecordsActualPartialCreditAndMakesIncomeShareUnknown()
		{
			var ledger = new InsuranceLedger(0, 0);
			Assert.That(ledger.Credit(20, int.MaxValue - 3L, int.MaxValue, 20, int.MaxValue), Is.EqualTo(3));
			Assert.That(ledger.Total, Is.EqualTo(3));
			Assert.That(ledger.Income, Is.Null);
		}

		[Test]
		public void NegativeOrImpossibleCreditAndDecreasingEarnedFailClosed()
		{
			var ledger = new InsuranceLedger(0, 10);
			ledger.Observe(9, 100);
			Assert.That(ledger.Income, Is.Null);
			var invalid = new InsuranceLedger(0, 0);
			invalid.Credit(1, 0, 2, 2, 2);
			Assert.That(invalid.Total, Is.Zero);
			Assert.That(invalid.Complete, Is.False);
		}

		[Test]
		public void SchemaHasNoPlayerNameAndSharesAreExplicitAccounting()
		{
			var ledger = new InsuranceLedger(0, 0);
			ledger.Credit(20, 5000, 5020, 100, 5020);
			using var row = JsonDocument.Parse(InsuranceSchema.Row("match", 3, "hard", "td_nod", 2, 100,
				"end", "none", 0, 0, ledger, true));
			Assert.That(row.RootElement.GetProperty("income_share").GetDouble(), Is.EqualTo(0.2));
			Assert.That(row.RootElement.TryGetProperty("player", out _), Is.False);
			Assert.That(row.RootElement.GetProperty("slot").GetInt32(), Is.EqualTo(3));
			Assert.That(InsuranceSchema.Difficulty("arbitrary-human-name"), Is.EqualTo("unknown"));
			Assert.That(InsuranceSchema.Difficulty("classic"), Is.EqualTo("hard"));
		}

		[Test]
		public void LegacyUsesRealInheritedSettingsWithoutEnginePinChanges()
		{
			Assert.That(typeof(InsuranceCashTricklerInfo).BaseType, Is.EqualTo(typeof(CashTricklerInfo)));
			Assert.That(typeof(InsuranceCashTrickler).BaseType, Is.EqualTo(typeof(CashTrickler)));
			var info = new InsuranceCashTricklerInfo();
			Assert.That(info.Interval, Is.EqualTo(new CashTricklerInfo().Interval));
			Assert.That(info.Create(null), Is.TypeOf<InsuranceCashTrickler>());
		}

		[TestCase(1)]
		[TestCase(750)]
		public void LegacySubclassRetainsActualEngineSyncHash(int ticks)
		{
			var standardInfo = new CashTricklerInfo();
			var observedInfo = new InsuranceCashTricklerInfo();
			typeof(CashTricklerInfo).GetField("InitialDelay").SetValue(standardInfo, ticks);
			typeof(CashTricklerInfo).GetField("InitialDelay").SetValue(observedInfo, ticks);
			var standard = new CashTrickler(standardInfo);
			var observed = new InsuranceCashTrickler(observedInfo);
			var hash = typeof(Sync).GetMethod("Hash", BindingFlags.Static | BindingFlags.NonPublic);
			Assert.That(hash.Invoke(null, new object[] { observed }), Is.EqualTo(hash.Invoke(null, new object[] { standard })));
			Assert.That(hash.Invoke(null, new object[] { observed }), Is.EqualTo(ticks));
		}

		[Test]
		public void ActualSchemaFixtureRoundTripsThroughBoundedWriter()
		{
			var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "insurance-fixture");
			Directory.CreateDirectory(directory);
			var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".jsonl");
			using (var writer = new AiEconomyHealthLogWriter(path))
			{
				var ledger = new InsuranceLedger(0, 0);
				Assert.That(writer.TryWrite(InsuranceSchema.Row("fixture", 1, "hard", "td_nod", 0, 0,
					"start", "none", 0, 0, ledger, true)), Is.True);
				ledger.Credit(20, 1000, 1020, 20, 1020);
				Assert.That(writer.TryWrite(InsuranceSchema.Row("fixture", 1, "hard", "td_nod", 1, 1,
					"payout", "dynamic_rescue", 20, 20, ledger, true)), Is.True);
				ledger.Observe(100, 1100);
				Assert.That(writer.TryWrite(InsuranceSchema.Row("fixture", 1, "hard", "td_nod", 2, 10,
					"end", "none", 0, 0, ledger, true)), Is.True);
			}
			File.WriteAllText(Path.Combine(TestContext.CurrentContext.WorkDirectory, "insurance-fixture-path.txt"), path);
		}
	}
}
