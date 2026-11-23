#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// UPGRADE-QUEUE-02 (add-ons never idle): the add-on service is the plug spawner's BuildingAddons producer with its own switch
	// (AddonService) and ablation row (pq_addon_service). Pins only: no headless world fixture here, so the yaml host map is not
	// checked by these tests. A later pass should load ai.yaml and check each host against the Plug.Type of its plug.
	[TestFixture]
	public sealed class AddonServiceTest
	{
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
			// The upgrade service serves Upgrades/Research/Promotions/Addons (the dead queue). BuildingAddons must stay out of it,
			// so neither service can starve the other.
			var upgrade = new UnitBuilderBotModuleCAInfo();
			Assert.That(upgrade.UpgradeServiceQueues, Does.Not.Contain("BuildingAddons"));
		}
	}
}
