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

// IMPL02-AGG-CORE (AGGRESSION_PLAN_01 A1-A3 and the A4 no-abandonment part): the world side of AggressionPolicyCA.
// Every member is reached only through a call site that first checks AggressionOn, and AggressionOn is false whenever
// StalemateBreakerEnabled (the ON-arm switch) is false, so the OFF arm reads, allocates and orders nothing here.
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCA
	{
		/// <summary>The one ON gate for the aggression rules (see AggressionPolicyCA.Enabled).</summary>
		internal bool AggressionOn => AggressionPolicyCA.Enabled(Info.StalemateBreakerEnabled, Info.AggressionEnabled, Info.UseCombatPredictor);

		/// <summary>
		/// A9 thin accessor (Opus ruling D1912510): every A1-A4 threshold is read here, by its yaml key, for the squad whose
		/// decision it is. FREEZE9 seam S1: Sonnet's layered resolver (Global x Personality x OwnFaction x Matchup), tactical
		/// form, against the seat the squad engages. With empty multiplier tables it is the personality yaml value.
		/// </summary>
		internal int AggressionParam(string key, SquadCA squad) => AggP(key, AggressionEnemyOf(squad));

		/// <summary>The seat a squad is engaging: the owner of its live or remembered target, null without one. The resolver's
		/// matchup layer keys on it; the owner of a frozen record is what the bot already saw, so no hidden state is read.</summary>
		internal static Player AggressionEnemyOf(SquadCA squad) => squad?.Target.Type switch
		{
			TargetType.Actor => squad.Target.Actor?.Owner,
			TargetType.FrozenActor => squad.Target.FrozenActor?.Owner,
			_ => null
		};

		/// <summary>
		/// A7 seam (Sonnet, impl02-agg-team): true while the finishing push is on. In the fight the hold rules already keep a
		/// squad in unless its coalition is below FleeRatio; the idle decision attacks in every band but Flee. FREEZE9 seam S2:
		/// the agg-team flag, which is false with CG off and under a defence RecallAll.
		/// </summary>
		internal bool AggressionFinishActive => AggTeamFinishActive;

		/// <summary>FREEZE9 CF seam: the squad holds a unit that the agg-team defence plan keeps home. The idle decision and the
		/// march hold leave such a squad at home. False with CG off and for a missing squad. Erratum CF-L1: only Rush squads,
		/// the plan's own domain (detachment candidates and the recall are Rush only), so a RecallAll does not freeze other types.</summary>
		internal bool AggressionSquadDefends(SquadCA squad) => squad?.Type == SquadCAType.Rush && AggTeamSquadDefends(squad);

		PlayerStatistics aggressionStats;

		/// <summary>The bot's own kill value so far: its own PlayerStatistics, so no hidden enemy state is read.</summary>
		long AggressionKillsCost() => (aggressionStats ??= Player.PlayerActor.TraitOrDefault<PlayerStatistics>())?.KillsCost ?? 0;

		/// <summary>A1: armed own or allied-bot units, and own armed buildings (defences). Allied buildings and human allies
		/// are not counted: an allied bot's units are what an allied bot can be relied on to fight with.</summary>
		bool IsAggressionFriend(Actor a)
		{
			if (!a.IsInWorld || a.IsDead || a.Owner == null || !a.Info.HasTraitInfo<AttackBaseInfo>())
				return false;

			if (a.Owner == Player)
				return true;

			return a.Owner.IsBot && Player.IsAlliedWith(a.Owner) && !a.Info.HasTraitInfo<BuildingInfo>();
		}

		/// <summary>A1: the friends a squad fights with: within AggressionCoalitionCells of the squad, and (idle check) within
		/// IdleScanRadius of its target. Squad members are excluded (they are counted as the squad itself). Ordered by actor id.</summary>
		internal List<Actor> AggressionFriends(SquadCA squad, WPos? target)
		{
			var members = new HashSet<Actor>(squad.Units.Select(u => u.Actor));
			var found = new HashSet<Actor>();
			foreach (var a in World.FindActorsInCircle(squad.CenterPosition, WDist.FromCells(AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionCoalitionCells), squad))))
				if (!members.Contains(a) && IsAggressionFriend(a))
					found.Add(a);

			if (target is WPos t)
				foreach (var a in World.FindActorsInCircle(t, WDist.FromCells(Info.IdleScanRadius)))
					if (!members.Contains(a) && IsAggressionFriend(a))
						found.Add(a);

			return found.OrderBy(a => a.ActorID).ToList();
		}

		/// <summary>A4: members of own or allied-bot ground squads that are in a fight (attack or hit state) and whose centre
		/// is within AggressionAllyEngagedCells of this squad. Read from the allies' own squad lists; ordered by actor id.</summary>
		internal List<Actor> AggressionAllyEngagedUnits(SquadCA squad)
		{
			var center = squad.CenterPosition;
			var range = (long)WDist.FromCells(AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionAllyEngagedCells), squad)).Length;
			var rangeSquared = range * range;
			var found = new HashSet<Actor>();
			foreach (var p in World.Players)
			{
				if (p != Player && !(p.IsBot && Player.IsAlliedWith(p)))
					continue;

				foreach (var manager in p.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>())
				{
					if (manager.IsTraitDisabled)
						continue;

					foreach (var s in manager.Squads)
					{
						if (ReferenceEquals(s, squad) || !s.IsValid || !AggressionPolicyCA.IsEngagedState(s.FuzzyStateMachine?.CurrentState))
							continue;

						if ((s.CenterPosition - center).HorizontalLengthSquared > rangeSquared)
							continue;

						foreach (var u in s.Units)
							if (u.Actor != null && u.Actor.IsInWorld && !u.Actor.IsDead)
								found.Add(u.Actor);
					}
				}
			}

			return found.OrderBy(a => a.ActorID).ToList();
		}

		/// <summary>A1: the square-law ratio of the squad plus its friends against the enemies (the PredictedRatio method,
		/// with the own side widened). Only enemies that can fight count, as in PredictedRatio.</summary>
		internal double AggressionRatio(SquadCA squad, IEnumerable<Actor> friends, IReadOnlyList<Actor> enemies)
		{
			CanaryObservedAll(enemies, "aggression-ratio");
			var rules = World.Map.Rules;
			var eff = Info.UseEffectiveDamageModel;
			var ownActors = squad.Units.Where(u => !unitCannotBeOrdered(u.Actor)).Select(u => u.Actor)
				.Concat(friends).Distinct().ToList();

			List<(BotUnitProfile, int)> Profiles(IEnumerable<Actor> actors) => actors.GroupBy(a => a.Info)
				.SelectMany(g => eff
					? g.Select(a => (BotUnitProfiles.Get(a, Player, true), 1))
					: new[] { (BotUnitProfiles.Get(rules, g.Key, false), g.Count()) })
				.ToList();

			var own = Profiles(ownActors);
			var foes = Profiles(enemies.Where(e => e.Info.HasTraitInfo<AttackBaseInfo>()));
			return BotCombatPredictor.Predict(own, foes, eff).Ratio;
		}

		/// <summary>
		/// FREEZE10 DIAG-FIX-01 seam (idle path): an idle squad in the flee band is re-judged on the ratio that also counts the allied squads
		/// fighting within AggressionAllyEngagedCells, as AggressionHoldsInFight does for a committed squad; a ratio at or above FleeRatio turns
		/// the flight into the forward regroup (AggressionPolicyCA.IdleBandWithAllies). Returns <paramref name="band"/> and
		/// <paramref name="ratio"/> unchanged, reading nothing, with the CG switch off or without an engaged ally. The idle site names no
		/// agg-team type: the switch is read here. The returned ratio is the one that decided (it is recorded for the retreat_start row).
		/// </summary>
		internal (AggressionBandCA Band, double Ratio) AggressionIdleAlliedBand(SquadCA squad, List<Actor> friends, IReadOnlyList<Actor> enemies, AggressionBandCA band, double ratio)
		{
			if (!Info.AggTeamEnabled || band != AggressionBandCA.Flee)
				return (band, ratio);

			var engaged = AggressionAllyEngagedUnits(squad);
			if (engaged.Count == 0)
				return (band, ratio);

			var withAllies = AggressionRatio(squad, friends.Concat(engaged), enemies);
			return (AggressionPolicyCA.IdleBandWithAllies(band, withAllies,
				AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionFleeRatioPct), squad), AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionEngageRatioPct), squad)), withAllies);
		}

		/// <summary>A2: start (or restart) the commit the window and the trade rule measure from.</summary>
		internal void AggressionCommit(SquadCA squad)
		{
			squad.AggressionCommitTick = World.WorldTick;
			squad.AggressionCommitKills = AggressionKillsCost();
			squad.AggressionCommitMembers.Clear();
			foreach (var u in squad.Units)
				if (u.Actor != null && !u.Actor.IsDead)
					squad.AggressionCommitMembers[u.Actor] = UnitValue(u.Actor);
		}

		/// <summary>A2: the squad is no longer committed (it is idle again).</summary>
		internal static void AggressionUncommit(SquadCA squad)
		{
			squad.AggressionCommitTick = -1;
			squad.AggressionCommitKills = 0;
			squad.AggressionCommitMembers.Clear();
		}

		/// <summary>The squad's committed value, and the part of it that is dead now.</summary>
		(long Committed, long Lost) AggressionCommitLoss(SquadCA squad)
		{
			long committed = 0, lost = 0;
			foreach (var (actor, value) in squad.AggressionCommitMembers)
			{
				committed += value;
				if (actor.IsDead)
					lost += value;
			}

			return (committed, lost);
		}

		/// <summary>Report-only (Opus S6 (i)), for A6 telemetry: whether a committed squad is winning its trade. The kills are the
		/// bot's own player-wide KillsCost delta since the commit (notes 01 §3.2). Never a hold reason.</summary>
		internal bool AggressionWinningTrade(SquadCA squad) =>
			squad.AggressionCommitTick >= 0
			&& AggressionPolicyCA.IsWinningTrade(AggressionKillsCost() - squad.AggressionCommitKills, AggressionCommitLoss(squad).Lost);

		/// <summary>
		/// A2/A4 for a committed squad whose legacy flee rule fired: why it stays (None = it may flee). In a fight (no target
		/// point) the enemies are the armed, observed enemies within DangerScanRadius of the squad, the legacy flee rule's own
		/// scan. On the march (a target point) they are the observed enemies within IdleScanRadius of the target, the idle
		/// check's scan, and friends near the target count too: a remembered-defence verdict does not turn back a squad whose
		/// coalition is not decisively out-numbered by what it observes there.
		/// The ratios are computed before any hold is honoured, so a decisive loss flees whatever the window says (review C1);
		/// on the march the window is not a reason (review C3).
		/// </summary>
		internal AggressionPolicyCA.HoldReason AggressionHoldsInFight(SquadCA squad, WPos? target = null)
		{
			if (squad.AggressionCommitTick < 0)
				AggressionCommit(squad);

			var (committed, lost) = AggressionCommitLoss(squad);
			var since = World.WorldTick - squad.AggressionCommitTick;

			var scanCenter = target ?? squad.CenterPosition;
			var scanRadius = target != null ? Info.IdleScanRadius : Info.DangerScanRadius;
			var enemies = World.FindActorsInCircle(scanCenter, WDist.FromCells(scanRadius))
				.Where(a => IsPreferredObservedEnemyUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>())
				.OrderBy(a => a.ActorID).ToList();

			// The engaged allies are counted only when the coalition alone is below FleeRatio; otherwise they cannot change it.
			var flee = AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionFleeRatioPct), squad);
			var friends = AggressionFriends(squad, target);
			var coalition = AggressionRatio(squad, friends, enemies);
			var withAllies = coalition;
			if (coalition * 100 < flee)
			{
				var engaged = AggressionAllyEngagedUnits(squad);
				if (engaged.Count > 0)
					withAllies = AggressionRatio(squad, friends.Concat(engaged), enemies);
			}

			var hold = AggressionPolicyCA.HoldsInFight(since, AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionMinCommitTicks), squad), committed, lost,
				AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionCommitBreakLossPct), squad), coalition, withAllies, flee, onMarch: target != null);

			// A6 telemetry: nothing holds the squad, so it retreats next; record what the rule saw for the retreat_start row.
			if (hold == AggressionPolicyCA.HoldReason.None)
				AggressionRecordFightEvidence(squad, withAllies, enemies, lost);

			return hold;
		}
	}
}
