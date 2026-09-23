#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors.
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class BotCounterDemandControllerInfo : ConditionalTraitInfo
	{
		public readonly string[] Demands = { "antiair", "antiarmour", "antiinfantry", "detector", "artillery" };
		public readonly string DemandPrefix = "demand-";

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Demands.Length == 0)
				throw new YamlException("Demands must contain at least one counter demand.");

			if (Demands.Any(string.IsNullOrEmpty))
				throw new YamlException("Demands must not contain empty names.");

			if (Demands.Distinct(StringComparer.Ordinal).Count() != Demands.Length)
				throw new YamlException("Demands must not contain duplicate names.");
		}

		public override object Create(ActorInitializer init) { return new BotCounterDemandController(init.Self, this); }
	}

	public class BotCounterDemandController : ConditionalTrait<BotCounterDemandControllerInfo>, IResolveOrder
	{
		readonly Dictionary<string, int> demandTokens = new(StringComparer.Ordinal);

		public IEnumerable<string> ActiveDemands => demandTokens.Keys;

		public BotCounterDemandController(Actor self, BotCounterDemandControllerInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self) { }

		protected override void TraitDisabled(Actor self)
		{
			foreach (var token in demandTokens.Values)
				self.RevokeCondition(token);

			demandTokens.Clear();
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || order.OrderString != "SetBotCounterDemand")
				return;

			var requested = (order.TargetString ?? "")
				.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(d => d.Trim())
				.Where(d => Info.Demands.Contains(d, StringComparer.Ordinal))
				.ToHashSet(StringComparer.Ordinal);

			self.World.AddFrameEndTask(_ =>
			{
				if (IsTraitDisabled)
					return;

				foreach (var demand in demandTokens.Keys.Where(d => !requested.Contains(d)).ToArray())
				{
					demandTokens[demand] = self.RevokeCondition(demandTokens[demand]);
					demandTokens.Remove(demand);
				}

				foreach (var demand in requested)
					if (!demandTokens.ContainsKey(demand))
						demandTokens[demand] = self.GrantCondition(Info.DemandPrefix + demand);
			});
		}
	}
}
