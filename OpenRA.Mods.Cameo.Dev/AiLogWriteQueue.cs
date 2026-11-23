#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace OpenRA.Mods.Cameo.Traits
{
	// E3-3 (DEV-only). One FIFO queue and one background writer thread. Writers enqueue a finished string on
	// the sim thread (O(1), never blocks). The named mutex, CreateDirectory, open/write/close and retry/backoff
	// run on the worker only. Single consumer, so record order per file equals enqueue order. A refused enqueue
	// (batch count or text budget full) is dropped and counted; the sim thread is never blocked.
	internal sealed class AiLogWriteRequest
	{
		public readonly string FilePath;
		public readonly string MutexName;
		public readonly string Text;
		public readonly int Tick;
		public readonly string Writer;

		// FinalRecord: the writer's GameOver/Disposing (or end-of-game) record. It is exempt from the text budget.
		public readonly bool FinalRecord;

		public AiLogWriteRequest(string filePath, string mutexName, string text, int tick, string writer = "unknown", bool finalRecord = false)
		{
			FilePath = filePath;
			MutexName = mutexName;
			Text = text;
			Tick = tick;
			Writer = writer;
			FinalRecord = finalRecord;
		}
	}

	// Wall-clock milliseconds per step of one sink call (probe fields).
	internal struct AiLogWriteTiming
	{
		public double MutexMs;
		public double CreateDirMs;
		public double OpenMs;
		public double WriteMs;
		public double CloseMs;
		public double TotalMs;
	}

	internal delegate AiLogAppendResult AiLogWriteSink(AiLogWriteRequest request, out AiLogWriteTiming timing);

	internal sealed class AiLogWriteQueue
	{
		// Bounds: 256 batches, and 16 Mi UTF-16 chars of pending text (about 32 MB).
		public const int DefaultCapacity = 256;
		public const int DefaultMaxPendingChars = 16 * 1024 * 1024;
		public const int MaxAttempts = 8;
		public static readonly TimeSpan StallThreshold = TimeSpan.FromMilliseconds(50);
		public static readonly TimeSpan GameEndFlushTimeout = TimeSpan.FromSeconds(5);

		static readonly object SharedLock = new object();
		static AiLogWriteQueue shared;

		readonly BlockingCollection<AiLogWriteRequest> queue;
		readonly AiLogWriteSink sink;
		readonly Action<string> log;
		readonly long maxPendingChars;
		long pending;
		long pendingChars;
		long dropped;
		long failed;
		long flushTimeouts;

		public AiLogWriteQueue(int capacity, long maxPendingChars, AiLogWriteSink sink, Action<string> log)
		{
			this.sink = sink;
			this.log = log;
			this.maxPendingChars = maxPendingChars;
			queue = new BlockingCollection<AiLogWriteRequest>(new ConcurrentQueue<AiLogWriteRequest>(), capacity);
			var worker = new Thread(Run) { IsBackground = true, Name = "CameoAiLogWriter" };
			worker.Start();
		}

		// Process-wide queue shared by the six DEV writers (one FIFO, one worker). Created on first append only.
		// The ProcessExit drain is registered here, once, together with the queue.
		public static AiLogWriteQueue Shared
		{
			get
			{
				lock (SharedLock)
				{
					if (shared == null)
					{
						shared = new AiLogWriteQueue(DefaultCapacity, DefaultMaxPendingChars, FileSink,
							msg => Log.Write("debug", msg));
						AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
					}

					return shared;
				}
			}
		}

		// Cumulative for the process: refused enqueues (never written).
		public long Dropped => Interlocked.Read(ref dropped);

		// Cumulative for the process: worker-side terminal failures (after retries).
		public long Failed => Interlocked.Read(ref failed);

		// Cumulative for the process: game-end flushes that hit the timeout (only the first is logged).
		public long FlushTimeouts => Interlocked.Read(ref flushTimeouts);

		public long Pending => Interlocked.Read(ref pending);

		// Never blocks. False means the batch was refused and counted as dropped, and a [LOG-IO-DROP] line names the
		// writer, the size and the reason. Final records skip the text budget; the queue count bound still applies.
		public bool TryEnqueue(AiLogWriteRequest request)
		{
			var chars = request.Text.Length;
			if (Interlocked.Add(ref pendingChars, chars) > maxPendingChars && !request.FinalRecord)
			{
				Interlocked.Add(ref pendingChars, -chars);
				return Refuse(request, "budget");
			}

			Interlocked.Increment(ref pending);
			if (queue.TryAdd(request))
				return true;

			Interlocked.Decrement(ref pending);
			Interlocked.Add(ref pendingChars, -chars);
			return Refuse(request, queue.IsAddingCompleted ? "closed" : "full");
		}

		bool Refuse(AiLogWriteRequest request, string reason)
		{
			Interlocked.Increment(ref dropped);
			log?.Invoke("[LOG-IO-DROP] writer=" + request.Writer + " chars=" + request.Text.Length + " reason=" + reason);
			return false;
		}

		// Waits at most timeout for every accepted batch to be written. Returns true when nothing is left.
		public bool FlushForGameEnd(TimeSpan timeout, out long remaining)
		{
			var clock = Stopwatch.StartNew();
			while (Pending > 0 && clock.Elapsed < timeout)
				Thread.Sleep(5);

			remaining = Pending;
			return remaining == 0;
		}

		// Game-end path (called from the writers' IGameOver). Bounded wait, then logs the outcome.
		// Timeout, drops and failures are never silent. The timeout line is logged once per process;
		// later timeouts are counted (FlushTimeouts) but not logged again.
		public bool FlushAndReport(TimeSpan timeout)
		{
			var ok = FlushForGameEnd(timeout, out var remaining);
			if (!ok)
			{
				if (Interlocked.Increment(ref flushTimeouts) == 1)
					log?.Invoke("[LOG-IO-FLUSH-TIMEOUT] unwritten=" + remaining + " dropped=" + Dropped + " failed=" + Failed);
			}
			else if (Dropped > 0 || Failed > 0)
				log?.Invoke("[LOG-IO-END] dropped=" + Dropped + " failed=" + Failed);

			return ok;
		}

		// Process exit (ProcessExit of the shared queue only). Same bound and report as the game-end flush, so
		// lines from producers without their own game-end flush (Engagement, Context) are written before exit.
		public bool DrainForProcessExit() => FlushAndReport(GameEndFlushTimeout);

		// Writers' game-end flush. Reads the existing shared queue only: it never creates the queue or its thread,
		// so a run with logging off starts no writer thread.
		public static void FlushSharedForGameEnd()
		{
			AiLogWriteQueue existing;
			lock (SharedLock)
				existing = shared;

			existing?.FlushAndReport(GameEndFlushTimeout);
		}

		static void OnProcessExit(object sender, EventArgs e)
		{
			AiLogWriteQueue existing;
			lock (SharedLock)
				existing = shared;

			existing?.DrainForProcessExit();
		}

		void Run()
		{
			foreach (var request in queue.GetConsumingEnumerable())
			{
				try
				{
					Write(request);
				}
				catch (Exception e)
				{
					Interlocked.Increment(ref failed);
					log?.Invoke("[LOG-IO-FAIL] file=" + Path.GetFileName(request.FilePath) + " tick=" + request.Tick
						+ " exception=" + e.GetType().Name);
				}
				finally
				{
					Interlocked.Add(ref pendingChars, -request.Text.Length);
					Interlocked.Decrement(ref pending);
				}
			}
		}

		void Write(AiLogWriteRequest request)
		{
			var result = AiLogAppendResult.Failed;
			for (var attempt = 1; attempt <= MaxAttempts; attempt++)
			{
				result = sink(request, out var timing);
				if (timing.TotalMs > StallThreshold.TotalMilliseconds)
					log?.Invoke(StallLine(request, timing));

				if (result != AiLogAppendResult.RetryableFailure || attempt == MaxAttempts)
					break;

				// Backoff on the worker only, between attempts: 10, 20, 40, 80, 160, 300, 300 ms (910 ms in all).
				Thread.Sleep(Math.Min(1 << Math.Min(attempt - 1, 5), 30) * 10);
			}

			if (result != AiLogAppendResult.Appended)
			{
				Interlocked.Increment(ref failed);
				log?.Invoke("[LOG-IO-FAIL] file=" + Path.GetFileName(request.FilePath) + " tick=" + request.Tick
					+ " result=" + result);
			}
		}

		static string StallLine(AiLogWriteRequest r, AiLogWriteTiming t)
		{
			return "[LOG-IO-STALL] file=" + Path.GetFileName(r.FilePath)
				+ " bytes=" + Encoding.UTF8.GetByteCount(r.Text)
				+ " tick=" + r.Tick
				+ " ms=" + Ms(t.TotalMs)
				+ " mutex_ms=" + Ms(t.MutexMs)
				+ " create_dir_ms=" + Ms(t.CreateDirMs)
				+ " open_ms=" + Ms(t.OpenMs)
				+ " write_ms=" + Ms(t.WriteMs)
				+ " close_ms=" + Ms(t.CloseMs);
		}

		static string Ms(double ms) => ms.ToString("F1", CultureInfo.InvariantCulture);

		static double Lap(Stopwatch lap)
		{
			var ms = lap.Elapsed.TotalMilliseconds;
			lap.Restart();
			return ms;
		}

		// Real sink. Same protocol as the former AiLogFileAppender.Append (named mutex with 100 ms wait,
		// CreateDirectory, append UTF-8 without BOM), with each step timed. Internal for the I/O tests.
		internal static AiLogAppendResult FileSink(AiLogWriteRequest r, out AiLogWriteTiming t)
		{
			var timing = default(AiLogWriteTiming);
			var total = Stopwatch.StartNew();
			var lap = Stopwatch.StartNew();
			Mutex mutex = null;
			var owned = false;
			try
			{
				mutex = new Mutex(false, r.MutexName);
				try { owned = mutex.WaitOne(TimeSpan.FromMilliseconds(100)); }
				catch (AbandonedMutexException) { owned = true; }

				timing.MutexMs = Lap(lap);
				if (!owned)
					return AiLogAppendResult.RetryableFailure;

				Directory.CreateDirectory(Path.GetDirectoryName(r.FilePath));
				timing.CreateDirMs = Lap(lap);

				using (var stream = new FileStream(r.FilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
				{
					timing.OpenMs = Lap(lap);
					var bytes = new UTF8Encoding(false).GetBytes(r.Text);
					stream.Write(bytes, 0, bytes.Length);
					timing.WriteMs = Lap(lap);
				}

				timing.CloseMs = Lap(lap);
				return AiLogAppendResult.Appended;
			}
			catch
			{
				return AiLogAppendResult.Failed;
			}
			finally
			{
				if (owned)
				{
					try { mutex.ReleaseMutex(); }
					catch { }
				}

				mutex?.Dispose();
				timing.TotalMs = total.Elapsed.TotalMilliseconds;
				t = timing;
			}
		}
	}

	// [GAMEOVER-MS] (E3 H-4, Opus _05): DEV only, no behaviour change. Each end-of-game writer reports how long its handler
	// took: enqueue = time inside AiLogFileAppender.TryAppend, build = the rest of the handler (text build and the game-end
	// flush included), so build + enqueue = the whole handler.
	internal static class AiGameOverProbe
	{
		internal static Action<string> Sink = msg => Log.Write("debug", msg);

		internal static string Line(string writer, TimeSpan build, TimeSpan enqueue, int chars)
		{
			return "[GAMEOVER-MS] writer=" + writer
				+ " build=" + build.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)
				+ " enqueue=" + enqueue.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)
				+ " chars=" + chars.ToString(CultureInfo.InvariantCulture);
		}

		internal static void Report(string writer, TimeSpan whole, TimeSpan enqueue, int chars)
		{
			Sink(Line(writer, whole - enqueue, enqueue, chars));
		}
	}
}
