#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using Policy = OpenRA.Mods.CA.Traits.BotModules.Squads.RetreatRallyPolicyCA;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// A10 RETREAT-RALLY (design DRAFT02 4.5): a retreating ground squad walks (Move) to the destination the manager chose, then supports the army there
	/// (AttackMove onto its target) or merges into the pool at an own idle squad. Entered only from the Flee state and the two idle flee sites, only
	/// while RetreatRallyOn; not a quiet state (A4, A5 and the A3 merge leave it alone; the detachment already stands a rallying squad down).
	/// The ends (RetreatRallyPolicyCA.NextState): merged_pool dismisses the squad into the pool; target_gone and the support expiry go Idle; the march expiry
	/// and abort_losing run the legacy flight at once (GroundUnitsFleeStateCA with baseOnly), so the squad never waits idle at a losing fight.
	/// </summary>
	class GroundUnitsRallyStateCA : StateBaseCA, IState
	{
		RetreatRallyPlan plan;
		Policy.RallyPhase phase = Policy.RallyPhase.March;
		Policy.RallyAction action;
		int supportSinceTick;
		int lastRescoreTick;
		int lastOrderTick = int.MinValue / 2;
		CPos? lastOrderCell;
		bool ended;

		public GroundUnitsRallyStateCA(RetreatRallyPlan plan)
		{
			this.plan = plan;
			action = plan.Action;
			lastRescoreTick = plan.PickTick;
		}

		public void Activate(SquadCA owner)
		{
			Order(owner, plan.DestPos);
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid || ended)
				return;

			var manager = owner.SquadManager;
			var now = owner.World.WorldTick;
			var observed = manager.RetreatRallyObserve(owner, plan, phase, action);

			// G7: the destination is re-scored every RescoreTicks (and at once when it became invalid); a switch needs the lock and the margin.
			if (!observed.Losing && (observed.DestInvalid || Policy.RescoreDue(lastRescoreTick, now, Param(owner, nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRescoreTicks)))))
			{
				lastRescoreTick = now;
				var next = manager.RetreatRallyRescore(owner, plan, observed.DestInvalid);
				if (next != null)
				{
					plan = next;
					action = plan.Action;
					phase = Policy.RallyPhase.March;
					lastOrderCell = null;
					Order(owner, plan.DestPos);
					return;
				}
			}

			var step = Policy.Step(new Policy.StepInput(phase, action, now - plan.StartTick, now - supportSinceTick, observed.Arrived, observed.Losing,
				observed.DestInvalid, observed.TargetEnded, observed.DestLaunched,
				Param(owner, nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMaxTicks)), Param(owner, nameof(SquadManagerBotModuleCAInfo.AggTeamRallySupportMaxTicks))));

			var atPhase = phase;
			if (phase == Policy.RallyPhase.March && step.Phase != Policy.RallyPhase.March)
			{
				// Arrival (Support or Merge): counted and, for a Support, committed in the agg-core sense.
				manager.RetreatRallyArrive(owner, plan, step.Action, observed.RatioMilli);
				atPhase = step.Phase;
			}

			action = step.Action;
			if (step.End != Policy.RallyEnd.None)
			{
				End(owner, step.End, atPhase);
				return;
			}

			if (atPhase == Policy.RallyPhase.Support && phase == Policy.RallyPhase.March)
			{
				phase = Policy.RallyPhase.Support;
				supportSinceTick = now;
				lastOrderCell = null;
			}

			Order(owner, phase == Policy.RallyPhase.Support ? observed.TargetPos : observed.DestPos);
		}

		static int Param(SquadCA owner, string name) => owner.SquadManager.RetreatRallyParam(name, owner);

		/// <summary>
		/// The leg's order, issued on entry and whenever the cell changed after a RescoreTicks: the march is a Move to the approach point in front of the
		/// destination (contacts on the way are not fought; march_flee answers a new threat), the support leg an AttackMove onto the army's target.
		/// </summary>
		void Order(SquadCA owner, WPos destination)
		{
			var manager = owner.SquadManager;
			var now = owner.World.WorldTick;
			if (lastOrderCell != null && now - lastOrderTick < Param(owner, nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRescoreTicks)))
				return;

			var map = owner.World.Map;
			var support = phase == Policy.RallyPhase.Support;
			var point = support ? destination
				: Policy.ApproachPoint(owner.CenterPosition, destination, Param(owner, nameof(SquadManagerBotModuleCAInfo.AggTeamRallyDestRadiusCells)));
			var cell = map.Clamp(map.CellContaining(point));
			if (lastOrderCell == cell)
				return;

			var members = owner.Units.Select(u => u.Actor).Where(a => !manager.unitCannotBeOrdered(a)).ToList();
			if (members.Count == 0)
				return;

			lastOrderCell = cell;
			lastOrderTick = now;
			if (support)
				QueueDeduped(owner, "AttackMove", SquadOrderKey.ForCell("AttackMove", cell), Target.FromCell(owner.World, cell), members);
			else
				QueueDeduped(owner, "Move", SquadOrderKey.ForCell("Move", cell), Target.FromCell(owner.World, cell), members);
		}

		void End(SquadCA owner, Policy.RallyEnd end, Policy.RallyPhase atPhase)
		{
			var manager = owner.SquadManager;
			ended = true;
			manager.RetreatRallyEnd(owner, plan, end, atPhase);
			switch (Policy.NextState(end, atPhase))
			{
				case Policy.RallyNext.DismissPool:
					manager.DismissSquad(owner);
					break;
				case Policy.RallyNext.Idle:
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
					break;
				case Policy.RallyNext.FleeBaseOnly:
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(true), false);
					break;
			}
		}

		// The state is replaced from outside only by a stand-down of the squad (the detachment dismisses it, the units are gone): a squad
		// that is replaced without an end of its own is booked as detached.
		public void Deactivate(SquadCA owner)
		{
			if (ended)
				return;

			ended = true;
			owner.SquadManager.RetreatRallyEnd(owner, plan, Policy.RallyEnd.Detached, phase);
		}
	}
}
