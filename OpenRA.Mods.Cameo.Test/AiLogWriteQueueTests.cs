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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// E3-3 checks (a), (b), (c), (e) and the successor's D-1/D-4 tests (g), (h), (i), (j), (k). Check (d) (old vs new
	// build hashes) and (f) (exclusion test) need a build and are not run here.
	[TestFixture]
	public sealed class AiLogWriteQueueTests
	{
		static AiLogWriteRequest Req(int tick) => new AiLogWriteRequest("unused.log", "unused-mutex", "line " + tick + "\n", tick);

		static AiLogWriteSink Recording(ConcurrentQueue<int> written, ManualResetEventSlim gate = null, int sleepMs = 0) =>
			(AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				gate?.Wait();
				if (sleepMs > 0)
					Thread.Sleep(sleepMs);
				written.Enqueue(r.Tick);
				t = default;
				return AiLogAppendResult.Appended;
			};

		// D-3: the first TryEnqueue, BlockingCollection and Stopwatch use JIT here, not inside the timed loops.
		static void WarmUp()
		{
			var warm = new AiLogWriteQueue(8, long.MaxValue, Recording(new ConcurrentQueue<int>()), null);
			for (var i = 0; i < 3; i++)
				warm.TryEnqueue(Req(i));

			warm.FlushForGameEnd(TimeSpan.FromSeconds(5), out _);
		}

		// (a) A sink that sleeps 2 s per append: enqueue returns in under 100 ms, and write order == enqueue order.
		[Test]
		public void SlowSinkDoesNotBlockEnqueueAndKeepsOrder()
		{
			WarmUp();
			var written = new ConcurrentQueue<int>();
			var queue = new AiLogWriteQueue(64, long.MaxValue, Recording(written, null, 2000), null);

			var worst = 0.0;
			for (var i = 0; i < 3; i++)
			{
				var timer = Stopwatch.StartNew();
				Assert.That(queue.TryEnqueue(Req(i)), Is.True);
				worst = Math.Max(worst, timer.Elapsed.TotalMilliseconds);
			}

			Assert.That(worst, Is.LessThan(100.0), "enqueue must not wait on the 2 s sink");
			Assert.That(queue.FlushForGameEnd(TimeSpan.FromSeconds(20), out var remaining), Is.True);
			Assert.That(remaining, Is.EqualTo(0));
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 0, 1, 2 }));
		}

		// (b) Queue full: refusals are counted, and the enqueueing thread never blocks.
		[Test]
		public void FullQueueDropsAndCountsWithoutBlocking()
		{
			WarmUp();
			var gate = new ManualResetEventSlim(false);
			var written = new ConcurrentQueue<int>();
			const int Capacity = 2;
			var queue = new AiLogWriteQueue(Capacity, long.MaxValue, Recording(written, gate), null);

			var accepted = 0;
			var worst = 0.0;
			for (var i = 0; i < 10; i++)
			{
				var timer = Stopwatch.StartNew();
				if (queue.TryEnqueue(Req(i)))
					accepted++;
				worst = Math.Max(worst, timer.Elapsed.TotalMilliseconds);
			}

			gate.Set();
			Assert.That(worst, Is.LessThan(100.0), "a full queue must refuse, never block");

			// The worker may already hold one batch, so accepted is capacity or capacity + 1.
			Assert.That(accepted, Is.InRange(Capacity, Capacity + 1));
			Assert.That(queue.Dropped, Is.EqualTo(10 - accepted));
			Assert.That(queue.FlushForGameEnd(TimeSpan.FromSeconds(5), out _), Is.True);
			Assert.That(written.Count, Is.EqualTo(accepted));
		}

		// (b, text budget) The pending-text budget refuses the second batch while the first is still held.
		[Test]
		public void TextBudgetRefusesAndCounts()
		{
			var gate = new ManualResetEventSlim(false);
			var written = new ConcurrentQueue<int>();
			var queue = new AiLogWriteQueue(64, 10, Recording(written, gate), null);

			// "line 0\n" is 7 chars: the first fits (7 <= 10), the second does not (14 > 10).
			Assert.That(queue.TryEnqueue(Req(0)), Is.True);
			Assert.That(queue.TryEnqueue(Req(1)), Is.False);
			Assert.That(queue.Dropped, Is.EqualTo(1));

			gate.Set();
			Assert.That(queue.FlushForGameEnd(TimeSpan.FromSeconds(5), out _), Is.True);
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 0 }));
		}

		// (c) Game-end flush writes every enqueued line; a stuck sink hits the timeout, which is logged once per
		// process (D-2): a second timeout is counted but logs nothing new.
		[Test]
		public void GameEndFlushWritesEverythingAndReportsTimeout()
		{
			var logs = new ConcurrentQueue<string>();
			var written = new ConcurrentQueue<int>();
			var queue = new AiLogWriteQueue(64, long.MaxValue, Recording(written), logs.Enqueue);
			for (var i = 0; i < 5; i++)
				Assert.That(queue.TryEnqueue(Req(i)), Is.True);

			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
			Assert.That(logs.Count, Is.EqualTo(0), "a clean flush logs nothing when nothing was dropped or failed");

			var gate = new ManualResetEventSlim(false);
			var stuckLogs = new ConcurrentQueue<string>();
			var stuckWritten = new ConcurrentQueue<int>();
			var stuck = new AiLogWriteQueue(64, long.MaxValue, Recording(stuckWritten, gate), stuckLogs.Enqueue);
			Assert.That(stuck.TryEnqueue(Req(0)), Is.True);
			Assert.That(stuck.TryEnqueue(Req(1)), Is.True);

			Assert.That(stuck.FlushAndReport(TimeSpan.FromMilliseconds(200)), Is.False);
			Assert.That(stuck.FlushAndReport(TimeSpan.FromMilliseconds(200)), Is.False);
			Assert.That(stuck.FlushTimeouts, Is.EqualTo(2));
			Assert.That(stuckLogs.ToArray(), Is.EqualTo(new[] { "[LOG-IO-FLUSH-TIMEOUT] unwritten=2 dropped=0 failed=0" }));

			gate.Set();
			Assert.That(stuck.FlushForGameEnd(TimeSpan.FromSeconds(5), out _), Is.True);
			Assert.That(stuckWritten.ToArray(), Is.EqualTo(new[] { 0, 1 }));
		}

		// (e) Probe: a write above 50 ms is logged with tick and ms; a write at 49.9 ms is not.
		[Test]
		public void StallProbeFiresAboveFiftyMsOnly()
		{
			var logs = new ConcurrentQueue<string>();
			AiLogWriteSink sink = (AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				var ms = r.Tick == 0 ? 49.9 : r.Tick == 1 ? 50.1 : 60.0;
				t = new AiLogWriteTiming { MutexMs = 1, CreateDirMs = 1, OpenMs = 1, WriteMs = 1, CloseMs = 1, TotalMs = ms };
				return AiLogAppendResult.Appended;
			};

			var queue = new AiLogWriteQueue(64, long.MaxValue, sink, logs.Enqueue);
			for (var i = 0; i < 3; i++)
				Assert.That(queue.TryEnqueue(Req(i)), Is.True);

			Assert.That(queue.FlushForGameEnd(TimeSpan.FromSeconds(5), out _), Is.True);
			var lines = logs.ToArray();
			Assert.That(lines.Length, Is.EqualTo(2));
			Assert.That(lines.Any(l => l.StartsWith("[LOG-IO-STALL] file=unused.log", StringComparison.Ordinal) && l.Contains(" tick=1 ") && l.Contains(" ms=50.1 ")), Is.True);
			Assert.That(lines.Any(l => l.Contains(" tick=2 ") && l.Contains(" ms=60.0 ")), Is.True);
			Assert.That(lines.Any(l => l.Contains(" tick=0 ")), Is.False, "49.9 ms must not trigger the probe");
		}

		// (g) Retry path: the sink fails once with RetryableFailure, then appends. Written once, nothing counted.
		[Test]
		public void RetryThenAppendWritesOnceAndCountsNothing()
		{
			var logs = new ConcurrentQueue<string>();
			var written = new ConcurrentQueue<int>();
			var calls = 0;
			AiLogWriteSink sink = (AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				t = default;
				if (Interlocked.Increment(ref calls) == 1)
					return AiLogAppendResult.RetryableFailure;

				written.Enqueue(r.Tick);
				return AiLogAppendResult.Appended;
			};

			var queue = new AiLogWriteQueue(64, long.MaxValue, sink, logs.Enqueue);
			Assert.That(queue.TryEnqueue(Req(0)), Is.True);
			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(5)), Is.True);

			Assert.That(calls, Is.EqualTo(2));
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 0 }));
			Assert.That(queue.Failed, Is.EqualTo(0));
			Assert.That(logs.Count, Is.EqualTo(0));
		}

		// (h) Exhaustion: eight RetryableFailure results fail one batch (one [LOG-IO-FAIL] line), and the next batch
		// is still written. The worker sleeps 910 ms between the eight attempts (no sleep after the last one).
		[Test]
		public void EightRetryableFailuresFailOneBatchAndTheQueueContinues()
		{
			var logs = new ConcurrentQueue<string>();
			var written = new ConcurrentQueue<int>();
			var callsForTickZero = 0;
			AiLogWriteSink sink = (AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				t = default;
				if (r.Tick != 0)
				{
					written.Enqueue(r.Tick);
					return AiLogAppendResult.Appended;
				}

				Interlocked.Increment(ref callsForTickZero);
				return AiLogAppendResult.RetryableFailure;
			};

			var queue = new AiLogWriteQueue(64, long.MaxValue, sink, logs.Enqueue);
			Assert.That(queue.TryEnqueue(Req(0)), Is.True);
			Assert.That(queue.TryEnqueue(Req(1)), Is.True);
			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(10)), Is.True);

			Assert.That(callsForTickZero, Is.EqualTo(AiLogWriteQueue.MaxAttempts));
			Assert.That(queue.Failed, Is.EqualTo(1));
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 1 }));
			Assert.That(logs.ToArray(), Is.EqualTo(new[]
			{
				"[LOG-IO-FAIL] file=unused.log tick=0 result=RetryableFailure",
				"[LOG-IO-END] dropped=0 failed=1",
			}));
		}

		// (i) The sink throws: the worker survives, the batch is counted as failed with its exception type, and
		// the next batch is still written.
		[Test]
		public void SinkThrowsIsLoggedAndWorkerSurvives()
		{
			var logs = new ConcurrentQueue<string>();
			var written = new ConcurrentQueue<int>();
			AiLogWriteSink sink = (AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				t = default;
				if (r.Tick == 0)
					throw new InvalidOperationException("boom");

				written.Enqueue(r.Tick);
				return AiLogAppendResult.Appended;
			};

			var queue = new AiLogWriteQueue(64, long.MaxValue, sink, logs.Enqueue);
			Assert.That(queue.TryEnqueue(Req(0)), Is.True);
			Assert.That(queue.TryEnqueue(Req(1)), Is.True);
			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(5)), Is.True);

			Assert.That(queue.Failed, Is.EqualTo(1));
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 1 }));
			Assert.That(logs.ToArray(), Is.EqualTo(new[]
			{
				"[LOG-IO-FAIL] file=unused.log tick=0 exception=InvalidOperationException",
				"[LOG-IO-END] dropped=0 failed=1",
			}));
		}

		// (j) Real FileSink on a temp directory: a missing folder is created, two batches are appended in order, and
		// the bytes are UTF-8 without BOM (the file equals the concatenation exactly).
		[Test]
		public void RealFileSinkAppendsInOrderWithoutBom()
		{
			var dir = Path.Combine(Path.GetTempPath(), "e33-sink-" + Guid.NewGuid().ToString("N"));
			try
			{
				var path = Path.Combine(dir, "missing-folder", "log.jsonl");
				var mutexName = "CameoE33SinkTest-" + Guid.NewGuid().ToString("N");
				var first = AiLogWriteQueue.FileSink(new AiLogWriteRequest(path, mutexName, "a\n", 1), out _);
				var second = AiLogWriteQueue.FileSink(new AiLogWriteRequest(path, mutexName, "b\n", 2), out _);

				Assert.That(first, Is.EqualTo(AiLogAppendResult.Appended));
				Assert.That(second, Is.EqualTo(AiLogAppendResult.Appended));
				Assert.That(File.ReadAllBytes(path), Is.EqualTo(new UTF8Encoding(false).GetBytes("a\nb\n")));
			}
			finally
			{
				if (Directory.Exists(dir))
					Directory.Delete(dir, true);
			}
		}

		// (j, mutex) A mutex held by another thread: FileSink waits its 100 ms and returns RetryableFailure without
		// creating the folder or file.
		[Test]
		public void RealFileSinkRefusesWhenMutexIsHeldElsewhere()
		{
			var dir = Path.Combine(Path.GetTempPath(), "e33-held-" + Guid.NewGuid().ToString("N"));
			var mutexName = "CameoE33HeldTest-" + Guid.NewGuid().ToString("N");
			var path = Path.Combine(dir, "log.jsonl");
			try
			{
				using (var held = new Mutex(false, mutexName))
				{
					Assert.That(held.WaitOne(0), Is.True);
					AiLogAppendResult result = default;
					AiLogWriteTiming timing = default;
					var worker = new Thread(() => result = AiLogWriteQueue.FileSink(new AiLogWriteRequest(path, mutexName, "x\n", 3), out timing));
					worker.Start();
					worker.Join();
					held.ReleaseMutex();

					Assert.That(result, Is.EqualTo(AiLogAppendResult.RetryableFailure));
					Assert.That(timing.MutexMs, Is.GreaterThanOrEqualTo(90.0));
				}

				Assert.That(File.Exists(path), Is.False);
			}
			finally
			{
				if (Directory.Exists(dir))
					Directory.Delete(dir, true);
			}
		}

		// (k) D-1: lines enqueued after the last game-end flush are still written by the ProcessExit drain.
		[Test]
		public void ProcessExitDrainWritesLinesQueuedAfterAFlush()
		{
			var logs = new ConcurrentQueue<string>();
			var written = new ConcurrentQueue<int>();
			var queue = new AiLogWriteQueue(64, long.MaxValue, Recording(written), logs.Enqueue);
			Assert.That(queue.TryEnqueue(Req(0)), Is.True);
			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(5)), Is.True);

			Assert.That(queue.TryEnqueue(Req(1)), Is.True);
			Assert.That(queue.DrainForProcessExit(), Is.True);
			Assert.That(written.ToArray(), Is.EqualTo(new[] { 0, 1 }));
			Assert.That(queue.Pending, Is.EqualTo(0));
		}

		// (l) SMOKE_02 H-1: a final record larger than the text budget is exempt from the budget. It is written
		// whole, and after the earlier batch (FIFO).
		[Test]
		public void OversizedFinalRecordIsWrittenWholeAndInOrder()
		{
			var logs = new ConcurrentQueue<string>();
			var texts = new ConcurrentQueue<string>();
			AiLogWriteSink sink = (AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				t = default;
				texts.Enqueue(r.Text);
				return AiLogAppendResult.Appended;
			};

			var queue = new AiLogWriteQueue(64, 10, sink, logs.Enqueue);
			var small = "line 0\n";
			var big = new string('f', 40) + "\n";
			Assert.That(queue.TryEnqueue(new AiLogWriteRequest("unused.log", "unused-mutex", small, 0, "TestWriter", false)), Is.True);
			Assert.That(queue.TryEnqueue(new AiLogWriteRequest("unused.log", "unused-mutex", big, 1, "TestWriter", true)), Is.True);
			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(5)), Is.True);

			Assert.That(texts.ToArray(), Is.EqualTo(new[] { small, big }));
			Assert.That(queue.Dropped, Is.EqualTo(0));
			Assert.That(logs.Count, Is.EqualTo(0), "a final record within the queue bound is not a drop");
		}

		// (m) SMOKE_02 H-1: a mid-game batch over the text budget is refused, and the refusal is logged with its
		// writer, size and reason.
		[Test]
		public void OversizedMidGameBatchIsRefusedWithDropLine()
		{
			var logs = new ConcurrentQueue<string>();
			var written = new ConcurrentQueue<int>();
			var queue = new AiLogWriteQueue(64, 10, Recording(written), logs.Enqueue);
			var big = new string('m', 40) + "\n";

			Assert.That(queue.TryEnqueue(new AiLogWriteRequest("unused.log", "unused-mutex", big, 3, "AiSituationLogWriterDev", false)), Is.False);
			Assert.That(queue.Dropped, Is.EqualTo(1));
			Assert.That(logs.ToArray(), Is.EqualTo(new[] { "[LOG-IO-DROP] writer=AiSituationLogWriterDev chars=41 reason=budget" }));
		}

		// (n) _05 (Opus, Sonnet): every end-of-game writer's final record bypasses the text budget, and the same
		// batch refused mid-game logs a DROP line naming that writer.
		[TestCase("AiMatchLogWriterDev")]
		[TestCase("AiPlacementLogWriterDev")]
		[TestCase("AiMissionLogWriterDev")]
		[TestCase("AiSituationLogWriterDev")]
		[TestCase("AiEngagementLogWriterDev")]
		public void EveryEndWriterFinalRecordBypassesBudgetAndMidGameIsDropped(string writer)
		{
			var logs = new ConcurrentQueue<string>();
			var texts = new ConcurrentQueue<string>();
			AiLogWriteSink sink = (AiLogWriteRequest r, out AiLogWriteTiming t) =>
			{
				t = default;
				texts.Enqueue(r.Text);
				return AiLogAppendResult.Appended;
			};

			var queue = new AiLogWriteQueue(64, 10, sink, logs.Enqueue);
			var big = new string('e', 40) + "\n";

			Assert.That(queue.TryEnqueue(new AiLogWriteRequest("unused.log", "unused-mutex", big, 5, writer, true)), Is.True);
			Assert.That(queue.FlushAndReport(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(texts.ToArray(), Is.EqualTo(new[] { big }));
			Assert.That(logs.Count, Is.EqualTo(0));

			Assert.That(queue.TryEnqueue(new AiLogWriteRequest("unused.log", "unused-mutex", big, 6, writer, false)), Is.False);
			Assert.That(logs.ToArray(), Is.EqualTo(new[] { "[LOG-IO-DROP] writer=" + writer + " chars=41 reason=budget" }));
		}

		// (n) The end-of-game call sites: the writers need a World that the test project cannot build, so each
		// writer's append call is checked in its source (this file sits at OpenRA.Mods.Cameo.Test/).
		static string DevSource(string file, [System.Runtime.CompilerServices.CallerFilePath] string testPath = "")
		{
			var dir = Path.Combine(Path.GetDirectoryName(testPath), "..", "OpenRA.Mods.Cameo.Dev");
			return File.ReadAllText(Path.Combine(dir, file));
		}

		[Test]
		public void EndOfGameWritersPassFinalRecordAtTheirCallSites()
		{
			Assert.That(DevSource("AiMatchLogWriterDev.cs"), Does.Contain("appender.TryAppend(pendingText, worldTick, out nextAttemptTick, finalRecord: true);"));
			Assert.That(DevSource("AiPlacementLogWriterDev.cs"), Does.Contain("appender.TryAppend(text, worldTick, out nextAttemptTick, finalRecord: true);"));
			Assert.That(DevSource("AiEngagementLogWriterDev.cs"), Does.Contain("appender.TryAppend(text, tick, out nextAttemptTick, finalRecord: true);"));

			var mission = DevSource("AiMissionLogWriterDev.cs");
			Assert.That(mission, Does.Contain("TryFlush(world.WorldTick, finalRecord: true);"));
			Assert.That(mission, Does.Contain("TryFlush(self.World.WorldTick, finalRecord: true);"));

			var situation = DevSource("AiSituationLogWriterDev.cs");
			Assert.That(situation, Does.Contain("appender.TryAppend(pendingText, worldTick, out nextAttemptTick, finalRecord: true);"));
			Assert.That(situation, Does.Not.Contain("finalRecord)"), "every Situation append is final, including the ITick path");

			foreach (var writer in new[] { "AiMatchLogWriterDev", "AiPlacementLogWriterDev", "AiMissionLogWriterDev", "AiSituationLogWriterDev", "AiEngagementLogWriterDev" })
				Assert.That(DevSource(writer + ".cs"), Does.Contain("AiGameOverProbe.Report(nameof(" + writer + "),"), writer + " emits [GAMEOVER-MS]");
		}

		// (o) _05 (Opus): the [GAMEOVER-MS] line is emitted in the documented shape, through the Sink.
		[Test]
		public void GameOverProbeEmitsOneLinePerWriter()
		{
			var lines = new List<string>();
			var saved = AiGameOverProbe.Sink;
			AiGameOverProbe.Sink = lines.Add;
			try
			{
				AiGameOverProbe.Report("AiMatchLogWriterDev", TimeSpan.FromMilliseconds(12.5), TimeSpan.FromMilliseconds(3), 4096);
			}
			finally
			{
				AiGameOverProbe.Sink = saved;
			}

			Assert.That(lines, Is.EqualTo(new[] { "[GAMEOVER-MS] writer=AiMatchLogWriterDev build=9.5 enqueue=3.0 chars=4096" }));
		}
	}
}
