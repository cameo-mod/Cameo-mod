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
	// Shared helper for the cyberintel UI colour theme. The baked variants live in
	// uibits/cyberintel-themes/<name>-ui.png + <name>-dialog.png; applying a theme copies the chosen
	// pair over the active cyberintel-ui.png / dialog.png. Chrome is loaded once at launch, so a copy
	// only takes visible effect after a restart — hence the two callers: the Display-settings dropdown
	// (writes the choice, applied next restart) and the loadscreen (re-applies "random" every boot,
	// before ChromeProvider initialises, so a fresh colour shows this boot).
	public static class CyberintelThemes
	{
		public const string Random = "random";

		public static readonly string[] Colours = ["cyan", "green", "amber", "orange", "blue", "white"];

		// Dropdown options: the concrete colours plus the special "random" entry.
		public static readonly string[] Options = ["cyan", "green", "amber", "orange", "blue", "white", Random];

		public static void Apply(string name, IReadOnlyFileSystem fileSystem)
		{
			var theme = name == Random ? Colours[Game.CosmeticRandom.Next(Colours.Length)] : name;

			try
			{
				if (!fileSystem.TryGetPackageContaining("cyberintel-ui.png", out var package, out _) || package is not Folder folder)
					return;

				var dir = folder.Name;
				var themeDir = Path.Combine(dir, "cyberintel-themes");
				File.Copy(Path.Combine(themeDir, theme + "-ui.png"), Path.Combine(dir, "cyberintel-ui.png"), true);
				File.Copy(Path.Combine(themeDir, theme + "-dialog.png"), Path.Combine(dir, "dialog.png"), true);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Cameo UI theme apply failed for '" + name + "': " + e);
			}
		}
	}
}
