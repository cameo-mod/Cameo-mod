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

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CRAWL-SAFETY (FINDINGS_2026-10-12 S3): cells a module wants scouted soon, published while the
	/// need holds (e.g. an unexplored crawl aim). The scout module folds them into region interest —
	/// publishing grants no vision; a scout still has to fly there. Providers live in
	/// OpenRA.Mods.Cameo and must not be referenced by name here.
	/// </summary>
	public interface IBotScoutInterestProvider
	{
		IReadOnlyList<CPos> ScoutInterestCells { get; }
	}
}
