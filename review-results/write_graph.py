from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
h=(r/'OpenRA.Mods.Cameo.Test/ZoneTopologyTest.cs').read_text().split('using ')[0]
(r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs').write_text(h+'''using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

	/// <summary>Immutable public geometry. No World, Player, Actor, PathFinder or RNG dependency.</summary>
	public sealed class PublicMapFeatureGraph
	{
		public const int MaximumCells = 262144;
		readonly PublicMapFeatureCell[] cells;
		readonly PublicMapFeatureEdge[][] edges;
		readonly Dictionary<int, int> offsets;
		readonly int width;
		readonly int height;

		public PublicMapFeatureGraph(int width, int height, IEnumerable<PublicMapFeatureCell> terrain,
			IEnumerable<PublicMapFeatureEdge> terrainEdges)
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
			var bySource = ordered.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.ToArray());
			edges = cells.Select(c => bySource.TryGetValue(c.Index, out var list) ? list : []).ToArray();
			if (edges.Any(e => e.Length > 8))
				throw new ArgumentException("Too many geometric neighbors.", nameof(terrainEdges));
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

			var result = new SortedDictionary<int, int>();
			for (var i = 0; i < cells.Length; i++)
				result.Add(cells[i].Index, steps[i]);
			return new ReadOnlyDictionary<int, int>(result);
		}

		public ContextFeatureVector Features(int ownStart, IEnumerable<int> publicEnemyStarts,
			IEnumerable<int> publicAllyStarts, int? playerCount = null, int? teamCount = null, int? spreaderCount = null)
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

			var enemy = publicEnemyStarts.Distinct().Order().Select(s => distances[s]).Where(d => d >= 0).ToArray();
			Distance("nearest_ally_path_cells", publicAllyStarts.Distinct().Order().Select(s => distances[s]));
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
			var fieldDistances = resourceFields.Select(f => f.Select(i => distances[cells[i].Index]).Where(d => d >= 0)
				.DefaultIfEmpty(-1).Min()).ToArray();
			Set("resource_field_count", resourceFields.Count);
			Set("reachable_field_count", fieldDistances.Count(d => d >= 0));
			Set("component_count", Components(c => c.GroundPassable).Count);
			var reachableFields = fieldDistances.Where(d => d >= 0).Order().ToArray();
			if (reachableFields.Length != 0)
			{
				Set("resource_mean_field_path_cells", ContextFeatureEncoding.Saturate(reachableFields.Sum(d => (long)d) / reachableFields.Length));
				Set("resource_field_path_spread_permille", ContextFeatureEncoding.Ratio(reachableFields[^1] - reachableFields[0], Math.Max(1, reachableFields[0])));
				if (enemy.Length != 0)
					Set("start_to_field_ratio_permille", ContextFeatureEncoding.Ratio(reachableFields[0], enemy.Min()));
			}

			// Fields requiring a public start association, POI classifier or public corridor adapter remain missing.
			return new(ContextFeatureKind.Map, fields);
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
						if (found[to] || !includes(cells[to]) || (cells[from].GroundPassable && cells[to].GroundPassable && edge.Cost < 0))
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
''')
