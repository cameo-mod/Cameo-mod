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
	// V08 rows (Opus 11-25).
	public static partial class AblationIds
	{
		// UNIT-ALL-01: the unit pick is one fraction roll over every listed UnitsToBuild type the queue can build, with a personality bias, an observed-enemy counter term and a
		// floor (every listed type keeps a chance; a type never built is picked first). Row off = the old mix layers.
		public const string UnitRoll = "ub_unit_roll";
	}
}
