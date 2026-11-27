#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

#if CAMEO_DEV_LOGGING
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// DEV only (training and dev logging exist only in the DEV build; this whole file is compiled out of an official build). The classifier seam of the raid rows: what the OBSERVATION classifier reports for an enemy owner's style
	/// (null = unseen). A bot never reads an enemy's personality or any internal state; the same classifier works against human players. No implementation exists on this branch, so every row carries a null style (the learner reads
	/// that as the exact three-axis value) until a classifier trait implements it.
	/// </summary>
	public interface IBotObservedStyleSource
	{
		string ObservedStyle(Player enemy);
	}

	/// <summary>
	/// RAID-HARV-01 P3: the pure builders of the two DEV rows the python extractor (tools/ai/extract_raid_harv_signals.py) reads. The schema is defined by that file's docstring; tools/tests/raid_harv_emitter_rows.jsonl pins the exact lines
	/// from both sides. One row per sample, never per tick; no newline ever enters a row.
	/// </summary>
	internal static class RaidHarvLogRows
	{
		public const string SeatEvent = "raid_harv_seat";
		public const string SignalEvent = "raid_harv_signal";

		/// <summary>The id charset of the fitter (game ids and style names become file and table keys).</summary>
		public static bool ValidId(string s)
		{
			if (string.IsNullOrEmpty(s) || s.Length > 128)
				return false;

			foreach (var c in s)
				if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '.' or '-'))
					return false;

			return true;
		}

		static void Str(StringBuilder b, string value)
		{
			if (value == null)
			{
				b.Append("null");
				return;
			}

			b.Append('"');
			foreach (var c in value)
			{
				switch (c)
				{
					case '"': b.Append("\\\""); break;
					case '\\': b.Append("\\\\"); break;
					default:
						if (c < ' ')
							b.Append("\\u").Append(((int)c).ToString("x4"));
						else
							b.Append(c);

						break;
				}
			}

			b.Append('"');
		}

		static string Bool(bool v) => v ? "true" : "false";

		/// <summary>The roster row: ONE per seat per game, EVERY enemy player of the seat (player id and faction, public lobby data), whether or not a raid ever meets it.</summary>
		public static string Seat(string gameId, string seat, string personality, string ownFaction, IReadOnlyList<(string Player, string Faction)> enemies)
		{
			var b = new StringBuilder(256);
			b.Append("{\"event\":\"").Append(SeatEvent).Append("\",\"game_id\":");
			Str(b, gameId);
			b.Append(",\"seat\":");
			Str(b, seat);
			b.Append(",\"personality\":");
			Str(b, string.IsNullOrEmpty(personality) ? null : personality);
			b.Append(",\"own_faction\":");
			Str(b, string.IsNullOrEmpty(ownFaction) ? null : ownFaction);
			b.Append(",\"enemies\":[");
			for (var i = 0; i < enemies.Count; i++)
			{
				if (i > 0)
					b.Append(',');

				b.Append("{\"player\":");
				Str(b, enemies[i].Player);
				b.Append(",\"faction\":");
				Str(b, enemies[i].Faction);
				b.Append('}');
			}

			return b.Append("]}").ToString();
		}

		/// <summary>One sample row. The target player and style are the snapshot taken AT THE PICK and repeated unchanged for the raid's life.</summary>
		public static string Signal(string gameId, string seat, int tick, string raidId, string targetPlayer, string targetStyle, RaidKey key, string payload)
		{
			var b = new StringBuilder(256);
			b.Append("{\"event\":\"").Append(SignalEvent).Append("\",\"game_id\":");
			Str(b, gameId);
			b.Append(",\"seat\":");
			Str(b, seat);
			b.Append(",\"tick\":").Append(tick).Append(",\"raid_id\":");
			Str(b, raidId);
			b.Append(",\"target_player\":");
			Str(b, targetPlayer);
			b.Append(",\"target_style\":");
			Str(b, targetStyle);
			b.Append(",\"key\":");
			Str(b, RaidHarvKeys.Spec(key).Id);
			return b.Append(',').Append(payload).Append('}').ToString();
		}

		// ---- payloads (the part after "key", without braces) ---------------------------------------------------------------------------

		public static string LostReacquired(int ticks, int current) => "\"kind\":\"reacquired\",\"ticks\":" + Math.Max(0, ticks) + ",\"current\":" + Math.Max(1, current);

		public static string LostExhausted(int current) => "\"kind\":\"exhausted\",\"current\":" + Math.Max(1, current);

		public static string RadiusReacquired(int distance, int current) => "\"kind\":\"reacquired\",\"distance\":" + Math.Max(0, distance) + ",\"current\":" + Math.Max(1, current);

		public static string RadiusExhausted(bool farSeen, int current) => "\"kind\":\"exhausted\",\"far_seen\":" + Bool(farSeen) + ",\"current\":" + Math.Max(1, current);

		public static string MoveRefresh(int drift, bool visiblePrev, bool visibleNow, string phase, int sinceChange) =>
			"\"drift\":" + Math.Max(0, drift) + ",\"visible_prev\":" + Bool(visiblePrev) + ",\"visible_now\":" + Bool(visibleNow) + ",\"phase\":\"" + phase + "\",\"since_change\":" + Math.Max(0, sinceChange);

		public static string ReqMargin(bool killed, int lossPermille) => "\"killed\":" + Bool(killed) + ",\"loss_permille\":" + Math.Clamp(lossPermille, 0, 1000);

		public static string IncidentLoss(RetreatOutcome o) => "\"lost_half_within_600\":" + Bool(o.LostHalfWithin600) + ",\"restrike_clean\":" + Bool(o.RestrikeClean);

		public static string IncidentWindow(int spanTicks, int current) => "\"span_ticks\":" + Math.Max(0, spanTicks) + ",\"current\":" + Math.Max(1, current);

		public static string IncidentDecay(DecayOutcome o) => "\"reincident_within_1500\":" + Bool(o.ReincidentWithin1500) + ",\"passed_clean_before_decay\":" + Bool(o.PassedCleanBeforeDecay);

		public static string WaypointSpacing(bool leftLane) => "\"left_lane\":" + Bool(leftLane);

		public static string DefenceRadius(bool outsideDefenderTookPart) => "\"outside_defender_took_part\":" + Bool(outsideDefenderTookPart);
	}

	/// <summary>
	/// The 600-tick watch after a loss incident's retreat (raid.incident_loss). The squad lost half or more of the value it had at the incident = too loose; else a re-strike on the same target that started inside the horizon and met no further
	/// incident there = too tight. Neither = no sample (the row is still written, the extractor skips it).
	/// </summary>
	internal sealed class RaidIncidentLossWatch
	{
		public const int HorizonTicks = 600;

		public readonly int StartTick;
		public readonly int TotalValue;
		public bool RestrikeStarted;
		public bool FurtherIncident;

		public RaidIncidentLossWatch(int startTick, int totalValue)
		{
			StartTick = startTick;
			TotalValue = totalValue;
		}

		/// <summary>Null while the watch is open; the outcome once it resolves (half lost: at once; otherwise at the horizon).</summary>
		public RetreatOutcome? Step(int now, long lostValue)
		{
			if (TotalValue > 0 && lostValue * 2 >= TotalValue)
				return new RetreatOutcome(true, false);

			if (now - StartTick >= HorizonTicks)
				return new RetreatOutcome(false, RestrikeStarted && !FurtherIncident);

			return null;
		}
	}

	/// <summary>
	/// The watch of one incident cell (raid.incident_decay): from the incident until 1500 ticks after the cell's decay. A new incident at the same cell inside that window after the decay = the memory was dropped too early (up); a LATER raid's
	/// lead passing the cell before the decay with no incident there = remembered too long (down). The raid that made the incident never counts as "a later raid".
	/// </summary>
	internal sealed class RaidIncidentDecayWatch
	{
		public const int AfterDecayTicks = 1500;
		public const int NearCells = 2;

		public readonly CPos Cell;
		public readonly int StartTick;
		public readonly int DecayTicks;
		public readonly string OriginRaidId;
		public bool Reincident;
		bool passedClean;
		bool incidentBeforeDecay;

		public RaidIncidentDecayWatch(CPos cell, int startTick, int decayTicks, string originRaidId)
		{
			Cell = cell;
			StartTick = startTick;
			DecayTicks = Math.Max(1, decayTicks);
			OriginRaidId = originRaidId;
		}

		int DecayTick => StartTick + DecayTicks;

		static bool Near(CPos a, CPos b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) <= NearCells;

		/// <summary>A raid's lead cell at a check tick.</summary>
		public void Passing(int now, CPos lead, string raidId)
		{
			if (raidId != OriginRaidId && now > StartTick && now < DecayTick && Near(lead, Cell))
				passedClean = true;
		}

		/// <summary>An incident (loss) of any raid at a cell.</summary>
		public void Incident(int now, CPos at, string raidId)
		{
			if (raidId == OriginRaidId || !Near(at, Cell))
				return;

			if (now > StartTick && now < DecayTick)
				incidentBeforeDecay = true;
			else if (now >= DecayTick && now <= DecayTick + AfterDecayTicks)
				Reincident = true;
		}

		/// <summary>Null while open; at the horizon (or at once on a re-incident) the outcome. A watch with neither is a no-sample outcome (both false).</summary>
		public DecayOutcome? Step(int now)
		{
			if (Reincident)
				return new DecayOutcome(true, false);

			if (now > DecayTick + AfterDecayTicks)
				return new DecayOutcome(false, passedClean && !incidentBeforeDecay);

			return null;
		}
	}

	/// <summary>
	/// The leg tracker of raid.waypoint_spacing: one leg is the stretch of the explored lane between two consecutive waypoints; it is "left" when the lead was ever more than <see cref="LaneCells"/> cells (Chebyshev) from every cell of that
	/// stretch (the engine replanned around something). A leg interrupted by a new plan or by another phase is dropped, never reported; a leg skipped between two samples is not reported either.
	/// </summary>
	internal sealed class RaidLegTracker
	{
		public const int LaneCells = 2;

		List<CPos> waypoints;
		List<CPos> lane;
		int legIndex = -1;
		int laneFrom;
		int laneTo;
		int maxDeviation;
		bool open;

		/// <summary>The lane (the full explored path, start first) that belongs to the waypoint list; call when a plan is adopted.</summary>
		public void SetPlan(List<CPos> planWaypoints, List<CPos> planLane)
		{
			waypoints = planWaypoints;
			lane = planLane;
			legIndex = -1;
			open = false;
		}

		/// <summary>The leg is no longer being walked (another phase).</summary>
		public void Drop() => open = false;

		public static int Deviation(CPos cell, IReadOnlyList<CPos> path, int from, int to)
		{
			var best = int.MaxValue;
			for (var i = Math.Max(0, from); i <= to && i < path.Count; i++)
				best = Math.Min(best, Math.Max(Math.Abs(cell.X - path[i].X), Math.Abs(cell.Y - path[i].Y)));

			return best;
		}

		/// <summary>One sample (the lead cell, the run's current waypoint list and index). Returns true/false for a leg that was completed since the previous sample (left the lane or not), null otherwise.</summary>
		public bool? Sample(List<CPos> currentWaypoints, int index, CPos lead)
		{
			if (lane == null || !ReferenceEquals(currentWaypoints, waypoints))
				return null;

			bool? done = null;
			if (open && index == legIndex + 1)
				done = maxDeviation > LaneCells;

			if (!open || index != legIndex)
			{
				open = false;
				legIndex = index;
				if (index >= 0 && index < waypoints.Count)
				{
					laneTo = lane.IndexOf(waypoints[index]);
					laneFrom = index == 0 ? 0 : lane.IndexOf(waypoints[index - 1]);
					open = laneTo >= 0 && laneFrom >= 0 && laneFrom <= laneTo;
					maxDeviation = 0;
				}
			}

			if (open)
				maxDeviation = Math.Max(maxDeviation, Deviation(lead, lane, laneFrom, laneTo));

			return done;
		}
	}

	/// <summary>The snapshot and the running observations of ONE raid (one squad's run). Pure values: nothing here reads the world.</summary>
	internal sealed class RaidLog
	{
		/// <summary>Armed enemies within this many cells of the squad at a loss are the ones that "took part" (raid.defence_radius).</summary>
		public const int FightCells = 15;

		public string RaidId;

		/// <summary>The owner of the target and the observed style, snapshotted AT THE PICK and repeated on every row of the raid.</summary>
		public string TargetPlayer;
		public string TargetStyle;

		/// <summary>A lost-target episode (raid.lost_recon / raid.recon_radius): its start tick, the cell the target was last seen at, the radius and budget in force.</summary>
		public int EpisodeStartTick = -1;
		public CPos EpisodeLostCell;
		public int EpisodeRadius;

		/// <summary>raid.move_refresh: the tick of the last sample and whether the target was visible then.</summary>
		public int LastRefreshTick = -1;
		public bool LastRefreshVisible = true;

		/// <summary>raid.incident_window: the squad's value at the previous check and the first check that saw it fall inside the current window.</summary>
		public int PrevValue;
		public int FirstLossTick = -1;

		public CPos LastLeadCell;
		public readonly RaidLegTracker Legs = new();

		/// <summary>raid.defence_radius: the armed enemy actors this player saw within the radius of the target at the pick (not defenders first seen outside it), and whether one first seen outside took part at a loss.</summary>
		public readonly HashSet<uint> InsideAtPick = new();
		public bool OutsideDefenderTookPart;
	}
}
#endif
