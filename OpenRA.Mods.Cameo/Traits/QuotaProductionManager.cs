#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enables Quota Mode: production buildings automatically re-queue units to maintain",
		"global alive-count targets. When Quota Mode is on, left-clicking a unit in the",
		"production panel increments its global quota; right-clicking decrements it.",
		"All buildings contribute to and draw from the same quota pool.",
		"The unit type with the worst alive/target ratio is prioritised across all buildings.",
		"Designed for single-player use. Behaviour in multiplayer is experimental.")]
	public class QuotaProductionManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new QuotaProductionManager(this);
	}

	public class QuotaProductionManager : INotifyCreated, ITick, INotifyOtherProduction
	{
		World world;

		public bool Enabled { get; set; }

		// Global quota targets: unit-type name → target alive count.
		readonly Dictionary<string, int> globalQuotas = new();

		// Alive actors per building (for tracking individual actor lifetimes).
		// Key: building ActorID → unit-type name → actor list.
		readonly Dictionary<uint, Dictionary<string, List<Actor>>> buildingAlive = new();

		// Queue depth snapshots for credit consumption.
		// Key: (building ActorID, ProductionQueue.Info.Type) → unit-type name → count in queue.
		readonly Dictionary<(uint, string), Dictionary<string, int>> queueSnapshots = new();

		// Credits for auto-queue orders issued but not yet visible in AllQueued().
		// Key: building ActorID → unit-type name → pending count.
		readonly Dictionary<uint, Dictionary<string, int>> autoQueueCredits = new();

		// Production completions from the previous frame (needed for credit consumption math).
		// INotifyOtherProduction fires inside AddFrameEndTask, so completions are one frame behind.
		readonly Dictionary<(uint buildingId, string type), int> completedPrevFrame = new();
		readonly Dictionary<(uint buildingId, string type), int> completedThisFrame = new();

		public QuotaProductionManager(QuotaProductionManagerInfo info) { }

		void INotifyCreated.Created(Actor self)
		{
			world = self.World;
			var isMultiplayer = world.Players.Count(p => !p.IsBot && p.Playable) > 1;
			Enabled = !isMultiplayer && Game.Settings.SinglePlayerSettings.QuotaModeEnabled;
		}

		void INotifyOtherProduction.UnitProducedByOther(
			Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (!Enabled) return;
			if (world.LocalPlayer == null || producer.Owner != world.LocalPlayer) return;

			var key = (producer.ActorID, produced.Info.Name);
			completedThisFrame[key] = completedThisFrame.GetValueOrDefault(key, 0) + 1;

			var byType = GetOrAdd(buildingAlive, producer.ActorID);
			GetOrAdd(byType, produced.Info.Name).Add(produced);
		}

		void ITick.Tick(Actor self)
		{
			if (!Enabled || world.LocalPlayer == null) return;

			// Promote this-frame completions (they fire after all actor ticks, so they lag one frame).
			completedPrevFrame.Clear();
			foreach (var kvp in completedThisFrame)
				completedPrevFrame[kvp.Key] = kvp.Value;
			completedThisFrame.Clear();

			// Pass 1: clean dead actors, suppress infinite mode, consume credits from resolved orders,
			// and accumulate the current global queue counts.
			var globalQueued = new Dictionary<string, int>();

			foreach (var building in world.ActorsHavingTrait<ProductionQueue>())
			{
				if (building.IsDead || !building.IsInWorld) continue;
				if (building.Owner != world.LocalPlayer) continue;

				CleanDeadActors(building.ActorID);

				foreach (var queue in building.TraitsImplementing<ProductionQueue>())
				{
					if (!queue.Enabled) continue;

					foreach (var item in queue.AllQueued())
						globalQueued[item.Item] = globalQueued.GetValueOrDefault(item.Item, 0) + 1;

					ConsumeCreditsFromSnapshot(building.ActorID, queue);
				}
			}

			// After credit consumption, build the starting inflight map from remaining credits.
			// dynamicInflight grows as AutoQueueDeficit issues new orders during pass 2,
			// preventing multiple buildings from double-filling the same deficit in one tick.
			var dynamicInflight = new Dictionary<string, int>();
			foreach (var (_, credits) in autoQueueCredits)
				foreach (var (type, count) in credits)
					if (count > 0)
						dynamicInflight[type] = dynamicInflight.GetValueOrDefault(type, 0) + count;

			// Pass 2: queue the most-needed unit from each building.
			foreach (var building in world.ActorsHavingTrait<ProductionQueue>())
			{
				if (building.IsDead || !building.IsInWorld) continue;
				if (building.Owner != world.LocalPlayer) continue;

				AutoQueueDeficit(building, globalQueued, dynamicInflight);
			}
		}

		// Consumes inflight credits when their orders appear in the queue.
		// Uses completedPrevFrame to account for simultaneous completions that offset new arrivals.
		void ConsumeCreditsFromSnapshot(uint buildingId, ProductionQueue queue)
		{
			var snapshotKey = (buildingId, queue.Info.Type);

			var current = queue.AllQueued()
				.GroupBy(item => item.Item)
				.ToDictionary(g => g.Key, g => g.Count());

			if (!queueSnapshots.TryGetValue(snapshotKey, out var prev))
			{
				queueSnapshots[snapshotKey] = current;
				return;
			}

			var allTypes = new HashSet<string>(current.Keys);
			allTypes.UnionWith(prev.Keys);

			foreach (var type in allTypes)
			{
				current.TryGetValue(type, out var cur);
				prev.TryGetValue(type, out var pre);
				var netDelta = cur - pre;

				// A completion reduces queue count but isn't a new arrival — add it back
				// to get the gross increase that represents resolved auto-queue orders.
				completedPrevFrame.TryGetValue((buildingId, type), out var completions);
				var grossIncrease = netDelta + completions;
				if (grossIncrease > 0)
					ConsumeCredit(buildingId, type, grossIncrease);
			}

			queueSnapshots[snapshotKey] = current;
		}

		// Finds the unit type with the worst global alive/target ratio that this building can produce,
		// and issues one production order for it.
		void AutoQueueDeficit(Actor building, Dictionary<string, int> globalQueued, Dictionary<string, int> dynamicInflight)
		{
			if (globalQuotas.Count == 0) return;

			var queues = building.TraitsImplementing<ProductionQueue>()
				.Where(q => q.Enabled)
				.ToArray();

			// Only queue one unit at a time per building; re-evaluate when the slot is free.
			if (queues.Any(q => q.AllQueued().Any())) return;
			if (autoQueueCredits.TryGetValue(building.ActorID, out var existingCredits) && existingCredits.Values.Any(c => c > 0)) return;

			string bestType = null;
			ProductionQueue bestQueue = null;
			var bestRatio = float.MaxValue;

			foreach (var (type, target) in globalQuotas)
			{
				if (target <= 0) continue;

				if (!world.Map.Rules.Actors.TryGetValue(type, out var actorInfo) ||
					(!actorInfo.HasTraitInfo<MobileInfo>() && !actorInfo.HasTraitInfo<AircraftInfo>()))
					continue;

				var queue = queues.FirstOrDefault(q => q.BuildableItems().Any(bi => bi.Name == type));
				if (queue == null) continue;

				var globalAlive = GetAliveCount(type);
				var inQueue = globalQueued.GetValueOrDefault(type, 0);
				var inflight = dynamicInflight.GetValueOrDefault(type, 0);
				var total = globalAlive + inQueue + inflight;

				var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				var effectiveTarget = buildableInfo != null && buildableInfo.BuildLimit > 0
					? Math.Min(target, buildableInfo.BuildLimit)
					: target;

				if (total >= effectiveTarget) continue;

				var ratio = (float)globalAlive / effectiveTarget;
				if (ratio < bestRatio)
				{
					bestRatio = ratio;
					bestType = type;
					bestQueue = queue;
				}
			}

			if (bestType == null) return;

			world.IssueOrder(new Order("StartProduction", building, true)
			{
				TargetString = bestType,
				ExtraData = 1
			});

			GetOrAdd(autoQueueCredits, building.ActorID)[bestType] =
				GetOrAdd(autoQueueCredits, building.ActorID).GetValueOrDefault(bestType, 0) + 1;

			// Update dynamicInflight so later buildings in this tick see this order.
			dynamicInflight[bestType] = dynamicInflight.GetValueOrDefault(bestType, 0) + 1;
		}

		void CleanDeadActors(uint buildingId)
		{
			if (!buildingAlive.TryGetValue(buildingId, out var byType)) return;
			foreach (var list in byType.Values)
				list.RemoveAll(a => a.IsDead || !a.IsInWorld);
		}

		int GetCredit(uint buildingId, string type)
		{
			if (!autoQueueCredits.TryGetValue(buildingId, out var credits)) return 0;
			return credits.GetValueOrDefault(type, 0);
		}

		void ConsumeCredit(uint buildingId, string type, int amount)
		{
			if (amount <= 0 || !autoQueueCredits.TryGetValue(buildingId, out var credits)) return;
			credits[type] = Math.Max(0, credits.GetValueOrDefault(type, 0) - amount);
		}

		public void AdjustQuota(string unitType, int delta)
		{
			var newVal = Math.Max(0, globalQuotas.GetValueOrDefault(unitType, 0) + delta);

			if (world.Map.Rules.Actors.TryGetValue(unitType, out var actorInfo))
			{
				var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildableInfo != null && buildableInfo.BuildLimit > 0)
					newVal = Math.Min(newVal, buildableInfo.BuildLimit);
			}

			if (newVal == 0)
				globalQuotas.Remove(unitType);
			else
				globalQuotas[unitType] = newVal;
		}

		public int GetQuota(string unitType) => globalQuotas.GetValueOrDefault(unitType, 0);

		public int GetAliveCount(string unitType)
		{
			var total = 0;
			foreach (var byType in buildingAlive.Values)
				total += byType.GetValueOrDefault(unitType)?.Count(a => !a.IsDead && a.IsInWorld) ?? 0;
			return total;
		}

		static TValue GetOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key)
			where TValue : new()
		{
			if (!dict.TryGetValue(key, out var value))
				dict[key] = value = new TValue();
			return value;
		}
	}
}
