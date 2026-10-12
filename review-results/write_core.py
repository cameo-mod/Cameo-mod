from pathlib import Path
root=Path(r'C:/cameo-wt/codex-context-providers')
header=(root/'OpenRA.Mods.Cameo.Test/ZoneTopologyTest.cs').read_text().split('using ')[0]
p=root/'OpenRA.Mods.CA/Traits/BotModules/IBotMapFeatures.cs'
s=p.read_text().replace('using System.Linq;\n','')
p.write_text(header+s)
(root/'OpenRA.Mods.Cameo/Traits/BotModules/ContextFeatureEncoding.cs').write_text(header+'''using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>Pure arithmetic and a closed, bounded wire representation; no simulation objects.</summary>
	public static class ContextFeatureEncoding
	{
		public static int Saturate(BigInteger value) => value < int.MinValue ? int.MinValue :
			value > int.MaxValue ? int.MaxValue : (int)value;

		public static int Ratio(long numerator, long denominator, int scale = 1000)
		{
			if (numerator < 0 || denominator < 0 || scale < 0)
				throw new ArgumentOutOfRangeException(nameof(numerator));
			return Saturate((BigInteger)numerator * scale / Math.Max(1, denominator));
		}

		/// <summary>OLS rate per 1000 ticks. Tick origin cancels; exact intermediates prevent overflow.</summary>
		public static int Slope(IReadOnlyList<(int Tick, int Value)> samples)
		{
			BigInteger x = 0, y = 0, xx = 0, xy = 0;
			foreach (var sample in samples)
			{
				x += sample.Tick;
				y += sample.Value;
				xx += (BigInteger)sample.Tick * sample.Tick;
				xy += (BigInteger)sample.Tick * sample.Value;
			}

			var denominator = samples.Count * xx - x * x;
			return denominator == 0 ? 0 : Saturate((samples.Count * xy - x * y) * 1000 / denominator);
		}

		public static string Serialize(ContextFeatureVector vector)
		{
			ArgumentNullException.ThrowIfNull(vector);
			using var bytes = new MemoryStream();
			using (var writer = new Utf8JsonWriter(bytes))
			{
				writer.WriteStartObject();
				writer.WriteString("record", vector.Kind == ContextFeatureKind.Map ? "map_features" : "opponent_features");
				writer.WriteNumber("schema", ContextFeatureVector.Version);
				writer.WriteStartObject("features");
				// The schema order is fixed. Caller enumeration never determines output order.
				foreach (var field in ContextFeatureVector.SchemaFields(vector.Kind))
				{
					var measurement = vector.Fields[field];
					writer.WriteStartObject(field);
					writer.WriteNumber("value", measurement.Value);
					writer.WriteBoolean("present", measurement.Present);
					writer.WriteNumber("confidence_permille", measurement.ConfidencePermille);
					writer.WriteEndObject();
				}

				writer.WriteEndObject();
				writer.WriteEndObject();
			}

			if (bytes.Length > ContextFeatureVector.MaximumSerializedBytes)
				throw new InvalidOperationException("Context vector exceeds the wire budget.");
			return Encoding.UTF8.GetString(bytes.ToArray());
		}
	}
}
''')
