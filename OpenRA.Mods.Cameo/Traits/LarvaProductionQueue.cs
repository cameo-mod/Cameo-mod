#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Production queue for Zerg hatchery that ticks up to MaxParallel items simultaneously.",
		"Requires LarvaConsumingProduction on the same actor to spawn units and consume larvae.")]
	public class LarvaProductionQueueInfo : ProductionQueueInfo
	{
		[Desc("Maximum number of units that can be built simultaneously (one per larva).")]
		public readonly int MaxParallel = 3;

		public override object Create(ActorInitializer init) { return new LarvaProductionQueue(init, this); }
	}

	public class LarvaProductionQueue : ProductionQueue, IHasParallelQueueSlots
	{
		public new LarvaProductionQueueInfo Info { get; }

		public int AvailableSlots => Math.Max(0, Info.MaxParallel - AllQueued().Count());

		LarvaConsumingProduction larvaProduction;

		public LarvaProductionQueue(ActorInitializer init, LarvaProductionQueueInfo info)
			: base(init, info)
		{
			Info = info;
		}

		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			CancelUnbuildableItems();

			if (allProductionPaused)
				return;

			// Lazy-resolve the companion trait (same actor).
			larvaProduction ??= Actor.TraitOrDefault<LarvaConsumingProduction>();

			// How many larvae are committed as eggs right now.
			// Done items hold a slot until Produce() consumes it; subtract them so
			// we know how many in-progress (non-Done) items may tick.
			var assignedSlots = larvaProduction?.AssignedSlotCount ?? 0;
			var doneCount = Queue.Count(i => i.Done);
			var tickableNonDone = Math.Max(0, assignedSlots - doneCount);

			// Use a snapshot of the queue so that EndProduction (called inside
			// OnComplete -> BuildUnit) can safely remove items mid-iteration.
			var ticked = 0;
			foreach (var item in Queue.ToArray())
			{
				// Always tick Done items – their OnComplete fires BuildUnit each frame
				// until Produce() succeeds and EndProduction removes the item.
				if (item.Done)
				{
					item.Tick(playerResources);
					continue;
				}

				// Only tick non-Done items that have an assigned larva slot so the
				// production timer never starts before a larva is physically available.
				if (item.Paused || ticked >= tickableNonDone)
					continue;

				item.Tick(playerResources);
				ticked++;
			}
		}

		protected override void BeginProduction(ProductionItem item, bool hasPriority)
		{
			base.BeginProduction(item, hasPriority);

			if (Info.InfiniteBuildLimit < 0)
				return;

			// After base runs, check if infinite mode just triggered.
			// If so, the base collapsed the queue to 1 item. Re-populate up to
			// MaxParallel so all larvae can build simultaneously.
			var infiniteCount = Queue.Count(i => i.Item == item.Item && i.Infinite);
			if (infiniteCount == 0)
				return;

			var totalOfType = Queue.Count(i => i.Item == item.Item);
			for (var i = totalOfType; i < Info.MaxParallel; i++)
				Queue.Add(new ProductionItem(this, item.Item, item.TotalCost, playerPower, item.OnComplete) { Infinite = true });
		}

		protected override void CancelProduction(string itemName, uint numberToCancel)
		{
			// If there are multiple infinite items (our parallel slots), clear them all
			// atomically so none self-replicates via EndProductionParallel after cancel.
			var infiniteItems = Queue.Where(i => i.Item == itemName && i.Infinite).ToList();
			if (infiniteItems.Count > 0)
			{
				foreach (var inf in infiniteItems)
					inf.Infinite = false;

				// Remove and refund all but the first; base CancelProduction handles the first.
				foreach (var extra in infiniteItems.Skip(1))
				{
					if (extra.ResourcesPaid > 0)
					{
						playerResources.GiveResources(extra.ResourcesPaid);
						extra.RemainingCost += extra.ResourcesPaid;
					}

					playerResources.GiveCash(extra.TotalCost - extra.RemainingCost);
					Queue.Remove(extra);
				}
			}

			base.CancelProduction(itemName, numberToCancel);
		}

		protected override bool BuildUnit(ActorInfo unit)
		{
			var mostLikelyProducerTrait = MostLikelyProducer().Trait;

			if (!Actor.IsInWorld || Actor.IsDead || mostLikelyProducerTrait == null)
			{
				CancelProduction(unit.Name, 1);
				return false;
			}

			var inits = new TypeDictionary
			{
				new OwnerInit(Actor.Owner),
				new FactionInit(BuildableInfo.GetInitialFaction(unit, Faction))
			};

			var bi = BuildableInfo.GetTraitForQueue(unit, Info.Type);
			var type = developerMode.AllTech ? Info.Type : (bi.BuildAtProductionType ?? Info.Type);

			// Find any Done item for this unit name (not just the first in queue).
			var item = Queue.FirstOrDefault(i => i.Done && i.Item == unit.Name);
			if (item == null)
				return false;

			if (!mostLikelyProducerTrait.IsTraitPaused && mostLikelyProducerTrait.Produce(Actor, unit, type, inits, item.TotalCost))
			{
				EndProductionParallel(item);
				return true;
			}

			return false;
		}

		// Used instead of EndProduction for the normal (non-cancellation) completion path.
		// Re-fills the queue to MaxParallel infinite items after each unit spawns so all
		// larvae remain occupied throughout an infinite build loop.
		void EndProductionParallel(ProductionItem item)
		{
			var wasInfinite = item.Infinite;
			var itemName = item.Item;
			Queue.Remove(item);

			if (!wasInfinite)
				return;

			// How many of this type are still queued (may be building in parallel).
			var existing = Queue.Count(i => i.Item == itemName);
			for (var i = existing; i < Info.MaxParallel; i++)
				Queue.Add(new ProductionItem(this, itemName, item.TotalCost, playerPower, item.OnComplete) { Infinite = true });
		}
	}
}
