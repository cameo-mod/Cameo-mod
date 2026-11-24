#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// TEAM-LEAD-01: a squad that took part in the leader's order walks to the order's one rally point and waits there with the rest of the
	/// team's armies. When the order pushes, it attack-moves onto the order's target together with all of them. A contact on the way is fought
	/// like a staging squad's. DEFEND-ALLY may still draft units from a gathering squad (defence comes first). Lives in its own file: the
	/// squad manager enters this state from outside, GroundStatesCA is not touched.
	/// </summary>
	class TeamLeadGatherStateCA : IState
	{
		readonly TeamLeadOrder order;

		public TeamLeadGatherStateCA(TeamLeadOrder order)
		{
			this.order = order;
		}

		public void Activate(SquadCA owner)
		{
			// TEAM-PINCER-01: on a pincer the squad gathers at its own prong's staging point (the first ask shares it out), by the threat-aware
			// route when a router has one; any other order gathers at the one rally point as before.
			var manager = owner.SquadManager;
			var cell = owner.World.Map.CellContaining(manager.TeamLeadRallyOf(owner, order));
			var route = order.Pincer && owner.Units.Count > 0 ? manager.RouteAroundThreat(owner.Units[0].Actor, cell) : null;
			foreach (var u in owner.Units)
			{
				var queued = false;
				if (route != null)
				{
					foreach (var waypoint in route)
					{
						owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, waypoint), queued));
						queued = true;
					}
				}

				owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, cell), queued));
			}
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var manager = owner.SquadManager;
			if (!manager.TeamLeadOn || order.Cancelled)
			{
				manager.TeamLeadAbandon(owner, !manager.TeamLeadOn ? "not_engaged" : "cancelled:" + (order.CancelReason ?? "unknown"));
				return;
			}

			// Contact on the way: fight at the point of contact; the squad leaves the order.
			var enemy = manager.FindClosestEnemy(owner.Units[0].Actor, WDist.FromCells(manager.Info.AttackScanRadius), owner);
			if (enemy != null)
			{
				manager.TeamLeadNoteContact(owner, order);
				owner.TargetActor = enemy;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);
				return;
			}

			manager.TeamLeadReport(owner, order);
			if (!order.Pushed)
				return;

			if (!manager.TeamLeadResolveTarget(owner, order))
			{
				manager.TeamLeadAbandon(owner, "resolve_failed");
				return;
			}

			manager.TeamLeadNoteGo(owner, order);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
		}

		public void Deactivate(SquadCA owner)
		{
			order.Forget(owner);
		}
	}
}
