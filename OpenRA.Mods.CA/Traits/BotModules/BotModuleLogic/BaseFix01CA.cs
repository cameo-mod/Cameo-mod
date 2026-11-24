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
	public enum OpeningRole
	{
		None,
		Power,
		Barracks,
		Refinery,
		WarFactory,
		Radar,
		Airfield,
		TechCenter,
		PassiveIncome,
	}

	public readonly record struct OpeningSlot(OpeningRole Role, int Required);

	// BASE-FIX-01 (AUDIT_BASE_01). Pure decisions, no state.
	public static class BaseFix01CA
	{
		/// <summary>
		/// BASE-02 (opening_standard_order, USER): the standard order, a soft guideline: power, barracks, refinery, war factory, refinery, then the 2nd barracks OR the
		/// 2nd war factory (<paramref name="secondBarracksFirst"/> picks which, per faction), radar, airfield, the other 2nd production building, tech center, the 3rd
		/// production buildings, passive income. The Required count is the number of the role owned or in production that satisfies the slot.
		/// </summary>
		public static OpeningSlot[] StandardOrder(bool secondBarracksFirst) =>
		[
			new(OpeningRole.Power, 1),
			new(OpeningRole.Barracks, 1),
			new(OpeningRole.Refinery, 1),
			new(OpeningRole.WarFactory, 1),
			new(OpeningRole.Refinery, 2),
			secondBarracksFirst ? new(OpeningRole.Barracks, 2) : new(OpeningRole.WarFactory, 2),
			new(OpeningRole.Radar, 1),
			new(OpeningRole.Airfield, 1),
			secondBarracksFirst ? new(OpeningRole.WarFactory, 2) : new(OpeningRole.Barracks, 2),
			new(OpeningRole.TechCenter, 1),
			new(OpeningRole.Barracks, 3),
			new(OpeningRole.WarFactory, 3),
			new(OpeningRole.PassiveIncome, 1),
		];

		/// <summary>
		/// The first slot of <paramref name="order"/> that can be built now (<c>Can</c>: the faction has it, the limits, delays and admission votes allow it) and is
		/// not yet owned or queued, skipping a slot that has been stuck too long. None = the sequence is done or nothing in it can be built now (the normal planner goes
		/// on). A Refinery result means "let the wanted-refinery rung pick it": the caller returns no pick of its own.
		/// </summary>
		public static OpeningRole NextRole(System.Collections.Generic.IReadOnlyList<OpeningSlot> order, Func<OpeningRole, (bool Can, int Have)> status,
			Func<OpeningRole, int, bool> stuck)
		{
			foreach (var slot in order)
			{
				var (can, have) = status(slot.Role);
				if (!can || have >= slot.Required || stuck(slot.Role, slot.Required))
					continue;

				return slot.Role;
			}

			return OpeningRole.None;
		}

		/// <summary>BASE-13: the margin capped at <paramref name="cap"/>, never below the base margin; off = unchanged.</summary>
		public static int CapPowerMargin(bool armed, int margin, int baseMargin, int cap) =>
			armed ? Math.Min(margin, Math.Max(baseMargin, cap)) : margin;

		/// <summary>BASE-04: a ground defence from the fraction roll is held while no threat has been seen.</summary>
		public static bool DefenceNeedHolds(bool armed, bool groundDefence, bool threatSeen) => armed && groundDefence && !threatSeen;
	}
}
