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
	// CONSOLIDATE-03 P2 (b), the cash ledger's behaviour rows (CONSOLIDATE_03_P2_DESIGN_02.md).
	public static partial class AblationIds
	{
		// The S8-S10 building reserve uses the unit reserve's personality lean (Info CashOneReserve). Off = the unscaled tier reserve, as before.
		public const string ClOneReserve = "cl_one_reserve";
	}
}
