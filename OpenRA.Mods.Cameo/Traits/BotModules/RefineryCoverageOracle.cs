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
using OpenRA.Primitives;

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
	/// REPAIR-B3: the bounded deterministic evaluation budget (SPEC budget section, R1). Site/probe
	/// counters span ONE WHOLE REFRESH — a full circuit of the anchor list that may run over many
	/// ticks; they never re-arm mid-refresh, so 32 site evaluations and 64 directed probes bound the
	/// entire pass (32A/64A over A anchors is exactly what the spec forbids). TickProbeLimit paces
	/// the pathfinder spend per player per world tick. A new budget instance starts the next refresh.
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

		/// <summary>
		/// True once a refresh ceiling is spent — either site evaluations or directed probes. The
		/// distinction matters: a tick-cap refusal rewinds the cursor and resumes next tick, while a
		/// refresh-cap refusal defers the anchor for the rest of this refresh.
		/// </summary>
		public bool SiteCapSpent => SitesEvaluated >= SiteLimit;
		public bool ProbeCapSpent => ProbesUsed >= ProbeLimit;
		public bool RefreshSpent => SiteCapSpent || ProbeCapSpent;

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
	/// REPAIR-B3: the route witness — one directed leg's verdict, travelled length in milli-tiles,
	/// and estimated simulation travel time in milli-ticks (R1: ranking uses travel time, not raw
	/// geometry). Cached with the topology version that produced it; a stale version never counts
	/// as evidence.
	/// </summary>
	public readonly struct RefineryRouteWitness
	{
		public readonly bool Reachable;
		public readonly int RouteMilli;
		public readonly int TravelMilli;
		public readonly int Version;

		public RefineryRouteWitness(bool reachable, int routeMilli, int travelMilli, int version)
		{
			Reachable = reachable;
			RouteMilli = routeMilli;
			TravelMilli = travelMilli;
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

		/// <summary>
		/// Estimated simulation travel time in milli-ticks (R1, SPEC "estimated simulation travel
		/// time using movement/terrain speed"): each step costs its WDist length
		/// (stepLenMilli × 1024/1000, the engine's own EstimatedMoveDuration pattern) divided by the
		/// effective speed of the entered cell — the caller's <paramref name="cellSpeed"/> supplies
		/// harvester MobileInfo.Speed adjusted by locomotor terrain speed, the same formula as
		/// Mobile.MovementSpeedForCell without actor-bound modifiers. A zero-speed cell contradicts
		/// the path that found it and can never win: the route ranks infinite.
		/// </summary>
		public static int RouteTravelMilli(IReadOnlyList<CPos> path, Func<CPos, int> cellSpeed)
		{
			if (path == null || path.Count < 2)
				return 0;

			long travel = 0;
			for (var i = 1; i < path.Count; i++)
			{
				var speed = cellSpeed(path[i]);
				if (speed <= 0)
					return int.MaxValue;

				var d = path[i] - path[i - 1];
				var stepMilli = d.X != 0 && d.Y != 0 ? MilliPerDiagonal : MilliPerCell;
				travel += (long)stepMilli * 1024 / speed;
				if (travel >= int.MaxValue)
					return int.MaxValue;
			}

			return (int)travel;
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

		/// <summary>
		/// The pair's travel rank in simulation ticks (R1 — terrain speed matters: a longer
		/// fast-terrain route must outrank a shorter slow one). The slower leg bounds a round trip;
		/// ties break by the other leg. Packed into a long — TravelMilli values are milli-ticks and
		/// int packing would overflow.
		/// </summary>
		public static long LegPairRank(RefineryRouteWitness outbound, RefineryRouteWitness inbound) =>
			Math.Max(outbound.TravelMilli, inbound.TravelMilli) * 1_000_000L + Math.Min(outbound.TravelMilli, inbound.TravelMilli);

		/// <summary>
		/// R1 (SPEC "gameplay docking eligibility, including loaded-return forceEnter semantics"):
		/// whether a refinery dock can serve this harvester pair — the same contract the engine
		/// resolves in Harvester.CanDock / IDockClient.CanDockAt minus the transient terms. A loaded
		/// harvester can always dock at a type-compatible enabled host (CanDock: forceEnter ||
		/// !IsEmpty — the return leg models the loaded client), so occupancy and drag never gate:
		/// GenericDockSequence drags the client in once it reaches the dock cell, and reservations
		/// are transient by design exclusion. An EMPTY harvester cannot re-enter without forceEnter
		/// — but it never needs to: the outbound leg is an exit, not an entry. A null
		/// <paramref name="dockType"/> is the wildcard (a refinery with no DockHost trait).
		/// </summary>
		public static bool DockEligible(bool enabledAndInWorld, BitSet<DockType> harvesterType, BitSet<DockType>? dockType) =>
			enabledAndInWorld && (!dockType.HasValue || harvesterType.Overlaps(dockType.Value));

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
