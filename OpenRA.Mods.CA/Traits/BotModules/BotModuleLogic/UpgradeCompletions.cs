#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// UPGRADE-COVERAGE (Opus 11-25): the once-done registry of one player's upgrade, research, add-on and promotion items. One entry per type, fed by the completion of the item
	/// (the same event that writes the DEV row ub_upgrade_done). Rules: a repeatable item counts as done after its first completion; of a mutually exclusive set (items that
	/// carry a "!other" prerequisite on each other) any one completed satisfies the whole set; among the not-done candidates the one that became available first goes first, so no type
	/// starves behind a heavier-weighted one. Pure and deterministic: no world, no random draw.
	/// </summary>
	public sealed class UpgradeCompletions
	{
		readonly Dictionary<string, int> availableSince = new(StringComparer.Ordinal);
		readonly Dictionary<string, HashSet<string>> blockedBy = new(StringComparer.Ordinal);
		readonly HashSet<string> done = new(StringComparer.Ordinal);
		readonly Dictionary<string, string> sources = new(StringComparer.Ordinal);
		readonly Dictionary<string, (int Tick, string Queue)> pending = new(StringComparer.Ordinal); // Queue = the key of the exact queue that last showed the item (null = never seen)

		public const string SourceGrant = "grant";
		public const string SourceApprox = "approx";
		public const string SourceInit = "init";

		/// <summary>Staleness: an order that is neither seen completing nor granted after this many ticks is dropped (cancelled, or never accepted by the engine).</summary>
		public const int PendingMaxTicks = 3000;

		/// <summary>PendingTimeout default: an order the queue never showed within this many ticks was not accepted: it is released (not done) and the item may be offered again.</summary>
		public const int DefaultPendingTimeoutTicks = 300;

		/// <summary>The releases of the last Settle: (name, reason). Reasons: timeout, cancelled_or_lost, left_still_offered, queue_lost, prerequisite_lost.</summary>
		public List<(string Name, string Reason)> Released { get; } = [];

		/// <summary>The service ordered the item: it is settled at the next visits against authoritative state, whatever happens between visits. A name already pending keeps its first issue tick and Seen bit.</summary>
		public void Started(string name, int tick) => pending.TryAdd(name, (tick, null));

		public bool AnyPending => pending.Count > 0;

		/// <summary>An order is outstanding for this item: it must not be offered again until it settles or is dropped.</summary>
		public bool IsPending(string name) => pending.ContainsKey(name);

		public IEnumerable<string> PendingNames => pending.Keys;

		/// <summary>
		/// Settle the ordered items. Authoritative state decides: an item is done when the player holds a prerequisite it REALLY grants (grantsOf = the prerequisites its traits provide, which need not be
		/// its name), so an item that was ordered and finished between two visits is still found. An item that grants no prerequisite (condition-only) has no state to read: it is done by approximation,
		/// and only on a CREDIBLE departure: it was seen in a queue, left it, is no longer offered, the EXACT queue that showed it is still alive (queueAlive gets that queue key, not any queue of the category) and its own prerequisites are still held (a sold producer or a lost
		/// prerequisite also stop the offer, and must not count as a completion). A never-seen order is never done by approximation: after the PendingTimeout it is released as not accepted.
		/// Returns the newly completed (name, source) pairs.
		/// </summary>
		public List<(string Name, string Source)> Settle(int tick, IReadOnlyDictionary<string, string> queuedIn, Func<string, IReadOnlyCollection<string>> grantsOf, ISet<string> owned, Func<ISet<string>> offered,
			Func<string, bool> queueAlive, Func<string, ISet<string>, bool> prerequisitesHeld, int pendingTimeoutTicks = DefaultPendingTimeoutTicks)
		{
			Released.Clear();
			var result = new List<(string, string)>();
			if (pending.Count == 0)
				return result;

			ISet<string> offeredNow = null;
			foreach (var name in new List<string>(pending.Keys))
			{
				var (started, seenIn) = pending[name];
				if (queuedIn.TryGetValue(name, out var queueKey))
				{
					pending[name] = (started, queueKey);
					continue;
				}

				var grants = grantsOf(name);
				if (grants.Count > 0 && grants.Any(owned.Contains))
				{
					pending.Remove(name);
					if (Complete(name, SourceGrant))
						result.Add((name, SourceGrant));

					continue;
				}

				if (seenIn == null)
				{
					// a delayed order keeps the item reserved (not offered again) until the queue shows it, or the grace runs out
					if (tick - started > pendingTimeoutTicks)
					{
						pending.Remove(name);
						Released.Add((name, "timeout"));
					}

					continue;
				}

				// seen, and gone from the queue
				pending.Remove(name);
				if (grants.Count > 0)
				{
					Released.Add((name, "cancelled_or_lost")); // it grants a prerequisite the player does not hold: cancelled or lost, never done
					continue;
				}

				offeredNow ??= offered();
				if (offeredNow.Contains(name))
					Released.Add((name, "left_still_offered"));
				else if (!queueAlive(seenIn))
					Released.Add((name, "queue_lost"));
				else if (!prerequisitesHeld(name, owned))
					Released.Add((name, "prerequisite_lost"));
				else if (Complete(name, SourceApprox))
					result.Add((name, SourceApprox));
			}

			return result;
		}

		public string Source(string name) => sources.TryGetValue(name, out var source) ? source : null;

		/// <summary>The type is buildable now: remember the first tick it was, and the other items it names with "!X" prerequisites ("!itself" only makes it non-repeatable).</summary>
		public void Observe(string name, int tick, IEnumerable<string> prerequisites)
		{
			availableSince.TryAdd(name, tick);
			if (prerequisites == null)
				return;

			foreach (var prerequisite in prerequisites)
			{
				if (prerequisite == null || !prerequisite.StartsWith('!'))
					continue;

				var other = prerequisite.Substring(1);
				if (other.Length == 0 || other == name)
					continue;

				if (!blockedBy.TryGetValue(name, out var set))
					blockedBy[name] = set = new HashSet<string>(StringComparer.Ordinal);

				set.Add(other);
			}
		}

		/// <summary>The item finished (authoritative completion: the item left its queue and the player now holds the granted prerequisite, or the state was found at init). Returns true on the first completion.</summary>
		public bool Complete(string name, string source = SourceInit)
		{
			if (!done.Add(name))
				return false;

			sources[name] = source;
			return true;
		}

		/// <summary>This type itself completed (not merely a member of a satisfied set).</summary>
		public bool Completed(string name) => done.Contains(name);

		/// <summary>Done = this type completed once, or a MUTUAL exclusive peer did: only a symmetric pair counts (A names !B and B names !A). A one-directional "!B" is a blocker, not a set, and links nothing.</summary>
		public bool IsDone(string name)
		{
			if (done.Contains(name))
				return true;

			if (blockedBy.TryGetValue(name, out var set))
				foreach (var peer in set)
					if (done.Contains(peer) && blockedBy.TryGetValue(peer, out var back) && back.Contains(name))
						return true;

			return false;
		}

		/// <summary>The first tick the type was seen buildable (int.MaxValue = never seen).</summary>
		public int Since(string name) => availableSince.TryGetValue(name, out var tick) ? tick : int.MaxValue;

		public int DoneCount => done.Count;
	}
}
