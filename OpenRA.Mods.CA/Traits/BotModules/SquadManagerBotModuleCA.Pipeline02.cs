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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 (CONSOLIDATE_02_SQUAD_DESIGN.md 4): the world wiring of the army plan. Everything here is read only behind Info.SquadPipeline02Enabled (default false,
	// never set in @classic) and its DEV rows: with the switch off no snapshot is built, no role is read and no order changes, so the bot plays exactly as before.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablatePipeline02;
		AblationLatch ablateSqRoles;
		AblationLatch ablateSqDefendArbiter;
		AblationLatch ablateSqLaunchArbiter;
		AblationLatch ablateSqOrderEngaged;
		AblationLatch ablateSqGroupedJoin;
		int armyPlanNextTick;
		ArmyPlan armyPlan = ArmyPlan.Empty;
		readonly List<ArmyPlanSquad> armyPlanSquads = new();

		/// <summary>The master switch of the squad pipeline: the Info switch and the DEV row sq_pipeline_02.</summary>
		internal bool ArmyPipelineOn => Info.SquadPipeline02Enabled && !ablatePipeline02.Off(Player, AblationIds.SqPipeline02);

		/// <summary>Q2 Step 1 (row sq_roles): one role per unit, the harass cap by value, attached squads follow the main squad.</summary>
		internal bool ArmyRolesOn => ArmyPipelineOn && !ablateSqRoles.Off(Player, AblationIds.SqRoles);

		/// <summary>The last Step 1 result (Empty until the first plan pass, and always with the switch off).</summary>
		internal ArmyPlan ArmyPlanNow => armyPlan;

		internal static ArmySquadKind ArmySquadKindOf(SquadCAType type) => type switch
		{
			SquadCAType.Rush => ArmySquadKind.Rush,
			SquadCAType.Guerrilla or SquadCAType.Harass => ArmySquadKind.Raid,
			SquadCAType.Protection => ArmySquadKind.Protection,
			SquadCAType.Artillery or SquadCAType.FireSupport or SquadCAType.Support => ArmySquadKind.Attached,
			_ => ArmySquadKind.Domain
		};

		/// <summary>Where the squad stands: gathering for the team order, a pushed team squad, quiet (Idle), or busy.</summary>
		internal static ArmyHolder ArmyHolderOf(SquadCA s)
		{
			var state = s.FuzzyStateMachine?.CurrentState;
			if (state is TeamLeadGatherStateCA)
				return ArmyHolder.Gathering;
			if (AggTeamIsQuietState(state))
				return ArmyHolder.Quiet;
			return s.Type == SquadCAType.Rush && s.TeamPushSeq > 0 ? ArmyHolder.Pushed : ArmyHolder.Busy;
		}

		/// <summary>Once per ArmyPlanTicks (switch ON only): the snapshot and Step 1, then the attached squads follow the main squad.</summary>
		void ArmyPlanTick()
		{
			if (!ArmyRolesOn || !BotPeriod.CarryDue(ref armyPlanNextTick, World.WorldTick, Info.ArmyPlanTicks))
				return;

			armyPlanSquads.Clear();
			for (var i = 0; i < Squads.Count; i++)
			{
				var s = Squads[i];
				if (!s.IsValid)
					continue;

				armyPlanSquads.Add(new ArmyPlanSquad(i, ArmySquadKindOf(s.Type), ArmyHolderOf(s), s.Units.Count, SquadValueOf(s)));
			}

			long poolValue = 0;
			var poolUnits = 0;
			foreach (var u in unitsHangingAroundTheBase)
			{
				if (unitCannotBeOrdered(u.Actor))
					continue;

				poolUnits++;
				poolValue += UnitValue(u.Actor);
			}

			armyPlan = ArmyPlanCA.Assign(armyPlanSquads, poolUnits, poolValue, Info.HarassCapPercent, HarassMinOn ? Info.HarassMinUnits : 1);
			ArmyAttachParents();

#if CAMEO_DEV_LOGGING
			if (DiagnosticWriter != null)
			{
				var p = armyPlan;
				DiagnosticTeamEvent("army_plan", "main=" + p.MainUnits + "/" + p.MainValue + " harass=" + p.HarassUnits + "/" + p.HarassValue + " cap=" + (p.HarassCapValue == long.MaxValue ? -1 : p.HarassCapValue)
					+ " attached=" + p.AttachedUnits + "/" + p.AttachedValue + " defence=" + p.DefenceUnits + "/" + p.DefenceValue + " other=" + p.OtherUnits
					+ " demoted=" + p.DemotedSmall + "," + p.DemotedOverCap + " main_squad=" + p.MainSquadKey);
			}
#endif
		}

		/// <summary>
		/// The seat's main squad, measured now (the plan cache may predate a wave formed this tick): the Rush squad of the team order with the most value, else the most
		/// valuable Rush squad. Null = no Rush squad.
		/// </summary>
		SquadCA ArmyMainSquad()
		{
			SquadCA best = null;
			var bestInOrder = false;
			long bestValue = -1;
			foreach (var s in Squads)
			{
				if (!s.IsValid || s.Type != SquadCAType.Rush)
					continue;

				var holder = ArmyHolderOf(s);
				var inOrder = holder is ArmyHolder.Gathering or ArmyHolder.Pushed;
				var value = SquadValueOf(s);
				if (best != null && !(inOrder && !bestInOrder) && !(inOrder == bestInOrder && value > bestValue))
					continue;

				best = s;
				bestInOrder = inOrder;
				bestValue = value;
			}

			return best;
		}

		/// <summary>
		/// The parent an orphaned attached squad takes when a wave forms: with sq_roles on the seat's main squad (never just the newest squad, design L18), else the old
		/// rule (the new wave). Null = keep the current parent.
		/// </summary>
		SquadCA ArmyAttachParent(SquadCA newWave)
		{
			if (ArmyRolesOn && ArmyMainSquad() is { } main)
				return main;

			return newWave.IsValid ? newWave : null;
		}

		// The artillery and support squads follow the main squad; the fire support follows the biggest artillery squad (its old rule), else the main squad.
		void ArmyAttachParents()
		{
			var main = ArmyMainSquad();
			if (main == null)
				return;

			SquadCA artillery = null;
			foreach (var s in Squads)
				if (s.IsValid && s.Type == SquadCAType.Artillery && (artillery == null || s.Units.Count > artillery.Units.Count))
					artillery = s;

			foreach (var s in Squads)
			{
				if (!s.IsValid || s.Type is not (SquadCAType.Artillery or SquadCAType.Support or SquadCAType.FireSupport))
					continue;

				var want = s.Type == SquadCAType.FireSupport && artillery != null ? artillery : main;
				var parent = s.Parent;
				if (ReferenceEquals(parent, want))
					continue;

				var parentIsMain = parent != null && (s.Type == SquadCAType.FireSupport ? parent.Type == SquadCAType.Artillery : parent.Type == SquadCAType.Rush);
				if (ArmyPlanCA.AttachedKeepsParent(parent is { IsValid: true }, parentIsMain, parent?.Units.Count ?? 0, want.Units.Count))
					continue;

				s.Parent = want;
				if (DiagnosticWriter != null)
					DiagnosticSquadEvent(s, "army_attach", "parent=" + want.Type + " units=" + want.Units.Count);
			}
		}

		/// <summary>
		/// Q2 (sq_roles, Opus D5): the raider cap by VALUE. The seat's ground combat value is the pool plus every Rush, raid and home-guard squad plus the candidate; the raid value
		/// is its guerrilla and harass squads. True = the candidate may join a raid squad.
		/// </summary>
		bool HarassCapAllowsByValue(Actor candidate)
		{
			long total = 0;
			foreach (var u in unitsHangingAroundTheBase)
				total += UnitValue(u.Actor);

			long raid = 0;
			var raiders = 0;
			var counted = false;
			foreach (var q in Squads)
			{
				var kind = ArmySquadKindOf(q.Type);
				if (kind is not (ArmySquadKind.Rush or ArmySquadKind.Raid or ArmySquadKind.Protection))
					continue;

				var value = SquadValueOf(q);
				total += value;
				if (kind == ArmySquadKind.Raid)
				{
					raid += value;
					raiders += q.Units.Count;
				}

				// The release path asks for a unit that still stands in the home guard: it is already in the total (once, not twice).
				if (!counted && candidate != null)
					foreach (var u in q.Units)
						if (ReferenceEquals(u.Actor, candidate))
						{
							counted = true;
							break;
						}
			}

			var candidateValue = candidate != null ? UnitValue(candidate) : 0;
			return ArmyPlanCA.HarassAllowsByValue(raid, raiders, counted ? total : total + candidateValue, candidateValue, Info.HarassCapPercent, HarassMinOn ? Info.HarassMinUnits : 0);
		}

		// ---- Q3: the defend arbiter (row sq_defend_arbiter) ----

		/// <summary>Q3 (row sq_defend_arbiter): every defence draft goes through DefendArbiterCA (the P0-P5 ladder and the share sizing).</summary>
		internal bool DefendArbiterOn => ArmyPipelineOn && !ablateSqDefendArbiter.Off(Player, AblationIds.SqDefendArbiter);

		/// <summary>
		/// Q4b: the arbiter replaces the defence RECALL (the recall share, the DF-3/DF-4 fast-squad fold) only together with the one launcher: the stalemate breaker's home gate reads
		/// AggTeamDefence().RecallAll, and with the launcher off the breaker still launches. The feeders (ProtectOwn, TEAM-DEFEND, DEFEND-ALLY, the quiet-only candidates) stay on
		/// DefendArbiterOn, so each row remains ablatable on its own.
		/// </summary>
		internal bool ArmyDefendOwnsRecall => DefendArbiterOn && ArmyLaunchArbiterOn;

		/// <summary>
		/// Q4b: the holder as the defend arbiter reads it. A squad in the quiet (Idle) state that the team lead still keeps (freshly formed, or gathering for the open order) or that
		/// belongs to the live pushed order is not quiet: it answers as the blob does (Gathering / Pushed), so a P1/P3 answer never takes it.
		/// </summary>
		ArmyHolder ArmyDefendHolderOf(SquadCA s)
		{
			var holder = ArmyHolderOf(s);
			if (holder != ArmyHolder.Quiet)
				return holder;

			if (ArmyEngagedOrderOf(s) != null)
				return ArmyHolder.Pushed;

			return TeamLeadKeepsSquad(s) ? ArmyHolder.Gathering : holder;
		}

		// A construction yard or a production building: a hit on one may let the base fall (P0).
		static bool ArbiterIsCore(Actor victim) =>
			victim.Info.HasTraitInfo<BaseBuildingInfo>() || (victim.Info.HasTraitInfo<BuildingInfo>() && victim.Info.HasTraitInfo<ProductionInfo>());

		/// <summary>
		/// The armed enemies the bot sees around <paramref name="at"/> (the attack) and the armed own and allied actors there (the defence already there, static defences
		/// included), worth by cost, within DefendAllyAttackerScanCells. Observation only: the enemies are what the bot sees.
		/// </summary>
		void ArbiterAssess(WPos at, out long attack, out long defence)
		{
			attack = 0;
			defence = 0;
			foreach (var e in World.FindActorsInCircle(at, WDist.FromCells(Info.DefendAllyAttackerScanCells)))
			{
				if (e.IsDead || !e.IsInWorld || e.Owner == null || e.Info.HasTraitInfo<HuskInfo>() || !e.Info.HasTraitInfo<AttackBaseInfo>())
					continue;

				if (e.Owner == Player || Player.RelationshipWith(e.Owner) == PlayerRelationship.Ally)
					defence += UnitValue(e);
				else if (IsPreferredObservedEnemyUnit(e))
					attack += UnitValue(e);
			}
		}

		/// <summary>
		/// ProtectOwn under the arbiter: an own priority asset is hit. The answer is sized at ArbiterDefendMarginPercent of the attack the defence there does not cover; the
		/// pool answers first (nearest to the attack), then the squads the priority may take: P1 the quiet Rush squads, P0 (a core building hit and the base would fall to the
		/// attack) every Main squad, the gathering and pushed blob included, nearest units first and only the share needed. An attacker the bot cannot see is answered by at most
		/// ArbiterBlindDraftUnits pool units when no guard stands yet. Raids, attached squads and the air/naval/stealth domains are never taken.
		/// </summary>
		void ArbiterProtectOwn(Actor attacker, Actor protectTarget, Actor victim, SquadCA protectSq)
		{
			var leases = BotUnitLeases.Of(Player);
			var draftable = unitsHangingAroundTheBase.Where(u => !unitCannotBeOrdered(u.Actor) && IsDefenderDraftable(u, leases)).ToList();
			// Q4b: the attack point is the attacker the bot SEES (protectTarget is null for a hidden one under fogged scans); a hidden attacker is answered around the victim.
			var at = protectTarget != null && protectTarget.IsInWorld ? protectTarget.CenterPosition
				: victim != null && victim.IsInWorld ? victim.CenterPosition : World.Map.CenterOfCell(initialBaseCenter);

			// Q4c: the defence there is always measured (the pool inside the scan radius is counted nowhere else); the attack only from an attacker the bot sees, so a hidden
			// attacker stays a blind hit.
			long attack = 0;
			ArbiterAssess(at, out var seenAttack, out var defence);
			if (protectTarget != null)
			{
				attack = seenAttack;

				// Q4b: a visible armed attacker the scan did not count (IsPreferredObservedEnemyUnit rejected it) is still an attack of its own value, not a blind hit.
				if (attack <= 0 && protectTarget.IsInWorld && !protectTarget.IsDead && protectTarget.Info.HasTraitInfo<AttackBaseInfo>())
					attack = UnitValue(protectTarget);
			}

			// The pool value OUTSIDE the scan radius: inside it the pool units are already counted in `defence` (Q4b: no double count in the P0 test).
			var scanSquared = WDist.FromCells(Info.DefendAllyAttackerScanCells).LengthSquared;
			long poolValue = 0;
			foreach (var u in draftable)
				if ((u.Actor.CenterPosition - at).HorizontalLengthSquared > scanSquared)
					poolValue += UnitValue(u.Actor);

			var core = victim != null && !victim.IsDead && ArbiterIsCore(victim);
			var priority = DefendArbiterCA.Own(core, attack, defence + poolValue);
			long need = 0;
			long got = 0;
			var fromPool = 0;
			var fromSquads = 0;

			void TakeFromPool(UnitWposWrapper u)
			{
				protectSq.Units.Add(u);
				unitsHangingAroundTheBase.Remove(u);
				LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), "own_pool_pick", new[] { u });
				got += UnitValue(u.Actor);
				fromPool++;
			}

			if (attack <= 0)
			{
				if (!protectSq.IsValid)
					foreach (var u in draftable.OrderBy(u => (u.Actor.CenterPosition - at).HorizontalLengthSquared).ThenBy(u => u.Actor.ActorID)
						.Take(DefendArbiterCA.BlindDraft(draftable.Count, Info.ArbiterBlindDraftUnits)).ToList())
						TakeFromPool(u);
			}
			else
			{
				need = DefendArbiterCA.Need(attack, defence, Info.ArbiterDefendMarginPercent);
				// Q4d: the pool inside the scan radius is already in `defence` (it is at the scene), so it joins the answer but covers none of `need`; the pick that covers the need
				// comes from the pool outside the radius (nearest first), the same split as poolValue. Counting an in-radius unit twice stopped the pick short.
				var outside = new List<UnitWposWrapper>();
				foreach (var u in draftable)
				{
					if ((u.Actor.CenterPosition - at).HorizontalLengthSquared > scanSquared)
						outside.Add(u);
					else if (need > 0)
					{
						protectSq.Units.Add(u);
						unitsHangingAroundTheBase.Remove(u);
						LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), "own_pool_in_radius", new[] { u });
						fromPool++;
					}
				}

				var pick = DefendArbiterCA.PickNearest(
					outside.Select((u, i) => new DefendCandidate(i, UnitValue(u.Actor), (u.Actor.CenterPosition - at).HorizontalLengthSquared)), need);
				foreach (var i in pick)
					TakeFromPool(outside[i]);

				if (got < need)
				{
					var candidates = new List<(SquadCA Squad, UnitWposWrapper Unit)>();
					foreach (var s in Squads)
					{
						if (ReferenceEquals(s, protectSq) || !s.IsValid || s.Type != SquadCAType.Rush || ReferenceEquals(s, stalemateSquad) || s.M13Response
							|| !DefendArbiterCA.MayTake(priority, ArmyRole.Main, ArmyDefendHolderOf(s)))
							continue;

						foreach (var u in s.Units)
							if (!unitCannotBeOrdered(u.Actor))
								candidates.Add((s, u));
					}

					var squadPick = DefendArbiterCA.PickNearest(candidates.Select((c, i) =>
						new DefendCandidate(i, UnitValue(c.Unit.Actor), (c.Unit.Actor.CenterPosition - at).HorizontalLengthSquared)), need - got);
					foreach (var i in squadPick)
					{
						var (squad, unit) = candidates[i];
						squad.Units.Remove(unit);
						protectSq.Units.Add(unit);
						LedgerClaim(ArmyDuty.OwnBaseDefend, nameof(ArbiterProtectOwn), "own_squad_pick", new[] { unit });
						got += UnitValue(unit.Actor);
						fromSquads++;
					}
				}
			}

