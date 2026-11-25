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

		// Once a crawl has started its target stays until it is reached, becomes invalid (taken, dead, out of the field list) or the no-advance park fires; no flip on a re-pick.
		public const string CrawlTargetSticky = "crawl_target_sticky";

		// BS-03 c9 port (light team crawl, Opus V08G): with the lobby option allybuild on, the visible allied construction yards are crawl anchors next to our own (the cap and the target choice)
		// and the allied buildable-area buildings are part of the frontier the crawl closes on the aim from. Off = own yards and own frontier only.
		public const string CrawlAllyAnchor = "crawl_ally_anchor";

		// Opus V08G ruling (a): a committed crawl target is released (and parked) when the own frontier gets no closer to it for CrawlStickyExpiryTicks, for ANY reason. Off = a commit lasts until reached/invalid/parked.
		public const string CrawlStickyExpiry = "crawl_sticky_expiry";

		// Opus V08G ruling (b), BS-03 c7 semantics: while the safety gate refuses the crawl because enemy value stands at the frontier or aim, the crawl is paused for at most CrawlMaxPauseTicks
		// (900 + 150 r), then the target is parked and the commit released; the escort request is at least CrawlEscortValue (900 + 150 r). Off = the safety refusals as before, unbounded.
		public const string CrawlPauseBound = "crawl_pause_bound";
	}
}
