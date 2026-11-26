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
	// buildable or the bank grows; it holds no state. A bias only: every weight outside the core stays exactly as the roll made it (a never-built type keeps its first-build pull, the roll's floor is not lowered), nothing leaves the pool.
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablateCoreFocus;

		/// <summary>HANS-PROFILE-01 (row ub_core_focus; needs the unit roll): the row is on and not ablated.</summary>
		bool CoreFocusOn => UnitRollOn && Info.CoreFocusSharePct > 0 && !ablateCoreFocus.Off(player, AblationIds.UnitCoreFocus);

		// The bank this fill's roll sees (set by the pass before ChooseRollUnit, 0 while the row is off): the core only names units the bank can start.
		int rollBankLeft;

		// The queue and the pool of the roll in progress (set by ChooseRollUnit around UnitPickArbiter.Decide only, null otherwise) and the one delegate the arbiter calls for the lift.
		ProductionQueue rollLiftQueue;
		List<ActorInfo> rollLiftPool;
		Func<double[], double[]> coreLiftCached;

		double[] RollCoreLift(double[] weights) => FocusCore(rollLiftQueue, rollLiftPool, weights);

		/// <summary>The weights with the core lifted (a new array), or the same array when the row finds nothing to do.</summary>
		double[] FocusCore(ProductionQueue queue, List<ActorInfo> pool, double[] weights)
		{
			var n = pool.Count;
			var price = new int[n];
			var settled = new bool[n];
			var eligible = new bool[n];
			admitOneRemoved.Clear();
			for (var i = 0; i < n; i++)
			{
				// PERF-UB-PICK-01: through the visit cost cache (Cost, row cost_cache_visit; the same integer as the live read). The live call here priced the whole pool
				// on every roll, and a refused pick re-rolls, so a low-cash fill paid about pool x pool live GetProductionCost calls (V08G Multi4: 98% of the visit in the pick).
				price[i] = Cost(queue, pool[i]);
				settled[i] = rollOrdered.Contains(pool[i].Name);
				eligible[i] = settled[i] && CoreFocus.Affordable(price[i], rollBankLeft, RollParameters.CoreFocusBankPct);

				// P5 (b) row ub_admit_one: a type the fill's admission would refuse right now is not eligible for the core (narrows the eligible set only; the fill hands the forecast over while the row is on).
				if (rollAdmit != null && eligible[i] && !R4Forecast(price[i]))
				{
					eligible[i] = false;
					admitOneRemoved.Add(i);
				}
			}

			AdmitOneLog(queue.Info.Type, pool, admitOneRemoved);

			var picked = CoreFocus.Core(price, eligible, RollParameters.CoreFocusCount);
			CoreFocusChurn(queue.Info.Type, pool, picked);
			if (picked.Count == 0)
				return weights;

			var core = new bool[n];
			foreach (var i in picked)
				core[i] = true;

			var multiplier = CoreFocus.MultiplierPct(weights, settled, core, RollParameters.CoreFocusSharePct, RollParameters.CoreFocusMaxPct);
			if (multiplier <= 100)
				return weights;

			var focused = CoreFocus.Apply(weights, core, multiplier);
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

		// Per queue type: the last core set seen (names, sorted), the changes counted in the open window and the tick the window opened.
		readonly Dictionary<string, (string Key, int Changes, int Start)> coreChurn = new(StringComparer.Ordinal);

		// ub_core_churn (DEV, record only): how often the core set of a queue changes (the set follows the bank and the pool per roll, no hysteresis). One unit_roll.csv row per queue type per 1000 ticks, written by the
		// first roll after the window has run 1000 ticks: tick,seat,ub_core_churn,,queue,changes;window ticks;core=the set now. Changes per 1000 ticks = changes x 1000 / window ticks. An empty set counts as a set.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void CoreFocusChurn(string queueType, List<ActorInfo> pool, List<int> core)
		{
			var names = new List<string>(core.Count);
			foreach (var i in core)
				names.Add(pool[i].Name);

			names.Sort(StringComparer.Ordinal);
			var key = string.Join("|", names);
			var now = world.WorldTick;
			(string Key, int Changes, int Start) state = coreChurn.TryGetValue(queueType, out var seen) ? seen : (null, 0, now);
			if (state.Key != null && state.Key != key)
				state.Changes++;

			state.Key = key;
			if (now - state.Start >= 1000)
			{
				RollLog($"{now},{Array.IndexOf(world.Players, player)},ub_core_churn,,{queueType},{state.Changes};{now - state.Start};core={key}");
				state.Changes = 0;
				state.Start = now;
			}

			coreChurn[queueType] = state;
		}
	}
}
