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
	// CONSOLIDATE-03 P5 (b) row (design 02 4c19e5f9 contradiction 1, addendum 3 item 4).
	public static partial class AblationIds
	{
		// ub_admit_one: the core focus (R2) asks the admission list (R4) before it names a type: a type that the fill's admission, reserve and depth-cash gates would refuse right now is not eligible for the core (the next-dearest admissible
		// type takes its place), so the core does not lift a pick that is then re-rolled to a cheaper unit. Row off = the core's own bank bar alone, as before.
		public const string UbAdmitOne = "ub_admit_one";
	}
}
