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
	// FOG-FROZEN-01 row. FogFrozenSkip off = BotFogMemory remembers Hidden and Shrouded frozen actors again, as the old walk did.
	public static partial class AblationIds
	{
		public const string FogFrozenSkip = "fog_frozen_skip";
	}
}
