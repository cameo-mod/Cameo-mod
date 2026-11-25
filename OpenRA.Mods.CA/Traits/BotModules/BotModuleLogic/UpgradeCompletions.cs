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
		readonly Dictionary<string, (int Tick, bool Seen)> pending = new(StringComparer.Ordinal);

		public const string SourceGrant = "grant";
		public const string SourceApprox = "approx";
		public const string SourceInit = "init";

		/// <summary>Staleness: an order that is neither seen completing nor granted after this many ticks is dropped (cancelled, or never accepted by the engine).</summary>
		public const int PendingMaxTicks = 3000;

		/// <summary>The service ordered the item: it is settled at the next visits against authoritative state, whatever happens between visits.</summary>
		public void Started(string name, int tick) => pending[name] = (tick, false);

		public bool AnyPending => pending.Count > 0;

		/// <summary>
		/// Settle the ordered items. Authoritative state decides: an item is done when the player holds a prerequisite it REALLY grants (grantsOf = the prerequisites its traits provide, which need not be
		/// its name), so an item that was ordered and finished between two visits is still found. An item that grants no prerequisite (condition-only) has no state to read: it is done by approximation
		/// when it left the queue (seen there, or ordered long enough ago) and is no longer offered, which is the one signal a cancelled item does not give for a non-repeatable item.
		/// Returns the newly completed (name, source) pairs.
		/// </summary>
		public List<(string Name, string Source)> Settle(int tick, ISet<string> inProduction, Func<string, IReadOnlyCollection<string>> grantsOf, ISet<string> owned, Func<ISet<string>> offered)
		{
			var result = new List<(string, string)>();
			if (pending.Count == 0)
				return result;

			ISet<string> offeredNow = null;
			foreach (var name in new List<string>(pending.Keys))
			{
				var (started, seen) = pending[name];
				if (inProduction.Contains(name))
				{
					pending[name] = (started, true);
					continue;
				}

				var grants = grantsOf(name);
				if (grants.Count > 0)
				{
					if (grants.Any(owned.Contains))
					{
						pending.Remove(name);
						if (Complete(name, SourceGrant))
							result.Add((name, SourceGrant));
					}
					else if (tick - started > PendingMaxTicks)
						pending.Remove(name);

					continue;
				}

				// condition-only: no prerequisite to read
				if (seen || tick - started > 100)
				{
					offeredNow ??= offered();
					pending.Remove(name);
					if (!offeredNow.Contains(name) && Complete(name, SourceApprox))
						result.Add((name, SourceApprox));
				}
				else if (tick - started > PendingMaxTicks)
					pending.Remove(name);
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
