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
using System.Linq;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-01 C9 (P7 + P1): X6 - with every refinery destroyed all unit production paused for the rest of the game; X13 - paid Promotions stalled behind a pause vote.
	[TestFixture]
	public sealed class UnitC9Pause01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static System.Collections.Immutable.ImmutableArray<MiniYamlNode> Player() => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;

		[Test]
		public void TheRefineryPauseLapsesOnlyAfterARefineryHasStoodAndOnlyForGenericbot()
		{
			var b = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(b, Does.Contain("bool IBotRequestPauseUnitProduction.PauseUnitProduction => !IsTraitDisabled && !HasMinimalRefineryCount() && !RefineryPauseLapsed();"));
			Assert.That(b, Does.Contain("if (!Info.RefineryPauseOpeningOnly || !switchesActive || ablateRefineryPause.Off(player, AblationIds.UnitRefineryPauseOpening))\n\t\t\t\treturn false;"));
			Assert.That(b, Does.Contain("var has = AIUtils.CountActorByCommonName(RefineryBuildings) >= Info.InititalMinimumRefineryCount;\n\t\t\tif (has)\n\t\t\t\trefineryEverOwned = true;\n\n\t\t\treturn has;"),
				"the helper still returns the CURRENT count; only the latch is new");
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.EarlyDef.cs"), Does.Contain("\"ub_refinery_pause_opening\""));

			var player = Player();
			Assert.That(player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes.Single(n => n.Key == "RefineryPauseOpeningOnly").Value.Value, Is.EqualTo("true"));
			foreach (var o in player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "RefineryPauseOpeningOnly"), Is.False, o.Key);
		}

		[Test]
		public void PaidPromotionsStartThroughAPauseOnGenericbotOnly()
		{
			var player = Player();
			var list = player.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes.Single(n => n.Key == "UpgradeServiceThroughPause").Value.Value
				.Split(',', StringSplitOptions.TrimEntries);
			Assert.That(list, Is.EquivalentTo(new[] { "Upgrades", "Research", "Addons", "Promotions" }));
			foreach (var o in player.Where(n => n.Key.StartsWith("UnitBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "UnitBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "UpgradeServiceThroughPause"), Is.False, o.Key);
		}
	}
}
