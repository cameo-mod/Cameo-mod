#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// S8-STARVE-01.
	public static partial class AblationIds
	{
		// A rejected S8 offer no longer holds S9 and S10 back under SUSTAINED cash (Info BbS8RejectedFallthrough). Off = the offer alone holds them, as before.
		public const string BbS8RejectedFallthrough = "bb_s8_rejected_fallthrough";
	}
}
