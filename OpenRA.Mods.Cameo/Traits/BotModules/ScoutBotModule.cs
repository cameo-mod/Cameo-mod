#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using CAAIUtils = OpenRA.Mods.CA.AIUtils;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[Desc("Keeps cheap scout units cycling through stale map regions so the fogged " +
		"observation layer (phase 6a) has fresh sightings. Claims units out of the squad " +
		"manager's idle pool, never from squads. See docs/design/AI_FRANSBOT_RESEARCH.md 6b.")]
	public class ScoutBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types usable as scouts (cheap, fast units; empty disables claiming).")]
		public readonly HashSet<string> ScoutUnitTypes = new();

		[Desc("Maximum scouts held at once.")]
		public readonly int MaxScouts = 2;

		[Desc("Ticks between claim/retarget evaluations.")]
		public readonly int ScanInterval = 50;

		[Desc("Ticks a scout keeps its assigned region before being re-evaluated.")]
		public readonly int RetargetTicks = 750;

		[Desc("Ticks until an unexplored region reaches full staleness interest.")]
		public readonly int StaleAfterTicks = 2500;

		[Desc("Ticks a recorded attacker value keeps counting against a region.")]
		public readonly int DangerDecayTicks = 7500;

		[Desc("Bonus interest added to regions containing a resource cluster.")]
		public readonly int ResourceSiteBonus = 3000;

		[Desc("Bonus interest per unit of remembered enemy value in a region.")]
		public readonly int RememberedValueWeight = 1;

		public override object Create(ActorInitializer init) { return new ScoutBotModule(init.Self, this); }
	}

	public class ScoutBotModule : ConditionalTrait<ScoutBotModuleInfo>, IBotTick, IBotEnabled,
		IBotNotifyIdleBaseUnits, IBotRespondToAttack, IBotRegionThreatProvider
	{
		readonly World world;
		readonly OpenRA.Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;

		readonly List<UnitWposWrapper> scouts = new();
		readonly Dictionary<Actor, (int Region, int AssignedTick)> scoutTargets = new();

		// Killer-attributed hostile value per region, fed by attacks on our scouts.
		// Exposed read-only for the 6c risk gate; keys are RegionMemory region indices.
		readonly Dictionary<int, (int Value, int Tick)> dangerByRegion = new();

		ResourceMapBotModule resourceMap;
		IBotRequestUnitProduction[] unitBuilders;
		List<UnitWposWrapper> idlePool = new();
		int scanTicks;

		public ScoutBotModule(Actor self, ScoutBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
		}

		public IReadOnlyDictionary<int, (int Value, int Tick)> DangerByRegion => dangerByRegion;

		protected override void TraitEnabled(Actor self)
		{
			resourceMap = self.TraitOrDefault<ResourceMapBotModule>();
			unitBuilders = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			scanTicks = world.LocalRandom.Next(0, Info.ScanInterval);
		}

		void IBotEnabled.BotEnabled(IBot bot) { }

		void IBotNotifyIdleBaseUnits.UpdatedIdleBaseUnits(List<UnitWposWrapper> unitsHangingAroundTheBase)
		{
			idlePool = unitsHangingAroundTheBase;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--scanTicks > 0)
				return;

			scanTicks = Info.ScanInterval;

			scouts.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			foreach (var dead in scoutTargets.Keys.Where(a => unitCannotBeOrdered(a)).ToArray())
				scoutTargets.Remove(dead);

			// A scout that went idle between scans re-enters the squad manager's
			// idle pool and can be claimed by a squad while we still hold it —
			// yielding duelling orders (DAWN's edge). Release ownership instead.
			if (scouts.Count != 0)
			{
				var squadOwned = player.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>()
					.Where(m => !m.IsTraitDisabled)
					.SelectMany(m => m.Squads)
					.SelectMany(s => s.Units)
					.Select(u => u.Actor)
					.ToHashSet();

				foreach (var scout in scouts.Where(u => squadOwned.Contains(u.Actor)).ToArray())
				{
					scoutTargets.Remove(scout.Actor);
					scouts.Remove(scout);
				}
			}

			var regions = player.PlayerActor.TraitOrDefault<MasterAiBotModule>()?.Situation?.Regions;
			if (regions == null)
				return;

			var tick = world.WorldTick;
			var taken = new HashSet<int>(scoutTargets.Values.Select(t => t.Region));

			// Claiming while nothing is stale only churns the idle pool.
			if (!AnyStaleRegion(regions, tick))
				return;

			ClaimScouts(bot);

			foreach (var scout in scouts.ToArray())
			{
				var actor = scout.Actor;
				if (scoutTargets.TryGetValue(actor, out var target) && tick - target.AssignedTick < Info.RetargetTicks && !actor.IsIdle)
					continue;

				var region = ChooseScoutTarget(regions, actor.Location, tick, taken);
				if (region < 0)
				{
					// No stale region: release the unit so squads can claim it.
					scoutTargets.Remove(actor);
					scouts.Remove(scout);
					continue;
				}

				taken.Add(region);
				scoutTargets[actor] = (region, tick);
				bot.QueueOrder(new Order("Move", actor, Target.FromCell(world, regions.CenterOf(region)), false));
			}
		}

		void ClaimScouts(IBot bot)
		{
			if (Info.ScoutUnitTypes.Count == 0 || scouts.Count >= Info.MaxScouts)
				return;

			var claimedAny = false;
			if (idlePool != null)
			{
				foreach (var candidate in idlePool.ToArray())
				{
					if (scouts.Count >= Info.MaxScouts)
						break;

					var actor = candidate.Actor;
					if (unitCannotBeOrdered(actor) || !Info.ScoutUnitTypes.Contains(actor.Info.Name))
						continue;

					if (scouts.Any(u => u.Actor == actor))
						continue;

					scouts.Add(candidate);
					idlePool.Remove(candidate);
					claimedAny = true;
					AIUtils.BotDebug("AI ({0}): claimed scout {1}", player.ClientIndex, actor);
				}
			}

			if (!claimedAny && scouts.Count < Info.MaxScouts)
				RequestScout(bot);
		}

		void RequestScout(IBot bot)
		{
			if (unitBuilders == null || unitBuilders.Length == 0)
				return;

			foreach (var name in Info.ScoutUnitTypes.OrderBy(n => n, StringComparer.Ordinal))
			{
				var builder = unitBuilders.FirstOrDefault(b => b.RequestedProductionCount(bot, name) == 0);
				if (builder == null)
					continue;

				// Unloaded ContentPacks leave their actor names out of Rules.Actors;
				// indexing a missing key throws, so TryGetValue is load-bearing.
				if (!world.Map.Rules.Actors.TryGetValue(name, out var actorInfo))
					continue;

				var buildable = actorInfo?.TraitInfoOrDefault<BuildableInfo>();
				if (buildable == null)
					continue;

				if (!buildable.Queue.Any(q => CAAIUtils.FindQueues(player, q).Any(pq => pq.BuildableItems().Any(b => b.Name == name))))
					continue;

				builder.RequestUnitProduction(bot, name);
				return;
			}
		}

		int ChooseScoutTarget(RegionMemory regions, CPos from, int tick, HashSet<int> taken)
		{
			return PickScoutRegion(regions, from, taken,
				i => Staleness(regions, i, tick), i => Interest(regions, i), DangerAt);
		}

		internal static int PickScoutRegion(RegionMemory regions, CPos from, IReadOnlySet<int> taken,
			Func<int, int> staleness, Func<int, int> interest, Func<int, int> danger)
		{
			var best = -1;
			var bestScore = long.MinValue;
			var cellCount = regions.CellCount;

			for (var i = 0; i < cellCount; i++)
			{
				if (taken.Contains(i))
					continue;

				var stale = staleness(i);
				if (stale <= 0)
					continue;

				var score = (long)stale * (1000 + interest(i)) - danger(i) - DistancePenalty(regions, from, i);
				if (score > bestScore || (score == bestScore && best >= 0 && i < best))
				{
					best = i;
					bestScore = score;
				}
			}

			return best;
		}

		bool AnyStaleRegion(RegionMemory regions, int tick)
		{
			for (var i = 0; i < regions.CellCount; i++)
			{
				if (Staleness(regions, i, tick) > 0)
					return true;
			}

			return false;
		}

		int Staleness(RegionMemory regions, int index, int tick)
		{
			var stalest = 0;
			foreach (var enemyRegions in regions.ByEnemy.Values)
			{
				var region = index < enemyRegions.Length ? enemyRegions[index] : null;
				var seenTick = region?.LastSeenTick ?? 0;
				var everSeen = region?.EverSeen ?? false;
				stalest = Math.Max(stalest, everSeen ? tick - seenTick : tick + Info.StaleAfterTicks);
			}

			if (regions.ByEnemy.Count == 0)
				stalest = tick + Info.StaleAfterTicks;

			return stalest;
		}

		int Interest(RegionMemory regions, int index)
		{
			var interest = 0;
			foreach (var enemyRegions in regions.ByEnemy.Values)
			{
				var region = index < enemyRegions.Length ? enemyRegions[index] : null;
				if (region != null)
					interest += (region.ArmyValue + region.DefenceValue + region.EconomyValue) * Info.RememberedValueWeight;
			}

			if (resourceMap != null && IsResourceSite(regions, index))
				interest += Info.ResourceSiteBonus;

			return interest;
		}

		bool IsResourceSite(RegionMemory regions, int index)
		{
			var count = resourceMap.GetIndicesLength();
			for (var i = 0; i < count; i++)
			{
				var indice = resourceMap.GetIndice(i);
				if (indice.ResourceCellsCount > 0 && regions.IndexOf(indice.IndiceCenter) == index)
					return true;
			}

			return false;
		}

		int DangerAt(int index)
		{
			if (!dangerByRegion.TryGetValue(index, out var mark))
				return 0;

			var age = world.WorldTick - mark.Tick;
			if (age > Info.DangerDecayTicks)
				return 0;

			return mark.Value;
		}

		static int DistancePenalty(RegionMemory regions, CPos from, int index)
		{
			return (regions.CenterOf(index) - from).Length / 64;
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (e.Attacker == null || !e.Attacker.Info.HasTraitInfo<IOccupySpaceInfo>() || !scouts.Any(u => u.Actor == self))
				return;

			var regions = player.PlayerActor.TraitOrDefault<MasterAiBotModule>()?.Situation?.Regions;
			if (regions == null)
				return;

			var value = e.Attacker.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			var index = regions.IndexOf(e.Attacker.Location);
			dangerByRegion.TryGetValue(index, out var existing);
			dangerByRegion[index] = (existing.Value + Math.Max(1, value), world.WorldTick);
		}

		// The 6c risk gate reads scout-loss marks through this CA-side interface;
		// a region that killed a scout counts its attacker's cost as threat.
		int IBotRegionThreatProvider.RememberedEnemyThreatAt(CPos cell)
		{
			var regions = player.PlayerActor.TraitOrDefault<MasterAiBotModule>()?.Situation?.Regions;
			return IsTraitDisabled || regions == null ? 0 : DangerAt(regions.IndexOf(cell));
		}
	}
}
