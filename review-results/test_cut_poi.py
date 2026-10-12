from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs'
s=p.read_text().replace('if (centre.GroundPassable && distances[centre.Index] > 1)\n\t\t\t\tSet("minimum_cut_cells", MinimumVertexCut(ownStart, centre.Index).Value);', '''if (centre.GroundPassable && distances[centre.Index] > 1)
			{
				var cut = MinimumVertexCut(ownStart, centre.Index);
				if (cut.HasValue)
					Set("minimum_cut_cells", cut.Value);
			}''')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs'
s=p.read_text();i=s.rfind('\t}\n}')
s=s[:i]+'''		[Test]
		public void VertexCutAgreesWithRemovingTheOnlyInteriorCell()
		{
			Assert.That(Line().MinimumVertexCut(0, 3), Is.EqualTo(1));
			Assert.That(Line().MinimumVertexCut(0, 1), Is.Null);
			Assert.That(Line().MinimumVertexCut(0, 4), Is.Null);
			Assert.That(Line().Features(0, new[] { 3 }, Array.Empty<int>()).Fields["minimum_cut_cells"].Value, Is.EqualTo(1));
		}

		[Test]
		public void TwoIndependentCorridorsRequireTwoInteriorCuts()
		{
			var cells = Enumerable.Range(0, 4).Select(i => new PublicMapFeatureCell(i, i % 2, i / 2, true, false, true, 0, 0));
			var links = new[] { (0, 1), (1, 3), (0, 2), (2, 3) };
			var edges = links.SelectMany(e => new[] { new PublicMapFeatureEdge(e.Item1, e.Item2, 1), new PublicMapFeatureEdge(e.Item2, e.Item1, 1) });
			Assert.That(new PublicMapFeatureGraph(2, 2, cells, edges).MinimumVertexCut(0, 3), Is.EqualTo(2));
		}

		[Test]
		public void DirectedComponentsDoNotDependOnTheFirstSeed()
		{
			var cells = Enumerable.Range(0, 3).Select(i => new PublicMapFeatureCell(i, i, 0, true, false, true, 0, 0));
			var edges = new[] { new PublicMapFeatureEdge(0, 1, 1), new PublicMapFeatureEdge(1, 0, -1),
				new PublicMapFeatureEdge(1, 2, 1), new PublicMapFeatureEdge(2, 1, 1) };
			var features = new PublicMapFeatureGraph(3, 1, cells, edges).Features(0, new[] { 2 }, Array.Empty<int>());
			Assert.That(features.Fields["component_count"].Value, Is.EqualTo(2));
		}

		[Test]
		public void PublicPoiAndCorridorGeometryHasKnownDistancesAndWidths()
		{
			var pois = new[] { new PublicMapFeaturePoi(2, true, true, true, 100), new PublicMapFeaturePoi(4, true, true, false, 300) };
			var corridors = new[] { new PublicMapFeatureCorridor(1, 1), new PublicMapFeatureCorridor(2, 3) };
			var fields = Line().Features(0, new[] { 3 }, Array.Empty<int>(), 2, publicPois: pois, publicCorridors: corridors).Fields;
			Assert.That(fields["nearest_poi_path_cells"].Value, Is.EqualTo(2));
			Assert.That(fields["nearest_oil_path_cells"].Value, Is.EqualTo(2));
			Assert.That(fields["poi_value_per_player"].Value, Is.EqualTo(200));
			Assert.That(fields["chokepoint_count"].Value, Is.EqualTo(2));
			Assert.That(fields["chokepoint_width_min_cells"].Value, Is.EqualTo(1));
			Assert.That(fields["chokepoint_width_mean_cells"].Value, Is.EqualTo(2));
			Assert.That(fields["chokepoint_width_p90_cells"].Value, Is.EqualTo(3));
		}

		[Test]
		public void PublicShapeMeasurementsDoNotCountUnreachableResourceAsNearby()
		{
			var fields = Line().Features(0, new[] { 3 }, Array.Empty<int>()).Fields;
			Assert.That(fields["resource_near_spawn_permille"].Value, Is.Zero);
			Assert.That(fields["resource_edge_permille"].Value, Is.EqualTo(1000));
			Assert.That(fields["resource_cluster_permille"].Value, Is.EqualTo(500));
			Assert.That(fields["nearest_expansion_path_cells"].Present, Is.False);
			Assert.That(fields["buildable_near_start_permille"].Value, Is.EqualTo(1000));
		}

		[Test]
		public void EnemyAndAllyAssociationsAreExclusiveAndNeverIncludeOwnStart()
		{
			Assert.Throws<ArgumentException>(() => Line().Features(0, new[] { 0 }, Array.Empty<int>()));
			Assert.Throws<ArgumentException>(() => Line().Features(0, new[] { 3 }, new[] { 3 }));
		}
''' + s[i:]
p.write_text(s)
