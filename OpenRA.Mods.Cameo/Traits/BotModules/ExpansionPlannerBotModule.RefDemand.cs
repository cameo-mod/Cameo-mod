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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// REF-DEMAND-01 (Opus ruling + user final spec 2026-11-25, row ref_demand): the refinery law's demand. With the switch on the anchors are
	/// resource zones (RefineryDemand.Zones over every field's cells and spreader spots, built once), and a zone is in the law's in-reach set while it
	/// lies within YardRadiusCells of ANY own construction yard (each yard counts its own radius, expansion yards included), a harvester can reach it
	/// from such a yard (the hierarchical pathfinder's domain check, cached per (zone, yard) and refreshed when the yard set changes or every
	/// ReachRefreshTicks — no path search), no allied refinery serves it and fewer than RefineryPerZoneCap friendly refineries stand within
	/// RefinerySpacingCells of its anchor. Placement keeps RefinerySpacingCells from every friendly refinery (RefinerySpacingFallbackCells when a
	/// zone's search finds no site). Integer math only; friendly refineries in actor order; nothing iterates a hash container into a decision.
	/// </summary>
	public partial class ExpansionPlannerBotModule
	{
		AblationLatch ablateRefDemand;

		// The zone model (built once per field model): the anchor cell, field id (= zone index) and cell set of every zone.
		List<CPos> zoneAnchors;
		int[] zoneFieldIds = Array.Empty<int>();
		List<IReadOnlyCollection<CPos>> zoneCellsById = new();
		bool zonesActive;

		// The yard cache: per (zone, yard) harvester connectivity; per zone the in-radius flag, the reachable flag and the squared yard distance.
		readonly Dictionary<(int Zone, CPos Yard), bool> zoneReach = new();
		List<CPos> zoneReachYards = new();
		int zoneReachTick = -1;
		bool[] zoneInRadius = Array.Empty<bool>();
		bool[] zoneReachable = Array.Empty<bool>();
		long[] zoneYardDistSq = Array.Empty<long>();

		// Friendly (own and allied) refinery cells, actor order, once per tick.
		readonly List<CPos> friendlyRefineryCells = new();
		readonly List<bool> friendlyRefineryOwn = new();
		int friendlyRefineryTick = -1;

		// The per-anchor verdicts and friendly counts, once per tick (and per anchor list).
		RefineryDemand.Verdict[] demandVerdicts = Array.Empty<RefineryDemand.Verdict>();
		int[] demandCounts = Array.Empty<int>();
		int demandVerdictTick = -1;
		List<CPos> demandVerdictAnchors;
		Func<int, RefineryDemand.Verdict> demandVerdictFunc;

		// The friendly count at an anchor when its pending commit was made: the commit has landed once the count rose.
		readonly Dictionary<CPos, int> zonePendingBase = new();

		bool RefDemandOn => Info.RefDemand && !IsTraitDisabled && Info.FieldCoverage && Info.DriveRefineries
			&& !ablateRefDemand.Off(player, AblationIds.RefDemand);

		bool RefDemandArmed => Info.RefDemand && zonesActive && LawActive;

		bool RefDemandZones => RefDemandArmed;

		/// <summary>The demand verdict per anchor for ClaimOrder; null = the old ReachCells and served rule.</summary>
		Func<int, RefineryDemand.Verdict> DemandOverride => RefDemandArmed ? (demandVerdictFunc ??= DemandVerdict) : null;

		/// <summary>A claim's field centre: the zone anchor with the switch on, else the field model's centre as before.</summary>
		CPos ClaimFieldCenter(int fieldId, CPos anchor) => zonesActive ? anchor : fieldId < fields.Count ? fields[fieldId].Center : anchor;

		bool IBotExpansionTargetProvider.RefinerySpacingActive => RefDemandArmed && Info.RefinerySpacingCells > 0;

		int IBotExpansionTargetProvider.RefinerySiteSpacingTier(CPos topLeft) => RefDemandArmed ? SpacingTierAt(topLeft) : RefineryDemand.SpacingAllowed;

		int SpacingTierAt(CPos topLeft) =>
			RefineryDemand.SpacingTier(topLeft, FriendlyRefineryCells(), Info.RefinerySpacingCells, Info.RefinerySpacingFallbackCells);

		/// <summary>
		/// Builds the zones once: every field's cells plus the spreaders that belong to it (the FE-1 field assignment), then the spreaders of no
		/// field. Field id order, then zone order. True when at least one zone exists.
		/// </summary>
		bool EnsureZoneModel(List<CPos> spreaders)
		{
			if (zoneAnchors != null)
				return zoneAnchors.Count > 0;

			var distinct = spreaders.Distinct().ToList();
			var fieldCells = fields.Select(f => (IReadOnlyCollection<CPos>)f.Cells).ToList();
			var spreaderFields = AssignAnchorFields(distinct, distinct.Count, fieldCells, fields.Select(f => f.Center).ToList(), Info.SpreaderFieldRadiusCells);
			var zones = new List<List<CPos>>();
			var points = 0;
			for (var f = 0; f < fields.Count; f++)
			{
				var set = new List<CPos>(fields[f].Cells);
				for (var s = 0; s < distinct.Count; s++)
					if (spreaderFields[s] == f)
						set.Add(distinct[s]);

				points += set.Count;
				zones.AddRange(RefineryDemand.Zones(set, Info.RefineryZoneRadius));
			}

			var orphans = new List<CPos>();
			for (var s = 0; s < distinct.Count; s++)
				if (spreaderFields[s] >= fields.Count)
					orphans.Add(distinct[s]);

			points += orphans.Count;
			zones.AddRange(RefineryDemand.Zones(orphans, Info.RefineryZoneRadius));

			zoneAnchors = new List<CPos>(zones.Count);
			zoneFieldIds = new int[zones.Count];
			zoneCellsById = new List<IReadOnlyCollection<CPos>>(zones.Count);
			for (var z = 0; z < zones.Count; z++)
			{
				zoneAnchors.Add(RefineryDemand.ZoneAnchor(zones[z]));
				zoneFieldIds[z] = z;
				zoneCellsById.Add(zones[z]);
			}

			ZonesBuiltObserved(zones.Count, fields.Count, points);
			return zoneAnchors.Count > 0;
		}

		/// <summary>Own and allied refinery cells, rebuilt at most once per tick in actor order (fog: allies share their vision).</summary>
		List<CPos> FriendlyRefineryCells()
		{
			var tick = world.WorldTick;
			if (friendlyRefineryTick == tick)
				return friendlyRefineryCells;

			friendlyRefineryTick = tick;
			friendlyRefineryCells.Clear();
			friendlyRefineryOwn.Clear();
			CollectFriendlyRefineries(friendlyRefineryCells, friendlyRefineryOwn);
			return friendlyRefineryCells;
		}

		// Fog honesty: only own and allied refineries (allies share their vision); the site is manifested in tools/audit/fog_honesty_manifest.json.
		void CollectFriendlyRefineries(List<CPos> cells, List<bool> own)
		{
			foreach (var a in world.ActorsHavingTrait<Refinery>())
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null)
					continue;

				var isOwn = a.Owner == player;
				if (!isOwn && !player.IsAlliedWith(a.Owner))
					continue;

				cells.Add(a.Location);
				own?.Add(isOwn);
			}
		}

		/// <summary>The refinery cap of zone <paramref name="z"/>: 1 for a small zone (RefinerySecondPerZoneMinCells), else RefineryPerZoneCap.</summary>
		int ZoneCapAt(int z) =>
			RefineryDemand.ZoneCapFor(Info.RefineryPerZoneCap, z >= 0 && z < zoneCellsById.Count ? zoneCellsById[z].Count : 0, Info.RefinerySecondPerZoneMinCells);

		RefineryDemand.Verdict DemandVerdict(int a)
		{
			var tick = world.WorldTick;
			if (demandVerdictTick != tick || !ReferenceEquals(demandVerdictAnchors, anchors) || demandVerdicts.Length != anchors.Count)
				RefreshDemandVerdicts(tick);

			return a < demandVerdicts.Length ? demandVerdicts[a] : RefineryDemand.Verdict.OutOfRadius;
		}

		void RefreshDemandVerdicts(int tick)
		{
			demandVerdictTick = tick;
			demandVerdictAnchors = anchors;
			RefreshZoneReach(tick);
			var friendly = FriendlyRefineryCells();
			var serve = Info.AnchorServeRadiusCells > 0 ? Info.AnchorServeRadiusCells : Info.ClaimRadiusCells;
			if (demandVerdicts.Length != anchors.Count)
			{
				demandVerdicts = new RefineryDemand.Verdict[anchors.Count];
				demandCounts = new int[anchors.Count];
			}

			for (var a = 0; a < anchors.Count; a++)
			{
				var allied = false;
				var count = 0;
				for (var r = 0; r < friendly.Count; r++)
				{
					var d2 = RefineryDemand.DistanceSq(friendly[r], anchors[a]);
					if (!friendlyRefineryOwn[r] && RefineryDemand.Within(d2, serve))
						allied = true;

					if (RefineryDemand.Covered(d2, Info.RefinerySpacingCells))
						count++;
				}

				var z = a < anchorFieldIds.Length ? anchorFieldIds[a] : -1;
				var inRadius = z >= 0 && z < zoneInRadius.Length && zoneInRadius[z];
				var reachable = z >= 0 && z < zoneReachable.Length && zoneReachable[z];
				demandCounts[a] = count;
				demandVerdicts[a] = RefineryDemand.Classify(inRadius, reachable, allied, count, ZoneCapAt(z));
			}
		}

		/// <summary>
		/// The yard cache: due when the own yard set changed (a yard deployed, died or moved) or ReachRefreshTicks passed. An interval refresh
		/// forgets every (zone, yard) connectivity verdict; a yard change keeps the verdicts of the yards that stay. Each yard counts its own radius.
		/// </summary>
		void RefreshZoneReach(int tick)
		{
			var yards = ownYardCells;
			var changed = !yards.SequenceEqual(zoneReachYards) || zoneInRadius.Length != zoneCellsById.Count;
			if (!RefineryDemand.ReachRefreshDue(tick, zoneReachTick, Info.ReachRefreshTicks, changed))
				return;

			var interval = zoneReachTick < 0 || tick - zoneReachTick >= Math.Max(1, Info.ReachRefreshTicks);
			if (interval)
			{
				zoneReach.Clear();
				zoneReachTick = tick;
			}
			else
			{
				// Drop the verdicts of yards that are gone; the kept ones stay valid until the interval refresh.
				foreach (var key in zoneReach.Keys.Where(k => !yards.Contains(k.Yard)).ToList())
					zoneReach.Remove(key);
			}

			zoneReachYards = new List<CPos>(yards);
			var n = zoneCellsById.Count;
			zoneInRadius = new bool[n];
			zoneReachable = new bool[n];
			zoneYardDistSq = new long[n];
			for (var z = 0; z < n; z++)
			{
				var cells = zoneCellsById[z];
				var best = long.MaxValue;
				foreach (var yard in yards)
				{
					var d = long.MaxValue;
					foreach (var c in cells)
						d = Math.Min(d, RefineryDemand.DistanceSq(c, yard));

					best = Math.Min(best, d);
					if (!RefineryDemand.Within(d, Info.YardRadiusCells))
						continue;

					zoneInRadius[z] = true;
					if (zoneReachable[z])
						continue;

					if (!zoneReach.TryGetValue((z, yard), out var reach))
						zoneReach[(z, yard)] = reach = YardReachesZone(yard, cells);

					zoneReachable[z] = reach;
				}

				zoneYardDistSq[z] = best;
			}
		}

		/// <summary>
		/// The cheap connectivity query only (the abstract-domain lookup the MCV site check uses), for every harvester locomotor: the zone is reachable
		/// when ANY of its cells is in the same domain as the yard (the yard cell, else the ring three cells around it for a boxed-in top-left; a
		/// cell's neighbours count for a spreader point). No path search. No path finder or no harvester profile = NOT reachable.
		/// </summary>
		bool YardReachesZone(CPos yard, IReadOnlyCollection<CPos> cells)
		{
			var specs = HarvesterSpecs();
			if (pathFinder == null || specs.Length == 0)
				return false;

			foreach (var spec in specs)
			{
				var locomotor = spec.Locomotor;
				if (RefineryDemand.AnyCellReachable(cells, true, c => DomainReaches(locomotor, yard, c, true)))
					return true;

				for (var dy = -3; dy <= 3; dy++)
					for (var dx = -3; dx <= 3; dx++)
					{
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != 3)
							continue;

						var source = new CPos(yard.X + dx, yard.Y + dy);
						if (world.Map.Contains(source) && RefineryDemand.AnyCellReachable(cells, true, c => DomainReaches(locomotor, source, c, false)))
							return true;
					}
			}

			return false;
		}

		// One domain-index query from <paramref name="from"/> to the cell (and, when asked, to a cell next to it).
		bool DomainReaches(Locomotor locomotor, CPos from, CPos cell, bool neighbours)
		{
			if (pathFinder.PathMightExistForLocomotorBlockedByImmovable(locomotor, from, cell))
				return true;

			if (!neighbours)
				return false;

			for (var dy = -1; dy <= 1; dy++)
				for (var dx = -1; dx <= 1; dx++)
				{
					var next = new CPos(cell.X + dx, cell.Y + dy);
					if ((dx != 0 || dy != 0) && world.Map.Contains(next) && pathFinder.PathMightExistForLocomotorBlockedByImmovable(locomotor, from, next))
						return true;
				}

			return false;
		}

		/// <summary>The pending commit at this anchor counts the friendly refineries standing at it now.</summary>
		void ZonePendingRecord(CPos anchor)
		{
			if (!RefDemandArmed)
				return;

			zonePendingBase[anchor] = RefineryDemand.CountWithin(anchor, FriendlyRefineryCells(), Info.RefinerySpacingCells);
		}

		/// <summary>The committed refinery landed: more friendly refineries stand at the zone than at the commit (no record = the route-served test).</summary>
		bool ZonePendingLanded(int index, CPos anchor)
		{
			if (!zonePendingBase.TryGetValue(anchor, out var before))
				return lastAssigned[index] >= 0;

			return RefineryDemand.CountWithin(anchor, FriendlyRefineryCells(), Info.RefinerySpacingCells) > before;
		}

		// ---- DEV only: every call below is compiled out of a release build, arguments included. ----
		IBotRefineryDemandObserver[] demandObservers;

		IBotRefineryDemandObserver[] DemandObservers => demandObservers ??= world.WorldActor.TraitsImplementing<IBotRefineryDemandObserver>().ToArray();

		int ZoneOf(CPos anchor)
		{
			var i = anchors.IndexOf(anchor);
			return i >= 0 && i < anchorFieldIds.Length ? anchorFieldIds[i] : -1;
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void ZonesBuiltObserved(int zones, int fieldCount, int points)
		{
			foreach (var o in DemandObservers)
				o.RefineryZonesBuilt(player, world.WorldTick, zones, fieldCount, points, Info.RefineryZoneRadius);
		}

		// Reads only what the anchor update just computed this tick (no refresh of its own), so DEV and release take the same decisions.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void RefDemandObserved(int tick)
		{
			if (!RefDemandArmed || DemandObservers.Length == 0 || demandVerdictTick != tick || demandVerdicts.Length != anchors.Count)
				return;

			// The demand is the eligible zone count, the very counter gameplay uses (ClaimOrder's unserved-in-reach); the cap left is its own field.
			var inDemand = 0;
			var demand = RefineryDemand.EligibleZones(anchors.Count, i => demandVerdicts[i], CoverageUnknown);
			var capLeft = 0;
			var outOfRadius = 0;
			var unreachable = 0;
			var capped = 0;
			var allied = 0;
			for (var a = 0; a < anchors.Count; a++)
			{
				var v = demandVerdicts[a];
				var z = a < anchorFieldIds.Length ? anchorFieldIds[a] : -1;
				var cap = ZoneCapAt(z);
				var reason = RefineryDemand.Reason(v);
				var want = RefineryDemand.Eligible(v, CoverageUnknown(a)) ? 1 : 0;
				var left = RefineryDemand.CapRemaining(v, cap, demandCounts[a]);
				switch (v)
				{
					case RefineryDemand.Verdict.Demand:
						inDemand++;
						capLeft += left;
						if (FieldGateBlocks(a, tick))
							reason = "zone_gate";
						break;
					case RefineryDemand.Verdict.OutOfRadius: outOfRadius++; break;
					case RefineryDemand.Verdict.Unreachable: unreachable++; break;
					case RefineryDemand.Verdict.ZoneCap: capped++; break;
					default: allied++; break;
				}

				var d2 = z >= 0 && z < zoneYardDistSq.Length ? zoneYardDistSq[z] : long.MaxValue;
				var yardDistance = d2 == long.MaxValue ? -1 : Exts.ISqrt((int)Math.Min(int.MaxValue, d2));
				foreach (var o in DemandObservers)
					o.RefineryZoneVerdict(player, tick, z, anchors[a], Info.RefineryZoneRadius, reason, demandCounts[a], want, left, yardDistance);
			}

			var own = 0;
			for (var r = 0; r < friendlyRefineryOwn.Count; r++)
				if (friendlyRefineryOwn[r])
					own++;

			foreach (var o in DemandObservers)
				o.RefineryDemandSummary(player, tick, anchors.Count, inDemand, demand, capLeft, own, friendlyRefineryCells.Count, outOfRadius, unreachable, capped, allied);
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void AnchorParkedObserved(CPos anchor, string reason, int untilTick)
		{
			foreach (var o in DemandObservers)
				o.RefineryAnchorParked(player, world.WorldTick, ZoneOf(anchor), anchor, reason, untilTick);
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void AnchorPendingObserved(CPos anchor, int untilTick)
		{
			foreach (var o in DemandObservers)
				o.RefineryAnchorPending(player, world.WorldTick, ZoneOf(anchor), anchor, untilTick);
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void RefineryPlacedObserved(CPos? anchor, CPos site)
		{
			if (DemandObservers.Length == 0)
				return;

			// Its own friendly list: the per-tick cache stays exactly where the release build fills it.
			var friendly = new List<CPos>();
			CollectFriendlyRefineries(friendly, null);

			var nearest = RefineryDemand.NearestSq(site, friendly);
			var cells = nearest == long.MaxValue ? -1 : Exts.ISqrt((int)Math.Min(int.MaxValue, nearest));
			var fallback = RefDemandArmed
				&& RefineryDemand.SpacingTier(site, friendly, Info.RefinerySpacingCells, Info.RefinerySpacingFallbackCells) == RefineryDemand.SpacingFallbackOnly;
			foreach (var o in DemandObservers)
				o.RefineryPlaced(player, world.WorldTick, anchor.HasValue ? ZoneOf(anchor.Value) : -1, anchor ?? site, site, fallback, cells);
		}
	}
}
