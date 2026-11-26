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
using System.Collections.Generic;
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

		// Per harvester type: the queued count we believe in (queue count plus starts the queue has not shown yet) and the tick of its last start. A refresh keeps the cached count while a start
		// younger than HarvesterOrderLatencyTicks is not in the queue yet, so a late outgoing order cannot be forgotten and the cap holds (Sol FIX of HARV-01).
		readonly Dictionary<string, int> harvesterQueuedByType = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> harvesterStartTick = new(StringComparer.Ordinal);

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

			var inQueue = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var q in world.ActorsWithTrait<ProductionQueue>())
				if (q.Actor.Owner == player)
					foreach (var i in q.Trait.AllQueued())
						if (h.HarvesterTypes.Contains(i.Item))
							inQueue[i.Item] = inQueue.GetValueOrDefault(i.Item) + 1;

			harvestersQueued = 0;
			foreach (var type in new HashSet<string>(inQueue.Keys.Concat(harvesterQueuedByType.Keys), StringComparer.Ordinal))
			{
				var effective = HarvesterTarget.EffectiveQueued(inQueue.GetValueOrDefault(type), harvesterQueuedByType.GetValueOrDefault(type),
					now - harvesterStartTick.GetValueOrDefault(type, int.MinValue / 2), Info.HarvesterOrderLatencyTicks);
				harvesterQueuedByType[type] = effective;
				harvestersQueued += effective;
			}

			// the target is the yaml per-refinery number (2 by the binding rule), not the harvester module's own HarvestersPerRefinery (3 on generic): target 2 / cap 3 per refinery
			harvesterTarget = HarvesterTarget.Target(refineries, Info.HarvesterTargetPerRefinery);
			harvesterCap = HarvesterTarget.Cap(refineries, Info.HarvesterTargetPerRefinery, Info.HarvesterCapSlackPerRefinery);
			return true;
		}

		void HarvesterStarted(string name)
		{
			if (harvesterModule != null && harvesterCountTick >= 0 && harvesterModule.Info.HarvesterTypes.Contains(name))
			{
				harvestersQueued++;
				harvesterQueuedByType[name] = harvesterQueuedByType.GetValueOrDefault(name) + 1;
				harvesterStartTick[name] = world.WorldTick;
			}
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
					.OrderBy(b => Cost(queue, b)).ThenBy(b => b.Name, StringComparer.Ordinal).FirstOrDefault();
				if (item != null && (best == null || Cost(queue, item) < Cost(best, bestItem)))
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
