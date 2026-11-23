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
	// UPGRADE-QUEUE-01: the upgrade, add-on, research and promotion queues have their own service that runs before the pause votes and the
	// cash floor. There is no headless world fixture, so these pins hold the wiring: default on, own ablation row, classic untouched, the
	// service sits before the floor, the sweep leaves served queues alone, and the pick is deterministic (no random draw).
	[TestFixture]
	public sealed class UpgradeQueueServiceTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Unit() => Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");

		[Test]
		public void TheServiceIsOnByDefaultForTheFourQueueTypesAndAblatable()
		{
			var info = new UnitBuilderBotModuleCAInfo();
			Assert.That(info.UpgradeService, Is.True);
			Assert.That(info.UpgradeServiceQueues, Is.EqualTo(new[] { "Promotions", "Upgrades", "Research", "Addons" }));
			Assert.That(AblationIds.UpgradeService, Is.EqualTo("pq_upgrade_service"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pq_upgrade_service"));
		}

		[Test]
		public void ClassicNeverRunsTheServiceBecauseItIsGatedOnTheGenericbotSwitch()
		{
			Assert.That(Unit(), Does.Contain("bool UpgradeServiceOn => switchesActive && Info.UpgradeService && !ablateUpgradeService.Off(player, AblationIds.UpgradeService);"));
		}

		[Test]
		public void TheServiceRunsBeforeThePauseVotesAndTheCashFloor()
		{
			var src = Unit();
			var tick = src.IndexOf("void IBotTick.BotTick(IBot bot)", StringComparison.Ordinal);
			var service = src.IndexOf("ServeUpgradeQueues(bot);", tick, StringComparison.Ordinal);
			var pause = src.IndexOf("requestPause.Any(rp =>", tick, StringComparison.Ordinal);
			var floor = src.IndexOf("Info.ProductionMinCashRequirement, cashLean))", tick, StringComparison.Ordinal);
			Assert.That(service, Is.GreaterThan(tick));
			Assert.That(service, Is.LessThan(pause));
			Assert.That(pause, Is.LessThan(floor));
		}

		[Test]
		public void TheOrdinarySweepLeavesTheServedQueuesToTheService()
		{
			Assert.That(Unit(), Does.Contain("if (UpgradeServiceOn && Array.IndexOf(Info.UpgradeServiceQueues, Info.UnitQueues[currentQueueIndex]) >= 0)"));
		}

		[Test]
		public void ThePickIsDeterministicAndOneSetCoversEveryQueueOfThePass()
		{
			var src = Unit();
			var from = src.IndexOf("void ServeUpgradeQueues(IBot bot)", StringComparison.Ordinal);
			var to = src.IndexOf("void TryBuildOpeningDefense(IBot bot)", from, StringComparison.Ordinal);
			var service = src.Substring(from, to - from);
			Assert.That(service, Does.Not.Contain("Random"));
			Assert.That(service, Does.Not.Contain("Shuffle"));
			Assert.That(service, Does.Not.Contain("ProductionMinCashRequirement"));
			Assert.That(service.Split("new HashSet<string>(").Length - 1, Is.EqualTo(1), "one picked-set per pass: two idle queues never start the same item");
			Assert.That(service, Does.Contain("Order.StartProduction(queue.Actor, item.Name, 1)"));
			Assert.That(service, Does.Contain("UpgradeRecheckTicks"));
			Assert.That(src, Does.Contain("cost <= 0 ? int.MaxValue"), "free items (promotions) are always picked first");
		}
	}
}
