using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class MissionEventDetachmentTest
	{
		[Test]
		public void OracleSourceIsPinned()
		{
			var path = Path.Combine(AppContext.BaseDirectory, "source", "MissionBuildLineOracle.cs");
			Assert.That(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
				Is.EqualTo("80CF4C3D585722FE7A0ADF0144DC9FE750C59E35D7BE2DF1D5122775C237C4CD"));
		}

		[TestCase("")]
		[TestCase("de-DE")]
		public void DetachedMissionMatchesOracleAndSurvivesSourceMutation(string cultureName)
		{
			var before = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
				var record = new BotMissionRecord
				{
					MissionId = "raid:Multi10:r4", Attempt = 2, State = BotMissionAttemptState.Failed,
					Reason = "lost_units", Executor = "", Tick = 750, RegionIndex = 0,
					TargetCell = new CPos(-17, 0), UnitCell = new CPos(12, 27), Units = 0, Value = -1,
				};
				var identity = new PlayerIdentity(-1, "Multi0", null, null, true, false);
				var detached = MissionEvent.Detach(record, identity);
				var time = DateTime.UnixEpoch;
				var expected = MissionBuildLineOracle.BuildLine(record, "g", "m", "map", time);
				var actual = AiMissionLogWriterDev.BuildLine(detached, "g", "m", "map", time);
				Assert.That(actual, Is.EqualTo(expected));
				record.MissionId = "changed";
				record.Player = null;
				record.Event = BotMissionEvent.Denied;
				record.Attempt = 99;
				record.State = BotMissionAttemptState.Success;
				record.Reason = null;
				record.Executor = "changed";
				record.MissionType = "changed";
				record.RegionIndex = 5;
				record.TargetCell = new CPos(1, 1);
				record.UnitCell = null;
				record.Units = null;
				record.Value = 99;
				record.Tick = 1;
				Assert.That(AiMissionLogWriterDev.BuildLine(detached, "g", "m", "map", time), Is.EqualTo(actual));
			}
			finally { CultureInfo.CurrentCulture = before; }
		}

		[Test]
		public void MissionAndAttemptWireFormsMatchThePinnedOracle()
		{
			foreach (var state in Enum.GetValues<BotMissionAttemptState>())
			foreach (var reason in new[] { null, "", "x_invalid_bad", "done" })
			foreach (var executor in new[] { null, "", "engineer" })
			{
				var record = new BotMissionRecord { MissionId = "capture:Multi1:oil:42", State = state,
					Reason = reason, Executor = executor, Attempt = 2, Tick = 12, Units = 0, RegionIndex = -1 };
				var detached = MissionEvent.Detach(record, default);
				Assert.That(AiMissionLogWriterDev.BuildLine(detached, "g", "m", "map", DateTime.UnixEpoch),
					Is.EqualTo(MissionBuildLineOracle.BuildLine(record, "g", "m", "map", DateTime.UnixEpoch)));
			}

			foreach (var missionEvent in Enum.GetValues<BotMissionEvent>())
			{
				var record = new BotMissionRecord { MissionId = "recon:Multi1:42", Event = missionEvent, Tick = 42 };
				Assert.That(AiMissionLogWriterDev.BuildLine(MissionEvent.Detach(record, default), "g", "m", "map", DateTime.UnixEpoch),
					Is.EqualTo(MissionBuildLineOracle.BuildLine(record, "g", "m", "map", DateTime.UnixEpoch)));
			}
		}

		[Test]
		public void CellTypeIsDetachedAndReadOnly()
		{
			Assert.That(typeof(MissionEvent).GetProperty(nameof(MissionEvent.TargetCell)).PropertyType, Is.EqualTo(typeof(CellXY?)));
			Assert.That(typeof(CellXY).GetFields(), Is.Empty);
			var cell = new CellXY(int.MinValue, int.MaxValue);
			Assert.That(cell.X, Is.EqualTo(int.MinValue));
			Assert.That(cell.Y, Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void EventDoesNotRetainTheSourcePlayer()
		{
			var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
			var record = new BotMissionRecord { Player = player, MissionId = "raid:self:r1" };
			var detached = MissionEvent.Detach(record, new PlayerIdentity(2, "seat", "faction", "bot", true, false));
			record.Player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
			Assert.That(detached.PlayerIndex, Is.EqualTo(2));
			Assert.That(detached.PlayerInternalName, Is.EqualTo("seat"));
			Assert.That(detached.FactionInternalName, Is.EqualTo("faction"));
			Assert.That(detached.BotType, Is.EqualTo("bot"));
		}

		[Test]
		public void AttemptAndCooldownStateStaysResident()
		{
			var actor = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
			var tracker = new BotMissionAttemptTracker(actor, new BotMissionAttemptTrackerInfo());
			var record = new BotMissionRecord { MissionId = "raid:self:r1", Attempt = 1, State = BotMissionAttemptState.Committed };
			tracker.Note(record);
			Assert.That(tracker.CurrentAttemptNumber(record.MissionId), Is.EqualTo(1));
			Assert.That(tracker.NextAttemptNumber(record.MissionId), Is.EqualTo(2));
			tracker.CoolAnswer(record.MissionId, 100);
			Assert.That(tracker.AnswerCooling(record.MissionId, 99), Is.True);
			Assert.That(tracker.AnswerCooling(record.MissionId, 100), Is.False);
			record.Attempt = 2;
			record.State = BotMissionAttemptState.Released;
			tracker.Note(record);
			Assert.That(tracker.NextAttemptNumber(record.MissionId), Is.EqualTo(3));
		}

		[Test]
		public void ResidentWriteStepsPrecedeTheEnabledGuard()
		{
			var path = Path.Combine(AppContext.BaseDirectory, "source", "mods", "CA", "Traits", "BotModules", "BotMissionLog.cs");
			var source = File.ReadAllText(path);
			var start = source.IndexOf("public static void Write(BotMissionRecord record)", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			var note = source.IndexOf("?.Note(record)", start, StringComparison.Ordinal);
			var reason = source.IndexOf("if (!BotMissionReasons.IsValid(record.Reason))", start, StringComparison.Ordinal);
			var stamp = source.IndexOf("record.Tick = world.WorldTick;", start, StringComparison.Ordinal);
			var guard = source.IndexOf("hub.Sink.Enabled", start, StringComparison.Ordinal);
			var detach = source.IndexOf("MissionEvent.Detach(record,", start, StringComparison.Ordinal);
			Assert.That(note, Is.GreaterThan(start));
			Assert.That(reason, Is.GreaterThan(note));
			Assert.That(stamp, Is.GreaterThan(reason));
			Assert.That(guard, Is.GreaterThan(stamp));
			Assert.That(detach, Is.GreaterThan(guard));
		}

		[Test]
		public void PassiveListsOwnTheirContents()
		{
			var failures = new List<BotPassiveCandidateFailure> { new() { TargetActor = 7, Reason = "blocked" } };
			var groups = new List<BotPassiveWaveGroup> { new() { Locomotor = "wheel", RepActor = 8 } };
			var job = new BotPassiveJobEndSnapshot { CandidateFailures = failures };
			var wave = new BotPassiveWaveStartSnapshot { Groups = groups };
			failures.Clear();
			groups[0] = new BotPassiveWaveGroup { Locomotor = "changed" };
			groups.Add(new BotPassiveWaveGroup { Locomotor = "extra" });
			Assert.That(job.CandidateFailures, Has.Count.EqualTo(1));
			Assert.That(job.CandidateFailures[0].Reason, Is.EqualTo("blocked"));
			Assert.That(wave.Groups[0].Locomotor, Is.EqualTo("wheel"));
			Assert.That(wave.Groups, Has.Count.EqualTo(1));
			Assert.That(new BotPassiveJobEndSnapshot { CandidateFailures = new List<BotPassiveCandidateFailure>() }.CandidateFailures, Is.Empty);
			Assert.That(new BotPassiveWaveStartSnapshot { Groups = new List<BotPassiveWaveGroup>() }.Groups, Is.Empty);
			Assert.That(new BotPassiveJobEndSnapshot { CandidateFailures = null }.CandidateFailures, Is.Null);
			Assert.That(new BotPassiveWaveStartSnapshot { Groups = null }.Groups, Is.Null);
		}
	}
}
