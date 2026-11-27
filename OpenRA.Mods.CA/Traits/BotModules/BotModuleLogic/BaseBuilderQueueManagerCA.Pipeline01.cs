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

		// CONSOLIDATE-03 P1: the capture-unlock lookup of the snapshot, one delegate per manager (captureUnlockOffered is refilled by every S5 source walk).
		Func<string, bool> captureUnlockOf;

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
			if (baseBuilder.NoFoundationArmed)
				buildable = FoundationExclusion.Without(buildable, baseBuilder.FoundationTypes); // BUILD-ALL-B: no concrete on any stage
			var power = GetProducibleBuilding(baseBuilder.Info.PowerTypes, buildable,
				a => a.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount));
			var law = baseBuilder.RefineryLawProvider();
			var snapshot = PipelineSnapshot(queue, buildable, power, law);

			pipelineFeasibility ??= new PipelineFeasibility(this);
			pipelineFeasibility.Begin(queue, buildable, power, law, snapshot);
			OfferedCollect(snapshot); // bb_skip, DEV only
			RefusalsCollect(snapshot); // bb_decide_row, DEV only
			var pick = BaseBuildPlanCA.Plan(snapshot, pipelineFeasibility, BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));
			SkipsObserved(buildable, snapshot, pick); // bb_skip, DEV only
			S8FallthroughObserved(snapshot, pick); // bb_s8_rejected_fallthrough, DEV only
			DecideObserved(snapshot, pick); // bb_decide_row, DEV only
			S8HoldUpdate(snapshot, pick); // bb_s8_cash_hold
			if (pick.IsNone)
				return false;

			var item = world.Map.Rules.Actors[pick.Name];
#if CAMEO_DEV_LOGGING
			AIUtils.BotDebug("{0} decided to build {1}: pipeline {2} (candidate {3}, {4} tests)", queue.Actor.Owner, item.Name, pick.Stage, pick.Candidate, pick.Tests);
