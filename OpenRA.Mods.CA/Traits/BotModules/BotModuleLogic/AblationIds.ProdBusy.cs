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
	// PROD-BUSY-01 rows (USER P0: every queue busy while cash > reserve). Owner: Workhorse.
	// pb_all_factories off = one factory per category per pass (two while the bank is saturated), as before.
	// pb_all_categories off = the sweep stops after the first served category below MaximiseProductionCash, as before.
	// pb_all_building_producers off = one building/defence start per category per base-builder sweep, as before.
	// pb_building_idle_sweep off = an idle base-builder sweep waits StructureProductionInactiveDelay, as before.
	// pb_idle_cap_tiered off (PROD-BUSY-03b) = the idle-unit limit is the module's IdleBaseUnitsMaximum (25), as before.
	// pb_idle_cap off (PROD-BUSY-03) = a met share mix idles the unit queue unless the bank is saturated, as before.
	public static partial class AblationIds
	{
		public const string ProdBusyAllFactories = "pb_all_factories";
		public const string ProdBusyAllCategories = "pb_all_categories";
		public const string ProdBusyAllBuildingProducers = "pb_all_building_producers";
		public const string ProdBusyBuildingIdleSweep = "pb_building_idle_sweep";
		public const string ProdBusyIdleCap = "pb_idle_cap";
		public const string ProdBusyIdleCapTiered = "pb_idle_cap_tiered";
	}
}
