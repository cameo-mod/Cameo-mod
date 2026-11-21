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

namespace OpenRA.Mods.CA
{
	/// <summary>
	/// STAGGER_DESIGN_06 9.2 class P with the R2 freeze (TIMER_TABLE_06 C001/C002): a call counter of the form
	/// <c>if (++calls % K == 0)</c> that is never reset, and that stands still while the module is not called (it is
	/// disabled), becomes ONE absolute due (<see cref="BotPeriod.F3"/> with c0 = 0). It is armed on the first call with
	/// r = v - 1: the first call is call 1, so at N = 1 call K is at tick v - 1 + K. The gap since this module's own
	/// previous call, minus the owner's visit gap (Elapsed), is time in which the old counter did not advance, so the due
	/// moves back by it. At N = 1 and for an ungated bot (a call every tick, Elapsed 1) it fires on exactly the calls
	/// the modulo did; at N &gt; 1 the period stays K world ticks and each firing is late by at most N - 1 ticks.
	/// A default struct is unarmed. Not saved: a load re-arms on the first call, as the old counter restarted at 0.
	/// </summary>
	public struct BotCallPeriod
	{
		BotPeriod due;
		int lastCall;
		bool armed;

		public bool Armed => armed;

		/// <summary>One call of the module at visit tick <paramref name="visitTick"/>; true when the old modulo test was true.
		/// <paramref name="phase"/> (QW-B slot phase in [0, K), used on the arming call only) moves every firing p calls later:
		/// calls K + p, 2K + p, ... (c0 = -p), never before call K. Phase 0 is the plain modulo.</summary>
		public bool Call(int visitTick, int elapsed, int k, int phase = 0)
		{
			if (!armed)
			{
				due = BotPeriod.F3(visitTick - 1, -phase, k);
				armed = true;
			}
			else
			{
				due.Freeze(Math.Max(0, visitTick - lastCall - Math.Max(1, elapsed)));
			}

			lastCall = visitTick;
			return due.TryFire(visitTick);
		}
	}
}
