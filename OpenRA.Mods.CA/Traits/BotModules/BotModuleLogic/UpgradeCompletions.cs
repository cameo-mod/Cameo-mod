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
		public bool Complete(string name) => done.Add(name);

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
