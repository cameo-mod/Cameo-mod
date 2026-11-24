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
	// SUSTAINED-CASH-01: a bank above the tier line for 1500 ticks forces the next building to be production (the cycle pick). The timer and the pick
	// are pure; the wiring is pinned in source (no headless world fixture).
	[TestFixture]
	public sealed class SustainedCash01Test
	{
		static string Queue() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs")).Replace("\r\n", "\n");

		[Test]
		public void TheTimerBecomesActiveAfterTheHoldOfAnUnbrokenRun()
		{
			var clock = new SustainedCashClock();
			for (var t = 100; t <= 1599; t += 25)
				clock.Observe(t, true);

			Assert.That(clock.Active(1599, 1500), Is.False, "1499 ticks above the line is not enough");
			clock.Observe(1600, true);
			Assert.That(clock.Active(1600, 1500), Is.True, "100 + 1500");
		}

		[Test]
		public void ADipBelowTheLineResetsTheRunAndTheTimerStartsAgain()
		{
			var clock = new SustainedCashClock();
			clock.Observe(0, true);
			clock.Observe(1500, true);
			Assert.That(clock.Active(1500, 1500), Is.True);

			clock.Observe(1525, false);
			Assert.That(clock.Active(1525, 1500), Is.False, "the dip reset it");
			clock.Observe(1550, true);
			Assert.That(clock.Active(3049, 1500), Is.False, "counted from 1550, not from 0");
			Assert.That(clock.Active(3050, 1500), Is.True);
		}

		[Test]
		public void ASecondObservationInTheSameTickIsIgnoredAndAFreshClockIsInactive()
		{
			var clock = new SustainedCashClock();
			Assert.That(clock.Active(5000, 1500), Is.False);
			clock.Observe(10, true);
			clock.Observe(10, false);
			Assert.That(clock.Active(1510, 1500), Is.True, "the same-tick dip did not reset it");
		}

		[Test]
		public void TheCycleRollsOverTypesAtTheirLimitAndNothingLeftMeansNormalBuilding()
		{
			// at-limit types are not candidates (OverflowProductionCandidates filters them), so the pick moves on
			Assert.That(CycleProductionCA.Pick(new[] { ("barracks", 3), ("warfactory", 1) }, "warfactory"), Is.EqualTo(1));
			Assert.That(CycleProductionCA.Pick(new[] { ("barracks", 3) }, "warfactory"), Is.EqualTo(0), "only barracks left: it keeps going");
			Assert.That(CycleProductionCA.Pick(new (string, int)[0], "barracks"), Is.EqualTo(-1), "every type at its limit: normal building resumes");
		}

		[Test]
		public void TheDefaultsAndTheRowAreRegistered()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.SustainedCashProduction, Is.False, "code default off; the yaml arms genericbot only");
			Assert.That(info.SustainedCashTicks, Is.EqualTo(1500));
			Assert.That(AblationIds.SustainedCashProduction, Is.EqualTo("pb_sustained_cash_production"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pb_sustained_cash_production"));
		}

		[Test]
		public void TheRungSitsAfterTheLowPowerRungAboveTheOpeningUsesThePowerResolvedCandidatesAndSkipsTheDefenceQueue()
		{
			var src = Queue();
			var from = src.IndexOf("ActorInfo ChooseBuildingToBuild(ProductionQueue queue)", StringComparison.Ordinal);
			var lowPower = src.IndexOf("Priority override (low power)", from, StringComparison.Ordinal);
			var rung = src.IndexOf("if (SustainedActive)", from, StringComparison.Ordinal);
			var opening = src.IndexOf("if (openingBarracksPolicyActive && !baseBuilder.HasBuiltOrQueuedBarracks())", from, StringComparison.Ordinal);
			var expansion = src.IndexOf("ChooseExpansionDemandItem(queue, buildableThings)", from, StringComparison.Ordinal);
			Assert.That(rung, Is.GreaterThan(lowPower));
			Assert.That(rung, Is.LessThan(opening));
			Assert.That(rung, Is.LessThan(expansion));
			Assert.That(src, Does.Contain("&& !baseBuilder.Info.DefenseQueues.Contains(Category)\n\t\t\t&& baseBuilder.SustainedCash.Active(world.WorldTick, baseBuilder.Info.SustainedCashTicks);"));
			Assert.That(src, Does.Contain("var sustained = ChooseCycledProduction(buildableThings, power);"));
			Assert.That(src, Does.Contain("BaseBuilderQueueEvalCA.PickOrPower(naval, power, HasSufficientPowerForActor)"), "power when the production building would leave power short");
			Assert.That(src, Does.Contain("bool SustainedActive => baseBuilder.SustainedCashArmed && !ablateSustained.Off(player, AblationIds.SustainedCashProduction)"));
		}
	}
}
