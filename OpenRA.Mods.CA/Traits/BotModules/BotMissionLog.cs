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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// The execution lifecycle of ONE attempt (docs/design/AI_MISSION_CARDS.md §2.2, schema `mission-card/1`). An attempt
	/// exists only from COMMIT — fransotto, 2026-09-30: a mission can be published and denied by every commander without
	/// any execution ever existing. Terminal: SUCCESS, FAILED (the units were lost), RELEASED (handed back, no verdict).
	/// </summary>
	public enum BotMissionAttemptState { Committed, Progressing, Stalled, Recover, Success, Failed, Released }

	/// <summary>
	/// Mission-level events: the card's own story, observable apart from any attempt. DENIED is bid/mission feedback (no
	/// executor took it); DORMANT puts the card on the shelf; REOPENED takes it off again (new recon, a changed Best Read,
	/// a rest that ended). The card is the strategic memory; attempts are evidence written back to it.
	/// </summary>
	public enum BotMissionEvent { Published, Denied, Dormant, Reopened }

	/// <summary>The closed reason set. Project-private reasons carry an `x_` prefix (e.g. `x_frans_board_closed`).</summary>
	public static class BotMissionReasons
	{
		public const string NoUnits = "no_units";
		public const string Unreachable = "unreachable";
		public const string Undeployable = "undeployable";
		public const string Reserved = "reserved";
		public const string Outmatched = "outmatched";
		public const string TargetGone = "target_gone";
		public const string Timeout = "timeout";
		public const string Stuck = "stuck";
		public const string Superseded = "superseded";
		public const string LostUnits = "lost_units";
		public const string Done = "done";

		/// <summary>The executor's order ended (the unit went idle) without a result either way.</summary>
		public const string Dropped = "dropped";

		public static readonly IReadOnlyCollection<string> Shared = new[]
		{
			NoUnits, Unreachable, Undeployable, Reserved, Outmatched, TargetGone, Timeout, Stuck, Superseded, LostUnits, Done, Dropped
		};

		/// <summary>Null (no reason), one of the shared reasons, or an `x_` private reason of lowercase words.</summary>
		public static bool IsValid(string reason) =>
			reason == null || Shared.Contains(reason)
				|| (reason.Length > 2 && reason.StartsWith("x_", System.StringComparison.Ordinal)
					&& reason.All(c => c == '_' || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')));
	}

	/// <summary>
	/// One record: a mission-level event when <see cref="Event"/> is set (no attempt), otherwise an attempt transition.
	/// Identity is the composite (game_uid, mission_id, attempt) — unique within one match, never across matches.
	/// Everything except the ids and the state/event is optional context.
	/// </summary>
	public sealed class BotMissionRecord
	{
		public Player Player;
		public string MissionId;
		public BotMissionEvent? Event;
		public int Attempt;
		public BotMissionAttemptState State;
		public string Reason;
		public string Executor;
		public string MissionType;
		public int? RegionIndex;
		public CPos? TargetCell;
		public int? Units;
		public int? Value;
		public int Tick;
	}

	/// <summary>
	/// Receives every mission record, e.g. Cameo's AiMissionLogWriter (World actor), which appends
	/// `cameo-ai-missions.jsonl` record-only (DESIGN §21.1). Implemented on the World actor.
	/// </summary>
	public interface IBotMissionRecordSink
	{
		void MissionRecorded(BotMissionRecord record);
	}

	/// <summary>
	/// MC1 (AI_MASTER_PLAN §3): the ONE writer of mission-card transitions for every bot stack — the genericbot squad
	/// layer, the master AI, the MCV and engineer owners, and the vendored Fransbot broker all emit through here, so
	/// both logs share one line format and one archive format. It never changes a decision: one plain-text debug.log
	/// line ("the General's log") plus a record handed to the World's sinks.
	/// </summary>
	public static class BotMissionLog
	{
		public static string StateName(BotMissionAttemptState state) => state.ToString().ToUpperInvariant();

		public static string EventName(BotMissionEvent e) => e.ToString().ToUpperInvariant();

		public static bool IsTerminal(BotMissionAttemptState state) =>
			state is BotMissionAttemptState.Success or BotMissionAttemptState.Failed or BotMissionAttemptState.Released;

		/// <summary>The debug.log line of a mission-level event. Grep key: `MISSION &lt;id&gt;`.</summary>
		public static string FormatEventLine(string playerName, string missionId, BotMissionEvent e, string reason, string by, int tick)
		{
			var line = $"AI {playerName}: MISSION {missionId} {EventName(e)}";
			if (reason != null)
				line += $" reason={reason}";

			return line + $" by={(string.IsNullOrEmpty(by) ? "?" : by)} tick={tick}";
		}

		/// <summary>The debug.log line, free of world state so it can be tested. Grep key: `MISSION &lt;id&gt; ATTEMPT &lt;n&gt;`.</summary>
		public static string FormatLine(string playerName, string missionId, int attempt, BotMissionAttemptState state,
			string reason, string executor, int tick)
		{
			var line = $"AI {playerName}: MISSION {missionId} ATTEMPT {attempt} {StateName(state)}";
			if (reason != null)
				line += $" reason={reason}";

			return line + $" by={(string.IsNullOrEmpty(executor) ? "?" : executor)} tick={tick}";
		}

		public static void Write(BotMissionRecord record)
		{
			if (record?.Player == null || string.IsNullOrEmpty(record.MissionId))
				return;

			// A reason outside the shared set is a contract bug in the emitter: write it, but mark it so the story tool
			// and tests catch it instead of the archive silently growing a dialect.
			if (!BotMissionReasons.IsValid(record.Reason))
				record.Reason = "x_invalid_" + new string(record.Reason.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

			var world = record.Player.World;
			record.Tick = world.WorldTick;
			Log.Write("debug", record.Event is BotMissionEvent e
				? FormatEventLine(record.Player.InternalName, record.MissionId, e, record.Reason, record.Executor, record.Tick)
				: FormatLine(record.Player.InternalName, record.MissionId, record.Attempt, record.State, record.Reason, record.Executor, record.Tick));

			foreach (var sink in world.WorldActor.TraitsImplementing<IBotMissionRecordSink>())
				sink.MissionRecorded(record);
		}
	}
}
