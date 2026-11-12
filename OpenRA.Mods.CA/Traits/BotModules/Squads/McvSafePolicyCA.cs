// IMPL02 MCV-SAFE-01: pure decisions for keeping expansion MCVs out of enemy base ranges. No world access, no RNG. The world side lives in
// ExpansionPlannerBotModule.McvSafe.cs (Cameo assembly); every threshold comes from a named, Range-documented AggTeamMcv* Info field.
using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>A circular area an MCV must stay out of: the radius already includes weapon range, artillery reach and the safety margin. ExpiryTick &lt; 0 = permanent.</summary>
	public readonly record struct McvThreatZone(CPos Center, int RadiusCells, int ExpiryTick);

	public static class McvSafePolicyCA
	{
		/// <summary>Kinds of threat sources, in the order they are built (only used for the debug line).</summary>
		public const string KindDefence = "defence";
		public const string KindProduction = "production";
		public const string KindArtillery = "artillery";
		public const string KindSpawn = "spawn";
		public const string KindLoss = "loss";

		/// <summary>Unit conversions, not tunables: a percent parameter x 10 is permille, and a permille factor / 1000.0 is a plain multiplier.</summary>
		public const int PermillePerPercent = 10;
		public const double PermilleUnit = 1000.0;

		/// <summary>The exclusion radius of an enemy structure: its own weapon reach (0 for an unarmed producer) + artillery reach + margin.</summary>
		public static int ZoneRadius(int weaponRangeCells, int artilleryCells, int marginCells) =>
			Math.Max(0, weaponRangeCells) + Math.Max(0, artilleryCells) + Math.Max(0, marginCells);

		static bool Live(McvThreatZone z, int now) => z.ExpiryTick < 0 || now < z.ExpiryTick;

		static long DistanceSquared(CPos a, CPos b)
		{
			long dx = a.X - b.X, dy = a.Y - b.Y;
			return dx * dx + dy * dy;
		}

		/// <summary>Is the cell inside any live zone grown by extraCells (inclusive)?</summary>
		public static bool InZone(CPos cell, IReadOnlyList<McvThreatZone> zones, int now, int extraCells = 0)
		{
			if (zones == null)
				return false;

			foreach (var z in zones)
			{
				if (!Live(z, now))
					continue;

				long r = Math.Max(0, z.RadiusCells + extraCells);
				if (DistanceSquared(cell, z.Center) <= r * r)
					return true;
			}

			return false;
		}

		/// <summary>Cells between the cell and the nearest live zone edge (0 = on the edge or inside); int.MaxValue with no live zone.</summary>
		public static int EdgeDistanceCells(CPos cell, IReadOnlyList<McvThreatZone> zones, int now)
		{
			var best = int.MaxValue;
			if (zones == null)
				return best;

			foreach (var z in zones)
			{
				if (!Live(z, now))
					continue;

				var edge = (int)Math.Max(0, Math.Floor(Math.Sqrt(DistanceSquared(cell, z.Center))) - z.RadiusCells);
				if (edge < best)
					best = edge;
			}

			return best;
		}

		/// <summary>
		/// Forward-contain bonus in permille (1000 = none): a site just OUTSIDE an enemy zone denies build radius. The bonus is largest at the zone edge
		/// (1000 + bonusPercent x 10) and falls linearly to nothing at bandCells from it. A site inside a zone gets none (it is excluded elsewhere).
		/// </summary>
		public static int ForwardFactorPermille(int edgeDistanceCells, int bandCells, int bonusPercent)
		{
			if (edgeDistanceCells == int.MaxValue || bandCells <= 0 || bonusPercent <= 0 || edgeDistanceCells > bandCells)
				return 1000;

			return 1000 + bonusPercent * PermillePerPercent * (bandCells - edgeDistanceCells) / bandCells;
		}

		/// <summary>Resource-value bonus in permille: a field with at least fullPercent of the biggest field's cells is a full field and scores bonusPercent more.</summary>
		public static int FullFieldFactorPermille(int value, int maxValue, int fullPercent, int bonusPercent)
		{
			if (maxValue <= 0 || bonusPercent <= 0)
				return 1000;

			return (long)value * 100 >= (long)maxValue * fullPercent ? 1000 + bonusPercent * PermillePerPercent : 1000;
		}

		/// <summary>The extra path cost of a cell: costPerCell inside a live zone, otherwise 0.</summary>
		public static int PathCost(CPos cell, IReadOnlyList<McvThreatZone> zones, int now, int costPerCell) =>
			InZone(cell, zones, now) ? Math.Max(0, costPerCell) : 0;

		/// <summary>
		/// Does the path touch a live zone? Cells at the very start that are still inside a zone are tolerated (the MCV is leaving one it already stands
		/// in); once the path has been outside, any later zone cell counts. An empty or null path does not cross.
		/// </summary>
		public static bool PathCrossesZone(IEnumerable<CPos> path, IReadOnlyList<McvThreatZone> zones, int now)
		{
			if (path == null)
				return false;

			var leftZone = false;
			foreach (var c in path)
			{
				if (!InZone(c, zones, now))
				{
					leftZone = true;
					continue;
				}

				if (leftZone)
					return true;
			}

			return false;
		}

		/// <summary>
		/// The zones that count for the abort of a trip that began at startCell: a zone that already contained the start (grown by the abort margin) is
		/// left out, so an MCV that sets off from inside a zone (a loss zone at the base, a close producer, an unexplored start, the base fallback) is not
		/// stopped on every trip. Only entering a zone from outside aborts (review M2).
		/// </summary>
		public static List<McvThreatZone> ZonesNotContainingStart(IReadOnlyList<McvThreatZone> zones, CPos startCell, int now, int abortMarginCells)
		{
			var result = new List<McvThreatZone>();
			if (zones == null)
				return result;

			foreach (var z in zones)
				if (!InZone(startCell, new[] { z }, now, abortMarginCells))
					result.Add(z);

			return result;
		}

		/// <summary>
		/// En-route abort: the MCV is inside a zone grown by abortMarginCells, or a visible armed enemy stands within threatCells
		/// (nearestVisibleThreatCells &lt; 0 = none seen).
		/// </summary>
		public static bool ShouldAbort(CPos mcvCell, IReadOnlyList<McvThreatZone> zones, int now, int abortMarginCells, int nearestVisibleThreatCells, int threatCells) =>
			InZone(mcvCell, zones, now, abortMarginCells) || (nearestVisibleThreatCells >= 0 && nearestVisibleThreatCells <= threatCells);

		/// <summary>
		/// Picks up to count waypoints from a path given source-to-target order, evenly spaced, always ending on the last cell.
		/// </summary>
		public static List<CPos> Waypoints(IReadOnlyList<CPos> pathSourceToTarget, int count)
		{
			var result = new List<CPos>();
			if (pathSourceToTarget == null || pathSourceToTarget.Count == 0 || count <= 0)
				return result;

			var n = Math.Min(count, pathSourceToTarget.Count);
			for (var i = 1; i <= n; i++)
				result.Add(pathSourceToTarget[(int)((long)i * pathSourceToTarget.Count / n) - 1]);

			return result;
		}

		/// <summary>
		/// DIAG-FIX-01: is the site strictly nearer (integer squared cell distance) to some enemy start than to every own base? A tie is not nearer.
		/// False when either list is empty: with no known enemy start or no own base there is nothing to compare, so no field is refused by it.
		/// </summary>
		public static bool NearerToEnemyStart(CPos site, IReadOnlyList<CPos> enemyStarts, IReadOnlyList<CPos> ownBases)
		{
			if (enemyStarts == null || enemyStarts.Count == 0 || ownBases == null || ownBases.Count == 0)
				return false;

			var enemy = long.MaxValue;
			foreach (var s in enemyStarts)
				enemy = Math.Min(enemy, DistanceSquared(site, s));

			var own = long.MaxValue;
			foreach (var b in ownBases)
				own = Math.Min(own, DistanceSquared(site, b));

			return enemy < own;
		}

		/// <summary>
		/// DIAG-FIX-02 (CG only): the suspected enemy starts left after dropping every start this seat has explored (isExplored) where no enemy building
		/// has been seen within matchCells (squared integer distance, inclusive, the SpawnMatchCells rule). An unexplored start stays a candidate; an
		/// explored start next to a seen enemy building stays an enemy start (occupied); an explored start with none near it is empty and is dropped, so
		/// on a map with more starts than players the fields around unused starts are no longer refused all game. Input order is kept; pure; no RNG.
		/// </summary>
		public static List<CPos> PruneEmptyExploredStarts(IReadOnlyList<CPos> starts, Func<CPos, bool> isExplored, IReadOnlyList<CPos> seenEnemyBuildingCells, int matchCells)
		{
			var result = new List<CPos>();
			if (starts == null)
				return result;

			long r = Math.Max(0, matchCells);
			foreach (var s in starts)
			{
				var explored = isExplored != null && isExplored(s);
				var occupied = false;
				if (explored && seenEnemyBuildingCells != null)
					foreach (var b in seenEnemyBuildingCells)
						if (DistanceSquared(s, b) <= r * r)
						{
							occupied = true;
							break;
						}

				// Kept = not explored (still only a suspicion) or explored and occupied.
				if (!explored || occupied)
					result.Add(s);
			}

			return result;
		}

		/// <summary>An MCV that disappeared without an own yard within matchCells of where it was last seen was lost, not deployed.</summary>
		public static bool WasLost(CPos lastCell, IEnumerable<CPos> ownYardCells, int matchCells)
		{
			long r = Math.Max(0, matchCells);
			if (ownYardCells != null)
				foreach (var y in ownYardCells)
					if (DistanceSquared(lastCell, y) <= r * r)
						return false;

			return true;
		}
	}
}
