#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// REFSCAN D-5 (design _12 3be85dba + ERRATUM_01 9c33a59b): the planner's read-only surface for the
	/// site-first refinery search the base builder runs (OpenRA.Mods.CA RefinerySiteSearch).
	/// </summary>
	public partial class ExpansionPlannerBotModule
	{
		RefinerySiteSearchSettings IBotExpansionTargetProvider.RefinerySiteSearch => LawActive
			? new RefinerySiteSearchSettings(Info.RefinerySiteSearchEnabled, Info.RefinerySearchVisitUnits,
				Info.RefinerySearchDeadlineVisitUnits, Info.RefinerySearchOrderBuildUnits,
				Info.RefinerySearchEpisodeBindGraceTicks, Info.RefineryOrderLatencyTicks)
			: default;

		/// <summary>
		/// REFSCAN sec 3.3 O-2 (F7, R1): the offer set of <see cref="ComputeClaim"/> at this tick without its
		/// writes — the same ClaimOrder call and filters, minus every anchor the ladder would park.
		/// </summary>
		IReadOnlyList<RefineryAnchorClaim> IBotExpansionTargetProvider.PeekRefineryClaims(CPos? near)
		{
			if (!LawActive)
				return Array.Empty<RefineryAnchorClaim>();

			var tick = world.WorldTick;
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			var order = ClaimOrder(anchors, anchorFieldIds, fieldCellsById, lastRefineryCells, lastBuildingTiles,
				serve, Info.ReachCells,
				i => AnchorBlockedForClaims(i, tick),
				i => AnchorCommittedForClaims(i, tick),
				near, out _, out _, out var tiers, out _, out _,
				RouteRank, CoverageUnknown, DemandOverride);

			var kept = PeekFilter(order, anchors, anchorOfferStreaks, Info.RefineryClaimFailAttempts);
			var claims = new List<RefineryAnchorClaim>(kept.Count);
			foreach (var i in kept)
			{
				var fieldId = anchorFieldIds[i];
				var center = ClaimFieldCenter(fieldId, anchors[i]);
				claims.Add(new RefineryAnchorClaim(anchors[i], center, fieldId, tiers[i], fieldCellsById[fieldId]));
			}

			return claims;
		}

		/// <summary>
		/// REFSCAN sec 3.3 (iii), R1: keep an anchor iff the ladder would offer it rather than park it —
		/// <c>streak + 1 &lt;= attempts</c> (a missing streak counts 0), in the given order. Pure.
		/// </summary>
		public static List<int> PeekFilter(IReadOnlyList<int> order, IReadOnlyList<CPos> anchorCells,
			IReadOnlyDictionary<CPos, int> streaks, int attempts)
		{
			var kept = new List<int>(order.Count);
			foreach (var i in order)
			{
				var streak = streaks.TryGetValue(anchorCells[i], out var s) ? s : 0;
				if (streak + 1 <= attempts)
					kept.Add(i);
			}

			return kept;
		}

		RefineryFallbackOutcome IBotExpansionTargetProvider.PollRefineryFallbackSite(Actor producer, string item,
			bool distanceToBaseIsImportant, int cellBudget, bool terminalSweep, out CPos? site)
		{
			return PollFallback(producer, item, distanceToBaseIsImportant, Math.Max(1, cellBudget), terminalSweep, out site);
		}

		// The resource-gap boxes, rebuilt when the field model is replaced (the list is only ever swapped wholesale).
		List<(int MinX, int MinY, int MaxX, int MaxY, List<CPos> Cells)> gapBoxes;
		object gapBoxesFor;

		/// <summary>
		/// REFSCAN sec 3.2 class-1 gap: min Chebyshev distance over (footprint tile, rank cell) pairs, the rank
		/// cells being every anchor plus every field cell (the widened scan's metric, 80646D1B). Fields whose
		/// bounding box cannot beat the running minimum are skipped, so the cost stays near the closest fields.
		/// </summary>
		int IBotExpansionTargetProvider.RefineryResourceGap(string item, CPos topLeft)
		{
			if (!LawActive || !world.Map.Rules.Actors.TryGetValue(item, out var actorInfo)
				|| actorInfo.TraitInfoOrDefault<BuildingInfo>() is not { } bi)
				return int.MaxValue;

			var tiles = bi.Tiles(topLeft).ToList();
			var best = int.MaxValue;
			foreach (var a in anchors)
				foreach (var t in tiles)
					best = Math.Min(best, RefineryFallbackScan.ChebyshevCells(a, t));

			var f = fields;
			if (f == null)
				return best;

			if (!ReferenceEquals(gapBoxesFor, f))
			{
				gapBoxes = f.Where(x => x.Cells.Count > 0).Select(x => (x.Cells.Min(c => c.X), x.Cells.Min(c => c.Y),
					x.Cells.Max(c => c.X), x.Cells.Max(c => c.Y), x.Cells)).ToList();
				gapBoxesFor = f;
			}

			foreach (var box in gapBoxes)
			{
				var lower = int.MaxValue;
				foreach (var t in tiles)
				{
					var dx = t.X < box.MinX ? box.MinX - t.X : t.X > box.MaxX ? t.X - box.MaxX : 0;
					var dy = t.Y < box.MinY ? box.MinY - t.Y : t.Y > box.MaxY ? t.Y - box.MaxY : 0;
					lower = Math.Min(lower, Math.Max(dx, dy));
				}

				if (lower >= best)
					continue;

				foreach (var c in box.Cells)
					foreach (var t in tiles)
						best = Math.Min(best, RefineryFallbackScan.ChebyshevCells(c, t));
			}

			return best;
		}

		/// <summary>
		/// REFSCAN sec 3.3 R: the widened scan's own reach providers (FallbackProviderSource), ActorID order.
		/// When the legal domain is the whole map (no RBA, BuildAnywhere, or no base-distance rule) one
		/// map-wide pseudo provider stands in.
		/// </summary>
		void IBotExpansionTargetProvider.RefinerySearchProviders(Actor producer, string item, List<(CPos Center, int Reach)> into)
		{
			into.Clear();
			if (!LawActive || !world.Map.Rules.Actors.TryGetValue(item, out var actorInfo)
				|| actorInfo.TraitInfoOrDefault<BuildingInfo>() is not { } bi)
				return;

			var rba = actorInfo.TraitInfoOrDefault<RequiresBuildableAreaInfo>();
			var buildAnywhere = player.PlayerActor.Trait<DeveloperMode>().BuildAnywhere;
			if (rba == null || buildAnywhere)
			{
				var size = world.Map.MapSize;
				into.Add((new CPos(size.Width / 2, size.Height / 2), Math.Max(size.Width, size.Height) / 2 + 1));
				return;
			}

			var mapBuildRadius = world.WorldActor.TraitOrDefault<MapBuildRadius>();
			var allyBuild = mapBuildRadius != null && mapBuildRadius.AllyBuildRadiusEnabled;
			var fpExtent = FootprintExtent(bi);
			var source = FallbackProviderSource(producer, rba, fpExtent, allyBuild);
			var found = new List<(uint Id, CPos Center, int Reach)>();
			while (source.MoveNext())
				if (source.Current is { } p)
					found.Add((p.Id, p.Center, p.ReachCells));

			foreach (var p in found.OrderBy(p => p.Id))
				into.Add((p.Center, p.Reach));
		}
	}
}
