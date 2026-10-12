from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
h=(r/'OpenRA.Mods.Cameo.Test/ZoneTopologyTest.cs').read_text().split('using ')[0]
(r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs').write_text(h+'''using System;
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
			Assert.That(ContextFeatureVector.SchemaFields(kind), Is.Ordered.Using(StringComparer.Ordinal));
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
	}
}
''')
p=r/'review-results/context-core-tests'
p.mkdir(exist_ok=True)
(p/'context-core-tests.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><LangVersion>13</LangVersion><Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="../../OpenRA.Mods.CA/Traits/BotModules/IBotMapFeatures.cs"/><Compile Include="../../OpenRA.Mods.Cameo/Traits/BotModules/ContextFeatureEncoding.cs"/><Compile Include="../../OpenRA.Mods.Cameo/Traits/BotModules/OpponentFeatureWindow.cs"/><Compile Include="../../OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs"/><PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0"/><PackageReference Include="NUnit" Version="4.3.2"/><PackageReference Include="NUnit3TestAdapter" Version="4.6.0"/></ItemGroup></Project>''')