#endif
			PickObserved(item.Name, pick.Stage, pick.Candidate, snapshot.Bank);
			AdmitPlanned(bot, queue, item);

			// c6: only the admitted S8 candidate moves the cycle (the stage source is a query); the production type tried, also when A5 built the power plant instead.
			if (pick.Stage == BaseBuildStage.S8ProductionCycle)
				lastCycledProduction = pick.Candidate;

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
				QueueSplit = baseBuilder.QueueSplitArmed && ThisQueueKind != QueueKind.Shared && SeparateDefenceQueue(),
				Bank = CashLedger.BuildingBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep),
				ProductionCashReserve = botLimits != null ? botLimits.Info.ProductionCashReserve : -1,
				ReserveLean = baseBuilder.CashOneReserveArmed ? baseBuilder.ReserveLean() : 1.0,
				TierLine = TierLine,
				LowCashCut = EssentialsOn,
				SustainedActive = SustainedActive,
				DefenceThreatSeen = DefenceThreatSeen(),
				HasArmyValues = armyValues,
				MobileValue = armyValues ? staticValues.MobileValue : 0,
				StaticDefenceValue = armyValues ? StaticDefenceValue(now) : 0,
				Buildable = buildable.Select(a => a.Name).ToList(),
				HasPowerManager = playerPower != null,
				ExcessPower = EmergencyExcess(),
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
				CoreOnceListedOnly = baseBuilder.BuildAllListedArmed,
				S8RejectedFallthrough = baseBuilder.S8RejectedFallthroughArmed,
				S5CaptureReason = baseBuilder.S5CaptureReasonArmed,
				CaptureUnlockOf = captureUnlockOf ??= name => captureUnlockOffered.Contains(name), // CONSOLIDATE-03 P1: names the S5 reason; bb_s5_capture_reason filters by it
				S8CashHold = baseBuilder.S8CashHoldArmed,
				S8HoldMaxTicks = b.S8HoldMaxTicks,
				S8HoldMinBankPercent = b.S8HoldMinBankPercent,
				S8HoldName = s8HoldName,
				S8HoldStart = s8HoldStart,
			};

			// CASH-LIVENESS-01 (cl_cap_spend): at the unit cap with spare cash the reserve that keeps cash for units is 0.
			if (baseBuilder.UnitCapSpare())
				s.ProductionCashReserve = 0;

			// c3, c4, c5: S2, S2b, S4, S5, S6, S7, S9 (Pipeline01Stages.cs); S5 reads the counts of this snapshot.
			s.StageSource = stage => PipelineStageNames(stage, queue, buildable, power, law, s);

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

			// MODE-BIAS-01: null (the old uniform roll) unless an armed profile's trigger holds this tick.
			s.BiasMilli = ModeBiasFor(buildable, s);

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

			// BUILD-ALL-B: with no refinery standing or in flight at all, the anchor law's max does not hold the listed first copy (V08: seat_8 lost its
			// only refinery at tick 5156 and never built another).
			if (baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.HasMaxRefineriesFor(info)
				&& !(baseBuilder.BuildAllListedArmed && baseBuilder.RefineryCountWithQueued() <= 0))
				return false;

			if (NavalNeedsWater(info) || NoSpaceBanned(name))
				return false;

			// REF2-FIRST-01 (rows ref2_before_radar, ref2_before_prod2): a NEW radar or 2nd production start is not offered while ref2 is due. Every stage passes here, nothing in flight is cancelled.
			if (Ref2Holds(info, buildableNames.Contains))
				return false;

			return !BuildingStillDelayed(name);
		}

		// S8-CASH-HOLD (row bb_s8_cash_hold): the running hold of this queue manager (the S8 type waiting for cash and the tick the wait was first seen; -1 = none). The state lives
		// across sweeps so the hold ends after HoldMaxTicks even while the type stays unaffordable; it is cleared as soon as a walk no longer finds an S8 type blocked only by cash or an S8 start is admitted.
		string s8HoldName;
		int s8HoldStart = -1;

		void S8HoldUpdate(BaseBuildSnapshot s, BaseBuildPick pick)
		{
			if (!s.S8CashHold)
				return;

			var blocked = pick.Stage == BaseBuildStage.S8ProductionCycle ? null : s.S8CashBlocked;
			if (blocked == null)
			{
				s8HoldName = null;
				s8HoldStart = -1;
				return;
			}

			if (blocked != s8HoldName || s8HoldStart < 0)
			{
				s8HoldName = blocked;
				s8HoldStart = s.WorldTick;
			}

			S8HoldObserved(s, blocked, pick);
		}

		// DEV only, one line per queue and 750 ticks: the type waiting for cash, the bank, the hold age and whether the hold still blocks S10 (reason s8_cash_hold).
#if CAMEO_DEV_LOGGING
		int s8HoldLogged = -750;
