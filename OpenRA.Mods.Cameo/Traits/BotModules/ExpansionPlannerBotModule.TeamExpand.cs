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
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	public partial class ExpansionPlannerBotModule
	{
		const int TeamExpandEpochTicks = 250;
		const int TeamExpandAtlasZoneRadius = 10;
		static readonly ConditionalWeakTable<World, ExpansionRegistry> ExpansionRegistries = new();

		readonly record struct ExpansionSite(CPos Center, CPos[] Cells);
		readonly record struct ExpansionBid(OpenRA.Player Player, int Deadline, int Limit, CPos[] Sites);
		readonly record struct ExpansionClaim(OpenRA.Player Player, int Expires, uint? ActorId = null);

		sealed class ExpansionRegistry
		{
			public ExpansionSite[] Atlas;
			public readonly Dictionary<(OpenRA.Player Owner, CPos Site), ExpansionClaim> Claims = new();
			public readonly Dictionary<OpenRA.Player, ExpansionBid> Bids = new();
			public int Epoch = -1;

			public bool ClaimedByAlly(OpenRA.Player player, CPos site) =>
				Claims.Keys.Any(k => k.Site == site && (k.Owner == player || k.Owner.IsAlliedWith(player)));

			public void Advance(World world, int now)
			{
				var epoch = now / TeamExpandEpochTicks;
				if (epoch == Epoch)
					return;

				var previousEpoch = Epoch;
				Epoch = epoch;
				foreach (var (site, claim) in Claims.ToArray())
				{
					var holder = claim.ActorId is uint id ? world.GetActorById(id) : null;
					var expired = claim.ActorId.HasValue
						? holder == null || holder.IsDead || !holder.IsInWorld || holder.Owner != claim.Player
						: claim.Expires <= now;
					if (expired || claim.Player.WinState != WinState.Undefined)
						Claims.Remove(site);
				}

				if (previousEpoch + 1 == epoch)
					foreach (var bid in Bids.Values.OrderBy(b => b.Deadline).ThenBy(b => b.Player.ClientIndex).ThenBy(b => b.Player.InternalName, StringComparer.Ordinal))
					{
						if (bid.Player.WinState != WinState.Undefined)
							continue;

						var held = Claims.Values.Count(c => c.Player == bid.Player);
						foreach (var site in bid.Sites)
						{
							if (held >= bid.Limit)
								break;

							if (ClaimedByAlly(bid.Player, site))
								continue;

							Claims.Add((bid.Player, site), new ExpansionClaim(bid.Player, now + 8 * TeamExpandEpochTicks));
							break;
						}
					}

				Bids.Clear();
			}
		}

		AblationLatch ablateExpansionAtlas;
		AblationLatch ablateExpansionForecast;
		AblationLatch ablateExpansionClaim;
		AblationLatch ablateExpansionFloor;
		int expansionForecastTick = -TeamExpandEpochTicks;
		int expansionDeadline = int.MaxValue;
		long expansionPreviousStock;
		readonly HashSet<CPos> expansionObservedCells = new();
		CPos? expansionPendingSite;
		int expansionPendingSince;
		string expansionPendingType;
		readonly Dictionary<uint, CPos> expansionMcvSites = new();
		readonly HashSet<uint> expansionPreexistingMcvs = new();

		bool ExpansionAtlasOn => Info.TeamExpandAtlas && !ablateExpansionAtlas.Off(player, AblationIds.TeamExpandAtlas);
		bool ExpansionForecastOn => ExpansionAtlasOn && Info.TeamExpandForecast && !ablateExpansionForecast.Off(player, AblationIds.TeamExpandForecast);
		bool ExpansionClaimOn => ExpansionAtlasOn && Info.DriveMcvSite && Info.TeamExpandClaim && !ablateExpansionClaim.Off(player, AblationIds.TeamExpandClaim);
		ExpansionRegistry ExpansionClaims => ExpansionRegistries.GetValue(world, _ => new ExpansionRegistry());

		ExpansionSite[] PublicExpansionAtlas()
		{
			var registry = ExpansionClaims;
			if (registry.Atlas != null)
				return registry.Atlas;

			// Map.Resources is the authored layer. The live resource layer is read only for visible cells.
			registry.Atlas = ResourceFields(world.Map.AllCells.Where(c => world.Map.Resources[c].Type != 0))
				.SelectMany(cells => RefineryDemand.Zones(cells, TeamExpandAtlasZoneRadius))
				.Select(cells => new ExpansionSite(RefineryDemand.ZoneAnchor(cells), cells.ToArray()))
				.ToArray();
			return registry.Atlas;
		}

		void ObserveExpansionDepletion(List<CPos> ownRefineries)
		{
			var now = world.WorldTick;
			if (!ExpansionForecastOn || now - expansionForecastTick < TeamExpandEpochTicks || player.Shroud == null)
				return;

			var elapsed = now - expansionForecastTick;
			expansionForecastTick = now;
			var layer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			var values = player.PlayerActor.Info.TraitInfoOrDefault<PlayerResourcesInfo>()?.ResourceValues;
			if (layer == null || values == null || ownRefineries.Count == 0)
			{
				expansionObservedCells.Clear();
				expansionDeadline = int.MaxValue;
				return;
			}

			long stock = 0;
			var observations = new HashSet<CPos>();
			foreach (var site in PublicExpansionAtlas())
			{
				if (!Claimed(site.Center, ownRefineries, Info.ClaimRadiusCells))
					continue;

				foreach (var cell in site.Cells)
				{
					if (!player.Shroud.IsVisible(cell))
						continue;

					observations.Add(cell);
					var res = layer.GetResource(cell);
					if (res.Type != null && values.TryGetValue(res.Type, out var value))
						stock += (long)res.Density * value;
				}
			}

			// Only compare like-for-like observed coverage. Unknown coverage is not exhausted stock.
			var consumed = observations.Count > 0 && expansionObservedCells.SetEquals(observations)
				? expansionPreviousStock - stock : 0;
			expansionDeadline = consumed > 0 && stock <= long.MaxValue / elapsed
				? now + (int)Math.Min(int.MaxValue - now, stock * elapsed / consumed) : int.MaxValue;
			expansionPreviousStock = stock;
			expansionObservedCells.Clear();
			expansionObservedCells.UnionWith(observations);
		}

		bool TryReserveExpansionRequest(int active, ref int targetCount)
		{
			var now = world.WorldTick;
			var registry = ExpansionClaims;
			registry.Advance(world, now);
			var atlas = PublicExpansionAtlas();
			var anchors = TeamExpansionAnchors();
			var limit = MapDiagonalCells() >= Info.TeamExpandLargeMapDiagonal ? Info.TeamExpandLargeClaims : Info.TeamExpandSmallClaims;
			limit = Math.Clamp(limit, 1, 2);

			foreach (var (actorId, site) in expansionMcvSites.ToArray())
			{
				var actor = world.GetActorById(actorId);
				if (actor != null && !actor.IsDead && actor.IsInWorld && actor.Owner == player)
					continue;

				expansionMcvSites.Remove(actorId);
				registry.Claims.Remove((player, site));
			}

			if (expansionPendingSite is CPos pending && now - expansionPendingSince >= 8 * TeamExpandEpochTicks)
			{
				registry.Claims.Remove((player, pending));
				expansionPendingSite = null;
			}

			// The map floor consumes site availability; it does not lift legality or create a second MCV order owner.
			var seats = Math.Max(1, world.Players.Count(p => p.WinState == WinState.Undefined && p.Playable));
			var capacityTarget = 1 + Math.Min(Info.TeamExpandMaxExtraYards, atlas.Length / seats);
			if (Info.TeamExpandCapacityFloor && !ablateExpansionFloor.Off(player, AblationIds.TeamExpandCapacityFloor))
				targetCount = Math.Max(targetCount, capacityTarget);
			var due = expansionDeadline != int.MaxValue && atlas.Any(s =>
				!Claimed(s.Center, anchors, Info.ClaimRadiusCells) &&
				expansionDeadline - now <= TeamExpansionLead(s.Center));
			if (due)
				targetCount = Math.Max(targetCount, active + 1);

			if (expansionPendingSite.HasValue || expansionMcvSites.Count >= limit || active >= targetCount)
				return false;

			if (!registry.Bids.ContainsKey(player))
			{
				var candidates = atlas.Where(s => !Claimed(s.Center, anchors, Info.ClaimRadiusCells) && !registry.ClaimedByAlly(player, s.Center))
					.Select(s => (Site: s, Score: (long)s.Cells.Length * Info.TeamExpandStockWeight
						- (long)NearestOwnBuildingDistance(s.Center) * Info.TeamExpandDistanceWeight
						- (long)threatProviders.MergedThreatAt(s.Center) * Info.TeamExpandThreatWeight))
					.OrderByDescending(s => s.Score).ThenBy(s => s.Site.Center.X).ThenBy(s => s.Site.Center.Y)
					.Take(Info.TeamExpandBidSites).Select(s => s.Site.Center).ToArray();
				registry.Bids.Add(player, new ExpansionBid(player, due ? expansionDeadline : int.MaxValue, limit, candidates));
			}

			return registry.Claims.Any(kv => kv.Value.Player == player && !expansionMcvSites.ContainsValue(kv.Key.Site));
		}

		void CommitExpansionRequest(string mcvType)
		{
			var site = ExpansionClaims.Claims.Where(kv => kv.Value.Player == player && !expansionMcvSites.ContainsValue(kv.Key.Site))
				.OrderBy(kv => kv.Key.Site.X).ThenBy(kv => kv.Key.Site.Y).First();

			expansionPendingSite = site.Key.Site;
			expansionPendingSince = world.WorldTick;
			expansionPendingType = mcvType;
			foreach (var a in world.Actors)
				if (a.Owner == player && constructionMcvTypes.Contains(a.Info.Name))
					expansionPreexistingMcvs.Add(a.ActorID);
		}

		CPos? TeamClaimedMcvSite(Actor mcv, out bool tracked)
		{
			tracked = expansionMcvSites.TryGetValue(mcv.ActorID, out var site);
			if (!tracked && expansionPendingSite is CPos pending && mcv.Info.Name == expansionPendingType
				&& !expansionPreexistingMcvs.Contains(mcv.ActorID))
			{
				site = pending;
				expansionMcvSites.Add(mcv.ActorID, site);
				expansionPendingSite = null;
				tracked = true;
			}

			if (!tracked)
				return null;

			var registry = ExpansionClaims;
			var now = world.WorldTick;
			registry.Advance(world, now);
			if (registry.Claims.TryGetValue((player, site), out var claim) && TeamExpandSiteLegal(mcv, site))
			{
				registry.Claims[(player, site)] = claim with { ActorId = mcv.ActorID };
				return site;
			}

			// Prefer another active claim in this epoch before falling back near an allied base.
			foreach (var alternate in registry.Claims.Keys.Where(k => k.Owner == player && k.Site != site && !expansionMcvSites.ContainsValue(k.Site))
				.OrderBy(k => (k.Site - mcv.Location).LengthSquared).ThenBy(k => k.Site.X).ThenBy(k => k.Site.Y))
			{
				if (expansionPendingSite == alternate.Site || !TeamExpandSiteLegal(mcv, alternate.Site))
					continue;

				expansionMcvSites[mcv.ActorID] = alternate.Site;
				var alternateClaim = registry.Claims[alternate];
				registry.Claims[alternate] = alternateClaim with { ActorId = mcv.ActorID };
				registry.Claims.Remove((player, site));
				return alternate.Site;
			}

			// Never return another player's claimed site. Prefer an accessible anchor on lease loss.
			foreach (var anchor in TeamExpansionAnchors().OrderBy(c => (c - mcv.Location).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y))
				if (TeamExpandSiteLegal(mcv, anchor))
					return anchor;

			TeamExpandFallback(mcv);
			return null;
		}

		long TeamExpansionLead(CPos site) =>
			(long)Math.Max(0, Info.TeamExpandMcvBuildTicks) + Math.Max(0, Info.TeamExpandDeployTicks)
			+ Math.Max(0, Info.TeamExpandRefineryBuildTicks) + Math.Max(0, Info.TeamExpandSafetyMarginTicks)
			+ (long)NearestOwnBuildingDistance(site) * Math.Max(0, Info.TeamExpandTravelTicksPerCell);

		List<CPos> TeamExpansionAnchors()
		{
			var anchors = new List<CPos>(ownYardCells);
			foreach (var actor in world.ActorsHavingTrait<GivesBuildableArea>())
				if (actor.Owner != player && actor.Owner.IsAlliedWith(player) && !actor.IsDead && actor.IsInWorld
					&& constructionYardTypes.Contains(actor.Info.Name) && actor.CanBeViewedByPlayer(player))
					anchors.Add(actor.Location);

			return anchors;
		}

		bool TeamExpandSiteLegal(Actor mcv, CPos site)
		{
			var locomotor = mcv.TraitOrDefault<Mobile>()?.Locomotor;
			return locomotor != null && pathFinder != null
				&& pathFinder.PathMightExistForLocomotorBlockedByImmovable(locomotor, mcv.Location, site)
				&& ScalingDeployCellExists(mcv, site);
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void TeamExpandFallback(Actor mcv) =>
			Log.Write("debug", $"AI ({player.ClientIndex}): team_expand_fallback mcv {mcv.ActorID} tick {world.WorldTick}");
	}
}
