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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-BUSY-02: the research, upgrade and add-on queues are served through the unit-production pause votes (one switch and one ablation row each).
	// The decision is pure (UpgradeServiceCA.HeldByPause + Choose); the wiring in the unit builder is pinned in source (no headless world fixture).
	[TestFixture]
	public sealed class ProdBusy02Test
	{
		static string Unit() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs")).Replace("\r\n", "\n");

		[Test]
		public void AServedThroughPauseTypeIsNeverHeldAndOtherTypesFollowTheVote()
		{
			Assert.That(UpgradeServiceCA.HeldByPause(true, true), Is.False);
			Assert.That(UpgradeServiceCA.HeldByPause(true, false), Is.True);
			Assert.That(UpgradeServiceCA.HeldByPause(false, false), Is.False);
			Assert.That(UpgradeServiceCA.HeldByPause(false, true), Is.False);
		}

		[Test]
		public void APaidItemStartsDuringAPauseVoteOnlyWhenTheTypeIsServedThroughPause()
		{
			var items = new[] { new UpgradeServiceCA.Candidate("research", 1500, 1) };
			var none = new HashSet<string>();
			Assert.That(UpgradeServiceCA.Choose(items, none, UpgradeServiceCA.HeldByPause(true, true)), Is.EqualTo(0));
			Assert.That(UpgradeServiceCA.Choose(items, none, UpgradeServiceCA.HeldByPause(true, false)), Is.EqualTo(-1));
		}

		[Test]
		public void TheDefaultsServeResearchUpgradesAndAddonsThroughPauseAndPromotionsNeedNoRow()
		{
			var info = new UnitBuilderBotModuleCAInfo();
			Assert.That(info.UpgradeServiceThroughPause, Is.EqualTo(new[] { "Upgrades", "Research", "Addons" }));
			Assert.That(info.UpgradeServiceThroughPause, Does.Not.Contain("Promotions"));
		}

		[Test]
		public void EachTypeHasItsOwnRegisteredAblationRow()
		{
			Assert.That(AblationIds.ProdBusyResearchThroughPause, Is.EqualTo("pb_research_through_pause"));
			Assert.That(AblationIds.ProdBusyUpgradeThroughPause, Is.EqualTo("pb_upgrade_through_pause"));
			Assert.That(AblationIds.ProdBusyAddonThroughPause, Is.EqualTo("pb_addon_through_pause"));
			Assert.That(AblationSpec.RegistryIds(), Is.SupersetOf(new[] { "pb_research_through_pause", "pb_upgrade_through_pause", "pb_addon_through_pause" }));
		}

		[Test]
		public void TheWiringIsSwitchGatedPerTypeAndFeedsTheChoice()
		{
			var src = Unit();
			Assert.That(src, Does.Contain("Array.IndexOf(Info.UpgradeServiceThroughPause, queueType) < 0"));
			Assert.That(src, Does.Contain("\"Research\" => !ablateThroughPauseResearch.Off(player, AblationIds.ProdBusyResearchThroughPause)"));
			Assert.That(src, Does.Contain("\"Upgrades\" => !ablateThroughPauseUpgrade.Off(player, AblationIds.ProdBusyUpgradeThroughPause)"));
			Assert.That(src, Does.Contain("\"Addons\" => !ablateThroughPauseAddon.Off(player, AblationIds.ProdBusyAddonThroughPause)"));
			Assert.That(src, Does.Contain("var heldByPause = UpgradeServiceCA.HeldByPause(paused, ServedThroughPause(category));"));
			Assert.That(src, Does.Contain("UpgradeServiceCA.Choose(candidates, inProduction, heldByPause,"));
			Assert.That(src, Does.Contain("if (UpgradeServiceOn)\n\t\t\t\tServeUpgradeQueues(bot);"), "the service stays behind the genericbot switch, so Classic never runs it");
		}
	}
}
