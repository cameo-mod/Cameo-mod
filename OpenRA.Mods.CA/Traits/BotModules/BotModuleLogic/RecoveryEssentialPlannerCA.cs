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
	// Read-only selection: admission, lease, costs, and ordering are owned by
	// the player recovery budget. Busy slots are not filtered out here.
	public sealed class RecoveryEssentialPlannerCA
	{
		readonly Player player;
		readonly BaseBuilderBotModuleCAInfo info;
		readonly HashSet<string> landQueues;

		public RecoveryEssentialPlannerCA(Player player, BaseBuilderBotModuleCAInfo info)
		{
			this.player = player;
			this.info = info;
			landQueues = player.World.Map.Rules.Actors.Values
				.Where(a => a.HasTraitInfo<MobileInfo>() && !a.HasTraitInfo<AircraftInfo>())
				.SelectMany(a => a.TraitInfos<BuildableInfo>()).SelectMany(b => b.Queue)
				.ToHashSet(StringComparer.Ordinal);
		}

		bool IsLandProduction(ActorInfo actor) => info.ProductionTypes.Contains(actor.Name)
			&& !info.NavalProductionTypes.Contains(actor.Name)
			&& actor.TraitInfos<ProductionInfo>().Any(p => p.Produces.Any(landQueues.Contains));

		public (ProductionQueue Queue, ActorInfo Item, RecoverySpendPolicyCA.EssentialLane Lane)? Select(
			IReadOnlyCollection<RecoverySpendEntry> outstanding, int minimumExcessPower)
		{
			var world = player.World;
			var actors = world.Actors.Where(a => a.Owner == player && !a.IsDead && a.IsInWorld).ToArray();
			var queues = world.ActorsWithTrait<ProductionQueue>()
				.Where(x => x.Actor.Owner == player && !x.Actor.IsDead && x.Actor.IsInWorld).Select(x => x.Trait)
				.Concat(player.PlayerActor.TraitsImplementing<ProductionQueue>()).Distinct().ToArray();
			var queued = queues.SelectMany(q => q.AllQueued()).Select(i => i.Item)
				.Concat(outstanding.Select(x => x.Key.Item)).ToHashSet(StringComparer.Ordinal);

			var powerQueued = queued.Any(info.PowerTypes.Contains);
			var powerCovered = actors.Any(a => info.PowerTypes.Contains(a.Info.Name)
				&& a.TraitsImplementing<Power>().Any(p => !p.IsTraitDisabled && p.GetEnabledPower() > 0))
				|| powerQueued;
			var powerManager = player.PlayerActor.TraitOrDefault<PowerManager>();
			var refineryCovered = actors.Any(a => info.RefineryTypes.Contains(a.Info.Name)
				&& a.TraitOrDefault<Refinery>() != null) || queued.Any(info.RefineryTypes.Contains);
			var productionCovered = actors.Any(a => IsLandProduction(a.Info)
				&& a.TraitsImplementing<Production>().Any(p => !p.IsTraitDisabled && !p.IsTraitPaused
					&& p.Info.Produces.Any(landQueues.Contains)))
				|| queued.Any(name => world.Map.Rules.Actors.TryGetValue(name, out var actor) && IsLandProduction(actor));

			var candidates = new List<(ProductionQueue Queue, ActorInfo Item, RecoverySpendPolicyCA.EssentialLane Lane, int Ordinal)>();
			var powerCandidates = new List<(ProductionQueue Queue, ActorInfo Item, RecoverySpendPolicyCA.EssentialLane Lane, int Ordinal)>();
			foreach (var queue in queues.Where(q => q.Enabled &&
				(info.BuildingQueues.Contains(q.Info.Type) || info.DefenseQueues.Contains(q.Info.Type))))
			{
				var ordinal = Array.IndexOf(queue.Actor.TraitsImplementing<ProductionQueue>().ToArray(), queue);
				if (ordinal < 0)
					continue;

				// BuildableItems filters by current cash for PayUpFront queues. A
				// legal recovery route must remain visible while its funds accumulate.
				foreach (var item in queue.AllItems().Where(queue.CanBuild))
				{
					if (!powerQueued && info.PowerTypes.Contains(item.Name)
						&& item.TraitInfos<PowerInfo>().Where(p => p.EnabledByDefault).Sum(p => p.Amount) > 0)
					{
						var power = (queue, item, RecoverySpendPolicyCA.EssentialLane.Power, ordinal);
						powerCandidates.Add(power);
						if (!powerCovered || PowerArbiter.Short(powerManager != null, powerManager?.ExcessPower ?? 0, minimumExcessPower))
							candidates.Add(power);
					}
					if (!refineryCovered && info.RefineryTypes.Contains(item.Name))
						candidates.Add((queue, item, RecoverySpendPolicyCA.EssentialLane.Refinery, ordinal));
					if (!productionCovered && IsLandProduction(item))
						candidates.Add((queue, item, RecoverySpendPolicyCA.EssentialLane.Production, ordinal));
				}
			}

			var pick = candidates.OrderBy(c => c.Lane).ThenBy(c => c.Queue.Actor.ActorID).ThenBy(c => c.Ordinal)
				.ThenBy(c => c.Item.Name, StringComparer.Ordinal).FirstOrDefault();
			if (pick.Queue != null && pick.Lane != RecoverySpendPolicyCA.EssentialLane.Power && powerManager != null
				&& !PowerArbiter.Admits(true, pick.Item.TraitInfos<PowerInfo>().Where(p => p.EnabledByDefault)
					.Sum(p => p.Amount), powerManager.ExcessPower, minimumExcessPower))
			{
				// An already queued source covers the prerequisite; do not duplicate it.
				if (powerQueued)
					return null;

				pick = powerCandidates.OrderBy(c => c.Queue.Actor.ActorID).ThenBy(c => c.Ordinal)
					.ThenBy(c => c.Item.Name, StringComparer.Ordinal).FirstOrDefault();
			}

			return pick.Queue == null ? null : (pick.Queue, pick.Item, pick.Lane);
		}
	}
}
