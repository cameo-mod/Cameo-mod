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
	// CONSOLIDATE-03 P5 (b) row (design 02 4c19e5f9 contradiction 2, addendum 3 item 5).
	public static partial class AblationIds
	{
		// ub_gates_up: the post-draw gates that need no cash and are batch state (the in-pass limit headroom, the capturer cap with the batch's pending capturers, the aircraft caps with the batch's pending aircraft)
		// move into the roll's legal set (R1), so a type those gates would refuse is not in the pool, the mean floor, the core, the draw bound or the retained pool maximum. NOT distribution-equal: the pool,
		// the floors, the bound and the RNG stream change. Row off = the gates stay after the draw, as before.
		public const string UbGatesUp = "ub_gates_up";
	}
}
