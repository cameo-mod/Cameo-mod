#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System.Linq;
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

	public class LarvaProductionQueue : ProductionQueue
	{
		public new LarvaProductionQueueInfo Info { get; }

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

			// Always tick Done items so their OnComplete callback fires each frame.
			// Tick up to MaxParallel non-done, non-paused items simultaneously.
			var ticked = 0;
			foreach (var item in Queue)
			{
				if (item.Done)
				{
					item.Tick(playerResources);
					continue;
				}

				if (item.Paused || ticked >= Info.MaxParallel)
					continue;

				item.Tick(playerResources);
				ticked++;
			}
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
				EndProduction(item);
				return true;
			}

			return false;
		}
	}
}