#endif

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void S8HoldObserved(BaseBuildSnapshot s, string blocked, BaseBuildPick pick)
		{
#if CAMEO_DEV_LOGGING
			if (s.WorldTick - s8HoldLogged < 750)
				return;

			s8HoldLogged = s.WorldTick;
			Log.Write("debug", $"AI ({player.ClientIndex}): BB-S8-HOLD queue {Category} tick {s.WorldTick} reason s8_cash_hold type {blocked} bank {s.Bank} age {s.WorldTick - s8HoldStart} max {s.S8HoldMaxTicks} active {BaseBuildPlanCA.S8CashHoldActive(s)} stage {pick.Stage}");
#endif
		}

		bool PipelineFeasible(ProductionQueue queue, HashSet<string> buildableNames, ActorInfo power, BaseBuildSnapshot s, BaseBuildCandidate c, out string admitted) =>
			PipelineFeasibleCore(queue, buildableNames, power, s, c, out admitted, out _);

		// S8-CASH-HOLD: A1-A5 pass and only the A6 cash test fails (cost > 0), and the bank is at least minBankPercent percent of that cost.
		bool PipelineCashOnly(ProductionQueue queue, HashSet<string> buildableNames, ActorInfo power, BaseBuildSnapshot s, BaseBuildCandidate c, int minBankPercent)
		{
			if (PipelineFeasibleCore(queue, buildableNames, power, s, c, out _, out var cashCost))
				return false;

			return cashCost > 0 && (long)s.Bank * 100 >= (long)cashCost * minBankPercent;
		}

		// A1-A6, side-effect free in a release build (DEV only: TechChainA6HoldRecord writes its rate-limit map techChainHoldLogged). A5: a start that would leave power short builds the power plant instead (the plant passes A1-A4 itself);
		// with no plant that gives power the candidate goes as the fraction roll did. cashCost > 0 = the start failed ONLY the A6 cash test, at that cost.
		bool PipelineFeasibleCore(ProductionQueue queue, HashSet<string> buildableNames, ActorInfo power, BaseBuildSnapshot s, BaseBuildCandidate c, out string admitted, out int cashCost)
		{
			admitted = null;
			cashCost = 0;
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

			var startCost = baseBuilder.ProductionCost(queue, start);
			// CONSOLIDATE-03 P2 (a): A6 is the cash ledger's building admission (the same test, CashLedger.AdmitBuilding).
			if (!CashLedger.AdmitBuilding(s.Bank, startCost, CashLedger.BuildingReserve(c.Stage, s.ProductionCashReserve, s.ReserveLean)).Ok)
			{
				// TECHCHAIN-PAYGO-01 (bb_techchain_paygo): an S3/S5 tech-chain start rides with any bank above 0 while no other tech-chain item is in flight.
				// Off, or any other start: the full-cost test as before. A held tech-chain start writes the DEV row pipeline_a6_hold.
				// BUILD-ALL-B (bb_build_all_listed): every S5 start (a listed first copy, or the power plant A5 put in its place) rides the same way while no
				// other listed first copy is in flight, so the high-tech factory no longer needs a place on the TechChainTypes list.
				var techChainStart = baseBuilder.Info.TechChainTypes.Contains(start.Name);
				var listedWaived = baseBuilder.BuildAllListedArmed && BuildAllListed.Waives(c.Stage, s.Bank, ListedFirstCopiesInFlight(queue, s));
				if (!listedWaived && (!techChainStart || !(baseBuilder.TechChainPaygoArmed && TechChainPaygo.Waives(c.Stage, s.Bank, TechChainInFlight()))))
				{
					if (techChainStart || c.Stage == BaseBuildStage.S5CoreOnce)
						TechChainA6HoldRecord(start.Name, c.Stage, s.Bank, startCost);

					cashCost = startCost;
					return false;
				}
			}

			// ECON-A-FIX (R5) exclusivity, a pure query: never duplicate a demand-bound name on this producer.
			if (baseBuilder.ExpansionPrebuildEnabled && baseBuilder.DemandForQueuedItem(start.Name, queue.Actor) != null)
				return false;

			admitted = start.Name;
			return true;
		}

		// TECHCHAIN-PAYGO-01: tech-chain items queued or in production (any producer), so a pay-as-you-go start never stacks on another one.
		int TechChainInFlight()
		{
			var n = 0;
			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (kv.Value > 0 && baseBuilder.Info.TechChainTypes.Contains(kv.Key))
					n += kv.Value;

			return n;
		}

		// BUILD-ALL-B: listed first copies queued or in production in THIS queue only (Opus ruling: separate queues never gate each other), and none of the
		// type (variants counted) stands. Owned-only on purpose: an in-flight copy of a type that already stands is a second copy, not a first one.
		int ListedFirstCopiesInFlight(ProductionQueue queue, BaseBuildSnapshot s) =>
			BuildAllListed.FirstCopiesInQueue(queue.AllQueued().Select(i => i.Item), baseBuilder.Info.BuildingFractions, name => BaseBuildPlanCA.CountOf(s, name));

		// BUILD-ALL-B, DEV only: the admitted start goes to the record-only planner observers (the DEV player telemetry). Compiled out of a release build.
		IBotBuildingPickObserver[] pickObservers;

		IBotBuildingPickObserver[] PickObservers => pickObservers ??= world.WorldActor.TraitsImplementing<IBotBuildingPickObserver>().ToArray();

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickObserved(string actor, BaseBuildStage stage, string candidate, int bank)
		{
			foreach (var o in PickObservers)
				o.BuildingPicked(player, world.WorldTick, actor, stage.ToString(), candidate, bank);
		}

		// bb_skip (Opus ruling 11-25, Haiku V08C RCA: 17 of 18 missing buildings were never offered), DEV only: every listed BuildingFractions type this queue
		// can build now with no copy owned or in flight that the walk did not start, as offered_rejected (the stage that offered it, and why it was rejected)
		// or not_offered (the stage that returned, None = no pick, and why). The observer dedupes per (player, queue, type) and rate-limits. Compiled out of a release build together with its arguments.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void SkipsObserved(List<ActorInfo> buildable, BaseBuildSnapshot s, BaseBuildPick pick)
		{
			if (PickObservers.Length == 0 || s.Fractions == null)
				return;

			var names = new HashSet<string>(buildable.Select(a => a.Name), StringComparer.Ordinal);
			var stage = pick.IsNone ? "None" : pick.Stage.ToString();
			var walkReason = pick.IsNone ? "no_pick" : pick.Stage < BaseBuildStage.S5CoreOnce ? "earlier_stage" : pick.Stage == BaseBuildStage.S5CoreOnce ? "other_first_copy"
				: "not_admitted_s5";
			foreach (var kv in s.Fractions.OrderBy(kv => kv.Key, StringComparer.Ordinal))
			{
				if (kv.Value <= 0 || kv.Key == pick.Name || !names.Contains(kv.Key) || BaseBuildPlanCA.CountOf(s, kv.Key) != 0)
					continue;

				// The ACTUAL offered set of this walk: an offered type was rejected (stage = the offering stage); a type no stage offered is not_offered (stage =
				// the stage that returned, detail = why the walk ended there).
				var offered = false;
				(BaseBuildStage Stage, string Reason) rejection = default;
				if (s.Offered != null)
					offered = s.Offered.TryGetValue(kv.Key, out rejection);

				foreach (var o in PickObservers)
					o.BuildingSkipped(player, world.WorldTick, kv.Key, Category, offered ? rejection.Stage.ToString() : stage, offered ? "offered_rejected" : "not_offered",
						offered ? rejection.Reason : walkReason);
			}
		}

		// S8-STARVE-01 (row bb_s8_rejected_fallthrough), DEV only: the walk offered S8 types under SUSTAINED cash, none was admitted, and S9 or S10 picked instead (the exact
		// case the row opens). One debug line per queue and 750 ticks: queue, tick, bank, tier line, S8 offers, the stage and candidate that won. Compiled out of a release build.
