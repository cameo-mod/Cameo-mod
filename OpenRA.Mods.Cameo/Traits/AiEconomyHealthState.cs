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
using System.Globalization;

namespace OpenRA.Mods.Cameo.Traits
{
	internal readonly record struct EconomyQueueObservation(object Queue, object Head,
		string QueueId, string Item, string State, bool ProducerLive, string Reason);

	internal readonly record struct EconomyQueueSnapshot(string QueueId, string ItemId,
		string Item, string State, int StateSinceTick, bool ProducerLive, string Reason);

	// Observation state only: owns no engine objects beyond bounded live queue/head references.
	internal sealed class AiEconomyHealthState
	{
		internal const int MaximumQueues = 128;
		readonly Dictionary<object, Entry> entries = new(ReferenceEqualityComparer.Instance);
		readonly HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
		readonly List<object> removed = [];
		readonly List<EconomyQueueObservation> census = [];
		readonly HashSet<string> ids = new(StringComparer.Ordinal);
		ulong nextItem = 1;
		int previousTick = -1;
		bool complete = true;

		sealed class Entry
		{
			public object Head;
			public EconomyQueueSnapshot Snapshot;
		}

		internal bool Complete => complete;
		internal int QueueCount => entries.Count;
		internal void MarkIncomplete() => complete = false;
		internal static bool PulseDue(int tick) => tick >= 0 && tick % 50 == 0;
		internal static bool AcceptedDelivery(int creditedValue) => creditedValue > 0;

		internal void Observe(int tick, IEnumerable<EconomyQueueObservation> observations)
		{
			if (tick < 0 || tick < previousTick || (previousTick < 0 && tick != 0) ||
				(previousTick >= 0 && tick - previousTick > 1))
				complete = false;

			previousTick = tick;
			seen.Clear();
			ids.Clear();
			census.Clear();
			foreach (var observation in observations)
			{
				if (census.Count >= MaximumQueues)
				{
					complete = false;
					break;
				}

				if (observation.Queue == null || string.IsNullOrEmpty(observation.QueueId) ||
					observation.State is not ("idle" or "ready" or "producing" or "paused") ||
					seen.Contains(observation.Queue) || ids.Contains(observation.QueueId))
				{
					complete = false;
					break;
				}

				seen.Add(observation.Queue);
				ids.Add(observation.QueueId);
				census.Add(observation);
			}

			// Prune before inserting replacements: a full census may turn over entirely.
			removed.Clear();
			foreach (var pair in entries)
				if (!seen.Contains(pair.Key))
					removed.Add(pair.Key);
			foreach (var queue in removed)
				entries.Remove(queue);

			foreach (var observation in census)
			{
				if (!entries.TryGetValue(observation.Queue, out var entry))
				{
					if (entries.Count >= MaximumQueues)
					{
						complete = false;
						continue;
					}

					entries.Add(observation.Queue, entry = new Entry());
				}

				var headChanged = !ReferenceEquals(entry.Head, observation.Head);
				var stateChanged = entry.Snapshot.State != observation.State;
				var itemId = entry.Snapshot.ItemId;
				if (headChanged || string.IsNullOrEmpty(itemId))
					itemId = observation.Head == null ? "" : (nextItem++).ToString(CultureInfo.InvariantCulture);

				if (observation.State == "ready" && (observation.Head == null || string.IsNullOrEmpty(observation.Item)))
					complete = false;

				entry.Snapshot = new EconomyQueueSnapshot(observation.QueueId, itemId, observation.Item ?? "",
					observation.State, headChanged || stateChanged ? tick : entry.Snapshot.StateSinceTick,
					observation.ProducerLive, observation.Reason ?? "unknown");
				entry.Head = observation.Head;
			}

		}

		internal void CopySnapshots(List<EconomyQueueSnapshot> target)
		{
			target.Clear();
			foreach (var entry in entries.Values)
				target.Add(entry.Snapshot);
			target.Sort((a, b) => StringComparer.Ordinal.Compare(a.QueueId, b.QueueId));
		}
	}
}
