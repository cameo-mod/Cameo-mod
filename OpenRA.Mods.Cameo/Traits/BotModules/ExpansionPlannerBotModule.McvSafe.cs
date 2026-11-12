// IMPL02 MCV-SAFE-01: keep expansion MCVs out of enemy defence/artillery range. Reached only through call sites that first build a
// McvSafeContext, which is null unless the CG switch group is on (IBotMcvSafetyPolicy.McvSafeEnabled), so with CG off the planner is FREEZE8.
// Parameters are the named, Range-documented AggTeamMcv* fields, read through the layered resolver (IBotMcvSafetyPolicy.McvSafeParam).
// Decisions are pure and live in McvSafePolicyCA; this file only gathers fog-honest world facts and issues orders.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	public partial class ExpansionPlannerBotModule
	{
		sealed class McvSafeContext
		{
			public readonly IBotMcvSafetyPolicy Policy;
			public readonly int Now;
			public readonly IReadOnlyList<McvThreatZone> Zones;
			public readonly int MaxFieldValue;

			// DIAG-FIX-01 (lazy, once per request): the enemy start candidates and the own bases the enemy-half refusal compares a field against.
			public List<CPos> EnemyStarts;
			public List<CPos> OwnBases;

			public McvSafeContext(IBotMcvSafetyPolicy policy, int now, IReadOnlyList<McvThreatZone> zones, int maxFieldValue)
			{
				Policy = policy;
				Now = now;
				Zones = zones;
				MaxFieldValue = maxFieldValue;
			}

			public int P(string name) => Policy.McvSafeParam(name);
		}

		sealed class McvTrack
		{
			public Actor Actor;
			public CPos LastCell;
			public int Field = -1;
			public int NextAbortTick;
			public CPos? TripStart;
		}

		IBot mcvSafeBot;
		int mcvZonesTick = int.MinValue / 2;
		List<McvThreatZone> mcvZoneCache = new();
		List<CPos> mcvStarts;
		int mcvNextGuardTick;
		readonly List<McvThreatZone> mcvLossZones = new();
		readonly Dictionary<uint, McvTrack> mcvTracks = new();
		readonly Dictionary<string, int> mcvWeaponRangeCache = new();

		const string PMargin = nameof(SquadManagerBotModuleCAInfo.AggTeamMcvDefenceMarginCells);

		/// <summary>The context for one MCV request, or null when MCV-SAFE is off (CG off or no enabled squad manager).</summary>
		McvSafeContext McvSafeBegin()
		{
			var policy = self?.TraitsImplementing<IBotMcvSafetyPolicy>().FirstOrDefault(t => t.McvSafeEnabled);
			if (policy == null)
				return null;

			var now = world.WorldTick;
			return new McvSafeContext(policy, now, McvZones(policy, now), initialCells.Count > 0 ? initialCells.Values.Max() : 0);
		}

		int McvWeaponRangeCells(ActorInfo info)
		{
			if (!mcvWeaponRangeCache.TryGetValue(info.Name, out var cells))
				mcvWeaponRangeCache[info.Name] = cells = (BotUnitProfiles.Get(world.Map.Rules, info, false).MaxRange.Length + AggTeamPolicyCA.CellUnits - 1) / AggTeamPolicyCA.CellUnits;

			return cells;
		}

		/// <summary>The exclusion zones, rebuilt every AggTeamMcvScanIntervalTicks from what this seat can legitimately know.</summary>
		IReadOnlyList<McvThreatZone> McvZones(IBotMcvSafetyPolicy policy, int now)
		{
			var interval = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvScanIntervalTicks));
			if (now - mcvZonesTick < interval)
				return mcvZoneCache;

			mcvZonesTick = now;
			var margin = policy.McvSafeParam(PMargin);
			var artillery = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvArtilleryCells));
			var artilleryMin = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvArtilleryMinRangeCells));
			var artilleryScans = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvArtilleryZoneScans));
			var zones = new List<McvThreatZone>();

			// Remembered and visible enemy defences and producers (the engine's frozen-actor layer already remembers fogged buildings).
			var layer = player.FrozenActorLayer;
			if (layer != null)
				foreach (var fa in layer.FrozenActorsInRegion(world.Map.AllCells, false))
				{
					if (!fa.IsValid || fa.Owner == null || player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy || !fa.Info.HasTraitInfo<BuildingInfo>())
						continue;

					var armed = fa.Info.HasTraitInfo<AttackBaseInfo>();
					var producer = fa.Info.HasTraitInfo<BaseBuildingInfo>()
						|| fa.Info.TraitInfos<ProductionInfo>().Any(p => p.Produces.Any(t => t != "air"));
					if (!armed && !producer)
						continue;

					zones.Add(new McvThreatZone(world.Map.CellContaining(fa.CenterPosition),
						McvSafePolicyCA.ZoneRadius(armed ? McvWeaponRangeCells(fa.Info) : 0, artillery, margin), -1));
				}

			// Visible enemy artillery (long-range mobile units): transient zones that live a few scans.
			foreach (var a in world.ActorsHavingTrait<AttackBase>())
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null || a.Info.HasTraitInfo<BuildingInfo>() || !a.Info.HasTraitInfo<MobileInfo>()
					|| player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || !a.CanBeViewedByPlayer(player))
					continue;

				var range = McvWeaponRangeCells(a.Info);
				if (range >= artilleryMin)
					zones.Add(new McvThreatZone(a.Location, range + Math.Max(0, margin), now + interval * artilleryScans));
			}

			// Every map start location that no own or allied player holds is a suspected enemy start until this seat has explored it (Opus ruling:
			// the map's start points are public, who stands on which is not). The real structures take over once seen.
			var spawn = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvSpawnZoneCells));
			if (spawn > 0)
			{
				mcvStarts ??= world.Map.ActorDefinitions.Where(d => d.Value.Value == "mpspawn")
					.Select(d => new ActorReference(d.Value.Value, d.Value).Get<LocationInit>().Value).ToList();
				var homes = world.Players.Where(p => p == player || player.IsAlliedWith(p)).Select(p => p.HomeLocation);
				foreach (var start in ArmyStagingEval.EnemySpawnCandidates(mcvStarts, homes, policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvSpawnMatchCells))))
				{
					if (!world.Map.Contains(start) || player.Shroud.IsExplored(start))
						continue;

					zones.Add(new McvThreatZone(start, spawn, -1));
				}
			}

			mcvLossZones.RemoveAll(z => z.ExpiryTick >= 0 && now >= z.ExpiryTick);
			zones.AddRange(mcvLossZones);
			mcvZoneCache = zones;
			return zones;
		}

		bool McvSafeSiteAllowed(McvSafeContext ctx, FieldScore f) => !McvSafePolicyCA.InZone(f.Center, ctx.Zones, ctx.Now);

		readonly HashSet<int> mcvEnemyHalfLogged = new();

		/// <summary>
		/// DIAG-FIX-01 (CG only, through the context): the field lies nearer to a suspected enemy start than to any own base. The starts are the
		/// public map start points no own or allied player holds (the same candidates as the spawn zones; DIAG-FIX-02 drops an explored start with no enemy
		/// building seen within SpawnMatchCells of it, an unused start; who stands on which is not read); the bases are this seat's own base buildings
		/// and its base centre. No own base known = nothing is refused.
		/// </summary>
		bool McvSafeSiteInEnemyHalf(McvSafeContext ctx, FieldScore f)
		{
			if (ctx.EnemyStarts == null)
			{
				mcvStarts ??= world.Map.ActorDefinitions.Where(d => d.Value.Value == "mpspawn")
					.Select(d => new ActorReference(d.Value.Value, d.Value).Get<LocationInit>().Value).ToList();
				var homes = world.Players.Where(p => p == player || player.IsAlliedWith(p)).Select(p => p.HomeLocation);
				var matchCells = ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvSpawnMatchCells));
				var candidates = ArmyStagingEval.EnemySpawnCandidates(mcvStarts, homes, matchCells);

				// DIAG-FIX-02: a start this seat has explored with no enemy building seen within SpawnMatchCells of it is empty (an unused start on a
				// map with more starts than players), not an enemy start. Same public information as the zones: the shroud and the frozen-actor layer.
				bool Explored(CPos c) => world.Map.Contains(c) && player.Shroud.IsExplored(c);
				ctx.EnemyStarts = McvSafePolicyCA.PruneEmptyExploredStarts(candidates, Explored,
					candidates.Any(Explored) ? McvSeenEnemyBuildingCells() : new List<CPos>(), matchCells);
				ctx.OwnBases = world.Actors.Where(a => a.Owner == player && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<BaseBuildingInfo>())
					.Select(a => a.Location).ToList();
				if (baseCenter.HasValue)
					ctx.OwnBases.Add(baseCenter.Value);
			}

			if (!McvSafePolicyCA.NearerToEnemyStart(f.Center, ctx.EnemyStarts, ctx.OwnBases))
				return false;

			if (mcvEnemyHalfLogged.Add(f.Index))
				Log.Write("debug", $"AI ({player.ClientIndex}): MS-6 field {f.Index} at {f.Center} is nearer to a suspected enemy start than to any own base ({ctx.EnemyStarts.Count} starts, {ctx.OwnBases.Count} own bases), refused at tick {ctx.Now}");

			return true;
		}

		/// <summary>DIAG-FIX-02: the cells of the enemy buildings this seat has seen (the frozen-actor layer already remembers fogged ones), as the zones read them.</summary>
		List<CPos> McvSeenEnemyBuildingCells()
		{
			var cells = new List<CPos>();
			var layer = player.FrozenActorLayer;
			if (layer == null)
				return cells;

			foreach (var fa in layer.FrozenActorsInRegion(world.Map.AllCells, false))
				if (fa.IsValid && fa.Owner != null && player.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy && fa.Info.HasTraitInfo<BuildingInfo>())
					cells.Add(world.Map.CellContaining(fa.CenterPosition));

			return cells;
		}

		/// <summary>DIAG-FIX-01: the zone state for the EX-3 line (CG only): live zones, own loss zones, suspected enemy starts, and the chosen field's and the MCV's distance to the nearest zone edge (0 = inside, -1 = no zone).</summary>
		string McvSafeZoneNote(McvSafeContext ctx, Actor mcv, FieldScore f)
		{
			var live = 0;
			foreach (var z in ctx.Zones)
				if (z.ExpiryTick < 0 || ctx.Now < z.ExpiryTick)
					live++;

			var fieldEdge = McvSafePolicyCA.EdgeDistanceCells(f.Center, ctx.Zones, ctx.Now);
			var mcvEdge = McvSafePolicyCA.EdgeDistanceCells(mcv.Location, ctx.Zones, ctx.Now);
			return $", zones {live} (loss {mcvLossZones.Count}, starts {ctx.EnemyStarts?.Count ?? -1}), field edge {(fieldEdge == int.MaxValue ? -1 : fieldEdge)}, mcv edge {(mcvEdge == int.MaxValue ? -1 : mcvEdge)}";
		}

		/// <summary>Forward-contain bonus x full-field bonus for a site that passed the exclusion (a multiplier on the field score).</summary>
		double McvSafeSiteFactor(McvSafeContext ctx, FieldScore f)
		{
			var edge = McvSafePolicyCA.EdgeDistanceCells(f.Center, ctx.Zones, ctx.Now);
			var forward = McvSafePolicyCA.ForwardFactorPermille(edge, ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvForwardBandCells)),
				ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvForwardBonusPercent)));
			var full = McvSafePolicyCA.FullFieldFactorPermille(f.Value, ctx.MaxFieldValue, ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvFullFieldPercent)),
				ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvFullFieldBonusPercent)));
			return forward / McvSafePolicyCA.PermilleUnit * full / McvSafePolicyCA.PermilleUnit;
		}

		/// <summary>
		/// A zone-avoiding route from the MCV to the site: the engine's pathfinder with a per-cell cost inside the zones. False when there is no path or
		/// the best path still re-enters a zone after leaving it (the site is then not safely reachable for this MCV). requireSafe = false accepts any path.
		/// </summary>
		bool McvSafeRoute(McvSafeContext ctx, Actor mcv, CPos site, bool requireSafe, out List<CPos> waypoints)
		{
			waypoints = null;
			var mobile = mcv.TraitOrDefault<Mobile>();
			if (mobile?.PathFinder is not PathFinder pf)
				return true;

			var cost = ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvPathCostPerCell));
			int Cost(CPos c) => McvSafePolicyCA.PathCost(c, ctx.Zones, ctx.Now, cost);
			var path = pf.FindPathToTargetCell(mcv, new[] { mobile.ToCell }, site, BlockedByActor.Immovable, Cost, laneBias: false);
			if (path == null || path.Count == 0)
				return !requireSafe;

			path.Reverse();
			if (requireSafe && McvSafePolicyCA.PathCrossesZone(path, ctx.Zones, ctx.Now))
				return false;

			waypoints = McvSafePolicyCA.Waypoints(path, ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvWaypoints)));
			return true;
		}

		/// <summary>Queues the safe waypoints; the engine's own queued Move to the deploy cell and DeployTransform follow them.</summary>
		void McvSafeIssue(Actor mcv, int fieldIndex, List<CPos> waypoints)
		{
			if (!mcvTracks.TryGetValue(mcv.ActorID, out var track))
				mcvTracks[mcv.ActorID] = track = new McvTrack { Actor = mcv, LastCell = mcv.Location };

			track.Field = fieldIndex;
			if (mcvSafeBot == null || waypoints == null || waypoints.Count == 0)
				return;

			var queued = false;
			foreach (var wp in waypoints)
			{
				mcvSafeBot.QueueOrder(new Order("Move", mcv, OpenRA.Traits.Target.FromCell(world, wp), queued));
				queued = true;
			}

			Log.Write("debug", $"AI ({player.ClientIndex}): MS-1 MCV {mcv.ActorID} routed around enemy zones with {waypoints.Count} waypoints, last {waypoints[^1]}, at tick {world.WorldTick}");
		}

		/// <summary>
		/// No safe far field is left: an expansion MCV deploys at the main base (the engine deploy search takes the cells around it) instead of the
		/// engine's own choice, which knows nothing about enemy ranges. Null for other roles or when the fallback is off.
		/// </summary>
		CPos? McvSafeFallback(McvSafeContext ctx, Actor mcv)
		{
			if (ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvFallbackToBase)) == 0 || !baseCenter.HasValue || RoleOf(mcv.Info) != McvRole.Expansion)
				return null;

			McvSafeRoute(ctx, mcv, baseCenter.Value, false, out var route);
			McvSafeIssue(mcv, -1, route);
			Log.Write("debug", $"AI ({player.ClientIndex}): MS-2 MCV {mcv.Info.Name} {mcv.ActorID} at {mcv.Location}: no safe far field, deploys at the base {baseCenter.Value} at tick {world.WorldTick}");
			return baseCenter;
		}

		int McvNearestVisibleThreatCells(Actor mcv, int threatCells)
		{
			var best = -1;
			foreach (var a in world.FindActorsInCircle(mcv.CenterPosition, WDist.FromCells(threatCells)))
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null || !a.Info.HasTraitInfo<AttackBaseInfo>() || a.Info.HasTraitInfo<HarvesterInfo>()
					|| player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || !a.CanBeViewedByPlayer(player))
					continue;

				var d = (a.Location - mcv.Location).Length;
				if (best < 0 || d < best)
					best = d;
			}

			return best;
		}

		/// <summary>The en-route guard and the loss memory: runs every AggTeamMcvGuardIntervalTicks, only with CG on.</summary>
		void McvSafeTick(IBot bot)
		{
			mcvSafeBot = bot;
			var policy = self?.TraitsImplementing<IBotMcvSafetyPolicy>().FirstOrDefault(t => t.McvSafeEnabled);
			if (policy == null)
			{
				mcvTracks.Clear();
				return;
			}

			var now = world.WorldTick;
			if (now < mcvNextGuardTick)
				return;

			mcvNextGuardTick = now + policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvGuardIntervalTicks));
			var zones = McvZones(policy, now);
			var abortMargin = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvAbortMarginCells));
			var threatCells = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvAbortThreatCells));
			var seen = new HashSet<uint>();

			foreach (var mcv in world.ActorsHavingTrait<Transforms>().Where(a => a.Owner == player && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<MobileInfo>()
				&& RoleOf(a.Info) != McvRole.BaseBuilding).ToList())
			{
				seen.Add(mcv.ActorID);
				if (!mcvTracks.TryGetValue(mcv.ActorID, out var track))
					mcvTracks[mcv.ActorID] = track = new McvTrack();

				track.Actor = mcv;
				track.LastCell = mcv.Location;
				if (mcv.IsIdle)
				{
					track.TripStart = null;
					continue;
				}

				track.TripStart ??= mcv.Location;
				if (now < track.NextAbortTick)
					continue;

				var threat = McvNearestVisibleThreatCells(mcv, threatCells);
				var tripZones = McvSafePolicyCA.ZonesNotContainingStart(zones, track.TripStart.Value, now, abortMargin);
				if (!McvSafePolicyCA.ShouldAbort(mcv.Location, tripZones, now, abortMargin, threat, threatCells))
					continue;

				track.NextAbortTick = now + policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvAbortCooldownTicks));
				if (!McvSafeAbortHome(bot, policy, zones, now, mcv))
					bot.QueueOrder(new Order("Stop", mcv, false));

				if (track.Field >= 0)
					parkedUntil[track.Field] = now + Info.ParkTicks;

				inflightMcvSites.Remove(mcv.ActorID);
				Log.Write("debug", $"AI ({player.ClientIndex}): MS-3 MCV {mcv.ActorID} at {mcv.Location} aborted its trip (zone/threat {threat}), field {track.Field} parked, at tick {now}");
			}

			foreach (var id in mcvTracks.Keys.ToList())
			{
				if (seen.Contains(id))
					continue;

				var track = mcvTracks[id];
				mcvTracks.Remove(id);
				McvSafeVanished(policy, track, now);
			}
		}

		/// <summary>
		/// DIAG-FIX-01: an aborted MCV leaves the threat for the main base instead of standing still beside it (a bare Stop left it there until the
		/// next hand-out). The route is the zone-costed pathfinder route (any path accepted); with no route the Move goes straight to the base cell.
		/// False (the caller then Stops, as before) when no base centre is known.
		/// </summary>
		bool McvSafeAbortHome(IBot bot, IBotMcvSafetyPolicy policy, IReadOnlyList<McvThreatZone> zones, int now, Actor mcv)
		{
			if (!baseCenter.HasValue)
				return false;

			var ctx = new McvSafeContext(policy, now, zones, 0);
			McvSafeRoute(ctx, mcv, baseCenter.Value, false, out var route);
			var queued = false;
			if (route != null)
				foreach (var wp in route)
				{
					bot.QueueOrder(new Order("Move", mcv, OpenRA.Traits.Target.FromCell(world, wp), queued));
					queued = true;
				}

			if (!queued)
				bot.QueueOrder(new Order("Move", mcv, OpenRA.Traits.Target.FromCell(world, baseCenter.Value), false));

			Log.Write("debug", $"AI ({player.ClientIndex}): MS-5 MCV {mcv.ActorID} at {mcv.Location} aborted: moves to the base {baseCenter.Value} ({route?.Count ?? 0} waypoints) at tick {now}");
			return true;
		}

		/// <summary>An MCV left the world: a deploy leaves an own yard/refinery beside it, a kill does not (a loaded MCV is alive and not a loss).</summary>
		void McvSafeVanished(IBotMcvSafetyPolicy policy, McvTrack track, int now)
		{
			if (track.Actor == null || !track.Actor.IsDead)
				return;

			var yards = world.Actors.Where(a => a.Owner == player && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<BuildingInfo>()
				&& (a.Info.HasTraitInfo<BaseBuildingInfo>() || a.Info.HasTraitInfo<RefineryInfo>() || constructionYardTypes.Contains(a.Info.Name)))
				.Select(a => a.Location).ToList();
			if (!McvSafePolicyCA.WasLost(track.LastCell, yards, policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvLossMatchCells))))
				return;

			mcvLossZones.Add(new McvThreatZone(track.LastCell, policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvLossZoneCells)),
				now + policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvLossZoneTicks))));
			mcvZonesTick = int.MinValue / 2;
			if (track.Field >= 0)
				parkedUntil[track.Field] = now + Info.ParkTicks;

			Log.Write("debug", $"AI ({player.ClientIndex}): MS-4 MCV lost near {track.LastCell} (field {track.Field}), loss zone and field park set, at tick {now}");
		}
	}
}
