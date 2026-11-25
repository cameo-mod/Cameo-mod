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
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// PUSHBAR-RCA-01 S1 (row sq_report_window). V08D 6v6: order 1 (issued 7482) was cancelled no_reports at 8009 (age 527) and order 2 (issued 16182) at 17377 (age 1195), while the
	/// gather window of an order is 1500 ticks (TeamLeadGatherMaxTicks): the 500-tick no-report grace (5 x TeamLeadCheckTicks) ended both before the window in which a push can be due.
	/// With the row on, no_reports waits for the join window.
	/// </summary>
	[TestFixture]
	public class Squad02ReportWindowTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		[Test]
		public void TheJoinWindowIsOpenThroughTheWindowAndTheTeamPassWhereThePushIsFirstDue()
		{
			// Window 1500 (TeamLeadGatherMaxTicks) + one team pass 100 (TeamLeadCheckTicks).
			Assert.That(ReportWindow.Open(7482, 7482, 1500, 100), Is.True, "the issue tick itself");
			Assert.That(ReportWindow.Open(7482 + 1499, 7482, 1500, 100), Is.True, "the last tick before the window push is due");
			Assert.That(ReportWindow.Open(7482 + 1500, 7482, 1500, 100), Is.True, "the window push is due: the cancel must not pre-empt it (CM review of 4da39449)");
			Assert.That(ReportWindow.Open(7482 + 1599, 7482, 1500, 100), Is.True, "the upkeep runs once per team pass: the first pass with the push due can be up to one pass late");
			Assert.That(ReportWindow.Open(7482 + 1600, 7482, 1500, 100), Is.False, "past the window and its pass: no_reports may cancel");
			Assert.That(ReportWindow.Open(7482 + 9000, 7482, 1500, 100), Is.False);

			// The two V08D cancels were both inside the window: with the row on neither happens.
			Assert.That(ReportWindow.Open(8009, 7482, 1500, 100), Is.True, "order 1, age 527");
			Assert.That(ReportWindow.Open(17377, 16182, 1500, 100), Is.True, "order 2, age 1195");

			// Whenever the push is first due (age 1500 to 1599 at a team pass), the window is still open there: the push test runs, never a no_reports cancel first.
			for (var age = 1500; age <= 1599; age++)
			{
				Assert.That(TeamLeadCA.PushDue(10000 + age, 10000, 300, 1500, 0, 0, 70), Is.True, "age " + age + ": the window push is due even with nothing committed");
				Assert.That(ReportWindow.Open(10000 + age, 10000, 1500, 100), Is.True, "age " + age);
			}

			Assert.That(TeamLeadCA.PushDue(10000 + 1499, 10000, 300, 1500, 0, 0, 70), Is.False, "nothing committed and the window not over: not due");

			// A zero or negative window (or pass) never keeps an order open for long (a yaml 0 cannot make an order immortal): the window is at least the one pass.
			Assert.That(ReportWindow.Open(199, 100, 0, 100), Is.True);
			Assert.That(ReportWindow.Open(200, 100, 0, 100), Is.False);
			Assert.That(ReportWindow.Open(200, 100, -50, 100), Is.False);
			Assert.That(ReportWindow.Open(100, 100, 1500, 0), Is.True);
			Assert.That(ReportWindow.Open(1601, 100, 1500, 0), Is.False, "a zero pass counts as one tick");
		}

		[Test]
		public void TheRowHasItsIdAndIsBehindTheOneLauncher()
		{
			Assert.That(AblationIds.SqReportWindow, Is.EqualTo("sq_report_window"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.Squad02ReportWindow.cs"), Does.Contain("public const string SqReportWindow = \"sq_report_window\";"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.ReportWindow.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqReportWindow;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyReportWindowOn => ArmyLaunchArbiterOn && !ablateSqReportWindow.Off(Player, AblationIds.SqReportWindow);"));
			Assert.That(wiring, Does.Contain("bool ArmyReportWindowOpen(TeamLeadOrder order, int now) => ArmyReportWindowOn && ReportWindow.Open(now, order.IssueTick, Info.TeamLeadGatherMaxTicks, Info.TeamLeadCheckTicks);"),
				"the window is the order's own gather window plus one team pass, counted from its issue");
			foreach (var banned in new[] { "Random", "FindActors", "World.Actors", "FindClosestEnemy" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void OnlyTheNoReportsClauseChangesAndTheOlderGraceIsUntouched()
		{
			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");

			// The clause keeps its older text (survive row off = the old grace) and only gains the window test after the grace test.
			Assert.That(tl, Does.Contain(": order.ReportCount == 0 && now - (TeamLeadOrderSurviveOn ? HarassCapCA.NoReportSince(order.IssueTick, order.LastReportTick) : order.IssueTick) > reportGrace && !ArmyReportWindowOpen(order, now) ? \"no_reports\""));
			Assert.That(tl, Does.Contain("var reportGrace = (Info.TeamLeadCadenceEnabled ? 5 : 2) * Info.TeamLeadCheckTicks;"), "the grace itself is unchanged");

			// The grace-hold and rally-grace clauses still come first, the clause still ends the chain with null.
			var upkeep = tl[tl.IndexOf("void TeamLeadMaintainOrder(TeamLeadTeam team, int now)", StringComparison.Ordinal)..tl.IndexOf("bool TeamLeadPushHeld(TeamLeadOrder order, int now)", StringComparison.Ordinal)];
			var graceHold = upkeep.IndexOf("TeamLeadCA.GraceHolds(", StringComparison.Ordinal);
			var rally = upkeep.IndexOf("TeamLeadCA.RallyGraceHolds(", StringComparison.Ordinal);
			var noReports = upkeep.IndexOf("\"no_reports\"", StringComparison.Ordinal);
			var windowTest = upkeep.IndexOf("!ArmyReportWindowOpen(order, now)", StringComparison.Ordinal);
			Assert.That(graceHold, Is.GreaterThan(0));
			Assert.That(rally, Is.GreaterThan(graceHold));
			Assert.That(windowTest, Is.GreaterThan(rally));
			Assert.That(noReports, Is.GreaterThan(windowTest));
			Assert.That(upkeep, Does.Contain("? \"no_reports\"\n\t\t\t\t: null;"));
			Assert.That(tl, Does.Not.Contain("TeamLeadGatherMaxTicks) ? \"no_reports\""), "no second window literal in the clause: the wiring owns it");

			// The cancel record keeps its fields (age, reports) so the V08D evidence reads the same in the next run.
			Assert.That(upkeep, Does.Contain("DiagnosticTeamEvent(\"team_cancel\", \"seq=\" + order.Seq + \" why=\" + cancel + \" age=\" + (now - order.IssueTick) + \" reports=\" + order.ReportCount"));
		}
	}
}
