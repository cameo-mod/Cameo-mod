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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("MC1 (docs/design/AI_MISSION_CARDS.md): appends every bot mission-card transition to a record-only JSONL",
		"archive, one line per transition, schema `mission-card/1`. The game never reads it back (DESIGN §21.1).",
		"Host only, regular games only, like the match and situation logs.")]
	public class AiMissionLogWriterInfo : TraitInfo
	{
		public readonly string FileName = "cameo-ai-missions.jsonl";

		[Desc("Ticks between flushes, so a match killed by the batch harness keeps most of its lines.")]
		public readonly int FlushIntervalTicks = 1500;

		[Desc("Most lines kept in memory between flushes; further lines are counted and dropped.")]
		public readonly int MaxPendingLines = 5000;

		public override object Create(ActorInitializer init) { return new AiMissionLogWriter(this); }
	}

	public class AiMissionLogWriter : IWorldLoaded, IGameOver, ITick, IBotMissionRecordSink, INotifyActorDisposing
	{
		public const string Schema = "mission-card/2";

		readonly AiMissionLogWriterInfo info;
		readonly StringBuilder pending = new();
		bool eligible;
		string gameUid;
		string mapUid;
		string mapTitle;
		int pendingLines;
		int nextFlushTick;
		int nextRetryTick;
		AiLogFileAppender appender;
		string inFlight;
		readonly Dictionary<OpenRA.Player, string> seats = new();
		readonly Dictionary<string, string> seatReplacements = new(StringComparer.Ordinal);

		public int Dropped { get; private set; }

		public AiMissionLogWriter(AiMissionLogWriterInfo info) { this.info = info; }

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			eligible = AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost);
			gameUid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(gameUid))
				gameUid = Guid.NewGuid().ToString("N");

			mapUid = world.Map.Uid;
			mapTitle = world.Map.Title;
			for (var i = 0; i < world.Players.Length; i++)
			{
				seats[world.Players[i]] = AiMatchLogWriter.SeatKey(world, world.Players[i]);
				seatReplacements[world.Players[i].InternalName] = seats[world.Players[i]];
			}
			nextFlushTick = info.FlushIntervalTicks;
		}

		void IBotMissionRecordSink.MissionRecorded(BotMissionRecord record)
		{
			if (!eligible || record?.Player == null || !AiMatchLogWriter.IsLoggableBot(record.Player))
				return;

			if (pendingLines >= info.MaxPendingLines)
			{
				Dropped++;
				return;
			}

			pending.Append(BuildLine(record, gameUid, mapUid, mapTitle, seats.GetValueOrDefault(record.Player, ""), seatReplacements, DateTime.UtcNow)).Append('\n');
			pendingLines++;
		}

		/// <summary>One JSONL line, free of world state beyond the record so it can be tested.</summary>
		public static string BuildLine(BotMissionRecord r, string gameUid, string mapUid, string mapTitle, string seat,
			IReadOnlyDictionary<string, string> seatReplacements, DateTime utc)
		{
			var b = new StringBuilder();
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", Schema, true);
			AiMatchLogWriter.AppendString(b, "recorded_utc", utc.ToString("o", CultureInfo.InvariantCulture));
			AiMatchLogWriter.AppendString(b, "game_uid", gameUid);
			AiMatchLogWriter.AppendString(b, "map_uid", mapUid);
			AiMatchLogWriter.AppendString(b, "map_title", mapTitle);
			AiMatchLogWriter.AppendString(b, "seat", seat);
			AiMatchLogWriter.AppendString(b, "faction", r.Player?.Faction?.InternalName);
			AiMatchLogWriter.AppendString(b, "bot", r.Player?.BotType);
			AiMatchLogWriter.AppendString(b, "mission_id", Anonymous(r.MissionId, seatReplacements));
			if (r.Event is BotMissionEvent e)
			{
				// A mission-level event: the card's own story, no attempt (AI_MISSION_CARDS §2.2).
				AiMatchLogWriter.AppendString(b, "record_kind", "mission");
				AiMatchLogWriter.AppendString(b, "event", BotMissionLog.EventName(e));
			}
			else
			{
				AiMatchLogWriter.AppendString(b, "record_kind", "attempt");
				AiMatchLogWriter.AppendNumber(b, "attempt", r.Attempt);
				AiMatchLogWriter.AppendString(b, "attempt_id", Anonymous(r.MissionId, seatReplacements) + "|A" + r.Attempt.ToString(CultureInfo.InvariantCulture));
				AiMatchLogWriter.AppendString(b, "state", BotMissionLog.StateName(r.State));
				AiMatchLogWriter.AppendBoolean(b, "terminal", BotMissionLog.IsTerminal(r.State));
			}
			if (r.Reason != null)
				AiMatchLogWriter.AppendString(b, "reason", r.Reason);

			// An executor-less record (a DENIED no commander bid on) omits `by` rather than writing an empty name.
			if (!string.IsNullOrEmpty(r.Executor))
				AiMatchLogWriter.AppendString(b, "by", r.Executor);
			AiMatchLogWriter.AppendNumber(b, "tick", r.Tick);
			if (r.MissionType != null)
				AiMatchLogWriter.AppendString(b, "type", r.MissionType);

			if (r.RegionIndex.HasValue)
				AiMatchLogWriter.AppendNumber(b, "region", r.RegionIndex.Value);

			if (r.TargetCell.HasValue)
				AiMatchLogWriter.AppendString(b, "target_cell", r.TargetCell.Value.X + "," + r.TargetCell.Value.Y);

			if (r.UnitCell.HasValue)
				AiMatchLogWriter.AppendString(b, "unit_cell", r.UnitCell.Value.X + "," + r.UnitCell.Value.Y);

			if (r.Units.HasValue)
				AiMatchLogWriter.AppendNumber(b, "units", r.Units.Value);

			if (r.Value.HasValue)
				AiMatchLogWriter.AppendNumber(b, "value", r.Value.Value);

			b.Append('}');
			return b.ToString();
		}

		public static string BuildLine(BotMissionRecord r, string gameUid, string mapUid, string mapTitle, DateTime utc) =>
			BuildLine(r, gameUid, mapUid, mapTitle, "seat_1", new Dictionary<string, string>(), utc);

		static string Anonymous(string value, IReadOnlyDictionary<string, string> replacements)
		{
			if (string.IsNullOrEmpty(value)) return value;
			// A player name can sit at any colon-delimited position — a nested frans:<mission id>,
			// a malformed capture arity — so map every segment that is exactly an identity
			// (an exact match only: Multi1 must not shadow Multi10).
			var parts = value.Split(':');
			for (var i = 0; i < parts.Length; i++)
				if (replacements.TryGetValue(parts[i], out var seat))
					parts[i] = seat;
			// Then emit only the shapes the closed log grammar (mission_ok in
			// tools/audit/audit_no_player_names.py) accepts; anything else — including a
			// pass-through prefix carrying a nested or malformed id — collapses to "unknown"
			// rather than leaking a token.
			if (parts.Length == 3 && (parts[0] == "raid" || parts[0] == "recon" || parts[0] == "secure" || parts[0] == "defend"))
			{
				if (!IsSeatToken(parts[1]))
					parts[1] = "unknown";
				return IsRegion(parts[2]) ? string.Join(":", parts) : "unknown";
			}
			if (parts[0] == "capture")
			{
				// capture:<actorType>:<actorId>, or capture:<target>:<actorType>:<actorId>.
				if (parts.Length == 3 && !IsSeatToken(parts[1]) && IsNumeric(parts[2]))
					return string.Join(":", parts);
				if (parts.Length == 4 && !IsSeatToken(parts[2]) && IsNumeric(parts[3]))
				{
					if (!IsSeat(parts[1]))
						parts[1] = "unknown";
					return string.Join(":", parts);
				}
			}
			else if (parts.Length == 2 && parts[0] == "frans" && IsNumeric(parts[1]) ||
				parts.Length == 2 && parts[0] == "garrison_contest" && parts[1].Length > 1 && parts[1][0] == 'a' && IsNumeric(parts[1][1..]) ||
				parts.Length == 3 && parts[0] == "veto" && IsVetoKind(parts[1]) && IsNumeric(parts[2]))
				return string.Join(":", parts);
			return "unknown";
		}

		static bool IsSeat(string s) =>
			s.Length > 5 && s.StartsWith("seat_", StringComparison.Ordinal) && s[5] is >= '1' and <= '9' && IsNumeric(s[5..]);

		static bool IsSeatToken(string s) => IsSeat(s) || s == "self" || s == "unknown";

		static bool IsRegion(string s) =>
			s.Length > 1 && s[0] == 'r' && IsNumeric(s[1..]) ||
			s.Length > 2 && s[0] == 'r' && s[1] == '-' && IsNumeric(s[2..]) ||
			s.Length > 7 && s.StartsWith("region_", StringComparison.Ordinal) && IsNumeric(s[7..]);

		static bool IsVetoKind(string s) =>
			s == "attack" || s == "defend" || s == "field" || s == "retreat" || s == "flee" || s == "engage";

		static bool IsNumeric(string s)
		{
			if (s.Length == 0) return false;
			foreach (var c in s) if (c is < '0' or > '9') return false;
			return true;
		}

		void ITick.Tick(Actor self)
		{
			var tick = self.World.WorldTick;
			if (inFlight != null)
			{
				if (tick >= nextRetryTick)
					TryFlush(tick);

				return;
			}

			if (tick >= nextFlushTick)
			{
				nextFlushTick = tick + info.FlushIntervalTicks;
				TryFlush(tick);
			}
		}

		void IGameOver.GameOver(World world)
		{
			// World.EndGame pauses before IGameOver and a paused world does not tick: flush now, retries included.
			for (var i = 0; i < 8 && (inFlight != null || pendingLines > 0); i++)
				TryFlush(world.WorldTick);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			// World.Dispose disposes newest actors first and the world actor last, so every
			// player-actor BotMissionAttemptTracker has already written its Released(match_end)
			// records into pending by now. The last GameOver flush ran before them — flush once
			// more or those terminal lines die in the buffer.
			for (var i = 0; i < 8 && (inFlight != null || pendingLines > 0); i++)
				TryFlush(self.World.WorldTick);
		}

		void TryFlush(int tick)
		{
			if (inFlight == null)
			{
				if (pendingLines == 0)
					return;

				inFlight = pending.ToString();
				pending.Clear();
				pendingLines = 0;

				// The appender is single-shot (terminal after one append), so each batch gets its own.
				appender = new AiLogFileAppender(info.FileName);
			}

			var result = appender.TryAppend(inFlight, tick, out nextRetryTick);
			if (result != AiLogAppendResult.RetryableFailure || appender.IsTerminal)
				inFlight = null;
		}
	}
}
