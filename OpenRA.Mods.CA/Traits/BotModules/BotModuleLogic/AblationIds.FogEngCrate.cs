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
	// FOG-ENGCRATE-01 row (Opus 11-27, user decision D2 on audit A8/A9). Owner: Code Monkey.
	public static partial class AblationIds
	{
		// Generic bots choose engineer capture targets, bridge huts and crates only among actors seen now or remembered (the player's frozen layer).
		// Off = the old omniscient scans (ai.yaml CheckCaptureTargetsForVisibility / CheckRepairTargetsForVisibility / CheckTargetsForVisibility: false).
		public const string FogEngCrate = "fog_engcrate";
	}
}
