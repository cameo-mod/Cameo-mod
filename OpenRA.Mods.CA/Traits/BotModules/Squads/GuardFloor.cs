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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// CONSOLIDATE-03 P7 (b), row sq_guard_floor (design 01 R4 "guard minimum", Opus review change 2). The home guard (R4) declares a minimum: guard-lend lends only the units above it. The minimum is a LEARNED number, not a
	/// constant: the personality prior comes from the yaml (turtle high, rush and steamroller low) and a learner overrides it per player under the keys below (the same contract as UnitRollTable, one table per module
	/// instance, so every own personality keeps its own table; the 4-value aggregation of global, own faction, enemy faction and observed enemy style is the learner's job, LEARN-MULT-01). During an open all-in the
	/// floor is 0, unless it is being enforced for an own-base defence (R1/R3). The lend-return tick is a learnable input of the same table. Pure: no world access, no RNG.
	/// </summary>
	public static class GuardFloor
	{
		/// <summary>An open all-in commits the army: the guard may lend up to the most a guard ever lends.</summary>
		public const int AllInLendPercent = GuardLend.MaxPercent;

		/// <summary>The guard units kept at home: floorPct percent of the guard, rounded up (never more than the guard).</summary>
		public static int Units(int guardUnits, int floorPct) =>
			guardUnits <= 0 ? 0 : (int)Math.Min(guardUnits, (((long)guardUnits * Math.Clamp(floorPct, 0, 100)) + 99) / 100);

		/// <summary>The floor in units: 0 while an all-in is open (the dynamic all-in rule), unless the floor is enforced for an own-base defence (R1/R3); the percent floor otherwise.</summary>
		public static int FloorUnits(int guardUnits, int floorPct, bool allInOpen, bool ownBaseDefenceEnforced) =>
			allInOpen && !ownBaseDefenceEnforced ? 0 : Units(guardUnits, floorPct);

		/// <summary>
		/// The units a guard lends once it keeps its floor: the planned lend, never more than the units above the floor, and only as a batch of at least the join floor (a squad needs it), like GuardLend.Units. 0 = nothing.
		/// </summary>
		public static int Lend(int plannedLend, int guardUnits, int floorUnits, int joinFloorUnits)
		{
			var spare = Math.Max(0, guardUnits - Math.Max(0, floorUnits));
			var lend = Math.Min(Math.Max(0, plannedLend), spare);
			return lend >= Math.Max(1, joinFloorUnits) ? lend : 0;
		}

		/// <summary>A lent unit is due back at the guard once the return window has run (0 or less = no return).</summary>
		public static bool ReturnDue(int now, int lentTick, int returnTicks) => returnTicks > 0 && lentTick >= 0 && now - lentTick >= returnTicks;
	}

	/// <summary>
	/// The learnable numbers of the guard floor: the percent of the guard kept at home and the lend-return window. The yaml fills the prior (per personality block); a learner overrides either under a stable key; clearing
	/// an override restores the prior. Every value is clamped, so the table never holds a negative or runaway number.
	/// </summary>
	public sealed class GuardFloorTable
	{
		public const string PercentKey = "guardfloor.pct";
		public const string ReturnKey = "guardfloor.return";
		public const int MaxReturnTicks = 20000;

		readonly int percent;
		readonly int returnTicks;
		readonly Dictionary<string, int> overrides = new(StringComparer.Ordinal);

		public GuardFloorTable(int percent, int returnTicks)
		{
			this.percent = percent;
			this.returnTicks = returnTicks;
		}

		/// <summary>The learner's overrides, by key (read only).</summary>
		public IReadOnlyDictionary<string, int> Overrides => overrides;

		public void Override(string key, int value) => overrides[key] = value;

		public void ClearOverride(string key) => overrides.Remove(key);

		public void ClearOverrides() => overrides.Clear();

		/// <summary>The percent of the guard kept at home, 0 to 100.</summary>
		public int FloorPct => Math.Clamp(overrides.TryGetValue(PercentKey, out var v) ? v : percent, 0, 100);

		/// <summary>The window after which a lent unit is due back, 0 to MaxReturnTicks (0 = no return).</summary>
		public int ReturnTicks => Math.Clamp(overrides.TryGetValue(ReturnKey, out var v) ? v : returnTicks, 0, MaxReturnTicks);
	}
}
