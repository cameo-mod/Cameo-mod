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
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// REF-INTERVAL-LADDER (row rf_interval_ladder, USER VALUE 2100 - 150 r): the refinery interval follows the shared difficulty ladder.
	[TestFixture]
	public sealed class RefIntervalLadderTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		[Test]
		public void TheLadderIs2100Minus150PerRankFromNoobToGod()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.RefineryMinIntervalEasiest, Is.EqualTo(2100));
			Assert.That(info.RefineryMinIntervalHardest, Is.EqualTo(750));
			Assert.That(info.RefineryMinIntervalUnranked, Is.EqualTo(info.RefineryMinIntervalTicks), "an unranked owner keeps the flat value (= the hard tier)");

			for (var r = -1; r <= 9; r++)
				Assert.That(BotDifficultyLadder.InterpolateByRank(info.RefineryMinIntervalEasiest, info.RefineryMinIntervalHardest, r, BotDifficultyLadder.Difficulties.Length),
					Is.EqualTo(2100 - 150 * r), "rank " + r);

			Assert.That(2100 - 150 * 4, Is.EqualTo(info.RefineryMinIntervalTicks), "the flat 1500 is the Hard value, so the row changes the other tiers only");
		}

		[Test]
		public void TheRowIsOffByDefaultNeedsTheEconomyGateAndBothSitesReadTheEffectiveValue()
		{
			Assert.That(new BaseBuilderBotModuleCAInfo().RefineryIntervalLadder, Is.False);

			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool RefineryIntervalLadderArmed => Info.RefineryIntervalLadder && Info.RefineryEconomyGate && switchesActive && !ablateRefineryIntervalLadder.Off(player, AblationIds.RefineryIntervalLadder);"));
			Assert.That(bb, Does.Contain("BotDifficultyLadder.Scale(Info.RefineryMinIntervalEasiest, Info.RefineryMinIntervalHardest, player, Info.RefineryMinIntervalUnranked)"));
			Assert.That(bb, Does.Contain(": Info.RefineryMinIntervalTicks;"), "off = the flat value");

			var qm = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(qm.Split("baseBuilder.RefineryMinIntervalEffective").Length - 1, Is.EqualTo(2), "both Economy calls");
			Assert.That(qm, Does.Not.Contain("b.RefineryMinIntervalTicks"), "no direct read of the flat value any more");
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegistered()
		{
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "RefineryIntervalLadder"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "RefineryIntervalLadder"), Is.Null);
			Assert.That(AblationIds.RefineryIntervalLadder, Is.EqualTo("rf_interval_ladder"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.RefineryIntervalLadder));
		}
	}
}
