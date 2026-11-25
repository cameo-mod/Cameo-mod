#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-01 c6 (CONSOLIDATE_01_DESIGN.md 3.1, stage S8, rule R7): the production cycle of the staged planner. One stage, two triggers: the bank at or above the
	// tier line L (switch pb_cycle_production) and SUSTAINED-CASH (switch pb_sustained_cash_production, which also moves the stage above S7: BaseBuildTiersCA). The source is a
	// QUERY: it never writes lastCycledProduction, never resolves PickOrPower (the planner's A5 swaps the power plant in, null-safe) and takes no admission vote
	// (ProductionTypeLimit and the no-space ban are the A2/A3 tests, the reserve is A6). Only the admitted S8 candidate moves the cycle (TickQueuePlanned).
	partial class BaseBuilderQueueManagerCA
	{
		// The production types the cycle offers, fewest copies (standing + in flight) first, then the next name after the last cycled type (CycleProductionCA.Order).
		// The candidate set is OverflowProductionCandidates (types under their limit, not banned for no space) plus the naval yard when the base has water.
		IEnumerable<string> PipelineProductionCycleStage(List<ActorInfo> buildable)
		{
			if (!(CycleOn || SustainedActive) || playerBuildings == null)
				yield break;

			var candidates = OverflowProductionCandidates(buildable, null, false);
			var naval = PipelineNavalCycleCandidate(buildable);
			if (naval != null)
				candidates.Add(naval);

			var counted = new List<(string Name, int Count)>(candidates.Count);
			foreach (var c in candidates)
				counted.Add((c.Name, playerBuildings.Count(a => a.Info.Name == c.Name)
					+ (baseBuilder.BuildingsBeingProduced.TryGetValue(c.Name, out var inProd) ? inProd : 0)));

			foreach (var name in CycleProductionCA.Order(counted, lastCycledProduction))
				yield return name;
		}

		// The naval yard of ChooseCycledProduction (keep in step with it): only with enough water inside the base perimeter, under its building limit and the production-type cap.
		ActorInfo PipelineNavalCycleCandidate(List<ActorInfo> buildable)
		{
			if (waterState != WaterCheck.EnoughWater
				|| !AIUtils.IsAreaAvailable<GivesBuildableArea>(world, player, world.Map, baseBuilder.Info.CheckForWaterRadius, baseBuilder.Info.WaterTerrainTypes))
				return null;

			var naval = GetProducibleBuilding(baseBuilder.Info.NavalProductionTypes, buildable);
			if (naval == null)
				return null;

			var producing = baseBuilder.BuildingsBeingProduced.TryGetValue(naval.Name, out var n) ? n : 0;
			var variants = naval.TraitInfoOrDefault<PlaceBuildingVariantsInfo>()?.Actors ?? [];
			var standing = playerBuildings.Count(a => a.Info.Name == naval.Name || variants.Contains(a.Info.Name));
			return BaseBuilderQueueEvalCA.LimitAdmits(baseBuilder.TryGetBuildingLimit(naval.Name, out var limit), limit,
					playerBuildings.Count(a => a.Info.Name == naval.Name), producing)
				&& BaseBuilderQueueEvalCA.OverflowTypeAdmits(standing, producing, ProductionTypeLimit)
				? naval
				: null;
		}
	}
}
