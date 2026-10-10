#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class AiLogPrivacyRegressionTest
	{
		static Player PlayerNamed(string name)
		{
			var p = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
			typeof(Player).GetField("InternalName").SetValue(p, name);
			return p;
		}

		[Test]
		public void CoalitionTargetUsesSameSeatMapAsEnemyDescriptor()
		{
			var enemy = PlayerNamed("Bravo");
			var situation = new BotSituation
			{
				Tick = 750, CoalitionMainTarget = "Bravo", Demand = new CounterDemand(),
				Enemies = new Dictionary<Player, EnemyProfile> { [enemy] = new EnemyProfile { Player = enemy, FactionName = "td_nod" } }
			};
			var b = new StringBuilder();
			AiSituationLogWriter.AppendSituation(b, p => "seat_13", "00000000-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000001", "map", "seat_4", "td_gdi", "hard", "rush", situation);
			using var doc = JsonDocument.Parse(b.ToString());
			Assert.That(doc.RootElement.GetProperty("own").GetProperty("coalition_main_target").GetString(), Is.EqualTo("seat_13"));
			Assert.That(b.ToString(), Does.Not.Contain("Bravo"));
			Export("situation.jsonl", b.ToString());
		}

		[TestCase(false, true, false)]
		[TestCase(false, false, false)]
		[TestCase(true, false, true)]
		public void LostBotDoesNotOpenTruthGateWhileHumansStillPlay(bool ended, bool botsResolved, bool expected)
		{
			Assert.That(AiMatchLogWriter.TruthCaptureReady(ended, botsResolved), Is.EqualTo(expected));
		}

		[Test]
		public void SightingChangeAndCadenceFloorProduceSamplesWithNumericTruth()
		{
			Assert.That(AiMatchLogWriter.SignatureSampleDue(0, -1, false, 750), Is.True);
			Assert.That(AiMatchLogWriter.SignatureSampleDue(25, 0, true, 750), Is.True);
			Assert.That(AiMatchLogWriter.SignatureSampleDue(749, 0, false, 750), Is.False);
			Assert.That(AiMatchLogWriter.SignatureSampleDue(750, 0, false, 750), Is.True);
			var a = new EnemyProfile { FactionName = "", ArmyValue = 100 };
			var truth = new EnemyProfile { ArmyValue = 800, VehicleValue = 800 };
			Assert.That(AiMatchLogWriter.SignatureChanged(a, truth), Is.True);
			var b = new StringBuilder();
			AiMatchLogWriter.AppendOpponentSignature(b, "seat_13", a, "td_nod", "lost", truth, 750);
			using var doc = JsonDocument.Parse(b.ToString());
			Assert.That(doc.RootElement.GetProperty("tick").GetInt32(), Is.EqualTo(750));
			Assert.That(doc.RootElement.GetProperty("seen").GetProperty("army_value").GetInt32(), Is.EqualTo(100));
			Assert.That(doc.RootElement.GetProperty("truth").GetProperty("army_value").GetInt32(), Is.EqualTo(800));
			Export("match.jsonl", "{\"schema\":3,\"game_uid\":\"00000000-0000-0000-0000-000000000001\",\"record_id\":\"00000000-0000-0000-0000-000000000001|seat_4\",\"player\":{\"seat\":\"seat_4\",\"faction\":\"td_gdi\",\"bot_type\":\"hard\",\"outcome\":\"won\"},\"opponent_signatures\":[" + b + "]}");
		}

		[Test]
		public void DeferredSignatureHistoryRetainsOnlyAggregatedTruthProfiles()
		{
			var actors = new[]
			{
				new ObservedActor { Combat = true, Vehicle = true, Value = 800, Location = new CPos(1, 2), LastSeenTick = 10 },
				new ObservedActor { Building = true, Defence = true, Value = 150, Location = new CPos(9, 2), LastSeenTick = 12 }
			};
			var truth = AiMatchLogWriter.ProfileOf(actors);
			Assert.That(truth.ArmyValue, Is.EqualTo(800));
			Assert.That(truth.VehicleValue, Is.EqualTo(800));
			Assert.That(truth.DefenceValue, Is.EqualTo(150));
			Assert.That(truth.BuildingCount, Is.EqualTo(1));
			Assert.That(truth.KnownRegions, Is.EqualTo(2));
			Assert.That(truth.LastSeenTick, Is.EqualTo(12));
			Assert.That(typeof(AiMatchLogWriter.SignatureSample).GetField("OwnActors"), Is.Null,
				"deferred histories must not retain per-actor snapshots for every tick");

			var b = new StringBuilder();
			AiMatchLogWriter.AppendOpponentSignature(b, "seat_2", new EnemyProfile { ArmyValue = 250 },
				"td_gdi", "lost", truth, 750);
			using var doc = JsonDocument.Parse(b.ToString());
			Assert.That(doc.RootElement.GetProperty("truth").GetProperty("army_value").GetInt32(), Is.EqualTo(800));
			Assert.That(doc.RootElement.GetProperty("truth").GetProperty("building_count").GetInt32(), Is.EqualTo(1));
		}

		[Test]
		public void OrdinaryHumanCombatSeatIsEligibleWithoutBotFlag()
		{
			var p = PlayerNamed("human");
			typeof(Player).GetField("Playable").SetValue(p, true);
			typeof(Player).GetField("PlayerReference").SetValue(p, (PlayerReference)RuntimeHelpers.GetUninitializedObject(typeof(PlayerReference)));
			Assert.That(AiMatchLogWriter.IsEligiblePlayer(p), Is.True);
			Assert.That(AiMatchLogWriter.IsLoggableBot(p), Is.False);
		}

		[Test]
		public void OverlappingMissionSlotsDoNotCorruptTargetOrPublicActorType()
		{
			var map = new Dictionary<string, string> { ["Multi1"] = "seat_4", ["Multi10"] = "seat_13" };
			var record = new BotMissionRecord { MissionId = "raid:Multi10:r4", Attempt = 1, Tick = 750 };
			var line = AiMissionLogWriter.BuildLine(record, "00000000-0000-0000-0000-000000000001", "map", "map", "seat_4", map, DateTime.UnixEpoch);
			using var doc = JsonDocument.Parse(line);
			Assert.That(doc.RootElement.GetProperty("mission_id").GetString(), Is.EqualTo("raid:seat_13:r4"));
			Assert.That(doc.RootElement.GetProperty("attempt_id").GetString(), Is.EqualTo("raid:seat_13:r4|A1"));
			Export("mission.jsonl", line);
			record.MissionId = "capture:oilb:611";
			line = AiMissionLogWriter.BuildLine(record, "00000000-0000-0000-0000-000000000001", "map", "map", "seat_4", new Dictionary<string,string> { ["oil"]="seat_9" }, DateTime.UnixEpoch);
			using var capture = JsonDocument.Parse(line);
			Assert.That(capture.RootElement.GetProperty("mission_id").GetString(), Is.EqualTo("capture:oilb:611"));
		}

		[Test]
		public void EngagementAndPostureEmittersKeepAnonymousReferenceGrammar()
		{
			var h = new EngagementHeader
			{
				GameUid = "00000000-0000-0000-0000-000000000001", MapUid = "map", Player = "seat_13",
				BotType = "hard", Faction = "td_gdi", Personality = "rush", CloseReason = "quiet",
				EndTick = 750, Kind = "defend", OwnBaseX = 10, OwnBaseY = 10,
				BalanceFingerprint = "0123456789abcdef",
				BalanceVersus = new SortedDictionary<string, int> { ["Bullet|None"] = 100 }
			};
			var state = new EngagementState { Id = 1, StartTick = 25, EventCount = 1 };
			state.LostByRole["frontline"] = 100;
			var line = EngagementRecord.BuildEngagement(h, state);
			using var doc = JsonDocument.Parse(line);
			Assert.That(doc.RootElement.GetProperty("record_id").GetString(), Is.EqualTo(h.GameUid + "|seat_13|1"));
			Export("engagement.jsonl", line);
			Export("posture.jsonl", EngagementRecord.BuildPosture(h, 0, 10, 10, 1, 100, 1, -1, 0, 20, 0, null, -1));
		}

		static void Export(string file, string line)
		{
			var dir = Environment.GetEnvironmentVariable("CAMEO_PRIVACY_TEST_OUTPUT");
			if (string.IsNullOrEmpty(dir)) return;
			Directory.CreateDirectory(dir);
			File.WriteAllText(Path.Combine(dir, file), line.TrimEnd() + "\n");
		}
	}
}
