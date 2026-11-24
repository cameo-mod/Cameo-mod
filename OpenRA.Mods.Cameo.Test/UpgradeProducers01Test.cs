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
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// UPGRADE-PRODUCERS-01 (USER P0, V03): the upgrade and research producers (td_gdi_communicationscenter, ra1_allies_radardome, the tech centres) have no
	// BuildingFractions row, so the fraction roll never built them and no upgrade ever started. Every unlisted structure type is built once.
	[TestFixture]
	public sealed class UpgradeProducers01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[TestCase("td_gdi_communicationscenter")]
		[TestCase("td_gdi_advancedcommunicationscenter")]
		[TestCase("ra1_allies_radardome")]
		[TestCase("ra1_allies_techcenter")]
		public void TheV03UpgradeAndResearchProducersAreCoveredWhileTheyHaveNoFractionRow(string name)
		{
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, name), Is.True);

			// the V03 cause: no live ai.yaml fraction row names it (the commented ladder rows do not count)
			var ai = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai");
			var all = ai.SelectMany(Flatten).Where(n => n.Key == "BuildingFractions").SelectMany(n => n.Value.Nodes).Select(n => n.Key);
			Assert.That(all, Does.Not.Contain(name));
		}

		static System.Collections.Generic.IEnumerable<MiniYamlNode> Flatten(MiniYamlNode n) =>
			new[] { n }.Concat(n.Value.Nodes.SelectMany(Flatten));

		[Test]
		public void ARowedWallFakeDefenceProxyOrSpecialRuleTypeIsNotCovered()
		{
			Assert.That(UnlistedOnceCA.Eligible(true, true, false, false, false, "td_gdi_barracks"), Is.False, "has a fraction row");
			Assert.That(UnlistedOnceCA.Eligible(false, false, false, false, false, "x"), Is.False, "no building");
			Assert.That(UnlistedOnceCA.Eligible(false, true, true, false, false, "td_gdi_wall"), Is.False, "wall or fake");
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, true, false, "td_gdi_refinery"), Is.False, "own rule: yard, refinery, power, silo");
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, true, "td_gdi_guardtower"), Is.False, "defence");
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, "powerproxy.parabombs"), Is.False, "proxy");
			Assert.That(UnlistedOnceCA.Eligible(false, true, false, false, false, null), Is.False);
		}

		[Test]
		public void TheCheapestFirstCopyWinsThenTheName()
		{
			var c = new[] { ("td_gdi_advancedcommunicationscenter", 2000), ("td_gdi_communicationscenter", 1000), ("a_other", 1000) };
			Assert.That(c[UnlistedOnceCA.Pick(c)].Item1, Is.EqualTo("a_other"));
			Assert.That(UnlistedOnceCA.Pick(Array.Empty<(string, int)>()), Is.EqualTo(-1));
		}

		[Test]
		public void TheRungSitsAfterTheEssentialPlannerAndDemandRungsAndBeforeTheCycleAndTheFractionRoll()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			var rung = q.IndexOf("var unlisted = ChooseUnlistedOnce(queue, buildableThings, power);", StringComparison.Ordinal);
			Assert.That(rung, Is.GreaterThan(q.IndexOf("var demandPick = ChooseExpansionDemandItem(queue, buildableThings);", StringComparison.Ordinal)));
			Assert.That(rung, Is.LessThan(q.IndexOf("if (CycleOn)", rung, StringComparison.Ordinal)));
			Assert.That(rung, Is.LessThan(q.IndexOf("// Build everything else", StringComparison.Ordinal)));
		}

		[Test]
		public void TheFirstCopyIsAnArmyFirstEssentialAndPassesTheLowCashHoldAsAFirstCopy()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(q, Does.Contain("if (UnlistedOnceOn && UnlistedOnceKind(actorInfo) && !OwnedOrQueued(actorInfo))\n\t\t\t\treturn true;"));
			Assert.That(q, Does.Contain("EssentialsLowCashCA.Holds(true, bank, line, !OwnedOrQueued(item)"), "first copy of any kind passes the low-cash hold");
			Assert.That(q, Does.Contain("if (!UnlistedOnceOn || baseBuilder.Info.DefenseQueues.Contains(Category) || !baseBuilder.HasAdequateProductionCount())"));
			Assert.That(q, Does.Contain("BaseBuilderQueueEvalCA.PickOrPower(infos[index], power, HasSufficientPowerForActor)"));
		}

		[Test]
		public void TheRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.ArmyFirst02.cs"), Does.Contain("Af2UnlistedOnce = \"af2_unlisted_once\""));
			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var armed = player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA@", StringComparison.Ordinal)
				&& n.Value.Nodes.Any(x => x.Key == "UnlistedBuildingsOnce" && x.Value.Value == "true")).ToArray();
			Assert.That(armed.Length, Is.EqualTo(1));
			Assert.That(armed[0].Key, Is.EqualTo("BaseBuilderBotModuleCA@generic"));
			Assert.That(armed[0].Value.Nodes.Single(x => x.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
		}
	}
}
