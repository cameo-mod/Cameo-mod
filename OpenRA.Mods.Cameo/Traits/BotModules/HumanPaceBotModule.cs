#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Human-likeness H1: a shared action + attention budget so a bot cannot spend",
		"superhuman bursts and decision points take turns. Consumers: ModularBot's",
		"order drain (TryConsumeActions) and SquadManagerBotModuleCA's squad updates",
		"(TryConsumeAttention).")]
	public class HumanPaceBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Orders admitted per sliding window (the sustained APM cap) when the tier's BotLimits.ActionsPerMinute is 0.",
			"0 = unlimited. 6 per 25 ticks is 360 per game minute. The AlphaStar lesson is that the cap must exist",
			"AND not be spendable in a burst; MaxActionsPerTick is the other half.")]
		public readonly int ActionsPerWindow = 6;

		[Desc("Window length in ticks used when BotLimits.ActionsPerMinute sets the cap (125 = 5 s at the default timestep).")]
		public readonly int LimitsWindowTicks = 125;

		[Desc("Ticks per game minute, to turn BotLimits.ActionsPerMinute into a per-window budget.")]
		public readonly int TicksPerMinute = 1500;

		[Desc("Window length in ticks for the sustained action budget (25 ticks = 1s at default timestep).")]
		public readonly int WindowTicks = 25;

		[Desc("Orders admitted in a single tick regardless of window headroom (the burst cap). 0 = unlimited.")]
		public readonly int MaxActionsPerTick = 3;

		[Desc("Distinct decision points (squads, managers) that may act in one tick. 0 = unlimited.",
			"Callers past the cap must defer — the deferral is the stagger that keeps group",
			"orders landing one after another instead of simultaneously.")]
		public readonly int AttentionSlotsPerTick = 2;

		public override object Create(ActorInitializer init) { return new HumanPaceBotModule(init.Self, this); }
	}

	public class HumanPaceBotModule : ConditionalTrait<HumanPaceBotModuleInfo>, IBotActionBudget
	{
		readonly World world;
		readonly Actor self;
		BotLimits botLimits;
		bool limitsResolved;

		// Sliding window as a per-tick ledger; expired entries are dropped lazily on the
		// next query, so the module needs no IBotTick and works at any point in the
		// tick order — the reset cannot be missed by ticking too early or too late.
		readonly Queue<KeyValuePair<int, int>> window = new();
		int windowTotal;
		int actionsThisTick;
		int burstTick = -1;
		readonly HashSet<object> attention = [];
		int attentionTick = -1;

		public HumanPaceBotModule(Actor self, HumanPaceBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			this.self = self;
		}

		/// <summary>Orders admitted and order attempts deferred so far: whether the cap binds (match log).</summary>
		public int ActionsAdmitted { get; private set; }
		public int ActionsDeferred { get; private set; }

		// The tier's BotLimits are granted by condition at game start, after this trait is created.
		(int Actions, int Window) Budget()
		{
			if (!limitsResolved || (botLimits != null && botLimits.IsTraitDisabled))
			{
				botLimits = self.Owner.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
				limitsResolved = botLimits != null;
			}

			var perMinute = botLimits?.Info.ActionsPerMinute ?? 0;
			return perMinute > 0
				? (BudgetPerWindow(perMinute, Info.LimitsWindowTicks, Info.TicksPerMinute), Math.Max(1, Info.LimitsWindowTicks))
				: (Info.ActionsPerWindow, Math.Max(1, Info.WindowTicks));
		}

		/// <summary>APM -> orders per window, rounded, at least one.</summary>
		public static int BudgetPerWindow(int actionsPerMinute, int windowTicks, int ticksPerMinute) =>
			Math.Max(1, (int)Math.Round((double)actionsPerMinute * windowTicks / Math.Max(1, ticksPerMinute)));

		public int ActionsInWindow => windowTotal;
		public int ActionsThisTick => burstTick == world.WorldTick ? actionsThisTick : 0;
		public int AttentionInUse => attentionTick == world.WorldTick ? attention.Count : 0;

		public bool TryConsumeActions(int count = 1)
		{
			if (IsTraitDisabled)
				return true;
			if (count <= 0)
				return true;

			var tick = world.WorldTick;
			var (actionsPerWindow, windowTicks) = Budget();
			ExpireWindow(tick, actionsPerWindow, windowTicks);

			if (Info.MaxActionsPerTick > 0)
			{
				if (burstTick != tick)
				{
					burstTick = tick;
					actionsThisTick = 0;
				}

				if (actionsThisTick + count > Info.MaxActionsPerTick)
				{
					ActionsDeferred += count;
					return false;
				}
			}

			if (actionsPerWindow > 0 && windowTotal + count > actionsPerWindow)
			{
				ActionsDeferred += count;
				return false;
			}

			ActionsAdmitted += count;
			actionsThisTick += count;
			windowTotal += count;
			window.Enqueue(new KeyValuePair<int, int>(tick, count));
			return true;
		}

		public bool TryConsumeAttention(object decisionPoint)
		{
			if (IsTraitDisabled)
				return true;
			if (decisionPoint == null)
				return true;

			var tick = world.WorldTick;
			if (attentionTick != tick)
			{
				attentionTick = tick;
				attention.Clear();
			}

			if (attention.Contains(decisionPoint))
				return true;

			if (Info.AttentionSlotsPerTick <= 0 || attention.Count < Info.AttentionSlotsPerTick)
			{
				attention.Add(decisionPoint);
				return true;
			}

			return false;
		}

		void ExpireWindow(int tick, int actionsPerWindow, int windowTicks)
		{
			if (actionsPerWindow <= 0)
			{
				if (window.Count > 0)
				{
					window.Clear();
					windowTotal = 0;
				}

				return;
			}

			var cutoff = tick - windowTicks;
			while (window.Count > 0 && window.Peek().Key <= cutoff)
				windowTotal -= window.Dequeue().Value;
		}
	}
}
