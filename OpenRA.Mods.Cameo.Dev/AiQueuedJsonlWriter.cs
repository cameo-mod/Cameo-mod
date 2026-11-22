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
	// PERF-SPK-01 (DEV only): the queued twin of AiEconomyHealthLogWriter. The sim thread admits each row (AiJsonlRowRules, the same
	// rule as the synchronous writer) and appends it to a batch; Flush hands the batch to AiLogWriteQueue through AiLogFileAppender
	// (enqueue-only, never blocks). The file is created exclusively once, at construction, in Platform.SupportDir/Logs: the only
	// I/O on the sim thread, at load. Failure is sticky. A refused hand-off, a drain that times out with batches pending, or a
	// worker failure seen before a terminal row marks the stream failed, so its Complete reads false and the terminal row says so.
	// The failure count is process-wide (AiLogWriteQueue.Failed): a worker failure on any other queued DEV file also marks this
	// stream failed. That is fail-closed and spurious at worst; it is never a silent complete claim.
	internal sealed class AiQueuedJsonlWriter
	{
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

		internal AiQueuedJsonlWriter(string fileName, string writer,
			long byteLimit = AiEconomyHealthLogWriter.MaximumFileBytes, int recordLimit = AiEconomyHealthLogWriter.MaximumRecords)
		{
			if (byteLimit < 1 || recordLimit < 1)
				throw new ArgumentOutOfRangeException(nameof(byteLimit));

			this.byteLimit = byteLimit;
			this.recordLimit = recordLimit;

			// The directory is fixed here, so the exclusive create and the appender can never name different files.
			var directory = Path.Combine(Platform.SupportDir, "Logs");
			Directory.CreateDirectory(directory);

			// Exclusive create: existing evidence is never overwritten (same rule as the synchronous writer).
			using (new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }
			appender = new AiLogFileAppender(fileName, writer);
			queueFailuresAtStart = AiLogWriteQueue.Shared.Failed;
		}

		internal bool Failed => failed;
		internal bool Complete => !failed && !closed;
		internal int Records => records;
		internal long Bytes => bytes;
		internal int MaximumRecordBytes => maximumRecordBytes;

		// Sim thread: admission and a StringBuilder append only. No I/O.
		internal bool TryWrite(string json)
		{
			if (failed || closed)
				return false;

			try
			{
				if (!AiJsonlRowRules.TryCount(json, bytes, records, byteLimit, recordLimit, out var count))
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

		// GameOver only (may wait, bounded by AiLogWriteQueue.GameEndFlushTimeout, as the sibling DEV writers do). BeginTerminal hands
		// over the rows written so far and drains the shared queue. It marks the stream failed when a refusal happened, a worker failure
		// was seen, or batches are still pending after the bound. The terminal rows are written only after it, and read Complete then.
		internal bool BeginTerminal(int tick)
		{
			Flush(tick);
			DrainOrFail();
			return !failed;
		}

		// After the terminal rows are handed over: hands them over and drains again, under the same rules.
		internal bool EndTerminal(int tick)
		{
			Flush(tick, finalRecord: true);
			DrainOrFail();
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

		void DrainOrFail()
		{
			var drained = AiLogWriteQueue.Shared.FlushForGameEnd(AiLogWriteQueue.GameEndFlushTimeout, out var remaining);
			if (!drained || remaining > 0 || AiLogWriteQueue.Shared.Failed != queueFailuresAtStart)
				failed = true;
		}

		bool Fail()
		{
			failed = true;
			return false;
		}
	}
}
