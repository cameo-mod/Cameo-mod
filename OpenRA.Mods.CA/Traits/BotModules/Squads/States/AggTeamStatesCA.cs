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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// IMPL02-AGG-TEAM (A4): an idle squad that joined an attack intent walks to the intent's rendezvous point, assembles, and starts together
	/// with the publisher (or after a bounded wait), then attack-moves onto the intent's target. A contact on the way is fought like a staging
	/// squad's. Lives in its own file: GroundStatesCA is not touched, the squad manager enters this state from outside.
	/// </summary>
	class AggTeamRendezvousStateCA : IState
	{
		readonly TeamAttackIntent intent;
		readonly int joinTick;

		public AggTeamRendezvousStateCA(TeamAttackIntent intent, int joinTick)
		{
			this.intent = intent;
			this.joinTick = joinTick;
		}

		public void Activate(SquadCA owner)
		{
			var cell = owner.World.Map.CellContaining(intent.Rendezvous);
			foreach (var u in owner.Units)
				owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, cell), false));
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var info = owner.SquadManager.Info;

			// Contact on the way: fight at the point of contact, like a staging squad.
			var enemy = owner.SquadManager.FindClosestEnemy(owner.Units[0].Actor, WDist.FromCells(info.AttackScanRadius), owner);
			if (enemy != null)
			{
				owner.TargetActor = enemy;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);
				return;
			}

			var now = owner.World.WorldTick;
			var radiusSquared = (long)WDist.FromCells(owner.SquadManager.AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamAssembleRadiusCells))).LengthSquared;
			var assembled = owner.Units.Count(u => (u.Actor.CenterPosition - intent.Rendezvous).LengthSquared <= radiusSquared);
			var percent = assembled * 100 / owner.Units.Count;
			if (!AggTeamPolicyCA.RendezvousCommit(now, percent, owner.SquadManager.AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamAssemblePercent)), intent.RendezvousTick, joinTick, owner.SquadManager.AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamStartWaitTicks))))
				return;

			if (!owner.SquadManager.AggTeamResolveTarget(owner, intent))
			{
				owner.SquadManager.AggTeamNoteJoinFailed(owner);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
				return;
			}

			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
		}

		public void Deactivate(SquadCA owner) { }
	}
}
