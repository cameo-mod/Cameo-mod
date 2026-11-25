#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// UPGRADE-QUEUE-01 row. Owner: Sonnet.
	// UpgradeService off = the upgrade, add-on, research and promotion queues are visited only by the ordinary unit sweep, behind the cash floor.
	public static partial class AblationIds
	{
		public const string UpgradeService = "pq_upgrade_service";

		// UPG-ADDONS: off = the BuildingAddons queue is skipped by the upgrade service (the other served queues are unchanged).
		public const string UpgradeBuildingAddons = "upg_building_addons";

		// UPGRADE-COVERAGE: off = the once-done registry has no gameplay effect (no re-offer exclusion, no reserved pending orders, no oldest-available ordering); the DEV coverage rows stay on.
		public const string UpgradeOnceRegistry = "upg_once_registry";
	}
}
