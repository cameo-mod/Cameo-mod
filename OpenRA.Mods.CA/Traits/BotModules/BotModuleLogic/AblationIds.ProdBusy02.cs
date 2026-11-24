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
	// PROD-BUSY-02 rows (USER P0: every queue busy). Owner: Sonnet.
	// Off = a unit-production pause vote holds the paid items of that served queue type again (UPGRADE-QUEUE-01 rule).
	// Promotions are free and never held by a vote, so they have no row.
	public static partial class AblationIds
	{
		public const string ProdBusyResearchThroughPause = "pb_research_through_pause";
		public const string ProdBusyUpgradeThroughPause = "pb_upgrade_through_pause";
		public const string ProdBusyAddonThroughPause = "pb_addon_through_pause";
	}
}
