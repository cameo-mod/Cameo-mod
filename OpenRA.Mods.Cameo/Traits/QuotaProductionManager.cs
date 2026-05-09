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
		"target alive-count targets. When Quota Mode is on, left-clicking a unit in the",
		"production panel increments its quota; right-clicking (cancel) decrements it.",
		"Each production building tracks its own quota independently (additive model).",
		"Buildings produce the unit type with the worst alive/target ratio first.",
		"Designed for single-player use. Behaviour in multiplayer is experimental.")]
	public class QuotaProductionManagerInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Label shown for the Quota Mode checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Quota Mode (EXPERIMENTAL)";

		[Desc("Tooltip shown for the Quota Mode checkbox in the lobby.")]
		public readonly string CheckboxDescription =
			"Production buildings auto-requeue units to maintain set alive counts. " +
			"Queue units normally to set targets; cancel to reduce them.";

		[Desc("Quota Mode is off by default.")]
		public readonly bool CheckboxEnabled = false;

		[Desc("Prevent Quota Mode from being toggled in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Show the Quota Mode checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the Quota Mode checkbox among other lobby options.")]
		public readonly int CheckboxDisplayOrder = 25;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(
				map, "quotamode",
				CheckboxLabel, CheckboxDescription,
				CheckboxVisible, CheckboxDisplayOrder,
				CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) => new QuotaProductionManager(this);
	}

	public class QuotaProductionManager : INotifyCreated, ITick, INotifyOtherProduction
	{
		readonly QuotaProductionManagerInfo info;
		World world;

		public bool Enabled { get; private set; }

		// Per-building quota targets.  Key: building ActorID → unit-type name → target alive count.
		readonly Dictionary<uint, Dictionary<string, int>> buildingQuotas = new();

		// Alive actors produced by each building.  Key: building ActorID → unit-type name → actor list.
		readonly Dictionary<uint, Dictionary<string, List<Actor>>> buildingAlive = new();

		// Queue depth snapshots taken at the start of the last tick.
		// Key: (building ActorID, ProductionQueue.Info.Type) → unit-type name → count in queue.
		readonly Dictionary<(uint, string), Dictionary<string, int>> queueSnapshots = new();

		// Credits for auto-queue orders we issued so we don't treat them as player actions.
		// Key: building ActorID → unit-type name → pending auto-queue count.
		readonly Dictionary<uint, Dictionary<string, int>> autoQueueCredits = new();

		// Production completions recorded via INotifyOtherProduction during the PREVIOUS frame.
		// (Completions fire inside AddFrameEndTask, i.e. after all actor ticks, so they are
		//  promoted here at the start of the world-actor tick on the NEXT frame.)
		readonly Dictionary<(uint buildingId, string type), int> completedPrevFrame = new();

		// Staging buffer filled by INotifyOtherProduction during the current frame.
		readonly Dictionary<(uint buildingId, string type), int> completedThisFrame = new();

		public QuotaProductionManager(QuotaProductionManagerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			world = self.World;
			Enabled = world.LobbyInfo.GlobalSettings.OptionOrDefault("quotamode", info.CheckboxEnabled);
		}

		// Fired (via AddFrameEndTask) after all actor ticks when a unit finishes production.
		void INotifyOtherProduction.UnitProducedByOther(
			Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (!Enabled) return;
			if (world.LocalPlayer == null || producer.Owner != world.LocalPlayer) return;

			var key = (producer.ActorID, produced.Info.Name);
			completedThisFrame[key] = completedThisFrame.GetValueOrDefault(key, 0) + 1;

			// Track the freshly spawned actor so we know when it dies.
			var byType = GetOrAdd(buildingAlive, producer.ActorID);
			GetOrAdd(byType, produced.Info.Name).Add(produced);
		}

		void ITick.Tick(Actor self)
		{
			if (!Enabled || world.LocalPlayer == null) return;

			// Promote this-frame completions to previous-frame (they were filled AFTER last tick).
			completedPrevFrame.Clear();
			foreach (var kvp in completedThisFrame)
				completedPrevFrame[kvp.Key] = kvp.Value;
			completedThisFrame.Clear();

			foreach (var building in world.ActorsHavingTrait<ProductionQueue>())
			{
				if (building.IsDead || !building.IsInWorld) continue;
				if (building.Owner != world.LocalPlayer) continue;

				CleanDeadActors(building.ActorID);

				foreach (var queue in building.TraitsImplementing<ProductionQueue>())
				{
					if (!queue.Enabled) continue;

					// Infinite mode conflicts with quota — suppress it so quota controls requeuing.
					foreach (var item in queue.AllQueued())
						if (item.Infinite)
							item.Infinite = false;

					UpdateQuotasFromSnapshot(building.ActorID, queue);
				}

				AutoQueueDeficit(building);
			}
		}

		// Compares current queue depth against last tick's snapshot to detect player actions.
		void UpdateQuotasFromSnapshot(uint buildingId, ProductionQueue queue)
		{
			var snapshotKey = (buildingId, queue.Info.Type);

			var current = queue.AllQueued()
				.GroupBy(item => item.Item)
				.ToDictionary(g => g.Key, g => g.Count());

			if (!queueSnapshots.TryGetValue(snapshotKey, out var prev))
			{
				// First tick for this queue – just record baseline, no delta to process.
				queueSnapshots[snapshotKey] = current;
				return;
			}

			var allTypes = new HashSet<string>(current.Keys);
			allTypes.UnionWith(prev.Keys);

			foreach (var type in allTypes)
			{
				current.TryGetValue(type, out var cur);
				prev.TryGetValue(type, out var pre);
				var delta = cur - pre;

				if (delta > 0)
				{
					// Queue grew – subtract any pending auto-queue credits before attributing to player.
					var credit = GetCredit(buildingId, type);
					var playerDelta = Math.Max(0, delta - credit);
					ConsumeCredit(buildingId, type, Math.Min(delta, credit));

					if (playerDelta > 0)
						GetOrAdd(buildingQuotas, buildingId)[type] =
							GetOrAdd(buildingQuotas, buildingId).GetValueOrDefault(type, 0) + playerDelta;
				}
				else if (delta < 0)
				{
					// Queue shrank – distinguish production completions from player cancellations.
					var absDecrease = -delta;
					completedPrevFrame.TryGetValue((buildingId, type), out var completionCount);
					var cancellations = Math.Max(0, absDecrease - completionCount);

					if (cancellations > 0)
					{
						var quotas = GetOrAdd(buildingQuotas, buildingId);
						var newVal = Math.Max(0, quotas.GetValueOrDefault(type, 0) - cancellations);
						if (newVal == 0)
							quotas.Remove(type);
						else
							quotas[type] = newVal;
					}
				}
			}

			queueSnapshots[snapshotKey] = current;
		}

		// Finds the unit type with the worst alive/target ratio for this building and queues one.
		void AutoQueueDeficit(Actor building)
		{
			if (!buildingQuotas.TryGetValue(building.ActorID, out var quotas) || quotas.Count == 0)
				return;

			buildingAlive.TryGetValue(building.ActorID, out var aliveByType);
			var queues = building.TraitsImplementing<ProductionQueue>()
				.Where(q => q.Enabled)
				.ToArray();

			string bestType = null;
			ProductionQueue bestQueue = null;
			var bestRatio = float.MaxValue;

			foreach (var (type, target) in quotas)
			{
				if (target <= 0) continue;

				// Find the queue capable of building this unit type.
				var queue = queues.FirstOrDefault(
					q => q.BuildableItems().Any(bi => bi.Name == type));
				if (queue == null) continue;

				var inQueue = queue.AllQueued().Count(i => i.Item == type);
				var alive = aliveByType?.GetValueOrDefault(type)?
					.Count(a => !a.IsDead && a.IsInWorld) ?? 0;

				// Also count orders we issued that haven't shown up in AllQueued() yet
				// (orders take at least one frame to resolve into the queue).
				var inflight = GetCredit(building.ActorID, type);
				var total = alive + inQueue + inflight;

				if (total >= target) continue;

				var ratio = (float)alive / target;
				if (ratio < bestRatio)
				{
					bestRatio = ratio;
					bestType = type;
					bestQueue = queue;
				}
			}

			if (bestType == null) return;

			// Issue the production order and record a credit so we don't misread it as a player action.
			world.IssueOrder(new Order("StartProduction", building, true)
			{
				TargetString = bestType,
				ExtraData = 1
			});

			GetOrAdd(autoQueueCredits, building.ActorID)[bestType] =
				GetOrAdd(autoQueueCredits, building.ActorID).GetValueOrDefault(bestType, 0) + 1;
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

		public int GetQuota(uint buildingId, string unitType)
		{
			if (!buildingQuotas.TryGetValue(buildingId, out var quotas)) return 0;
			return quotas.GetValueOrDefault(unitType, 0);
		}

		public int GetAliveCount(uint buildingId, string unitType)
		{
			if (!buildingAlive.TryGetValue(buildingId, out var byType)) return 0;
			return byType.GetValueOrDefault(unitType)?.Count(a => !a.IsDead && a.IsInWorld) ?? 0;
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
