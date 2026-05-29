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
using System.Reflection;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ButtonTooltipLogic : ChromeLogic
	{
		const string ColorTagOpen = "<color=";
		const string ColorTagClose = "</color>";

		[ObjectCreator.UseCtor]
		public ButtonTooltipLogic(Widget widget, ButtonWidget button)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var font = Game.Renderer.Fonts[label.Font];
			var text = StripInlineColorTags(button.GetTooltipText() ?? string.Empty);
			var labelWidth = font.Measure(text).X;
			var key = button.Key.GetValue();

			label.GetText = () => text;
			label.Bounds.Width = labelWidth;
			widget.Bounds.Width = 2 * label.Bounds.X + labelWidth;

			if (key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = $"({key.DisplayString()})";
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Bounds.X = labelWidth + 2 * label.Bounds.X;

				widget.Bounds.Width = hotkey.Bounds.X + label.Bounds.X + font.Measure(hotkeyLabel).X;
			}

			var desc = button.GetTooltipDesc();
			if (!string.IsNullOrEmpty(desc))
			{
				var descTemplate = widget.Get<LabelWidget>("DESC");
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = descTemplate.Bounds.Y;
				foreach (var line in desc.Split('\n', StringSplitOptions.None))
				{
					if (TryExtractInlineColor(line, out var prefix, out var highlighted, out var suffix, out var colorToken)
						&& TryParseColorToken(colorToken, out var highlightColor))
					{
						var plainLine = prefix + highlighted + suffix;
						descWidth = Math.Max(descWidth, descFont.Measure(plainLine).X);
						var segmentX = descTemplate.Bounds.X;

						if (!string.IsNullOrEmpty(prefix))
						{
							var prefixLabel = (LabelWidget)descTemplate.Clone();
							prefixLabel.GetText = () => prefix;
							prefixLabel.Bounds.X = segmentX;
							prefixLabel.Bounds.Y = descOffset;
							widget.AddChild(prefixLabel);
							segmentX += descFont.Measure(prefix).X;
						}

						if (!string.IsNullOrEmpty(highlighted))
						{
							var highlightedLabel = (LabelWidget)descTemplate.Clone();
							highlightedLabel.GetText = () => highlighted;
							highlightedLabel.GetColor = () => highlightColor;
							highlightedLabel.Bounds.X = segmentX;
							highlightedLabel.Bounds.Y = descOffset;
							widget.AddChild(highlightedLabel);
							segmentX += descFont.Measure(highlighted).X;
						}

						if (!string.IsNullOrEmpty(suffix))
						{
							var suffixLabel = (LabelWidget)descTemplate.Clone();
							suffixLabel.GetText = () => suffix;
							suffixLabel.Bounds.X = segmentX;
							suffixLabel.Bounds.Y = descOffset;
							widget.AddChild(suffixLabel);
						}
					}
					else
					{
						var plainLine = StripInlineColorTags(line);
						descWidth = Math.Max(descWidth, descFont.Measure(plainLine).X);
						var lineLabel = (LabelWidget)descTemplate.Clone();
						lineLabel.GetText = () => plainLine;
						lineLabel.Bounds.Y = descOffset;
						widget.AddChild(lineLabel);
					}

					descOffset += descTemplate.Bounds.Height;
				}

				widget.Bounds.Width = Math.Max(widget.Bounds.Width, descTemplate.Bounds.X * 2 + descWidth);
				widget.Bounds.Height += descOffset - descTemplate.Bounds.Y + descTemplate.Bounds.X;
			}
		}

		static string StripInlineColorTags(string input)
		{
			if (TryExtractInlineColor(input, out var prefix, out var highlighted, out var suffix, out _))
				return prefix + highlighted + suffix;

			return input;
		}

		static bool TryExtractInlineColor(string input, out string prefix, out string highlighted, out string suffix, out string colorToken)
		{
			prefix = highlighted = suffix = colorToken = null;
			if (string.IsNullOrEmpty(input))
				return false;

			var openStart = input.IndexOf(ColorTagOpen, StringComparison.OrdinalIgnoreCase);
			if (openStart < 0)
				return false;

			var openEnd = input.IndexOf('>', openStart + ColorTagOpen.Length);
			if (openEnd < 0)
				return false;

			var closeStart = input.IndexOf(ColorTagClose, openEnd + 1, StringComparison.OrdinalIgnoreCase);
			if (closeStart < 0)
				return false;

			prefix = input[..openStart];
			colorToken = input[(openStart + ColorTagOpen.Length)..openEnd].Trim();
			highlighted = input[(openEnd + 1)..closeStart];
			suffix = input[(closeStart + ColorTagClose.Length)..];
			return true;
		}

		static bool TryParseColorToken(string token, out Color color)
		{
			color = default;
			if (string.IsNullOrWhiteSpace(token))
				return false;

			var normalized = token.Trim();
			if (normalized.StartsWith("#", StringComparison.Ordinal))
				normalized = normalized[1..];

			if (Color.TryParse(normalized, out color))
				return true;

			var property = typeof(Color).GetProperty(normalized, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
			if (property == null || property.PropertyType != typeof(Color))
				return false;

			color = (Color)property.GetValue(null);
			return true;
		}
	}
}
