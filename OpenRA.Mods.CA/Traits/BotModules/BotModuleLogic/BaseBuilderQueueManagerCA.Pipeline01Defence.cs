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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-01 c5 (CONSOLIDATE_01_DESIGN.md 3.1): the defence stages of the staged planner. S4 = the anti-rush base set (rows bb_antirush_set), S9 = the extra
	// defences after a seen threat (row bb_extra_defence), and the threat-scaled defence cap (DefenceCapCA, row af2_defence_cap) that bounds S9 and the defences of the S10
	// roll. The sources are QUERIES (a name list, no claim, no record, no marker): the admitted start's side effects stay in AdmitPlanned, and RATIO-2X still records the
	// start there. No PickOrPower result is dereferenced here (the V07 crash: PickOrPower returns null with no power plant buildable): A5 of the planner swaps the power
	// plant in and is itself null-safe.
	partial class BaseBuilderQueueManagerCA
	{
		// The planner falls through at most this many anti-rush candidates (the K bound must not be spent on one stage).
		const int AntiRushStageMaxCandidates = 3;

		bool AntiRushSetOn => baseBuilder.AntiRushSetArmed;

		bool ExtraDefenceOn => baseBuilder.ExtraDefenceArmed;

		// S4 (R3): while fewer than the tier's N ground defences are owned or queued and a production building stands, the ground defences the bank can pay now,
		// the type with the fewest copies first, then the cheapest (AntiRushCA.Order). Reserve 0 (stage S4 < S8), no need gate, no seen-threat rule, no RATIO-2X (D1).
		// Walls, limits and the no-space ban are filtered here as in ChooseAntiRushBase; A1-A6 test the rest and the planner falls through to the next type.
		IEnumerable<string> PipelineAntiRushStage(ProductionQueue queue, List<ActorInfo> buildable)
		{
			if (!AntiRushSetOn || playerBuildings == null)
				yield break;

			var b = baseBuilder.Info;
			var have = playerBuildings.Count(a => IsGroundDefence(a.Info));
			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (world.Map.Rules.Actors.TryGetValue(kv.Key, out var queued) && IsGroundDefence(queued))
					have += kv.Value;

			var size = AntiRushCA.SetSize(botLimits != null ? botLimits.Info.ArmyShareTargetPermille : -1, b.AntiRushBaseMin, b.AntiRushBaseMax);
			if (!AntiRushCA.Wants(playerBuildings.Any(a => b.ProductionTypes.Contains(a.Info.Name)), have, size))
				yield break;

			var bank = ProdBusyCA.SweepBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep);
			var candidates = new List<(string Name, int Cost, int OwnedOrQueued)>();
			foreach (var info in buildable)
			{
				if (!IsGroundDefence(info) || NoSpaceBanned(info.Name) || queue.GetProductionCost(info) > bank)
					continue;

				var count = OwnedOrQueuedCount([info.Name]);
				if (baseBuilder.TryGetBuildingLimit(info.Name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(count, limit))
					continue;

				candidates.Add((info.Name, ValuedCost(info), count));
			}

			foreach (var name in AntiRushCA.Order(candidates, AntiRushStageMaxCandidates))
				yield return name;
		}

		// S9 (R6): the armed defences of BuildingFractions that this queue can build, ordinal-sorted then shuffled with the synced bot random (lazily, one draw per entry
		// reached); the stage caps (need gate, defence cap, RATIO-2X with FLOAT, air intel) are StageAdmits, so a refused defence costs no feasibility test. Defence
		// queue only, and nothing at all while the cap is reached.
		IEnumerable<string> PipelineExtraDefenceStage(List<ActorInfo> buildable)
		{
			if (!ExtraDefenceOn || baseBuilder.Info.BuildingFractions == null || !baseBuilder.Info.DefenseQueues.Contains(Category) || PipelineDefenceCapHolds())
				yield break;

			var names = new HashSet<string>(buildable.Select(a => a.Name), StringComparer.Ordinal);
			var defences = baseBuilder.Info.BuildingFractions
				.Where(kv => names.Contains(kv.Key) && world.Map.Rules.Actors.TryGetValue(kv.Key, out var info) && IsArmedDefenceInfo(info))
				.OrderBy(kv => kv.Key, StringComparer.Ordinal);
			foreach (var kv in defences.Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA))))
				yield return kv.Key;
		}

		// The stage caps that need the world, per stage: S4 none (D1: RATIO-2X exempt, no need gate; the cap counts the set but never vetoes it), S5 buildable and not a yard or refinery, S8 the admission votes (Pipeline01Cycle.cs), S9 as above plus the admission votes, S10 the old
		// caps plus the defence cap for a defence (so the cap holds whichever stage reaches it, and after the af02 votes are retired). Other stages: no cap.
		bool PipelineStageAdmits(ProductionQueue queue, HashSet<string> buildableNames, BaseBuildCandidate candidate)
		{
			switch (candidate.Stage)
			{
				case BaseBuildStage.S5CoreOnce:
					// No army-share, RATIO or need cap: S5 is never vetoed by later rules. Only a type this player cannot build (a doctrine alternative) or a
					// type with a stage of its own (yard, refinery) is skipped, at no test cost.
					return buildableNames.Contains(candidate.Name)
						&& !baseBuilder.Info.ConstructionYardTypes.Contains(candidate.Name)
						&& (!baseBuilder.Info.RefineryTypes.Contains(candidate.Name) || (baseBuilder.CaptureUnlockArmed && captureUnlockOffered.Contains(candidate.Name))); // CAPTURE-TECH-01: an unlock refinery
				case BaseBuildStage.S8ProductionCycle:
					return PipelineCycleAdmits(queue, candidate.Name);
				case BaseBuildStage.S9ExtraDefence:
					return PipelineExtraDefenceAdmits(queue, buildableNames, candidate.Name);
				case BaseBuildStage.S10Fractions:
					return PipelineFractionCapsAdmit(queue, buildableNames, candidate.Name)
						&& !(world.Map.Rules.Actors.TryGetValue(candidate.Name, out var info) && IsArmedDefenceInfo(info) && PipelineDefenceCapHolds());
				default:
					return true;
			}
		}

		// The last cap is the admission votes (the pause votes and the mobile spend floor, VotesAdmit), as the old roll's defence picks ran them (class General).
		bool PipelineExtraDefenceAdmits(ProductionQueue queue, HashSet<string> buildableNames, string name)
		{
			if (!buildableNames.Contains(name) || !world.Map.Rules.Actors.TryGetValue(name, out var info) || !IsArmedDefenceInfo(info))
				return false;

			if (!BaseBuilderQueueEvalCA.AirDefenceAdmits(baseBuilder.IsGatedAirDefence(name), baseBuilder.DedicatedAntiAirAllowed))
				return false;

			if (DefenceNeedOn && BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(info), DefenceThreatSeen()))
				return false;

			if (PipelineDefenceCapHolds())
				return false;

			return !MobileStaticRatioCA.Holds(RatioGate(info, out _))
				&& VotesAdmit(queue, info, RecoverySpendPolicyCA.Admission.General);
		}

		static bool IsArmedDefenceInfo(ActorInfo info) => DefenceCapCA.IsArmedDefence(info.HasTraitInfo<BuildingInfo>(), info.HasTraitInfo<AttackBaseInfo>());

		// The armed defences owned (standing) or in flight (queued, C1 recount; an admitted start counts at once with every producer served): what the cap counts,
		// the S4 set included.
		int PipelineArmedDefenceCount()
		{
			var n = 0;
			foreach (var kv in pipelineOwned)
				if (world.Map.Rules.Actors.TryGetValue(kv.Key, out var info) && IsArmedDefenceInfo(info))
					n += kv.Value;

			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (world.Map.Rules.Actors.TryGetValue(kv.Key, out var queued) && IsArmedDefenceInfo(queued))
					n += kv.Value;

			return n;
		}

		bool PipelineDefenceCapHolds() => baseBuilder.DefenceCapArmed && DefenceCapCA.Holds(PipelineDefenceCap(), PipelineArmedDefenceCount());

		int pipelineCapTick = -1;
		int pipelineCapValue = -1;
		IBotCurrentPersonality pipelinePersonality;
		bool pipelinePersonalityResolved;

		// base + floor(perThreat x intensity), turtle bonus early to mid game; the threat is read once per tick (the cap moves only with the analysis).
		int PipelineDefenceCap()
		{
			var now = world.WorldTick;
			if (pipelineCapTick == now)
				return pipelineCapValue;

			var b = baseBuilder.Info;
			if (!defenceThreatResolved)
			{
				defenceThreatAnalysis = player.PlayerActor.TraitsImplementing<IBotThreatAnalysis>().FirstOrDefault();
				defenceThreatResolved = true;
			}

			var intensity = 0.0;
			if (defenceThreatAnalysis != null)
			{
				var saturation = Math.Max(100, b.DefenceThreatSaturationPercent) / 100f;
				intensity = DefenceCapCA.Intensity(defenceThreatAnalysis.GetThreatIntensity(BotThreatRoles.AntiAir, saturation),
					defenceThreatAnalysis.GetThreatIntensity(BotThreatRoles.AntiArmour, saturation),
					defenceThreatAnalysis.GetThreatIntensity(BotThreatRoles.AntiInfantry, saturation));
			}

			if (!pipelinePersonalityResolved)
			{
				pipelinePersonality = player.PlayerActor.TraitsImplementing<IBotCurrentPersonality>().FirstOrDefault();
				pipelinePersonalityResolved = true;
			}

			var turtle = DefenceCapCA.TurtleBonus(now, b.TurtleDefenceUntilTick, pipelinePersonality?.CurrentPersonality);
			pipelineCapValue = DefenceCapCA.Cap(b.DefenceCapBase, b.DefenceCapPerThreat, intensity, turtle, b.TurtleDefenceCapPercent);
			pipelineCapTick = now;
			return pipelineCapValue;
		}
	}
}
