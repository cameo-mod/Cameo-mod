from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/OpponentFeatureWindow.cs'
p.write_text(p.read_text().replace('rememberedRoleValues.ToArray()', 'rememberedRoleValues.Take(8).ToArray()'))
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs'
s=p.read_text().replace('public const int MaximumCells = 262144;', 'public const int MaximumCells = 262144;\n\t\tpublic const int MaximumPublicStarts = 64;')
s=s.replace('var enemyStarts = publicEnemyStarts.Distinct().Order().ToArray();', '''var enemyStarts = publicEnemyStarts.Take(MaximumPublicStarts + 1).Distinct().Order().ToArray();
			var allyStarts = publicAllyStarts.Take(MaximumPublicStarts + 1).Distinct().Order().ToArray();
			if (enemyStarts.Length > MaximumPublicStarts || allyStarts.Length > MaximumPublicStarts ||
				enemyStarts.Contains(ownStart) || allyStarts.Contains(ownStart) || enemyStarts.Intersect(allyStarts).Any())
				throw new ArgumentException("Invalid public start associations.");''')
s=s.replace('publicAllyStarts.Distinct().Order().Select(s => distances[s])', 'allyStarts.Select(s => distances[s])')
# Separate directed movement connectivity from undirected geometric resource fields.
s=s.replace('Set("component_count", Components(c => c.GroundPassable, true).Count);', 'Set("component_count", GroundComponentCount());')
s=s.replace('Components(Func<PublicMapFeatureCell, bool> includes, bool requireMovement = false)', 'Components(Func<PublicMapFeatureCell, bool> includes)').replace(' || (requireMovement && edge.Cost < 0)', '')
marker='\t\tList<int[]> Components('
idx=s.index(marker)
s=s[:idx]+'''		int GroundComponentCount()
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

''' + s[idx:]
p.write_text(s)
