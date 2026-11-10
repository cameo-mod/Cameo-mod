#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

// A10 RETREAT-RALLY (design DRAFT02): the world side of RetreatRallyPolicyCA. A ground squad that decides to retreat (the decision is
// unchanged: ShouldFlee, the agg-core bands) is sent to the best friendly army instead of the base, or to the base cell on the
// enemy-to-own-base axis when none qualifies.
// OFF identity: every entry point below returns on its first line while RetreatRallyOn is false (flag off, CG off, CF off, no
// predictor): no allocation, no random number, no order, no telemetry row. The module draws no random numbers at all.
// Candidates are read in-process from own and allied-bot squad managers (AggressionAllyEngagedUnits form), never from a world-wide actor
// scan, and the enemy only through what the bot can see (IsPreferredObservedEnemyUnit).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using Policy = OpenRA.Mods.CA.Traits.BotModules.Squads.RetreatRallyPolicyCA;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>The destination a rally state works toward: identity, action, the lock tick and what the telemetry needs at the end.</summary>
	internal sealed class RetreatRallyPlan
	{
		public string Key;
		public string DestName;
		public string PlayerKey;
		public SquadCA DestSquad;
		public bool Ally;
		public bool DefenceRequest;
		public Policy.RallyAction Action;
		public Policy.RallyNeed Need;
		public int Score;
		public int PickTick;
		public int StartTick;
		public int Size0;
		public long V0;
		public string Site;
		public WPos DestPos;
		public HashSet<uint> PursuerIds;
	}

	/// <summary>One tick's measured facts about the destination (feeds Policy.Step).</summary>
	internal readonly record struct RetreatRallyObservation(bool Arrived, bool Losing, bool DestInvalid, bool TargetEnded, bool DestLaunched,
		WPos DestPos, WPos TargetPos, int RatioMilli);

	public partial class SquadManagerBotModuleCA
	{
		sealed class RallyEpisode
		{
			public int Hops;
			public int LastRallyTick = -1;
			public int FlightDecisionTick = -1;
			public CPos? Axis;
			public int AxisTick = int.MinValue / 2;
		}

		sealed class RallyCand
		{
			public SquadCA Squad;
			public string PlayerKey;
			public WPos Pos;
			public List<Actor> Friends;
		}

		sealed class RallyScan
		{
			public readonly List<Policy.Candidate> List = new();
			public readonly Dictionary<string, RallyCand> ByKey = new();
		}

		static readonly ConditionalWeakTable<SquadCA, StrongBox<int>> RallyCooldowns = new();
		static readonly ConditionalWeakTable<SquadCA, StrongBox<int>> RallyIds = new();
		static readonly ConditionalWeakTable<SquadCA, RallyEpisode> RallyEpisodes = new();
		static readonly ConditionalWeakTable<SquadCA, List<(string Key, int Until)>> RallyBlacklists = new();

		int retreatRallyNextId;

		// Counters for tests and telemetry hooks (never read by a decision).
		internal int RallyPicks;
		internal int RallyBases;
		internal int RallyArrivals;
		internal int RallyEnds;
		internal int RallyDowngrades;
		internal int RallyMerges;
		internal int RallyHopBlocks;
		internal int RallyPingPongBlocks;

		/// <summary>The one ON gate of A10: the agg-team and agg-core switches (M1: every agg-core member A10 calls is behind AggressionOn) and the A10 flag.</summary>
		internal bool RetreatRallyOn => Info.AggTeamEnabled && Info.RetreatRallyEnabled && AggressionOn && !IsTraitDisabled;

		int RallyP(string name, SquadCA squad) => AggP(name, AggressionEnemyOf(squad));

		/// <summary>An A10 parameter for the rally state (A9 resolver against the seat the squad engages). Zero while A10 is off (the state cannot exist then).</summary>
		internal int RetreatRallyParam(string name, SquadCA squad) => RetreatRallyOn ? RallyP(name, squad) : 0;

		static string Inv(FormattableString text) => FormattableString.Invariant(text);

		static string RallyPlayerKey(Player p) => p.InternalName ?? "#" + p.ClientIndex;

		static bool RallyIsGroundType(SquadCA squad) => squad.Type == SquadCAType.Rush || squad.Type == SquadCAType.Guerrilla;

		Policy.Params RallyParams(SquadCA squad) => new(
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMaxDistanceCells), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyWeightDistance), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyWeightStrength), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyWeightNeed), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyStrengthTargetPercent), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyNeedFightPoints), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyNeedDefendPoints), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyNeedMarchPoints), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyNeedIdlePoints), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyAllyPenaltyPoints), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMinScore), squad),
			RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMinSquadValue), squad));

		int RetreatRallyIdOf(SquadCA squad) => RallyIds.GetValue(squad, _ => new StrongBox<int>(++retreatRallyNextId)).Value;

		/// <summary>
		/// M2: a squad that starts (or continues) a flight is marked, so no other squad rallies onto it. Called at every flight tick: both idle flee
		/// sites and the Flee state. A flight that was not marked within RetreatMarkTicks is a new episode and may decide again.
		/// </summary>
		internal void RetreatRallyNoteFlight(SquadCA squad)
		{
			if (!RetreatRallyOn)
				return;
			if (squad == null)
				return;

			var now = World.WorldTick;
			if (!Policy.IsMarked(squad.RetreatSinceTick, now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRetreatMarkTicks), squad)))
			{
				var episode = RallyEpisodes.GetValue(squad, _ => new RallyEpisode());
				episode.FlightDecisionTick = -1;
				episode.Axis = null;
				episode.AxisTick = int.MinValue / 2;
			}

			squad.RetreatSinceTick = now;
		}

		/// <summary>
		/// A retreating ground squad asks where to go (H1 Flee state, H2 both idle flee sites). True = a destination was chosen and <paramref name="plan"/> is
		/// the rally to enter; false = the legacy flight runs (the caller's code is unchanged), with the base cell on the axis. One decision per flight
		/// episode: later asks of the same flight return false at once (a rally is a one-way downgrade, never base -> rally within an episode).
		/// </summary>
		internal bool TryRetreatRally(SquadCA squad, string site, out RetreatRallyPlan plan)
		{
			plan = null;
			if (!RetreatRallyOn)
				return false;
			if (squad == null || !squad.IsValid || !RallyIsGroundType(squad))
				return false;

			var now = World.WorldTick;
			var episode = RallyEpisodes.GetValue(squad, _ => new RallyEpisode());
			if (episode.FlightDecisionTick >= 0)
				return false;

			episode.FlightDecisionTick = now;
			var p = RallyParams(squad);

			string why = null;
			var best = 0;
			if (ReferenceEquals(squad, stalemateSquad))
				why = "breaker";

			if (why == null)
			{
				var defence = AggTeamDefence();
				var provider = StalemateUrgency;
				why = Policy.HomeBlock(defence.RecallAll, AggTeamSquadDefends(squad), defence.IsActive, provider != null && provider.StalemateHomeThreat);
			}

			if (why == null && Policy.HopsExhausted(episode.Hops, episode.LastRallyTick, now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMaxHops), squad),
				RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyEpisodeResetTicks), squad)))
			{
				why = "hops";
				RallyHopBlocks++;
			}

			if (why == null && RallyCooldowns.TryGetValue(squad, out var cooldown) && Policy.Blocked(cooldown.Value, now))
				why = "cooldown";

			Policy.Choice choice = default;
			RallyScan scan = null;
			HashSet<uint> pursuerIds = null;
			if (why == null)
			{
				scan = RallyCollect(squad, p, now);
				var pursuers = RallyPursuers(squad);
				pursuerIds = new HashSet<uint>(pursuers.Select(a => a.ActorID));
				choice = Policy.Choose(scan.List, SquadValueOf(squad), p, c => RallyWorldGuard(squad, c, scan, pursuers));
				RallyPingPongBlocks += choice.MarkedRejects;
				best = choice.Best;
				if (!choice.Found)
					why = choice.Why;
			}

			if (why != null)
			{
				RallyBases++;
				if (Info.StalemateDiagnosticsEnabled)
				{
					var axis = RetreatRallyBaseCellPeek(squad);
					DiagnosticSquadEventAt(squad, "rally_base",
						Inv($"why={why};best={best};axis={(axis == null ? "none" : BotPassiveDiagnosticWriter.Cell(axis.Value.X, axis.Value.Y))}"), null);
				}

				return false;
			}

			var rc = scan.ByKey[Policy.KeyOf(choice.Pick)];
			plan = RallyMakePlan(choice.Pick, rc, choice.Score, now, squad, site, pursuerIds);
			episode.Hops = Policy.EffectiveHops(episode.Hops, episode.LastRallyTick, now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyEpisodeResetTicks), squad)) + 1;
			episode.LastRallyTick = now;
			RallyPicks++;
			if (Info.StalemateDiagnosticsEnabled)
			{
				var cell = World.Map.CellContaining(rc.Pos);
				DiagnosticSquadEventAt(squad, "rally_pick",
					Inv($"dest={plan.DestName};score={choice.Score};d={choice.Pick.DistanceCells};str={choice.Pick.Strength};need={RallyNeedName(choice.Pick.Need)};n={choice.Considered};site={site}"), cell);
			}

			return true;
		}

		static string RallyNeedName(Policy.RallyNeed need) => need switch
		{
			Policy.RallyNeed.Fight => "fight",
			Policy.RallyNeed.Defend => "defend",
			Policy.RallyNeed.March => "march",
			_ => "idle"
		};

		RetreatRallyPlan RallyMakePlan(Policy.Candidate c, RallyCand rc, int score, int now, SquadCA squad, string site, HashSet<uint> pursuerIds) => new()
		{
			Key = Policy.KeyOf(c),
			DestName = c.DefenceRequest ? "defreq" : c.Ally ? "ally:" + rc.PlayerKey : c.Idle ? "own_idle" : "own",
			PlayerKey = rc.PlayerKey,
			DestSquad = rc.Squad,
			Ally = c.Ally,
			DefenceRequest = c.DefenceRequest,
			Action = Policy.ActionFor(c),
			Need = c.Need,
			Score = score,
			PickTick = now,
			StartTick = now,
			Size0 = squad.Units.Count,
			V0 = RallyUnitValue(squad),
			Site = site,
			DestPos = rc.Pos,
			PursuerIds = pursuerIds
		};

		long RallyUnitValue(SquadCA squad)
		{
			long value = 0;
			foreach (var u in squad.Units)
				if (u.Actor != null && !u.Actor.IsDead)
					value += UnitValue(u.Actor);
			return value;
		}

		// ---- candidates ----

		bool RallyIsFriend(Actor a, bool humanAllies)
		{
			if (!a.IsInWorld || a.IsDead || a.Owner == null || !a.Info.HasTraitInfo<AttackBaseInfo>() || a.Info.HasTraitInfo<BuildingInfo>())
				return false;
			if (a.Owner == Player)
				return true;

			return Player.IsAlliedWith(a.Owner) && (a.Owner.IsBot || humanAllies);
		}

		/// <summary>The armed own and allied-bot (human allies behind AggTeamRallyHumanAllies) non-building units within the destination radius of a point, and the members of the destination squad, minus the retreating squad.</summary>
		List<Actor> RallyFriendsNear(SquadCA squad, WPos pos, SquadCA dest)
		{
			var humans = RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyHumanAllies), squad) != 0;
			var members = new HashSet<Actor>(squad.Units.Select(u => u.Actor));
			var found = new HashSet<Actor>();
			foreach (var a in World.FindActorsInCircle(pos, WDist.FromCells(RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyDestRadiusCells), squad))))
				if (!members.Contains(a) && RallyIsFriend(a, humans))
					found.Add(a);

			if (dest != null)
				foreach (var u in dest.Units)
					if (u.Actor != null && !members.Contains(u.Actor) && u.Actor.IsInWorld && !u.Actor.IsDead)
						found.Add(u.Actor);

			return found.OrderBy(a => a.ActorID).ToList();
		}

		long RallyStrength(IEnumerable<Actor> friends)
		{
			long value = 0;
			foreach (var a in friends)
				value += UnitValue(a);
			return value;
		}

		static int RallyCells(WVec delta) => (int)Math.Min(int.MaxValue, (long)delta.HorizontalLength / Policy.CellUnits);

		RallyScan RallyCollect(SquadCA squad, in Policy.Params p, int now)
		{
			var scan = new RallyScan();
			var center = squad.CenterPosition;
			var markTicks = RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRetreatMarkTicks), squad);
			var destRadius = WDist.FromCells(RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyDestRadiusCells), squad));
			var requests = TeamBlackboard.CollectBroadcasts(Player).Where(b => b.RequestsDefence && b.DefendPosition != WPos.Zero).ToList();

			foreach (var pl in World.Players)
			{
				if (pl != Player && !(pl.IsBot && Player.IsAlliedWith(pl)))
					continue;

				var ally = pl != Player;
				var playerKey = RallyPlayerKey(pl);
				foreach (var m in pl.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>())
				{
					if (m.IsTraitDisabled)
						continue;

					var ownerKey = playerKey + "/" + (m.Info.InstanceName ?? "");
					foreach (var s in m.Squads)
					{
						if (!RallyIsGroundType(s) || !s.IsValid)
							continue;

						var state = s.FuzzyStateMachine?.CurrentState;
						var idle = state is GroundUnitsIdleStateCA;
						var engaged = AggressionPolicyCA.IsEngagedState(state);
						var attacking = AggTeamIsAttackState(state);
						var need = engaged ? Policy.RallyNeed.Fight : attacking ? Policy.RallyNeed.March : Policy.RallyNeed.Idle;
						if (!idle && (attacking || engaged))
							foreach (var r in requests)
								if ((r.DefendPosition - s.CenterPosition).HorizontalLengthSquared <= (long)destRadius.Length * destRadius.Length)
								{
									need = Policy.RallyNeed.Fight;
									break;
								}

						// An own idle squad is merged into the pool by the A3 mechanism, which is Rush only: an idle guerrilla squad is not a destination.
						var other = !(idle ? s.Type == SquadCAType.Rush : engaged || attacking);
						var self = ReferenceEquals(s, squad);
						var key = ownerKey + "#" + m.RetreatRallyIdOf(s);
						var c = new Policy.Candidate(ownerKey, m.RetreatRallyIdOf(s), ally, idle, false, need, m.SquadValueOf(s), 0,
							RallyCells(s.CenterPosition - center), IsRetreatingForLiveness(s), state is GroundUnitsRallyStateCA,
							Policy.IsMarked(s.RetreatSinceTick, now, markTicks), RallyCooldowns.TryGetValue(s, out var cooldown) && Policy.Blocked(cooldown.Value, now),
							RallyBlacklisted(squad, key, now), ReferenceEquals(s, m.stalemateSquad), self, other);
						List<Actor> friends = null;
						if (Policy.Exclusion(c, p) == null)
						{
							friends = RallyFriendsNear(squad, s.CenterPosition, s);
							c = c with { Strength = RallyStrength(friends) };
						}

						scan.List.Add(c);
						scan.ByKey[key] = new RallyCand { Squad = s, PlayerKey = playerKey, Pos = s.CenterPosition, Friends = friends };
					}
				}
			}

			foreach (var b in requests)
			{
				var ownerKey = CoalitionFold.ParticipantKey(b);
				var key = ownerKey + "#req";
				var c = new Policy.Candidate(ownerKey, -1, true, false, true, Policy.RallyNeed.Defend, b.OwnArmyValue, b.OwnArmyValue,
					RallyCells(b.DefendPosition - center), false, false, false, false, RallyBlacklisted(squad, key, now), false, false, false);
				List<Actor> friends = null;
				if (Policy.Exclusion(c, p) == null)
					friends = RallyFriendsNear(squad, b.DefendPosition, null);

				scan.List.Add(c);
				scan.ByKey[key] = new RallyCand { Squad = null, PlayerKey = ownerKey, Pos = b.DefendPosition, Friends = friends };
			}

			return scan;
		}

		static bool RallyBlacklisted(SquadCA squad, string key, int now)
		{
			if (!RallyBlacklists.TryGetValue(squad, out var list))
				return false;

			foreach (var (k, until) in list)
				if (k == key && Policy.Blocked(until, now))
					return true;
			return false;
		}

		// ---- guards that need the world (G1, G2, reachability) ----

		/// <summary>The visible armed enemies within the danger scan radius of a point (fog-honest), ordered by actor id.</summary>
		List<Actor> RallyEnemiesNear(SquadCA squad, WPos pos)
		{
			var radius = WDist.FromCells(RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyDangerScanRadiusCells), squad));
			return World.FindActorsInCircle(pos, radius)
				.Where(a => IsPreferredObservedEnemyUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>())
				.OrderBy(a => a.ActorID).ToList();
		}

		List<Actor> RallyPursuers(SquadCA squad) => RallyEnemiesNear(squad, squad.CenterPosition);

		static WPos RallyCentroid(IReadOnlyList<Actor> actors)
		{
			long x = 0, y = 0;
			foreach (var a in actors)
			{
				x += a.CenterPosition.X;
				y += a.CenterPosition.Y;
			}

			return new WPos((int)(x / actors.Count), (int)(y / actors.Count), 0);
		}

		bool RallyReachable(Actor leader, CPos cell)
		{
			var mobile = leader?.TraitOrDefault<Mobile>();
			return mobile != null && World.Map.Contains(cell) && mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, leader.Location, cell);
		}

		string RallyWorldGuard(SquadCA squad, Policy.Candidate c, RallyScan scan, List<Actor> pursuers)
		{
			if (!RetreatRallyOn)
				return null;

			var rc = scan.ByKey[Policy.KeyOf(c)];
			if (pursuers.Count > 0)
			{
				var at = RallyCentroid(pursuers);
				if (Policy.BehindPursuers(true, (rc.Pos - at).HorizontalLength, (squad.CenterPosition - at).HorizontalLength,
					RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyAwayGainCells), squad)))
					return "pursuer";
			}

			if (!RallyReachable(squad.Units[0].Actor, World.Map.CellContaining(rc.Pos)))
				return "unreachable";

			if (Policy.ActionFor(c) == Policy.RallyAction.Support)
			{
				var enemies = RallyEnemiesNear(squad, rc.Pos);
				if (enemies.Count > 0 && Policy.Losing(AggressionRatio(squad, rc.Friends ?? new List<Actor>(), enemies),
					RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMinJoinRatioPercent), squad), true, false))
					return "losing";
			}

			return null;
		}

		// ---- the rally state's measurements ----

		/// <summary>L6 march_flee: the coalition (the squad and the friends near it) against the visible armed enemies near it is below the agg-core flee ratio. The pursuers the squad is already running from (plan.PursuerIds) do not count: they are what it is leaving.</summary>
		internal bool RetreatRallyMarchFlee(SquadCA owner, RetreatRallyPlan plan)
		{
			if (!RetreatRallyOn)
				return false;

			var enemies = World.FindActorsInCircle(owner.CenterPosition, WDist.FromCells(Info.DangerScanRadius))
				.Where(a => IsPreferredObservedEnemyUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>() && (plan?.PursuerIds == null || !plan.PursuerIds.Contains(a.ActorID)))
				.OrderBy(a => a.ActorID).ToList();
			if (enemies.Count == 0)
				return false;

			var ratio = AggressionRatio(owner, AggressionFriends(owner, null), enemies);
			return Policy.MarchFlee(ratio, AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionFleeRatioPct), owner));
		}

		/// <summary>G1 at arrival: the squad joining the army at its destination now faces a lost fight there.</summary>
		bool RallyJoinLosing(SquadCA owner, WPos pos, out int ratioMilli)
		{
			ratioMilli = AggressionEvidenceCA.RatioMilliCap;
			if (!RetreatRallyOn)
				return false;

			var enemies = RallyEnemiesNear(owner, pos);
			if (enemies.Count == 0)
				return false;

			var ratio = AggressionRatio(owner, RallyFriendsNear(owner, pos, null), enemies);
			ratioMilli = AggressionEvidenceCA.ToMilli(ratio);
			return Policy.Losing(ratio, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMinJoinRatioPercent), owner), true, false);
		}

		/// <summary>One tick's facts about the destination: where it is, whether it is still valid, whether its fight is over, whether the squad arrived and whether it is losing.</summary>
		internal RetreatRallyObservation RetreatRallyObserve(SquadCA owner, RetreatRallyPlan plan, Policy.RallyPhase phase, Policy.RallyAction action)
		{
			if (!RetreatRallyOn)
				return default;

			var now = World.WorldTick;
			var destPos = plan.DestPos;
			var targetPos = plan.DestPos;
			bool invalid = false, ended = false, launched = false;
			if (plan.DefenceRequest)
			{
				var request = TeamBlackboard.CollectBroadcasts(Player)
					.FirstOrDefault(b => b.RequestsDefence && b.DefendPosition != WPos.Zero && CoalitionFold.ParticipantKey(b) == plan.PlayerKey);
				if (request == null)
					ended = true;
				else
					destPos = targetPos = request.DefendPosition;
			}
			else
			{
				var dest = plan.DestSquad;
				if (dest == null || !dest.IsValid)
					invalid = true;
				else
				{
					var state = dest.FuzzyStateMachine?.CurrentState;
					destPos = dest.CenterPosition;
					targetPos = dest.IsTargetValid ? dest.Target.CenterPosition : destPos;
					var idle = state is GroundUnitsIdleStateCA;
					var active = AggressionPolicyCA.IsEngagedState(state) || AggTeamIsAttackState(state);
					if (IsRetreatingForLiveness(dest) || state is GroundUnitsRallyStateCA
						|| Policy.IsMarked(dest.RetreatSinceTick, now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRetreatMarkTicks), owner))
						|| (plan.Ally && idle))
						invalid = true;
					else if (action == Policy.RallyAction.Merge)
						launched = active;
					else
						ended = !active;
				}
			}

			plan.DestPos = destPos;
			var arrived = Policy.Arrived((owner.CenterPosition - destPos).HorizontalLength, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyDestRadiusCells), owner));
			var losing = !invalid && !ended && RetreatRallyMarchFlee(owner, plan);
			var ratioMilli = AggressionEvidenceCA.RatioMilliCap;
			if (!losing && !invalid && !ended && arrived && phase == Policy.RallyPhase.March && action == Policy.RallyAction.Support)
				losing = RallyJoinLosing(owner, destPos, out ratioMilli);

			return new RetreatRallyObservation(arrived, losing, invalid, ended, launched, destPos, targetPos, ratioMilli);
		}

		/// <summary>
		/// G7: re-score the destinations. The current one is replaced when it became invalid (at once), or after the lock by a destination scoring
		/// the switch margin above it. Returns the new plan (and writes rally_switch), or null to keep the current one.
		/// </summary>
		internal RetreatRallyPlan RetreatRallyRescore(SquadCA owner, RetreatRallyPlan plan, bool destinationInvalid)
		{
			if (!RetreatRallyOn)
				return null;

			var now = World.WorldTick;
			var p = RallyParams(owner);
			var scan = RallyCollect(owner, p, now);
			var pursuers = RallyPursuers(owner);
			var choice = Policy.Choose(scan.List, SquadValueOf(owner), p, c => RallyWorldGuard(owner, c, scan, pursuers));
			if (!choice.Found || Policy.KeyOf(choice.Pick) == plan.Key)
				return null;

			var oldScore = plan.Score;
			if (!destinationInvalid)
				foreach (var c in scan.List)
					if (Policy.KeyOf(c) == plan.Key && Policy.Exclusion(c, p) == null)
					{
						oldScore = Policy.Score(c, SquadValueOf(owner), p);
						break;
					}

			if (!Policy.MaySwitch(plan.PickTick, now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyMinCommitTicks), owner), choice.Score, oldScore,
				RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallySwitchMarginPercent), owner), destinationInvalid))
				return null;

			var rc = scan.ByKey[Policy.KeyOf(choice.Pick)];
			var next = RallyMakePlan(choice.Pick, rc, choice.Score, now, owner, plan.Site, plan.PursuerIds);
			next.StartTick = plan.StartTick;
			next.Size0 = plan.Size0;
			next.V0 = plan.V0;
			if (Info.StalemateDiagnosticsEnabled)
				DiagnosticSquadEventAt(owner, "rally_switch", Inv($"from={plan.Key};to={next.Key};gain={choice.Score - oldScore}"), World.Map.CellContaining(rc.Pos));

			return next;
		}

		/// <summary>
		/// A stand-down of an open rally from outside (AggTeamApplyDetachment and the other DismissSquad callers do not run the state's Deactivate: the
		/// squad's units are cleared and the state machine is left alone): book the rally as detached (one rally_end row, the G5 cooldowns). Called first
		/// in DismissSquad, before the units are cleared; a no-op while A10 is off, for any other state, and for a rally that already ended (the
		/// merged_pool end itself dismisses through here).
		/// </summary>
		internal void RetreatRallyDetach(SquadCA squad)
		{
			if (!RetreatRallyOn)
				return;
			if (squad?.FuzzyStateMachine?.CurrentState is GroundUnitsRallyStateCA rally)
				rally.Deactivate(squad);
		}

		/// <summary>Arrival at the destination: counters, the rally_arrive row, and for a Support the agg-core commit (so the Coalition and AllyEngaged holds judge the squad as committed).</summary>
		internal void RetreatRallyArrive(SquadCA owner, RetreatRallyPlan plan, Policy.RallyAction action, int ratioMilli)
		{
			if (!RetreatRallyOn)
				return;

			RallyArrivals++;
			if (action == Policy.RallyAction.Support)
				AggressionCommit(owner);

			if (Info.StalemateDiagnosticsEnabled)
				DiagnosticSquadEventAt(owner, "rally_arrive", Inv($"dest={plan.DestName};ratio={ratioMilli}"), World.Map.CellContaining(plan.DestPos));
		}

		/// <summary>The end of a rally: cooldowns for the squad and its destination (G5), the blacklist after a lost destination (G6), the counters and the rally_end row.</summary>
		internal void RetreatRallyEnd(SquadCA owner, RetreatRallyPlan plan, Policy.RallyEnd end, Policy.RallyPhase phase)
		{
			if (!RetreatRallyOn)
				return;

			var now = World.WorldTick;
			RallyEnds++;
			if (Policy.NextState(end, phase) == Policy.RallyNext.FleeBaseOnly)
				RallyDowngrades++;
			if (end == Policy.RallyEnd.MergedPool)
				RallyMerges++;

			var until = Policy.BlockedUntil(now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyCooldownTicks), owner));
			RallyCooldowns.GetValue(owner, _ => new StrongBox<int>()).Value = until;
			if (plan.DestSquad != null)
				RallyCooldowns.GetValue(plan.DestSquad, _ => new StrongBox<int>()).Value = until;

			if (end == Policy.RallyEnd.AbortLosing || (end == Policy.RallyEnd.Expired && phase == Policy.RallyPhase.March))
			{
				var list = RallyBlacklists.GetValue(owner, _ => new List<(string Key, int Until)>());
				list.RemoveAll(e => e.Key == plan.Key || !Policy.Blocked(e.Until, now));
				list.Add((plan.Key, Policy.BlockedUntil(now, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyBlacklistTicks), owner))));
			}

			RallyEpisodes.GetValue(owner, _ => new RallyEpisode()).LastRallyTick = now;
			if (Info.StalemateDiagnosticsEnabled)
				DiagnosticSquadEventAt(owner, "rally_end",
					Inv($"end={Policy.EndName(end)};phase={Policy.PhaseName(phase)};ticks={now - plan.StartTick};size0={plan.Size0};size1={owner.Units.Count};v0={plan.V0};v1={RallyUnitValue(owner)}"),
					World.Map.CellContaining(plan.DestPos));
		}

		// ---- the fallback base cell ----

		/// <summary>
		/// H4: the cell a fleeing Rush/Guerrilla squad without a staging plan goes to: on the line enemy -> own base, AggTeamRallyFallbackDepthCells from the
		/// base (positive = the enemy-facing side), clamped and reachable. Null (the legacy random building) when A10 is off, the squad is another type,
		/// or no enemy reference or reachable cell exists. Held for one RescoreTicks per flight episode (stable orders).
		/// </summary>
		internal CPos? RetreatRallyBaseCell(SquadCA squad)
		{
			if (!RetreatRallyOn)
				return null;
			if (squad == null || !squad.IsValid || !RallyIsGroundType(squad))
				return null;

			var now = World.WorldTick;
			var episode = RallyEpisodes.GetValue(squad, _ => new RallyEpisode());
			if (episode.Axis != null && now - episode.AxisTick < RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRescoreTicks), squad))
				return episode.Axis;

			var cell = RallyAxisCompute(squad);
			if (cell == null)
				return null;

			episode.Axis = cell;
			episode.AxisTick = now;
			return episode.Axis;
		}

		/// <summary>
		/// The base cell for the rally_base telemetry row: the same value RetreatRallyBaseCell would give, but READ-ONLY (it never writes the episode cache),
		/// so the diagnostics flag cannot change a later flee cell (a diagnostic must never change a decision).
		/// </summary>
		internal CPos? RetreatRallyBaseCellPeek(SquadCA squad)
		{
			if (!RetreatRallyOn)
				return null;
			if (squad == null || !squad.IsValid || !RallyIsGroundType(squad))
				return null;

			if (RallyEpisodes.TryGetValue(squad, out var episode) && episode.Axis != null
				&& World.WorldTick - episode.AxisTick < RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyRescoreTicks), squad))
				return episode.Axis;

			return RallyAxisCompute(squad);
		}

		/// <summary>The pure axis computation (no cache access, no writes).</summary>
		CPos? RallyAxisCompute(SquadCA squad)
		{
			var map = World.Map;
			var enemyRef = RallyEnemyReference(squad);
			if (enemyRef == null)
				return null;

			var own = map.CellContaining(AggTeamHomePos());
			var enemy = map.CellContaining(enemyRef.Value);
			var leader = squad.Units[0].Actor;
			var cell = Policy.AxisCell(own.X, own.Y, enemy.X, enemy.Y, RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyFallbackDepthCells), squad),
				(x, y) => { var c = map.Clamp(new CPos(x, y)); return (c.X, c.Y); },
				(x, y) => RallyReachable(leader, new CPos(x, y)));
			return cell == null ? null : new CPos(cell.Value.X, cell.Value.Y);
		}

		/// <summary>Where the enemy is, for the axis: the pursuers' centroid, else the squad's target, else the nearest map start no own or allied player holds (public map data).</summary>
		WPos? RallyEnemyReference(SquadCA squad)
		{
			var pursuers = RallyPursuers(squad);
			if (pursuers.Count > 0)
				return RallyCentroid(pursuers);

			if (squad.IsTargetValid)
				return squad.Target.CenterPosition;

			var from = squad.CenterPosition;
			var homes = World.Players.Where(p => p == Player || Player.IsAlliedWith(p)).Select(p => p.HomeLocation).ToList();
			WPos? best = null;
			long bestDistance = long.MaxValue;
			foreach (var d in World.Map.ActorDefinitions)
			{
				if (d.Value.Value != "mpspawn")
					continue;

				var start = new ActorReference(d.Value.Value, d.Value).Get<LocationInit>().Value;
				if (homes.Any(h => (long)(h.X - start.X) * (h.X - start.X) + (long)(h.Y - start.Y) * (h.Y - start.Y) <= 100))
					continue;

				var pos = World.Map.CenterOfCell(start);
				var distance = (long)(pos - from).HorizontalLengthSquared;
				if (distance < bestDistance)
				{
					best = pos;
					bestDistance = distance;
				}
			}

			return best;
		}
	}
}
