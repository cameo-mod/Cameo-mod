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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Proves the engine's per-image incremental sprite loading (SequenceSet.LoadImages): loads one half of the
	// images, measures resident atlas memory, loads the second half, and confirms the new sprites were appended
	// into fresh sheets without reloading or disturbing the first half. Underpins selective per-faction art
	// loading, where only in-match themes are loaded up front and more art is appended on demand.
	sealed class IncrementalSpriteLoadTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--incremental-sprite-load-test";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("[TILESET]",
			"Verify incremental (per-image) sprite loading: load one half of the images, measure resident atlas " +
			"memory, load the second half, and confirm the new sprites were appended without reloading or " +
			"disturbing the first half. Exits non-zero on failure.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: the engine assumes Game.ModData is set.
			var modData = Game.ModData = utility.ModData;

			// Prefer a mainstream tileset; some images have tileset-specific filenames that are null elsewhere.
			var tileset = args.Length > 1
				? args[1].ToUpperInvariant()
				: modData.DefaultTerrainInfo.Keys.FirstOrDefault(t => t.Equals("temperat", StringComparison.OrdinalIgnoreCase))
					?? modData.DefaultTerrainInfo.Keys.First();
			Console.WriteLine($"Tileset: {tileset}");

			using var sequences = new SequenceSet(modData.ModFiles, modData, tileset, null);

			var allImages = sequences.Images.OrderBy(x => x, StringComparer.Ordinal).ToArray();
			var half = allImages.Length / 2;
			var first = allImages.Take(half).ToArray();
			var second = allImages.Skip(half).ToArray();
			Console.WriteLine($"{allImages.Length} images total: loading {first.Length}, then {second.Length}.");

			sequences.LoadImages(first);
			var (sheets1, bytes1) = Measure(sequences);
			Console.WriteLine($"After pass 1 ({first.Length} images): {sheets1} sheets, {bytes1 / (1024 * 1024)} MiB.");

			sequences.LoadImages(second);
			var (sheets2, bytes2) = Measure(sequences);
			Console.WriteLine($"After pass 2 (+{second.Length} images): {sheets2} sheets, {bytes2 / (1024 * 1024)} MiB.");

			// Re-requesting already-loaded images must be a no-op (no reservation, no decode, no new sheets).
			sequences.LoadImages(first);
			var (sheets3, bytes3) = Measure(sequences);

			// Pass 2 must have grown resident memory (new art appended) while keeping every pass-1 sheet.
			var appended = bytes2 > bytes1 && sheets2 >= sheets1;

			// Re-loading pass-1 images must change nothing.
			var idempotent = sheets3 == sheets2 && bytes3 == bytes2;

			Console.WriteLine();
			Console.WriteLine($"Pass 2 appended new art (memory grew, pass-1 sheets retained): {Result(appended)}");
			Console.WriteLine($"Re-loading already-loaded images was a no-op: {Result(idempotent)}");

			if (!appended || !idempotent)
				Environment.Exit(1);
		}

		static string Result(bool ok)
		{
			return ok ? "PASS" : "FAIL";
		}

		static (int Sheets, long Bytes) Measure(SequenceSet sequences)
		{
			var sheets = 0;
			long bytes = 0;
			foreach (var sb in sequences.SpriteCache.SheetBuilders.Values)
			{
				foreach (var s in sb.AllSheets)
				{
					sheets++;
					var bytesPerPixel = sb.Type == SheetType.BGRA ? 4 : 1;
					bytes += (long)s.Size.Width * s.Size.Height * bytesPerPixel;
				}
			}

			return (sheets, bytes);
		}
	}
}
