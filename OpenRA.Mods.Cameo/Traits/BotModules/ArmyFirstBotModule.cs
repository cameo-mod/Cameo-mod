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
using System.Linq;
using OpenRA.Mods.CA;
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

		/// <summary>Freeze only ticks spent disabled; a visit later in the transition tick has already counted.</summary>
		public static int PauseScan(int transitionTick, int lastVisitTick) => Math.Max(transitionTick, lastVisitTick + 1);

		public static void ResumeScan(ref BotCooldown cooldown, int tick, int disabledFromTick)
		{
			if (disabledFromTick >= 0)
				cooldown.Freeze(Math.Max(0, tick - disabledFromTick));
		}

		public static bool ShouldScan(ref BotCooldown cooldown, int visitTick, int interval)
		{
			if (!cooldown.Ready(visitTick))
				return false;

			cooldown.Start(visitTick, interval);
			return true;
		}
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
		PlayerResources playerResources;
		bool paused;
		bool hasUnitProducer;
		int armyUnits;
		BotCooldown scanCooldown;
		int disabledFromTick = -1;
		int lastBotTick;
		bool scanStarted;

		public ArmyFirstBotModule(ArmyFirstBotModuleInfo info, ActorInitializer init)
			: base(info)
		{
			this.info = info;
		}

		bool IBotRequestPauseBuildingProduction.PausesBuilding(ActorInfo building, bool essential)
		{
			if (IsTraitDisabled || playerResources == null)
				return false;

			var armyHolds = ArmyFirstEval.ArmyCountHolds(hasUnitProducer, playerResources.GetCashAndResources(), armyUnits,
				info.MinArmyUnitsBeforeBuildings, info.ArmyFirstMinCash);
			return ArmyFirstEval.Holds(essential, paused, armyHolds);
		}

		protected override void Created(Actor self)
		{
			playerResources = self.Owner.PlayerActor.TraitOrDefault<PlayerResources>();
			base.Created(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			disabledFromTick = scanStarted
				? ArmyFirstEval.PauseScan(self.World.WorldTick, lastBotTick)
				: -1;
		}

		protected override void TraitEnabled(Actor self)
		{
			ArmyFirstEval.ResumeScan(ref scanCooldown, self.World.WorldTick, disabledFromTick);
			disabledFromTick = -1;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || playerResources == null)
				return;

			var visitTick = BotVisitClock.Of(bot).VisitTick;
			lastBotTick = visitTick;
			if (!ArmyFirstEval.ShouldScan(ref scanCooldown, visitTick, info.ScanInterval))
				return;
			scanStarted = true;

			var world = bot.Player.World;

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
