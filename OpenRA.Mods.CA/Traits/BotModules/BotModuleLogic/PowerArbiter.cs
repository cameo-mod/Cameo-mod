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
	// CONSOLIDATE-03 P4 (Opus FINISH-01, Sol RULE_CONFLICT_MAP_01 item 4): the ONE place that answers "is power short" and "does this building fit the power".
	// Before, the required margin (minimumExcessPower), the admission floor (ScaledBaseMinimumExcessPower) and six emergency comparisons were separate formulas.
	// Pure integer maths, no state: the same inputs give the same answer, so the order stream is unchanged when the P4 rows are off.
	public static class PowerArbiter
	{
		/// <summary>The sites that ask (the DEV stream row and its hash name them).</summary>
		public const int SiteS1 = 1, SiteOverride = 2, SiteEssential = 3, SiteModeBias = 4, SiteAdmit = 5, SiteRecovery = 6, SiteBlocked = 7;

		/// <summary>The required surplus margin: Min plus Increment per Threshold buildings, clamped to Max, the BASE-13 cap (pb_power_margin_cap), then the build-order power_margin knob.</summary>
		public static int Required(int min, int max, int increment, int threshold, int buildings, bool capOn, int cap, int knobMilli)
		{
			var bonus = increment * (buildings / threshold.Clamp(1, int.MaxValue));
			var margin = (min + bonus).Clamp(min, max);
			margin = BaseFix01CA.CapPowerMargin(capOn, margin, min, cap);
			return BotBuildOrderKnobs.Scale(margin, knobMilli);
		}

		/// <summary>The admission floor of a candidate building: the base margin scaled by the power_margin knob, plus the BP-2 radar reserve.</summary>
		public static int Floor(int min, int knobMilli, int radarReserve) => BotBuildOrderKnobs.Scale(min, knobMilli) + radarReserve;

		/// <summary>The surplus every test reads: the current excess, plus the power of the plants in production when the forecast is armed (an in-flight plant already answers the shortage).</summary>
		public static int Effective(int excess, int inFlight, bool forecastOn) => forecastOn ? excess + inFlight : excess;

		/// <summary>The one emergency predicate: a power manager and a surplus under the required margin.</summary>
		public static bool Short(bool hasManager, int excess, int required) => hasManager && excess < required;

		/// <summary>The candidate admission predicate: the building's own power plus the surplus reaches the floor (no manager: always).</summary>
		public static bool Admits(bool hasManager, int candidatePower, int excess, int floor) => !hasManager || candidatePower + excess >= floor;
	}
}
