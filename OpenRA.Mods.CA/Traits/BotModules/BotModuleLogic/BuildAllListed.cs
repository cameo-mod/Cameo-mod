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
	// BUILD-ALL-B (user ruling 2026-11-25): the build-once set is the BuildingFractions entries with a positive weight that the queue can build now, exactly
	// the original fraction machinery, with no extra list on top (no tech-chain list, no unlisted fallback). V08 seed 100775 showed why: an Ixian bot
	// placed about 30 concrete slabs (500 each, unlisted, removed on placement so never owned) and never its listed high-tech factory, whose full cost the
	// drained bank never reached. Pure, no world.
	public static class BuildAllListed
	{
		/// <summary>A type is a listed first copy: a BuildingFractions entry with a positive weight and none owned (variants counted) or in flight.</summary>
		public static bool ListedFirstCopy(IReadOnlyDictionary<string, int> fractions, string name, int count) =>
			name != null && fractions != null && fractions.TryGetValue(name, out var weight) && weight > 0 && count <= 0;

		/// <summary>
		/// Listed first copies in flight in ONE queue (separate queues never gate each other): <paramref name="queuedItems"/> are that queue's own queued items,
		/// <paramref name="countOf"/> the standing + in-flight count of a type. A queued type counts when it is listed and none of it stands beyond its queued copies.
		/// </summary>
		public static int FirstCopiesInQueue(IEnumerable<string> queuedItems, IReadOnlyDictionary<string, int> fractions, Func<string, int> countOf)
		{
			if (fractions == null || queuedItems == null)
				return 0;

			var n = 0;
			foreach (var group in queuedItems.GroupBy(i => i, StringComparer.Ordinal))
			{
				var queued = group.Count();
				if (fractions.TryGetValue(group.Key, out var weight) && weight > 0 && countOf(group.Key) - queued <= 0)
					n += queued;
			}

			return n;
		}

		/// <summary>
		/// True = test A6 is waived for this start: an S5 pick, a bank above 0 and no other listed first copy in flight (one pay-as-you-go start at a
		/// time). The caller has already checked that the switch is armed.
		/// </summary>
		public static bool Waives(BaseBuildStage stage, int bank, int firstCopiesInFlight) =>
			stage == BaseBuildStage.S5CoreOnce && bank > 0 && firstCopiesInFlight <= 0;
	}
}
