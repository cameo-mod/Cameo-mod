#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 *
 * Ported from the Crystallized Nexus CNTacticalMapBotModule (crystallized-nexus
 * commit 30cf70a, GPLv3, Copyright (c) The Crystallized Nexus Developers).
 *
 * ZG-a scope: STATIC TOPOLOGY ONLY — passability snapshot, chokepoints
 * (passages / cliff ramps / bridges), the region cut gated by them, the
 * cell -> region-id lookup, and the shared Generation counter bumped on
 * bridge re-cuts. Territory claiming, region ownership, doors, sealable-wall
 * candidates, useful-chokepoint and high-ground refresh are all ZG-b/ZG-c
 * and are deliberately absent here.
 *
 * Adaptations: CNChokepoint/CNRegion/CNSealableCorridor -> ZoneChokepoint/Zone/
 * ZoneGateCorridor (see IBotZoneTopology.cs); MaxDoorWidth -> MaxGateWidth
 * (its only ZG-a use is bounding the corridor the region cut resolves to);
 * CNBotLog -> CAAIUtils.BotDebug; the CNDestroyableCliff bridge-watch is
 * dropped — Cameo has no destroyable-cliff trait, so only bridge cells are
 * watched for passability changes. No CN engine patches required: the Cameo
 * engine already exposes PathFinder.GetOverlayDataForLocomotor returning the
 * same (abstract graph, abstract domains) pair the donor consumes.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using CAAIUtils = OpenRA.Mods.CA.AIUtils;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// The terrain-only topology (chokepoints + connectivity), shared by all bots on the same map+locomotor so the
	// expensive scan runs once. Base-relative results (useful set, high-ground, corridors) stay per bot in the
	// donor; none of that exists yet in ZG-a.
	public sealed class SharedZoneTopology
	{
		public readonly List<ZoneChokepoint> Chokepoints = [];
		public readonly List<CPos> BridgeWatchCells = [];
		public CellLayer<bool> Passability;
		public IReadOnlyDictionary<CPos, uint> AbstractDomains;
		public CPos[] NodeCells = [];

		// A fold over the watched bridge cells, not a yes/no - see CurrentBridgeSignature.
		public int LastBridgeSignature;
		public int Generation;

		// The map's shape, cut once by the same chokepoints that gate the territory walk - not anyone's
		// claim, a terrain fact every bot can read regardless of who (if anyone) owns it.
		public readonly List<Zone> Regions = [];
		public CellLayer<int> RegionIdByCell;

		// The corridors Zone.GateCorridorIndices index into. Kept because the indices are otherwise
		// unreadable: the list was local to BuildRegions and thrown away, so a region could say it had
		// three doors and nothing could ask how wide any of them was.
		public readonly List<ZoneGateCorridor> RegionGateCorridors = [];

		// What the cut actually ran along: resolved chokepoint corridors plus cliff ramps. Kept because
		// "the regions are wrong here" always turns out to mean "the barrier has a hole here", and a hole
		// is not visible from the region outlines alone - they simply run past it.
		public HashSet<CPos> RegionBarrier = [];

		// ZG-b adds the periodically-refreshed region-ownership tally (RegionOwners /
		// NextOwnershipRefreshTick) on top of this shape.
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Cartographs the map once at game start (and on bridge changes) to find chokepoints (bridges, cliff ramps,",
		"narrow land passages) and cut the map into regions (IBotZoneTopology).",
		"Reads the hierarchical pathfinder's abstract graph for connectivity domains.")]
	public class TacticalMapBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Locomotor name(s) used to analyse ground access. The first one present is used.",
			"Should be the bot's main ground combat locomotor (e.g. tracked). Empty = first available ground locomotor.")]
		public readonly FrozenSet<string> TopologyLocomotors = FrozenSet<string>.Empty;

		[Desc("Base weight of a chokepoint that is a bridge (the strongest natural funnel).")]
		public readonly int BridgeWeight = 220;

		[Desc("Base weight of a chokepoint that is a cliff ramp / height transition.")]
		public readonly int RampWeight = 170;

		[Desc("Base weight of a chokepoint that is a narrow land passage.")]
		public readonly int PassageWeight = 120;

		[Desc("Ticks between re-checks of bridge-chokepoint passability. A change triggers a rebuild.")]
		public readonly int TopologyRecheckInterval = 50;

		[Desc("Optional long fallback recompute interval in ticks. 0 disables it (rely on bridge-change detection).")]
		public readonly int RecomputeInterval = 0;

		[Desc("Ignore chokepoints in regions (domains) with fewer abstract graph nodes than this — tiny isolated",
			"areas like an unreachable mesa top are not tactically meaningful.")]
		public readonly int MinDomainNodes = 4;

		[Desc("Maximum width (cells) of a passable corridor still considered a Passage chokepoint. Wider gaps are open",
			"terrain, not bottlenecks.")]
		public readonly int MaxPassageWidth = 8;

		[Desc("Both sides of a Passage must flood to at least this many cells, else it is a dead end / non-separating",
			"gap and is discarded.")]
		public readonly int MinPassageSideCells = 24;

		[Desc("Merge chokepoints that end up within this many cells of each other (cuts duplicate markers/clutter).")]
		public readonly int ChokepointMergeRadius = 5;

		[Desc("How far (cells) a ramp seal may grow away from the cliff it is anchored at. A ramp is a cut",
			"through a cliff and is at most about this wide; without a bound the run of slope tiles it grows",
			"along is connected across half a rolling map and the seal swallows the map with it. Both edges",
			"of a wide ramp seed the growth, so a ramp up to roughly twice this wide still closes.")]
		public readonly int RampSealMaxSpread = 8;

		[Desc("Regions smaller than this (cells) are merged into their larger neighbour by dropping the",
			"barrier between them, repeated until no undersized region has anywhere left to merge into.",
			"A chokepoint can be a perfectly genuine bottleneck and still be far too minor a wrinkle to",
			"deserve a region of its own - a coastline pinched every few cells came out as a chain of",
			"15/35/47-cell regions, which is not how anybody reads that ground. 0 disables merging.")]
		public readonly int MinRegionSize = 60;

		[Desc("The coarse chokepoint cell is snapped to the genuinely narrowest crossing within this radius (cells).")]
		public readonly int ChokepointSnapRadius = 3;

		[Desc("Widest a gap may be and still resolve to a gate corridor between two regions. Past this the terrain",
			"is not pinching anything. (Donor's MaxDoorWidth — renamed: doors are ZG-b, the width bound itself is",
			"needed here because it is what the region cut resolves along.)")]
		public readonly int MaxGateWidth = 14;

		public override object Create(ActorInitializer init) { return new TacticalMapBotModule(init.Self, this); }
	}

	public class TacticalMapBotModule : ConditionalTrait<TacticalMapBotModuleInfo>, IBotTick, IWorldLoaded, IBotZoneTopology
	{
		readonly World world;
		readonly OpenRA.Player player;

		PathFinder pathFinder;
		Locomotor locomotor;

		readonly List<ZoneChokepoint> chokepoints = [];
		IReadOnlyDictionary<CPos, uint> abstractDomains;
		CPos[] nodeCells = [];

		// Region shape (Cells/GateCorridorIndices/AdjacentRegionIds/counts) is small and copied per-instance
		// like chokepoints; RegionIdByCell is heavy like Passability, so it is only ever referenced, never copied.
		readonly List<Zone> regions = [];
		readonly List<ZoneGateCorridor> regionGateCorridors = [];
		CellLayer<int> regionIdByCell;
		HashSet<CPos> regionBarrier = [];
		IResourceLayer resourceLayer;

		// The terrain-only topology is built once per (world, locomotor) and shared across all bots. The first bot to
		// build publishes here; the rest adopt it. Generation bumps on a (shared) bridge-change rebuild.
		static readonly ConditionalWeakTable<World, Dictionary<string, SharedZoneTopology>> Registries = [];
		SharedZoneTopology shared;
		int sharedGeneration = -1;

		// True once the topology has been built. Exposed so a consumer can read without triggering a build.
		public bool TopologyReady { get; private set; }

		// Passability signature of bridge chokepoints, used to detect destroyed/repaired bridges cheaply.
		readonly List<CPos> bridgeWatchCells = [];
		int lastBridgeSignature;
		int recheckTick;
		int recomputeTick;

		// Terrain passability cached once per rebuild (avoids repeated Locomotor.MovementCostForCell in scans/floods).
		CellLayer<bool> passability;

		public TacticalMapBotModule(Actor self, TacticalMapBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			pathFinder = world.WorldActor.TraitOrDefault<PathFinder>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();

			if (Info.TopologyLocomotors.Count > 0)
				locomotor = world.WorldActor.TraitsImplementing<Locomotor>()
					.FirstOrDefault(l => Info.TopologyLocomotors.Contains(l.Info.Name));

			// Fallback: first locomotor that actually allows ground movement.
			locomotor ??= world.WorldActor.TraitsImplementing<Locomotor>()
				.FirstOrDefault(l => l.Info.TerrainSpeeds.Values.Any(t => t.Cost < short.MaxValue));

			base.Created(self);
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			// Build the (terrain-only) topology up front so bots never pay the one-time scan as a mid-game hitch.
			// Deferred to a frame-end task so it runs AFTER PathFinder.WorldLoaded has created its abstract graph
			// (trait WorldLoaded order is not guaranteed), and gated to enabled bot players — a disabled
			// (non-genericbot) module never builds, which is the ZG-a activation gate.
			if (player.IsBot)
				w.AddFrameEndTask(_ =>
				{
					if (!IsTraitDisabled)
						EnsureBuilt();
				});
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (!TopologyReady)
				return;

			// Another bot rebuilt the shared topology (e.g. after a bridge change) -> adopt the newer version.
			// FIRST, before anything below reads a region id: the shared Regions list is already the new cut
			// while this bot's regionIdByCell is still the old one.
			if (shared != null && shared.Generation != sharedGeneration)
			{
				Adopt(shared);
				return;
			}

			if (Info.RecomputeInterval > 0 && --recomputeTick <= 0)
			{
				recomputeTick = Info.RecomputeInterval;
				RebuildSharedOnBridgeChange();
				return;
			}

			if (--recheckTick > 0)
				return;

			recheckTick = Math.Max(1, Info.TopologyRecheckInterval);

			// A bridge being destroyed/repaired flips the passability of its cells -> rebuild the shared topology once.
			if (shared != null && CurrentBridgeSignature() != lastBridgeSignature)
				RebuildSharedOnBridgeChange();
		}

		int CurrentBridgeSignature()
		{
			// A fold over each watched cell, not "are they all passable". As a boolean this could only ever
			// report the FIRST change: once one bridge was down it read false and stayed false, so a second
			// bridge falling, or the first being repaired while the second stayed down, changed nothing and
			// the graph went stale for the rest of the match.
			// Checked LIVE rather than through the frozen passability snapshot, so a bridge actually
			// destroyed or repaired is seen. Only a handful of cells, and bridgeWatchCells is filled in
			// scan order, so the same map state always folds to the same number everywhere.
			var signature = 17;
			foreach (var cell in bridgeWatchCells)
			{
				var passable = locomotor != null && world.Map.Contains(cell)
					&& locomotor.MovementCostForCell(cell) != short.MaxValue;

				signature = signature * 31 + (passable ? 1 : 0);
			}

			return signature;
		}

		void EnsureBuilt()
		{
			if (!TopologyReady)
				Rebuild();
		}

		void Rebuild()
		{
			// TopologyReady is set only on success, at the end. It used to be set here, before the
			// guards below — so a build that bailed out (no abstract graph yet, no locomotor) left the
			// module permanently "ready" with zero chokepoints, and EnsureBuilt, which only rebuilds
			// while !TopologyReady, never tried again. The tactical map stayed silently dead for the
			// whole match. BuildOwnTopology returning false is exactly the "graph not created yet"
			// case the WorldLoaded frame-end task is meant to avoid, so it must stay retryable.
			recheckTick = Math.Max(1, Info.TopologyRecheckInterval);
			recomputeTick = Math.Max(1, Info.RecomputeInterval);
			chokepoints.Clear();
			bridgeWatchCells.Clear();
			abstractDomains = null;
			nodeCells = [];
			passability = null;
			regions.Clear();
			regionGateCorridors.Clear();
			regionIdByCell = null;
			shared = null;
			sharedGeneration = -1;

			if (pathFinder == null || locomotor == null)
				return;

			// Reuse another bot's build for the same locomotor if present; otherwise build it once and publish.
			var registry = Registries.GetValue(world, _ => []);
			var key = locomotor.Info.Name;
			if (registry.TryGetValue(key, out var existing) && existing != null)
			{
				Adopt(existing);
				TopologyReady = true;
				return;
			}

			if (!BuildOwnTopology())
				return;

			shared = Publish();
			registry[key] = shared;
			sharedGeneration = shared.Generation;
			TopologyReady = true;
		}

		// Adopt a shared topology built by another bot: reference the heavy data, copy the small lists.
		void Adopt(SharedZoneTopology sh)
		{
			shared = sh;
			sharedGeneration = sh.Generation;
			passability = sh.Passability;
			abstractDomains = sh.AbstractDomains;
			nodeCells = sh.NodeCells;
			lastBridgeSignature = sh.LastBridgeSignature;
			chokepoints.Clear();
			chokepoints.AddRange(sh.Chokepoints);
			bridgeWatchCells.Clear();
			bridgeWatchCells.AddRange(sh.BridgeWatchCells);
			regions.Clear();
			regions.AddRange(sh.Regions);
			regionGateCorridors.Clear();
			regionGateCorridors.AddRange(sh.RegionGateCorridors);
			regionIdByCell = sh.RegionIdByCell;
			regionBarrier = sh.RegionBarrier;
		}

		SharedZoneTopology Publish()
		{
			var sh = new SharedZoneTopology
			{
				Passability = passability,
				AbstractDomains = abstractDomains,
				NodeCells = nodeCells,
				LastBridgeSignature = lastBridgeSignature,
				RegionIdByCell = regionIdByCell,
				RegionBarrier = regionBarrier,
			};
			sh.Chokepoints.AddRange(chokepoints);
			sh.BridgeWatchCells.AddRange(bridgeWatchCells);
			sh.Regions.AddRange(regions);
			sh.RegionGateCorridors.AddRange(regionGateCorridors);
			return sh;
		}

		// Rebuild the shared topology after a bridge change and bump the generation so other bots re-adopt it.
		void RebuildSharedOnBridgeChange()
		{
			if (shared == null || !BuildOwnTopology())
				return;

			shared.Passability = passability;
			shared.AbstractDomains = abstractDomains;
			shared.NodeCells = nodeCells;
			shared.LastBridgeSignature = lastBridgeSignature;
			shared.Chokepoints.Clear();
			shared.Chokepoints.AddRange(chokepoints);
			shared.BridgeWatchCells.Clear();
			shared.BridgeWatchCells.AddRange(bridgeWatchCells);

			// A bridge changing can split or merge regions (it changes which corridors resolve), so the
			// shape has to be rebuilt alongside the chokepoints that gate it.
			shared.Regions.Clear();
			shared.Regions.AddRange(regions);
			shared.RegionGateCorridors.Clear();
			shared.RegionGateCorridors.AddRange(regionGateCorridors);
			shared.RegionIdByCell = regionIdByCell;
			shared.RegionBarrier = regionBarrier;

			shared.Generation++;
			sharedGeneration = shared.Generation;
		}

		// The heavy terrain scan (shared by all bots via Publish): fills passability, abstractDomains, nodeCells,
		// chokepoints, bridgeWatchCells and lastBridgeSignature. Returns false if the locomotor has no abstract graph.
		bool BuildOwnTopology()
		{
			chokepoints.Clear();
			bridgeWatchCells.Clear();

			// Terrain-only connectivity (BlockedByActor.None): topology should reflect permanent terrain (cliffs,
			// water, bridges, ramps), NOT trees/buildings/units, which would create fake chokepoints around obstacles.
			// Queried BEFORE the passability fill so a not-yet-created abstract graph costs nothing: this method is
			// retryable (see Rebuild), and the fill is a full-map pass we don't want to repeat on every failed attempt.
			var (graph, domains) = pathFinder.GetOverlayDataForLocomotor(locomotor, BlockedByActor.None);
			if (graph == null || domains == null || graph.Count == 0)
				return false;

			// Cache terrain passability once so the scans/floods below don't call Locomotor.MovementCostForCell repeatedly.
			passability = new CellLayer<bool>(world.Map);
			foreach (var c in world.Map.AllCells)
				passability[c] = locomotor.MovementCostForCell(c) != short.MaxValue;

			abstractDomains = domains;
			nodeCells = graph.Keys.ToArray();

			// Count abstract nodes per domain so we can ignore tiny, isolated regions (mesa tops, unreachable
			// islands). Those are their own domain with only a handful of nodes and produce meaningless chokepoints.
			var domainNodeCount = new Dictionary<uint, int>();
			foreach (var node in nodeCells)
				if (abstractDomains.TryGetValue(node, out var dom))
					domainNodeCount[dom] = (domainNodeCount.TryGetValue(dom, out var dc) ? dc : 0) + 1;

			// De-duplicate chokepoints that map to the same representative cell.
			var byCell = new Dictionary<CPos, ZoneChokepoint>();

			void AddTyped(CPos cell, uint domain, ZoneChokepointType type)
			{
				if (byCell.ContainsKey(cell))
					return;

				// Merge near-duplicate passages to cut clutter. Discrete points (ramps, bridges) are kept as-is.
				var mergeSq = Math.Max(0, Info.ChokepointMergeRadius) * Math.Max(0, Info.ChokepointMergeRadius);
				if (type == ZoneChokepointType.Passage && mergeSq > 0)
					foreach (var existing in byCell.Keys)
						if ((existing - cell).LengthSquared <= mergeSq)
							return;

				var weight = type switch
				{
					ZoneChokepointType.Bridge => Info.BridgeWeight,
					ZoneChokepointType.Ramp => Info.RampWeight,
					_ => Info.PassageWeight,
				};

				byCell[cell] = new ZoneChokepoint(cell, domain, type, weight);
				if (type == ZoneChokepointType.Bridge)
					bridgeWatchCells.Add(cell);
			}

			void Add(CPos cell, ZoneChokepointType type)
			{
				// Ignore chokepoints inside tiny, isolated regions (e.g. an unreachable mesa top): not tactically real.
				var domain = DomainOf(cell);
				if (domainNodeCount.TryGetValue(domain, out var nodes) && nodes < Math.Max(1, Info.MinDomainNodes))
					return;

				AddTyped(cell, domain, type);
			}

			// Chokepoints are found directly from the terrain (not from the coarse abstract graph, which over-produces
			// noise): narrow separating passages, cliff ramps (marked below + above), and bridges.
			// Order matters: the near-duplicate merge in AddTyped only drops a Passage that lands close
			// to an ALREADY REGISTERED chokepoint. Scanning passages first meant bridges and ramps did
			// not exist yet, so nothing could be merged away — and bridges/ramps are never merge-checked
			// themselves. A single bridge therefore produced three markers (B in the middle, P at each
			// approach), which then crowded out other approaches in the donor's top-N hotspot query.
			// Discrete features first, passages last, so ChokepointMergeRadius actually does its job.
			foreach (var center in ScanBridgeCenters())
				Add(center, ZoneChokepointType.Bridge);

			foreach (var endpoint in ScanRampEndpoints())
				Add(endpoint, ZoneChokepointType.Ramp);

			foreach (var center in ScanPassageCenters())
				Add(center, ZoneChokepointType.Passage);

			chokepoints.AddRange(byCell.Values);

			// Donor also watched CNDestroyableCliff footprints here: a cliff shot open re-cuts the map exactly
			// the way a bridge falling does. Cameo has no destroyable-cliff trait, so only bridge cells are
			// watched — if one is ever added, extend the watch here rather than adding a second mechanism.
			lastBridgeSignature = CurrentBridgeSignature();

			BuildRegions(domainNodeCount);
			return true;
		}

		// The map's shape, cut by the same chokepoint corridors that gate the territory walk PLUS every
		// cliff ramp's full footprint - computed once here (unseeded by any claim) so every bot reads the
		// same regions regardless of who, if anyone, owns them. A ramp only sealable-corridor-resolves to
		// a narrow gate cell when it happens to be the tightest crossing nearby; a wide or gentle one
		// otherwise let the flood walk straight through it, so a visible cliff with a walkable slope ended
		// up inside one region instead of splitting high ground from low. The full ramp footprint (not
		// just its two chokepoint marker endpoints) is barrier here regardless of whether it resolved to a
		// gate corridor, because height is a real separation even where nothing is narrow enough to
		// wall. One flood-fill component per gap between barrier cells is one region; adjacency between
		// regions falls out of which ones share a barrier cell, whether or not that cell also happens to
		// carry a resolved door corridor.
		//
		// Not every barrier deserves to stand, though: a coastline pinched every few cells came out as a
		// chain of 15/35/47-cell regions, which is not how anybody reads that ground. So the fill runs
		// more than once - undersized regions merge by dropping the barrier piece between them and
		// filling again (see MinRegionSize), which recomputes every outline, count and adjacency instead
		// of patching finished records up by hand.
		void BuildRegions(Dictionary<uint, int> domainNodeCount)
		{
			var gate = new HashSet<CPos>();
			var corridors = ResolveChokepointCorridors(gate);

			// Kept for the whole run, not just for cellToCorridorIndex below: GateCorridorIndices on every
			// finished region points in here, and the merge only ever drops barrier pieces - it never
			// renumbers this list - so the indices stay valid however many rounds the fill takes.
			regionGateCorridors.Clear();
			regionGateCorridors.AddRange(corridors);

			var cellToCorridorIndex = new Dictionary<CPos, int>();
			for (var i = 0; i < corridors.Count; i++)
				foreach (var cell in corridors[i].Cells)
					cellToCorridorIndex[cell] = i;

			// IsCliffRamp only flags a cell with a cliff immediately beside it, so on a ramp wider than a
			// couple of cells only its outermost columns qualify: the middle of the slope touches nothing
			// impassable and was never barrier at all, which leaves a hole straight through the middle of
			// the very thing being sealed - and the one-ring dilation below only ever covered a hole two
			// cells across. A ramp is one connected run of walkable slope, bounded by the cliff it cuts
			// through, so the flagged cells are grown across that run: the seal ends up the ramp's true
			// width, however wide it is, while open bumpy ground stays untouched because no cliff seeds it
			// in the first place.
			// Map.Ramp was tried as part of this run and taken back out: on this tileset a great many
			// ordinary ground tiles carry a nonzero ramp index, so the run was connected across most of the
			// map and the seal carpeted open ground - see the terrain census logged below, which is what
			// any further attempt here should be decided on rather than on what a ramp "ought" to look
			// like.
			var slopeCells = new HashSet<CPos>();
			var rampCells = new HashSet<CPos>();
			foreach (var c in world.Map.AllCells)
			{
				if (!IsPassable(c) || !IsHeightTransition(c))
					continue;

				slopeCells.Add(c);

				// Seeded from the cliff, so a hillside nowhere near one is never barrier however much it
				// slopes.
				if (HasCliffAbove(c))
					rampCells.Add(c);
			}

			// What the map is actually made of, because three attempts at sealing ramps were each argued
			// from a different guess about that and each was wrong in a different direction. Once per map,
			// alongside the region census below.
			var passableCells = 0;
			var slopeTileCells = 0;
			var transitionCells = 0;
			var cliffAdjacentCells = 0;
			var heightCensus = new Dictionary<byte, int>();
			foreach (var c in world.Map.AllCells)
			{
				if (!IsPassable(c))
					continue;

				passableCells++;
				var h = world.Map.Height[c];
				heightCensus[h] = heightCensus.GetValueOrDefault(h) + 1;

				if (world.Map.Ramp[c] != 0)
					slopeTileCells++;

				if (IsHeightTransition(c))
					transitionCells++;

				if (HasCliffAbove(c))
					cliffAdjacentCells++;
			}

			CAAIUtils.BotDebug("terrain: {0} passable, {1} slope tiles, {2} height transitions, {3} cliff-adjacent | heights {4}",
				passableCells, slopeTileCells, transitionCells, cliffAdjacentCells,
				string.Join(" ", heightCensus.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}")));

			// Bounded, because slope tiles are not rare on rolling terrain: their connected run reaches
			// across half a map, and an unbounded growth swallowed it whole - the barrier covered open
			// ground everywhere and left regions of one and two cells behind. A ramp is a cut through a
			// cliff and is only so wide, and both of its edges seed, so the bound closes a ramp up to
			// roughly twice RampSealMaxSpread across while leaving open hillside alone.
			var maxSpread = Math.Max(1, Info.RampSealMaxSpread);
			var rampQueue = new Queue<(CPos Cell, int Depth)>();
			foreach (var seed in rampCells)
				rampQueue.Enqueue((seed, 0));

			while (rampQueue.Count > 0)
			{
				var (cell, depth) = rampQueue.Dequeue();
				if (depth >= maxSpread)
					continue;

				foreach (var dir in CVec.Directions)
				{
					var next = cell + dir;
					if (slopeCells.Contains(next) && rampCells.Add(next))
						rampQueue.Enqueue((next, depth + 1));
				}
			}

			// The barrier is kept as individually droppable pieces - one per resolved corridor, one per
			// chokepoint that never resolved to one, one per physical ramp - because merging undersized
			// regions below works by dropping the piece that separates them and running the whole fill
			// again, rather than by editing finished Zone records. Recomputing boundaries, adjacency
			// and the resource/buildable counts by hand per merge is exactly the bespoke bookkeeping that
			// re-deriving door geometry by hand already cost this feature five attempts.
			// Droppable says whether the merge below may give a piece up. Corridors and chokepoint cells
			// may: a bottleneck can be too minor a wrinkle to deserve a region. A ramp may not - dropping
			// it merges high ground into low ground, which is the one separation the whole ramp barrier
			// exists for, and a small plateau is still a place in its own right rather than part of the
			// ground below it.
			var pieces = new List<(HashSet<CPos> Cells, bool Droppable)>();
			foreach (var corridor in corridors)
				pieces.Add(([.. corridor.Cells], true));

			foreach (var cell in gate)
				if (!cellToCorridorIndex.ContainsKey(cell))
					pieces.Add(([cell], true));

			// A one-cell-thin barrier line has two known ways to leak with 8-directional movement: the
			// flood corner-cuts diagonally between two ramp cells that only touch corner-to-corner, and
			// the flat lead-in/lead-out cells at a ramp's very top and bottom are not height transitions
			// at all, so nothing above flags them - exactly where a region flowed straight through in the
			// first overlay pass. Dilating by one ring closes both: thick enough that no diagonal gap fits,
			// and wide enough to cover the ramp's open ends too. Per ramp rather than over one combined
			// blob - same cells, but each physical ramp can then be dropped on its own.
			var rampVisited = new HashSet<CPos>();
			var rampPieces = 0;
			foreach (var start in rampCells)
			{
				if (!rampVisited.Add(start))
					continue;

				var component = ConnectedComponent(start, rampCells, rampVisited);
				var piece = new HashSet<CPos>(component);
				foreach (var cell in component)
					foreach (var dir in CVec.Directions)
					{
						var n = cell + dir;
						if (world.Map.Contains(n) && IsPassable(n))
							piece.Add(n);
					}

				pieces.Add((piece, false));
				rampPieces++;
			}

			var active = new bool[pieces.Count];
			for (var i = 0; i < active.Length; i++)
				active[i] = true;

			var minRegionSize = Math.Max(0, Info.MinRegionSize);
			var piecesDropped = 0;
			var round = 0;

			// Cheap insurance, not a tuning knob: dropping is monotone, so this terminates on its own -
			// and it all happens once inside BuildOwnTopology, never per tick. One piece goes per round
			// now, so the ceiling has to be the number of droppable pieces rather than a flat ten, which
			// would have stopped the merge a tenth of the way through. Each extra round is one more
			// full-map fill, paid once at map load.
			var maxMergeRounds = pieces.Count + 1;

			for (; ; round++)
			{
				var barrier = new HashSet<CPos>();
				for (var i = 0; i < pieces.Count; i++)
					if (active[i])
						barrier.UnionWith(pieces[i].Cells);

				FloodRegions(barrier, cellToCorridorIndex, domainNodeCount);
				regionBarrier = barrier;

				if (minRegionSize == 0 || round >= maxMergeRounds - 1)
					break;

				// Every INDEPENDENT drop of the round at once. Dropping every qualifying piece together was
				// wrong because they were all measured against a fill still cut by pieces about to be
				// dropped themselves: a side reads undersized only because its neighbours have not been
				// merged into it yet, the piece goes on that reading, and dropping is monotone so it never
				// comes back. A played map lost 67 of 120 pieces that way and left passages separating
				// nothing.
				//
				// But only pieces touching the SAME region can spoil each other's measurement. Two whose
				// neighbouring regions are disjoint can go in one round with no artifact at all, and nearly
				// every sliver is of that kind. Taking strictly one per round was the safe reading and cost
				// 129 full-map fills at load on a 367-piece map - 1901 ms inside a single bot tick, which
				// is a visible hitch and was waved through in its own commit message as "paid once at map
				// load" without anyone measuring it.
				// An undersized pocket with nothing to merge into keeps its pieces and stays small, the
				// same way MinDomainNodes leaves unreachable pockets alone.
				var eligible = new List<(int Index, int Separated, HashSet<int> Touching)>();
				for (var i = 0; i < pieces.Count; i++)
				{
					if (!active[i] || !pieces[i].Droppable)
						continue;

					var touching = TouchedRegions(pieces[i].Cells);
					if (touching.Count < 2)
						continue;

					var separated = int.MaxValue;
					foreach (var id in touching)
						separated = Math.Min(separated, regions[id].Size);

					if (separated < minRegionSize)
						eligible.Add((i, separated, touching));
				}

				if (eligible.Count == 0)
					break;

				// Worst first, so where two genuinely do conflict the more urgent one goes this round and
				// the other is re-measured in the next.
				eligible.Sort((a, b) => a.Separated.CompareTo(b.Separated));

				var claimed = new HashSet<int>();
				foreach (var (index, _, touching) in eligible)
				{
					if (touching.Overlaps(claimed))
						continue;

					claimed.UnionWith(touching);
					active[index] = false;
					piecesDropped++;
				}
			}

			CAAIUtils.BotDebug(
				"regions: {0} barrier pieces ({1} ramps, {2} ramp cells, kept whatever happens), {3} dropped "
				+ "over {4} rounds -> {5} regions (min size {6})",
				pieces.Count, rampPieces, rampCells.Count, piecesDropped, round + 1, regions.Count, minRegionSize);
		}

		/// <summary>
		/// The regions this barrier piece holds apart. Read off the finished fill rather than tracked
		/// during it: a piece is small, and what it separates is simply whatever <see cref="regionIdByCell"/>
		/// says on either side of it.
		/// <para>
		/// The whole set rather than only the smallest size, because the merge needs to know which drops
		/// may share a round: two pieces with no neighbouring region in common cannot spoil each other's
		/// measurement, and batching those is the difference between a handful of full-map fills and one
		/// per dropped piece. Fewer than two regions means nothing on the far side to merge into - a piece
		/// against the map edge, or buried inside a wider barrier.
		/// </para>
		/// </summary>
		HashSet<int> TouchedRegions(HashSet<CPos> piece)
		{
			var touching = new HashSet<int>();
			foreach (var cell in piece)
				foreach (var dir in CVec.Directions)
				{
					var id = RegionIdAt(cell + dir);
					if (id >= 0)
						touching.Add(id);
				}

			return touching;
		}

		/// <summary>
		/// One flood-fill pass over the map with the given barrier: every gap between barrier cells
		/// becomes one region, with its outline, adjacency and counts derived fresh. Called repeatedly by
		/// <see cref="BuildRegions"/> with a shrinking barrier, so everything a merge would otherwise have
		/// to patch up by hand is simply recomputed instead.
		/// </summary>
		void FloodRegions(HashSet<CPos> barrier, Dictionary<CPos, int> cellToCorridorIndex, Dictionary<uint, int> domainNodeCount)
		{
			regions.Clear();
			regionIdByCell = new CellLayer<int>(world.Map);
			foreach (var c in world.Map.AllCells)
				regionIdByCell[c] = -1;

			var minDomainNodes = Math.Max(1, Info.MinDomainNodes);
			var visited = new HashSet<CPos>(barrier);
			var regionsTouchingBarrierCell = new Dictionary<CPos, List<int>>();

			foreach (var start in world.Map.AllCells)
			{
				if (!IsPassable(start) || !visited.Add(start))
					continue;

				// Tiny isolated pockets (a mesa top, an unreachable sliver) are not a tactically real
				// region, same reasoning MinDomainNodes already applies to chokepoints in one.
				if (domainNodeCount.TryGetValue(DomainOf(start), out var nodes) && nodes < minDomainNodes)
					continue;

				var id = regions.Count;
				var cells = new List<CPos>();
				var gateIndices = new HashSet<int>();
				var touchedBarrierCells = new List<CPos>();
				var resourceCells = 0;
				var buildableCells = 0;
				var queue = new Queue<CPos>();
				queue.Enqueue(start);
				cells.Add(start);

				while (queue.Count > 0)
				{
					var cell = queue.Dequeue();
					regionIdByCell[cell] = id;

					if (resourceLayer != null && resourceLayer.GetResource(cell).Type != null)
						resourceCells++;

					if (world.Map.Ramp[cell] == 0)
						buildableCells++;

					foreach (var dir in CVec.Directions)
					{
						var next = cell + dir;
						if (!world.Map.Contains(next))
							continue;

						if (barrier.Contains(next))
						{
							if (cellToCorridorIndex.TryGetValue(next, out var corridorIndex))
								gateIndices.Add(corridorIndex);

							touchedBarrierCells.Add(next);
							continue;
						}

						if (!IsPassable(next) || !visited.Add(next))
							continue;

						cells.Add(next);
						queue.Enqueue(next);
					}
				}

				foreach (var barrierCell in touchedBarrierCells)
				{
					if (!regionsTouchingBarrierCell.TryGetValue(barrierCell, out var touching))
						regionsTouchingBarrierCell[barrierCell] = touching = [];

					if (!touching.Contains(id))
						touching.Add(id);
				}

				// A cell with any neighbour outside this region's own cell set is on the outline - a
				// separate pass over the finished set rather than tracked during the flood, since a
				// neighbour failing visited.Add there could mean either "already ours" or "someone else's
				// region", and only this region's own finished cell set can tell the two apart.
				var cellSet = new HashSet<CPos>(cells);
				var boundary = new List<CPos>();
				foreach (var cell in cells)
				{
					var isBoundary = false;
					foreach (var dir in CVec.Directions)
					{
						if (world.Map.Contains(cell + dir) && cellSet.Contains(cell + dir))
							continue;

						isBoundary = true;
						break;
					}

					if (isBoundary)
						boundary.Add(cell);
				}

				regions.Add(new Zone(id, cells.ToArray(), gateIndices.ToArray(), [], resourceCells,
					buildableCells, boundary.ToArray()));
			}

			// Adjacency falls out of which regions share a barrier cell - a resolved gate corridor or an
			// unresolved cliff ramp both count. One pass over the (small) barrier-cell map rather than one
			// per region: most barrier cells touch exactly two regions, so this stays close to linear.
			var adjacency = new Dictionary<int, HashSet<int>>();
			foreach (var touching in regionsTouchingBarrierCell.Values)
			{
				if (touching.Count < 2)
					continue;

				foreach (var a in touching)
				{
					if (!adjacency.TryGetValue(a, out var set))
						adjacency[a] = set = [];

					foreach (var b in touching)
						if (b != a)
							set.Add(b);
				}
			}

			for (var id = 0; id < regions.Count; id++)
			{
				var region = regions[id];
				var adjacent = adjacency.TryGetValue(id, out var set) ? set.ToArray() : [];
				regions[id] = new Zone(id, region.Cells, region.GateCorridorIndices, adjacent,
					region.ResourceCellCount, region.BuildableCellCount, region.BoundaryCells);
			}
		}

		// Directly scans the terrain for narrow separating passages: the narrowest cell of each bounded, thick-
		// shouldered through-corridor whose two sides are both substantial (not a dead end). One cell per local
		// minimum; near-duplicates are merged by the caller.
		IEnumerable<CPos> ScanPassageCenters()
		{
			var maxWidth = Math.Max(1, Info.MaxPassageWidth);
			var widthH = new Dictionary<CPos, int>();
			var widthV = new Dictionary<CPos, int>();

			foreach (var c in world.Map.AllCells)
			{
				if (!IsPassable(c))
					continue;

				var wh = CorridorWidth(c, new CVec(1, 0), maxWidth);
				if (wh <= maxWidth)
					widthH[c] = wh;

				var wv = CorridorWidth(c, new CVec(0, 1), maxWidth);
				if (wv <= maxWidth)
					widthV[c] = wv;
			}

			var centers = new List<CPos>();

			// Horizontal corridors (narrow in X): keep the narrowest along the corridor length (Y).
			foreach (var (c, w) in widthH)
			{
				var up = widthH.GetValueOrDefault(c + new CVec(0, 1), int.MaxValue);
				var dn = widthH.GetValueOrDefault(c + new CVec(0, -1), int.MaxValue);
				if (w <= up && w <= dn && IsRealSeparator(c, new CVec(1, 0)))
					centers.Add(c);
			}

			// Vertical corridors (narrow in Y): keep the narrowest along the corridor length (X).
			foreach (var (c, w) in widthV)
			{
				var l = widthV.GetValueOrDefault(c + new CVec(-1, 0), int.MaxValue);
				var r = widthV.GetValueOrDefault(c + new CVec(1, 0), int.MaxValue);
				if (w <= l && w <= r && IsRealSeparator(c, new CVec(0, 1)))
					centers.Add(c);
			}

			return centers;
		}

		// Width of the passable run through c along step, if it is a through-corridor (open along the perpendicular
		// length axis) bounded by thick (>=2 cell) impassable shoulders within maxWidth. Otherwise int.MaxValue.
		int CorridorWidth(CPos c, CVec step, int maxWidth)
		{
			var perp = new CVec(step.Y, step.X);
			if (!IsPassable(c + perp) || !IsPassable(c - perp))
				return int.MaxValue;

			var pos = WalkSide(c, step, maxWidth);
			if (pos < 0)
				return int.MaxValue;

			var neg = WalkSide(c, new CVec(-step.X, -step.Y), maxWidth);
			if (neg < 0)
				return int.MaxValue;

			return pos + neg + 1;
		}

		// Passable cells from c along step until a thick shoulder; returns that count, or -1 if no thick shoulder.
		int WalkSide(CPos c, CVec step, int maxWidth)
		{
			for (var i = 1; i <= maxWidth; i++)
			{
				var cell = c + step * i;
				if (!IsPassable(cell))
				{
					var beyond = c + step * (i + 1);
					return !world.Map.Contains(beyond) || !IsPassable(beyond) ? i - 1 : -1;
				}
			}

			return -1;
		}

		// True if blocking the narrow corridor through c (along step) separates two substantial regions — both
		// perpendicular sides flood to at least MinPassageSideCells. Drops dead ends and non-separating gaps.
		bool IsRealSeparator(CPos c, CVec step)
		{
			var perp = new CVec(step.Y, step.X);
			var barrier = new HashSet<CPos> { c };
			for (var i = 1; i <= Info.MaxPassageWidth; i++)
			{
				var p = c + step * i;
				if (!IsPassable(p))
					break;
				barrier.Add(p);
			}

			for (var i = 1; i <= Info.MaxPassageWidth; i++)
			{
				var nn = c + new CVec(-step.X, -step.Y) * i;
				if (!IsPassable(nn))
					break;
				barrier.Add(nn);
			}

			var min = Math.Max(1, Info.MinPassageSideCells);
			return FloodAtLeast(c + perp, barrier, min) && FloodAtLeast(c - perp, barrier, min);
		}

		bool FloodAtLeast(CPos seed, HashSet<CPos> barrier, int min)
		{
			if (!world.Map.Contains(seed) || !IsPassable(seed) || barrier.Contains(seed))
				return false;

			var visited = new HashSet<CPos>(barrier) { seed };
			var queue = new Queue<CPos>();
			queue.Enqueue(seed);
			var count = 0;
			while (queue.Count > 0)
			{
				var cell = queue.Dequeue();
				if (++count >= min)
					return true;

				foreach (var d in CVec.Directions)
				{
					var nx = cell + d;
					if (!world.Map.Contains(nx) || !IsPassable(nx) || !visited.Add(nx))
						continue;

					queue.Enqueue(nx);
				}
			}

			return false;
		}

		// Finds cliff ramps directly and yields a point below and above each (clustered: one ramp = two points).
		IEnumerable<CPos> ScanRampEndpoints()
		{
			var rampCells = new HashSet<CPos>();
			foreach (var c in world.Map.AllCells)
				if (IsPassable(c) && IsCliffRamp(c))
					rampCells.Add(c);

			var endpoints = new List<CPos>();
			var visited = new HashSet<CPos>();
			foreach (var start in rampCells)
			{
				if (!visited.Add(start))
					continue;

				var (bottom, top) = RampEndpoints(Centroid(ConnectedComponent(start, rampCells, visited)));
				if (bottom != null)
					endpoints.Add(bottom.Value);
				if (top != null)
					endpoints.Add(top.Value);
			}

			return endpoints;
		}

		// Finds bridges directly (terrain type "Bridge", bridge actor footprints, and MapEditorData-tagged
		// bridge actors) and yields one cell per bridge.
		IEnumerable<CPos> ScanBridgeCenters()
		{
			var bridgeCells = new HashSet<CPos>();
			foreach (var c in world.Map.AllCells)
				if (world.Map.GetTerrainInfo(c).Type == "Bridge")
					bridgeCells.Add(c);

			foreach (var b in world.ActorsWithTrait<Bridge>())
			{
				var bi = b.Actor.Info.TraitInfoOrDefault<BuildingInfo>();
				if (bi == null)
					continue;

				foreach (var cell in bi.Tiles(b.Actor.Location))
					bridgeCells.Add(cell);
			}

			// High/elevated bridges (the donor's BRIDGE1/BRIDGE2/RAILBRDG1/RAILBRDG2) are pure decoration there -
			// Immobile, OccupiesSpace: false, no Bridge trait, and nothing patches the terrain layer to type
			// "Bridge" under them since they are not destructible - so neither check above ever saw them,
			// and they fell through to being scanned as a plain Passage instead. Every bridge actor in the
			// donor's rules, low or high, was tagged MapEditorData Categories: Bridge, which is the one
			// thing both families shared. Kept in the port: harmless when nothing is tagged, and it covers
			// the same decoration-only case if a Cameo content pack ever ships it.
			foreach (var a in world.Actors)
			{
				if (a.IsDead || !a.IsInWorld)
					continue;

				var med = a.Info.TraitInfoOrDefault<MapEditorDataInfo>();
				if (med == null || !med.Categories.Contains("Bridge"))
					continue;

				var bi = a.Info.TraitInfoOrDefault<BuildingInfo>();
				if (bi == null)
					continue;

				foreach (var cell in bi.Tiles(a.Location))
					bridgeCells.Add(cell);
			}

			var centers = new List<CPos>();
			var visited = new HashSet<CPos>();
			foreach (var start in bridgeCells)
			{
				if (!visited.Add(start))
					continue;

				centers.Add(Centroid(ConnectedComponent(start, bridgeCells, visited)));
			}

			return centers;
		}

		// One 8-connected component of `cells`, grown from `start`. The caller must already have
		// added `start` to `visited` — the donors all do `if (!visited.Add(start)) continue;` —
		// otherwise the walk re-enters through a neighbour and `start` is collected twice.
		internal static List<CPos> ConnectedComponent(CPos start, HashSet<CPos> cells, HashSet<CPos> visited)
		{
			var comp = new List<CPos> { start };
			var queue = new Queue<CPos>();
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				var c = queue.Dequeue();
				foreach (var d in CVec.Directions)
				{
					var n = c + d;
					if (cells.Contains(n) && visited.Add(n))
					{
						comp.Add(n);
						queue.Enqueue(n);
					}
				}
			}

			return comp;
		}

		internal static CPos Centroid(List<CPos> cells)
		{
			long sx = 0;
			long sy = 0;
			foreach (var c in cells)
			{
				sx += c.X;
				sy += c.Y;
			}

			var centre = new CPos((int)(sx / cells.Count), (int)(sy / cells.Count));
			var rep = cells[0];
			var best = (rep - centre).LengthSquared;
			foreach (var c in cells)
			{
				var s = (c - centre).LengthSquared;
				if (s < best)
				{
					best = s;
					rep = c;
				}
			}

			return rep;
		}

		// A ramp: a walkable height transition AND flanked by a cliff (a higher, impassable neighbour = the cliff wall
		// the ramp cuts through). This excludes gentle hills / bumpy open ground that merely vary in height.
		bool IsCliffRamp(CPos cell)
		{
			return IsHeightTransition(cell) && HasCliffAbove(cell);
		}

		// The cliff-wall half of IsCliffRamp on its own: a higher, impassable neighbour. Split out because
		// the region barrier seeds from slope tiles as well, not only from cells that read as a walkable
		// height transition.
		bool HasCliffAbove(CPos cell)
		{
			var h = world.Map.Height[cell];
			foreach (var d in CVec.Directions)
			{
				var n = cell + d;
				if (world.Map.Contains(n) && !IsPassable(n) && world.Map.Height[n] > h)
					return true;
			}

			return false;
		}

		// Representative cells just below and just above a cliff ramp, instead of on the slope.
		(CPos? Bottom, CPos? Top) RampEndpoints(CPos cell)
		{
			CPos? transition = IsHeightTransition(cell) ? cell : null;
			if (transition == null)
				foreach (var d in CVec.Directions)
					if (IsHeightTransition(cell + d))
					{
						transition = cell + d;
						break;
					}

			if (transition == null)
				return (null, null);

			var t = transition.Value;
			var h = world.Map.Height[t];
			CVec downhill = default;
			CVec uphill = default;
			var haveDown = false;
			var haveUp = false;
			foreach (var d in CVec.Directions)
			{
				var n = t + d;
				if (!world.Map.Contains(n) || !IsPassable(n))
					continue;

				var nh = world.Map.Height[n];
				if (Math.Abs(nh - h) > 1)
					continue;

				if (nh < h && !haveDown)
				{
					downhill = d;
					haveDown = true;
				}
				else if (nh > h && !haveUp)
				{
					uphill = d;
					haveUp = true;
				}
			}

			var bottom = haveDown ? (CPos?)ProjectPassable(t, downhill, 2) : null;
			var top = haveUp ? (CPos?)ProjectPassable(t, uphill, 2) : null;
			return (bottom, top);
		}

		// Steps up to 'steps' cells along dir while passable, returning the furthest reachable cell.
		CPos ProjectPassable(CPos start, CVec dir, int steps)
		{
			var c = start;
			for (var i = 0; i < steps; i++)
			{
				var n = c + dir;
				if (!world.Map.Contains(n) || !IsPassable(n))
					break;

				c = n;
			}

			return c;
		}

		bool IsHeightTransition(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			var h = world.Map.Height[cell];
			var lower = false;
			var higher = false;
			foreach (var d in CVec.Directions)
			{
				var n = cell + d;
				if (!world.Map.Contains(n) || !IsPassable(n))
					continue;

				var nh = world.Map.Height[n];

				// Skip cliff jumps (height diff > 1 is not walkable); we only want actual walkable transitions.
				if (Math.Abs(nh - h) > 1)
					continue;

				if (nh < h)
					lower = true;
				else if (nh > h)
					higher = true;
			}

			return lower && higher;
		}

		bool IsPassable(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (passability != null)
				return passability[cell];

			return locomotor != null && locomotor.MovementCostForCell(cell) != short.MaxValue;
		}

		uint DomainOf(CPos reference)
		{
			if (nodeCells.Length == 0)
				return 0;

			var best = nodeCells[0];
			var bestDist = (best - reference).LengthSquared;
			foreach (var node in nodeCells)
			{
				var dist = (node - reference).LengthSquared;
				if (dist < bestDist)
				{
					bestDist = dist;
					best = node;
				}
			}

			return abstractDomains.TryGetValue(best, out var d) ? d : 0;
		}

		/// <summary>
		/// Resolves every scanned chokepoint to its genuine wall-to-wall corridor via
		/// <see cref="FindNarrowestCrossing"/> and folds each into <paramref name="gate"/> at full width.
		/// A chokepoint that fails to resolve to a real corridor (no thick shoulder in reach) still blocks
		/// its own coarse cell, so the region cut cannot pass straight through a marker that turned out to
		/// be unresolvable.
		/// </summary>
		List<ZoneGateCorridor> ResolveChokepointCorridors(HashSet<CPos> gate)
		{
			var corridors = new List<ZoneGateCorridor>();
			var seenCenters = new HashSet<CPos>();
			var snapRadius = Math.Max(0, Info.ChokepointSnapRadius);
			var maxWidth = Math.Max(1, Info.MaxGateWidth) + 1;

			foreach (var cp in chokepoints)
			{
				var corridor = FindNarrowestCrossing(cp.Cell, snapRadius, maxWidth);
				if (corridor == null)
				{
					gate.Add(cp.Cell);
					continue;
				}

				gate.UnionWith(corridor.Cells);
				if (seenCenters.Add(corridor.Center))
					corridors.Add(corridor);
			}

			return corridors;
		}

		// Searches near the coarse chokepoint cell for the narrowest passable crossing that is a genuine bottleneck:
		// bounded by thick (>=2 cell) impassable shoulders on both sides AND open along the approach axis.
		// This stops a single rock in open ground (or a wide field) from being mistaken for a sealable chokepoint.
		// Public: ZG-b also uses it to search for a narrower fallback pinch behind a door that is a poor line to hold.
		public ZoneGateCorridor FindNarrowestCrossing(CPos near, int snapRadius, int maxWidth)
		{
			ZoneGateCorridor best = null;
			var bestWidth = int.MaxValue;

			foreach (var center in world.Map.FindTilesInCircle(near, snapRadius))
			{
				if (!IsPassable(center))
					continue;

				for (var axis = 0; axis < 2; axis++)
				{
					var wallRunsHorizontal = axis == 0;
					var step = wallRunsHorizontal ? new CVec(1, 0) : new CVec(0, 1);

					// A real passage is open along the approach (perpendicular) axis on both sides of the line.
					var approach = wallRunsHorizontal ? new CVec(0, 1) : new CVec(1, 0);
					if (!IsPassable(center + approach) || !IsPassable(center - approach))
						continue;

					var cells = new List<CPos> { center };
					if (!WalkToShoulder(center, step, maxWidth, cells) || !WalkToShoulder(center, -step, maxWidth, cells))
						continue;

					if (cells.Count <= maxWidth && cells.Count < bestWidth)
					{
						bestWidth = cells.Count;
						best = new ZoneGateCorridor(center, wallRunsHorizontal, cells.ToArray());
					}
				}
			}

			return best;
		}

		// Walks passable cells from origin along step. Succeeds only at a *thick* shoulder (>=2 impassable cells or the
		// map edge), so a single isolated obstacle in open ground is not mistaken for a wall of a chokepoint.
		bool WalkToShoulder(CPos origin, CVec step, int maxWidth, List<CPos> into)
		{
			for (var i = 1; i <= maxWidth; i++)
			{
				var cell = origin + step * i;
				if (!IsPassable(cell))
				{
					var beyond = origin + step * (i + 1);
					return !world.Map.Contains(beyond) || !IsPassable(beyond);
				}

				into.Add(cell);
			}

			return false;
		}

		#region IBotZoneTopology

		/// <inheritdoc/>
		public IReadOnlyList<Zone> Regions
		{
			get
			{
				EnsureBuilt();
				return regions;
			}
		}

		/// <inheritdoc/>
		public IReadOnlyList<ZoneChokepoint> Chokepoints
		{
			get
			{
				EnsureBuilt();
				return chokepoints;
			}
		}

		/// <inheritdoc/>
		public int RegionIdAt(CPos cell) => regionIdByCell != null && world.Map.Contains(cell) ? regionIdByCell[cell] : -1;

		/// <summary>
		/// The generation this bot has ADOPTED, not the one currently published. The two differ for a tick
		/// after a bridge falls: the rebuilding bot publishes at once, while every other bot still answers
		/// <see cref="Regions"/> from the old cut until its own Adopt runs. Reporting the published
		/// number there would tell a caller its cached per-region state was current when it was not - and
		/// since the number then matches for good, the state would never be rebuilt at all.
		/// </summary>
		public int Generation => TopologyReady ? sharedGeneration : -1;

		/// <inheritdoc/>
		public bool IsPassableCell(CPos cell)
		{
			EnsureBuilt();
			return IsPassable(cell);
		}

		#endregion

		/// <summary>
		/// The corridor a region's <see cref="Zone.GateCorridorIndices"/> entry names, or null when the
		/// index does not resolve. This is how a region's gates get a width: the region itself only
		/// carries the indices.
		/// </summary>
		public ZoneGateCorridor GetRegionGateCorridor(int corridorIndex)
		{
			EnsureBuilt();
			return corridorIndex >= 0 && corridorIndex < regionGateCorridors.Count ? regionGateCorridors[corridorIndex] : null;
		}

		/// <summary>The resolved gate corridors bounding regions, indexed by <see cref="Zone.GateCorridorIndices"/>.</summary>
		public IReadOnlyList<ZoneGateCorridor> RegionGateCorridors
		{
			get
			{
				EnsureBuilt();
				return regionGateCorridors;
			}
		}

		/// <summary>
		/// The cells the region cut ran along: resolved gate corridors plus dilated cliff-ramp pieces.
		/// Read-only view for debugging/overlays: never builds or recomputes, just reads what the sim produced.
		/// </summary>
		public IReadOnlyCollection<CPos> RegionBarrierForOverlay() => regionBarrier;
	}
}
