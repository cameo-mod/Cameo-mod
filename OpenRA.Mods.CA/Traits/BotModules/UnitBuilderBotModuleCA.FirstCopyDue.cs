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
	// FIRST-COPY-DUE-01 (row ub_first_copy_due; RULES_AUDIT_04 A5, user D3): the first-copy deadline of the unit roll. The pure rules are FirstCopyDue; this half reads the game state
	// (bank, income, the roll pool) and feeds them in. Nothing here draws a random number or writes a synced field outside the module's own clocks.
	public partial class UnitBuilderBotModuleCA
	{
		// Income = a live harvester of the harvester module's own types (the module counts them at most every 25 ticks). Without a harvester module the economy is not ours to judge:
		// income is assumed, so the clock keeps running (the guarantee never silently stops).
		bool FirstDueHasIncome() => !RefreshHarvesterCounts() || harvestersLive > 0;

		// One advance per roll visit: every never-ordered type of the pool is credited the time since its last advance (capped by FirstCopyDue.MaxStepTicks), unless the economy is dead.
		void AdvanceFirstCopyClocks(List<ActorInfo> pool)
		{
			var now = world.WorldTick;
			var alive = FirstCopyDue.Alive(playerResources.GetCashAndResources(), RollParameters.FirstDueCash, FirstDueHasIncome());
			foreach (var a in pool)
			{
				if (rollOrdered.Contains(a.Name))
					continue;

				var previous = rollDueAt.TryGetValue(a.Name, out var at) ? at : -1;
				rollDueCredit[a.Name] = rollDueCredit.GetValueOrDefault(a.Name) + FirstCopyDue.Step(previous, now, alive);
				rollDueAt[a.Name] = now;
			}
		}

		// The pool index to start first (the type overdue longest, a tie to the cheaper one), or -1 when no never-ordered type is due.
		int FirstCopyDuePick(ProductionQueue queue, List<ActorInfo> pool, out long overdueBy, out int due)
		{
			var dueTicks = RollParameters.FirstDueTicks;
			var overdue = new long[pool.Count];
			var costs = new int[pool.Count];
			due = 0;
			for (var i = 0; i < pool.Count; i++)
			{
				overdue[i] = rollOrdered.Contains(pool[i].Name) ? long.MinValue : FirstCopyDue.Overdue(rollDueCredit.GetValueOrDefault(pool[i].Name), dueTicks);
				costs[i] = System.Math.Max(0, Cost(queue, pool[i]));
				if (overdue[i] >= 0)
					due++;
			}

			var pick = FirstCopyDue.Pick(overdue, costs);
			overdueBy = pick >= 0 ? overdue[pick] : 0;
			return pick;
		}

		// ub_first_copy_due (DEV, record only): one unit_roll.csv row per forced first-copy pick: tick,seat,ub_first_copy_due,type,queue,overdue ticks;price;types due together.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void FirstCopyDueLog(string queueType, string name, long overdueBy, int price, int due) =>
			RollLog($"{world.WorldTick},{System.Array.IndexOf(world.Players, player)},ub_first_copy_due,{name},{queueType},{overdueBy};{price};{due}");
	}
}
