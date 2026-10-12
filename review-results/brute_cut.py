from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs'
s=p.read_text();i=s.rfind('\t}\n}')
s=s[:i]+'''		[Test]
		public void VertexCutsMatchBruteForceRemovalAcrossEverySmallGraph()
		{
			// Independent oracle: enumerate every subset of the three interior vertices.
			var cells = Enumerable.Range(0, 5).Select(i => new PublicMapFeatureCell(i, i, 0, true, false, true, 0, 0)).ToArray();
			var candidates = Enumerable.Range(0, 5).SelectMany(a => Enumerable.Range(a + 1, 4 - a)
				.Select(b => (A: a, B: b))).Where(e => e != (0, 4)).ToArray();
			for (var mask = 0; mask < (1 << candidates.Length); mask++)
			{
				var links = candidates.Where((e, index) => (mask & (1 << index)) != 0).ToArray();
				var edges = links.SelectMany(e => new[] { new PublicMapFeatureEdge(e.A, e.B, 1), new PublicMapFeatureEdge(e.B, e.A, 1) });
				var expected = 3;
				for (var removed = 0; removed < 8; removed++)
				{
					var reachable = new HashSet<int> { 0 };
					var queue = new Queue<int>();
					queue.Enqueue(0);
					while (queue.TryDequeue(out var from))
					{
						foreach (var link in links)
						{
							var to = link.A == from ? link.B : link.B == from ? link.A : -1;
							if (to < 0 || (to > 0 && to < 4 && (removed & (1 << (to - 1))) != 0) || !reachable.Add(to))
								continue;
							queue.Enqueue(to);
						}
					}

					if (!reachable.Contains(4))
						expected = Math.Min(expected, System.Numerics.BitOperations.PopCount((uint)removed));
				}

				Assert.That(new PublicMapFeatureGraph(5, 1, cells, edges).MinimumVertexCut(0, 4), Is.EqualTo(expected), $"graph mask {mask}");
			}
		}
''' + s[i:]
p.write_text(s)
