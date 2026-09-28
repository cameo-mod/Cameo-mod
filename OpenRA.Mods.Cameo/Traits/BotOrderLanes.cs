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

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// A bot's pending orders when its hands are limited (DESIGN.md §19.1 ActionsPerMinute). A capped FIFO starves:
	/// the modules re-issue the same unit orders every tick, the queue fills with repeats and the oldest orders —
	/// production and placement among them — are dropped. So, like a human's hands:
	/// <list type="bullet">
	/// <item>economy first: priority items (player- and building-level orders) drain before unit orders;</item>
	/// <item>last command wins: a new item with the same key (unit + order type) replaces the pending one, keeping
	/// its place in line; items without a key (grouped or queued orders) are never merged;</item>
	/// <item>stale items cost nothing: the drain skips items the caller says are no longer valid.</item>
	/// </list>
	/// </summary>
	public sealed class BotOrderLanes<TItem, TKey> where TKey : IEquatable<TKey>
	{
		readonly Func<TItem, bool> isPriority;
		readonly Func<TItem, TKey> keyOf;
		readonly Func<TItem, bool> hasKey;
		readonly LinkedList<TItem> priority = new();
		readonly LinkedList<TItem> units = new();
		readonly Dictionary<TKey, LinkedListNode<TItem>> pendingByKey = new();

		public BotOrderLanes(Func<TItem, bool> isPriority, Func<TItem, bool> hasKey, Func<TItem, TKey> keyOf)
		{
			this.isPriority = isPriority;
			this.hasKey = hasKey;
			this.keyOf = keyOf;
		}

		public int Count => priority.Count + units.Count;
		public int PriorityCount => priority.Count;

		/// <summary>Adds an item; `maxUnitItems` caps the unit lane (its oldest items are dropped first).</summary>
		public void Enqueue(TItem item, int maxUnitItems)
		{
			if (isPriority(item))
			{
				priority.AddLast(item);
				return;
			}

			if (hasKey(item))
			{
				var key = keyOf(item);
				if (pendingByKey.TryGetValue(key, out var pending))
				{
					pending.Value = item;
					return;
				}

				pendingByKey[key] = units.AddLast(item);
			}
			else
				units.AddLast(item);

			while (units.Count > Math.Max(1, maxUnitItems))
				Remove(units.First);
		}

		/// <summary>Next item worth an action: priority lane first; invalid items are discarded for free.</summary>
		public bool TryDequeue(Func<TItem, bool> isValid, out TItem item)
		{
			while (priority.Count > 0)
			{
				item = priority.First.Value;
				priority.RemoveFirst();
				if (isValid(item))
					return true;
			}

			while (units.Count > 0)
			{
				item = units.First.Value;
				Remove(units.First);
				if (isValid(item))
					return true;
			}

			item = default;
			return false;
		}

		void Remove(LinkedListNode<TItem> node)
		{
			if (hasKey(node.Value))
			{
				var key = keyOf(node.Value);
				if (pendingByKey.TryGetValue(key, out var pending) && pending == node)
					pendingByKey.Remove(key);
			}

			units.Remove(node);
		}
	}
}
