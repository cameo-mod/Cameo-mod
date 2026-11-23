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
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// UPGRADE-QUEUE-02 (add-ons never idle, Opus ruling): the add-on service is the plug spawner's BuildingAddons producer. It is LIVE for
	// the genericbot tier bots, behind its own switch (AddonService, default on) and its own ablation row (pq_addon_service). Classic and
	// fransbot must stay byte-identical: the module is gated on genericbot only, and no classic or fransbot bot is granted genericbot.
	[TestFixture]
	public sealed class AddonServiceTest
	{
		static string Ai() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "packaging", "ai.yaml")).Replace("\r\n", "\n");

		// The RequiresCondition line of the PlugSpawnerBotModuleCA block in ai.yaml.
		static string PlugSpawnerGate()
		{
			var lines = Ai().Split('\n');
			var header = Array.FindIndex(lines, l => l.Trim() == "PlugSpawnerBotModuleCA:");
			Assert.That(header, Is.GreaterThanOrEqualTo(0), "PlugSpawnerBotModuleCA block not found in ai.yaml");
			return lines[header + 1].Trim();
		}

		[Test]
		public void TheAddonServiceIsOnByDefault()
		{
			Assert.That(new PlugSpawnerBotModuleCAInfo().AddonService, Is.True);
		}

		[Test]
		public void TheAddonServiceHasItsOwnAblationRow()
		{
			Assert.That(AblationIds.AddonService, Is.EqualTo("pq_addon_service"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pq_addon_service"));
		}

		[Test]
		public void TheAddonServiceIsNotAnUpgradeQueueType()
		{
			// The upgrade service serves Upgrades/Research/Promotions/Addons. BuildingAddons stays out, so neither service starves the other.
			var upgrade = new UnitBuilderBotModuleCAInfo();
			Assert.That(upgrade.UpgradeServiceQueues, Does.Not.Contain("BuildingAddons"));
		}

		[Test]
		public void TheAddonServiceIsGatedOnGenericbotTierBotsOnly()
		{
			// Live for genericbot tier bots: the gate is exactly genericbot. No plug_spawn (it kept add-ons idle on master), no classicbot.
			Assert.That(PlugSpawnerGate(), Is.EqualTo("RequiresCondition: genericbot"));
		}

		[Test]
		public void ClassicAndFransbotAreNotGrantedGenericbot()
		{
			// The genericbot grant lists tier bots only. Classic never carries it; fransbot is a separate bot type that stays out of the list.
			var lines = Ai().Split('\n');
			var header = Array.FindIndex(lines, l => l.Trim() == "GrantConditionOnBotOwner@generic:");
			Assert.That(header, Is.GreaterThanOrEqualTo(0));
			for (var i = header + 1; i < lines.Length && lines[i].StartsWith("\t\t"); i++)
			{
				var t = lines[i].Trim();
				if (t.StartsWith("Bots:", StringComparison.Ordinal))
				{
					Assert.That(t, Does.Not.Contain("classic"));
					Assert.That(t, Does.Not.Contain("fransbot"));
					return;
				}
			}

			Assert.Fail("Bots list not found in the genericbot grant");
		}
	}
}
