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
	/// REF-TIER-01 (USER BINDING 11-26, Opus ruling): the refinery target is a FLOOR for refinery demand, not a precedence hold. 2 refineries early, +2 once an own radar provider stands, +2 once
	/// an own tech-centre building stands (6 in all): the PRIOR, from Info, that the 4-value learning can replace later. The map caps it (only reachable fields with room under the per-field cap and
	/// a claimable anchor are ever lifted, never an anchorless refinery) and the personality scales it (a prior multiplier, rounded and clamped 1..8). Standing means a completed own building:
	/// a radar or tech centre that is lost drops the tier at once, and refineries above the target are never sold. Pure decisions, no state.
	/// </summary>
	public static class RefTier
	{
		public const int MinTarget = 1;
		public const int MaxTarget = 8;
		public const string ExpansionPersonality = "expansion";

		/// <summary>The target is recomputed at most this often (ticks): a lost radar or tech centre drops the tier within it.</summary>
		public const int RecomputeTicks = 25;

		/// <summary>The tier target before personality: the early target plus one step per completed tier (radar provider, tech centre).</summary>
		public static int BaseTarget(int early, int radarStep, int techStep, bool radarStanding, bool techStanding) =>
			early + (radarStanding ? radarStep : 0) + (techStanding ? techStep : 0);

		/// <summary>
		/// The personality prior as a percent of the tier target (100 = unchanged): expansion scales every tier, rush only while no radar stands, every other personality (and an unknown one) is 100.
		/// Row off (<paramref name="armed"/> false) = 100. The one seam the learned value replaces later.
		/// </summary>
		public static int PersonalityPct(bool armed, string personality, bool radarStanding, int expansionPct, int rushPreRadarPct)
		{
			if (!armed || personality == null)
				return 100;

			if (personality == ExpansionPersonality)
				return expansionPct;

			if (personality == Ref2First.RushPersonality && !radarStanding)
				return rushPreRadarPct;

			return 100;
		}

		/// <summary>The target: the tier target times the personality percent, rounded half up (2 x 75% = 2, 2 x 125% = 3), clamped to <see cref="MinTarget"/>..<see cref="MaxTarget"/>.</summary>
		public static int Target(int baseTarget, int pct)
		{
			var scaled = (baseTarget * pct + 50) / 100;
			return scaled < MinTarget ? MinTarget : scaled > MaxTarget ? MaxTarget : scaled;
		}

		/// <summary>The planner's zone-cap lift: +1 while fewer than the target refineries stand or are in production (in production counts, as for ref2: with the next one queued the lift drops).</summary>
		public static bool LiftsCap(bool armed, int ownStanding, int inProduction, int target) => armed && ownStanding + inProduction < target;

		/// <summary>Below the target the economy gate's interval wait is outranked (the first-war-factory wait, the first-copy yield and the expansion-refinery rules are not).</summary>
		public static bool OutranksIntervalWait(bool armed, int refineriesOwnedOrQueued, int target) => armed && refineriesOwnedOrQueued < target;

		/// <summary>DEV verdict: off, met (at or above the target), below_target (a claimable anchor exists) or below_no_anchor (below the target but no field can be claimed: the map caps it, nothing is held).</summary>
		public static string Reason(bool armed, int refineriesOwnedOrQueued, int target, int unclaimedAnchors) =>
			!armed ? "off" : refineriesOwnedOrQueued >= target ? "met" : unclaimedAnchors > 0 ? "below_target" : "below_no_anchor";
	}
}
