using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	internal static class MissionBuildLineOracle
	{
		public const string Schema = "mission-card/2";
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
			// IdentityKey has exactly three colon-delimited fields. Replace only the target,
			// never a substring of the mission type, region or a prefix-overlapping slot.
			var parts = value.Split(':');
			if ((parts.Length == 3 && (parts[0] == "raid" || parts[0] == "recon" || parts[0] == "secure" || parts[0] == "defend")) ||
				(parts.Length == 4 && parts[0] == "capture"))
				parts[1] = parts[1] == "self" ? "self" : replacements.GetValueOrDefault(parts[1], "unknown");
			// Other producer grammars contain actor-type/actor-ID or numeric IDs, not player references.
			else if (parts[0] != "capture" && parts[0] != "frans" && parts[0] != "garrison_contest" && parts[0] != "veto")
				return "unknown";
			return string.Join(":", parts);
		}

	}
}
