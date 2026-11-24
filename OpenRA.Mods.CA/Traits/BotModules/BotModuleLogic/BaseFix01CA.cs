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

using System;

namespace OpenRA.Mods.CA.Traits
{
	public enum OpeningStep
	{
		None,
		Power,
		Barracks,
		WarFactory,
	}

	// BASE-FIX-01 (AUDIT_BASE_01). Pure decisions, no state.
	public static class BaseFix01CA
	{
		/// <summary>
		/// BASE-02: the next forced opening step. Power first, then the barracks, then the first refinery (None: the wanted-refinery rung picks it), then the
		/// war factory. Counts include orders in production. A step whose building the faction cannot build is skipped, and the opening ends at
		/// <paramref name="maxTicks"/>. Everything after the war factory (power if needed, the second refinery) is the normal ladder.
		/// </summary>
		public static OpeningStep NextOpeningStep(bool armed, int tick, int maxTicks, int powerCount, bool canPower, int barracksCount, bool canBarracks,
			int refineryCount, int warFactoryCount, bool canWarFactory)
		{
			if (!armed || tick >= maxTicks)
				return OpeningStep.None;

			if (powerCount <= 0)
				return canPower ? OpeningStep.Power : OpeningStep.None;

			if (barracksCount <= 0 && canBarracks)
				return OpeningStep.Barracks;

			if (refineryCount <= 0)
				return OpeningStep.None;

			if (warFactoryCount <= 0 && canWarFactory)
				return OpeningStep.WarFactory;

			return OpeningStep.None;
		}

		/// <summary>BASE-13: the margin capped at <paramref name="cap"/>, never below the base margin; off = unchanged.</summary>
		public static int CapPowerMargin(bool armed, int margin, int baseMargin, int cap) =>
			armed ? Math.Min(margin, Math.Max(baseMargin, cap)) : margin;

		/// <summary>BASE-04: a ground defence from the fraction roll is held while no threat has been seen.</summary>
		public static bool DefenceNeedHolds(bool armed, bool groundDefence, bool threatSeen) => armed && groundDefence && !threatSeen;
	}
}
