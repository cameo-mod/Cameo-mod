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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// HANS-PROFILE-01 row 2 (ub_core_focus; design _out/HANS_PROFILE_DESIGN_01.md, Hans' top 3 unit types are 48 to 64 percent of his requests). After the roll weights are final, the CORE of a
	// queue (the CoreFocusCount dearest types this bot has built once that the bank can pay for, CoreFocus.Core) gets a weight multiplier so that it holds about CoreFocusSharePct percent of
	// the weight of the types built once (CoreFocus.MultiplierPct). The set is recomputed on every roll from the pool and the bank, so it follows the tier by itself when a dearer type becomes
	// buildable or the bank grows; it holds no state. A bias only: the floor chance of every candidate is applied again after the lift, a never-built type keeps its first-build pull untouched.
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablateCoreFocus;

		/// <summary>HANS-PROFILE-01 (row ub_core_focus; needs the unit roll): the row is on and not ablated.</summary>
		bool CoreFocusOn => UnitRollOn && Info.CoreFocusSharePct > 0 && !ablateCoreFocus.Off(player, AblationIds.UnitCoreFocus);

		// The bank this fill's roll sees (set by the pass before ChooseRollUnit, 0 while the row is off): the core only names units the bank can start.
		int rollBankLeft;

		/// <summary>The weights with the core lifted (a new array), or the same array when the row finds nothing to do.</summary>
		double[] FocusCore(ProductionQueue queue, List<ActorInfo> pool, double[] weights)
		{
			var n = pool.Count;
			var price = new int[n];
			var settled = new bool[n];
			var eligible = new bool[n];
			for (var i = 0; i < n; i++)
			{
				price[i] = queue.GetProductionCost(pool[i]);
				settled[i] = rollOrdered.Contains(pool[i].Name);
				eligible[i] = settled[i] && CoreFocus.Affordable(price[i], rollBankLeft, Info.CoreFocusBankPct);
			}

			var picked = CoreFocus.Core(price, eligible, Info.CoreFocusCount);
			if (picked.Count == 0)
				return weights;

			var core = new bool[n];
			foreach (var i in picked)
				core[i] = true;

			var multiplier = CoreFocus.MultiplierPct(weights, settled, core, Info.CoreFocusSharePct, Info.CoreFocusMaxPct);
			if (multiplier <= 100)
				return weights;

			var focused = CoreFocus.Apply(weights, core, multiplier, RollParameters.FloorPct);
			CoreFocusLog(queue.Info.Type, pool, picked, multiplier, CoreFocus.SharePct(weights, settled, core), CoreFocus.SharePct(focused, settled, core));
			return focused;
		}

		readonly Dictionary<string, int> coreFocusLogTick = new(StringComparer.Ordinal);

		// ub_core_focus (DEV, record only): at most one unit_roll.csv row per queue type per 250 ticks while the lift is on: tick,seat,ub_core_focus,,queue,multiplier percent;core=a|b|c;share before;share after
		// (the shares are percents of the weight of the types built once). The ub_unit_built rows give the real top-3 share to compare with.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void CoreFocusLog(string queueType, List<ActorInfo> pool, List<int> core, int multiplier, int shareBefore, int shareAfter)
		{
			if (coreFocusLogTick.TryGetValue(queueType, out var last) && world.WorldTick - last < 250)
				return;

			coreFocusLogTick[queueType] = world.WorldTick;
			var names = new List<string>(core.Count);
			foreach (var i in core)
				names.Add(pool[i].Name);

			RollLog($"{world.WorldTick},{Array.IndexOf(world.Players, player)},ub_core_focus,,{queueType},{multiplier};core={string.Join("|", names)};{shareBefore};{shareAfter}");
		}
	}
}
