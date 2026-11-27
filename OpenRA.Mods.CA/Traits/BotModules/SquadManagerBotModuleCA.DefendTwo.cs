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
	// TEAM-DEFEND-02 (row tm_defend_two_tier; design pending/TEAM_DEFEND_02_DESIGN_01.md, user-approved two-tier ally defence). T1 (the local 25-cell response, DefendAllyRespond) is NOT touched. This file is T2: the answer to the team alarm.
	// Every ally answers with no radius; the team sends the presence margin x the SEEN attack minus the defence already there, split by the spare value each seat published (a team ledger, TeamLeadTeam.DefendSpare/DefendCommit); a pushed blob keeps
	// its order unless the ally would die; the existing Protection-squad release returns the defenders when the threat is over. Fog-safe: nothing of an unseen attacker is read (the alarm carries its position, a blind alarm only the damaged
	// location). With the row off none of this runs and TeamDefendTick is byte-identical to before.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateTeamDefendTwo;

		// The Protection squad serves the alarm (set when T2 drafts into it) and the free squads T2 redirected; both reset when the alarm's victim changes or the alarm is over.
		bool defendTwoHome;
		string defendTwoVictim;
		readonly HashSet<SquadCA> defendTwoSquads = [];

		internal bool TeamDefendTwoOn => TeamDefendOn && DefendAllyActive && Info.TeamDefendTwoTierEnabled && !ablateTeamDefendTwo.Off(Player, AblationIds.TeamDefendTwoTier);

		/// <summary>A base asset: what RespondToAttack treats as a protected priority asset (building, harvester, base builder). Its damage alarms at any attack size under the row.</summary>
		static bool TeamDefendBaseAsset(Actor a) => a.Info.HasTraitInfo<HarvesterInfo>() || a.Info.HasTraitInfo<BuildingInfo>() || a.Info.HasTraitInfo<BaseBuildingInfo>();

		/// <summary>
		/// The alarm's attacker, only if this player can view it now: the visibility is tested before anything of the actor is read, and "unknown" and "not visible" are the same answer (null), so neither the position nor the liveness of an
		/// unseen attacker is read. A blind alarm (no attacker id) has none.
		/// </summary>
		Actor DefendTwoSeenAttacker(TeamDefendCA.Alarm alarm)
		{
			if (DefendTwo.IsBlind(alarm.AttackerId))
				return null;

			var a = World.GetActorById(alarm.AttackerId);
			return a != null && a.CanBeViewedByPlayer(Player) && a.IsInWorld && !a.IsDead ? a : null;
		}

		/// <summary>An enemy counts as an attacker of the assessment. Under the row only what this player can view now counts, whatever FoggedScans says (IsPreferredObservedEnemyUnit is fog-safe only with FoggedScans on); off: the old test.</summary>
		bool TeamDefendCountsAttacker(Actor e, bool two) => two ? IsPreferredEnemyUnit(e) && IsNotHiddenUnit(e) : IsPreferredObservedEnemyUnit(e);

		/// <summary>A1/A2 blind alarm: a base asset lost hit points and no attacker is seen. The alarm carries no attacker (id 0) and the damaged location as its position; the need is the blind value minus the defence already there. Never replaces a seen alarm of the same victim.</summary>
		bool TeamDefendRaiseBlind(TeamLeadTeam team, Actor victim, long defence, int now)
		{
			var alarm = new TeamDefendCA.Alarm(now, TeamLeadKeyOf(victim.Owner), victim.ActorID, victim.CenterPosition, 0, victim.CenterPosition, 0, defence);
			if (!DefendTwo.Replaces(team.DefendAlarm, alarm, now, Info.TeamLeadDefendTeamAlarmTicks))
				return true;

			team.DefendAlarm = alarm;
			if (DiagnosticWriter != null)
				DiagnosticTeamEvent("team_defend_alarm", "victim=" + alarm.VictimKey + " actor=" + victim.Info.Name + "#" + victim.ActorID + " attacker=none blind=1 defence=" + defence + " by=" + teamLeadKey);

			return true;
		}

		long DefendTwoScanSquared()
		{
			var r = (long)WDist.FromCells(Info.DefendAllyAttackerScanCells).Length;
			return r * r;
		}

		/// <summary>The target of a squad that answers the alarm: the attacker when this seat sees it, else the terrain position the alarm carries (never the actor of an unseen attacker, never the ally victim: an ally is not targetable).</summary>
		static void DefendTwoPoint(SquadCA squad, Actor seen, TeamDefendCA.Alarm alarm)
		{
			if (seen != null)
				squad.TargetActor = seen;
			else
				squad.Target = Target.FromPos(alarm.AttackerPos);
		}

		/// <summary>The value this seat has on ally defence that is still on its way (units beyond the attacker-scan radius of the victim; arrived units are already in the alarm's defence value).</summary>
		long DefendTwoCommit(TeamDefendCA.Alarm alarm, SquadCA home)
		{
			var units = new List<(long Value, long DistanceSquared)>();
			void Add(SquadCA s)
			{
				foreach (var u in s.Units)
					if (u.Actor != null && !u.Actor.IsDead && u.Actor.IsInWorld)
						units.Add((UnitValue(u.Actor), (u.Actor.CenterPosition - alarm.VictimPos).HorizontalLengthSquared));
			}

			if (defendTwoHome && home is { IsValid: true })
				Add(home);

			foreach (var s in defendTwoSquads)
				if (s.IsValid && !ReferenceEquals(s, home))
					Add(s);

			return DefendTwo.CommitValue(units, DefendTwoScanSquared());
		}

		/// <summary>
		/// A Protection squad that T2 pointed at a terrain position is valid and visible for its state forever, so the quiet-release test would never fire. It is cleared (the state then reads it as quiet and takes the existing
		/// ShouldReleaseDefenders / ReleaseDefenders path) when the alarm is over, or when its leader stands at the position (within the attacker-scan radius) and this seat sees no attacker there.
		/// </summary>
		void DefendTwoMaintain(SquadCA home, TeamDefendCA.Alarm alarm, bool active)
		{
			if (!defendTwoHome || home is not { IsValid: true } || home.Target.Type != TargetType.Terrain)
				return;

			var arrived = (home.Units[0].Actor.CenterPosition - home.Target.CenterPosition).HorizontalLengthSquared <= DefendTwoScanSquared();
			if (!active || (arrived && DefendTwoSeenAttacker(alarm) == null))
				home.Target = Target.Invalid;
		}

		/// <summary>One send pass of this seat under the row (called from TeamDefendTick; cadence TeamLeadDefendTeamSendTicks).</summary>
		void TeamDefendTwoTick(IBot bot)
		{
			var team = teamLeadTeam;
			var now = World.WorldTick;
			if (team?.DefendAlarm is not { } alarm || now < teamDefendNextSend)
				return;

			var active = TeamDefendCA.Active(now, alarm.Tick, Info.TeamLeadDefendTeamAlarmTicks);
			if (!active && defendTwoVictim == null && !defendTwoHome && defendTwoSquads.Count == 0)
				return;

			teamDefendNextSend = now + Math.Max(1, Info.TeamLeadDefendTeamSendTicks);
			var key = teamLeadKey;
			var home = GetSquadOfType(SquadCAType.Protection);
			defendTwoSquads.RemoveWhere(s => !s.IsValid || s.FuzzyStateMachine?.CurrentState is not GroundUnitsAttackMoveStateCA);

			// The alarm is over: nothing is sent, the ledger and this seat's bookkeeping are dropped, a terrain target is released.
			if (!active)
			{
				DefendTwoMaintain(home, alarm, false);
				defendTwoHome = false;
				defendTwoVictim = null;
				defendTwoSquads.Clear();
				team.ClearDefendLedger();
				return;
			}

			team.BindDefendLedger(alarm.VictimKey);
			var victimChanged = defendTwoVictim != alarm.VictimKey;
			defendTwoVictim = alarm.VictimKey;

			// Own base first: the victim's own bot does not answer, and a Protection squad that fights an enemy inside this bot's own base radius keeps its units and the pool (a squad T2 pointed at the ally does not count as fighting at home).
			var baseRadius = (long)Info.MaxBaseRadius * Info.MaxBaseRadius;
			var fightingHome = !defendTwoHome && home is { IsValid: true } && home.IsTargetValid
				&& (World.Map.CellContaining(home.Target.CenterPosition) - initialBaseCenter).LengthSquared <= baseRadius;
			if (alarm.VictimKey == key || fightingHome)
			{
				team.PublishDefendSpare(key, 0, now);
				team.PublishDefendCommit(key, DefendTwoCommit(alarm, home), now);
				DefendTwoMaintain(home, alarm, true);
				return;
			}

			var arbiter = DefendArbiterOn;
			var priority = DefendArbiterCA.Ally(alarm.AttackValue, alarm.DefenceValue);
			var wouldDie = arbiter ? priority == DefendPriority.P2AllyFalls : TeamDefendCA.WouldDie(alarm.AttackValue, alarm.DefenceValue, Info.TeamLeadDefendTeamDiePercent);
			var seen = DefendTwoSeenAttacker(alarm);
			var attackerPos = alarm.AttackerPos;
			var leases = BotUnitLeases.Of(Player);

			// A new victim: the defenders already on the way (home guard and redirected squads) are pointed at the new alarm; they keep counting as commit.
			if (victimChanged)
			{
				if (defendTwoHome && home is { IsValid: true })
					DefendTwoPoint(home, seen, alarm);

				foreach (var s in defendTwoSquads)
				{
					DefendTwoPoint(s, seen, alarm);
					s.FuzzyStateMachine.ChangeState(s, new GroundUnitsAttackMoveStateCA(), false);
				}
			}

			// What this seat could send: the draftable pool units and the free squads the arbiter lets answer. A pushed or gathering blob is not spare; it answers only when the ally would die.
			var draftable = unitsHangingAroundTheBase.Where(u => !unitCannotBeOrdered(u.Actor) && IsDefenderDraftable(u, leases)).ToList();
			long spare = 0;
			var candidates = new List<DefendCandidate>(draftable.Count);
			for (var i = 0; i < draftable.Count; i++)
			{
				var value = UnitValue(draftable[i].Actor);
				spare += value;
				candidates.Add(new DefendCandidate(i, value, (draftable[i].Actor.CenterPosition - attackerPos).HorizontalLengthSquared));
			}

			var freeSquads = new List<(SquadCA Squad, int Value, long DistanceSquared)>();
			var blobs = new List<SquadCA>();
			foreach (var s in Squads)
			{
				if (s == home || !s.IsValid)
					continue;

				var free = DefendAllyFreeSquad(s);
				if (arbiter)
				{
					var holder = ArmyDefendHolderOf(s);
					if (s.Type != SquadCAType.Rush || ReferenceEquals(s, stalemateSquad) || s.M13Response || !DefendArbiterCA.MayTake(priority, ArmyRole.Main, holder))
						continue;

					free = holder == ArmyHolder.Quiet;
				}
				else
				{
					var blob = !free && s.Type == SquadCAType.Rush && s.TeamPushSeq > 0 && s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA;
					if (!TeamDefendCA.SquadResponds(free, blob, wouldDie))
						continue;
				}

				if (free)
				{
					var value = SquadValueOf(s);
					spare += value;
					freeSquads.Add((s, value, (s.CenterPosition - attackerPos).HorizontalLengthSquared));
				}
				else
					blobs.Add(s);
			}

			// The ledger, in this order: publish the spare of the pre-send snapshot, read the sums, compute the quota, send, then publish the commit from the live squads.
			team.PublishDefendSpare(key, spare, now);
			var others = team.DefendSumsExcept(key, now, DefendTwo.LedgerMaxAgeTicks);
			var blind = DefendTwo.IsBlind(alarm.AttackerId);
			var need = blind ? DefendTwo.BlindNeed(Info.TeamDefendBlindValue, alarm.DefenceValue) : DefendTwo.Need(alarm.AttackValue, alarm.DefenceValue, Info.TeamDefendMarginPercent);
			var committed = others.Commit + DefendTwoCommit(alarm, home);
			var quota = DefendTwo.Quota(need, committed, spare, others.Spare + spare);

			// Pool units, nearest the attacker position first, until the quota is reached (no radius, no preservation reserve: the quota is the bound).
			var drafted = 0;
			long sent = 0;
			var picked = DefendArbiterCA.PickNearest(candidates, quota);
			if (picked.Count > 0)
			{
				home ??= RegisterNewSquad(bot, SquadCAType.Protection, seen);
				foreach (var i in picked)
				{
					var u = draftable[i];
					home.Units.Add(u);
					unitsHangingAroundTheBase.Remove(u);
					LedgerClaim(ArmyDuty.AllyDefend, nameof(TeamDefendTwoTick), "team_defend_two_pool", new[] { u });
					sent += Math.Max(1, candidates[i].Value);
				}

				defendTwoHome = true;
				drafted = picked.Count;
			}

			if (defendTwoHome && home is { IsValid: true })
			{
				if (!home.IsTargetValid)
					DefendTwoPoint(home, seen, alarm);
				else if (seen != null && home.Target.Type == TargetType.Terrain)
					home.TargetActor = seen;
			}

			// Free squads, nearest the attacker position first, one whole squad at a time while quota is left (the last one may overshoot by one squad).
			var redirected = 0;
			foreach (var f in freeSquads.OrderBy(f => f.DistanceSquared).ThenBy(f => f.Squad.Units[0].Actor.ActorID))
			{
				if (sent >= quota)
					break;

				DefendTwoPoint(f.Squad, seen, alarm);
				f.Squad.FuzzyStateMachine.ChangeState(f.Squad, new GroundUnitsAttackMoveStateCA(), false);
				defendTwoSquads.Add(f.Squad);
				sent += Math.Max(1, f.Value);
				redirected++;
			}

			// A pushed or gathering blob answers only when the ally would die (the arbiter's P2 or the die percent), as before.
			foreach (var s in blobs)
			{
				DefendTwoPoint(s, seen, alarm);
				s.FuzzyStateMachine.ChangeState(s, new GroundUnitsAttackMoveStateCA(), false);
				defendTwoSquads.Add(s);
			}

			team.PublishDefendCommit(key, DefendTwoCommit(alarm, home), now);
			DefendTwoMaintain(home, alarm, true);
			if (drafted + redirected + blobs.Count == 0)
				return;

			// Claim the draft now, not at the next heartbeat (same reason as ProtectOwn and DEFEND-ALLY-01).
			ReconcileSquadLeases();
			if (DiagnosticWriter != null)
				DiagnosticTeamEvent("team_defend_two", DefendTwo.Detail(alarm.VictimKey, blind, need, committed, spare, others.Spare + spare, quota, draftable.Count, drafted, redirected, blobs.Count, seen != null) + ";age=" + (now - alarm.Tick));
		}
	}
}
