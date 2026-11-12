#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

// BOTPASSIVE-FIX01 telemetry (design _08 §2, _09 LF/LG). Adopted from FullStack TELEM01 (botpassive-telem01).
// Observation only: no orders, no RNG, no saved state; decisions never read it.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace OpenRA.Mods.CA.Traits.BotModules
{
	/// <summary>
	/// Bounded diagnostic-only JSONL sink. Callers pass immutable snapshots containing only
	/// seat-legal observations; this type never reads simulation state or issues game orders.
	/// </summary>
	public sealed class BotPassiveDiagnosticWriter : IDisposable
	{
		public const string Schema = "botpassive-diagnostic/1";
		public const int MaximumRecordBytes = 8192;
		public const int MaximumRecords = 32768;
		public const int MaximumQueueEntries = 4096;
		public const long MaximumDataBytes = 64L * 1024 * 1024;
		public const int MaximumPositionChunkEntries = 256;
		public const int MaximumPositionChunks = 16;
		public const int PositionIntervalTicks = 250;
		public const int MaximumPositionTick = 50000;
		public const int MaximumCandidateFailures = 4;
		public const int MaximumWaveGroups = 3;
		public static readonly TimeSpan CompleteTimeout = TimeSpan.FromSeconds(10);

		// The terminal manifest is written beyond the data cap, within one record of reserve.
		const int ManifestReserveBytes = MaximumRecordBytes;

		sealed record QueuedRecord(byte[] Bytes, long Sequence);

		readonly bool enabled;
		readonly string gameUid;
		readonly string seat;
		readonly string botType;
		readonly FileStream output;
		readonly BlockingCollection<QueuedRecord> queue;
		readonly Thread worker;
		readonly object admissionLock = new();

		// IMPL02 L6c: the worker's completeness decision and the join-timeout failure are mutually exclusive under this lock.
		readonly object finalizeLock = new();
		bool manifestDecided;

		// Test seams (InternalsVisibleTo; all null/default in production). They only let a test stall or fail the worker deterministically.
		internal static Action<Thread> TestStartWorker;
		internal Action<long> TestBeforeWrite;
		internal Action TestBeforeManifest;
		internal TimeSpan? TestCompleteTimeout;

		long nextSequence;
		long reservedBytes;
		int acceptedRecords;
		int overflowCount;
		int missingCount;
		int failed;
		int closed;
		int complete;
		int finalTick = -1;
		long dataBytes;
		int writtenRecords;
		string dataSha256;

		/// <summary>False means collection is disabled or file creation failed.</summary>
		public bool Enabled => enabled;

		/// <summary>True only after a complete manifest was written and nothing was lost before or after it.</summary>
		public bool IsComplete => Volatile.Read(ref complete) == 1 && Volatile.Read(ref failed) == 0
			&& Volatile.Read(ref missingCount) == 0 && Volatile.Read(ref overflowCount) == 0;
		public int OverflowCount => Volatile.Read(ref overflowCount);
		public int MissingCount => Volatile.Read(ref missingCount);
		public long LastSequence => Interlocked.Read(ref nextSequence);
		public int AcceptedRecords => Volatile.Read(ref acceptedRecords);
		public long DataBytes => Interlocked.Read(ref dataBytes);
		public string DataSha256 => Volatile.Read(ref dataSha256);

		BotPassiveDiagnosticWriter(string gameUid, string seat, string botType)
		{
			this.gameUid = gameUid;
			this.seat = seat;
			this.botType = botType;
		}

		BotPassiveDiagnosticWriter(string path, string gameUid, string seat, string botType)
			: this(gameUid, seat, botType)
		{
			output = new FileStream(Path.GetFullPath(path), FileMode.CreateNew, FileAccess.Write, FileShare.Read,
				4096, FileOptions.SequentialScan);
			queue = new BlockingCollection<QueuedRecord>(new ConcurrentQueue<QueuedRecord>(), MaximumQueueEntries);

			// Background: Complete joins the writer, so an abandoned writer can never block process exit.
			worker = new Thread(WriteLoop)
			{
				IsBackground = true,
				Name = "Bot passive diagnostic writer"
			};

			// IMPL02 L6e: a worker that cannot start must not leak the open FileStream (the exclusive file would stay locked).
			try
			{
				var start = TestStartWorker;
				if (start != null)
					start(worker);
				else
					worker.Start();
			}
			catch (Exception)
			{
				queue.Dispose();
				output.Dispose();
				throw;
			}

			enabled = true;
		}

		/// <summary>
		/// Creates an exclusive new JSONL file only when the released harness gate is enabled.
		/// Creation failures produce a disabled/incomplete writer and never escape into game logic.
		/// </summary>
		public static BotPassiveDiagnosticWriter TryCreate(bool enabled, string freshJsonlPath,
			string gameUid, string seat, string botType, out BotPassiveDiagnosticWriter writer)
		{
			// IMPL02 L6d: diagnostics never throw into game logic, so a blank identity is a failed (disabled, incomplete) writer, not an exception.
			if (!enabled)
			{
				writer = new BotPassiveDiagnosticWriter(gameUid, seat, botType);
				return writer;
			}

			try
			{
				if (string.IsNullOrWhiteSpace(gameUid) || string.IsNullOrWhiteSpace(seat) || string.IsNullOrWhiteSpace(botType))
					throw new ArgumentException("Diagnostic identity fields are required.");

				if (string.IsNullOrWhiteSpace(freshJsonlPath))
					throw new ArgumentException("Enabled telemetry requires a fresh output path.", nameof(freshJsonlPath));

				writer = new BotPassiveDiagnosticWriter(freshJsonlPath, gameUid, seat, botType);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
				or NotSupportedException or System.Security.SecurityException or OutOfMemoryException
				or ThreadStateException)
			{
				writer = new BotPassiveDiagnosticWriter(gameUid, seat, botType);
				Interlocked.Exchange(ref writer.failed, 1);
				Interlocked.Exchange(ref writer.missingCount, 1);
			}

			return writer;
		}

		/// <summary>Formats a cell as the wire "x,y" string.</summary>
		public static string Cell(int x, int y) =>
			x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ","
				+ y.ToString(System.Globalization.CultureInfo.InvariantCulture);

		public bool TryEnqueueSquadEvent(in BotPassiveSquadEventSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("squad_event", s.Tick, seq, writer =>
			{
				writer.WriteString("squad_id", s.SquadId);
				writer.WriteNumber("generation", s.Generation);
				writer.WriteString("event", s.Event);
				WriteNullableString(writer, "member_actor_id", s.MemberActorId);
				writer.WriteNumber("size", s.Size);
				writer.WriteNumber("value", s.Value);
				WriteNullableString(writer, "state_from", s.StateFrom);
				WriteNullableString(writer, "state_to", s.StateTo);
				WriteNullableString(writer, "target_type", s.TargetType);
				WriteNullableString(writer, "target_cell", s.TargetCell);
				WriteNullableString(writer, "reason", s.Reason);
				WriteNullableString(writer, "lease_owner", s.LeaseOwner);

				// IMPL02 A6 (switch ON only): the evidence an aggression rule held at a retreat. Written only when present, so every
				// other squad_event row, and every row of the OFF arm, keeps its bytes.
				if (s.PredictedRatioMilli.HasValue)
					writer.WriteNumber("predicted_ratio_milli", s.PredictedRatioMilli.Value);

				if (s.CoalitionRatioMilli.HasValue)
					writer.WriteNumber("coalition_ratio_milli", s.CoalitionRatioMilli.Value);

				if (s.Band != null)
					writer.WriteString("band", s.Band);

				if (s.TradeDelta.HasValue)
					writer.WriteNumber("trade_delta", s.TradeDelta.Value);

				if (s.AlliesEngaged.HasValue)
					writer.WriteNumber("allies_engaged", s.AlliesEngaged.Value);

				if (s.OwnFaction != null)
					writer.WriteString("own_faction", s.OwnFaction);

				if (s.EnemyFaction != null)
					writer.WriteString("enemy_faction", s.EnemyFaction);
			}));
		}

		/// <summary>
		/// Enqueues one position sample, splitting a sorted roster into chunks of at most 256 entries that each
		/// also fit the 8192-byte record cap (a unit entry is ~100 bytes, so the byte bound binds first).
		/// More than 16 chunks is a cap hit (overflow). IMPL02 L6f: expectedCount may exceed the roster size when the caller truncated
		/// the roster to the chunk cap, so emitted_count &lt; expected_count then records the truncation; expectedCount &lt; roster size is invalid.
		/// </summary>
		public bool TryEnqueueUnitPosition(int tick, IReadOnlyList<BotPassiveUnitPositionSnapshot> sortedRoster,
			int expectedCount, string rosterChecksum)
		{
			if (sortedRoster == null || tick < 0 || tick > MaximumPositionTick || tick % PositionIntervalTicks != 0
				|| expectedCount < sortedRoster.Count || string.IsNullOrWhiteSpace(rosterChecksum)
				|| sortedRoster.Count > MaximumPositionChunkEntries * MaximumPositionChunks)
				return Reject();

			for (var i = 0; i < sortedRoster.Count; i++)
				if (sortedRoster[i] == null || (i > 0 && sortedRoster[i - 1].ActorId >= sortedRoster[i].ActorId))
					return Reject();

			if (!enabled || Volatile.Read(ref closed) != 0 || Volatile.Read(ref failed) != 0)
				return Reject();

			byte[][] entries;
			byte[] header;
			try
			{
				entries = new byte[sortedRoster.Count][];
				for (var i = 0; i < entries.Length; i++)
					entries[i] = SerializeUnit(sortedRoster[i]);

				// Upper bound for the non-unit part of a chunk: widest sequence, chunk index and chunk count.
				header = SerializeUnitPositionChunk(tick, long.MaxValue, expectedCount, sortedRoster.Count,
					MaximumPositionChunks, MaximumPositionChunks, rosterChecksum, entries, 0, 0);
			}
			catch (Exception)
			{
				return Reject();
			}

			// Greedy packing: header + entries + separating commas + newline <= MaximumRecordBytes.
			var bounds = new List<(int Start, int End)>();
			var chunkStart = 0;
			var size = header.Length + 1;
			for (var i = 0; i < entries.Length; i++)
			{
				var add = entries[i].Length + (i > chunkStart ? 1 : 0);
				if (i > chunkStart && (i - chunkStart >= MaximumPositionChunkEntries || size + add > MaximumRecordBytes))
				{
					bounds.Add((chunkStart, i));
					chunkStart = i;
					size = header.Length + 1;
					add = entries[i].Length;
				}

				size += add;
			}

			bounds.Add((chunkStart, entries.Length));
			if (bounds.Count > MaximumPositionChunks)
			{
				lock (admissionLock)
					MarkOverflow();
				return false;
			}

			var chunkCount = bounds.Count;
			var allAccepted = true;
			for (var chunk = 0; chunk < chunkCount; chunk++)
			{
				var (start, end) = bounds[chunk];
				var chunkIndex = chunk;
				allAccepted &= TryEnqueue(seq => SerializeUnitPositionChunk(tick, seq, expectedCount, sortedRoster.Count,
					chunkIndex, chunkCount, rosterChecksum, entries, start, end));
			}

			return allAccepted;
		}

		byte[] SerializeUnitPositionChunk(int tick, long sequence, int expectedCount, int emittedCount, int chunkIndex,
			int chunkCount, string rosterChecksum, byte[][] entries, int start, int end) =>
			SerializeCommon("unit_position", tick, sequence, writer =>
			{
				writer.WriteNumber("expected_count", expectedCount);
				writer.WriteNumber("emitted_count", emittedCount);
				writer.WriteNumber("tick_index", tick / PositionIntervalTicks);
				writer.WriteNumber("chunk_index", chunkIndex);
				writer.WriteNumber("chunk_count", chunkCount);
				writer.WriteString("roster_checksum", rosterChecksum);
				writer.WriteStartArray("units");
				for (var i = start; i < end; i++)
					writer.WriteRawValue(entries[i], skipInputValidation: true);
				writer.WriteEndArray();
			});

		static byte[] SerializeUnit(BotPassiveUnitPositionSnapshot unit)
		{
			using var stream = new MemoryStream();
			using (var writer = new Utf8JsonWriter(stream))
			{
				writer.WriteStartObject();
				writer.WriteNumber("actor_id", unit.ActorId);
				writer.WriteString("actor_type", unit.ActorType);
				writer.WriteNumber("x", unit.X);
				writer.WriteNumber("y", unit.Y);
				WriteNullableString(writer, "squad_id", unit.SquadId);
				writer.WriteString("activity", unit.Activity);
				writer.WriteBoolean("live", unit.Live);
				writer.WriteBoolean("in_world", unit.InWorld);
				writer.WriteEndObject();
			}

			return stream.ToArray();
		}

		public bool TryEnqueueAttackCommit(in BotPassiveAttackCommitSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("attack_commit", s.Tick, seq, writer =>
			{
				writer.WriteString("kind", s.Kind);
				writer.WriteString("squad_id", s.SquadId);
				writer.WriteNumber("generation", s.Generation);
				WriteNullableNumber(writer, "wave_id", s.WaveId);
				writer.WriteNumber("committed_count", s.CommittedCount);
				writer.WriteNumber("committed_value", s.CommittedValue);
				writer.WriteNumber("pool_count", s.PoolCount);
				writer.WriteNumber("pool_value", s.PoolValue);
				writer.WriteNumber("army_count", s.ArmyCount);
				writer.WriteNumber("army_value", s.ArmyValue);
				WriteNullableString(writer, "target_seat", s.TargetSeat);
				WriteNullableString(writer, "target_cell", s.TargetCell);
			}));
		}

		public bool TryEnqueueDispatchCheck(in BotPassiveDispatchCheckSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("dispatch_check", s.Tick, seq, writer =>
			{
				writer.WriteNumber("cand_index", s.CandIndex);
				writer.WriteString("target_seat", s.TargetSeat);
				WriteNullableNumber(writer, "target_actor", s.TargetActor);
				writer.WriteNumber("numL", s.NumL);
				writer.WriteNumber("denL", s.DenL);
				writer.WriteNumber("den_defence_fresh", s.DenDefenceFresh);
				writer.WriteNumber("numT", s.NumT);
				writer.WriteNumber("denT", s.DenT);
				writer.WriteNumber("score_milli", s.ScoreMilli);
				writer.WriteBoolean("force_ok", s.ForceOk);
				writer.WriteBoolean("clock_ok", s.ClockOk);
				writer.WriteNumber("ticks_since_kill", s.TicksSinceKill);
				writer.WriteString("result", s.Result);
				writer.WriteString("urgency", s.Urgency);
				writer.WriteString("urgency_legacy", s.UrgencyLegacy);
				writer.WriteBoolean("home_threat", s.HomeThreat);
				writer.WriteBoolean("home_latch", s.HomeLatch);
			}));
		}

		public bool TryEnqueueJobEnd(in BotPassiveJobEndSnapshot snapshot)
		{
			var s = snapshot;
			if (s == null || (s.CandidateFailures != null && s.CandidateFailures.Count > MaximumCandidateFailures))
			{
				MarkMissing();
				return false;
			}

			return TryEnqueue(seq => SerializeCommon("job_end", s.Tick, seq, writer =>
			{
				writer.WriteString("result", s.Result);
				writer.WriteString("hold_reason", s.HoldReason);
				writer.WriteStartArray("candidate_failures");
				foreach (var failure in s.CandidateFailures ?? Array.Empty<BotPassiveCandidateFailure>())
				{
					writer.WriteStartObject();
					writer.WriteNumber("target_actor", failure.TargetActor);
					writer.WriteString("reason", failure.Reason);
					writer.WriteEndObject();
				}

				writer.WriteEndArray();
				writer.WriteNumber("deferred_ticks", s.DeferredTicks);
				writer.WriteNumber("paths", s.Paths);
				writer.WriteNumber("reach_checks", s.ReachChecks);
				writer.WriteNumber("candidates_tried", s.CandidatesTried);
				writer.WriteNumber("units_admitted", s.UnitsAdmitted);
				writer.WriteNumber("units_unreachable", s.UnitsUnreachable);
				writer.WriteNumber("units_excluded_groups", s.UnitsExcludedGroups);
				writer.WriteNumber("value_admitted", s.ValueAdmitted);
			}));
		}

		public bool TryEnqueueWaveStart(in BotPassiveWaveStartSnapshot snapshot)
		{
			var s = snapshot;
			if (s == null || s.Groups == null || s.Groups.Count > MaximumWaveGroups)
			{
				MarkMissing();
				return false;
			}

			return TryEnqueue(seq => SerializeCommon("wave_start", s.Tick, seq, writer =>
			{
				writer.WriteNumber("wave_id", s.WaveId);
				writer.WriteNumber("c", s.C);
				writer.WriteNumber("e", s.E);
				writer.WriteStartArray("groups");
				foreach (var group in s.Groups)
				{
					writer.WriteStartObject();
					writer.WriteString("locomotor", group.Locomotor);
					writer.WriteNumber("rep_actor", group.RepActor);
					writer.WriteNumber("l", group.L);
					writer.WriteNumber("s", group.S);
					writer.WriteString("tc", group.Tc);
					writer.WriteString("cen", group.Cen);
					writer.WriteEndObject();
				}

				writer.WriteEndArray();
				writer.WriteNumber("rally_duration", s.RallyDuration);
				writer.WriteNumber("no_progress_duration", s.NoProgressDuration);
				writer.WriteNumber("absolute_duration", s.AbsoluteDuration);
				writer.WriteNumber("rally_deadline", s.RallyDeadline);
				writer.WriteNumber("absolute_deadline", s.AbsoluteDeadline);
				writer.WriteNumber("arm_tick", s.ArmTick);
				writer.WriteNumber("n0", s.N0);
				writer.WriteNumber("v0", s.V0);
				writer.WriteNumber("committed_count", s.CommittedCount);
				writer.WriteNumber("committed_value", s.CommittedValue);
				writer.WriteNumber("leader_actor", s.LeaderActor);
				writer.WriteString("target_seat", s.TargetSeat);
				writer.WriteNumber("target_actor", s.TargetActor);
				writer.WriteString("target_cell", s.TargetCell);
			}));
		}

		public bool TryEnqueueWaveSample(in BotPassiveWaveSampleSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("wave_sample", s.Tick, seq, writer =>
			{
				writer.WriteNumber("wave_id", s.WaveId);
				writer.WriteString("event", s.Event);
				writer.WriteNumber("last_progress_tick", s.LastProgressTick);
				writer.WriteNumber("best_l", s.BestL);
				WriteNullableNumber(writer, "cur_l", s.CurL);
				writer.WriteBoolean("arrived", s.Arrived);
				writer.WriteNumber("units_alive", s.UnitsAlive);
				writer.WriteNumber("value_alive", s.ValueAlive);
				writer.WriteNumber("dead_cost", s.DeadCost);
				writer.WriteNumber("leader_actor", s.LeaderActor);
			}));
		}

		public bool TryEnqueueRallyEnd(in BotPassiveRallyEndSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("rally_end", s.Tick, seq, writer =>
			{
				writer.WriteNumber("wave_id", s.WaveId);
				writer.WriteNumber("assembled_count", s.AssembledCount);
				writer.WriteNumber("assembled_value", s.AssembledValue);
				writer.WriteNumber("n0", s.N0);
				writer.WriteNumber("v0", s.V0);
				writer.WriteBoolean("pass", s.Pass);
			}));
		}

		public bool TryEnqueueWaveExit(in BotPassiveWaveExitSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("wave_exit", s.Tick, seq, writer =>
			{
				writer.WriteNumber("wave_id", s.WaveId);
				writer.WriteString("exit_reason", s.ExitReason);
				writer.WriteNumber("last_progress_tick", s.LastProgressTick);
				writer.WriteNumber("best_l", s.BestL);
				writer.WriteNumber("units_alive", s.UnitsAlive);
				writer.WriteNumber("value_lost", s.ValueLost);
				writer.WriteNumber("duration", s.Duration);
				writer.WriteNumber("route_est_l", s.RouteEstL);
				writer.WriteNumber("route_actual_cells", s.RouteActualCells);
				writer.WriteNumber("route_best_l", s.RouteBestL);
			}));
		}

		public bool TryEnqueueHealth(in BotPassiveHealthSnapshot snapshot)
		{
			var s = snapshot;
			return s == null ? Reject() : TryEnqueue(seq => SerializeCommon("health", s.Tick, seq, writer =>
			{
				writer.WriteNumber("idle_combat_units", s.IdleCombatUnits);
				writer.WriteNumber("unsquadded_combat_units", s.UnsquaddedCombatUnits);
				writer.WriteNumber("home_third_units", s.HomeThirdUnits);
				writer.WriteNumber("combat_units", s.CombatUnits);
				writer.WriteNumber("stuck_squads", s.StuckSquads);
				writer.WriteNumber("retreats_window", s.RetreatsWindow);
				writer.WriteNumber("kills_window", s.KillsWindow);
				writer.WriteString("urgency", s.Urgency);
				writer.WriteString("urgency_legacy", s.UrgencyLegacy);
			}));
		}

		/// <summary>
		/// Closes admission, waits (bounded) for the writer to drain and write the manifest with tick = finalTick.
		/// When this returns the file is final, unless the wait timed out: then the stream is marked failed, except when the worker had
		/// already committed its completeness decision (IMPL02 L6c), in which case memory and file both follow that decision once it finishes.
		/// </summary>
		public void Complete(int finalTick)
		{
			if (!enabled || Interlocked.Exchange(ref closed, 1) != 0)
				return;

			Volatile.Write(ref this.finalTick, finalTick);
			lock (admissionLock)
			{
				try
				{
					queue.CompleteAdding();
				}
				catch (ObjectDisposedException)
				{
					MarkFailed();
				}
			}

			try
			{
				if (!worker.Join(TestCompleteTimeout ?? CompleteTimeout))
				{
					lock (finalizeLock)
					{
						if (!manifestDecided)
							MarkFailed();
					}
				}
			}
			catch (ThreadStateException)
			{
				MarkFailed();
			}
		}

		public void Dispose() => Complete(-1);

		bool TryEnqueue(Func<long, byte[]> serialize)
		{
			if (!enabled || Volatile.Read(ref closed) != 0 || Volatile.Read(ref failed) != 0)
			{
				MarkMissing();
				return false;
			}

			lock (admissionLock)
			{
				if (closed != 0 || failed != 0)
				{
					MarkMissing();
					return false;
				}

				// The sequence is committed only on admission, so written records are contiguous from 1.
				var sequence = Interlocked.Read(ref nextSequence) + 1;
				byte[] bytes;
				try
				{
					bytes = serialize(sequence);
				}
				catch (Exception)
				{
					// Diagnostics must never throw into game logic: any serialization failure is a lost record.
					MarkMissing();
					return false;
				}

				if (bytes.Length + 1 > MaximumRecordBytes || acceptedRecords >= MaximumRecords
					|| reservedBytes + bytes.Length + 1 > MaximumDataBytes)
				{
					MarkOverflow();
					return false;
				}

				bool added;
				try
				{
					added = queue.TryAdd(new QueuedRecord(bytes, sequence));
				}
				catch (Exception e) when (e is ObjectDisposedException or InvalidOperationException)
				{
					MarkFailed();
					return false;
				}

				if (!added)
				{
					MarkOverflow();
					return false;
				}

				Interlocked.Exchange(ref nextSequence, sequence);
				reservedBytes += bytes.Length + 1;
				Interlocked.Increment(ref acceptedRecords);
				return true;
			}
		}

		byte[] SerializeCommon(string record, int tick, long sequence, Action<Utf8JsonWriter> writePayload)
		{
			using var stream = new MemoryStream();
			using (var json = new Utf8JsonWriter(stream))
			{
				WriteEnvelope(json, record, tick, sequence);
				writePayload(json);
				json.WriteEndObject();
			}

			return stream.ToArray();
		}

		void WriteEnvelope(Utf8JsonWriter json, string record, int tick, long sequence)
		{
			json.WriteStartObject();
			json.WriteString("schema", Schema);
			json.WriteString("record", record);
			json.WriteString("game_uid", gameUid);
			json.WriteString("seat", seat);
			json.WriteString("bot_type", botType);
			json.WriteNumber("tick", tick);
			json.WriteNumber("seq", sequence);
		}

		void WriteLoop()
		{
			try
			{
				using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
				foreach (var item in queue.GetConsumingEnumerable())
				{
					TestBeforeWrite?.Invoke(item.Sequence);
					var line = new byte[item.Bytes.Length + 1];
					Buffer.BlockCopy(item.Bytes, 0, line, 0, item.Bytes.Length);
					line[^1] = (byte)'\n';
					output.Write(line, 0, line.Length);
					hash.AppendData(line);
					Interlocked.Add(ref dataBytes, line.Length);
					Interlocked.Increment(ref writtenRecords);
				}

				output.Flush(true);
				Volatile.Write(ref dataSha256, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());

				// One completeness decision, used both for the manifest and the in-memory flag. IMPL02 L6c: the counters are read once, so the
				// manifest's complete flag always agrees with its own overflow_count and missing_count, and the decision is committed under
				// finalizeLock so a join timeout cannot mark the stream failed after a complete=true decision was taken.
				bool isComplete;
				int overflowSnapshot;
				int missingSnapshot;
				lock (finalizeLock)
				{
					overflowSnapshot = Volatile.Read(ref overflowCount);
					missingSnapshot = Volatile.Read(ref missingCount);
					isComplete = Volatile.Read(ref failed) == 0 && missingSnapshot == 0 && overflowSnapshot == 0;
					manifestDecided = true;
				}

				TestBeforeManifest?.Invoke();
				var manifest = SerializeManifest(Volatile.Read(ref finalTick), isComplete, overflowSnapshot, missingSnapshot);
				if (manifest.Length + 1 <= MaximumRecordBytes
					&& Interlocked.Read(ref dataBytes) + manifest.Length + 1 <= MaximumDataBytes + ManifestReserveBytes)
				{
					output.Write(manifest, 0, manifest.Length);
					output.WriteByte((byte)'\n');
					output.Flush(true);
					Interlocked.Exchange(ref complete, isComplete ? 1 : 0);
				}
				else
					MarkOverflow();
			}
			catch (Exception)
			{
				// Never let a diagnostic I/O fault escape the writer thread (it would terminate the process).
				MarkFailed();
			}
			finally
			{
				try
				{
					output.Dispose();
				}
				catch (Exception)
				{
					MarkFailed();
				}
			}
		}

		byte[] SerializeManifest(int tick, bool isComplete, int overflows, int missing)
		{
			using var stream = new MemoryStream();
			using (var json = new Utf8JsonWriter(stream))
			{
				var last = Interlocked.Read(ref nextSequence);
				WriteEnvelope(json, "manifest", tick, last + 1);
				json.WriteBoolean("complete", isComplete);
				json.WriteNumber("data_records", Volatile.Read(ref writtenRecords));
				json.WriteNumber("data_bytes", Interlocked.Read(ref dataBytes));
				json.WriteString("data_sha256", Volatile.Read(ref dataSha256));
				json.WriteNumber("overflow_count", overflows);
				json.WriteNumber("missing_count", missing);
				json.WriteNumber("last_sequence", last);
				json.WriteEndObject();
			}

			return stream.ToArray();
		}

		void MarkMissing() => Interlocked.Increment(ref missingCount);

		bool Reject()
		{
			MarkMissing();
			return false;
		}

		void MarkFailed()
		{
			Interlocked.Exchange(ref failed, 1);
			MarkMissing();
		}

		// overflow_count and missing_count partition the lost records by cause: a cap hit (record size, record count, data bytes, queue,
		// chunk count) is an overflow and a rejected or failed enqueue is missing. A manifest with complete=false therefore always has
		// overflow_count + missing_count > 0 (IMPL02 L6a: documented, not merged, so existing readers keep their semantics).
		void MarkOverflow()
		{
			Interlocked.Increment(ref overflowCount);
			Interlocked.Exchange(ref failed, 1);
		}

		static void WriteNullableString(Utf8JsonWriter writer, string name, string value)
		{
			if (value == null)
				writer.WriteNull(name);
			else
				writer.WriteString(name, value);
		}

		static void WriteNullableNumber(Utf8JsonWriter writer, string name, long? value)
		{
			if (value.HasValue)
				writer.WriteNumber(name, value.Value);
			else
				writer.WriteNull(name);
		}
	}

	public sealed class BotPassiveSquadEventSnapshot
	{
		public int Tick { get; init; }
		public string SquadId { get; init; }
		public int Generation { get; init; }
		public string Event { get; init; }
		public string MemberActorId { get; init; }
		public int Size { get; init; }
		public long Value { get; init; }
		public string StateFrom { get; init; }
		public string StateTo { get; init; }
		public string TargetType { get; init; }
		public string TargetCell { get; init; }
		public string Reason { get; init; }
		public string LeaseOwner { get; init; }

		/// <summary>IMPL02 A6 evidence (switch ON only). A null member is not written; see AggressionEvidenceCA for the meanings.</summary>
		public int? PredictedRatioMilli { get; init; }
		public int? CoalitionRatioMilli { get; init; }
		public string Band { get; init; }
		public long? TradeDelta { get; init; }
		public int? AlliesEngaged { get; init; }
		public string OwnFaction { get; init; }
		public string EnemyFaction { get; init; }
	}

	public sealed class BotPassiveUnitPositionSnapshot
	{
		public long ActorId { get; init; }
		public string ActorType { get; init; }
		public int X { get; init; }
		public int Y { get; init; }
		public string SquadId { get; init; }
		public string Activity { get; init; }
		public bool Live { get; init; }
		public bool InWorld { get; init; }
	}

	public sealed class BotPassiveAttackCommitSnapshot
	{
		public int Tick { get; init; }

		/// <summary>"ordinary" or "breaker".</summary>
		public string Kind { get; init; }
		public string SquadId { get; init; }
		public int Generation { get; init; }
		public long? WaveId { get; init; }
		public int CommittedCount { get; init; }
		public long CommittedValue { get; init; }
		public int PoolCount { get; init; }
		public long PoolValue { get; init; }
		public int ArmyCount { get; init; }
		public long ArmyValue { get; init; }
		public string TargetSeat { get; init; }

		/// <summary>"x,y" or null.</summary>
		public string TargetCell { get; init; }
	}

	public sealed class BotPassiveDispatchCheckSnapshot
	{
		public int Tick { get; init; }
		public int CandIndex { get; init; }
		public string TargetSeat { get; init; }
		public long? TargetActor { get; init; }
		public long NumL { get; init; }
		public long DenL { get; init; }
		public long DenDefenceFresh { get; init; }
		public long NumT { get; init; }
		public long DenT { get; init; }
		public long ScoreMilli { get; init; }
		public bool ForceOk { get; init; }
		public bool ClockOk { get; init; }
		public int TicksSinceKill { get; init; }
		public string Result { get; init; }
		public string Urgency { get; init; }
		public string UrgencyLegacy { get; init; }
		public bool HomeThreat { get; init; }
		public bool HomeLatch { get; init; }
	}

	public sealed class BotPassiveCandidateFailure
	{
		public long TargetActor { get; init; }
		public string Reason { get; init; }
	}

	public sealed class BotPassiveJobEndSnapshot
	{
		public int Tick { get; init; }

		/// <summary>"admit" or "hold".</summary>
		public string Result { get; init; }
		public string HoldReason { get; init; }

		/// <summary>At most 4 entries.</summary>
		public IReadOnlyList<BotPassiveCandidateFailure> CandidateFailures { get; init; }
		public int DeferredTicks { get; init; }
		public int Paths { get; init; }
		public int ReachChecks { get; init; }
		public int CandidatesTried { get; init; }
		public int UnitsAdmitted { get; init; }
		public int UnitsUnreachable { get; init; }
		public int UnitsExcludedGroups { get; init; }
		public long ValueAdmitted { get; init; }
	}

	public sealed class BotPassiveWaveGroup
	{
		public string Locomotor { get; init; }
		public long RepActor { get; init; }
		public int L { get; init; }
		public int S { get; init; }

		/// <summary>Resolved target cell, "x,y".</summary>
		public string Tc { get; init; }

		/// <summary>Group centroid cell, "x,y".</summary>
		public string Cen { get; init; }
	}

	public sealed class BotPassiveWaveStartSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }
		public int C { get; init; }
		public int E { get; init; }

		/// <summary>At most 3 entries.</summary>
		public IReadOnlyList<BotPassiveWaveGroup> Groups { get; init; }
		public int RallyDuration { get; init; }
		public int NoProgressDuration { get; init; }
		public int AbsoluteDuration { get; init; }
		public int RallyDeadline { get; init; }
		public int AbsoluteDeadline { get; init; }
		public int ArmTick { get; init; }
		public int N0 { get; init; }
		public long V0 { get; init; }
		public int CommittedCount { get; init; }
		public long CommittedValue { get; init; }
		public long LeaderActor { get; init; }
		public string TargetSeat { get; init; }
		public long TargetActor { get; init; }
		public string TargetCell { get; init; }
	}

	public sealed class BotPassiveWaveSampleSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }

		/// <summary>"periodic", "leader_change" or "retarget".</summary>
		public string Event { get; init; }
		public int LastProgressTick { get; init; }
		public int BestL { get; init; }
		public int? CurL { get; init; }
		public bool Arrived { get; init; }
		public int UnitsAlive { get; init; }
		public long ValueAlive { get; init; }
		public long DeadCost { get; init; }
		public long LeaderActor { get; init; }
	}

	public sealed class BotPassiveRallyEndSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }
		public int AssembledCount { get; init; }
		public long AssembledValue { get; init; }
		public int N0 { get; init; }
		public long V0 { get; init; }
		public bool Pass { get; init; }
	}

	public sealed class BotPassiveWaveExitSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }

		/// <summary>StalemateBreakerWaveCA.WireName of the end reason.</summary>
		public string ExitReason { get; init; }
		public int LastProgressTick { get; init; }
		public int BestL { get; init; }
		public int UnitsAlive { get; init; }
		public long ValueLost { get; init; }
		public int Duration { get; init; }
		public int RouteEstL { get; init; }
		public int RouteActualCells { get; init; }
		public int RouteBestL { get; init; }
	}

	public sealed class BotPassiveHealthSnapshot
	{
		public int Tick { get; init; }
		public int IdleCombatUnits { get; init; }
		public int UnsquaddedCombatUnits { get; init; }
		public int HomeThirdUnits { get; init; }
		public int CombatUnits { get; init; }
		public int StuckSquads { get; init; }
		public int RetreatsWindow { get; init; }
		public int KillsWindow { get; init; }
		public string Urgency { get; init; }
		public string UrgencyLegacy { get; init; }
	}
}
