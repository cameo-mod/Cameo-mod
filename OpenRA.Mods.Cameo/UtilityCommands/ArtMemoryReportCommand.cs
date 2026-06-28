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
using OpenRA.Graphics;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Measures how much resident sprite-sheet memory each per-game art "theme" (sequence file) costs.
	// Loads a shared "floor" set on its own, then the floor plus each gateable theme, isolating each
	// load (no shared sprite-cache pool) so the per-theme figures are independent. Also reports the
	// cross-theme image borrows (referenced sprites a theme does not itself define) via the missing-file
	// list. Used to estimate the memory that selective per-faction loading could save.
	sealed class ArtMemoryReportCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--art-memory-report";

		// Sequence themes that are always loaded (shared effects, civilian, decorations, etc.).
		// Overridable with --floor a,b,c. Everything else in Manifest.Sequences is "gateable".
		static readonly string[] DefaultFloor = { "misc", "shared_effects", "decorations", "funpark", "civilian", "campaign" };

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("[--floor a,b,c]",
			"Report resident sprite-sheet memory (MiB) for the shared 'floor' themes and for the floor plus " +
			"each gateable theme, plus the all-themes baseline. Also lists cross-theme image borrows per theme. " +
			"Use to estimate savings from loading only in-match factions' art.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: the engine assumes Game.ModData is set.
			var modData = Game.ModData = utility.ModData;
			var fs = modData.DefaultFileSystem;

			// Prefer a mainstream tileset; some images have tileset-specific filenames that are null elsewhere.
			var tileset = modData.DefaultTerrainInfo.Keys.FirstOrDefault(t => t.Equals("temperat", StringComparison.OrdinalIgnoreCase))
				?? modData.DefaultTerrainInfo.Keys.First();
			Console.WriteLine($"Tileset: {tileset}");

			var floorNames = DefaultFloor.ToHashSet(StringComparer.OrdinalIgnoreCase);
			for (var i = 1; i < args.Length - 1; i++)
				if (args[i] == "--floor")
					floorNames = args[i + 1].Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Map theme short-name -> manifest sequence path (e.g. "tiberiandawn" -> "cameo|sequences/tiberiandawn.yaml").
			// Skip voxels (model definitions, not sprite sequences).
			var themePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in modData.Manifest.Sequences)
			{
				var name = Path.GetFileNameWithoutExtension(path.Contains('|') ? path[(path.IndexOf('|') + 1)..] : path);
				if (name.Contains("voxel", StringComparison.OrdinalIgnoreCase))
					continue;
				themePaths[name] = path;
			}

			var floorThemes = themePaths.Keys.Where(floorNames.Contains).OrderBy(n => n).ToArray();
			var gateableThemes = themePaths.Keys.Where(n => !floorNames.Contains(n)).OrderBy(n => n).ToArray();
			var floorPaths = floorThemes.Select(n => themePaths[n]).ToArray();

			Console.WriteLine($"Floor themes ({floorThemes.Length}): {string.Join(", ", floorThemes)}");
			Console.WriteLine($"Gateable themes ({gateableThemes.Length}): {string.Join(", ", gateableThemes)}");
			Console.WriteLine();

			// Map each top-level image key -> the file that defines it. Read each file on its own (the cross-file
			// merge below drops the per-file source name on merged top-level nodes, so we can't use node.Location).
			var keyToFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in themePaths.Values)
			{
				using var stream = fs.Open(path);
				foreach (var node in MiniYaml.FromStream(stream, path))
				{
					if (node.Key.StartsWith(ActorInfo.AbstractActorPrefix))
						continue;
					keyToFile[node.Key] = path; // last definition wins
				}
			}

			// Parse ALL active sequence files once so cross-file inheritance (^bases scattered across themes)
			// resolves; then reserve/measure only the images belonging to the wanted files (matched by key).
			var allNodes = MiniYaml.Load(fs, themePaths.Values.ToArray(), null);
			var allFileSet = themePaths.Values.ToHashSet();
			var floorFileSet = floorPaths.ToHashSet();

			var baseline = Measure(modData, fs, tileset, allNodes, keyToFile, allFileSet);
			var floor = Measure(modData, fs, tileset, allNodes, keyToFile, floorFileSet);

			// Measure floor + each gateable theme ONCE; reuse for every table below.
			var results = new Dictionary<string, MeasureResult>();
			var errors = new Dictionary<string, string>();
			foreach (var theme in gateableThemes)
			{
				try
				{
					var wanted = new HashSet<string>(floorFileSet) { themePaths[theme] };
					results[theme] = Measure(modData, fs, tileset, allNodes, keyToFile, wanted);
				}
				catch (Exception ex) { errors[theme] = ex.Message.Split('\n')[0]; }
			}

			Console.WriteLine($"{"THEME",-16}{"SHEETS",8}{"MiB",10}{"+MiB vs floor",16}{"borrows",10}");
			Console.WriteLine(new string('-', 60));
			Console.WriteLine($"{"(floor)",-16}{floor.Sheets,8}{floor.MiB,10:F0}{"-",16}{floor.Missing.Count,10}");

			foreach (var theme in gateableThemes)
			{
				if (errors.TryGetValue(theme, out var err))
				{
					Console.WriteLine($"{theme,-16}{"ERR",8}  could not isolate: {err}");
					continue;
				}

				var r = results[theme];
				Console.WriteLine($"{theme,-16}{r.Sheets,8}{r.MiB,10:F0}{r.MiB - floor.MiB,16:F0}{r.Missing.Count - floor.Missing.Count,10}");
			}

			Console.WriteLine(new string('-', 60));
			Console.WriteLine($"{"BASELINE (all)",-16}{baseline.Sheets,8}{baseline.MiB,10:F0}");
			Console.WriteLine();
			Console.WriteLine($"Images skipped (null filename for tileset): floor={floor.Skipped}, baseline={baseline.Skipped}");
			Console.WriteLine($"Floor (always loaded):        {floor.MiB,8:F0} MiB");
			Console.WriteLine($"Sum of per-theme marginals:   {results.Values.Sum(r => r.MiB - floor.MiB),8:F0} MiB (overlap makes this differ from baseline-floor)");
			Console.WriteLine($"Baseline - floor:             {baseline.MiB - floor.MiB,8:F0} MiB (max removable across all gateable themes)");
			Console.WriteLine();
			Console.WriteLine("Per-theme marginal cost (sorted, = approx saving if this bundle is NOT in the match):");
			foreach (var kv in results.OrderByDescending(kv => kv.Value.MiB - floor.MiB))
				Console.WriteLine($"  {kv.Key,-16}{kv.Value.MiB - floor.MiB,8:F0} MiB");

			// Cross-theme borrows: images a theme references but does not define (potential gating crashes).
			Console.WriteLine();
			Console.WriteLine("Cross-theme image borrows per gateable theme (theme -> referenced sprites not in floor+theme):");
			var floorMissing = floor.Missing.Select(m => m.Filename).ToHashSet(StringComparer.OrdinalIgnoreCase);
			foreach (var theme in gateableThemes)
			{
				if (!results.TryGetValue(theme, out var r))
					continue;
				var borrows = r.Missing.Select(m => m.Filename).Where(f => !floorMissing.Contains(f))
					.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f).ToArray();
				if (borrows.Length > 0)
					Console.WriteLine($"  {theme}: {string.Join(", ", borrows)}");
			}
		}

		struct MeasureResult
		{
			public int Sheets;
			public double MiB;
			public int Skipped;
			public List<(string Filename, MiniYamlNode.SourceLocation Location)> Missing;
		}

		static MeasureResult Measure(ModData modData, OpenRA.FileSystem.IReadOnlyFileSystem fs, string tileset,
			List<MiniYamlNode> allNodes, Dictionary<string, string> keyToFile, HashSet<string> wantedFiles)
		{
			var rc = modData.Manifest.RendererConstants;

			// null pool => isolated SheetBuilders, so this load's sheet bytes are independent of other measurements.
			using var cache = new SpriteCache(fs, modData.SpriteLoaders, rc.SequenceBgraSheetSize, rc.SequenceIndexedSheetSize, null);

			var skipped = 0;
			foreach (var node in allNodes)
			{
				if (node.Key.StartsWith(ActorInfo.AbstractActorPrefix))
					continue;

				// allNodes is parsed from every theme (inheritance resolved); reserve only the wanted files' images.
				if (!keyToFile.TryGetValue(node.Key, out var sourceFile) || !wantedFiles.Contains(sourceFile))
					continue;

				// An image whose filename is null for this tileset throws; skip it rather than abort the whole pass.
				try { modData.SpriteSequenceLoader.ParseSequences(modData, tileset, cache, node); }
				catch { skipped++; }
			}

			cache.LoadReservations(modData);

			long totalBytes = 0;
			var sheets = 0;
			foreach (var sb in cache.SheetBuilders.Values)
			{
				foreach (var s in sb.AllSheets)
				{
					sheets++;
					var bytesPerPixel = sb.Type == SheetType.BGRA ? 4 : 1;
					totalBytes += (long)s.Size.Width * s.Size.Height * bytesPerPixel;
				}
			}

			return new MeasureResult
			{
				Sheets = sheets,
				MiB = totalBytes / (1024.0 * 1024.0),
				Skipped = skipped,
				Missing = cache.MissingFiles.ToList(),
			};
		}
	}
}
