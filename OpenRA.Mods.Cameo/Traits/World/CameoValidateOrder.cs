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
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Shadows OpenRA.Mods.Common.Traits.ValidateOrder (world.yaml uses -ValidateOrder:). " +
		"Same authority rules, plus: orders for a takeover seat pass only from the elected " +
		"controller client. Reads BotTakeoverTracker's synced state — never LobbyInfo rows — so " +
		"the verdict is identical on every client.")]
	public class CameoValidateOrderInfo : TraitInfo<CameoValidateOrder> { }

	public class CameoValidateOrder : IValidateOrder
	{
		BotTakeoverTracker tracker;

		public bool OrderValidation(OrderManager orderManager, World world, int clientId, Order order)
		{
			if (order.Subject == null || order.Subject.Owner == null)
				return true;

			var owner = order.Subject.Owner;
			tracker ??= world.WorldActor.TraitOrDefault<BotTakeoverTracker>();

			var takeoverSeat = tracker != null && tracker.IsTakenOver(owner);
			var subjectClient = orderManager.LobbyInfo.ClientWithIndex(owner.ClientIndex);
			if (!takeoverSeat && subjectClient == null)
				Log.Write("debug", $"Tick {world.WorldTick}: " +
					$"Order sent to {owner.ResolvedPlayerName}: " +
					$"resolved ClientIndex `{owner.ClientIndex}` doesn't exist");

			return OrderIsValid(
				clientId,
				subjectClient,
				owner.ClientIndex,
				takeoverSeat,
				controller: tracker?.Controller ?? -1,
				accepts: order.Subject.AcceptsOrder(order.OrderString));
		}

		// The full authority decision, pure and unit-testable:
		//  - takeover seat: only the CURRENT elected controller (a surrendered human's own
		//    orders for their old seat fail here, as does a stale controller's).
		//  - otherwise identical to stock ValidateOrder: the subject's owner client, or the
		//    BotControllerClientIndex of a session row with a Bot.
		internal static bool OrderIsValid(int clientId, Session.Client subjectClient, int subjectClientId,
			bool takeoverSeat, int controller, bool accepts)
		{
			if (takeoverSeat)
				return clientId == controller && accepts;

			if (subjectClient == null)
				return false;

			var isBotOrder = subjectClient.Bot != null && clientId == subjectClient.BotControllerClientIndex;
			return (subjectClientId == clientId || isBotOrder) && accepts;
		}
	}
}
