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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P7 (b), row sq_guard_floor (design 01 R4 guard minimum, Opus review change 2). With the row on the quiet home guard keeps a learned minimum: ArmyGuardPick hands the planned lend (the lend share) to
	// GuardFloorLend, which lets only the units above the floor go. The floor is a percent of the guard from the personality prior in the yaml (turtle high, rush and steamroller low), overridable per player through
	// GuardFloorParameters (the learner's table; the 4-value aggregation is its job). While an all-in order is open the WHOLE quiet guard may go (Opus second read of 8b1235d2b: a quiet guard is R7, the all-in claims R4-R7): the plan is every
	// joinable unit and the floor is 0, with no 90 percent constant and no ordinary share, neither would be a hold; only the join minimum applies. The one exception is an own core under attack: the learned floor is then
	// enforced and only the units above it go, computed from the R1 condition (CoreUnderAttackNow, see NoteCoreAttack), not assumed. The lend-return window is a key of the same table; its use at the return path comes after the baseline counters (the cancelled order already hands the units back).
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateGuardFloor;
		GuardFloorTable guardFloorTable;
		int guardFloorLogTick = -1000;
		int coreAttackTick = -1;

		/// <summary>sq_guard_floor: guard-lend is on (the row narrows its lend), the row is on and not ablated.</summary>
		internal bool GuardFloorOn => ArmyGuardLendOn && Info.GuardFloorEnabled && !ablateGuardFloor.Off(Player, AblationIds.SqGuardFloor);

		/// <summary>The learnable numbers of the guard floor of this seat (prior = the yaml): the learner overrides them under GuardFloorTable.PercentKey and ReturnKey.</summary>
		internal GuardFloorTable GuardFloorParameters => guardFloorTable ??= new GuardFloorTable(Info.GuardFloorPercent, Info.GuardFloorReturnTicks);

		/// <summary>
		/// R1 "own core under attack" as this seat last observed it: a hit on the yard, a production building or a refinery by an observed force at or above the home defence (the CoreAttackThreshold of the duty seam), within
		/// GuardFloor.CoreAttackHoldTicks. The same value is what the rank rows supply holder-side as DutyClaim.CoreUnderAttack once they are wired.
		/// </summary>
		internal bool CoreUnderAttackNow => GuardFloor.CoreAttackRecent(World.WorldTick, coreAttackTick);

		/// <summary>
		/// Records the R1 condition where the own-base answer measures it (ArbiterProtectOwn: the victim, the attack value of the attackers the bot SEES, the home defence at the scene). Only a tick is written; no unit,
		/// state or order changes. A hit by an attacker nobody saw has value 0 and never counts.
		/// </summary>
		void NoteCoreAttack(Actor victim, long observedAttack, long homeDefence)
		{
			if (!GuardFloorOn || victim == null || victim.IsDead)
				return;

			var yard = victim.Info.HasTraitInfo<BaseBuildingInfo>();
			var production = victim.Info.HasTraitInfo<BuildingInfo>() && victim.Info.HasTraitInfo<ProductionInfo>();
			var refinery = victim.Info.HasTraitInfo<RefineryInfo>();
			if (DutyArbiter.CoreUnderAttack(yard, production, refinery, observedAttack, homeDefence))
				coreAttackTick = World.WorldTick;
		}

		/// <summary>The units the quiet guard lends once it keeps its floor. Outside an all-in: the planned lend (the ordinary share), narrowed by the floor, never added to. Under an open all-in: every joinable unit above the floor, which is 0 unless the own core is under attack (R1).</summary>
		int GuardFloorLend(SquadCA guard, int plannedLend)
		{
			var count = guard.Units.Count;
			var allIn = TeamLeadOpenOrder() is { AllIn: true };
			var enforced = CoreUnderAttackNow;
			var plan = GuardFloor.Plan(plannedLend, armyGuardLend.Count, allIn);
			var floor = GuardFloor.FloorUnits(count, GuardFloorParameters.FloorPct, allIn, enforced);
			var lend = GuardFloor.Lend(plan, count, floor, Info.TeamLeadJoinMinUnits);
			if (DiagnosticWriter != null && World.WorldTick - guardFloorLogTick >= 250)
			{
				guardFloorLogTick = World.WorldTick;
				DiagnosticTeamEvent("sq_guard_floor", "guard=" + count + " floor=" + floor + " pct=" + GuardFloorParameters.FloorPct + " all_in=" + (allIn ? 1 : 0) + " core_attack=" + (enforced ? 1 : 0) + " planned=" + plannedLend + " plan=" + plan + " lend=" + lend);
			}

			return lend;
		}
	}
}
