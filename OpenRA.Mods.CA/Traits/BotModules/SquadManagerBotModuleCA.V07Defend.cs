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
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// V07 TEAM-DEFEND, unit side (USER master replay 2026-11-24, via Opus). Reached only behind Info.TeamLeadDefendTeamEnabled: with the switch off no assessment runs, no
	// alarm is raised and no unit is sent. The work is one throttled look per TeamLeadDefendTeamRaiseTicks (only when a friendly was damaged near this bot) and one
	// pass per TeamLeadDefendTeamSendTicks while an alarm is in force; nothing runs per tick otherwise.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateDefendTeam;
		int teamDefendNextRaise;
		int teamDefendNextSend;
		readonly List<(uint Id, WPos Pos)> teamDefendEnemies = [];
		readonly List<Actor> teamDefendEnemyActors = [];

		// Read by every teammate (the alarm is team state); needs the engaged team, so it is off for a bot with no allied bot.
		internal bool TeamDefendOn => TeamLeadOn && Info.TeamLeadDefendTeamEnabled && !ablateDefendTeam.Off(Player, AblationIds.TeamLeadDefendTeam);

		// Called by the DEFEND-ALLY scan with the friendlies whose hit points dropped near this bot's responders: the look is throttled, and at most the first
		// three damaged friendlies are assessed.
		void TeamDefendScan(int now)
		{
			if (defendAllyAttacked.Count == 0 || now < teamDefendNextRaise || !TeamDefendOn || teamLeadAliveSeats < 2)
				return;

			teamDefendNextRaise = now + Math.Max(1, Info.TeamLeadDefendTeamRaiseTicks);
			for (var i = 0; i < defendAllyAttacked.Count && i < 3; i++)
				if (TeamDefendAssess(defendAllyAttacked[i], now))
					break;
		}

		// Called from RespondToAttack when an own priority asset (harvester, building, base builder) is hit: the victim's own side raises the alarm even when none
		// of its idle units stands near.
		void TeamDefendRaiseOwn(Actor victim)
		{
			var now = World.WorldTick;
			if (now < teamDefendNextRaise || !TeamDefendOn || teamLeadAliveSeats < 2)
				return;

			teamDefendNextRaise = now + Math.Max(1, Info.TeamLeadDefendTeamRaiseTicks);
			TeamDefendAssess(victim, now);
		}

		// The armed enemies (seen) and the armed friendlies (own, allied) around the victim, worth by cost. A serious attack raises the alarm (it takes the board by
		// TeamDefendCA.Replaces). True = a serious attack was found.
		bool TeamDefendAssess(Actor victim, int now)
		{
			var team = teamLeadTeam;
			if (team == null || victim == null || victim.IsDead || !victim.IsInWorld || victim.Owner == null)
				return false;

			var scan = WDist.FromCells(Info.DefendAllyAttackerScanCells);
			teamDefendEnemies.Clear();
			teamDefendEnemyActors.Clear();
			long attack = 0;
			long defence = 0;
			foreach (var e in World.FindActorsInCircle(victim.CenterPosition, scan))
			{
				if (e.IsDead || !e.IsInWorld || e.Owner == null || e.Info.HasTraitInfo<HuskInfo>() || !e.Info.HasTraitInfo<AttackBaseInfo>())
					continue;

				if (e.Owner == Player || Player.RelationshipWith(e.Owner) == PlayerRelationship.Ally)
				{
					defence += UnitValue(e);
					continue;
				}

				if (!IsPreferredObservedEnemyUnit(e))
					continue;

				attack += UnitValue(e);
				teamDefendEnemies.Add((e.ActorID, e.CenterPosition));
				teamDefendEnemyActors.Add(e);
			}

			var pick = DefendAllyCA.NearestAttacker(victim.CenterPosition, teamDefendEnemies, scan.Length);
			if (pick < 0 || !TeamDefendCA.Serious(attack, Info.TeamLeadDefendTeamMinAttackValue))
				return false;

			var attacker = teamDefendEnemyActors[pick];
			var alarm = new TeamDefendCA.Alarm(now, TeamLeadKeyOf(victim.Owner), victim.ActorID, victim.CenterPosition, attacker.ActorID, attacker.CenterPosition, attack, defence);
			if (TeamDefendCA.Replaces(team.DefendAlarm, alarm, now, Info.TeamLeadDefendTeamAlarmTicks))
			{
				team.DefendAlarm = alarm;
				if (DiagnosticWriter != null)
					DiagnosticTeamEvent("team_defend_alarm", "victim=" + alarm.VictimKey + " actor=" + victim.Info.Name + "#" + victim.ActorID + " attacker=" + attacker.Info.Name + "#" + attacker.ActorID
						+ " attack=" + attack + " defence=" + defence + " would_die=" + (TeamDefendCA.WouldDie(attack, defence, Info.TeamLeadDefendTeamDiePercent) ? 1 : 0) + " by=" + teamLeadKey);
			}

			return true;
		}

		// Every teammate but the victim's own bot answers the alarm in force: its pool units and its home-guard (Protection) squad go to the attacker, its free squads
		// are redirected, and a blob that already pushed is redirected only when the ally would die. A bot that is fighting at its own base keeps its units.
		void TeamDefendTick(IBot bot)
		{
			if (!TeamDefendOn || teamLeadTeam?.DefendAlarm is not { } alarm)
				return;

			var now = World.WorldTick;
			if (!TeamDefendCA.Active(now, alarm.Tick, Info.TeamLeadDefendTeamAlarmTicks) || now < teamDefendNextSend || alarm.VictimKey == teamLeadKey)
				return;

			teamDefendNextSend = now + Math.Max(1, Info.TeamLeadDefendTeamSendTicks);
			var attacker = World.GetActorById(alarm.AttackerId);
			if (attacker == null || attacker.IsDead || !attacker.IsInWorld)
				return;

			// Own base first: a Protection squad that fights an enemy inside this bot's own base radius keeps its units and the pool stays for it.
			var home = GetSquadOfType(SquadCAType.Protection);
			var baseRadius = (long)Info.MaxBaseRadius * Info.MaxBaseRadius;
			if (home is { IsValid: true } && home.IsTargetValid && (World.Map.CellContaining(home.Target.CenterPosition) - initialBaseCenter).LengthSquared <= baseRadius)
				return;

			var wouldDie = TeamDefendCA.WouldDie(alarm.AttackValue, alarm.DefenceValue, Info.TeamLeadDefendTeamDiePercent);
			var leases = BotUnitLeases.Of(Player);
			var drafted = 0;
			var draftable = unitsHangingAroundTheBase.Where(u => !unitCannotBeOrdered(u.Actor) && IsDefenderDraftable(u, leases)).ToList();
			if (draftable.Count > 0)
			{
				home ??= RegisterNewSquad(bot, SquadCAType.Protection, attacker);
				foreach (var u in draftable)
				{
					home.Units.Add(u);
					unitsHangingAroundTheBase.Remove(u);
				}

				drafted = draftable.Count;
			}

			var homeUnits = 0;
			if (home != null && home.IsValid && !home.IsTargetValid)
			{
				home.TargetActor = attacker;
				homeUnits = home.Units.Count - drafted;
			}

			var idle = 0;
			var pushed = 0;
			foreach (var s in Squads)
			{
				if (s == home || !s.IsValid)
					continue;

				var free = DefendAllyFreeSquad(s);
				var blob = !free && s.Type == SquadCAType.Rush && s.TeamPushSeq > 0 && s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA;
				if (!TeamDefendCA.SquadResponds(free, blob, wouldDie))
					continue;

				s.TargetActor = attacker;
				s.FuzzyStateMachine.ChangeState(s, new GroundUnitsAttackMoveStateCA(), false);
				if (free)
					idle += s.Units.Count;
				else
					pushed += s.Units.Count;
			}

			if (drafted + homeUnits + idle + pushed == 0)
				return;

			// Claim the draft now, not at the next heartbeat (same reason as ProtectOwn and DEFEND-ALLY-01).
			ReconcileSquadLeases();
			if (DiagnosticWriter != null)
				DiagnosticTeamEvent("team_defend_send", "victim=" + alarm.VictimKey + " pool=" + drafted + " home=" + homeUnits + " idle=" + idle + " pushed=" + pushed + " would_die=" + (wouldDie ? 1 : 0)
					+ " attacker=" + attacker.Info.Name + "#" + attacker.ActorID + " age=" + (now - alarm.Tick));
		}
	}
}
