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
using System.Diagnostics;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Measures the cost of on-demand incremental sprite loading vs one batched load, to decide whether a pure
	// fallback (load nothing up front, load each image on first use) is viable. Loads a fixed set of images in
	// one of three patterns and reports resident atlas sheets + MiB (fragmentation) and decode/pack wall time
	// (a CPU hitch proxy — GPU upload is null in headless, so this UNDER-estimates the in-game stall).
	// Each LoadImages call starts a fresh sheet (SpriteCache.BeginNewSession), so per-miss loading packs one
	// sprite per full-size sheet. Run each mode in a SEPARATE process (fresh sprite-cache pool) so the patterns
	// don't share atlases:
	//   --ondemand-load-test single
	//   --ondemand-load-test batch:20
	//   --ondemand-load-test permiss
	sealed class OnDemandLoadTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--ondemand-load-test";

		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;

		[Desc("MODE(single|batch:N|permiss) [count] [tileset]",
			"Measure incremental sprite-load cost for one pattern: resident sheets/MiB + decode time. Run each " +
			"mode in a separate process (fresh cache). GPU upload excluded (headless), so times under-estimate.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var mode = args[1];
			var count = args.Length > 2 && int.TryParse(args[2], out var c) ? c : 600;
			var tileset = args.Length > 3
				? args[3].ToUpperInvariant()
				: modData.DefaultTerrainInfo.Keys.FirstOrDefault(t => t.Equals("temperat", StringComparison.OrdinalIgnoreCase))
					?? modData.DefaultTerrainInfo.Keys.First();

			using var sequences = new SequenceSet(modData.ModFiles, modData, tileset, null);
			var images = sequences.Images.OrderBy(x => x, StringComparer.Ordinal).Take(count).ToArray();
			Console.WriteLine($"Tileset {tileset}; loading {images.Length} images; mode={mode}");

			var callTimes = new List<double>();
			var overall = Stopwatch.StartNew();

			void Load(string[] chunk)
			{
				var t = Stopwatch.StartNew();
				sequences.LoadImages(chunk);
				callTimes.Add(t.Elapsed.TotalMilliseconds);
			}

			if (mode == "single")
				Load(images);
			else if (mode.StartsWith("batch:", StringComparison.Ordinal))
			{
				var n = Math.Max(1, int.Parse(mode["batch:".Length..]));
				for (var i = 0; i < images.Length; i += n)
					Load(images.Skip(i).Take(n).ToArray());
			}
			else if (mode == "permiss")
			{
				foreach (var img in images)
					Load(new[] { img });
			}
			else
			{
				Console.WriteLine("Unknown mode. Use single | batch:N | permiss.");
				Environment.Exit(1);
			}

			overall.Stop();

			var (sheets, bytes) = Measure(sequences);
			callTimes.Sort();
			double Pct(double p) => callTimes.Count == 0 ? 0 : callTimes[Math.Min(callTimes.Count - 1, (int)(p * (callTimes.Count - 1)))];

			Console.WriteLine();
			Console.WriteLine($"LoadImages calls:   {callTimes.Count}");
			Console.WriteLine($"Resident sheets:    {sheets}");
			Console.WriteLine($"Resident MiB:       {bytes / (1024 * 1024)}");
			Console.WriteLine($"Total load time:    {overall.Elapsed.TotalMilliseconds:F0} ms");
			if (callTimes.Count > 1)
				Console.WriteLine($"Per-call ms:        p50 {Pct(0.5):F2}  p95 {Pct(0.95):F2}  max {callTimes[^1]:F2}");
		}

		static (int Sheets, long Bytes) Measure(SequenceSet s)
		{
			var sheets = 0;
			long bytes = 0;
			foreach (var sb in s.SpriteCache.SheetBuilders.Values)
				foreach (var sh in sb.AllSheets)
				{
					sheets++;
					bytes += (long)sh.Size.Width * sh.Size.Height * (sb.Type == SheetType.BGRA ? 4 : 1);
				}

			return (sheets, bytes);
		}
	}
}
