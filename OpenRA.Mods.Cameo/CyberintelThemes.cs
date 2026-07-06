#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo
{
	// Shared helper for the cyberintel UI colour theme. The baked variants ship in the tracked
	// uibits/cyberintel-themes/<name>-ui.png + <name>-dialog.png. Applying a theme copies the chosen
	// pair into a writable SupportDir folder (Content/cameo/theme) that mod.yaml mounts AFTER
	// cameo|uibits, so the copies shadow the shipped cyberintel-ui.png / dialog.png by bare name
	// without ever touching the tracked files. Chrome is loaded once at launch, so a write only takes
	// visible effect on a boot where the override file already existed at mount time — i.e. the next
	// restart for a dropdown pick, and (for "random") from the second boot onward.
	public static class CyberintelThemes
	{
		public const string Random = "random";

		// The stock-OpenRA look: classic-dialog.png is the original grey chrome, and the main menu
		// reverts its neon panels/buttons to the stock dialog/button regions (see CameoMainMenuLogic).
		public const string Classic = "classic";

		// Imported native skin: the Dune 2000 UI sheet, re-baked into Cameo's dialog.png geometry (its
		// region layout is common-derived, so structurally compatible). Like "classic" it uses the
		// native stock-chrome menu drawn from its own dialog.png.
		public const string Dune = "d2k";

		// The concrete themes, and also the pool "random" draws from.
		public static readonly string[] Colours =
			["cyan", "green", "amber", "orange", "blue", "white", "red", "purple", "magenta", "d2k", "classic"];

		// Dropdown options: the concrete themes plus the special "random" entry.
		public static readonly string[] Options =
			["cyan", "green", "amber", "orange", "blue", "white", "red", "purple", "magenta", "d2k", "classic", Random];

		// Themes that use the mod's native stock-chrome menu (drawn from the themed dialog.png) rather
		// than the cyberintel neon menu sheet: the pristine OpenRA look plus the imported d2k skin,
		// whose authentic menu is simply its own dialog panels and buttons.
		public static bool UsesNativeChrome(string theme) =>
			theme == Classic || theme == Dune;

		// Human-facing dropdown label; the raw id for the imported skin is terse ("d2k").
		public static string DisplayName(string option)
		{
			switch (option)
			{
				case Dune: return "Dune 2000";
				default: return string.IsNullOrEmpty(option) ? option : char.ToUpperInvariant(option[0]) + option[1..];
			}
		}

		// The concrete theme resolved by the most recent Apply() this process — set by the loadscreen
		// at boot for the "random" case, so the menu can colour its text to match the actual pick
		// (the setting itself just says "random"). Null when no Apply ran (e.g. a fixed theme, where
		// the setting already names the concrete theme).
		public static string CurrentTheme { get; private set; }

		public static void Apply(string name, IReadOnlyFileSystem fileSystem)
		{
			var theme = name == Random ? Colours[Game.CosmeticRandom.Next(Colours.Length)] : name;
			CurrentTheme = theme;

			try
			{
				// Anchor on a uibits-only top-level file (cameologo.png) to find the tracked preset
				// source folder — resolving via cyberintel-ui.png would return the SupportDir override
				// once it exists, not uibits.
				if (!fileSystem.TryGetPackageContaining("cameologo.png", out var package, out _) || package is not Folder uibits)
					return;

				var themeDir = Path.Combine(uibits.Name, "cyberintel-themes");
				var outDir = Platform.ResolvePath("^SupportDir|Content/cameo/theme");
				Directory.CreateDirectory(outDir);

				File.Copy(Path.Combine(themeDir, theme + "-ui.png"), Path.Combine(outDir, "cyberintel-ui.png"), true);
				File.Copy(Path.Combine(themeDir, theme + "-dialog.png"), Path.Combine(outDir, "dialog.png"), true);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Cameo UI theme apply failed for '" + name + "': " + e);
			}
		}

		// Menu title + body text colours per theme, matched to each sheet's neon family, so the menu
		// text tracks the selected colour. Returns false for unknown themes and for "classic" (which
		// resets to the stock chrome colours in CameoMainMenuLogic instead).
		public static bool TryGetMenuColours(string theme, out Color title, out Color text)
		{
			(int, int, int) t, b;
			switch (theme)
			{
				case "cyan": t = (190, 246, 255); b = (120, 224, 244); break;
				case "green": t = (190, 255, 190); b = (130, 235, 150); break;
				case "amber": t = (255, 236, 180); b = (238, 198, 112); break;
				case "orange": t = (255, 206, 150); b = (248, 168, 110); break;
				case "blue": t = (200, 224, 255); b = (150, 196, 252); break;
				case "white": t = (255, 255, 255); b = (210, 218, 230); break;
				case "red": t = (255, 206, 190); b = (248, 150, 138); break;
				case "purple": t = (224, 206, 255); b = (192, 160, 240); break;
				case "magenta": t = (255, 200, 236); b = (248, 144, 208); break;
				default: title = default; text = default; return false;
			}

			title = Color.FromArgb(t.Item1, t.Item2, t.Item3);
			text = Color.FromArgb(b.Item1, b.Item2, b.Item3);
			return true;
		}
	}
}
