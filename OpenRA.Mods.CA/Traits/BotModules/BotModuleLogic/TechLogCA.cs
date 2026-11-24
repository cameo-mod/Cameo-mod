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

namespace OpenRA.Mods.CA.Traits
{
	// TECH-LOG-01 (DEV only): pure helpers for tech_log.csv. The rows are written by the DEV sink; release never builds them.
	public static class TechLogCA
	{
		public const string FileName = "tech_log.csv";
		public const string Header = "tick,seat,faction,kind,type,reason";

		/// <summary>At most one rejected-pick row per type in this many ticks.</summary>
		public const int RejectThrottleTicks = 750;

		/// <summary>A rejected-pick row for a type is due at tick <paramref name="now"/> once its next-allowed tick has passed.</summary>
		public static bool RejectRowDue(int now, int nextAllowedTick) => now >= nextAllowedTick;

		public static string Row(int tick, string seat, string faction, string kind, string type, string reason) =>
			string.Join(",", tick, seat, faction, kind, type, reason);
	}
}
