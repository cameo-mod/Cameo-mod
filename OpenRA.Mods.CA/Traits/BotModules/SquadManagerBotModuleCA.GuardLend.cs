#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	// PUSHBAR-RCA-01 (row sq_guard_lend; V08D 6v6 scored 0 team_push, 3 team_order, 2 team_cancel no_reports): the seats' home guards (Protection squads) held 56 to 126 of the team's
	// units, the pool and the quiet wave squads 3 to 16, so a team order found at most 3 joinable pool units per seat (the join floor) and was cancelled for no reports after 500 ticks,
	// before its 1500-tick gather window could push. A QUIET guard lends its spare units to the open order. Read only behind the one launcher (SquadPipeline02Enabled), and with
	// ArmyGuardLendPercent 0 or the DEV row off nothing here runs: the guard never lends and the idle army does not count it.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqGuardLend;

		// The units the quiet guard would lend now (filled by ArmyGuardPick; reused, never allocated per pass).
		readonly List<UnitWposWrapper> armyGuardLend = new();

		/// <summary>PUSHBAR-RCA-01 (row sq_guard_lend; needs the one launcher): a quiet home guard lends its spare units to the open order and they count in the idle army.</summary>
		internal bool ArmyGuardLendOn => ArmyLaunchArbiterOn && Info.ArmyGuardLendPercent > 0 && !ablateSqGuardLend.Off(Player, AblationIds.SqGuardLend);

		// The seat's quiet home guard and what it would lend now (armyGuardLend). False = nothing to lend. No enemy scan: the guard's own state sets its target for every enemy near it,
		// so a guard with no target is a guard with nothing to fight (the leader's blob count runs this on every team check).
		bool ArmyGuardPick(out SquadCA guard)
		{
			armyGuardLend.Clear();
			guard = GetSquadOfType(SquadCAType.Protection);
			if (guard == null || !guard.IsValid || ReferenceEquals(guard, m13ResponseSquad) || ReferenceEquals(guard, m13IntentSquad)
				|| guard.FuzzyStateMachine?.CurrentState is not (UnitsForProtectionIdleState or UnitsForProtectionAttackState))
				return false;

			var threatened = threatPredictionProviders != null && threatPredictionProviders.Any(p => p.PerceivedBaseThreat || p.PredictedThreats.Count > 0);
			if (!GuardLend.Quiet(guard.IsTargetValid, TryGetProtectionRally(out _), SelectProtectionRequest() != null, threatened))
				return false;

			var baseRadius = (long)Info.MaxBaseRadius * Info.MaxBaseRadius;
			foreach (var u in guard.Units)
				if (TeamLeadJoinable(u) && (u.Actor.Location - initialBaseCenter).LengthSquared <= baseRadius)
					armyGuardLend.Add(u);

			var lend = GuardLend.Units(guard.Units.Count, armyGuardLend.Count, Info.ArmyGuardLendPercent, Info.TeamLeadJoinMinUnits);
			if (lend <= 0)
			{
				armyGuardLend.Clear();
				return false;
			}

			armyGuardLend.RemoveRange(lend, armyGuardLend.Count - lend);
			return true;
		}

		// The member's idle army (TeamLeadBlob) counts what its quiet guard would lend, so the leader can publish the order the join below then fills.
		void ArmyGuardBlob(ref int units, ref long value)
		{
			if (!ArmyGuardLendOn || !ArmyGuardPick(out _))
				return;

			foreach (var u in armyGuardLend)
			{
				units++;
				value += UnitValue(u.Actor);
			}
		}

		// TeamLeadJoin, for an order that has not pushed: the quiet guard hands its spare units to the pool first, so the join bars see them and the squad forms on this very pass
		// (a squad that gathers for the order is kept from the ally defence by TeamLeadKeepsSquad).
		void ArmyGuardLendTo(TeamLeadOrder order)
		{
			if (!ArmyGuardLendOn || order.Pushed || !ArmyGuardPick(out var guard))
				return;

			foreach (var u in armyGuardLend)
			{
				guard.Units.Remove(u);
				unitsHangingAroundTheBase.Add(u);
			}

			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			if (DiagnosticWriter != null)
				DiagnosticTeamEvent("army_guard_lend", "seq=" + order.Seq + " units=" + armyGuardLend.Count + " guard=" + guard.Units.Count);
		}
	}
}
