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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Pathfinder;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// REPAIR-B3 (fleet/SPEC_2026-10-09_refinery_shared_coverage.md): the per-anchor coverage verdict.
	/// Covered = a usable own refinery is proven reachable in both directions within the serve bound;
	/// Unserved = examined and definitively not covered; Unknown = not yet examined within budget —
	/// never treated as covered nor as a permanent barrier.
	/// </summary>
	public enum RefineryCoverageVerdict : byte
	{
		Unknown = 0,
		Covered = 1,
		Unserved = 2,
	}

	/// <summary>
	/// REPAIR-B3: the bounded deterministic evaluation budget (SPEC budget section). Site/probe limits
	/// bound ONE anchor's evaluation (a pathological dense cluster defers instead of eating the whole
	/// refresh); TickProbeLimit paces the pathfinder spend shared across all anchors per world tick.
	/// DeferredCandidates and CacheHits are refresh-lifetime telemetry counters.
	/// </summary>
	public sealed class RefineryProbeBudget
	{
		public readonly int SiteLimit;
		public readonly int ProbeLimit;
		public readonly int TickProbeLimit;

		public int SitesEvaluated;
		public int ProbesUsed;
		public int DeferredCandidates;
		public int CacheHits;
		public int TickProbesUsed;

		public RefineryProbeBudget(int siteLimit, int probeLimit, int tickProbeLimit)
		{
			SiteLimit = siteLimit;
			ProbeLimit = probeLimit;
			TickProbeLimit = tickProbeLimit;
		}

		public void NewTick() => TickProbesUsed = 0;

		/// <summary>Per-anchor spend re-arm — the sweep calls it before each anchor it evaluates.</summary>
		public void NextAnchor()
		{
			SitesEvaluated = 0;
			ProbesUsed = 0;
		}

		public bool TryConsumeSite()
		{
			if (SitesEvaluated >= SiteLimit)
			{
				DeferredCandidates++;
				return false;
			}

			SitesEvaluated++;
			return true;
		}

		public bool TryConsumeProbe()
		{
			if (ProbesUsed >= ProbeLimit || TickProbesUsed >= TickProbeLimit)
			{
				DeferredCandidates++;
				return false;
			}

			ProbesUsed++;
			TickProbesUsed++;
			return true;
		}

		/// <summary>False when this tick's pathfinder spend is exhausted — resume next tick.</summary>
		public bool TickProbesOpen => TickProbesUsed < TickProbeLimit;
	}

	/// <summary>
	/// REPAIR-B3: the route witness — one directed leg's verdict and length in milli-tiles.
	/// Cached with the topology version that produced it; a stale version never counts as evidence.
	/// </summary>
	public readonly struct RefineryRouteWitness
	{
		public readonly bool Reachable;
		public readonly int RouteMilli;
		public readonly int Version;

		public RefineryRouteWitness(bool reachable, int routeMilli, int version)
		{
			Reachable = reachable;
			RouteMilli = routeMilli;
			Version = version;
		}
	}

	/// <summary>
	/// REPAIR-B3: pure route accounting — integer milli-tile path length (1000 per orthogonal step,
	/// 1414 per diagonal step, deterministic and symmetric across clients), the inclusive squared
	/// radius gate, and verdict aggregation. No World, no RNG, no allocations beyond the caller's.
	/// </summary>
	public static class RefineryCoverageOracle
	{
		public const int MilliPerCell = 1000;
		public const int MilliPerDiagonal = 1414;

		/// <summary>Path length in milli-tiles along the returned path steps (diagonal = √2 ≈ 1414).</summary>
		public static int RouteLengthMilli(IReadOnlyList<CPos> path)
		{
			if (path == null || path.Count < 2)
				return 0;

			var length = 0;
			for (var i = 1; i < path.Count; i++)
			{
				var d = path[i] - path[i - 1];
				length += d.X != 0 && d.Y != 0 ? MilliPerDiagonal : MilliPerCell;
			}

			return length;
		}

		/// <summary>True when the geometric cell distance (inclusive) is within the radius.</summary>
		public static bool WithinServeRadius(CPos a, CPos b, int serveRadiusCells)
		{
			var r = (long)serveRadiusCells * serveRadiusCells;
			return (a - b).LengthSquared <= r;
		}

		/// <summary>
		/// Both directed legs of a candidate (patch cell, dock cell) pair must exist and each must be
		/// within <paramref name="legLimitMilli"/> travelled milli-tiles. A missing or over-long leg is
		/// a durable access failure (Unserved for that pair), not congestion — the probe already runs
		/// with stationary-only blocking so moving traffic never fails a leg.
		/// </summary>
		public static bool BothLegsCover(RefineryRouteWitness outbound, RefineryRouteWitness inbound, int legLimitMilli) =>
			outbound.Reachable && inbound.Reachable
				&& outbound.RouteMilli <= legLimitMilli
				&& inbound.RouteMilli <= legLimitMilli;

		/// <summary>The pair's travel rank — the slower leg bounds a round trip; ties break by the other leg.</summary>
		public static int LegPairRank(RefineryRouteWitness outbound, RefineryRouteWitness inbound) =>
			Math.Max(outbound.RouteMilli, inbound.RouteMilli) * 100000 + Math.Min(outbound.RouteMilli, inbound.RouteMilli);

		/// <summary>
		/// Candidate-site ordering (SPEC §4): most newly covered anchors first, then lower travel cost,
		/// then stable X/Y. Returns a negative/zero/positive comparer result for (a vs b).
		/// </summary>
		public static int CompareSites(int aNewlyCovered, int aCost, CPos aCell, int bNewlyCovered, int bCost, CPos bCell)
		{
			var c = bNewlyCovered.CompareTo(aNewlyCovered);
			if (c != 0)
				return c;

			c = aCost.CompareTo(bCost);
			if (c != 0)
				return c;

			c = aCell.X.CompareTo(bCell.X);
			return c != 0 ? c : aCell.Y.CompareTo(bCell.Y);
		}

		/// <summary>
		/// The per-anchor verdict over the evaluated candidate pairs: Covered carries the best rank;
		/// any still-pending pair under budget pressure leaves the anchor Unknown rather than Unserved.
		/// </summary>
		public static RefineryCoverageVerdict Aggregate(bool anyCovered, bool anyDeferred) =>
			anyCovered ? RefineryCoverageVerdict.Covered
				: anyDeferred ? RefineryCoverageVerdict.Unknown
				: RefineryCoverageVerdict.Unserved;
	}

	/// <summary>
	/// REPAIR-B3: the live half of the oracle — wraps <see cref="PathSearch"/> with a null self (the
	/// theoretical-locomotor mode the engine explicitly supports) and stationary-only blocking so the
	/// verdict reflects durable obstacles (cliffs, water, walls, buildings, blocked passages) and never
	/// transient moving-unit congestion. World-free callers substitute their own probe delegate.
	/// </summary>
	public static class RefineryRouteProbe
	{
		public delegate List<CPos> Probe(CPos from, CPos to);

		/// <summary>
		/// One directed leg: from → to under <paramref name="locomotor"/>'s movement rules, stationary
		/// actors blocking, movers ignored. Returns the path cells or an empty list when unreachable.
		/// </summary>
		public static List<CPos> ProbeLeg(World world, Locomotor locomotor, CPos from, CPos to)
		{
			var search = PathSearch.ToTargetCell(world, locomotor, null, new[] { from }, to,
				BlockedByActor.Stationary, 100, laneBias: false);
			return search.FindPath();
		}
	}
}
