using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets
{
	public static class ProductionIconButtonizer
	{
		public const int BarHeight = 8;
		const int TextPadding = 2;
		const int VerticalOffset = 1;

		static readonly Color DefaultBarTopColor = Color.FromArgb(150, 70, 70, 70);
		static readonly Color DefaultBarBottomColor = Color.FromArgb(180, 40, 40, 40);
		static readonly Color DefaultTextLight = Color.FromArgb(235, 235, 235);
		static readonly Color DefaultTextDark = Color.FromArgb(40, 40, 40);

		static readonly Dictionary<ProductionButtonStyle, ButtonVisualStyle> ButtonStyles = new()
		{
			{ ProductionButtonStyle.Ra2, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(200, 0, 0, 0),
				topEdge: Color.FromArgb(120, 245, 245, 245),
				rightEdge: Color.FromArgb(190, 220, 220, 220),
				bottomEdge: Color.FromArgb(160, 10, 10, 10),
				topHighlight: Color.FromArgb(70, 255, 255, 255),
				topHighlightHeight: 1,
				barTopColor: DefaultBarTopColor,
				barBottomColor: DefaultBarBottomColor,
				textLight: DefaultTextLight,
				textDark: DefaultTextDark) },
			{ ProductionButtonStyle.Ra1, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(255, 255, 255, 255),
				topEdge: Color.FromArgb(235, 235, 235, 235),
				rightEdge: Color.FromArgb(40, 40, 40, 40),
				bottomEdge: Color.FromArgb(20, 20, 20, 20),
				topHighlight: Color.FromArgb(60, 255, 255, 255),
				topHighlightHeight: 1,
				barTopColor: Color.FromArgb(180, 70, 70, 70),
				barBottomColor: Color.FromArgb(200, 40, 40, 40),
				textLight: Color.FromArgb(240, 240, 240),
				textDark: Color.FromArgb(20, 20, 20)) },
			{ ProductionButtonStyle.Td, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(245, 250, 235, 200),
				topEdge: Color.FromArgb(235, 245, 225, 190),
				rightEdge: Color.FromArgb(80, 70, 50, 25),
				bottomEdge: Color.FromArgb(60, 55, 35, 15),
				topHighlight: Color.FromArgb(70, 255, 235, 180),
				topHighlightHeight: 1,
				barTopColor: Color.FromArgb(170, 120, 95, 55),
				barBottomColor: Color.FromArgb(190, 85, 60, 30),
				textLight: Color.FromArgb(240, 235, 210),
				textDark: Color.FromArgb(35, 25, 10)) },
			{ ProductionButtonStyle.Ts, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(180, 120, 175, 190),
				topEdge: Color.FromArgb(180, 145, 205, 215),
				rightEdge: Color.FromArgb(170, 40, 80, 100),
				bottomEdge: Color.FromArgb(170, 30, 60, 80),
				topHighlight: Color.FromArgb(70, 180, 230, 255),
				topHighlightHeight: 1,
				barTopColor: Color.FromArgb(150, 50, 80, 95),
				barBottomColor: Color.FromArgb(190, 35, 55, 70),
				textLight: Color.FromArgb(220, 235, 240),
				textDark: Color.FromArgb(25, 35, 45)) },
			{ ProductionButtonStyle.None, new ButtonVisualStyle(
				borderThickness: 0,
				leftEdge: Color.Transparent,
				topEdge: Color.Transparent,
				rightEdge: Color.Transparent,
				bottomEdge: Color.Transparent,
				topHighlight: null,
				topHighlightHeight: 0,
				barTopColor: DefaultBarTopColor,
				barBottomColor: DefaultBarBottomColor,
				textLight: DefaultTextLight,
				textDark: DefaultTextDark) },
		};

		public static void Draw(ProductionIcon icon, Rectangle rect, string fallbackText, string fallbackFont)
		{
			if (icon == null || !icon.Buttonize)
				return;

			var style = ResolveStyle(icon.ButtonStyle);

			var fontName = icon.ButtonLabelFont ?? fallbackFont;
			var useOsFont = OsShpCameoFontRenderer.CanHandle(fontName);
			var frameRect = useOsFont
				? new Rectangle(rect.Left - 1, rect.Top - 1, rect.Width + 2, rect.Height + 2)
				: rect;
			DrawFrame(frameRect, style);

			var label = icon.ButtonLabel ?? fallbackText;
			if (string.IsNullOrEmpty(label))
				return;

			var labelRect = rect;
			if (icon.ButtonLabelOffset != float2.Zero)
				labelRect = new Rectangle(
					rect.Left + (int)icon.ButtonLabelOffset.X,
					rect.Top + (int)icon.ButtonLabelOffset.Y,
					rect.Width,
					rect.Height);
			DrawLabel(labelRect, label, fontName, fallbackFont, style);
		}

		static ButtonVisualStyle ResolveStyle(ProductionButtonStyle style)
		{
			if (!ButtonStyles.TryGetValue(style, out var resolved))
				resolved = ButtonStyles[ProductionButtonStyle.Ra2];

			return resolved;
		}

		static void DrawFrame(Rectangle rect, ButtonVisualStyle style)
		{
			var thickness = Math.Min(style.BorderThickness, Math.Min(rect.Width / 2, rect.Height / 2));
			if (thickness > 0)
			{
				WidgetUtils.FillRectWithColor(new Rectangle(rect.Left, rect.Top, rect.Width, thickness), style.TopEdge);
				WidgetUtils.FillRectWithColor(new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), style.BottomEdge);

				var sideHeight = Math.Max(0, rect.Height - 2 * thickness);
				if (sideHeight > 0)
				{
					WidgetUtils.FillRectWithColor(new Rectangle(rect.Left, rect.Top + thickness, thickness, sideHeight), style.LeftEdge);
					WidgetUtils.FillRectWithColor(new Rectangle(rect.Right - thickness, rect.Top + thickness, thickness, sideHeight), style.RightEdge);
				}
			}

			if (style.TopHighlight.HasValue)
			{
				var highlightHeight = Math.Min(style.TopHighlightHeight, Math.Max(0, thickness));
				if (highlightHeight > 0)
				{
					var highlightRect = new Rectangle(
						rect.Left,
						rect.Top,
						rect.Width,
						highlightHeight);
					WidgetUtils.FillRectWithColor(highlightRect, style.TopHighlight.Value);
				}
			}
		}

		static void DrawLabel(Rectangle rect, string label, string fontName, string fallbackFont, ButtonVisualStyle style)
		{
			if (OsShpCameoFontRenderer.CanHandle(fontName) && OsShpCameoFontRenderer.TryDraw(label, rect, style))
				return;

			if (!Game.Renderer.Fonts.TryGetValue(fontName, out var font))
				font = Game.Renderer.Fonts[fallbackFont];

			var barHeight = Math.Max(4, Math.Min(BarHeight, rect.Height / 3));
			var bar = new Rectangle(rect.Left, rect.Bottom - barHeight + VerticalOffset, rect.Width + VerticalOffset, barHeight);
			WidgetUtils.FillRectWithColor(bar, style.BarTopColor, style.BarTopColor, style.BarBottomColor, style.BarBottomColor);

			var maxWidth = Math.Max(0, bar.Width - TextPadding * 2);
			var text = WidgetUtils.TruncateText(label, maxWidth, font);
			var textSize = font.Measure(text);
			var x = bar.Left + (bar.Width - textSize.X) / 2;
			var y = bar.Top + (bar.Height - textSize.Y) / 2;
			var pos = new int2(x, y);

			font.DrawText(text, pos + new int2(-1, 0), style.TextDark);
			font.DrawText(text, pos + new int2(1, 0), style.TextDark);
			font.DrawText(text, pos + new int2(0, -1), style.TextDark);
			font.DrawText(text, pos + new int2(0, 1), style.TextDark);
			font.DrawText(text, pos, style.TextLight);
		}

		static class OsShpCameoFontRenderer
		{
			public const string FontKey = "OsShp";
			const string Asset = "ca|uibits/osshp-font.png";
			const int TileWidth = 20;
			const int TileHeight = 12;
			const int VisibleHeight = 6;
			const int SpaceAdvance = 3;
			const int MaxCharsPerLine = 14;
			const int MaxLines = 2;
			static readonly object Sync = new();
			static Sprite[] glyphs;
			static int[] advances;
			static Sheet glyphSheet;
			static bool failed;

			public static bool CanHandle(string fontName)
			{
				return !string.IsNullOrEmpty(fontName) && fontName.Equals(FontKey, StringComparison.OrdinalIgnoreCase);
			}

			public static bool TryDraw(string label, Rectangle rect, ButtonVisualStyle style)
			{
				if (!EnsureGlyphs())
					return false;

				var normalized = Normalize(label);
				if (string.IsNullOrEmpty(normalized))
					return false;

				var lines = Wrap(normalized);
				if (lines.Count == 0)
					return false;

				var padding = lines.Count == 1 ? 1 : 4;
				var textHeight = lines.Count * VisibleHeight;
				var barHeight = Math.Min(rect.Height, textHeight + padding);
				if (lines.Count > 1)
					barHeight = Math.Max(4, barHeight - 2);
				var bar = new Rectangle(rect.Left, rect.Bottom - barHeight + VerticalOffset, rect.Width + VerticalOffset, barHeight);
				WidgetUtils.FillRectWithColor(bar, style.BarTopColor, style.BarTopColor, style.BarBottomColor, style.BarBottomColor);

				var firstLineTop = bar.Bottom - textHeight;
				for (var i = 0; i < lines.Count; i++)
				{
					var lineTop = firstLineTop + i * VisibleHeight;
					DrawLine(lines[i], bar, lineTop);
				}

				return true;
			}

			static void DrawLine(string text, Rectangle bar, int lineTop)
			{
				var runs = BuildGlyphRuns(text);
				if (runs.Count == 0)
					return;

				var totalWidth = 0;
				foreach (var run in runs)
					totalWidth += run.Advance;

				var x = bar.Left + (bar.Width - totalWidth) / 2;
				var y = lineTop;

				foreach (var run in runs)
				{
					if (run.Sprite != null)
						WidgetUtils.DrawSprite(run.Sprite, new float2(x, y));

					x += run.Advance;
				}
			}

			static List<GlyphRun> BuildGlyphRuns(string text)
			{
				var runs = new List<GlyphRun>(text.Length);
				foreach (var ch in text)
				{
					if (ch == ' ')
					{
						runs.Add(GlyphRun.Space);
						continue;
					}

					var index = MapIndex(ch);
					if (index < 0 || index >= glyphs.Length)
						continue;

					var sprite = glyphs[index];
					var advance = advances[index];
					if (sprite == null || advance == 0)
						continue;

					runs.Add(new GlyphRun(sprite, advance));
				}

				return runs;
			}

			static int MapIndex(char c)
			{
				if (c >= '0' && c <= '9')
					return c - '0';
				if (c >= 'A' && c <= 'Z')
					return c - 'A' + 10;
				if (c == '.')
					return 36;
				return -1;
			}

			static string Normalize(string text)
			{
				if (string.IsNullOrWhiteSpace(text))
					return string.Empty;

				var builder = new StringBuilder(text.Length);
				var pendingSpace = false;
				foreach (var ch in text)
				{
					var upper = char.ToUpperInvariant(ch);
					if ((upper >= 'A' && upper <= 'Z') || (upper >= '0' && upper <= '9'))
					{
						builder.Append(upper);
						pendingSpace = false;
					}
					else if (upper == '.')
					{
						builder.Append('.');
						pendingSpace = false;
					}
					else if (char.IsWhiteSpace(ch) || upper == '-' || upper == '_' || upper == '/')
					{
						if (!pendingSpace && builder.Length > 0)
						{
							builder.Append(' ');
							pendingSpace = true;
						}
					}
				}

				if (builder.Length == 0)
					return string.Empty;

				if (builder[^1] == ' ')
					builder.Length--;

				return builder.ToString();
			}

			static List<string> Wrap(string text)
			{
				var lines = new List<string>(MaxLines);
				var remaining = text.Trim();

				while (!string.IsNullOrEmpty(remaining) && lines.Count < MaxLines)
				{
					if (remaining.Length <= MaxCharsPerLine)
					{
						lines.Add(remaining);
						remaining = string.Empty;
						break;
					}

					var breakIndex = remaining.LastIndexOf(' ', MaxCharsPerLine - 1);
					var brokeAtSpace = breakIndex > 0;
					if (!brokeAtSpace)
						breakIndex = MaxCharsPerLine;

					var sliceLength = Math.Min(breakIndex, remaining.Length);
					var line = remaining.Substring(0, sliceLength).TrimEnd();
					if (!string.IsNullOrEmpty(line))
						lines.Add(line);

					if (sliceLength >= remaining.Length)
					{
						remaining = string.Empty;
						break;
					}

					var nextStart = brokeAtSpace ? Math.Min(breakIndex + 1, remaining.Length) : sliceLength;
					remaining = remaining.Substring(nextStart).TrimStart();
				}

				if (!string.IsNullOrEmpty(remaining) && lines.Count < MaxLines)
					lines.Add(remaining.Length > MaxCharsPerLine ? remaining.Substring(0, MaxCharsPerLine) : remaining);

				return lines;
			}

			static bool EnsureGlyphs()
			{
				if (glyphs != null)
					return true;
				if (failed || Game.ModData == null)
					return false;

				lock (Sync)
				{
					if (glyphs != null)
						return true;
					if (failed || Game.ModData == null)
						return false;

					try
					{
					using var stream = Game.ModData.DefaultFileSystem.Open(Asset);
					var sheet = LoadFontSheet(stream);
					ApplyTransparency(sheet);
					BuildGlyphSprites(sheet);
					return true;
					}
					catch
					{
						failed = true;
						return false;
					}
				}
			}

			static Sheet LoadFontSheet(Stream stream)
			{
				var png = new Png(stream);
				var paddedWidth = NextPowerOfTwo(png.Width);
				var paddedHeight = NextPowerOfTwo(png.Height);
				var sheet = new Sheet(SheetType.BGRA, new Size(paddedWidth, paddedHeight));
				var destBytes = sheet.GetData();
				var destPixels = MemoryMarshal.Cast<byte, uint>(destBytes.AsSpan());
				var sentinel = Color.FromArgb(0, 0, 255).ToArgb();
				for (var i = 0; i < destPixels.Length; i++)
					destPixels[i] = sentinel;

				var sprite = new Sprite(sheet, new Rectangle(0, 0, png.Width, png.Height), TextureChannel.Red);
				OpenRA.Graphics.Util.FastCopyIntoSprite(sprite, png);

				sheet.CommitBufferedData();
				return sheet;
			}

			static int NextPowerOfTwo(int value)
			{
				var result = 1;
				while (result < value)
					result <<= 1;
				return result;
			}

			static void ApplyTransparency(Sheet sheet)
			{
				var colors = MemoryMarshal.Cast<byte, uint>(sheet.GetData().AsSpan());
				for (var i = 0; i < colors.Length; i++)
				{
				var value = colors[i];
				if ((value & 0x00FFFFFF) == 0x0000FF)
					colors[i] = 0;
				}

				sheet.CommitBufferedData();
			}

			static void BuildGlyphSprites(Sheet sheet)
			{
				glyphSheet = sheet;
				var columns = sheet.Size.Width / TileWidth;
				var rows = sheet.Size.Height / TileHeight;
				var total = columns * rows;
				glyphs = new Sprite[total];
				advances = new int[total];

				var pixels = MemoryMarshal.Cast<byte, uint>(sheet.GetData().AsSpan());
				var stride = sheet.Size.Width;
				for (var row = 0; row < rows; row++)
				{
					for (var col = 0; col < columns; col++)
					{
						var index = row * columns + col;
						var tileX = col * TileWidth;
						var tileY = row * TileHeight;
						var width = MeasureGlyphWidth(pixels, stride, tileX, tileY);
						if (width <= 0)
							continue;

						width = Math.Max(2, Math.Min(width, TileWidth));
						var bounds = new Rectangle(tileX, tileY, width, VisibleHeight);
						glyphs[index] = new Sprite(sheet, bounds, TextureChannel.RGBA);
						advances[index] = width;
					}
				}
			}

			static int MeasureGlyphWidth(Span<uint> pixels, int stride, int tileX, int tileY)
			{
				var width = 0;
				for (var x = 0; x < TileWidth; x++)
				{
					var hasPixel = false;
					for (var y = 0; y < VisibleHeight; y++)
					{
						var idx = (tileY + y) * stride + tileX + x;
						if ((pixels[idx] & 0xFF000000) != 0)
						{
							hasPixel = true;
							break;
						}
					}

					if (hasPixel)
						width = x + 1;
				}

				return width;
			}

			readonly struct GlyphRun
			{
				public readonly Sprite Sprite;
				public readonly int Advance;

				public GlyphRun(Sprite sprite, int advance)
				{
					Sprite = sprite;
					Advance = advance;
				}

				public static GlyphRun Space => new(null, SpaceAdvance);
			}
		}

		readonly struct ButtonVisualStyle
		{
			public readonly int BorderThickness;
			public readonly Color LeftEdge;
			public readonly Color TopEdge;
			public readonly Color RightEdge;
			public readonly Color BottomEdge;
			public readonly Color? TopHighlight;
			public readonly int TopHighlightHeight;
			public readonly Color BarTopColor;
			public readonly Color BarBottomColor;
			public readonly Color TextLight;
			public readonly Color TextDark;

			public ButtonVisualStyle(
				int borderThickness,
				Color leftEdge,
				Color topEdge,
				Color rightEdge,
				Color bottomEdge,
				Color? topHighlight,
				int topHighlightHeight,
				Color barTopColor,
				Color barBottomColor,
				Color textLight,
				Color textDark)
			{
				BorderThickness = borderThickness;
				LeftEdge = leftEdge;
				TopEdge = topEdge;
				RightEdge = rightEdge;
				BottomEdge = bottomEdge;
				TopHighlight = topHighlight;
				TopHighlightHeight = topHighlightHeight;
				BarTopColor = barTopColor;
				BarBottomColor = barBottomColor;
				TextLight = textLight;
				TextDark = textDark;
			}
		}
	}
}