#if CAMEO_DEV_LOGGING
		int s8FallthroughLogged = -750;
#endif

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void S8FallthroughObserved(BaseBuildSnapshot s, BaseBuildPick pick)
		{
#if CAMEO_DEV_LOGGING
			if (!s.S8RejectedFallthrough || !s.SustainedActive || s.TierLine <= 0 || s.S8Offers == 0 || pick.IsNone)
				return;

			if (pick.Stage != BaseBuildStage.S9ExtraDefence && pick.Stage != BaseBuildStage.S10Fractions)
				return;

			var now = world.WorldTick;
			if (now - s8FallthroughLogged < 750)
				return;

			s8FallthroughLogged = now;
			Log.Write("debug", $"AI ({player.ClientIndex}): BB-S8-FALLTHROUGH queue {Category} tick {now} bank {s.Bank} line {s.TierLine} s8_offers {s.S8Offers} stage {pick.Stage} candidate {pick.Candidate} tests {pick.Tests}");
#endif
		}

		// CONSOLIDATE-03 P1 (b), bb_decide_row, DEV only: arms the per-walk refusal list of the arbiter when a recorder is present.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void RefusalsCollect(BaseBuildSnapshot s)
		{
			if (PickObservers.Length > 0)
				s.Refusals = new List<BuildingRefusal>();
		}

#if CAMEO_DEV_LOGGING
		string decideSignature;
		int decideLogged = -750;
