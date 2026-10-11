#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Hotspot #4 extraction (2026-10-04): the pure decisions inside
	/// TacticalMapBotModule.BuildRegions' merge loop — whether a round runs at all,
	/// which barrier pieces are eligible to drop, what "undersized" is measured
	/// against, and which eligible pieces may share a round without spoiling each
	/// other's measurement. Bit-identical; all map state stays in the caller.
	/// </summary>
	public static class RegionMergeEval
	{
		/// <summary>
		/// Whether another fill-and-measure round runs. A zero MinRegionSize disables
		/// merging outright; otherwise every non-terminating round drops at least one
		/// piece and dropping is monotone, so the piece count bounds the sequence
		/// (maxMergeRounds = pieces.Count + 1 in the caller is that bound plus one).
		/// </summary>
		public static bool MergeContinues(int minRegionSize, int round, int pieceCount)
		{
			return minRegionSize > 0 && round < pieceCount;
		}

		/// <summary>
		/// One barrier piece may give way: still active, droppable by kind (corridor
		/// or stray gate cell — ramps never drop), separating at least two regions
		/// (a piece touching one region holds nothing apart), and the smallest region
		/// it holds apart is undersized.
		/// </summary>
		public static bool IsMergeEligible(bool active, bool droppable, int touchingCount,
			int smallestTouchedSize, int minRegionSize)
		{
			return active && droppable && touchingCount >= 2 && smallestTouchedSize < minRegionSize;
		}

		/// <summary>
		/// What "undersized" is measured against: the smallest region this piece
		/// touches. Empty input is int.MaxValue — never smaller than any configured
		/// minimum, so a piece touching no region is never eligible.
		/// </summary>
		public static int SmallestTouchingSize(IEnumerable<int> touchedRegionSizes)
		{
			var min = int.MaxValue;
			foreach (var size in touchedRegionSizes)
				min = System.Math.Min(min, size);

			return min;
		}

		/// <summary>
		/// Which eligible pieces may drop in the same round. Worst-first order, then
		/// greedy disjoint-claim: a piece drops iff none of the regions it touches
		/// was already claimed by an earlier drop this round. Two pieces sharing a
		/// neighbouring region would spoil each other's undersized measurement, so
		/// the later one waits and is re-measured next round; pieces whose touched
		/// sets are disjoint drop together — the difference between a handful of
		/// full-map fills and one fill per piece.
		/// </summary>
		public static List<int> SelectRoundDrops(
			List<(int Index, int Separated, HashSet<int> Touching)> eligible)
		{
			eligible.Sort((a, b) => a.Separated.CompareTo(b.Separated));

			var claimed = new HashSet<int>();
			var drops = new List<int>(eligible.Count);
			foreach (var (index, _, touching) in eligible)
			{
				if (touching.Overlaps(claimed))
					continue;

				claimed.UnionWith(touching);
				drops.Add(index);
			}

			return drops;
		}
	}
}
