#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// HARV-01 (row hv_harvester_target): the harvester target per refinery in both directions (HarvesterTarget). Below the target with the bank at
	// HarvesterTargetMinCash, a harvester starts first, before the pause votes, the cash floor and the army mix (as MCV-REBUILD-01). At or above the cap,
	// ShouldBuild refuses a harvester type on every path (the roll, the shares and the external requests). The harvester and refinery types are the
	// enabled HarvesterBotModuleCA's own; without one the rule is inert.
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablateHarvesterTarget; // HARV-01
		bool HarvesterTargetOn => switchesActive && Info.HvHarvesterTarget && !ablateHarvesterTarget.Off(player, AblationIds.HvHarvesterTarget);

		HarvesterBotModuleCA harvesterModule;
		int harvesterCountTick = -1;
		int harvesterModuleLookTick = -1;
		int harvestersLive, harvestersQueued, harvesterTarget, harvesterCap;

		// The counts, at most once per 25 ticks. Between refreshes every start (NotifyStarted -> HarvesterStarted, CM review) is added to
		// harvestersQueued at once; a completion moves a harvester from queued to live, so the sum stays right; a loss or a cancel only errs high
		// (the cap holds a little early, never late) until the next refresh.
		bool RefreshHarvesterCounts()
		{
			var now = world.WorldTick;
			if (harvesterModule == null && (harvesterModuleLookTick < 0 || now - harvesterModuleLookTick >= 25))
			{
				harvesterModuleLookTick = now; // a conditional module can arm late: looked up again at most every 25 ticks
				harvesterModule = player.PlayerActor.TraitsImplementing<HarvesterBotModuleCA>().FirstEnabledTraitOrDefault();
			}

			if (harvesterModule == null || harvesterModule.Info.HarvesterTypes.Count == 0 || harvesterModule.Info.RefineryTypes.Count == 0)
				return false;

			if (harvesterCountTick >= 0 && now - harvesterCountTick < 25)
				return true;

			harvesterCountTick = now;
			var h = harvesterModule.Info;
			harvestersLive = world.ActorsHavingTrait<Harvester>().Count(a => a.Owner == player && !a.IsDead && a.IsInWorld && h.HarvesterTypes.Contains(a.Info.Name));
			var refineries = world.ActorsHavingTrait<Building>().Count(a => a.Owner == player && !a.IsDead && a.IsInWorld && h.RefineryTypes.Contains(a.Info.Name));

			harvestersQueued = 0;
			foreach (var q in world.ActorsWithTrait<ProductionQueue>())
				if (q.Actor.Owner == player)
					harvestersQueued += q.Trait.AllQueued().Count(i => h.HarvesterTypes.Contains(i.Item));

			harvesterTarget = HarvesterTarget.Target(refineries, h.HarvestersPerRefinery);
			harvesterCap = HarvesterTarget.Cap(refineries, h.HarvestersPerRefinery, Info.HarvesterCapSlackPerRefinery);
			return true;
		}

		void HarvesterStarted(string name)
		{
			if (harvesterModule != null && harvesterCountTick >= 0 && harvesterModule.Info.HarvesterTypes.Contains(name))
				harvestersQueued++;
		}

		// ShouldBuildReject: a harvester type at or above the cap. Not a harvester type, or no harvester module = no opinion.
		bool HarvesterOverCap(string name)
		{
			if (!RefreshHarvesterCounts() || !harvesterModule.Info.HarvesterTypes.Contains(name))
				return false;

			return HarvesterTarget.OverCap(harvestersLive, harvestersQueued, harvesterCap);
		}

		int harvesterServeTick = -1;

		// At most one look for a free slot per 25 ticks: with no factory or every slot busy the scan does not repeat every visit.
		void TryServeHarvesterTarget(IBot bot)
		{
			var now = world.WorldTick;
			if (harvesterServeTick >= 0 && now - harvesterServeTick < 25)
				return;

			harvesterServeTick = now;
			if (!RefreshHarvesterCounts()
				|| !HarvesterTarget.Due(harvestersLive, harvestersQueued, harvesterTarget, playerResources.GetCashAndResources(), Info.HarvesterTargetMinCash))
				return;

			var types = harvesterModule.Info.HarvesterTypes;
			ProductionQueue best = null;
			ActorInfo bestItem = null;
			foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
			{
				var queue = pair.Trait;
				if (pair.Actor.Owner != player || !queue.Enabled || LiveFreeSlots(queue) <= 0)
					continue;

				var item = queue.BuildableItems().Where(b => types.Contains(b.Name))
					.OrderBy(b => queue.GetProductionCost(b)).ThenBy(b => b.Name, StringComparer.Ordinal).FirstOrDefault();
				if (item != null && (best == null || queue.GetProductionCost(item) < best.GetProductionCost(bestItem)))
				{
					best = queue;
					bestItem = item;
				}
			}

			if (best == null)
				return;

			AIUtils.BotDebug("AI: {0} decided to build {1}: harvesters {2}+{3} below target {4} (hv_harvester_target).", player, bestItem.Name,
				harvestersLive, harvestersQueued, harvesterTarget);
			bot.QueueOrder(Order.StartProduction(best.Actor, bestItem.Name, 1));
			NotifyStarted(bestItem.Name, best.Info.Type); // counts it in harvestersQueued (HarvesterStarted)
		}
	}
}
