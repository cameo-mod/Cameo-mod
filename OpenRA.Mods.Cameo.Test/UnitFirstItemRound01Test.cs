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
	// CONSOLIDATE-01 C8 (P3 round-robin): X5 - a 4-deep tank queue took its depth items before infantry had a first one. With ub_first_item_round the category loop runs twice:
	// round 1 serves only FIRST items (a queue that already holds an item is skipped without costing an offer), round 2 is today's loop and fills the depth.
	[TestFixture]
	public sealed class UnitFirstItemRound01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheCategoryLoopRunsTwiceRoundOneFirstAndRestoresTheRotation()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var round1 = u.IndexOf("if (FirstItemRoundOn)", StringComparison.Ordinal);
			var round2 = u.IndexOf("\t\t\t\tServeCategories();\n\n\t\t\t\tvoid ServeCategories()", StringComparison.Ordinal);
			Assert.That(round1, Is.GreaterThan(0));
			Assert.That(round2, Is.GreaterThan(round1), "round 2 (today's loop) follows round 1");

			var block = u.Substring(round1, round2 - round1);
			Assert.That(block, Does.Contain("firstItemRoundPass = true;"));
			Assert.That(block, Does.Contain("firstItemRoundPass = false;"), "reset in finally");
			Assert.That(block, Does.Contain("currentQueueIndex = savedIndex;"), "the pass rotates once");
			Assert.That(block, Does.Contain("serviceCursorByCategory[pair.Key] = pair.Value;"), "the fair-service cursors are restored too");
			Assert.That(u.IndexOf("UnitMixDeficitQueues().ToList())", StringComparison.Ordinal), Is.LessThan(round1), "the deficit categories are served before both rounds");
			Assert.That(u, Does.Contain("if (firstItemRoundPass)\n\t\t\t\trejectLog = null;"), "DEV reject rows are written once per queue per pass (round 2)");
			Assert.That(u, Does.Contain("readonly Dictionary<string, string> firstRoundSavedCursors"), "no per-tick allocation");
		}

		[Test]
		public void RoundOneSkipsQueuesThatHoldAnItemAndNeverOrdersASecondOne()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var skip = u.IndexOf("if (firstItemRoundPass && depthQueue && queuedItems > 0)", StringComparison.Ordinal);
			var offer = u.IndexOf("offers--;", skip, StringComparison.Ordinal);
			Assert.That(skip, Is.GreaterThan(0));
			Assert.That(offer, Is.GreaterThan(skip), "the skip costs no offer");
			Assert.That(u, Does.Contain("if (firstItemRoundPass && depthQueue && held > 0)\n\t\t\t\t\t\tbreak;"), "a queue that got its first item in round 1 stops there");
			Assert.That(u, Does.Contain("bool FirstItemRoundOn => switchesActive && Info.UnitFirstItemRound && FairService && !ablateFirstRound.Off(player, AblationIds.UnitFirstItemRound);"));
		}

		[Test]
		public void TheRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.EarlyDef.cs"), Does.Contain("\"ub_first_item_round\""));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(generic.Single(n => n.Key == "UnitFirstItemRound").Value.Value, Is.EqualTo("true"));
			foreach (var o in player.Where(n => n.Key.StartsWith("UnitBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "UnitBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "UnitFirstItemRound"), Is.False, o.Key);
		}
	}
}
