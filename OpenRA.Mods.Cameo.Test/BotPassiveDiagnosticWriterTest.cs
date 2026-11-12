using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotPassiveDiagnosticWriterTest
	{
		static readonly string[] Envelope = { "schema", "record", "game_uid", "seat", "bot_type", "tick", "seq" };

		string directory;
		string path;

		[SetUp]
		public void SetUp()
		{
			directory = Path.Combine(Path.GetTempPath(), "botpassive-diag-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			path = Path.Combine(directory, "seat0.jsonl");
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		BotPassiveDiagnosticWriter Create()
		{
			BotPassiveDiagnosticWriter.TryCreate(true, path, "game-1", "seat0", "normal", out var writer);
			Assert.That(writer.Enabled, Is.True);
			return writer;
		}

		List<byte[]> Lines()
		{
			var bytes = File.ReadAllBytes(path);
			Assert.That(bytes.Length, Is.GreaterThan(0));
			Assert.That(bytes[^1], Is.EqualTo((byte)'\n'));
			var lines = new List<byte[]>();
			var start = 0;
			for (var i = 0; i < bytes.Length; i++)
				if (bytes[i] == '\n')
				{
					lines.Add(bytes[start..i]);
					start = i + 1;
				}

			return lines;
		}

		List<JsonElement> Records() => Lines().Select(l => JsonDocument.Parse(l).RootElement.Clone()).ToList();

		static void AssertFields(JsonElement record, string kind, params string[] fields)
		{
			Assert.That(record.GetProperty("record").GetString(), Is.EqualTo(kind));
			Assert.That(record.EnumerateObject().Select(p => p.Name).ToArray(), Is.EqualTo(Envelope.Concat(fields).ToArray()),
				kind);
			Assert.That(record.GetProperty("schema").GetString(), Is.EqualTo("botpassive-diagnostic/1"));
			Assert.That(record.GetProperty("game_uid").GetString(), Is.EqualTo("game-1"));
			Assert.That(record.GetProperty("seat").GetString(), Is.EqualTo("seat0"));
			Assert.That(record.GetProperty("bot_type").GetString(), Is.EqualTo("normal"));
		}

		static BotPassiveUnitPositionSnapshot Unit(long id) => new()
		{
			ActorId = id, ActorType = "e1", X = (int)id, Y = 2, SquadId = null, Activity = "Idle", Live = true, InWorld = true
		};

		static BotPassiveSquadEventSnapshot SquadEvent(int tick, string reason = "lost_fight") => new()
		{
			Tick = tick, SquadId = "sq-1", Generation = 2, Event = "retreat_start", MemberActorId = null, Size = 5,
			Value = 4000, StateFrom = "attack", StateTo = "flee", TargetType = "fact", TargetCell = "10,12",
			Reason = reason, LeaseOwner = null
		};

		void WriteOneOfEach(BotPassiveDiagnosticWriter writer)
		{
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(10)), Is.True);
			Assert.That(writer.TryEnqueueUnitPosition(250, new[] { Unit(5) }, 1, "abc"), Is.True);
			Assert.That(writer.TryEnqueueAttackCommit(new BotPassiveAttackCommitSnapshot
			{
				Tick = 300, Kind = "ordinary", SquadId = "sq-1", Generation = 1, WaveId = null, CommittedCount = 12,
				CommittedValue = 9000, PoolCount = 20, PoolValue = 15000, ArmyCount = 30, ArmyValue = 22000,
				TargetSeat = null, TargetCell = null
			}), Is.True);
			Assert.That(writer.TryEnqueueDispatchCheck(new BotPassiveDispatchCheckSnapshot
			{
				Tick = 400, CandIndex = 0, TargetSeat = "seat1", TargetActor = null, NumL = 9000, DenL = 3000,
				DenDefenceFresh = 1200, NumT = 22000, DenT = 10000, ScoreMilli = 2569, ForceOk = true, ClockOk = false,
				TicksSinceKill = 6100, Result = "admit", Urgency = "Normal", UrgencyLegacy = "Pressured",
				HomeThreat = false, HomeLatch = true
			}), Is.True);
			Assert.That(writer.TryEnqueueJobEnd(new BotPassiveJobEndSnapshot
			{
				Tick = 400, Result = "hold", HoldReason = "no_candidate",
				CandidateFailures = new[] { new BotPassiveCandidateFailure { TargetActor = 77, Reason = "unreachable" } },
				DeferredTicks = 1, Paths = 3, ReachChecks = 40, CandidatesTried = 1, UnitsAdmitted = 0,
				UnitsUnreachable = 4, UnitsExcludedGroups = 2, ValueAdmitted = 0
			}), Is.True);
			Assert.That(writer.TryEnqueueWaveStart(new BotPassiveWaveStartSnapshot
			{
				Tick = 500, WaveId = 1, C = 500, E = 12000,
				Groups = new[]
				{
					new BotPassiveWaveGroup { Locomotor = "wheeled", RepActor = 41, L = 300, S = 128, Tc = BotPassiveDiagnosticWriter.Cell(60, 61), Cen = "5,6" }
				},
				RallyDuration = 4000, NoProgressDuration = 5000, AbsoluteDuration = 28000, RallyDeadline = 4500,
				AbsoluteDeadline = 28500, ArmTick = 4550, N0 = 24, V0 = 20000, CommittedCount = 22, CommittedValue = 19000,
				LeaderActor = 41, TargetSeat = "seat1", TargetActor = 99, TargetCell = "60,60"
			}), Is.True);
			Assert.That(writer.TryEnqueueRallyEnd(new BotPassiveRallyEndSnapshot
			{
				Tick = 4500, WaveId = 1, AssembledCount = 20, AssembledValue = 17000, N0 = 24, V0 = 20000, Pass = true
			}), Is.True);
			Assert.That(writer.TryEnqueueWaveSample(new BotPassiveWaveSampleSnapshot
			{
				Tick = 5250, WaveId = 1, Event = "periodic", LastProgressTick = 5175, BestL = 250, CurL = null,
				Arrived = false, UnitsAlive = 21, ValueAlive = 18000, DeadCost = 1000, LeaderActor = 41
			}), Is.True);
			Assert.That(writer.TryEnqueueWaveExit(new BotPassiveWaveExitSnapshot
			{
				Tick = 9000, WaveId = 1, ExitReason = "no_progress", LastProgressTick = 3975, BestL = 250, UnitsAlive = 21,
				ValueLost = 1000, Duration = 8500, RouteEstL = 300, RouteActualCells = 40, RouteBestL = 250
			}), Is.True);
			Assert.That(writer.TryEnqueueHealth(new BotPassiveHealthSnapshot
			{
				Tick = 9750, IdleCombatUnits = 3, UnsquaddedCombatUnits = 4, HomeThirdUnits = 10, CombatUnits = 30,
				StuckSquads = 1, RetreatsWindow = 2, KillsWindow = 5, Urgency = "Normal", UrgencyLegacy = "Emergency"
			}), Is.True);
		}

		[Test]
		public void EveryRecordKindUsesTheExactFieldNames()
		{
			var writer = Create();
			WriteOneOfEach(writer);
			writer.Complete(45000);
			var records = Records();
			Assert.That(records.Count, Is.EqualTo(11));

			AssertFields(records[0], "squad_event", "squad_id", "generation", "event", "member_actor_id", "size", "value",
				"state_from", "state_to", "target_type", "target_cell", "reason", "lease_owner");
			Assert.That(records[0].GetProperty("member_actor_id").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(records[0].GetProperty("tick").GetInt32(), Is.EqualTo(10));

			AssertFields(records[1], "unit_position", "expected_count", "emitted_count", "tick_index", "chunk_index",
				"chunk_count", "roster_checksum", "units");
			Assert.That(records[1].GetProperty("units")[0].EnumerateObject().Select(p => p.Name),
				Is.EqualTo(new[] { "actor_id", "actor_type", "x", "y", "squad_id", "activity", "live", "in_world" }));

			AssertFields(records[2], "attack_commit", "kind", "squad_id", "generation", "wave_id", "committed_count",
				"committed_value", "pool_count", "pool_value", "army_count", "army_value", "target_seat", "target_cell");
			Assert.That(records[2].GetProperty("wave_id").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(records[2].GetProperty("target_seat").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(records[2].GetProperty("target_cell").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(records[2].GetProperty("pool_value").GetInt64(), Is.EqualTo(15000));

			AssertFields(records[3], "dispatch_check", "cand_index", "target_seat", "target_actor", "numL", "denL",
				"den_defence_fresh", "numT", "denT", "score_milli", "force_ok", "clock_ok", "ticks_since_kill", "result",
				"urgency", "urgency_legacy", "home_threat", "home_latch");
			Assert.That(records[3].GetProperty("target_actor").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(records[3].GetProperty("force_ok").GetBoolean(), Is.True);
			Assert.That(records[3].GetProperty("home_threat").GetBoolean(), Is.False);
			Assert.That(records[3].GetProperty("home_latch").GetBoolean(), Is.True);
			Assert.That(records[3].GetProperty("urgency_legacy").GetString(), Is.EqualTo("Pressured"));

			AssertFields(records[4], "job_end", "result", "hold_reason", "candidate_failures", "deferred_ticks", "paths",
				"reach_checks", "candidates_tried", "units_admitted", "units_unreachable", "units_excluded_groups",
				"value_admitted");
			var failure = records[4].GetProperty("candidate_failures")[0];
			Assert.That(failure.EnumerateObject().Select(p => p.Name), Is.EqualTo(new[] { "target_actor", "reason" }));
			Assert.That(failure.GetProperty("target_actor").GetInt64(), Is.EqualTo(77));

			AssertFields(records[5], "wave_start", "wave_id", "c", "e", "groups", "rally_duration", "no_progress_duration",
				"absolute_duration", "rally_deadline", "absolute_deadline", "arm_tick", "n0", "v0", "committed_count",
				"committed_value", "leader_actor", "target_seat", "target_actor", "target_cell");
			var group = records[5].GetProperty("groups")[0];
			Assert.That(group.EnumerateObject().Select(p => p.Name),
				Is.EqualTo(new[] { "locomotor", "rep_actor", "l", "s", "tc", "cen" }));
			Assert.That(group.GetProperty("tc").GetString(), Is.EqualTo("60,61"));
			Assert.That(records[5].GetProperty("arm_tick").GetInt32(), Is.EqualTo(4550));

			AssertFields(records[6], "rally_end", "wave_id", "assembled_count", "assembled_value", "n0", "v0", "pass");
			Assert.That(records[6].GetProperty("pass").GetBoolean(), Is.True);

			AssertFields(records[7], "wave_sample", "wave_id", "event", "last_progress_tick", "best_l", "cur_l", "arrived",
				"units_alive", "value_alive", "dead_cost", "leader_actor");
			Assert.That(records[7].GetProperty("cur_l").ValueKind, Is.EqualTo(JsonValueKind.Null));
			Assert.That(records[7].GetProperty("dead_cost").GetInt64(), Is.EqualTo(1000));

			AssertFields(records[8], "wave_exit", "wave_id", "exit_reason", "last_progress_tick", "best_l", "units_alive",
				"value_lost", "duration", "route_est_l", "route_actual_cells", "route_best_l");
			Assert.That(records[8].GetProperty("exit_reason").GetString(), Is.EqualTo("no_progress"));

			AssertFields(records[9], "health", "idle_combat_units", "unsquadded_combat_units", "home_third_units",
				"combat_units", "stuck_squads", "retreats_window", "kills_window", "urgency", "urgency_legacy");

			AssertFields(records[10], "manifest", "complete", "data_records", "data_bytes", "data_sha256", "overflow_count",
				"missing_count", "last_sequence");
		}

		[Test]
		public void SequenceIsContiguousAndManifestHashesThePrecedingBytes()
		{
			var writer = Create();
			WriteOneOfEach(writer);
			writer.Complete(45123);
			Assert.That(writer.IsComplete, Is.True);

			var lines = Lines();
			var records = Records();
			for (var i = 0; i < records.Count; i++)
				Assert.That(records[i].GetProperty("seq").GetInt64(), Is.EqualTo(i + 1));

			var manifest = records[^1];
			var dataBytes = lines.Take(lines.Count - 1).Sum(l => l.Length + 1);
			var all = File.ReadAllBytes(path);
			var expectedSha = Convert.ToHexString(SHA256.HashData(all.AsSpan(0, dataBytes))).ToLowerInvariant();
			Assert.That(manifest.GetProperty("tick").GetInt32(), Is.EqualTo(45123));
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.True);
			Assert.That(manifest.GetProperty("data_records").GetInt32(), Is.EqualTo(10));
			Assert.That(manifest.GetProperty("data_bytes").GetInt64(), Is.EqualTo(dataBytes));
			Assert.That(manifest.GetProperty("data_sha256").GetString(), Is.EqualTo(expectedSha));
			Assert.That(manifest.GetProperty("overflow_count").GetInt32(), Is.EqualTo(0));
			Assert.That(manifest.GetProperty("missing_count").GetInt32(), Is.EqualTo(0));
			Assert.That(manifest.GetProperty("last_sequence").GetInt64(), Is.EqualTo(10));
			Assert.That(writer.DataSha256, Is.EqualTo(expectedSha));
		}

		[Test]
		public void DisabledWriterCreatesNoFileAndAcceptsNothing()
		{
			BotPassiveDiagnosticWriter.TryCreate(false, path, "game-1", "seat0", "normal", out var writer);
			Assert.That(writer.Enabled, Is.False);
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(1)), Is.False);
			Assert.That(writer.TryEnqueueUnitPosition(0, new[] { Unit(1) }, 1, "x"), Is.False);
			Assert.That(writer.TryEnqueueHealth(new BotPassiveHealthSnapshot { Urgency = "Normal", UrgencyLegacy = "Normal" }),
				Is.False);
			writer.Complete(100);
			writer.Dispose();
			Assert.That(File.Exists(path), Is.False);
			Assert.That(Directory.GetFiles(directory), Is.Empty);
			Assert.That(writer.IsComplete, Is.False);
		}

		[Test]
		public void ExistingFileIsNeverOverwritten()
		{
			File.WriteAllText(path, "previous");
			BotPassiveDiagnosticWriter writer = null;
			Assert.DoesNotThrow(() => BotPassiveDiagnosticWriter.TryCreate(true, path, "game-1", "seat0", "normal", out writer));
			Assert.That(writer.Enabled, Is.False);
			Assert.That(writer.MissingCount, Is.GreaterThan(0));
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(1)), Is.False);
			writer.Complete(1);
			Assert.That(writer.IsComplete, Is.False);
			Assert.That(File.ReadAllText(path), Is.EqualTo("previous"));
		}

		[Test]
		public void OversizedRecordOverflowsAndMarksTheStreamIncomplete()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(1)), Is.True);
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(2, new string('a', 9000))), Is.False);
			Assert.That(writer.OverflowCount, Is.EqualTo(1));
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(3)), Is.False);
			writer.Complete(10);
			Assert.That(writer.IsComplete, Is.False);

			var records = Records();
			Assert.That(records.Count, Is.EqualTo(2));
			var manifest = records[^1];
			Assert.That(manifest.GetProperty("record").GetString(), Is.EqualTo("manifest"));
			Assert.That(manifest.GetProperty("complete").GetBoolean(), Is.False);
			Assert.That(manifest.GetProperty("overflow_count").GetInt32(), Is.EqualTo(1));
			Assert.That(manifest.GetProperty("missing_count").GetInt32(), Is.EqualTo(1));
			Assert.That(manifest.GetProperty("last_sequence").GetInt64(), Is.EqualTo(1));
			Assert.That(manifest.GetProperty("seq").GetInt64(), Is.EqualTo(2));
		}

		[Test]
		public void EnqueueAfterCompleteIsMissing()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(1)), Is.True);
			writer.Complete(50);
			Assert.That(writer.IsComplete, Is.True);
			var finalBytes = File.ReadAllBytes(path);

			Assert.That(writer.TryEnqueueSquadEvent(SquadEvent(60)), Is.False);
			Assert.That(writer.MissingCount, Is.EqualTo(1));
			Assert.That(writer.IsComplete, Is.False);
			writer.Complete(70);
			Assert.That(File.ReadAllBytes(path), Is.EqualTo(finalBytes));
		}

		[Test]
		public void InvalidSnapshotsAreMissingNotExceptions()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueSquadEvent(null), Is.False);
			Assert.That(writer.TryEnqueueUnitPosition(125, new[] { Unit(1) }, 1, "x"), Is.False);
			Assert.That(writer.TryEnqueueUnitPosition(250, new[] { Unit(2), Unit(1) }, 2, "x"), Is.False);
			Assert.That(writer.TryEnqueueJobEnd(new BotPassiveJobEndSnapshot
			{
				Result = "hold", HoldReason = "no_candidate",
				CandidateFailures = Enumerable.Range(1, 5)
					.Select(i => new BotPassiveCandidateFailure { TargetActor = i, Reason = "unreachable" }).ToArray()
			}), Is.False);
			Assert.That(writer.TryEnqueueWaveStart(new BotPassiveWaveStartSnapshot
			{
				Groups = Enumerable.Range(1, 4).Select(i => new BotPassiveWaveGroup { Locomotor = "x", Tc = "0,0", Cen = "0,0" })
					.ToArray()
			}), Is.False);
			Assert.That(writer.MissingCount, Is.EqualTo(5));
			writer.Complete(1);
			Assert.That(Records()[^1].GetProperty("complete").GetBoolean(), Is.False);
		}

		[Test]
		public void UnitPositionChunksRespectEntryAndByteCaps()
		{
			var writer = Create();
			var roster = Enumerable.Range(1, 257).Select(i => Unit(i)).ToArray();
			Assert.That(writer.TryEnqueueUnitPosition(500, roster, 257, "sum"), Is.True);
			Assert.That(writer.TryEnqueueUnitPosition(750, roster.Take(3).ToArray(), 3, "sum3"), Is.True);
			writer.Complete(750);
			Assert.That(writer.IsComplete, Is.True);

			var lines = Lines();
			var records = Records();
			var chunks = records.Where(r => r.GetProperty("tick").GetInt32() == 500 && r.GetProperty("record").GetString() == "unit_position").ToList();
			Assert.That(chunks.Count, Is.GreaterThanOrEqualTo(2));
			Assert.That(chunks.Count, Is.LessThanOrEqualTo(BotPassiveDiagnosticWriter.MaximumPositionChunks));
			var ids = new List<long>();
			for (var i = 0; i < chunks.Count; i++)
			{
				Assert.That(chunks[i].GetProperty("chunk_index").GetInt32(), Is.EqualTo(i));
				Assert.That(chunks[i].GetProperty("chunk_count").GetInt32(), Is.EqualTo(chunks.Count));
				Assert.That(chunks[i].GetProperty("tick_index").GetInt32(), Is.EqualTo(2));
				Assert.That(chunks[i].GetProperty("emitted_count").GetInt32(), Is.EqualTo(257));
				Assert.That(chunks[i].GetProperty("units").GetArrayLength(), Is.InRange(1, 256));
				ids.AddRange(chunks[i].GetProperty("units").EnumerateArray().Select(u => u.GetProperty("actor_id").GetInt64()));
			}

			Assert.That(ids, Is.EqualTo(Enumerable.Range(1, 257).Select(i => (long)i)));
			Assert.That(lines.All(l => l.Length + 1 <= BotPassiveDiagnosticWriter.MaximumRecordBytes), Is.True);

			var small = records.Where(r => r.GetProperty("tick").GetInt32() == 750 && r.GetProperty("record").GetString() == "unit_position").ToList();
			Assert.That(small.Count, Is.EqualTo(1));
			Assert.That(small[0].GetProperty("chunk_count").GetInt32(), Is.EqualTo(1));
			Assert.That(small[0].GetProperty("units").GetArrayLength(), Is.EqualTo(3));
		}

		[Test]
		public void UnitPositionOverSixteenChunksOverflows()
		{
			var writer = Create();
			var roster = Enumerable.Range(1, 4096).Select(i => Unit(i)).ToArray();
			Assert.That(writer.TryEnqueueUnitPosition(0, roster, 4096, "sum"), Is.False);
			Assert.That(writer.OverflowCount, Is.EqualTo(1));
			writer.Complete(0);
			var records = Records();
			Assert.That(records.Count, Is.EqualTo(1));
			Assert.That(records[0].GetProperty("complete").GetBoolean(), Is.False);
		}
	}
}
