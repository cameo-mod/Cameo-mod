#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("MissionObjectives plus a bot-takeover intercept: a Surrender order first asks the " +
		"BotTakeoverTracker whether the seat converts to AI control; if not, the stock defeat " +
		"path runs unchanged.")]
	public class CameoMissionObjectivesInfo : MissionObjectivesInfo
	{
		public override object Create(ActorInitializer init) { return new CameoMissionObjectives(init.Self.Owner, this); }
	}

	// IResolveOrder is re-listed so interface dispatch (Actor.ResolveOrder over
	// TraitsImplementing<IResolveOrder>) binds to this implementation, not the base
	// class's implicit one. Trait<MissionObjectives> still resolves this instance via
	// the base-class registration in TypeDictionary.
	public class CameoMissionObjectives : MissionObjectives, IResolveOrder
	{
		public CameoMissionObjectives(OpenRA.Player player, CameoMissionObjectivesInfo info)
			: base(player, info) { }

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Surrender" && !TakeoverSurrender(self))
				ForceDefeat(self.Owner);
		}

		// Separate virtual so tests can probe the interface dispatch without a World.
		internal virtual bool TakeoverSurrender(Actor self)
		{
			var tracker = self.World.WorldActor.TraitOrDefault<BotTakeoverTracker>();
			return tracker != null && tracker.TryTakeoverOnSurrender(self.Owner);
		}
	}
}
