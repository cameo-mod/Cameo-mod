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
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// HARV-01 (switch hv_harvester_target): the harvester target per refinery in both directions. V08 seed 100775: seat_9 GDI had 0 harvesters on 1
	// refinery for 12000 ticks while seat_14 Ixian ran 21 on 1 refinery. The pure rule is tested directly, the wiring by pins, the arm by the yaml.
	[TestFixture]
	public sealed class HarvesterTargetTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		[Test]
		public void TheTargetIsAtLeastTwoPerRefineryAndNoneWithoutOne()
		{
			Assert.That(HarvesterTarget.Target(0, 2), Is.EqualTo(0));
			Assert.That(HarvesterTarget.Target(1, 2), Is.EqualTo(2));
			Assert.That(HarvesterTarget.Target(3, 1), Is.EqualTo(6), "never below two per refinery");
			Assert.That(HarvesterTarget.Target(2, 3), Is.EqualTo(6));
		}

		[Test]
		public void TheCapIsTheTargetPlusTheSlack()
		{
			Assert.That(HarvesterTarget.Cap(1, 2, 1), Is.EqualTo(3));
			Assert.That(HarvesterTarget.Cap(4, 2, 1), Is.EqualTo(12));
			Assert.That(HarvesterTarget.Cap(1, 2, -5), Is.EqualTo(2), "negative slack = the target");
			Assert.That(HarvesterTarget.Cap(0, 2, 1), Is.EqualTo(0));
		}

		[Test]
		public void SeatFourteenWouldHaveStoppedAtThree()
		{
			var cap = HarvesterTarget.Cap(1, 2, 1);
			Assert.That(HarvesterTarget.OverCap(2, 0, cap), Is.False);
			Assert.That(HarvesterTarget.OverCap(3, 0, cap), Is.True);
			Assert.That(HarvesterTarget.OverCap(2, 1, cap), Is.True, "queued counts");
			Assert.That(HarvesterTarget.OverCap(21, 0, cap), Is.True);
		}

		[Test]
		public void AReplacementIsDueBelowTheTargetWithTheBankAtTheMinimum()
		{
			Assert.That(HarvesterTarget.Due(0, 0, 2, 300, 300), Is.True, "seat_9: 0 on 1 refinery");
			Assert.That(HarvesterTarget.Due(1, 1, 2, 5000, 300), Is.False, "a queued one covers it");
			Assert.That(HarvesterTarget.Due(0, 0, 2, 299, 300), Is.False, "bank below the minimum");
			Assert.That(HarvesterTarget.Due(0, 0, 0, 5000, 300), Is.False, "no refinery, no target");
		}

		[Test]
		public void TheWiringIsBehindTheSwitch()
		{
			var ub = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(ub, Does.Contain("if (HarvesterTargetOn)\n\t\t\t\tTryServeHarvesterTarget(bot);"), "served before the pause votes");
			Assert.That(ub.IndexOf("TryServeHarvesterTarget(bot);", StringComparison.Ordinal), Is.LessThan(ub.IndexOf("var baseBuilderPause = requestPause", StringComparison.Ordinal)));
			Assert.That(ub, Does.Contain("if (HarvesterTargetOn && HarvesterOverCap(name))\n\t\t\t\treturn \"harvester_cap\";"), "the cap is a ShouldBuild gate");

			var part = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.HarvesterTarget.cs");
			Assert.That(part, Does.Contain("bool HarvesterTargetOn => switchesActive && Info.HvHarvesterTarget && !ablateHarvesterTarget.Off(player, AblationIds.HvHarvesterTarget);"));
		}

		[Test]
		public void OnlyTheGenericUnitBuilderArmsItAndTheRowIsRegistered()
		{
			Assert.That(FieldOrNull(PlayerNode("UnitBuilderBotModuleCA@generic"), "HvHarvesterTarget"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("UnitBuilderBotModuleCA@classic"), "HvHarvesterTarget"), Is.Null);
			Assert.That(AblationIds.HvHarvesterTarget, Is.EqualTo("hv_harvester_target"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.HvHarvesterTarget));

			var info = new UnitBuilderBotModuleCAInfo();
			Assert.That(info.HvHarvesterTarget, Is.False);
			Assert.That(info.HarvesterTargetMinCash, Is.EqualTo(300));
			Assert.That(info.HarvesterCapSlackPerRefinery, Is.EqualTo(1));
			Assert.That(info.HarvesterTargetPerRefinery, Is.EqualTo(2), "the binding rule: target 2 per refinery");
			Assert.That(info.HarvesterOrderLatencyTicks, Is.EqualTo(30));
		}

		// Sol FIX of HARV-01 d7edb732 (1): the target follows the binding 2 / cap 3, not the harvester module's HarvestersPerRefinery of 3.
		[Test]
		public void TheTargetIsTheBindingTwoPerRefineryAndNotTheHarvesterModulesThree()
		{
			var part = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.HarvesterTarget.cs");
			Assert.That(part, Does.Contain("harvesterTarget = HarvesterTarget.Target(refineries, Info.HarvesterTargetPerRefinery);"));
			Assert.That(part, Does.Contain("harvesterCap = HarvesterTarget.Cap(refineries, Info.HarvesterTargetPerRefinery, Info.HarvesterCapSlackPerRefinery);"));
			Assert.That(part, Does.Not.Contain("h.HarvestersPerRefinery"), "the module's own number (3 on generic) no longer feeds the target");

			Assert.That(HarvesterTarget.Target(2, new UnitBuilderBotModuleCAInfo().HarvesterTargetPerRefinery), Is.EqualTo(4));
			Assert.That(HarvesterTarget.Cap(2, new UnitBuilderBotModuleCAInfo().HarvesterTargetPerRefinery, new UnitBuilderBotModuleCAInfo().HarvesterCapSlackPerRefinery), Is.EqualTo(6), "2 per refinery, cap 3 per refinery");
		}

		// Sol FIX of HARV-01 (2): a start counts as queued until the queue shows it or the latency window passes, so a refresh cannot forget a late outgoing order.
		[Test]
		public void AStartThatTheQueueHasNotShownYetSurvivesARefreshAndCannotExceedTheCap()
		{
			// one start issued 1 tick before the refresh: the cache says 1, the queue still 0
			Assert.That(HarvesterTarget.EffectiveQueued(0, 1, 1, 30), Is.EqualTo(1), "not forgotten");
			Assert.That(HarvesterTarget.EffectiveQueued(1, 1, 5, 30), Is.EqualTo(1), "shown: the queue's count");
			Assert.That(HarvesterTarget.EffectiveQueued(0, 1, 30, 30), Is.EqualTo(0), "refused or dropped: released after the window");
			Assert.That(HarvesterTarget.EffectiveQueued(0, 1, 500, 30), Is.EqualTo(0));
			Assert.That(HarvesterTarget.EffectiveQueued(2, 1, 1, 30), Is.EqualTo(2), "the queue may hold more than the cache");

			// the cap: 1 refinery, cap 3, live 2, one outgoing order not yet in the queue
			var cap = HarvesterTarget.Cap(1, 2, 1);
			Assert.That(HarvesterTarget.OverCap(2, HarvesterTarget.EffectiveQueued(0, 1, 1, 30), cap), Is.True, "the late order counts: no second order passes the cap");
			Assert.That(HarvesterTarget.OverCap(2, 0, cap), Is.False, "the old refresh (forgot it) would have let a second one through");

			var part = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.HarvesterTarget.cs");
			Assert.That(part, Does.Contain("HarvesterTarget.EffectiveQueued(inQueue.GetValueOrDefault(type), harvesterQueuedByType.GetValueOrDefault(type),"));
			Assert.That(part, Does.Contain("harvesterStartTick[name] = world.WorldTick;"));
			Assert.That(part, Does.Contain("harvesterQueuedByType[name] = harvesterQueuedByType.GetValueOrDefault(name) + 1;"), "every start enters the per-type cache");
		}
	}
}
