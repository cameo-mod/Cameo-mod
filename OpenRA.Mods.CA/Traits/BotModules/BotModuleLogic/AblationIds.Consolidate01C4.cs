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
	// CONSOLIDATE-01 c4 row (CONSOLIDATE_01_DESIGN.md 3.3). Off = stage S6 yields nothing (passive income is then built only through the opening slot
	// and the first-copy rules, as before).
	public static partial class AblationIds
	{
		public const string BbPassiveIncome = "bb_passive_income";
	}
}
