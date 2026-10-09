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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>Where a front anchor comes from; enemy anchors outrank the expansion (crawl) anchor.</summary>
	public enum FrontAnchorKind { RememberedDefences = 0, LastAttack = 1, EnemySpawn = 2, MapCentre = 3, Expansion = 4 }

	/// <summary>One front of one base cluster: a bearing toward an anchor plus the defence line on it.</summary>
	public sealed class BaseFront
	{
		public int Id;
		public FrontAnchorKind Kind;
		public CPos Anchor;
		public CPos Centre;
		public double DirX, DirY;
		public readonly List<CPos> LineCells = new();
		public int FrontProj = int.MinValue;
		public int RearProj = int.MaxValue;
		public int ArcEdgeProj = int.MinValue;
		public bool HasLine => LineCells.Count > 0;
		public int FirstSeenTick;
	}

	[Desc("Front/back placement owner (DESIGN 19.15): radar one per defended front strictly behind its defence line",
		"covering NEW approach ground (union coverage, overlap is waste); ground/naval production at the front,",
		"air-only producers exempt; tech/superweapon/passive-income buildings in the back. Advises the shared base",
		"builder through IBotFrontBackAdvisor; switched off, the base builder is untouched.")]
	public class BaseFrontBackPlannerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Advise the base builder. False = inert (placement classes stay with today's owners).")]
		public readonly bool Enabled = false;

		[Desc("Two building cells belong to one base cluster when they are at most this many cells apart (Chebyshev).")]
		public readonly int FrontLinkRadius = 14;

		[Desc("Two anchors closer than this angle merge into one front.")]
		public readonly int FrontMergeDegrees = 45;

		[Desc("Most fronts one base cluster tracks.")]
		public readonly int MaxFrontsPerBase = 3;

		[Desc("Half-angle of the cone that makes a front's approach area.")]
		public readonly int FrontConeHalfDegrees = 45;

		[Desc("A defence sits on the perimeter when its edgeness (percent of the maximum defence radius) is at least this.")]
		public readonly int PerimeterEdgePercent = 70;

		[Desc("Reference radius for edgeness when no base builder is found.")]
		public readonly int MaximumRadius = 20;

		[Desc("Radar cells must sit this many cells behind the rearmost defence of their front.")]
		public readonly int RadarMinSetbackCells = 2;

		[Desc("Radar cells must not sit farther behind the rearmost defence than this.")]
		public readonly int RadarMaxSetbackCells = 8;

		[Desc("How deep past the foremost defence a front's approach area reaches.")]
		public readonly int RadarApproachDepthCells = 14;

		[Desc("A second radar on one front is wanted only while it adds at least this many new approach cells.")]
		public readonly int RadarMinNewCoverageCells = 30;

		[Desc("Most radars one front may want.")]
		public readonly int RadarMaxPerFront = 2;

		[Desc("Ticks a radar want waits for its front to earn a defence line before it takes a back slot instead.")]
		public readonly int RadarWaitForDefenceTicks = 1500;

		[Desc("Excess power held per owned/planned radar provider so low power never blinds it.")]
		public readonly int RadarPowerMarginPerProvider = 60;

		[Desc("Production cells sit at most this far behind the foremost defence (0 = up to the line).")]
		public readonly int ProductionSetbackCells = 0;

		public override object Create(ActorInitializer init) { return new BaseFrontBackPlannerBotModule(init.Self, this); }
	}

	public class BaseFrontBackPlannerBotModule : ConditionalTrait<BaseFrontBackPlannerBotModuleInfo>, IBotFrontBackAdvisor, INotifyActorDisposing
	{
		const int CoverageScore = 1000;

		readonly World world;
		readonly OpenRA.Player player;

		// Own buildings, kept from the world's add/remove events: no enumeration of the world's actors.
		readonly HashSet<Actor> buildings = new();

		// "Front seen" memory for the radar wait-then-back rule, keyed on the front id (cluster * 64 + bearing bucket).
		readonly Dictionary<int, int> frontFirstSeen = new();
		readonly List<int> frontsSeenNow = new();

		BaseBuilderBotModuleCA[] baseBuilders;
		IBotMainTargetProvider[] mainTargetProviders;
		IBotRememberedDefenceProvider[] defenceProviders;
		IBotExpansionTargetProvider[] expansionProviders;
		IBotBuildOrderKnobs[] knobsProviders;

		int refreshedTick = -1;
		List<BaseFront> lastFronts = new();
		List<(CPos Cell, int RangeCells)> lastProviders = new();
		readonly Dictionary<BaseFront, int> frontRadarCount = new();
		readonly Dictionary<BaseFront, int> frontUncoveredApproach = new();
		readonly HashSet<CPos> lastUnionApproach = new();
		readonly HashSet<CPos> lastUnionCovered = new();
		int lastWantedRadars;
		int lastFrontsWithoutRadar;

		public BaseFrontBackPlannerBotModule(Actor self, BaseFrontBackPlannerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			world.ActorAdded += Added;
			world.ActorRemoved += Removed;
		}

		void Added(Actor a)
		{
			if (a.Owner == player && a.Info.HasTraitInfo<BuildingInfo>())
				buildings.Add(a);
		}

		void Removed(Actor a) { buildings.Remove(a); }

		void INotifyActorDisposing.Disposing(Actor self)
		{
			world.ActorAdded -= Added;
			world.ActorRemoved -= Removed;
		}

		bool IBotFrontBackAdvisor.IsActive => !IsTraitDisabled && Info.Enabled;

		// --- world-free helpers (tested) ---

		public static int CompareCell(CPos a, CPos b)
		{
			return a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
		}

		static CPos Centroid(IReadOnlyCollection<CPos> cells)
		{
			return new CPos(cells.Sum(c => c.X) / cells.Count, cells.Sum(c => c.Y) / cells.Count);
		}

		/// <summary>Unit direction origin to anchor; a degenerate anchor points +X.</summary>
		public static (double X, double Y) UnitDir(CPos origin, CPos anchor)
		{
			var dx = anchor.X - origin.X;
			var dy = anchor.Y - origin.Y;
			var len = Math.Sqrt(dx * dx + dy * dy);
			return len < 1e-6 ? (1, 0) : (dx / len, dy / len);
		}

		/// <summary>Signed projection of the cell on the front axis, in whole cells.</summary>
		public static int Project(CPos cell, CPos origin, double dirX, double dirY)
		{
			return (int)Math.Round((cell.X - origin.X) * dirX + (cell.Y - origin.Y) * dirY);
		}

		/// <summary>The quantized bearing (0..63) of origin to anchor — the stable part of a front's id.</summary>
		public static int BearingBucket(CPos origin, CPos anchor)
		{
			var d = UnitDir(origin, anchor);
			var b = (int)Math.Round(Math.Atan2(d.Y, d.X) * 32 / Math.PI);
			return (b + 64) & 63;
		}

		/// <summary>Cosine of an angle in degrees, for merge/cone thresholds.</summary>
		public static double CosDegrees(int degrees)
		{
			return Math.Cos(degrees * Math.PI / 180);
		}

		static BaseFront BestAligned(CPos cell, CPos baseCenter, List<BaseFront> fronts)
		{
			var dir = UnitDir(baseCenter, cell);
			BaseFront best = null;
			var bestCos = 0.0;
			foreach (var f in fronts)
			{
				var cos = dir.X * f.DirX + dir.Y * f.DirY;
				if (cos > bestCos)
				{
					bestCos = cos;
					best = f;
				}
			}

			return best;
		}

		/// <summary>
		/// BP-1: anchors (priority order, enemy first, expansion last) become fronts — an anchor whose bearing is
		/// within mergeCos of an already-kept one merges into it. Perimeter defences and base cells join the front
		/// their own bearing aligns with best (positive alignment only). At most maxFronts fronts, in anchor order.
		/// </summary>
		public static List<BaseFront> BuildFronts(CPos baseCenter,
			IReadOnlyList<(CPos Anchor, FrontAnchorKind Kind)> anchors,
			IReadOnlyList<CPos> perimeterDefences, IReadOnlyList<CPos> baseCells,
			double mergeCos, int maxFronts)
		{
			var fronts = new List<BaseFront>();
			foreach (var (anchor, kind) in anchors)
			{
				if (anchor == baseCenter || fronts.Count >= maxFronts)
					continue;

				var dir = UnitDir(baseCenter, anchor);
				var merged = fronts.Any(kept => dir.X * kept.DirX + dir.Y * kept.DirY >= mergeCos);
				if (!merged)
					fronts.Add(new BaseFront { Id = fronts.Count, Kind = kind, Anchor = anchor, Centre = baseCenter, DirX = dir.X, DirY = dir.Y });
			}

			foreach (var d in perimeterDefences)
			{
				var front = BestAligned(d, baseCenter, fronts);
				if (front == null)
					continue;

				front.LineCells.Add(d);
				var p = Project(d, baseCenter, front.DirX, front.DirY);
				front.FrontProj = Math.Max(front.FrontProj, p);
				front.RearProj = Math.Min(front.RearProj, p);
			}

			foreach (var c in baseCells)
			{
				var front = BestAligned(c, baseCenter, fronts);
				if (front != null)
					front.ArcEdgeProj = Math.Max(front.ArcEdgeProj, Project(c, baseCenter, front.DirX, front.DirY));
			}

			return fronts;
		}

		/// <summary>
		/// Approach band bounds in double space: [Inner, Outer] Euclidean radii around the centre covering
		/// every cell whose projection reaches [frontProj, frontProj+depthCells] inside the cone — the
		/// worst-case distance of a band cell is (frontProj+depthCells)/coneCos, so the outer edge grows
		/// with how far out the line crawled and can exceed the engine's MaximumTileSearchRange on large
		/// maps. All arithmetic in double so Inf/NaN/sentinel ints can never wrap an int cast into a
		/// legal-looking range. Null ONLY when the cone math is NaN or (forward cone only) the whole
		/// band lies below radius 0 — a band past the engine cap or a cone of 90°+ still exists on the
		/// map and must be enumerated (see ApproachSpace); returning no cells there would silently
		/// truncate coverage.
		/// </summary>
		public static (double Inner, double Outer)? ApproachBandBounds(int frontProj, int depthCells, double coneCos)
		{
			// coneCos <= 0 (cone half-angle >= 90°): the cone admits cells at ANY distance
			// perpendicular to or behind the front axis — the band is genuinely unbounded, so the
			// outer edge is +Inf (the box degenerates to the whole map) rather than empty.
			var outer = coneCos > 0
				? Math.Ceiling(((double)frontProj + depthCells) / coneCos) + 1
				: double.PositiveInfinity;
			var inner = Math.Max(0.0, frontProj - 1.0);
			if (double.IsNaN(outer) || double.IsNaN(coneCos) || inner > outer)
				return null;

			return (inner, outer);
		}

		/// <summary>
		/// Bounding-box half-side for the beyond-cap enumeration, clamped to the map's own span — every
		/// playable coordinate lies inside [0, MapSize) on any grid type, so a box of that radius covers
		/// the whole map and a huge or infinite outer bound degenerates to it without overflow.
		/// </summary>
		public static int WideSpaceRadius(double outer, int mapReach)
		{
			return double.IsFinite(outer) ? (int)Math.Min((long)Math.Ceiling(outer), mapReach) : mapReach;
		}

		/// <summary>
		/// Playable cells of the square [centre ± r] ∩ [0, MapSize): iterates the box directly — O(box),
		/// never a full-map scan — and applies the same playable-bounds predicate the default
		/// FindTilesInAnnulus path uses, so the wide path counts exactly the cells a legal annulus would.
		/// </summary>
		static IEnumerable<CPos> PlayableBox(Map map, CPos centre, int r)
		{
			var x0 = Math.Max(0L, centre.X - (long)r);
			var x1 = Math.Min(map.MapSize.Width - 1L, centre.X + (long)r);
			var y0 = Math.Max(0L, centre.Y - (long)r);
			var y1 = Math.Min(map.MapSize.Height - 1L, centre.Y + (long)r);
			for (var y = y0; y <= y1; y++)
				for (var x = x0; x <= x1; x++)
				{
					var c = new CPos((int)x, (int)y);
					if (map.Contains(c))
						yield return c;
				}
		}

		/// <summary>
		/// Candidate cells for <see cref="ApproachCells"/>: when the band fits inside the engine's
		/// MaximumTileSearchRange, the exact annulus rings; past it — a defence line crawled far out on a
		/// large map (playtest C1) — the playable Chebyshev box of half-side ceil(outer). The box is a
		/// guaranteed superset: TilesByDistance rings are Euclidean in CPos space (i²+j² ≤ d² for ring d)
		/// on every grid type, so every cell the unconstrained annulus would return satisfies |dx|,|dy|
		/// ≤ ceil(outer). Coverage is never silently truncated, bounded by map area, and deterministic
		/// in row-major order; ApproachCells still applies the exact band+cone predicate.
		/// </summary>
		public static IEnumerable<CPos> ApproachSpace(Map map, CPos centre, int frontProj, int depthCells, double coneCos)
		{
			var bounds = ApproachBandBounds(frontProj, depthCells, coneCos);
			if (!bounds.HasValue)
				return Enumerable.Empty<CPos>();

			var (inner, outer) = bounds.Value;
			if (outer <= map.Grid.MaximumTileSearchRange)
				return map.FindTilesInAnnulus(centre, (int)inner, (int)outer);

			var reach = Math.Max(map.MapSize.Width, map.MapSize.Height);
			var r = WideSpaceRadius(outer, reach);
			return PlayableBox(map, centre, r);
		}

		/// <summary>
		/// The cells of a front's approach: the band from the foremost defence depthCells farther out, inside the
		/// front cone. A front with no defence line has no approach (nothing defines the line).
		/// </summary>
		public static List<CPos> ApproachCells(IEnumerable<CPos> space, CPos baseCenter, BaseFront front,
			int depthCells, double coneCos)
		{
			var approach = new List<CPos>();
			if (!front.HasLine)
				return approach;

			// Double space: frontProj + depthCells must not wrap into a negative upper edge
			// at extreme configured depth (int.MinValue/maxValue inputs would empty the band).
			var bandMax = front.FrontProj + (double)depthCells;
			foreach (var c in space)
			{
				var p = Project(c, baseCenter, front.DirX, front.DirY);
				if (p < front.FrontProj || p > bandMax)
					continue;

				var dir = UnitDir(baseCenter, c);
				if (dir.X * front.DirX + dir.Y * front.DirY >= coneCos)
					approach.Add(c);
			}

			return approach;
		}

		/// <summary>Approach cells a provider at <paramref name="cell"/> would newly cover (overlap with an own circle is waste).</summary>
		public static int NewCoverageCount(CPos cell, int rangeCells, IReadOnlyCollection<CPos> approach, IReadOnlySet<CPos> covered)
		{
			var n = 0;
			var r2 = rangeCells * rangeCells;
			foreach (var c in approach)
				if (!covered.Contains(c) && (c - cell).LengthSquared <= r2)
					n++;

			return n;
		}

		/// <summary>
		/// Radar pick (§19.15b): strictly inside the setback band behind the rearmost defence, scored by NEWLY
		/// covered approach cells (union coverage) then smaller setback. No legal candidate = FrontBackPick.None;
		/// the caller then waits for the line or takes the back slot — never forward.
		/// </summary>
		public static FrontBackPick ChooseRadarCell(IReadOnlyList<CPos> candidates, CPos baseCenter, BaseFront front,
			IReadOnlyCollection<CPos> approach, IReadOnlySet<CPos> covered,
			int rangeCells, int minSetback, int maxSetback)
		{
			if (!front.HasLine)
				return FrontBackPick.None;

			CPos? best = null;
			var bestScore = int.MinValue;
			var bestNew = 0;
			var bestOverlap = 0;
			var bestSetback = 0;
			foreach (var cell in candidates)
			{
				var proj = Project(cell, baseCenter, front.DirX, front.DirY);
				var setback = front.RearProj - proj;
				if (setback < minSetback || setback > maxSetback)
					continue;

				var inRange = 0;
				var r2 = rangeCells * rangeCells;
				foreach (var c in approach)
					if ((c - cell).LengthSquared <= r2)
						inRange++;

				var newCov = NewCoverageCount(cell, rangeCells, approach, covered);
				var score = newCov * CoverageScore - setback;
				if (best == null || score > bestScore || (score == bestScore && CompareCell(cell, best.Value) < 0))
				{
					bestScore = score;
					best = cell;
					bestNew = newCov;
					bestOverlap = inRange - newCov;
					bestSetback = setback;
				}
			}

			return best == null ? FrontBackPick.None
				: new FrontBackPick(best, false, front.Id, Project(best.Value, baseCenter, front.DirX, front.DirY), bestNew, bestOverlap, bestSetback);
		}

		/// <summary>
		/// Production pick: the most forward legal cell that is still at or behind the front limit — the foremost
		/// defence of the enemy front, or the foremost own building in its arc when the front has no line yet —
		/// minus the configured setback. No qualifying cell = FrontBackPick.None (the caller keeps a neutral pick).
		/// </summary>
		public static FrontBackPick ChooseProductionCell(IReadOnlyList<CPos> candidates, CPos baseCenter,
			BaseFront front, int limitProj, int setbackCells)
		{
			CPos? best = null;
			var bestProj = int.MinValue;
			foreach (var cell in candidates)
			{
				var proj = Project(cell, baseCenter, front.DirX, front.DirY);
				if (proj > limitProj - setbackCells)
					continue;

				if (best == null || proj > bestProj || (proj == bestProj && CompareCell(cell, best.Value) < 0))
				{
					best = cell;
					bestProj = proj;
				}
			}

			return best == null ? FrontBackPick.None
				: new FrontBackPick(best, false, front.Id, bestProj, 0, 0, 0);
		}

		/// <summary>
		/// The ABSOLUTE radar target of one front (B1): one provider per defended front, plus one justified extra
		/// while uncovered approach ground still reaches the threshold — the pick re-checks the candidate's own
		/// delta — capped at <paramref name="maxPerFront"/>. An undefended front wants none; it never goes forward.
		/// </summary>
		public static int WantedForFront(bool hasLine, int uncoveredApproachCells, int minNewCoverage, int maxPerFront)
		{
			if (!hasLine)
				return 0;

			var want = 1;
			if (uncoveredApproachCells >= minNewCoverage)
				want++;

			return Math.Min(want, maxPerFront);
		}

		/// <summary>
		/// The ABSOLUTE radar target the caller compares against its total owned/planned count (B1 rev-2):
		/// every front's want is met against the providers already assigned to THAT front — owned + the
		/// unmet per-front need. A surplus or unassigned provider (an extra on one front, a survivor at a
		/// now-undefended base) stays owned but never consumes another front's first slot; two fronts can
		/// never starve each other.
		/// </summary>
		public static int AggregateRadarTarget(int ownedProviders, IReadOnlyList<int> wantPerFront, IReadOnlyList<int> assignedPerFront)
		{
			var target = ownedProviders;
			for (var i = 0; i < wantPerFront.Count; i++)
				target += Math.Max(0, wantPerFront[i] - assignedPerFront[i]);

			return target;
		}

		/// <summary>
		/// New approach cells a band pick must add to be accepted (B4): the first provider on a front must
		/// positively reach the approach (>= 1 new cell); an extra only pays for itself at the configured threshold.
		/// </summary>
		public static int RequiredNewCoverage(int insideProviders, int minNewCoverage)
		{
			return insideProviders == 0 ? 1 : minNewCoverage;
		}

		/// <summary>
		/// Defence cells belonging to this cluster (B2): a remote outpost's towers never create or move this
		/// cluster's defence line.
		/// </summary>
		public static List<CPos> ClusterDefences(IEnumerable<CPos> defenceLocations, IReadOnlySet<CPos> clusterCells)
		{
			var own = new List<CPos>();
			foreach (var d in defenceLocations)
				if (clusterCells.Contains(d))
					own.Add(d);

			return own;
		}

		/// <summary>
		/// The cells a fallback radar may take (B3): strictly behind the front's rearmost defence, or not ahead
		/// of the base centre when the front has no line yet. Everything else is forward — the radar Holds instead.
		/// </summary>
		public static List<CPos> SafeBackCells(IReadOnlyList<CPos> candidates, CPos baseCenter, BaseFront front)
		{
			var safe = new List<CPos>();
			foreach (var c in candidates)
			{
				var p = Project(c, baseCenter, front.DirX, front.DirY);
				if (front.HasLine ? p < front.RearProj : p <= 0)
					safe.Add(c);
			}

			return safe;
		}

		/// <summary>
		/// Valuable pick: the cell farthest from EVERY front (minimise the maximum projection over all front axes),
		/// the multi-front generalisation of the legacy sortMax away from the last attack. No fronts = no answer.
		/// </summary>
		public static FrontBackPick ChooseValuableCell(IReadOnlyList<CPos> candidates, CPos baseCenter,
			IReadOnlyList<BaseFront> fronts)
		{
			if (fronts.Count == 0)
				return FrontBackPick.None;

			CPos? best = null;
			var bestMax = int.MinValue;
			var bestProj = 0;
			foreach (var cell in candidates)
			{
				var max = int.MinValue;
				var proj = 0;
				foreach (var f in fronts)
				{
					proj = Project(cell, baseCenter, f.DirX, f.DirY);
					if (proj > max)
						max = proj;
				}

				if (best == null || max < bestMax || (max == bestMax && CompareCell(cell, best.Value) < 0))
				{
					best = cell;
					bestMax = max;
					bestProj = proj;
				}
			}

			return best == null ? FrontBackPick.None
				: new FrontBackPick(best, false, -1, bestProj, 0, 0, 0);
		}

		// --- classification (world-free) ---

		public static bool IsRadarProvider(ActorInfo info)
		{
			return info.HasTraitInfo<RangedGpsProviderInfo>() || info.HasTraitInfo<ProvidesRadarInfo>();
		}

		/// <summary>The provider's dot radius in whole cells (Range is a WDist; floors — a partial cell does not count).</summary>
		public static int RadarRangeCells(ActorInfo info)
		{
			// Max over providers: an actor's effective dot radius is its strongest coverage
			// (multi-provider actors like ra1_allies_radardome crash single-instance lookups).
			// OPTIMISTIC: info-level Max counts every declared provider, including
			// condition-gated variants that may spawn disabled (the dome/sensor carry
			// mutually exclusive 20000/30000 ranges). Live actors must use
			// EnabledRadarRangeCells, which filters to enabled traits.
			return info.TraitInfos<RangedGpsProviderInfo>().Select(p => p.Range.Length).DefaultIfEmpty().Max() / 1024;
		}

		/// <summary>A live actor's dot radius in whole cells: Max over ENABLED providers only.</summary>
		public static int EnabledRadarRangeCells(IEnumerable<RangedGpsProvider> providers)
		{
			return providers.Where(t => !t.IsTraitDisabled).Select(t => t.Info.Range.Length).DefaultIfEmpty().Max() / 1024;
		}

		static bool IsAircraftQueueName(string name, ProductionQueueInfo[] queues)
		{
			if (name != null && name.Contains("aircraft", StringComparison.OrdinalIgnoreCase))
				return true;

			foreach (var q in queues)
				if (string.Equals(q.Type, name, StringComparison.Ordinal)
					&& ((q.Group != null && q.Group.Contains("aircraft", StringComparison.OrdinalIgnoreCase))
						|| (q.Type != null && q.Type.Contains("aircraft", StringComparison.OrdinalIgnoreCase))))
					return true;

			return false;
		}

		/// <summary>
		/// An air-only producer: produces at least one queue and EVERY produced queue is aircraft (rules-derived via
		/// Production.Produces queue names and the actor's ProductionQueue types/groups). "Aircraft is fast" — exempt
		/// from front placement (maintainer ruling 2026-10-04).
		/// </summary>
		public static bool IsAirOnlyProducer(ActorInfo info)
		{
			var produced = info.TraitInfos<ProductionInfo>()
				.Where(p => !p.Produces.IsDefaultOrEmpty)
				.SelectMany(p => p.Produces)
				.ToArray();
			if (produced.Length == 0)
				return false;

			var queues = info.TraitInfos<ProductionQueueInfo>().ToArray();
			return produced.All(n => IsAircraftQueueName(n, queues));
		}

		/// <summary>Passive income (the ruling's "income buildings"): periodic cash generation.</summary>
		public static bool IsPassiveIncome(ActorInfo info) => info.HasTraitInfo<CashTricklerInfo>();

		/// <summary>
		/// BP-1 class order: radar carrier → defence → ground/naval production → valuable (fragile list, tech or
		/// superweapon category when a knobs provider answers, passive income) → cheap → residue. Refineries are
		/// labelled for the log only — the refinery law places them.
		/// </summary>
		public static FrontBackClass Classify(ActorInfo info, int crawlCostThreshold,
			IReadOnlySet<string> valuableNames, Func<ActorInfo, string> categoryOf)
		{
			if (info.HasTraitInfo<RefineryInfo>())
				return FrontBackClass.Refinery;

			if (IsRadarProvider(info))
				return FrontBackClass.Radar;

			if (info.HasTraitInfo<AttackBaseInfo>())
				return FrontBackClass.Defence;

			if (info.TraitInfos<ProductionInfo>().Any(p => !p.Produces.IsDefaultOrEmpty) && !IsAirOnlyProducer(info))
				return FrontBackClass.Production;

			var valuable = valuableNames != null && valuableNames.Contains(info.Name);
			if (!valuable && categoryOf != null)
			{
				var category = categoryOf(info);
				valuable = string.Equals(category, BuildOrderCategory.Tech, StringComparison.Ordinal)
					|| string.Equals(category, BuildOrderCategory.Superweapon, StringComparison.Ordinal);
			}

			if (valuable || IsPassiveIncome(info))
				return FrontBackClass.Valuable;

			var cost = info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			if (cost > 0 && cost < crawlCostThreshold)
				return FrontBackClass.Crawl;

			return FrontBackClass.Building;
		}

		/// <summary>
		/// The radar building to queue: prefer one whose produced queues do not collide with queues an owned
		/// building already produces (the Upgrades queue is singleton — a second commcenter doubles it). Cheapest
		/// first, name breaks ties; when every candidate collides the cheapest still wins (a blind front is worse).
		/// </summary>
		public static ActorInfo ChooseRadarProvider(IReadOnlyList<ActorInfo> candidates, IReadOnlySet<string> ownedQueues)
		{
			ActorInfo best = null;
			var bestKey = 0;
			var bestCost = int.MaxValue;
			foreach (var c in candidates)
			{
				var collides = c.TraitInfos<ProductionInfo>()
					.Where(p => !p.Produces.IsDefaultOrEmpty)
					.SelectMany(p => p.Produces)
					.Any(ownedQueues.Contains) ? 1 : 0;
				var cost = c.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? int.MaxValue;
				if (best == null || collides < bestKey || (collides == bestKey && (cost < bestCost
					|| (cost == bestCost && string.CompareOrdinal(c.Name, best.Name) < 0))))
				{
					best = c;
					bestKey = collides;
					bestCost = cost;
				}
			}

			return best;
		}

		// --- world-side assembly ---

		List<Actor> OwnBuildingsOrdered()
		{
			return buildings.Where(b => !b.IsDead && b.IsInWorld && b.Owner == player)
				.OrderBy(a => a.Location.X).ThenBy(a => a.Location.Y).ThenBy(a => a.ActorID)
				.ToList();
		}

		List<(CPos Anchor, FrontAnchorKind Kind)> EnemyAnchors(CPos baseCenter)
		{
			var anchors = new List<(CPos, FrontAnchorKind)>();
			defenceProviders ??= player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>().ToArray();
			var remembered = defenceProviders.SelectMany(p => p.RememberedDefences()).ToArray();
			if (remembered.Length > 0)
			{
				// One anchor per enemy with remembered defences: the main target first, then the
				// best-remembered enemies — each is a side of our territory that faces someone.
				mainTargetProviders ??= player.PlayerActor.TraitsImplementing<IBotMainTargetProvider>().ToArray();
				var main = mainTargetProviders.Select(p => p.MainTarget).FirstOrDefault(t => t != null);
				foreach (var g in remembered.GroupBy(d => d.Enemy)
					.OrderByDescending(g => main != null && g.Key == main)
					.ThenByDescending(g => g.Count())
					.ThenBy(g => g.Key?.ClientIndex ?? int.MaxValue))
					anchors.Add((new CPos((int)g.Average(d => d.Cell.X), (int)g.Average(d => d.Cell.Y)), FrontAnchorKind.RememberedDefences));
			}
			else
			{
				baseBuilders ??= player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().ToArray();
				var builder = baseBuilders.FirstOrDefault(b => b.IsTraitEnabled());
				if (builder?.DefenseCenter is CPos attacked)
					anchors.Add((attacked, FrontAnchorKind.LastAttack));
				else
				{
					// Enemy spawn = public map data (lobby spawn assignment), the pre-contact anchor.
					var spawns = world.Players
						.Where(p => p.Playable && !p.IsAlliedWith(player))
						.OrderBy(p => (p.HomeLocation - baseCenter).LengthSquared)
						.ThenBy(p => p.ClientIndex)
						.ToArray();
					if (spawns.Length > 0)
						anchors.Add((spawns[0].HomeLocation, FrontAnchorKind.EnemySpawn));
					else
					{
						var size = world.Map.MapSize;
						anchors.Add((new CPos(size.Width / 2, size.Height / 2), FrontAnchorKind.MapCentre));
					}
				}
			}

			return anchors;
		}

		CPos? ExpansionAnchor()
		{
			expansionProviders ??= player.PlayerActor.TraitsImplementing<IBotExpansionTargetProvider>().ToArray();
			return expansionProviders.Select(p => p.ExpansionTarget).FirstOrDefault(t => t != null);
		}

		string CategoryOf(ActorInfo info)
		{
			knobsProviders ??= player.PlayerActor.TraitsImplementing<IBotBuildOrderKnobs>().ToArray();
			return knobsProviders.FirstEnabled()?.CategoryOf(info.Name);
		}

		/// <summary>Own live actors carrying an enabled radar trait (power-gated: disabled does not count).</summary>
		List<(CPos Cell, int RangeCells)> EnabledRadarProviders(List<Actor> owned)
		{
			var providers = new List<(CPos, int)>();
			foreach (var b in owned)
			{
				var ranged = b.TraitsImplementing<RangedGpsProvider>();
				if (ranged.Any(t => !t.IsTraitDisabled))
				{
					providers.Add((b.Location, EnabledRadarRangeCells(ranged)));
					continue;
				}

				if (b.TraitsImplementing<ProvidesRadar>().Any(t => !t.IsTraitDisabled))
					providers.Add((b.Location, 0));
			}

			return providers;
		}

		/// <summary>
		/// Fronts + radar bookkeeping, at most once per tick: clusters the base, builds the fronts, computes each
		/// front's approach and the union the live providers already cover, and counts the radar wants
		/// (one per defended front + a justified extra where a wide approach stays uncovered).
		/// </summary>
		void Refresh()
		{
			if (refreshedTick == world.WorldTick)
				return;

			refreshedTick = world.WorldTick;
			var owned = OwnBuildingsOrdered();
			lastProviders = EnabledRadarProviders(owned);
			lastFronts = new List<BaseFront>();
			frontRadarCount.Clear();
			frontUncoveredApproach.Clear();
			lastUnionApproach.Clear();
			lastUnionCovered.Clear();
			lastWantedRadars = 0;
			lastFrontsWithoutRadar = 0;

			var cells = new List<CPos>();
			foreach (var b in owned)
			{
				var bi = b.Info.TraitInfoOrDefault<BuildingInfo>();
				if (bi != null)
					cells.AddRange(bi.Tiles(b.Location));
			}

			if (cells.Count == 0)
				return;

			baseBuilders ??= player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().ToArray();
			var maxRadius = baseBuilders.FirstOrDefault(b => b.IsTraitEnabled())?.Info.MaximumDefenseRadius ?? Info.MaximumRadius;
			var mergeCos = CosDegrees(Info.FrontMergeDegrees);
			var coneCos = CosDegrees(Info.FrontConeHalfDegrees);
			var expansion = ExpansionAnchor();
			frontsSeenNow.Clear();

			var clusterIndex = 0;
			foreach (var cluster in DefenseCoveragePlanner.ClusterFronts(cells, Info.FrontLinkRadius))
			{
				var centre = Centroid(cluster);
				var anchors = EnemyAnchors(centre);
				if (expansion is CPos aim)
					anchors.Add((aim, FrontAnchorKind.Expansion));

				// Only this cluster's own defences draw its line — a remote outpost's towers never move it (B2).
				var clusterSet = new HashSet<CPos>(cluster);
				var perimeter = ClusterDefences(
						owned.Where(b => b.Info.HasTraitInfo<AttackBaseInfo>()).Select(b => b.Location), clusterSet)
					.Where(d => DefenseCoveragePlanner.Edgeness(d, centre, maxRadius) >= Info.PerimeterEdgePercent)
					.ToList();

				var fronts = BuildFronts(centre, anchors, perimeter, cluster, mergeCos, Info.MaxFrontsPerBase);
				foreach (var f in fronts)
				{
					f.Id = clusterIndex * 64 + BearingBucket(centre, f.Anchor);
					if (!frontFirstSeen.TryGetValue(f.Id, out var seen))
						frontFirstSeen[f.Id] = seen = world.WorldTick;

					f.FirstSeenTick = seen;
					frontsSeenNow.Add(f.Id);
					lastFronts.Add(f);
					frontRadarCount[f] = 0;
				}

				// A provider counts for the front of ITS OWN cluster its bearing aligns with best.
				foreach (var (cell, _) in lastProviders)
					if (clusterSet.Contains(cell) && BestAligned(cell, centre, fronts) is BaseFront aligned)
						frontRadarCount[aligned]++;

				clusterIndex++;
			}

			foreach (var key in frontFirstSeen.Keys.Where(k => !frontsSeenNow.Contains(k)).ToArray())
				frontFirstSeen.Remove(key);

			// Approach areas per front, their union, and the share the live providers already cover.
			foreach (var f in lastFronts)
			{
				if (!f.HasLine)
				{
					frontUncoveredApproach[f] = 0;
					continue;
				}

				// The approach band can reach past the engine's MaximumTileSearchRange when the
				// defence line crawled far out (playtest C1 — outer=71 on Imminent Destruction):
				// exact annulus rings inside the cap, map-clipped bounding box beyond it — coverage
				// is never silently truncated and FindTilesInAnnulus never sees an illegal range.
				var space = ApproachSpace(world.Map, f.Centre, f.FrontProj,
					Info.RadarApproachDepthCells, coneCos);
				var approach = ApproachCells(space, f.Centre, f, Info.RadarApproachDepthCells, coneCos);
				var uncovered = 0;
				foreach (var c in approach)
				{
					lastUnionApproach.Add(c);
					var coveredBy = false;
					foreach (var (cell, range) in lastProviders)
						if (range > 0 && (c - cell).LengthSquared <= range * range)
						{
							coveredBy = true;
							break;
						}

					if (coveredBy)
						lastUnionCovered.Add(c);
					else
						uncovered++;
				}

				frontUncoveredApproach[f] = uncovered;
			}

			foreach (var f in lastFronts)
			{
				if (!f.HasLine)
					continue;

				if (frontRadarCount[f] == 0)
					lastFrontsWithoutRadar++;
			}

			// The absolute target the caller compares against ALL owned providers (B1 rev-2): each
			// front's want is met against the providers assigned to THAT front — owned + the unmet
			// per-front need. A surplus provider on one front or a survivor at a now-undefended base
			// stays owned but never consumes another front's first slot.
			var wants = new List<int>(lastFronts.Count);
			var assigned = new List<int>(lastFronts.Count);
			foreach (var f in lastFronts)
			{
				wants.Add(WantedForFront(f.HasLine, frontUncoveredApproach[f],
					Info.RadarMinNewCoverageCells, Info.RadarMaxPerFront));
				assigned.Add(frontRadarCount.GetValueOrDefault(f));
			}

			lastWantedRadars = AggregateRadarTarget(lastProviders.Count, wants, assigned);
		}

		BaseFront FrontForCell(CPos baseCenter)
		{
			return lastFronts.Count == 0 ? null
				: lastFronts.OrderBy(f => (f.Centre - baseCenter).LengthSquared)
					.ThenBy(f => f.Centre.X).ThenBy(f => f.Centre.Y)
					.First();
		}

		// --- IBotFrontBackAdvisor ---

		FrontBackClass IBotFrontBackAdvisor.Classify(ActorInfo info)
		{
			baseBuilders ??= player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().ToArray();
			var builder = baseBuilders.FirstOrDefault(b => b.IsTraitEnabled());
			return Classify(info, builder?.Info.BaseCrawlCostThreshold ?? 1000, builder?.Info.FragileTypes, CategoryOf);
		}

		FrontBackPick IBotFrontBackAdvisor.ChooseCell(FrontBackClass cls, ActorInfo building, CPos baseCenter,
			IReadOnlyList<CPos> candidates)
		{
			if (candidates.Count == 0)
				return FrontBackPick.None;

			Refresh();
			var front = FrontForCell(baseCenter);
			switch (cls)
			{
				case FrontBackClass.Radar:
					return RadarPick(building, front, candidates);
				case FrontBackClass.Production:
					return front == null ? FrontBackPick.None
						: ChooseProductionCell(candidates, front.Centre, front,
							front.HasLine ? front.FrontProj : Math.Max(0, front.ArcEdgeProj), Info.ProductionSetbackCells);
				case FrontBackClass.Valuable:
					return ChooseValuableCell(candidates, front?.Centre ?? baseCenter, lastFronts);
				default:
					return FrontBackPick.None;
			}
		}

		FrontBackPick RadarPick(ActorInfo building, BaseFront front, IReadOnlyList<CPos> candidates)
		{
			// The front this radar serves: a defended front still lacking a provider (nearest the caller's base
			// first), else a wide-approach front earning its extra, else the caller's own front.
			var reference = front?.Centre ?? new CPos();
			var target = lastFronts
				.Where(f => f.HasLine && frontRadarCount.GetValueOrDefault(f) == 0)
				.OrderBy(f => (f.Centre - reference).LengthSquared)
				.ThenBy(f => f.Id)
				.FirstOrDefault();
			target ??= lastFronts
				.Where(f => f.HasLine && frontRadarCount.GetValueOrDefault(f) < Info.RadarMaxPerFront
					&& frontUncoveredApproach.GetValueOrDefault(f) >= Info.RadarMinNewCoverageCells)
				.OrderByDescending(f => frontUncoveredApproach[f])
				.ThenBy(f => f.Id)
				.FirstOrDefault();
			target ??= front;

			if (target == null || !target.HasLine)
			{
				// No defence line to hide behind: the radar waits for the line, then falls back to a SAFE
				// back cell — it never goes forward (maintainer ruling d). All-forward candidates = Hold (B3).
				if (target == null || world.WorldTick - target.FirstSeenTick < Info.RadarWaitForDefenceTicks)
					return new FrontBackPick(null, true, target?.Id ?? -1, 0, 0, 0, 0);

				var noLineSafe = SafeBackCells(candidates, target.Centre, target);
				return noLineSafe.Count == 0 ? new FrontBackPick(null, true, target.Id, 0, 0, 0, 0)
					: ChooseValuableCell(noLineSafe, target.Centre, lastFronts);
			}

			var pick = ChooseRadarCell(candidates, target.Centre, target, lastUnionApproach, lastUnionCovered,
				RadarRangeCells(building), Info.RadarMinSetbackCells, Info.RadarMaxSetbackCells);
			var inside = frontRadarCount.GetValueOrDefault(target);
			var required = RequiredNewCoverage(inside, Info.RadarMinNewCoverageCells);
			if (pick.Cell != null && pick.NewCoverageCells >= required)
				return pick;

			// An EXTRA that cannot add the required new coverage holds — it is optional and earns nothing
			// anywhere else; a pick with no legal band cell stays optional the same way (B4).
			if (inside > 0)
				return new FrontBackPick(null, true, target.Id, 0, 0, 0, 0);

			// A first provider with no legal band cell, or one that cannot positively reach the approach:
			// same wait-then-back rule as a missing line — and only SAFE back cells, never forward (B3/B4).
			if (world.WorldTick - target.FirstSeenTick < Info.RadarWaitForDefenceTicks)
				return new FrontBackPick(null, true, target.Id, 0, 0, 0, 0);

			var safe = SafeBackCells(candidates, target.Centre, target);
			return safe.Count == 0 ? new FrontBackPick(null, true, target.Id, 0, 0, 0, 0)
				: ChooseValuableCell(safe, target.Centre, lastFronts);
		}

		int IBotFrontBackAdvisor.WantedRadarProviders
		{
			get
			{
				Refresh();
				return lastWantedRadars;
			}
		}

		int IBotFrontBackAdvisor.RadarPowerMargin
		{
			get
			{
				Refresh();
				return Info.RadarPowerMarginPerProvider * Math.Max(lastWantedRadars, lastProviders.Count);
			}
		}

		ActorInfo IBotFrontBackAdvisor.PreferredRadarProvider(IReadOnlyList<ActorInfo> candidates)
		{
			var ownedQueues = new HashSet<string>(StringComparer.Ordinal);
			foreach (var b in OwnBuildingsOrdered())
				foreach (var p in b.Info.TraitInfos<ProductionInfo>())
					if (!p.Produces.IsDefaultOrEmpty)
						ownedQueues.UnionWith(p.Produces);

			return ChooseRadarProvider(candidates, ownedQueues);
		}

		int IBotFrontBackAdvisor.FrontCount
		{
			get
			{
				Refresh();
				return lastFronts.Count;
			}
		}

		int IBotFrontBackAdvisor.FrontsWithoutRadar
		{
			get
			{
				Refresh();
				return lastFrontsWithoutRadar;
			}
		}

		int IBotFrontBackAdvisor.RadarUnionCells
		{
			get
			{
				Refresh();
				return lastUnionCovered.Count;
			}
		}

		int IBotFrontBackAdvisor.RadarApproachCells
		{
			get
			{
				Refresh();
				return lastUnionApproach.Count;
			}
		}
	}
}
