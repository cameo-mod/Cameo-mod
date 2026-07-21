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
using System.IO;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class CameoCareerRepositoryTest
	{
		string directory;

		[OneTimeSetUp]
		public void SetUpLogging()
		{
			Log.AddChannel("debug", "");
		}

		[SetUp]
		public void SetUp()
		{
			directory = Path.Combine(Path.GetTempPath(), "cameo-career-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		[Test]
		public void AppendRoundTripsAndRejectsDuplicateRecordIds()
		{
			var repository = new CameoCareerRepository(directory);
			var match = Match("atreides", "Won", 12);

			Assert.That(repository.Append("match-one", match), Is.True);
			Assert.That(repository.Append("match-one", match), Is.False);

			var loaded = repository.Load();
			Assert.That(loaded.Status, Is.EqualTo(CameoCareerLoadStatus.Loaded));
			Assert.That(loaded.Profile.Matches, Has.Count.EqualTo(1));
			Assert.That(loaded.Profile.Matches["match-one"].Faction, Is.EqualTo("atreides"));
			Assert.That(loaded.Profile.Matches["match-one"].UnitsKilled, Is.EqualTo(12));
		}

		[Test]
		public void FutureSchemaIsReadOnlyAndRemainsUntouched()
		{
			var path = Path.Combine(directory, CameoCareerRepository.CareerFileName);
			const string future = "SchemaVersion: 999\nCareerId: future-career\nFutureData: keep-me\n";
			File.WriteAllText(path, future);
			var repository = new CameoCareerRepository(directory);

			var loaded = repository.Load();
			Assert.That(loaded.Status, Is.EqualTo(CameoCareerLoadStatus.UnsupportedFutureVersion));
			Assert.That(loaded.CanWrite, Is.False);
			Assert.That(repository.Append("blocked", Match("ordos", "Lost", 0)), Is.False);
			Assert.That(File.ReadAllText(path), Is.EqualTo(future));
		}

		[Test]
		public void LegacyTotalsAreImportedWithoutChangingTheLegacyFile()
		{
			var legacyPath = Path.Combine(directory, CameoCareerRepository.LegacyFileName);
			const string legacy = "atreides:\n\tGamesPlayed: 3\n\tGamesWon: 2\n\tGamesLost: 1\n\tUnitsKilled: 14\n";
			File.WriteAllText(legacyPath, legacy);
			var repository = new CameoCareerRepository(directory);

			var loaded = repository.LoadOrImportLegacy();
			Assert.That(loaded.Status, Is.EqualTo(CameoCareerLoadStatus.Loaded));
			Assert.That(loaded.Profile.LegacyTotals["atreides"].GamesPlayed, Is.EqualTo(3));
			Assert.That(loaded.Profile.LegacyTotals["atreides"].UnitsKilled, Is.EqualTo(14));
			Assert.That(File.ReadAllText(legacyPath), Is.EqualTo(legacy));
			Assert.That(File.Exists(Path.Combine(directory, CameoCareerRepository.CareerFileName)), Is.True);
		}

		[Test]
		public void BackupRecoveryPreservesInvalidPrimaryBeforeWriting()
		{
			var repository = new CameoCareerRepository(directory);
			Assert.That(repository.Append("first", Match("atreides", "Won", 1)), Is.True);
			Assert.That(repository.Append("second", Match("ordos", "Lost", 2)), Is.True);

			var path = Path.Combine(directory, CameoCareerRepository.CareerFileName);
			File.WriteAllText(path, "not: [valid career yaml");
			Assert.That(repository.Load().Status, Is.EqualTo(CameoCareerLoadStatus.RecoveredFromBackup));
			Assert.That(repository.Append("recovered", Match("harkonnen", "Won", 3)), Is.True);

			Assert.That(Directory.GetFiles(directory, "cameo-career.yaml.invalid-*"), Has.Length.EqualTo(1));
			var loaded = repository.Load();
			Assert.That(loaded.Profile.Matches.ContainsKey("first"), Is.True);
			Assert.That(loaded.Profile.Matches.ContainsKey("recovered"), Is.True);
		}

		[Test]
		public void AggregateCombinesLegacyTotalsAndRecordedMatches()
		{
			var profile = new CameoCareerProfile();
			profile.LegacyTotals.Add("atreides", new FactionStatistics
			{
				GamesPlayed = 2,
				GamesWon = 1,
				GamesLost = 1,
				UnitsKilled = 10
			});
			profile.Matches.Add("new-match", Match("atreides", "Won", 5));

			var aggregate = CameoStatistics.Aggregate(profile)["atreides"];
			Assert.That(aggregate.GamesPlayed, Is.EqualTo(3));
			Assert.That(aggregate.GamesWon, Is.EqualTo(2));
			Assert.That(aggregate.GamesLost, Is.EqualTo(1));
			Assert.That(aggregate.UnitsKilled, Is.EqualTo(15));
		}

		[Test]
		public void CareerIdentityAndMatchesSurviveMultipleReleaseVersions()
		{
			var firstRepository = new CameoCareerRepository(directory);
			var first = Match("atreides", "Won", 5);
			first.ModVersion = "playtest-20260721";
			Assert.That(firstRepository.Append("release-a-match", first), Is.True);
			var careerId = firstRepository.Load().Profile.CareerId;

			var nextReleaseRepository = new CameoCareerRepository(directory);
			var second = Match("ordos", "Lost", 2);
			second.ModVersion = "playtest-20260801";
			Assert.That(nextReleaseRepository.Append("release-b-match", second), Is.True);

			var loaded = nextReleaseRepository.Load().Profile;
			Assert.That(loaded.CareerId, Is.EqualTo(careerId));
			Assert.That(loaded.Matches, Has.Count.EqualTo(2));
			Assert.That(loaded.Matches["release-a-match"].ModVersion, Is.EqualTo("playtest-20260721"));
			Assert.That(loaded.Matches["release-b-match"].ModVersion, Is.EqualTo("playtest-20260801"));
		}

		static CareerMatchRecord Match(string faction, string outcome, int unitsKilled)
		{
			return new CareerMatchRecord
			{
				RecordedUtc = "2026-07-21T00:00:00.0000000Z",
				Faction = faction,
				Outcome = outcome,
				UnitsKilled = unitsKilled
			};
		}
	}
}
