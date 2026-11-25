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

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q1 fix-up (DEV telemetry): the launch_source helpers are [Conditional("CAMEO_DEV_LOGGING")] (compiled out of release),
	/// the L20 row skips gather-only orders, and the L11 stage exit is a stage_exit row, not a launch. Source pins, as in V07Test.
	/// </summary>
	[TestFixture]
	public class ConsolidateQ1LaunchSourceTest
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

		static int CountOf(string text, string needle)
		{
			var n = 0;
			for (var i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
				n++;
			return n;
		}

		[Test]
		public void LaunchSourceHelpersAreConditionalOnDevLogging()
		{
			var helper = RepoFile(Bots + "SquadManagerBotModuleCA.LaunchSource.cs");
			Assert.That(helper, Does.Contain("using System.Diagnostics;"), "the attribute needs the namespace");
			Assert.That(helper, Does.Contain("[Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tinternal void LaunchSourceRecord(SquadCA squad, string path, string role, int units)"), "the launch helper is compiled out of release");
			Assert.That(helper, Does.Contain("[Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tinternal void StageExitRecord(SquadCA squad, string path, string role, int units)"), "the stage-exit helper is compiled out of release");
			// This count pins exactly the two Q1 helpers. Update it if a third [Conditional] helper is added to this file.
			Assert.That(CountOf(helper, "[Conditional(\"CAMEO_DEV_LOGGING\")]"), Is.EqualTo(2), "exactly the two helpers carry the attribute");
			Assert.That(helper, Does.Contain("DiagnosticSquadId(squad)"), "launch rows join the squad rows by the squad id");
		}

		[Test]
		public void L20SkipsGatherOnlyAndL11IsAStageExit()
		{
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(team, Does.Contain("if (why != TeamLeadCA.PublishWhy.Gather)\n\t\t\t\tLaunchSourceRecord(null, \"L20\", \"Team\", blob.Units);"), "L20 is recorded after the gather branch, for attack orders only");
			var ground = RepoFile(Bots + "Squads/States/GroundStatesCA.cs");
			Assert.That(ground, Does.Not.Contain("LaunchSourceRecord(owner, \"L11\""), "L11 is a stage exit, not a launch");
			Assert.That(CountOf(ground, "StageExitRecord(owner, \"L11\""), Is.EqualTo(3), "the three stage-exit sites");
		}
	}
}
