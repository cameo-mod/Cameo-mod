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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ECON-A-FIX (REVIEW_2026-10-06_econ_a R6, REF-1 §12.24 v2): anchor reservations owned by expansion
	/// demands — while a demand's refinery sits on a producer queue its anchor is off the market for
	/// every other claim, so a second producer cannot take it before the bound item lands. World-free
	/// (the tick and the "anchor already taken" probe are supplied by the caller) so the lifecycle —
	/// reserve, refresh, release, expire, commit — is directly testable.
	/// </summary>
	public sealed class RefineryAnchorReservations
	{
		readonly Dictionary<CPos, (int Until, object Owner)> held = new();

		public int Count => held.Count;

		/// <summary>Held (unexpired) by exactly this owner.</summary>
		public bool LiveFor(CPos anchor, object owner, int now) =>
			held.TryGetValue(anchor, out var r) && now < r.Until && ReferenceEquals(r.Owner, owner);

		/// <summary>Held (unexpired) by anyone — the anchor is off the market for other claims.</summary>
		public bool LiveAt(CPos anchor, int now) =>
			held.TryGetValue(anchor, out var r) && now < r.Until;

		/// <summary>
		/// Take or refresh <paramref name="owner"/>'s hold until <paramref name="until"/>. Refused while a
		/// different owner holds it live, or while the anchor is otherwise taken (served, parked,
		/// pending — the caller's probe). An expired hold lapses and can be re-taken by anyone.
		/// </summary>
		public bool TryReserve(CPos anchor, object owner, int now, int until, Func<CPos, bool> taken)
		{
			if (held.TryGetValue(anchor, out var r) && now < r.Until)
			{
				if (!ReferenceEquals(r.Owner, owner))
					return false;

				held[anchor] = (until, owner);
				return true;
			}

			if (taken != null && taken(anchor))
				return false;

			held[anchor] = (until, owner);
			return true;
		}

		/// <summary>The owner lets go early (expiry, unbind, adoption failure). No-op for anyone else.</summary>
		public bool Release(CPos anchor, object owner)
		{
			if (!held.TryGetValue(anchor, out var r) || !ReferenceEquals(r.Owner, owner))
				return false;

			held.Remove(anchor);
			return true;
		}

		/// <summary>Ground truth overrides: a committed anchor's reservation is done whoever owned it.</summary>
		public void Clear(CPos anchor) => held.Remove(anchor);

		/// <summary>Drop every anchor of a set a commit consumed — idempotent, whoever owned each member.</summary>
		public void ClearAll(IReadOnlyCollection<CPos> anchors)
		{
			foreach (var anchor in anchors)
				held.Remove(anchor);
		}

		/// <summary>
		/// REPAIR-B3 (SPEC_2026-10-09 shared coverage): the atomic multi-anchor transaction — one call
		/// validates the whole set against live reservations (foreign owner refuses) and the caller's
		/// taken probe, then commits all members or none. Never reserves the first anchor and fails on
		/// the second; members the same owner already holds live are refreshed together.
		/// </summary>
		public bool TryReserveAll(IReadOnlyCollection<CPos> anchors, object owner, int now, int until, Func<CPos, bool> taken)
		{
			foreach (var anchor in anchors)
			{
				if (held.TryGetValue(anchor, out var r) && now < r.Until)
				{
					if (!ReferenceEquals(r.Owner, owner))
						return false;
				}
				else if (taken != null && taken(anchor))
					return false;
			}

			foreach (var anchor in anchors)
				held[anchor] = (until, owner);

			return true;
		}

		/// <summary>Release every member the owner holds live — idempotent; returns the count released.</summary>
		public int ReleaseAll(IReadOnlyCollection<CPos> anchors, object owner)
		{
			var released = 0;
			foreach (var anchor in anchors)
				if (Release(anchor, owner))
					released++;

			return released;
		}

		/// <summary>Every member live-held by exactly this owner.</summary>
		public bool LiveSetFor(IReadOnlyCollection<CPos> anchors, object owner, int now)
		{
			foreach (var anchor in anchors)
				if (!LiveFor(anchor, owner, now))
					return false;

			return anchors.Count > 0;
		}

		/// <summary>Drop expired holds and holds whose anchor was taken over meanwhile; returns the count.</summary>
		public int Prune(int now, Func<CPos, bool> taken)
		{
			var pruned = 0;
			foreach (var kv in held.ToList())
				if (now >= kv.Value.Until || (taken != null && taken(kv.Key)))
				{
					held.Remove(kv.Key);
					pruned++;
				}

			return pruned;
		}
	}

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
		/// REPAIR-B3 (SPEC_2026-10-09 §3): a produced refinery committed to <paramref name="anchor"/> at the actual
		/// placement <paramref name="site"/> — the provider publishes pending coverage for every anchor that site's
		/// docks demonstrably serve, atomically, before another queue evaluates demand. The default bridges to the
		/// legacy single-anchor commit until the caller passes the site.
		/// </summary>
		void RefineryClaimCommitted(CPos anchor, CPos site) => RefineryClaimCommitted(anchor);

		/// <summary>
		/// REPAIR-B3: the queue's site search for <paramref name="anchor"/> failed — the provider parks it
		/// for the bounded claim-fail cooldown so the next claim goes through instead of an unbounded
		/// cancel/requeue cycle on the same deterministic best anchor. Default no-op: providers that
		/// count uncommitted re-offers bound the churn without the signal.
		/// </summary>
		void RefineryClaimPlacementFailed(CPos anchor) { }

		/// <summary>
		/// REPAIR-B3 (SPEC §3): the anchors a refinery admitted for <paramref name="anchor"/> is expected
		/// to cover — untaken, not already covered, inside the serve radius of the claim. Callers bind the
		/// whole set atomically via <see cref="TryReserveRefineryAnchors"/> at admission. Default empty:
		/// classic and switch-off bind nothing extra.
		/// </summary>
		IReadOnlyList<CPos> RefineryClaimCoveredAnchors(CPos anchor) => Array.Empty<CPos>();

		/// <summary>
		/// REPAIR-B3: the atomic coverage reservation — one transaction carrying the chosen
		/// <paramref name="site"/>, the validated covered-<paramref name="anchors"/> set, the claimant
		/// <paramref name="owner"/> and the coverage-model <paramref name="modelVersion"/> the set was
		/// computed against. The provider refuses when its model version moved on (the caller must
		/// re-derive the set) or any member is taken/foreign-held — all members commit or none.
		/// <paramref name="modelVersion"/> negative skips the version check (tests, simple callers).
		/// </summary>
		bool TryReserveRefineryAnchors(CPos site, IReadOnlyCollection<CPos> anchors, object owner, int untilTick, int modelVersion = -1) => false;

		/// <summary>REPAIR-B3: release <paramref name="owner"/>'s hold on every member — idempotent; returns released count.</summary>
		int ReleaseRefineryAnchors(IReadOnlyCollection<CPos> anchors, object owner) => 0;

		/// <summary>REPAIR-B3: every member of the set is live-held by exactly this owner and untaken.</summary>
		bool RefineryAnchorsReserved(IReadOnlyCollection<CPos> anchors, object owner) => false;

		/// <summary>
		/// REPAIR-B3 (SPEC §52): anchors whose coverage verdict is still UNKNOWN under the probe budget —
		/// they must not count as unclaimed (that would queue a duplicate) nor as served. While this is
		/// nonzero the model has deferred evaluation outstanding.
		/// </summary>
		int RefineryCoveragePendingAnchors => 0;

		/// <summary>
		/// REF-1: anchors claimable right now — unserved, not parked, not pending. The production gate:
		/// a refinery is allowed only while this exceeds the in-flight refinery count, never by comparing global totals
		/// (a duplicate at home must not eat the quota of a forward anchor). The first-refinery fallback claim (an
		/// unserved anchor beyond reach, claimed when nothing else is) counts too, so the first refinery is never
		/// cap-blocked.
		/// </summary>
		int UnclaimedAnchorsInReach => 0;

		/// <summary>
		/// REF-1 B1 (§12.24 v2): the name of the cheapest crawl-eligible building the planner wants produced while
		/// the crawl target's field sits beyond reach and no anchor is claimable — the crawl's own supply, instead of
		/// power demand's accident. Null = no want (classic, switch off, nothing to do).
		/// </summary>
		string WantedLinkBuilding => null;

		/// <summary>
		/// REF-1 B1: the crawl aim refined to the target field's resource cell nearest our frontier — every building
		/// placed to close the gap. Null = <see cref="ExpansionTarget"/> (the field centre) remains the aim.
		/// </summary>
		CPos? CrawlTargetEdge => null;

		/// <summary>
		/// REF-1 B2: the name of the cheapest currently-buildable provider of a due construction MCV's missing
		/// prerequisite (td_gdi: the repair facility). Null = none — the MCV is producible or none is due.
		/// </summary>
		string WantedMcvPrerequisite => null;

		/// <summary>
		/// REF-1 B4: anchors with no serving refinery whose field edge sits beyond building reach — the law's
		/// expansion-nudge condition ("all anchors in reach served and unserved anchors exist beyond reach").
		/// </summary>
		int UnservedAnchorsBeyondReach => 0;

		/// <summary>
		/// ECON-A-FIX (R6): the claim a bound demand's refinery would take — ranked from <paramref name="near"/>
		/// with <paramref name="futureProviderTiles"/> (the committed yard's footprint) counting as part of the
		/// frontier, so an outpost beyond today's reach still reserves its anchor — the reach-only quota must not
		/// gate a field the demand's own yard will open. Null = nothing claimable for it, or the law is off.
		/// </summary>
		RefineryAnchorClaim? DemandRefineryClaim(CPos near, IReadOnlyCollection<CPos> futureProviderTiles) => null;

		/// <summary>
		/// ECON-A-FIX (R6): take or refresh <paramref name="owner"/>'s hold on <paramref name="anchor"/> until
		/// <paramref name="untilTick"/> — refused while another owner holds it live or the anchor is already taken
		/// (served, parked, pending). Callers renew the hold while the bound item is still queued.
		/// </summary>
		bool TryReserveRefineryAnchor(CPos anchor, object owner, int untilTick) => false;

		/// <summary>
		/// ECON-A-FIX (R6): <paramref name="owner"/>'s hold on <paramref name="anchor"/> is live and the anchor is
		/// still untaken — the only case where a placement re-adopts the reserved anchor instead of a fresh claim.
		/// </summary>
		bool RefineryAnchorReserved(CPos anchor, object owner) => false;

		/// <summary>
		/// ECON-A-FIX (R6): <paramref name="owner"/> releases its hold early (demand expiry, binding unwind). A
		/// committed anchor's hold is cleared by <see cref="RefineryClaimCommitted"/> instead.
		/// </summary>
		void ReleaseRefineryAnchor(CPos anchor, object owner) { }
	}

	/// <summary>
	/// REF-1 B4: the expansion nudge. Under the refinery law the old raw refinery total
	/// (<c>numRef &gt;= InititalMinimumRefineryCount + AdditionalMinimumRefineryCount</c>) is replaced by coverage:
	/// fire when every anchor in reach is served and unserved anchors still exist beyond reach. Classic and
	/// switch-off keep the refinery-count test verbatim.
	/// </summary>
	public static class RefineryLawNudge
	{
		public static bool Due(IBotExpansionTargetProvider law, int refineryCount, int refineryMinimum) =>
			law != null
				? law.UnservedAnchorsInReach == 0 && law.UnservedAnchorsBeyondReach > 0
				: refineryCount >= refineryMinimum;
	}

	/// <summary>
	/// REF-1 silo containment (maintainer report 2026-10-04): the classic "head room for resource
	/// storage" priority override fires whenever resources exceed 80% of capacity — under the law a
	/// healthy refinery economy stays above 80% permanently, so the override won every pick and
	/// spammed silos. Under the law a silo is wanted only at &gt;95% capacity with none already in
	/// production; classic and switch-off keep the plain 80% test.
	/// </summary>
	public static class RefineryLawSilo
	{
		public static bool Wanted(bool lawActive, int resources, int capacity, bool siloInProduction) =>
			lawActive
				? resources > 0.95 * capacity && !siloInProduction
				: resources > 0.8 * capacity;
	}

	/// <summary>
	/// REF-1 silo containment, placement side (B1 maintainer ruling): a crawl placement must extend the
	/// buildable area, so under the law only buildings with GivesBuildableArea may take the organic
	/// BaseCrawl roll. The predicate is checked before the roll's random draw — a law-blocked building
	/// consumes no randoms (determinism: the draw count must not depend on which building is produced).
	/// Classic and switch-off keep the unfiltered roll.
	/// </summary>
	public static class RefineryLawCrawlRoll
	{
		public static bool LegalLink(bool lawActive, ActorInfo actorInfo) =>
			!lawActive || actorInfo.HasTraitInfo<GivesBuildableAreaInfo>();
	}
}
