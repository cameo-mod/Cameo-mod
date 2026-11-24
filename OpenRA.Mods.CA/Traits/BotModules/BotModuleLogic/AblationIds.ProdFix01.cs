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
	// PROD-FIX-01 rows (AUDIT_PRODUCTION_01 unit production gates). Each row off = the previous behaviour.
	public static partial class AblationIds
	{
		public const string ProdFixUnitReserveExclUpgrades = "pb_unit_reserve_excl_upgrades"; // off = upgrade/research/add-on unpaid cost and the reserve P gate unit orders
		public const string ProdFixRotateStart = "pb_rotate_start"; // off = the category sweep starts at the same category every pass
		public const string ProdFixSatBatchConsistent = "pb_sat_batch_consistent"; // off = the saturation verdict reads the pass-start bank
		public const string ProdFixIntervalElapsed = "pb_interval_elapsed"; // off = UnitIntervals count bot visits
		public const string ProdFixNoUnitFloor = "pb_no_unit_floor"; // off = the pass-level cash floor ends the unit pass
		public const string TelemetryIdleReasonOrder = "tm_idle_reason_order"; // off = IdleReason reports saturation_stop before prerequisite_missing / unit_cap
	}
}
