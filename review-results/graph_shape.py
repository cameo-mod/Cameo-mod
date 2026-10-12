from pathlib import Path
p=Path(r'C:/cameo-wt/codex-context-providers/OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs')
s=p.read_text()
s=s.replace('public int? xxx', '')
s=s.replace('var enemy = publicEnemyStarts.Distinct().Order().Select(s => distances[s]).Where(d => d >= 0).ToArray();', 'var enemyStarts = publicEnemyStarts.Distinct().Order().ToArray();\n\t\t\tvar enemy = enemyStarts.Select(s => distances[s]).Where(d => d >= 0).ToArray();')
s=s.replace('var resourceFields = Components(c => c.ResourceValue > 0);', '''var resourceFields = Components(c => c.ResourceValue > 0);
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
''')
s=s.replace('if (enemy.Length != 0)\n\t\t\t\t\tSet("start_to_field_ratio_permille", ContextFeatureEncoding.Ratio(reachableFields[0], enemy.Min()));', '''if (reachableFields.Length > 1)
					Set("nearest_expansion_path_cells", reachableFields[1]);
				if (enemy.Length != 0)
				{
					Set("start_to_field_ratio_permille", ContextFeatureEncoding.Ratio(reachableFields[0], enemy.Min()));
					if (reachableFields.Length > 1)
						Set("expansion_to_enemy_ratio_permille", ContextFeatureEncoding.Ratio(reachableFields[1], enemy.Min()));
				}''')
s=s.replace('// Fields requiring a public start association, POI classifier or public corridor adapter remain missing.', '''if (enemy.Length != 0)
			{
				var nearStart = cells.Where(c => distances[c.Index] >= 0 && (long)distances[c.Index] * 4 <= enemy.Min()).ToArray();
				if (nearStart.Length != 0)
					Set("buildable_near_start_permille", ContextFeatureEncoding.Ratio(nearStart.Count(c => c.Buildable), nearStart.Length));
				// Compare public shortest paths only. No actual enemy spawn is inferred when its assignment is private.
				var enemyPaths = enemyStarts.Select(Distances).ToArray();
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

			// POI, naval route, defence and corridor fields need their public classifier/geometry adapters.''')
p.write_text(s)
