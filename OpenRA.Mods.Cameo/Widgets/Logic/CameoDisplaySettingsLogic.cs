#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	// Adds the cyberintel "UI Colour Theme" dropdown to the Display settings panel. Attached as a
	// SECOND Logic on DISPLAY_PANEL (alongside the engine's DisplaySettingsLogic) so it can bind a
	// Cameo-only widget without editing the engine: the panel widget is injected as the ctor's
	// `widget` arg (Widget.PostInit), and it reads CameoSettings from the mod assembly.
	public class CameoDisplaySettingsLogic : ChromeLogic
	{
		readonly CameoSettings cameoSettings;

		[ObjectCreator.UseCtor]
		public CameoDisplaySettingsLogic(Widget widget)
		{
			cameoSettings = Game.ModData.GetSettings<CameoSettings>();

			var uiThemeDropDown = widget.GetOrNull<DropDownButtonWidget>("UI_THEME_DROP_DOWN");
			if (uiThemeDropDown == null)
				return;

			uiThemeDropDown.OnMouseDown = _ => ShowUIThemeDropdown(uiThemeDropDown);
			uiThemeDropDown.GetText = () => FirstUpper(cameoSettings.UITheme);
		}

		void ShowUIThemeDropdown(DropDownButtonWidget dropdown)
		{
			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => cameoSettings.UITheme == o,
					() =>
					{
						cameoSettings.UITheme = o;
						cameoSettings.Save();

						// Concrete colours are copied over the active sheets now (visible next restart).
						// "random" is left to the loadscreen, which re-picks a colour on every boot.
						if (o != CyberintelThemes.Random)
							CyberintelThemes.Apply(o, Game.ModData.DefaultFileSystem);
					});

				item.Get<LabelWidget>("LABEL").GetText = () => FirstUpper(o);
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, CyberintelThemes.Options, SetupItem);
		}

		static string FirstUpper(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
	}
}
