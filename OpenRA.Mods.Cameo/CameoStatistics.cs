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
using System.Linq;

namespace OpenRA.Mods.Cameo
{
	// Cumulative results for a single faction, accumulated across every completed game and
	// persisted between sessions. Public fields so FieldLoader/FieldSaver can round-trip it.
	public class FactionStatistics
	{
		public int GamesPlayed;
		public int GamesWon;
		public int GamesLost;
		public int UnitsKilled;
		public int BuildingsKilled;
		public int UnitsLost;
		public int BuildingsLost;
		public long ResourcesEarned;
		public long ResourcesSpent;
	}

	// Compatibility facade for the Statistics window. Durable data is owned by CameoCareerRepository;
	// the UI receives an aggregate so it does not depend on the on-disk career schema.
	public static class CameoStatistics
	{
		public static Dictionary<string, FactionStatistics> Load()
		{
			var profile = new CameoCareerRepository(Platform.SupportDir).LoadOrImportLegacy().Profile;
			return Aggregate(profile);
		}

		public static Dictionary<string, FactionStatistics> Aggregate(CameoCareerProfile profile)
		{
			var result = profile.LegacyTotals.ToDictionary(
				kv => kv.Key,
				kv => Clone(kv.Value),
				StringComparer.Ordinal);

			foreach (var match in profile.Matches.Values)
			{
				if (string.IsNullOrEmpty(match.Faction))
					continue;
				var won = string.Equals(match.Outcome, "Won", StringComparison.Ordinal);
				var lost = string.Equals(match.Outcome, "Lost", StringComparison.Ordinal);
				if (!won && !lost)
					continue;

				if (!result.TryGetValue(match.Faction, out var stats))
					result.Add(match.Faction, stats = new FactionStatistics());

				stats.GamesPlayed++;
				if (won)
					stats.GamesWon++;
				else if (lost)
					stats.GamesLost++;

				stats.UnitsKilled += match.UnitsKilled;
				stats.BuildingsKilled += match.BuildingsKilled;
				stats.UnitsLost += match.UnitsLost;
				stats.BuildingsLost += match.BuildingsLost;
				stats.ResourcesEarned += match.ResourcesEarned;
				stats.ResourcesSpent += match.ResourcesSpent;
			}

			return result;
		}

		static FactionStatistics Clone(FactionStatistics source)
		{
			return new FactionStatistics
			{
				GamesPlayed = source.GamesPlayed,
				GamesWon = source.GamesWon,
				GamesLost = source.GamesLost,
				UnitsKilled = source.UnitsKilled,
				BuildingsKilled = source.BuildingsKilled,
				UnitsLost = source.UnitsLost,
				BuildingsLost = source.BuildingsLost,
				ResourcesEarned = source.ResourcesEarned,
				ResourcesSpent = source.ResourcesSpent
			};
		}
	}
}
