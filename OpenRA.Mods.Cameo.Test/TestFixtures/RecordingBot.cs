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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test.TestFixtures
{
	/// <summary>
	/// Minimal <see cref="IBot"/> for seam tests: records every queued order so
	/// tests can assert what a module would have issued without a World.
	/// </summary>
	public sealed class RecordingBot : IBot
	{
		readonly List<Order> queuedOrders = new();

		public IReadOnlyList<Order> QueuedOrders => queuedOrders;
		public Order LastOrder => queuedOrders.Count > 0 ? queuedOrders[queuedOrders.Count - 1] : null;
		public Player Player { get; private set; }
		public IBotInfo Info { get; set; }

		public void Activate(Player p)
		{
			Player = p;
		}

		public void QueueOrder(Order order)
		{
			queuedOrders.Add(order);
		}
	}
}
