from pathlib import Path
p=Path(r'C:/cameo-wt/codex-context-providers/OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs')
s=p.read_text().replace('public readonly record struct PublicMapFeatureEdge(int From, int To, int Cost);', '''public readonly record struct PublicMapFeatureEdge(int From, int To, int Cost);
	public readonly record struct PublicMapFeaturePoi(int CellIndex, bool IsTech, bool IsCapturable, bool IsOil, long PublicValue);
	public readonly record struct PublicMapFeatureCorridor(int CellIndex, int WidthCells);''')
s=s.replace('int? playerCount = null, int? teamCount = null, int? spreaderCount = null)', '''int? playerCount = null, int? teamCount = null, int? spreaderCount = null,
			IEnumerable<PublicMapFeaturePoi> publicPois = null, IEnumerable<PublicMapFeatureCorridor> publicCorridors = null)''')
s=s.replace('Set("playable_cells", cells.Length);', '''Set("playable_cells", cells.Length);
			var positions = cells.Select(c => (c.X, c.Y)).ToHashSet();
			Set("perimeter_cells", cells.Count(c => !positions.Contains((c.X - 1, c.Y)) || !positions.Contains((c.X + 1, c.Y)) ||
				!positions.Contains((c.X, c.Y - 1)) || !positions.Contains((c.X, c.Y + 1))));''')
s=s.replace('// POI, naval route, defence and corridor fields need their public classifier/geometry adapters.', '''var centre = cells.Where(c => c.GroundPassable).OrderBy(c => Math.Abs((long)c.X * 2 - (width - 1)) +
				Math.Abs((long)c.Y * 2 - (height - 1))).ThenBy(c => c.Index).FirstOrDefault();
			if (centre.GroundPassable && distances[centre.Index] > 1)
				Set("minimum_cut_cells", MinimumVertexCut(ownStart, centre.Index).Value);

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
				Distance("nearest_tech_path_cells", pois.Where(p => p.IsTech).Select(p => distances[p.CellIndex]));
				Distance("nearest_oil_path_cells", pois.Where(p => p.IsOil).Select(p => distances[p.CellIndex]));
				Distance("nearest_capturable_path_cells", pois.Where(p => p.IsCapturable).Select(p => distances[p.CellIndex]));
				if (playerCount.HasValue)
					Set("poi_value_per_player", ContextFeatureEncoding.Saturate(pois.Aggregate(BigInteger.Zero, (a, p) => a + p.PublicValue) / playerCount.Value));
				if (enemy.Length != 0)
				{
					var enemyPaths = enemyStarts.Select(Distances).ToArray();
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

			// Naval connectivity and defence projection require their dedicated public geometry adapters.''')
idx=s.index('\t\tint GroundComponentCount()')
s=s[:idx]+'''		/// <summary>Exact vertex cut with unit interior capacities. Adjacent endpoints have no finite interior cut.</summary>
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

''' + s[idx:]
p.write_text(s)
