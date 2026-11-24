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
	// CN-ARM-01 (Opus 11-24, yaml only): the ported CN features that never ran for genericbot are armed for every tier and personality bot,
	// not for classic, each behind its existing switch. Deploy, region roles, stealth and assault fan-out stay off.
	[TestFixture]
	public sealed class CnArm01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode[] Player() => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.ToArray();

		static string[] Bots(string grant) => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "^AIDifficulties").Value.Nodes
			.Single(n => n.Key == "GrantConditionOnBotOwner@" + grant).Value.Nodes
			.Single(n => n.Key == "Bots").Value.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		static readonly string[] GenericTiers =
		{
			"noob", "easiest", "veryeasy", "easy", "medium", "hard", "veryhard", "brutal", "challenger", "unbeatable", "cameogod",
			"exploit_rush", "exploit_turtle", "exploit_guerrilla", "exploit_expansion", "exploit_steamroller", "exploit_tech",
		};

		[TestCase("cn2unitrepair")]
		[TestCase("cn2garrison")]
		[TestCase("cn3bridgerepair")]
		public void TheGrantCoversEveryGenericbotTierAndPersonalityAndNotClassic(string grant)
		{
			var bots = Bots(grant);
			Assert.That(bots, Does.Contain("fransbot"), "the old arm stays");
			foreach (var tier in GenericTiers)
				Assert.That(bots, Does.Contain(tier), grant + " " + tier);
			Assert.That(bots.Any(b => b.Contains("classic", StringComparison.OrdinalIgnoreCase)), Is.False);
		}

		[TestCase("cn3deploy")]
		[TestCase("cn4regionroles")]
		public void DeployAndRegionRolesStayFransbotOnly(string grant)
		{
			Assert.That(Bots(grant), Is.EqualTo(new[] { "fransbot" }));
		}

		[Test]
		public void TheModulesStillHaveTheirSwitchesSoTheAblationPathIsKept()
		{
			var src = Source("packaging/ai.yaml");
			Assert.That(src, Does.Contain("RequiresCondition: genericbot && cn2_unit_repair"));
			Assert.That(src, Does.Contain("RequiresCondition: genericbot && cn2_garrison_defense"));
			Assert.That(src, Does.Contain("RequiresCondition: genericbot && cn3_bridge_repair"));
		}

		[Test]
		public void TheGarrisonerModuleStillYieldsToGarrisonDefenceSoThereIsNoDoubleOwner()
		{
			Assert.That(Source("packaging/ai.yaml"), Does.Contain("RequiresCondition: classicbot || (genericbot && !garrison_contest && !cn2_garrison_defense)"));
		}

		[Test]
		public void EveryGenericbotSquadManagerCarriesTheGroundTargetTagsAndClassicDoesNot()
		{
			var blocks = Player().Where(n => n.Key.StartsWith("SquadManagerBotModuleCA@", StringComparison.Ordinal)).ToArray();
			var generic = blocks.Where(n => n.Key != "SquadManagerBotModuleCA@classic").ToArray();
			Assert.That(generic.Length, Is.EqualTo(6));
			foreach (var b in generic)
			{
				Assert.That(b.Value.Nodes.Single(n => n.Key == "RushPriorityTags").Value.Value, Is.EqualTo("artillery, production"), b.Key);
				Assert.That(b.Value.Nodes.Single(n => n.Key == "AssaultPriorityTags").Value.Value, Is.EqualTo("production, superweapon"), b.Key);
			}

			var classic = blocks.Single(n => n.Key == "SquadManagerBotModuleCA@classic");
			Assert.That(classic.Value.Nodes.Any(n => n.Key == "RushPriorityTags" || n.Key == "AssaultPriorityTags"), Is.False);
		}
	}
}
