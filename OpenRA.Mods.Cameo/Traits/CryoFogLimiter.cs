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
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Global cap on the number of concurrent cryo-fog particles spawned by CryoFogEmitter. Place on the world actor.")]
	public class CryoFogLimiterInfo : TraitInfo
	{
		[Desc("Maximum number of cryo-fog particles alive at once across all emitters.")]
		public readonly int Max = 10;

		public override object Create(ActorInitializer init) { return new CryoFogLimiter(this); }
	}

	public class CryoFogLimiter : ITick
	{
		readonly CryoFogLimiterInfo info;

		// Tick at which each reserved slot frees up. Driven by a deterministic world-tick
		// counter, so the active count is identical on every client (multiplayer-safe).
		readonly List<int> reservedUntil = [];
		int tick;

		public CryoFogLimiter(CryoFogLimiterInfo info) { this.info = info; }

		public int Active => reservedUntil.Count;

		// Reserve a slot for durationTicks if the global budget has room. The reservation is
		// conservative (callers pass the particle's maximum lifetime), so the on-screen count
		// never exceeds Max.
		public bool TryReserve(int durationTicks)
		{
			if (reservedUntil.Count >= info.Max)
				return false;

			reservedUntil.Add(tick + durationTicks);
			return true;
		}

		void ITick.Tick(Actor self)
		{
			tick++;

			for (var i = reservedUntil.Count - 1; i >= 0; i--)
				if (reservedUntil[i] <= tick)
					reservedUntil.RemoveAt(i);
		}
	}
}
