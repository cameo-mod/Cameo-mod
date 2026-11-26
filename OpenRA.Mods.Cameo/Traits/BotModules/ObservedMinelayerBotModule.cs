#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Frozen;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// FOG-MINELAYER-01 (Opus 11-26 BINDING no-omniscience; Haiku FOG_SCAN_03 3ec93acd; Terra RM-04 aabe98b8). Owner: Frontrunner.
	// The engine MinelayerBotModule reads hidden enemies four ways (a random enemy anywhere and its real location, the enemy branch of the nearby-actor test, the live trait state of an attacker,
	// the attacker's real location). ai.yaml now gives that module to classicbot only; genericbot gets this one, which knows only what the bot sees (enemy actors in sight, enemy buildings the
	// frozen layer remembers, our own and allied actors) and adds RM-04's stall abort and retry. Row fog_minelayer: off = no generic minelaying at all, never the old hidden read.
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI minelayer units like the engine MinelayerBotModule, from what the bot observes only. Records conflict locations where an enemy hurt our actors,",
		"merges them into favorite locations, and aims at an observed enemy when it has none. An ordered minelayer that stops making progress is stopped and its site parked.")]
	public class ObservedMinelayerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Enemy target types to ignore when add the minefield location to conflict location.")]
		public readonly BitSet<TargetableType> IgnoredEnemyTargetTypes = default;

		[Desc("Victim target types that considering conflict location as enemy location instead of victim location (used only when the attacker is in sight).")]
		public readonly BitSet<TargetableType> UseEnemyLocationTargetTypes = default;

		[ActorReference(typeof(MinelayerInfo))]
		[Desc("Actors with " + nameof(Minelayer) + " trait.")]
		public readonly FrozenSet<string> MinelayingActorTypes = default;

		[Desc("Find this amount of suitable actors and lay mine to a location.")]
		public readonly int MaxPerAssign = 1;

		[Desc("Scan suitable actors and target in this interval (world ticks).")]
		public readonly int ScanTick = 320;

		[Desc("Radius per mine laying order.")]
		public readonly int MineFieldRadius = 1;

		[Desc("Minefield location is cancelled if those whose target type belong to allied nearby.")]
		public readonly BitSet<TargetableType> AwayFromAlliedTargetTypes = default;

		[Desc("Minefield location is cancelled if those whose target type belong to an enemy that is in sight or remembered nearby.")]
		public readonly BitSet<TargetableType> AwayFromEnemyTargetTypes = default;

		[Desc("Minefield location check distance to AwayFromAlliedTargettype and AwayFromEnemyTargettype.",
			"In addition, if any observed enemy actor within this range and minefield location is not cancelled,",
			"minelayer will try lay mines at the 3/4 path to minefield location")]
		public readonly int AwayFromCellDistance = 9;

		[Desc("Merge conflict point minefield position to a favorite minefield position if within this range and closest.",
			"If favorite minefield positions is at the max of 5, we always merge it to closest regardless of this.",
			"A parked site covers this radius too.")]
		public readonly int FavoritePositionDistance = 6;

		[Desc("FOG-MINELAYER-01: the module acts only while this is true (code default false; ai.yaml arms it for genericbot). Row fog_minelayer.")]
		public readonly bool FogMinelayer = false;

		[Desc("RM-04: an ordered minelayer whose cell has not changed for this many ticks is stopped and its site parked.")]
		public readonly int StallTicks = 750;

		[Desc("RM-04: how often the ordered minelayers are checked for a stall.")]
		public readonly int StallCheckTicks = 50;

		[Desc("RM-04: a stalled site is parked for this many ticks times its failure count; after MaxStallAttempts failures it is parked for the rest of the game.")]
		public readonly int ParkTicks = 3000;

		[Desc("RM-04: stalls at one site before it is parked for the rest of the game.")]
		public readonly int MaxStallAttempts = 2;

		public override object Create(ActorInitializer init) { return new ObservedMinelayerBotModule(init.Self, this); }
	}

	public class ObservedMinelayerBotModule : ConditionalTrait<ObservedMinelayerBotModuleInfo>, IBotTick, IBotRespondToAttack
	{
		const int MaxPositionCacheLength = 5;
		const int RepeatedAlertTicks = 40;

		sealed class OrderedMiner
		{
			public Actor Actor;
			public CPos Site;
			public CPos LastCell;
			public int LastProgressTick;
		}

		struct ParkedSite
		{
			public CPos Site;
			public int Until;
			public int Failures;
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly MersenneTwister random;
		readonly CPos?[] conflictPositionQueue;
		readonly CPos?[] favoritePositions;
		readonly List<Actor> idleMinelayers = [];
		readonly List<CPos> observedCells = [];
		readonly List<OrderedMiner> ordered = [];
		readonly List<ParkedSite> parked = [];

		AblationLatch ablateFogMinelayer;
		FrozenActorLayer frozenLayer;
		PathFinder pathFinder;

		int nextScanTick;
		int nextStallCheckTick;
		int conflictPositionLength;
		int favoritePositionsLength;
		int currentFavoritePositionIndex;
		int alertedUntilTick;

		bool FogMinelayerOn => Info.FogMinelayer && !ablateFogMinelayer.Off(player, AblationIds.FogMinelayer);

		public ObservedMinelayerBotModule(Actor self, ObservedMinelayerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			random = BotRandom.Create(world, player, 0x11); // deterministic per-module stream (lobby seed + client index + module salt); the engine module never runs for the same player
			conflictPositionQueue = new CPos?[MaxPositionCacheLength];
			favoritePositions = new CPos?[MaxPositionCacheLength];
		}

		protected override void Created(Actor self)
		{
			frozenLayer = player.PlayerActor.TraitOrDefault<FrozenActorLayer>();
			pathFinder = world.WorldActor.Trait<PathFinder>();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay. World ticks, not call counts, so the round-robin stagger needs no replay registration.
			nextScanTick = world.WorldTick + random.Next(0, Info.ScanTick);
			nextStallCheckTick = world.WorldTick + Info.StallCheckTicks;
			alertedUntilTick = 0;
			conflictPositionLength = 0;
			favoritePositionsLength = 0;
			currentFavoritePositionIndex = 0;
			ordered.Clear();
			parked.Clear();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (!FogMinelayerOn)
				return;

			var now = world.WorldTick;
			if (ordered.Count > 0 && now >= nextStallCheckTick)
			{
				nextStallCheckTick = now + Info.StallCheckTicks;
				CheckStalls(bot, now);
			}

			if (now < nextScanTick)
				return;

			nextScanTick = now + Info.ScanTick;
			Scan(bot, now);
		}

		void Scan(IBot bot, int now)
		{
			var minelayingPosition = CPos.Zero;
			var useFavoritePosition = false;
			var layMineOnHalfway = false;

			while (conflictPositionLength > 0)
			{
				minelayingPosition = conflictPositionQueue[0].Value;
				var (hasInvalidActors, hasEnemyNearby) = HasInvalidActorInCircle(world.Map.CenterOfCell(minelayingPosition), WDist.FromCells(Info.AwayFromCellDistance));
				if (hasInvalidActors || IsParked(minelayingPosition, now))
					DequeueFirstConflictPosition();
				else
				{
					layMineOnHalfway = hasEnemyNearby;
					break;
				}
			}

			if (conflictPositionLength == 0)
			{
				// With no recorded position (an enemy that turtles at its base) look for a location in the middle of the path to something we have observed.
				if (favoritePositionsLength == 0)
				{
					if (!ListIdleMinelayers())
						return;

					if (!TryPickObservedCell(out var observed))
						return;

					foreach (var minelayer in idleMinelayers)
					{
						// Terrain only: actors we cannot see must not bend the path.
						var cells = pathFinder.FindPathToTargetCell(minelayer, [minelayer.Location], observed, BlockedByActor.None, laneBias: false);
						if (cells != null && cells.Count != 0)
						{
							EnqueueConflictPosition(cells[cells.Count / 2]);

							// We don't do other things in this tick, just find new location and abort
							return;
						}
					}

					return;
				}
				else
				{
					while (favoritePositionsLength > 0)
					{
						minelayingPosition = favoritePositions[currentFavoritePositionIndex].Value;
						var (hasInvalidActors, hasEnemyNearby) = HasInvalidActorInCircle(world.Map.CenterOfCell(minelayingPosition), WDist.FromCells(Info.AwayFromCellDistance));
						if (hasInvalidActors || IsParked(minelayingPosition, now))
						{
							DeleteCurrentFavoritePosition();
							if (favoritePositionsLength == 0)
								return;
						}
						else
						{
							layMineOnHalfway = hasEnemyNearby;
							useFavoritePosition = true;
							break;
						}
					}
				}
			}

			if (!ListIdleMinelayers())
				return;

			var orderedActors = new List<Actor>();

			foreach (var minelayer in idleMinelayers)
			{
				var cells = pathFinder.FindPathToTargetCell(minelayer, [minelayer.Location], minelayingPosition, BlockedByActor.None, laneBias: false);
				if (cells != null && cells.Count != 0)
				{
					orderedActors.Add(minelayer);

					// If an observed enemy actor is nearby, lay the mines on 3/4 of the way to the desired position (the path cell is reversed)
					if (layMineOnHalfway)
					{
						minelayingPosition = cells[cells.Count * 1 / 4];
						layMineOnHalfway = false;
					}

					if (orderedActors.Count >= Info.MaxPerAssign)
						break;
				}
			}

			if (orderedActors.Count > 0)
			{
				if (useFavoritePosition)
					NextFavoritePositionIndex();
				else
				{
					DequeueFirstConflictPosition();
					AddPositionToFavoritePositions(minelayingPosition);
				}

				var vec = new CVec(Info.MineFieldRadius, Info.MineFieldRadius);
				bot.QueueOrder(
					new Order(
						"PlaceMinefield",
						null,
						Target.FromCell(world, minelayingPosition + vec),
						false,
						groupedActors: orderedActors.ToArray())
					{ ExtraLocation = minelayingPosition - vec });
				bot.QueueOrder(
					new Order(
						"Move",
						null,
						Target.FromCell(world, orderedActors[0].Location),
						true,
						groupedActors: orderedActors.ToArray()));

				foreach (var actor in orderedActors)
					Track(actor, minelayingPosition, now);
			}
			else
			{
				if (useFavoritePosition)
					DeleteCurrentFavoritePosition();
				else
					DequeueFirstConflictPosition();
			}
		}

		// Our own idle minelayers (own actors are always known to us).
		bool ListIdleMinelayers()
		{
			idleMinelayers.Clear();
			foreach (var at in world.ActorsWithTrait<Minelayer>())
			{
				var a = at.Actor;
				if (a == null || a.IsDead || !a.IsInWorld || a.Owner != player || !a.IsIdle)
					continue;

				idleMinelayers.Add(a);
			}

			return idleMinelayers.Count > 0;
		}

		// The cell of one thing the bot has observed: an enemy actor in sight that passes the preferred-unit test, else a remembered enemy building. False when it has observed neither.
		bool TryPickObservedCell(out CPos cell)
		{
			observedCells.Clear();
			foreach (var a in world.Actors)
				if (IsPreferredEnemyUnit(a))
					observedCells.Add(a.Location);

			if (observedCells.Count == 0 && frozenLayer != null)
			{
				foreach (var fa in frozenLayer.FrozenActorsInRegion(world.Map.AllCells))
				{
					var enemyOwner = fa.Owner != null && player.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy;
					if (!enemyOwner)
						continue;

					if (ObservedMine.RememberedCandidate(fa.IsValid, fa.Visible, fa.Hidden, enemyOwner, fa.Info.HasTraitInfo<BuildingInfo>(), fa.Info.HasTraitInfo<HuskInfo>()))
						observedCells.Add(world.Map.CellContaining(fa.CenterPosition));
				}
			}

			if (observedCells.Count == 0)
			{
				cell = CPos.Zero;
				return false;
			}

			cell = observedCells.Random(random);
			return true;
		}

		// The engine's IsPreferredEnemyUnit with the visibility test first: a hidden actor's owner, type, state and place are not read.
		bool IsPreferredEnemyUnit(Actor actor)
		{
			if (actor == null || !actor.CanBeViewedByPlayer(player))
				return false;

			if (player.RelationshipWith(actor.Owner) != PlayerRelationship.Enemy)
				return false;

			var targetTypes = actor.GetEnabledTargetTypes();
			return ObservedMine.PreferredVisibleEnemy(true, true, actor.IsDead, actor.Info.HasTraitInfo<HuskInfo>(), targetTypes.IsEmpty, targetTypes.Overlaps(Info.IgnoredEnemyTargetTypes));
		}

		// Allies as in the engine; enemy actors only when we see them, plus the enemy records the frozen layer remembers.
		(bool HasInvalidActors, bool HasEnemyNearby) HasInvalidActorInCircle(WPos pos, WDist dist)
		{
			var hasEnemyActor = false;
			foreach (var actor in world.FindActorsInCircle(pos, dist))
			{
				var relationship = actor.Owner.RelationshipWith(player);
				if (relationship == PlayerRelationship.Ally)
				{
					var allyTypes = actor.GetEnabledTargetTypes();
					if (!allyTypes.IsEmpty && allyTypes.Overlaps(Info.AwayFromAlliedTargetTypes))
						return (true, hasEnemyActor);
				}
				else if (relationship == PlayerRelationship.Enemy && actor.CanBeViewedByPlayer(player))
				{
					hasEnemyActor = true;
					var enemyTypes = actor.GetEnabledTargetTypes();
					if (!enemyTypes.IsEmpty && enemyTypes.Overlaps(Info.AwayFromEnemyTargetTypes))
						return (true, hasEnemyActor);
				}
			}

			if (frozenLayer != null)
			{
				foreach (var fa in frozenLayer.FrozenActorsInCircle(world, pos, dist))
				{
					var enemyOwner = fa.Owner != null && player.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy;
					if (!ObservedMine.RememberedEnemyNearby(fa.IsValid, fa.Visible, fa.Hidden, enemyOwner))
						continue;

					hasEnemyActor = true;
					var rememberedTypes = fa.TargetTypes;
					if (!rememberedTypes.IsEmpty && rememberedTypes.Overlaps(Info.AwayFromEnemyTargetTypes))
						return (true, hasEnemyActor);
				}
			}

			return (false, hasEnemyActor);
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!FogMinelayerOn)
				return;

			var now = world.WorldTick;
			var attacker = e.Attacker;
			if (now < alertedUntilTick || attacker == null)
				return;

			// A hidden attacker is judged by its owner relationship only; an attacker in sight also passes the preferred-unit test.
			var attackerInSight = attacker.CanBeViewedByPlayer(player);
			var attackerEnemy = player.RelationshipWith(attacker.Owner) == PlayerRelationship.Enemy;
			var preferredWhenSeen = attackerInSight && IsPreferredEnemyUnit(attacker);
			if (!ObservedMine.AttackCounts(attackerInSight, attackerEnemy, preferredWhenSeen))
				return;

			alertedUntilTick = now + RepeatedAlertTicks;

			var hasInvalidActor = HasInvalidActorInCircle(self.CenterPosition, WDist.FromCells(Info.AwayFromCellDistance)).HasInvalidActors;
			if (hasInvalidActor)
				return;

			var targetTypes = self.GetEnabledTargetTypes();
			var victimWantsEnemyLocation = !targetTypes.IsEmpty && targetTypes.Overlaps(Info.UseEnemyLocationTargetTypes);
			var pos = ObservedMine.UsesAttackerCell(attackerInSight, victimWantsEnemyLocation) ? attacker.Location : self.Location;

			EnqueueConflictPosition(pos);
		}

		// RM-04: remember who got a minefield order, and where.
		void Track(Actor actor, CPos site, int now)
		{
			for (var i = ordered.Count - 1; i >= 0; i--)
				if (ordered[i].Actor == actor)
					ordered.RemoveAt(i);

			ordered.Add(new OrderedMiner { Actor = actor, Site = site, LastCell = actor.Location, LastProgressTick = now });
		}

		// RM-04: an ordered minelayer whose cell does not change for StallTicks gets one Stop and its site is parked. The order is never cancelled while the minelayer still moves,
		// and an actor that finished, died, changed owner or was given another order leaves the list.
		void CheckStalls(IBot bot, int now)
		{
			for (var i = ordered.Count - 1; i >= 0; i--)
			{
				var miner = ordered[i];
				var actor = miner.Actor;
				if (actor == null || actor.IsDead || !actor.IsInWorld || actor.Owner != player || actor.IsIdle || !HasLayMines(actor.CurrentActivity))
				{
					ordered.RemoveAt(i);
					continue;
				}

				if (actor.Location != miner.LastCell)
				{
					miner.LastCell = actor.Location;
					miner.LastProgressTick = now;
					continue;
				}

				if (!ObservedMine.Stalled(now, miner.LastProgressTick, Info.StallTicks))
					continue;

				bot.QueueOrder(new Order("Stop", actor, false));
				ParkSite(miner.Site, now);
				ordered.RemoveAt(i);
			}
		}

		static bool HasLayMines(Activity activity)
		{
			for (var a = activity; a != null; a = a.NextActivity)
			{
				if (a is LayMines)
					return true;

				var child = a.ChildActivity;
				if (child != null && HasLayMines(child))
					return true;
			}

			return false;
		}

		bool IsParked(CPos site, int now)
		{
			var radiusSquare = Info.FavoritePositionDistance * Info.FavoritePositionDistance;
			for (var i = 0; i < parked.Count; i++)
				if ((parked[i].Site - site).LengthSquared <= radiusSquare && ObservedMine.ParkedActive(now, parked[i].Until))
					return true;

			return false;
		}

		void ParkSite(CPos site, int now)
		{
			var radiusSquare = Info.FavoritePositionDistance * Info.FavoritePositionDistance;
			var failures = 1;
			var index = -1;
			for (var i = 0; i < parked.Count; i++)
			{
				if ((parked[i].Site - site).LengthSquared <= radiusSquare)
				{
					index = i;
					failures = parked[i].Failures + 1;
					break;
				}
			}

			var entry = new ParkedSite { Site = site, Until = ObservedMine.ParkUntil(now, Info.ParkTicks, failures, Info.MaxStallAttempts), Failures = failures };
			if (index >= 0)
				parked[index] = entry;
			else
				parked.Add(entry);

			RemoveFavoritesNear(site);
		}

		void RemoveFavoritesNear(CPos site)
		{
			var radiusSquare = Info.FavoritePositionDistance * Info.FavoritePositionDistance;
			for (var i = favoritePositionsLength - 1; i >= 0; i--)
			{
				if ((favoritePositions[i].Value - site).LengthSquared > radiusSquare)
					continue;

				for (var j = i; j < favoritePositionsLength - 1; j++)
					favoritePositions[j] = favoritePositions[j + 1];

				favoritePositions[favoritePositionsLength - 1] = null;
				favoritePositionsLength--;
			}

			currentFavoritePositionIndex = favoritePositionsLength > 0 ? currentFavoritePositionIndex % favoritePositionsLength : 0;
		}

		void DequeueFirstConflictPosition()
		{
			for (var i = 1; i < conflictPositionLength; i++)
				conflictPositionQueue[i - 1] = conflictPositionQueue[i];

			conflictPositionQueue[conflictPositionLength - 1] = null;
			conflictPositionLength--;
		}

		void DeleteCurrentFavoritePosition()
		{
			for (var i = currentFavoritePositionIndex; i < favoritePositionsLength - 1; i++)
				favoritePositions[i] = favoritePositions[i + 1];
			favoritePositions[favoritePositionsLength - 1] = null;

			if (--favoritePositionsLength > 0)
				currentFavoritePositionIndex %= favoritePositionsLength;
		}

		void AddPositionToFavoritePositions(CPos cpos)
		{
			var favoriteDistSquare = Info.FavoritePositionDistance * Info.FavoritePositionDistance;
			var closestIndex = 0;
			var closestDistSquare = int.MaxValue;
			for (var i = 0; i < favoritePositionsLength; i++)
			{
				var lengthsquare = (favoritePositions[i].Value - cpos).LengthSquared;
				if (lengthsquare < closestDistSquare)
				{
					closestIndex = i;
					closestDistSquare = lengthsquare;
				}
			}

			// Add new if there is space
			if (closestDistSquare > favoriteDistSquare && favoritePositionsLength < favoritePositions.Length)
			{
				favoritePositions[favoritePositionsLength] = cpos;
				favoritePositionsLength++;
			}
			else
			{
				var pos = favoritePositions[closestIndex].Value;
				favoritePositions[closestIndex] = (pos - cpos) / 2 + cpos;
			}
		}

		void NextFavoritePositionIndex()
		{
			currentFavoritePositionIndex = (currentFavoritePositionIndex + 1) % favoritePositionsLength;
		}

		void EnqueueConflictPosition(CPos cell)
		{
			if (conflictPositionLength < MaxPositionCacheLength)
			{
				conflictPositionQueue[conflictPositionLength] = cell;
				conflictPositionLength++;
			}
			else
				conflictPositionQueue[MaxPositionCacheLength - 1] = cell;
		}
	}
}
