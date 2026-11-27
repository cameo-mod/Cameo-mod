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
	// STALEMATE-SCOUT-01 (USER decision D5 on audit A7).
	public static partial class AblationIds
	{
		// ss_scout_push: a ready push with no known target scouts the stale regions of the tactical map (then attacks what it finds) instead of waiting at home;
		// only an unreachable target excuses it. Row off = the finish-mode and all-in hunt on the plain grid, as before.
		public const string SsScoutPush = "ss_scout_push";
	}
}
