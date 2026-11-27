#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// STALEMATE-SCOUT-01: the unscouted or stale regions of the tactical map, best first, for a push that is ready with no known target.
	/// The provider offers the centre of each region in score order (staleness, interest, danger, distance: the scout module's own scoring);
	/// the caller decides which centre it can walk to. Only the bot's own region memory is read: no enemy actor, trait or order stream.
	/// Providers live in OpenRA.Mods.Cameo and must not be referenced by name here.
	/// </summary>
	public interface IBotScoutCellProvider
	{
		/// <summary>
		/// Offers up to <paramref name="maxChecks"/> stale region centres, best first, to <paramref name="acceptable"/> and returns the first it accepts.
		/// <paramref name="checks"/> is how many centres were offered (0 = no stale region at all, so nothing to scout); <paramref name="exhausted"/> is true when no stale region was left unoffered
		/// (false = the scan stopped at maxChecks with regions still to try).
		/// </summary>
		bool TryPickScoutCell(CPos from, int maxChecks, Func<CPos, bool> acceptable, out CPos cell, out int checks, out bool exhausted);
	}
}
