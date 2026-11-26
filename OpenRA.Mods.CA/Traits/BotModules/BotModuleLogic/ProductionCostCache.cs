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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// PERF-BOTTICK-01 FIX 4 (row cost_cache_visit): ProductionQueue.GetProductionCost memoised for ONE bot module visit, per (queue actor, queue type, actor type).
	/// V08F Leg A (LEGA_REPORT_01 7ac5aebe): 73% of the long visits was GetProductionCost, because GetProductionCostModifier gathers the owned prerequisites into a new dictionary on every call.
	/// Inside a visit, nothing that feeds the cost can change: the module only reads and queues orders (they run in later ticks), and actor add, remove and owner change are frame-end tasks.
	/// Outside a visit (an interface call from another module, a notification), the cost is read live. The value is the same integer, so the order stream is unchanged.
	/// DEV builds recompute every 64th cached answer and log a mismatch.
	/// Audit (CM COSTMOD_AUDIT_01 fe720f9e, engine f47bf432): the only cost modifier is ProductionCostMultiplier (a constant multiplier, a static queue set, owned prerequisites through
	/// TechTree); no condition, time, handicap or cheat is read, and the mod adds no modifier. Prerequisite providers can toggle their enabled flag without an ActorChanged event, so a
	/// per-world-tick cache would be unsafe (Opus ruling: excluded). A toggle needs another trait to run, and none runs inside one module visit, so the memo lives for one visit only.
	/// </summary>
	public sealed class ProductionCostCache
	{
		readonly Dictionary<(uint Actor, string Queue, string Type), int> costs = new();
		bool active;
#if CAMEO_DEV_LOGGING
		const int ShadowEvery = 64;
		int shadowCount;
#endif

		/// <summary>Starts a visit: the cache is empty and is used only while `on` (the row's switch and ablation).</summary>
		public void BeginVisit(bool on)
		{
			costs.Clear();
			active = on;
		}

		/// <summary>Ends the visit: later calls read the cost live until the next visit begins.</summary>
		public void EndVisit()
		{
			costs.Clear();
			active = false;
		}

		public bool Active => active;

		/// <summary>The production cost, exactly as queue.GetProductionCost returns it.</summary>
		public int Get(ProductionQueue queue, ActorInfo unit)
		{
			if (!active)
				return queue.GetProductionCost(unit);

			var key = (queue.Actor.ActorID, queue.Info.Type, unit.Name);
			if (costs.TryGetValue(key, out var cached))
			{
#if CAMEO_DEV_LOGGING
				if (++shadowCount % ShadowEvery == 0)
				{
					var fresh = queue.GetProductionCost(unit);
					if (fresh != cached)
						Log.Write("debug", $"PERF-BOTTICK-01 SHADOW-MISMATCH production-cost {unit.Name} queue {queue.Info.Type} actor {queue.Actor.ActorID} cached {cached} fresh {fresh} tick {queue.Actor.World.WorldTick}");
				}
#endif
				return cached;
			}

			return costs[key] = queue.GetProductionCost(unit);
		}
	}
}
