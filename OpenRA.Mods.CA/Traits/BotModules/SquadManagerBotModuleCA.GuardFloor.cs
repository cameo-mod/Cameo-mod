#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P7 (b), row sq_guard_floor (design 01 R4 guard minimum, Opus review change 2). With the row on the quiet home guard keeps a learned minimum: ArmyGuardPick hands the planned lend (the lend share) to
	// GuardFloorLend, which lets only the units above the floor go. The floor is a percent of the guard from the personality prior in the yaml (turtle high, rush and steamroller low), overridable per player through
	// GuardFloorParameters (the learner's table; the 4-value aggregation is its job). While an all-in order is open the floor is 0 and the guard may lend up to the most a guard ever lends (the dynamic all-in rule).
	// ArmyGuardPick only gets this far for a QUIET guard (no target, no held rally, no live request, no threat), so an own-base defence (R1/R3) is never live here: the "enforced for own-base defence" case keeps the guard's
	// units by not lending at all, earlier in the pick. The lend-return window is a key of the same table; its use at the return path comes after the baseline counters (the cancelled order already hands the units back).
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateGuardFloor;
		GuardFloorTable guardFloorTable;
		int guardFloorLogTick = -1000;

		/// <summary>sq_guard_floor: guard-lend is on (the row narrows its lend), the row is on and not ablated.</summary>
		internal bool GuardFloorOn => ArmyGuardLendOn && Info.GuardFloorEnabled && !ablateGuardFloor.Off(Player, AblationIds.SqGuardFloor);

		/// <summary>The learnable numbers of the guard floor of this seat (prior = the yaml): the learner overrides them under GuardFloorTable.PercentKey and ReturnKey.</summary>
		internal GuardFloorTable GuardFloorParameters => guardFloorTable ??= new GuardFloorTable(Info.GuardFloorPercent, Info.GuardFloorReturnTicks);

		/// <summary>The units the quiet guard lends once it keeps its floor (the planned lend, narrowed). Under an open all-in the floor is 0 and the planned lend is the all-in share.</summary>
		int GuardFloorLend(SquadCA guard, int plannedLend)
		{
			var count = guard.Units.Count;
			var allIn = TeamLeadOpenOrder() is { AllIn: true };
			if (allIn)
				plannedLend = GuardLend.Units(count, armyGuardLend.Count, GuardFloor.AllInLendPercent, Info.TeamLeadJoinMinUnits);

			var floor = GuardFloor.FloorUnits(count, GuardFloorParameters.FloorPct, allIn, false);
			var lend = GuardFloor.Lend(plannedLend, count, floor, Info.TeamLeadJoinMinUnits);
			if (DiagnosticWriter != null && World.WorldTick - guardFloorLogTick >= 250)
			{
				guardFloorLogTick = World.WorldTick;
				DiagnosticTeamEvent("sq_guard_floor", "guard=" + count + " floor=" + floor + " pct=" + GuardFloorParameters.FloorPct + " all_in=" + (allIn ? 1 : 0) + " planned=" + plannedLend + " lend=" + lend);
			}

			return lend;
		}
	}
}
