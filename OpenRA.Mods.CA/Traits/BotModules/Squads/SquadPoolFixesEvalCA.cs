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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>Pure decisions for the squad-pool hotspot fixes.</summary>
	public static class SquadPoolFixesEvalCA
	{
		/// <summary>Preserve pool order while dropping only entries whose key was dispatched.</summary>
		public static List<T> RetainUnassigned<T, TKey>(IEnumerable<T> pool, IEnumerable<TKey> assignedKeys, Func<T, TKey> keySelector)
		{
			var assigned = new HashSet<TKey>(assignedKeys);
			var retained = new List<T>();
			foreach (var item in pool)
				if (!assigned.Contains(keySelector(item)))
					retained.Add(item);

			return retained;
		}

		/// <summary>The legacy gate counts the idle pool; the fix counts only draftable units.</summary>
		public static bool MeetsAnswerPoolMinimum(int idleCount, int draftableCount, int minimum, bool countDraftable) =>
			(countDraftable ? draftableCount : idleCount) >= minimum;
	}
}
