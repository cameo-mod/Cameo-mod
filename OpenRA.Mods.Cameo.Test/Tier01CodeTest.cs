#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// TIER-01 code (Workhorse; Opus TIER_TUNING_DECISIONS_01 D1, HVCAP R1, erratum CF-L1).
	[TestFixture]
	public sealed class Tier01CodeTest
	{
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static BotFightThresholds Rows(string body) => BotFightThresholds.Parse(MiniYaml.FromString("BotFightLearning:\n\tSchema: 1\n" + body, "fight"));

		static IBotFightThresholds Loaded(string body)
		{
			var module = new FightThresholdsBotModule(null, new FightThresholdsBotModuleInfo());
			typeof(FightThresholdsBotModule).GetField("thresholds", Any).SetValue(module, Rows(body));
			typeof(FightThresholdsBotModule).GetField("loaded", Any).SetValue(module, true);
			return module;
		}

		// ---- D1: effective retreat = base x learned multiplier / 100, clamped 0..100 ----

		[Test]
		public void TheEffectiveRetreatIsTheBaseTimesTheMultiplierClampedToTheRetreatRange()
		{
			var info = new FightThresholdsBotModuleInfo();
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(50, 100, info), Is.EqualTo(50), "1.00 keeps the base");
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(50, 150, info), Is.EqualTo(75));
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(25, 140, info), Is.EqualTo(35));
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(50, 0, info), Is.EqualTo(0), "the learned minimum is 0");
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(50, -10, info), Is.EqualTo(0));
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(40, 300, info), Is.EqualTo(80), "the multiplier is clamped to 200 first");
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(80, 200, info), Is.EqualTo(100), "the result is clamped to 100");
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(0, 200, info), Is.EqualTo(0), "a tier of 0 never retreats");
		}

		[Test]
		public void TheRangesAreZeroToOneHundredAndZeroToTwoHundred()
		{
			var info = new FightThresholdsBotModuleInfo();
			Assert.That((info.MinimumRetreatRatioPct, info.MaximumRetreatRatioPct), Is.EqualTo((0, 100)));
			Assert.That((info.MinimumRetreatMultPct, info.MaximumRetreatMultPct), Is.EqualTo((0, 200)));
		}

		[Test]
		public void ALearnedRowScalesTheCallersBaseAndNoRowReturnsTheBase()
		{
			var any = Loaded("\tEvidence@any: 150\n\tRetreatMultPct@any: 150\n");
			Assert.That(any.RetreatRatioPct("td_gdi", "td_nod", 50), Is.EqualTo(75), "squad manager base");
			Assert.That(any.RetreatRatioPct("td_gdi", "td_nod", 999), Is.EqualTo(100), "clamped");

			var gdiOnly = Loaded("\tEvidence@td_gdi: 150\n\tRetreatMultPct@td_gdi: 0\n");
			Assert.That(gdiOnly.RetreatRatioPct("td_gdi", "", 60), Is.EqualTo(0));
			Assert.That(gdiOnly.RetreatRatioPct("ra1_allies", "", 60), Is.EqualTo(60), "no row: the base, unchanged");
			Assert.That(gdiOnly.RetreatRatioPct("ra1_allies", "", 130), Is.EqualTo(130), "no row: the caller clamps");
		}

		[Test]
		public void AnAbsoluteRowOfTheFormerFormatIsNotReadAsAMultiplier()
		{
			var rows = Rows("\tEvidence@any: 200\n\tRetreatRatioPct@any: 42\n");
			Assert.That(rows.TryRetreatMultPct("td_gdi", "td_nod", out _), Is.False, "fails closed to the tier value");
		}

		[Test]
		public void TheSquadManagerScalesTierPlusDeltaAndClampsEveryPath()
		{
			var src = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(src, Does.Contain("var basePct = RetreatBasePct(botLimits?.Info.RetreatRatioPct ?? Info.DefaultRetreatRatioPct, InMatchAdaptation?.RetreatRatioDeltaPct ?? 0);"));
			Assert.That(src, Does.Contain("return Math.Clamp(provider == null ? basePct : provider.RetreatRatioPct(Player.Faction.InternalName, EnemyFaction(enemies), basePct), 0, 100);"));
			Assert.That(src, Does.Not.Contain("Math.Max(0, (botLimits"), "the former unclamped fallback is gone");
		}

		[Test]
		public void ANoobTierOfZeroNeverRetreatsWhateverTheInMatchDelta()
		{
			// User ruling (Opus, Sonnet LOW-1 on b3adea44): noob NEVER retreats, so the in-match delta cannot lift a tier of 0.
			Assert.That(SquadManagerBotModuleCA.RetreatBasePct(0, 20), Is.EqualTo(0));
			Assert.That(SquadManagerBotModuleCA.RetreatBasePct(0, -20), Is.EqualTo(0));
			Assert.That(SquadManagerBotModuleCA.RetreatBasePct(-5, 20), Is.EqualTo(0));
			Assert.That(SquadManagerBotModuleCA.RetreatBasePct(25, 20), Is.EqualTo(45), "every other tier takes the delta");
			Assert.That(SquadManagerBotModuleCA.RetreatBasePct(25, -30), Is.EqualTo(-5), "the caller clamps to 0");
			Assert.That(FightThresholdsBotModule.EffectiveRetreatPct(SquadManagerBotModuleCA.RetreatBasePct(0, 20), 200, new FightThresholdsBotModuleInfo()),
				Is.EqualTo(0), "and no learned multiplier lifts it");
		}

		[Test]
		public void CombatVetoAppliesTheSameRuleToItsOwnEngageBar()
		{
			var src = Source("mods/Cameo/Traits/BotModules/CombatVetoBotModule.cs");
			Assert.That(src, Does.Contain("var engage = provider.RetreatRatioPct(player.Faction.InternalName, enemy, Info.VetoEngageRatioPct);"));
			Assert.That(src, Does.Contain("var abort = Info.VetoAbortRatioPct * engage / System.Math.Max(1, Info.VetoEngageRatioPct);"));
			var veto = new CombatVetoBotModuleInfo();
			var any = Loaded("\tEvidence@any: 150\n\tRetreatMultPct@any: 100\n");
			Assert.That(any.RetreatRatioPct("td_gdi", "td_nod", veto.VetoEngageRatioPct), Is.EqualTo(veto.VetoEngageRatioPct), "a row of 100 keeps the veto bar");
		}

		// ---- HVCAP R1: the first harvester ----

		[Test]
		public void TheValueCapAlwaysAdmitsTheFirstHarvester()
		{
			Assert.That(HarvesterValueCap.MayProduce(0, 8, 8, 100, 0, 250), Is.True, "a cap below one harvester's cost still admits the first");
			Assert.That(HarvesterValueCap.MayProduce(1, 8, 8, 100, 250, 250), Is.False, "the second is capped");
			Assert.That(HarvesterValueCap.MayProduce(0, 8, 0, 100, 0, 250), Is.True, "USER RULING: the value cap replaces the count gate");
			Assert.That(HarvesterValueCap.MayProduce(0, 0, int.MaxValue, 1, 0, 250), Is.True);
		}

		// USER RULING (2026-11-08): the value cap replaces the count limit (HarvesterLimit and MaxHarvesters) when it is on.

		static int BuildUntilRefused(int cost, int cap, int limit, int maxHarvesters)
		{
			var n = 0;
			while (n < 1000 && HarvesterValueCap.MayProduce(n, limit, maxHarvesters, cap, n * cost, cost))
				n++;
			return n;
		}

		[Test]
		public void TwoHundredFiftyCreditHarvestersWithACapOf6000BuildTwentyFour()
		{
			Assert.That(BuildUntilRefused(250, 6000, 0, 8), Is.EqualTo(24), "more than MaxHarvesters 8: the count gates do not apply");
			Assert.That(BuildUntilRefused(250, 6000, 3, 8), Is.EqualTo(24), "HarvesterLimit is ignored when the value cap is on");
		}

		[Test]
		public void OneThousandCreditHarvestersWithACapOf6000BuildSix()
		{
			Assert.That(BuildUntilRefused(1000, 6000, 0, 8), Is.EqualTo(6));
		}

		[Test]
		public void ZeroLimitMeansNoCountLimitAndMaxHarvestersStillAppliesWhenCapIsOff()
		{
			Assert.That(HarvesterValueCap.MayProduce(3, 0, 8, 0, 0, 0), Is.True, "limit 0 = no count limit");
			Assert.That(HarvesterValueCap.MayProduce(8, 0, 8, 0, 0, 0), Is.False, "MaxHarvesters applies when the cap is off");
		}
	}
}