#endif

		// CONSOLIDATE-03 P1 (b), bb_decide_row (S8_STARVE_01 telemetry ask), DEV only: the walk's winner, its deciding named rule and every refusal, as one bb_decide row per queue
		// when the decision changes (at most one per 25 ticks) or every 750 ticks while it stays the same. Compiled out of a release build with its arguments.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void DecideObserved(BaseBuildSnapshot s, BaseBuildPick pick)
		{
#if CAMEO_DEV_LOGGING
			if (s.Refusals == null || PickObservers.Length == 0)
				return;

			var decider = pick.IsNone ? BuildingRule.None : BuildingPickArbiter.RuleOf(pick.Stage, s, pick.Candidate);
			var refusals = BuildingPickArbiter.Summary(s.Refusals, 24);
			var signature = $"{(pick.IsNone ? "-" : pick.Candidate)}|{decider}|{refusals}";
			var now = world.WorldTick;
			if (now - decideLogged < (signature == decideSignature ? 750 : 25))
				return;

			decideSignature = signature;
			decideLogged = now;
			foreach (var o in PickObservers)
				o.BuildingDecided(player, now, Category, s.SustainedActive, s.Bank, s.TierLine, pick.IsNone ? "" : pick.Name, pick.IsNone ? "None" : pick.Stage.ToString(),
					decider.ToString(), s.S8Offers, s.Refusals.Count, refusals);
#endif
		}

		// bb_skip, DEV only: arms the per-walk offered-set collector when a recorder is present.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void OfferedCollect(BaseBuildSnapshot s)
		{
			if (PickObservers.Length > 0)
				s.Offered = new Dictionary<string, (BaseBuildStage Stage, string Reason)>(StringComparer.Ordinal);
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void RefineryHeldObserved(string gate)
		{
			var refineries = baseBuilder.RefineryCountWithQueued();
			foreach (var o in PickObservers)
				o.RefineryHeld(player, world.WorldTick, gate, refineries, Category);
		}

		// TECHCHAIN-PAYGO-01, DEV only: last pipeline_a6_hold tick per name. Null until the first row, and always null in a release build (its only writer is compiled out).
		Dictionary<string, int> techChainHoldLogged;

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void TechChainA6HoldRecord(string name, BaseBuildStage stage, int bank, int cost)
		{
			techChainHoldLogged ??= new Dictionary<string, int>();
			var now = world.WorldTick;
			if (!TechChainPaygo.HoldLogDue(techChainHoldLogged.TryGetValue(name, out var last) ? last : -1, now))
				return;

			techChainHoldLogged[name] = now;
			AIUtils.BotDebug("{0} pipeline_a6_hold name={1} stage={2} bank={3} cost={4} paygo={5} tick={6}",
				player, name, stage, bank, cost, baseBuilder.TechChainPaygoArmed, now);

			// BUILD-ALL-B: the same row into the DEV telemetry file (BotDebug alone reaches only the in-game chat, so V08 had no such row on disk).
			foreach (var o in PickObservers)
				o.BuildingA6Held(player, now, name, stage.ToString(), bank, cost);
		}

		// A7: the atomic claims of TickQueue (crawl link, front defence), taken only for the candidate the planner is about to admit. A refused
		// claim holds nothing (the claim contract of IBaseBuildFeasibility). The demand refinery reservation does not apply: the pipeline makes no demand picks.
		bool PipelineTryClaim(ProductionQueue queue, IBotExpansionTargetProvider law, string admitted)
		{
			if (law == null)
				return true;

			var info = world.Map.Rules.Actors[admitted];
			if (admitted == law.WantedLinkBuilding && !law.TryClaimCrawlLink(queue.Actor, admitted, baseBuilder.ProductionCost(queue, info)))
				return false;

			return admitted != law.WantedFrontDefence || law.TryClaimFrontDefence(queue.Actor, admitted, baseBuilder.ProductionCost(queue, info));
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
			var cost = baseBuilder.ProductionCost(queue, item);
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

			public bool StageAdmits(BaseBuildCandidate candidate) => qm.PipelineStageAdmits(queue, buildableNames, candidate); // c5: S9, S10 (Pipeline01Defence.cs)

			public bool Feasible(BaseBuildCandidate candidate, out string admitted) =>
				qm.PipelineFeasible(queue, buildableNames, power, snapshot, candidate, out admitted);

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => qm.PipelineTryClaim(queue, law, admitted);

			public bool CashOnlyBlocked(BaseBuildCandidate candidate, int minBankPercent) =>
				qm.PipelineCashOnly(queue, buildableNames, power, snapshot, candidate, minBankPercent);
		}
	}
}
