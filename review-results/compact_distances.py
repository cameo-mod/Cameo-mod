from pathlib import Path
p=Path(r'C:/cameo-wt/codex-context-providers/OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs')
s=p.read_text().replace('using System.Collections.ObjectModel;\n', 'using System.Collections;\n')
s=s.replace('var result = new SortedDictionary<int, int>();\n\t\t\tfor (var i = 0; i < cells.Length; i++)\n\t\t\t\tresult.Add(cells[i].Index, steps[i]);\n\t\t\treturn new ReadOnlyDictionary<int, int>(result);', 'return new TerrainDistances(cells, offsets, steps);')
s=s.replace('var enemy = enemyStarts.Select(s => distances[s]).Where(d => d >= 0).ToArray();', 'var enemy = enemyStarts.Select(s => distances[s]).Where(d => d >= 0).ToArray();\n\t\t\tvar enemyPaths = enemyStarts.Select(Distances).ToArray();')
s=s.replace('\t\t\t\tvar enemyPaths = enemyStarts.Select(Distances).ToArray();\n','').replace('\t\t\t\t\tvar enemyPaths = enemyStarts.Select(Distances).ToArray();\n','')
s=s.replace('var routes = enemyStarts.Count(start => ShoreCells(Distances(start)).Any(waterReach.Contains));', 'var routes = enemyPaths.Count(path => ShoreCells(path).Any(waterReach.Contains));')
idx=s.index('\t\tint[] ShoreCells(')
s=s[:idx]+'''		sealed class TerrainDistances : IReadOnlyDictionary<int, int>
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
			public IEnumerable<int> Values => distances;
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

''' +s[idx:]
# Never expose the backing array through Values (a caller could cast it and alter cached paths).
s=s.replace('public IEnumerable<int> Values => distances;', 'public IEnumerable<int> Values => distances.Select(d => d);')
p.write_text(s)
