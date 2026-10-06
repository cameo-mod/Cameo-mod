#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// Cell indices and positions exist only in this transient geometry input, never in a vector or learned key.
	public readonly record struct PublicMapFeatureCell(int Index, int X, int Y, bool GroundPassable,
		bool NavalPassable, bool Buildable, int Height, long ResourceValue);
	// Cost is the selected locomotor's integer terrain-only cost. -1 is a geometric edge blocked for it.
	public readonly record struct PublicMapFeatureEdge(int From, int To, int Cost);
	public readonly record struct PublicMapFeaturePoi(int CellIndex, bool IsTech, bool IsCapturable, bool IsOil, long PublicValue);
	public readonly record struct PublicMapFeatureCorridor(int CellIndex, int WidthCells);
	public readonly record struct PublicMapFeatureDefenceArc(int CoveredDoorSectors, int TotalSectors);

	/// <summary>Immutable public geometry. No World, Player, Actor, PathFinder or RNG dependency.</summary>
	public sealed class PublicMapFeatureGraph
	{
		public const int MaximumCells = 262144;
		public const int MaximumPublicStarts = 64;
		readonly PublicMapFeatureCell[] cells;
		readonly PublicMapFeatureEdge[][] edges;
		readonly PublicMapFeatureEdge[][] navalEdges;
		readonly Dictionary<int, int> offsets;
		readonly int width;
		readonly int height;

		public PublicMapFeatureGraph(int width, int height, IEnumerable<PublicMapFeatureCell> terrain,
			IEnumerable<PublicMapFeatureEdge> terrainEdges, IEnumerable<PublicMapFeatureEdge> navalTerrainEdges = null)
		{
			if (width <= 0 || height <= 0)
				throw new ArgumentOutOfRangeException(nameof(width));
			this.width = width;
			this.height = height;
			// Materialization is capped even for an adversarial iterator.
			cells = terrain.Take(MaximumCells + 1).OrderBy(c => c.Index).ToArray();
			if (cells.Length == 0 || cells.Length > MaximumCells || cells.Any(c => c.Index < 0 || c.X < 0 ||
				c.X >= width || c.Y < 0 || c.Y >= height || c.ResourceValue < 0) ||
				cells.Select(c => (c.X, c.Y)).Distinct().Count() != cells.Length)
				throw new ArgumentException("Invalid public map geometry.", nameof(terrain));
			offsets = cells.Select((c, i) => (c.Index, i)).ToDictionary(c => c.Index, c => c.i);
			var ordered = terrainEdges.Take(MaximumCells * 8 + 1).OrderBy(e => e.From).ThenBy(e => e.To).ToArray();
			if (ordered.Length > MaximumCells * 8 || ordered.Any(e => !offsets.ContainsKey(e.From) ||
				!offsets.ContainsKey(e.To) || e.From == e.To || e.Cost == 0 || e.Cost < -1) ||
				ordered.Select(e => (e.From, e.To)).Distinct().Count() != ordered.Length)
				throw new ArgumentException("Invalid public terrain edge.", nameof(terrainEdges));
			var links = ordered.Select(e => (e.From, e.To)).ToHashSet();
			if (ordered.Any(e => !links.Contains((e.To, e.From))))
				throw new ArgumentException("Geometry edges must be reciprocal.", nameof(terrainEdges));
			var bySource = ordered.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.ToArray());
			edges = cells.Select(c => bySource.TryGetValue(c.Index, out var list) ? list : []).ToArray();
			if (edges.Any(e => e.Length > 8))
				throw new ArgumentException("Too many geometric neighbors.", nameof(terrainEdges));
			if (navalTerrainEdges != null)
			{
				var naval = navalTerrainEdges.Take(MaximumCells * 8 + 1).OrderBy(e => e.From).ThenBy(e => e.To).ToArray();
				if (naval.Length > MaximumCells * 8 || naval.Any(e => !links.Contains((e.From, e.To)) || e.Cost == 0 || e.Cost < -1) ||
					naval.Select(e => (e.From, e.To)).Distinct().Count() != naval.Length)
					throw new ArgumentException("Invalid public naval edge.", nameof(navalTerrainEdges));
				var byNavalSource = naval.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.ToArray());
				navalEdges = cells.Select(c => byNavalSource.TryGetValue(c.Index, out var list) ? list : []).ToArray();
			}
		}

		/// <summary>Weighted shortest terrain path, ties by edge count then cell index; no actor blockers.</summary>
		public IReadOnlyDictionary<int, int> Distances(int origin)
		{
			if (!offsets.TryGetValue(origin, out var start))
				throw new ArgumentOutOfRangeException(nameof(origin));
			var costs = Enumerable.Repeat(long.MaxValue, cells.Length).ToArray();
			var steps = Enumerable.Repeat(-1, cells.Length).ToArray();
			var queue = new PriorityQueue<int, (long Cost, int Steps, int Cell)>();
			if (cells[start].GroundPassable)
			{
				costs[start] = 0;
				steps[start] = 0;
				queue.Enqueue(start, (0, 0, origin));
			}

			while (queue.TryDequeue(out var from, out var priority))
			{
				if (priority.Cost != costs[from] || priority.Steps != steps[from])
					continue;
				foreach (var edge in edges[from])
				{
					var to = offsets[edge.To];
					if (edge.Cost < 0 || !cells[to].GroundPassable)
						continue;
					var cost = costs[from] + edge.Cost;
					var distance = steps[from] + 1;
					if (cost > costs[to] || (cost == costs[to] && distance >= steps[to]))
						continue;
					costs[to] = cost;
					steps[to] = distance;
					queue.Enqueue(to, (cost, distance, edge.To));
				}
			}

			return new TerrainDistances(cells, offsets, steps);
		}

		public ContextFeatureVector Features(int ownStart, IEnumerable<int> publicEnemyStarts,
			IEnumerable<int> publicAllyStarts, int? playerCount = null, int? teamCount = null, int? spreaderCount = null,
			IEnumerable<PublicMapFeaturePoi> publicPois = null, IEnumerable<PublicMapFeatureCorridor> publicCorridors = null,
			PublicMapFeatureDefenceArc? publicDefenceArc = null)
		{
			if (playerCount < 1 || teamCount < 1 || spreaderCount < 0)
				throw new ArgumentOutOfRangeException(nameof(playerCount));
			var distances = Distances(ownStart);
			var fields = new SortedDictionary<string, ContextFeatureMeasurement>(StringComparer.Ordinal);
			void Set(string name, int value) => fields.Add(name, ContextFeatureMeasurement.Observed(value));
			void Distance(string name, IEnumerable<int> values)
			{
				var reachable = values.Where(d => d >= 0).ToArray();
				if (reachable.Length != 0)
					Set(name, reachable.Min());
			}

			Set("width_cells", width);
			Set("height_cells", height);
			Set("aspect_permille", ContextFeatureEncoding.Ratio(Math.Max(width, height), Math.Min(width, height)));
			Set("playable_cells", cells.Length);
			var positions = cells.Select(c => (c.X, c.Y)).ToHashSet();
			Set("perimeter_cells", cells.Count(c => !positions.Contains((c.X - 1, c.Y)) || !positions.Contains((c.X + 1, c.Y)) ||
				!positions.Contains((c.X, c.Y - 1)) || !positions.Contains((c.X, c.Y + 1))));
			Set("blocked_share_permille", ContextFeatureEncoding.Ratio(cells.Count(c => !c.GroundPassable), cells.Length));
			Set("water_share_permille", ContextFeatureEncoding.Ratio(cells.Count(c => c.NavalPassable), cells.Length));
			Set("elevation_spread", ContextFeatureEncoding.Saturate((BigInteger)cells.Max(c => c.Height) - cells.Min(c => c.Height)));
			Set("own_start_degree", edges[offsets[ownStart]].Count(e => e.Cost > 0 && cells[offsets[e.To]].GroundPassable));
			Set("openness_permille", ContextFeatureEncoding.Ratio(distances.Values.Count(d => d >= 0), (long)width * height));
			if (playerCount.HasValue)
				Set("player_count", playerCount.Value);
			if (teamCount.HasValue)
				Set("team_count", teamCount.Value);
			if (spreaderCount.HasValue)
				Set("spreader_count", spreaderCount.Value);

			var enemyInput = publicEnemyStarts.Take(MaximumPublicStarts + 1).ToArray();
			var allyInput = publicAllyStarts.Take(MaximumPublicStarts + 1).ToArray();
			if (enemyInput.Length > MaximumPublicStarts || allyInput.Length > MaximumPublicStarts)
				throw new ArgumentException("Public start count exceeds budget.");
			var enemyStarts = enemyInput.Distinct().Order().ToArray();
			var allyStarts = allyInput.Distinct().Order().ToArray();
			if (enemyStarts.Length > MaximumPublicStarts || allyStarts.Length > MaximumPublicStarts ||
				enemyStarts.Contains(ownStart) || allyStarts.Contains(ownStart) || enemyStarts.Intersect(allyStarts).Any())
				throw new ArgumentException("Invalid public start associations.");
			var enemy = enemyStarts.Select(s => distances[s]).Where(d => d >= 0).ToArray();
			var enemyPaths = enemyStarts.Select(Distances).ToArray();
			Distance("nearest_ally_path_cells", allyStarts.Select(s => distances[s]));
			if (enemy.Length != 0)
			{
				Set("closest_enemy_path_cells", enemy.Min());
				Set("furthest_enemy_path_cells", enemy.Max());
				Set("enemy_path_spread_permille", ContextFeatureEncoding.Ratio(enemy.Max() - enemy.Min(), Math.Max(1, enemy.Min())));
			}

			var resources = cells.Where(c => c.ResourceValue > 0).ToArray();
			var totalValue = resources.Aggregate(BigInteger.Zero, (a, b) => a + b.ResourceValue);
			Set("total_resource_value", ContextFeatureEncoding.Saturate(totalValue));
			Set("resource_value_per_playable_cell_milli", ContextFeatureEncoding.Saturate(totalValue * 1000 / cells.Length));
			if (playerCount.HasValue)
				Set("resource_value_per_player", ContextFeatureEncoding.Saturate(totalValue / playerCount.Value));
			Distance("nearest_resource_path_cells", resources.Select(c => distances[c.Index]));
			var resourceFields = Components(c => c.ResourceValue > 0);
			if (totalValue > 0)
			{
				void ResourceShare(string name, Func<PublicMapFeatureCell, bool> includes) =>
					Set(name, ContextFeatureEncoding.Saturate(resources.Where(includes).Aggregate(BigInteger.Zero,
						(a, b) => a + b.ResourceValue) * 1000 / totalValue));
				// Normalized quarter-board bands: middle half on both axes is centre;
				// border quarter on either axis is edge, and on both axes is corner.
				bool NearX(PublicMapFeatureCell c) => (long)Math.Min(c.X, width - 1 - c.X) * 4 < width;
				bool NearY(PublicMapFeatureCell c) => (long)Math.Min(c.Y, height - 1 - c.Y) * 4 < height;
				ResourceShare("resource_centre_permille", c => !NearX(c) && !NearY(c));
				ResourceShare("resource_edge_permille", c => NearX(c) || NearY(c));
				ResourceShare("resource_corner_permille", c => NearX(c) && NearY(c));
				var largestField = resourceFields.Max(f => f.Aggregate(BigInteger.Zero, (a, b) => a + cells[b].ResourceValue));
				Set("resource_cluster_permille", ContextFeatureEncoding.Saturate(largestField * 1000 / totalValue));
				if (enemy.Length != 0)
					ResourceShare("resource_near_spawn_permille", c => distances[c.Index] >= 0 && (long)distances[c.Index] * 4 <= enemy.Min());
			}

			var fieldDistances = resourceFields.Select(f => f.Select(i => distances[cells[i].Index]).Where(d => d >= 0)
				.DefaultIfEmpty(-1).Min()).ToArray();
			Set("resource_field_count", resourceFields.Count);
			Set("reachable_field_count", fieldDistances.Count(d => d >= 0));
			Set("component_count", GroundComponentCount());
			var reachableFields = fieldDistances.Where(d => d >= 0).Order().ToArray();
			if (reachableFields.Length != 0)
			{
				Set("resource_mean_field_path_cells", ContextFeatureEncoding.Saturate(reachableFields.Sum(d => (long)d) / reachableFields.Length));
				Set("resource_field_path_spread_permille", ContextFeatureEncoding.Ratio(reachableFields[^1] - reachableFields[0], Math.Max(1, reachableFields[0])));
				if (reachableFields.Length > 1)
					Set("nearest_expansion_path_cells", reachableFields[1]);
				if (enemy.Length != 0)
				{
					Set("start_to_field_ratio_permille", ContextFeatureEncoding.Ratio(reachableFields[0], enemy.Min()));
					if (reachableFields.Length > 1)
						Set("expansion_to_enemy_ratio_permille", ContextFeatureEncoding.Ratio(reachableFields[1], enemy.Min()));
				}
			}

			if (enemy.Length != 0)
			{
				var nearStart = cells.Where(c => distances[c.Index] >= 0 && (long)distances[c.Index] * 4 <= enemy.Min()).ToArray();
				if (nearStart.Length != 0)
					Set("buildable_near_start_permille", ContextFeatureEncoding.Ratio(nearStart.Count(c => c.Buildable), nearStart.Length));
				// Compare public shortest paths only. No actual enemy spawn is inferred when its assignment is private.
				bool Contested(PublicMapFeatureCell c) => distances[c.Index] >= 0 && enemyPaths.Any(path =>
					path[c.Index] >= 0 && (long)Math.Abs(distances[c.Index] - path[c.Index]) * 10 <= Math.Max(distances[c.Index], path[c.Index]));
				if (totalValue > 0)
					Set("resource_contested_permille", ContextFeatureEncoding.Saturate(resources.Where(Contested)
						.Aggregate(BigInteger.Zero, (a, b) => a + b.ResourceValue) * 1000 / totalValue));
				var frontier = cells.Where(Contested).ToArray();
				if (frontier.Length != 0)
				{
					Set("buildable_frontier_permille", ContextFeatureEncoding.Ratio(frontier.Count(c => c.Buildable), frontier.Length));
					var frontierPaths = frontier.Select(c => distances[c.Index]).ToArray();
					Set("frontier_path_spread_permille", ContextFeatureEncoding.Ratio(frontierPaths.Max() - frontierPaths.Min(), Math.Max(1, frontierPaths.Min())));
				}
			}

			var centre = cells.Where(c => c.GroundPassable).OrderBy(c => Math.Abs((long)c.X * 2 - (width - 1)) +
				Math.Abs((long)c.Y * 2 - (height - 1))).ThenBy(c => c.Index).FirstOrDefault();
			if (centre.GroundPassable && distances[centre.Index] > 1)
			{
				var cut = MinimumVertexCut(ownStart, centre.Index);
				if (cut.HasValue)
					Set("minimum_cut_cells", cut.Value);
			}

			if (enemy.Length != 0)
			{
				var origin = cells[offsets[ownStart]];
				var detours = enemyStarts.Where(i => distances[i] >= 0).Select(i =>
				{
					var dest = cells[offsets[i]];
					var lowerBound = Math.Max(Math.Abs(origin.X - dest.X), Math.Abs(origin.Y - dest.Y));
					return ContextFeatureEncoding.Ratio(distances[i], lowerBound);
				}).Order().ToArray();
				Set("route_detour_permille", ContextFeatureEncoding.Saturate(((long)detours[(detours.Length - 1) / 2] + detours[detours.Length / 2]) / 2));
			}

			if (publicPois != null)
			{
				var pois = publicPois.Take(MaximumCells + 1).OrderBy(p => p.CellIndex).ThenBy(p => p.PublicValue).ToArray();
				if (pois.Length > MaximumCells || pois.Any(p => !offsets.ContainsKey(p.CellIndex) || p.PublicValue < 0))
					throw new ArgumentException("Invalid public POI geometry.", nameof(publicPois));
				Distance("nearest_poi_path_cells", pois.Select(p => distances[p.CellIndex]));
				if (enemy.Length != 0 && pois.Any(p => distances[p.CellIndex] >= 0))
					Set("start_to_poi_ratio_permille", ContextFeatureEncoding.Ratio(pois.Select(p => distances[p.CellIndex])
						.Where(d => d >= 0).Min(), enemy.Min()));
				Distance("nearest_tech_path_cells", pois.Where(p => p.IsTech).Select(p => distances[p.CellIndex]));
				Distance("nearest_oil_path_cells", pois.Where(p => p.IsOil).Select(p => distances[p.CellIndex]));
				Distance("nearest_capturable_path_cells", pois.Where(p => p.IsCapturable).Select(p => distances[p.CellIndex]));
				if (playerCount.HasValue)
					Set("poi_value_per_player", ContextFeatureEncoding.Saturate(pois.Aggregate(BigInteger.Zero, (a, p) => a + p.PublicValue) / playerCount.Value));
				if (enemy.Length != 0)
				{
						var contested = pois.Count(p => distances[p.CellIndex] >= 0 && enemyPaths.Any(path => path[p.CellIndex] >= 0 &&
						(long)Math.Abs(distances[p.CellIndex] - path[p.CellIndex]) * 10 <= Math.Max(distances[p.CellIndex], path[p.CellIndex])));
					Set("poi_contested_permille", ContextFeatureEncoding.Ratio(contested, pois.Length));
				}
			}

			if (publicCorridors != null)
			{
				var corridors = publicCorridors.Take(MaximumCells + 1).OrderBy(c => c.CellIndex).ThenBy(c => c.WidthCells).ToArray();
				if (corridors.Length > MaximumCells || corridors.Any(c => !offsets.ContainsKey(c.CellIndex) || c.WidthCells < 1))
					throw new ArgumentException("Invalid public corridor geometry.", nameof(publicCorridors));
				Set("chokepoint_count", corridors.Length);
				Distance("nearest_chokepoint_path_cells", corridors.Select(c => distances[c.CellIndex]));
				if (corridors.Length != 0)
				{
					var widths = corridors.Select(c => c.WidthCells).Order().ToArray();
					Set("chokepoint_width_min_cells", widths[0]);
					Set("chokepoint_width_mean_cells", ContextFeatureEncoding.Saturate(widths.Sum(w => (long)w) / widths.Length));
					Set("chokepoint_width_p90_cells", widths[(widths.Length * 9 + 9) / 10 - 1]);
				}
			}

			var ownShore = ShoreCells(distances);
			var waterApproaches = cells.Where(c => c.NavalPassable && distances[c.Index] >= 0).Select(c => distances[c.Index]).ToList();
			foreach (var c in cells.Where(c => distances[c.Index] >= 0))
				if (edges[offsets[c.Index]].Any(e => cells[offsets[e.To]].NavalPassable))
					waterApproaches.Add(distances[c.Index] + 1);
			Distance("nearest_water_path_cells", waterApproaches);
			if (navalEdges != null)
			{
				var waterReach = NavalReach(ownShore);
				Set("own_start_water_access_permille", ContextFeatureEncoding.Ratio(waterReach.Count, cells.Count(c => c.NavalPassable)));
				if (enemyStarts.Length != 0)
				{
					var routes = enemyPaths.Count(path => ShoreCells(path).Any(waterReach.Contains));
					Set("naval_route_share_permille", ContextFeatureEncoding.Ratio(routes, enemyStarts.Length));
				}
			}

			if (enemy.Length != 0)
			{
				var nearestStart = enemyStarts.Where(i => distances[i] >= 0).OrderBy(i => distances[i]).ThenBy(i => i).First();
				var origin = cells[offsets[ownStart]];
				var dest = cells[offsets[nearestStart]];
				BigInteger dx = dest.X - origin.X, dy = dest.Y - origin.Y;
				var squaredLength = dx * dx + dy * dy;
				if (squaredLength > 0)
				{
					var behind = cells.Where(c => distances[c.Index] >= 0).Aggregate(BigInteger.Zero,
						(max, c) => BigInteger.Max(max, -((c.X - origin.X) * dx + (c.Y - origin.Y) * dy)));
					Set("backline_depth_cells", ContextFeatureEncoding.Saturate(ContextFeatureEncoding.IntegerSquareRoot(behind * behind / squaredLength)));
				}
			}

			if (publicDefenceArc.HasValue)
			{
				var arc = publicDefenceArc.Value;
				if (arc.TotalSectors < 1 || arc.CoveredDoorSectors < 0 || arc.CoveredDoorSectors > arc.TotalSectors)
					throw new ArgumentException("Invalid public defence arc.", nameof(publicDefenceArc));
				Set("defence_arc_permille", ContextFeatureEncoding.Ratio(arc.CoveredDoorSectors, arc.TotalSectors));
			}
			return new(ContextFeatureKind.Map, fields);
		}

		sealed class TerrainDistances : IReadOnlyDictionary<int, int>
		{
			readonly PublicMapFeatureCell[] terrain;
			readonly Dictionary<int, int> indices;
			readonly int[] distances;
			public TerrainDistances(PublicMapFeatureCell[] terrain, Dictionary<int, int> indices, int[] distances)
			{
				this.terrain = terrain;
				this.indices = indices;
				this.distances = distances;
			}

			public int this[int key] => distances[indices[key]];
			public int Count => distances.Length;
			public IEnumerable<int> Keys => terrain.Select(c => c.Index);
			public IEnumerable<int> Values => distances.Select(d => d);
			public bool ContainsKey(int key) => indices.ContainsKey(key);
			public bool TryGetValue(int key, out int value)
			{
				if (indices.TryGetValue(key, out var index))
				{
					value = distances[index];
					return true;
				}

				value = default;
				return false;
			}

			public IEnumerator<KeyValuePair<int, int>> GetEnumerator()
			{
				for (var i = 0; i < terrain.Length; i++)
					yield return new(terrain[i].Index, distances[i]);
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		int[] ShoreCells(IReadOnlyDictionary<int, int> groundDistances)
		{
			var shore = new SortedSet<int>();
			foreach (var cell in cells)
			{
				if (groundDistances[cell.Index] < 0)
					continue;
				if (cell.NavalPassable)
					shore.Add(cell.Index);
				foreach (var edge in edges[offsets[cell.Index]])
					if (cells[offsets[edge.To]].NavalPassable)
						shore.Add(edge.To);
			}

			return shore.ToArray();
		}

		HashSet<int> NavalReach(IEnumerable<int> shore)
		{
			var reached = new HashSet<int>();
			var queue = new Queue<int>();
			foreach (var index in shore.Order())
				if (reached.Add(index))
					queue.Enqueue(index);
			while (queue.TryDequeue(out var from))
			{
				foreach (var edge in navalEdges[offsets[from]])
					if (edge.Cost > 0 && cells[offsets[edge.To]].NavalPassable && reached.Add(edge.To))
						queue.Enqueue(edge.To);
			}

			return reached;
		}

		/// <summary>Exact vertex cut with unit interior capacities. Adjacent endpoints have no finite interior cut.</summary>
		public int? MinimumVertexCut(int source, int target)
		{
			var from = offsets[source];
			var to = offsets[target];
			if (!cells[from].GroundPassable || !cells[to].GroundPassable || from == to ||
				edges[from].Any(e => e.To == target && e.Cost > 0))
				return null;
			var sourceNode = from * 2 + 1;
			var targetNode = to * 2;
			var vertexFlow = new int[cells.Length];
			var flow = new Dictionary<(int From, int To), int>();
			var parent = new int[cells.Length * 2];
			var queue = new Queue<int>();
			var paths = 0;
			while (true)
			{
				Array.Fill(parent, -1);
				parent[sourceNode] = sourceNode;
				queue.Clear();
				queue.Enqueue(sourceNode);
				void Visit(int previous, int next)
				{
					if (parent[next] >= 0)
						return;
					parent[next] = previous;
					queue.Enqueue(next);
				}

				while (parent[targetNode] < 0 && queue.TryDequeue(out var node))
				{
					var cell = node / 2;
					if (node % 2 == 0)
					{
						if (vertexFlow[cell] == 0)
							Visit(node, node + 1);
						foreach (var edge in edges[cell])
						{
							var neighbor = offsets[edge.To];
							if (flow.GetValueOrDefault((neighbor, cell)) > 0)
								Visit(node, neighbor * 2 + 1);
						}
					}
					else
					{
						if (vertexFlow[cell] > 0)
							Visit(node, node - 1);
						foreach (var edge in edges[cell])
						{
							var neighbor = offsets[edge.To];
							if (edge.Cost > 0 && cells[neighbor].GroundPassable)
								Visit(node, neighbor * 2);
						}
					}
				}

				if (parent[targetNode] < 0)
					return paths;
				for (var node = targetNode; node != sourceNode; node = parent[node])
				{
					var previous = parent[node];
					var a = previous / 2;
					var b = node / 2;
					if (a == b)
						vertexFlow[a] += previous % 2 == 0 ? 1 : -1;
					else if (previous % 2 == 1)
						flow[(a, b)] = flow.GetValueOrDefault((a, b)) + 1;
					else
						flow[(b, a)] = flow.GetValueOrDefault((b, a)) - 1;
				}

				paths++;
			}
		}

		int GroundComponentCount()
		{
			// Strong components handle one-way terrain edges without seed-order artifacts.
			var visited = new bool[cells.Length];
			var finished = new List<int>(cells.Length);
			var stack = new Stack<(int Cell, bool Finish)>();
			for (var seed = 0; seed < cells.Length; seed++)
			{
				if (!cells[seed].GroundPassable || visited[seed])
					continue;
				stack.Push((seed, false));
				while (stack.TryPop(out var item))
				{
					if (item.Finish)
					{
						finished.Add(item.Cell);
						continue;
					}

					if (visited[item.Cell])
						continue;
					visited[item.Cell] = true;
					stack.Push((item.Cell, true));
					for (var i = edges[item.Cell].Length - 1; i >= 0; i--)
					{
						var edge = edges[item.Cell][i];
						var to = offsets[edge.To];
						if (edge.Cost > 0 && cells[to].GroundPassable && !visited[to])
							stack.Push((to, false));
					}
				}
			}

			Array.Clear(visited);
			var components = 0;
			var queue = new Queue<int>();
			for (var i = finished.Count - 1; i >= 0; i--)
			{
				var seed = finished[i];
				if (visited[seed])
					continue;
				components++;
				visited[seed] = true;
				queue.Enqueue(seed);
				while (queue.TryDequeue(out var from))
				{
					foreach (var neighbor in edges[from])
					{
						var to = offsets[neighbor.To];
						if (visited[to] || !cells[to].GroundPassable ||
							!edges[to].Any(reverse => reverse.To == cells[from].Index && reverse.Cost > 0))
							continue;
						visited[to] = true;
						queue.Enqueue(to);
					}
				}
			}

			return components;
		}

		List<int[]> Components(Func<PublicMapFeatureCell, bool> includes)
		{
			var found = new bool[cells.Length];
			var result = new List<int[]>();
			for (var seed = 0; seed < cells.Length; seed++)
			{
				if (found[seed] || !includes(cells[seed]))
					continue;
				var queue = new Queue<int>();
				var component = new List<int>();
				found[seed] = true;
				queue.Enqueue(seed);
				while (queue.TryDequeue(out var from))
				{
					component.Add(from);
					foreach (var edge in edges[from])
					{
						var to = offsets[edge.To];
						if (found[to] || !includes(cells[to]))
							continue;
						found[to] = true;
						queue.Enqueue(to);
					}
				}

				result.Add(component.ToArray());
			}

			return result;
		}
	}
}
