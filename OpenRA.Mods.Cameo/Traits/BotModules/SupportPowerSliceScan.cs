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

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// SUPPORTPOWER-SLICE-01: the engine's support power target scan (SupportPowerBotASModule.FindAttackLocationToSupportPower) as a resumable loop.
	/// The rule is the engine's, term for term: walk the candidates in order; a candidate replaces the best only when its score is strictly higher than the best so far
	/// (which starts at 0) and at least the minimum. So the first of several equal maxima wins. A step examines at most `budget` candidates (0 or less = all that are left);
	/// with an unlimited budget one step is the engine's loop. No engine type here: the unit test drives it with plain values.
	/// </summary>
	public sealed class SupportPowerSliceScan<T>
	{
		readonly IReadOnlyList<T> candidates;
		int next;

		public SupportPowerSliceScan(IReadOnlyList<T> candidates, int startTick)
		{
			this.candidates = candidates;
			StartTick = startTick;
		}

		public int StartTick { get; }
		public int Count => candidates.Count;
		public bool Done => next >= candidates.Count;
		public bool HasBest { get; private set; }
		public T Best { get; private set; }
		public int BestScore { get; private set; }

		/// <summary>
		/// Examines up to `budget` candidates and returns how many it examined. `stillValid` is asked again for every candidate at the moment it is scored
		/// (a candidate that left the legal set since the snapshot is skipped, and still costs one unit of budget). `onBest` is told each new best, right after it was scored.
		/// </summary>
		public int Step(int budget, Func<T, bool> stillValid, Func<T, int> score, int minimum, Action<T> onBest = null)
		{
			var examined = 0;
			while (next < candidates.Count && (budget <= 0 || examined < budget))
			{
				var candidate = candidates[next++];
				examined++;
				if (!stillValid(candidate))
					continue;

				var considered = score(candidate);
				if (considered <= BestScore || considered < minimum)
					continue;

				BestScore = considered;
				Best = candidate;
				HasBest = true;
				onBest?.Invoke(candidate);
			}

			return examined;
		}
	}
}
