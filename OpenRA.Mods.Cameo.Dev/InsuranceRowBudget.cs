using System;
using System.Text;

namespace OpenRA.Mods.Cameo.Traits
{
	// SPK-01 (DEV only): the pure row-budget rule of InsuranceTelemetryDev. No World, no I/O, so it is pinned by tests.
	// A data row is admitted only if, with it queued, the file still has room for one terminal row per participant
	// (an "end" row, or a "dropped" row when rows were refused). Terminal rows are never refused by this rule.
	internal static class InsuranceRowBudget
	{
		// Bytes held back per terminal row. A real insurance row is about 350 ASCII bytes.
		internal const long TerminalRowBytes = 1024;

		// Bytes of one queued line, including its newline.
		internal static long RowBytes(string row) => Encoding.UTF8.GetByteCount(row) + 1;

		// committedRows / committedBytes: rows and bytes already queued, including the pending batch.
		internal static bool DataRowFits(int committedRows, long committedBytes, long rowBytes, int participants,
			int recordLimit, long byteLimit)
		{
			if (rowBytes > AiEconomyHealthLogWriter.MaximumLineBytes)
				return false;

			var terminals = (long)participants;
			return committedRows + 1 + terminals <= recordLimit
				&& committedBytes + rowBytes + terminals * TerminalRowBytes <= byteLimit;
		}
	}
}
