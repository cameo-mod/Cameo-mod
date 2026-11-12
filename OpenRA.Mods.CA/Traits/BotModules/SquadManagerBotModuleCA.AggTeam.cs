// IMPL02-AGG-TEAM: world wiring of A4 (allied attack intents, joining them) and A5 (home-reserve cap, max-strength push).
// Every entry point below is reached only behind Info.AggTeamEnabled (the switch CG_agg_team); with the flag off no intent is
// published, no state is entered, no RNG is drawn and no order is issued. The decision logic is pure and lives in AggTeamPolicyCA.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCA : IBotAttackIntentSource, IBotMcvSafetyPolicy
	{
		static readonly ConditionalWeakTable<SquadCA, StrongBox<int>> AggJoinCooldowns = new();
		static readonly ConditionalWeakTable<SquadCA, StrongBox<int>> AggTinySince = new();

		int aggNextCheckTick;
		int aggPushUntilTick;
		bool aggFinishActive;
		int aggNextPushTick;
		int aggIntentTick = -1;
		TeamAttackIntent aggIntent;
		int aggCandidatesTick = -1;
		List<AggTeamPolicyCA.Candidate> aggCandidates;

		// Counters for tests and telemetry hooks (never read by a decision).
		internal int AggJoins;
		internal int AggLaunches;
		internal int AggPushes;
		internal int AggFinishes;
		internal int AggPicks;
		internal int AggMerges;
		internal int AggDetaches;
		internal int AggRecalls;

		/// <summary>A7: the finishing push is on. The ground states read it to hold the fight unless the coalition ratio is below the flee ratio.</summary>
		internal bool AggTeamFinishActive => Info.AggTeamEnabled && aggFinishActive && !AggTeamDefence().RecallAll;

		/// <summary>MCV-SAFE-01 seam for the expansion planner: on only with the CG switch (AggTeamEnabled) and an enabled trait.</summary>
		bool IBotMcvSafetyPolicy.McvSafeEnabled => Info.AggTeamEnabled && !IsTraitDisabled;

		int IBotMcvSafetyPolicy.McvSafeParam(string name, Player enemy) => AggP(name, enemy);

		TeamAttackIntent IBotAttackIntentSource.AttackIntent => Info.AggTeamEnabled && !IsTraitDisabled ? AggTeamComputeIntent() : null;

		/// <summary>A9: the effective value of an AggTeam/Aggression parameter for this seat's faction against <paramref name="enemy"/>'s faction (layers default to 1.0).</summary>
		internal int AggP(string name, Player enemy = null) =>
			AggressionParams.Resolve(Info, name, Player.Faction?.InternalName, enemy?.Faction?.InternalName);

		// A9 strategic form: cached per tick (own coalition, enemy seats and the resolved values).
		int aggStrategicTick = -1;
		List<string> aggCoalition;
		List<AggressionParams.EnemySeat> aggEnemySeats;
		readonly Dictionary<string, int> aggStrategicValues = new();
		int aggSeatArmyTick = -1;
		Dictionary<Player, long> aggSeatArmies;

		/// <summary>A9 strategic: the effective value of a parameter against the whole enemy field (A5 reserve, A7 finish push). Weighted geometric mean over enemy seats of M(own coalition, f_e).</summary>
		internal int AggS(string name)
		{
			var now = World.WorldTick;
			if (aggStrategicTick != now)
			{
				aggStrategicTick = now;
				aggStrategicValues.Clear();
				aggCoalition = new List<string> { Player.Faction?.InternalName };
				aggEnemySeats = new List<AggressionParams.EnemySeat>();
				var armies = AggTeamSeatArmies();
				var floor = AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamEnemyWeightFloorValue));
				foreach (var p in World.Players)
				{
					if (p == Player || p.NonCombatant || p.WinState != WinState.Undefined)
						continue;
					if (Player.IsAlliedWith(p))
						aggCoalition.Add(p.Faction?.InternalName);
					else if (Player.RelationshipWith(p) == PlayerRelationship.Enemy)
						aggEnemySeats.Add(new AggressionParams.EnemySeat(p.Faction?.InternalName, armies.GetValueOrDefault(p) + floor));
				}
			}

			if (!aggStrategicValues.TryGetValue(name, out var value))
				aggStrategicValues[name] = value = AggressionParams.ResolveStrategic(Info, name, aggCoalition, aggEnemySeats);
			return value;
		}

		/// <summary>Visible armed mobile enemy value per seat (the observation behind the A8 weak-seat bonus and the A9 strategic enemy weights), cached per tick.</summary>
		Dictionary<Player, long> AggTeamSeatArmies()
		{
			var now = World.WorldTick;
			if (aggSeatArmyTick == now && aggSeatArmies != null)
				return aggSeatArmies;

			aggSeatArmyTick = now;
			aggSeatArmies = new Dictionary<Player, long>();
			foreach (var e in World.Actors)
				if (!e.IsDead && e.IsInWorld && e.Owner != null && IsPreferredObservedEnemyUnit(e) && e.Info.HasTraitInfo<AttackBaseInfo>()
					&& !e.Info.HasTraitInfo<BuildingInfo>() && !e.Info.HasTraitInfo<HarvesterInfo>())
					aggSeatArmies[e.Owner] = aggSeatArmies.GetValueOrDefault(e.Owner) + UnitValue(e);
			return aggSeatArmies;
		}

		internal static bool AggTeamIsAttackState(IState state) =>
			state is GroundUnitsAttackMoveStateCA or GroundUnitsAttackState;

		internal static bool AggTeamIsQuietState(IState state) => state is GroundUnitsIdleStateCA;

		/// <summary>The best live ground assault of this seat as an intent: the most valuable Rush squad attacking a valid target.</summary>
		TeamAttackIntent AggTeamComputeIntent()
		{
			var now = World.WorldTick;
			if (aggIntentTick == now)
				return aggIntent;

			aggIntentTick = now;
			aggIntent = null;
			SquadCA best = null;
			var bestValue = 0;
			foreach (var s in Squads)
			{
				if (s.Type != SquadCAType.Rush || !s.IsValid || s.Units.Count < AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamIntentMinUnits))
					|| !AggTeamIsAttackState(s.FuzzyStateMachine?.CurrentState) || !s.IsTargetValid)
					continue;

				var value = SquadValueOf(s);
				if (best == null || value > bestValue)
				{
					best = s;
					bestValue = value;
				}
			}

			if (best != null)
				aggIntent = AggTeamPolicyCA.BuildIntent(best.CenterPosition, best.Target.CenterPosition, now, bestValue,
					best.Target.Type == TargetType.Actor && best.Target.Actor != null ? best.Target.Actor.ActorID : 0u,
					AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamRendezvousBackCells)), AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTicksPerCell)));
			return aggIntent;
		}

		List<AggTeamPolicyCA.Candidate> AggTeamCandidates()
		{
			var now = World.WorldTick;
			if (aggCandidatesTick == now && aggCandidates != null)
				return aggCandidates;

			aggCandidatesTick = now;
			aggCandidates = new List<AggTeamPolicyCA.Candidate>();
			var own = AggTeamComputeIntent();
			if (own != null)
				aggCandidates.Add(new AggTeamPolicyCA.Candidate(Player.InternalName ?? "#" + Player.ClientIndex, own));
			foreach (var b in TeamBlackboard.CollectBroadcasts(Player))
				if (b.AttackIntent != null)
					aggCandidates.Add(new AggTeamPolicyCA.Candidate(CoalitionFold.ParticipantKey(b), b.AttackIntent));
			return aggCandidates;
		}

		long AggTeamQuietSquadValue()
		{
			long value = 0;
			foreach (var q in Squads)
				if (q.Type == SquadCAType.Rush && q.IsValid && AggTeamIsQuietState(q.FuzzyStateMachine?.CurrentState))
					value += SquadValueOf(q);
			return value;
		}

		long AggTeamHomeValue()
		{
			long value = AggTeamQuietSquadValue();
			foreach (var u in unitsHangingAroundTheBase)
				value += UnitValue(u.Actor);
			return value;
		}

		bool AggTeamPushActive(int now) => aggPushUntilTick > now;

		// ---- DEFENCE-DETACH (Opus ruling on C1/C2): a home threat detaches a proportional part of the army instead of freezing the attack ----
		int aggDetachTick = -1;
		DefenceDetachmentPlan aggDetachPlan = DefenceDetachmentPlan.None;
		static readonly DefenceDetachmentPlan AggRecallAllPlan = new(true, new HashSet<uint>(), 0, 0);

		/// <summary>The own CY (lowest id) as the home point, else the start cell.</summary>
		WPos AggTeamHomePos()
		{
			foreach (var a in World.ActorsHavingTrait<BaseBuilding>().Where(a => a.Owner == Player && a.IsInWorld && !a.IsDead).OrderBy(a => a.ActorID))
				return a.CenterPosition;
			return World.Map.CenterOfCell(Player.HomeLocation);
		}

		IEnumerable<DetachCandidate> AggTeamDetachCandidates()
		{
			var home = AggTeamHomePos();
			var radius = (long)AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamDefenceHomeRadiusCells)) * AggTeamPolicyCA.CellUnits;
			var radiusSquared = radius * radius;
			foreach (var u in unitsHangingAroundTheBase)
			{
				var d = (long)(u.Actor.CenterPosition - home).HorizontalLengthSquared;
				yield return new DetachCandidate(u.Actor.ActorID, UnitValue(u.Actor), d, d <= radiusSquared);
			}

			foreach (var s in Squads)
			{
				if (s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad))
					continue;
				foreach (var u in s.Units)
				{
					var d = (long)(u.Actor.CenterPosition - home).HorizontalLengthSquared;
					yield return new DetachCandidate(u.Actor.ActorID, UnitValue(u.Actor), d, d <= radiusSquared);
				}
			}
		}

		/// <summary>
		/// The defence plan in force this tick (cached; latched while the home latch is active). Fail-closed recall without a provider. The one entry point
		/// for A5 launches, A7, A8 and (merged) the breaker and agg-core idle/march decisions.
		/// </summary>
		internal DefenceDetachmentPlan AggTeamDefence()
		{
			var now = World.WorldTick;
			if (aggDetachTick == now)
				return aggDetachPlan;

			aggDetachTick = now;
			if (!Info.AggTeamEnabled)
				return aggDetachPlan = DefenceDetachmentPlan.None;

			var provider = StalemateUrgency;
			if (provider == null)
				return aggDetachPlan = AggRecallAllPlan;

			var net = DefenceDetachment.NetThreat(provider.StalemateHomeThreatValue, provider.StalemateHomeDefenceValue, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamDefenceSharePercent)));
			var fresh = net > 0
				? DefenceDetachment.Plan(net, provider.StalemateOwnArmy, AggTeamDetachCandidates(), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamDefendMarginPercent)),
					AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamMaxDetachSharePercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamRecallSharePercent)))
				: DefenceDetachmentPlan.None;
			var plan = DefenceDetachment.Latch(aggDetachPlan, fresh, provider.StalemateHomeThreat, provider.StalemateHomeLatch);
			if (plan.IsActive && !aggDetachPlan.IsActive)
			{
				if (plan.RecallAll)
					AggRecalls++;
				else
					AggDetaches++;
			}

			return aggDetachPlan = plan;
		}

		/// <summary>True when the squad holds a unit that the defence plan keeps home (agg-core's idle/march decisions hold such a squad at home).</summary>
		internal bool AggTeamSquadDefends(SquadCA squad) =>
			Info.AggTeamEnabled && squad != null && squad.IsValid && AggTeamDefence().ContainsAny(squad.Units.Select(u => u.Actor.ActorID));

		/// <summary>
		/// Apply the plan to the squads in the field: a squad that is detached as a whole (or any Rush squad under a recall) is stood down and sent home;
		/// a squad with some detached units releases just those. Quiet squads and the pool already stand at home and are handled by the launch/join gates.
		/// </summary>
		void AggTeamApplyDetachment(IBot bot, DefenceDetachmentPlan plan)
		{
			if (!plan.IsActive)
				return;

			var homeCell = World.Map.CellContaining(AggTeamHomePos());
			foreach (var s in Squads.ToList())
			{
				if (s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad) || AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState))
					continue;

				var ids = s.Units.Select(u => u.Actor.ActorID).ToList();
				if (plan.ContainsAll(ids))
				{
					var members = s.Units.ToList();
					DiagnosticSquadEvent(s, "agg_recall", reason: plan.RecallAll ? "recall" : "detach");
					DismissSquad(s);
					AggTeamSendHome(bot, members, homeCell);
				}
				else if (plan.ContainsAny(ids))
				{
					var released = s.Units.Where(u => plan.Contains(u.Actor.ActorID)).ToList();
					DiagnosticSquadEvent(s, "agg_release", reason: "detach");
					var leases = BotUnitLeases.Of(Player);
					foreach (var u in released)
					{
						s.Units.Remove(u);
						if (leases != null && u.Actor != null && claimedBySquads.Remove(u.Actor))
							leases.Release(u.Actor, LeaseOwner);
						unitsHangingAroundTheBase.Add(u);
					}

					AggTeamSendHome(bot, released, homeCell);
				}
			}
		}

		void AggTeamSendHome(IBot bot, List<UnitWposWrapper> units, CPos homeCell)
		{
			foreach (var u in units)
				if (u.Actor != null && !unitCannotBeOrdered(u.Actor))
					bot.QueueOrder(new Order("AttackMove", u.Actor, Target.FromCell(World, homeCell), false));
		}

		/// <summary>Pool units the plan keeps home for the defence are taken out of the pool for the launch (the caller puts them back).</summary>
		List<UnitWposWrapper> AggTeamTakeDetached()
		{
			var plan = AggTeamDefence();
			if (!plan.IsActive)
				return null;
			var kept = unitsHangingAroundTheBase.Where(u => plan.Contains(u.Actor.ActorID)).ToList();
			if (kept.Count == 0)
				return null;
			unitsHangingAroundTheBase.RemoveAll(u => plan.Contains(u.Actor.ActorID));
			return kept;
		}

		/// <summary>Workhorse C4: the pool above the reserve and the detachment that a forced launch would take is at least the dispatch floor.</summary>
		bool AggTeamDraftMeetsFloor(long reserve, DefenceDetachmentPlan plan)
		{
			var need = Math.Max(0, reserve - AggTeamQuietSquadValue());
			var pool = unitsHangingAroundTheBase.Where(u => !plan.Contains(u.Actor.ActorID)).Select(u => (u.Actor.ActorID, UnitValue(u.Actor))).ToList();
			var keep = AggTeamPolicyCA.KeepForReserve(pool, need);
			var draft = pool.Where(u => !keep.Contains(u.ActorID)).ToList();
			return AggTeamPolicyCA.LaunchMeetsFloor(draft.Count, draft.Sum(u => (long)u.Item2),
				AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTinyMergeMinUnits)), AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTinyMergeValue)));
		}

		/// <summary>
		/// A3 merge (Opus ruling a): a quiet Rush squad that has stayed below the dispatch floor for AggTeamTinyMergeTicks is dismissed, so its
		/// units return to the pool and the next attack force takes them. One squad per check, never the breaker squad.
		/// </summary>
		void AggTeamMergeTiny(int now)
		{
			foreach (var s in Squads.ToList())
			{
				if (s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad) || !AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState))
				{
					AggTinySince.Remove(s);
					continue;
				}

				var tiny = AggTeamPolicyCA.IsTinyRemnant(s.Units.Count, SquadValueOf(s),
					AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTinyMergeMinUnits)), AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTinyMergeValue)));
				if (!tiny)
				{
					AggTinySince.Remove(s);
					continue;
				}

				var since = AggTinySince.GetValue(s, _ => new StrongBox<int>(now)).Value;
				if (!AggTeamPolicyCA.TinyMergeDue(true, since, now, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTinyMergeTicks))))
					continue;

				AggMerges++;
				DiagnosticSquadEvent(s, "agg_merge");
				AggTinySince.Remove(s);
				DismissSquad(s);
				return;
			}
		}

		void AggTeamUpdatePush(IBotStalemateUrgencyProvider provider, int now, bool blocked)
		{
			if (blocked)
			{
				aggPushUntilTick = 0;
				return;
			}

			if (aggPushUntilTick > now || now < aggNextPushTick)
				return;
			if (!AggTeamPolicyCA.PushDue(provider.StalemateOwnArmy, now, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamPushArmyValue)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamPushLateTick))))
				return;

			aggPushUntilTick = now + AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamPushHoldTicks));
			aggNextPushTick = now + AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamPushCooldownTicks));
			AggPushes++;
		}

		/// <summary>A5: evaluated every AggTeamCheckIntervalTicks after the initial attack delay. Launches the pool above the reserve.</summary>
		void AggTeamTick(IBot bot)
		{
			var now = World.WorldTick;
			if (now < aggNextCheckTick)
				return;

			aggNextCheckTick = now + Math.Max(1, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamCheckIntervalTicks)));
			if (now < initialAttackDelay)
				return;

			var provider = StalemateUrgency;
			var defence = AggTeamDefence();
			var blocked = provider == null || defence.RecallAll;
			AggTeamApplyDetachment(bot, defence);
			AggTeamUpdatePush(provider, now, blocked);
			AggTeamUpdateFinish(provider);
			if (blocked)
				return;

			AggTeamMergeTiny(now);

			unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			long poolValue = 0;
			foreach (var u in unitsHangingAroundTheBase)
				poolValue += UnitValue(u.Actor);

			var reserve = AggTeamPolicyCA.EffectiveReserve(AggTeamPushActive(now), aggFinishActive, provider.StalemateOwnArmy, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamReserveSharePercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishMinReservePercent)));
			if (AggTeamPolicyCA.ExcessLaunchDue(poolValue, AggTeamQuietSquadValue(), reserve, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamMinExcessValue)), blocked)
				&& AggTeamDraftMeetsFloor(reserve, defence))
				CreateAttackForce(bot, aggForce: true);
		}

		/// <summary>
		/// A5: take the reserve out of the pool for the duration of a forced launch (the strongest units are drafted first). The caller puts
		/// the returned units back after the wave is formed. Null when nothing has to stay home.
		/// </summary>
		List<UnitWposWrapper> AggTeamTakeReserve()
		{
			var detached = AggTeamTakeDetached();
			var reserved = AggTeamTakeReserveOnly();
			if (detached == null)
				return reserved;
			if (reserved != null)
				detached.AddRange(reserved);
			return detached;
		}

		List<UnitWposWrapper> AggTeamTakeReserveOnly()
		{
			var now = World.WorldTick;
			var provider = StalemateUrgency;
			if (provider == null)
				return null;

			var reserve = AggTeamPolicyCA.EffectiveReserve(AggTeamPushActive(now), aggFinishActive, provider.StalemateOwnArmy, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamReserveSharePercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishMinReservePercent)));
			var need = Math.Max(0, reserve - AggTeamQuietSquadValue());
			if (need <= 0)
				return null;

			var keepIds = AggTeamPolicyCA.KeepForReserve(unitsHangingAroundTheBase.Select(u => (u.Actor.ActorID, UnitValue(u.Actor))), need);
			if (keepIds.Count == 0)
				return null;

			var kept = unitsHangingAroundTheBase.Where(u => keepIds.Contains(u.Actor.ActorID)).ToList();
			unitsHangingAroundTheBase.RemoveAll(u => keepIds.Contains(u.Actor.ActorID));
			return kept;
		}

		/// <summary>A4/A5: an idle Rush squad joins the best live attack intent (allied or own) while the reserve stays covered and home is not threatened.</summary>
		void AggTeamPrepare(SquadCA s)
		{
			if (s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad)
				|| !AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState) || s.Units.Count < (aggFinishActive ? AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishJoinMinUnits)) : AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamJoinMinUnits))))
				return;

			var now = World.WorldTick;
			if (AggJoinCooldowns.TryGetValue(s, out var cooldown) && now < cooldown.Value)
				return;

			var provider = StalemateUrgency;
			var defence = AggTeamDefence();
			var blocked = provider == null || defence.RecallAll || AggTeamSquadDefends(s);
			if (blocked)
				return;

			var reserve = AggTeamPolicyCA.EffectiveReserve(AggTeamPushActive(now), aggFinishActive, provider.StalemateOwnArmy, AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamReserveSharePercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishMinReservePercent)));
			if (!AggTeamPolicyCA.MayLeaveHome(AggTeamHomeValue(), SquadValueOf(s), reserve, blocked))
				return;

			var pick = AggTeamPolicyCA.ChooseIntent(AggTeamCandidates(), s.CenterPosition, now,
				AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamIntentMaxAgeTicks)), AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamIntentGraceTicks)), AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamJoinRangeCells)));
			TeamAttackIntent intent;
			string source;
			if (pick != null)
			{
				intent = pick.Value.Intent;
				source = pick.Value.Key == (Player.InternalName ?? "#" + Player.ClientIndex) ? "own" : "ally:" + pick.Value.Key;
			}
			else if (aggFinishActive && AggTeamPushIntent(s, now) is { } pushIntent)
			{
				// A7: during the all-in an idle squad is assigned even with no attack to join.
				intent = pushIntent;
				source = "finish";
			}
			else
				return;

			if (!AggTeamResolveTarget(s, intent))
			{
				AggTeamNoteJoinFailed(s);
				return;
			}

			AggJoins++;
			DiagnosticSquadEvent(s, "agg_join", reason: source);
			s.FuzzyStateMachine.ChangeState(s, new AggTeamRendezvousStateCA(intent, now), false);
		}

		/// <summary>A7: the intent an idle squad gets in the all-in when nothing is attacking: the A8 target, no rendezvous wait (it starts where it stands).</summary>
		TeamAttackIntent AggTeamPushIntent(SquadCA s, int now)
		{
			var from = s.CenterPosition;
			var actor = FindClosestEnemy(s.Units[0].Actor, s);
			if (actor != null)
				return new TeamAttackIntent(actor.CenterPosition, from, now, now + AggTeamPolicyCA.TravelTicks(from, actor.CenterPosition, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTicksPerCell))), now, SquadValueOf(s), actor.ActorID);
			if (FoggedScans && FindFrozenEnemyTarget(from, -1, s) is { } frozen)
				return new TeamAttackIntent(frozen.CenterPosition, from, now, now + AggTeamPolicyCA.TravelTicks(from, frozen.CenterPosition, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTicksPerCell))), now, SquadValueOf(s), 0);
			return null;
		}

		internal void AggTeamNoteJoinFailed(SquadCA squad)
		{
			AggJoinCooldowns.GetValue(squad, _ => new StrongBox<int>()).Value = World.WorldTick + AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamJoinCooldownTicks));
		}

		/// <summary>Point the squad at the intent's target: the published actor if it is still a visible enemy, else the nearest visible enemy there, else a remembered building.</summary>
		internal bool AggTeamResolveTarget(SquadCA squad, TeamAttackIntent intent)
		{
			if (intent.TargetActorId != 0)
			{
				var a = World.GetActorById(intent.TargetActorId);
				if (a != null && !a.IsDead && a.IsInWorld && IsPreferredObservedEnemyUnit(a))
				{
					squad.TargetActor = a;
					return true;
				}
			}

			var radius = WDist.FromCells(AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetScanCells)));
			var near = VisibleEnemiesNear(intent.Target, radius);
			if (near.Count > 0)
			{
				squad.TargetActor = near.MinBy(e => (e.CenterPosition - intent.Target).HorizontalLengthSquared);
				return true;
			}

			if (FoggedScans)
			{
				var frozen = FindFrozenEnemyTarget(intent.Target, -1, squad);
				if (frozen != null && (frozen.CenterPosition - intent.Target).HorizontalLengthSquared <= (long)radius.LengthSquared)
				{
					squad.Target = Target.FromFrozenActor(frozen);
					return true;
				}
			}

			return false;
		}

		void AggTeamUpdateFinish(IBotStalemateUrgencyProvider provider)
		{
			if (provider == null)
			{
				aggFinishActive = false;
				return;
			}

			var was = aggFinishActive;
			aggFinishActive = AggTeamPolicyCA.FinishStep(was, provider.StalemateOwnArmy, provider.StalemateAlliedArmy, provider.StalemateEnemyArmy,
				AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishRatioPercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishExitRatioPercent)), AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishMinArmyValue)), AggTeamEnemySeatWithoutProduction());
			if (aggFinishActive && !was)
				AggFinishes++;
		}

		/// <summary>A7 (fog-honest, from the frozen-actor memory): a living enemy seat whose known buildings (at least the configured count) include no CY and no production.</summary>
		bool AggTeamEnemySeatWithoutProduction()
		{
			var layer = Player.FrozenActorLayer;
			if (layer == null)
				return false;

			var known = new Dictionary<Player, List<(bool Wall, bool Armed, bool Key)>>();
			foreach (var fa in layer.FrozenActorsInRegion(World.Map.AllCells))
			{
				if (!fa.IsValid || fa.Owner == null || fa.Owner == Player || fa.Owner.WinState != WinState.Undefined || fa.Owner.NonCombatant
					|| Player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy || !fa.Info.HasTraitInfo<BuildingInfo>())
					continue;

				if (!known.TryGetValue(fa.Owner, out var list))
					known[fa.Owner] = list = new List<(bool Wall, bool Armed, bool Key)>();
				list.Add((fa.Info.HasTraitInfo<LineBuildInfo>(), fa.Info.HasTraitInfo<AttackBaseInfo>(),
					fa.Info.HasTraitInfo<BaseBuildingInfo>() || fa.Info.HasTraitInfo<ProductionInfo>()));
			}

			var minKnown = AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishNoProductionMinKnownBuildings));
			return known.Values.Any(list => AggTeamPolicyCA.SeatLacksProduction(list, minKnown));
		}

		/// <summary>A8: the squad's pick among the visible enemies. Flag off, or not a Rush squad: exactly the legacy nearest pick.</summary>
		Actor AggTeamPickVisible(List<Actor> visible, Actor source, SquadCA owner)
		{
			var nearest = visible.ClosestToIgnoringPath(source.CenterPosition);
			if (!Info.AggTeamEnabled || owner == null || owner.Type != SquadCAType.Rush || visible.Count < 2 || AggTeamSquadDefends(owner))
				return nearest;

			var from = source.CenterPosition;
			var candidates = visible.OrderBy(a => (a.CenterPosition - from).HorizontalLengthSquared).ThenBy(a => a.ActorID)
				.Take(Math.Max(1, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetMaxCandidates)))).ToList();

			var seatArmy = AggTeamSeatArmies();

			var radius = WDist.FromCells(AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetDefenceRadiusCells)));
			var list = new List<AggTeamPolicyCA.TargetCandidate>();
			foreach (var a in candidates)
			{
				var key = a.Info.HasTraitInfo<BuildingInfo>() && (a.Info.HasTraitInfo<BaseBuildingInfo>() || a.Info.HasTraitInfo<ProductionInfo>());
				var eco = a.Info.HasTraitInfo<BuildingInfo>() && a.Info.HasTraitInfo<RefineryInfo>();
				long defence = 0;
				foreach (var d in World.FindActorsInCircle(a.CenterPosition, radius))
					if (d != a && IsPreferredObservedEnemyUnit(d) && d.Info.HasTraitInfo<AttackBaseInfo>() && !d.Info.HasTraitInfo<HarvesterInfo>())
						defence += UnitValue(d);

				list.Add(new AggTeamPolicyCA.TargetCandidate(a.ActorID, UnitValue(a), (int)Math.Min(int.MaxValue, defence),
					key ? AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetKeyWeightPercent), a.Owner) : eco ? AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetEconomyWeightPercent), a.Owner) : AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetOtherWeightPercent), a.Owner),
					key || eco, (long)(a.CenterPosition - from).HorizontalLengthSquared, a.Owner != null ? seatArmy.GetValueOrDefault(a.Owner) : 0));
			}

			var id = AggTeamPolicyCA.PickTarget(list, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetDistanceHalfCells)),
				AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetWeakSeatBonusPercent)) * (aggFinishActive ? Math.Max(1, AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamFinishWeakSeatMultiplier))) : 1), AggP(nameof(SquadManagerBotModuleCAInfo.AggTeamTargetReachCells)));
			var picked = id == null ? null : candidates.FirstOrDefault(a => a.ActorID == id.Value);
			if (picked != null && picked != nearest)
				AggPicks++;
			return picked ?? nearest;
		}
	}
}
