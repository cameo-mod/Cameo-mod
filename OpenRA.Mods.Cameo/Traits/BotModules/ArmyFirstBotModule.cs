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

		[Desc("Vote PauseBuildingProduction while CashAndResources is below this. Once paused, stays paused until cash "
			+ "recovers above ArmyReserveHighCash — hysteresis keeps building production from flickering every tick.")]
		public readonly int ArmyReserveLowCash = 2500;

		[Desc("Resume building production above this cash level.")]
		public readonly int ArmyReserveHighCash = 4000;

		[Desc("Ticks between re-evaluations of the pause vote.")]
		public readonly int ScanInterval = 25;
	}

	/// <summary>
	/// The army-first vote (AI_ARCHITECTURE §12.20). While the bot is cash-poor, unit production gets the money
	/// before optional buildings do — a base that keeps spawning structures it cannot defend loses the units
	/// that were supposed to protect it. Refineries stay exempt in the consumer (they ARE the economy), and the
	/// vote releases entirely while the bot owns no unit-producing structure, so early-tech sequencing and
	/// rebuilding a wiped production chain can never be starved out by its own guard.
	/// </summary>
	public class ArmyFirstBotModule : ConditionalTrait<ArmyFirstBotModuleInfo>, IBotTick, IBotRequestPauseBuildingProduction
	{
		readonly ArmyFirstBotModuleInfo info;
		PlayerResources playerResources;
		bool paused;
		int scanTicks;

		public ArmyFirstBotModule(ArmyFirstBotModuleInfo info, ActorInitializer init)
			: base(info)
		{
			this.info = info;
		}

		public bool PauseBuildingProduction => paused;

		protected override void Created(Actor self)
		{
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

			// The vote only exists while at least one unit producer does — otherwise it would block the
			// very structures that make an army-first policy meaningful (first barracks/war factory, or
			// rebuilding a wiped production chain).
			var hasUnitProducer = bot.Player.World.ActorsHavingTrait<Building>()
				.Any(a => a.Owner == bot.Player && a.TraitsImplementing<Production>().Any());

			if (!hasUnitProducer)
				paused = false;
			else if (paused)
			{
				if (playerResources.GetCashAndResources() >= info.ArmyReserveHighCash)
					paused = false;
			}
			else if (playerResources.GetCashAndResources() < info.ArmyReserveLowCash)
				paused = true;
		}
	}
}
