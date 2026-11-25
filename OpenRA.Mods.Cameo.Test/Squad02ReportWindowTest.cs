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
		public void TheJoinWindowIsOpenForTheWholeGatherWindowAndClosesAtItsEnd()
		{
			Assert.That(ReportWindow.Open(7482, 7482, 1500), Is.True, "the issue tick itself");
			Assert.That(ReportWindow.Open(7482 + 1499, 7482, 1500), Is.True, "the last tick of the window");
			Assert.That(ReportWindow.Open(7482 + 1500, 7482, 1500), Is.False, "the window is over: no_reports may cancel (and the window push is due)");
			Assert.That(ReportWindow.Open(7482 + 9000, 7482, 1500), Is.False);

			// The two V08D cancels were both inside the window: with the row on neither happens.
			Assert.That(ReportWindow.Open(8009, 7482, 1500), Is.True, "order 1, age 527");
			Assert.That(ReportWindow.Open(17377, 16182, 1500), Is.True, "order 2, age 1195");

			// A zero or negative window never keeps an order open past its own issue tick (a yaml 0 cannot make an order immortal).
			Assert.That(ReportWindow.Open(100, 100, 0), Is.True);
			Assert.That(ReportWindow.Open(101, 100, 0), Is.False);
			Assert.That(ReportWindow.Open(101, 100, -50), Is.False);
		}

		[Test]
		public void TheRowHasItsIdAndIsBehindTheOneLauncher()
		{
			Assert.That(AblationIds.SqReportWindow, Is.EqualTo("sq_report_window"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.Squad02ReportWindow.cs"), Does.Contain("public const string SqReportWindow = \"sq_report_window\";"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.ReportWindow.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqReportWindow;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyReportWindowOn => ArmyLaunchArbiterOn && !ablateSqReportWindow.Off(Player, AblationIds.SqReportWindow);"));
			Assert.That(wiring, Does.Contain("bool ArmyReportWindowOpen(TeamLeadOrder order, int now) => ArmyReportWindowOn && ReportWindow.Open(now, order.IssueTick, Info.TeamLeadGatherMaxTicks);"),
				"the window is the order's own gather window, counted from its issue");
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
