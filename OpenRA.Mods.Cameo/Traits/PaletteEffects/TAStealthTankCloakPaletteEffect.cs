#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Tiberian Aurora stealth tank cloak palette effect.")]
	public class TAStealthTankCloakPaletteEffectInfo : TraitInfo
	{
		[FieldLoader.Require]
		[PaletteReference(true)]
		[Desc("Palette modified by the cloak shimmer.")]
		public readonly string AffectedPalette = null;

		[Desc("Colors of the shimmering zaps. Each entry becomes one moving highlight.")]
		public readonly Color[] Colors =
		{
			Color.FromArgb(255, 128, 255, 128),
			Color.FromArgb(128, 80, 255, 80),
			Color.FromArgb(64, 0, 255, 0)
		};

		[Desc("Override shimmer colors using ARGB hex values (optional).")]
		public readonly string[] ColorsHex = Array.Empty<string>();

		[Desc("Lower index bound for the shimmer loop.")]
		public readonly int StartIndex = 0;

		[Desc("Upper index bound for the shimmer loop.")]
		public readonly int EndIndex = 32;

		[Desc("When true the shimmer colors are derived from the base palette entry instead of the configured Colors array.")]
		public readonly bool UseBaseColors = false;

		[Desc("Alpha value [0..1] applied to the base palette when UseBaseColors is enabled.")]
		public readonly float BaseAlpha = 0.25f;

		[Desc("Premultiply the base colors by the BaseAlpha value when UseBaseColors is enabled.")]
		public readonly bool BasePremultiply = false;

		[Desc("Alpha value [0..1] applied to the shimmering highlight when UseBaseColors is enabled.")]
		public readonly float HighlightAlpha = 0.45f;

		[Desc("Brightness multiplier applied to the shimmering highlight when UseBaseColors is enabled.")]
		public readonly float HighlightBrightness = 1.6f;

		[Desc("Saturation multiplier applied to the shimmering highlight when UseBaseColors is enabled.")]
		public readonly float HighlightSaturation = 1.0f;

		[Desc("Blend factor [0..1] between the derived base color and the configured Colors entry when UseBaseColors is enabled.")]
		public readonly float HighlightBlend = 0f;

		[Desc("Number of palette entries that form a full shimmer cycle when UseBaseColors is enabled.")]
		public readonly int HighlightCycleLength = 96;

		[Desc("Offset applied to the palette index when evaluating the shimmer phase (higher values create wider patches).")]
		public readonly int HighlightStride = 11;

		[Desc("Speed multiplier for the shimmering animation when UseBaseColors is enabled.")]
		public readonly float HighlightSpeed = 1f;

		[Desc("Exponent that sharpens or softens the shimmering highlight. Values > 1 sharpen the peak.")]
		public readonly float HighlightExponent = 1.35f;

		[Desc("Width [0..1] of the shimmering highlight band when UseBaseColors is enabled.")]
		public readonly float HighlightWidth = 0.35f;

		[Desc("Random phase jitter [0..1] applied per palette entry to break up uniform shimmering when UseBaseColors is enabled.")]
		public readonly float HighlightJitter = 0.4f;

		internal Color[] ResolveColors()
		{
			if (ColorsHex.Length == 0)
				return Colors;

			var result = new Color[ColorsHex.Length];
			for (var i = 0; i < ColorsHex.Length; i++)
			{
				var hex = ColorsHex[i];
				if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
				{
					result[i] = Colors[Math.Min(i, Colors.Length - 1)];
					continue;
				}

				var a = (int)((value >> 24) & 0xFF);
				var r = (int)((value >> 16) & 0xFF);
				var g = (int)((value >> 8) & 0xFF);
				var b = (int)(value & 0xFF);

				result[i] = Color.FromArgb(a, r, g, b);
			}

			return result;
		}

		public override object Create(ActorInitializer init)
		{
			return new TAStealthTankCloakPaletteEffect(this);
		}
	}

	public class TAStealthTankCloakPaletteEffect : IPaletteModifier, ITick
	{
		int t;
		readonly TAStealthTankCloakPaletteEffectInfo info;
		readonly Color[] colors;

		public TAStealthTankCloakPaletteEffect(TAStealthTankCloakPaletteEffectInfo info)
		{
			this.info = info;
			colors = info.ResolveColors();
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			foreach (var palette in GetMatchingPalettes(palettes))
				ApplyPaletteEffect(palette);
		}

		IEnumerable<MutablePalette> GetMatchingPalettes(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (info.AffectedPalette == null)
				yield break;

			foreach (var kv in palettes)
			{
				if (PaletteMatches(kv.Key))
					yield return kv.Value;
			}
		}

		bool PaletteMatches(string paletteName)
		{
			if (paletteName == null)
				return false;

			if (paletteName.Equals(info.AffectedPalette, StringComparison.OrdinalIgnoreCase))
				return true;

			return paletteName.StartsWith(info.AffectedPalette, StringComparison.OrdinalIgnoreCase);
		}

		void ApplyPaletteEffect(MutablePalette palette)
		{
			if (info.UseBaseColors)
			{
				ApplyBasePaletteShimmer(palette);
				return;
			}

			for (var i = 0; i < colors.Length; i++)
			{
				var primaryIndex = (t + i) % 255 + 1;
				var secondaryIndex = (primaryIndex + 32) % 255;

				palette.SetColor(primaryIndex, colors[i]);
				palette.SetColor(secondaryIndex, colors[i]);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (info.UseBaseColors)
			{
				unchecked { t++; }
				return;
			}

			if (++t >= info.EndIndex)
				t = info.StartIndex;
		}

		void ApplyBasePaletteShimmer(MutablePalette palette)
		{
			var originalColors = CaptureOriginalColors(palette);
			if (originalColors == null)
				return;

			var cycle = Math.Max(1f, info.HighlightCycleLength);
			var stride = Math.Max(1f, info.HighlightStride);
			var exponent = Math.Max(0.01f, info.HighlightExponent);
			var width = Clamp01(info.HighlightWidth);
			var jitter = Clamp01(info.HighlightJitter);
			var speed = info.HighlightSpeed;

			for (var idx = 1; idx < Palette.Size; idx++)
			{
				var original = originalColors[idx];
				if (original.A == 0)
					continue;

				var intensity = EvaluateShimmerIntensity(idx, original, cycle, stride, speed, exponent, width, jitter);
				var shimmered = BuildShimmerColor(original, intensity);
				palette.SetColor(idx, shimmered);
			}
		}

		float EvaluateShimmerIntensity(int paletteIndex, Color baseColor, float cycle, float stride, float speed, float exponent, float width, float jitter)
		{
			var jitterOffset = jitter <= 0f ? 0f : (HashToUnitInterval(baseColor, paletteIndex) - 0.5f) * jitter * cycle;
			var shift = paletteIndex * stride + t * speed + jitterOffset;
			var normalized = shift / cycle - (float)Math.Floor(shift / cycle);
			var halfWidth = Math.Max(0.001f, width * 0.5f);
			var distance = Math.Abs(normalized - 0.5f);
			var pulse = Clamp01(1f - distance / halfWidth);
			return (float)Math.Pow(pulse, exponent);
		}

		Color BuildShimmerColor(Color baseColor, float intensity)
		{
			var baseAlpha = Clamp01(info.BaseAlpha);
			var highlightAlpha = Clamp01(info.HighlightAlpha);
			var alpha = baseAlpha + (highlightAlpha - baseAlpha) * intensity;

			var (_, h, s, v) = baseColor.ToAhsv();
			s = Clamp01(s * (1f + (info.HighlightSaturation - 1f) * intensity));
			v = Clamp01(v * (1f + (info.HighlightBrightness - 1f) * intensity));
			var result = Color.FromAhsv(ToByte(alpha), h, s, v);

			var whiteHighlight = Color.FromArgb(ToByte(info.HighlightAlpha), 255, 255, 255);
			result = LerpColor(result, whiteHighlight, intensity);

			if (info.HighlightBlend > 0f && colors.Length > 0 && !info.UseBaseColors)
			{
				var fallbackIndex = Math.Min(colors.Length - 1, (int)Math.Round(intensity * (colors.Length - 1)));
				var fallback = colors[fallbackIndex];
				fallback = Color.FromArgb(ToByte(info.HighlightAlpha), fallback.R, fallback.G, fallback.B);
				var blendAmount = Clamp01(info.HighlightBlend * intensity);
				result = LerpColor(result, fallback, blendAmount);
			}

			if (info.BasePremultiply)
			{
				var factor = result.A / 255f;
				result = Color.FromArgb(result.A,
					MultiplyComponent(result.R, factor),
					MultiplyComponent(result.G, factor),
					MultiplyComponent(result.B, factor));
			}

			return result;
		}

		Color[] CaptureOriginalColors(MutablePalette palette)
		{
			var snapshot = new Color[Palette.Size];
			for (var i = 0; i < Palette.Size; i++)
				snapshot[i] = palette.GetColor(i);
			return snapshot;
		}

		static Color LerpColor(Color a, Color b, float t)
		{
			var blend = Clamp01(t);
			var inv = 1f - blend;

			var alpha = ClampToByte((int)Math.Round(a.A * inv + b.A * blend));
			var red = ClampToByte((int)Math.Round(a.R * inv + b.R * blend));
			var green = ClampToByte((int)Math.Round(a.G * inv + b.G * blend));
			var blue = ClampToByte((int)Math.Round(a.B * inv + b.B * blend));

			return Color.FromArgb(alpha, red, green, blue);
		}

		static byte MultiplyComponent(byte component, float factor)
		{
			return ClampToByte((int)Math.Round(component * factor));
		}

		static byte ToByte(float value)
		{
			return ClampToByte((int)Math.Round(Clamp01(value) * 255f));
		}

		static float Clamp01(float value)
		{
			if (float.IsNaN(value))
				return 0f;
			if (value < 0f)
				return 0f;
			return value > 1f ? 1f : value;
		}

		static float HashToUnitInterval(Color color, int index)
		{
			var hash = (uint)(color.R * 73856093 ^ color.G * 19349663 ^ color.B * 83492791 ^ index * 2654435761);
			return (hash & 0x00FFFFFF) / 16777215f;
		}

		static byte ClampToByte(int value)
		{
			if (value < 0)
				return 0;
			if (value > 255)
				return 255;
			return (byte)value;
		}
	}
}
