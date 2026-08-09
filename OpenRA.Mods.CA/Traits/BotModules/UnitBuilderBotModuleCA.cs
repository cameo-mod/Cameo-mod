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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Controls AI unit production.")]
	public class UnitBuilderBotModuleCAInfo : ConditionalTraitInfo
	{
		// TODO: Investigate whether this might the (or at least one) reason why bots occasionally get into a state of doing nothing.
		// Reason: If this is less than SquadSize, the bot might get stuck between not producing more units due to this,
		// but also not creating squads since there aren't enough idle units.
		[Desc("Only produce units as long as there are less than this amount of units idling inside the base.")]
		public readonly int IdleBaseUnitsMaximum = 12;

		[Desc("Production queues AI uses for producing units.")]
		public readonly string[] UnitQueues = { "VehicleSQ", "InfantrySQ", "AircraftSQ", "ShipSQ", "VehicleMQ", "InfantryMQ", "AircraftMQ", "ShipMQ" };

		[Desc("Basic combat units that may be produced from the starting-cash surplus while the opening refinery is unfinished.")]
		public readonly HashSet<string> OpeningDefenseUnitTypes = new HashSet<string>();

		[Desc("What units to the AI should build.", "What relative share of the total army must be this type of unit.")]
		public readonly Dictionary<string, int> UnitsToBuild = null;

		[Desc("What units should the AI have a maximum limit to train.")]
		public readonly Dictionary<string, int> UnitLimits = null;

		[Desc("When should the AI start train specific units.")]
		public readonly Dictionary<string, int> UnitDelays = null;

		[Desc("Minimum duration between building a specific unit.")]
		public readonly Dictionary<string, int> UnitIntervals = null;

		[Desc("How often should the unit builder check to build more units")]
		public readonly int UnitBuilderInterval = 0;

		[Desc("Only queue construction of a new unit when above this requirement.")]
		public readonly int ProductionMinCashRequirement = 2000;

		[Desc("Only queue construction of a new unit when above this requirement.")]
		public readonly int MaximiseProductionCashRequirement = 10000;

		[Desc("Bot types that preserve RecoveryCashReserve and use it as their queue-saturation threshold.")]
		public readonly FrozenSet<string> ExcessCashBotTypes = FrozenSet<string>.Empty;

		[Desc("Cash retained by ExcessCashBotTypes when choosing ordinary production.")]
		public readonly int RecoveryCashReserve = 0;

		[Desc("Bot types allowed to adapt production weights to an omniscient enemy composition sample.")]
		public readonly FrozenSet<string> AdaptiveCounterBotTypes = FrozenSet<string>.Empty;

		[Desc("Maximum percentage of production weight supplied by adaptive counter scoring.")]
		public readonly int AdaptiveCounterWeight = 40;

		[Desc("Ticks between composition samples.")]
		public readonly int AdaptiveObservationInterval = 250;

		[Desc("Ticks to retain the selected primary enemy before it may change.")]
		public readonly int AdaptiveTargetLockDuration = 1500;

		[Desc("Optional counter-score bonuses for specialist units whose capability is not obvious from weapons.")]
		public readonly Dictionary<string, int> AdaptiveCounterOverrides = new();

		[Desc("Explicit observed roles for ambiguous actor types. Values are comma-separated CombatRole names.")]
		public readonly Dictionary<string, string> AdaptiveRoleOverrides = new();

		[Desc("Explicit enemy roles countered by ambiguous specialist units.")]
		public readonly Dictionary<string, string> AdaptiveCounterRoleOverrides = new();

		[Desc("Maximum number of aircraft AI can build.",
			"If MaintainAirSuperiority is true this only applies to units not listed in AirToAirUnits.")]
		public readonly int MaxAircraft = 4;

		[Desc("If true, will always attempt to match the number of enemy air threats.")]
		public readonly bool MaintainAirSuperiority = false;

		[Desc("If MaintainAirSuperiority is true and this is non-zero,",
			"sets an upper limit for the number of air superiority aircraft.")]
		public readonly int MaxAirSuperiority = 0;

		[Desc("List of actor types to be used for air superiority.")]
		public readonly HashSet<string> AirToAirUnits = new HashSet<string>();

		[Desc("List of actor types to measure against for air superiority.")]
		public readonly HashSet<string> AirThreatUnits = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new UnitBuilderBotModuleCA(init.Self, this); }
	}

	public class UnitBuilderBotModuleCA : ConditionalTrait<UnitBuilderBotModuleCAInfo>, IBotTick, IBotNotifyIdleBaseUnits, IBotRequestUnitProduction, IGameSaveTraitData, IBotAircraftBuilder, INotifyActorDisposing
	{
		[Flags]
		enum CombatRole { None = 0, Infantry = 1, LightVehicle = 2, HeavyArmor = 4, Artillery = 8, Aircraft = 16, Naval = 32, Stealth = 64, Support = 128 }

		static readonly BitSet<TargetableType> InfantryTargets = new("Infantry");
		static readonly BitSet<TargetableType> AircraftTargets = new("Air", "Aircraft");
		static readonly BitSet<TargetableType> NavalTargets = new("Water", "Naval", "Ship");

		public const int FeedbackTime = 30; // ticks; = a bit over 1s. must be >= netlag.

		readonly World world;
		readonly Player player;

		readonly List<string> queuedBuildRequests = new List<string>();
		readonly ActorIndex.OwnerAndNames unitsToBuild;
		readonly Dictionary<string, int> activeUnitIntervals = new Dictionary<string, int>();
		readonly Dictionary<string, int> observedEnemyValue = new Dictionary<string, int>();

		IBotRequestPauseUnitProduction[] requestPause;
		IBotRequestPauseUnitProductionForQueue[] requestQueuePause;
		IBotCashReservation cashReservation;
		int idleUnitCount;
		int currentQueueIndex = 0;
		PlayerResources playerResources;
		BotLimits botLimits;
		BaseBuilderBotModuleCA baseBuilder;
		bool useExcessCashPolicy;
		bool useAdaptiveCounters;
		int adaptiveTargetClientIndex = -1;
		int adaptiveTargetLockUntil;
		int nextAdaptiveObservation;
		int committedCashTick = -1;
		int committedProductionCash;
		int adaptiveSelections;
		int totalWeightedSelections;
		bool lastChoiceAdaptive;

		int ticks;
		int openingDefenseTicks;
		int unitDelayModifier = 100;
		int unitIntervalModifier = 100;
		bool firstTick = true;

		public UnitBuilderBotModuleCA(Actor self, UnitBuilderBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitsToBuild = new ActorIndex.OwnerAndNames(world, info.UnitsToBuild.Keys, player);
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			requestPause = self.TraitsImplementing<IBotRequestPauseUnitProduction>().ToArray();
			requestQueuePause = self.TraitsImplementing<IBotRequestPauseUnitProductionForQueue>().ToArray();
			cashReservation = self.TraitsImplementing<IBotCashReservation>().FirstOrDefault();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		protected override void TraitEnabled(Actor self)
		{
			RefreshDifficultyTraits();
		}

		void RefreshDifficultyTraits()
		{
			botLimits = player.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			baseBuilder = player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			unitDelayModifier = botLimits?.Info.UnitDelayModifier ?? 100;
			unitIntervalModifier = botLimits?.Info.UnitIntervalModifier ?? 100;
			useExcessCashPolicy = Info.ExcessCashBotTypes.Contains(player.BotType);
			useAdaptiveCounters = Info.AdaptiveCounterBotTypes.Contains(player.BotType);
		}

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<UnitWposWrapper> idleUnits)
		{
			idleUnitCount = idleUnits.Count;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (committedCashTick != world.WorldTick)
			{
				committedCashTick = world.WorldTick;
				committedProductionCash = 0;
			}

			if (firstTick)
			{
				RefreshDifficultyTraits();
				firstTick = false;
			}

			if (useAdaptiveCounters && world.WorldTick >= nextAdaptiveObservation)
			{
				nextAdaptiveObservation = world.WorldTick + Math.Max(1, Info.AdaptiveObservationInterval);
				ObserveEnemyComposition();
			}

			// Decrement any active unit intervals, removing any that reach zero
			foreach (KeyValuePair<string, int> i in activeUnitIntervals.ToList())
			{
				activeUnitIntervals[i.Key]--;
				if (activeUnitIntervals[i.Key] <= 0)
					activeUnitIntervals.Remove(i.Key);
			}

			var baseBuilderPause = requestPause.FirstOrDefault(rp => ReferenceEquals(rp, baseBuilder));
			if (requestPause.Any(rp => !ReferenceEquals(rp, baseBuilderPause) && rp.PauseUnitProduction))
				return;

			if (baseBuilderPause != null && baseBuilderPause.PauseUnitProduction)
			{
				if (++openingDefenseTicks % (FeedbackTime + Info.UnitBuilderInterval) == 0)
					TryBuildOpeningDefense(bot);

				return;
			}

			openingDefenseTicks = 0;

			ticks++;

			if (ticks % (FeedbackTime + Info.UnitBuilderInterval) == 0)
			{
				var buildRequest = queuedBuildRequests.FirstOrDefault();
				if (buildRequest != null)
				{
					var requestQueued = BuildUnit(bot, buildRequest);
					queuedBuildRequests.Remove(buildRequest);
					if (requestQueued)
						return;
				}

				// Don't produce if we don't have enough cash
				if (playerResources.Cash + playerResources.Resources < Info.ProductionMinCashRequirement)
					return;

				for (var i = 0; i < Info.UnitQueues.Length; i++)
				{
					if (++currentQueueIndex >= Info.UnitQueues.Length)
						currentQueueIndex = 0;

					if (AIUtils.FindQueues(player, Info.UnitQueues[currentQueueIndex]).Any())
					{
						// PERF: We tick only one type of valid queue at a time
						// if AI gets enough cash, it can fill all of its queues with enough ticks
						BuildUnit(bot, Info.UnitQueues[currentQueueIndex], idleUnitCount < Info.IdleBaseUnitsMaximum, false);

						var maximiseThreshold = useExcessCashPolicy ? Info.RecoveryCashReserve : Info.MaximiseProductionCashRequirement;
						if (playerResources.Cash + playerResources.Resources < maximiseThreshold)
							break;
					}
				}
			}
		}

		void TryBuildOpeningDefense(IBot bot)
		{
			if (baseBuilder == null || !baseBuilder.CanTrainOpeningDefense || Info.OpeningDefenseUnitTypes.Count == 0)
				return;

			foreach (var queue in Info.UnitQueues.SelectMany(category => AIUtils.FindQueues(player, category)).Distinct())
			{
				if (queue.AllQueued().Any())
					continue;

				var unit = queue.BuildableItems()
					.FirstOrDefault(a => Info.OpeningDefenseUnitTypes.Contains(a.Name) && ShouldBuild(a.Name, false));
				if (unit == null)
					continue;

				var cost = queue.GetProductionCost(unit);
				if (playerResources.GetCashAndResources() < cost || !baseBuilder.TryCommitOpeningDefenseCost(cost))
					return;

				SetUnitInterval(unit.Name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, unit.Name, 1));
				AIUtils.BotDebug("AI: {0} decided to build {1} from the opening defense budget.", player, unit.Name);
				return;
			}
		}

		void IBotRequestUnitProduction.RequestUnitProduction(IBot bot, string requestedActor)
		{
			queuedBuildRequests.Add(requestedActor);
		}

		int IBotRequestUnitProduction.RequestedProductionCount(IBot bot, string requestedActor)
		{
			return queuedBuildRequests.Count(r => r == requestedActor);
		}

		void BuildUnit(IBot bot, string category, bool buildRandom, bool excludeLimited)
		{
			// For queues that support parallel production (e.g. Zerg hatchery), find one with a free slot.
			// For standard queues, require the queue to be completely empty.
			var queue = AIUtils.FindQueues(player, category).FirstOrDefault(q =>
			{
				if (q is IHasParallelQueueSlots p)
					return p.AvailableSlots > 0;
				return !q.AllQueued().Any();
			});

			if (queue == null)
				return;

			// Fill all available parallel slots in one pass so that every larva stays occupied.
			var slotsToFill = (queue is IHasParallelQueueSlots parallelQueue)
				? parallelQueue.AvailableSlots
				: 1;

			for (var slot = 0; slot < slotsToFill; slot++)
			{
				lastChoiceAdaptive = false;
				var unit = buildRandom && !(useAdaptiveCounters && observedEnemyValue.Count > 0) ?
					ChooseRandomUnitToBuild(queue, excludeLimited) :
					ChooseUnitToBuild(queue, excludeLimited);

				if (unit == null)
					return;

				var name = unit.Name;

				if (!ShouldBuild(name, false))
				{
					if (!excludeLimited)
						BuildUnit(bot, category, buildRandom, true);

					return;
				}

				var cost = queue.GetProductionCost(unit);
				if (!TryReserveCash(cost, useExcessCashPolicy ? Info.RecoveryCashReserve : 0))
					return;

				if (requestQueuePause.Any(rp => rp.PauseUnitProductionForQueue(category, unit)))
					return;

				SetUnitInterval(name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
				if (IsMobileCombatActor(unit))
				{
					totalWeightedSelections++;
					if (lastChoiceAdaptive)
						adaptiveSelections++;
				}
			}
		}

		// In cases where we want to build a specific unit but don't know the queue name (because there's more than one possibility)
		bool BuildUnit(IBot bot, string name)
		{
			var actorInfo = world.Map.Rules.Actors[name];
			if (actorInfo == null)
				return false;

			var buildableInfo = actorInfo.TraitInfoOrDefault<BuildableInfo>();
			if (buildableInfo == null)
				return false;

			if (!ShouldBuild(name, true))
				return false;

			ProductionQueue queue = null;
			foreach (var pq in buildableInfo.Queue)
			{
				queue = AIUtils.FindQueues(player, pq).FirstOrDefault(q => !q.AllQueued().Any());
				if (queue != null)
					break;
			}

			if (queue != null && queue.BuildableItems().Any(b => b.Name == name))
			{
				var cost = queue.GetProductionCost(actorInfo);
				var externalReserve = useExcessCashPolicy ? Math.Min(Info.RecoveryCashReserve, 2500) : 0;
				if (!TryReserveCash(cost, externalReserve))
					return false;

				if (requestQueuePause.Any(rp => rp.PauseUnitProductionForQueue(queue.Info.Type, actorInfo)))
					return false;

				SetUnitInterval(name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));
				AIUtils.BotDebug("AI: {0} decided to build {1} (external request)", queue.Actor.Owner, name);
				return true;
			}

			return false;
		}

		bool TryReserveCash(int cost, int reserve)
		{
			if (cashReservation != null)
				return cashReservation.TryReserveCash(cost, reserve);

			if (playerResources.GetCashAndResources() - committedProductionCash - cost < reserve)
				return false;

			committedProductionCash += cost;
			return true;
		}

		void SetUnitInterval(string name)
		{
			if (Info.UnitIntervals == null || !Info.UnitIntervals.ContainsKey(name))
				return;

			activeUnitIntervals[name] = Info.UnitIntervals[name] * unitIntervalModifier / 100;
		}

		bool ShouldBuild(string name, bool ignoreUnitsToBuild)
		{
			if (!ignoreUnitsToBuild && Info.UnitsToBuild != null && !Info.UnitsToBuild.ContainsKey(name))
				return false;

			if (Info.UnitDelays != null &&
				Info.UnitDelays.ContainsKey(name) &&
				Info.UnitDelays[name] * unitDelayModifier / 100 > world.WorldTick)
				return false;

			if (Info.UnitIntervals != null &&
				Info.UnitIntervals.ContainsKey(name) &&
				activeUnitIntervals.ContainsKey(name))
				return false;

			if (Info.UnitLimits != null &&
				Info.UnitLimits.ContainsKey(name) &&
				world.Actors.Count(a => !a.IsDead && a.Owner == player && a.Info.Name == name) >= Info.UnitLimits[name])
				return false;

			return true;
		}

		ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue, bool excludeLimited)
		{
			var buildableThings = queue.BuildableItems().Where(a => Info.UnitsToBuild.ContainsKey(a.Name) && (!excludeLimited || !Info.UnitLimits.ContainsKey(a.Name)));
			if (!buildableThings.Any())
				return null;

			var unit = buildableThings.Random(world.LocalRandom);
			return CanBuildMoreOfAircraft(unit) ? unit : null;
		}

		ActorInfo ChooseUnitToBuild(ProductionQueue queue, bool excludeLimited)
		{
			lastChoiceAdaptive = false;
			var buildableThings = queue.BuildableItems();
			if (!buildableThings.Any())
				return null;

			var myUnits = player.World
				.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == player)
				.Select(a => a.Info.Name).ToList();
			myUnits.AddRange(world.ActorsWithTrait<ProductionQueue>()
				.Where(q => q.Actor.Owner == player && q.Trait.Enabled)
				.SelectMany(q => q.Trait.AllQueued()).Select(q => q.Item)
				.Where(name => world.Map.Rules.Actors.TryGetValue(name, out var actorInfo) && actorInfo.HasTraitInfo<IPositionableInfo>()));

			var allowAdaptiveSelection = useAdaptiveCounters && totalWeightedSelections >= 2 &&
				(adaptiveSelections + 1) * 100 <= (totalWeightedSelections + 1) * Math.Clamp(Info.AdaptiveCounterWeight, 0, 40);
			if (allowAdaptiveSelection)
			{
				var adaptiveChoice = buildableThings
					.Where(a => IsMobileCombatActor(a) && Info.UnitsToBuild.ContainsKey(a.Name) &&
						(!excludeLimited || !Info.UnitLimits.ContainsKey(a.Name)) && ShouldBuild(a.Name, false))
					.Select(a => new { Actor = a, Score = AdaptiveCounterScore(a) / (1 + myUnits.Count(n => n == a.Name)) })
					.Where(x => x.Score > 0 && CanBuildMoreOfAircraft(x.Actor))
					.OrderByDescending(x => x.Score).ThenBy(x => x.Actor.Name).FirstOrDefault();
				if (adaptiveChoice != null)
				{
					lastChoiceAdaptive = true;
					return adaptiveChoice.Actor;
				}
			}

			foreach (var unit in Info.UnitsToBuild.Shuffle(world.LocalRandom))
				if (buildableThings.Any(b => b.Name == unit.Key))
					if (!excludeLimited || !Info.UnitLimits.ContainsKey(unit.Key))
					{
						if (myUnits.Count(a => a == unit.Key) * 100 < unit.Value * Math.Max(1, myUnits.Count))
							if (CanBuildMoreOfAircraft(world.Map.Rules.Actors[unit.Key]))
								return world.Map.Rules.Actors[unit.Key];
					}

			return null;
		}

		void ObserveEnemyComposition()
		{
			var enemies = world.Players.Where(p => p.WinState == WinState.Undefined &&
				player.RelationshipWith(p) == PlayerRelationship.Enemy).OrderBy(p => p.ClientIndex).ToArray();
			if (enemies.Length == 0)
			{
				observedEnemyValue.Clear();
				adaptiveTargetClientIndex = -1;
				return;
			}

			var target = world.WorldTick < adaptiveTargetLockUntil
				? enemies.FirstOrDefault(p => p.ClientIndex == adaptiveTargetClientIndex)
				: null;
			if (target == null)
			{
				var previousTarget = adaptiveTargetClientIndex;
				target = enemies.Select(p => new
				{
					Player = p,
					Value = world.ActorsHavingTrait<IPositionable>().Where(a => a.Owner == p && !a.IsDead &&
						IsObservedArmyActor(a.Info)).Sum(a => a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 1)
				}).OrderByDescending(x => x.Value).ThenBy(x => x.Player.ClientIndex).First().Player;
				adaptiveTargetClientIndex = target.ClientIndex;
				adaptiveTargetLockUntil = world.WorldTick + Math.Max(1, Info.AdaptiveTargetLockDuration);
				if (previousTarget >= 0 && previousTarget != adaptiveTargetClientIndex)
					observedEnemyValue.Clear();
			}

			var current = world.ActorsHavingTrait<IPositionable>()
				.Where(a => a.Owner == target && !a.IsDead && IsObservedArmyActor(a.Info))
				.GroupBy(a => a.Info.Name).ToDictionary(g => g.Key,
					g => g.Sum(a => a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 1));

			foreach (var name in observedEnemyValue.Keys.Union(current.Keys).ToArray())
			{
				current.TryGetValue(name, out var value);
				observedEnemyValue.TryGetValue(name, out var oldValue);
				var smoothed = (oldValue * 3 + value) / 4;
				if (smoothed > 0)
					observedEnemyValue[name] = smoothed;
				else
					observedEnemyValue.Remove(name);
			}
		}

		bool IsObservedArmyActor(ActorInfo actor) => actor.HasTraitInfo<AttackBaseInfo>() ||
			actor.HasTraitInfo<CargoInfo>() || actor.HasTraitInfo<CloakInfo>();

		bool IsMobileCombatActor(ActorInfo actor) => actor.HasTraitInfo<IPositionableInfo>() &&
			actor.HasTraitInfo<AttackBaseInfo>() && !actor.HasTraitInfo<HarvesterInfo>();

		int AdaptiveCounterScore(ActorInfo candidate)
		{
			var weapons = candidate.TraitInfos<ArmamentInfo>().Select(a => a.WeaponInfo).Where(w => w != null).ToArray();
			var score = Info.AdaptiveCounterOverrides.TryGetValue(candidate.Name, out var bonus) ? bonus : 0;
			if (weapons.Length == 0)
				return score;

			foreach (var enemy in observedEnemyValue)
			{
				if (!world.Map.Rules.Actors.TryGetValue(enemy.Key, out var enemyInfo))
					continue;

				var enemyTargets = enemyInfo.GetAllTargetTypes();
				var roles = ClassifyRoles(enemyInfo);
				var explicitCounterRoles = ParseRoleOverride(Info.AdaptiveCounterRoleOverrides, candidate.Name);
				var weaponCoverage = weapons.Any(w => w.ValidTargets.Overlaps(enemyTargets) && !w.InvalidTargets.Overlaps(enemyTargets));
				if (!weaponCoverage && (explicitCounterRoles & roles) == CombatRole.None)
					continue;

				var candidateCost = candidate.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
				var candidateRange = candidate.TraitInfos<ArmamentInfo>().Select(a => a.ModifiedRange.Length).DefaultIfEmpty(0).Max();
				var candidateRoles = ClassifyRoles(candidate);
				var multiplier = 1;
				if (roles.HasFlag(CombatRole.Infantry) && candidateCost <= 1200)
					multiplier++;
				if (roles.HasFlag(CombatRole.LightVehicle) && candidateCost is >= 500 and <= 1600)
					multiplier++;
				if (roles.HasFlag(CombatRole.HeavyArmor) && candidateCost >= 1000)
					multiplier++;
				if (roles.HasFlag(CombatRole.Artillery) && (candidate.HasTraitInfo<AircraftInfo>() || candidateRange >= WDist.FromCells(7).Length))
					multiplier++;
				if (roles.HasFlag(CombatRole.Aircraft) && weapons.Any(w => w.ValidTargets.Overlaps(AircraftTargets)))
					multiplier += 2;
				if (roles.HasFlag(CombatRole.Naval) && (candidateRoles.HasFlag(CombatRole.Naval) || candidateRoles.HasFlag(CombatRole.Aircraft)))
					multiplier++;
				if (roles.HasFlag(CombatRole.Stealth) && candidate.HasTraitInfo<DetectCloakedInfo>())
					multiplier += 2;
				if (roles.HasFlag(CombatRole.Support) && (candidateRoles.HasFlag(CombatRole.Aircraft) || candidateRange >= WDist.FromCells(6).Length))
					multiplier++;
				if ((explicitCounterRoles & roles) != CombatRole.None)
					multiplier += 3;

				score += enemy.Value * multiplier;
			}

			return score;
		}

		CombatRole ClassifyRoles(ActorInfo actor)
		{
			var roles = CombatRole.None;
			var targets = actor.GetAllTargetTypes();
			var cost = actor.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			var hp = actor.TraitInfoOrDefault<HealthInfo>()?.HP ?? 0;
			if (actor.HasTraitInfo<AircraftInfo>())
				roles |= CombatRole.Aircraft;
			else if (targets.Overlaps(NavalTargets))
				roles |= CombatRole.Naval;
			else if (targets.Overlaps(InfantryTargets))
				roles |= CombatRole.Infantry;
			else if (cost >= 1400 || hp >= 100000)
				roles |= CombatRole.HeavyArmor;
			else
				roles |= CombatRole.LightVehicle;

			if (actor.TraitInfos<ArmamentInfo>().Any(a => a.ModifiedRange.Length >= WDist.FromCells(8).Length))
				roles |= CombatRole.Artillery;
			if (actor.HasTraitInfo<CloakInfo>())
				roles |= CombatRole.Stealth;
			if (actor.HasTraitInfo<CargoInfo>() || !actor.HasTraitInfo<AttackBaseInfo>())
				roles |= CombatRole.Support;

			return roles | ParseRoleOverride(Info.AdaptiveRoleOverrides, actor.Name);
		}

		static CombatRole ParseRoleOverride(Dictionary<string, string> overrides, string actorName)
		{
			if (!overrides.TryGetValue(actorName, out var configuredRoles))
				return CombatRole.None;

			var roles = CombatRole.None;
			foreach (var role in configuredRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				if (Enum.TryParse<CombatRole>(role, true, out var parsed))
					roles |= parsed;

			return roles;
		}

		bool IBotAircraftBuilder.CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			return CanBuildMoreOfAircraft(actorInfo);
		}

		bool CanBuildMoreOfAircraft(ActorInfo actorInfo)
		{
			var attackAircraftInfo = actorInfo.TraitInfoOrDefault<AircraftInfo>();
			if (attackAircraftInfo == null)
				return true;

			var limit = Info.MaxAircraft;
			var currentCount = 0;

			if (Info.MaintainAirSuperiority)
			{
				var numAirToAirUnits = AIUtils.GetActorsWithTrait<Aircraft>(player.World).Count(a => a.Owner == player && Info.AirToAirUnits.Contains(a.Info.Name));

				if (Info.AirToAirUnits.Contains(actorInfo.Name))
				{
					currentCount = numAirToAirUnits;
					var numFriendlyAirToAirUnits = player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Ally && Info.AirToAirUnits.Contains(a.Info.Name));
					var numEnemyAirThreatUnits = player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy && Info.AirThreatUnits.Contains(a.Info.Name));
					limit = Math.Max(numEnemyAirThreatUnits - numFriendlyAirToAirUnits + 1, limit);

					if (Info.MaxAirSuperiority > 0)
						limit = Math.Min(Info.MaxAirSuperiority, limit);
				}
				else
					currentCount = AIUtils.GetActorsWithTrait<Aircraft>(player.World).Count(a => a.Owner == player && a.Info.HasTraitInfo<BuildableInfo>()) - numAirToAirUnits;
			}
			else
				currentCount = AIUtils.GetActorsWithTrait<Aircraft>(player.World).Count(a => a.Owner == player && a.Info.HasTraitInfo<BuildableInfo>());

			return currentCount < limit;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("QueuedBuildRequests", FieldSaver.FormatValue(queuedBuildRequests.ToArray())),
				new("IdleUnitCount", FieldSaver.FormatValue(idleUnitCount)),
				new("AdaptiveTargetClientIndex", FieldSaver.FormatValue(adaptiveTargetClientIndex)),
				new("AdaptiveTargetLockUntil", FieldSaver.FormatValue(adaptiveTargetLockUntil)),
				new("NextAdaptiveObservation", FieldSaver.FormatValue(nextAdaptiveObservation)),
				new("ObservedEnemyTypes", FieldSaver.FormatValue(observedEnemyValue.Keys.ToArray())),
				new("ObservedEnemyValues", FieldSaver.FormatValue(observedEnemyValue.Values.ToArray())),
				new("AdaptiveSelections", FieldSaver.FormatValue(adaptiveSelections)),
				new("TotalWeightedSelections", FieldSaver.FormatValue(totalWeightedSelections))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var queuedBuildRequestsNode = data.NodeWithKeyOrDefault("QueuedBuildRequests");
			if (queuedBuildRequestsNode != null)
			{
				queuedBuildRequests.Clear();
				queuedBuildRequests.AddRange(FieldLoader.GetValue<string[]>("QueuedBuildRequests", queuedBuildRequestsNode.Value.Value));
			}

			var idleUnitCountNode = data.NodeWithKeyOrDefault("IdleUnitCount");
			if (idleUnitCountNode != null)
				idleUnitCount = FieldLoader.GetValue<int>("IdleUnitCount", idleUnitCountNode.Value.Value);

			var targetNode = data.NodeWithKeyOrDefault("AdaptiveTargetClientIndex");
			if (targetNode != null)
				adaptiveTargetClientIndex = FieldLoader.GetValue<int>("AdaptiveTargetClientIndex", targetNode.Value.Value);
			var lockNode = data.NodeWithKeyOrDefault("AdaptiveTargetLockUntil");
			if (lockNode != null)
				adaptiveTargetLockUntil = FieldLoader.GetValue<int>("AdaptiveTargetLockUntil", lockNode.Value.Value);
			var observationNode = data.NodeWithKeyOrDefault("NextAdaptiveObservation");
			if (observationNode != null)
				nextAdaptiveObservation = FieldLoader.GetValue<int>("NextAdaptiveObservation", observationNode.Value.Value);
			var typesNode = data.NodeWithKeyOrDefault("ObservedEnemyTypes");
			var valuesNode = data.NodeWithKeyOrDefault("ObservedEnemyValues");
			if (typesNode != null && valuesNode != null)
			{
				var types = FieldLoader.GetValue<string[]>("ObservedEnemyTypes", typesNode.Value.Value);
				var values = FieldLoader.GetValue<int[]>("ObservedEnemyValues", valuesNode.Value.Value);
				observedEnemyValue.Clear();
				for (var i = 0; i < Math.Min(types.Length, values.Length); i++)
					observedEnemyValue[types[i]] = values[i];
			}

			var adaptiveSelectionsNode = data.NodeWithKeyOrDefault("AdaptiveSelections");
			if (adaptiveSelectionsNode != null)
				adaptiveSelections = FieldLoader.GetValue<int>("AdaptiveSelections", adaptiveSelectionsNode.Value.Value);
			var totalSelectionsNode = data.NodeWithKeyOrDefault("TotalWeightedSelections");
			if (totalSelectionsNode != null)
				totalWeightedSelections = FieldLoader.GetValue<int>("TotalWeightedSelections", totalSelectionsNode.Value.Value);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			unitsToBuild.Dispose();
		}
	}
}
