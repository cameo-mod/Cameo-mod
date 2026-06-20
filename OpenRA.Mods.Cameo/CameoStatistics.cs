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

	// Reads and writes the local, cross-session statistics file. The data is keyed by
	// faction internal name and lives next to the other per-user data under the support
	// directory, so it survives reinstalls of the mod itself.
	public static class CameoStatistics
	{
		const string FileName = "cameo-statistics.yaml";

		static string FilePath => Path.Combine(Platform.SupportDir, FileName);

		// Never throws: a missing or malformed file simply yields an empty set, so a corrupt
		// file can never crash the menu (the next Save overwrites it cleanly).
		public static Dictionary<string, FactionStatistics> Load()
		{
			var result = new Dictionary<string, FactionStatistics>();
			var path = FilePath;
			if (!File.Exists(path))
				return result;

			try
			{
				foreach (var node in MiniYaml.FromFile(path, false))
				{
					if (string.IsNullOrEmpty(node.Key))
						continue;

					result[node.Key] = FieldLoader.Load<FactionStatistics>(node.Value);
				}
			}
			catch (Exception)
			{
				return new Dictionary<string, FactionStatistics>();
			}

			return result;
		}

		public static void Save(Dictionary<string, FactionStatistics> stats)
		{
			try
			{
				stats
					.OrderBy(kv => kv.Key, StringComparer.Ordinal)
					.Select(kv => new MiniYamlNode(kv.Key, FieldSaver.Save(kv.Value)))
					.ToList()
					.WriteToFile(FilePath);
			}
			catch (Exception)
			{
				// Best-effort: failing to persist statistics must never interrupt the game.
			}
		}

		// Folds one completed game's results into the running total for a faction.
		public static void Record(string faction, bool won,
			int unitsKilled, int buildingsKilled, int unitsLost, int buildingsLost,
			long resourcesEarned, long resourcesSpent)
		{
			if (string.IsNullOrEmpty(faction))
				return;

			var stats = Load();
			if (!stats.TryGetValue(faction, out var s))
				stats[faction] = s = new FactionStatistics();

			s.GamesPlayed++;
			if (won)
				s.GamesWon++;
			else
				s.GamesLost++;

			s.UnitsKilled += unitsKilled;
			s.BuildingsKilled += buildingsKilled;
			s.UnitsLost += unitsLost;
			s.BuildingsLost += buildingsLost;
			s.ResourcesEarned += resourcesEarned;
			s.ResourcesSpent += resourcesSpent;

			Save(stats);
		}
	}
}
