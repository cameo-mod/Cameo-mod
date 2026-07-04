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

using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	// Wires up the Cameo-specific main-menu buttons that the shared MainMenuLogic does not know
	// about, and applies the selected cyberintel UI theme to the menu chrome/text. Attached as a
	// second Logic on the main-menu container so the engine's MainMenuLogic stays untouched.
	public class CameoMainMenuLogic : ChromeLogic
	{
		// The menu title labels that carry a themed colour (their bodies/buttons are handled by walk).
		static readonly string[] TitleIds =
			["MAINMENU_LABEL_TITLE", "SINGLEPLAYER_MENU_TITLE", "EXTRAS_MENU_TITLE", "MAP_EDITOR_MENU_TITLE"];

		[ObjectCreator.UseCtor]
		public CameoMainMenuLogic(Widget widget, World world)
		{
			var statisticsButton = widget.GetOrNull<ButtonWidget>("STATISTICS_BUTTON");
			if (statisticsButton != null)
				statisticsButton.OnClick = () => Ui.OpenWindow("STATISTICS_PANEL", new WidgetArgs
				{
					{ "world", world }
				});

			// Resolve the active theme. When the setting is "random" the concrete pick lives in the
			// static CurrentTheme (set by the loadscreen at boot); a fixed theme names itself.
			var theme = Game.ModData.GetSettings<CameoSettings>().UITheme;
			if (theme == CyberintelThemes.Random)
				theme = CyberintelThemes.CurrentTheme;

			// "classic" reverts the menu to the stock OpenRA look; every other theme recolours the
			// cyberintel menu text to match its neon family (the panels/buttons are already coloured
			// by the swapped sheet).
			if (theme == CyberintelThemes.Classic)
				RevertToClassicChrome(widget);
			else if (theme != null && CyberintelThemes.TryGetMenuColours(theme, out var title, out var text))
			{
				foreach (var id in TitleIds)
				{
					var label = widget.GetOrNull<LabelWidget>(id);
					if (label != null)
						label.TextColor = title;
				}

				SetButtonTextColour(widget, text);
			}
		}

		static void SetButtonTextColour(Widget widget, Color text)
		{
			foreach (var child in widget.Children)
			{
				if (child is ButtonWidget b && b.Background == "cyberintel-button")
					b.TextColor = text;

				SetButtonTextColour(child, text);
			}
		}

		static void RevertToClassicChrome(Widget widget)
		{
			foreach (var child in widget.Children)
			{
				switch (child)
				{
					case ButtonWidget b when b.Background == "cyberintel-button":
						b.Background = "button";
						b.TextColor = ChromeMetrics.Get<Color>("ButtonTextColor");
						break;
					case BackgroundWidget bg when bg.Background == "cyberintel-panel":
						bg.Background = "dialog";
						break;
					case LabelWidget l:
						l.TextColor = ChromeMetrics.Get<Color>("TextColor");
						break;
				}

				RevertToClassicChrome(child);
			}
		}
	}
}
