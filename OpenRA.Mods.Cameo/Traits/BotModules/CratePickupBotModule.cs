#region Copyright & License Information
/*
 * Copyright 2021-2025 The OpenHV Developers (see AUTHORS)
 * This file is part of OpenHV, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Put this on the Player actor. Manages crate collection.")]
	public class CratePickupBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that should not start hunting for Crates.")]
		public readonly FrozenSet<string> ExcludedUnitTypes = FrozenSet<string>.Empty;

		[Desc("Only these actor types should start hunting for Crates.")]
		public readonly FrozenSet<string> IncludedUnitTypes = FrozenSet<string>.Empty;

		[Desc("Interval (in ticks) between giving out orders to idle units.")]
		public readonly int ScanForCratesInterval = 50;

		[Desc("Only move this far away from base. Disabled if set to zero.")]
		public readonly int MaxProximityRadius = 0;

		[Desc("Avoid enemy actors nearby when searching for Crates. Should be somewhere near the max weapon range.")]
		public readonly WDist EnemyAvoidanceRadius = WDist.FromCells(8);

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for Crates?")]
		public readonly bool CheckTargetsForVisibility = true;

		[Desc("FOG-ENGCRATE-01: genericbot crates are those seen now or remembered in the frozen layer, whatever CheckTargetsForVisibility says. Gated by SwitchCondition. Row fog_engcrate.")]
		public readonly bool FogEngCrate = false;

		[Desc("LC2 (AI_REVIEW_FRANSOTTO P1b): a crate stays reserved for its collector at most this many ticks.",
			"-1 = never released (the old behaviour: a crate whose collector died or gave up stayed reserved forever).")]
		public readonly int ReservationTimeoutTicks = 1500;

		[Desc("LC2: the reservation is released when its collector has been idle this long after the order",
			"(the Move lost to another module, or the collector gave up).")]
		public readonly int CollectorIdleGraceTicks = 100;

		[Desc("CRATE-RESERVE-01: the genericbot switch for the reserve rule below (classic keeps the old crate pickup).")]
		public readonly BooleanExpression SwitchCondition = null;

		[Desc("CRATE-RESERVE-01 (USER rule): crates within ReserveYardRadiusCells of any own or allied construction yard are left alone while the bot has a yard or an MCV; other crates go to a",
			"cheap nearby idle unit away from known enemy threat; with no yard and no MCV every crate is hunted by all idle units, safest first. Gated by SwitchCondition; row crate_reserve_mcv.")]
		public readonly bool CrateReserveMcv = false;

		[Desc("CRATE-RESERVE-01: reserve radius around each own or allied construction yard, in cells.")]
		public readonly int ReserveYardRadiusCells = 25;

		[Desc("CRATE-RESERVE-01: scan interval while the bot has no yard and no MCV and is hunting every crate.")]
		public readonly int HuntScanInterval = 50;

		[Desc("CRATE-RESERVE-01: the collector is the cheapest of this many nearest idle units.")]
		public readonly int CollectorCandidates = 3;

		[Desc("CRATE-RESERVE-01: an escorting unit that falls farther than this many cells behind the MCV is ordered back to it.")]
		public readonly int EscortRadiusCells = 4;

		[Desc("CRATE-RESERVE-01: spare hunters search unexplored cells on a grid of this many cells, each to a different one.")]
		public readonly int ExploreGridCells = 10;

		[Desc("CRATE-RESERVE-01: how often the crate memory is refreshed from what the bot sees.")]
		public readonly int CrateMemoryScanTicks = 50;

		[Desc("CRATE-RESERVE-01: a remembered crate that has not been seen again is forgotten after this many ticks.")]
		public readonly int CrateMemoryTicks = 9000;

		public override object Create(ActorInitializer init) { return new CratePickupBotModule(init.Self, this); }
	}

	public class CratePickupBotModule : ConditionalTrait<CratePickupBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly OpenRA.Player player;
		readonly int maxProximity;

		CrateSpawner crateSpawner;

		BotCooldown scanForCratesCooldown;

		// CRATE-RESERVE-01
		bool switchesActive;
		AblationLatch ablateReserve;
		AblationLatch ablateFogEngCrate; // FOG-ENGCRATE-01 hook fog_engcrate
		bool FogOn => Info.FogEngCrate && switchesActive && !ablateFogEngCrate.Off(player, AblationIds.FogEngCrate);
		readonly HashSet<CPos> reservedCrateCells = [];
		bool ReserveOn => Info.CrateReserveMcv && switchesActive && !ablateReserve.Off(player, AblationIds.CrateReserveMcv);

		// LC2: crate -> (collector, tick ordered). Released by ReservationStale; the old List<Actor> was never cleared.
		readonly Dictionary<Actor, (Actor Collector, int Tick)> reservations = [];

		public CratePickupBotModule(Actor self, CratePickupBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			maxProximity = Info.MaxProximityRadius > 0 ? info.MaxProximityRadius : world.Map.Grid.MaximumTileSearchRange;
			switchesActive = info.SwitchCondition == null;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.SwitchCondition != null)
				yield return new VariableObserver(SwitchConditionChanged, Info.SwitchCondition.Variables);
		}

		void SwitchConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			switchesActive = Info.SwitchCondition.Evaluate(conditions);
		}

		protected override void Created(Actor self)
		{
			crateSpawner = self.Owner.World.WorldActor.TraitOrDefault<CrateSpawner>();
		}

		protected override void TraitEnabled(Actor self)
		{
			scanForCratesCooldown.Start(world.WorldTick, Info.ScanForCratesInterval);
		}

		void IBotTick.BotTick(IBot bot)
		{
			var clock = BotVisitClock.Of(bot);
			if (crateSpawner == null || !crateSpawner.IsTraitEnabled())
			{
				scanForCratesCooldown.Freeze(clock.Elapsed);
				return;
			}

			if (ReserveOn)
				UpdateCrateMemory();

			if (!scanForCratesCooldown.Ready(clock.VisitTick))
				return;

			if (ReserveOn && TryReservedScan(bot, clock))
				return;

			scanForCratesCooldown.Start(clock.VisitTick, Info.ScanForCratesInterval);

			ReleaseStaleReservations();

			var crates = world.ActorsHavingTrait<Crate>().ToList();
			if (crates.Count < 1)
				return;

			if (FogOn || Info.CheckTargetsForVisibility)
				crates.RemoveAll(c => !(FogOn ? FogEngCrateGate.Admits(player, c) : c.CanBeViewedByPlayer(player)));

			// LC1: an idle unit may still belong to a scout, a beacon response or a capture — never take a claimed one.
			var leases = BotUnitLeases.Of(player);
			var idleUnits = world.ActorsHavingTrait<Mobile>().Where(a => a.Owner == player && a.IsInWorld && a.IsIdle
				&& (Info.IncludedUnitTypes.Contains(a.Info.Name) || (Info.IncludedUnitTypes.Count < 1 && !Info.ExcludedUnitTypes.Contains(a.Info.Name)))
				&& !BotUnitLeases.IsClaimedByOther(leases, a, LeaseOwner)).ToList();

			if (idleUnits.Count < 1)
				return;

			foreach (var crate in crates)
			{
				if (reservations.ContainsKey(crate))
					continue;

				if (!crate.IsAtGroundLevel())
					continue;

				var crateCollector = idleUnits.ClosestToIgnoringPath(crate);
				if (crateCollector == null)
					continue;

				if ((crate.Location - crateCollector.Location).Length > maxProximity)
					continue;

				idleUnits.Remove(crateCollector);

				var target = PathToNextCrate(crateCollector, crate);
				if (target.Type == TargetType.Invalid)
					continue;

				if (!BotUnitLeases.TryClaim(leases, crateCollector, LeaseOwner, BotLeasePurpose.Crate,
					Math.Max(0, Info.ReservationTimeoutTicks)))
					continue;

				var cell = world.Map.CellContaining(target.CenterPosition);
				OpenRA.Mods.CA.AIUtils.BotDebug($"{bot.Player}: Ordering {crateCollector} to {cell} for Crate pick up.");
				bot.QueueOrder(new Order("Move", crateCollector, target, true));
				reservations[crate] = (crateCollector, world.WorldTick);
			}
		}

		const string LeaseOwner = nameof(CratePickupBotModule);

		// CRATE-RESERVE-01: the crate memory. A crate is remembered when the bot player sees it (shared allied vision is part of the player's shroud) and forgotten when its cell is seen
		// again without a crate (picked up by anyone) or after CrateMemoryTicks. Every mode of the reserve rule works from this set only: the bot plays fair: only crates it can see enter the memory, a hidden or fogged crate is never used.
		readonly Dictionary<CPos, int> crateMemory = [];
		readonly Dictionary<CPos, (Actor Collector, int Tick)> cellReservations = [];
		int nextMemoryTick;

		void UpdateCrateMemory()
		{
			var now = world.WorldTick;
			if (now < nextMemoryTick)
				return;

			nextMemoryTick = now + Math.Max(1, Info.CrateMemoryScanTicks);
			var present = new HashSet<CPos>();
			foreach (var crate in world.ActorsHavingTrait<Crate>())
			{
				if (!crate.IsInWorld || !crate.IsAtGroundLevel())
					continue;

				// Only a crate the bot (or an ally, through the shared shroud) can see counts: a hidden or fogged crate changes nothing.
				if (!crate.CanBeViewedByPlayer(player))
					continue;

				present.Add(crate.Location);
				crateMemory[crate.Location] = now;
			}

			foreach (var (cell, seen) in crateMemory.ToList())
				if ((player.Shroud.IsVisible(cell) && !present.Contains(cell)) || now - seen > Info.CrateMemoryTicks)
					crateMemory.Remove(cell);
		}

		void ReleaseStaleCellReservations()
		{
			if (cellReservations.Count == 0)
				return;

			var tick = world.WorldTick;
			var leases = BotUnitLeases.Of(player);
			foreach (var cell in cellReservations.Keys.ToList())
			{
				var (collector, ordered) = cellReservations[cell];
				var collectorGone = collector.IsDead || !collector.IsInWorld || collector.Owner != player;
				if (ReservationStale(!crateMemory.ContainsKey(cell), collectorGone, !collectorGone && collector.IsIdle,
					tick - ordered, Info.CollectorIdleGraceTicks, Info.ReservationTimeoutTicks))
				{
					cellReservations.Remove(cell);
					leases?.Release(collector, LeaseOwner);
				}
			}
		}

		// CRATE-RESERVE-01: the reserve-aware scan. False = no MCV module here, the old scan runs.
		bool TryReservedScan(IBot bot, (int VisitTick, int Elapsed, bool InVisit) clock)
		{
			var mcvInfo = player.PlayerActor.TraitsImplementing<McvExpansionManagerBotModule>().FirstEnabledTraitOrDefault()?.Info;
			if (mcvInfo == null || mcvInfo.ConstructionYardTypes.Count == 0)
				return false;

			var mcvTypes = mcvInfo.ConstructionMcvTypes.Count > 0 ? mcvInfo.ConstructionMcvTypes : mcvInfo.McvTypes;
			var yardCells = new List<CPos>();
			var ownYards = 0;
			foreach (var a in world.ActorsHavingTrait<Building>())
			{
				if (a.IsDead || !mcvInfo.ConstructionYardTypes.Contains(a.Info.Name))
					continue;

				if (a.Owner == player)
					ownYards++;
				else if (player.RelationshipWith(a.Owner) != PlayerRelationship.Ally)
					continue;

				yardCells.Add(a.Location);
			}

			var ownMcvs = world.ActorsHavingTrait<Transforms>().Where(a => a.Owner == player && !a.IsDead && a.IsInWorld && mcvTypes.Contains(a.Info.Name)).ToList();
			var mode = CrateReserveCA.ModeFor(ownYards, ownMcvs.Count);
			var needMcv = mode == CrateMode.Hunt;

			scanForCratesCooldown.Start(clock.VisitTick, mode != CrateMode.Normal ? Math.Min(Info.ScanForCratesInterval, Info.HuntScanInterval) : Info.ScanForCratesInterval);
			ReleaseStaleCellReservations();
			reservedCrateCells.Clear();
			var leases = BotUnitLeases.Of(player);

			// The moment an MCV exists the hunt stops: every unit falls back and escorts it until it deploys.
			if (mode == CrateMode.Escort)
			{
				EscortMcv(bot, leases, ownMcvs[0], mcvTypes);
				return true;
			}

			var candidates = new List<(CPos Cell, bool Threatened)>();
			foreach (var cell in crateMemory.Keys)
			{
				var reserved = yardCells.Any(y => CrateReserveCA.Reserved((cell - y).Length, FogEngCrateGate.ClampReserveRadius(Info.ReserveYardRadiusCells)));
				if (reserved)
					reservedCrateCells.Add(cell);

				var threatened = world.FindActorsInCircle(world.Map.CenterOfCell(cell), Info.EnemyAvoidanceRadius)
					.Any(u => !u.IsDead && u.CanBeViewedByPlayer(player) && player.RelationshipWith(u.Owner) == PlayerRelationship.Enemy && u.Info.HasTraitInfo<AttackBaseInfo>());
				if (!cellReservations.ContainsKey(cell) && CrateReserveCA.Eligible(needMcv, reserved, threatened))
					candidates.Add((cell, threatened));
			}

			var idleUnits = EligibleUnits(leases).Where(a => a.IsIdle).ToList();
			if (idleUnits.Count == 0)
				return true;

			if (!needMcv)
			{
				// One cheap collector per remembered crate, nearest first; reserved and threatened crates never reach here.
				foreach (var (cell, _) in candidates.OrderBy(c => idleUnits.Count == 0 ? 0 : idleUnits.Min(u => (u.Location - c.Cell).LengthSquared)))
				{
					if (idleUnits.Count == 0)
						break;

					var collector = CheapCollector(idleUnits, cell);
					if (collector != null && (cell - collector.Location).Length <= maxProximity)
					{
						idleUnits.Remove(collector);
						SendToCrate(bot, leases, collector, cell);
					}
				}

				return true;
			}

			// No yard and no MCV: ONE unit per remembered crate, distinct crates, the nearest unit to each (cheapest first on ties); safe crates before threatened ones.
			var units = idleUnits.OrderBy(UnitCost).ToList();
			foreach (var group in new[] { candidates.Where(c => !c.Threatened).ToList(), candidates.Where(c => c.Threatened).ToList() })
			{
				if (units.Count == 0 || group.Count == 0)
					continue;

				var pairs = CrateReserveCA.AssignDistinct(units.Select(u => (u.Location.X, u.Location.Y)).ToList(), group.Select(c => (c.Cell.X, c.Cell.Y)).ToList());
				var used = new HashSet<Actor>();
				foreach (var (ui, ti) in pairs)
				{
					if ((group[ti].Cell - units[ui].Location).Length > maxProximity)
						continue;

					if (SendToCrate(bot, leases, units[ui], group[ti].Cell))
						used.Add(units[ui]);
				}

				units.RemoveAll(u => used.Contains(u));
			}

			// Spare units search unexplored (then fogged) areas for more crates, each to a different place.
			if (units.Count > 0)
			{
				var cells = SearchCells(units.Count);
				var pairs = CrateReserveCA.AssignDistinct(units.Select(u => (u.Location.X, u.Location.Y)).ToList(), cells.Select(c => (c.X, c.Y)).ToList());
				foreach (var (ui, ti) in pairs)
					if (BotUnitLeases.TryClaim(leases, units[ui], LeaseOwner, BotLeasePurpose.Crate, Math.Max(0, Info.ReservationTimeoutTicks)))
						bot.QueueOrder(new Order("Move", units[ui], Target.FromCell(world, cells[ti]), false));
			}

			return true;
		}

		IEnumerable<Actor> EligibleUnits(IBotUnitLeases leases) =>
			world.ActorsHavingTrait<Mobile>().Where(a => a.Owner == player && a.IsInWorld && !a.IsDead
				&& (Info.IncludedUnitTypes.Contains(a.Info.Name) || (Info.IncludedUnitTypes.Count < 1 && !Info.ExcludedUnitTypes.Contains(a.Info.Name)))
				&& !BotUnitLeases.IsClaimedByOther(leases, a, LeaseOwner));

		static int UnitCost(Actor u) => u.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? int.MaxValue;

		bool SendToCrate(IBot bot, IBotUnitLeases leases, Actor collector, CPos cell)
		{
			var target = PathToCell(collector, cell);
			if (target.Type == TargetType.Invalid)
				return false;

			if (!BotUnitLeases.TryClaim(leases, collector, LeaseOwner, BotLeasePurpose.Crate, Math.Max(0, Info.ReservationTimeoutTicks)))
				return false;

			OpenRA.Mods.CA.AIUtils.BotDebug($"{bot.Player}: Ordering {collector} to remembered crate {cell} (reserve rule).");
			bot.QueueOrder(new Order("Move", collector, target, true));
			cellReservations[cell] = (collector, world.WorldTick);
			return true;
		}

		// Search cells on a coarse grid for the spare hunters (each unit gets a different one): cells never explored first, fogged ones when those do not suffice.
		List<CPos> SearchCells(int wanted)
		{
			var unexplored = new List<CPos>();
			var fogged = new List<CPos>();
			var step = Math.Max(2, Info.ExploreGridCells);
			var map = world.Map;
			for (var y = map.AllCells.TopLeft.Y; y < map.AllCells.BottomRight.Y; y += step)
				for (var x = map.AllCells.TopLeft.X; x < map.AllCells.BottomRight.X; x += step)
				{
					var cell = new CPos(x, y);
					if (!map.Contains(cell) || player.Shroud.IsVisible(cell))
						continue;

					(player.Shroud.IsExplored(cell) ? fogged : unexplored).Add(cell);
				}

			if (unexplored.Count < wanted)
				unexplored.AddRange(fogged);

			return unexplored;
		}

		// The hunt is over: every free unit stays with the MCV (the MCV module picks the safe deploy site and deploys it); a unit that fell behind is ordered back.
		void EscortMcv(IBot bot, IBotUnitLeases leases, Actor mcv, FrozenSet<string> mcvTypes)
		{
			foreach (var (_, (collector, _)) in cellReservations.ToList())
				leases?.Release(collector, LeaseOwner);

			cellReservations.Clear();
			foreach (var unit in EligibleUnits(leases).Where(a => a != mcv && !mcvTypes.Contains(a.Info.Name)))
			{
				leases?.Release(unit, LeaseOwner);
				if (CrateReserveCA.NeedsEscortOrder((unit.Location - mcv.Location).Length, Info.EscortRadiusCells))
					bot.QueueOrder(new Order("Move", unit, Target.FromCell(world, mcv.Location), false));
			}
		}

		// The cheapest of the nearest idle units: a cheap, quick runner, not the biggest tank that happens to stand closest.
		Actor CheapCollector(List<Actor> idle, CPos cell) =>
			idle.OrderBy(u => (u.Location - cell).LengthSquared).Take(Math.Max(1, Info.CollectorCandidates))
				.OrderBy(UnitCost).ThenBy(u => (u.Location - cell).LengthSquared).FirstOrDefault();

		/// <summary>LC2: whether a crate reservation must be released. Pure, so tests can drive every case.</summary>
		public static bool ReservationStale(bool crateGone, bool collectorGone, bool collectorIdle, int ageTicks,
			int idleGraceTicks, int timeoutTicks)
		{
			if (crateGone || collectorGone)
				return true;

			if (collectorIdle && ageTicks > idleGraceTicks)
				return true;

			return timeoutTicks >= 0 && ageTicks > timeoutTicks;
		}

		void ReleaseStaleReservations()
		{
			if (reservations.Count == 0)
				return;

			var tick = world.WorldTick;
			var leases = BotUnitLeases.Of(player);
			foreach (var crate in reservations.Keys.ToList())
			{
				var (collector, ordered) = reservations[crate];
				var collectorGone = collector.IsDead || !collector.IsInWorld || collector.Owner != player;
				if (ReservationStale(crate.IsDead || !crate.IsInWorld, collectorGone, !collectorGone && collector.IsIdle,
					tick - ordered, Info.CollectorIdleGraceTicks, Info.ReservationTimeoutTicks))
				{
					reservations.Remove(crate);
					leases?.Release(collector, LeaseOwner);
				}
			}
		}

		Target PathToNextCrate(Actor collector, Actor crate) => PathToCell(collector, crate.Location);

		Target PathToCell(Actor collector, CPos crateCell)
		{
			var mobile = collector.Trait<Mobile>();
			var path = mobile.PathFinder.FindPathToTargetCell(
				collector, [collector.Location], crateCell, BlockedByActor.Stationary,
				location => world.FindActorsInCircle(world.Map.CenterOfCell(location), Info.EnemyAvoidanceRadius)
					.Where(u => !u.IsDead && u.CanBeViewedByPlayer(collector.Owner)
						&& collector.Owner.RelationshipWith(u.Owner) == PlayerRelationship.Enemy)
					.Sum(u => Math.Max(WDist.Zero.Length, Info.EnemyAvoidanceRadius.Length - (world.Map.CenterOfCell(location) - u.CenterPosition).Length))
				// CRATE-RESERVE-01: avoid stepping on a reserved crate on the way (a unit entering a crate cell collects it).
				+ (reservedCrateCells.Count > 0 && location != crateCell && reservedCrateCells.Contains(location) ? 10000 : 0));

			if (path.Count == 0)
				return Target.Invalid;

			// Don't use the actor to avoid invalid targets when the Crate disappears midway.
			return Target.FromCell(world, crateCell);
		}
	}
}
