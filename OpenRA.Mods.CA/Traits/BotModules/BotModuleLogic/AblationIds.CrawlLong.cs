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
	// CRAWL-LONG-01 (Opus 11-25, user issue: over long distances the crawl wastes power plants). Owner: Sonnet. Off = the behaviour of the base tip (9262610a).
	public static partial class AblationIds
	{
		// A crawl placement under the refinery law must strictly reduce the distance from our buildable-area frontier to the aim; a link with no forward cell is placed at a base cell
		// and parks the crawl target (no random-resource or toward-the-enemy fallback), and the search is anchored on the frontier instead of a (random) yard.
		public const string CrawlAdvanceOnly = "crawl_advance_only";
	}
}
