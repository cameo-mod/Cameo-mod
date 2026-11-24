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
	// RATIO-2X-01 row. Owner: Frontrunner. Off = no 2x mobile/static ratio at a static-defence start (the army-first gate as before).
	// Read at the base builder's gate and its fraction-loop pre-filter, next to the Info key MobileStaticRatioEnabled.
	public static partial class AblationIds
	{
		public const string Ratio2x = "army_first_02_ratio";
	}
}
