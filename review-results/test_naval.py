from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs'
s=p.read_text().replace('if (enemy.Length != 0)\n\t\t\t\t{\n\t\t\t\t\tvar routes', 'if (enemyStarts.Length != 0)\n\t\t\t\t{\n\t\t\t\t\tvar routes')
p.write_text(s)
p=r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs'
s=p.read_text();i=s.rfind('\t}\n}')
s=s[:i]+'''		[Test]
		public void SquareRootAndBacklineProjectionStayExactWithoutFloatingPoint()
		{
			var value = System.Numerics.BigInteger.Parse("123456789012345678901234567890");
			Assert.That(ContextFeatureEncoding.IntegerSquareRoot(value * value), Is.EqualTo(value));
			Assert.That(ContextFeatureEncoding.IntegerSquareRoot(value * value + 2 * value), Is.EqualTo(value));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContextFeatureEncoding.IntegerSquareRoot(-1));
			Assert.That(Line().Features(1, new[] { 3 }, Array.Empty<int>()).Fields["backline_depth_cells"].Value, Is.EqualTo(1));
		}

		[Test]
		public void IslandNavalRouteCanExistWhenNoGroundRouteExists()
		{
			var cells = Enumerable.Range(0, 7).Select(i => new PublicMapFeatureCell(i, i, 0,
				i is 0 or 1 or 4 or 5, i is 2 or 3 or 6, i is 0 or 1 or 4 or 5, 0, 0)).ToArray();
			var edges = Enumerable.Range(0, 6).SelectMany(i => new[]
			{
				new PublicMapFeatureEdge(i, i + 1, i is 0 or 4 ? 1 : -1),
				new PublicMapFeatureEdge(i + 1, i, i is 0 or 4 ? 1 : -1)
			}).ToArray();
			var naval = new[] { new PublicMapFeatureEdge(2, 3, 1), new PublicMapFeatureEdge(3, 2, 1) };
			var fields = new PublicMapFeatureGraph(7, 1, cells, edges, naval)
				.Features(0, new[] { 4 }, Array.Empty<int>()).Fields;
			Assert.That(fields["closest_enemy_path_cells"].Present, Is.False);
			Assert.That(fields["nearest_water_path_cells"].Value, Is.EqualTo(2));
			Assert.That(fields["own_start_water_access_permille"].Value, Is.EqualTo(666));
			Assert.That(fields["naval_route_share_permille"].Value, Is.EqualTo(1000));
		}

		[Test]
		public void PublicDefenceArcRequiresARealGeometryDenominator()
		{
			Assert.That(Line().Features(0, new[] { 3 }, Array.Empty<int>(),
				publicDefenceArc: new(3, 8)).Fields["defence_arc_permille"].Value, Is.EqualTo(375));
			Assert.Throws<ArgumentException>(() => Line().Features(0, new[] { 3 }, Array.Empty<int>(), publicDefenceArc: new(1, 0)));
		}
''' + s[i:]
p.write_text(s)
