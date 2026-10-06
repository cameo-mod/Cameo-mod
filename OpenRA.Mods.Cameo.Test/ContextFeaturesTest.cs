#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ContextFeaturesTest
	{
		static KeyValuePair<string, ContextFeatureMeasurement> Field(string name, int value) =>
			new(name, ContextFeatureMeasurement.Observed(value));
		static OpponentFeatureCensus Census(int tick, int value = 100, int visibleTick = -1, int coverage = 1000,
			int actorSamples = 4) => new(tick, visibleTick < 0 ? tick : visibleTick, actorSamples, coverage,
			100, 200, new[] { value, 0, 0, 0, 0, 0, 0 });

		[TestCase(ContextFeatureKind.Map, 57)]
		[TestCase(ContextFeatureKind.Opponent, 61)]
		public void SchemaHasExactlyTheReviewedFields(ContextFeatureKind kind, int count)
		{
			var vector = new ContextFeatureVector(kind);
			Assert.That(vector.Fields.Count, Is.EqualTo(count));
			Assert.That(vector.Fields.Values.All(v => !v.Present && v.Value == -1 && v.ConfidencePermille == 0));
			Assert.That(ContextFeatureVector.SchemaFields(kind), Is.Ordered.Using<string>(StringComparer.Ordinal));
		}

		[TestCase("player_name")]
		[TestCase("map_uid")]
		[TestCase("seat_1")]
		[TestCase("x")]
		[TestCase("PrivatePlayerName")]
		public void ArbitraryKeysCannotEnterTheVector(string key) =>
			Assert.Throws<ArgumentException>(() => new ContextFeatureVector(ContextFeatureKind.Map, new[] { Field(key, 1) }));

		[Test]
		public void DuplicatesAndInvalidConfidenceAreRejected()
		{
			Assert.Throws<ArgumentException>(() => new ContextFeatureVector(ContextFeatureKind.Map,
				new[] { Field("width_cells", 1), Field("width_cells", 2) }));
			Assert.Throws<ArgumentException>(() => new ContextFeatureVector(ContextFeatureKind.Map,
				new[] { new KeyValuePair<string, ContextFeatureMeasurement>("width_cells", new(1, true, 1001)) }));
		}

		[Test]
		public void MissingCannotCarryStaleValueOrConfidence()
		{
			var vector = new ContextFeatureVector(ContextFeatureKind.Map,
				new[] { new KeyValuePair<string, ContextFeatureMeasurement>("width_cells", new(99, false, 1000)) });
			Assert.That(vector.Fields["width_cells"], Is.EqualTo(ContextFeatureMeasurement.Missing));
		}

		[Test]
		public void EncoderIsCanonicalAndMeasuredZeroRemainsPresent()
		{
			var input = new[] { Field("width_cells", 0), Field("height_cells", int.MaxValue) };
			var encoded = ContextFeatureEncoding.Serialize(new(ContextFeatureKind.Map, input));
			Assert.That(encoded, Is.EqualTo(ContextFeatureEncoding.Serialize(new(ContextFeatureKind.Map, input.Reverse()))));
			using var json = JsonDocument.Parse(encoded);
			Assert.That(json.RootElement.EnumerateObject().Select(p => p.Name),
				Is.EqualTo(new[] { "record", "schema", "features" }));
			Assert.That(json.RootElement.GetProperty("features").GetProperty("width_cells").GetProperty("present").GetBoolean());
		}

		[TestCase(ContextFeatureKind.Map)]
		[TestCase(ContextFeatureKind.Opponent)]
		public void LargestNumbersFitWithinTheWireBudget(ContextFeatureKind kind)
		{
			var encoded = ContextFeatureEncoding.Serialize(new(kind,
				ContextFeatureVector.SchemaFields(kind).Select(s => Field(s, int.MinValue))));
			Assert.That(Encoding.UTF8.GetByteCount(encoded), Is.LessThanOrEqualTo(ContextFeatureVector.MaximumSerializedBytes));
		}

		[Test]
		public void IntegerRateDoesNotDependOnTickOriginOrEnumeration()
		{
			var samples = new[] { (0, 10), (750, 760), (1500, 1510) };
			Assert.That(ContextFeatureEncoding.Slope(samples), Is.EqualTo(1000));
			Assert.That(ContextFeatureEncoding.Slope(samples.Reverse().ToArray()), Is.EqualTo(1000));
			Assert.That(ContextFeatureEncoding.Slope(samples.Select(s => (s.Item1 + 2000000000, s.Item2)).ToArray()), Is.EqualTo(1000));
			Assert.That(ContextFeatureEncoding.Slope(new[] { (0, 1510), (750, 760), (1500, 10) }), Is.EqualTo(-1000));
		}

		[Test]
		public void ExtremeArithmeticSaturatesAfterExactDivision()
		{
			Assert.That(ContextFeatureEncoding.Slope(new[] { (0, int.MinValue), (1, int.MaxValue) }), Is.EqualTo(int.MaxValue));
			Assert.That(ContextFeatureEncoding.Slope(new[] { (0, int.MaxValue), (1, int.MinValue) }), Is.EqualTo(int.MinValue));
			Assert.That(ContextFeatureEncoding.Ratio(long.MaxValue, long.MaxValue), Is.EqualTo(1000));
			Assert.That(ContextFeatureEncoding.Slope(new[] { (1, 4), (1, 8) }), Is.Zero);
		}

		[Test]
		public void SamplingFloorCannotBeSatisfiedByRepeatedRecords()
		{
			var window = new OpponentFeatureWindow();
			Assert.That(window.Add(Census(0)));
			for (var tick = 0; tick < 750; tick++)
				Assert.That(window.Add(Census(tick)), Is.False);
			Assert.That(window.Features(749).Fields["army_value_slope"].Present, Is.False);
			Assert.That(window.Add(Census(750, 850)));
			Assert.That(window.Features(1000).Fields["army_value_slope"].Value, Is.EqualTo(1000));
		}

		[Test]
		public void RecomputeCadenceIs250TicksAndHistoryIsBounded()
		{
			var window = new OpponentFeatureWindow();
			for (var i = 0; i < 100; i++)
				window.Add(Census(i * 750));
			Assert.That(window.SampleCount, Is.EqualTo(OpponentFeatureWindow.MaximumSamples));
			var first = window.Features(74250);
			Assert.That(window.Features(74499), Is.SameAs(first));
			Assert.That(window.Features(74500), Is.Not.SameAs(first));
			Assert.Throws<ArgumentException>(() => window.Add(Census(1)));
			Assert.Throws<ArgumentOutOfRangeException>(() => window.Features(74250));
		}

		[Test]
		public void CoverageAndRecencyReduceConfidenceWithoutRemovingHiddenArmy()
		{
			var window = new OpponentFeatureWindow();
			window.Add(Census(0));
			window.Add(Census(750, visibleTick: 0, coverage: 500));
			var features = window.Features(750);
			Assert.That(features.Fields["confidence_permille"].Value, Is.EqualTo(475));
			Assert.That(features.Fields["army_value_slope"].Value, Is.Zero);
			Assert.That(features.Fields["role_share_infantry"].Value, Is.EqualTo(1000));
			Assert.That(window.Features(15000).Fields["role_share_infantry"].Present, Is.False);
		}

		[Test]
		public void ZeroCoverageAndUnknownEventsNeverBecomeConfidentZero()
		{
			var window = new OpponentFeatureWindow();
			window.Add(Census(0));
			window.Add(Census(750, coverage: 0));
			var features = window.Features(750);
			Assert.That(features.Fields["army_value_slope"].Present, Is.False);
			foreach (var field in new[] { "command_burst_permille", "visible_bank_pressure_permille", "focus_convergence_permille", "seen_regions" })
				Assert.That(features.Fields[field], Is.EqualTo(ContextFeatureMeasurement.Missing));
		}

		[Test]
		public void CensusCopiesItsRolesAndUsesUnclampedDenominatorForShares()
		{
			var roles = new[] { int.MaxValue, int.MaxValue, 0, 0, 0, 0, 0 };
			var census = new OpponentFeatureCensus(0, 0, 4, 1000, 0, 0, roles);
			roles[0] = 0;
			Assert.That(census.RememberedRoleValues[0], Is.EqualTo(int.MaxValue));
			var window = new OpponentFeatureWindow();
			window.Add(census);
			window.Add(new(750, 750, 4, 1000, 0, 0, census.RememberedRoleValues));
			Assert.That(window.Features(750).Fields["role_share_infantry"].Value, Is.EqualTo(500));
		}
		static PublicMapFeatureGraph Line(bool reverse = false)
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
		[Test]
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
		[Test]
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
		[Test]
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
		[Test]
		public void EntropyIsTrueNormalizedIntegerEntropy()
		{
			Assert.That(ContextFeatureEncoding.Entropy(new[] { 1, 1, 1, 1, 1, 1, 1 }), Is.EqualTo(1000));
			Assert.That(ContextFeatureEncoding.Entropy(new[] { 5, 0, 0, 0, 0, 0, 0 }), Is.Zero);
			Assert.That(ContextFeatureEncoding.Entropy(new[] { 1, 1, 0, 0, 0, 0, 0 }), Is.EqualTo(356));
			Assert.That(ContextFeatureEncoding.Entropy(new[] { int.MaxValue, int.MaxValue, 0, 0, 0, 0, 0 }), Is.EqualTo(356));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContextFeatureEncoding.Entropy(new[] { -1, 1 }));
		}

		[Test]
		public void AccelerationRequiresTwoSeparate750TickWindows()
		{
			var window = new OpponentFeatureWindow();
			window.Add(Census(0, 100));
			window.Add(Census(750, 850));
			Assert.That(window.Features(750).Fields["army_value_acceleration"].Present, Is.False);
			window.Add(Census(1500, 1600));
			window.Add(Census(2250, 2350));
			Assert.That(window.Features(2250).Fields["army_value_acceleration"].Value, Is.Zero);
		}
		[Test]
		public void ArmyRateDoesNotFlattenWhenRoleTotalsExceedInt32()
		{
			var window = new OpponentFeatureWindow();
			window.Add(new(0, 0, 4, 1000, 100, 100, new[] { int.MaxValue, int.MaxValue, 0, 0, 0, 0, 0 }));
			window.Add(new(750, 750, 4, 1000, 100, 100, new[] { int.MaxValue, int.MaxValue, 750, 0, 0, 0, 0 }));
			Assert.That(window.Features(750).Fields["army_value_slope"].Value, Is.EqualTo(1000));
		}
	}
}

