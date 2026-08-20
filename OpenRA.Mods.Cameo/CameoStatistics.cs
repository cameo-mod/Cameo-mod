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
		public long EnemyAssetsDestroyed;
		public long AssetsOwned;
	}

	public sealed class GameLengthStatistics
	{
		public readonly int Games;
		public readonly double AverageMilliseconds;
		public readonly double MedianMilliseconds;

		public GameLengthStatistics(int games, double averageMilliseconds, double medianMilliseconds)
		{
			Games = games;
			AverageMilliseconds = averageMilliseconds;
			MedianMilliseconds = medianMilliseconds;
		}
	}

	public sealed class MapPlayStatistics
	{
		public readonly string Title;
		public readonly int Games;

		public MapPlayStatistics(string title, int games)
		{
			Title = title;
			Games = games;
		}
	}

	public sealed class CareerStatisticsSummary
	{
		public readonly Dictionary<string, FactionStatistics> Factions;
		public readonly Dictionary<string, GameLengthStatistics> FactionGameLengths;
		public readonly GameLengthStatistics OverallGameLength;
		public readonly IReadOnlyList<MapPlayStatistics> TopMaps;

		public CareerStatisticsSummary(
			Dictionary<string, FactionStatistics> factions,
			Dictionary<string, GameLengthStatistics> factionGameLengths,
			GameLengthStatistics overallGameLength,
			IReadOnlyList<MapPlayStatistics> topMaps)
		{
			Factions = factions;
			FactionGameLengths = factionGameLengths;
			OverallGameLength = overallGameLength;
			TopMaps = topMaps;
		}
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

		public static CareerStatisticsSummary LoadSummary()
		{
			var profile = new CameoCareerRepository(Platform.SupportDir).LoadOrImportLegacy().Profile;
			return Summarize(profile);
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
				stats.EnemyAssetsDestroyed += match.EnemyAssetsDestroyed;
				stats.AssetsOwned += match.AssetsOwned;
			}

			return result;
		}

		public static CareerStatisticsSummary Summarize(CameoCareerProfile profile)
		{
			var factions = Aggregate(profile);
			var completedMatches = profile.Matches.Values
				.Where(IsCompletedMatch)
				.ToList();

			var gameLengths = completedMatches
				.Where(m => m.DurationTicks > 0)
				.GroupBy(m => m.Faction, StringComparer.Ordinal)
				.ToDictionary(
					g => g.Key,
					g => CalculateGameLengths(g.Select(DurationMilliseconds)),
					StringComparer.Ordinal);

			var overallGameLength = CalculateGameLengths(completedMatches
				.Where(m => m.DurationTicks > 0)
				.Select(DurationMilliseconds));

			var topMaps = completedMatches
				.Where(m => !string.IsNullOrEmpty(m.MapUid) || !string.IsNullOrEmpty(m.MapTitle))
				.GroupBy(m => string.IsNullOrEmpty(m.MapUid) ? "title:" + m.MapTitle : "uid:" + m.MapUid,
					StringComparer.Ordinal)
				.Select(g =>
				{
					var title = g.Select(m => m.MapTitle).FirstOrDefault(t => !string.IsNullOrEmpty(t));
					return new MapPlayStatistics(string.IsNullOrEmpty(title) ? g.First().MapUid : title, g.Count());
				})
				.OrderByDescending(m => m.Games)
				.ThenBy(m => m.Title, StringComparer.CurrentCultureIgnoreCase)
				.Take(10)
				.ToList();

			return new CareerStatisticsSummary(factions, gameLengths, overallGameLength, topMaps);
		}

		static bool IsCompletedMatch(CareerMatchRecord match)
		{
			return !string.IsNullOrEmpty(match.Faction) &&
				(string.Equals(match.Outcome, "Won", StringComparison.Ordinal) ||
					string.Equals(match.Outcome, "Lost", StringComparison.Ordinal));
		}

		static double DurationMilliseconds(CareerMatchRecord match)
		{
			// Schema 1 records predate GameTimestep. Cameo's historical/default timestep is 40 ms.
			return (double)match.DurationTicks * (match.GameTimestep > 0 ? match.GameTimestep : 40);
		}

		static GameLengthStatistics CalculateGameLengths(IEnumerable<double> durations)
		{
			var ordered = durations.OrderBy(d => d).ToArray();
			if (ordered.Length == 0)
				return new GameLengthStatistics(0, 0, 0);

			var middle = ordered.Length / 2;
			var median = ordered.Length % 2 == 0
				? (ordered[middle - 1] + ordered[middle]) / 2
				: ordered[middle];
			return new GameLengthStatistics(ordered.Length, ordered.Average(), median);
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
				ResourcesSpent = source.ResourcesSpent,
				EnemyAssetsDestroyed = source.EnemyAssetsDestroyed,
				AssetsOwned = source.AssetsOwned
			};
		}
	}
}
