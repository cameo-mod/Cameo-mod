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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Warheads
{
	[Desc("Scatters an immobile actor across the cells in the impact area, each with an independent chance. "
		+ "Intended for spawning ground fire from superweapons. Don't use this with buildings.")]
	public class SpawnActorInAreaWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[FieldLoader.Require]
		[Desc("Immobile actor to spawn in affected cells.")]
		public readonly string Actor = null;

		[Desc("Cell radius around the impact to scatter the actor over.")]
		public readonly int Range = 3;

		[Desc("Percentage chance (0-100) that each affected cell spawns the actor.")]
		public readonly int Chance = 50;

		[Desc("Owner of the spawned actor. Allowed keywords: 'Attacker' and 'InternalName'.")]
		public readonly ASOwnerType OwnerType = ASOwnerType.InternalName;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			var actorInfo = rules.Actors[Actor.ToLowerInvariant()];

			if (actorInfo.TraitInfoOrDefault<BuildingInfo>() != null)
				throw new YamlException($"SpawnActorInAreaWarhead cannot be used to spawn building actor '{Actor}'!");

			if (actorInfo.TraitInfoOrDefault<ImmobileInfo>() == null)
				throw new YamlException($"SpawnActorInAreaWarhead requires '{Actor}' to have the Immobile trait!");
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (firedBy == null || !target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var world = firedBy.World;
			var map = world.Map;
			var targetCell = map.CellContaining(target.CenterPosition);
			var actorName = Actor.ToLowerInvariant();

			var owner = OwnerType == ASOwnerType.Attacker
				? firedBy.Owner
				: world.Players.First(p => p.InternalName == InternalOwner);

			// Runs in simulation on every client; World.SharedRandom keeps the per-cell rolls deterministic (MP-safe).
			foreach (var cell in map.FindTilesInCircle(targetCell, Range))
			{
				if (world.SharedRandom.Next(100) >= Chance)
					continue;

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
