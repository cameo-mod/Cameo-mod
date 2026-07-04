#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.IO;
using OpenRA.FileSystem;
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

		// Baked cyberintel theme variants in uibits/cyberintel-themes/ (<name>-ui.png + <name>-dialog.png).
		readonly string[] uiThemes = ["cyan", "green", "amber", "orange", "blue", "white"];

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
					() => { cameoSettings.UITheme = o; cameoSettings.Save(); ApplyUITheme(o); });

				item.Get<LabelWidget>("LABEL").GetText = () => FirstUpper(o);
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, uiThemes, SetupItem);
		}

		// Copies the selected theme's baked chrome sheets over the active cyberintel-ui.png / dialog.png.
		// Chrome is loaded once at startup, so this only takes visible effect after a restart. Best-effort:
		// a read-only install location will throw on copy, which we log rather than surface. The setting
		// itself always persists, so the copy can be reattempted (or the files shipped correctly) later.
		static void ApplyUITheme(string name)
		{
			try
			{
				if (!Game.ModData.DefaultFileSystem.TryGetPackageContaining("cyberintel-ui.png", out var package, out _) || package is not Folder folder)
					return;

				var dir = folder.Name;
				var themeDir = Path.Combine(dir, "cyberintel-themes");
				File.Copy(Path.Combine(themeDir, name + "-ui.png"), Path.Combine(dir, "cyberintel-ui.png"), true);
				File.Copy(Path.Combine(themeDir, name + "-dialog.png"), Path.Combine(dir, "dialog.png"), true);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to apply UI theme '" + name + "': " + e);
			}
		}

		static string FirstUpper(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
	}
}
