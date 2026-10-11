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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.UpdateRules;
using OpenRA.Mods.Common.UpdateRules.Rules;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	using YamlFileSet = List<(IReadWritePackage Package, string File, List<MiniYamlNodeBuilder> Nodes)>;

	/// <summary>
	/// --update-unified-traits: runs the MigrateUnifiedTraits update rule over the whole
	/// reachable corpus AND the dormant one. Unlike --update-mod this command also sweeps
	/// loose *.yaml/*.yml files under the mod root that no manifest list mounts (disabled
	/// ContentPacks, parked donor files) so a family flip cannot leave an orphaned old name
	/// behind. Files are classified by directory convention: under a "weapons" directory the
	/// weapon transform runs, everywhere else the actor transform runs, and a loose map.yaml
	/// gets its Rules/Weapons/Actors sections transformed. Packed .oramap files outside the
	/// enumerated map folders are reported as manual steps rather than repacked blindly.
	/// Dry run (no --apply) reports which files would change without writing anything.
	/// </summary>
	sealed class UpdateUnifiedTraitsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--update-unified-traits";

		bool IUtilityCommand.ValidateArguments(string[] args) { return true; }

		[Desc("[--apply] [--yes] [--skip-maps] [--skip-dormant]",
			"Rewrites yaml from suffixed implementation names to unified plain names for every " +
			"state=active entry in tools/audit/trait_aliases.json. Without --apply, prints which " +
			"files would change. The dormant sweep covers loose yaml no manifest mounts.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var rule = new MigrateUnifiedTraits();
			var apply = args.Contains("--apply");
			var skipMaps = args.Contains("--skip-maps");
			var skipDormant = args.Contains("--skip-dormant");

			if (apply && !args.Contains("--yes"))
			{
				Console.WriteLine("WARNING: This command will automatically rewrite your mod rules.");
				Console.WriteLine();
				Console.Write("Press y to continue, or any other key to cancel: ");
				if (Console.ReadKey().KeyChar != 'y')
				{
					Console.WriteLine("\nUpdate cancelled.");
					return;
				}

				Console.WriteLine();
			}

			var logWriter = File.CreateText("update-unified-traits.log");
			logWriter.AutoFlush = true;

			var externalFilenames = new HashSet<string>();
			var manualSteps = new List<string>();

			LogLine(logWriter, $"MigrateUnifiedTraits: {rule.Name}");
			LogLine(logWriter, $"   {rule.ActiveAliases.Count} active of {rule.AliasCount} aliases");

			YamlFileSet files;
			try
			{
				Log(logWriter, "   Updating mod... ");
				manualSteps.AddRange(UpdateUtils.UpdateMod(modData, rule, out files, externalFilenames));
				LogSuccess(logWriter, "COMPLETE");
			}
			catch (Exception ex)
			{
				LogError(logWriter, "FAILED");
				LogLine(logWriter, "   " + ex.ToString().Replace("\n", "\n     "));
				return;
			}

			if (!skipMaps)
			{
				Log(logWriter, "   Updating maps... ");
				foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
				{
					try
					{
						var mapSteps = UpdateUtils.UpdateMap(modData, package, rule, out var mapFiles, externalFilenames);
						files.AddRange(mapFiles);
						if (mapSteps.Count > 0)
							manualSteps.Add("Map: " + package.Name + ":\n" + UpdateUtils.FormatMessageList(mapSteps));
					}
					catch (Exception ex)
					{
						LogError(logWriter, "FAILED");
						LogLine(logWriter, "   map " + package.Name + ": " + ex.ToString().Replace("\n", "\n     "));
						return;
					}
				}

				LogSuccess(logWriter, "COMPLETE");
			}

			var changed = files.Where(f => f.Package != null && FileChanged(f.Package, f.File, f.Nodes))
				.Select(f => f.Package.Name + ":" + f.File)
				.ToList();

			if (!skipDormant)
				manualSteps.AddRange(SweepDormant(modData, rule, apply, changed, logWriter));
			else
				LogLine(logWriter, "   Dormant sweep SKIPPED (--skip-dormant)");

			foreach (var c in changed)
				LogLine(logWriter, $"   {(apply ? "updated" : "would update")} {c}");

			if (changed.Count == 0)
				LogLine(logWriter, "   no yaml changes needed");

			if (apply)
				files.Save();

			if (manualSteps.Count > 0)
			{
				LogWarning(logWriter, "   Manual steps:");
				LogLine(logWriter, UpdateUtils.FormatMessageList(manualSteps, 1));
			}

			if (externalFilenames.Count > 0)
			{
				LogLine(logWriter, "External mod files ignored (update the referenced mod):");
				LogLine(logWriter, UpdateUtils.FormatMessageList(externalFilenames));
			}

			Console.WriteLine(apply
				? "Semi-automated update complete. Messages are in update-unified-traits.log."
				: "Dry run only - rerun with --apply to write changes.");
		}

		static bool FileChanged(IReadWritePackage package, string file, List<MiniYamlNodeBuilder> nodes)
		{
			var text = Encoding.UTF8.GetBytes(nodes.WriteToString());
			using var stream = package.GetStream(file);
			return stream == null || !text.SequenceEqual(stream.ReadAllBytes());
		}

		/// <summary>
		/// Walks every loose yaml under the mod root that no manifest list mounts and no
		/// enumerated map package contains, applies the rule, and (with apply) rewrites the
		/// file in place. Returns manual steps and appends changed labels to changed.
		/// </summary>
		static List<string> SweepDormant(
			ModData modData, MigrateUnifiedTraits rule, bool apply, List<string> changed, StreamWriter log)
		{
			var manual = new List<string>();

			if (rule.ActiveAliases.Count == 0)
			{
				LogLine(log, "   Dormant sweep: 0 active aliases - nothing can rewrite");
				return manual;
			}

			var modRoot = modData.Manifest.Package.Name;
			if (!Directory.Exists(modRoot))
			{
				manual.Add($"dormant sweep: mod root '{modRoot}' is not a directory - skipped");
				return manual;
			}

			var mounted = MountedPaths(modData);
			var mapRoots = modData.MapCache.EnumerateMapPackagesWithoutCaching()
				.Select(p => Path.GetFullPath(p.Name))
				.ToList();

			var loose = Directory.EnumerateFiles(modRoot, "*.yaml", SearchOption.AllDirectories)
				.Concat(Directory.EnumerateFiles(modRoot, "*.yml", SearchOption.AllDirectories));

			var swept = 0;
			foreach (var file in loose)
			{
				var full = Path.GetFullPath(file);
				if (full.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj"))
					continue;
				if (mounted.Contains(full))
					continue;
				if (mapRoots.Any(r => full.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
					continue;

				swept++;
				List<MiniYamlNodeBuilder> nodes;
				try
				{
					using var stream = File.OpenRead(full);
					nodes = MiniYaml.FromStream(stream, full, false)
						.Select(n => new MiniYamlNodeBuilder(n))
						.ToList();
				}
				catch (Exception ex)
				{
					manual.Add($"dormant: cannot parse {full}: {ex.Message}");
					continue;
				}

				var steps = ApplyToDormant(nodes, rule, Path.GetFileName(full), IsWeaponsPath(full));
				if (nodes.WriteToString() != File.ReadAllText(full))
				{
					changed.Add("dormant:" + full);
					if (apply)
						File.WriteAllText(full, nodes.WriteToString());
				}

				manual.AddRange(steps.Select(s => $"dormant {full}: {s}"));
			}

			foreach (var pack in Directory.EnumerateFiles(modRoot, "*.oramap", SearchOption.AllDirectories))
			{
				var full = Path.GetFullPath(pack);
				if (mapRoots.Any(r => string.Equals(r, full, StringComparison.OrdinalIgnoreCase)))
					continue;
				manual.Add($"dormant: packed map {pack} is outside the enumerated map folders - " +
					"inspect and update it by hand if it overrides a migrated name");
			}

			LogLine(log, $"   Dormant sweep: {swept} unmounted yaml file(s) checked");
			return manual;
		}

		static bool IsWeaponsPath(string fullPath)
		{
			return fullPath.Split(Path.DirectorySeparatorChar)
				.Any(p => string.Equals(p, "weapons", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Classifies a dormant file and runs the matching transform on its nodes.</summary>
		static List<string> ApplyToDormant(
			List<MiniYamlNodeBuilder> nodes, MigrateUnifiedTraits rule, string fileName, bool isWeapons)
		{
			var steps = new List<string>();
			var traitRenames = rule.ActiveRenames("trait");
			var projectileRenames = rule.ActiveRenames("projectile");
			var warheadRenames = rule.ActiveRenames("warhead");

			if (fileName.Equals("map.yaml", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var section in nodes)
				{
					var sectionNodes = section.Value?.Nodes ?? [];
					switch (section.Key)
					{
						case "Rules":
						case "Actors":
							foreach (var n in sectionNodes)
								steps.AddRange(MigrateUnifiedTraits.RenameMatchingChildren(n, traitRenames));
							break;
						case "Weapons":
							foreach (var n in sectionNodes)
								steps.AddRange(MigrateUnifiedTraits.RenameWeaponValues(n, projectileRenames, warheadRenames));
							break;
					}
				}

				return steps;
			}

			foreach (var n in nodes)
			{
				if (n.Key == null)
					continue;
				if (isWeapons)
					steps.AddRange(MigrateUnifiedTraits.RenameWeaponValues(n, projectileRenames, warheadRenames));
				else
					steps.AddRange(MigrateUnifiedTraits.RenameMatchingChildren(n, traitRenames));
			}

			return steps;
		}

		/// <summary>Absolute paths of every file mounted by a manifest list.</summary>
		static HashSet<string> MountedPaths(ModData modData)
		{
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var manifest = modData.Manifest;
			foreach (var list in new IEnumerable<string>[]
			{
				manifest.Rules, manifest.Weapons, manifest.Sequences, manifest.ModelSequences,
				manifest.Chrome, manifest.ChromeLayout, manifest.TileSets, manifest.Cursors,
				manifest.FluentMessages, manifest.Voices, manifest.Notifications,
				manifest.Music, manifest.Playlists, manifest.Hotkeys, manifest.Missions,
				manifest.ServerTraits, manifest.ChromeMetrics, manifest.MapCompatibility,
			})
			{
				foreach (var f in list)
				{
					if (!modData.ModFiles.TryGetPackageContaining(f, out var package, out var name))
						continue;
					if (package is Folder)
						set.Add(Path.GetFullPath(Path.Combine(package.Name, name)));
				}
			}

			return set;
		}

		static void Log(StreamWriter w, string format, params object[] args)
		{
			w.Write(format, args);
			Console.Write(format, args);
		}

		static void LogLine(StreamWriter w, string line = "")
		{
			w.WriteLine(line);
			Console.WriteLine(line);
		}

		static void LogSuccess(StreamWriter w, string text) { LogLine(w, text); }

		static void LogWarning(StreamWriter w, string text) { LogLine(w, text); }

		static void LogError(StreamWriter w, string text) { LogLine(w, text); }
	}
}
