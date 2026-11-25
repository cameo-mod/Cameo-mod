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
	public static partial class AblationIds
	{
		// Q8b Step 3 (hunt): a pushed hunt order follows the team's hunt cell: when the cell it hunts is explored it moves on to the next one (row off = the order stays at its first cell).
		public const string SqHuntFollow = "sq_hunt_follow";
	}
}
