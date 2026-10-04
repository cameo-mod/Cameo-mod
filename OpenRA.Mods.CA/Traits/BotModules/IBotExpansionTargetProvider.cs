#region Copyright & License Information
/**
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
	/// REF-1 (AI_ARCHITECTURE §12.24 v2, DESIGN §19.1b): one refinery-to-anchor claim — the anchor (a resource
	/// spreader, or the centre of a field that has none), the field it belongs to, the claim tier
	/// (1 = first refinery of a field that has none yet, 2 = an additional spreader of an already covered field),
	/// and the field's valuable resource cells so the placement can sit flush to the field.
	/// </summary>
	public readonly struct RefineryAnchorClaim
	{
		public readonly CPos Anchor;
		public readonly CPos FieldCenter;
		public readonly int FieldId;
		public readonly int Tier;
		public readonly IReadOnlyCollection<CPos> ResourceCells;

		public RefineryAnchorClaim(CPos anchor, CPos fieldCenter, int fieldId, int tier, IReadOnlyCollection<CPos> resourceCells)
		{
			Anchor = anchor;
			FieldCenter = fieldCenter;
			FieldId = fieldId;
			Tier = tier;
			ResourceCells = resourceCells;
		}
	}

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

		/// <summary>
		/// FE-1 (§12.24, DESIGN §19.1b): the provider enforces one refinery per anchor (resource spreader or spreaderless
		/// field centre). While true the base builder replaces its yard-based refinery cap, OptimalRefineryCount and the
		/// scale-target refinery cap by <see cref="UnclaimedAnchorsInReach"/>, and every refinery is placed through
		/// <see cref="NextRefineryClaim"/>. Default false: no provider (classic) or switch off = unchanged.
		/// </summary>
		bool RefineryLawActive => false;

		/// <summary>FE-1: the number of anchors; the physical ceiling of the refinery count.</summary>
		int RefineryAnchorCount => 0;

		/// <summary>FE-1: anchors within building reach that no own refinery serves yet.</summary>
		int UnservedAnchorsInReach => 0;

		/// <summary>FE-1: the resource centre of the anchor's field, the tie-break of the claim placement. Null = none.</summary>
		CPos? RefineryClaimFieldCenter => null;

		/// <summary>
		/// REF-1 (§12.24 v2): the claim the next refinery must serve. <paramref name="near"/> is the ranking origin for an
		/// MCV-requested refinery (the requesting yard) — within each tier the anchor nearest it wins; null = the planner's
		/// own ordering (home first). Null result = no anchor wants a refinery: the caller retries later and must NOT fall
		/// back to the old base placement — a refinery that serves no anchor is exactly what the law forbids.
		/// </summary>
		RefineryAnchorClaim? NextRefineryClaim(CPos? near) => null;

		/// <summary>
		/// REF-1: a produced refinery was just committed to <paramref name="anchor"/> — it is pending-served until the
		/// building lands (or the pending expires), so another refinery does not claim the same anchor meanwhile.
		/// </summary>
		void RefineryClaimCommitted(CPos anchor) { }

		/// <summary>
		/// REF-1: anchors in reach that are claimable right now — unserved, not parked, not pending. The production gate:
		/// a refinery is allowed only while this exceeds the in-flight refinery count, never by comparing global totals
		/// (a duplicate at home must not eat the quota of a forward anchor).
		/// </summary>
		int UnclaimedAnchorsInReach => 0;
	}
}
