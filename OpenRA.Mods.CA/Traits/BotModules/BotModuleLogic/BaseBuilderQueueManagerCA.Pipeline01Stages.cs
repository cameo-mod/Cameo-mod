#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
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

		// CAPTURE-TECH-01: the types the last S5 sweep offered as capture unlocks (PipelineStageAdmits lets an unlock refinery through).
		readonly HashSet<string> captureUnlockOffered = new(StringComparer.Ordinal);

		IEnumerable<string> PipelineStageNames(BaseBuildStage stage, ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power,
			IBotExpansionTargetProvider law, BaseBuildSnapshot snapshot)
		{
			switch (stage)
			{
				case BaseBuildStage.S2ExpansionRefinery: return PipelineExpansionStage(queue, buildable, power, law);
				case BaseBuildStage.S4AntiRush: return PipelineAntiRushStage(queue, buildable);
				case BaseBuildStage.S5CoreOnce: return PipelineCoreOnceStage(queue, buildable, snapshot);
				case BaseBuildStage.S6PassiveIncome: return PipelinePassiveIncomeStage(queue, buildable, power, law);
				case BaseBuildStage.S7RefineryPerField: return PipelineRefineryStage(queue, buildable, power);
				case BaseBuildStage.S8ProductionCycle: return PipelineProductionCycleStage(buildable);
				case BaseBuildStage.S9ExtraDefence: return PipelineExtraDefenceStage(buildable);
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
				else
					RefineryHeldObserved(RefineryHoldGate(buildable)); // BUILD-ALL-B, DEV only
			}
			else if (ZoneDemandAtS2(law))
			{
				// REF-S2-01 (row bb_refinery_s2): unmet refinery zone demand on the building queue, ahead of S3-S5; the helper keeps the law, safety, max and economy gate.
				var refinery = ChooseWantedRefinery(queue, buildable, power);
				if (refinery != null)
					yield return refinery.Name;
				else
					S2RefineryHeldObserved(buildable); // ref_s2_hold_log, DEV only
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

		// S5 (c3c): every listed building once. The first pass is the ORIGINAL BuildingFractions roll on first copies (BaseBuildPlanCA.RollCandidates: count 0,
		// weight > 0, the same shuffle as S10), so the fraction weights decide. This method is only the fallback for what the roll cannot offer: buildable types
		// with owned + in flight = 0 that are a BuildingFractions entry with weight 0, or a first-copy kind (FirstCopyKind: no wall, defence, fake or proxy) that
		// is not listed. Ordered by tech depth, cost, name (CoreOnceCA.Order). Nothing vetoes S5 later; only step A applies (BuildingDelays on every stage, X14).
		// The pipeline ignores TechChainTypes: the derived depth replaces it. The defence queue gets nothing (S4 and S9 own it). Construction yards and refineries
		// keep their own stages (S2, S7). A doctrine alternative this player cannot build is not in queue.BuildableItems(), so it is absent here, and
		// PipelineStageAdmits skips it at no test cost.
		IEnumerable<string> PipelineCoreOnceStage(ProductionQueue queue, List<ActorInfo> buildable, BaseBuildSnapshot snapshot)
		{
			var b = baseBuilder.Info;
			if (snapshot.DefenceQueue)
				return Array.Empty<string>();

			var depths = CoreDepths();
			var inputs = new List<CoreOnceInput>(buildable.Count);
			var captureOn = baseBuilder.CaptureUnlockArmed;
			captureUnlockOffered.Clear();
			Dictionary<string, bool> lacksCache = null;
			foreach (var item in buildable)
			{
				var name = item.Name;
				var weight = -1;
				if (b.BuildingFractions != null && b.BuildingFractions.TryGetValue(name, out var listed))
					weight = Math.Max(0, listed);

				var firstCopy = FirstCopyKind(item);
				var unlock = false;
				if (!firstCopy && captureOn)
				{
					lacksCache ??= new Dictionary<string, bool>(StringComparer.Ordinal);
					unlock = CaptureUnlockType(item, snapshot, lacksCache);
					if (unlock)
						captureUnlockOffered.Add(name);
				}

				if (!firstCopy && !unlock)
					continue;

				inputs.Add(new CoreOnceInput(
					name,
					Buildable: true,
					weight,
					firstCopy || unlock,
					OwnRuleStage: b.ConstructionYardTypes.Contains(name) || (b.RefineryTypes.Contains(name) && !unlock),
					Count: BaseBuildPlanCA.CountOf(snapshot, name),
					Depth: depths.TryGetValue(name, out var depth) ? depth : 0,
					Cost: queue.GetProductionCost(item)));
			}

			return CoreOnceCA.Order(inputs);
		}

		// CAPTURE-TECH-01 (cs_capture_unlock): an unowned power plant, silo or refinery type of another faction that gives a prerequisite this player lacks, which gates
		// another building type, while the player already owns one of that category (CaptureUnlockCA.IsUnlock). The TechTree is asked only for these candidate
		// types, once per token per sweep (lacksCache).
		bool CaptureUnlockType(ActorInfo item, BaseBuildSnapshot snapshot, Dictionary<string, bool> lacksCache)
		{
			var b = baseBuilder.Info;
			var name = item.Name;
			var category = b.PowerTypes.Contains(name) ? b.PowerTypes : b.SiloTypes.Contains(name) ? b.SiloTypes : b.RefineryTypes.Contains(name) ? b.RefineryTypes : null;
			if (category == null || BaseBuildPlanCA.CountOf(snapshot, name) > 0)
				return false;

			var owned = 0;
			foreach (var other in category)
				owned += BaseBuildPlanCA.CountOf(snapshot, other);

			if (owned <= 0)
				return false;

			var maps = CaptureMaps();
			if (!maps.ProvidedBy.TryGetValue(name, out var provided))
				return false;

			var tree = player.PlayerActor.TraitOrDefault<TechTree>();
			if (tree == null)
				return false;

			return CaptureUnlockCA.IsUnlock(provided,
				token =>
				{
					if (!lacksCache.TryGetValue(token, out var lacks))
						lacksCache[token] = lacks = !tree.HasPrerequisites(new[] { token });

					return lacks;
				},
				token => maps.GatesOther(token, name),
				owned);
		}

		// The provided tokens and gates of every building type, no faction filter, once per player (the Buildable prerequisites against every ProvidesPrerequisite).
		CaptureUnlockMapsCA CaptureMaps()
		{
			if (baseBuilder.CaptureUnlockMaps != null)
				return baseBuilder.CaptureUnlockMaps;

			var prereqsOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			var providesOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			foreach (var info in world.Map.Rules.Actors.Values.OrderBy(a => a.Name, StringComparer.Ordinal))
			{
				if (!info.HasTraitInfo<BuildingInfo>())
					continue;

				var raws = new List<string>();
				foreach (var buildableInfo in info.TraitInfos<BuildableInfo>())
					raws.AddRange(buildableInfo.Prerequisites);

				prereqsOf[info.Name] = raws;
				providesOf[info.Name] = info.TraitInfos<ProvidesPrerequisiteInfo>().Select(p => p.Prerequisite ?? info.Name).ToList();
			}

			return baseBuilder.CaptureUnlockMaps = CaptureUnlockMapsCA.Build(prereqsOf, providesOf);
		}

		// The tech depth of every building type, computed once per player from the rules (not the world): Buildable prerequisites against the
		// ProvidesPrerequisite traits of the building types this player's faction can get. ~ strips, ! negations add no depth (TechDepthCA.Positive).
		IReadOnlyDictionary<string, int> CoreDepths()
		{
			if (baseBuilder.CoreTechDepth != null)
				return baseBuilder.CoreTechDepth;

			var faction = player.Faction.InternalName;
			var prereqsOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			var providersOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			foreach (var info in world.Map.Rules.Actors.Values.OrderBy(a => a.Name, StringComparer.Ordinal))
			{
				if (!info.HasTraitInfo<BuildingInfo>())
					continue;

				var raws = new List<string>();
				foreach (var buildable in info.TraitInfos<BuildableInfo>())
					raws.AddRange(buildable.Prerequisites);

				prereqsOf[info.Name] = raws;
				foreach (var provides in info.TraitInfos<ProvidesPrerequisiteInfo>())
				{
					if (provides.Factions.Count > 0 && !provides.Factions.Any(f => string.Equals(f, faction, StringComparison.OrdinalIgnoreCase)))
						continue;

					var key = (provides.Prerequisite ?? info.Name).ToLowerInvariant();
					if (!providersOf.TryGetValue(key, out var list))
						providersOf[key] = list = new List<string>();

					((List<string>)list).Add(info.Name);
				}
			}

			return baseBuilder.CoreTechDepth = TechDepthCA.Compute(prereqsOf, providersOf, baseBuilder.Info.ConstructionYardTypes);
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
		// ChooseWantedRefinery may run twice in one sweep (S2 and S7): its only state is RefineryEconomyHolds' observation clocks (rise on a higher count, first-stood,
		// yield-since), each written only on a change, so the second call in the same tick moves nothing; the old ladder advanced the same clocks on every sweep and queue.
		IEnumerable<string> PipelineRefineryStage(ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power)
		{
			var refinery = ChooseWantedRefinery(queue, buildable, power);
			if (refinery != null)
				yield return refinery.Name;
			else
				RefineryHeldObserved(RefineryHoldGate(buildable)); // BUILD-ALL-B, DEV only
		}

		// BUILD-ALL-B, DEV only (its one caller, RefineryHeldObserved, is compiled out of a release build together with this argument): the first gate of
		// ChooseWantedRefinery that holds, re-asked with the same queries in the same order. The economy gate is not run again (it moves clocks); its
		// verdict of the call just made is read instead.
		string RefineryHoldGate(List<ActorInfo> buildable)
		{
			if (baseBuilder.HasAdequateRefineryCount())
				return "adequate";

			if (SafetyOn && SafetyAllOn && baseBuilder.ExpansionRefineryBlockedByAllUnsafe())
				return "unsafe_all";

			if (SafetyOn && !SafetyAllOn)
			{
				var wantedBy = baseBuilder.ExpansionWantsRefinery();
				var wantedField = wantedBy?.RefineryClaimTarget ?? wantedBy?.ExpansionTarget;
				if (wantedField != null && !baseBuilder.ExpansionRefinerySafe(wantedField.Value))
					return "unsafe_field";
			}

			var refinery = GetProducibleBuilding(baseBuilder.Info.RefineryTypes, buildable);
			if (refinery == null)
				return "not_buildable";

			if (baseBuilder.HasMaxRefineriesFor(refinery))
				return "max_refineries";

			return RefineryEconomyGateOn ? "economy_" + lastRefineryEconomyVerdict : "power";
		}

		// ref_s2_hold_log (REF-GATE-V08F, Opus 11-25, REF_PEAK_RCA_01): the S2 zone-demand branch asked for the refinery and got none; record the gate that held it,
		// as a bb_refinery_held row whose gate carries an "s2_" prefix (S7 writes the same row unprefixed in the same pass, so the two stay apart). DEV only: compiled
		// out of a release build with its argument. Off (ablated) = no S2 row, as in V08E.
		AblationLatch ablateS2HoldLog;

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void S2RefineryHeldObserved(List<ActorInfo> buildable)
		{
			if (ablateS2HoldLog.Off(player, AblationIds.RefS2HoldLog))
				return;

			RefineryHeldObserved("s2_" + RefineryHoldGate(buildable));
		}
	}
}
