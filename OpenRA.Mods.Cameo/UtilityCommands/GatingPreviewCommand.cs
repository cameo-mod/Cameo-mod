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
using OpenRA.Mods.Cameo.Graphics;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Offline preview of theme-file gating for a given faction set: reports which sequence bundles (themes)
	// would be loaded vs skipped, and any image whose owning bundle would NOT be loaded (a leak that must be
	// fixed before gating). Validates the FactionArtClosure resolver headlessly before it drives the in-game
	// sprite load. Cross-reference the skipped bundles with --art-memory-report for the MiB saved.
	sealed class GatingPreviewCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--gating-preview";

		static readonly string[] DefaultFloor = { "misc", "shared_effects", "decorations", "funpark", "civilian", "campaign" };

		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;

		[Desc("FACTION[,FACTION...] [--floor a,b,c]",
			"Preview theme-file gating for the given factions: bundles loaded vs skipped and any un-covered " +
			"image (leak). Cross-reference skipped bundles with --art-memory-report for MiB saved.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var rules = modData.DefaultRules;

			var requested = args[1].Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

			var floor = DefaultFloor.ToHashSet(StringComparer.OrdinalIgnoreCase);
			var excludeShared = args.Contains("--exclude-shared");
			var startingUnits = args.Contains("--startingunits");
			for (var i = 2; i < args.Length - 1; i++)
				if (args[i] == "--floor")
					floor = args[i + 1].Split(',').Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

			var allFactions = FactionArtClosure.Factions(rules).ToHashSet(StringComparer.OrdinalIgnoreCase);
			var unknown = requested.Where(f => !allFactions.Contains(f)).ToArray();
			if (unknown.Length > 0)
			{
				Console.WriteLine($"Unknown faction(s): {string.Join(", ", unknown)}");
				Console.WriteLine($"Known factions: {string.Join(", ", allFactions.OrderBy(f => f))}");
				Environment.Exit(1);
			}

			// All gateable bundles (manifest sequence files minus voxels), for the skipped list.
			var allBundles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in modData.Manifest.Sequences)
			{
				var bundle = Path.GetFileNameWithoutExtension(path.Contains('|') ? path[(path.IndexOf('|') + 1)..] : path);
				if (!bundle.Contains("voxel", StringComparison.OrdinalIgnoreCase))
					allBundles.Add(bundle);
			}

			var result = startingUnits
				? FactionArtClosure.ResolveByStartingUnits(modData, rules, requested, floor)
				: FactionArtClosure.Resolve(modData, rules, requested, floor, excludeUniversal: excludeShared);

			var basis = startingUnits ? "starting-units" : (excludeShared ? "closure/exclude-shared" : "closure");
			Console.WriteLine($"Factions: {string.Join(", ", requested)}  (basis: {basis})");
			foreach (var faction in requested)
				if (result.RosterByFaction.TryGetValue(faction, out var roster))
					Console.WriteLine($"  {faction}: {roster.Count} ownable actors");
			Console.WriteLine($"Images referenced: {result.Images.Count}");
			Console.WriteLine();

			var loaded = result.Bundles.OrderBy(b => b, StringComparer.OrdinalIgnoreCase).ToArray();
			var skipped = allBundles.Where(b => !floor.Contains(b) && !result.Bundles.Contains(b))
				.OrderBy(b => b, StringComparer.OrdinalIgnoreCase).ToArray();

			Console.WriteLine($"FLOOR (always loaded): {string.Join(", ", floor.OrderBy(b => b))}");
			Console.WriteLine($"LOAD ({loaded.Length} bundles): {string.Join(", ", loaded)}");
			Console.WriteLine($"SKIP ({skipped.Length} bundles): {string.Join(", ", skipped)}");
			Console.WriteLine();

			if (result.Leaks.Count == 0)
				Console.WriteLine("LEAKS: none - every referenced image is covered by the floor or a loaded bundle. PASS");
			else
			{
				Console.WriteLine($"LEAKS ({result.Leaks.Count}) - referenced images not covered by floor or a loaded bundle:");
				foreach (var leak in result.Leaks)
					Console.WriteLine($"    {leak}");
				Environment.Exit(1);
			}
		}
	}
}
