#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CASH-LIVENESS-01 (user D6; Opus retarget 11-27 after the ABL_01 finding: the G1 idle excess was the BotGlobalUnitBudget unit cap, not cash).
	/// The pure rules of the three rows, each off in code:
	/// cl_stale_release: a unit-production pause vote that is not a cap is released once it has held for the hold age while the bank has spare cash;
	/// cl_cap_spend: while the unit cap holds with spare cash, the cash the unit reserve keeps goes to buildings and defences (inside their caps);
	/// cl_cap_allin: a team seat held at its unit cap long enough is an all-in trigger (an army that cannot grow should be used).
	/// Every threshold is an Info prior, learnable in DEV training only, and clamped here.
	/// </summary>
	public static class CashLiveness
	{
		/// <summary>The all-in reason of cl_cap_allin.</summary>
		public const string WhyBudgetCap = "budget_cap";

		public const int HoldTicksMin = 75;
		public const int HoldTicksMax = 7500;
		public const int SpareMin = 0;
		public const int SpareMax = 20000;
		public const int CapAllInTicksMin = 375;
		public const int CapAllInTicksMax = 7500;

		/// <summary>The age of one hold in ticks. <paramref name="since"/> is the hold's start (-1 = not holding); a tick without the hold resets it.</summary>
		public static int Age(ref int since, bool holding, int now)
		{
			if (!holding)
			{
				since = -1;
				return 0;
			}

			if (since < 0 || since > now)
				since = now;

			return now - since;
		}

		/// <summary>cl_stale_release: the hold has lasted at least <paramref name="maxTicks"/> and the bank holds at least <paramref name="spare"/>.
		/// The clock runs (and resets) on every call, whatever the answer.</summary>
		public static bool Released(ref int since, bool holding, int now, int maxTicks, long bank, int spare)
		{
			var age = Age(ref since, holding, now);
			return holding && age >= Math.Clamp(maxTicks, HoldTicksMin, HoldTicksMax) && bank >= Math.Clamp(spare, SpareMin, SpareMax);
		}

		/// <summary>cl_cap_spend: the unit cap holds and the bank has spare cash.</summary>
		public static bool CapSpare(bool capHolds, long bank, int spare) => capHolds && bank >= Math.Clamp(spare, SpareMin, SpareMax);

		/// <summary>cl_cap_allin: the cap has held for at least <paramref name="ticks"/> (sampled at every call; a sample without it resets the clock).</summary>
		public static bool Sustained(ref int since, bool holding, int now, int ticks)
		{
			var age = Age(ref since, holding, now);
			return holding && age >= Math.Clamp(ticks, CapAllInTicksMin, CapAllInTicksMax);
		}
	}
}
