#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// LATEPUSH-01 P0 (LATEPUSH_01_DESIGN.md D6): the DEV-only assignment evidence rows. The record shape, a null member is not written, and the source pins that keep the rows out
	/// of a release build and keep every hook read-only (the DEV game must play exactly like the same game without the rows).
	/// </summary>
	[TestFixture]
	public class LatePushP0Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";
		static readonly string[] Envelope = { "schema", "record", "game_uid", "seat", "bot_type", "tick", "seq" };

		string directory;
		string path;

		[SetUp]
		public void SetUp()
		{
			directory = Path.Combine(Path.GetTempPath(), "latepush-p0-test-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);
			path = Path.Combine(directory, "seat0.jsonl");
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(directory))
				Directory.Delete(directory, true);
		}

		BotPassiveDiagnosticWriter Create()
		{
			BotPassiveDiagnosticWriter.TryCreate(true, path, "game-1", "seat0", "normal", out var writer);
			Assert.That(writer.Enabled, Is.True);
			return writer;
		}

		List<JsonElement> Records() => File.ReadAllLines(path).Where(l => l.Length > 0).Select(l => JsonDocument.Parse(l).RootElement.Clone()).ToList();

		static void AssertFields(JsonElement record, params string[] fields)
		{
			Assert.That(record.GetProperty("record").GetString(), Is.EqualTo("attack_assign"));
			Assert.That(record.EnumerateObject().Select(p => p.Name).ToArray(), Is.EqualTo(Envelope.Concat(fields).ToArray()));
			Assert.That(record.GetProperty("schema").GetString(), Is.EqualTo("botpassive-diagnostic/1"));
		}

		static string RepoRoot()
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return dir.FullName;
		}

		static string RepoFile(string relative) => File.ReadAllText(Path.Combine(RepoRoot(), relative)).Replace("\r\n", "\n");

		[Test]
		public void OrderRowCarriesBlobBarsAndTheObservedEnemyView()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueAttackAssign(new BotPassiveAttackAssignSnapshot
			{
				Tick = 49373, Phase = "order", OrderSeq = 1, Why = "ahead_battle", AllIn = true, BlobUnits = 14, BlobValue = 7206, BarUnits = 12, BarValue = 6000,
				OwnArmy = 113516, EnemyArmy = 50092, EnemyDefence = 4000, EnemyBuildings = 31, ScanTick = 49250, ArmyKnown = true
			}), Is.True);
			writer.Complete(50000);

			var r = Records()[0];
			AssertFields(r, "phase", "order_seq", "why", "all_in", "blob_units", "blob_value", "bar_units", "bar_value", "own_army", "enemy_army", "enemy_defence",
				"enemy_buildings", "scan_tick", "army_known");
			Assert.That(r.GetProperty("tick").GetInt32(), Is.EqualTo(49373));
			Assert.That(r.GetProperty("phase").GetString(), Is.EqualTo("order"));
			Assert.That(r.GetProperty("why").GetString(), Is.EqualTo("ahead_battle"));
			Assert.That(r.GetProperty("blob_value").GetInt64(), Is.EqualTo(7206));
			Assert.That(r.GetProperty("army_known").GetBoolean(), Is.True);
		}

		[Test]
		public void JoinRowCarriesTheGuardAndItsLendClauses()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueAttackAssign(new BotPassiveAttackAssignSnapshot
			{
				Tick = 20000, Phase = "join", OrderSeq = 3, Why = null, AllIn = false, FreeUnits = 2, FreeValue = 900, GuardUnits = 48, GuardValue = 50140, GuardJoinable = 5,
				GuardFar = 31, GuardState = "UnitsForProtectionAttackState", ClauseTargetValid = false, ClauseRally = false, ClauseRequest = false, ClausePerceived = true,
				ClausePredicted = 2, GuardQuiet = false, QuietTicks = -1, LendOn = true, Lent = 0, SentUnits = 3, SentValue = 1200
			}), Is.True);
			writer.Complete(50000);

			var r = Records()[0];
			AssertFields(r, "phase", "order_seq", "all_in", "free_units", "free_value", "guard_units", "guard_value", "guard_joinable", "guard_far", "guard_state",
				"clause_target_valid", "clause_rally", "clause_request", "clause_perceived", "clause_predicted", "guard_quiet", "quiet_ticks", "lend_on", "lent", "sent_units",
				"sent_value");
			Assert.That(r.GetProperty("guard_far").GetInt32(), Is.EqualTo(31));
			Assert.That(r.GetProperty("clause_perceived").GetBoolean(), Is.True);
			Assert.That(r.GetProperty("clause_predicted").GetInt32(), Is.EqualTo(2));
			Assert.That(r.GetProperty("guard_quiet").GetBoolean(), Is.False);
			Assert.That(r.GetProperty("quiet_ticks").GetInt32(), Is.EqualTo(-1));
		}

		[Test]
		public void OutcomeRowCarriesAliveAndIdleAtHome()
		{
			var writer = Create();
			Assert.That(writer.TryEnqueueAttackAssign(new BotPassiveAttackAssignSnapshot
			{
				Tick = 20600, Phase = "out600", OrderSeq = 3, AllIn = true, Why = "late", SentUnits = 3, AliveUnits = 2, AliveValue = 800, IdleHomeUnits = 7, GuardNowUnits = 40
			}), Is.True);
			writer.Complete(50000);

			var r = Records()[0];
			AssertFields(r, "phase", "order_seq", "why", "all_in", "sent_units", "alive_units", "alive_value", "idle_home_units", "guard_now_units");
			Assert.That(r.GetProperty("alive_units").GetInt32(), Is.EqualTo(2));
			Assert.That(r.GetProperty("idle_home_units").GetInt32(), Is.EqualTo(7));
		}

		[Test]
		public void SinkInterfaceAndNullSinkCarryTheRow()
		{
			var sink = RepoFile(Bots + "CameoLogSink.cs");
			Assert.That(sink, Does.Contain("void PassiveAttackAssign(int playerIndex, BotPassiveAttackAssignSnapshot snapshot);"));
			Assert.That(sink, Does.Contain("public void PassiveAttackAssign(int playerIndex, BotPassiveAttackAssignSnapshot snapshot) { }"), "the release default does nothing");
			Assert.That(sink, Does.Contain("public void TryEnqueueAttackAssign(BotPassiveAttackAssignSnapshot s) => sink.PassiveAttackAssign(playerIndex, s);"));
			Assert.That(RepoFile("OpenRA.Mods.Cameo.Dev/CameoDevLogSinkInstaller.cs"),
				Does.Contain("Writer(playerIndex)?.TryEnqueueAttackAssign(s);"));
		}

		[Test]
		public void TheRecordNameExistsOnlyInTheDevWriter()
		{
			Assert.That(RepoFile("OpenRA.Mods.Cameo.Dev/BotPassiveDiagnosticWriterCA.cs"), Does.Contain("SerializeCommon(\"attack_assign\""));
			foreach (var file in Directory.EnumerateFiles(Path.Combine(RepoRoot(), "OpenRA.Mods.CA"), "*.cs", SearchOption.AllDirectories))
				Assert.That(File.ReadAllText(file), Does.Not.Contain("\"attack_assign\""), file);
		}

		[Test]
		public void TheWholeWiringFileIsDevOnly()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.LatePush.cs");
			var first = text.IndexOf("#if CAMEO_DEV_LOGGING", StringComparison.Ordinal);
			Assert.That(first, Is.GreaterThanOrEqualTo(0));
			Assert.That(text[..first], Does.Not.Contain("namespace"), "nothing but the header comments before the guard");
			Assert.That(text.TrimEnd().EndsWith("#endif", StringComparison.Ordinal), Is.True);
			Assert.That(text.Split("#if ").Length - 1, Is.EqualTo(1), "one guard around the whole file");
		}

		[Test]
		public void EveryCallSiteSitsInsideADevBlock()
		{
			var sites = new (string File, string Call)[]
			{
				("SquadManagerBotModuleCA.TeamLead.cs", "LatePushOrderRow("),
				("SquadManagerBotModuleCA.TeamLead.cs", "LatePushJoinBegin("),
				("SquadManagerBotModuleCA.TeamLead.cs", "LatePushJoinEnd("),
				("SquadManagerBotModuleCA.Pipeline02.cs", "LatePushJoinBegin("),
				("SquadManagerBotModuleCA.Pipeline02.cs", "LatePushJoinEnd("),
				("SquadManagerBotModuleCA.Pipeline02.cs", "LatePushPlanFields()"),
				("SquadManagerBotModuleCA.Stalemate.cs", "LatePushOutcomeTick()")
			};

			foreach (var (file, call) in sites)
			{
				var text = RepoFile(Bots + file);
				var at = text.IndexOf(call, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThanOrEqualTo(0), file + " " + call);
				while (at >= 0)
				{
					var before = text[..at];
					Assert.That(before.LastIndexOf("#if CAMEO_DEV_LOGGING", StringComparison.Ordinal), Is.GreaterThan(before.LastIndexOf("#endif", StringComparison.Ordinal)),
						file + " " + call + ": the call is not inside a CAMEO_DEV_LOGGING block");
					at = text.IndexOf(call, at + call.Length, StringComparison.Ordinal);
				}
			}
		}

		[Test]
		public void TheHooksOnlyReadState()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.LatePush.cs");
			foreach (var write in new[]
			{
				"QueueOrder(", ".Units.Add(", ".Units.Remove(", ".Units.Clear(", "unitsHangingAroundTheBase.Add", "unitsHangingAroundTheBase.Remove", "unitsHangingAroundTheBase.Clear",
				"RegisterNewSquad", "DismissSquad", "ReleaseDefenders", "BotRng", "Random", "ArmyGuardPick(", "ArmyGuardLendTo(", "AheadScan(", "AheadWhy("
			})
				Assert.That(text, Does.Not.Contain(write), "a DEV hook must not write state, draw a random number or run a scan: " + write);

			// Only the pending list of the rows themselves is written.
			Assert.That(text, Does.Contain("latePushPending.Add("));
		}

		[Test]
		public void TheJoinPassReadsOneCheapWalkAndTheClausesOnlyWhenUnitsWent()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.LatePush.cs");
			var begin = text[text.IndexOf("LatePushJoinContext LatePushJoinBegin(TeamLeadOrder order)", StringComparison.Ordinal)..text.IndexOf("void LatePushJoinEnd(", StringComparison.Ordinal)];
			Assert.That(begin, Does.Contain("unitsHangingAroundTheBase.Count == 0 && (!guardValid || guard.Units.Count == 0)"), "nothing pooled and no guard unit: no context");
			Assert.That(begin, Does.Not.Contain("LatePushGuardNow()"), "the begin must not read the clauses (request lookup, threat providers) on every join pass");
			Assert.That(begin, Does.Not.Contain("SelectProtectionRequest"));
			Assert.That(begin, Does.Contain("LatePushGuardUnits(ctx.Guard);"));
			Assert.That(begin, Does.Contain("Order = order"), "the end uses the order the begin captured, not a re-read after the join");
			Assert.That(begin, Does.Not.Contain("Persistent"), "the begin serves the reinforce pass of a persistent order too; only the TeamLeadJoin call site skips it");
			Assert.That(begin, Does.Contain("unitCannotBeOrdered(u.Actor)"), "a unit TeamLeadJoin drops from the pool must not count as sent");

			// TargetValid depends on the guard's first unit, so it is read with the units before any lend; the other clauses are written by nothing in the join.
			var units = text[text.IndexOf("void LatePushGuardUnits(", StringComparison.Ordinal)..text.IndexOf("void LatePushGuardClauses(", StringComparison.Ordinal)];
			Assert.That(units, Does.Contain("view.TargetValid = guard.IsTargetValid;"));
			var clauses = text[text.IndexOf("void LatePushGuardClauses(", StringComparison.Ordinal)..text.IndexOf("string LatePushPlanFields()", StringComparison.Ordinal)];
			Assert.That(clauses, Does.Not.Contain("IsTargetValid"));
			var lend = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			var lendTo = lend[lend.IndexOf("void ArmyGuardLendTo(", StringComparison.Ordinal)..];
			foreach (var state in new[] { "protectionRally", "protectionHoldUntilTick", "protectionQuietSinceTick", "protectionRequestProviders" })
				Assert.That(lendTo, Does.Not.Contain(state), "the lend must not write what the clauses read after it: " + state);

			var end = text[text.IndexOf("void LatePushJoinEnd(", StringComparison.Ordinal)..text.IndexOf("void LatePushOutcomeTick()", StringComparison.Ordinal)];
			var at = end.IndexOf("LatePushGuardClauses(g);", StringComparison.Ordinal);
			Assert.That(at, Is.GreaterThan(end.IndexOf("if (sent.Count == 0)", StringComparison.Ordinal)), "the clauses are read only after the no-units-went return");

			// The hooks sit around the call; TeamLeadJoin itself keeps the release text (its source pins in the squad tests stay valid). A pushed persistent order is reinforced
			// inside TeamLeadJoin with its own rows (ArmyReinforce), so only the call site skips it.
			var lead = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var joinBody = lead[lead.IndexOf("void TeamLeadJoin(IBot bot, int now)", StringComparison.Ordinal)..lead.IndexOf("// Where the order goes, from the leader's own view", StringComparison.Ordinal)];
			Assert.That(joinBody, Does.Not.Contain("LatePush"));
			Assert.That(joinBody, Does.Not.Contain("CAMEO_DEV_LOGGING"));
			Assert.That(lead, Does.Contain("latePushOrder is not { Persistent: true, Pushed: true } ? LatePushJoinBegin(latePushOrder) : null;"));
			Assert.That(lead, Does.Contain("LatePushJoinEnd(latePushJoin, latePushJoin?.Order, \"join\");"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs"), Does.Contain("var latePushJoin = LatePushJoinBegin(order);"), "the reinforce pass begins with its own order");
		}

		[Test]
		public void ArmyPlanRowAppendsTheGuardFields()
		{
			var plan = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(plan, Does.Contain("main_squad=\" + p.MainSquadKey + LatePushPlanFields());"));
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.LatePush.cs");
			foreach (var field in new[] { "\" guard=\"", "\" gjoin=\"", "\" gfar=\"", "\" gstate=\"", "\" gcl=\"", "\" gquiet=\"", "\" gqt=\"" })
				Assert.That(wiring, Does.Contain(field));
		}
	}
}
