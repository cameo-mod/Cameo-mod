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
	// about. Attached as a second Logic on the main-menu container so the engine's MainMenuLogic
	// stays untouched. The Statistics window is a self-contained modal overlay (it dims and
	// blocks the menu behind it), so there is no need to drive the menu's private show/hide state.
	public class CameoMainMenuLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public CameoMainMenuLogic(Widget widget, World world)
		{
			var statisticsButton = widget.GetOrNull<ButtonWidget>("STATISTICS_BUTTON");
			if (statisticsButton != null)
				statisticsButton.OnClick = () => Ui.OpenWindow("STATISTICS_PANEL", new WidgetArgs
				{
					{ "world", world }
				});

			// The "classic" UI theme reverts the menu to the stock OpenRA look: swap the cyberintel
			// neon panels/buttons back to the stock dialog/button chrome and drop the themed text
			// colours. The other themes keep the cyberintel menu chrome (the theme only recolours it).
			if (Game.ModData.GetSettings<CameoSettings>().UITheme == CyberintelThemes.Classic)
				RevertToClassicChrome(widget);
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
