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
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// M13 tempo (BV_m13_liveness saturation arm): the pure decisions behind
	/// UnitBuilderBotModuleCA's banked-cash override. Armed = a positive per-tier
	/// BotLimits.BankSaturationCash plus the switch field on the shared @generic
	/// instance; SwitchCondition keeps it inert on classic. Everything not listed
	/// here — the cash floor, pause votes, queue emptiness, buildability — is
	/// unchanged upstream behaviour.
	/// </summary>
	public static class ProductionSaturationCA
	{
		/// <summary>0/disabled fails closed; saturation needs cash+resources at or above the tier bar.</summary>
		public static bool Saturated(long cashAndResources, int threshold) =>
			threshold > 0 && cashAndResources >= threshold;

		/// <summary>Off = the legacy MaximiseProductionCash break after each filled category;
		/// saturated sweeps every existing category while the bank holds.</summary>
		public static bool StopCategorySweep(bool saturated, long cashAndResources, int maximiseCash) =>
			!saturated && cashAndResources < maximiseCash;

		/// <summary>Saturated lifts the per-category cap to two free queues; a wider
		/// configured provider width still wins. Off = the legacy single-queue fill.</summary>
		public static int QueuesPerCategory(bool saturated, int providerWidth) =>
			saturated ? Math.Max(2, providerWidth) : Math.Max(1, providerWidth);

		/// <summary>Second pick when only the proportional-share test rejected every candidate.
		/// Callers keep every other gate (membership, UnitLimits, delays, intervals,
		/// composition narrowing, aircraft caps, buildability) inside <paramref name="eligible"/>.</summary>
		public static string PickExcludingShare(IEnumerable<string> candidates,
			Func<string, bool> buildable, Func<string, bool> eligible)
		{
			foreach (var name in candidates)
				if (buildable(name) && eligible(name))
					return name;

			return null;
		}
	}
}
