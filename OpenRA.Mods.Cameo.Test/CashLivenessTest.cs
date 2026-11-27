#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CASH-LIVENESS-01 (user D6; Opus retarget 11-27 after the ABL_01 finding: the G1 idle excess was the BotGlobalUnitBudget unit cap, not cash).
	/// Three rows, each off in code and not armed in yaml: cl_stale_release, cl_cap_spend, cl_cap_allin (CashLiveness.cs).
	/// </summary>
	[TestFixture]
	public class CashLivenessTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";
		const string Logic = Bots + "BotModuleLogic/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Between(string text, string start, string end)
		{
			var a = text.IndexOf(start, StringComparison.Ordinal);
			Assert.That(a, Is.GreaterThanOrEqualTo(0), start);
			var b = text.IndexOf(end, a + start.Length, StringComparison.Ordinal);
			Assert.That(b, Is.GreaterThan(a), end);
			return text[a..b];
		}

		// --- the pure rules ------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheAgeStartsAtTheFirstHoldingTickAndATickWithoutTheHoldResetsIt()
		{
			var since = -1;
			Assert.That(CashLiveness.Age(ref since, true, 100), Is.EqualTo(0));
			Assert.That(since, Is.EqualTo(100));
			Assert.That(CashLiveness.Age(ref since, true, 400), Is.EqualTo(300));
			Assert.That(CashLiveness.Age(ref since, false, 401), Is.EqualTo(0));
			Assert.That(since, Is.EqualTo(-1));
			Assert.That(CashLiveness.Age(ref since, true, 500), Is.EqualTo(0), "a new hold starts a new clock");

			since = 900;
			Assert.That(CashLiveness.Age(ref since, true, 500), Is.EqualTo(0), "a start after now (a reloaded tick) restarts the clock");
			Assert.That(since, Is.EqualTo(500));
		}

		[Test]
		public void AStaleHoldIsReleasedOnlyAfterTheHoldAgeWithTheSpareCashBanked()
		{
			var since = -1;
			Assert.That(CashLiveness.Released(ref since, true, 0, 750, 5000, 3000), Is.False);
			Assert.That(CashLiveness.Released(ref since, true, 749, 750, 5000, 3000), Is.False, "younger than the hold age");
			Assert.That(CashLiveness.Released(ref since, true, 750, 750, 2999, 3000), Is.False, "too little cash banked");
			Assert.That(CashLiveness.Released(ref since, true, 750, 750, 3000, 3000), Is.True);
			Assert.That(CashLiveness.Released(ref since, false, 751, 750, 9000, 3000), Is.False, "not holding: nothing to release");
			Assert.That(since, Is.EqualTo(-1), "and the clock was reset");
			Assert.That(CashLiveness.Released(ref since, true, 752, 750, 9000, 3000), Is.False, "the next hold starts its own clock");
		}

		[Test]
		public void TheThresholdsAreClamped()
		{
			var since = -1;
			CashLiveness.Released(ref since, true, 0, 0, 0, 0);
			Assert.That(CashLiveness.Released(ref since, true, CashLiveness.HoldTicksMin - 1, 0, 0, -5), Is.False, "a hold age under the floor is the floor");
			Assert.That(CashLiveness.Released(ref since, true, CashLiveness.HoldTicksMin, 0, 0, -5), Is.True, "a negative spare is 0");

			since = -1;
			CashLiveness.Released(ref since, true, 0, int.MaxValue, long.MaxValue, int.MaxValue);
			Assert.That(CashLiveness.Released(ref since, true, CashLiveness.HoldTicksMax, int.MaxValue, CashLiveness.SpareMax, int.MaxValue), Is.True,
				"the hold age and the spare are capped");

			Assert.That(CashLiveness.CapSpare(true, CashLiveness.SpareMax, int.MaxValue), Is.True);
			Assert.That(CashLiveness.CapSpare(true, 999, 1000), Is.False);
			Assert.That(CashLiveness.CapSpare(true, 1000, 1000), Is.True);
			Assert.That(CashLiveness.CapSpare(false, 50000, 1000), Is.False, "no cap: no cap spend");

			since = -1;
			CashLiveness.Sustained(ref since, true, 0, 1);
			Assert.That(CashLiveness.Sustained(ref since, true, CashLiveness.CapAllInTicksMin - 1, 1), Is.False);
			Assert.That(CashLiveness.Sustained(ref since, true, CashLiveness.CapAllInTicksMin, 1), Is.True);
			since = -1;
			CashLiveness.Sustained(ref since, true, 0, int.MaxValue);
			Assert.That(CashLiveness.Sustained(ref since, true, CashLiveness.CapAllInTicksMax, int.MaxValue), Is.True);
		}

		[Test]
		public void TheCapAllInNeedsTheCapHeldWithoutABreak()
		{
			var since = -1;
			Assert.That(CashLiveness.Sustained(ref since, true, 0, 1500), Is.False);
			Assert.That(CashLiveness.Sustained(ref since, true, 1499, 1500), Is.False);
			Assert.That(CashLiveness.Sustained(ref since, false, 1500, 1500), Is.False, "a sample without the cap");
			Assert.That(CashLiveness.Sustained(ref since, true, 1600, 1500), Is.False, "restarts the clock");
			Assert.That(CashLiveness.Sustained(ref since, true, 3100, 1500), Is.True);
			Assert.That(CashLiveness.WhyBudgetCap, Is.EqualTo("budget_cap"));
		}

		// --- the rows, off in code, not armed -----------------------------------------------------------------------------------------------

		[Test]
		public void TheThreeRowsAreRegisteredAblationIds()
		{
			Assert.That(AblationIds.ClStaleRelease, Is.EqualTo("cl_stale_release"));
			Assert.That(AblationIds.ClCapSpend, Is.EqualTo("cl_cap_spend"));
			Assert.That(AblationIds.ClCapAllIn, Is.EqualTo("cl_cap_allin"));
			var ids = AblationSpec.RegistryIds();
			Assert.That(ids, Does.Contain("cl_stale_release").And.Contain("cl_cap_spend").And.Contain("cl_cap_allin"));
			Assert.That(AblationSpec.TryParse("cl_stale_release,cl_cap_spend,cl_cap_allin", ids, out _, out _), Is.True);
		}

		[Test]
		public void EverySwitchIsOffInCodeAndNoYamlArmsIt()
		{
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.cs"), Does.Contain("public readonly bool CashLivenessStaleRelease = false;"));
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.cs"), Does.Contain("public readonly int StaleHoldMaxTicks = 750;"));
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.cs"), Does.Contain("public readonly int StaleHoldSpareCash = 3000;"));
			Assert.That(RepoFile(Bots + "BaseBuilderBotModuleCA.cs"), Does.Contain("public readonly bool CashLivenessCapSpend = false;"));
			Assert.That(RepoFile(Bots + "BaseBuilderBotModuleCA.cs"), Does.Contain("public readonly int CapSpareCash = 1000;"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.CashLiveness.cs"), Does.Contain("public readonly bool CashLivenessCapAllInEnabled = false;"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.CashLiveness.cs"), Does.Contain("public readonly int CapAllInTicks = 1500;"));

			Assert.That(new UnitBuilderBotModuleCAInfo().CashLivenessStaleRelease, Is.False);
			Assert.That(new BaseBuilderBotModuleCAInfo().CashLivenessCapSpend, Is.False);
			Assert.That(new SquadManagerBotModuleCAInfo().CashLivenessCapAllInEnabled, Is.False);

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			foreach (var key in new[] { "CashLiveness", "StaleHoldMaxTicks", "StaleHoldSpareCash", "CapSpareCash", "CapAllInTicks" })
				Assert.That(yaml, Does.Not.Contain(key), key + ": not armed or tuned in yaml (the A/B arms it)");
		}

		// --- wiring --------------------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheStaleReleaseNeverReleasesACapAndBothClocksRunEveryVisit()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.CashLiveness.cs");
			Assert.That(part, Does.Contain("bool StaleReleaseOn => Info.CashLivenessStaleRelease && !ablateStaleRelease.Off(player, AblationIds.ClStaleRelease);"));
			Assert.That(part, Does.Contain("var capInHold = held && !FairService && requestPause.Any(rp => !ReferenceEquals(rp, baseBuilderPause) && rp is IBotItemAwareUnitCap"));
			Assert.That(part, Does.Contain("CashLiveness.Released(ref voteHoldSince, held && !capInHold, world.WorldTick,"), "a cap in the hold stops the clock");
			Assert.That(part, Does.Contain("CashLiveness.Released(ref refineryHoldSince, held, world.WorldTick,"));
			Assert.That(Count(part, "return released && StaleReleaseOn;"), Is.EqualTo(2), "the clock runs before the switch is read");

			var ub = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var gate = Between(ub, "var held = requestPause.Any(", "var clock = BotVisitClock.Of(bot);");
			Assert.That(gate, Does.Contain("if (StaleVoteReleased(held, baseBuilderPause))\n\t\t\t\theld = false;\n\n\t\t\tif (held)\n\t\t\t\treturn;"));
			Assert.That(gate, Does.Contain("&& !(FairService && rp is IBotItemAwareUnitCap));"), "the fair-service cap exclusion is unchanged");
			Assert.That(ub, Does.Contain("var refineryHeld = baseBuilderPause != null && baseBuilderPause.PauseUnitProduction;\n\t\t\tif (StaleRefineryReleased(refineryHeld))\n\t\t\t\trefineryHeld = false;\n\n\t\t\tif (refineryHeld)\n"));
			Assert.That(ub, Does.Contain("var paused = requestPause.Any(rp => rp.IsTraitEnabled() && rp.PauseUnitProduction && rp is not IBotItemAwareUnitCap);"),
				"the upgrade path still ignores the unit cap");
		}

		[Test]
		public void TheCapSpendZeroesTheUnitReserveAndWaivesOnlyTheDefenceNeedGate()
		{
			var bb = RepoFile(Bots + "BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool CapSpendArmed => Info.CashLivenessCapSpend && switchesActive && !ablateCapSpend.Off(player, AblationIds.ClCapSpend);"));
			var spare = Between(bb, "public bool UnitCapSpare()", "AblationLatch ablatePwInflightEmergency");
			Assert.That(spare, Does.Contain("if (!CapSpendArmed)\n\t\t\t\treturn false;"), "off: false before any work");
			Assert.That(spare, Does.Contain(".Where(v => v is IBotItemAwareUnitCap)"), "only the count caps");

			var p1 = RepoFile(Logic + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("ProductionCashReserve = botLimits != null ? botLimits.Info.ProductionCashReserve : -1,"));
			Assert.That(p1, Does.Contain("if (baseBuilder.UnitCapSpare())\n\t\t\t\ts.ProductionCashReserve = 0;"));

			var d = RepoFile(Logic + "BaseBuilderQueueManagerCA.Pipeline01Defence.cs");
			var caps = Between(d, "bool PipelineExtraDefenceAdmits(", "static bool IsArmedDefenceInfo(");
			var need = caps.IndexOf("if (DefenceNeedOn && BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(info), DefenceThreatSeen()) && !baseBuilder.UnitCapSpare())", StringComparison.Ordinal);
			var cap = caps.IndexOf("if (PipelineDefenceCapHolds())", StringComparison.Ordinal);
			var ratio = caps.IndexOf("return !MobileStaticRatioCA.Holds(RatioGate(info, out _))", StringComparison.Ordinal);
			Assert.That(need, Is.GreaterThan(0));
			Assert.That(cap, Is.GreaterThan(need), "the AF2 defence cap still holds after the waived need gate");
			Assert.That(ratio, Is.GreaterThan(cap), "and the per-personality army/defence ratio");
			Assert.That(Count(caps, "UnitCapSpare()"), Is.EqualTo(1), "only the need gate is waived");
		}

		[Test]
		public void TheCapAllInIsSampledBeforeTheEarlyReturnAndFiresAfterTheAheadTriggersBeforeTheLateFallback()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.CashLiveness.cs");
			Assert.That(part, Does.Contain("bool CapAllInOn => TeamLeadAllInOn && Info.CashLivenessCapAllInEnabled && !ablateCapAllIn.Off(Player, AblationIds.ClCapAllIn);"));
			Assert.That(part, Does.Contain("p.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().Where(v => v is IBotItemAwareUnitCap)"));
			Assert.That(part, Does.Contain("Player.RelationshipWith(p) != PlayerRelationship.Ally"), "the own team only");

			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			var why = Between(v07, "string TeamLeadAllInWhy(", "// Fog-honest, like TeamLeadKnownBuildings");
			var de = why.IndexOf("AheadDeescalate(now);", StringComparison.Ordinal);
			var sample = why.IndexOf("var capWhy = CapAllInWhy(now);", StringComparison.Ordinal);
			var early = why.IndexOf("if (TeamLeadOpenOrder() is { } open && (open.AllIn || open.GatherOnly))", StringComparison.Ordinal);
			var ahead = why.IndexOf("if (gate.Open && AheadWhy(now) is { } ahead)", StringComparison.Ordinal);
			var cap = why.IndexOf("if (gate.Open && capWhy != null)\n\t\t\t\treturn AllInReturn(now, capWhy);", StringComparison.Ordinal);
			var late = why.IndexOf("if (gate.Open && ArmyAllInLateOn(now))", StringComparison.Ordinal);
			Assert.That(sample, Is.GreaterThan(de));
			Assert.That(early, Is.GreaterThan(sample), "sampled before the early return");
			Assert.That(cap, Is.GreaterThan(ahead));
			Assert.That(late, Is.GreaterThan(cap), "the late tick stays the fallback");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;
	}
}
