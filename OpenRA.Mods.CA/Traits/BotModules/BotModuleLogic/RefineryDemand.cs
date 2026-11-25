#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// REF-DEMAND-01 (Opus ruling and user final spec 2026-11-25, row ref_demand). Pure rules, no state, integer math only (squared cell distances):
	/// - ZONES: the resource cells (and spreader spots) of every field are cut into zones of radius RefineryZoneRadius by a greedy cover in row-major
	///   order (seed = the first uncovered point, the zone = every uncovered point within the radius of the seed), so a big field gives several zones.
	///   Each zone is one anchor of the refinery law; its anchor cell is the zone point nearest the zone's integer centroid.
	/// - DEMAND: a zone asks for refineries while it lies within YardRadiusCells of an own construction yard, a harvester can reach it from that yard,
	///   no allied refinery serves it, and fewer than RefineryPerZoneCap friendly (own or allied) refineries stand within RefinerySpacingCells of it.
	/// - SPACING: no new refinery within RefinerySpacingCells of a friendly refinery; a zone with unmet demand whose search finds no such site retries
	///   with RefinerySpacingFallbackCells.
	/// </summary>
	public static class RefineryDemand
	{
		/// <summary>A zone's state for the law: in demand, served (at its cap, or by an ally), or beyond reach (radius or route).</summary>
		public enum Verdict { Demand, OutOfRadius, Unreachable, ZoneCap, AlliedServed }

		/// <summary>Spacing tiers of a site: allowed at the full spacing, allowed only at the fallback spacing, refused.</summary>
		public const int SpacingAllowed = 0;
		public const int SpacingFallbackOnly = 1;
		public const int SpacingRefused = -1;

		/// <summary>
		/// The verdict of one zone. A friendly refinery that serves or fills the zone wins over the reach tests, so a covered zone reads as served
		/// (the law neither claims it nor counts it as backlog).
		/// </summary>
		public static Verdict Classify(bool inRadius, bool reachable, bool alliedServed, int friendlyRefineries, int cap)
		{
			if (alliedServed)
				return Verdict.AlliedServed;

			if (friendlyRefineries >= ZoneCap(cap))
				return Verdict.ZoneCap;

			if (!inRadius)
				return Verdict.OutOfRadius;

			return reachable ? Verdict.Demand : Verdict.Unreachable;
		}

		/// <summary>The refineries a zone still asks for: the cap minus the friendly refineries counted at it, 0 when it is out of radius,
		/// unreachable or served by an ally.</summary>
		public static int Demand(bool inRadius, bool reachable, bool alliedServed, int friendlyRefineries, int cap) =>
			Classify(inRadius, reachable, alliedServed, friendlyRefineries, cap) == Verdict.Demand ? Math.Max(0, ZoneCap(cap) - friendlyRefineries) : 0;

		/// <summary>Whether a zone's relaxed (fallback spacing) sites may be used yet: not needed, deferred until the strict search is exhausted, or promoted.</summary>
		public enum RelaxedStep { NotNeeded, Deferred, Promote }

		/// <summary>
		/// The relaxed-site gate. A relaxed site is used only after the strict (full spacing) search has fully finished with no site; while the strict
		/// search is still running the relaxed sites are deferred (nothing is placed, the search goes on next tick).
		/// </summary>
		public static RelaxedStep RelaxedGate(bool strictFinished, int strictSites, int relaxedSites)
		{
			if (relaxedSites <= 0 || strictSites > 0)
				return RelaxedStep.NotNeeded;

			return strictFinished ? RelaxedStep.Promote : RelaxedStep.Deferred;
		}

		/// <summary>
		/// A zone is reachable when ANY of its cells is in the harvester locomotor's domain of the yard (<paramref name="reaches"/> is the cheap domain-index
		/// query, no path search). Without a harvester profile no zone is accepted.
		/// </summary>
		public static bool AnyCellReachable(IEnumerable<CPos> cells, bool hasHarvesterProfile, Func<CPos, bool> reaches)
		{
			if (!hasHarvesterProfile || cells == null || reaches == null)
				return false;

			foreach (var c in cells)
				if (reaches(c))
					return true;

			return false;
		}

		/// <summary>A zone counts as demand for the law (<c>ClaimOrder</c>'s unserved-in-reach count): verdict Demand and its coverage is known.</summary>
		public static bool Eligible(Verdict v, bool coverageUnknown) => v == Verdict.Demand && !coverageUnknown;

		/// <summary>The eligible zone count of <paramref name="zones"/> zones: the one counter gameplay (<c>ClaimOrder</c>) and the DEV row share.</summary>
		public static int EligibleZones(int zones, Func<int, Verdict> verdict, Func<int, bool> coverageUnknown)
		{
			var n = 0;
			for (var i = 0; i < zones; i++)
				if (Eligible(verdict(i), coverageUnknown != null && coverageUnknown(i)))
					n++;

			return n;
		}

		/// <summary>The refineries a demand zone may still take before its cap (0 for any other verdict). Reported next to the demand, never as it.</summary>
		public static int CapRemaining(Verdict v, int cap, int friendlyRefineries) =>
			v == Verdict.Demand ? Math.Max(0, ZoneCap(cap) - friendlyRefineries) : 0;

		/// <summary>The per-zone refinery cap; at least 1 (a zone always takes its first refinery).</summary>
		public static int ZoneCap(int cap) => Math.Max(1, cap);

		/// <summary>The cap of one zone: 1 when the zone has fewer than <paramref name="secondMinCells"/> resource cells (0 = never), else <see cref="ZoneCap"/>.</summary>
		public static int ZoneCapFor(int cap, int zoneCells, int secondMinCells) => secondMinCells > 0 && zoneCells < secondMinCells ? 1 : ZoneCap(cap);

		/// <summary>The verdict counts as served for the law's claim order (no claim, no backlog).</summary>
		public static bool Covers(Verdict v) => v == Verdict.ZoneCap || v == Verdict.AlliedServed;

		/// <summary>The verdict puts the zone in the law's in-reach set.</summary>
		public static bool InReach(Verdict v) => v == Verdict.Demand;

		/// <summary>The DEV row name of a verdict.</summary>
		public static string Reason(Verdict v) => v switch
		{
			Verdict.Demand => "demand",
			Verdict.OutOfRadius => "out_of_radius",
			Verdict.Unreachable => "unreachable",
			Verdict.ZoneCap => "zone_cap",
			_ => "allied_served",
		};

		/// <summary>Two cells <paramref name="distanceSq"/> apart (squared cells) are within <paramref name="radiusCells"/>; a radius of 0 or less never holds.</summary>
		public static bool Within(long distanceSq, int radiusCells) => radiusCells > 0 && distanceSq <= (long)radiusCells * radiusCells;

		/// <summary>A friendly refinery <paramref name="distanceSq"/> away covers the point at this spacing.</summary>
		public static bool Covered(long distanceSq, int spacingCells) => Within(distanceSq, spacingCells);

		public static long DistanceSq(CPos a, CPos b)
		{
			var dx = (long)a.X - b.X;
			var dy = (long)a.Y - b.Y;
			return dx * dx + dy * dy;
		}

		/// <summary>The squared distance to the nearest of <paramref name="refineries"/>, long.MaxValue when there is none.</summary>
		public static long NearestSq(CPos cell, IReadOnlyList<CPos> refineries)
		{
			var best = long.MaxValue;
			if (refineries == null)
				return best;

			for (var i = 0; i < refineries.Count; i++)
				best = Math.Min(best, DistanceSq(cell, refineries[i]));

			return best;
		}

		/// <summary>No friendly refinery stands within <paramref name="spacingCells"/> of <paramref name="site"/>.</summary>
		public static bool SiteAllowed(CPos site, IReadOnlyList<CPos> friendlyRefineries, int spacingCells) =>
			!Covered(NearestSq(site, friendlyRefineries), spacingCells);

		/// <summary>
		/// The spacing tier of a site: <see cref="SpacingAllowed"/> when no friendly refinery is within <paramref name="spacingCells"/>,
		/// <see cref="SpacingFallbackOnly"/> when one is but none within the smaller <paramref name="fallbackCells"/>, else <see cref="SpacingRefused"/>.
		/// </summary>
		public static int SpacingTier(CPos site, IReadOnlyList<CPos> friendlyRefineries, int spacingCells, int fallbackCells)
		{
			var nearest = NearestSq(site, friendlyRefineries);
			if (!Covered(nearest, spacingCells))
				return SpacingAllowed;

			return fallbackCells < spacingCells && !Covered(nearest, fallbackCells) ? SpacingFallbackOnly : SpacingRefused;
		}

		/// <summary>
		/// The spacing pick over candidate sites in their search order: the first one allowed at the full spacing; only when none is, the first one
		/// allowed at the fallback spacing (<paramref name="fallback"/> true). Null when spacing leaves no site at either radius.
		/// </summary>
		public static CPos? PickSpaced(IEnumerable<CPos> candidates, IReadOnlyList<CPos> friendlyRefineries, int spacingCells, int fallbackCells, out bool fallback)
		{
			fallback = false;
			CPos? relaxed = null;
			foreach (var c in candidates)
			{
				var tier = SpacingTier(c, friendlyRefineries, spacingCells, fallbackCells);
				if (tier == SpacingAllowed)
					return c;

				if (tier == SpacingFallbackOnly && relaxed == null)
					relaxed = c;
			}

			fallback = relaxed.HasValue;
			return relaxed;
		}

		/// <summary>
		/// The friendly refineries counted at a zone anchor: those within <paramref name="spacingCells"/> of it.
		/// </summary>
		public static int CountWithin(CPos anchor, IReadOnlyList<CPos> refineries, int spacingCells)
		{
			var n = 0;
			if (refineries == null)
				return n;

			for (var i = 0; i < refineries.Count; i++)
				if (Covered(DistanceSq(anchor, refineries[i]), spacingCells))
					n++;

			return n;
		}

		/// <summary>
		/// The zones of one point set: deduplicated, sorted row-major (Y, then X); while points are uncovered, the first uncovered point seeds a zone
		/// that takes every uncovered point within <paramref name="radiusCells"/> of the seed (in row-major order). A radius of 0 or less makes every
		/// point its own zone. Deterministic: the result depends only on the point set.
		/// </summary>
		public static List<List<CPos>> Zones(IEnumerable<CPos> points, int radiusCells)
		{
			var sorted = new List<CPos>(new HashSet<CPos>(points));
			sorted.Sort((a, b) => a.Y != b.Y ? a.Y - b.Y : a.X - b.X);
			var covered = new bool[sorted.Count];
			var zones = new List<List<CPos>>();
			for (var s = 0; s < sorted.Count; s++)
			{
				if (covered[s])
					continue;

				var seed = sorted[s];
				var zone = new List<CPos>();
				for (var i = s; i < sorted.Count; i++)
				{
					if (covered[i] || (i != s && !Within(DistanceSq(seed, sorted[i]), radiusCells)))
						continue;

					covered[i] = true;
					zone.Add(sorted[i]);
				}

				zones.Add(zone);
			}

			return zones;
		}

		/// <summary>The anchor cell of a zone: the zone point nearest its integer centroid (ties: the earlier point in the zone's order).</summary>
		public static CPos ZoneAnchor(IReadOnlyList<CPos> zone)
		{
			if (zone == null || zone.Count == 0)
				return CPos.Zero;

			var sx = 0L;
			var sy = 0L;
			for (var i = 0; i < zone.Count; i++)
			{
				sx += zone[i].X;
				sy += zone[i].Y;
			}

			var centroid = new CPos((int)(sx / zone.Count), (int)(sy / zone.Count));
			var best = zone[0];
			var bestD = DistanceSq(best, centroid);
			for (var i = 1; i < zone.Count; i++)
			{
				var d = DistanceSq(zone[i], centroid);
				if (d < bestD)
				{
					best = zone[i];
					bestD = d;
				}
			}

			return best;
		}

		/// <summary>The yard cache refreshes when the yard set changed or <paramref name="refreshTicks"/> passed since the last refresh (never refreshed = due).</summary>
		public static bool ReachRefreshDue(int now, int lastRefreshTick, int refreshTicks, bool yardsChanged) =>
			yardsChanged || lastRefreshTick < 0 || now - lastRefreshTick >= Math.Max(1, refreshTicks);
	}
}
