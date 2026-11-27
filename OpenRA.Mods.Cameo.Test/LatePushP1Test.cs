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
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// LATEPUSH-01 P1 (rows lp_age_min and lp_commit; design 01 D1 + D2, design 02 R-A to R-D, D-2 to D-4). The age-scaled team bar and the commit fraction are pure integer functions; the wiring keeps every other bar on WAVE-MIN,
	/// leaves the text of the guard-lend contract and of the old publish path in place, and the new switches are off in code and in classic.
	/// </summary>
	[TestFixture]
	public class LatePushP1Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		// The WAVE-MIN rows of the eleven tiers (design 01 section 2): units / value.
		static readonly (int Units, int Value)[] Tiers =
		{
			(0, 0), (2, 1200), (5, 2400), (7, 3600), (10, 4800), (12, 6000), (14, 7200), (17, 8400), (19, 9600), (22, 10800), (24, 12000)
		};

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

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		// An independent reference of the bar: exact integer fraction, ceiling by quotient and remainder.
		static int ReferenceBar(int tierBase, int tick, int k, int l)
		{
			if (tierBase <= 0)
				return 0;

			var num = (long)tierBase * (30000 + tick) * k * l;
			var q = Math.DivRem(num, 30000L * 100 * 100, out var rem);
			return (int)(rem == 0 ? q : q + 1);
		}

		[Test]
		public void TheBarIsTheTierBarTimesAgeTimesMapTimesLearnedAndMatchesAnIndependentGrid()
		{
			// Design 01 examples at K = 100 and L = 100: Hard 12/6000 -> 24/12000 at 30000 -> 30/15000 at 45000 -> 36/18000 at 60000.
			Assert.That(LatePush.Bar(12, 0, 30000, 100, 100), Is.EqualTo(12));
			Assert.That(LatePush.Bar(12, 30000, 30000, 100, 100), Is.EqualTo(24));
			Assert.That(LatePush.Bar(6000, 30000, 30000, 100, 100), Is.EqualTo(12000));
			Assert.That(LatePush.Bar(12, 45000, 30000, 100, 100), Is.EqualTo(30));
			Assert.That(LatePush.Bar(6000, 45000, 30000, 100, 100), Is.EqualTo(15000));
			Assert.That(LatePush.Bar(12, 60000, 30000, 100, 100), Is.EqualTo(36));
			Assert.That(LatePush.Bar(6000, 60000, 30000, 100, 100), Is.EqualTo(18000));
			// God 24/12000 -> 48/24000 -> 60/30000; Medium 10/4800 -> 20/9600 -> 25/12000.
			Assert.That(LatePush.Bar(24, 30000, 30000, 100, 100), Is.EqualTo(48));
			Assert.That(LatePush.Bar(12000, 45000, 30000, 100, 100), Is.EqualTo(30000));
			Assert.That(LatePush.Bar(10, 45000, 30000, 100, 100), Is.EqualTo(25));
			Assert.That(LatePush.Bar(4800, 45000, 30000, 100, 100), Is.EqualTo(12000));

			foreach (var (units, value) in Tiers)
				foreach (var tierBase in new[] { units, value })
					for (var t = 0; t <= 90000; t += 5000)
						foreach (var k in new[] { 80, 100, 140 })
							foreach (var l in new[] { 50, 85, 100, 120, 200 })
								Assert.That(LatePush.Bar(tierBase, t, 30000, k, l), Is.EqualTo(ReferenceBar(tierBase, t, k, l)), $"base {tierBase} t {t} k {k} l {l}");

			// No limit stays no limit; the clamps hold; the factor keeps rising after 30000 (not capped); a bad age window cannot divide by zero.
			Assert.That(LatePush.Bar(0, 60000, 30000, 140, 200), Is.EqualTo(0));
			Assert.That(LatePush.Bar(-5, 60000, 30000, 140, 200), Is.EqualTo(0));
			Assert.That(LatePush.Bar(12, 0, 30000, 10, 10), Is.EqualTo(LatePush.Bar(12, 0, 30000, LatePush.MapPercentMin, LatePush.LearnedPercentMin)));
			Assert.That(LatePush.Bar(12, 0, 30000, 900, 900), Is.EqualTo(LatePush.Bar(12, 0, 30000, LatePush.MapPercentMax, LatePush.LearnedPercentMax)));
			Assert.That(LatePush.Bar(12, 90000, 30000, 100, 100), Is.GreaterThan(LatePush.Bar(12, 60000, 30000, 100, 100)));
			Assert.That(LatePush.Bar(12, 1000, 0, 100, 100), Is.GreaterThan(0));
			Assert.That(LatePush.Bar(12, -500, 30000, 100, 100), Is.EqualTo(12), "a negative tick is no age");
			Assert.That(LatePush.AgePermille(30000, 30000), Is.EqualTo(2000));
			Assert.That(LatePush.AgePermille(-1, 30000), Is.EqualTo(1000));
		}

		[Test]
		public void TheBarIsNeverAboveTheCapOfWhatTheTeamCanSendAndNeverBelowWaveMin()
		{
			// Hard at t = 60000: M = 36 units; a team of 40 eligible units at the 60 percent cap can reach 24.
			Assert.That(LatePush.EffectiveBar(36, 12, 60, 40), Is.EqualTo(24));
			Assert.That(LatePush.EffectiveBar(36, 12, 60, 100), Is.EqualTo(36), "a big army reaches the full bar");
			Assert.That(LatePush.EffectiveBar(36, 12, 60, 10), Is.EqualTo(12), "a small team falls back to the WAVE-MIN floor, never below it");
			Assert.That(LatePush.EffectiveBar(36, 12, 60, 0), Is.EqualTo(12), "no eligible army reported: the old WAVE-MIN bar");
			Assert.That(LatePush.EffectiveBar(36, 12, 60, -4), Is.EqualTo(12));
			Assert.That(LatePush.EffectiveBar(8, 12, 60, 100), Is.EqualTo(12), "an age-scaled bar below WAVE-MIN (small map, low L) is lifted to the floor");
			Assert.That(LatePush.EffectiveBar(0, 0, 60, 100), Is.EqualTo(0), "no limit stays no limit");
			Assert.That(LatePush.EffectiveBar(15000, 6000, 60, 20000L), Is.EqualTo(12000), "values use the same rule (long eligible)");

			for (var bar = 0; bar <= 80; bar += 4)
				for (var eligible = 0; eligible <= 120; eligible += 6)
				{
					var e = LatePush.EffectiveBar(bar, 12, 60, eligible);
					Assert.That(e, Is.GreaterThanOrEqualTo(12));
					Assert.That(e, Is.LessThanOrEqualTo(Math.Max(12, bar)));
				}
		}

		[Test]
		public void TheMapContextIsTheDistanceOverTheLearnedReferenceClampedAndNeutralBeforeTheFirstPick()
		{
			Assert.That(LatePush.MapPercent(-1, 64), Is.EqualTo(100), "no target picked yet");
			Assert.That(LatePush.MapPercent(64, 64), Is.EqualTo(100));
			Assert.That(LatePush.MapPercent(0, 64), Is.EqualTo(80), "a target at the door still needs the lower clamp, not zero");
			Assert.That(LatePush.MapPercent(32, 64), Is.EqualTo(80));
			Assert.That(LatePush.MapPercent(80, 64), Is.EqualTo(125));
			Assert.That(LatePush.MapPercent(200, 64), Is.EqualTo(140));
			Assert.That(LatePush.MapPercent(64, 128), Is.EqualTo(80), "the learned reference moves the same distance down");
			Assert.That(LatePush.MapPercent(64, 0), Is.EqualTo(140), "a bad reference cannot divide by zero");
		}

		[Test]
		public void TheCommitShareGrowsWithTimeAndWithTheObservedRatioAndIsAllOfItWhenAhead()
		{
			// Design 01 D2: 30 / 45 / 60 / 75 percent at 0 / 15000 / 30000 / 45000, capped at 85 from time alone.
			Assert.That(LatePush.CommitPercent(0, 30000, 30, 85, 100, 150), Is.EqualTo(30));
			Assert.That(LatePush.CommitPercent(15000, 30000, 30, 85, 100, 150), Is.EqualTo(45));
			Assert.That(LatePush.CommitPercent(30000, 30000, 30, 85, 100, 150), Is.EqualTo(60));
			Assert.That(LatePush.CommitPercent(45000, 30000, 30, 85, 100, 150), Is.EqualTo(75));
			Assert.That(LatePush.CommitPercent(55000, 30000, 30, 85, 100, 150), Is.EqualTo(85));
			Assert.That(LatePush.CommitPercent(90000, 30000, 30, 85, 100, 150), Is.EqualTo(85), "the cap holds from time alone");

			// t = 30000, r = 125 -> 80; r at the all-in ratio or more -> 100 (it IS the all-in); r below 100 adds nothing.
			Assert.That(LatePush.CommitPercent(30000, 30000, 30, 85, 125, 150), Is.EqualTo(80));
			Assert.That(LatePush.CommitPercent(30000, 30000, 30, 85, 150, 150), Is.EqualTo(100));
			Assert.That(LatePush.CommitPercent(0, 30000, 30, 85, 400, 150), Is.EqualTo(100));
			Assert.That(LatePush.CommitPercent(30000, 30000, 30, 85, 60, 150), Is.EqualTo(60));

			// L_commit scales the base inside its clamp; a bad all-in ratio cannot divide by zero.
			Assert.That(LatePush.CommitPercent(0, 30000, 30, 85, 100, 150, 150), Is.EqualTo(45));
			Assert.That(LatePush.CommitPercent(0, 30000, 30, 85, 100, 150, 50), Is.EqualTo(15));
			Assert.That(LatePush.CommitPercent(0, 30000, 30, 85, 100, 150, 1000), Is.EqualTo(45), "clamped to 150");
			Assert.That(LatePush.CommitPercent(0, 30000, 30, 85, 101, 100), Is.EqualTo(100));

			// Monotone in the tick and in the ratio, always inside 0..100.
			var last = -1;
			for (var t = 0; t <= 120000; t += 3000)
			{
				var f = LatePush.CommitPercent(t, 30000, 30, 85, 100, 150);
				Assert.That(f, Is.GreaterThanOrEqualTo(last));
				Assert.That(f, Is.InRange(0, 85));
				last = f;
			}

			last = -1;
			for (var r = 0; r <= 300; r += 5)
			{
				var f = LatePush.CommitPercent(20000, 30000, 30, 85, r, 150);
				Assert.That(f, Is.GreaterThanOrEqualTo(last));
				Assert.That(f, Is.InRange(0, 100));
				last = f;
			}
		}

		[Test]
		public void TheLendTakesOnlyWhatTheShareNeedsAndNeverMoreThanTheGuardAboveTheFloor()
		{
			// free 5, guard above the floor 20, E = 25: 60 percent is 15 units, 10 of them from the guard.
			Assert.That(LatePush.Eligible(5, 20), Is.EqualTo(25));
			Assert.That(LatePush.LendPlan(60, 5, 20), Is.EqualTo(10));
			Assert.That(LatePush.LendPlan(100, 5, 20), Is.EqualTo(20), "the whole eligible army: all of the spare");
			Assert.That(LatePush.LendPlan(10, 5, 20), Is.EqualTo(0), "the free units already meet the share");
			Assert.That(LatePush.LendPlan(60, 40, 20), Is.EqualTo(0));
			Assert.That(LatePush.LendPlan(60, 5, 0), Is.EqualTo(0), "no guard above the floor, nothing to lend");
			Assert.That(LatePush.LendPlan(60, -3, 20), Is.EqualTo(LatePush.LendPlan(60, 0, 20)), "a negative free count is none");
			Assert.That(LatePush.LendPlan(60, 5, -2), Is.EqualTo(0));
			Assert.That(LatePush.LendPlan(250, 5, 20), Is.EqualTo(20), "the share is clamped to 100 percent");

			for (var f = 0; f <= 100; f += 5)
				for (var free = 0; free <= 40; free += 4)
					for (var spare = 0; spare <= 40; spare += 4)
					{
						var lend = LatePush.LendPlan(f, free, spare);
						var want = (int)Math.Ceiling((free + spare) * f / 100.0);
						Assert.That(lend, Is.InRange(0, spare));
						Assert.That(lend, Is.EqualTo(spare == 0 ? 0 : Math.Clamp(want - free, 0, spare)), $"f {f} free {free} spare {spare}");
					}
		}

		[Test]
		public void TheTableHoldsYamlPriorsOverridesClampsAndRestoresThePrior()
		{
			var t = new LatePushTable(85, 64);
			Assert.That(t.LearnedPercent, Is.EqualTo(85));
			Assert.That(t.CommitPercent, Is.EqualTo(100));
			Assert.That(t.MapRefCells, Is.EqualTo(64), "R-A: the reference distance is a table entry, the yaml value its prior");

			t.Override(LatePushTable.LearnedKey, 400);
			t.Override(LatePushTable.CommitKey, 10);
			t.Override(LatePushTable.MapRefKey, 1);
			Assert.That(t.LearnedPercent, Is.EqualTo(LatePush.LearnedPercentMax));
			Assert.That(t.CommitPercent, Is.EqualTo(LatePush.CommitLearnedMin));
			Assert.That(t.MapRefCells, Is.EqualTo(LatePush.MapRefMin));
			Assert.That(t.Overrides.Count, Is.EqualTo(3));

			t.Override(LatePushTable.MapRefKey, 1000);
			Assert.That(t.MapRefCells, Is.EqualTo(LatePush.MapRefMax));
			t.ClearOverride(LatePushTable.LearnedKey);
			Assert.That(t.LearnedPercent, Is.EqualTo(85));
			t.ClearOverrides();
			Assert.That(t.CommitPercent, Is.EqualTo(100));
			Assert.That(t.MapRefCells, Is.EqualTo(64));
			Assert.That(t.Overrides, Is.Empty);

			Assert.That(new LatePushTable(-9, -9).LearnedPercent, Is.EqualTo(LatePush.LearnedPercentMin), "a bad prior is clamped too");
			Assert.That(new LatePushTable(-9, -9).MapRefCells, Is.EqualTo(LatePush.MapRefMin));
		}

		[Test]
		public void TheTeamReportsEligibleArmyTheLastTargetAndTheObservedRatio()
		{
			var team = new TeamLeadTeam();

			// Without a figure of its own a seat is eligible with its blob (D-2: the row off changes nothing).
			team.Publish("a", 10, 5000, 100);
			team.Publish("b", 8, 4000, 100);
			Assert.That(team.Eligible(120, 300), Is.EqualTo((18, 9000L)));
			Assert.That(team.Pool(120, 300), Is.EqualTo((18, 9000L)));

			// A seat's own eligible figure replaces its blob in the eligible sum only.
			team.PublishEligible("a", 25, 12000, 100);
			Assert.That(team.Eligible(120, 300), Is.EqualTo((33, 16000L)));
			Assert.That(team.Pool(120, 300), Is.EqualTo((18, 9000L)), "the blob the order counts is untouched");

			// The same staleness rule as the pool.
			Assert.That(team.Eligible(500, 300), Is.EqualTo((0, 0L)));
			team.Publish("a", 11, 5500, 450);
			Assert.That(team.Eligible(500, 300), Is.EqualTo((11, 5500L)));

			// The last picked target: none (-1) before the first pick.
			Assert.That(team.LastTargetTick, Is.EqualTo(-1));
			team.NoteTarget(new WPos(1024 * 80, 0, 0), 777);
			Assert.That(team.LastTargetTick, Is.EqualTo(777));
			Assert.That(team.LastTarget, Is.EqualTo(new WPos(1024 * 80, 0, 0)));

			// The observed ratio: 100 (unknown) until the leader scanned, and again when the scan is stale.
			Assert.That(team.AheadRatio(1000, 1000), Is.EqualTo(100));
			team.NoteAheadRatio(160, 1000);
			Assert.That(team.AheadRatio(1100, 300), Is.EqualTo(160));
			Assert.That(team.AheadRatio(1400, 300), Is.EqualTo(100));
		}

		[Test]
		public void TheRowsHaveTheirOwnAblationIdsInfoSwitchesOffInCodeAndInClassicAndPriorsPerPersonality()
		{
			Assert.That(AblationIds.LpAgeMin, Is.EqualTo("lp_age_min"));
			Assert.That(AblationIds.LpCommit, Is.EqualTo("lp_commit"));
			Assert.That(Regex.IsMatch(AblationIds.LpAgeMin, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");
			Assert.That(Regex.IsMatch(AblationIds.LpCommit, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True);
			var ids = RepoFile(Bots + "BotModuleLogic/AblationIds.LatePush.cs");
			Assert.That(ids, Does.Contain("public const string LpAgeMin = \"lp_age_min\";"));
			Assert.That(ids, Does.Contain("public const string LpCommit = \"lp_commit\";"));

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.LatePush.cs");
			Assert.That(info, Does.Contain("public readonly bool LatePushAgeMinEnabled = false;"), "off in code");
			Assert.That(info, Does.Contain("public readonly bool LatePushCommitEnabled = false;"), "off in code");
			Assert.That(info, Does.Contain("public readonly int LatePushAgeTicks = 30000;"));
			Assert.That(info, Does.Contain("public readonly int LatePushCapPercent = 60;"));
			Assert.That(info, Does.Contain("public readonly int LatePushCommitBasePercent = 30;"));
			Assert.That(info, Does.Contain("public readonly int LatePushCommitCapPercent = 85;"));
			Assert.That(info, Does.Contain("public readonly int LatePushMapRefCells = 64;"));
			Assert.That(info, Does.Contain("public readonly int LatePushPersonalityPercent = 100;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(yaml[classic..], Does.Not.Contain("LatePush"), "classic untouched");
			// Both rows ship OFF in every block until their A/B passes (Opus, 2026-11-27); the A/B arm turns them on.
			foreach (var key in new[] { "LatePushAgeMinEnabled: false", "LatePushCommitEnabled: false", "LatePushAgeTicks: 30000", "LatePushCapPercent: 60", "LatePushCommitBasePercent: 30", "LatePushCommitCapPercent: 85", "LatePushMapRefCells: 64" })
				Assert.That(Count(yaml[..classic], "\n\t\t" + key + "\n"), Is.EqualTo(6), key);

			// L's prior per personality: rush and guerrilla 85, tech and expansion 100, steamroller 110, turtle 120 (design 01 D1).
			int Prior(string block)
			{
				var start = yaml.IndexOf("\tSquadManagerBotModuleCA@" + block + ":", StringComparison.Ordinal);
				Assert.That(start, Is.GreaterThan(0), block);
				var m = Regex.Match(yaml[start..], "\n\t\tLatePushPersonalityPercent: (\\d+)\n");
				Assert.That(m.Success, Is.True, block);
				return int.Parse(m.Groups[1].Value);
			}

			Assert.That(new[] { "rush", "guerrilla" }.Select(Prior), Is.All.EqualTo(85));
			Assert.That(new[] { "tech", "expansion" }.Select(Prior), Is.All.EqualTo(100));
			Assert.That(Prior("steamroller"), Is.EqualTo(110));
			Assert.That(Prior("turtle"), Is.EqualTo(120));
			Assert.That(Count(yaml, "LatePushPersonalityPercent:"), Is.EqualTo(6));
			Assert.That(yaml, Does.Not.Contain("LatePushAgeMinEnabled: true"), "default OFF until the A/B passes");
			Assert.That(yaml, Does.Not.Contain("LatePushCommitEnabled: true"), "default OFF until the A/B passes");
		}

		[Test]
		public void OnlyTheLeadersOrdinaryBarsAreAgeScaledEveryOtherWaveMinSiteKeepsItsBar()
		{
			var lead = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var publish = Between(lead, "void TeamLeadPublish(TeamLeadTeam team, int now)", "void TeamLeadPromoteRally(TeamLeadOrder order, int now)");
			Assert.That(publish, Does.Contain("var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);"), "the rally floor stays on WAVE-MIN");
			Assert.That(publish, Does.Contain("var (barUnits, barValue) = WaveMinActive ? LatePushBars(team, now, allIn != null) : (0, 0);"), "an open all-in keeps WAVE-MIN (P2 owns the all-in publish)");
			Assert.That(publish, Does.Contain("var ready = TeamLeadCA.BlobReady(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(barUnits, barValue), barUnits, barValue);"));
			Assert.That(publish, Does.Contain("!TeamLeadCA.RallyFloorPassed(blob.Units, blob.Value, BlobLaunch01CA.MinUnits(minUnits, blob.Units), minValue))"), "the old rally floor text is untouched");
			Assert.That(publish.IndexOf("if (!picked)\n\t\t\t\treturn;\n\n\t\t\t// LATEPUSH-01 P1 (design 02 D-3)", StringComparison.Ordinal), Is.GreaterThan(0));
			Assert.That(publish, Does.Contain("team.NoteTarget(target, now);"), "the last picked target feeds the map context (D-3)");
			Assert.That(publish.IndexOf("team.NoteTarget(target, now);", StringComparison.Ordinal), Is.GreaterThan(publish.IndexOf("if (!picked)", StringComparison.Ordinal)));

			var promote = Between(lead, "void TeamLeadPromoteRally(TeamLeadOrder order, int now)", "// Every member (the leader too) forms its wave");
			Assert.That(promote, Does.Contain("var (minUnits, minValue) = WaveMinActive ? LatePushBars(teamLeadTeam, now, order.AllIn) : (0, 0);"));
			Assert.That(promote, Does.Contain("if (!TeamLeadCA.BlobReady(units, value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue))"), "the promotion asks the same bars as the ready path");
			Assert.That(promote, Does.Contain("teamLeadTeam?.NoteTarget(target, now);"));

			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs"), Does.Contain("var (minUnits, minValue) = WaveMinActive ? LatePushBars(team, now, false) : (0, 0);\n\t\t\t\tif (TeamLeadCA.BlobReady(blob.Units, blob.Value"),
				"the nearly-won scan asks the same age-scaled bars as the ready test");

			// Exactly three call sites: the ready test, the rally promotion, the nearly-won scan. No member-side floor (squad launch, keeps-going, pincer, reinforce, stalemate) is age-scaled.
			var root = Bots;
			var calls = 0;
			foreach (var name in new[] { "TeamLead", "V07", "Pipeline02", "PincerPush", "Stalemate01", "WaveMin", "Squad", "AllInAhead", "GuardLend", "GuardFloor" })
			{
				var file = name == "Squad" ? "SquadManagerBotModuleCA.cs" : "SquadManagerBotModuleCA." + name + ".cs";
				var text = RepoFile(root + file);
				calls += Count(text, "LatePushBars(");
			}

			Assert.That(calls, Is.EqualTo(3), "TeamLead.cs: the ready test and the rally promotion, V07.cs: the nearly-won scan; nowhere else");
		}

		[Test]
		public void TheBarFunctionHasOnlyTierAgeMapAndLearnedAndTheAllInKeepsWaveMin()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushBars.cs");
			var bars = text[text.IndexOf("(int Units, int Value) LatePushBars(TeamLeadTeam team, int now, bool allIn)", StringComparison.Ordinal)..];
			Assert.That(bars, Does.Contain("var bars = WaveMinBars();"), "the base is the WAVE-MIN bar as used today");
			Assert.That(bars, Does.Contain("if (allIn || team == null || !LatePushAgeMinOn || !WaveMinCA.Limits(bars.Units, bars.Value))\n\t\t\t\treturn bars;"));
			Assert.That(bars, Does.Contain("var mu = LatePush.Bar(bars.Units, now, Info.LatePushAgeTicks, k, l);"));
			Assert.That(bars, Does.Contain("var mv = LatePush.Bar(bars.Value, now, Info.LatePushAgeTicks, k, l);"));
			Assert.That(bars, Does.Contain("var l = LatePushParameters.LearnedPercent;"), "L is the one learned multiplier (R-C)");
			Assert.That(bars, Does.Contain("LatePush.EffectiveBar(mu, bars.Units, Info.LatePushCapPercent, eligible.Units)"));
			Assert.That(bars, Does.Contain("LatePush.EffectiveBar(mv, bars.Value, Info.LatePushCapPercent, eligible.Value)"));

			// R-C: no second multiplier from the existing aggression values.
			foreach (var banned in new[] { "MaxIdleUnits", "SquadValue", "AggTeamReserve", "MinimumAttackForceDelay", "Personality", "PersonalityPercent" })
				Assert.That(bars, Does.Not.Contain(banned), "no other factor in the bar: " + banned);

			// The map context reads the last picked target (D-3), never the one about to be picked, and only observed positions.
			var map = Between(text, "int LatePushMapPercent(TeamLeadTeam team, out int cells)", "/// <summary>\n\t\t/// The bars of the leader's ordinary team push");
			Assert.That(map, Does.Contain("team.LastTargetTick < 0"));
			Assert.That(map, Does.Contain("(team.LastTarget - AggTeamHomePos()).HorizontalLength / WDist.FromCells(1).Length"));
			Assert.That(map, Does.Contain("LatePush.MapPercent(cells, LatePushParameters.MapRefCells)"), "R-A: the reference distance is the table entry");

			// No scan, no RNG, no enemy state: the eligible figure rides on the blob the seat already computes.
			foreach (var banned in new[] { "FindClosestEnemy", "FindActorsInCircle", "World.Actors", "ActorsWithTrait", "FrozenActorLayer", "Random", "personality", "Trait<" })
				Assert.That(text, Does.Not.Contain(banned), "per-check code: no scan, no RNG (" + banned + ")");

			Assert.That(RepoFile(Bots + "Squads/LatePush.cs"), Does.Not.Contain("Random"));
			Assert.That(RepoFile(Bots + "Squads/LatePush.cs"), Does.Not.Contain("World"), "pure: no world access");
		}

		[Test]
		public void TheCommitRowNeedsTheGuardFloorAndSaysSoOnceInTheDevLog()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushBars.cs");
			Assert.That(text, Does.Contain("bool LatePushCommitRequested => ArmyGuardLendOn && Info.LatePushCommitEnabled && !ablateLpCommit.Off(Player, AblationIds.LpCommit);"));
			Assert.That(text, Does.Contain("internal bool LatePushCommitOn => LatePushCommitRequested && GuardFloorOn;"), "design 02 R-D: the floor is the one reserve");
			Assert.That(text, Does.Contain("internal bool LatePushAgeMinOn => ArmyLaunchArbiterOn && WaveMinActive && Info.LatePushAgeMinEnabled && !ablateLpAgeMin.Off(Player, AblationIds.LpAgeMin);"));

			Assert.That(Count(text, "inert: guard floor off"), Is.EqualTo(1));
			Assert.That(text, Does.Contain("if (LatePushCommitRequested && !GuardFloorOn && !latePushInertLogged && DiagnosticWriter != null)\n\t\t\t{\n\t\t\t\tlatePushInertLogged = true;"), "once per seat, DEV writer only, a release build builds no string");
			Assert.That(text, Does.Not.Contain("#if"), "release code: the DEV rows are guarded by the writer, like the other team events");

			// All-in: the lend plan is not replaced while an all-in order is open (the existing all-in lend rule stays, P2 owns the all-in publish).
			var lend = text[text.IndexOf("int LatePushLend(SquadCA guard, int ordinaryLend)", StringComparison.Ordinal)..text.IndexOf("int LatePushGuardSpare(", StringComparison.Ordinal)];
			Assert.That(lend, Does.Contain("if (!LatePushCommitOn || TeamLeadOpenOrder() is { AllIn: true })\n\t\t\t\treturn ordinaryLend;"));
			Assert.That(lend, Does.Contain("Math.Min(LatePush.LendPlan(f, free, spare), armyGuardLend.Count)"), "never a plan above the list the cut reads");
		}

		[Test]
		public void TheGuardLendContractAndTheBlobKeepTheirOldTextWithTheHooksAroundIt()
		{
			var lendText = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			var share = lendText.IndexOf("var lend = GuardLend.Units(guard.Units.Count, armyGuardLend.Count, Info.ArmyGuardLendPercent, Info.TeamLeadJoinMinUnits);", StringComparison.Ordinal);
			var hook = lendText.IndexOf("lend = LatePushLend(guard, lend);", StringComparison.Ordinal);
			var floor = lendText.IndexOf("if (GuardFloorOn)\n\t\t\t\tlend = GuardFloorLend(guard, lend);", StringComparison.Ordinal);
			Assert.That(share, Is.GreaterThan(0));
			Assert.That(hook, Is.GreaterThan(share), "after the ordinary share");
			Assert.That(floor, Is.GreaterThan(hook), "before the floor, so the plan still goes through the one lend contract");
			Assert.That(Count(lendText, "LatePushLend("), Is.EqualTo(1));
			Assert.That(Count(lendText, "GuardFloorLend("), Is.EqualTo(1));

			var lead = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var blob = Between(lead, "(int Units, long Value) TeamLeadBlob()", "bool TeamLeadWaveSquad(SquadCA s)");
			var begin = blob.IndexOf("LatePushBlobBegin(units, value);", StringComparison.Ordinal);
			var guardCall = blob.IndexOf("ArmyGuardBlob(ref units, ref value);", StringComparison.Ordinal);
			var end = blob.IndexOf("LatePushBlobEnd();", StringComparison.Ordinal);
			Assert.That(begin, Is.GreaterThan(blob.IndexOf("value += SquadValueOf(s);", StringComparison.Ordinal)), "the free army is read after the wave squads, before the lend");
			Assert.That(guardCall, Is.GreaterThan(begin));
			Assert.That(end, Is.GreaterThan(guardCall));
			Assert.That(blob.IndexOf("return (units, value);", StringComparison.Ordinal), Is.GreaterThan(end));
			Assert.That(lead, Does.Contain("TeamLeadCadenceActive ? TeamLeadBlob() : (unitsHangingAroundTheBase.Count, TeamLeadPoolValue())"), "the publish call is untouched");

			// The free-army definition of the join pass is the one of the blob.
			var free = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushBars.cs");
			var freeBody = Between(free, "int LatePushFreeUnits()", "// The share of the eligible army one order commits now");
			Assert.That(blob, Does.Contain("unitsHangingAroundTheBase.Count(TeamLeadJoinable) : unitsHangingAroundTheBase.Count"));
			Assert.That(freeBody, Does.Contain("if (TeamLeadJoinable(u))"));
			Assert.That(freeBody, Does.Contain("units = unitsHangingAroundTheBase.Count;"));
			Assert.That(freeBody, Does.Contain("if (TeamLeadWaveSquad(s))\n\t\t\t\t\tunits += s.Units.Count;"));

			// The team state: a seat without a figure of its own is eligible with its blob; the observed ratio is noted by the leader's scan.
			Assert.That(RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs"), Does.Contain("pools[key] = (units, value, tick);\n\t\t\teligible[key] = (units, value, tick);"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.AllInAhead.cs"), Does.Contain("LatePushNoteRatio(v, now);\n\t\t\tv.Scanned = true;"));
			Assert.That(free, Does.Contain("v.ArmyKnown && v.FactsKnown && enemy > 0 ? (int)Math.Clamp(100L * v.OwnArmy / enemy, 0, 1000) : 100"), "100 (unknown, time only) while the enemy army is not observed");
		}
	}
}
