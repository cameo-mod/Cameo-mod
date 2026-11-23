#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>One observed or remembered enemy that can hurt a drop: where, how far it reaches (cells, margin included), and what kind.</summary>
	internal readonly record struct AreaThreat(CPos Cell, int Value, int RangeCells, bool Defence, bool Army, bool AntiAir, bool Detector, int DetectorRangeCells);

	/// <summary>
	/// ENGDROP area knowledge from the bot's OWN observations (ENG-FOG): a bounded table of visibility stamps (at most 16 targets, at most 256
	/// deterministic sample cells each) updated only for cells the bot actually sees now. A sample with no stamp, or a stamp older than the
	/// freshness window, makes the area unknown. Pure: no world state, so tests can vary hidden worlds with equal observation.
	/// </summary>
	internal sealed class VisibilityStamps
	{
		public const int MaxTargets = 16;
		public const int MaxSamples = 256;

		// One bounded dictionary of cell -> last tick the bot saw that cell, per tracked target (at most MaxSamples cells, the freshest kept).
		sealed class Table
		{
			public readonly Dictionary<CPos, int> Stamp = [];
		}

		readonly Dictionary<CPos, Table> tables = [];
		readonly List<CPos> insertionOrder = [];

		/// <summary>The deterministic sample set: the target disc (row-major, strided so it never exceeds MaxSamples minus the extras),
		/// then the approach cells, then the landing cells, each distinct. Truncated when the extras alone cannot fit.</summary>
		public static (CPos[] Samples, bool Truncated) SampleCells(CPos center, int radiusCells, IReadOnlyList<CPos> approach, IReadOnlyList<CPos> landing,
			int max = MaxSamples)
		{
			var extras = new List<CPos>();
			var seen = new HashSet<CPos> { center };
			foreach (var c in (approach ?? []).Concat(landing ?? []))
				if (seen.Add(c))
					extras.Add(c);

			var truncated = extras.Count + 1 > max;
			if (truncated)
				extras.RemoveRange(Math.Max(0, max - 1), extras.Count - Math.Max(0, max - 1));

			var disc = new List<CPos>();
			var r = Math.Max(0, radiusCells);
			for (var dy = -r; dy <= r; dy++)
				for (var dx = -r; dx <= r; dx++)
					if (dx * dx + dy * dy <= r * r && (dx != 0 || dy != 0))
					{
						var cell = new CPos(center.X + dx, center.Y + dy);
						if (seen.Add(cell))
							disc.Add(cell);
					}

			var room = Math.Max(0, max - 1 - extras.Count);
			var stride = room == 0 ? int.MaxValue : Math.Max(1, (disc.Count + room - 1) / room);
			var samples = new List<CPos> { center };
			for (var i = 0; i < disc.Count && samples.Count < 1 + room; i += stride)
				samples.Add(disc[i]);

			samples.AddRange(extras);
			return (samples.ToArray(), truncated);
		}

		/// <summary>The cells of a planned route polyline (waypoints of an air corridor or the cells of a ground path), made contiguous one cell at
		/// a time and sampled evenly to at most max cells, always keeping the first and the last. Deterministic.</summary>
		public static IReadOnlyList<CPos> SampleRoute(IReadOnlyList<CPos> polyline, int max)
		{
			if (polyline == null || polyline.Count == 0 || max <= 0)
				return [];

			var dense = new List<CPos> { polyline[0] };
			for (var i = 1; i < polyline.Count; i++)
			{
				var from = polyline[i - 1];
				var delta = polyline[i] - from;
				var steps = Math.Min(2048, Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)));
				for (var s = 1; s <= steps; s++)
					dense.Add(new CPos(from.X + delta.X * s / steps, from.Y + delta.Y * s / steps));
			}

			dense = dense.Distinct().ToList();
			if (dense.Count <= max)
				return dense;

			var sampled = new List<CPos>(max);
			for (var i = 0; i < max; i++)
				sampled.Add(dense[(int)((long)i * (dense.Count - 1) / Math.Max(1, max - 1))]);

			return sampled.Distinct().ToList();
		}

		/// <summary>The landing ring: up to count cells at Chebyshev distance radius + 2 from the center, evenly spaced along the perimeter.</summary>
		public static IReadOnlyList<CPos> Ring(CPos center, int radiusCells, int count)
		{
			var d = Math.Max(1, radiusCells) + 2;
			var perimeter = new List<CPos>();
			for (var x = -d; x <= d; x++)
			{
				perimeter.Add(new CPos(center.X + x, center.Y - d));
				perimeter.Add(new CPos(center.X + x, center.Y + d));
			}

			for (var y = -d + 1; y <= d - 1; y++)
			{
				perimeter.Add(new CPos(center.X - d, center.Y + y));
				perimeter.Add(new CPos(center.X + d, center.Y + y));
			}

			var stride = Math.Max(1, perimeter.Count / Math.Max(1, count));
			return perimeter.Where((_, i) => i % stride == 0).Take(count).ToList();
		}

		/// <summary>Stamp every sample cell the bot sees right now. Stamps of cells seen earlier are kept (bounded, the freshest win), so a
		/// route that changes between scans still benefits from what was seen on the cells it shares with an earlier one.</summary>
		public void Observe(CPos center, int radiusCells, IReadOnlyList<CPos> approach, IReadOnlyList<CPos> landing, Func<CPos, bool> visibleNow, int now)
		{
			var (samples, _) = SampleCells(center, radiusCells, approach, landing);
			if (!tables.TryGetValue(center, out var table))
			{
				tables[center] = table = new Table();
				insertionOrder.Add(center);
				while (insertionOrder.Count > MaxTargets)
				{
					tables.Remove(insertionOrder[0]);
					insertionOrder.RemoveAt(0);
				}
			}

			foreach (var cell in samples)
				if (visibleNow(cell))
					table.Stamp[cell] = Math.Max(1, now);

			if (table.Stamp.Count > MaxSamples)
				foreach (var drop in table.Stamp.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key.X).ThenBy(kv => kv.Key.Y)
					.Take(table.Stamp.Count - MaxSamples).Select(kv => kv.Key).ToList())
					table.Stamp.Remove(drop);
		}

		/// <summary>Known only when the sample set fits the table (not truncated) and EVERY sample - target area, planned route and landing - has a
		/// stamp no older than freshTicks. A missing, stale or clipped sample is unknown, never safe.</summary>
		public bool IsKnown(CPos center, int radiusCells, IReadOnlyList<CPos> approach, IReadOnlyList<CPos> landing, int now, int freshTicks, out int oldestObserved)
		{
			oldestObserved = 0;
			var (samples, truncated) = SampleCells(center, radiusCells, approach, landing);
			if (truncated || !tables.TryGetValue(center, out var table))
				return false;

			oldestObserved = int.MaxValue;
			foreach (var cell in samples)
			{
				if (!table.Stamp.TryGetValue(cell, out var stamp) || now - stamp > freshTicks)
				{
					oldestObserved = table.Stamp.TryGetValue(cell, out stamp) ? stamp : 0;
					return false;
				}

				oldestObserved = Math.Min(oldestObserved, stamp);
			}

			return true;
		}

		/// <summary>Threat inside the area: a threat counts when the target disc lies within its reach, or any approach/landing sample does.</summary>
		public static AreaIntel Summarize(bool known, int oldestObserved, CPos center, int radiusCells, IReadOnlyList<CPos> samples, IReadOnlyList<AreaThreat> threats)
		{
			int defence = 0, army = 0, antiAir = 0, maxRange = 0, detectors = 0;
			foreach (var t in threats)
			{
				var reach = Math.Max(0, t.RangeCells);
				var toCenter = (t.Cell - center).LengthSquared;
				var inArea = toCenter <= (long)(radiusCells + reach) * (radiusCells + reach);
				if (!inArea)
				{
					var near = Math.Max(1, reach);
					inArea = samples.Any(s => (s - t.Cell).LengthSquared <= (long)near * near);
				}

				if (t.Detector && toCenter <= (long)(radiusCells + t.DetectorRangeCells) * (radiusCells + t.DetectorRangeCells))
					detectors++;

				if (!inArea)
					continue;

				if (t.Defence)
				{
					defence += t.Value;
					maxRange = Math.Max(maxRange, reach);
				}

				if (t.Army)
					army += t.Value;

				if (t.AntiAir)
					antiAir += t.Value;
			}

			return new AreaIntel(known, defence, army, antiAir, maxRange, oldestObserved, detectors);
		}
	}

	/// <summary>The engineer owner's IBotAreaIntelProvider: visibility stamps from the bot's own shroud, threats from its fogged memory.</summary>
	internal sealed class EngineerAreaIntel : IBotAreaIntelProvider
	{
		readonly OpenRA.Player player;
		readonly Func<IReadOnlyList<AreaThreat>> threats;
		readonly VisibilityStamps stamps = new();

		public EngineerAreaIntel(OpenRA.Player player, Func<IReadOnlyList<AreaThreat>> threats)
		{
			this.player = player;
			this.threats = threats;
		}

		/// <summary>Stamp the cells of this target's area the bot sees now (called by the scan, at most 16 targets).</summary>
		public void Observe(CPos center, int radiusCells, IReadOnlyList<CPos> approach, int now)
		{
			var shroud = player.Shroud;
			var map = player.World.Map;
			stamps.Observe(center, radiusCells, approach, VisibilityStamps.Ring(center, radiusCells, 24),
				c => shroud != null && map.Contains(c) && shroud.IsVisible(c), now);
		}

		public AreaIntel Assess(CPos center, int radiusCells, IReadOnlyList<CPos> approach, IReadOnlyList<CPos> landing, int nowTick, int freshTicks)
		{
			landing ??= VisibilityStamps.Ring(center, radiusCells, 24);
			var known = stamps.IsKnown(center, radiusCells, approach, landing, nowTick, freshTicks, out var oldest);
			var (samples, _) = VisibilityStamps.SampleCells(center, radiusCells, approach, landing);
			return VisibilityStamps.Summarize(known, oldest, center, radiusCells, samples, threats());
		}
	}
}
