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
	/// Cameo (docs/design/AI_ARCHITECTURE.md §12.13, EX-1): the resource field an expansion planner wants the base
	/// to walk toward. <c>BaseBuilderBotModuleCA</c>'s BaseCrawl placements aim at it when one is published; null
	/// keeps today's behaviour. Providers live in OpenRA.Mods.Cameo and must not be referenced by name here.
	/// </summary>
	public interface IBotExpansionTargetProvider
	{
		CPos? ExpansionTarget { get; }

		/// <summary>EX-2: the target field is in reach and unclaimed, so a refinery should go there next.</summary>
		bool WantsRefineryAtExpansionTarget { get; }

		/// <summary>EX-2: how close (cells) to the target a refinery must stand to claim the field.</summary>
		int ExpansionTargetClaimRadius { get; }

		/// <summary>
		/// EX-2c: the field whose resource centre the next refinery should claim. May differ from
		/// <see cref="ExpansionTarget"/> (the crawl aim): any free field already in reach qualifies, so an
		/// outpost yard draws its refinery the moment it can place one instead of waiting to become the
		/// crawl target. Null = claim <see cref="ExpansionTarget"/> (the EX-2 behaviour).
		/// </summary>
		CPos? RefineryClaimTarget { get; }
	}
}
