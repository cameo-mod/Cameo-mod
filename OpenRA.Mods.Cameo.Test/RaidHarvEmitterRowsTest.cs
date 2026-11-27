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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// RAID-HARV-01 P3 (DEV build only: the emitter does not exist in an official build). tools/tests/raid_harv_emitter_rows.jsonl holds the exact lines the C# row builders write; this fixture rebuilds every line from the same inputs and compares the
	/// bytes, and tools/tests/test_extract_raid_harv_signals.py feeds the same file to the python extractor, so the writer and the reader cannot drift apart. The source pins check that the call sites sit inside the DEV guard and that the sample
	/// sites are the ones the design lists.
	/// </summary>
	[TestFixture]
	public sealed class RaidHarvEmitterRowsTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string[] Golden() => File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "raid_harv_emitter_rows.jsonl")).Where(l => l.Length > 0).ToArray();

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		static string Sig(int tick, string raid, string player, string style, RaidKey key, string payload) =>
			RaidHarvLogRows.Signal("g1", "seatA", tick, raid, player, style, key, payload);

		[Test]
		public void EveryGoldenLineIsWhatTheBuildersWrite()
		{
			var expected = new[]
			{
				RaidHarvLogRows.Seat("g1", "seatA", "rush", "gdi", new[] { ("e1", "nod"), ("e2", "gdi") }),
				Sig(1000, "seatA-1", "e1", "boom", RaidKey.LostRecon, RaidHarvLogRows.LostReacquired(120, 300)),
				Sig(1010, "seatA-1", "e1", "boom", RaidKey.ReconRadius, RaidHarvLogRows.RadiusReacquired(8, 6)),
				Sig(1020, "seatA-1", "e1", "boom", RaidKey.MoveRefresh, RaidHarvLogRows.MoveRefresh(3, true, true, "Travel", 80)),
				Sig(1030, "seatA-1", "e1", "boom", RaidKey.ReqMargin, RaidHarvLogRows.ReqMargin(true, 250)),
				Sig(1040, "seatA-1", "e1", "boom", RaidKey.IncidentWindow, RaidHarvLogRows.IncidentWindow(10, 40)),
				Sig(1050, "seatA-1", "e1", "boom", RaidKey.WaypointSpacing, RaidHarvLogRows.WaypointSpacing(false)),
				Sig(2000, "seatA-2", "e2", null, RaidKey.LostRecon, RaidHarvLogRows.LostExhausted(300)),
				Sig(2010, "seatA-2", "e2", null, RaidKey.ReconRadius, RaidHarvLogRows.RadiusExhausted(true, 6)),
				Sig(2020, "seatA-2", "e2", null, RaidKey.IncidentLoss, RaidHarvLogRows.IncidentLoss(new RetreatOutcome(true, false))),
				Sig(2030, "seatA-2", "e2", null, RaidKey.IncidentDecay, RaidHarvLogRows.IncidentDecay(new DecayOutcome(false, true))),
				Sig(2040, "seatA-2", "e2", null, RaidKey.DefenceRadius, RaidHarvLogRows.DefenceRadius(true)),
			};

			Assert.That(expected, Is.EqualTo(Golden()));
		}

		[Test]
		public void AllNineKeysAreCoveredAndNoRowHasANewline()
		{
			var lines = Golden();
			foreach (var key in RaidHarvKeys.All)
				Assert.That(lines.Count(l => l.Contains("\"key\":\"" + key.Id + "\"")), Is.GreaterThan(0), key.Id);

			foreach (var line in lines)
				Assert.That(line, Does.Not.Contain("\n").And.Not.Contain("\r"));
		}

		[Test]
		public void TheValuesAreClampedAndEscaped()
		{
			Assert.That(RaidHarvLogRows.ReqMargin(true, 5000), Is.EqualTo("\"killed\":true,\"loss_permille\":1000"), "a payload is the part after the key, without braces");
			Assert.That(RaidHarvLogRows.ReqMargin(true, -4), Is.EqualTo("\"killed\":true,\"loss_permille\":0"));
			Assert.That(RaidHarvLogRows.LostReacquired(-5, 0), Is.EqualTo("\"kind\":\"reacquired\",\"ticks\":0,\"current\":1"), "a negative span reads 0, a current below 1 reads 1");
			Assert.That(RaidHarvLogRows.Seat("g1", "s", "a\"b\\c\n", null, Array.Empty<(string, string)>()),
				Is.EqualTo("{\"event\":\"raid_harv_seat\",\"game_id\":\"g1\",\"seat\":\"s\",\"personality\":\"a\\\"b\\\\c\\u000a\",\"own_faction\":null,\"enemies\":[]}"));
			Assert.That(RaidHarvLogRows.ValidId("g1_a.b-C"), Is.True);
			Assert.That(RaidHarvLogRows.ValidId("a b"), Is.False);
			Assert.That(RaidHarvLogRows.ValidId(""), Is.False);
			Assert.That(RaidHarvLogRows.ValidId(null), Is.False);
			Assert.That(RaidHarvLogRows.ValidId(new string('a', 129)), Is.False);
		}

		[Test]
		public void TheIncidentLossWatchResolvesHalfLostAtOnceAndOtherwiseAtTheHorizon()
		{
			var w = new RaidIncidentLossWatch(100, 1000);
			Assert.That(w.Step(110, 400), Is.Null, "less than half lost, horizon not reached");
			Assert.That(w.Step(110, 500), Is.EqualTo(new RetreatOutcome(true, false)), "half or more lost: too loose, at once");

			var calm = new RaidIncidentLossWatch(100, 1000) { RestrikeStarted = true };
			Assert.That(calm.Step(699, 0), Is.Null);
			Assert.That(calm.Step(700, 0), Is.EqualTo(new RetreatOutcome(false, true)), "a clean re-strike inside the horizon: too tight");

			var hit = new RaidIncidentLossWatch(100, 1000) { RestrikeStarted = true, FurtherIncident = true };
			Assert.That(hit.Step(700, 0), Is.EqualTo(new RetreatOutcome(false, false)), "a re-strike that met a further incident is no sample");
		}

		[Test]
		public void TheDecayWatchSeparatesAReincidentCellFromACleanPassAndNeverCountsTheOriginRaid()
		{
			var cell = new CPos(10, 10);
			var reincident = new RaidIncidentDecayWatch(cell, 100, 400, "r1");
			reincident.Incident(520, new CPos(11, 10), "r2");
			Assert.That(reincident.Step(521), Is.EqualTo(new DecayOutcome(true, false)), "a new incident near the cell after the decay: dropped too early");

			var clean = new RaidIncidentDecayWatch(cell, 100, 400, "r1");
			clean.Passing(300, new CPos(10, 12), "r2");
			Assert.That(clean.Step(500 + RaidIncidentDecayWatch.AfterDecayTicks), Is.Null);
			Assert.That(clean.Step(501 + RaidIncidentDecayWatch.AfterDecayTicks), Is.EqualTo(new DecayOutcome(false, true)), "a later raid passed before the decay with no incident: remembered too long");

			var own = new RaidIncidentDecayWatch(cell, 100, 400, "r1");
			own.Passing(300, cell, "r1");
			own.Incident(520, cell, "r1");
			Assert.That(own.Step(501 + RaidIncidentDecayWatch.AfterDecayTicks), Is.EqualTo(new DecayOutcome(false, false)), "the raid that made the incident is never a later raid");
		}

		[Test]
		public void TheLegTrackerReportsALeftLaneOnlyForACompletedLegAndDropsInterruptedOnes()
		{
			var lane = Enumerable.Range(0, 11).Select(i => new CPos(i, 0)).ToList();
			var waypoints = new System.Collections.Generic.List<CPos> { lane[3], lane[7], lane[10] };
			var t = new RaidLegTracker();
			t.SetPlan(waypoints, lane);
			Assert.That(t.Sample(waypoints, 0, new CPos(1, 0)), Is.Null);
			Assert.That(t.Sample(waypoints, 0, new CPos(2, 5)), Is.Null, "off the lane inside leg 0");
			Assert.That(t.Sample(waypoints, 1, new CPos(4, 0)), Is.True, "leg 0 finished and the lead was more than two cells off it");
			Assert.That(t.Sample(waypoints, 1, new CPos(6, 0)), Is.Null);
			Assert.That(t.Sample(waypoints, 2, new CPos(8, 1)), Is.False, "leg 1 finished on the lane");

			t.Drop();
			Assert.That(t.Sample(waypoints, 2, new CPos(9, 0)), Is.Null, "a dropped leg is never reported");
			Assert.That(t.Sample(new System.Collections.Generic.List<CPos>(waypoints), 2, new CPos(9, 0)), Is.Null, "another waypoint list is another plan");
		}

		// ---- source pins ----

		[Test]
		public void EveryCallSiteSitsInsideTheDevGuardAndTheEmitterFilesAreDevOnly()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			foreach (var call in new[] { "RaidLogStart(", "RaidLogRefresh(", "RaidLogCheck(", "RaidLogWindowReset(", "RaidLogPhase(", "RaidLogIncident(", "RaidLogRelease(" })
			{
				Assert.That(Count(run, call), Is.EqualTo(1), call);
				var at = run.IndexOf(call, StringComparison.Ordinal);
				var before = run[..at];
				Assert.That(before.LastIndexOf("#if CAMEO_DEV_LOGGING", StringComparison.Ordinal), Is.GreaterThan(before.LastIndexOf("#endif", StringComparison.Ordinal)), call + " is outside the DEV guard");
			}

			var core = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			var pass = core.IndexOf("RaidLogPass();", StringComparison.Ordinal);
			Assert.That(pass, Is.GreaterThan(0));
			Assert.That(core[..pass].LastIndexOf("#if CAMEO_DEV_LOGGING", StringComparison.Ordinal), Is.GreaterThan(core[..pass].LastIndexOf("#endif", StringComparison.Ordinal)));

			foreach (var file in new[] { Bots + "Squads/RaidHarvLog.cs", Bots + "SquadManagerBotModuleCA.RaidHarvLog.cs" })
			{
				var text = RepoFile(file);
				var firstCode = text.IndexOf("using ", StringComparison.Ordinal);
				Assert.That(text[..firstCode], Does.Contain("#if CAMEO_DEV_LOGGING"), file);
				Assert.That(text.TrimEnd(), Does.EndWith("#endif"), file);
			}

			var plan = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");
			Assert.That(plan, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t/// <summary>DEV only (raid.waypoint_spacing)"));
			Assert.That(plan, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tplan.Lane = travel;\n#endif"));
		}

		[Test]
		public void TheEmitterOnlyObservesAndReadsNothingOfAnUnseenActor()
		{
			var log = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvLog.cs");

			// the only actor reads: the armed enemies the player can view (visibility first), the resolver, the squad's own movers
			Assert.That(log, Does.Contain("if (!a.CanBeViewedByPlayer(Player) || !a.IsInWorld || a.IsDead || a.Owner == null)"));
			Assert.That(log, Does.Not.Contain("GetActorById"), "the resolver is the only GetActorById of the raid files");
			Assert.That(log, Does.Contain("RaidTargetResolve(w.TargetId)"));

			// no order, no squad target, no raid field the raid reads
			foreach (var banned in new[] { "QueueOrder", "ChangeState", "squad.Target", ".TargetActor =", "run.Phase = ", "run.LastOrderTick = ", "run.WaypointIndex = ", "run.Waypoints = " })
				Assert.That(log, Does.Not.Contain(banned), "observation only: " + banned);

			// the target owner and style are snapshotted once, at the pick
			Assert.That(Count(log, "TargetStyle = RaidLogStyle(owner)"), Is.EqualTo(1));
			Assert.That(Count(log, "TargetPlayer = owner.InternalName"), Is.EqualTo(1));

			// the sink is only the DEV sink: an enabled sink, a live regular game, a valid game id; decided once
			Assert.That(log, Does.Contain("sink != null && sink.Enabled && !World.IsReplay && World.Type == WorldType.Regular && RaidHarvLogRows.ValidId(uid)"));
		}

		[Test]
		public void TheSinkMethodIsAResidentValueOnlyBoundaryAndTheWriterIsRegisteredInTheDevRulesOnly()
		{
			var sink = RepoFile(Bots + "CameoLogSink.cs");
			Assert.That(sink, Does.Contain("void RaidHarvRow(string row, int tick);"));
			Assert.That(sink, Does.Contain("public void RaidHarvRow(string row, int tick) { }"), "the release sink ignores it");

			var dev = RepoFile("OpenRA.Mods.Cameo.Dev/CameoDevLogSinkInstaller.cs");
			Assert.That(dev, Does.Contain("public void RaidHarvRow(string row, int tick) => worldActor.TraitOrDefault<RaidHarvLogWriterDev>()?.Write(row);"));

			Assert.That(RepoFile("mods/cameo/rules/dev/logging.yaml"), Does.Contain("\n\tRaidHarvLogWriterDev:\n"));
			Assert.That(RepoFile("mods/cameo/rules/dev/logging.yaml").Split('\n').Count(l => l.Contains("RaidHarvLogWriterDev")), Is.EqualTo(1));
			foreach (var yaml in new[] { "mods/cameo/mod.yaml" })
				Assert.That(RepoFile(yaml), Does.Not.Contain("RaidHarvLogWriterDev"), "never in an official rules file");
		}

		[Test]
		public void TheWriterSendsOneQueuedFileAndNeverBlocksTheSimThread()
		{
			var writer = RepoFile("OpenRA.Mods.Cameo.Dev/RaidHarvLogWriterDev.cs");
			Assert.That(writer, Does.Contain("writer = new AiQueuedJsonlWriter($\"cameo-ai-raidharv-{uid}.jsonl\", \"raidharv\");"));
			Assert.That(writer, Does.Contain("writer?.TryWrite(row);"));
			Assert.That(writer, Does.Contain("AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost)"));
			Assert.That(writer, Does.Not.Contain("File.Write").And.Not.Contain("StreamWriter"), "no I/O on the tick path");
		}
	}
}
#endif
