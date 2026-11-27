#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// STALEMATE-SCOUT-01 (USER decision D5 on audit A7, via Opus): the only exception to "never stalemate" is a target that is genuinely unreachable.
	// A push that is ready with no known target scouts first (the unscouted or stale regions of the tactical map), then attacks what it finds.
	// Pure decisions only; the wiring is SquadManagerBotModuleCA.StalemateScout.cs.
	public static class StalemateScoutCA
	{
		public enum Verdict
		{
			/// <summary>A target is known: the push attacks it (the visible, remembered and focus tiers come before any hunt).</summary>
			Attack,

			/// <summary>No target known, a reachable unscouted or stale region was found: the push scouts it.</summary>
			Scout,

			/// <summary>No target known, stale regions exist but none can be walked to: the one excused case.</summary>
			Unreachable,

			/// <summary>No target known and no region left to scout (everything currently seen or fresh): not excused, the push keeps retrying.</summary>
			Explored,
		}

		/// <summary>After a region centre was refused as unwalkable, it is not asked again for this long.</summary>
		public const int RejectHoldTicks = 3000;

		/// <summary>The decision for a ready push. <paramref name="regionsChecked"/> is how many stale regions the scout pick had to try.</summary>
		public static Verdict Classify(bool targetKnown, bool scoutCellFound, int regionsChecked)
		{
			if (targetKnown)
				return Verdict.Attack;

			if (scoutCellFound)
				return Verdict.Scout;

			return regionsChecked > 0 ? Verdict.Unreachable : Verdict.Explored;
		}

		/// <summary>Only an unreachable target excuses a push that does not attack.</summary>
		public static bool Excused(Verdict verdict) => verdict == Verdict.Unreachable;

		/// <summary>The leader may carry a hunt cell on its order: as before (<paramref name="existing"/>: finish mode, all-in), or with the row on from <paramref name="startTick"/>.</summary>
		public static bool Hunts(bool existing, bool rowOn, int now, int startTick) => existing || (rowOn && now >= startTick);

		/// <summary>A refused region centre is skipped while its refusal is younger than <see cref="RejectHoldTicks"/>.</summary>
		public static bool Rejected(int rejectedTick, int now) => rejectedTick >= 0 && now - rejectedTick < RejectHoldTicks;

		/// <summary>The scouted cell counts as reached once the bot sees it or its leader stands within the arrive distance (DEV evidence only).</summary>
		public static bool Reached(bool visible, long distSq, int arriveCells) => visible || distSq <= (long)arriveCells * arriveCells;
	}
}
