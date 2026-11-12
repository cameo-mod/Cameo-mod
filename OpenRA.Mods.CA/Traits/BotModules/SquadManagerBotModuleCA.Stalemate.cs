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

// BOTPASSIVE-FIX01 IMPL01: the world wiring of the stalemate breaker (switch CD_stalemate_breaker, Info.StalemateBreakerEnabled)
// and the botpassive-diagnostic/1 telemetry (Info.StalemateDiagnosticsEnabled). Both flags default off; with both off every
// entry point below returns before allocating, reading the world, issuing an order or drawing a random number.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCA
	{
		static readonly ConditionalWeakTable<World, StalemateRotationCA> StalemateRotations = new();
		static readonly ConditionalWeakTable<SquadCA, string> DiagnosticSquadIds = new();

		// ---- breaker state (lives on the first switch-ON instance; the enabled instance drives it) ----
		SquadManagerBotModuleCA stalemateCachedOwner;
		StalemateBreakerSeatCA stalemateSeat;
		StalemateSeatOwnerCA stalemateSeatOwner;
		IBotStalemateUrgencyProvider[] stalemateUrgencyProviders;
		Dictionary<uint, CPos[]> stalemateNeutralActors;
		HashSet<CPos> stalemateNeutralCells;
		readonly HashSet<uint> stalemateReportedDead = new();

		// The breaker squad in THIS instance's Squads (null when none). Only ever set while the switch is ON.
		SquadCA stalemateSquad;

		// ---- telemetry state (lives on the first telemetry-ON instance) ----
		SquadManagerBotModuleCA diagnosticCachedOwner;
		BotPassiveDiagnosticWriter diagnosticWriter;
		bool diagnosticInitialized;
		int diagnosticSquadCounter;
		List<SquadCA> diagnosticPendingCommits;
		readonly Queue<int> diagnosticRetreatTicks = new();
		readonly Queue<(int Tick, int Kills)> diagnosticKillSamples = new();
		readonly StalemateStuckTrackerCA<SquadCA> diagnosticStuck = new();
		long diagnosticHomeThirdSquared = -1;

		SquadManagerBotModuleCA StalemateOwner => stalemateCachedOwner ??= Player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>()
			.FirstOrDefault(m => m.Info.StalemateBreakerEnabled) ?? this;

		SquadManagerBotModuleCA DiagnosticOwner => diagnosticCachedOwner ??= Player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>()
			.FirstOrDefault(m => m.Info.StalemateDiagnosticsEnabled) ?? this;

		string StalemateManagerName => Info.InstanceName ?? "";

		int StalemateSeatIndex => Array.IndexOf(World.Players, Player);

		IBotStalemateUrgencyProvider StalemateUrgency => (stalemateUrgencyProviders ??= Player.PlayerActor
			.TraitsImplementing<IBotStalemateUrgencyProvider>().ToArray()).FirstOrDefault(p => p.StalemateUrgencyEnabled);

		// IMPL01-HOTFIX03 (IMPL02-B): the one home predicate for admission, wave continuation and retreat deferral. Fail closed without a provider.
		static bool StalemateHomeBlocks(IBotStalemateUrgencyProvider provider) =>
			provider == null || StalemateBreakerPolicyCA.HomeBlocksBreaker(provider.StalemateHomeThreat, provider.StalemateHomeLatch);

		// FREEZE9 CF seam (Opus): with CG on, the agg-team defence plan answers a home threat by a proportional detachment, and
		// the breaker squad is never a detachment candidate, so the breaker is blocked only without a provider or under a
		// RecallAll. With CG off the plan is None and the HOTFIX03 predicate stays, so a CD-only arm is FREEZE8.
		bool StalemateBlocked(IBotStalemateUrgencyProvider provider) =>
			Info.AggTeamEnabled ? provider == null || AggTeamDefence().RecallAll : StalemateHomeBlocks(provider);

		static string UrgencyName(int u) => u switch { 0 => "normal", 1 => "pressured", 2 => "emergency", _ => "unknown" };

		/// <summary>The breaker squad is held back from the ordinary state machine during the rally (design _06 §2 rally cadence).</summary>
		bool StalemateSkipsUpdate(SquadCA s) => stalemateSquad != null && ReferenceEquals(s, stalemateSquad)
			&& StalemateOwner.stalemateSeat?.Wave is { IsActive: true, Phase: StalemateWavePhaseCA.Rally };

		/// <summary>
		/// GroundStatesCA predicted-loss disengagement sites ask this (design _02 B.1, _06 §2 grid). True only for the live
		/// breaker squad, before arrival, while the last sample's DeferRetreat holds; each bypass is logged as guard_bypass.
		/// </summary>
		internal bool StalemateDefersRetreat(SquadCA squad, string site)
		{
			if (!Info.StalemateBreakerEnabled || stalemateSquad == null || !ReferenceEquals(squad, stalemateSquad))
				return false;
			var seat = StalemateOwner.stalemateSeat;
			if (seat?.Wave == null || !seat.Wave.IsActive || seat.Wave.Arrived || !seat.RetreatDeferred)
				return false;
			DiagnosticSquadEvent(squad, "guard_bypass", reason: site);
			return true;
		}

		/// <summary>Telemetry only (BOTHEALTH H13): the GroundStatesCA predicted-loss retreat site.</summary>
		internal void NoteRetreatStart(SquadCA squad, string site)
		{
			if (!Info.StalemateDiagnosticsEnabled)
				return;
			var owner = DiagnosticOwner;
			owner.diagnosticRetreatTicks.Enqueue(World.WorldTick);

			// IMPL02 A6: the evidence the aggression rule recorded for this squad in this tick (switch ON only; null otherwise).
			DiagnosticSquadEventWith(squad, "retreat_start", site, AggressionEvidenceFor(squad));
		}

		// ======================================================================================
		// Breaker tick
		// ======================================================================================
		void StalemateTick(IBot bot)
		{
			if (!Info.StalemateBreakerEnabled)
				return;

			var owner = StalemateOwner;
			var seat = owner.stalemateSeat ??= new StalemateBreakerSeatCA();
			owner.stalemateSeatOwner ??= new StalemateSeatOwnerCA();
			var t = World.WorldTick;

			if (t % StalemateBreakerWaveCA.SampleInterval == 0)
				StalemateObserve(seat, t);

			if (seat.Wave != null)
			{
				StalemateDriveWave(bot, owner, seat, t);
				return;
			}

			StalemateTryAdmission(bot, owner, seat, t);
		}

		// Seat-legal sightings (denL, den_defence_fresh) and observed CY/production destructions (the no-kill clock).
		void StalemateObserve(StalemateBreakerSeatCA seat, int t)
		{
			var visibleWatch = new SortedDictionary<uint, (int X, int Y)>();
			foreach (var a in World.Actors)
			{
				if (a.IsDead || !a.IsInWorld || a.OccupiesSpace == null || a.Owner == null || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy
					|| !a.CanBeViewedByPlayer(Player))
					continue;

				var building = a.Info.HasTraitInfo<BuildingInfo>();
				var armed = a.Info.HasTraitInfo<AttackBaseInfo>();
				var ownerIndex = Array.IndexOf(World.Players, a.Owner);
				if (armed && !a.Info.HasTraitInfo<HarvesterInfo>())
					seat.Sightings[a.ActorID] = new StalemateSightingCA(ownerIndex, UnitValue(a), t, building, a.Location.X, a.Location.Y);

				if (building && (a.Info.HasTraitInfo<BaseBuildingInfo>() || a.Info.HasTraitInfo<ProductionInfo>()))
				{
					visibleWatch[a.ActorID] = (a.Location.X, a.Location.Y);
					seat.TargetSeen[a.ActorID] = t;
				}
			}

			foreach (var (id, cell) in seat.KillWatch)
			{
				if (visibleWatch.ContainsKey(id))
					continue;
				var actor = World.GetActorById(id);
				var c = new CPos(cell.X, cell.Y);
				if ((actor == null || actor.IsDead) && World.Map.Contains(c) && Player.Shroud.IsVisible(c))
					seat.LastKillTick = t;
			}

			seat.KillWatch.Clear();
			foreach (var kv in visibleWatch)
				seat.KillWatch[kv.Key] = kv.Value;
			seat.PruneSightings(t);
		}

		// ---- eligibility (design _02 B.1) ----
		bool StalemateEligible(Actor a, IBotUnitLeases leases)
		{
			if (a == null || a.IsDead || !a.IsInWorld || a.Owner != Player || unitCannotBeOrdered(a))
				return false;
			var info = a.Info;
			if (!info.HasTraitInfo<AttackBaseInfo>() || !info.HasTraitInfo<MobileInfo>() || info.HasTraitInfo<AircraftInfo>()
				|| info.HasTraitInfo<BuildingInfo>() || info.HasTraitInfo<HarvesterInfo>() || info.HasTraitInfo<CargoInfo>()
				|| info.HasTraitInfo<CapturesInfo>() || info.HasTraitInfo<TransformsInfo>() || IsNavalUnit(a)
				|| Info.ExcludeFromSquadsTypes.Contains(info.Name))
				return false;
			return !BotUnitLeases.IsClaimedByOther(leases, a, LeaseOwner);
		}

		/// <summary>
		/// IMPL01-HOTFIX03 (IMPL02-A): whether a squad is idle in the sense the breaker can draft its members. Rush and Guerrilla squads start
		/// in, and wait in, GroundUnitsIdleStateCA (unchanged). A Harass squad waiting for its launch quorum is quiet. Artillery, Support and
		/// FireSupport squads are quiet only in their idle state (Artillery also once it acts as an assault) with no valid parent assault to escort.
		/// Protection and Stealth squads are never quiet: their idle class acts on the next tick, so it is not a waiting state.
		/// </summary>
		internal static bool StalemateSquadQuiet(SquadCAType type, IState state, bool hasValidParent) => type switch
		{
			SquadCAType.Rush or SquadCAType.Guerrilla => state is GroundUnitsIdleStateCA,
			SquadCAType.Harass => state is HarasserUnitsIdleStateCA or GroundUnitsIdleStateCA,
			SquadCAType.Artillery => !hasValidParent && state is ArtilleryUnitsIdleStateCA or GroundUnitsIdleStateCA,
			SquadCAType.Support => !hasValidParent && state is SupportUnitsIdleStateCA,
			SquadCAType.FireSupport => !hasValidParent && state is FireSupportUnitsIdleStateCA,
			_ => false
		};

		/// <summary>Pool (N0/V0): the idle pool, Rush/Guerrilla members and (HOTFIX03) the members of any other quiet squad, by ActorID. Transferable: idle pool, or a squad still idle.</summary>
		List<(Actor Actor, long Value, SquadCA Source, bool Transferable)> StalematePool(IBotUnitLeases leases)
		{
			var byId = new SortedDictionary<uint, (Actor, long, SquadCA, bool)>();
			foreach (var u in unitsHangingAroundTheBase)
				if (StalemateEligible(u.Actor, leases))
					byId[u.Actor.ActorID] = (u.Actor, UnitValue(u.Actor), null, true);

			foreach (var s in Squads)
			{
				if (ReferenceEquals(s, stalemateSquad))
					continue;
				var quiet = StalemateSquadQuiet(s.Type, s.FuzzyStateMachine?.CurrentState, s.Parent != null && s.Parent.IsValid);
				if (!StalemateBreakerPolicyCA.PoolContributes(s.Type, quiet))
					continue;
				foreach (var u in s.Units)
					if (StalemateEligible(u.Actor, leases) && !byId.ContainsKey(u.Actor.ActorID))
						byId[u.Actor.ActorID] = (u.Actor, UnitValue(u.Actor), s, quiet);
			}

			return byId.Values.ToList();
		}

		/// <summary>Telemetry only: the eligibility function evaluated read-only (design _08 §2 committed share).</summary>
		(int Count, long Value) StalematePoolSize()
		{
			var leases = BotUnitLeases.Of(Player);
			var pool = StalemateEligiblePoolReadOnly(leases);
			return (pool.Count, pool.Sum(p => p.Value));
		}

		// HOTFIX03: deliberately still the Rush/Guerrilla-only pool, so the pool_count/pool_value telemetry (written on both arms by the CE diagnostics) keeps its meaning.
		List<(Actor Actor, long Value)> StalemateEligiblePoolReadOnly(IBotUnitLeases leases)
		{
			var seen = new HashSet<uint>();
			var result = new List<(Actor, long)>();
			foreach (var u in unitsHangingAroundTheBase)
				if (StalemateEligible(u.Actor, leases) && seen.Add(u.Actor.ActorID))
					result.Add((u.Actor, UnitValue(u.Actor)));
			foreach (var s in Squads)
				if (s.Type is SquadCAType.Rush or SquadCAType.Guerrilla)
					foreach (var u in s.Units)
						if (StalemateEligible(u.Actor, leases) && seen.Add(u.Actor.ActorID))
							result.Add((u.Actor, UnitValue(u.Actor)));
			return result;
		}

		// ---- candidates (design _08 §4) ----
		List<StalemateCandidateCA> StalemateCandidates(StalemateBreakerSeatCA seat, int t)
		{
			var list = new List<StalemateCandidateCA>();
			static bool IsTargetType(ActorInfo info) => info.HasTraitInfo<BuildingInfo>()
				&& (info.HasTraitInfo<BaseBuildingInfo>() || info.HasTraitInfo<ProductionInfo>());

			foreach (var a in World.ActorsHavingTrait<Building>())
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy
					|| !IsTargetType(a.Info) || !a.CanBeViewedByPlayer(Player))
					continue;
				var c = World.Map.CellContaining(a.CenterPosition);
				list.Add(new StalemateCandidateCA(a.ActorID, Array.IndexOf(World.Players, a.Owner), true, c.X, c.Y));
			}

			var layer = Player.FrozenActorLayer;
			if (layer != null)
				foreach (var fa in layer.FrozenActorsInRegion(World.Map.AllCells))
				{
					if (!fa.IsValid || !fa.Visible || fa.Hidden || fa.Owner == null
						|| Player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy || !IsTargetType(fa.Info)
						|| !seat.RememberedFresh(fa.ID, t))
						continue;
					var c = World.Map.CellContaining(fa.CenterPosition);
					list.Add(new StalemateCandidateCA(fa.ID, Array.IndexOf(World.Players, fa.Owner), false, c.X, c.Y));
				}

			return StalemateBreakerCA.OrderCandidates(list, (Player.HomeLocation.X, Player.HomeLocation.Y));
		}

		// ---- legal blocker set (design _08 §1, _09 M3; Sonnet D09 L5: the neutral set is separate) ----
		HashSet<CPos> StalemateBuildingBlockers()
		{
			var cells = new HashSet<CPos>();
			foreach (var a in World.ActorsHavingTrait<Building>())
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null)
					continue;
				var rel = Player.RelationshipWith(a.Owner);
				var legal = a.Owner == Player || rel == PlayerRelationship.Ally
					|| (rel == PlayerRelationship.Enemy && a.CanBeViewedByPlayer(Player));
				if (legal)
					foreach (var c in a.Info.TraitInfo<BuildingInfo>().OccupiedTiles(a.Location))
						cells.Add(c);
			}

			var layer = Player.FrozenActorLayer;
			if (layer != null)
				foreach (var fa in layer.FrozenActorsInRegion(World.Map.AllCells))
				{
					if (!fa.IsValid || !fa.Visible || fa.Hidden || fa.Owner == null
						|| Player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy)
						continue;
					var bi = fa.Info.TraitInfoOrDefault<BuildingInfo>();
					if (bi == null)
						continue;
					var topLeft = World.Map.CellContaining(fa.CenterPosition - bi.CenterOffset(World));
					foreach (var c in bi.OccupiedTiles(topLeft))
						cells.Add(c);
				}

			return cells;
		}

		HashSet<CPos> StalemateNeutralBlockers()
		{
			if (stalemateNeutralActors == null)
			{
				stalemateNeutralActors = new Dictionary<uint, CPos[]>();
				foreach (var a in World.Actors)
				{
					if (a.IsDead || !a.IsInWorld || a.Owner == null || a.Owner.Playable || a.OccupiesSpace == null
						|| a.Info.HasTraitInfo<MobileInfo>())
						continue;
					stalemateNeutralActors[a.ActorID] = a.OccupiesSpace.OccupiedCells().Select(c => c.Cell).ToArray();
				}

				stalemateNeutralCells = null;
			}

			var removed = stalemateNeutralActors.Keys.Where(id =>
			{
				var a = World.GetActorById(id);
				return a == null || a.IsDead || !a.IsInWorld;
			}).ToList();
			foreach (var id in removed)
				stalemateNeutralActors.Remove(id);

			if (stalemateNeutralCells == null || removed.Count > 0)
				stalemateNeutralCells = stalemateNeutralActors.Values.SelectMany(c => c).ToHashSet();
			return stalemateNeutralCells;
		}

		sealed class StalemateEngineOracle : IStalemateRouteOracleCA
		{
			readonly SquadManagerBotModuleCA m;
			readonly HashSet<CPos> buildings;
			readonly HashSet<CPos> neutral;
			readonly IPathFinder pathFinder;

			public StalemateEngineOracle(SquadManagerBotModuleCA m, HashSet<CPos> buildings, HashSet<CPos> neutral)
			{
				this.m = m;
				this.buildings = buildings;
				this.neutral = neutral;
				pathFinder = m.World.WorldActor.Trait<IPathFinder>();
			}

			bool Blocked(CPos c) => buildings.Contains(c) || neutral.Contains(c);

			Mobile MobileOf(uint id, out Actor actor)
			{
				actor = m.World.GetActorById(id);
				return actor == null || actor.IsDead || !actor.IsInWorld ? null : actor.TraitOrDefault<Mobile>();
			}

			public (int X, int Y)? ResolveTarget(StalemateGroupCA group, int rawX, int rawY)
			{
				var mobile = MobileOf(group.RepId, out _);
				if (mobile == null)
					return null;
				var map = m.World.Map;
				bool Legal(CPos c) => map.Contains(c) && !Blocked(c)
					&& mobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;
				var raw = new CPos(rawX, rawY);
				var tc = mobile.NearestCell(raw, Legal, 1, 10);
				return Legal(tc) ? (tc.X, tc.Y) : null;
			}

			public bool Connected(StalemateDraftUnitCA member, (int X, int Y) tc)
			{
				var mobile = MobileOf(member.ActorId, out var actor);
				return mobile != null && pathFinder.PathExistsForLocomotor(mobile.Locomotor, actor.Location, new CPos(tc.X, tc.Y));
			}

			public StalemateRouteCA Route(StalemateGroupCA group, (int X, int Y) tc, IReadOnlyList<uint> typeRepresentatives)
			{
				var mobile = MobileOf(group.RepId, out var rep);
				if (mobile == null)
					return new StalemateRouteCA(false, 0, 0);
				var target = new CPos(tc.X, tc.Y);
				List<CPos> cells;
				if (mobile.ToCell == target)
					cells = new List<CPos> { mobile.ToCell };
				else
				{
					var path = m.StalematePath(rep, mobile, target, buildings, neutral);
					if (path == null)
						return new StalemateRouteCA(false, 0, 0);
					cells = path;
				}

				var l = mobile.ToCell == target ? 0 : cells.Count;
				if (l > StalemateBreakerCA.MaxRouteCells)
					return new StalemateRouteCA(true, l, 1);
				var speed = int.MaxValue;
				foreach (var id in typeRepresentatives)
				{
					var typeMobile = MobileOf(id, out _);
					if (typeMobile == null)
						return new StalemateRouteCA(true, l, 0);
					foreach (var c in cells)
						speed = Math.Min(speed, typeMobile.MovementSpeedForCell(c));
				}

				return new StalemateRouteCA(true, l, speed == int.MaxValue ? 0 : Math.Max(0, speed));
			}
		}

		/// <summary>The N2 call (design _08 §1): the engine search with a seat-legal custom cost, trimmed like Move.EvalPath.
		/// Null when the engine returns no path.</summary>
		List<CPos> StalematePath(Actor rep, Mobile mobile, CPos target, HashSet<CPos> buildings, HashSet<CPos> neutral)
		{
			var pathFinder = World.WorldActor.Trait<IPathFinder>();
			var path = pathFinder.FindPathToTargetCell(rep, new[] { mobile.ToCell }, target, BlockedByActor.None,
				customCost: c => buildings.Contains(c) || neutral.Contains(c) ? PathGraph.PathCostForInvalidPath : 0,
				ignoreActor: null, laneBias: true);
			if (path == null || path.Count == 0)
				return null;
			return path.TakeWhile(c => c != mobile.ToCell).ToList();
		}

		int? StalemateRemainingL(StalemateBreakerSeatCA seat, uint actorId)
		{
			var actor = World.GetActorById(actorId);
			var mobile = actor == null || actor.IsDead || !actor.IsInWorld || actor.Owner != Player ? null : actor.TraitOrDefault<Mobile>();
			var group = seat.Groups.FirstOrDefault(g => g.Members.Contains(actorId)) ?? seat.Groups.FirstOrDefault();
			if (mobile == null || group == null)
				return null;
			var tc = new CPos(group.TcX, group.TcY);
			if (mobile.ToCell == tc)
				return 0;
			return StalematePath(actor, mobile, tc, StalemateBuildingBlockers(), StalemateNeutralBlockers())?.Count;
		}

		// ======================================================================================
		// Admission (design _06 §1, _07 M2, _08 §5, _09 M1)
		// ======================================================================================
		void StalemateTryAdmission(IBot bot, SquadManagerBotModuleCA owner, StalemateBreakerSeatCA seat, int t)
		{
			if (t < seat.NextAttemptTick)
				return;

			var rotation = StalemateRotations.GetValue(World, _ => new StalemateRotationCA());
			var seatIndex = StalemateSeatIndex;
			var provider = StalemateUrgency;
			if (provider == null)
			{
				// Fail closed: without the ON urgency there is no homeEmergency predicate, so no attempt is made.
				rotation.Clear(seatIndex);
				seat.NextAttemptTick = t + StalemateBreakerCA.AttemptInterval;
				return;
			}

			string earlyHold = null;
			if (t < seat.CooldownUntil)
				earlyHold = "cooldown";
			else if (StalemateBlocked(provider))
				earlyHold = "home_emergency";

			if (earlyHold != null)
			{
				rotation.Clear(seatIndex);
				seat.NextAttemptTick = t + StalemateBreakerCA.AttemptInterval;
				DiagnosticJobEnd(t, "hold", earlyHold, null, 0, 0, 0, 0, 0, 0, 0, 0);
				return;
			}

			if (!rotation.IsPending(seatIndex))
			{
				rotation.MarkPending(seatIndex, t);
				return;
			}

			if (!rotation.IsWinner(seatIndex, t, World.Players.Length))
				return;

			var deferred = Math.Max(0, t - rotation.PendingSince(seatIndex) - 1);
			rotation.Clear(seatIndex);
			seat.NextAttemptTick = t + StalemateBreakerCA.AttemptInterval;
			StalemateRunAdmission(bot, seat, t, deferred, provider);
		}

		void StalemateRunAdmission(IBot bot, StalemateBreakerSeatCA seat, int t, int deferred, IBotStalemateUrgencyProvider provider)
		{
			var leases = BotUnitLeases.Of(Player);
			var pool = StalematePool(leases);
			var n0 = pool.Count;
			var v0 = pool.Sum(p => p.Value);
			if (n0 > StalemateBreakerCA.MaxDraft)
			{
				DiagnosticJobEnd(t, "hold", "draft_too_large", null, deferred, 0, 0, 0, 0, 0, 0, 0);
				return;
			}

			if (n0 < StalemateBreakerPolicyCA.MinimumUnits || v0 < StalemateBreakerPolicyCA.MinimumValue)
			{
				DiagnosticJobEnd(t, "hold", "pool_too_small", null, deferred, 0, 0, 0, 0, 0, 0, 0);
				return;
			}

			var draft = pool.Where(p => p.Transferable).Select(p =>
			{
				var a = p.Actor;
				return new StalemateDraftUnitCA(a.ActorID, a.Info.TraitInfo<MobileInfo>().Locomotor, a.Info.Name, a.Location.X, a.Location.Y, p.Value);
			}).ToList();
			var groups = StalemateBreakerCA.BuildGroups(draft, out var unitsExcludedGroups, out _, out _);
			if (groups.Count == 0)
			{
				DiagnosticJobEnd(t, "hold", "draft_empty", null, deferred, 0, 0, 0, 0, 0, unitsExcludedGroups, 0);
				return;
			}

			var numL = groups.Sum(g => g.Drafted.Sum(m => m.Value));
			var numT = provider.StalemateOwnArmy + provider.StalemateAlliedArmy;
			var denT = provider.StalemateEnemyArmy;
			var clock = StalemateBreakerPolicyCA.ClockTrigger(true, t, seat.LastKillTick);
			var forceLow = new List<int>();
			var admissible = StalemateBreakerCA.FilterByTrigger(StalemateCandidates(seat, t), id => seat.Exclusion.IsExcluded(id, t), clock,
				ownerIndex => StalemateBreakerPolicyCA.ForceTrigger(true, numL, seat.DenL(ownerIndex, t), numT, denT), forceLow);

			foreach (var ownerIndex in forceLow)
				DiagnosticDispatch(seat, provider, t, -1, ownerIndex, null, 0, 0, numL, numT, denT, clock, "force_low");

			if (admissible.Count == 0)
			{
				seat.NextAttemptTick = Math.Max(seat.NextAttemptTick, t + StalemateBreakerCA.NoCandidateCooldown);
				DiagnosticJobEnd(t, "hold", "no_candidate", new List<BotPassiveCandidateFailure>(), deferred, 0, 0, 0, 0, 0, unitsExcludedGroups, 0);
				return;
			}

			var oracle = new StalemateEngineOracle(this, StalemateBuildingBlockers(), StalemateNeutralBlockers());
			var attempt = StalemateBreakerCA.RunAttempt(admissible, groups, n0, v0, oracle);
			for (var i = 0; i < attempt.CandidatesTried; i++)
			{
				var eval = i < attempt.Failures.Count ? attempt.Failures[i] : attempt.Admitted;
				DiagnosticDispatch(seat, provider, t, i, eval.Candidate.OwnerIndex, eval.Candidate, eval.Candidate.X, eval.Candidate.Y,
					numL, numT, denT, clock, eval.Result);
				if (eval != attempt.Admitted)
					seat.Exclusion.Add(eval.Candidate.ActorId, t, eval.Result);
			}

			var failures = attempt.Failures.Select(f => new BotPassiveCandidateFailure { TargetActor = f.Candidate.ActorId, Reason = f.Result }).ToList();
			if (attempt.Admitted == null)
			{
				seat.NextAttemptTick = Math.Max(seat.NextAttemptTick, t + StalemateBreakerCA.NoCandidateCooldown);
				DiagnosticJobEnd(t, "hold", "no_candidate", failures, deferred, attempt.Paths, attempt.ReachChecks, attempt.CandidatesTried,
					0, draft.Count - groups.Sum(g => g.Drafted.Count), unitsExcludedGroups, 0);
				return;
			}

			var admitted = attempt.Admitted;
			var roster = admitted.Groups.SelectMany(g => g.Reachable).Select(m => World.GetActorById(m.ActorId)).ToList();
			if (leases != null && !StalemateLeasePlanCA.TryClaimAll(leases, roster, LeaseOwner, LeaseHeartbeatTicks(),
					a => StalemateEligible(a, leases), out _))
			{
				DiagnosticJobEnd(t, "hold", "lease_failed", failures, deferred, attempt.Paths, attempt.ReachChecks, attempt.CandidatesTried,
					0, admitted.UnreachableUnits, unitsExcludedGroups, 0);
				return;
			}

			StalemateCommit(bot, seat, t, admitted, pool, n0, v0);
			DiagnosticJobEnd(t, "admit", "none", failures, deferred, attempt.Paths, attempt.ReachChecks, attempt.CandidatesTried,
				admitted.ReachableCount, admitted.UnreachableUnits, unitsExcludedGroups, admitted.ReachableValue);
		}

		void StalemateCommit(IBot bot, StalemateBreakerSeatCA seat, int t, StalemateCandidateEvalCA admitted,
			List<(Actor Actor, long Value, SquadCA Source, bool Transferable)> pool, int n0, long v0)
		{
			var members = admitted.Groups.SelectMany(g => g.Reachable)
				.Select(m => new StalemateWaveMemberCA(m.ActorId, 1, (int)Math.Min(int.MaxValue, Math.Max(0, m.Value)))).ToList();
			var wave = new StalemateBreakerWaveCA(t, members, admitted.Timing, n0, v0, admitted.LeaderL, admitted.LeaderId);
			var owner = StalemateOwner;
			if (!owner.stalemateSeatOwner.TryStart(StalemateManagerName, wave))
				return;

			seat.Wave = wave;
			seat.WaveId = ++seat.WaveCounter;
			seat.TargetId = admitted.Candidate.ActorId;
			seat.TargetOwnerIndex = admitted.Candidate.OwnerIndex;
			seat.TargetX = admitted.Candidate.X;
			seat.TargetY = admitted.Candidate.Y;
			seat.TargetLastHp = -1;
			seat.RetreatDeferred = false;
			seat.WindowDeaths = 0;
			seat.Groups = admitted.Groups.Select(g => new StalemateWaveGroupCA
			{
				Locomotor = g.Group.Locomotor, RepId = g.Group.RepId, TcX = g.Tc.X, TcY = g.Tc.Y, CenX = g.Group.CenX, CenY = g.Group.CenY,
				L = g.L, S = g.S, Members = g.Reachable.Select(m => m.ActorId).OrderBy(id => id).ToList()
			}).ToList();
			var leader = World.GetActorById(admitted.LeaderId);
			seat.RallyX = leader.Location.X;
			seat.RallyY = leader.Location.Y;
			stalemateReportedDead.Clear();

			var memberIds = members.Select(m => m.ActorId).ToHashSet();
			foreach (var p in pool)
			{
				if (!memberIds.Contains(p.Actor.ActorID))
					continue;
				if (p.Source == null)
					unitsHangingAroundTheBase.RemoveAll(u => u.Actor == p.Actor);
				else
				{
					p.Source.Units.RemoveAll(u => u.Actor == p.Actor);
					if (!p.Source.IsValid)
					{
						DismissSquad(p.Source);
						Squads.Remove(p.Source);
					}
				}
			}

			StalemateCreateSquad(bot, seat, memberIds);
			wave.ObserveLeaderCell(t, seat.RallyX, seat.RallyY);
			StalemateIssueRally(bot, seat, force: true);
			DiagnosticWaveStart(seat, t);
		}

		void StalemateCreateSquad(IBot bot, StalemateBreakerSeatCA seat, IEnumerable<uint> memberIds)
		{
			var squad = new SquadCA(bot, this, SquadCAType.Rush, null);
			squad.PriorityTags = PriorityTagsFor(SquadCAType.Rush);
			StalemateSetSquadTarget(squad, seat);
			foreach (var id in memberIds.OrderBy(i => i))
			{
				var a = World.GetActorById(id);
				if (a == null || a.IsDead || !a.IsInWorld || a.Owner != Player)
					continue;
				unitsHangingAroundTheBase.RemoveAll(u => u.Actor == a);
				foreach (var s in Squads)
					s.Units.RemoveAll(u => u.Actor == a);
				squad.Units.Add(new UnitWposWrapper(a));
				if (!activeUnits.Contains(a))
					activeUnits.Add(a);
			}

			Squads.Add(squad);
			stalemateSquad = squad;
			if (seat.Wave.Phase != StalemateWavePhaseCA.Rally)
				squad.FuzzyStateMachine.ChangeState(squad, new GroundUnitsAttackMoveStateCA(), false);
			DiagnosticSquadEvent(squad, "formed", reason: "breaker");
		}

		void StalemateSetSquadTarget(SquadCA squad, StalemateBreakerSeatCA seat)
		{
			var actor = World.GetActorById(seat.TargetId);
			if (actor != null && !actor.IsDead && actor.IsInWorld && actor.CanBeViewedByPlayer(Player))
				squad.Target = Target.FromActor(actor);
			else
			{
				var fa = Player.FrozenActorLayer?.FromID(seat.TargetId);
				if (fa != null && fa.IsValid && fa.Visible && !fa.Hidden)
					squad.Target = Target.FromFrozenActor(fa);
			}
		}

		void StalemateIssueRally(IBot bot, StalemateBreakerSeatCA seat, bool force)
		{
			if (stalemateSquad == null)
				return;
			var rally = new CPos(seat.RallyX, seat.RallyY);
			var minSquared = (long)StalemateBreakerCA.RallyReissueMinCells * StalemateBreakerCA.RallyReissueMinCells;
			foreach (var u in stalemateSquad.Units)
			{
				var a = u.Actor;
				if (a == null || a.IsDead || !a.IsInWorld)
					continue;
				if (!force && (!a.IsIdle || (a.Location - rally).LengthSquared <= minSquared))
					continue;
				bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(World, rally), false));
			}
		}

		// ======================================================================================
		// Wave FSM driver (design _06 §2, _08 §3/N3/N10, _09 LA/LF/LG)
		// ======================================================================================
		(bool Known, bool Destroyed, CPos Cell, int Hp) StalemateTargetStatus(StalemateBreakerSeatCA seat)
		{
			var actor = World.GetActorById(seat.TargetId);
			var raw = new CPos(seat.TargetX, seat.TargetY);
			if (actor != null && !actor.IsDead && actor.IsInWorld && actor.CanBeViewedByPlayer(Player))
			{
				if (actor.Owner == null || Player.RelationshipWith(actor.Owner) != PlayerRelationship.Enemy)
					return (false, false, raw, -1);
				return (true, false, World.Map.CellContaining(actor.CenterPosition), actor.TraitOrDefault<IHealth>()?.HP ?? -1);
			}

			var fa = Player.FrozenActorLayer?.FromID(seat.TargetId);
			if (fa != null && fa.IsValid && fa.Visible && !fa.Hidden && fa.Owner != null
				&& Player.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy)
				return (true, false, raw, -1);

			// Observed destruction: no live visible actor and the target cell is in view.
			var destroyed = (actor == null || actor.IsDead) && World.Map.Contains(raw) && Player.Shroud.IsVisible(raw);
			return (false, destroyed, raw, -1);
		}

		void StalemateDriveWave(IBot bot, SquadManagerBotModuleCA owner, StalemateBreakerSeatCA seat, int t)
		{
			var wave = seat.Wave;
			if (!wave.IsActive)
			{
				StalemateEndWave(bot, owner, seat, t);
				return;
			}

			// IMPL02 L1: this manager's reported-dead set is empty after a load or a personality handoff, so deaths the wave already
			// holds would be re-reported (WindowDeaths++ and a spurious no-progress reset). Derive the set from the wave first.
			// In a straight-through game the set is already a superset of the wave's dead set, so this adds nothing.
			if (stalemateReportedDead.Count < wave.DeadCount)
				foreach (var id in wave.DeadActorIds)
					stalemateReportedDead.Add(id);

			// Personality handoff: the newly enabled manager takes the wave over (design _03 §1 single owner, two-phase).
			if (stalemateSquad == null || !Squads.Contains(stalemateSquad))
			{
				var seatOwner = owner.stalemateSeatOwner;
				if (seatOwner.Wave == null)
					seatOwner.TryStart(StalemateManagerName, wave);
				else if (seatOwner.Manager != StalemateManagerName && seatOwner.TryBeginHandoff(seatOwner.Manager, StalemateManagerName, out var token))
					seatOwner.FinishHandoff(token, StalemateManagerName, true, false);
				var alive = wave.Members.Select(m => m.ActorId).Where(id => !stalemateReportedDead.Contains(id)).ToList();
				StalemateCreateSquad(bot, seat, alive);
			}

			foreach (var m in wave.Members)
			{
				if (stalemateReportedDead.Contains(m.ActorId))
					continue;
				var a = World.GetActorById(m.ActorId);
				if (a == null || a.IsDead)
				{
					stalemateReportedDead.Add(m.ActorId);
					seat.WindowDeaths++;
					wave.RecordDeath(m.ActorId, m.Generation, true);
				}
			}

			var provider = StalemateUrgency;
			var status = StalemateTargetStatus(seat);
			if (wave.IsActive && !status.Known && !status.Destroyed)
				status = StalemateRetarget(seat, t, status);

			wave.Continue(t, provider != null, true, StalemateBlocked(provider), status.Known, status.Destroyed);
			wave.CheckDeadlines(t);

			if (wave.IsActive && wave.Phase == StalemateWavePhaseCA.Rally && t == wave.RallyDeadline)
				StalemateRallyEnd(seat, t);

			if (wave.IsActive && wave.IsSampleTick(t))
				StalemateSample(seat, t, status, provider);

			if (wave.IsActive && wave.Phase == StalemateWavePhaseCA.Rally && t > wave.C
				&& (t - wave.C) % StalemateBreakerCA.RallyReissueInterval == 0)
				StalemateIssueRally(bot, seat, force: false);

			if (!wave.IsActive)
				StalemateEndWave(bot, owner, seat, t);
		}

		void StalemateRallyEnd(StalemateBreakerSeatCA seat, int t)
		{
			var wave = seat.Wave;
			var rally = new CPos(seat.RallyX, seat.RallyY);
			var radius = (long)StalemateBreakerCA.RallyRadiusCells * StalemateBreakerCA.RallyRadiusCells;
			var pathFinder = World.WorldActor.Trait<IPathFinder>();
			var count = 0;
			long value = 0;
			foreach (var m in wave.Members)
			{
				var a = World.GetActorById(m.ActorId);
				var mobile = a == null || a.IsDead || !a.IsInWorld || a.Owner != Player ? null : a.TraitOrDefault<Mobile>();
				var group = seat.Groups.FirstOrDefault(g => g.Members.Contains(m.ActorId));
				if (mobile == null || group == null || (a.Location - rally).LengthSquared > radius
					|| !pathFinder.PathExistsForLocomotor(mobile.Locomotor, a.Location, new CPos(group.TcX, group.TcY)))
					continue;
				count++;
				value += m.InitialCost;
			}

			var pass = wave.RallyEnd(t, count, value);
			DiagnosticRallyEnd(seat, t, count, value, pass);
			if (pass && stalemateSquad != null)
			{
				StalemateSetSquadTarget(stalemateSquad, seat);
				stalemateSquad.FuzzyStateMachine.ChangeState(stalemateSquad, new GroundUnitsAttackMoveStateCA(), false);
			}
		}

		void StalemateSample(StalemateBreakerSeatCA seat, int t, (bool Known, bool Destroyed, CPos Cell, int Hp) status,
			IBotStalemateUrgencyProvider provider)
		{
			var wave = seat.Wave;
			bool Alive(uint id)
			{
				var a = World.GetActorById(id);
				return a != null && !a.IsDead && a.IsInWorld && a.Owner == Player;
			}

			string sampleEvent = null;
			if (!Alive(wave.LeaderId))
			{
				var next = StalemateBreakerCA.NextLeader(seat.Groups.Select(g => (g.RepId, (IReadOnlyList<uint>)g.Members)).ToList(), Alive);
				if (next != 0)
				{
					wave.LeaderChange(t, next, StalemateRemainingL(seat, next) ?? wave.BestL);
					sampleEvent = "leader_change";
				}
			}

			if (!wave.IsActive)
				return;

			var remaining = StalemateRemainingL(seat, wave.LeaderId);
			var arrivalSquared = (long)StalemateBreakerCA.ArrivalRadiusCells * StalemateBreakerCA.ArrivalRadiusCells;
			var arrived = wave.Members.Where(m => Alive(m.ActorId)
					&& (World.GetActorById(m.ActorId).Location - status.Cell).LengthSquared <= arrivalSquared)
				.Select(m => (m.ActorId, m.Generation)).ToList();
			var wasArrived = wave.Arrived;
			wave.ObserveArrival(arrived);
			if (!wasArrived && wave.Arrived && stalemateSquad != null)
				DiagnosticSquadEvent(stalemateSquad, "arrival", reason: "breaker");

			var damaged = seat.WindowDeaths > 0 || (status.Hp >= 0 && seat.TargetLastHp >= 0 && status.Hp < seat.TargetLastHp);
			if (status.Hp >= 0)
				seat.TargetLastHp = status.Hp;
			seat.WindowDeaths = 0;
			wave.ObserveSample(t, remaining, damaged);
			var leader = World.GetActorById(wave.LeaderId);
			if (leader != null && !leader.IsDead && leader.IsInWorld)
				wave.ObserveLeaderCell(t, leader.Location.X, leader.Location.Y);

			seat.RetreatDeferred = wave.IsActive && wave.Phase != StalemateWavePhaseCA.Rally && !wave.Arrived
				&& StalemateBreakerPolicyCA.DeferRetreat(true, provider != null, status.Known, status.Destroyed,
					StalemateBlocked(provider), t, wave.C, wave.LastProgressTick, wave.InitialValue,
					wave.ConfirmedDeadValue, wave.NoProgressDuration, wave.AbsoluteDuration);

			if (sampleEvent != null || (t - wave.C) % StalemateBreakerCA.WaveSampleRecordInterval == 0)
				DiagnosticWaveSample(seat, t, sampleEvent ?? "periodic", remaining);
		}

		(bool Known, bool Destroyed, CPos Cell, int Hp) StalemateRetarget(StalemateBreakerSeatCA seat, int t,
			(bool Known, bool Destroyed, CPos Cell, int Hp) status)
		{
			var wave = seat.Wave;
			if (wave.RetargetUsed)
				return status;

			// One retarget (_02, _06 §1.4): the same procedure with a 1-candidate limit and the original N0/V0. Charged 3 paths;
			// it is not deferred by the world rotation (IMPL01 note).
			var leases = BotUnitLeases.Of(Player);
			var draft = wave.Members.Where(m => !stalemateReportedDead.Contains(m.ActorId)).Select(m => World.GetActorById(m.ActorId))
				.Where(a => a != null && !a.IsDead && a.IsInWorld && a.Owner == Player && a.Info.HasTraitInfo<MobileInfo>())
				.Select(a => new StalemateDraftUnitCA(a.ActorID, a.Info.TraitInfo<MobileInfo>().Locomotor, a.Info.Name, a.Location.X, a.Location.Y, UnitValue(a)))
				.ToList();
			var groups = StalemateBreakerCA.BuildGroups(draft, out _, out _, out _);
			var candidates = StalemateCandidates(seat, t).Where(c => c.ActorId != seat.TargetId && !seat.Exclusion.IsExcluded(c.ActorId, t)).ToList();
			var attempt = groups.Count == 0 || candidates.Count == 0 ? null
				: StalemateBreakerCA.RunAttempt(candidates, groups, wave.N0, wave.V0,
					new StalemateEngineOracle(this, StalemateBuildingBlockers(), StalemateNeutralBlockers()), candidateLimit: 1);
			var admitted = attempt?.Admitted;
			if (admitted == null)
			{
				wave.TryRetarget(t, int.MaxValue, 0);
				return status;
			}

			if (!wave.TryRetarget(t, admitted.Timing.EstimatedTicks, admitted.LeaderL))
				return status;

			seat.TargetId = admitted.Candidate.ActorId;
			seat.TargetOwnerIndex = admitted.Candidate.OwnerIndex;
			seat.TargetX = admitted.Candidate.X;
			seat.TargetY = admitted.Candidate.Y;
			seat.TargetLastHp = -1;
			foreach (var ge in admitted.Groups)
			{
				var g = seat.Groups.FirstOrDefault(x => x.Locomotor == ge.Group.Locomotor);
				if (g != null)
				{
					g.TcX = ge.Tc.X;
					g.TcY = ge.Tc.Y;
				}
			}

			if (stalemateSquad != null)
			{
				StalemateSetSquadTarget(stalemateSquad, seat);
				DiagnosticSquadEvent(stalemateSquad, "retarget", reason: "breaker");
			}

			DiagnosticWaveSample(seat, t, "retarget", admitted.LeaderL);
			return StalemateTargetStatus(seat);
		}

		void StalemateEndWave(IBot bot, SquadManagerBotModuleCA owner, StalemateBreakerSeatCA seat, int t)
		{
			var wave = seat.Wave;
			DiagnosticWaveExit(seat, t);
			owner.stalemateSeatOwner?.ReleaseEndedWave();
			seat.CooldownUntil = t + StalemateBreakerCA.WaveCooldown;

			// rally_failed returns the units to normal assignment (design _08 §3); any other end keeps them as an ordinary squad
			// under the standard predictor, with no exemption.
			if (wave.End == StalemateWaveEndCA.RallyFailed && stalemateSquad != null && Squads.Contains(stalemateSquad))
			{
				var squad = stalemateSquad;
				DismissSquad(squad);
				Squads.Remove(squad);
				foreach (var u in squad.Units)
					activeUnits.Remove(u.Actor);
			}

			stalemateSquad = null;
			stalemateReportedDead.Clear();
			seat.ClearWave();
		}

		// ---- persistence (ON only) ----
		void StalemateSave(List<MiniYamlNode> data)
		{
			if (!Info.StalemateBreakerEnabled)
				return;
			if (this == StalemateOwner && stalemateSeat != null)
				data.Add(new MiniYamlNode("StalemateBreaker", "", stalemateSeat.Save()));
			var index = stalemateSquad == null ? -1 : Squads.IndexOf(stalemateSquad);
			if (index >= 0)
				data.Add(new MiniYamlNode("StalemateSquadIndex", FieldSaver.FormatValue(index)));

			// IMPL02 L2: the neutral blocker cache is built once, lazily, at the first route query; persist it so a loaded game
			// routes over the same cells as a straight-through one (rebuilding from the world would add neutrals that spawned later).
			if (stalemateNeutralActors != null)
				data.Add(new MiniYamlNode("StalemateNeutrals", "", StalemateNeutralCacheCA.Save(stalemateNeutralActors)));
		}

		void StalemateLoad(Dictionary<string, MiniYaml> nodes)
		{
			if (!Info.StalemateBreakerEnabled)
				return;
			if (nodes.TryGetValue("StalemateBreaker", out var breaker))
			{
				stalemateSeat = StalemateBreakerSeatCA.Load(breaker);
				stalemateSeatOwner = new StalemateSeatOwnerCA();
				if (stalemateSeat.Wave != null && stalemateSeat.Wave.IsActive)
					stalemateSeatOwner.TryStart(StalemateManagerName, stalemateSeat.Wave);
			}

			if (nodes.TryGetValue("StalemateSquadIndex", out var indexNode))
			{
				var index = FieldLoader.GetValue<int>("StalemateSquadIndex", indexNode.Value);
				stalemateSquad = index >= 0 && index < Squads.Count ? Squads[index] : null;
			}

			if (nodes.TryGetValue("StalemateNeutrals", out var neutralsNode))
			{
				stalemateNeutralActors = StalemateNeutralCacheCA.Load(neutralsNode);
				stalemateNeutralCells = null;
			}
		}

		// ======================================================================================
		// Telemetry (telemetry flag only; observation only)
		// ======================================================================================
		BotPassiveDiagnosticWriter DiagnosticWriter
		{
			get
			{
				if (!Info.StalemateDiagnosticsEnabled)
					return null;
				var owner = DiagnosticOwner;
				if (!owner.diagnosticInitialized)
				{
					owner.diagnosticInitialized = true;
					var enabled = !World.IsReplay && World.Type == WorldType.Regular;
					var uid = World.LobbyInfo?.GlobalSettings?.GameUid;
					if (string.IsNullOrWhiteSpace(uid))
						uid = "unknown";
					var seat = string.IsNullOrWhiteSpace(Player.InternalName) ? "seat" + StalemateSeatIndex : Player.InternalName;
					string path = null;
					try
					{
						var directory = Path.Combine(Platform.SupportDir, "Logs");
						Directory.CreateDirectory(directory);
						path = Path.Combine(directory, $"cameo-ai-botpassive-{uid}-{seat}-{World.WorldTick}.jsonl");
					}
					catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
					{
						path = null;
					}

					BotPassiveDiagnosticWriter.TryCreate(enabled, path, uid, seat,
						string.IsNullOrWhiteSpace(Player.BotType) ? "unknown" : Player.BotType, out owner.diagnosticWriter);
				}

				return owner.diagnosticWriter is { Enabled: true } w ? w : null;
			}
		}

		string DiagnosticSquadId(SquadCA squad) => squad == null ? null
			: DiagnosticSquadIds.GetValue(squad, _ => (Info.InstanceName ?? "sq") + "-" + (++DiagnosticOwner.diagnosticSquadCounter));

		void DiagnosticSquadEvent(SquadCA squad, string evt, string reason = null) => DiagnosticSquadEventWith(squad, evt, reason, null);

		/// <summary>A10: a squad event whose TargetCell is the given cell (the rally destination) instead of the squad's target; null keeps the squad's target.</summary>
		void DiagnosticSquadEventAt(SquadCA squad, string evt, string reason, CPos? cell) => DiagnosticSquadEventWith(squad, evt, reason, null, cell);

		void DiagnosticSquadEventWith(SquadCA squad, string evt, string reason, AggressionEvidenceCA evidence, CPos? cellOverride = null)
		{
			var w = DiagnosticWriter;
			if (w == null || squad == null)
				return;
			var cell = cellOverride ?? (squad.Target.Type == TargetType.Invalid ? (CPos?)null : World.Map.CellContaining(squad.Target.CenterPosition));
			w.TryEnqueueSquadEvent(new BotPassiveSquadEventSnapshot
			{
				Tick = World.WorldTick, SquadId = DiagnosticSquadId(squad), Generation = 1, Event = evt,
				Size = squad.Units.Count, Value = squad.Units.Sum(u => (long)UnitValue(u.Actor)),
				StateTo = squad.FuzzyStateMachine?.CurrentState?.GetType().Name, TargetType = squad.Target.Type.ToString(),
				TargetCell = cell == null ? null : BotPassiveDiagnosticWriter.Cell(cell.Value.X, cell.Value.Y), Reason = reason,
				LeaseOwner = LeaseOwner,
				PredictedRatioMilli = evidence?.PredictedRatioMilli, CoalitionRatioMilli = evidence?.CoalitionRatioMilli, Band = evidence?.Band,
				TradeDelta = evidence?.TradeDelta, AlliesEngaged = evidence?.AlliesEngaged, OwnFaction = evidence?.OwnFaction, EnemyFaction = evidence?.EnemyFaction
			});
		}

		/// <summary>Ordinary launches are recorded at the end of the tick, when the launch site has added its members.</summary>
		void DiagnosticNoteLaunch(SquadCA squad)
		{
			if (!Info.StalemateDiagnosticsEnabled)
				return;
			(diagnosticPendingCommits ??= new List<SquadCA>()).Add(squad);
		}

		(int Count, long Value) DiagnosticArmy()
		{
			var count = 0;
			long value = 0;
			foreach (var a in World.ActorsHavingTrait<AttackBase>())
			{
				if (a.Owner != Player || a.IsDead || !a.IsInWorld || a.Info.HasTraitInfo<BuildingInfo>() || a.Info.HasTraitInfo<HarvesterInfo>())
					continue;
				count++;
				value += UnitValue(a);
			}

			return (count, value);
		}

		void DiagnosticAttackCommit(SquadCA squad, string kind, long? waveId)
		{
			var w = DiagnosticWriter;
			if (w == null || squad == null)
				return;
			var pool = StalematePoolSize();
			var army = DiagnosticArmy();
			var cell = squad.Target.Type == TargetType.Invalid ? (CPos?)null : World.Map.CellContaining(squad.Target.CenterPosition);
			var targetOwner = squad.Target.Type == TargetType.Actor ? squad.Target.Actor?.Owner
				: squad.Target.Type == TargetType.FrozenActor ? squad.Target.FrozenActor?.Owner : null;
			w.TryEnqueueAttackCommit(new BotPassiveAttackCommitSnapshot
			{
				Tick = World.WorldTick, Kind = kind, SquadId = DiagnosticSquadId(squad), Generation = 1, WaveId = waveId,
				CommittedCount = squad.Units.Count, CommittedValue = squad.Units.Sum(u => (long)UnitValue(u.Actor)),
				PoolCount = pool.Count + (kind == "breaker" ? 0 : squad.Units.Count), PoolValue = pool.Value
					+ (kind == "breaker" ? 0 : squad.Units.Sum(u => (long)UnitValue(u.Actor))),
				ArmyCount = army.Count, ArmyValue = army.Value, TargetSeat = targetOwner?.InternalName,
				TargetCell = cell == null ? null : BotPassiveDiagnosticWriter.Cell(cell.Value.X, cell.Value.Y)
			});
		}

		void DiagnosticDispatch(StalemateBreakerSeatCA seat, IBotStalemateUrgencyProvider provider, int t, int candIndex, int ownerIndex,
			StalemateCandidateCA? candidate, int rawX, int rawY, long numL, long numT, long denT, bool clock, string result)
		{
			var w = DiagnosticWriter;
			if (w == null)
				return;
			var denL = seat.DenL(ownerIndex, t);
			var cl = StalemateBreakerPolicyCA.ClampForceInput;
			w.TryEnqueueDispatchCheck(new BotPassiveDispatchCheckSnapshot
			{
				Tick = t, CandIndex = candIndex,
				TargetSeat = ownerIndex >= 0 && ownerIndex < World.Players.Length ? World.Players[ownerIndex].InternalName : null,
				TargetActor = candidate?.ActorId,
				NumL = cl(numL), DenL = cl(denL), NumT = cl(numT), DenT = cl(denT),
				DenDefenceFresh = candidate == null ? 0 : seat.DenDefenceFresh(ownerIndex, rawX, rawY, t),
				ScoreMilli = StalemateBreakerPolicyCA.ScoreMilli(numL, denL, numT, denT),
				ForceOk = StalemateBreakerPolicyCA.ForceTrigger(true, numL, denL, numT, denT), ClockOk = clock,
				TicksSinceKill = t - Math.Max(StalemateBreakerPolicyCA.StartTick, seat.LastKillTick),
				Result = result, Urgency = UrgencyName(provider.StalemateUrgency), UrgencyLegacy = UrgencyName(provider.StalemateUrgencyLegacy),
				HomeThreat = provider.StalemateHomeThreat, HomeLatch = provider.StalemateHomeLatch
			});
		}

		void DiagnosticJobEnd(int t, string result, string holdReason, List<BotPassiveCandidateFailure> failures, int deferred, int paths,
			int reachChecks, int tried, int admitted, int unreachable, int excludedGroups, long valueAdmitted)
		{
			DiagnosticWriter?.TryEnqueueJobEnd(new BotPassiveJobEndSnapshot
			{
				Tick = t, Result = result, HoldReason = holdReason, CandidateFailures = failures ?? new List<BotPassiveCandidateFailure>(),
				DeferredTicks = deferred, Paths = paths, ReachChecks = reachChecks, CandidatesTried = tried, UnitsAdmitted = admitted,
				UnitsUnreachable = unreachable, UnitsExcludedGroups = excludedGroups, ValueAdmitted = valueAdmitted
			});
		}

		void DiagnosticWaveStart(StalemateBreakerSeatCA seat, int t)
		{
			var w = DiagnosticWriter;
			if (w == null)
				return;
			var wave = seat.Wave;
			w.TryEnqueueWaveStart(new BotPassiveWaveStartSnapshot
			{
				Tick = t, WaveId = seat.WaveId, C = wave.C, E = wave.E,
				Groups = seat.Groups.Select(g => new BotPassiveWaveGroup
				{
					Locomotor = g.Locomotor, RepActor = g.RepId, L = g.L, S = g.S,
					Tc = BotPassiveDiagnosticWriter.Cell(g.TcX, g.TcY), Cen = BotPassiveDiagnosticWriter.Cell(g.CenX, g.CenY)
				}).ToList(),
				RallyDuration = wave.RallyDuration, NoProgressDuration = wave.NoProgressDuration, AbsoluteDuration = wave.AbsoluteDuration,
				RallyDeadline = wave.RallyDeadline, AbsoluteDeadline = wave.AbsoluteDeadline, ArmTick = wave.ArmTick,
				N0 = wave.N0, V0 = wave.V0, CommittedCount = wave.InitialCount, CommittedValue = wave.InitialValue,
				LeaderActor = wave.LeaderId,
				TargetSeat = seat.TargetOwnerIndex >= 0 && seat.TargetOwnerIndex < World.Players.Length ? World.Players[seat.TargetOwnerIndex].InternalName : null,
				TargetActor = seat.TargetId, TargetCell = BotPassiveDiagnosticWriter.Cell(seat.TargetX, seat.TargetY)
			});
			DiagnosticAttackCommit(stalemateSquad, "breaker", seat.WaveId);
		}

		void DiagnosticWaveSample(StalemateBreakerSeatCA seat, int t, string evt, int? curL)
		{
			var w = DiagnosticWriter;
			if (w == null)
				return;
			var wave = seat.Wave;
			var alive = wave.Members.Where(m => !stalemateReportedDead.Contains(m.ActorId)).ToList();
			w.TryEnqueueWaveSample(new BotPassiveWaveSampleSnapshot
			{
				Tick = t, WaveId = seat.WaveId, Event = evt, LastProgressTick = wave.LastProgressTick, BestL = wave.BestL, CurL = curL,
				Arrived = wave.Arrived, UnitsAlive = alive.Count, ValueAlive = alive.Sum(m => (long)m.InitialCost),
				DeadCost = wave.DeadCost, LeaderActor = wave.LeaderId
			});
		}

		void DiagnosticRallyEnd(StalemateBreakerSeatCA seat, int t, int count, long value, bool pass)
		{
			DiagnosticWriter?.TryEnqueueRallyEnd(new BotPassiveRallyEndSnapshot
			{
				Tick = t, WaveId = seat.WaveId, AssembledCount = count, AssembledValue = value, N0 = seat.Wave.N0, V0 = seat.Wave.V0, Pass = pass
			});
		}

		void DiagnosticWaveExit(StalemateBreakerSeatCA seat, int t)
		{
			var w = DiagnosticWriter;
			if (w == null)
				return;
			var wave = seat.Wave;
			w.TryEnqueueWaveExit(new BotPassiveWaveExitSnapshot
			{
				Tick = t, WaveId = seat.WaveId, ExitReason = StalemateBreakerWaveCA.WireName(wave.End),
				LastProgressTick = wave.LastProgressTick, BestL = wave.BestL,
				UnitsAlive = wave.Members.Count(m => !stalemateReportedDead.Contains(m.ActorId)), ValueLost = wave.ConfirmedDeadValue,
				Duration = t - wave.C, RouteEstL = wave.RouteEstL, RouteActualCells = wave.RouteActualCells, RouteBestL = wave.RouteBestL
			});
		}

		/// <summary>End of every BotTick: pending ordinary attack_commit rows, unit_position, health, and the terminal manifest.</summary>
		void DiagnosticTick()
		{
			if (!Info.StalemateDiagnosticsEnabled)
				return;
			var w = DiagnosticWriter;
			var t = World.WorldTick;
			if (diagnosticPendingCommits != null)
			{
				foreach (var s in diagnosticPendingCommits)
					if (s.IsValid && !ReferenceEquals(s, stalemateSquad))
					{
						DiagnosticSquadEvent(s, "formed", reason: "ordinary");
						DiagnosticAttackCommit(s, "ordinary", null);
					}

				diagnosticPendingCommits.Clear();
			}

			if (w == null)
				return;

			if (World.IsGameOver || Player.WinState != WinState.Undefined)
			{
				w.Complete(t);
				return;
			}

			if (t % BotPassiveDiagnosticWriter.PositionIntervalTicks == 0 && t <= BotPassiveDiagnosticWriter.MaximumPositionTick)
				DiagnosticUnitPositions(w, t);

			if (t % StalemateBreakerCA.HealthInterval == 0)
				DiagnosticHealth(w, t);
		}

		IEnumerable<Actor> DiagnosticCombatUnits() => World.ActorsHavingTrait<AttackBase>()
			.Where(a => a.Owner == Player && !a.IsDead && a.IsInWorld && a.OccupiesSpace != null && !a.Info.HasTraitInfo<BuildingInfo>() && !a.Info.HasTraitInfo<HarvesterInfo>())
			.Distinct().OrderBy(a => a.ActorID);

		void DiagnosticUnitPositions(BotPassiveDiagnosticWriter w, int t)
		{
			var squadOf = new Dictionary<Actor, SquadCA>();
			foreach (var s in Squads)
				foreach (var u in s.Units)
					if (u.Actor != null)
						squadOf.TryAdd(u.Actor, s);
			var roster = DiagnosticCombatUnits().Select(a => new BotPassiveUnitPositionSnapshot
			{
				ActorId = a.ActorID, ActorType = a.Info.Name, X = a.Location.X, Y = a.Location.Y,
				SquadId = squadOf.TryGetValue(a, out var s) ? DiagnosticSquadId(s) : null,
				Activity = a.CurrentActivity?.GetType().Name ?? "idle", Live = !a.IsDead, InWorld = a.IsInWorld
			}).ToList();
			var checksum = roster.Aggregate(17L, (h, u) => unchecked(h * 31 + u.ActorId)).ToString(System.Globalization.CultureInfo.InvariantCulture);

			// IMPL02 L6f: a roster above the chunk cap is truncated, but expected_count keeps the full count so emitted_count < expected_count shows it.
			var expectedCount = roster.Count;
			if (roster.Count > BotPassiveDiagnosticWriter.MaximumPositionChunkEntries * BotPassiveDiagnosticWriter.MaximumPositionChunks)
				roster = roster.Take(BotPassiveDiagnosticWriter.MaximumPositionChunkEntries * BotPassiveDiagnosticWriter.MaximumPositionChunks).ToList();
			w.TryEnqueueUnitPosition(t, roster, expectedCount, checksum);
		}

		void DiagnosticHealth(BotPassiveDiagnosticWriter w, int t)
		{
			var owner = DiagnosticOwner;
			var leases = BotUnitLeases.Of(Player);
			var inSquad = Squads.SelectMany(s => s.Units).Select(u => u.Actor).Where(a => a != null).ToHashSet();
			var units = DiagnosticCombatUnits().ToList();
			if (owner.diagnosticHomeThirdSquared < 0)
			{
				var home = Player.HomeLocation;
				var nearest = World.Players.Where(p => p != Player && p.Playable && Player.RelationshipWith(p) == PlayerRelationship.Enemy)
					.Select(p => (long)(p.HomeLocation - home).LengthSquared).DefaultIfEmpty(0).Min();
				owner.diagnosticHomeThirdSquared = nearest / 9;
			}

			var homeThird = units.Count(a => (long)(a.Location - Player.HomeLocation).LengthSquared <= owner.diagnosticHomeThirdSquared);
			var idle = units.Count(a => a.IsIdle && !inSquad.Contains(a) && (leases == null || leases.LeaseOf(a) == null));
			var stuck = owner.diagnosticStuck.Sample(Squads
				.Where(s => s.IsValid && s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA or GroundUnitsAttackState)
				.Where(s => s.Units[0].Actor?.OccupiesSpace != null)
				.Select(s => (s, s.Units[0].Actor.Location.X, s.Units[0].Actor.Location.Y)));

			while (owner.diagnosticRetreatTicks.Count > 0 && t - owner.diagnosticRetreatTicks.Peek() > 1500)
				owner.diagnosticRetreatTicks.Dequeue();
			var stats = Player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			var kills = stats == null ? 0 : stats.UnitsKilled + stats.BuildingsKilled;
			owner.diagnosticKillSamples.Enqueue((t, kills));
			while (owner.diagnosticKillSamples.Count > 0 && t - owner.diagnosticKillSamples.Peek().Tick > 1500)
				owner.diagnosticKillSamples.Dequeue();
			var killsWindow = kills - owner.diagnosticKillSamples.Peek().Kills;

			var provider = StalemateUrgency;
			w.TryEnqueueHealth(new BotPassiveHealthSnapshot
			{
				Tick = t, IdleCombatUnits = idle, UnsquaddedCombatUnits = units.Count(a => !inSquad.Contains(a)),
				HomeThirdUnits = homeThird, CombatUnits = units.Count, StuckSquads = stuck,
				RetreatsWindow = owner.diagnosticRetreatTicks.Count, KillsWindow = killsWindow,
				Urgency = provider == null ? "unknown" : UrgencyName(provider.StalemateUrgency),
				UrgencyLegacy = provider == null ? "unknown" : UrgencyName(provider.StalemateUrgencyLegacy)
			});
		}

		void DiagnosticComplete()
		{
			if (Info.StalemateDiagnosticsEnabled && this == DiagnosticOwner)
				diagnosticWriter?.Complete(World.WorldTick);
		}
	}
}
