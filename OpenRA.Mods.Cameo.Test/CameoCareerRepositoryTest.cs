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

			Assert.That(repository.Append("match-one", match), Is.EqualTo(CameoCareerAppendResult.Appended));
			Assert.That(repository.Append("match-one", match), Is.EqualTo(CameoCareerAppendResult.AlreadyPresent));

			var loaded = repository.Load();
			Assert.That(loaded.Status, Is.EqualTo(CameoCareerLoadStatus.Loaded));
			Assert.That(loaded.Profile.Matches, Has.Count.EqualTo(1));
			Assert.That(loaded.Profile.Matches["match-one"].Faction, Is.EqualTo("atreides"));
			Assert.That(loaded.Profile.Matches["match-one"].UnitsKilled, Is.EqualTo(12));
			Assert.That(loaded.Profile.Matches["match-one"].GameTimestep, Is.EqualTo(40));
			Assert.That(loaded.Profile.Matches["match-one"].EnemyAssetsDestroyed, Is.EqualTo(1200));
			Assert.That(loaded.Profile.Matches["match-one"].AssetsOwned, Is.EqualTo(2400));
		}

		[Test]
		public void SchemaOneCareerUpgradesOnTheNextAppend()
		{
			var path = Path.Combine(directory, CameoCareerRepository.CareerFileName);
			File.WriteAllText(path, "SchemaVersion: 1\nCareerId: schema-one\n");
			var repository = new CameoCareerRepository(directory);

			Assert.That(repository.Load().Profile.SchemaVersion, Is.EqualTo(CameoCareerRepository.CurrentSchemaVersion));
			Assert.That(repository.Append("new-match", Match("atreides", "Won", 3)),
				Is.EqualTo(CameoCareerAppendResult.Appended));
			Assert.That(repository.Load().Profile.SchemaVersion, Is.EqualTo(CameoCareerRepository.CurrentSchemaVersion));
			Assert.That(File.ReadAllText(path),
				Does.Contain($"SchemaVersion: {CameoCareerRepository.CurrentSchemaVersion}"));
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
			Assert.That(repository.Append("blocked", Match("ordos", "Lost", 0)),
				Is.EqualTo(CameoCareerAppendResult.ReadOnly));
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
			Assert.That(repository.Append("first", Match("atreides", "Won", 1)),
				Is.EqualTo(CameoCareerAppendResult.Appended));
			Assert.That(repository.Append("second", Match("ordos", "Lost", 2)),
				Is.EqualTo(CameoCareerAppendResult.Appended));

			var path = Path.Combine(directory, CameoCareerRepository.CareerFileName);
			File.WriteAllText(path, "not: [valid career yaml");
			Assert.That(repository.Load().Status, Is.EqualTo(CameoCareerLoadStatus.RecoveredFromBackup));
			Assert.That(repository.Append("recovered", Match("harkonnen", "Won", 3)),
				Is.EqualTo(CameoCareerAppendResult.Appended));

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
		public void SummaryCalculatesTopMapsGameLengthsAndAssetTotals()
		{
			var profile = new CameoCareerProfile();
			var first = Match("atreides", "Won", 5);
			first.MapUid = "map-one";
			first.MapTitle = "Map One";
			first.DurationTicks = 100;
			first.EnemyAssetsDestroyed = 1000;
			first.AssetsOwned = 2000;
			var second = Match("atreides", "Lost", 4);
			second.MapUid = "map-one";
			second.MapTitle = "Map One";
			second.DurationTicks = 200;
			second.EnemyAssetsDestroyed = 500;
			second.AssetsOwned = 1500;
			var third = Match("ordos", "Won", 7);
			third.MapUid = "map-two";
			third.MapTitle = "Map Two";
			third.DurationTicks = 300;
			third.EnemyAssetsDestroyed = 2500;
			third.AssetsOwned = 3000;
			profile.Matches.Add("first", first);
			profile.Matches.Add("second", second);
			profile.Matches.Add("third", third);

			var summary = CameoStatistics.Summarize(profile);

			Assert.That(summary.TopMaps[0].Title, Is.EqualTo("Map One"));
			Assert.That(summary.TopMaps[0].Games, Is.EqualTo(2));
			Assert.That(summary.OverallGameLength.AverageMilliseconds, Is.EqualTo(8000));
			Assert.That(summary.OverallGameLength.MedianMilliseconds, Is.EqualTo(8000));
			Assert.That(summary.FactionGameLengths["atreides"].AverageMilliseconds, Is.EqualTo(6000));
			Assert.That(summary.FactionGameLengths["atreides"].MedianMilliseconds, Is.EqualTo(6000));
			Assert.That(summary.Factions["atreides"].EnemyAssetsDestroyed, Is.EqualTo(1500));
			Assert.That(summary.Factions["atreides"].AssetsOwned, Is.EqualTo(3500));
		}

		[Test]
		public void CareerIdentityAndMatchesSurviveMultipleReleaseVersions()
		{
			var firstRepository = new CameoCareerRepository(directory);
			var first = Match("atreides", "Won", 5);
			first.ModVersion = "playtest-20260721";
			Assert.That(firstRepository.Append("release-a-match", first),
				Is.EqualTo(CameoCareerAppendResult.Appended));
			var careerId = firstRepository.Load().Profile.CareerId;

			var nextReleaseRepository = new CameoCareerRepository(directory);
			var second = Match("ordos", "Lost", 2);
			second.ModVersion = "playtest-20260801";
			Assert.That(nextReleaseRepository.Append("release-b-match", second),
				Is.EqualTo(CameoCareerAppendResult.Appended));

			var loaded = nextReleaseRepository.Load().Profile;
			Assert.That(loaded.CareerId, Is.EqualTo(careerId));
			Assert.That(loaded.Matches, Has.Count.EqualTo(2));
			Assert.That(loaded.Matches["release-a-match"].ModVersion, Is.EqualTo("playtest-20260721"));
			Assert.That(loaded.Matches["release-b-match"].ModVersion, Is.EqualTo("playtest-20260801"));
		}

		[Test]
		public void ConcurrentRepositoriesPreserveEveryMatch()
		{
			const int count = 24;
			var results = new CameoCareerAppendResult[count];
			System.Threading.Tasks.Parallel.For(0, count, i =>
			{
				var repository = new CameoCareerRepository(directory);
				for (var attempt = 0; attempt < 100; attempt++)
				{
					results[i] = repository.Append("concurrent-" + i, Match("atreides", "Won", i));
					if (results[i] != CameoCareerAppendResult.RetryableFailure)
						break;

					System.Threading.Thread.Sleep(5);
				}
			});

			Assert.That(results, Is.All.EqualTo(CameoCareerAppendResult.Appended));
			Assert.That(new CameoCareerRepository(directory).Load().Profile.Matches, Has.Count.EqualTo(count));
		}

		[Test]
		public void TransientWriteFailureCanBeRetriedWithoutDuplicatingMatch()
		{
			var failNextSave = true;
			var repository = new CameoCareerRepository(directory, TimeSpan.FromMilliseconds(100), () =>
			{
				if (failNextSave)
				{
					failNextSave = false;
					throw new IOException("Injected transient write failure.");
				}
			});

			Assert.That(repository.Append("retry-match", Match("ordos", "Lost", 4)),
				Is.EqualTo(CameoCareerAppendResult.RetryableFailure));
			Assert.That(repository.Append("retry-match", Match("ordos", "Lost", 4)),
				Is.EqualTo(CameoCareerAppendResult.Appended));
			Assert.That(repository.Append("retry-match", Match("ordos", "Lost", 4)),
				Is.EqualTo(CameoCareerAppendResult.AlreadyPresent));
			Assert.That(repository.Load().Profile.Matches, Has.Count.EqualTo(1));
		}

		[Test]
		public void PendingMatchRetriesAtGameOverAndBecomesTerminalExactlyOnce()
		{
			var appender = new SequencedAppender(
				CameoCareerAppendResult.RetryableFailure,
				CameoCareerAppendResult.Appended);
			var pending = new PendingCameoCareerMatch(appender, "game-over-match", Match("atreides", "Won", 7));

			Assert.That(pending.TryAppend(), Is.EqualTo(CameoCareerAppendResult.RetryableFailure));
			Assert.That(pending.IsTerminal, Is.False);
			Assert.That(pending.TryAppend(), Is.EqualTo(CameoCareerAppendResult.Appended));
			Assert.That(pending.IsTerminal, Is.True);
			Assert.That(pending.TryAppend(), Is.EqualTo(CameoCareerAppendResult.AlreadyPresent));
			Assert.That(appender.CallCount, Is.EqualTo(2));
		}

		sealed class SequencedAppender : ICameoCareerAppender
		{
			readonly CameoCareerAppendResult[] results;

			public int CallCount { get; private set; }

			public SequencedAppender(params CameoCareerAppendResult[] results)
			{
				this.results = results;
			}

			public CameoCareerAppendResult Append(string recordId, CareerMatchRecord match)
			{
				return results[CallCount++];
			}
		}

		static CareerMatchRecord Match(string faction, string outcome, int unitsKilled)
		{
			return new CareerMatchRecord
			{
				RecordedUtc = "2026-07-21T00:00:00.0000000Z",
				Faction = faction,
				Outcome = outcome,
				UnitsKilled = unitsKilled,
				GameTimestep = 40,
				EnemyAssetsDestroyed = 1200,
				AssetsOwned = 2400
			};
		}
	}
}
