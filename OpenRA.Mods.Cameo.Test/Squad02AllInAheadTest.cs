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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// ALLIN-TRIGGER-01 (USER 2026-11-25, designer-dynamic-all-in): the all-in is dynamic. The rules are pure (AllInAhead, AheadLedger, AheadWindow); the wiring, the rows, the yaml and the
	/// observed-only inputs are pinned as text.
	/// </summary>
	[TestFixture]
	public class Squad02AllInAheadTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";
		const string CameoBots = "OpenRA.Mods.Cameo/Traits/BotModules/";

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
			Assert.That(start, Is.GreaterThanOrEqualTo(0), "missing: " + from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), "missing after it: " + to);
			return text.Substring(start, end - start);
		}

		// --- names ---------------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void EveryTriggerHasItsOwnName()
		{
			var triggers = Enum.GetValues(typeof(AheadTrigger)).Cast<AheadTrigger>().ToArray();
			Assert.That(triggers, Has.Length.EqualTo(9));
			Assert.That(AllInAhead.Names, Has.Length.EqualTo(triggers.Length));
			Assert.That(AllInAhead.Names.Distinct().Count(), Is.EqualTo(triggers.Length));
			foreach (var t in triggers)
			{
				Assert.That(AllInAhead.Why(t), Does.StartWith(AllInAhead.WhyPrefix));
				Assert.That(AllInAhead.IsAheadWhy(AllInAhead.Why(t)), Is.True);
			}

			Assert.That(AllInAhead.Why(AheadTrigger.Yard), Is.EqualTo("ahead_yard"));
			Assert.That(AllInAhead.Why(AheadTrigger.Super), Is.EqualTo("ahead_super"));
		}

		[Test]
		public void TheOlderInputsAreNotAheadReasons()
		{
			foreach (var why in new[] { AllInCA.WhyCap, AllInCA.WhyIdle, AllInCA.WhyWon, AllInCA.WhyStalemate, AllInCA.WhyLate })
				Assert.That(AllInAhead.IsAheadWhy(why), Is.False, why);

			Assert.That(AllInAhead.IsAheadWhy(null), Is.False);
		}

		// --- ratio, trade, battle ------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheRatioIsAPercentAndAGainAgainstNoLossIsCapped()
		{
			Assert.That(AllInAhead.RatioPct(200, 100), Is.EqualTo(200));
			Assert.That(AllInAhead.RatioPct(50, 100), Is.EqualTo(50));
			Assert.That(AllInAhead.RatioPct(5, 0), Is.EqualTo(AllInAhead.RatioCap));
			Assert.That(AllInAhead.RatioPct(0, 0), Is.EqualTo(0));
			Assert.That(AllInAhead.RatioPct(0, 10), Is.EqualTo(0));
			Assert.That(AllInAhead.RatioPct(long.MaxValue / 1000, 1), Is.EqualTo(AllInAhead.RatioCap));
		}

		[Test]
		public void TheTradeNeedsTwiceTheValueAndANoteworthyAmount()
		{
			Assert.That(AllInAhead.Trade(8000, 4000, 4000, 200), Is.True, "exactly 2x");
			Assert.That(AllInAhead.Trade(7999, 4000, 4000, 200), Is.False, "just below 2x");
			Assert.That(AllInAhead.Trade(2000, 0, 4000, 200), Is.False, "early noise is no lead");
			Assert.That(AllInAhead.Trade(4000, 0, 4000, 200), Is.True, "no loss at all");
			Assert.That(AllInAhead.Trade(9000, 1000, 4000, 0), Is.False, "a percent of 0 = off");
			Assert.That(AllInAhead.Trade(0, 0, 0, 200), Is.False, "nothing happened");
		}

		[Test]
		public void TheBattleNeedsAWindowWinAndASurvivingArmy()
		{
			Assert.That(AllInAhead.Battle(6000, 2000, 6000, 200, 5000), Is.True);
			Assert.That(AllInAhead.Battle(5999, 2000, 6000, 200, 5000), Is.False, "too little destroyed");
			Assert.That(AllInAhead.Battle(6000, 3001, 6000, 200, 5000), Is.False, "below 2x inside the window");
			Assert.That(AllInAhead.Battle(6000, 2000, 6000, 200, 1500), Is.False, "the army did not survive");
			Assert.That(AllInAhead.Battle(6000, 2000, 6000, 200, 0), Is.False, "no army");
			Assert.That(AllInAhead.Battle(6000, 2000, 6000, 0, 5000), Is.False, "a percent of 0 = off");
		}

		// --- defeat, army, structures, counter -------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void ADefeatFiresOncePerDefeatWhenTheTeamHasTheNumbers()
		{
			Assert.That(AllInAhead.Defeat(1, 0, 2, 1, 0, 0, 120), Is.True, "more living seats");
			Assert.That(AllInAhead.Defeat(1, 0, 1, 1, 1200, 1000, 120), Is.True, "an army of 120 percent");
			Assert.That(AllInAhead.Defeat(1, 0, 1, 1, 1199, 1000, 120), Is.False, "no numbers");
			Assert.That(AllInAhead.Defeat(1, 0, 1, 1, 5000, 0, 120), Is.False, "an unknown enemy army is no proof");
			Assert.That(AllInAhead.Defeat(1, 1, 3, 1, 9000, 1000, 120), Is.False, "that defeat is answered");
			Assert.That(AllInAhead.Defeat(2, 1, 3, 1, 9000, 1000, 120), Is.True, "the next defeat fires again");
			Assert.That(AllInAhead.Defeat(0, 0, 3, 1, 9000, 1000, 120), Is.False, "no defeat");
		}

		[Test]
		public void TheArmyTriggerIsOneAndAHalfTimesTheKnownEnemyArmyAndDefences()
		{
			Assert.That(AllInAhead.Army(300, 100, 100, 150), Is.True, "exactly 1.5x of 200");
			Assert.That(AllInAhead.Army(299, 100, 100, 150), Is.False);
			Assert.That(AllInAhead.Army(300, 0, 200, 150), Is.True, "defences count");
			Assert.That(AllInAhead.Army(300, 200, 0, 150), Is.True, "the army counts");
			Assert.That(AllInAhead.Army(5000, 0, 0, 150), Is.False, "a blind bot has nothing to compare");
			Assert.That(AllInAhead.Army(0, 100, 0, 150), Is.False, "no own army");
			Assert.That(AllInAhead.Army(5000, 100, 0, 0), Is.False, "a percent of 0 = off");
		}

		[Test]
		public void TheStructureTriggersNeedAMajorityOfTheLivingEnemies()
		{
			Assert.That(AllInAhead.Majority(1, 1, 50), Is.True);
			Assert.That(AllInAhead.Majority(0, 1, 50), Is.False);
			Assert.That(AllInAhead.Majority(1, 2, 50), Is.True, "one of two at 50 percent");
			Assert.That(AllInAhead.Majority(1, 3, 50), Is.False);
			Assert.That(AllInAhead.Majority(2, 3, 50), Is.True);
			Assert.That(AllInAhead.Majority(1, 0, 50), Is.False, "no living enemy known");
		}

		[Test]
		public void TheCounterNeedsAnAllyUnderAttackAQuietHomeAndAKnownThinBase()
		{
			Assert.That(AllInAhead.Counter(true, false, 3000, 2000, 5, 150), Is.True);
			Assert.That(AllInAhead.Counter(true, false, 3000, 0, 5, 150), Is.True, "no known defence");
			Assert.That(AllInAhead.Counter(false, false, 3000, 2000, 5, 150), Is.False, "no ally asks for defence");
			Assert.That(AllInAhead.Counter(true, true, 3000, 2000, 5, 150), Is.False, "the own home is threatened");
			Assert.That(AllInAhead.Counter(true, false, 3000, 2000, 0, 150), Is.False, "the enemy base is unknown");
			Assert.That(AllInAhead.Counter(true, false, 2999, 2000, 5, 150), Is.False, "not enough army for its defences");
		}

		// --- de-escalation -------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheTeamIsBehindBelowTheRatioOfOne()
		{
			Assert.That(AllInAhead.Behind(99, 100, 100), Is.True);
			Assert.That(AllInAhead.Behind(100, 100, 100), Is.False, "ratio 1.0 is not behind");
			Assert.That(AllInAhead.Behind(0, 0, 100), Is.False, "an unknown enemy army is not behind");
			Assert.That(AllInAhead.Behind(105, 100, 110), Is.True, "the resume bar is a little higher");
		}

		[Test]
		public void TheExchangeIsBehindOnlyAfterANoteworthyLoss()
		{
			Assert.That(AllInAhead.TradeBehind(1000, 3000, 2000, 100), Is.True);
			Assert.That(AllInAhead.TradeBehind(3000, 3000, 2000, 100), Is.False, "even");
			Assert.That(AllInAhead.TradeBehind(0, 1500, 2000, 100), Is.False, "too small a loss to read");
			Assert.That(AllInAhead.TradeBehind(0, 0, 0, 100), Is.False, "nothing happened");
		}

		// --- what was seen alive -------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void NothingIsDeadThatWasNeverSeen()
		{
			var ledger = new AheadLedger();
			ledger.Note(7, 0, 0, 0);
			Assert.That(ledger.YardDead(7, 0, 0), Is.False);
			Assert.That(ledger.ProductionDead(7, 0), Is.False);
			Assert.That(ledger.EconomyDead(7, 0, 0), Is.False);
			Assert.That(ledger.YardDead(99, 0, 0), Is.False, "an unknown seat");
		}

		[Test]
		public void AYardSeenAndGoneIsDeadUnlessAnMcvIsKnown()
		{
			var ledger = new AheadLedger();
			ledger.Note(7, 1, 3, 2);
			Assert.That(ledger.YardDead(7, 1, 0), Is.False, "still standing");
			ledger.Note(7, 0, 0, 0);
			Assert.That(ledger.YardDead(7, 0, 0), Is.True);
			Assert.That(ledger.YardDead(7, 0, 1), Is.False, "an MCV can rebuild it");
			Assert.That(ledger.ProductionDead(7, 0), Is.True);
			Assert.That(ledger.ProductionDead(7, 1), Is.False);
		}

		[Test]
		public void TheEconomyIsDeadWhenNeitherRefineryNorHarvesterIsKnown()
		{
			var ledger = new AheadLedger();
			ledger.Note(3, 1, 1, 1);
			Assert.That(ledger.EconomyDead(3, 0, 2), Is.False, "a harvester is left");
			Assert.That(ledger.EconomyDead(3, 1, 0), Is.False, "a refinery is left");
			Assert.That(ledger.EconomyDead(3, 0, 0), Is.True);
		}

		[Test]
		public void TheLedgerKeepsTheHighestAndEverySeatSeparately()
		{
			var ledger = new AheadLedger();
			ledger.Note(1, 2, 0, 0);
			ledger.Note(1, 1, 0, 0);
			ledger.Note(2, 0, 0, 0);
			Assert.That(ledger.YardDead(1, 0, 0), Is.True, "a yard was seen at seat 1");
			Assert.That(ledger.YardDead(2, 0, 0), Is.False, "seat 2 never showed one");
		}

		// --- the window ----------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheWindowReadsTheGainOfTheLastTicks()
		{
			var w = new AheadWindow();
			Assert.That(w.Delta(0, 1500), Is.EqualTo((0L, 0L)));
			w.Add(0, 0, 0, 3000);
			w.Add(500, 1000, 200, 3000);
			w.Add(1000, 3000, 400, 3000);
			w.Add(1500, 7000, 900, 3000);
			Assert.That(w.Delta(1500, 1000), Is.EqualTo((6000L, 700L)), "from tick 500");
			Assert.That(w.Delta(1500, 9999), Is.EqualTo((7000L, 900L)), "history shorter than the window: since the first sample");
		}

		[Test]
		public void TheWindowKeepsABaselineAndDropsTheOldSamples()
		{
			var w = new AheadWindow();
			for (var t = 0; t <= 10000; t += 250)
				w.Add(t, t, t / 2, 3000);

			Assert.That(w.Delta(10000, 3000), Is.EqualTo((3000L, 1500L)));
			Assert.That(w.Delta(10000, 1000), Is.EqualTo((1000L, 500L)));
		}

		[Test]
		public void AShrinkingCounterNeverGivesANegativeGain()
		{
			var w = new AheadWindow();
			w.Add(0, 5000, 5000, 3000);
			w.Add(250, 4000, 4500, 3000);
			Assert.That(w.Delta(250, 1000), Is.EqualTo((0L, 0L)), "an ally left, the sum dropped");
		}

		[Test]
		public void ASecondSampleOfTheSameTickReplacesTheFirst()
		{
			var w = new AheadWindow();
			w.Add(0, 0, 0, 3000);
			w.Add(100, 10, 10, 3000);
			w.Add(100, 20, 15, 3000);
			Assert.That(w.Delta(100, 500), Is.EqualTo((20L, 15L)));
		}

		[Test]
		public void ThePureRulesHaveNoRandomnessAndNoWorldAccess()
		{
			var text = RepoFile(Bots + "Squads/AllInAhead.cs");
			foreach (var banned in new[] { "Random", "World", "Actor ", "Player ", "DateTime", "Stopwatch" })
				Assert.That(text, Does.Not.Contain(banned), banned);
		}

		// --- rows and yaml -------------------------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void EveryTriggerHasItsOwnRowAndYamlSwitchAndThresholds()
		{
			var rows = RepoFile(Bots + "BotModuleLogic/AblationIds.AllInAhead.cs");
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.AllInAhead.cs");
			foreach (var id in new[]
			{
				"sq_ahead", "sq_ahead_yard", "sq_ahead_production", "sq_ahead_economy", "sq_ahead_defeat", "sq_ahead_trade", "sq_ahead_battle", "sq_ahead_army", "sq_ahead_counter",
				"sq_ahead_super", "sq_ahead_deescalate"
			})
				Assert.That(rows, Does.Contain("= \"" + id + "\";"), id);

			foreach (var flag in new[]
			{
				"ArmyAheadYardEnabled", "ArmyAheadProductionEnabled", "ArmyAheadEconomyEnabled", "ArmyAheadDefeatEnabled", "ArmyAheadTradeEnabled", "ArmyAheadBattleEnabled",
				"ArmyAheadArmyEnabled", "ArmyAheadCounterEnabled", "ArmyAheadSuperEnabled", "ArmyAheadDeescalateEnabled"
			})
				Assert.That(info, Does.Contain("public readonly bool " + flag + " = true;"), flag);

			// V08F-FIX-02: the family switch defaults off in code; the six personality blocks with the one launcher set it on.
			Assert.That(info, Does.Contain("public readonly bool ArmyAheadEnabled = false;"));

			foreach (var number in new[]
			{
				"ArmyAheadStartTick = 6000", "ArmyAheadScanTicks = 250", "ArmyAheadMajorityPercent = 50", "ArmyAheadDefeatArmyPercent = 120", "ArmyAheadTradePercent = 200",
				"ArmyAheadTradeMinKills = 4000", "ArmyAheadBattleWindowTicks = 1500", "ArmyAheadBattlePercent = 200", "ArmyAheadBattleMinKills = 6000", "ArmyAheadArmyPercent = 150",
				"ArmyAheadCounterPercent = 150", "ArmyAheadSuperMinChargeTicks = 4500", "ArmyAheadDeescalatePercent = 100", "ArmyAheadDeescalateWindowTicks = 3000",
				"ArmyAheadDeescalateMinLoss = 2000", "ArmyAheadResumePercent = 110"
			})
				Assert.That(info, Does.Contain("public readonly int " + number + ";"), number);

			Assert.That(info.Split(new[] { "public readonly" }, StringSplitOptions.None).Length - 1, Is.EqualTo(info.Split(new[] { "[Desc(" }, StringSplitOptions.None).Length - 1),
				"every field has a Desc");
		}

		[Test]
		public void TheTriggerRowsFollowTheEnumOrderAndTheSwitchesAreRead()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInAhead.cs");
			var rows = Between(wiring, "static readonly string[] AheadRows =", "const int AheadFamilySlot = 9;");
			var order = new[]
			{
				"AblationIds.SqAheadYard", "AblationIds.SqAheadProduction", "AblationIds.SqAheadEconomy", "AblationIds.SqAheadDefeat", "AblationIds.SqAheadTrade",
				"AblationIds.SqAheadBattle", "AblationIds.SqAheadArmy", "AblationIds.SqAheadCounter", "AblationIds.SqAheadSuper", "AblationIds.SqAhead,", "AblationIds.SqAheadDeescalate"
			};
			var last = -1;
			foreach (var id in order)
			{
				var at = rows.IndexOf(id, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThan(last), id + " (the slot order is the AheadTrigger order, then the family and the de-escalation)");
				last = at;
			}

			Assert.That(wiring, Does.Contain("const int AheadDeescalateSlot = 10;"));
			Assert.That(wiring, Does.Contain("readonly AblationLatch[] ablateAhead = new AblationLatch[11];"));
			var on = Between(wiring, "bool AheadTriggerOn(AheadTrigger trigger)", "// The master module's observed enemy facts");
			foreach (var flag in new[]
			{
				"Info.ArmyAheadYardEnabled", "Info.ArmyAheadProductionEnabled", "Info.ArmyAheadEconomyEnabled", "Info.ArmyAheadDefeatEnabled", "Info.ArmyAheadTradeEnabled",
				"Info.ArmyAheadBattleEnabled", "Info.ArmyAheadArmyEnabled", "Info.ArmyAheadCounterEnabled", "Info.ArmyAheadSuperEnabled"
			})
				Assert.That(on, Does.Contain(flag), flag);

			Assert.That(wiring, Does.Contain("internal bool ArmyAheadOn => ArmyLaunchArbiterOn && Info.ArmyAheadEnabled && AheadRowOn(AheadFamilySlot);"));
			Assert.That(wiring, Does.Contain("!AheadRowOn(AheadDeescalateSlot)"));
			Assert.That(wiring, Does.Contain("!Info.ArmyAheadDeescalateEnabled"));
		}

		// --- wiring: where it hooks in and what it reads -------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheAheadInputComesAfterTheNamedOnesBeforeTheLateTickAndTheDeescalationRunsFirst()
		{
			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			var why = Between(v07, "string TeamLeadAllInWhy(", "// Fog-honest, like TeamLeadKnownBuildings");
			var off = why.IndexOf("if (!TeamLeadAllInOn)", StringComparison.Ordinal);
			var de = why.IndexOf("AheadDeescalate(now);", StringComparison.Ordinal);
			var early = why.IndexOf("if (TeamLeadOpenOrder() is { } open && (open.AllIn || open.GatherOnly))", StringComparison.Ordinal);
			var ahead = why.IndexOf("if (gate.Open && AheadWhy(now) is { } ahead)\n\t\t\t\treturn AllInReturn(now, ahead);", StringComparison.Ordinal);
			var late = why.IndexOf("if (gate.Open && ArmyAllInLateOn(now))\n\t\t\t\treturn AllInReturn(now, AllInCA.WhyLate);", StringComparison.Ordinal);
			var named = why.IndexOf("return AllInReturn(now, AllInCA.Why(gate.AtCap, gate.Huge, gate.Won)", StringComparison.Ordinal);
			Assert.That(off, Is.GreaterThanOrEqualTo(0));
			Assert.That(de, Is.GreaterThan(off), "after the switch");
			Assert.That(early, Is.GreaterThan(de), "the de-escalation runs before the early return that skips an all-in order");
			Assert.That(ahead, Is.GreaterThan(early));
			Assert.That(late, Is.GreaterThan(ahead), "the late tick is only the fallback");
			Assert.That(named, Is.GreaterThan(late));
			Assert.That(why.IndexOf("var won = false;", StringComparison.Ordinal), Is.LessThan(ahead), "the cap, idle and nearly-won results come first");
			Assert.That(why, Does.Contain("?? (ArmyLaunchArbiterOn && AllInCA.StalemateSignal(now, team.StalemateSignalTick, Info.ArmyStalemateSignalTicks) ? AllInCA.WhyStalemate : null));"),
				"the Q4 stalemate input is unchanged");
		}

		[Test]
		public void TheAheadTriggersAreSilentBeforeTheStartTickAndWhileTheTeamIsBehind()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInAhead.cs");
			var fire = Between(wiring, "string AheadWhy(int now)", "void AheadDeescalate(int now)");
			Assert.That(fire, Does.Contain("if (!ArmyAheadOn || now < Info.ArmyAheadStartTick)"));
			Assert.That(fire, Does.Contain("AheadBehind(v, Info.ArmyAheadResumePercent)"));
			Assert.That(fire, Does.Contain("if (!on(trigger))"));
			Assert.That(fire, Does.Contain("AheadPick(v, Info, AheadTriggerOn, aheadDefeatsHandled)"), "the enables are handed over as one delegate");
			Assert.That(fire.IndexOf("AheadScan(now);", StringComparison.Ordinal), Is.LessThan(fire.IndexOf("AheadPick(v, Info, AheadTriggerOn, aheadDefeatsHandled)", StringComparison.Ordinal)));
			Assert.That(fire, Does.Contain("aheadDefeatsHandled = v.Defeated;"), "a defeat is answered once");
		}

		[Test]
		public void TheDeescalationEndsOnlyAnAheadOrderAtTheRatioOfOne()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInAhead.cs");
			var de = Between(wiring, "void AheadDeescalate(int now)", "// DEV record");
			Assert.That(de, Does.Contain("TeamLeadOpenOrder() is not { AllIn: true } open"));
			Assert.That(de, Does.Contain("AllInAhead.IsAheadWhy(open.AllInWhy)"));
			Assert.That(de, Does.Contain("AheadBehind(v, Info.ArmyAheadDeescalatePercent)"));
			Assert.That(de, Does.Contain("open.ClearAllIn();"));
			Assert.That(de.IndexOf("AheadRecord(\"all_in_ahead_end\"", StringComparison.Ordinal), Is.LessThan(de.IndexOf("open.ClearAllIn();", StringComparison.Ordinal)),
				"the record reads the open order before it is cleared");
			var order = RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs");
			Assert.That(order, Does.Contain("public void MarkAllIn(string why) => AllInWhy ??= why;"), "marking is unchanged");
			Assert.That(order, Does.Contain("public void ClearAllIn() => AllInWhy = null;"));
		}

		[Test]
		public void TheDevRowsCarryTheTriggerTheRatioAndTheTick()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInAhead.cs");
			var record = Between(wiring, "void AheadRecord(string evt, string trigger, long ratio, int now)", "\n\t}\n}");
			Assert.That(record, Does.Contain("if (DiagnosticWriter == null)"));
			Assert.That(record, Does.Contain("\"trigger=\" + trigger + \" ratio=\" + ratio + \" tick=\" + now"));
			Assert.That(wiring, Does.Contain("AheadRecord(\"all_in_ahead\", AllInAhead.Why(trigger), pick.Ratio, now);"));
			Assert.That(wiring, Does.Contain("AheadRecord(\"all_in_ahead_end\", open.AllInWhy, AheadObservedRatio(v), now);"));
		}

		[Test]
		public void TheWiringReadsOnlyObservedInformation()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInAhead.cs");
			foreach (var banned in new[] { "FindActors", "FindClosestEnemy", "FrozenActorLayer", "World.Actors", "ActorsWithTrait", "ActorsHavingTrait", "CanBeViewedByPlayer", "Random", "Experience" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);

			var scan = Between(wiring, "void AheadScan(int now)", "// An own support power is ready");
			Assert.That(scan, Does.Contain("p.NonCombatant"));
			Assert.That(scan, Does.Contain("p.PlayerActor.TraitOrDefault<PlayerStatistics>()"), "the own side's counters");
			Assert.That(scan, Does.Contain("p.WinState == WinState.Lost"), "the scoreboard");
			Assert.That(scan, Does.Contain("AheadProvider?.AheadEnemies"), "the enemy sightings of the master module");
			Assert.That(scan, Does.Contain("TeamBlackboard.Collect(Player).DefendRequests > 0"));
			Assert.That(scan, Does.Contain("urgency.StalemateOwnArmy + urgency.StalemateAlliedArmy"));
			Assert.That(wiring, Does.Contain("power.Info.ChargeInterval >= Info.ArmyAheadSuperMinChargeTicks"));
		}

		[Test]
		public void TheLateTickStaysAsTheFallbackAtTwentyFourThousand()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.Pipeline02.cs");
			Assert.That(info, Does.Contain("public readonly int ArmyAllInLateTick = 24000;"));
			var late = RepoFile(Bots + "SquadManagerBotModuleCA.AllInFold.cs");
			Assert.That(late, Does.Contain("internal bool ArmyAllInLateOn(int now) => ArmyLaunchArbiterOn"));
			Assert.That(late, Does.Contain("&& AllInCA.StalemateSignal(now, teamLeadTeam?.StalemateSignalTick ?? -1, Info.ArmyStalemateSignalTicks)"), "never on the tick alone (Opus ruling): the Q4 stagnation signal must hold too");
			Assert.That(info, Does.Contain("only while the Q4 stagnation signal"));
		}

		// --- the master module's observed facts ----------------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheMasterModuleOffersOnlyFogHonestFactsOfTheLivingEnemies()
		{
			var situation = RepoFile(CameoBots + "BotSituation.cs");
			Assert.That(situation, Does.Contain("IBotStalemateUrgencyProvider, IBotMobileStaticValues, IBotAheadFactsProvider"));
			Assert.That(situation, Does.Contain("bool IBotAheadFactsProvider.AheadFactsEnabled => !IsTraitDisabled && Info.UseFoggedObservation && player.Shroud != null && Situation?.Enemies != null;"));
			var build = Between(situation, "List<AheadEnemyFacts> BuildAheadFacts()", "internal static List<BotMission> DeriveMissions(");
			Assert.That(build, Does.Contain("profile.Player.WinState == WinState.Undefined"));
			Assert.That(build, Does.Contain("aheadFacts.Sort((a, b) => a.Seat.CompareTo(b.Seat));"), "total order, no dictionary order");
			Assert.That(build, Does.Contain("ReferenceEquals(aheadFactsFrom, situation)"), "rebuilt once per snapshot");
			Assert.That(situation, Does.Contain("if (actor.Mcv)\n\t\t\t\t\tprofile.Mcvs++;"));
			Assert.That(situation, Does.Contain("if (actor.BaseBuilding)\n\t\t\t\t\t\tprofile.BaseBuildings++;"));
		}

		[Test]
		public void AnMcvIsAUnitThatTransformsIntoABaseBuildingActor()
		{
			var fog = RepoFile(CameoBots + "BotFogMemory.cs");
			Assert.That(fog, Does.Contain("Mcv = t.Mcv,"), "the cached template copies it");
			Assert.That(fog, Does.Not.Contain("HasTraitInfo<MobileInfo>() && actorInfo.HasTraitInfo<TransformsInfo>()"), "no heuristic");
			var type = Between(fog, "bool IsMcvType(ActorInfo actorInfo)", "// Classify with the ActorInfo-static part");
			Assert.That(type, Does.Contain("actorInfo.TraitInfoOrDefault<TransformsInfo>()"));
			Assert.That(type, Does.Contain("viewer.World.Map.Rules.Actors.TryGetValue(transforms.IntoActor, out var into) && into.HasTraitInfo<BaseBuildingInfo>();"), "the target of the transform carries the yard trait");
			Assert.That(type, Does.Contain("mcvTypes[actorInfo] = mcv;"), "resolved once per type");
			var classify = Between(fog, "internal ObservedActor ClassifyObserved(", "internal static ObservedActor Classify(");
			Assert.That(classify, Does.Contain("plain.Mcv = IsMcvType(actorInfo);"), "the plain path sets it");
			Assert.That(classify, Does.Contain("template.Mcv = IsMcvType(actorInfo);"), "the cached template sets it");
		}
	}
}
