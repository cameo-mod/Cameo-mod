#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// The `expansion` object of a situation snapshot (AI_ARCHITECTURE 12.24 FE-0). Record-only: nothing in the bot reads it.
	/// Distances are in cells. Angles are degrees, -1 when not defined.
	/// </summary>
	internal sealed class ExpansionSnapshot
	{
		public int FieldsKnown, FieldsInReach, FieldsServed, FieldsHarvested;
		public int AnchorsSpreader, AnchorsField;
		public int Refineries, ExcessRefineries, UnassignedRefineries;
		public double AnchorDistMean, AnchorDistMax;
		public int Conyards, Outposts;
		public string CrawlTarget = "", McvSite = "";
		public int CrawlMcvAngle = -1;
		public int CoverageMilli;

		// REF-1 (§12.24 v2): the most refineries serving one anchor (the law's cap: must stay 1), the anchors in reach
		// still waiting for a refinery, and the fields in reach with no refinery yet (the tier-1 backlog — it must
		// drain to 0 before any second-refinery-on-a-field placement). The ids list is that backlog spelled out.
		public int RefineriesPerAnchorMax, AnchorsInReachUnserved, FieldsInReachUnserved;
		public int[] FieldsInReachUnservedIds = Array.Empty<int>();
	}

	internal readonly struct RefineryAssignment
	{
		public readonly int Excess;
		public readonly int Unassigned;
		public readonly double MeanDistance;
		public readonly double MaxDistance;

		public RefineryAssignment(int excess, int unassigned, double mean, double max)
		{
			Excess = excess;
			Unassigned = unassigned;
			MeanDistance = mean;
			MaxDistance = max;
		}
	}

	/// <summary>Pure helpers of the field-economy telemetry, free of world state so they can be tested.</summary>
	internal static class ExpansionMath
	{
		/// <summary>A field within this many cells of a spreader belongs to that spreader; a refinery within it of an anchor serves it.</summary>
		public const int AnchorRadiusCells = 12;

		/// <summary>Bearing in degrees, clockwise from north (map up = -y), 0..359.</summary>
		public static int BearingDegrees(CPos from, CPos to)
		{
			var dx = to.X - from.X;
			var dy = to.Y - from.Y;
			if (dx == 0 && dy == 0)
				return 0;

			var degrees = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
			return ((int)Math.Round(degrees) % 360 + 360) % 360;
		}

		/// <summary>The smaller angle between two bearings, 0..180.</summary>
		public static int AngleBetween(int bearingA, int bearingB)
		{
			var d = Math.Abs(bearingA - bearingB) % 360;
			return d > 180 ? 360 - d : d;
		}

		public static double Distance(CPos a, CPos b)
		{
			return Math.Sqrt((a - b).LengthSquared);
		}

		/// <summary>The fields (centres) with no spreader within <paramref name="radius"/> cells: each is an anchor of its own.</summary>
		public static List<CPos> SpreaderlessFields(IEnumerable<CPos> fields, IReadOnlyList<CPos> spreaders, int radius)
		{
			var result = new List<CPos>();
			foreach (var f in fields)
			{
				var covered = false;
				foreach (var s in spreaders)
					if (Distance(f, s) <= radius)
					{
						covered = true;
						break;
					}

				if (!covered)
					result.Add(f);
			}

			return result;
		}

		/// <summary>Index and distance of the anchor nearest to <paramref name="cell"/>; (-1, 0) with no anchors.</summary>
		public static (int Index, double Distance) Nearest(CPos cell, IReadOnlyList<CPos> anchors)
		{
			var best = -1;
			var bestDistance = double.MaxValue;
			for (var i = 0; i < anchors.Count; i++)
			{
				var d = Distance(cell, anchors[i]);
				if (d < bestDistance)
				{
					bestDistance = d;
					best = i;
				}
			}

			return best < 0 ? (-1, 0) : (best, bestDistance);
		}

		/// <summary>
		/// Assign each refinery to its nearest anchor; it serves that anchor when within <paramref name="serveRadius"/>, else it is
		/// unassigned. Excess = for each anchor the refineries beyond the first, plus every unassigned refinery. The distance stats
		/// run over all refineries to their nearest anchor (assigned or not).
		/// </summary>
		public static RefineryAssignment AssignRefineries(IReadOnlyList<CPos> refineries, IReadOnlyList<CPos> anchors, int serveRadius)
		{
			if (refineries.Count == 0)
				return new RefineryAssignment(0, 0, 0, 0);

			var perAnchor = new Dictionary<int, int>();
			var unassigned = 0;
			double sum = 0, max = 0;
			var measured = 0;
			foreach (var r in refineries)
			{
				var (index, distance) = Nearest(r, anchors);
				if (index < 0)
				{
					unassigned++;
					continue;
				}

				sum += distance;
				max = Math.Max(max, distance);
				measured++;
				if (distance <= serveRadius)
					perAnchor[index] = perAnchor.TryGetValue(index, out var n) ? n + 1 : 1;
				else
					unassigned++;
			}

			var excess = unassigned + perAnchor.Values.Sum(n => n - 1);
			return new RefineryAssignment(excess, unassigned, measured == 0 ? 0 : sum / measured, max);
		}

		/// <summary>
		/// REF-1 (§12.24 v2): the gap between a placed footprint and a field's resource cells — 0 when a footprint cell
		/// sits on or 8-adjacent to a resource cell, 1 when exactly one empty cell separates them, 2 when farther.
		/// -1 when either side has no cells (a lone spreader has no known field).
		/// </summary>
		public static int ResourceGap(IReadOnlyCollection<CPos> footprintCells, IReadOnlyCollection<CPos> fieldCells)
		{
			if (footprintCells == null || footprintCells.Count == 0 || fieldCells == null || fieldCells.Count == 0)
				return -1;

			var best = int.MaxValue;
			foreach (var fp in footprintCells)
			{
				foreach (var c in fieldCells)
				{
					var chebyshev = Math.Max(Math.Abs(fp.X - c.X), Math.Abs(fp.Y - c.Y));
					if (chebyshev < best)
						best = chebyshev;
					if (best <= 1)
						return 0;
				}
			}

			return Math.Max(0, best - 1);
		}

		/// <summary>Rules-derived building category of the placement log; the first match in this order wins.</summary>
		public static string Category(IReadOnlySet<string> tags, IReadOnlySet<string> roles)
		{
			bool Has(string t) => tags != null && tags.Contains(t);
			if (Has(BotTargetTags.Conyard))
				return "conyard";
			if (Has(BotTargetTags.Refinery))
				return "refinery";
			if (Has(BotTargetTags.Superweapon))
				return "superweapon";
			if (Has(BotTargetTags.Power))
				return "power";
			if (Has(BotTargetTags.Defence))
				return "defence";
			if (Has(BotTargetTags.Production))
				return "production";
			if (roles != null && roles.Contains(BotUnitRole.Tech))
				return "tech";
			if (roles != null && roles.Contains(BotUnitRole.Support))
				return "support";
			return "other";
		}
	}

	/// <summary>
	/// Per-player collector of the field-economy telemetry (12.24 FE-0). Observer only: it reads the resource map the bot already keeps
	/// (no second field model; this only remembers each field's first-seen centre like the planner does), the planner's published
	/// target and MCV site, and the map's spreaders (public map data). It never writes to any module.
	/// </summary>
	internal sealed class ExpansionTelemetry
	{
		const int MainBaseRadiusCells = 20;
		const int SpreaderRescanTicks = 1500;

		readonly Dictionary<int, CPos> fieldCenters = new();
		readonly List<CPos> spreaders = new();
		readonly List<CPos> anchors = new();
		int spreaderCount;
		int nextSpreaderScan;
		CPos? mainConyard;

		// REF-1 (§12.24 v2): the field model — the planner's own when one is mounted (the logged field ids are then
		// exactly the ones the law claimed with), else a locally built component model frozen at the first scan.
		ExpansionPlannerBotModule planner;
		List<RefineryField> ownFields;
		int[] ownAnchorFieldIds = Array.Empty<int>();
		IReadOnlyList<IReadOnlyCollection<CPos>> ownFieldCellsById = Array.Empty<IReadOnlyCollection<CPos>>();

		/// <summary>Anchors: the spreaders first, then the spreaderless fields' centres.</summary>
		public IReadOnlyList<CPos> Anchors => ActiveAnchors;

		IReadOnlyList<CPos> ActiveAnchors => planner?.ComponentFields != null ? planner.AnchorCells : anchors;
		IReadOnlyList<int> ActiveAnchorFieldIds => planner?.ComponentFields != null ? planner.AnchorFieldIds : ownAnchorFieldIds;
		IReadOnlyList<IReadOnlyCollection<CPos>> ActiveFieldCellsById => planner?.ComponentFields != null ? planner.FieldCellsById : ownFieldCellsById;
		int ActiveSpreaderCount => planner?.ComponentFields != null ? planner.SpreaderAnchorCount : spreaderCount;

		void Refresh(OpenRA.Player player, int tick)
		{
			var map = player.PlayerActor.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());
			if (map != null)
				for (var i = 0; i < map.GetIndicesLength(); i++)
				{
					var field = map.GetIndice(i);
					if (field != null && field.ResourceCellsCount > 0 && !fieldCenters.ContainsKey(i))
						fieldCenters[i] = field.ResourceCellsCenter;
				}

			planner = player.PlayerActor.TraitsImplementing<ExpansionPlannerBotModule>().FirstEnabledTraitOrDefault();

			if (tick >= nextSpreaderScan)
			{
				nextSpreaderScan = tick + SpreaderRescanTicks;
				spreaders.Clear();
				foreach (var a in player.World.ActorsWithTrait<ISeedableResource>())
					if (!a.Actor.IsDead && a.Actor.IsInWorld)
						spreaders.Add(a.Actor.Location);
			}

			if (planner?.ComponentFields != null)
				return;

			// No planner model (classic, or a planner that has not scanned yet): the local component model — the
			// map's initial valuable cells grouped into 8-connected fields, frozen at first scan.
			if (ownFields == null)
			{
				ownFields = new List<RefineryField>();
				var layer = player.World.WorldActor.TraitOrDefault<IResourceLayer>();
				var valuable = map?.Info.ValuableResourceTypes;
				if (layer != null && valuable != null)
				{
					var cells = new List<CPos>();
					foreach (var cell in player.World.Map.AllCells)
						if (valuable.Contains(layer.GetResource(cell).Type))
							cells.Add(cell);

					ownFields = ExpansionPlannerBotModule.ResourceFields(cells)
						.Select((component, i) => new RefineryField(i, component)).ToList();
				}
			}

			anchors.Clear();
			anchors.AddRange(spreaders);
			spreaderCount = spreaders.Count;
			anchors.AddRange(ExpansionMath.SpreaderlessFields(ownFields.Select(f => f.Center), spreaders, ExpansionMath.AnchorRadiusCells));

			ownAnchorFieldIds = ExpansionPlannerBotModule.AssignAnchorFields(anchors, spreaderCount,
				ownFields.Select(f => (IReadOnlyCollection<CPos>)f.Cells).ToList(), ownFields.Select(f => f.Center).ToList(),
				ExpansionMath.AnchorRadiusCells);
			var maxField = ownAnchorFieldIds.Length == 0 ? -1 : ownAnchorFieldIds.Max();
			var byId = new List<IReadOnlyCollection<CPos>>();
			for (var f = 0; f <= maxField; f++)
				byId.Add(f < ownFields.Count ? ownFields[f].Cells : (IReadOnlyCollection<CPos>)Array.Empty<CPos>());
			ownFieldCellsById = byId;
		}

		/// <summary>The anchor nearest to a placed building, for the placement log. Index -1 when the map knows none.</summary>
		public (int Index, CPos Anchor, bool Spreader, double Distance) NearestAnchor(OpenRA.Player player, CPos cell)
		{
			Refresh(player, player.World.WorldTick);
			var active = ActiveAnchors;
			var (index, distance) = ExpansionMath.Nearest(cell, active);
			return index < 0 ? (-1, CPos.Zero, false, 0) : (index, active[index], index < ActiveSpreaderCount, distance);
		}

		/// <summary>
		/// REF-1 (§12.24 v2): the placement record's field context — the field id of the anchor nearest the placed
		/// cell, the tier it implied (2 when another own refinery already serves that field, else 1), and the gap
		/// between the footprint and the field's resource cells. (-1, 0, -1) when nothing is known.
		/// </summary>
		public (int FieldId, int Tier, int Gap) PlacementFieldContext(OpenRA.Player player, CPos cell, IReadOnlyCollection<CPos> footprint)
		{
			Refresh(player, player.World.WorldTick);
			var active = ActiveAnchors;
			var fieldIds = ActiveAnchorFieldIds;
			var (index, _) = ExpansionMath.Nearest(cell, active);
			if (index < 0 || index >= fieldIds.Count)
				return (-1, 0, -1);

			var fieldId = fieldIds[index];
			var cells = fieldId < ActiveFieldCellsById.Count ? ActiveFieldCellsById[fieldId] : null;
			var gap = ExpansionMath.ResourceGap(footprint, cells);

			// A field another own refinery serves is tier 2 — the same binding the law uses (proximity or a
			// footprint flush to the field's cells), so the logged tier matches the claim the law ran with.
			var builder = player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().FirstOrDefault(t => t.IsTraitEnabled());
			var ownRefineries = builder?.RefineryBuildings.Actors.Where(a => !a.IsDead).ToList() ?? new List<Actor>();
			var refineryCells = ownRefineries.Select(a => a.Location).ToList();
			var refineryTiles = ownRefineries
				.Select(a => (IReadOnlyCollection<CPos>)a.Info.TraitInfos<BuildingInfo>().FirstOrDefault()?.Tiles(a.Location).ToList())
				.ToList();
			var refineryFields = ExpansionPlannerBotModule.RefineryFlushFields(refineryTiles, ActiveFieldCellsById);
			var assigned = ExpansionPlannerBotModule.AssignRefineries(active, refineryCells, ExpansionMath.AnchorRadiusCells,
				fieldIds, refineryFields);
			var served = Enumerable.Range(0, Math.Min(active.Count, fieldIds.Count))
				.Any(i => fieldIds[i] == fieldId && assigned[i] >= 0);
			return (fieldId, served ? 2 : 1, gap);
		}

		public ExpansionSnapshot Capture(OpenRA.Player player, IReadOnlyCollection<Actor> ownBuildings)
		{
			var world = player.World;
			Refresh(player, world.WorldTick);

			var map = player.PlayerActor.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());
			var planner = player.PlayerActor.TraitsImplementing<ExpansionPlannerBotModule>().FirstEnabledTraitOrDefault();
			var reach = planner?.Info.ReachCells ?? 6;

			var buildableArea = new List<CPos>();
			var refineries = new List<CPos>();
			var refineryTiles = new List<IReadOnlyCollection<CPos>>();
			var conyards = new List<CPos>();
			foreach (var b in ownBuildings)
			{
				if (b.Info.HasTraitInfo<GivesBuildableAreaInfo>())
				{
					// The buildable frontier is the building's footprint tiles (BuildingInfluence registers tiles
					// and IsCloseEnoughToBase measures against them) — not the single top-left cell.
					if (b.Info.TraitInfos<BuildingInfo>().FirstOrDefault() is { } bbi)
						buildableArea.AddRange(bbi.Tiles(b.Location));
					else
						buildableArea.Add(b.Location);
				}

				if (b.Info.HasTraitInfo<RefineryInfo>())
				{
					refineries.Add(b.Location);
					refineryTiles.Add(b.Info.TraitInfos<BuildingInfo>().FirstOrDefault()?.Tiles(b.Location).ToList());
				}

				if (b.Info.HasTraitInfo<BaseBuildingInfo>())
					conyards.Add(b.Location);
			}

			var snapshot = new ExpansionSnapshot
			{
				FieldsKnown = fieldCenters.Count,
				AnchorsSpreader = ActiveSpreaderCount,
				AnchorsField = ActiveAnchors.Count - ActiveSpreaderCount,
				Refineries = refineries.Count,
				Conyards = conyards.Count
			};

			// FieldsHarvested stays on the bot's seen-fields model (ResourceMapBotModule) — it is the fog-limited
			// half of the report; reach/served below run on the law's own field ids so the three counters agree.
			foreach (var kv in fieldCenters)
				if (map != null && map.GetIndice(kv.Key)?.PlayerHarvetserCount > 0)
					snapshot.FieldsHarvested++;

			var activeAnchors = ActiveAnchors;
			var assignment = ExpansionMath.AssignRefineries(refineries, activeAnchors, ExpansionMath.AnchorRadiusCells);
			snapshot.ExcessRefineries = assignment.Excess;
			snapshot.UnassignedRefineries = assignment.Unassigned;
			snapshot.AnchorDistMean = Math.Round(assignment.MeanDistance, 1);
			snapshot.AnchorDistMax = Math.Round(assignment.MaxDistance, 1);

			// REF-1 (§12.24 v2): the per-anchor refinery max (the law's cap — 1), the anchors in reach with no
			// refinery, and the fields in reach with no refinery at all (the tier-1 backlog the claim order drains
			// before any field gets a second one). Serving mirrors the law's own binding: the greedy anchor
			// assignment where a refinery qualifies by proximity OR by sitting flush to the anchor's field —
			// a legal far-edge placement must bind, or the anchor would be claimed twice.
			var activeFieldIds = ActiveAnchorFieldIds;
			var fieldCellsById = ActiveFieldCellsById;
			var refineryFields = ExpansionPlannerBotModule.RefineryFlushFields(refineryTiles, fieldCellsById);
			var assigned = ExpansionPlannerBotModule.AssignRefineries(activeAnchors, refineries,
				ExpansionMath.AnchorRadiusCells, activeFieldIds, refineryFields);

			var bound = new bool[refineries.Count];
			foreach (var r in assigned)
				if (r >= 0)
					bound[r] = true;

			var servedPerAnchor = new int[activeAnchors.Count];
			for (var a = 0; a < activeAnchors.Count; a++)
				if (assigned[a] >= 0)
					servedPerAnchor[a] = 1;

			// Every unbound refinery still counts toward its nearest eligible anchor — a stack on one spreader
			// is the violation this metric exists to show.
			for (var r = 0; r < refineries.Count; r++)
			{
				if (bound[r])
					continue;

				var best = -1;
				var bestD = double.MaxValue;
				for (var a = 0; a < activeAnchors.Count; a++)
				{
					var eligible = r < refineryFields.Length && a < activeFieldIds.Count
						&& refineryFields[r] >= 0 && refineryFields[r] == activeFieldIds[a];
					var d = ExpansionMath.Distance(refineries[r], activeAnchors[a]);
					if ((eligible || d <= ExpansionMath.AnchorRadiusCells) && d < bestD)
					{
						bestD = d;
						best = a;
					}
				}

				if (best >= 0)
					servedPerAnchor[best]++;
			}

			snapshot.RefineriesPerAnchorMax = servedPerAnchor.Length == 0 ? 0 : servedPerAnchor.Max();

			// Reach mirrors the law too: the refinery sits flush to the field's resource EDGE, so a field is in
			// reach when one of its cells is — not when the spreader cell itself is (an orphan anchor with no
			// recorded field cells keeps its own cell as the measure).
			var fieldReach = new Dictionary<int, double>();
			for (var f = 0; f < fieldCellsById.Count; f++)
			{
				var cells = fieldCellsById[f];
				if (cells == null || cells.Count == 0)
					continue;

				var d = double.MaxValue;
				foreach (var c in cells)
					foreach (var b in buildableArea)
						d = Math.Min(d, ExpansionMath.Distance(c, b));
				fieldReach[f] = d;
			}

			var fieldServed = new Dictionary<int, bool>();
			var fieldInReach = new Dictionary<int, bool>();
			for (var a = 0; a < activeAnchors.Count; a++)
			{
				if (a >= activeFieldIds.Count)
					break;

				var fid = activeFieldIds[a];
				var reachD = fid < fieldCellsById.Count && fieldCellsById[fid] is { Count: > 0 }
					? fieldReach.GetValueOrDefault(fid, double.MaxValue)
					: buildableArea.Count == 0 ? double.MaxValue : buildableArea.Min(b => ExpansionMath.Distance(b, activeAnchors[a]));
				var inReach = reachD <= reach;
				var servedAnchor = assigned[a] >= 0;
				if (inReach && !servedAnchor)
					snapshot.AnchorsInReachUnserved++;

				fieldServed[fid] = fieldServed.GetValueOrDefault(fid) || servedAnchor;
				fieldInReach[fid] = fieldInReach.GetValueOrDefault(fid) || inReach;
			}

			// All three field counters run on the law's own field ids and reach model: in reach = the field's
			// resource edge within `reach` of a buildable-area tile (or an orphan anchor's own cell); served =
			// a refinery bound to one of its anchors (proximity or flush); unserved = in reach and not served.
			var covered = 0;
			var unservedIds = new List<int>();
			foreach (var fid in fieldInReach.Keys)
			{
				var inReach = fieldInReach[fid];
				var servedField = fieldServed.GetValueOrDefault(fid);
				if (inReach)
					snapshot.FieldsInReach++;
				if (servedField)
					snapshot.FieldsServed++;
				if (inReach || servedField)
					covered++;
				if (inReach && !servedField)
				{
					snapshot.FieldsInReachUnserved++;
					unservedIds.Add(fid);
				}
			}

			unservedIds.Sort();
			snapshot.FieldsInReachUnservedIds = unservedIds.ToArray();
			snapshot.CoverageMilli = fieldInReach.Count == 0 ? 0 : covered * 1000 / fieldInReach.Count;

			// The main base: the first construction yard seen (kept for the match); outposts are the yards beyond MainBaseRadiusCells of it.
			if (mainConyard == null && conyards.Count > 0)
				mainConyard = conyards[0];
			CPos? main = mainConyard ?? (ownBuildings.Count > 0 ? ownBuildings.First().Location : null);
			if (main.HasValue)
			{
				snapshot.Outposts = conyards.Count(c => ExpansionMath.Distance(c, main.Value) > MainBaseRadiusCells);
				var near = ownBuildings.Where(b => ExpansionMath.Distance(b.Location, main.Value) <= MainBaseRadiusCells)
					.Select(b => b.Location).ToList();
				var centroid = near.Count == 0 ? main.Value
					: new CPos((int)near.Average(c => c.X), (int)near.Average(c => c.Y));
				var crawl = planner?.Target?.Center;
				var mcv = planner?.LastMcvSite;
				snapshot.CrawlTarget = crawl.HasValue ? $"{crawl.Value.X},{crawl.Value.Y}" : "";
				snapshot.McvSite = mcv.HasValue ? $"{mcv.Value.X},{mcv.Value.Y}" : "";
				if (crawl.HasValue && mcv.HasValue)
					snapshot.CrawlMcvAngle = ExpansionMath.AngleBetween(
						ExpansionMath.BearingDegrees(centroid, crawl.Value), ExpansionMath.BearingDegrees(centroid, mcv.Value));
			}

			return snapshot;
		}
	}
}
