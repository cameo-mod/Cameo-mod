#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Text;

namespace OpenRA.Mods.Cameo.Traits
{
	// PERF-SPK-01 (DEV only): the queued twin of AiEconomyHealthLogWriter. The sim thread validates each row and appends it to
	// a batch; Flush hands the batch to AiLogWriteQueue through AiLogFileAppender (enqueue-only, never blocks). The file is
	// created exclusively once here (the only I/O on the sim thread, at load). A refused hand-off, or a worker failure seen
	// before the terminal row, is sticky: Finish then writes no terminal row, so the file stays incomplete (reader: UNKNOWN).
	internal sealed class AiQueuedJsonlWriter
	{
		static readonly Encoding Utf8 = new UTF8Encoding(false, true);
		readonly AiLogFileAppender appender;
		readonly StringBuilder batch = new();
		readonly long byteLimit;
		readonly int recordLimit;
		readonly long queueFailuresAtStart;
		long bytes;
		int records;
		int maximumRecordBytes;
		bool failed;
		bool closed;

		internal AiQueuedJsonlWriter(string directory, string fileName, string writer,
			long byteLimit = AiEconomyHealthLogWriter.MaximumFileBytes, int recordLimit = AiEconomyHealthLogWriter.MaximumRecords)
		{
			if (byteLimit < 1 || recordLimit < 1)
				throw new ArgumentOutOfRangeException(nameof(byteLimit));
			this.byteLimit = byteLimit;
			this.recordLimit = recordLimit;

			// Exclusive create: existing evidence is never overwritten (same rule as the synchronous writer).
			using (new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }
			appender = new AiLogFileAppender(fileName, writer);
			queueFailuresAtStart = AiLogWriteQueue.Shared.Failed;
		}

		internal bool Failed => failed;
		internal int Records => records;
		internal long Bytes => bytes;
		internal int MaximumRecordBytes => maximumRecordBytes;

		// Sim thread: validation and a StringBuilder append only. No I/O.
		internal bool TryWrite(string json)
		{
			if (failed || closed)
				return false;

			try
			{
				if (json == null || json.Length > AiEconomyHealthLogWriter.MaximumLineBytes || json.Contains('\n') || json.Contains('\r'))
					return Fail();

				var count = Utf8.GetByteCount(json) + 1;
				if (count > AiEconomyHealthLogWriter.MaximumLineBytes || count > byteLimit - bytes || records >= recordLimit)
					return Fail();

				batch.Append(json).Append('\n');
				bytes += count;
				records++;
				maximumRecordBytes = Math.Max(maximumRecordBytes, count);
				return true;
			}
			catch (Exception e) when (e is EncoderFallbackException or ArgumentException)
			{
				return Fail();
			}
		}

		// Sim thread: one non-blocking hand-off of the pending batch. A refusal is sticky.
		internal bool Flush(int tick, bool finalRecord = false)
		{
			if (batch.Length == 0)
				return !failed;

			if (!failed && appender.TryAppend(batch.ToString(), tick, out _, finalRecord) != AiLogAppendResult.Appended)
				failed = true;

			batch.Clear();
			return !failed;
		}

		// GameOver only (may wait, bounded, as the sibling DEV writers do): hands over the last batch, drains the shared queue,
		// and writes the terminal row only if no failure has been seen by then. Returns false when the file is incomplete.
		internal bool Finish(int tick, string terminalRow)
		{
			if (closed)
				return false;

			Flush(tick);
			AiLogWriteQueue.FlushSharedForGameEnd();
			if (failed || AiLogWriteQueue.Shared.Failed != queueFailuresAtStart)
			{
				failed = true;
				Close(tick);
				return false;
			}

			var written = TryWrite(terminalRow) && Flush(tick, finalRecord: true);
			AiLogWriteQueue.FlushSharedForGameEnd();
			if (!written || AiLogWriteQueue.Shared.Failed != queueFailuresAtStart)
				failed = true;

			closed = true;
			return !failed;
		}

		// Disposal path (not GameOver): hands over complete data rows only, never waits, never writes a terminal row.
		internal void Close(int tick)
		{
			if (closed)
				return;

			Flush(tick);
			closed = true;
		}

		bool Fail()
		{
			failed = true;
			return false;
		}
	}
}
