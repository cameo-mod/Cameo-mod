using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// SPK-01: the pure row-budget rule behind InsuranceTelemetryDev (no World, no I/O).
	[TestFixture]
	public class InsuranceRowBudgetTest
	{
		[Test]
		public void RowBytesCountsUtf8PlusNewline()
		{
			Assert.That(InsuranceRowBudget.RowBytes("abc"), Is.EqualTo(4));
			Assert.That(InsuranceRowBudget.RowBytes("é"), Is.EqualTo(3));
		}

		[Test]
		public void RecordBoundKeepsOneTerminalRowPerParticipant()
		{
			// Two participants: rows including this one plus two terminals must fit in 10 records.
			Assert.That(InsuranceRowBudget.DataRowFits(7, 0, 100, 2, 10, long.MaxValue), Is.True);
			Assert.That(InsuranceRowBudget.DataRowFits(8, 0, 100, 2, 10, long.MaxValue), Is.False);
		}

		[Test]
		public void ByteBoundKeepsTerminalReserve()
		{
			// Two participants reserve 2 * 1024 bytes for terminals; limit 10000.
			Assert.That(InsuranceRowBudget.DataRowFits(0, 7850, 100, 2, int.MaxValue, 10000), Is.True);
			Assert.That(InsuranceRowBudget.DataRowFits(0, 7952, 100, 2, int.MaxValue, 10000), Is.False);
		}

		[Test]
		public void OverlongRowIsRefused()
		{
			Assert.That(InsuranceRowBudget.DataRowFits(0, 0, AiEconomyHealthLogWriter.MaximumLineBytes + 1, 1,
				int.MaxValue, long.MaxValue), Is.False);
		}

		[Test]
		public void NoParticipantsStillRefusesPastTheLimit()
		{
			Assert.That(InsuranceRowBudget.DataRowFits(9, 0, 10, 0, 10, long.MaxValue), Is.True);
			Assert.That(InsuranceRowBudget.DataRowFits(10, 0, 10, 0, 10, long.MaxValue), Is.False);
		}
	}
}
