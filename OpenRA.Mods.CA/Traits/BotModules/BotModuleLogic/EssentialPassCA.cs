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
	// ESSENTIAL-PASS-01 (USER P0, Opus 11-24). Pure decision: which building starts are army-first essentials beyond the always-essential
	// yards, refineries, power plants, tech chain and first production building (those stay in IsArmyFirstEssential).
	public static class EssentialPassCA
	{
		/// <summary>
		/// A first copy of an eligible structure kind (no wall, defence, fake or proxy; see BaseBuilderQueueManagerCA.FirstCopyKind) while none is owned or
		/// queued, and any cash trickler (passive income is built up to its limit), pass the army-first share and reserve votes and the mobile-spend reserve.
		/// </summary>
		public static bool Essential(bool firstCopyKind, bool ownedOrQueued, bool cashTrickler) =>
			(firstCopyKind && !ownedOrQueued) || cashTrickler;
	}
}
