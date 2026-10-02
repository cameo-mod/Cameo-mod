#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// SP-1 (AI_ARCHITECTURE §12.20): base sprawl. The maintainer's complaint: the bot packs buildings wall-to-wall,
	// the base becomes a single traffic jam, and the pathfinder grinds as hundreds of units path around a solid
	// block of structures. Upstream placement takes the first valid cell in the annulus scan, so whatever the
	// shuffle returns first lands, and buildings accrete edge-by-edge into a monolith.
	//
	// This module is the IBotPlacementAdvisor the queue manager consults for every non-refinery building
	// placement: it re-ranks the caller's already-legal candidate cells by *distance to the nearest own
	// building* — the golden rule is "keep as much space between buildings as possible". Cells closer than
	// DesiredGapCells to an existing building are only taken when nothing better places at all, so placement
	// never deadlocks on spacing alone: worst case is the widest cell left.
	//
	// ONE OWNER of base spacing (maintainer 2026-10-02, merged with DAWN's BaseBuilder MinBuildingGapCells):
	// besides the re-ranking above (off until RerankCandidates is armed by the AD_spaced_base_placement switch), this
	// module owns the HARD building gap findPos applies - cells whose footprint lands within MinBuildingGapCells of
	// an own footprint (MinBuildingGapDefensesCells for defences) are rejected. The gap is live whenever the module
	// is loaded (genericbot); classic has no advisor, so it keeps the old edge-to-edge placement.
	//
	// Refineries keep their own placement owner (the expansion planner's field claim wins — a refinery sited
	// for spacing instead of field reach would undo EX-2). Defense cells keep DefenseCoveragePlanner (DEF-3) —
	// when an IBotDefensePlacementAdvisor is active it already picks, so this advisor is never asked for them.
	[TraitLocation(SystemActors.Player)]
	[Desc("Re-ranks candidate building-placement cells toward maximum distance from existing own buildings, keeping bases open.")]
	public class SpacingAdvisorBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("HARD gap: cells of empty space between a new building's footprint and every own footprint (0 disables).",
			"Live whenever the module is loaded; findPos rejects the cells, an exhausted annulus returns null and retries later.")]
		public readonly int MinBuildingGapCells = 2;

		[Desc("The hard gap for defence placements, so walls and turrets can still form tighter lines.")]
		public readonly int MinBuildingGapDefensesCells = 1;

		[Desc("Arm the re-ranking of findPos' first placeable candidates (the SP-1 part; the hard gap is always on).")]
		public readonly bool RerankCandidates = false;

		[Desc("Cells of clear gap a placement tries to keep to the nearest own building. Candidates inside this gap",
			"are only used when nothing wider places at all.")]
		public readonly int DesiredGapCells = 3;

		[Desc("Extra weight per candidate given to distance from the base anchor, so growth pushes outward",
			"instead of in-filling (percent, 0 = nearest-own-building distance only).")]
		public readonly int OutwardLeanPercent = 25;

		public override object Create(ActorInitializer init) { return new SpacingAdvisorBotModule(init.Self, this); }
	}

	public class SpacingAdvisorBotModule : ConditionalTrait<SpacingAdvisorBotModuleInfo>, IBotPlacementAdvisor
	{
		readonly World world;
		readonly OpenRA.Player player;

		public SpacingAdvisorBotModule(Actor self, SpacingAdvisorBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		bool IBotPlacementAdvisor.IsActive => !IsTraitDisabled;
		bool IBotPlacementAdvisor.RanksCandidates => !IsTraitDisabled && Info.RerankCandidates;
		int IBotPlacementAdvisor.MinBuildingGapCells => Info.MinBuildingGapCells;
		int IBotPlacementAdvisor.MinBuildingGapDefensesCells => Info.MinBuildingGapDefensesCells;

		CPos? IBotPlacementAdvisor.ChooseCell(ActorInfo building, IReadOnlyList<CPos> candidates, Func<CPos, bool> stillPlaceable)
		{
			if (IsTraitDisabled || !Info.RerankCandidates || candidates == null || candidates.Count == 0)
				return null;

			// One enumeration of own occupied cells per placement call — placements are a few per minute,
			// never per-tick, so this stays cheap.
			var ownCells = new List<CPos>();
			long anchorX = 0;
			long anchorY = 0;
			var anchorCount = 0;
			foreach (var a in world.Actors)
			{
				if (a.Owner != player || a.IsDead || !a.IsInWorld || !a.Info.HasTraitInfo<BuildingInfo>())
					continue;

				foreach (var cell in a.OccupiesSpace.OccupiedCells())
					ownCells.Add(cell.Cell);

				anchorX += a.Location.X;
				anchorY += a.Location.Y;
				anchorCount++;
			}

			if (ownCells.Count == 0)
				return null; // nothing to space from — keep the caller's pick

			var anchor = new CPos((int)(anchorX / anchorCount), (int)(anchorY / anchorCount));

			CPos best = candidates[0];
			var bestScore = long.MinValue;
			var sawWide = false;
			var found = false;
			foreach (var cell in candidates)
			{
				if (stillPlaceable != null && !stillPlaceable(cell))
					continue;

				var nearestSq = long.MaxValue;
				foreach (var own in ownCells)
				{
					var d = (cell - own).LengthSquared;
					if (d < nearestSq)
						nearestSq = d;
				}

				// Beyond DesiredGapCells more gap stops mattering — a 10-cell gap scores no higher than 4,
				// otherwise the frontier drifts absurdly far chasing "most open".
				var gap = (long)Math.Min(Math.Sqrt(nearestSq), Info.DesiredGapCells + 1);
				var outward = (cell - anchor).LengthSquared;
				var score = gap * 100 + outward / Math.Max(1, 100 - Info.OutwardLeanPercent);

				if (gap <= Info.DesiredGapCells && sawWide)
					continue;

				if (score > bestScore)
				{
					found = true;
					bestScore = score;
					best = cell;
					sawWide |= gap > Info.DesiredGapCells;
				}
			}

			// Nothing survived the re-check — keep the caller's own pick rather than inventing a cell.
			return found ? best : (CPos?)null;
		}
	}
}
