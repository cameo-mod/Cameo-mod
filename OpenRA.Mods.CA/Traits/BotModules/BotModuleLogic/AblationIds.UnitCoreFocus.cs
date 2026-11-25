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
	// V08G row (Opus 11-25, HANS-PROFILE-01 row 2).
	public static partial class AblationIds
	{
		// HANS-PROFILE-01: the dearest affordable types (the core) of a queue's pool, among the types already built once, get a weight multiplier so that the core holds about half of the roll.
		// Row off = the roll weights are untouched, as before. A bias only: the floor chance of every type stays.
		public const string UnitCoreFocus = "ub_core_focus";
	}
}
