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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-01 c2 (row bb_pipeline_01, CONSOLIDATE_01_DESIGN.md 3.1): the queue manager side of the staged planner. TickQueue enters
	// TickQueuePlanned only while the switch is armed (genericbot with BbPipeline01, not ablated); off, nothing here runs and the old ladder
	// (ChooseBuildingToBuild + the post-pick vetoes) is unchanged. The planner (BaseBuildPlanCA) is pure; this file builds its snapshot, implements
	// the feasibility tests A1-A7 with the existing checks, and performs the admitted start's side effects (the same tail as TickQueue's).
	partial class BaseBuilderQueueManagerCA
	{
		bool PipelineOn => baseBuilder.Pipeline01Armed;

		PipelineFeasibility pipelineFeasibility;

		// Owned (standing) count by actor name, rebuilt once per sweep (playerBuildings is re-read at every sweep start).
		readonly Dictionary<string, int> pipelineOwned = new();
		Actor[] pipelineOwnedFor;

		bool TickQueuePlanned(IBot bot, ProductionQueue queue)
		{
			// A6 replaces the building cash floor and its starvation clock, and S1-S7 ride at reserve 0 by construction: no clock run and no
			// exemption marker is kept on this path (RatioGate reads antiRushPickName, so it is cleared here as ChooseBuildingToBuild does).
			cashClock.Reset(queue.Actor.ActorID);
			sustainedPickName = null;
			openingPickName = null;
			antiRushPickName = null;

			// B03: without pb_all_building_producers one start per category per sweep. A sweep-level limit, not a candidate veto.
			if (ProdBusyCA.QueuedThisSweepBlocks(AllProducersOn, itemQueuedThisTick))
				return false;

			var buildable = queue.BuildableItems().ToList();
			var power = GetProducibleBuilding(baseBuilder.Info.PowerTypes, buildable,
				a => a.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount));
			var law = baseBuilder.RefineryLawProvider();
			var snapshot = PipelineSnapshot(queue, buildable, power, law);

			pipelineFeasibility ??= new PipelineFeasibility(this);
			pipelineFeasibility.Begin(queue, buildable, power, law, snapshot);
			var pick = BaseBuildPlanCA.Plan(snapshot, pipelineFeasibility, BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));
			if (pick.IsNone)
				return false;

			var item = world.Map.Rules.Actors[pick.Name];
			AIUtils.BotDebug("{0} decided to build {1}: pipeline {2} (candidate {3}, {4} tests)", queue.Actor.Owner, item.Name, pick.Stage, pick.Candidate, pick.Tests);
			AdmitPlanned(bot, queue, item);
			return true;
		}

		// Step 0. Bank and in-flight counts are per producer (an earlier start of this sweep is already in them); the owned counts once per sweep.
		BaseBuildSnapshot PipelineSnapshot(ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power, IBotExpansionTargetProvider law)
		{
			var b = baseBuilder.Info;
			var now = world.WorldTick;
			if (!ReferenceEquals(pipelineOwnedFor, playerBuildings))
			{
				pipelineOwned.Clear();
				foreach (var a in playerBuildings)
					pipelineOwned[a.Info.Name] = (pipelineOwned.TryGetValue(a.Info.Name, out var n) ? n : 0) + 1;

				pipelineOwnedFor = playerBuildings;
			}

			if (!staticValuesResolved)
			{
				staticValues = player.PlayerActor.TraitsImplementing<IBotMobileStaticValues>().FirstOrDefault();
				staticValuesResolved = true;
			}

			var armyValues = staticValues != null && MobileStaticRatioCA.MeasureUsable(staticValues.HasSnapshot, staticValues.SnapshotTick, now);
			var s = new BaseBuildSnapshot
			{
				WorldTick = now,
				DefenceQueue = b.DefenseQueues.Contains(Category),
				Bank = ProdBusyCA.SweepBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep),
				ProductionCashReserve = botLimits != null ? botLimits.Info.ProductionCashReserve : -1,
				TierLine = TierLine,
				SustainedActive = SustainedActive,
				DefenceThreatSeen = DefenceThreatSeen(),
				HasArmyValues = armyValues,
				MobileValue = armyValues ? staticValues.MobileValue : 0,
				StaticDefenceValue = armyValues ? StaticDefenceValue(now) : 0,
				Buildable = buildable.Select(a => a.Name).ToList(),
				HasPowerManager = playerPower != null,
				ExcessPower = playerPower?.ExcessPower ?? 0,
				PowerMargin = minimumExcessPower,
				PowerType = power?.Name,
				PowerTypeAmount = power == null ? 0 : power.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount),
				Owned = pipelineOwned,
				InFlight = baseBuilder.BuildingsBeingProduced,
				VariantsOf = PipelineVariantsOf,
				Fractions = b.BuildingFractions,
				BuildingCount = playerBuildings.Length,
				FractionMilli = FractionMilli,
				IntervalActive = name => b.BuildingIntervals != null && b.BuildingIntervals.ContainsKey(name) && activeBuildingIntervals.ContainsKey(name),
			};

			// S3: the same gate as ChooseOpeningStandard (switch, building queue, before OpeningStandardMaxTicks, one yard, no front defence want).
			if (OpeningStepOn && !s.DefenceQueue && now < b.OpeningStandardMaxTicks
				&& OwnedOrQueuedCount(b.ConstructionYardTypes) <= 1 && !(law != null && law.WantedFrontDefence != null))
			{
				var airRole = new HashSet<string>(b.ProductionTypes.Except(b.BarracksTypes).Except(b.VehiclesFactoryTypes).Except(b.NavalProductionTypes));
				s.OpeningActive = true;
				s.OpeningOrder = BaseFix01CA.StandardOrder(b.OpeningSecondBarracksFirstFactions.Contains(player.Faction.InternalName));
				s.OpeningStatus = role => PipelineOpeningStatus(role, queue, buildable, power, airRole);
				s.OpeningStuck = PipelineOpeningStuck;
				s.OpeningSatisfied = (role, required) => openingStepSince.Remove((role, required));
			}

			return s;
		}

		IReadOnlyList<string> PipelineVariantsOf(string name)
		{
			var variants = world.Map.Rules.Actors.TryGetValue(name, out var info) ? info.TraitInfoOrDefault<PlaceBuildingVariantsInfo>() : null;
			if (variants == null || variants.Actors.IsDefaultOrEmpty)
				return null;

			return variants.Actors;
		}

		// S3 role status: the same role -> pick mapping as ChooseOpeningStandard's Status, without the admission votes (A decides instead).
		(string Pick, int Have) PipelineOpeningStatus(OpeningRole role, ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power, HashSet<string> airRole)
		{
			var b = baseBuilder.Info;
			switch (role)
			{
				case OpeningRole.Power:
					return (power?.Name, OwnedOrQueuedCount(b.PowerTypes));
				case OpeningRole.Barracks:
					return (OpeningCheapest(queue, buildable, a => b.BarracksTypes.Contains(a.Name), false)?.Name, OwnedOrQueuedCount(b.BarracksTypes));
				case OpeningRole.Refinery:
					// The wanted-refinery rung's pick (refinery law, safety, max and economy gate all apply), as the old slot test.
					return (ChooseWantedRefinery(queue, buildable, power)?.Name, OwnedOrQueuedCount(b.RefineryTypes));
				case OpeningRole.WarFactory:
					return (OpeningCheapest(queue, buildable, a => b.VehiclesFactoryTypes.Contains(a.Name), false)?.Name, OwnedOrQueuedCount(b.VehiclesFactoryTypes));
				case OpeningRole.Radar:
					return (OpeningCheapest(queue, GetRadarProducibles(buildable), a => true, true)?.Name, OwnedRadarProviders());
				case OpeningRole.Airfield:
					return (OpeningCheapest(queue, buildable, a => airRole.Contains(a.Name), false)?.Name, OwnedOrQueuedCount(airRole));
				case OpeningRole.TechCenter:
					return (OpeningCheapest(queue, buildable, a => b.TechChainTypes.Contains(a.Name), true)?.Name, OwnedOrQueuedCount(b.TechChainTypes));
				default:
					var have = playerBuildings.Count(a => a.Info.HasTraitInfo<CashTricklerInfo>())
						+ baseBuilder.BuildingsBeingProduced.Count(kv => kv.Value > 0 && world.Map.Rules.Actors.TryGetValue(kv.Key, out var info) && info.HasTraitInfo<CashTricklerInfo>());
					return (OpeningCheapest(queue, buildable, a => a.HasTraitInfo<CashTricklerInfo>(), true)?.Name, have);
			}
		}

		// ChooseOpeningStandard's Stuck, on the same openingStepSince ledger: cumulative since the step first became the next step.
		bool PipelineOpeningStuck(OpeningRole role, int required)
		{
			var now = world.WorldTick;
			if (!openingStepSince.TryGetValue((role, required), out var since))
			{
				openingStepSince[(role, required)] = now;
				return false;
			}

			return now - since > baseBuilder.Info.OpeningStepStuckTicks;
		}

		// S10 stage caps that need the world: the old roll's in-loop filters (air intel, refinery safety, defence need, RATIO-2X) and the
		// army-share/reserve votes as a cap instead of a post-pick veto. An entry this queue cannot build is no candidate of this queue.
		bool PipelineFractionCapsAdmit(ProductionQueue queue, HashSet<string> buildableNames, string name)
		{
			if (!buildableNames.Contains(name) || !world.Map.Rules.Actors.TryGetValue(name, out var info))
				return false;

			if (!BaseBuilderQueueEvalCA.AirDefenceAdmits(baseBuilder.IsGatedAirDefence(name), baseBuilder.DedicatedAntiAirAllowed))
				return false;

			if (SafetyOn && SafetyAllOn && baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.ExpansionRefineryBlockedByAllUnsafe())
				return false;

			if (DefenceNeedOn && BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(info), DefenceThreatSeen()))
				return false;

			if (MobileStaticRatioCA.Holds(RatioGate(info, out _)))
				return false;

			return VotesAdmit(queue, info, RecoverySpendPolicyCA.Admission.General);
		}

		// A1-A4 for one type: buildable here, BuildingLimits / ProductionTypeLimit / refinery max (owned + variants + in flight), water and the
		// no-space ban, BuildingDelays (every stage).
		bool PipelineBasicFeasible(HashSet<string> buildableNames, BaseBuildSnapshot s, ActorInfo info)
		{
			var name = info.Name;
			if (!buildableNames.Contains(name))
				return false;

			var count = BaseBuildPlanCA.CountOf(s, name);
			if (botLimits != null && baseBuilder.Info.ProductionTypes.Contains(name) && BaseBuilderQueueEvalCA.LimitReached(count, ProductionTypeLimit))
				return false;

			if (baseBuilder.TryGetBuildingLimit(name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(count, limit))
				return false;

			if (baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.HasMaxRefineriesFor(info))
				return false;

			if (NavalNeedsWater(info) || NoSpaceBanned(name))
				return false;

			return !BuildingStillDelayed(name);
		}

		// A1-A6, side-effect free. A5: a start that would leave power short builds the power plant instead (the plant passes A1-A4 itself);
		// with no plant that gives power the candidate goes as the fraction roll did.
		bool PipelineFeasible(ProductionQueue queue, HashSet<string> buildableNames, ActorInfo power, BaseBuildSnapshot s, BaseBuildCandidate c, out string admitted)
		{
			admitted = null;
			if (!world.Map.Rules.Actors.TryGetValue(c.Name, out var info) || !PipelineBasicFeasible(buildableNames, s, info))
				return false;

			var start = info;
			if (!HasSufficientPowerForActor(info) && power != null && power != info
				&& power.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount) > 0)
			{
				if (!PipelineBasicFeasible(buildableNames, s, power))
					return false;

				start = power;
			}

			if (!BaseBuildPlanCA.Affordable(s.Bank, queue.GetProductionCost(start), BaseBuildPlanCA.ReserveFor(c.Stage, s.ProductionCashReserve)))
				return false;

			// ECON-A-FIX (R5) exclusivity, a pure query: never duplicate a demand-bound name on this producer.
			if (baseBuilder.ExpansionPrebuildEnabled && baseBuilder.DemandForQueuedItem(start.Name, queue.Actor) != null)
				return false;

			admitted = start.Name;
			return true;
		}

		// A7: the atomic claims of TickQueue (crawl link, front defence), taken only for the candidate the planner is about to admit. A refused
		// claim holds nothing (TryClaim contract). The demand refinery reservation does not apply: the pipeline makes no demand picks.
		bool PipelineTryClaim(ProductionQueue queue, IBotExpansionTargetProvider law, string admitted)
		{
			if (law == null)
				return true;

			var info = world.Map.Rules.Actors[admitted];
			if (admitted == law.WantedLinkBuilding && !law.TryClaimCrawlLink(queue.Actor, admitted, queue.GetProductionCost(info)))
				return false;

			return admitted != law.WantedFrontDefence || law.TryClaimFrontDefence(queue.Actor, admitted, queue.GetProductionCost(info));
		}

		// RATIO-2X-01: whether an admitted start is a static defence the tracker records (RatioGate's isDefence, without a verdict).
		bool PipelineRatioRecords(ActorInfo item)
		{
			if (!RatioActive())
				return false;

			if (!staticValuesResolved)
			{
				staticValues = player.PlayerActor.TraitsImplementing<IBotMobileStaticValues>().FirstOrDefault();
				staticValuesResolved = true;
			}

			return staticValues != null && staticValues.IsStaticDefence(item);
		}

		// The admitted start: the same tail as TickQueue's admission (order, storage observation, ratio record, share bookkeeping, producer order,
		// sweep cost, in-flight count, interval). The only place the planned path writes state besides the A7 claim. Keep in step with TickQueue.
		void AdmitPlanned(IBot bot, ProductionQueue queue, ActorInfo item)
		{
			pauseBuilding ??= player.PlayerActor.TraitsImplementing<IBotRequestPauseBuildingProduction>().ToArray();
			var cost = queue.GetProductionCost(item);
			var ratioDefence = PipelineRatioRecords(item);

			baseBuilder.RecordOpeningStructureQueued(queue, item);
			baseBuilder.BuildOrderKnobs?.NotifyQueued(item.Name);

			var storageReservation = baseBuilder.StorageAdmissionObserving
				? baseBuilder.StorageObservation.ObserveEmitting(world, playerResources, queue, item.Name, cost, baseBuilder.Info)
				: null;
			var produceOrder = Order.StartProduction(queue.Actor, item.Name, 1);
			if (storageReservation == null || !(bot is IBotOrderTracking tracking && tracking.QueueOrderTracked(produceOrder, storageReservation)))
				bot.QueueOrder(produceOrder);

			if (storageReservation != null)
				baseBuilder.StorageObservation.NoteEmitted(storageReservation, world.WorldTick);

			queuedAt[queue.Actor.ActorID] = (item.Name, world.WorldTick);

			if (ratioDefence)
				baseBuilder.StaticRatio.Record(world.WorldTick, item.Name, ValuedCost(item));

			foreach (var p in pauseBuilding)
				p.BuildingAdmitted(item, cost);

			baseBuilder.RecordProducerOrder(queue.Actor, item.Name);
			itemQueuedThisTick = true;
			orderedCostThisSweep += Math.Max(0, cost);

			// As TickQueue (PROD-BUSY-01): with every producer served in one sweep a start counts as in flight at once.
			if (AllProducersOn)
				baseBuilder.BuildingsBeingProduced[item.Name] = (baseBuilder.BuildingsBeingProduced.TryGetValue(item.Name, out var inFlight) ? inFlight : 0) + 1;

			SetBuildingInterval(item.Name);
		}

		// IBaseBuildFeasibility over the queue manager's own checks, rebound to the producer at every planned sweep.
		sealed class PipelineFeasibility : IBaseBuildFeasibility
		{
			readonly BaseBuilderQueueManagerCA qm;
			readonly HashSet<string> buildableNames = new(StringComparer.Ordinal);
			ProductionQueue queue;
			ActorInfo power;
			IBotExpansionTargetProvider law;
			BaseBuildSnapshot snapshot;

			public PipelineFeasibility(BaseBuilderQueueManagerCA qm)
			{
				this.qm = qm;
			}

			public void Begin(ProductionQueue queue, List<ActorInfo> buildable, ActorInfo power, IBotExpansionTargetProvider law, BaseBuildSnapshot snapshot)
			{
				this.queue = queue;
				this.power = power;
				this.law = law;
				this.snapshot = snapshot;
				buildableNames.Clear();
				foreach (var a in buildable)
					buildableNames.Add(a.Name);
			}

			public bool StageAdmits(BaseBuildCandidate candidate) =>
				candidate.Stage != BaseBuildStage.S10Fractions || qm.PipelineFractionCapsAdmit(queue, buildableNames, candidate.Name);

			public bool Feasible(BaseBuildCandidate candidate, out string admitted) =>
				qm.PipelineFeasible(queue, buildableNames, power, snapshot, candidate, out admitted);

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => qm.PipelineTryClaim(queue, law, admitted);
		}
	}
}
