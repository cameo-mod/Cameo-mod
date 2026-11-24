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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// pb_expansion_refinery_safety (USER 11-24): the expansion refinery goes into the very-next slot only while the new yard is safe.
	// The rule is pure (ExpansionSafetyCA); the wiring is pinned in source because there is no headless world fixture.
	[TestFixture]
	public sealed class ExpansionSafety01Test
	{
		const int Quiet = 750;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void ASafeYardGetsTheRefineryFirst()
		{
			Assert.That(ExpansionSafetyCA.Safe(1000, -1, Quiet), Is.True, "no threat ever recorded");
		}

		[Test]
		public void ADamagedYardGetsNoRefineryInTheSlot()
		{
			Assert.That(ExpansionSafetyCA.Safe(1000, 1000, Quiet), Is.False, "damaged this tick");
			Assert.That(ExpansionSafetyCA.Safe(1500, 1000, Quiet), Is.False, "damaged 500 ticks ago");
		}

		[Test]
		public void AVisibleEnemyWithinTwelveCellsKeepsTheYardUnsafeWhileItStays()
		{
			// The scan renews the threat tick every pass while the unit is in range.
			var last = -1;
			for (var now = 100; now <= 1000; now += 25)
			{
				last = now;
				Assert.That(ExpansionSafetyCA.Safe(now, last, Quiet), Is.False);
			}
		}

		[Test]
		public void TheYardIsSafeAgainExactlyAfter750QuietTicks()
		{
			Assert.That(ExpansionSafetyCA.Safe(1000 + 749, 1000, Quiet), Is.False);
			Assert.That(ExpansionSafetyCA.Safe(1000 + 750, 1000, Quiet), Is.True, "the refinery goes first again");
		}

		[Test]
		public void OnlyAYardBeyondTheMainBaseRadiusIsAnExpansionYard()
		{
			Assert.That(ExpansionSafetyCA.IsExpansionYard(28 * 28, 28), Is.False);
			Assert.That(ExpansionSafetyCA.IsExpansionYard(28 * 28 + 1, 28), Is.True);
			Assert.That(ExpansionSafetyCA.IsExpansionYard(0, 28), Is.False, "the main yard is never gated");
		}

		[Test]
		public void TheSwitchIsOffByDefaultTheYamlArmsItForGenericbotOnlyAndTheRowIsRegistered()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.ExpansionRefinerySafety, Is.False);
			Assert.That(info.ExpansionSafetyQuietTicks, Is.EqualTo(750));
			Assert.That(info.ExpansionSafetyScanCells, Is.EqualTo(12));
			Assert.That(AblationIds.ExpansionRefinerySafety, Is.EqualTo("pb_expansion_refinery_safety"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pb_expansion_refinery_safety"));
			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml, Does.Contain("ExpansionRefinerySafety: true"));
			Assert.That(Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs"), Does.Contain("public bool ExpansionSafetyArmed => Info.ExpansionRefinerySafety && switchesActive;"), "Classic never runs it");
		}

		[Test]
		public void TheGateSitsInTheSharedRefineryPickSoTheEconomyRungAndTheSustainedBlockBothObeyItAndItDropsRatherThanHolds()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			var from = q.IndexOf("ActorInfo ChooseWantedRefinery(", StringComparison.Ordinal);
			var to = q.IndexOf("var pick = BaseBuilderQueueEvalCA.PickOrPower(refinery, power", from, StringComparison.Ordinal);
			var body = q.Substring(from, to - from);
			Assert.That(body, Does.Contain("if (SafetyOn)"));
			Assert.That(body, Does.Contain("!baseBuilder.ExpansionRefinerySafe(wantedField.Value)"));
			Assert.That(body, Does.Contain("return null;"), "dropped from the slot, not held");
			Assert.That(q, Does.Contain("bool SafetyOn => baseBuilder.ExpansionSafetyArmed && !ablateSafety.Off(player, AblationIds.ExpansionRefinerySafety);"));
		}

		[Test]
		public void DamageAndAVisibleScanBothRecordTheThreatFogHonestly()
		{
			var b = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(b, Does.Contain("if (ExpansionSafetyArmed && Info.ConstructionYardTypes.Contains(self.Info.Name))\n\t\t\t\tyardThreatTick[self.ActorID] = world.WorldTick;"));
			Assert.That(b, Does.Contain("a.CanBeViewedByPlayer(player)"), "a unit the player cannot see is not a threat");
			Assert.That(b, Does.Contain("IsEnemyGroundUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>()"));
			Assert.That(b, Does.Contain("WDist.FromCells(Info.ExpansionSafetyScanCells)"));
		}
	}
}
