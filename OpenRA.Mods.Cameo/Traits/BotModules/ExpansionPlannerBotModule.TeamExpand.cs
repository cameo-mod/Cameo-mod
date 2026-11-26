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
using System.Text.Json;
using OpenRA.Primitives;
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

		readonly record struct ExpansionSite(CPos Center, CPos[] Cells, int Spreaders);
		readonly record struct ExpansionBid(OpenRA.Player Player, int Deadline, int Limit, CPos[] Sites);
		readonly record struct ExpansionClaim(OpenRA.Player Player, int Expires, uint? ActorId = null);
		const string ExpansionSaveKey = "TeamExpansionV2";
		const int ExpansionSaveVersion = 2;
		internal sealed record SavedClaim(int X, int Y, int Expires, uint? ActorId);
		internal sealed record SavedMcv(uint ActorId, SavedCell Site);
		internal sealed record SavedCell(int X, int Y)
		{
			public CPos ToCell() => new(X, Y);
		}

		/// <summary>
		/// One expansion MCV request waiting for its unit, in request order (FIFO). ProducerId is the factory the request was issued to
		/// when the requester knows it (null = unknown). Plain ints so the record round-trips through JSON.
		/// RESERVED: ProducerId is never filled today. IBotRequestUnitProduction carries no producer/lane info, so every request binds by
		/// the FIFO fallback (oldest of the same type). It stays reserved until ENG-MCV-DEFER-01 (deferred engine MCV work) gives lane info.
		/// </summary>
		internal sealed record PendingExpansion(int X, int Y, int Since, string Type, uint? ProducerId = null)
		{
			public CPos Cell() => new(X, Y);
		}

		// Version 2: the pending request is a FIFO list. Key and version changed with it; the V1 shape never left a draft (every switch default off).
		internal sealed record SavedExpansion(int Version, int Epoch, SavedClaim[] Claims, SavedMcv[] Mcvs, SavedCell[] BidSites, int BidDeadline,
			int BidLimit, PendingExpansion[] Pending, int ForecastTick, int Deadline, long Stock, SavedCell[] Observed);

		/// <summary>The save text: base64 of the JSON of one planner's state. A damaged or foreign text decodes to null, never throws.</summary>
		internal static class ExpansionSave
		{
			public static string Encode(SavedExpansion state) => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(state));

			public static SavedExpansion Decode(string text)
			{
				if (string.IsNullOrEmpty(text))
					return null;

				try
				{
					var state = JsonSerializer.Deserialize<SavedExpansion>(Convert.FromBase64String(text));
					if (state == null || state.Version != ExpansionSaveVersion)
						return null;

					return state with
					{
						Claims = state.Claims ?? [],
						Mcvs = state.Mcvs ?? [],
						BidSites = state.BidSites ?? [],
						Pending = state.Pending ?? [],
						Observed = state.Observed ?? [],
					};
				}
				catch (Exception e) when (e is FormatException or JsonException or NotSupportedException or ArgumentException)
				{
					return null;
				}
			}
		}

		/// <summary>
		/// FIFO binding of a produced MCV to an expansion request. Only an MCV that answers a request binds: with no live construction yard the
		/// unit is a recovery or bootstrap MCV and never binds (recovery keeps priority); crate and mission MCVs never raise a production event.
		/// A request that recorded its factory binds first on that factory; otherwise the oldest request of the same type.
		/// </summary>
		internal static class ExpansionRequestBinder
		{
			public static int Match(IReadOnlyList<PendingExpansion> pending, string producedType, uint producerId, bool hasLiveYard)
			{
				if (!hasLiveYard || producedType == null)
					return -1;

				for (var i = 0; i < pending.Count; i++)
					if (pending[i].Type == producedType && pending[i].ProducerId == producerId)
						return i;

				for (var i = 0; i < pending.Count; i++)
					if (pending[i].Type == producedType)
						return i;

				return -1;
			}
		}

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
		readonly List<PendingExpansion> expansionPending = new();
		readonly Dictionary<uint, CPos> expansionMcvSites = new();
		CPos[] expansionBidSites = [];

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
			var spreaders = world.Map.ActorDefinitions
				.Where(d => resourceMap != null && resourceMap.Info.ResourceCreatorTypes.Contains(d.Value.Value))
				.Select(d => new ActorReference(d.Value.Value, d.Value).Get<LocationInit>().Value)
				.Where(c => world.Map.Contains(c))
				.ToHashSet();
			var authored = world.Map.AllCells.Where(c => world.Map.Resources[c].Type != 0).Concat(spreaders);
			registry.Atlas = ResourceFields(authored)
				.SelectMany(cells => RefineryDemand.Zones(cells, TeamExpandAtlasZoneRadius))
				.Select(cells => new ExpansionSite(RefineryDemand.ZoneAnchor(cells), cells.ToArray(), cells.Count(spreaders.Contains)))
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

			for (var i = expansionPending.Count - 1; i >= 0; i--)
			{
				if (now - expansionPending[i].Since < 8 * TeamExpandEpochTicks)
					continue;

				registry.Claims.Remove((player, expansionPending[i].Cell()));
				expansionPending.RemoveAt(i);
			}

			// Recovery keeps priority: with no live construction yard the MCV module's own recovery owns the next MCV, and no expansion request is raised.
			if (!HasLiveConstructionYard())
				return false;

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

			if (expansionPending.Count > 0 || expansionMcvSites.Count >= limit || active >= targetCount)
				return false;

			if (!registry.Bids.ContainsKey(player))
			{
				var candidates = RankExpansionSites(registry, anchors);
				expansionBidSites = candidates;
				registry.Bids.Add(player, new ExpansionBid(player, due ? expansionDeadline : int.MaxValue, limit, candidates));
			}

			return UnboundClaimSites(registry).Any();
		}

		IEnumerable<CPos> UnboundClaimSites(ExpansionRegistry registry) =>
			registry.Claims.Where(kv => kv.Value.Player == player && !expansionMcvSites.ContainsValue(kv.Key.Site)
					&& !expansionPending.Exists(p => p.Cell() == kv.Key.Site))
				.Select(kv => kv.Key.Site).OrderBy(c => c.X).ThenBy(c => c.Y);

		bool HasLiveConstructionYard()
		{
			foreach (var actor in world.ActorsHavingTrait<GivesBuildableArea>())
				if (actor.Owner == player && actor.IsInWorld && !actor.IsDead && constructionYardTypes.Contains(actor.Info.Name))
					return true;

			return false;
		}

		CPos[] RankExpansionSites(ExpansionRegistry registry, List<CPos> anchors)
		{
			var enemyBases = world.ActorsHavingTrait<GivesBuildableArea>()
				.Where(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy
					&& a.IsInWorld && !a.IsDead && a.CanBeViewedByPlayer(player))
				.Select(a => a.Location).ToArray();
			var resourceInfo = world.WorldActor.TraitOrDefault<IResourceLayer>()?.Info;
			var resourceValues = player.PlayerActor.Info.TraitInfoOrDefault<PlayerResourcesInfo>()?.ResourceValues;
			var authoredValues = new Dictionary<byte, int>();
			if (resourceInfo != null && resourceValues != null)
				foreach (var (type, value) in resourceValues)
					if (resourceInfo.TryGetResourceIndex(type, out var index))
						authoredValues[index] = Math.Max(authoredValues.GetValueOrDefault(index), value);

			var mapScale = Math.Max(1, MapDiagonalCells());
			var rankedAnchors = anchors.OrderBy(c => c.X).ThenBy(c => c.Y).ToArray();
			return PublicExpansionAtlas().Where(s => !Claimed(s.Center, anchors, Info.ClaimRadiusCells) && !registry.ClaimedByAlly(player, s.Center))
				.Select(s => (Site: s, Score: s.Cells.Sum(c => (long)authoredValues.GetValueOrDefault(world.Map.Resources[c].Type)) * Info.TeamExpandStockWeight
					+ (long)s.Spreaders * Info.TeamExpandRegrowthWeight
					- (long)NearestOwnBuildingDistance(s.Center) * Info.TeamExpandDistanceWeight * 100 / mapScale
					- (long)NearestExpansionAnchorDistance(s.Center, anchors) * Info.TeamExpandAnchorDistanceWeight * 100 / mapScale
					- (enemyBases.Length == 0 ? 0 : (long)Math.Max(0, Info.ClaimRadiusCells - NearestExpansionAnchorDistance(s.Center, enemyBases))
						* Info.TeamExpandEnemyProximityWeight)
					- (long)ExpansionApproachThreat(s.Center, rankedAnchors) * Info.TeamExpandThreatWeight
					- (long)threatProviders.MergedThreatAt(s.Center) * Info.TeamExpandContestedWeight))
				.OrderByDescending(s => s.Score).ThenBy(s => s.Site.Center.X).ThenBy(s => s.Site.Center.Y)
				.Take(Info.TeamExpandBidSites).Select(s => s.Site.Center).ToArray();
		}

		int ExpansionApproachThreat(CPos site, CPos[] anchors)
		{
			if (anchors.Length == 0)
				return 0;

			var nearest = anchors.MinBy(c => (c - site).LengthSquared);
			// A bounded corridor proxy based only on the fog-filtered threat cache.
			var early = new CPos((3 * nearest.X + site.X) / 4, (3 * nearest.Y + site.Y) / 4);
			var late = new CPos((nearest.X + 3 * site.X) / 4, (nearest.Y + 3 * site.Y) / 4);
			return Math.Max(threatProviders.MergedThreatAt(early), threatProviders.MergedThreatAt(late));
		}

		void CommitExpansionRequest(string mcvType)
		{
			var site = UnboundClaimSites(ExpansionClaims).First();
			expansionPending.Add(new PendingExpansion(site.X, site.Y, world.WorldTick, mcvType));
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (world.IsLoadingGameSave || expansionPending.Count == 0 || produced.Owner != player || producer.Owner != player)
				return;

			var index = ExpansionRequestBinder.Match(expansionPending, produced.Info.Name, producer.ActorID, HasLiveConstructionYard());
			if (index < 0)
				return;

			var site = expansionPending[index].Cell();
			expansionPending.RemoveAt(index);
			var registry = ExpansionClaims;
			if (registry.Claims.TryGetValue((player, site), out var claim) && !claim.ActorId.HasValue)
			{
				registry.Claims[(player, site)] = claim with { ActorId = produced.ActorID };
				expansionMcvSites[produced.ActorID] = site;
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			// Any of the three switches keeps estimator state, so any of them saves it (the forecast window must not be lost when claims are off).
			if (!ExpansionAtlasOn)
				return null;

			var registry = ExpansionClaims;
			var claims = registry.Claims.Where(kv => kv.Key.Owner == player)
				.OrderBy(kv => kv.Key.Site.X).ThenBy(kv => kv.Key.Site.Y)
				.Select(kv => new SavedClaim(kv.Key.Site.X, kv.Key.Site.Y, kv.Value.Expires, kv.Value.ActorId)).ToArray();
			var mcvs = expansionMcvSites.OrderBy(kv => kv.Key)
				.Select(kv => new SavedMcv(kv.Key, new SavedCell(kv.Value.X, kv.Value.Y))).ToArray();
			registry.Bids.TryGetValue(player, out var bid);
			var state = new SavedExpansion(ExpansionSaveVersion, registry.Epoch, claims, mcvs, SaveCells(expansionBidSites),
				bid.Deadline, bid.Limit, expansionPending.ToArray(), expansionForecastTick, expansionDeadline, expansionPreviousStock,
				SaveCells(expansionObservedCells.OrderBy(c => c.X).ThenBy(c => c.Y)));
			return [new MiniYamlNode(ExpansionSaveKey, ExpansionSave.Encode(state))];
		}

		static SavedCell[] SaveCells(IEnumerable<CPos> cells) => cells.Select(c => new SavedCell(c.X, c.Y)).ToArray();

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (world.IsReplay || !ExpansionAtlasOn || data.NodeWithKeyOrDefault(ExpansionSaveKey) is not MiniYamlNode node)
				return;

			// A damaged or other-version text restores nothing: the planner starts clean and re-claims.
			var state = ExpansionSave.Decode(node.Value.Value);
			if (state == null)
				return;

			var registry = ExpansionClaims;

			// The claim generation (epoch) is shared by every planner of the world: never move it backwards.
			registry.Epoch = Math.Max(registry.Epoch, state.Epoch);
			expansionBidSites = state.BidSites.Select(c => c.ToCell()).ToArray();
			if (state.BidLimit > 0)
				registry.Bids[player] = new ExpansionBid(player, state.BidDeadline, state.BidLimit, expansionBidSites);

			foreach (var claim in state.Claims)
				registry.Claims[(player, new CPos(claim.X, claim.Y))] = new ExpansionClaim(player, claim.Expires, claim.ActorId);

			expansionMcvSites.Clear();
			foreach (var mcv in state.Mcvs)
				expansionMcvSites[mcv.ActorId] = mcv.Site.ToCell();

			expansionPending.Clear();
			expansionPending.AddRange(state.Pending);
			expansionForecastTick = state.ForecastTick;
			expansionDeadline = state.Deadline;
			expansionPreviousStock = state.Stock;
			expansionObservedCells.Clear();
			expansionObservedCells.UnionWith(state.Observed.Select(c => c.ToCell()));
		}

		CPos? TeamClaimedMcvSite(Actor mcv, out bool tracked)
		{
			tracked = expansionMcvSites.TryGetValue(mcv.ActorID, out var site);
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
				if (expansionPending.Exists(p => p.Cell() == alternate.Site) || !TeamExpandSiteLegal(mcv, alternate.Site))
					continue;

				expansionMcvSites[mcv.ActorID] = alternate.Site;
				var alternateClaim = registry.Claims[alternate];
				registry.Claims[alternate] = alternateClaim with { ActorId = mcv.ActorID };
				registry.Claims.Remove((player, site));
				return alternate.Site;
			}

			// Re-rank with current anchors and observations before giving up the team lease.
			var anchors = TeamExpansionAnchors();
			foreach (var candidate in RankExpansionSites(registry, anchors))
			{
				if (registry.ClaimedByAlly(player, candidate) || Claimed(candidate, anchors, Info.ClaimRadiusCells)
					|| !TeamExpandSiteLegal(mcv, candidate))
					continue;

				registry.Claims.Remove((player, site));
				registry.Claims.Add((player, candidate), new ExpansionClaim(player, now, mcv.ActorID));
				expansionMcvSites[mcv.ActorID] = candidate;
				return candidate;
			}

			// Never return another player's claimed site. Prefer an accessible anchor on lease loss.
			foreach (var anchor in anchors.OrderBy(c => (c - mcv.Location).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y))
				if (TeamExpandSiteLegal(mcv, anchor))
					return anchor;

			TeamExpandFallback(mcv);
			return null;
		}

		static int NearestExpansionAnchorDistance(CPos site, IReadOnlyList<CPos> anchors)
		{
			var distance = int.MaxValue;
			foreach (var anchor in anchors)
				distance = Math.Min(distance, (anchor - site).Length);

			return distance == int.MaxValue ? 0 : distance;
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
