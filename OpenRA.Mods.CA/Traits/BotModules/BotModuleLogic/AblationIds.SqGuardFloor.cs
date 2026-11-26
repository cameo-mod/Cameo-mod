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
	// CONSOLIDATE-03 P7 (b) row (design 01 R4 guard minimum, Opus review change 2).
	public static partial class AblationIds
	{
		// sq_guard_floor: the home guard keeps a learned minimum (percent of the guard, personality prior from the yaml) and guard-lend lends only the units above it; during an open all-in the floor is 0 and the guard may lend
		// up to the most a guard ever lends. Row off = the lend share alone, as before.
		public const string SqGuardFloor = "sq_guard_floor";
	}
}
