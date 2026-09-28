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
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotOrderLanesTest
	{
		// An item: "P:<name>" = priority (economy), "U:<key>:<name>" = unit order with a merge key, "G:<name>" = unkeyed.
		static BotOrderLanes<string, string> Lanes() => new(
			i => i.StartsWith('P'),
			i => i.StartsWith('U'),
			i => i.Split(':')[1]);

		static List<string> Drain(BotOrderLanes<string, string> lanes, System.Func<string, bool> valid = null)
		{
			var result = new List<string>();
			while (lanes.TryDequeue(valid ?? (_ => true), out var item))
				result.Add(item);
			return result;
		}

		[Test]
		public void EconomyDrainsBeforeUnitOrders()
		{
			var lanes = Lanes();
			lanes.Enqueue("U:tank1:move", 10);
			lanes.Enqueue("P:build", 10);
			Assert.That(Drain(lanes), Is.EqualTo(new[] { "P:build", "U:tank1:move" }));
		}

		[Test]
		public void TheLastCommandWinsAndKeepsItsPlaceInLine()
		{
			var lanes = Lanes();
			lanes.Enqueue("U:tank1:first", 10);
			lanes.Enqueue("U:tank2:only", 10);
			lanes.Enqueue("U:tank1:second", 10);
			Assert.That(lanes.Count, Is.EqualTo(2));
			Assert.That(Drain(lanes), Is.EqualTo(new[] { "U:tank1:second", "U:tank2:only" }));
		}

		[Test]
		public void UnkeyedOrdersAreNeverMerged()
		{
			var lanes = Lanes();
			lanes.Enqueue("G:group", 10);
			lanes.Enqueue("G:group", 10);
			Assert.That(lanes.Count, Is.EqualTo(2));
		}

		[Test]
		public void TheCapDropsTheOldestUnitOrderNotTheEconomy()
		{
			var lanes = Lanes();
			lanes.Enqueue("P:build", 2);
			lanes.Enqueue("U:a:1", 2);
			lanes.Enqueue("U:b:1", 2);
			lanes.Enqueue("U:c:1", 2);
			Assert.That(Drain(lanes), Is.EqualTo(new[] { "P:build", "U:b:1", "U:c:1" }));

			// The dropped item's key is free again.
			lanes.Enqueue("U:a:2", 2);
			Assert.That(Drain(lanes), Is.EqualTo(new[] { "U:a:2" }));
		}

		[Test]
		public void StaleItemsAreSkippedForFree()
		{
			var lanes = Lanes();
			lanes.Enqueue("U:dead:move", 10);
			lanes.Enqueue("U:alive:move", 10);
			Assert.That(Drain(lanes, i => !i.Contains("dead")), Is.EqualTo(new[] { "U:alive:move" }));
			Assert.That(lanes.Count, Is.Zero);
		}
	}
}
