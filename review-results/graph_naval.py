from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/ContextFeatureEncoding.cs'
s=p.read_text().replace('public static int Ratio(', '''public static BigInteger IntegerSquareRoot(BigInteger value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));
			if (value <= 1)
				return value;
			var root = BigInteger.One << (int)((value.GetBitLength() + 1) / 2);
			while (true)
			{
				var next = (root + value / root) / 2;
				if (next >= root)
					return root;
				root = next;
			}
		}

		public static int Ratio(''')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs'
s=p.read_text().replace('public readonly record struct PublicMapFeatureCorridor(int CellIndex, int WidthCells);', '''public readonly record struct PublicMapFeatureCorridor(int CellIndex, int WidthCells);
	public readonly record struct PublicMapFeatureDefenceArc(int CoveredDoorSectors, int TotalSectors);''')
s=s.replace('readonly PublicMapFeatureEdge[][] edges;', 'readonly PublicMapFeatureEdge[][] edges;\n\t\treadonly PublicMapFeatureEdge[][] navalEdges;')
s=s.replace('IEnumerable<PublicMapFeatureEdge> terrainEdges)', 'IEnumerable<PublicMapFeatureEdge> terrainEdges, IEnumerable<PublicMapFeatureEdge> navalTerrainEdges = null)')
s=s.replace('if (edges.Any(e => e.Length > 8))\n\t\t\t\tthrow new ArgumentException("Too many geometric neighbors.", nameof(terrainEdges));', '''if (edges.Any(e => e.Length > 8))
				throw new ArgumentException("Too many geometric neighbors.", nameof(terrainEdges));
			if (navalTerrainEdges != null)
			{
				var naval = navalTerrainEdges.Take(MaximumCells * 8 + 1).OrderBy(e => e.From).ThenBy(e => e.To).ToArray();
				if (naval.Length > MaximumCells * 8 || naval.Any(e => !links.Contains((e.From, e.To)) || e.Cost == 0 || e.Cost < -1) ||
					naval.Select(e => (e.From, e.To)).Distinct().Count() != naval.Length)
					throw new ArgumentException("Invalid public naval edge.", nameof(navalTerrainEdges));
				var byNavalSource = naval.GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.ToArray());
				navalEdges = cells.Select(c => byNavalSource.TryGetValue(c.Index, out var list) ? list : []).ToArray();
			}''')
s=s.replace('IEnumerable<PublicMapFeaturePoi> publicPois = null, IEnumerable<PublicMapFeatureCorridor> publicCorridors = null)', 'IEnumerable<PublicMapFeaturePoi> publicPois = null, IEnumerable<PublicMapFeatureCorridor> publicCorridors = null,\n\t\t\tPublicMapFeatureDefenceArc? publicDefenceArc = null)')
s=s.replace('var enemyStarts = publicEnemyStarts.Take(MaximumPublicStarts + 1).Distinct().Order().ToArray();\n\t\t\tvar allyStarts = publicAllyStarts.Take(MaximumPublicStarts + 1).Distinct().Order().ToArray();', '''var enemyInput = publicEnemyStarts.Take(MaximumPublicStarts + 1).ToArray();
			var allyInput = publicAllyStarts.Take(MaximumPublicStarts + 1).ToArray();
			if (enemyInput.Length > MaximumPublicStarts || allyInput.Length > MaximumPublicStarts)
				throw new ArgumentException("Public start count exceeds budget.");
			var enemyStarts = enemyInput.Distinct().Order().ToArray();
			var allyStarts = allyInput.Distinct().Order().ToArray();''')
s=s.replace('Distance("nearest_poi_path_cells", pois.Select(p => distances[p.CellIndex]));', '''Distance("nearest_poi_path_cells", pois.Select(p => distances[p.CellIndex]));
				if (enemy.Length != 0 && pois.Any(p => distances[p.CellIndex] >= 0))
					Set("start_to_poi_ratio_permille", ContextFeatureEncoding.Ratio(pois.Select(p => distances[p.CellIndex])
						.Where(d => d >= 0).Min(), enemy.Min()));''')
s=s.replace('// Naval connectivity and defence projection require their dedicated public geometry adapters.', '''var ownShore = ShoreCells(distances);
			var waterApproaches = cells.Where(c => c.NavalPassable && distances[c.Index] >= 0).Select(c => distances[c.Index]).ToList();
			foreach (var c in cells.Where(c => distances[c.Index] >= 0))
				if (edges[offsets[c.Index]].Any(e => cells[offsets[e.To]].NavalPassable))
					waterApproaches.Add(distances[c.Index] + 1);
			Distance("nearest_water_path_cells", waterApproaches);
			if (navalEdges != null)
			{
				var waterReach = NavalReach(ownShore);
				Set("own_start_water_access_permille", ContextFeatureEncoding.Ratio(waterReach.Count, cells.Count(c => c.NavalPassable)));
				if (enemy.Length != 0)
				{
					var routes = enemyStarts.Count(start => ShoreCells(Distances(start)).Any(waterReach.Contains));
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
			}''')
idx=s.index('\t\t/// <summary>Exact vertex cut')
s=s[:idx]+'''		int[] ShoreCells(IReadOnlyDictionary<int, int> groundDistances)
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

''' + s[idx:]
p.write_text(s)
