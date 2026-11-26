#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// RM-03 (row rm03_yard_ramp, design RM_03_YARD_RAMP_DESIGN_02 49BFE42A): the construction-yard demand floor.
	/// A table of (minimum elapsed world tick, minimum own difficulty tier, desired yards) rows; the floor at a tick
	/// and tier is the largest desired count over the rows both bounds admit, so it can only grow with time and
	/// with tier (monotone by construction, whatever the row order). Inputs are the bot's own elapsed world ticks
	/// and its own difficulty tier only: no enemy, no personality (the planner's scale/utility result carries that,
	/// once). The tier is the difficulty ladder identity (BotDifficultyLadder; noob = NoobRank); an unrecognised or
	/// unranked bot type has no tier and gets floor 0. Pure, for the tests.
	/// </summary>
	public static class YardRamp
	{
		/// <summary>Upper bound on a row's desired yard count; a larger value rejects the table.</summary>
		public const int MaxDesired = 16;

		/// <summary>Bot types that never ramp, aliased or not (Opus 11-26): classic (A/B reference) and fransbot (frozen).</summary>
		public static readonly string[] UnrampedBotTypes = { "classic", "fransbot" };

		public readonly struct Row
		{
			public readonly int MinTick;
			public readonly int MinRank;
			public readonly int Desired;

			public Row(int minTick, int minRank, int desired)
			{
				MinTick = minTick;
				MinRank = minRank;
				Desired = desired;
			}
		}

		/// <summary>
		/// The ladder tier of a bot type: noob = NoobRank (-1), the canonical ladder = its index (easiest 0 ..
		/// cameogod 9). A ladder alias (BotDifficultyLadder.DefaultAliases: the exploit_* league bots play as hard)
		/// takes its alias's tier, one straight ladder (Opus 11-26). Null, so floor 0, for the unramped types
		/// (classic = the A/B reference; fransbot = frozen until RETIRE-FRANSBOT-01) and any empty or unlisted type.
		/// </summary>
		public static int? RankForBotType(string botType)
		{
			if (string.IsNullOrEmpty(botType))
				return null;

			foreach (var unramped in UnrampedBotTypes)
				if (string.Equals(botType, unramped, StringComparison.OrdinalIgnoreCase))
					return null;

			if (BotDifficultyLadder.DefaultAliases.TryGetValue(botType, out var alias))
				botType = alias;

			if (string.Equals(botType, BotDifficultyLadder.NoobBotType, StringComparison.OrdinalIgnoreCase))
				return BotDifficultyLadder.NoobRank;

			var rank = Array.FindIndex(BotDifficultyLadder.Difficulties, d => string.Equals(d, botType, StringComparison.OrdinalIgnoreCase));
			return rank < 0 ? (int?)null : rank;
		}

		/// <summary>The ladder tier of a player (null for a human or an unranked bot).</summary>
		public static int? RankFor(OpenRA.Player owner) => owner != null && owner.IsBot ? RankForBotType(owner.BotType) : null;

		/// <summary>
		/// Parses "minTick:tier:desired" entries (tier = a ladder bot type, e.g. "0:easiest:2", "12000:hard:3").
		/// Any malformed entry (wrong arity, negative or non-integer tick, unknown tier, desired outside 0..MaxDesired)
		/// rejects the WHOLE table: the result is empty (floor 0 everywhere) and <paramref name="error"/> names the
		/// first bad entry. A null or empty list is an empty table without error.
		/// </summary>
		public static IReadOnlyList<Row> Parse(string[] entries, out string error)
		{
			error = null;
			var rows = new List<Row>();
			if (entries == null)
				return rows;

			foreach (var raw in entries)
			{
				var parts = (raw ?? string.Empty).Split(':');
				if (parts.Length != 3
					|| !int.TryParse(parts[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var minTick)
					|| !int.TryParse(parts[2].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var desired)
					|| desired > MaxDesired)
				{
					error = raw ?? "(null)";
					return Array.Empty<Row>();
				}

				var rank = RankForBotType(parts[1].Trim());
				if (rank == null)
				{
					error = raw;
					return Array.Empty<Row>();
				}

				rows.Add(new Row(minTick, rank.Value, desired));
			}

			return rows;
		}

		/// <summary>
		/// The demand floor: the largest Desired over the rows with worldTick &gt;= MinTick and rank &gt;= MinRank;
		/// 0 when no row admits, the table is empty or the rank is unknown (null). Never negative.
		/// </summary>
		public static int Floor(int worldTick, int? rank, IReadOnlyList<Row> rows)
		{
			if (rank == null || rows == null)
				return 0;

			var floor = 0;
			for (var i = 0; i < rows.Count; i++)
			{
				var r = rows[i];
				if (worldTick >= r.MinTick && rank.Value >= r.MinRank && r.Desired > floor)
					floor = r.Desired;
			}

			return floor;
		}

		/// <summary>
		/// The floor's disclosure: did it decide the target? True only when it exceeds both the target it was applied to
		/// (scale/utility result) and the cover-all-fields floor (active + inflight, 0 when CoverAllFields is off).
		/// False = the ramp was immaterial on this request (the existing drivers already asked for at least as much).
		/// </summary>
		public static bool Material(int rampFloor, int preRampTarget, int coverFloor) => rampFloor > Math.Max(preRampTarget, coverFloor);
	}
}
