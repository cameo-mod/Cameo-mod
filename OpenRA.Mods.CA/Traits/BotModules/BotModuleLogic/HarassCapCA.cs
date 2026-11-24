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
	/// HARASS-CAP-01 (USER P0, V02 RCA 2026-11-24): the pure rules that keep raiders from eating the blob. V02 had guerrilla/harass squads hold a
	/// large part of every seat's army while the team blob never reached the push bar. Three rules, each behind its own switch:
	/// the cap (raiders are at most a percent of the seat's combat units; the rest go to the pool = the blob), the minimum raid
	/// (a raid launches with at least HarassMinUnits; a smaller quiet squad merges into a bigger one or, after a hold, goes to the blob),
	/// and the recall (from the late-escalation start the raiders count into the blob and join the open team order).
	/// No game state here: the manager measures the numbers and acts on the answers.
	/// </summary>
	public static class HarassCapCA
	{
		/// <summary>What a quiet raid squad does this pass.</summary>
		public enum RaidAction
		{
			/// <summary>Big enough: it plays as before.</summary>
			Go,

			/// <summary>Too small, may still grow or merge: it holds (no launch).</summary>
			Hold,

			/// <summary>Too small and a bigger raid squad of its kind has room: its units move over.</summary>
			Merge,

			/// <summary>Too small for too long: its units go to the pool (the blob).</summary>
			ToPool
		}

		/// <summary>
		/// One more raider is allowed. <paramref name="total"/> counts the candidate; <paramref name="raiders"/> does not. While the raiders are still
		/// fewer than <paramref name="minRaid"/> (a first squad being formed) the cap must also be able to hold that many, or no raider is taken
		/// at all - a squad that could never reach the minimum raid would only hold units back. 100 = no cap, 0 = no raiders.
		/// </summary>
		public static bool Allows(int raiders, int total, int capPercent, int minRaid = 0)
		{
			if (capPercent >= 100)
				return true;

			var pct = Math.Max(0, capPercent);
			var all = Math.Max(1, (long)total);
			if ((raiders + 1L) * 100 > all * pct)
				return false;

			return raiders >= minRaid || minRaid * 100L <= all * pct;
		}

		/// <summary>The most raiders a seat with <paramref name="total"/> combat units may have at <paramref name="capPercent"/> (rounded down).</summary>
		public static int MaxRaiders(int total, int capPercent) => capPercent >= 100 ? Math.Max(0, total) : (int)(Math.Max(0, total) * (long)Math.Max(0, capPercent) / 100);

		/// <summary>
		/// A quiet raid squad of <paramref name="units"/> units, small since <paramref name="heldTicks"/> ticks. At least <paramref name="minUnits"/> =
		/// launches. Below it a merge target (a quiet squad of its kind with room) takes the units; with none the squad holds
		/// <paramref name="holdMaxTicks"/> and then goes to the pool (0 = at once, so no unit is held back for ever). minUnits 0 or 1 = every squad is big enough.
		/// </summary>
		public static RaidAction RaidDecision(int units, int minUnits, int heldTicks, int holdMaxTicks, bool mergeTarget)
		{
			if (units >= Math.Max(1, minUnits))
				return RaidAction.Go;

			if (mergeTarget)
				return RaidAction.Merge;

			return heldTicks >= Math.Max(0, holdMaxTicks) ? RaidAction.ToPool : RaidAction.Hold;
		}

		/// <summary>The raider recall is in force: its switch is on and the late-escalation start is reached.</summary>
		public static bool RecallActive(bool enabled, int tick, int lateStartTick) => enabled && tick >= lateStartTick;

		/// <summary>
		/// The tick a no-report order grace counts from: the issue tick, or the last squad report when that is later. An order whose squads all
		/// vanished (a personality switch throws every squad of the old module back to the pool) is given the grace again from there.
		/// </summary>
		public static int NoReportSince(int issueTick, int lastReportTick) => Math.Max(issueTick, lastReportTick);
	}
}