#if CAMEO_DEV_LOGGING
			if (DiagnosticWriter != null && fromPool + fromSquads > 0)
				DiagnosticTeamEvent("arbiter_own", "pri=" + priority + " core=" + (core ? 1 : 0) + " attack=" + attack + " defence=" + defence + " pool=" + poolValue
					+ " need=" + need + " got=" + got + " from_pool=" + fromPool + " from_squads=" + fromSquads);
#endif
		}

		// ---- Q4: the one launcher and the order phases (rows sq_launch_arbiter, sq_order_engaged) ----

		/// <summary>
		/// Q4 (row sq_launch_arbiter; needs the TEAM-LEAD cadence since Q4b: the leader's publish pass is the only thing that issues an order once the squad issue is bypassed): the team order is the only ground launcher. The per-bot attack timer, the AggTeam A5 launch, the stalemate
		/// breaker wave, the leader's squad issue and the Idle-state launch are bypassed; a solo bot leads itself.
		/// </summary>
		internal bool ArmyLaunchArbiterOn => ArmyPipelineOn && TeamLeadCadenceActive && !ablateSqLaunchArbiter.Off(Player, AblationIds.SqLaunchArbiter);

		/// <summary>Q4 (row sq_order_engaged): the order stays open through Push and Engaged; the leader re-picks its target for the whole blob; Done closes it.</summary>
		internal bool ArmyOrderEngagedOn => ArmyLaunchArbiterOn && !ablateSqOrderEngaged.Off(Player, AblationIds.SqOrderEngaged);

		/// <summary>
		/// Q5 (row sq_grouped_join; needs the one launcher): before the push the pool merges into the seat's gathering squad only as a batch of at least the join floor, every unit
		/// ordered on the same tick. A unit that was produced a moment ago waits at home for its batch instead of crossing the map alone (X2: median contact of 3). After the push a
		/// WAVE-MIN group forms a squad of its own (Q4, ArmyReinforce); nothing joins alone.
		/// </summary>
		internal bool ArmyGroupedJoinOn => ArmyLaunchArbiterOn && !ablateSqGroupedJoin.Off(Player, AblationIds.SqGroupedJoin);

		/// <summary>The live pushed persistent order this squad was pushed with, or null (none, or the order is done).</summary>
		TeamLeadOrder ArmyEngagedOrderOf(SquadCA s)
		{
			if (s.TeamPushSeq <= 0)
				return null;

			var order = TeamLeadOpenOrder();
			return order != null && order.Persistent && order.Pushed && order.Seq == s.TeamPushSeq ? order : null;
		}

		bool ArmyKeepsGoing(SquadCA s) => TeamLeadTeamSquad(s) && ArmyEngagedOrderOf(s) != null;

		// A pushed persistent order takes no raider: a recalled raid would walk to the target alone (D3).
		bool ArmyRaidersStayOut() => ArmyLaunchArbiterOn && TeamLeadOpenOrder() is { Persistent: true, Pushed: true };

		/// <summary>
		/// The order's target is gone from the leader's view: the actor is dead, left the world or is no longer an enemy seat's; a remembered (frozen) target counts as
		/// long as the frozen actor is valid. A hunt cell (id 0) is never gone: it stays until the leader knows a real target.
		/// </summary>
		bool ArmyOrderTargetGone(TeamLeadOrder order)
		{
			var id = order.TargetActorId;
			if (id == 0)
				return false;

			var actor = World.GetActorById(id);
			if (actor != null)
				return actor.IsDead || !actor.IsInWorld || actor.Owner == null || !TeamLeadFocusEnemy(actor.Owner);

			var frozen = Player.FrozenActorLayer?.FromID(id);
			return frozen == null || !frozen.IsValid;
		}

		/// <summary>A pushed squad takes the order's current target. False = the order target is gone, or it is the one this squad froze on (the stuck skip is active).</summary>
		bool ArmyFollowOrder(SquadCA s, TeamLeadOrder order)
		{
			if (ArmyOrderTargetGone(order))
				return false;

			if (order.TargetActorId != 0 && s.TeamSkipActor == order.TargetActorId && World.WorldTick < s.TeamSkipUntil)
				return false;

			if (!TeamLeadResolveTarget(s, order))
				return false;

			s.TeamTargetSeq = order.TargetSeq;
			return true;
		}

		/// <summary>Every squad pass, for a squad of the pushed order: report what it holds (the Done test) and, while it marches, take the leader's new target.</summary>
		void ArmyEngagedStep(SquadCA s)
		{
			if (!s.IsValid || s.Type != SquadCAType.Rush || ArmyEngagedOrderOf(s) is not { } order)
				return;

			var state = s.FuzzyStateMachine?.CurrentState;
			var wasEngaged = order.EngagedTick >= 0;
			order.ReportEngaged(s, s.Units.Count, SquadValueOf(s), World.WorldTick, state is GroundUnitsAttackState);
			if (!wasEngaged && order.EngagedTick >= 0)
				DiagnosticTeamEvent("team_engaged", "seq=" + order.Seq + " age=" + (World.WorldTick - order.PushTick));

			if (s.TeamTargetSeq != order.TargetSeq && state is GroundUnitsAttackMoveStateCA && ArmyFollowOrder(s, order))
				DiagnosticSquadEvent(s, "army_follow", "seq=" + order.Seq + " target=" + order.TargetActorId + " tseq=" + order.TargetSeq);
		}

		/// <summary>A group passes WAVE-MIN (units AND value; with WAVE-MIN off the join floor).</summary>
		bool ArmyWaveMinGroup(int units, long value)
		{
			if (!WaveMinActive)
				return units >= Info.TeamLeadJoinMinUnits;

			var (minUnits, minValue) = WaveMinBars();
			return WaveMinCA.Passes(units, value, minUnits, minValue);
		}

		/// <summary>
		/// The launch hold under the one launcher: a wave forms only for the open team order. Before the push the ordinary join floor (the leader's own share at the leader
		/// floor, an all-in order at its floor); after the push only a WAVE-MIN group of joinable units (Opus D3).
		/// </summary>
		bool ArmyMayJoin()
		{
			var order = TeamLeadOpenOrder();
			if (order == null || World.WorldTick < initialAttackDelay)
				return false;

			if (order.Pushed)
				return TeamLeadAllInRejoinGroup();

			var min = AllInCA.JoinMinUnits(order.AllIn, teamLeadIsLeader && !order.GatherOnly && !order.Extending ? Info.TeamLeadLeaderMinUnits : Info.TeamLeadJoinMinUnits,
				Info.TeamLeadAllInJoinMinUnits);
			return unitsHangingAroundTheBase.Count(TeamLeadJoinable) >= min;
		}

		/// <summary>
		/// A quiet team squad under the one launcher (true = handled, no state update): a remnant of the pushed order goes back to the order's target whatever its size; any
		/// other squad joins the open order (after the push only as a WAVE-MIN group, D3) or, with no order, goes back to the pool. Nothing issues an order or launches here.
		/// </summary>
		bool ArmyPrepare(SquadCA s)
		{
			var now = World.WorldTick;
			if (ArmyEngagedOrderOf(s) is { } engaged)
			{
				if (!TeamLeadCA.GoOnDue(now, s.TeamGoOnTick, Info.TeamLeadGoOnTicks))
					return true;

				s.TeamGoOnTick = now;
				if (ArmyFollowOrder(s, engaged) || TeamLeadGoOn(s, s.Units[0].Actor))
				{
					DiagnosticSquadEvent(s, "team_goon", reason: "seq=" + engaged.Seq + " tseq=" + engaged.TargetSeq);
					s.FuzzyStateMachine.ChangeState(s, new GroundUnitsAttackMoveStateCA(), false);
				}

				// Nothing known: it waits idle for the leader's next target (it never launches alone and is never dissolved for being small).
				return true;
			}

			var order = TeamLeadOpenOrder();
			if (order != null && (!order.Pushed || ArmyWaveMinGroup(s.Units.Count, SquadValueOf(s))))
			{
				DiagnosticSquadEvent(s, "team_join", reason: (teamLeadIsLeader ? "leader" : "follower") + " phase=" + order.Phase);
				s.TeamPushSeq = 0;
				s.FuzzyStateMachine.ChangeState(s, new TeamLeadGatherStateCA(order), false);
				if (order.HasProngs)
					DiagnosticSquadEvent(s, "team_prong", reason: "prong=" + order.ProngOrNone(s) + (order.PincerCollapsed ? " collapsed" : ""));
				return true;
			}

			DiagnosticSquadEvent(s, "team_hold", order == null ? "no_order" : "below_wave_min");
			DismissSquad(s);
			return true;
		}

		/// <summary>Opus D3: the pool joins a pushed persistent order only as a WAVE-MIN group; the new squad goes straight to the order's target.</summary>
		void ArmyReinforce(IBot bot, TeamLeadOrder order, int now)
		{
			if (now < initialAttackDelay)
				return;

			unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			if (!TeamLeadAllInRejoinGroup())
				return;

			var before = unitsHangingAroundTheBase.Count;
			CreateAttackForce(bot);
			if (unitsHangingAroundTheBase.Count < before)
				DiagnosticTeamEvent("army_reinforce", "seq=" + order.Seq + " sent=" + (before - unitsHangingAroundTheBase.Count) + " left=" + unitsHangingAroundTheBase.Count
					+ " phase=" + order.Phase);
		}

		/// <summary>
		/// Team upkeep of a pushed persistent order (whichever seat runs the team window): a leader change hands it over (or closes it without tl_order_survive); it is Done
		/// when no pushed squad has reported for the report grace (the blob is spent) or after ArmyEngagedMaxTicks.
		/// </summary>
		void ArmyMaintainEngaged(TeamLeadTeam team, TeamLeadOrder order, int now)
		{
			if (team.Election.Leader != null && order.LeaderKey != team.Election.Leader)
			{
				if (!TeamLeadOrderSurviveOn)
				{
					ArmyCloseOrder(order, now, "leader_changed");
					return;
				}

				DiagnosticTeamEvent("team_handover", "seq=" + order.Seq + " from=" + order.LeaderKey + " to=" + team.Election.Leader + " phase=" + order.Phase);
				order.HandOver(team.Election.Leader);
			}

			var grace = 5 * Info.TeamLeadCheckTicks;
			var (units, _) = order.EngagedTotals(now, grace);
			var reason = now - order.PushTick > grace && units == 0 ? "done_spent"
				: now - order.PushTick > TeamLeadCA.EngagedMaxAge(Info.ArmyEngagedMaxTicks) ? "done_max_age"
				: null;
			if (reason != null)
				ArmyCloseOrder(order, now, reason);
		}

		// Done is not a cancel: no republish hold follows it (the leader may publish the next order on its next pass).
		void ArmyCloseOrder(TeamLeadOrder order, int now, string reason)
		{
			order.Close(reason);
			DiagnosticTeamEvent("team_done", "seq=" + order.Seq + " why=" + reason + " pushed_for=" + (now - order.PushTick) + " engaged=" + (order.EngagedTick >= 0 ? 1 : 0)
				+ " retargets=" + order.TargetSeq);
		}

		/// <summary>
		/// Leader only, once per team check: the pushed order's target is gone (or the order hunts a cell and a real target is now known): the leader picks the next one from
		/// its own view, nearest to the old target, and every squad of the blob follows it (X3). Nothing known at all = Done.
		/// </summary>
		void ArmyRetargetEngaged(int now)
		{
			var order = TeamLeadOpenOrder();
			if (order == null || !order.Persistent || !order.Pushed || order.LeaderKey != teamLeadKey)
				return;

			var gone = ArmyOrderTargetGone(order);
			if (!gone && order.TargetActorId != 0)
				return;

			teamLeadAllInPicking = order.AllIn;
			var picked = TeamLeadPickTarget(order.Target, now, out var target, out var targetId);
			teamLeadAllInPicking = false;
			if (!picked)
			{
				if (gone)
					ArmyCloseOrder(order, now, "done_no_target");
				return;
			}

			// Q8b (sq_hunt_follow): a pushed hunt order moves on to the team's next hunt cell once the old one is explored (the pick below returns the same cell until then).
			if (!gone && targetId == 0 && ArmyHuntFollowMoves(order, target, now))
				return;

			if ((!gone && targetId == 0) || (targetId != 0 && targetId == order.TargetActorId))
				return;

			order.Retarget(target, targetId);
			DiagnosticTeamEvent("team_retarget", "seq=" + order.Seq + " target=" + targetId + " tseq=" + order.TargetSeq + " gone=" + (gone ? 1 : 0));
		}

		/// <summary>Opus D4: the breaker of this seat would have launched now; the team board carries the signal to the leader's all-in trigger.</summary>
		void ArmyNoteStalemate(int now)
		{
			if (teamLeadTeam != null)
				teamLeadTeam.StalemateSignalTick = now;

			DiagnosticTeamEvent("army_stalemate_signal", "tick=" + now);
		}
	}
}
