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
using System.Globalization;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Cameo
{
	public sealed class CareerMatchRecord
	{
		public string RecordedUtc = "";
		public string Outcome = "";
		public string Faction = "";
		public string GameUid = "";
		public string MapUid = "";
		public string MapTitle = "";
		public string ModVersion = "";
		public int DurationTicks;
		public int UnitsKilled;
		public int BuildingsKilled;
		public int UnitsLost;
		public int BuildingsLost;
		public long ResourcesEarned;
		public long ResourcesSpent;
	}

	public sealed class CameoCareerProfile
	{
		public int SchemaVersion;
		public string CareerId = "";
		public string CreatedUtc = "";
		public string LastUpdatedUtc = "";
		public readonly Dictionary<string, FactionStatistics> LegacyTotals = new(StringComparer.Ordinal);
		public readonly Dictionary<string, CareerMatchRecord> Matches = new(StringComparer.Ordinal);
	}

	public enum CameoCareerLoadStatus
	{
		Missing,
		Loaded,
		RecoveredFromBackup,
		UnsupportedFutureVersion,
		Invalid
	}

	public sealed class CameoCareerLoadResult
	{
		public readonly CameoCareerProfile Profile;
		public readonly CameoCareerLoadStatus Status;
		public readonly string Error;

		public bool CanWrite => Status != CameoCareerLoadStatus.UnsupportedFutureVersion &&
			Status != CameoCareerLoadStatus.Invalid;

		public CameoCareerLoadResult(CameoCareerProfile profile, CameoCareerLoadStatus status, string error = null)
		{
			Profile = profile;
			Status = status;
			Error = error;
		}
	}

	// Owns the durable, release-independent Cameo career file. The schema gate prevents an older
	// build from rewriting a career created by a newer build that it cannot understand.
	public sealed class CameoCareerRepository
	{
		public const int CurrentSchemaVersion = 1;
		public const string CareerFileName = "cameo-career.yaml";
		public const string LegacyFileName = "cameo-statistics.yaml";

		readonly string filePath;
		readonly string backupPath;
		readonly string legacyPath;

		public CameoCareerRepository(string supportDirectory)
		{
			filePath = Path.Combine(supportDirectory, CareerFileName);
			backupPath = filePath + ".bak";
			legacyPath = Path.Combine(supportDirectory, LegacyFileName);
		}

		public CameoCareerLoadResult Load()
		{
			if (!File.Exists(filePath))
				return new CameoCareerLoadResult(NewProfile(), CameoCareerLoadStatus.Missing);

			try
			{
				return Parse(filePath, CameoCareerLoadStatus.Loaded);
			}
			catch (Exception primaryError)
			{
				Log.Write("debug", $"Unable to load Cameo career '{filePath}': {primaryError}");
				if (File.Exists(backupPath))
				{
					try
					{
						return Parse(backupPath, CameoCareerLoadStatus.RecoveredFromBackup);
					}
					catch (Exception backupError)
					{
						Log.Write("debug", $"Unable to load Cameo career backup '{backupPath}': {backupError}");
						return new CameoCareerLoadResult(NewProfile(), CameoCareerLoadStatus.Invalid,
							$"Primary: {primaryError.Message}; Backup: {backupError.Message}");
					}
				}

				return new CameoCareerLoadResult(NewProfile(), CameoCareerLoadStatus.Invalid, primaryError.Message);
			}
		}

		public CameoCareerLoadResult LoadOrImportLegacy()
		{
			var loaded = Load();
			if (loaded.Status != CameoCareerLoadStatus.Missing || !File.Exists(legacyPath))
				return loaded;

			try
			{
				foreach (var node in MiniYaml.FromFile(legacyPath, false))
				{
					if (!string.IsNullOrEmpty(node.Key))
						loaded.Profile.LegacyTotals[node.Key] = FieldLoader.Load<FactionStatistics>(node.Value);
				}

				if (loaded.Profile.LegacyTotals.Count == 0)
					return loaded;

				Save(loaded.Profile);
				Log.Write("debug", $"Imported legacy Cameo statistics from '{legacyPath}'.");
				return new CameoCareerLoadResult(loaded.Profile, CameoCareerLoadStatus.Loaded);
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Unable to import legacy Cameo statistics '{legacyPath}': {e}");
				return loaded;
			}
		}

		public bool Append(string recordId, CareerMatchRecord match)
		{
			if (string.IsNullOrEmpty(recordId) || match == null)
				return false;

			var loaded = LoadOrImportLegacy();
			if (!loaded.CanWrite)
			{
				Log.Write("debug", $"Cameo career is read-only ({loaded.Status}); match '{recordId}' was not recorded.");
				return false;
			}

			if (loaded.Profile.Matches.ContainsKey(recordId))
				return false;

			try
			{
				if (loaded.Status == CameoCareerLoadStatus.RecoveredFromBackup)
					PreserveInvalidPrimary();

				loaded.Profile.Matches.Add(recordId, match);
				Save(loaded.Profile);
				return true;
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Unable to append match '{recordId}' to Cameo career '{filePath}': {e}");
				return false;
			}
		}

		public void Save(CameoCareerProfile profile)
		{
			if (profile.SchemaVersion != CurrentSchemaVersion)
				throw new InvalidOperationException(
					$"Refusing to write Cameo career schema {profile.SchemaVersion}; supported schema is {CurrentSchemaVersion}.");

			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			profile.LastUpdatedUtc = UtcNow();
			var temporaryPath = filePath + ".tmp-" + Guid.NewGuid().ToString("N");

			try
			{
				Serialize(profile).WriteToFile(temporaryPath);
				if (File.Exists(filePath))
					File.Replace(temporaryPath, filePath, backupPath, true);
				else
					File.Move(temporaryPath, filePath);
			}
			finally
			{
				if (File.Exists(temporaryPath))
					File.Delete(temporaryPath);
			}
		}

		CameoCareerLoadResult Parse(string path, CameoCareerLoadStatus successStatus)
		{
			var yaml = MiniYaml.FromFile(path, false).ToList();
			var versionNode = yaml.FirstOrDefault(n => n.Key == "SchemaVersion") ??
				throw new InvalidDataException("The Cameo career has no SchemaVersion.");
			if (!int.TryParse(versionNode.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
				throw new InvalidDataException($"Invalid Cameo career SchemaVersion '{versionNode.Value.Value}'.");

			if (version > CurrentSchemaVersion)
			{
				Log.Write("debug", $"Cameo career '{path}' uses future schema {version}; opening read-only.");
				return new CameoCareerLoadResult(new CameoCareerProfile { SchemaVersion = version },
					CameoCareerLoadStatus.UnsupportedFutureVersion);
			}
			if (version < 1)
				throw new InvalidDataException($"Unsupported Cameo career SchemaVersion '{version}'.");

			var profile = new CameoCareerProfile
			{
				SchemaVersion = version,
				CareerId = Value(yaml, "CareerId"),
				CreatedUtc = Value(yaml, "CreatedUtc"),
				LastUpdatedUtc = Value(yaml, "LastUpdatedUtc")
			};

			if (string.IsNullOrEmpty(profile.CareerId))
				throw new InvalidDataException("The Cameo career has no CareerId.");

			var legacy = yaml.FirstOrDefault(n => n.Key == "LegacyTotals");
			if (legacy != null)
				foreach (var node in legacy.Value.Nodes)
					profile.LegacyTotals[node.Key] = FieldLoader.Load<FactionStatistics>(node.Value);

			var matches = yaml.FirstOrDefault(n => n.Key == "Matches");
			if (matches != null)
				foreach (var node in matches.Value.Nodes)
					profile.Matches[node.Key] = FieldLoader.Load<CareerMatchRecord>(node.Value);

			return new CameoCareerLoadResult(profile, successStatus);
		}

		static IEnumerable<MiniYamlNode> Serialize(CameoCareerProfile profile)
		{
			yield return new MiniYamlNode("SchemaVersion", profile.SchemaVersion.ToString(CultureInfo.InvariantCulture));
			yield return new MiniYamlNode("CareerId", profile.CareerId);
			yield return new MiniYamlNode("CreatedUtc", profile.CreatedUtc);
			yield return new MiniYamlNode("LastUpdatedUtc", profile.LastUpdatedUtc);
			yield return new MiniYamlNode("LegacyTotals", "", profile.LegacyTotals
				.OrderBy(kv => kv.Key, StringComparer.Ordinal)
				.Select(kv => new MiniYamlNode(kv.Key, FieldSaver.Save(kv.Value))));
			yield return new MiniYamlNode("Matches", "", profile.Matches
				.OrderBy(kv => kv.Key, StringComparer.Ordinal)
				.Select(kv => new MiniYamlNode(kv.Key, FieldSaver.Save(kv.Value))));
		}

		static string Value(IEnumerable<MiniYamlNode> yaml, string key)
		{
			return yaml.FirstOrDefault(n => n.Key == key)?.Value.Value ?? "";
		}

		static CameoCareerProfile NewProfile()
		{
			var now = UtcNow();
			return new CameoCareerProfile
			{
				SchemaVersion = CurrentSchemaVersion,
				CareerId = Guid.NewGuid().ToString("D"),
				CreatedUtc = now,
				LastUpdatedUtc = now
			};
		}

		void PreserveInvalidPrimary()
		{
			if (!File.Exists(filePath))
				return;

			var suffix = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
			var preservedPath = filePath + ".invalid-" + suffix;
			var counter = 1;
			while (File.Exists(preservedPath))
				preservedPath = filePath + ".invalid-" + suffix + "-" + counter++;

			File.Move(filePath, preservedPath);
			Log.Write("debug", $"Preserved invalid Cameo career as '{preservedPath}'.");
		}

		static string UtcNow()
		{
			return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
		}
	}
}
