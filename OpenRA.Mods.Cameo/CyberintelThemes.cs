#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.IO;
using OpenRA.FileSystem;

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

		// The concrete themes, and also the pool "random" draws from.
		public static readonly string[] Colours =
			["cyan", "green", "amber", "orange", "blue", "white", "red", "purple", "magenta", "classic"];

		// Dropdown options: the concrete themes plus the special "random" entry.
		public static readonly string[] Options =
			["cyan", "green", "amber", "orange", "blue", "white", "red", "purple", "magenta", "classic", Random];

		public static void Apply(string name, IReadOnlyFileSystem fileSystem)
		{
			var theme = name == Random ? Colours[Game.CosmeticRandom.Next(Colours.Length)] : name;

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
	}
}
