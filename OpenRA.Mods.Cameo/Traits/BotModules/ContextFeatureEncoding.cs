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
using System.IO;
using System.Linq;
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

		// Binary logarithm at Q20 precision, using only exact integer intermediates.
		static BigInteger Log2Ratio(long numerator, long denominator)
		{
			var unit = BigInteger.One << 32;
			var value = (BigInteger)numerator * unit / denominator;
			BigInteger logarithm = 0;
			while (value >= 2 * unit)
			{
				value >>= 1;
				logarithm += BigInteger.One << 20;
			}

			for (var bit = 19; bit >= 0; bit--)
			{
				value = value * value / unit;
				if (value >= 2 * unit)
				{
					value >>= 1;
					logarithm += BigInteger.One << bit;
				}
			}

			return logarithm;
		}

		public static int Entropy(IReadOnlyList<int> values)
		{
			long total = 0;
			foreach (var value in values)
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(values));
				total += value;
			}

			if (total == 0 || values.Count <= 1)
				return 0;
			BigInteger weightedLog = 0;
			foreach (var value in values)
				if (value > 0)
					weightedLog += value * Log2Ratio(total, value);
			return Math.Clamp(Saturate(weightedLog * 1000 / (total * Log2Ratio(values.Count, 1))), 0, 1000);
		}

		public static BigInteger IntegerSquareRoot(BigInteger value)
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));
			if (value <= 1)
				return value;
			var root = BigInteger.One << (int)((value.GetBitLength() + 1) / 2);
			while (true)
			{
				var next = (root + value / root) / 2;
				if (next >= root)
					return root;
				root = next;
			}
		}

		public static int Ratio(long numerator, long denominator, int scale = 1000)
		{
			if (numerator < 0 || denominator < 0 || scale < 0)
				throw new ArgumentOutOfRangeException(nameof(numerator));
			return Saturate((BigInteger)numerator * scale / Math.Max(1, denominator));
		}

		/// <summary>OLS rate per 1000 ticks. Tick origin cancels; exact intermediates prevent overflow.</summary>
		public static int Slope(IReadOnlyList<(int Tick, int Value)> samples) =>
			Slope(samples.Select(s => (s.Tick, (long)s.Value)).ToArray());

		public static int Slope(IReadOnlyList<(int Tick, long Value)> samples)
		{
			var rate = SlopeRatio(samples);
			return Saturate(rate.Numerator / rate.Denominator);
		}

		public static (BigInteger Numerator, BigInteger Denominator) SlopeRatio(IReadOnlyList<(int Tick, long Value)> samples)
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
			return denominator == 0 ? (0, 1) : ((samples.Count * xy - x * y) * 1000, denominator);
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
