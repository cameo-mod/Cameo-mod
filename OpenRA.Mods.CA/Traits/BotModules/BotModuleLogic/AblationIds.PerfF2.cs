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
	// PERF-F2 rows (PERF_SCAN_BURST_01 section 5, F2). Owner: Sonnet.
	// FogClassifyCache off = BotFogMemory.Observe classifies every sighting from scratch, as before.
	// FogFrozenSnapshot off = every enemy walks the whole frozen-actor layer itself, as before.
	public static partial class AblationIds
	{
		public const string FogClassifyCache = "pf_fog_classify";
		public const string FogFrozenSnapshot = "pf_fog_frozen";
	}
}
