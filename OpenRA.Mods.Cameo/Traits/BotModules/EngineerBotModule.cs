#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// Merged from CA CaptureManagerBotModuleCA (Cameo's copy of CAmod's, itself from OpenRA.Mods.AS) and AS
	// CncEngineerManagerBotModule (engine d5d8b2a685) under DESIGN §19.3 (one module per decision). AI_MASTER_PLAN ENG.
	//   from CA: capture — a priority target by chance (nearest to the base first), otherwise the most valuable
	//            capturable of a random enemy/neutral player, nearest per engineer; SafePath routes the engineer
	//            around enemy fire.
	//   from AS: bridge-hut repair and instant building repair (nearest path-reachable target, one engineer per
	//            evaluation, the repair jobs taking turns); the stuck check (moving, not moved since the last sample).
	//            DESIGN §19.5 lets the whole module see through fog (maintainer 2026-09-30); both visibility switches
	//            remain, so a fog-honest engineer owner is one yaml line each.
	//   new:     the ONE owner of every engineer (review P0b, "IsIdle is not ownership"): an LC1 claim before every
	//            order, renewed while the engineer works and released when it is idle again; its own in-flight set,
	//            so the capture and repair passes never order one engineer twice in a tick (the lease lets the same
	//            owner re-claim, so it cannot catch that); a stuck engineer is stopped, released and retried after
	//            StuckRetryTicks (the AS module benched it for the rest of the match); an optional visibility check
	//            for bridge huts (the AS module had none).
	[TraitLocation(SystemActors.Player)]
	[Desc("The ONE owner of the bot's engineers (DESIGN §19.3): replaces CaptureManagerBotModuleCA + CncEngineerBotModule,",
		"which ordered the same idle engineers side by side. classic keeps running the CA copy alone.")]
	public class EngineerBotModuleInfo : ConditionalTraitInfo
	{
		// The three id lists below: empty = read from CaptureManagerBotModuleCA on the same Player actor (classic's copy,
		// defined whatever its condition), so the ids live in ai.yaml once (audit_central_ids is lower-only).
		[Desc("Actor types that capture (via `Captures`). Empty: CaptureManagerBotModuleCA's list on this actor.")]
		public readonly HashSet<string> CapturingActorTypes = [];

		[Desc("Percentage chance of trying a priority capture.")]
		public readonly int PriorityCaptureChance = 75;

		[Desc("Actor types captured first, nearest to the base first. Empty: CaptureManagerBotModuleCA's list.")]
		public readonly HashSet<string> PriorityCapturableActorTypes = [];

		[Desc("Actor types that may be captured; empty on both modules includes all actors. Empty: CaptureManagerBotModuleCA's list.")]
		public readonly HashSet<string> CapturableActorTypes = [];

		[Desc("Avoid enemy actors this close to the path when routing a capturer. Near the maximum weapon range.")]
		public readonly WDist EnemyAvoidanceRadius = WDist.FromCells(8);

		[Desc("Ticks between capture evaluations.")]
		public readonly int MinimumCaptureDelay = 375;

		[Desc("Most capture targets considered per evaluation (at least 1).")]
		public readonly int MaximumCaptureTargetOptions = 10;

		[Desc("Consider visibility (shroud, fog, cloak) when choosing capture targets. DESIGN §19.5 allows false for this module.")]
		public readonly bool CheckCaptureTargetsForVisibility = true;

		[Desc("Player relationships whose actors capturers target.")]
		public readonly PlayerRelationship CapturableRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("Actor types that repair: bridge huts (via `RepairsBridges`) and buildings (via `InstantlyRepairs`).")]
		public readonly HashSet<string> RepairingActorTypes = [];

		[Desc("Allied building types an engineer repairs instantly (via `InstantlyRepairable`). Empty disables it.")]
		public readonly HashSet<string> RepairableActorTypes = [];

		[Desc("Repair an allied building at this damage state or worse.")]
		public readonly DamageState RepairableDamageState = DamageState.Heavy;

		[Desc("Bridge hut types an engineer repairs once their bridge is down. Empty disables it.")]
		public readonly HashSet<string> RepairableHutActorTypes = [];

		[Desc("Ticks between repair evaluations; also the stuck-check sample interval.")]
		public readonly int AssignRoleDelay = 120;

		[Desc("Only repair bridge huts the player can see. DESIGN §19.5 allows false for this module.")]
		public readonly bool CheckRepairTargetsForVisibility = true;

		[Desc("LC1 lease length in ticks, renewed at every evaluation while the engineer works.")]
		public readonly int LeaseTicks = 3000;

		[Desc("Ticks after an order before an idle engineer counts as done (orders reach the unit a few ticks late).")]
		public readonly int OrderGraceTicks = 50;

		[Desc("Ticks a stuck engineer is left alone before it may be ordered again.")]
		public readonly int StuckRetryTicks = 1500;

		[Desc("Most engineers sent at one capture target at a time (one mission, one live attempt). 0 = no limit,",
			"the parents' behaviour and the default until its A/B: a smoke match sent three engineers at one oil derrick",
			"within 400 ticks; the candidate sets 1 (AI_MASTER_PLAN §1.2 step 6).")]
		public readonly int MaxEngineersPerTarget = 0;

		[Desc("A capture mission whose attempts fail this many times in a row (the engineer died) goes dormant: its target",
			"is skipped for CaptureDormantTicks (fransotto's dormant shelf; AI_MISSION_CARDS §2.2). 0 disables. A smoke match",
			"lost five engineers one after another at one defended derrick. Off until its A/B; the candidate sets 2.")]
		public readonly int CaptureFailuresBeforeDormant = 0;

		[Desc("Ticks a dormant capture mission rests before its target may be tried again.")]
		public readonly int CaptureDormantTicks = 3000;

		[Desc("Escort as ONE mission (maintainer 2026-09-30): a TECH building (neutral, or a PriorityCapturableActorTypes",
			"entry) defended by enemy armed units (within EnemyAvoidanceRadius) is not attempted solo; a building in the",
			"enemy base is never escorted — the engineer sneaks in alone (SafePath), an escort would give it away. The mission is PUBLISHED and a protection request is raised at the",
			"target (IBotProtectionRequestProvider, the squad manager's escort seam — it needs UseProtectionRequests); the",
			"engineer goes once our armed value there reaches EscortSuperiority percent of the defenders'. Off until its A/B.")]
		public readonly bool EscortDefendedCaptures = false;

		[Desc("Percent of the defenders' value our armed units near the target must reach before the engineer goes.")]
		public readonly int EscortSuperiority = 100;

		[Desc("The engineer also waits until the defenders' value near the target has fallen to this percent of their value",
			"when the mission was published: the escort must have thinned them out, not merely arrived (#693's first A/B",
			"smoke: the escort arrived, the engineer went into the firefight and died twice). 100 = no thinning required.")]
		public readonly int EscortThinnedPercent = 100;

		[Desc("Ticks an escorted capture waits for its escort before the mission is DENIED (no_units) and goes dormant.")]
		public readonly int EscortWaitTicks = 3000;

		[Desc("Ticks a published protection request stays valid; it is re-published while the mission lives.")]
		public readonly int EscortRequestTicks = 250;

		public override object Create(ActorInitializer init) { return new EngineerBotModule(init.Self, this); }
	}

	public enum EngineerJob { Capture, RepairBridge, RepairBuilding }

	public enum EngineerCheck { Working, Done, Stuck, Gone }

	public class EngineerBotModule : ConditionalTrait<EngineerBotModuleInfo>, IBotTick, IBotPositionsUpdated, IGameSaveTraitData,
		IBotProtectionRequestProvider
	{
		const string LeaseOwner = nameof(EngineerBotModule);

		sealed class Assignment
		{
			public EngineerJob Job;
			public int OrderedTick;
			public int SampleTick;
			public WPos SamplePos;
			public Actor Target;
			public string MissionId;
			public int Attempt;
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly int maximumCaptureTargetOptions;
		readonly EngineerJob[] repairJobs;
		readonly HashSet<string> capturingTypes;
		readonly HashSet<string> priorityCapturableTypes;
		readonly HashSet<string> capturableTypes;
		readonly Dictionary<Actor, Assignment> assigned = [];
		readonly Dictionary<Actor, int> stuckUntil = [];
		readonly Dictionary<string, int> missionAttempts = [];
		readonly Dictionary<string, int> missionFailStreak = [];
		readonly Dictionary<string, int> dormantUntil = [];

		// The one escorted capture in progress: the mission waits (no attempt) until the escort holds the target area.
		sealed class EscortPlan
		{
			public Actor Target;
			public string MissionId;
			public int SinceTick;
			public int DefenceValue;
			public int InitialDefenceValue;
			public bool Committed;
		}

		EscortPlan escort;

		// The squad manager serves a protection request only at or above its PrepositionMinThreatValue: a request valued at
		// a few riflemen (the first flag-on match published 440-1000) is dropped in silence. Read the bar from the squad
		// managers on this player (Info-level, fixed for the match) instead of copying the number.
		int escortRequestFloor = -1;

		int EscortRequestFloor() =>
			escortRequestFloor >= 0 ? escortRequestFloor
				: escortRequestFloor = player.PlayerActor.Info.TraitInfos<SquadManagerBotModuleCAInfo>()
					.Select(i => i.PrepositionMinThreatValue).DefaultIfEmpty(0).Max();

		/// <summary>The request value, free of world state so it can be tested: the defenders' value, never below the bar
		/// the squad manager serves requests at.</summary>
		public static int EscortRequestValue(int defenceValue, int floor) => Math.Max(Math.Max(1, defenceValue), floor);

		int captureTicks;
		int repairTicks;
		int nextRepairJob;
		int housekeptTick = -1;
		CPos initialBaseCenter;

		// Telemetry (debug.log). SkippedClaimed counts evaluations that left an idle engineer alone because another
		// module holds it — each is a chance for the order pair that used to collide.
		public int CaptureOrders { get; private set; }
		public int BridgeOrders { get; private set; }
		public int BuildingRepairOrders { get; private set; }
		public int StuckStops { get; private set; }
		public int SkippedClaimed { get; private set; }

		public EngineerBotModule(Actor self, EngineerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			maximumCaptureTargetOptions = Math.Max(1, info.MaximumCaptureTargetOptions);

			var shared = self.Info.TraitInfos<CaptureManagerBotModuleCAInfo>().FirstOrDefault();
			capturingTypes = info.CapturingActorTypes.Count > 0 || shared == null ? info.CapturingActorTypes : shared.CapturingActorTypes;
			priorityCapturableTypes = info.PriorityCapturableActorTypes.Count > 0 || shared == null ? info.PriorityCapturableActorTypes : shared.PriorityCapturableActorTypes;
			capturableTypes = info.CapturableActorTypes.Count > 0 || shared == null ? info.CapturableActorTypes : shared.CapturableActorTypes;

			var jobs = new List<EngineerJob>();
			if (info.RepairableHutActorTypes.Count > 0)
				jobs.Add(EngineerJob.RepairBridge);
			if (info.RepairableActorTypes.Count > 0)
				jobs.Add(EngineerJob.RepairBuilding);
			repairJobs = jobs.ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating on the same tick (both parents do this).
			captureTicks = world.LocalRandom.Next(Info.MinimumCaptureDelay);
			repairTicks = world.LocalRandom.Next(Info.AssignRoleDelay);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation) { initialBaseCenter = newLocation; }

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		/// <summary>What happened to an engineer this module ordered. Free of world state so it can be tested.</summary>
		public static EngineerCheck Check(bool gone, bool idle, bool moving, bool movedSinceSample,
			int ticksSinceOrder, int ticksSinceSample, int graceTicks, int sampleTicks)
		{
			if (gone)
				return EngineerCheck.Gone;

			// A queued order reaches the unit a few ticks late: an engineer idle inside the grace is still on its way.
			if (idle)
				return ticksSinceOrder >= graceTicks ? EngineerCheck.Done : EngineerCheck.Working;

			// The AS stuck rule: moving, yet at the same spot a whole sample interval later.
			if (moving && !movedSinceSample && ticksSinceSample >= sampleTicks && ticksSinceOrder >= graceTicks)
				return EngineerCheck.Stuck;

			return EngineerCheck.Working;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (player.WinState != WinState.Undefined)
				return;

			if (--repairTicks <= 0)
			{
				repairTicks = Info.AssignRoleDelay;
				if (repairJobs.Length > 0)
				{
					Housekeep(bot);
					var job = repairJobs[nextRepairJob];
					nextRepairJob = (nextRepairJob + 1) % repairJobs.Length;
					if (job == EngineerJob.RepairBridge)
						QueueRepairBridgeOrder(bot);
					else
						QueueRepairBuildingOrder(bot);
				}
			}

			if (--captureTicks <= 0)
			{
				captureTicks = Info.MinimumCaptureDelay;
				Housekeep(bot);
				QueueCaptureOrders(bot);
			}
		}

		bool IsGone(Actor a) => a.IsDead || !a.IsInWorld || a.Owner != player;

		/// <summary>Renew the leases of working engineers, release finished ones, stop stuck ones. Once per tick.</summary>
		void Housekeep(IBot bot)
		{
			var tick = world.WorldTick;
			if (housekeptTick == tick)
				return;

			housekeptTick = tick;
			HousekeepEscort(tick);
			var leases = BotUnitLeases.Of(player);
			foreach (var (a, job) in assigned.ToList())
			{
				var moving = !IsGone(a) && a.CurrentActivity?.ChildActivity?.ActivityType == ActivityType.Move;
				var check = Check(IsGone(a), !IsGone(a) && a.IsIdle, moving, IsGone(a) || a.CenterPosition != job.SamplePos,
					tick - job.OrderedTick, tick - job.SampleTick, Info.OrderGraceTicks, Info.AssignRoleDelay);

				switch (check)
				{
					case EngineerCheck.Gone:
						assigned.Remove(a);
						EndMission(a, job, check);
						break;
					case EngineerCheck.Done:
						assigned.Remove(a);
						leases?.Release(a, LeaseOwner);
						EndMission(a, job, check);
						break;
					case EngineerCheck.Stuck:
						assigned.Remove(a);
						EndMission(a, job, check);
						leases?.Release(a, LeaseOwner);
						stuckUntil[a] = tick + Info.StuckRetryTicks;
						StuckStops++;
						bot.QueueOrder(new Order("Stop", a, false));
						Log.Write("debug", $"AI ({player.ClientIndex}): ENG stuck {a.Info.Name} {a.ActorID} on {job.Job}, stopped and released (tick {tick})");
						break;
					default:
						BotUnitLeases.TryClaim(leases, a, LeaseOwner, PurposeOf(job.Job), Info.LeaseTicks);
						if (tick - job.SampleTick >= Info.AssignRoleDelay)
						{
							job.SampleTick = tick;
							job.SamplePos = a.CenterPosition;
						}

						break;
				}
			}

			foreach (var a in stuckUntil.Where(kv => kv.Value <= tick || IsGone(kv.Key)).Select(kv => kv.Key).ToList())
				stuckUntil.Remove(a);
		}

		static BotLeasePurpose PurposeOf(EngineerJob job) => job == EngineerJob.Capture ? BotLeasePurpose.Capture : BotLeasePurpose.Engineer;

		/// <summary>An engineer this module may order now: ours, idle, not already on a job, not benched, not held by another module.</summary>
		bool Available(Actor a, IBotUnitLeases leases, bool allowFlyIdle)
		{
			if (IsGone(a) || assigned.ContainsKey(a) || stuckUntil.ContainsKey(a))
				return false;

			if (!(a.IsIdle || (allowFlyIdle && a.CurrentActivity is FlyIdle)))
				return false;

			if (BotUnitLeases.IsClaimedByOther(leases, a, LeaseOwner))
			{
				SkippedClaimed++;
				return false;
			}

			return true;
		}

		bool Assign(IBot bot, IBotUnitLeases leases, Actor engineer, EngineerJob job, Order order, Actor target)
		{
			if (!BotUnitLeases.TryClaim(leases, engineer, LeaseOwner, PurposeOf(job), Info.LeaseTicks))
				return false;

			var tick = world.WorldTick;
			var assignment = new Assignment { Job = job, OrderedTick = tick, SampleTick = tick, SamplePos = engineer.CenterPosition, Target = target };
			assigned[engineer] = assignment;
			bot.QueueOrder(order);
			if (job == EngineerJob.Capture)
			{
				StartMission(assignment, engineer);
				if (escort != null && escort.Target == target)
					escort.Committed = true;
			}

			switch (job)
			{
				case EngineerJob.Capture: CaptureOrders++; break;
				case EngineerJob.RepairBridge: BridgeOrders++; break;
				default: BuildingRepairOrders++; break;
			}

			Log.Write("debug", $"AI ({player.ClientIndex}): ENG {job} {engineer.Info.Name} {engineer.ActorID} -> {target.Info.Name} {target.ActorID} (tick {tick}; captures {CaptureOrders}, bridges {BridgeOrders}, repairs {BuildingRepairOrders}, stuck {StuckStops}, skipped-claimed {SkippedClaimed})");
			return true;
		}

		void QueueCaptureOrders(IBot bot)
		{
			if (capturingTypes.Count == 0)
				return;

			var leases = BotUnitLeases.Of(player);
			var capturers = world.ActorsHavingTrait<Captures>()
				.Where(a => a.Owner == player && capturingTypes.Contains(a.Info.Name.ToLowerInvariant()) && Available(a, leases, false))
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null)
				.ToList();

			if (capturers.Count == 0)
				return;

			var next = 0;
			var baseCenter = world.Map.CenterOfCell(initialBaseCenter);

			if (world.LocalRandom.Next(100) < Info.PriorityCaptureChance)
			{
				var priorityTargets = world.Actors.Where(a =>
					!a.IsDead && a.IsInWorld && Info.CapturableRelationships.HasRelationship(player.RelationshipWith(a.Owner))
					&& priorityCapturableTypes.Contains(a.Info.Name.ToLowerInvariant()));

				// DESIGN §19.5: the engineer owner's fog exception.
				if (Info.CheckCaptureTargetsForVisibility)
					priorityTargets = priorityTargets.Where(a => a.CanBeViewedByPlayer(player));

				var candidates = priorityTargets.Where(t => !TargetFull(t) && !Dormant(t)).OrderBy(a => (a.CenterPosition - baseCenter).LengthSquared).ToList();
				ConsiderEscort(candidates);
				var ordered = candidates.Where(t => !BlockedByEscort(t)).ToList();

				// As the CA parent: each attempt uses up a target; a capturer is used up only when it is sent.
				var attempts = Math.Min(capturers.Count, ordered.Count);
				for (var i = 0; i < attempts && next < capturers.Count; i++)
				{
					var capturer = capturers[next];
					var target = ordered[i];
					var captureManager = target.TraitOrDefault<CaptureManager>();
					if (captureManager != null && capturer.Trait.CanTarget(captureManager) && SafePath(capturer.Actor, target).Type != TargetType.Invalid
						&& Assign(bot, leases, capturer.Actor, EngineerJob.Capture, new Order("CaptureActor", capturer.Actor, Target.FromActor(target), true), target))
						next++;
				}

				if (next >= capturers.Count)
					return;
			}

			var randPlayers = world.Players.Where(p => !p.Spectating
				&& Info.CapturableRelationships.HasRelationship(player.RelationshipWith(p))).ToList();
			if (randPlayers.Count == 0)
				return;

			var randPlayer = randPlayers.Random(world.LocalRandom);
			var remaining = capturers.Skip(next).ToList();

			var options = world.Actors.Where(a => a.Owner == randPlayer && !a.IsDead && a.IsInWorld);
			if (Info.CheckCaptureTargetsForVisibility)
				options = options.Where(a => a.CanBeViewedByPlayer(player));

			var capturable = options
				.Where(target =>
				{
					var captureManager = target.TraitOrDefault<CaptureManager>();
					return captureManager != null && remaining.Any(tp => tp.Trait.CanTarget(captureManager));
				})
				.OrderByDescending(target => target.GetSellValue())
				.Take(maximumCaptureTargetOptions);

			if (capturableTypes.Count > 0)
				capturable = capturable.Where(target => capturableTypes.Contains(target.Info.Name.ToLowerInvariant()));

			var targets = capturable.ToList();
			if (targets.Count == 0)
				return;

			foreach (var capturer in remaining)
			{
				ConsiderEscort(targets.Where(t => !TargetFull(t) && !Dormant(t)).OrderByDescending(t => t.GetSellValue()));
				var target = targets.Where(t => !TargetFull(t) && !Dormant(t) && !BlockedByEscort(t)).MinByOrDefault(t => (t.CenterPosition - capturer.Actor.CenterPosition).LengthSquared);
				if (target == null || SafePath(capturer.Actor, target).Type == TargetType.Invalid)
					continue;

				Assign(bot, leases, capturer.Actor, EngineerJob.Capture, new Order("CaptureActor", capturer.Actor, Target.FromActor(target), true), target);
			}
		}

		// CA parent verbatim. The enemy scan along the path has no visibility check: DESIGN §19.5's engineer exception
		// (engineers route around the army).
		Target SafePath(Actor capturer, Actor target)
		{
			var mobile = capturer.TraitOrDefault<Mobile>();
			if (mobile == null)
				return Target.Invalid;

			var locomotor = mobile.Locomotor;
			if (!mobile.PathFinder.PathExistsForLocomotor(locomotor, capturer.Location, target.Location))
				return Target.Invalid;

			var path = mobile.PathFinder.FindPathToTargetCellByPredicate(
				capturer, new[] { capturer.Location }, loc => true, BlockedByActor.Stationary,
				loc => world.FindActorsInCircle(world.Map.CenterOfCell(loc), Info.EnemyAvoidanceRadius)
					.Where(u => !u.IsDead && capturer.Owner.RelationshipWith(u.Owner) == PlayerRelationship.Enemy && capturer.IsTargetableBy(u))
					.Sum(u => Math.Max(WDist.Zero.Length, Info.EnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(loc) - u.CenterPosition).Length)));

			return path.Count == 0 ? Target.Invalid : Target.FromActor(target);
		}

		void QueueRepairBridgeOrder(IBot bot)
		{
			if (Info.RepairingActorTypes.Count == 0)
				return;

			var leases = BotUnitLeases.Of(player);
			var repairers = world.ActorsHavingTrait<RepairsBridges>()
				.Where(a => a.Owner == player && Info.RepairingActorTypes.Contains(a.Info.Name) && Available(a, leases, true))
				.ToList();

			if (repairers.Count == 0)
				return;

			// Few bridge huts per map, so a list is fine (AS parent).
			var huts = world.ActorsWithTrait<BridgeHut>()
				.Where(at => Info.RepairableHutActorTypes.Contains(at.Actor.Info.Name) && at.Trait.BridgeDamageState >= DamageState.Dead)
				.Select(at => at.Actor).ToList();
			huts.AddRange(world.ActorsWithTrait<LegacyBridgeHut>()
				.Where(at => Info.RepairableHutActorTypes.Contains(at.Actor.Info.Name) && at.Trait.BridgeDamageState >= DamageState.Dead)
				.Select(at => at.Actor));

			if (Info.CheckRepairTargetsForVisibility)
				huts.RemoveAll(h => !h.CanBeViewedByPlayer(player));

			SendOneRepairer(bot, leases, repairers, huts, EngineerJob.RepairBridge, "RepairBridge");
		}

		void QueueRepairBuildingOrder(IBot bot)
		{
			if (Info.RepairingActorTypes.Count == 0)
				return;

			var leases = BotUnitLeases.Of(player);
			var repairers = world.ActorsHavingTrait<InstantlyRepairs>()
				.Where(a => a.Owner == player && Info.RepairingActorTypes.Contains(a.Info.Name) && Available(a, leases, true))
				.ToList();

			if (repairers.Count == 0)
				return;

			// Allied buildings only: the bot knows its own and its allies' base, no fog involved.
			var targets = world.ActorsHavingTrait<InstantlyRepairable>().Where(t =>
			{
				if (!Info.RepairableActorTypes.Contains(t.Info.Name) || t.Owner.RelationshipWith(player) != PlayerRelationship.Ally)
					return false;

				var health = t.TraitOrDefault<IHealth>();
				return health != null && health.DamageState >= Info.RepairableDamageState;
			}).ToList();

			SendOneRepairer(bot, leases, repairers, targets, EngineerJob.RepairBuilding, "InstantRepair");
		}

		/// <summary>AS parent: the first repairer that reaches its nearest target goes; one per evaluation.</summary>
		void SendOneRepairer(IBot bot, IBotUnitLeases leases, List<Actor> repairers, List<Actor> targets, EngineerJob job, string orderString)
		{
			if (targets.Count == 0)
				return;

			foreach (var r in repairers)
			{
				foreach (var target in targets.OrderBy(a => (r.Location - a.Location).LengthSquared))
				{
					if (!AIUtils.PathExist(r, target.Location, target))
						continue;

					if (Assign(bot, leases, r, job, new Order(orderString, r, Target.FromActor(target), true), target))
						return;

					break;
				}
			}
		}

		/// <summary>The escort rule, free of world state so it can be tested: an undefended target needs no escort;
		/// otherwise our armed value near it must reach `superiorityPct` percent of the defenders'.</summary>
		public static bool EscortReady(int ownValue, int defenceValue, int superiorityPct) =>
			defenceValue <= 0 || (long)ownValue * 100 >= (long)defenceValue * superiorityPct;

		/// <summary>Superiority AND thinning: the defenders must also have fallen to `thinnedPct` percent of their value
		/// at publish. Free of world state so it can be tested.</summary>
		public static bool EscortReady(int ownValue, int defenceValue, int initialDefenceValue, int superiorityPct, int thinnedPct) =>
			defenceValue <= 0 || (EscortReady(ownValue, defenceValue, superiorityPct)
				&& (long)defenceValue * 100 <= (long)Math.Max(defenceValue, initialDefenceValue) * thinnedPct);

		static int CostOf(Actor a) => a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

		// DESIGN §19.5: the engineer owner is omniscient as a whole (maintainer 2026-09-30) — these scans see through fog.
		int DefenceValue(Actor target) =>
			world.FindActorsInCircle(target.CenterPosition, Info.EnemyAvoidanceRadius)
				.Where(u => !u.IsDead && u.IsInWorld && player.RelationshipWith(u.Owner) == PlayerRelationship.Enemy
					&& u.Info.HasTraitInfo<AttackBaseInfo>())
				.Sum(CostOf);

		int OwnArmedValueNear(Actor target) =>
			world.FindActorsInCircle(target.CenterPosition, Info.EnemyAvoidanceRadius)
				.Where(u => !u.IsDead && u.IsInWorld && u.Owner == player && u.Info.HasTraitInfo<AttackBaseInfo>()
					&& !u.Info.HasTraitInfo<BuildingInfo>())
				.Sum(CostOf);

		/// <summary>
		/// Maintainer 2026-09-30: escorts are ONLY for tech buildings (neutral, or a PriorityCapturableActorTypes
		/// entry) in an unsafe area. A building in the enemy's base is taken by stealth — an escort would give the
		/// engineer away — so it is never escorted: the engineer sneaks in alone along SafePath.
		/// </summary>
		public static bool EscortEligible(bool enemyOwned, bool priorityType) => priorityType || !enemyOwned;

		bool EscortEligible(Actor target) =>
			EscortEligible(player.RelationshipWith(target.Owner) == PlayerRelationship.Enemy,
				priorityCapturableTypes.Contains(target.Info.Name.ToLowerInvariant()));

		/// <summary>True when a defended tech target may not be attempted yet: escorts on, defenders present, and no
		/// ready escort plan for exactly this target. Enemy-base buildings are never blocked (stealth).</summary>
		bool BlockedByEscort(Actor target)
		{
			if (!Info.EscortDefendedCaptures || !EscortEligible(target))
				return false;

			var defence = DefenceValue(target);
			if (defence <= 0)
				return false;

			if (escort == null || escort.Target != target)
				return true;

			escort.DefenceValue = defence;
			return !EscortReady(OwnArmedValueNear(target), defence, escort.InitialDefenceValue, Info.EscortSuperiority, Info.EscortThinnedPercent);
		}

		/// <summary>Opens the one escort plan for the first defended candidate (the candidates arrive best first).</summary>
		void ConsiderEscort(IEnumerable<Actor> candidates)
		{
			if (!Info.EscortDefendedCaptures || escort != null)
				return;

			foreach (var t in candidates)
			{
				if (!EscortEligible(t))
					continue;

				var defence = DefenceValue(t);
				if (defence <= 0)
					continue;

				escort = new EscortPlan
				{
					Target = t, MissionId = CaptureMissionId(t.Info.Name, t.ActorID), SinceTick = world.WorldTick, DefenceValue = defence,
					InitialDefenceValue = defence
				};

				BotMissionLog.Write(new BotMissionRecord
				{
					Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Published,
					Executor = "Engineers", MissionType = "capture", TargetCell = t.Location, Value = defence
				});
				return;
			}
		}

		/// <summary>Ends a plan whose target is gone or ours, and denies one whose escort never came.</summary>
		void HousekeepEscort(int tick)
		{
			if (escort == null || escort.Committed)
				return;

			var t = escort.Target;
			if (t.IsDead || !t.IsInWorld || t.Owner == player)
			{
				BotMissionLog.Write(new BotMissionRecord
				{
					Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Denied, Reason = BotMissionReasons.TargetGone,
					Executor = "Engineers", MissionType = "capture"
				});
				escort = null;
				return;
			}

			if (tick - escort.SinceTick < Info.EscortWaitTicks)
				return;

			// No escort in time: no execution attempt ever existed — mission feedback, then the shelf.
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Denied, Reason = BotMissionReasons.NoUnits,
				Executor = "Engineers", MissionType = "capture", TargetCell = t.Location, Value = escort.DefenceValue
			});
			dormantUntil[escort.MissionId] = tick + Info.CaptureDormantTicks;
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = escort.MissionId, Event = BotMissionEvent.Dormant, Reason = BotMissionReasons.NoUnits,
				Executor = "Engineers", MissionType = "capture", TargetCell = t.Location
			});
			escort = null;
		}

		/// <summary>The escort seam: one standing guard request at the escorted target while its mission lives.</summary>
		IReadOnlyList<BotProtectionRequest> IBotProtectionRequestProvider.ProtectionRequests =>
			escort == null || escort.Target.IsDead || !escort.Target.IsInWorld
				? []
				: [new BotProtectionRequest(escort.Target.Location, EscortRequestValue(escort.DefenceValue, EscortRequestFloor()), world.WorldTick + Info.EscortRequestTicks)];

		/// <summary>A target already carrying `max` live capture attempts takes no more (max ≤ 0: no limit).</summary>
		public static bool TargetFull(int liveAttempts, int max) => max > 0 && liveAttempts >= max;

		bool TargetFull(Actor target) =>
			TargetFull(assigned.Values.Count(j => j.Job == EngineerJob.Capture && j.Target == target), Info.MaxEngineersPerTarget);

		// MC1 (docs/design/AI_MISSION_CARDS.md): every capture is a mission card. The mission is the building (its
		// strategic reason survives a lost engineer, and a change of owner); each engineer sent at it is one attempt.
		public static string CaptureMissionId(string actorType, uint actorId) => $"capture:{actorType}:{actorId}";

		/// <summary>The dormant shelf's trigger, free of world state so it can be tested.</summary>
		public static bool GoesDormant(int failStreak, int threshold) => threshold > 0 && failStreak >= threshold;

		bool Dormant(Actor target) =>
			dormantUntil.TryGetValue(CaptureMissionId(target.Info.Name, target.ActorID), out var until) && world.WorldTick < until;

		/// <summary>How a capture attempt ended, free of world state so it can be tested. Order matters: an engineer
		/// consumed by a successful capture is also "dead" (Actor.IsDead includes Disposed).</summary>
		public static (BotMissionAttemptState State, string Reason) CaptureVerdict(bool targetOurs, bool stuck, bool engineerDead, bool targetGone)
		{
			if (targetOurs)
				return (BotMissionAttemptState.Success, BotMissionReasons.Done);

			if (stuck)
				return (BotMissionAttemptState.Released, BotMissionReasons.Stuck);

			if (engineerDead)
				return (BotMissionAttemptState.Failed, BotMissionReasons.LostUnits);

			if (targetGone)
				return (BotMissionAttemptState.Released, BotMissionReasons.TargetGone);

			return (BotMissionAttemptState.Released, BotMissionReasons.Dropped);
		}

		void StartMission(Assignment job, Actor engineer)
		{
			var target = job.Target;
			job.MissionId = CaptureMissionId(target.Info.Name, target.ActorID);

			// A card leaving the shelf is its own event, before the attempt that follows it.
			if (dormantUntil.Remove(job.MissionId))
				BotMissionLog.Write(new BotMissionRecord
				{
					Player = player, MissionId = job.MissionId, Event = BotMissionEvent.Reopened, Reason = BotMissionReasons.Timeout,
					Executor = "Engineers", MissionType = "capture", TargetCell = target.Location
				});

			job.Attempt = missionAttempts.GetValueOrDefault(job.MissionId) + 1;
			missionAttempts[job.MissionId] = job.Attempt;
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = job.MissionId, Attempt = job.Attempt, State = BotMissionAttemptState.Committed,
				Executor = "Engineers", MissionType = "capture", TargetCell = target.Location, Units = 1
			});
		}

		void EndMission(Actor engineer, Assignment job, EngineerCheck check)
		{
			if (job.MissionId == null)
				return;

			// The escorted attempt is over either way: the escort's job ends with it.
			if (escort != null && escort.MissionId == job.MissionId)
				escort = null;

			var target = job.Target;
			var targetGone = target == null || target.IsDead || !target.IsInWorld;
			var (state, reason) = CaptureVerdict(!targetGone && target.Owner == player, check == EngineerCheck.Stuck,
				engineer.IsDead, targetGone);
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player, MissionId = job.MissionId, Attempt = job.Attempt, State = state, Reason = reason,
				Executor = "Engineers", MissionType = "capture", TargetCell = target?.Location, Units = 1
			});

			// The dormant shelf: a success clears the streak; consecutive losses rest the mission.
			if (state == BotMissionAttemptState.Success)
				missionFailStreak.Remove(job.MissionId);
			else if (state == BotMissionAttemptState.Failed)
			{
				var streak = missionFailStreak.GetValueOrDefault(job.MissionId) + 1;
				missionFailStreak[job.MissionId] = streak;
				if (GoesDormant(streak, Info.CaptureFailuresBeforeDormant))
				{
					missionFailStreak.Remove(job.MissionId);
					dormantUntil[job.MissionId] = world.WorldTick + Info.CaptureDormantTicks;
					BotMissionLog.Write(new BotMissionRecord
					{
						Player = player, MissionId = job.MissionId, Event = BotMissionEvent.Dormant, Reason = BotMissionReasons.Outmatched,
						Executor = "Engineers", MissionType = "capture", TargetCell = target?.Location
					});
				}
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return [new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter))];
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();
			if (nodes.TryGetValue("InitialBaseCenter", out var node))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", node.Value);
		}
	}
}
