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

using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Disposes this actor immediately after it spawns if another actor with this trait already",
		"occupies the same cell. Lets transient effects (e.g. ground fire spawned in bursts) cap to one",
		"per tile to save performance, WITHOUT using Immobile.OccupiesSpace (which would block movement).")]
	public class OneActorPerCellInfo : TraitInfo, Requires<IOccupySpaceInfo>
	{
		public override object Create(ActorInitializer init) { return new OneActorPerCell(); }
	}

	public class OneActorPerCell : INotifyAddedToWorld
	{
		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var cell = self.Location;

			// Frame-end spawn tasks are processed sequentially, so the first actor placed on a cell is
			// already registered when later ones run this check - the first one wins, the rest remove
			// themselves. ActorsHavingTrait is used (not ActorMap.GetActorsAt) so it works even when the
			// actor does not occupy space.
			foreach (var other in self.World.ActorsHavingTrait<OneActorPerCell>())
			{
				if (other == self || other.IsDead || !other.IsInWorld)
					continue;

				if (other.Location == cell)
				{
					self.World.AddFrameEndTask(w =>
					{
						if (!self.IsDead && self.IsInWorld)
							self.Dispose();
					});

					return;
				}
			}
		}
	}
}
