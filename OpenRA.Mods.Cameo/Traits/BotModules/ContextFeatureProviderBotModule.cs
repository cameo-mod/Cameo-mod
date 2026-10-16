#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Read-only seam into <see cref="AiMatchLogWriter"/>'s fog-honest seen buffer: the same
	/// remembered set <see cref="AiMatchLogWriter.ProfileOf"/> already consumes, so this provider
	/// never builds a second hidden-state reader. Implemented by the writer on the world actor
	/// under seam ACK ACK_2026-10-16_context_opponent_seam_devin.md (bound shape, telemetry only,
	/// no consumers); a world actor without the writer trait still resolves to null and every
	/// opponent-side path stays inert.
	/// </summary>
	internal interface IAiSeenProfileSource
	{
		IReadOnlyCollection<ObservedActor> SeenActorsOf(OpenRA.Player observer, OpenRA.Player enemy);
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Public-map + seen-side opponent context feature provider (task 01a11190). TELEMETRY ONLY:",
		"publishes IBotMapFeatures/IBotOpponentFeatures and appends anonymous schema-1 rows to its own",
		"JSONL channel via AiLogFileAppender under the writer's owner election. No orders, no RNG, no",
		"gameplay consumer, no fog access of its own. Inert unless the context_features condition is",
		"granted (BX_context_providers, default off).")]
	public class ContextFeatureProviderBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Append-only anonymous JSONL file under Logs/ written by the elected owner.")]
		public readonly string FileName = "cameo-ai-context.jsonl";

		[Desc("Preferred ground locomotor names; the first locomotor with a passable non-Water terrain speed is the fallback.")]
		public readonly string[] GroundLocomotors = [];

		[Desc("Preferred naval locomotor names; the first locomotor with a passable Water speed is the fallback. No candidate => naval measurements stay missing.")]
		public readonly string[] NavalLocomotors = ["naval"];

		public override object Create(ActorInitializer init) { return new ContextFeatureProviderBotModule(init.Self, this); }
	}

	public class ContextFeatureProviderBotModule : ConditionalTrait<ContextFeatureProviderBotModuleInfo>,
		IBotMapFeatures, IBotOpponentFeatures, IWorldLoaded, IBotTick, IGameOver
	{
		const int MaxPendingRows = 64;
		static readonly CVec[] NeighborDeltas = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];

		// One immutable geometry build per (world, locomotors, resource table) - the same bounded
		// scan every enabled module would otherwise repeat (SharedZoneTopology precedent).
		static readonly ConditionalWeakTable<World, Dictionary<string, BuiltMapGeometry>> SharedGeometry = new();
		static readonly ContextFeatureVector MissingMap = new(ContextFeatureKind.Map);

		sealed class BuiltMapGeometry
		{
			public PublicMapFeatureGraph Graph;
			public Dictionary<CPos, int> IndexOf;
			public int RegionCount;
		}

		sealed class PendingRow
		{
			public string Text;
			public AiLogFileAppender Appender;
			public int NextAttemptTick;
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly List<PendingRow> pendingRows = new();
		readonly List<OpenRA.Player> enemies = new();
		readonly List<OpponentFeatureWindow> windows = new();
		readonly List<ContextFeatureVector> opponentVectors = new();

		BotTakeoverTracker takeover;
		bool eligibleAtWorldLoad;
		IAiSeenProfileSource seenSource;
		ContextFeatureVector mapFeatures;
		int regionCount;

		public ContextFeatureProviderBotModule(Actor self, ContextFeatureProviderBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		public ContextFeatureVector MapFeatures => mapFeatures ?? MissingMap;

		bool IBotOpponentFeatures.TryObservedFeatures(int currentEnemySeat, out ContextFeatureVector features)
		{
			features = null;
			if (currentEnemySeat < 0 || currentEnemySeat >= opponentVectors.Count)
				return false;
			features = opponentVectors[currentEnemySeat];
			return features != null;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			// Same frame-end deferral as TacticalMapBotModule: the one-time geometry scan is paid up
			// front (never mid-game) and only once locomotors and the resource layer exist.
			w.AddFrameEndTask(_ =>
			{
				if (IsTraitDisabled)
					return;
				eligibleAtWorldLoad = AiMatchLogWriter.Eligible(w.Type, w.IsReplay, w.IsLoadingGameSave, host: true);
				if (!eligibleAtWorldLoad)
					return;

				takeover = w.WorldActor.TraitOrDefault<BotTakeoverTracker>();
				seenSource = w.WorldActor.TraitOrDefault<IAiSeenProfileSource>();

				var ground = LocomotorFor(w, Info.GroundLocomotors, IsGroundCapable);
				var naval = LocomotorFor(w, Info.NavalLocomotors, IsNavalCapable);
				var resourceValues = player.PlayerActor.Info.TraitInfoOrDefault<PlayerResourcesInfo>()?.ResourceValues;
				var key = (ground?.Info.Name ?? "-") + "|" + (naval?.Info.Name ?? "-") + "|" + ResourceKey(resourceValues);
				var byKey = SharedGeometry.GetValue(w, _ => new Dictionary<string, BuiltMapGeometry>());
				if (!byKey.TryGetValue(key, out var geometry))
					byKey[key] = geometry = BuildGeometry(w, ground, naval, resourceValues);

				BuildFeatures(w, geometry);
				EnqueueRow(SafeSerialize(MapFeatures));
			});
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (!eligibleAtWorldLoad || IsTraitDisabled)
				return;
			var tick = world.WorldTick;
			if (!IsContextOwner())
				return;
			if (seenSource != null && player.Shroud != null && enemies.Count != 0)
			{
				if (tick % OpponentFeatureWindow.SampleInterval == 0)
					SampleCensuses(tick);
				if (tick % OpponentFeatureWindow.UpdateInterval == 0)
					EmitOpponentRows(tick);
			}

			DrainPending(tick);
		}

		void IGameOver.GameOver(World w)
		{
			if (eligibleAtWorldLoad && IsContextOwner())
				DrainPending(w.WorldTick);
		}

		bool IsContextOwner() => AiMatchLogWriter.OwnerIsLocal(Game.IsHost,
			takeover != null && takeover.Enabled, takeover?.Controller ?? -1, Game.LocalClientId);

		static Locomotor LocomotorFor(World w, IReadOnlyList<string> preferred, Func<Locomotor, bool> capable)
		{
			var all = w.WorldActor.TraitsImplementing<Locomotor>().ToArray();
			var selected = preferred.Count > 0 ? all.FirstOrDefault(l => preferred.Contains(l.Info.Name)) : null;
			return selected ?? all.FirstOrDefault(capable);
		}

		static bool IsGroundCapable(Locomotor l) =>
			l.Info.TerrainSpeeds.Any(kv => kv.Key != "Water" && kv.Value.Cost < short.MaxValue);

		static bool IsNavalCapable(Locomotor l) =>
			l.Info.TerrainSpeeds.TryGetValue("Water", out var speed) && speed.Cost < short.MaxValue;

		static string ResourceKey(IReadOnlyDictionary<string, int> resourceValues) =>
			resourceValues == null ? "-" :
				string.Join(",", resourceValues.OrderBy(kv => kv.Key, StringComparer.Ordinal)
					.Select(kv => kv.Key + "=" + kv.Value));

		static BuiltMapGeometry BuildGeometry(World w, Locomotor ground, Locomotor naval,
			IReadOnlyDictionary<string, int> resourceValues)
		{
			try
			{
				var map = w.Map;
				var positions = map.AllCells.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
				if (positions.Length == 0 || positions.Length > PublicMapFeatureGraph.MaximumCells)
					return null;

				var resourceLayer = w.WorldActor.TraitOrDefault<IResourceLayer>();
				var indexOf = new Dictionary<CPos, int>(positions.Length);
				var cells = new PublicMapFeatureCell[positions.Length];
				var passable = new bool[positions.Length];
				var navalPassable = new bool[positions.Length];
				var width = 0;
				var height = 0;
				for (var i = 0; i < positions.Length; i++)
				{
					var c = positions[i];
					indexOf[c] = i;
					var gp = ground != null && ground.MovementCostForCell(c) != short.MaxValue;
					var np = naval != null && naval.MovementCostForCell(c) != short.MaxValue;
					passable[i] = gp;
					navalPassable[i] = np;
					var resourceValue = 0L;
					var content = resourceLayer?.GetResource(c) ?? default;
					if (resourceValues != null && resourceValues.TryGetValue(content.Type, out var unitValue))
						resourceValue = (long)unitValue * content.Density;
					if (c.X + 1 > width)
						width = c.X + 1;
					if (c.Y + 1 > height)
						height = c.Y + 1;
					cells[i] = new PublicMapFeatureCell(i, c.X, c.Y, gp, np, gp && map.Ramp[c] == 0,
						map.Height[c], resourceValue);
				}

				// Directed 4-neighbour edges; the ctor requires reciprocal links, so both halves of
				// every pair are emitted (cost is the target cell's locomotor cost, -1 when blocked).
				var terrainEdges = new List<PublicMapFeatureEdge>();
				var navalEdges = naval != null ? new List<PublicMapFeatureEdge>() : null;
				for (var i = 0; i < positions.Length; i++)
				{
					var c = positions[i];
					foreach (var d in NeighborDeltas)
					{
						if (!indexOf.TryGetValue(c + d, out var j))
							continue;
						terrainEdges.Add(new PublicMapFeatureEdge(i, j,
							passable[j] ? Math.Max(1, (int)ground.MovementCostForCell(c + d)) : -1));
						navalEdges?.Add(new PublicMapFeatureEdge(i, j,
							navalPassable[j] ? Math.Max(1, (int)naval.MovementCostForCell(c + d)) : -1));
					}
				}

				var graph = new PublicMapFeatureGraph(width, height, cells, terrainEdges, navalEdges);
				return new BuiltMapGeometry
				{
					Graph = graph,
					IndexOf = indexOf,
					RegionCount = Math.Max(1, ((width + 7) / 8) * ((height + 7) / 8)),
				};
			}
			catch
			{
				return null; // Telemetry must never crash the sim; a bad geometry stays all-missing.
			}
		}

		void BuildFeatures(World w, BuiltMapGeometry geometry)
		{
			if (geometry == null)
				return;
			regionCount = geometry.RegionCount;
			var eligible = w.Players.Where(AiMatchLogWriter.IsEligiblePlayer).ToArray();
			var indexOf = geometry.IndexOf;

			// Eligible opponents are frozen as stable seat indices (seat = position in this list).
			foreach (var p in eligible)
			{
				if (p == player || p.AlliedPlayersMask.Overlaps(player.PlayerMask))
					continue;
				enemies.Add(p);
				windows.Add(new OpponentFeatureWindow());
				opponentVectors.Add(null);
			}

			if (!indexOf.TryGetValue(player.HomeLocation, out var ownStart))
				return; // No own anchor: every distance field would be fabricated. Stay all-missing.

			var enemyStarts = new List<int>();
			var allyStarts = new List<int>();
			foreach (var p in eligible)
			{
				if (p == player || p.DisplaySpawnPoint <= 0 ||
					!indexOf.TryGetValue(p.HomeLocation, out var startIndex))
					continue; // DisplaySpawnPoint <= 0: engine-assigned spawn is not public. Missing, not guessed.
				if (p.AlliedPlayersMask.Overlaps(player.PlayerMask))
				{
					if (allyStarts.Count < PublicMapFeatureGraph.MaximumPublicStarts)
						allyStarts.Add(startIndex);
				}
				else if (enemyStarts.Count < PublicMapFeatureGraph.MaximumPublicStarts)
				{
					enemyStarts.Add(startIndex);
				}
			}

			// Team count: union-find over mutual-ally eligible players, world.Players order.
			var parent = Enumerable.Range(0, eligible.Length).ToArray();
			int Find(int x)
			{
				while (parent[x] != x)
				{
					parent[x] = parent[parent[x]];
					x = parent[x];
				}

				return x;
			}

			for (var i = 0; i < eligible.Length; i++)
				for (var j = i + 1; j < eligible.Length; j++)
					if (eligible[i].AlliedPlayersMask.Overlaps(eligible[j].PlayerMask) &&
						eligible[j].AlliedPlayersMask.Overlaps(eligible[i].PlayerMask))
						parent[Find(i)] = Find(j);
			var teamCount = eligible.Select((_, i) => i).Select(Find).Distinct().Count();

			// Fog-honest by ruling (DESIGN 19.1b): spreaders are neutral map actors, public map data.
			var spreaders = 0;
			foreach (var tp in w.ActorsWithTrait<ISeedableResource>())
			{
				if (tp.Actor.IsDead || !tp.Actor.IsInWorld || (tp.Trait is IDisabledTrait d && d.IsTraitDisabled))
					continue;
				spreaders++;
			}

			// POIs: capturable/oil structures owned by non-combatant map sides only. IsTech shares
			// the capturable marker - in this ruleset capturable map buildings are tech buildings.
			var pois = new List<PublicMapFeaturePoi>();
			foreach (var a in w.Actors.OrderBy(x => x.ActorID))
			{
				if (!a.IsInWorld || a.IsDead || AiMatchLogWriter.IsEligiblePlayer(a.Owner) ||
					!a.Info.HasTraitInfo<BuildingInfo>() || !indexOf.TryGetValue(a.Location, out var cellIndex))
					continue;
				var capturable = a.TraitOrDefault<CaptureManager>() != null;
				pois.Add(new PublicMapFeaturePoi(cellIndex, capturable, capturable,
					a.Info.HasTraitInfo<CashTricklerInfo>(),
					Math.Max(0L, a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0)));
			}

			try
			{
				mapFeatures = geometry.Graph.Features(ownStart, enemyStarts, allyStarts,
					eligible.Length > 0 ? eligible.Length : null, teamCount > 0 ? teamCount : null,
					spreaders, pois); // corridors + defence arc stay null => those fields honestly missing.
			}
			catch
			{
				mapFeatures = null;
			}
		}

		void SampleCensuses(int tick)
		{
			for (var i = 0; i < enemies.Count; i++)
			{
				var actors = seenSource.SeenActorsOf(player, enemies[i]);
				if (actors == null)
					continue;
				var profile = AiMatchLogWriter.ProfileOf(actors); // Same 8x8 region bucketing as P0.
				long infantry = 0, vehicle = 0, air = 0, naval = 0, defence = 0, other = 0, economy = 0, production = 0;
				var lastSeen = 0;
				foreach (var a in actors)
				{
					if (a.Combat)
					{
						if (a.Aircraft)
							air += a.Value;
						else
						{
							if (a.Infantry) infantry += a.Value;
							if (a.Vehicle) vehicle += a.Value;
							if (a.Naval) naval += a.Value;
							if (!a.Infantry && !a.Vehicle && !a.Naval) other += a.Value;
						}
					}

					if (a.Defence) defence += a.Value;
					if (a.Harvester || a.Refinery) economy += a.Value;
					if (a.Production) production += a.Value;
					if (a.LastSeenTick > lastSeen)
						lastSeen = a.LastSeenTick;
				}

				var coverage = (int)Math.Min(1000L, profile.KnownRegions * 1000L / regionCount);
				// Artillery stays 0: the observed classifier has no artillery axis - no synthesis.
				try
				{
					windows[i].Add(new OpponentFeatureCensus(tick, lastSeen, actors.Count, coverage,
						Clamp(economy), Clamp(production),
						new[] { Clamp(infantry), Clamp(vehicle), Clamp(air), Clamp(naval), Clamp(defence), 0, Clamp(other) }));
				}
				catch
				{
					// A rejected census skips the sample; telemetry never faults the sim.
				}
			}
		}

		void EmitOpponentRows(int tick)
		{
			for (var i = 0; i < windows.Count; i++)
			{
				if (windows[i].SampleCount == 0)
					continue;
				ContextFeatureVector vector;
				try
				{
					vector = windows[i].Features(tick);
				}
				catch
				{
					continue;
				}

				opponentVectors[i] = vector;
				EnqueueRow(SafeSerialize(vector));
			}
		}

		void EnqueueRow(string text)
		{
			if (text == null)
				return;
			if (pendingRows.Count >= MaxPendingRows)
				pendingRows.RemoveAt(0);
			pendingRows.Add(new PendingRow
			{
				Text = text + "\n",
				Appender = new AiLogFileAppender(Info.FileName),
				NextAttemptTick = world.WorldTick,
			});
		}

		void DrainPending(int tick)
		{
			for (var i = pendingRows.Count - 1; i >= 0; i--)
			{
				var row = pendingRows[i];
				if (tick < row.NextAttemptTick)
					continue;
				var result = row.Appender.TryAppend(row.Text, tick, out var next);
				if (result == AiLogAppendResult.RetryableFailure && !row.Appender.IsTerminal)
					row.NextAttemptTick = next;
				else
					pendingRows.RemoveAt(i); // Appended, or terminally failed: bounded telemetry drops.
			}
		}

		static int Clamp(long value) => value > int.MaxValue ? int.MaxValue : (int)value;

		static string SafeSerialize(ContextFeatureVector vector)
		{
			try
			{
				return ContextFeatureEncoding.Serialize(vector);
			}
			catch
			{
				return null;
			}
		}
	}
}
