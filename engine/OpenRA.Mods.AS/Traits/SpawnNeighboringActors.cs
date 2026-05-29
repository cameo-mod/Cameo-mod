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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor places other actors around itself, which keep connected as in they get removed when the parent is sold or destroyed.")]
	public class SpawnNeighboringActorsInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[ActorReference]
		[Desc("Types of actors to place. If multiple are defined, a random one will be selected for each actor spawned.")]
		public readonly HashSet<string> ActorTypes = [];

		[FieldLoader.Require]
		[Desc("Locations to spawn the actors relative to the origin (top-left for buildings) of this actor.")]
		public readonly CVec[] Locations = [];

		[Desc("Initial delay to create the actors.")]
		public readonly int InitialDelay = 1;

		[Desc("Recreate the actors, if they are destroyed after this much of time.")]
		public readonly int RecreationInterval = 2500;

		[Desc("Remove the actors if the trait gets disabled.")]
		public readonly bool RemoveOnDisable = true;

		[Desc("Kill the actors if the trait gets disabled or parent is killed, else dispose.")]
		public readonly bool KillOnRemove = true;

		[Desc("Types of damage this actor explodes with due to being killed. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Ignore placement rules")]
		public readonly bool PlacementIgnoresOccupiesSpace = true;

		[Desc("Skip make animations for spawned actors")]
		public readonly bool SkipMakeAnimations = true;

		[Desc("Should actors be spawned after the player has been defeated?")]
		public readonly bool SpawnAfterDefeat = true;

		public override object Create(ActorInitializer init) { return new SpawnNeighboringActors(this, init.Self); }
	}

	public class SpawnNeighboringActors : PausableConditionalTrait<SpawnNeighboringActorsInfo>, INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing, INotifySold, ITick, ISync
	{
		readonly List<Actor> actors = [];

		[Sync]
		int ticks;

		bool spawned = false;

		public SpawnNeighboringActors(SpawnNeighboringActorsInfo info, Actor self)
			: base(info)
		{
			ticks = Info.InitialDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled || (Info.RecreationInterval <= 0 && spawned) )
				return;

			if (--ticks < 0)
			{
				ticks = Info.RecreationInterval;
				SpawnActors(self);
				spawned = true;
			}
		}

		public void SpawnActors(Actor self)
		{
			if (IsTraitDisabled)
				return;

			actors.RemoveAll(a => a.IsDead || a.Disposed);

			if (actors.Count >= Info.Locations.Length)
				return;

			foreach (var offset in Info.Locations)
			{
				if (actors.Count >= Info.Locations.Length)
					return;

				var cell = self.Location + offset;

				bool positionOccupied = actors.Any(a => a.Location == cell);

				if (positionOccupied)
					continue;

				self.World.AddFrameEndTask(w =>
				{
					var actorType = Info.ActorTypes.Random(self.World.SharedRandom).ToLowerInvariant();

					var td = new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new LocationInit(cell)
					};

					if (Info.SkipMakeAnimations)
						td.Add(new SkipMakeAnimsInit());

					var actor = w.CreateActor(true, actorType, td);

					// Check placement rules if needed
					if (!Info.PlacementIgnoresOccupiesSpace)
					{
						var ai = self.World.Map.Rules.Actors[actorType];
						var ip = ai.TraitInfo<IPositionableInfo>();
						if (!ip.CanEnterCell(self.World, null, cell))
						{
							actor.Dispose();
							return;
						}
					}

					actors.Add(actor);
				});
			}
		}

		public void RemoveActors()
		{
			foreach (var actor in actors)
			{
				if (Info.KillOnRemove)
					actor.Kill(actor, Info.DamageTypes);
				else
					actor.Dispose();
			}

			actors.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var actor in actors)
				actor.ChangeOwnerSync(newOwner);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			RemoveActors();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			RemoveActors();
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			RemoveActors();
		}

		protected override void TraitEnabled(Actor self)
		{
			ticks = Info.InitialDelay;

			// Initial spawn wenn kein Delay
			if (Info.InitialDelay <= 0)
				SpawnActors(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			ticks = Info.InitialDelay;

			if (Info.RemoveOnDisable)
				RemoveActors();
		}
	}
}
