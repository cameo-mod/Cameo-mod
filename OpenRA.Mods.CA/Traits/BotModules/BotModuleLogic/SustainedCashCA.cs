#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// SUSTAINED-CASH-01 (USER 11-24): a bank that stays above the tier line for a long time means the factories cannot spend the income, so the next
	// building must be another production building. This is the timer; the pick is CycleProductionCA.
	public sealed class SustainedCashClock
	{
		// World tick plus one at which the bank first went above the line in the current unbroken run; 0 = not above.
		int sincePlusOne;
		int lastObserved = -1;

		/// <summary>One observation per world tick (a second call in the same tick is ignored). Any observation at or below the line resets the run.</summary>
		public void Observe(int now, bool aboveLine)
		{
			if (now == lastObserved)
				return;

			lastObserved = now;
			if (!aboveLine)
				sincePlusOne = 0;
			else if (sincePlusOne == 0)
				sincePlusOne = now + 1;
		}

		/// <summary>True when the bank has been above the line, unbroken, for at least <paramref name="holdTicks"/>.</summary>
		public bool Active(int now, int holdTicks) => sincePlusOne > 0 && now - (sincePlusOne - 1) >= holdTicks;
	}
}
