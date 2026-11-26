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

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P6 (Opus FINISH-01, Sol RULE_CONFLICT_MAP_01 item 8): ONE keyed registry of pending starts.
	// The LIFETIME belongs to the owner, not to the registry: a ProducerServiceBatch owns one (one pass, the same tick, dropped with the batch; key = queue, Count = emissions);
	// UpgradeCompletions owns one (key = upgrade name, Tick = the first issue tick, Queue = the queue that last showed it, 300-tick timeout and Settle reasons).
	// Unit build intents (BuildIntentLedger) keep their own state machine and are NOT routed here. Pure: no state outside the owner, no RNG, no save key.
	public sealed class PendingStarts<TKey>
		where TKey : notnull
	{
		public struct Entry
		{
			/// <summary>The tick of the first start (kept: a second start of the same key does not move it).</summary>
			public int Tick;

			/// <summary>The key of the queue that last showed the item (null = never seen).</summary>
			public string Queue;

			/// <summary>Emissions recorded under this key (per-pass owners).</summary>
			public int Count;
		}

		readonly Dictionary<TKey, Entry> entries = new();

		public int Count => entries.Count;

		public bool Contains(TKey key) => entries.ContainsKey(key);

		/// <summary>Start a key once: a key already present keeps its first tick and its queue.</summary>
		public bool TryStart(TKey key, int tick) => entries.TryAdd(key, new Entry { Tick = tick });

		public bool TryGet(TKey key, out Entry entry) => entries.TryGetValue(key, out entry);

		/// <summary>Bind the key to the queue that showed it; the tick is unchanged.</summary>
		public void SetQueue(TKey key, string queue)
		{
			var entry = entries[key];
			entry.Queue = queue;
			entries[key] = entry;
		}

		/// <summary>Count one emission under the key (created at zero); returns the new count.</summary>
		public int Increment(TKey key)
		{
			entries.TryGetValue(key, out var entry);
			entry.Count++;
			entries[key] = entry;
			return entry.Count;
		}

		public int CountOf(TKey key) => entries.TryGetValue(key, out var entry) ? entry.Count : 0;

		public void Remove(TKey key) => entries.Remove(key);

		/// <summary>A snapshot of the keys, safe to iterate while the registry changes.</summary>
		public List<TKey> Keys() => new(entries.Keys);
	}
}
