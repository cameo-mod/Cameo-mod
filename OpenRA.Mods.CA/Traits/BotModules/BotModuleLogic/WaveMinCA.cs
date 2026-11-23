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
using System.Globalization;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// WAVE-MIN-01 (USER rule, 2026-11-23): the World-free half of the attack-wave launch gate. A wave launches only when the idle pool
	/// holds at least N units AND at least V summed unit cost. N and V come from the difficulty line (BotLimits.WaveMinUnits /
	/// WaveMinValue, written in every tier); a personality may RAISE either bar through its own squad manager fields, never lower it.
	/// 0 means no limit (noob).
	/// </summary>
	public static class WaveMinCA
	{
		/// <summary>The bar actually applied: the tier value (negative = not written, counts as 0) raised by the personality floor.</summary>
		public static int Effective(int tierValue, int personalityFloor) => Math.Max(Math.Max(tierValue, 0), Math.Max(personalityFloor, 0));

		/// <summary>True when either bar is above zero, i.e. the gate can hold a wave back at all.</summary>
		public static bool Limits(int minUnits, int minValue) => minUnits > 0 || minValue > 0;

		/// <summary>Both bars must be met.</summary>
		public static bool Passes(int units, long value, int minUnits, int minValue) => units >= minUnits && value >= minValue;

		/// <summary>The roles of the units in a wave as "role:count" pairs in role order, for the DEV launch log. Unclassified units count as "none".</summary>
		public static string RoleSummary(IEnumerable<string> primaryRoles)
		{
			var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
			foreach (var role in primaryRoles)
			{
				var key = string.IsNullOrEmpty(role) ? "none" : role;
				counts.TryGetValue(key, out var n);
				counts[key] = n + 1;
			}

			return string.Join(",", counts.Select(kv => kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture)));
		}
	}
}
