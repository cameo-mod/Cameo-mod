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
	/// FIRST-COPY-DUE-01 (row ub_first_copy_due; RULES_AUDIT_04 A5, user D3): every newly available unit type gets a real first-copy deadline (about two minutes, a learned knob). The old
	/// first-copy boost only raises a weight, so a type could lose the roll for any number of draws. This is the guarantee: a type whose clock has run past the deadline and that was never
	/// ordered is picked before the roll, the one overdue LONGEST first and, on a tie, the CHEAPER type. Production is pay-as-you-go, so a bank below the full price does not stop a start
	/// while there is income: the clock only pauses when there is no cash AND no income. Pure integer math: no game state, no random draw, deterministic on every client.
	/// </summary>
	public static class FirstCopyDue
	{
		/// <summary>
		/// One advance credits at most this many ticks. The clock is WALL time since the type was first seen buildable (user D3: it pauses only with no cash and no income; a hard gate such as a limit,
		/// a delay or a lost prerequisite does not pause it, the same wall time the old first-copy boost uses). The sweep samples every bot visit, so the cap only bounds a gap in which the bot
		/// was not visited at all (for example its module was switched off), and that gap is not credited.
		/// </summary>
		public const int MaxStepTicks = 250;

		/// <summary>The clock moves while the player has any cash (bank above the learned floor) or any income. Both missing = the economy is dead and the deadline waits.</summary>
		public static bool Alive(long bank, long cashFloor, bool hasIncome) => bank > Math.Max(0, cashFloor) || hasIncome;

		/// <summary>The ticks credited for one visit: the time since the previous advance, capped at MaxStepTicks, and zero while the economy is dead or the clock is new (previous below 0).</summary>
		public static int Step(int previousTick, int tick, bool alive) =>
			!alive || previousTick < 0 || tick <= previousTick ? 0 : Math.Min(MaxStepTicks, tick - previousTick);

		/// <summary>How far past its deadline a clock is (credited - due); negative = not due yet. A due of 0 or less disables the service (nothing is ever due).</summary>
		public static long Overdue(long credited, int dueTicks) => dueTicks <= 0 ? long.MinValue : credited - dueTicks;

		/// <summary>
		/// The index to start first, or -1 when none is due: among the candidates that are due (overdue at or above 0) the one overdue longest, a tie goes to the cheaper price, a further tie to the
		/// lower index (the pool is name-ordered, so the result is deterministic). Candidates that are already ordered, or not eligible, must carry int.MinValue in overdue.
		/// </summary>
		public static int Pick(IReadOnlyList<long> overdue, IReadOnlyList<int> cost)
		{
			var best = -1;
			for (var i = 0; i < overdue.Count; i++)
			{
				if (overdue[i] < 0)
					continue;

				if (best < 0 || overdue[i] > overdue[best] || (overdue[i] == overdue[best] && cost[i] < cost[best]))
					best = i;
			}

			return best;
		}
	}
}
