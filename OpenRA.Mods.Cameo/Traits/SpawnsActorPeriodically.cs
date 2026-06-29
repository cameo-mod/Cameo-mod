#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Periodically scatters an immobile actor in the cells around this actor while the trait is enabled.",
		"Gate it with RequiresCondition (e.g. a damage-state condition) to make a wounded unit leave ground fire.",
		"Don't use this to spawn buildings.")]
	public class SpawnsActorPeriodicallyInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Immobile actor to spawn.")]
		public readonly string Actor = null;

		[Desc("Ticks between spawn attempts. Two values pick a random range each time.")]
		public readonly ImmutableArray<int> Interval = [16, 32];

		[Desc("Ticks before the first spawn after the trait is enabled. Two values pick a random range.")]
		public readonly ImmutableArray<int> InitialDelay = [0];

		[Desc("Cell radius around this actor to scatter the spawned actor over. 0 spawns under the actor only.")]
		public readonly int Range = 1;

		[Desc("Spawn attempts per interval. Each attempt rolls Chance and picks a random cell within Range.")]
		public readonly int Count = 1;

		[Desc("Percentage chance (0-100) that each attempt spawns the actor.")]
		public readonly int Chance = 100;

		[Desc("Owner of the spawned actor. Allowed keywords: 'Attacker' (this actor's owner) and 'InternalName'.")]
		public readonly ASOwnerType OwnerType = ASOwnerType.InternalName;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		public override object Create(ActorInitializer init) { return new SpawnsActorPeriodically(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			var actorInfo = rules.Actors[Actor.ToLowerInvariant()];

			if (actorInfo.TraitInfoOrDefault<BuildingInfo>() != null)
				throw new YamlException($"SpawnsActorPeriodically cannot be used to spawn building actor '{Actor}'!");

			if (actorInfo.TraitInfoOrDefault<ImmobileInfo>() == null)
				throw new YamlException($"SpawnsActorPeriodically requires '{Actor}' to have the Immobile trait!");
		}
	}

	public class SpawnsActorPeriodically : ConditionalTrait<SpawnsActorPeriodicallyInfo>, ITick
	{
		readonly string actorName;
		int delay;

		public SpawnsActorPeriodically(Actor self, SpawnsActorPeriodicallyInfo info)
			: base(info)
		{
			actorName = info.Actor.ToLowerInvariant();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Re-rolled each time the trait turns on (e.g. the unit drops below half health again).
			delay = Util.RandomInRange(self.World.SharedRandom, Info.InitialDelay);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld || self.IsDead)
				return;

			if (--delay >= 0)
				return;

			delay = Util.RandomInRange(self.World.SharedRandom, Info.Interval);

			var world = self.World;
			var map = world.Map;
			var owner = Info.OwnerType == ASOwnerType.Attacker
				? self.Owner
				: world.Players.First(p => p.InternalName == Info.InternalOwner);

			var cells = map.FindTilesInCircle(self.Location, Info.Range).ToList();
			if (cells.Count == 0)
				return;

			for (var i = 0; i < Info.Count; i++)
			{
				// World.SharedRandom keeps the rolls deterministic across clients (MP-safe).
				if (world.SharedRandom.Next(100) >= Info.Chance)
					continue;

				var cell = cells[world.SharedRandom.Next(cells.Count)];

				// Skip cells that already hold this actor type; OneActorPerCell would dispose the duplicate anyway.
				if (world.ActorMap.GetActorsAt(cell).Any(a => a.Info.Name == actorName))
					continue;

				var spawnCell = cell;
				world.AddFrameEndTask(w =>
				{
					var td = new TypeDictionary
					{
						new OwnerInit(owner),
						new LocationInit(spawnCell),
					};

					w.CreateActor(true, actorName, td);
				});
			}
		}
	}
}
