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
	/// <summary>
	/// REF2-FIRST-01 (USER BINDING 11-26, Opus rulings): every player has at least 2 refineries early. The second refinery comes BEFORE the radar (the TECH personality
	/// is exempt from that half) and BEFORE the 2nd production building of each type (the RUSH personality is exempt from that half, so an early all-in stays possible).
	/// Order stays ref1, first war factory, ref2: the first copy of any production type is never held. Pure decisions, no state. The gate never cancels anything in flight;
	/// it only keeps a NEW radar or 2nd production start from being offered while ref2 is due.
	/// </summary>
	public static class Ref2First
	{
		public const int RefineryTarget = 2;
		public const string TechPersonality = "tech";
		public const string RushPersonality = "rush";

		/// <summary>
		/// ref2 is due: the switch is armed, the gate has not finished (ref2 reached or the bound ran out), fewer than <see cref="RefineryTarget"/> refineries stand or are in
		/// production, a refinery type is buildable and not at its max, and some reachable field still has room for one (the planner claims one with the cap lift).
		/// No reachable field under its cap = not due: the gate is skipped rather than holding the order for a refinery that cannot be placed.
		/// </summary>
		public static bool Due(bool armed, bool finished, int refineriesOwnedOrQueued, bool refineryBuildable, bool refineryMaxed, bool fieldUnderCap) =>
			armed && !finished && refineriesOwnedOrQueued < RefineryTarget && refineryBuildable && !refineryMaxed && fieldUnderCap;

		/// <summary>
		/// The planner's zone-cap lift: +1 while fewer than <see cref="RefineryTarget"/> refineries stand OR are in production. In production counts, matching the claim gate
		/// (claimable anchors must exceed the refineries in flight): with ref2 queued the lift drops, so no 3rd refinery is claimed before ref2 lands.
		/// </summary>
		public static bool LiftsCap(bool armed, int ownStanding, int inProduction) => armed && ownStanding + inProduction < RefineryTarget;

		/// <summary>The bound ran out: ref2 has been due for more than <paramref name="boundTicks"/> ticks (dueSince negative = never due).</summary>
		public static bool BoundExpired(int dueSince, int now, int boundTicks) => dueSince >= 0 && now - dueSince > boundTicks;

		/// <summary>Row ref2_before_radar: a radar provider is held while ref2 is due, except for the TECH personality.</summary>
		public static bool HoldsRadar(bool due, bool isRadarProvider, string personality) =>
			due && isRadarProvider && !IsPersonality(personality, TechPersonality);

		/// <summary>
		/// Row ref2_before_prod2: the 2nd or later production building of a role (barracks, vehicle factory, naval, air) is held while ref2 is due, except for the RUSH
		/// personality. The first copy of a role is never held.
		/// </summary>
		public static bool HoldsSecondProduction(bool due, bool isProduction, int roleOwnedOrQueued, string personality) =>
			due && isProduction && roleOwnedOrQueued >= 1 && !IsPersonality(personality, RushPersonality);

		/// <summary>ref2 outranks the refinery interval: a due ref2 passes the economy gate's interval wait (the first-war-factory wait is untouched).</summary>
		public static bool OutranksIntervalWait(bool due, int refineriesOwnedOrQueued) => due && refineriesOwnedOrQueued < RefineryTarget;

		static bool IsPersonality(string personality, string name) => string.Equals(personality, name, System.StringComparison.OrdinalIgnoreCase);
	}
}
