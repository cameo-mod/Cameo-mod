using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>PLAYTEST-FIX-B1: IMPL02 L1 (reported-dead derivation), L3 (test-only constructor) and L2 (neutral blocker cache persistence).</summary>
	[TestFixture]
	public class BotPassiveIssue02WaveTest
	{
		static readonly StalemateBreakerPolicyCA.TimingCA Short = new(1000, 750, 1500, 30000);

		static StalemateBreakerWaveCA FiveMemberWave()
		{
			var members = Enumerable.Range(1, 5).Select(i => new StalemateWaveMemberCA((uint)i, 1, 100)).ToArray();
			return new StalemateBreakerWaveCA(0, members, Short, 5, 500, 1000, 1);
		}

		static StalemateBreakerWaveCA RoundTrip(StalemateBreakerWaveCA wave)
		{
			var nodes = MiniYaml.FromString(wave.Save().WriteToString(), "b1-wave-test");
			return StalemateBreakerWaveCA.Load(new MiniYaml("", nodes));
		}

		[Test]
		public void DeadActorIdsListConfirmedDeathsInAscendingOrderAndSurviveSaveAndLoad()
		{
			var wave = FiveMemberWave();
			Assert.That(wave.DeadCount, Is.EqualTo(0));
			Assert.That(wave.DeadActorIds, Is.Empty);

			Assert.That(wave.RecordDeath(4, 1, true), Is.True);
			Assert.That(wave.RecordDeath(2, 1, true), Is.True);
			Assert.That(wave.RecordDeath(4, 1, true), Is.True);
			Assert.That(wave.IsActive, Is.True);
			Assert.That(wave.DeadCount, Is.EqualTo(2));
			Assert.That(wave.DeadActorIds.ToArray(), Is.EqualTo(new uint[] { 2, 4 }));

			var loaded = RoundTrip(wave);
			Assert.That(loaded.DeadCount, Is.EqualTo(2));
			Assert.That(loaded.DeadActorIds.ToArray(), Is.EqualTo(new uint[] { 2, 4 }));
			Assert.That(loaded.ConfirmedDeadValue, Is.EqualTo(200));
		}

		[Test]
		public void ReportedDeadSetDerivedFromTheWaveAfterALoadRereportsNothing()
		{
			var original = FiveMemberWave();
			original.RecordDeath(2, 1, true);
			original.RecordDeath(4, 1, true);
			var loaded = RoundTrip(original);

			// The manager's reported-dead set is empty after a load; the derivation step must fill it from the wave.
			var reported = new HashSet<uint>();
			if (reported.Count < loaded.DeadCount)
				foreach (var id in loaded.DeadActorIds)
					reported.Add(id);

			Assert.That(reported.OrderBy(i => i).ToArray(), Is.EqualTo(new uint[] { 2, 4 }));

			// The per-member loop then skips both ids, so RecordDeath is not called again and WindowDeaths is not raised.
			var windowDeaths = 0;
			foreach (var m in loaded.Members)
			{
				if (reported.Contains(m.ActorId))
					continue;
				if (m.ActorId == 5)
				{
					reported.Add(m.ActorId);
					windowDeaths++;
					loaded.RecordDeath(m.ActorId, m.Generation, true);
				}
			}

			Assert.That(windowDeaths, Is.EqualTo(1));
			Assert.That(loaded.DeadActorIds.ToArray(), Is.EqualTo(new uint[] { 2, 4, 5 }));

			// A straight-through set is already a superset of the wave's dead set: the guard adds nothing.
			Assert.That(reported.Count < loaded.DeadCount, Is.False);
		}

		[Test]
		public void TheTwoArgumentWaveConstructorIsNotPublic()
		{
			var publicTwoArgument = typeof(StalemateBreakerWaveCA).GetConstructors().Where(c => c.GetParameters().Length == 2).ToArray();
			Assert.That(publicTwoArgument, Is.Empty);

			// The test assembly still reaches it through InternalsVisibleTo (StalemateSeatOwnerTest).
			var wave = new StalemateBreakerWaveCA(100, new[] { new StalemateWaveMemberCA(1, 1, 100) });
			Assert.That(wave.StartTick, Is.EqualTo(100));
		}
	}

	[TestFixture]
	public class StalemateNeutralCacheTest
	{
		[Test]
		public void CacheRoundTripsExactlyIncludingLayersAndEmptyEntries()
		{
			var cache = new Dictionary<uint, CPos[]>
			{
				[9] = new[] { new CPos(0, 0) },
				[3] = Array.Empty<CPos>(),
				[7] = new[] { new CPos(1, 2), new CPos(-3, 4, 1), new CPos(40, 41) }
			};

			var saved = StalemateNeutralCacheCA.Save(cache);
			Assert.That(saved.Select(n => n.Key).ToArray(), Is.EqualTo(new[] { "3", "7", "9" }));

			var nodes = MiniYaml.FromString(saved.WriteToString(), "b1-neutral-test");
			var loaded = StalemateNeutralCacheCA.Load(new MiniYaml("", nodes));
			Assert.That(loaded.Keys.OrderBy(k => k).ToArray(), Is.EqualTo(new uint[] { 3, 7, 9 }));
			foreach (var kv in cache)
				Assert.That(loaded[kv.Key], Is.EqualTo(kv.Value), "actor " + kv.Key);

			Assert.That(loaded[7][1].X, Is.EqualTo(-3));
			Assert.That(loaded[7][1].Layer, Is.EqualTo((byte)1));
		}

		[Test]
		public void MissingNodeLoadsAnEmptyCacheAndDuplicateActorsAreRejected()
		{
			Assert.That(StalemateNeutralCacheCA.Load(null), Is.Empty);

			var duplicate = new MiniYaml("", new[] { new MiniYamlNode("7", "1"), new MiniYamlNode("7", "2") });
			Assert.Throws<InvalidDataException>(() => StalemateNeutralCacheCA.Load(duplicate));
		}
	}

	/// <summary>PLAYTEST-FIX-B1: IMPL02 L6 (diagnostic writer). NonParallelizable: one test installs a static worker-start seam.</summary>
	[TestFixture, NonParallelizable]
	public class BotPassiveIssue02WriterTest
	{
		string directory;
		string path;

		[SetUp]
		public void SetUp()
		{
			directory = Path.Combine(Path.GetTempPath(), "botpassive-b1-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			path = Path.Combine(directory, "seat0.jsonl");
		}

		[TearDown]
		public void TearDown()
		{
			BotPassiveDiagnosticWriter.TestStartWorker = null;
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		BotPassiveDiagnosticWriter Create()
		{
			BotPassiveDiagnosticWriter.TryCreate(true, path, "game-1", "seat0", "normal", out var writer);
			Assert.That(writer.Enabled, Is.True);
			return writer;
		}

		static BotPassiveHealthSnapshot Health() => new()
		{
			Tick = 9750, IdleCombatUnits = 3, UnsquaddedCombatUnits = 4, HomeThirdUnits = 10, CombatUnits = 30,
			StuckSquads = 1, RetreatsWindow = 2, KillsWindow = 5, Urgency = "Normal", UrgencyLegacy = "Normal"
		};

		static BotPassiveSquadEventSnapshot SquadEvent(string reason) => new()
		{
			Tick = 10, SquadId = "sq-1", Generation = 2, Event = "retreat_start", MemberActorId = null, Size = 5,
			Value = 4000, StateFrom = "attack", StateTo = "flee", TargetType = "fact", TargetCell = "10,12",
			Reason = reason, LeaseOwner = null
		};

		static BotPassiveUnitPositionSnapshot Unit(long id) => new()
		{
			ActorId = id, ActorType = "e1", X = (int)id, Y = 2, SquadId = null, Activity = "Idle", Live = true, InWorld = true
		};

		static void WaitFor(Func<bool> condition, string what)
		{
			Assert.That(SpinWait.SpinUntil(condition, TimeSpan.FromSeconds(20)), Is.True, "timed out waiting for " + what);
		}

		/// <summary>Reads the last JSONL record, retrying while the writer still holds the file open for write.</summary>
		JsonElement LastRecord()
		{
			var watch = Stopwatch.StartNew();
			while (true)
			{
				try
				{
					using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
					var length = (int)Math.Min(stream.Length, 4096);
					stream.Seek(-length, SeekOrigin.End);
					var tail = new byte[length];
					var read = 0;
					while (read < length)
					{
						var n = stream.Read(tail, read, length - read);
						if (n <= 0)
							break;
						read += n;
					}

					var text = Encoding.UTF8.GetString(tail, 0, read).TrimEnd('\n');
					return JsonDocument.Parse(text[(text.LastIndexOf('\n') + 1)..]).RootElement.Clone();
				}
				catch (IOException)
				{
					Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(20)), "file stayed locked");
					Thread.Sleep(10);
				}
			}
		}

		/// <summary>Keeps the writer queue far below its cap: the queue holds accepted - written records and written &gt;= DataBytes / maxLine.</summary>
		static void Pace(BotPassiveDiagnosticWriter writer, long maxLine)
		{
			var watch = Stopwatch.StartNew();
			while (writer.AcceptedRecords - writer.DataBytes / maxLine > 3000)
			{
				Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(60)), "writer stalled");
				Thread.Sleep(1);
			}
		}

		[Test]
		public void BlankIdentityIsAFailedWriterNotAnException()
		{
			BotPassiveDiagnosticWriter writer = null;
			Assert.DoesNotThrow(() => BotPassiveDiagnosticWriter.TryCreate(true, path, "", "seat0", "normal", out writer));
			Assert.That(writer.Enabled, Is.False);
			Assert.That(writer.MissingCount, Is.GreaterThan(0));
			Assert.That(writer.IsComplete, Is.False);
			Assert.That(File.Exists(path), Is.False);

			BotPassiveDiagnosticWriter disabled = null;
			Assert.DoesNotThrow(() => BotPassiveDiagnosticWriter.TryCreate(false, null, null, null, null, out disabled));
			Assert.That(disabled.Enabled, Is.False);
			Assert.That(disabled.TryEnqueueHealth(Health()), Is.False);
		}

		[Test]
		public void AWorkerThatCannotStartDoesNotLeakTheFileStream()
		{
			BotPassiveDiagnosticWriter.TestStartWorker = _ => throw new ThreadStateException("simulated start failure");
			BotPassiveDiagnosticWriter writer = null;
			try
			{
				Assert.DoesNotThrow(() => BotPassiveDiagnosticWriter.TryCreate(true, path, "game-1", "seat0", "normal", out writer));
			}
			finally
			{
				BotPassiveDiagnosticWriter.TestStartWorker = null;
			}

			Assert.That(writer.Enabled, Is.False);
			Assert.That(writer.MissingCount, Is.GreaterThan(0));
			Assert.That(File.Exists(path), Is.True);

			// An exclusive open succeeds only when the writer released its handle.
			Assert.DoesNotThrow(() =>
			{
				using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
			});
		}

		[Test]
		public void TruncatedRosterIsRecordedAsEmittedBelowExpected()
		{
			var writer = Create();
			var roster = new[] { Unit(1), Unit(2), Unit(3) };
			Assert.That(writer.TryEnqueueUnitPosition(250, roster, 2, "sum"), Is.False);
			Assert.That(writer.TryEnqueueUnitPosition(500, roster, 5, "sum"), Is.True);
			writer.Complete(500);

			var record = JsonDocument.Parse(File.ReadAllLines(path)[0]).RootElement;
			Assert.That(record.GetProperty("record").GetString(), Is.EqualTo("unit_position"));
			Assert.That(record.GetProperty("expected_count").GetInt32(), Is.EqualTo(5));
			Assert.That(record.GetProperty("emitted_count").GetInt32(), Is.EqualTo(3));
			Assert.That(record.GetProperty("units").GetArrayLength(), Is.EqualTo(3));

			var manifest = LastRecord();
			Assert.That(manifest.GetProperty("missing_count").GetInt32(), Is.EqualTo(1));
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.False);
		}

		[Test]
		public void AnOverflowOnlyManifestIsIncompleteWithMissingCountZero()
		{
			// L6a is by design: overflow_count and missing_count partition the lost records by cause, and complete=false needs either.
			var writer = Create();
			Assert.That(writer.TryEnqueueUnitPosition(0, Enumerable.Range(1, 4096).Select(i => Unit(i)).ToArray(), 4096, "sum"), Is.False);
			writer.Complete(0);

			var manifest = LastRecord();
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.False);
			Assert.That(manifest.GetProperty("overflow_count").GetInt32(), Is.EqualTo(1));
			Assert.That(manifest.GetProperty("missing_count").GetInt32(), Is.EqualTo(0));
		}

		[Test]
		public void TheRecordCountCapOverflowsTheNextRecord()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueHealth(Health()), Is.True);
			WaitFor(() => writer.DataBytes > 0, "the first record");
			var maxLine = writer.DataBytes + 4; // the sequence number has at most five digits

			for (var i = 2; i <= BotPassiveDiagnosticWriter.MaximumRecords; i++)
			{
				Pace(writer, maxLine);
				Assert.That(writer.TryEnqueueHealth(Health()), Is.True, "record " + i);
			}

			Assert.That(writer.AcceptedRecords, Is.EqualTo(BotPassiveDiagnosticWriter.MaximumRecords));
			Assert.That(writer.TryEnqueueHealth(Health()), Is.False);
			Assert.That(writer.OverflowCount, Is.EqualTo(1));
			writer.Complete(45000);
			Assert.That(writer.IsComplete, Is.False);

			var manifest = LastRecord();
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.False);
			Assert.That(manifest.GetProperty("data_records").GetInt32(), Is.EqualTo(BotPassiveDiagnosticWriter.MaximumRecords));
			Assert.That(manifest.GetProperty("last_sequence").GetInt64(), Is.EqualTo(BotPassiveDiagnosticWriter.MaximumRecords));
			Assert.That(manifest.GetProperty("overflow_count").GetInt32(), Is.EqualTo(1));
		}

		[Test]
		public void TheDataByteCapOverflowsBeforeTheRecordCapWhenRecordsAreMaximumSize()
		{
			// Measure the fixed part of a squad_event line with a probe writer, then size the reason so a four-digit-sequence line is exactly 8192 bytes.
			var probePath = Path.Combine(directory, "probe.jsonl");
			BotPassiveDiagnosticWriter.TryCreate(true, probePath, "game-1", "seat0", "normal", out var probe);
			Assert.That(probe.TryEnqueueSquadEvent(SquadEvent("")), Is.True);
			probe.Complete(1);
			var fixedLine = File.ReadAllLines(probePath)[0].Length + 1;
			var reason = new string('a', BotPassiveDiagnosticWriter.MaximumRecordBytes - 3 - fixedLine);

			var writer = Create();
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(reason)), Is.True);
			WaitFor(() => writer.DataBytes > 0, "the first record");
			var maxLine = writer.DataBytes + 3;
			Assert.That(maxLine, Is.LessThanOrEqualTo(BotPassiveDiagnosticWriter.MaximumRecordBytes));

			var accepted = 1;
			while (true)
			{
				Pace(writer, maxLine);
				if (!writer.TryEnqueueSquadEvent(SquadEvent(reason)))
					break;
				accepted++;
				Assert.That(accepted, Is.LessThan(BotPassiveDiagnosticWriter.MaximumRecords), "the byte cap should bind first");
			}

			Assert.That(writer.OverflowCount, Is.EqualTo(1));
			Assert.That(writer.AcceptedRecords, Is.EqualTo(accepted));
			Assert.That(accepted, Is.InRange(8000, 8192));
			writer.Complete(45000);
			Assert.That(writer.IsComplete, Is.False);
			Assert.That(writer.DataBytes, Is.LessThanOrEqualTo(BotPassiveDiagnosticWriter.MaximumDataBytes));

			var manifest = LastRecord();
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.False);
			Assert.That(manifest.GetProperty("data_records").GetInt32(), Is.EqualTo(accepted));
			Assert.That(manifest.GetProperty("data_bytes").GetInt64(), Is.LessThanOrEqualTo(BotPassiveDiagnosticWriter.MaximumDataBytes));
		}

		[Test]
		public void AFullQueueOverflowsTheRecordAndKeepsEarlierRecords()
		{
			var writer = Create();
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			writer.TestBeforeWrite = _ =>
			{
				entered.Set();
				release.Wait(TimeSpan.FromSeconds(30));
			};

			try
			{
				// Record 1 is taken by the worker, which then blocks in the seam; the queue itself is empty.
				Assert.That(writer.TryEnqueueHealth(Health()), Is.True);
				Assert.That(entered.Wait(TimeSpan.FromSeconds(20)), Is.True);
				for (var i = 0; i < BotPassiveDiagnosticWriter.MaximumQueueEntries; i++)
					Assert.That(writer.TryEnqueueHealth(Health()), Is.True, "queued record " + i);

				Assert.That(writer.TryEnqueueHealth(Health()), Is.False);
				Assert.That(writer.OverflowCount, Is.EqualTo(1));
				Assert.That(writer.AcceptedRecords, Is.EqualTo(BotPassiveDiagnosticWriter.MaximumQueueEntries + 1));
			}
			finally
			{
				release.Set();
			}

			writer.Complete(100);
			Assert.That(writer.IsComplete, Is.False);
			var manifest = LastRecord();
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.False);
			Assert.That(manifest.GetProperty("data_records").GetInt32(), Is.EqualTo(BotPassiveDiagnosticWriter.MaximumQueueEntries + 1));
			Assert.That(manifest.GetProperty("last_sequence").GetInt64(), Is.EqualTo(BotPassiveDiagnosticWriter.MaximumQueueEntries + 1));
			Assert.That(manifest.GetProperty("overflow_count").GetInt32(), Is.EqualTo(1));
		}

		[Test]
		public void AWriteFailureFailsTheStreamAndWritesNoManifest()
		{
			var writer = Create();

			// A simulated I/O fault inside the worker's write step (the seam throws where output.Write would).
			writer.TestBeforeWrite = _ => throw new IOException("simulated write failure");
			Assert.That(writer.TryEnqueueHealth(Health()), Is.True);
			WaitFor(() => writer.MissingCount > 0, "the worker to fail");

			Assert.That(writer.TryEnqueueHealth(Health()), Is.False);
			writer.Complete(100);
			Assert.That(writer.IsComplete, Is.False);

			// Complete joined the worker, so its finally block closed the file; it holds no manifest and no data.
			Assert.That(new FileInfo(path).Length, Is.EqualTo(0));
		}

		[Test]
		public void ALateFinishAfterTheJoinTimeoutLeavesMemoryAndFileInAgreement()
		{
			var writer = Create();
			using var decided = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			writer.TestCompleteTimeout = TimeSpan.FromSeconds(2);
			writer.TestBeforeManifest = () =>
			{
				decided.Set();
				release.Wait(TimeSpan.FromSeconds(30));
			};

			Assert.That(writer.TryEnqueueHealth(Health()), Is.True);
			try
			{
				// The worker commits "complete" and stalls before writing the manifest, so the join times out.
				writer.Complete(50);
				Assert.That(decided.IsSet, Is.True);
				Assert.That(writer.IsComplete, Is.False);
			}
			finally
			{
				release.Set();
			}

			WaitFor(() => writer.IsComplete, "the late manifest");
			Assert.That(writer.MissingCount, Is.EqualTo(0));
			Assert.That(LastRecord().GetProperty("complete").GetBoolean(), Is.True);
		}
	}
}
