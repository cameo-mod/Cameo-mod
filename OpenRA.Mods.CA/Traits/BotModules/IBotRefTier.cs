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
	/// <summary>
	/// REF-TIER-01: the refinery target floor (2 early, +2 after a radar provider, +2 after a tech centre, scaled by personality), readable from the expansion planner (OpenRA.Mods.Cameo).
	/// Implemented by BaseBuilderBotModuleCA. While armed the planner lifts every refinery zone cap by one as long as fewer than <see cref="RefTierTarget"/> own refineries stand or are in production.
	/// </summary>
	public interface IBotRefTier
	{
		bool RefTierArmed { get; }

		/// <summary>The target for this tick (completed standing radar and tech centre only; recomputed, so a lost tier drops it).</summary>
		int RefTierTarget { get; }
	}
}
