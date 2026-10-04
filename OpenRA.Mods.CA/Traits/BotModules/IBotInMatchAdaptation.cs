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
	/// <summary>
	/// EL-1 bounded in-match adaptation (DESIGN 19.13 "in-match vs between matches", AI_ARCHITECTURE 12.32):
	/// a provider exposing a bounded bias on the commit/retreat bar derived from this match's own engagement
	/// results. The squad manager owns the bar; the provider only suggests a delta. Bounded by the provider's
	/// own clamp — consumers must still clamp into a sane range.
	/// </summary>
	public interface IBotInMatchAdaptation
	{
		/// <summary>
		/// Delta in percentage points added to the effective RetreatRatioPct: positive tightens the bar
		/// (retreat sooner, commit only at higher advantage — losing form), negative loosens it toward
		/// today's values (winning form). Bounded to the provider's MaxDeltaPct.
		/// </summary>
		int RetreatRatioDeltaPct { get; }
	}
}
