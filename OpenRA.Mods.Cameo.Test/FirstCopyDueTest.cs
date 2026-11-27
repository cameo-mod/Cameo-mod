#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// FIRST-COPY-DUE-01 (row ub_first_copy_due; RULES_AUDIT_04 A5, user D3): every newly available unit type gets a real first-copy deadline (about two minutes, learned). Several types overdue:
	/// the one overdue longest first, a tie to the cheaper. Pay-as-you-go: the clock pauses only with no cash AND no income. OFF by default, with an ablation row.
	/// </summary>
	[TestFixture]
	public class FirstCopyDueTest
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
		public void TheClockPausesOnlyWithNoCashAndNoIncome()
		{
			Assert.That(FirstCopyDue.Alive(500, 0, false), Is.True, "cash, no income: running");
			Assert.That(FirstCopyDue.Alive(0, 0, true), Is.True, "no cash but income: pay-as-you-go keeps the clock running");
			Assert.That(FirstCopyDue.Alive(0, 0, false), Is.False, "no cash AND no income: paused");
			Assert.That(FirstCopyDue.Alive(50, 50, false), Is.False, "a bank at the learned floor counts as no cash");
			Assert.That(FirstCopyDue.Alive(51, 50, false), Is.True);
			Assert.That(FirstCopyDue.Alive(0, -5, false), Is.False, "a negative floor cannot make an empty bank cash");

			Assert.That(FirstCopyDue.Step(100, 160, true), Is.EqualTo(60));
			Assert.That(FirstCopyDue.Step(100, 160, false), Is.EqualTo(0), "a dead economy credits nothing");
			Assert.That(FirstCopyDue.Step(-1, 160, true), Is.EqualTo(0), "a new clock starts at its first sighting");
			Assert.That(FirstCopyDue.Step(160, 160, true), Is.EqualTo(0), "a re-roll on the same tick adds nothing");
			Assert.That(FirstCopyDue.Step(0, 100000, true), Is.EqualTo(FirstCopyDue.MaxStepTicks), "a gap in which the bot was not visited at all is not credited; a hard gate does not pause the clock (user D3: only no cash AND no income does)");
		}

		[Test]
		public void TheTypeOverdueLongestGoesFirstAndATieGoesToTheCheaperType()
		{
			long none = long.MinValue;
			Assert.That(FirstCopyDue.Pick([none, none], [10, 20]), Is.EqualTo(-1), "nothing due (or all already ordered): the roll decides");
			Assert.That(FirstCopyDue.Pick([-1, -400], [10, 20]), Is.EqualTo(-1), "not due yet");
			Assert.That(FirstCopyDue.Pick([0, none], [900, 5]), Is.EqualTo(0), "exactly due counts");
			Assert.That(FirstCopyDue.Pick([100, 700, 300], [50, 900, 10]), Is.EqualTo(1), "the longest overdue wins even when it is the dearest");
			Assert.That(FirstCopyDue.Pick([300, 300, 300], [900, 100, 500]), Is.EqualTo(1), "a tie: the cheaper type");
			Assert.That(FirstCopyDue.Pick([300, 300], [100, 100]), Is.EqualTo(0), "equal overdue and equal price: the lower index (the pool is name-ordered)");
			Assert.That(FirstCopyDue.Pick([none, 5, none, 5], [1, 70, 2, 60]), Is.EqualTo(3), "ordered types never compete");
		}

		[Test]
		public void TheDeadlineIsALearnedKnobWithPriorsAndZeroMeansNoDeadline()
		{
			Assert.That(FirstCopyDue.Overdue(3000, 3000), Is.EqualTo(0));
			Assert.That(FirstCopyDue.Overdue(2999, 3000), Is.EqualTo(-1));
			Assert.That(FirstCopyDue.Overdue(4500, 3000), Is.EqualTo(1500));
			Assert.That(FirstCopyDue.Overdue(1_000_000, 0), Is.LessThan(0), "0 = the service is off for every type");

			var table = new UnitRollTable(null, null, null, null, 0, 0);
			Assert.That(table.FirstDueTicks, Is.EqualTo(3000), "prior: two minutes at 25 ticks per second");
			Assert.That(table.FirstDueCash, Is.EqualTo(0), "prior: an empty bank is no cash");
			table.Override(UnitRollTable.FirstDueKey, 2000);
			table.Override(UnitRollTable.FirstDueCashKey, 120);
			Assert.That(table.FirstDueTicks, Is.EqualTo(2000));
			Assert.That(table.FirstDueCash, Is.EqualTo(120));
			table.Override(UnitRollTable.FirstDueKey, -9);
			Assert.That(table.FirstDueTicks, Is.EqualTo(0), "clamped, never negative");
			table.ClearOverrides();
			Assert.That(table.FirstDueTicks, Is.EqualTo(3000), "clearing restores the prior");
			Assert.That(new UnitRollTable(null, null, null, null, 0, 0, firstDueTicks: 1800, firstDueCash: 10).FirstDueTicks, Is.EqualTo(1800), "the yaml value is the prior");
		}

		[Test]
		public void TheSwitchIsOffByDefaultHasItsAblationRowAndTheOldWeightBoostIsUntouched()
		{
			Assert.That(AblationIds.UnitFirstCopyDue, Is.EqualTo("ub_first_copy_due"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.UnitFirstCopyDue.cs"), Does.Contain("public const string UnitFirstCopyDue = \"ub_first_copy_due\";"));

			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("public readonly bool FirstCopyDue = false;"), "OFF by default: only a yaml arms it");
			Assert.That(unit, Does.Contain("public readonly int UnitRollFirstDueTicks = 3000;"));
			Assert.That(unit, Does.Contain("public readonly int UnitRollFirstDueCash = 0;"));
			Assert.That(unit, Does.Contain("AblationLatch ablateFirstCopyDue;"));
			Assert.That(unit, Does.Contain("bool FirstCopyDueOn => UnitRollOn && Info.FirstCopyDue && !ablateFirstCopyDue.Off(player, AblationIds.UnitFirstCopyDue);"), "needs the roll, the switch and the row");
			Assert.That(unit, Does.Contain("Info.TechLeanBankPct,\n\t\t\t\tInfo.UnitRollFirstDueTicks, Info.UnitRollFirstDueCash)"), "both knobs reach the learnable table");

			// The old weight-only boost stays exactly as it was: the due service sits on top of it.
			Assert.That(unit, Does.Contain("UnitRoll.FirstBoost(RollParameters.FirstBoost, world.WorldTick - rollAvailableSince[name], RollParameters.FirstRampTicks, RollParameters.FirstCap)"));
			// B1 ARM (playtest night 11-27): armed in the generic UnitBuilder block only; classic untouched; the code default stays off.
			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var armed = yaml.IndexOf("\n\t\tFirstCopyDue: true", StringComparison.Ordinal);
			Assert.That(yaml.Split("\n\t\tFirstCopyDue: true").Length - 1, Is.EqualTo(1), "armed exactly once");
			Assert.That(armed, Is.GreaterThan(yaml.IndexOf("	UnitBuilderBotModuleCA@generic:", StringComparison.Ordinal)));
			Assert.That(armed, Is.LessThan(yaml.IndexOf("	UnitBuilderBotModuleCA@classic:", StringComparison.Ordinal)));
			Assert.That(yaml, Does.Not.Contain("UnitRollFirstDueTicks:").And.Not.Contain("UnitRollFirstDueCash:"), "the thresholds stay at their code priors");
		}

		[Test]
		public void TheRollAdvancesTheClocksAndForcesThePickOnlyWhileTheRowIsOnWithoutChangingTheRandomDraw()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var roll = unit.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			Assert.That(roll, Is.GreaterThan(0));
			var body = unit[roll..];

			Assert.That(body, Does.Contain("if (FirstCopyDueOn)\n\t\t\t{\n\t\t\t\tTrackFirstCopyClocks(pool);\n\t\t\t\tSweepFirstCopyClocks(true);\n\t\t\t}"), "the clocks move only while the row is on");
			Assert.That(unit, Does.Contain("if (FirstCopyDueOn)\n\t\t\t\tSweepFirstCopyClocks(false);\n\n\t\t\t// TF-2:"), "every bot visit samples the clocks, before any pause return (the roll alone visits too rarely)");
			Assert.That(unit, Does.Not.Contain("rollDueIncome"), "no dead fields");

			const string Draw = "var pick = decision.Pick;";
			var draw = body.IndexOf(Draw, StringComparison.Ordinal);
			var forced = body.IndexOf("var dueIndex = FirstCopyDuePick(queue, pool, out var overdueBy, out var dueCount);", StringComparison.Ordinal);
			var first = body.IndexOf("rollFirst = !rollOrdered.Contains(pool[pick].Name);", StringComparison.Ordinal);
			Assert.That(draw, Is.GreaterThan(0), "the random draw is the old line");
			Assert.That(forced, Is.GreaterThan(draw), "the draw is taken first: the random stream is the same with the row on or off");
			Assert.That(first, Is.GreaterThan(forced), "a forced pick is still a first-copy pick (pay-as-you-go admission)");
			Assert.That(body.Substring(draw, first - draw), Does.Contain("if (FirstCopyDueOn)").And.Contain("pick = dueIndex;"));

			var half = RepoFile(Bots + "UnitBuilderBotModuleCA.FirstCopyDue.cs");
			Assert.That(half, Does.Contain("bool FirstDueHasIncome() => !RefreshHarvesterCounts() || harvestersLive > 0;"), "income = a live harvester; no harvester module = assumed");
			Assert.That(half, Does.Contain("FirstCopyDue.Alive(playerResources.GetCashAndResources(), RollParameters.FirstDueCash, FirstDueHasIncome())"));
			Assert.That(half, Does.Not.Contain("Random").And.Not.Contain("BotRng"), "no random number in the due service");
			Assert.That(half, Does.Contain("const int FirstCopySweepTicks = 25;").And.Contain("FirstCopyDue.Step(rollDueAt[name], now, alive)"), "a sweep credits the time since the last advance");
		}
	}
}
