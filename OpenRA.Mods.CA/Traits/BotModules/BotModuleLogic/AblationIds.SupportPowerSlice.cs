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
	// SUPPORTPOWER-SLICE-01 row (Opus 11-27).
	public static partial class AblationIds
	{
		// SUPPORTPOWER-SLICE-01: the support power target scan runs in the mod-side SupportPowerSliceBotModule, a bounded number of candidates per bot tick, instead of the engine's
		// SupportPowerBotASModule (one whole-world scan in a single tick: 0.3-3.2 s calls in the V08H J3 game). Row off = the engine module, untouched.
		public const string SupportPowerSlice = "sp_slice";
	}
}
