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
	/// <summary>
	/// DEFEND-ALLY-01 (USER P0, 2026-11-23): the World-free half. The user saw five idle tanks ignore an ally under attack next to
	/// them; own-actor attacks only reached the bot through <see cref="IBotRespondToAttack"/> for harvesters and buildings, and allied
	/// attacks not at all. The squad manager now notices damage to any own or allied actor near its idle units by watching hit points.
	/// </summary>
	public static class DefendAllyCA
	{
		/// <summary>True when an actor seen before now has fewer hit points than at the last scan.</summary>
		public static bool Dropped(int lastHp, int nowHp) => nowHp < lastHp;

		/// <summary>The scan is due at tick <paramref name="now"/>.</summary>
		public static bool Due(int now, int nextTick) => now >= nextTick;

		/// <summary>
		/// One centre per occupied square bucket of side <paramref name="bucketLen"/> (WDist length), the mean position of the bucket's
		/// members, in bucket order. Keeps the number of friendly scans at one per cluster of responders instead of one per unit.
		/// </summary>
		public static List<WPos> ClusterCentres(IReadOnlyList<WPos> positions, int bucketLen)
		{
			var size = Math.Max(1, bucketLen);
			var sums = new SortedDictionary<(int X, int Y), (long X, long Y, int Count)>();
			foreach (var p in positions)
			{
				var key = (p.X / size, p.Y / size);
				sums.TryGetValue(key, out var acc);
				sums[key] = (acc.X + p.X, acc.Y + p.Y, acc.Count + 1);
			}

			var centres = new List<WPos>(sums.Count);
			foreach (var kv in sums)
				centres.Add(new WPos((int)(kv.Value.X / kv.Value.Count), (int)(kv.Value.Y / kv.Value.Count), 0));

			return centres;
		}

		/// <summary>Index of the nearest enemy within <paramref name="radiusLen"/> of <paramref name="friendly"/> (ties: lower id), or -1.</summary>
		public static int NearestAttacker(WPos friendly, IReadOnlyList<(uint Id, WPos Pos)> enemies, int radiusLen)
		{
			var radiusSq = (long)radiusLen * radiusLen;
			var best = -1;
			var bestSq = long.MaxValue;
			for (var i = 0; i < enemies.Count; i++)
			{
				var d = (enemies[i].Pos - friendly).HorizontalLengthSquared;
				if (d > radiusSq)
					continue;

				if (d < bestSq || (d == bestSq && enemies[i].Id < enemies[best].Id))
				{
					best = i;
					bestSq = d;
				}
			}

			return best;
		}

		/// <summary>Whether a responder at <paramref name="responder"/> is within <paramref name="radiusLen"/> of the attacked friendly.</summary>
		public static bool InRadius(WPos responder, WPos friendly, int radiusLen) =>
			(responder - friendly).HorizontalLengthSquared <= (long)radiusLen * radiusLen;
	}

	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateArty;
		AblationLatch ablateArtyStruct;
		AblationLatch ablateDefendAlly;

		// DEFEND-ALLY-01: last seen hit points of the friendlies near the responders (rebuilt each scan, so it stays small).
		Dictionary<uint, int> allyHp = [];
		Dictionary<uint, int> allyHpNext = [];
		readonly List<WPos> defendAllyResponders = [];
		readonly List<Actor> defendAllyAttacked = [];
		readonly List<(uint Id, WPos Pos)> defendAllyEnemies = [];
		readonly List<Actor> defendAllyEnemyActors = [];
		int defendAllyNextScan;
		int defendAllyNextResponse;

		/// <summary>ARTY-01 is on for this bot: the Info switch and the DEV ablation row (row off = the old artillery state).</summary>
		internal bool ArtyMicroActive => Info.ArtyMicroEnabled && !ablateArty.Off(Player, AblationIds.ArtyMicro);

		/// <summary>ARTY-TARGET-01: the structure-first sub-switch, only while the micro is on (row arty_struct_first).</summary>
		internal bool ArtyStructFirstActive => ArtyMicroActive && Info.ArtyStructFirstEnabled && !ablateArtyStruct.Off(Player, AblationIds.ArtyStructFirst);

		bool DefendAllyActive => Info.DefendAllyEnabled && !ablateDefendAlly.Off(Player, AblationIds.DefendAlly);

		// An attack squad that has not moved out yet - idle, or staging/gathering at its rally building - is free to answer a call for
		// help (USER 2026-11-23: helping own or allied units comes first and ignores the wave minimum). A squad already marching or
		// fighting keeps its job.
		static bool IsIdleResponderSquad(SquadCA s) =>
			s.IsValid && s.Type is SquadCAType.Rush or SquadCAType.Guerrilla
			&& s.FuzzyStateMachine?.CurrentState is GroundUnitsIdleStateCA or GroundUnitsStageStateCA or TeamLeadGatherStateCA;

		// TEAM-GATHER-02 (tl_defend_respects_order): an idle responder squad that is not gathering for the open order and not freshly formed.
		bool DefendAllyFreeSquad(SquadCA s) => IsIdleResponderSquad(s) && !TeamLeadKeepsSquad(s);

		void DefendAllyTick(IBot bot)
		{
			if (!DefendAllyActive)
				return;

			var now = World.WorldTick;
			if (!DefendAllyCA.Due(now, defendAllyNextScan))
				return;

			defendAllyNextScan = now + Math.Max(1, Info.DefendAllyIntervalTicks);

			// Responders: pool units that may be drafted (leases respected) and idle attack squads.
			defendAllyResponders.Clear();
			var leases = BotUnitLeases.Of(Player);
			foreach (var u in unitsHangingAroundTheBase)
				if (!unitCannotBeOrdered(u.Actor) && IsDefenderDraftable(u, leases))
					defendAllyResponders.Add(u.Actor.CenterPosition);

			foreach (var s in Squads)
				if (DefendAllyFreeSquad(s))
					defendAllyResponders.Add(s.CenterPosition);

			if (defendAllyResponders.Count == 0)
			{
				allyHp.Clear();
				return;
			}

			var radius = WDist.FromCells(Info.DefendAllyRadiusCells).Length;

			// Buckets of half the radius: every responder is within ~0.71 radius of its bucket centre, so a query of 1.75 radius around
			// the centre covers every friendly within one radius of any member; the exact per-responder check comes at response time.
			var centres = DefendAllyCA.ClusterCentres(defendAllyResponders, Math.Max(1, radius / 2));
			var queryRadius = new WDist(radius * 7 / 4);

			allyHpNext.Clear();
			defendAllyAttacked.Clear();
			foreach (var centre in centres)
			{
				foreach (var f in World.FindActorsInCircle(centre, queryRadius))
				{
					if (f.IsDead || !f.IsInWorld || f.Owner == null || f.Info.HasTraitInfo<HuskInfo>())
						continue;

					if (f.Owner != Player && Player.RelationshipWith(f.Owner) != PlayerRelationship.Ally)
						continue;

					var health = f.TraitOrDefault<IHealth>();
					if (health == null)
						continue;

					var hp = health.HP;
					if (allyHpNext.ContainsKey(f.ActorID))
						continue;

					allyHpNext[f.ActorID] = hp;
					if (allyHp.TryGetValue(f.ActorID, out var last) && DefendAllyCA.Dropped(last, hp))
						defendAllyAttacked.Add(f);
				}
			}

			(allyHp, allyHpNext) = (allyHpNext, allyHp);
			allyHpNext.Clear();

			// V07 tl_defend_team: a serious attack on a friendly raises the team alarm (throttled; free of work with the switch off).
			TeamDefendScan(now);

			if (defendAllyAttacked.Count == 0 || now < defendAllyNextResponse)
				return;

			// Deterministic order, nearest-to-a-responder first would need another pass; actor id is stable and cheap.
			defendAllyAttacked.Sort((a, b) => a.ActorID.CompareTo(b.ActorID));
			var scan = WDist.FromCells(Info.DefendAllyAttackerScanCells);
			foreach (var friendly in defendAllyAttacked)
			{
				defendAllyEnemies.Clear();
				defendAllyEnemyActors.Clear();
				foreach (var e in World.FindActorsInCircle(friendly.CenterPosition, scan))
				{
					if (!IsPreferredObservedEnemyUnit(e) || !e.Info.HasTraitInfo<AttackBaseInfo>())
						continue;

					defendAllyEnemies.Add((e.ActorID, e.CenterPosition));
					defendAllyEnemyActors.Add(e);
				}

				var pick = DefendAllyCA.NearestAttacker(friendly.CenterPosition, defendAllyEnemies, scan.Length);
				if (pick < 0)
					continue;

				if (DefendAllyRespond(bot, friendly, defendAllyEnemyActors[pick], radius, leases))
				{
					defendAllyNextResponse = now + Math.Max(1, Info.DefendAllyCooldownTicks);
					break;
				}
			}
		}

		// Draft the pool units and redirect the idle squads that are within `radius` of the attacked friendly. True when anyone answered.
		bool DefendAllyRespond(IBot bot, Actor friendly, Actor attacker, int radius, IBotUnitLeases leases)
		{
			var responded = false;
			var friendlyPos = friendly.CenterPosition;

			var protectSq = GetSquadOfType(SquadCAType.Protection);
			var canJoinProtect = protectSq == null || !protectSq.IsValid
				|| DefendAllyCA.InRadius(protectSq.CenterPosition, friendlyPos, radius);

			if (canJoinProtect)
			{
				var draftable = unitsHangingAroundTheBase
					.Where(u => !unitCannotBeOrdered(u.Actor) && IsDefenderDraftable(u, leases)
						&& DefendAllyCA.InRadius(u.Actor.CenterPosition, friendlyPos, radius))
					.ToList();

				var toDraft = DefendDraftLimit(draftable.Count, false, Info, utilityAxesProviders);
				if (toDraft > 0)
				{
					protectSq ??= RegisterNewSquad(bot, SquadCAType.Protection, attacker);
					for (var i = 0; i < toDraft; i++)
					{
						protectSq.Units.Add(draftable[i]);
						unitsHangingAroundTheBase.Remove(draftable[i]);
					}

					if (protectSq.IsValid && !protectSq.IsTargetValid)
					{
						// CONSOLIDATE-02 Q6 (sq_target_pick): the nearest armed attacker seen around the friendly, else the one that hit it (row off: the one that hit it).
						var defenceTarget = ArmyTargetPickOn ? PickDefenceTarget(attacker, friendlyPos) : attacker;
						if (defenceTarget != null)
							protectSq.TargetActor = defenceTarget;
					}

					responded = true;
				}
			}

			foreach (var s in Squads)
			{
				if (s == protectSq || !DefendAllyFreeSquad(s) || !DefendAllyCA.InRadius(s.CenterPosition, friendlyPos, radius))
					continue;

				// CONSOLIDATE-02 Q3 (sq_defend_arbiter): a local hit (P4) takes pool units in the radius only, never a squad (X9: staging and gathering squads were hijacked).
				if (DefendArbiterOn && !DefendArbiterCA.MayTake(DefendPriority.P4LocalHit, ArmyRole.Main, ArmyHolderOf(s)))
					continue;

				s.TargetActor = attacker;
				s.FuzzyStateMachine.ChangeState(s, new GroundUnitsAttackMoveStateCA(), false);
				responded = true;
			}

			if (responded)
			{
				AIUtils.BotDebug("AI ({0}): DEFEND-ALLY {1} attacked near ({2}), responders sent at {3}", Player.ClientIndex,
					friendly.Info.Name, friendly.Location, attacker.Info.Name);

				// Claim the draft now, not at the next heartbeat (same reason as ProtectOwn).
				ReconcileSquadLeases();
			}

			return responded;
		}
	}
}
