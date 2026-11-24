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
	// ESSENTIALS-01: below the tier MaximiseProductionCash line the base builder admits only essentials. The decision is pure
	// (EssentialsLowCashCA.Holds); the wiring is pinned in source (no headless world fixture).
	[TestFixture]
	public sealed class Essentials01Test
	{
		static string Queue() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs")).Replace("\r\n", "\n");

		// bank 1000 under a line of 3500; flags: firstCopy, techChain, refinery, powerWhileShort, passiveIncome, plannerOrDemand
		static bool Low(bool first = false, bool tech = false, bool refinery = false, bool power = false, bool passive = false, bool demand = false) =>
			EssentialsLowCashCA.Holds(true, 1000, 3500, first, tech, refinery, power, passive, demand);

		[Test]
		public void AnExtraCopyIsHeldBelowTheLine()
		{
			Assert.That(Low(), Is.True);
		}

		[Test]
		public void EachEssentialKindIsAdmittedBelowTheLine()
		{
			Assert.That(Low(first: true), Is.False, "each available building once");
			Assert.That(Low(tech: true), Is.False, "tech-chain prerequisite");
			Assert.That(Low(refinery: true), Is.False, "refinery (per-field law bounds the count)");
			Assert.That(Low(power: true), Is.False, "power while short");
			Assert.That(Low(passive: true), Is.False, "passive-income generator, up to its build limit, at low cash too");
			Assert.That(Low(demand: true), Is.False, "threat-driven defence / planner want");
		}

		[Test]
		public void AtOrAboveTheLineNothingIsHeldAndNoLineOrNoSwitchMeansInert()
		{
			Assert.That(EssentialsLowCashCA.Holds(true, 3500, 3500, false, false, false, false, false, false), Is.False);
			Assert.That(EssentialsLowCashCA.Holds(true, 9000, 3500, false, false, false, false, false, false), Is.False);
			Assert.That(EssentialsLowCashCA.Holds(true, 100, 0, false, false, false, false, false, false), Is.False, "no tier line");
			Assert.That(EssentialsLowCashCA.Holds(false, 100, 3500, false, false, false, false, false, false), Is.False, "switch off");
		}

		[Test]
		public void TheDefaultsAndTheRowAreRegistered()
		{
			Assert.That(new BaseBuilderBotModuleCAInfo().EssentialsOnlyBelowTierLine, Is.False, "code default off; the yaml arms genericbot only");
			Assert.That(AblationIds.EssentialsLowCash, Is.EqualTo("pb_essentials_low_cash"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pb_essentials_low_cash"));
		}

		[Test]
		public void TheWiringIsSwitchGatedAndAFirstStartCountsAsBeingProducedAtOnce()
		{
			var src = Queue();
			Assert.That(src, Does.Contain("bool EssentialsOn => baseBuilder.EssentialsLowCashArmed && !ablateEssentials.Off(player, AblationIds.EssentialsLowCash);"));
			Assert.That(src, Does.Contain("if (EssentialsOn && EssentialsHolds(item, isRefinery, exempt, cashNow))"));
			Assert.That(src, Does.Contain("item.HasTraitInfo<CashTricklerInfo>()"), "the same passive-income predicate as BaseFrontBackPlanner.IsPassiveIncome");
			Assert.That(src, Does.Contain("if (AllProducersOn)\n\t\t\t\t\tbaseBuilder.BuildingsBeingProduced[item.Name] ="), "same-sweep duplicates are blocked");
		}
	}
}
