from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs'
s=p.read_text().replace('var bySource = ordered.GroupBy','var links = ordered.Select(e => (e.From, e.To)).ToHashSet();\n\t\t\tif (ordered.Any(e => !links.Contains((e.To, e.From))))\n\t\t\t\tthrow new ArgumentException("Geometry edges must be reciprocal.", nameof(terrainEdges));\n\t\t\tvar bySource = ordered.GroupBy').replace('Components(c => c.GroundPassable).Count','Components(c => c.GroundPassable, true).Count').replace('Components(Func<PublicMapFeatureCell, bool> includes)','Components(Func<PublicMapFeatureCell, bool> includes, bool requireMovement = false)').replace('(cells[from].GroundPassable && cells[to].GroundPassable && edge.Cost < 0)','(requireMovement && edge.Cost < 0)')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs'
s=p.read_text(); i=s.rfind('\t}\n}')
s=s[:i]+'''		static PublicMapFeatureGraph Line(bool reverse = false)
		{
			var terrain = new[]
			{
				new PublicMapFeatureCell(0, 0, 0, true, false, true, 0, 0),
				new PublicMapFeatureCell(1, 1, 0, true, false, true, 0, 10),
				new PublicMapFeatureCell(2, 2, 0, true, false, true, 3, 20),
				new PublicMapFeatureCell(3, 3, 0, true, false, false, 0, 0),
				new PublicMapFeatureCell(4, 4, 0, false, true, false, 0, 30)
			};
			var edges = Enumerable.Range(0, 4).SelectMany(i => new[]
			{
				new PublicMapFeatureEdge(i, i + 1, i == 3 ? -1 : 1000),
				new PublicMapFeatureEdge(i + 1, i, i == 3 ? -1 : 1000)
			}).ToArray();
			return new(5, 1, reverse ? terrain.Reverse() : terrain, reverse ? edges.Reverse() : edges);
		}

		[Test]
		public void PublicGeometryHasIndependentPathAndResourceExpectations()
		{
			var vector = Line().Features(0, new[] { 3 }, Array.Empty<int>(), 2, 2);
			Assert.That(Line().Distances(0).Values, Is.EqualTo(new[] { 0, 1, 2, 3, -1 }));
			foreach (var pair in new Dictionary<string, int>
			{
				["playable_cells"] = 5, ["blocked_share_permille"] = 200, ["water_share_permille"] = 200,
				["openness_permille"] = 800, ["closest_enemy_path_cells"] = 3, ["resource_field_count"] = 2,
				["reachable_field_count"] = 1, ["nearest_resource_path_cells"] = 1, ["total_resource_value"] = 60,
				["resource_value_per_player"] = 30, ["resource_value_per_playable_cell_milli"] = 12000,
				["component_count"] = 1, ["elevation_spread"] = 3, ["start_to_field_ratio_permille"] = 333
			})
				Assert.That(vector.Fields[pair.Key].Value, Is.EqualTo(pair.Value), pair.Key);
			Assert.That(vector.Fields["nearest_chokepoint_path_cells"].Present, Is.False);
			Assert.That(vector.Fields["nearest_ally_path_cells"].Present, Is.False);
		}

		[Test]
		public void ShufflingGeometryDoesNotChangeTheCanonicalVector()
		{
			var first = ContextFeatureEncoding.Serialize(Line().Features(0, new[] { 3 }, Array.Empty<int>()));
			Assert.That(ContextFeatureEncoding.Serialize(Line(true).Features(0, new[] { 3 }, Array.Empty<int>())), Is.EqualTo(first));
		}

		[Test]
		public void SymmetricOwnStartSwapPreservesRelativeDistances()
		{
			var first = Line().Features(0, new[] { 3 }, Array.Empty<int>());
			var second = Line().Features(3, new[] { 0 }, Array.Empty<int>());
			Assert.That(first.Fields["closest_enemy_path_cells"], Is.EqualTo(second.Fields["closest_enemy_path_cells"]));
			Assert.That(first.Fields["nearest_resource_path_cells"], Is.EqualTo(second.Fields["nearest_resource_path_cells"]));
		}

		[Test]
		public void ShortestTerrainCostCanChooseMoreStepsThanTheDirectEdge()
		{
			var cells = Enumerable.Range(0, 4).Select(i => new PublicMapFeatureCell(i, i % 2, i / 2, true, false, true, 0, 0));
			var links = new[] { (0, 1, 1), (1, 3, 1), (0, 3, 100), (0, 2, 1), (2, 3, 1) };
			var edges = links.SelectMany(e => new[] { new PublicMapFeatureEdge(e.Item1, e.Item2, e.Item3), new PublicMapFeatureEdge(e.Item2, e.Item1, e.Item3) });
			Assert.That(new PublicMapFeatureGraph(2, 2, cells, edges).Distances(0)[3], Is.EqualTo(2));
		}

		[Test]
		public void UnreachableAndPrivateStartAssignmentsStayMissing()
		{
			var unknown = Line().Features(0, Array.Empty<int>(), Array.Empty<int>());
			var unreachable = Line().Features(0, new[] { 4 }, Array.Empty<int>());
			Assert.That(unknown.Fields["closest_enemy_path_cells"].Present, Is.False);
			Assert.That(unreachable.Fields["closest_enemy_path_cells"].Present, Is.False);
			Assert.That(unknown.Fields["player_count"].Present, Is.False);
		}

		[Test]
		public void MapAggregatesClampOnlyAfterDivision()
		{
			var cells = new[] { new PublicMapFeatureCell(0, 0, 0, true, false, true, 0, long.MaxValue) };
			var vector = new PublicMapFeatureGraph(1, 1, cells, Array.Empty<PublicMapFeatureEdge>())
				.Features(0, Array.Empty<int>(), Array.Empty<int>(), 2);
			Assert.That(vector.Fields["total_resource_value"].Value, Is.EqualTo(int.MaxValue));
			Assert.That(vector.Fields["resource_value_per_player"].Value, Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void MalformedGeometryCannotSlipIntoPathCalculations()
		{
			var cell = new PublicMapFeatureCell(0, 0, 0, true, false, true, 0, 0);
			Assert.Throws<ArgumentException>(() => new PublicMapFeatureGraph(1, 1, new[] { cell, cell }, Array.Empty<PublicMapFeatureEdge>()));
			Assert.Throws<ArgumentException>(() => new PublicMapFeatureGraph(1, 1, new[] { cell }, new[] { new PublicMapFeatureEdge(0, 99, 1) }));
		}
''' + s[i:]
p.write_text(s)
p=r/'review-results/context-core-tests/context-core-tests.csproj'
s=p.read_text().replace('<Compile Include="../../OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs"/>', '<Compile Include="../../OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs"/><Compile Include="../../OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs"/>')
p.write_text(s)
