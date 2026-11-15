using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class MissionEventDetachmentTest
	{
		[Test]
		public void OracleSourceIsPinned()
		{
			var path = Path.Combine(AppContext.BaseDirectory, "source", "MissionBuildLineOracle.cs");
			Assert.That(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
				Is.EqualTo("80CF4C3D585722FE7A0ADF0144DC9FE750C59E35D7BE2DF1D5122775C237C4CD"));
		}

		[TestCase("")]
		[TestCase("de-DE")]
		public void DetachedMissionMatchesOracleAndSurvivesSourceMutation(string cultureName)
		{
			var before = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = cultureName.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
				var record = new BotMissionRecord
				{
					MissionId = "raid:Multi10:r4", Attempt = 2, State = BotMissionAttemptState.Failed,
					Reason = "lost_units", Executor = "", Tick = 750, RegionIndex = 0,
					TargetCell = new CPos(-17, 0), UnitCell = new CPos(12, 27), Units = 0, Value = -1,
				};
				var identity = new PlayerIdentity(-1, "Multi0", null, null, true, false);
				var detached = MissionEvent.Detach(record, identity);
				var time = DateTime.UnixEpoch;
				var expected = MissionBuildLineOracle.BuildLine(record, "g", "m", "map", time);
				var actual = AiMissionLogWriterDev.BuildLine(detached, "g", "m", "map", time);
				Assert.That(actual, Is.EqualTo(expected));
				record.MissionId = "changed";
				record.Reason = null;
				record.TargetCell = new CPos(1, 1);
				record.Tick = 1;
				Assert.That(AiMissionLogWriterDev.BuildLine(detached, "g", "m", "map", time), Is.EqualTo(actual));
			}
			finally { CultureInfo.CurrentCulture = before; }
		}

		[Test]
		public void CellTypeIsDetachedAndReadOnly()
		{
			Assert.That(typeof(MissionEvent).GetProperty(nameof(MissionEvent.TargetCell)).PropertyType, Is.EqualTo(typeof(CellXY?)));
			Assert.That(typeof(CellXY).GetFields(), Is.Empty);
			var cell = new CellXY(int.MinValue, int.MaxValue);
			Assert.That(cell.X, Is.EqualTo(int.MinValue));
			Assert.That(cell.Y, Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void PassiveListsOwnTheirContents()
		{
			var failures = new List<BotPassiveCandidateFailure> { new() { TargetActor = 7, Reason = "blocked" } };
			var groups = new List<BotPassiveWaveGroup> { new() { Locomotor = "wheel", RepActor = 8 } };
			var job = new BotPassiveJobEndSnapshot { CandidateFailures = failures };
			var wave = new BotPassiveWaveStartSnapshot { Groups = groups };
			failures.Clear();
			groups[0] = new BotPassiveWaveGroup { Locomotor = "changed" };
			Assert.That(job.CandidateFailures, Has.Count.EqualTo(1));
			Assert.That(job.CandidateFailures[0].Reason, Is.EqualTo("blocked"));
			Assert.That(wave.Groups[0].Locomotor, Is.EqualTo("wheel"));
			Assert.That(new BotPassiveJobEndSnapshot { CandidateFailures = null }.CandidateFailures, Is.Null);
			Assert.That(new BotPassiveWaveStartSnapshot { Groups = null }.Groups, Is.Null);
		}
	}
}
