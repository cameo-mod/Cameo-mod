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
	// CONSOLIDATE-01 (CONSOLIDATE_01_DESIGN.md 3.1): the world-dependent stage sources of the staged planner, one owner per commit. c4: S2 expansion
	// refinery + S2b planner wants, S6 passive income + silo, S7 refinery per field. BaseBuildSnapshot.StageSource is bound to PipelineStageNames in
	// PipelineSnapshot; the planner reads it lazily, only when it reaches the stage. Every source here is a QUERY (no admission side effect): the
	// admitted start, its claims and its records belong to the planner's caller (AdmitPlanned, PipelineTryClaim). ChooseWantedRefinery keeps its
	// existing observation clocks (refinery gate rise / first-stood / yield), which the old ladder advanced on every sweep too.
	partial class BaseBuilderQueueManagerCA
	{
		bool PassiveIncomeOn => baseBuilder.PassiveIncomeArmed;

		IEnumerable<string> PipelineStageNames(BaseBuildStage stage, ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power,
			IBotExpansionTargetProvider law)
		{
			switch (stage)
			{
				case BaseBuildStage.S2ExpansionRefinery: return PipelineExpansionStage(queue, buildable, power, law);
				case BaseBuildStage.S6PassiveIncome: return PipelinePassiveIncomeStage(queue, buildable, power, law);
				case BaseBuildStage.S7RefineryPerField: return PipelineRefineryStage(queue, buildable, power);
				default: return null;
			}
		}

		// S2: a yard with no refinery gets its refinery first (designer: the expansion refinery is the very next item, exempt from the interval), through the
		// existing wanted-refinery helper (refinery law, safety over all claims, max, economy gate, bank-free). S2b: the planner's own wants after it
		// (the due MCV's prerequisite, the crawl front defence, the crawl link; claims are taken at admission, A7). Types in flight are not asked again.
		IEnumerable<string> PipelineExpansionStage(ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power, IBotExpansionTargetProvider law)
		{
			if (baseBuilder.YardWithoutRefinery())
			{
				var refinery = ChooseWantedRefinery(queue, buildable, power);
				if (refinery != null)
					yield return refinery.Name;
			}

			if (law == null)
				yield break;

			foreach (var want in PlannerWantsCA.Order(law.WantedMcvPrerequisite, law.WantedFrontDefence, law.WantedLinkBuilding))
			{
				var pick = PipelinePlannerWant(want, buildable, power);
				if (pick != null)
					yield return pick.Name;
			}
		}

		// The planner want as the old ladder's PlannerWantPick: skipped while the type is in production, else the producible building, power-resolved.
		ActorInfo PipelinePlannerWant(string want, List<ActorInfo> buildable, ActorInfo power)
		{
			if (want == null || (baseBuilder.BuildingsBeingProduced?.ContainsKey(want) ?? false))
				return null;

			var pick = GetProducibleBuilding(new HashSet<string> { want }, buildable, a => 0);
			return pick == null ? null : BaseBuilderQueueEvalCA.PickOrPower(pick, power, HasSufficientPowerForActor);
		}

		// S6 (row bb_passive_income): the cash tricklers up to their limit (PassiveIncomeCA.Order), then a silo when storage is nearly full
		// (RefineryLawSilo, none already in production). Reserve 0, so a poor bank still gets its income buildings (R8, R9).
		IEnumerable<string> PipelinePassiveIncomeStage(ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power, IBotExpansionTargetProvider law)
		{
			if (!PassiveIncomeOn)
				yield break;

			var tricklers = new List<(string Name, int Cost, int Count, int Limit)>();
			foreach (var info in buildable)
			{
				if (!info.HasTraitInfo<CashTricklerInfo>())
					continue;

				tricklers.Add((info.Name, queue.GetProductionCost(info), OwnedOrQueuedCount([info.Name]),
					baseBuilder.TryGetBuildingLimit(info.Name, out var limit) ? limit : -1));
			}

			foreach (var name in PassiveIncomeCA.Order(tricklers))
				yield return name;

			var siloInProduction = baseBuilder.BuildingsBeingProduced?.Keys.Any(baseBuilder.Info.SiloTypes.Contains) ?? false;
			if (!RefineryLawSilo.Wanted(law != null, playerResources.Resources, playerResources.ResourceCapacity, siloInProduction))
				yield break;

			var silo = GetProducibleBuilding(baseBuilder.Info.SiloTypes, buildable);
			var siloPick = BaseBuilderQueueEvalCA.PickOrPower(silo, power, HasSufficientPowerForActor);
			if (siloPick != null)
				yield return siloPick.Name;
		}

		// S7: the refinery per field (R4): the same wanted-refinery helper, whose law anchors, per-field cap with loss lock (rf_field_gate), spacing
		// ref1 -> war factory -> ref2, interval and the first-copy yield (rf_economy_gate) stay as they are. A refinery S2 or S3 already offered and A
		// refused is skipped by the planner's failed set (no second test).
		IEnumerable<string> PipelineRefineryStage(ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power)
		{
			var refinery = ChooseWantedRefinery(queue, buildable, power);
			if (refinery != null)
				yield return refinery.Name;
		}
	}
}
