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
	// UPGRADE-QUEUE-01: the upgrade, add-on, research and promotion queues have their own service that runs before the cash floor. The
	// choice is a pure function (UpgradeServiceCA.Choose) tested here for behaviour; the wiring in the unit builder is pinned in source
	// because there is no headless world fixture.
	[TestFixture]
	public sealed class UpgradeQueueServiceTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Unit() => Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");

		static UpgradeServiceCA.Candidate C(string name, int cost, int weight = 1) => new(name, cost, weight);

		static string Pick(UpgradeServiceCA.Candidate[] items, int bank, bool paused = false, params string[] excluded)
		{
			var i = UpgradeServiceCA.Choose(items, new HashSet<string>(excluded), bank, paused);
			return i < 0 ? null : items[i].Name;
		}

		[Test]
		public void FreeItemsComeFirstWhateverTheWeightTheBankOrThePauseVote()
		{
			var items = new[] { C("big", 500, 9), C("promo", 0, 1), C("small", 50, 5) };
			Assert.That(Pick(items, 10000), Is.EqualTo("promo"));
			Assert.That(Pick(items, 0, paused: true), Is.EqualTo("promo"));
		}

		[Test]
		public void PaidItemsNeedTheBankAndNoPauseVote()
		{
			var items = new[] { C("a", 500, 3), C("b", 200, 1) };
			Assert.That(Pick(items, 499), Is.EqualTo("b"), "the unaffordable higher-weight item is skipped, not queued unfunded");
			Assert.That(Pick(items, 199), Is.Null);
			Assert.That(Pick(items, 10000, paused: true), Is.Null, "a pause vote holds paid items");
		}

		[Test]
		public void TheOrderIsWeightThenCostThenNameAndUnlistedItemsComeLast()
		{
			Assert.That(Pick(new[] { C("low", 10, 1), C("high", 90, 4) }, 1000), Is.EqualTo("high"));
			Assert.That(Pick(new[] { C("dear", 90, 2), C("cheap", 10, 2) }, 1000), Is.EqualTo("cheap"));
			Assert.That(Pick(new[] { C("b", 10, 2), C("a", 10, 2) }, 1000), Is.EqualTo("a"));
			Assert.That(Pick(new[] { C("unlisted", 1, -1), C("listed", 900, 0) }, 1000), Is.EqualTo("listed"));
			Assert.That(Pick(new[] { C("unlisted", 1, -1) }, 1000), Is.EqualTo("unlisted"), "a table gap never idles the queue");
		}

		[Test]
		public void AnItemAlreadyInProductionInAnyQueueIsNotStartedAgain()
		{
			var items = new[] { C("a", 10, 5), C("b", 10, 1) };
			Assert.That(Pick(items, 1000, false, "a"), Is.EqualTo("b"));
			Assert.That(Pick(items, 1000, false, "a", "b"), Is.Null);
		}

		[Test]
		public void TheChoiceIsDeterministic()
		{
			var items = new[] { C("x", 10, 2), C("y", 10, 2), C("z", 0, 1) };
			for (var n = 0; n < 5; n++)
				Assert.That(Pick(items, 100), Is.EqualTo("z"));
		}

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
		public void TheServiceRunsBeforeThePauseVotesAndTheCashFloorAndTheSweepLeavesItsQueuesAlone()
		{
			var src = Unit();
			var tick = src.IndexOf("void IBotTick.BotTick(IBot bot)", StringComparison.Ordinal);
			var service = src.IndexOf("ServeUpgradeQueues(bot);", tick, StringComparison.Ordinal);
			var pause = src.IndexOf("requestPause.Any(rp =>", tick, StringComparison.Ordinal);
			var floor = src.IndexOf("Info.ProductionMinCashRequirement, cashLean))", tick, StringComparison.Ordinal);
			Assert.That(service, Is.GreaterThan(tick));
			Assert.That(service, Is.LessThan(pause));
			Assert.That(pause, Is.LessThan(floor));
			Assert.That(src, Does.Contain("if (UpgradeServiceOn && Array.IndexOf(Info.UpgradeServiceQueues, Info.UnitQueues[currentQueueIndex]) >= 0)"));
		}

		[Test]
		public void TheServiceUsesTheChoiceFunctionTheGatesAndNoRandomDraw()
		{
			var src = Unit();
			var from = src.IndexOf("void ServeUpgradeQueues(IBot bot)", StringComparison.Ordinal);
			var to = src.IndexOf("void TryBuildOpeningDefense(IBot bot)", from, StringComparison.Ordinal);
			var service = src.Substring(from, to - from);
			Assert.That(service, Does.Contain("UpgradeServiceCA.Choose(candidates, inProduction, bank, paused)"));
			Assert.That(service, Does.Contain("ShouldBuild(item.Name, true, queue.Info.Type)"));
			Assert.That(service, Does.Contain("rp is not IBotItemAwareUnitCap"));
			Assert.That(service, Does.Contain("upgradeIdleUntil.Remove(key)"));
			Assert.That(service, Does.Not.Contain("Random"));
			Assert.That(service, Does.Not.Contain("ProductionMinCashRequirement"));
			Assert.That(service, Does.Contain("Order.StartProduction(queue.Actor, item2.Name, 1)"));
		}
	}
}
