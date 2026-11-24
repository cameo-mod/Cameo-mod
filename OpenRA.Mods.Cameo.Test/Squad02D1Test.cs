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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 D1 stop-gap (Opus 2026-11-24, CONSOLIDATE_02_SQUAD_DESIGN.md X1 / X11): the push bar counts the committed blob that has ARRIVED at WAVE-MIN, without the
	/// "half of the team idle army must have arrived" term, so units still at home never raise the bar and the all-in is not held by it. Switch tl_bar_arrived_only
	/// (Info TeamLeadBarArrivedOnlyEnabled, default off, ON in the six personalities, never @classic).
	/// </summary>
	[TestFixture]
	public class Squad02D1Test
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

		// X1: a 4-bot team at tick 26000. Each pool holds about 25 units, the order holds 30 committed, 40 have arrived. The old bar is half of the team idle army.
		[Test]
		public void TheScenarioOfX1PassesTheArrivedBarAndStaysHeldUnderTheOldBar()
		{
			const int waveMinUnits = 12;
			const long waveMinValue = 6000;
			const int joinMin = 3;
			var teamIdle = 100 + 30;
			var oldNeed = BlobLaunch01CA.MinUnits(waveMinUnits, teamIdle);

			Assert.That(oldNeed, Is.EqualTo(65), "the old bar chases the idle army");
			Assert.That(TeamLeadCA.PushBarPassed(40, 20000, joinMin, oldNeed, waveMinValue), Is.False, "40 arrived is held by the old bar (X1)");
			Assert.That(TeamLeadCA.PushBarPassed(40, 20000, joinMin, waveMinUnits, waveMinValue), Is.True, "40 arrived passes WAVE-MIN (D1)");
		}

		[Test]
		public void TheArrivedBarStillHoldsAnyBlobBelowWaveMinUnitsOrValueAndTheJoinFloor()
		{
			const int waveMinUnits = 12;
			const long waveMinValue = 6000;

			Assert.That(TeamLeadCA.PushBarPassed(11, 20000, 3, waveMinUnits, waveMinValue), Is.False, "11 arrived units are below WAVE-MIN");
			Assert.That(TeamLeadCA.PushBarPassed(12, 20000, 3, waveMinUnits, waveMinValue), Is.True, "12 arrived units at the bar pass");
			Assert.That(TeamLeadCA.PushBarPassed(30, 5999, 3, waveMinUnits, waveMinValue), Is.False, "enough units but below the WAVE-MIN value");
			Assert.That(TeamLeadCA.PushBarPassed(2, 20000, 3, 0, 0), Is.False, "WAVE-MIN off: the join floor still holds a trickle");
			Assert.That(TeamLeadCA.PushBarPassed(3, 0, 3, 0, 0), Is.True, "WAVE-MIN off: the join floor alone");
		}

		[Test]
		public void TheBarWiringUsesTheArrivedBlobWhenOnAndKeepsTheOldTermWhenOff()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("bool TeamLeadPushHeld(", StringComparison.Ordinal);
			var end = wiring.IndexOf("void DiagnosticTeamEvent(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			Assert.That(end, Is.GreaterThan(start));
			var held = wiring[start..end];

			Assert.That(held, Does.Contain("if (!TeamLeadPushBarOn)\n\t\t\t\treturn false;"), "the bar itself is still the existing switch");
			Assert.That(held, Does.Contain("var needUnits = TeamLeadBarArrivedOnlyOn ? minUnits : BlobLaunch01CA.MinUnits(minUnits, teamIdle);"));
			Assert.That(held, Does.Contain("order.ArrivedTotals()"), "the units that arrived, not the whole blob");
			Assert.That(held, Does.Contain("TeamLeadCA.PushBarPassed(units, value, Info.TeamLeadJoinMinUnits, needUnits, minValue)"));
			Assert.That(held, Does.Contain("DiagnosticTeamEvent(\"team_push_hold\""), "the hold record stays");

			Assert.That(wiring, Does.Contain("bool TeamLeadBarArrivedOnlyOn => TeamLeadOn && Info.TeamLeadBarArrivedOnlyEnabled && !ablateBarArrivedOnly.Off(Player, AblationIds.TeamLeadBarArrivedOnly);"));
			Assert.That(wiring, Does.Contain("AblationLatch ablateBarArrivedOnly;"));
		}

		// X11: the all-in fires through the same due-push test, so the arrived bar is the only bar it meets (single blob and pincer).
		[Test]
		public void EveryDuePushGoesThroughTheOneBar()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(Regex.Matches(wiring, Regex.Escape("&& !TeamLeadPushHeld(order, now)")).Count, Is.EqualTo(2), "pincer push and single-blob push");
			Assert.That(Regex.Matches(wiring, Regex.Escape("TeamLeadPushHeld(")).Count, Is.EqualTo(3), "two callers and the definition: no third path");
		}

		[Test]
		public void TheSwitchIsOffByDefaultOnInTheSixPersonalitiesNeverInClassicAndTheRowIsRegistered()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadBarArrivedOnlyEnabled = false;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], "\n\t\tTeamLeadBarArrivedOnlyEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadBarArrivedOnlyEnabled"), "never in @classic");

			Assert.That(AblationIds.TeamLeadBarArrivedOnly, Is.EqualTo("tl_bar_arrived_only"));
		}
	}
}
