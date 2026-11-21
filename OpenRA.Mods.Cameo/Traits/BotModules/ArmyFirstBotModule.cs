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

using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class ArmyFirstBotModuleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ArmyFirstBotModule(this, init); }

		[Desc("Cash-poor vote: hold non-essential buildings while CashAndResources is below this. Once paused, stays paused until cash "
			+ "recovers above ArmyReserveHighCash - hysteresis keeps building production from flickering every tick.")]
		public readonly int ArmyReserveLowCash = 2500;

		[Desc("Resume building production above this cash level.")]
		public readonly int ArmyReserveHighCash = 4000;

		[Desc("Army-count gate (DAWN part): while the player owns fewer combat units (own non-building actors with an attack trait) than "
			+ "this, non-essential buildings are held whenever cash is above ArmyFirstMinCash. 0 disables the gate.")]
		public readonly int MinArmyUnitsBeforeBuildings = 0;

		[Desc("Cash level above which the MinArmyUnitsBeforeBuildings gate applies.")]
		public readonly int ArmyFirstMinCash = 2000;

		[Desc("Ticks between re-evaluations of the vote.")]
		public readonly int ScanInterval = 25;
	}

	/// <summary>Pure army-first decisions (AI_ARCHITECTURE 12.20), unit-tested without a world.</summary>
	public static class ArmyFirstEval
	{
		/// <summary>Cash-poor hysteresis; with no unit producer the vote releases entirely.</summary>
		public static bool NextPaused(bool paused, bool hasUnitProducer, int cash, int lowCash, int highCash)
		{
			if (!hasUnitProducer)
				return false;

			return paused ? cash < highCash : cash < lowCash;
		}

		/// <summary>The DAWN gate: too few combat units while cash is high enough that unit production could have used it.</summary>
		public static bool ArmyCountHolds(bool hasUnitProducer, int cash, int army, int minArmy, int minCash) =>
			minArmy > 0 && hasUnitProducer && cash > minCash && army < minArmy;

		/// <summary>Essentials (conyard, refinery, power, first factory) always pass; everything else yields to either vote.</summary>
		public static bool Holds(bool essential, bool cashPoorPaused, bool armyCountHolds) => !essential && (cashPoorPaused || armyCountHolds);

		/// <summary>
		/// PR-3 projected-share vote (PROD-IDLE_PLAN_03 section 3): only K6 orders (General class: PW, defences, walls,
		/// extra tech and support) are ratio-tested. Essentials, K4 essential buildings and K1/K2 pass untested.
		/// An unset target (negative) holds K6 closed; see <see cref="ArmyShareCA.BuildingAdmits"/>.
		/// </summary>
		public static bool ShareHolds(bool essential, RecoverySpendPolicyCA.Admission admission, ArmyShareCA share, long cost,
			int targetPermille) =>
			!essential && admission == RecoverySpendPolicyCA.Admission.General && !share.BuildingAdmits(cost, targetPermille);
	}

	/// <summary>
	/// The ONE army-first owner (AI_ARCHITECTURE 12.20, merged 2026-10-02). While the bot is cash-poor, or while its army is
	/// below MinArmyUnitsBeforeBuildings with cash to spare, unit production gets the money before optional buildings do - a
	/// base that keeps spawning structures it cannot defend loses the units that were supposed to protect it. The base builder
	/// classifies essentials (construction yard, refinery, power, first production building) and they always pass; the vote
	/// releases entirely while the bot owns no unit-producing structure, so early-tech sequencing and rebuilding a wiped
	/// production chain can never be starved out by its own guard. Counts own units only (fog honest).
	/// </summary>
	public class ArmyFirstBotModule : ConditionalTrait<ArmyFirstBotModuleInfo>, IBotTick, IBotRequestPauseBuildingProduction
	{
		readonly ArmyFirstBotModuleInfo info;
		readonly ArmyShareCA share = new();
		PlayerResources playerResources;
		OpenRA.Player player;
		AblationLatch ablateArmyRatio; // ABLATE-01 hook pi_army_ratio (DEV provider only; release = on)
		BotLimits botLimits;
		bool paused;
		bool hasUnitProducer;
		int armyUnits;
		int scanTicks;

		public ArmyFirstBotModule(ArmyFirstBotModuleInfo info, ActorInitializer init)
			: base(info)
		{
			this.info = info;
		}

		// T(r) in permille from the tier line (500 + 25 r); -1 = unset, which holds K6 closed.
		int ShareTarget => botLimits != null ? botLimits.Info.ArmyShareTargetPermille : -1;

		bool IBotRequestPauseBuildingProduction.PausesBuilding(ActorInfo building, bool essential,
			RecoverySpendPolicyCA.Admission admission, int cost)
		{
			if (IsTraitDisabled || playerResources == null)
				return false;

			var armyHolds = ArmyFirstEval.ArmyCountHolds(hasUnitProducer, playerResources.GetCashAndResources(), armyUnits,
				info.MinArmyUnitsBeforeBuildings, info.ArmyFirstMinCash);
			return ArmyFirstEval.Holds(essential, paused, armyHolds)
				|| !ablateArmyRatio.Off(player, AblationIds.ArmyRatio)
					&& ArmyFirstEval.ShareHolds(essential, admission, share, cost, ShareTarget);
		}

		void IBotRequestPauseBuildingProduction.BuildingAdmitted(ActorInfo building, int cost)
		{
			if (!IsTraitDisabled)
				share.Record(ArmyShareSide.Building, cost);
		}

		// PLAN_02 section 2 values: live actors by their Valued cost (the Squad Manager's UnitValue),
		// in-flight items by their full queue cost. One scan per ScanInterval; admitted orders are
		// recorded on top by BuildingAdmitted until the next scan.
		void RefreshShare(World world, OpenRA.Player owner)
		{
			long armyLive = 0, buildingLive = 0, armyInflight = 0, buildingInflight = 0;

			foreach (var a in world.ActorsHavingTrait<Building>())
				if (a.Owner == owner && !a.IsDead && a.IsInWorld)
					buildingLive += a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

			foreach (var a in world.ActorsHavingTrait<AttackBase>())
				if (a.Owner == owner && !a.IsDead && a.IsInWorld
					&& ArmyShareCA.Classify(a.Info.HasTraitInfo<BuildingInfo>(), true, a.Info.HasTraitInfo<HarvesterInfo>()) == ArmyShareSide.Army)
					armyLive += a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

			foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
			{
				if (pair.Actor.Owner != owner || pair.Actor.IsDead || !pair.Trait.Enabled)
					continue;

				foreach (var item in pair.Trait.AllQueued())
				{
					if (!world.Map.Rules.Actors.TryGetValue(item.Item, out var itemInfo))
						continue;

					var side = ArmyShareCA.Classify(itemInfo.HasTraitInfo<BuildingInfo>(), itemInfo.HasTraitInfo<AttackBaseInfo>(),
						itemInfo.HasTraitInfo<HarvesterInfo>());
					if (side == ArmyShareSide.Army)
						armyInflight += item.TotalCost;
					else if (side == ArmyShareSide.Building)
						buildingInflight += item.TotalCost;
				}
			}

			share.Refresh(armyLive, armyInflight, buildingLive, buildingInflight);
		}

		protected override void Created(Actor self)
		{
			player = self.Owner;
			playerResources = self.Owner.PlayerActor.TraitOrDefault<PlayerResources>();
			base.Created(self);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || playerResources == null)
				return;

			if (--scanTicks > 0)
				return;

			scanTicks = info.ScanInterval;

			var world = bot.Player.World;

			// PR-3: the target is the tier's line; the share snapshot is rebuilt here, once per scan.
			botLimits = bot.Player.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			RefreshShare(world, bot.Player);

			// The vote only exists while at least one unit producer does - otherwise it would block the
			// very structures that make an army-first policy meaningful (first barracks/war factory, or
			// rebuilding a wiped production chain).
			hasUnitProducer = world.ActorsHavingTrait<Building>()
				.Any(a => a.Owner == bot.Player && a.TraitsImplementing<Production>().Any());

			if (info.MinArmyUnitsBeforeBuildings > 0)
				armyUnits = world.ActorsHavingTrait<AttackBase>()
					.Count(a => a.Owner == bot.Player && !a.IsDead && !a.Info.HasTraitInfo<BuildingInfo>());

			paused = ArmyFirstEval.NextPaused(paused, hasUnitProducer, playerResources.GetCashAndResources(), info.ArmyReserveLowCash, info.ArmyReserveHighCash);
		}
	}
}
