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
	/// CONSOLIDATE-02 D1 stop-gap (Opus 2026-11-24, CONSOLIDATE_02_SQUAD_DESIGN.md X1 / X11, tweak 2026-11-25): the push bar no longer asks for half of the team idle army (the home
	/// pool kept raising it), but it is not WAVE-MIN alone either: ARRIVED >= max(WAVE-MIN, 75% of the units COMMITTED to the order), so a small group never pushes ahead of a big
	/// blob that is still walking. At the existing push-bar timeout (3000 ticks after the first hold) the held order pushes when the arrived blob passes plain WAVE-MIN instead of
	/// being dissolved. Switch tl_bar_arrived_only (Info TeamLeadBarArrivedOnlyEnabled, default off, ON in the six personalities, never @classic; needs WAVE-MIN active).
	/// </summary>
	[TestFixture]
	public class Squad02D1Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";
		const int WaveMinUnits = 12;
		const long WaveMinValue = 6000;
		const int JoinMin = 3;
		const int Percent = 75;
		const int MaxTicks = 3000;

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		[Test]
		public void TheArrivedBarIsTheLargerOfWaveMinAndTheShareOfTheCommittedUnitsRoundedUp()
		{
			Assert.That(TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 60, Percent), Is.EqualTo(45), "75% of 60 committed");
			Assert.That(TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 50, Percent), Is.EqualTo(38), "37.5 rounds up");
			Assert.That(TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 10, Percent), Is.EqualTo(WaveMinUnits), "a small order: WAVE-MIN is the bar");
			Assert.That(TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 0, Percent), Is.EqualTo(WaveMinUnits), "nothing committed yet: WAVE-MIN");
			Assert.That(TeamLeadCA.ArrivedBarUnits(0, 40, Percent), Is.EqualTo(30), "no WAVE-MIN tier: the committed share alone");
			Assert.That(TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 60, 0), Is.EqualTo(WaveMinUnits), "0 percent = WAVE-MIN only");
			Assert.That(TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 60, 250), Is.EqualTo(60), "the percent is clamped to 100");
			Assert.That(TeamLeadCA.ArrivedBarUnits(-5, -9, -3), Is.EqualTo(0), "negative inputs never raise a bar");
		}

		// Opus 2026-11-25: 12 arrived of 60 committed must not push before the timeout; 46 of 60 pushes.
		[Test]
		public void TwelveArrivedOfSixtyCommittedHoldsBeforeTheTimeoutAndFortySixOfSixtyPushes()
		{
			var need = TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 60, Percent);

			Assert.That(TeamLeadCA.PushBarPassed(12, 20000, JoinMin, need, WaveMinValue), Is.False, "12 of 60 is a trickle behind a blob that is still walking");
			Assert.That(TeamLeadCA.PushBarPassed(44, 20000, JoinMin, need, WaveMinValue), Is.False, "44 of 60 is below 45");
			Assert.That(TeamLeadCA.PushBarPassed(46, 20000, JoinMin, need, WaveMinValue), Is.True, "46 of 60 pushes");

			// Before the timeout (3000 ticks after the first hold) the 12 arrived are not pushed by the timeout rule either.
			Assert.That(TeamLeadCA.BarTimeoutPush(1000 + MaxTicks - 1, 1000, MaxTicks, 12, 20000, JoinMin, WaveMinUnits, WaveMinValue), Is.False);
		}

		[Test]
		public void AtTheTimeoutTheHeldOrderPushesTheArrivedBlobAtWaveMinAndBelowItIsStillDissolved()
		{
			const int since = 1000;

			Assert.That(TeamLeadCA.BarTimeoutPush(since + MaxTicks, since, MaxTicks, 12, 20000, JoinMin, WaveMinUnits, WaveMinValue), Is.True, "12 arrived at the timeout: push");
			Assert.That(TeamLeadCA.BarTimeoutPush(since + MaxTicks, since, MaxTicks, 11, 20000, JoinMin, WaveMinUnits, WaveMinValue), Is.False, "below WAVE-MIN units: dissolve");
			Assert.That(TeamLeadCA.BarTimeoutPush(since + MaxTicks, since, MaxTicks, 30, 5999, JoinMin, WaveMinUnits, WaveMinValue), Is.False, "below the WAVE-MIN value: dissolve");
			Assert.That(TeamLeadCA.BarTimeoutPush(since + MaxTicks + 500, since, MaxTicks, 12, 20000, JoinMin, WaveMinUnits, WaveMinValue), Is.True, "after the timeout too");
			Assert.That(TeamLeadCA.BarTimeoutPush(5000, -1, MaxTicks, 60, 90000, JoinMin, WaveMinUnits, WaveMinValue), Is.False, "never held (-1): no timeout");
		}

		// X1: a 4-bot team at tick 26000. The pools hold 100 units, the order holds 50 committed, 40 have arrived. The old bar is half of the team idle army.
		[Test]
		public void TheScenarioOfX1PassesTheCommittedShareBarAndStaysHeldUnderTheOldBar()
		{
			var teamIdle = 100 + 50;
			var oldNeed = BlobLaunch01CA.MinUnits(WaveMinUnits, teamIdle);
			var newNeed = TeamLeadCA.ArrivedBarUnits(WaveMinUnits, 50, Percent);

			Assert.That(oldNeed, Is.EqualTo(75), "the old bar chases the idle army, the home pool included");
			Assert.That(newNeed, Is.EqualTo(38), "the new bar looks at the committed units only");
			Assert.That(TeamLeadCA.PushBarPassed(40, 20000, JoinMin, oldNeed, WaveMinValue), Is.False, "40 arrived is held by the old bar (X1)");
			Assert.That(TeamLeadCA.PushBarPassed(40, 20000, JoinMin, newNeed, WaveMinValue), Is.True, "40 of 50 committed pushes");
			Assert.That(TeamLeadCA.PushBarPassed(30, 20000, JoinMin, newNeed, WaveMinValue), Is.False, "30 of 50 still waits for the rest");
		}

		[Test]
		public void TheBarStillHoldsAnyBlobBelowWaveMinValueAndTheJoinFloor()
		{
			Assert.That(TeamLeadCA.PushBarPassed(12, 5999, JoinMin, WaveMinUnits, WaveMinValue), Is.False, "enough units but below the WAVE-MIN value");
			Assert.That(TeamLeadCA.PushBarPassed(2, 20000, JoinMin, 0, 0), Is.False, "WAVE-MIN off: the join floor still holds a trickle");
			Assert.That(TeamLeadCA.PushBarPassed(3, 0, JoinMin, 0, 0), Is.True, "WAVE-MIN off: the join floor alone");
		}

		[Test]
		public void TheBarWiringUsesTheCommittedShareWhenOnAndWaveMinIsActiveAndKeepsTheOldTermOtherwise()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var held = Between(wiring, "bool TeamLeadPushHeld(", "bool TeamLeadBarTimeoutPush(");

			Assert.That(held, Does.Contain("if (!TeamLeadPushBarOn)\n\t\t\t\treturn false;"), "the bar itself is still the existing switch");
			Assert.That(held, Does.Contain("var committedUnits = order.CommittedTotals().Units;"), "assigned to the order, never the home pool");
			Assert.That(held, Does.Contain("var needUnits = TeamLeadArrivedBarOn ? TeamLeadCA.ArrivedBarUnits(minUnits, committedUnits, Info.TeamLeadBarArrivedPercent) : BlobLaunch01CA.MinUnits(minUnits, teamIdle);"));
			Assert.That(held, Does.Contain("order.ArrivedTotals()"), "the units that arrived, not the whole blob");
			Assert.That(held, Does.Contain("TeamLeadCA.PushBarPassed(units, value, Info.TeamLeadJoinMinUnits, needUnits, minValue)"));
			Assert.That(held, Does.Contain("DiagnosticTeamEvent(\"team_push_hold\""), "the hold record stays");

			Assert.That(wiring, Does.Contain("bool TeamLeadBarArrivedOnlyOn => TeamLeadOn && Info.TeamLeadBarArrivedOnlyEnabled && !ablateBarArrivedOnly.Off(Player, AblationIds.TeamLeadBarArrivedOnly);"));
			Assert.That(wiring, Does.Contain("bool TeamLeadArrivedBarOn => TeamLeadBarArrivedOnlyOn && WaveMinActive;"), "WAVE-MIN off = the old bar, the two rows never compound");
			Assert.That(wiring, Does.Contain("AblationLatch ablateBarArrivedOnly;"));
		}

		[Test]
		public void TheTimeoutPushRunsBeforeTheCancelChainAndOnlyForTheLeadersOwnLiveRallyOrder()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var maintain = Between(wiring, "void TeamLeadMaintainOrder(", "bool TeamLeadPushHeld(");

			const string call = "if (order.LeaderKey == team.Election.Leader && !order.GatherOnly && TeamLeadBarTimeoutPush(order, now))\n\t\t\t\treturn;";
			Assert.That(maintain, Does.Contain(call));
			Assert.That(maintain.IndexOf(call, StringComparison.Ordinal), Is.LessThan(maintain.IndexOf("var cancel = ", StringComparison.Ordinal)), "before the dissolve");
			Assert.That(maintain, Does.Contain(": order.Extending && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, Info.TeamLeadPushBarMaxTicks) ? \"below_bar\""), "below WAVE-MIN the order is dissolved as before");

			var timeout = Between(wiring, "bool TeamLeadBarTimeoutPush(", "// TEAM-BLOB-01 telemetry");
			Assert.That(timeout, Does.Contain("if (!TeamLeadArrivedBarOn)\n\t\t\t\treturn false;"), "off = the old dissolve");
			Assert.That(timeout, Does.Contain("var (minUnits, minValue) = WaveMinBars();"), "plain WAVE-MIN, not the committed share");
			Assert.That(timeout, Does.Contain("TeamLeadCA.BarTimeoutPush(now, order.PushBarSince, Info.TeamLeadPushBarMaxTicks, units, value, Info.TeamLeadJoinMinUnits, minUnits, minValue)"));
			Assert.That(timeout, Does.Contain("order.PushTick = now;"));
			Assert.That(timeout, Does.Contain("order.PushReason = \"bar_timeout\";"));
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
			Assert.That(info, Does.Contain("public readonly int TeamLeadBarArrivedPercent = 75;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], "\n\t\tTeamLeadBarArrivedOnlyEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadBarArrived"), "never in @classic");

			Assert.That(AblationIds.TeamLeadBarArrivedOnly, Is.EqualTo("tl_bar_arrived_only"));
		}
	}
}
