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
	// PERF-F1 row (PERF_SCAN_BURST_01 section 5, F1). Owner: Sonnet. Off = every enemy search scans World.Actors afresh, as before.
	public static partial class AblationIds
	{
		public const string EnemyScan = "pf_enemy_scan";
	}
}
