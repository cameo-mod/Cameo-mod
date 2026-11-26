#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// RM-01 SCOUT-FRONTIER-01 (design d33605f3): the region data the frontier pick reads. Every value is the bot's own knowledge
	/// (staleness / interest / danger already published for its own and its team's vision, region centres, adjacency). Nothing here
	/// can name an actor, a player or an enemy condition.
	/// </summary>
	internal interface IScoutFrontierField
	{
		int Count { get; }

		/// <summary>True when the index space is grid cells (the per-sector cap rule applies); false when zone-backed (a region IS a sector).</summary>
		bool GridBacked { get; }

		/// <summary>Staleness of region i in the module's own unit (ticks since seen). 0 or less = fresh.</summary>
		long Stale(int i);

		/// <summary>The base value stale x (1000 + interest); 0 for a fresh region.</summary>
		long Value(int i);

		long Danger(int i);

		IReadOnlyList<int> Neighbors(int i);

		CPos Center(int i);
	}

	/// <summary>RM-01: per-module memory of visits that gave nothing and of unroutable targets. Pure counters keyed by region index.</summary>
	internal sealed class ScoutFrontierMemory
	{
		readonly Dictionary<int, long> noGain = new();
		readonly Dictionary<int, int> retryUntil = new();

		public int Count => noGain.Count + retryUntil.Count;

		/// <summary>A visit that could not see in (staleness did not fall since the assignment) halves the region until it is seen.</summary>
		public bool Halved(int region, long staleNow) => noGain.TryGetValue(region, out var at) && staleNow >= at;

		public bool CoolingDown(int region, int tick) => retryUntil.TryGetValue(region, out var until) && tick < until;

		/// <summary>
		/// The scout is released or idle at its target. staleNow below staleAtAssign = the visit gave vision (forget the region).
		/// Otherwise: idle sooner than retryTicks after the order = unroutable (cool down), later = could not see in (halve).
		/// </summary>
		public void RecordVisit(int region, long staleAtAssign, long staleNow, int elapsedTicks, int tick, int retryTicks)
		{
			if (region < 0)
				return;

			if (staleNow < staleAtAssign)
			{
				noGain.Remove(region);
				retryUntil.Remove(region);
				return;
			}

			if (elapsedTicks < retryTicks)
				retryUntil[region] = tick + retryTicks;
			else
				noGain[region] = staleAtAssign;
		}

		public void Clear()
		{
			noGain.Clear();
			retryUntil.Clear();
		}
	}

	/// <summary>
	/// RM-01 SCOUT-FRONTIER-01: the Fransbot E2 frontier-edge + per-sector-cap recon term, generalised to a fading-certainty field.
	/// Pure and deterministic (no RNG, arrays by index, no LINQ/closures/hash sets); one instance per module, buffers reused so a pick
	/// allocates nothing after warm-up. score(i) = [ base_i + W x G(i) / 1000 ] x H / 1000 (/ divisor when halved) - danger_i - distance_i / 64.
	/// </summary>
	internal sealed class ScoutFrontierGain
	{
		public readonly record struct Tunables(int WeightPermille, int ReachHops, int VisionCells, int HeadingFwd, int HeadingBack,
			int RevisitDivisor, int SectorCapRadius, int Prefilter);

		int[] coverStamp = Array.Empty<int>();
		int coverEpoch;
		int[] walkStamp = Array.Empty<int>();
		int walkEpoch;
		int[] queue = Array.Empty<int>();
		int[] depth = Array.Empty<int>();
		int[] blockStamp = Array.Empty<int>();
		int blockEpoch;
		int[] topIndex = Array.Empty<int>();
		long[] topScore = Array.Empty<long>();

		// DEV row of the last pick.
		public int LastRegion { get; private set; } = -1;
		public long LastGain { get; private set; }
		public int LastHeading { get; private set; } = 1000;
		public bool LastHalved { get; private set; }
		public int LastScanned { get; private set; }

		void Fit(int count, int prefilter)
		{
			if (coverStamp.Length < count)
			{
				coverStamp = new int[count];
				walkStamp = new int[count];
				blockStamp = new int[count];
				queue = new int[count];
				depth = new int[count];
				coverEpoch = walkEpoch = blockEpoch = 0;
			}

			if (topIndex.Length < prefilter)
			{
				topIndex = new int[prefilter];
				topScore = new long[prefilter];
			}
		}

		/// <summary>H: 1250 when the move continues the heading (dot > 0), 750 when it reverses (dot &lt; 0), 1000 otherwise or with no heading.</summary>
		public static int HeadingPermille(bool hasHeading, CVec heading, CVec delta, in Tunables t)
		{
			if (!hasHeading)
				return 1000;

			var dot = (long)heading.X * delta.X + (long)heading.Y * delta.Y;
			return dot > 0 ? t.HeadingFwd : dot < 0 ? t.HeadingBack : 1000;
		}

		/// <summary>The one combining formula, public for the golden vectors.</summary>
		public static long Combine(long baseValue, long gain, int weightPermille, int headingPermille, bool halved, int revisitDivisor, long danger, long distancePenalty)
		{
			var positive = baseValue + weightPermille * gain / 1000;
			positive = positive * headingPermille / 1000;
			if (halved && revisitDivisor > 1)
				positive /= revisitDivisor;

			return positive - danger - distancePenalty;
		}

		/// <summary>The existing PickScoutRegion distance term, kept additive.</summary>
		public static long DistancePenalty(CPos center, CPos from) => (center - from).Length / 64;

		/// <summary>Start a new "covered by other scouts" set. Call once per pick before <see cref="Cover"/>.</summary>
		public void BeginCover(int count, int prefilter)
		{
			Fit(count, prefilter);
			coverEpoch++;
			blockEpoch++;
		}

		/// <summary>Marks the vision reach of an other scout's target as covered (the uncovered rule), and the sector-cap block in a grid index space.</summary>
		public void Cover(IScoutFrontierField f, int region, in Tunables t)
		{
			if (region < 0 || region >= f.Count)
				return;

			Walk(f, region, t.ReachHops, t.VisionCells, coverStamp, coverEpoch, null);
			if (f.GridBacked && t.SectorCapRadius > 0)
				Walk(f, region, t.SectorCapRadius, int.MaxValue, blockStamp, blockEpoch, null);
		}

		/// <summary>Regions inside the reach (hops and centre distance) of <paramref name="region"/>, optionally summing the uncovered base value.</summary>
		long Walk(IScoutFrontierField f, int region, int hops, int visionCells, int[] mark, int markEpoch, int[] sumOnlyUncovered)
		{
			walkEpoch++;
			var head = 0;
			var tail = 0;
			long sum = 0;
			queue[tail] = region;
			depth[tail++] = 0;
			walkStamp[region] = walkEpoch;
			var origin = f.Center(region);
			while (head < tail)
			{
				var cur = queue[head];
				var d = depth[head++];
				if (sumOnlyUncovered != null)
				{
					if (sumOnlyUncovered[cur] != markEpoch)
						sum += f.Value(cur);
				}
				else
					mark[cur] = markEpoch;

				if (d >= hops)
					continue;

				var next = f.Neighbors(cur);
				for (var n = 0; n < next.Count; n++)
				{
					var k = next[n];
					if (k < 0 || k >= f.Count || walkStamp[k] == walkEpoch)
						continue;

					if ((f.Center(k) - origin).Length > visionCells)
						continue;

					walkStamp[k] = walkEpoch;
					queue[tail] = k;
					depth[tail++] = d + 1;
				}
			}

			return sum;
		}

		/// <summary>G(i): summed base value of the regions inside the reach of i that no other scout already covers (i itself included).</summary>
		public long Gain(IScoutFrontierField f, int region, in Tunables t) =>
			Walk(f, region, t.ReachHops, t.VisionCells, null, coverEpoch, coverStamp);

		/// <summary>
		/// Picks the next region: prefilter the top <see cref="Tunables.Prefilter"/> by the base score, then re-score them with the gain,
		/// heading and revisit terms. Returns -1 when no region is stale. Ties go to the lower index at both stages.
		/// <see cref="BeginCover"/> and <see cref="Cover"/> must have run for this pick.
		/// </summary>
		public int Pick(IScoutFrontierField f, CPos from, bool hasHeading, CVec heading, IReadOnlySet<int> taken,
			ScoutFrontierMemory memory, int tick, in Tunables t)
		{
			var count = f.Count;
			var k = Math.Max(1, t.Prefilter);
			Fit(count, k);
			var kept = 0;
			var scanned = 0;
			for (var i = 0; i < count; i++)
			{
				if (taken.Contains(i) || f.Stale(i) <= 0)
					continue;

				if (blockStamp[i] == blockEpoch || (memory != null && memory.CoolingDown(i, tick)))
					continue;

				scanned++;
				var score = f.Value(i) - f.Danger(i) - DistancePenalty(f.Center(i), from);
				if (kept == k && score <= topScore[k - 1])
					continue;

				// Insert, descending by score; equal scores keep the earlier (lower) index first because i only grows.
				var pos = kept < k ? kept : k - 1;
				while (pos > 0 && topScore[pos - 1] < score)
				{
					topScore[pos] = topScore[pos - 1];
					topIndex[pos] = topIndex[pos - 1];
					pos--;
				}

				topScore[pos] = score;
				topIndex[pos] = i;
				if (kept < k)
					kept++;
			}

			LastScanned = scanned;
			LastRegion = -1;
			LastGain = 0;
			LastHeading = 1000;
			LastHalved = false;
			var best = -1;
			var bestScore = long.MinValue;
			for (var c = 0; c < kept; c++)
			{
				var i = topIndex[c];
				var gain = Gain(f, i, t);
				var h = HeadingPermille(hasHeading, heading, f.Center(i) - from, t);
				var halved = memory != null && memory.Halved(i, f.Stale(i));
				var score = Combine(f.Value(i), gain, t.WeightPermille, h, halved, t.RevisitDivisor, f.Danger(i), DistancePenalty(f.Center(i), from));
				if (score > bestScore || (score == bestScore && best >= 0 && i < best))
				{
					best = i;
					bestScore = score;
					LastRegion = i;
					LastGain = gain;
					LastHeading = h;
					LastHalved = halved;
				}
			}

			return best;
		}
	}
}
