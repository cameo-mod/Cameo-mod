#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Per-player trait. Enables Quota Mode for the owning player: production buildings",
		"automatically re-queue units to maintain alive-count targets per unit type.",
		"Left-clicking a unit in the production panel increments the target; right-clicking decrements it.",
		"All buildings owned by the player contribute to and draw from the same per-type pool.",
		"The unit type with the worst alive/target ratio is prioritised across all buildings.",
		"State is synchronised across clients via orders, making it multiplayer-safe.")]
	public class QuotaProductionManagerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new QuotaProductionManager(init.Self, this);
	}

	public class QuotaProductionManager : IResolveOrder, INotifyCreated, IWorldLoaded, ITick, INotifyOtherProduction
	{
		World world;
		OpenRA.Player owner;

		// Synced state — only mutated via IResolveOrder so all clients agree.
		bool enabled;
		readonly Dictionary<string, int> globalQuotas = new();

		// Local-only bookkeeping; meaningful only on the owning player's client (gated below).
		readonly Dictionary<uint, Dictionary<string, List<Actor>>> buildingAlive = new();
		readonly Dictionary<(uint, string), Dictionary<string, int>> queueSnapshots = new();
		readonly Dictionary<uint, Dictionary<string, int>> autoQueueCredits = new();
		readonly Dictionary<(uint, string), int> completedPrevFrame = new();
		readonly Dictionary<(uint, string), int> completedThisFrame = new();

		public QuotaProductionManager(Actor self, QuotaProductionManagerInfo info) { }

		public bool Enabled => enabled;

		void INotifyCreated.Created(Actor self)
		{
			world = self.World;
			owner = self.Owner;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			if (owner == w.LocalPlayer && !owner.NonCombatant &&
				w.GetSettings<CameoSettings>().QuotaModeEnabled)
			{
				SetEnabled(true);
			}
		}

		public void SetEnabled(bool enable)
		{
			if (world?.LocalPlayer == null || owner != world.LocalPlayer) return;
			world.IssueOrder(new Order("SetQuotaEnabled", owner.PlayerActor, false)
			{
				ExtraData = enable ? 1u : 0u
			});
		}

		public void AdjustQuota(string unitType, int delta)
		{
			if (world?.LocalPlayer == null || owner != world.LocalPlayer || string.IsNullOrEmpty(unitType)) return;
			world.IssueOrder(new Order("AdjustQuota", owner.PlayerActor, false)
			{
				TargetString = unitType,
				ExtraData = unchecked((uint)delta)
			});
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			switch (order.OrderString)
			{
				case "SetQuotaEnabled":
					enabled = order.ExtraData != 0;
					break;

				case "AdjustQuota":
					ApplyQuotaAdjustment(order.TargetString, unchecked((int)order.ExtraData));
					break;
			}
		}

		void ApplyQuotaAdjustment(string unitType, int delta)
		{
			if (string.IsNullOrEmpty(unitType)) return;

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

		void INotifyOtherProduction.UnitProducedByOther(
			Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (owner != world.LocalPlayer) return;
			if (!enabled) return;
			if (producer.Owner != owner) return;

			var key = (producer.ActorID, produced.Info.Name);
			completedThisFrame[key] = completedThisFrame.GetValueOrDefault(key, 0) + 1;

			var byType = GetOrAdd(buildingAlive, producer.ActorID);
			GetOrAdd(byType, produced.Info.Name).Add(produced);
		}

		void ITick.Tick(Actor self)
		{
			if (!enabled) return;
			if (owner != world.LocalPlayer) return;

			completedPrevFrame.Clear();
			foreach (var kvp in completedThisFrame)
				completedPrevFrame[kvp.Key] = kvp.Value;
			completedThisFrame.Clear();

			// Pass 1: clean dead actors, consume credits from resolved orders,
			// and accumulate the current global queue counts.
			var globalQueued = new Dictionary<string, int>();

			foreach (var building in world.ActorsHavingTrait<ProductionQueue>())
			{
				if (building.IsDead || !building.IsInWorld) continue;
				if (building.Owner != owner) continue;

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
				if (building.Owner != owner) continue;

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

			if (queues.Length == 0) return;

			// Parallel-queue capacity (e.g. Zerg hatchery: MaxParallel larvae).
			// Normal queues fall back to 1 — preserves "one at a time per building".
			var buildingCapacity = queues.Max(q => q is IHasParallelQueueSlots p
				? p.AvailableSlots + q.AllQueued().Count()
				: 1);

			var queuedCount = queues.Sum(q => q.AllQueued().Count());
			var pendingCredits = autoQueueCredits.TryGetValue(building.ActorID, out var existingCredits)
				? existingCredits.Values.Sum()
				: 0;

			if (queuedCount + pendingCredits >= buildingCapacity) return;

			string bestType = null;
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
				}
			}

			if (bestType == null) return;

			world.IssueOrder(new Order("StartProduction", building, true)
			{
				TargetString = bestType,
				ExtraData = 1
			});

			var bc = GetOrAdd(autoQueueCredits, building.ActorID);
			bc[bestType] = bc.GetValueOrDefault(bestType, 0) + 1;

			// Update dynamicInflight so later buildings in this tick see this order.
			dynamicInflight[bestType] = dynamicInflight.GetValueOrDefault(bestType, 0) + 1;
		}

		void CleanDeadActors(uint buildingId)
		{
			if (!buildingAlive.TryGetValue(buildingId, out var byType)) return;
			foreach (var list in byType.Values)
				list.RemoveAll(a => a.IsDead || !a.IsInWorld);
		}

		void ConsumeCredit(uint buildingId, string type, int amount)
		{
			if (amount <= 0) return;
			if (!autoQueueCredits.TryGetValue(buildingId, out var credits)) return;
			credits[type] = Math.Max(0, credits.GetValueOrDefault(type, 0) - amount);
		}

		public int GetQuota(string unitType)
		{
			return globalQuotas.GetValueOrDefault(unitType, 0);
		}

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
