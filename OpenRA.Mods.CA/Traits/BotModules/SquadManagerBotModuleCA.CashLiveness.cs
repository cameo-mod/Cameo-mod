#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("CASH-LIVENESS-01 (row cl_cap_allin; user D6 with the no-stalemate and dynamic ALL-IN rules, Opus 11-27): the team leader marks its order ALL-IN (reason budget_cap)",
			"when a living bot of its team (itself included) has been at its unit-count cap (an IBotItemAwareUnitCap votes PauseUnitProduction, e.g. BotGlobalUnitBudget) for",
			"CapAllInTicks: an army that cannot grow should be used. Needs the leader's all-in switch. Off in code.")]
		public readonly bool CashLivenessCapAllInEnabled = false;

		[Desc("CASH-LIVENESS-01: how long a team seat stays at its unit cap before the budget_cap all-in fires, in ticks (1500 = 60 s). Learnable (DEV training only); clamped 375..7500.")]
		public readonly int CapAllInTicks = 1500;
	}

	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateCapAllIn;
		int capAllInSince = -1;
		readonly Dictionary<Player, IBotRequestPauseUnitProduction[]> unitCapVotes = new();

		bool CapAllInOn => TeamLeadAllInOn && Info.CashLivenessCapAllInEnabled && !ablateCapAllIn.Off(Player, AblationIds.ClCapAllIn);

		/// <summary>cl_cap_allin (leader, once per team check, sampled before any early return so the clock sees every check): the reason, or null.</summary>
		string CapAllInWhy(int now)
		{
			var capped = CapAllInOn && TeamSeatAtUnitCap();
			return CashLiveness.Sustained(ref capAllInSince, capped, now, Info.CapAllInTicks) ? CashLiveness.WhyBudgetCap : null;
		}

		// The team = this bot and its living allied bots; each seat's own cap vote (the votes are resolved once per player).
		bool TeamSeatAtUnitCap()
		{
			foreach (var p in World.Players)
			{
				if (p != Player && (!p.IsBot || p.WinState != WinState.Undefined || Player.RelationshipWith(p) != PlayerRelationship.Ally))
					continue;

				if (!unitCapVotes.TryGetValue(p, out var votes))
					unitCapVotes[p] = votes = p.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().Where(v => v is IBotItemAwareUnitCap).ToArray();

				foreach (var v in votes)
					if (v.IsTraitEnabled() && v.PauseUnitProduction)
						return true;
			}

			return false;
		}
	}
}
