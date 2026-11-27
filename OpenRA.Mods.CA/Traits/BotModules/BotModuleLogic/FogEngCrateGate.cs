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

using System;
using OpenRA;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// FOG-ENGCRATE-01: the fog test for generic-bot capture, bridge-hut and crate targets. With the row on, a target counts when the bot sees it now
	/// or the player's frozen layer holds a memory of it. With no frozen memory (no fog provider) only what is seen now counts: fail closed.
	/// Classic bots never call this, so their scans stay byte-identical.
	/// </summary>
	public static class FogEngCrateGate
	{
		public const int ReserveRadiusMin = 15;
		public const int ReserveRadiusMax = 40;

		/// <summary>The pure rule, free of world state so it can be tested: seen now, or remembered.</summary>
		public static bool Admits(bool seenNow, bool remembered) => seenNow || remembered;

		/// <summary>The player's frozen memory of the actor (last sighting). Null layer = no fog provider = no memory.</summary>
		public static bool Remembered(OpenRA.Player player, Actor actor) =>
			player.FrozenActorLayer != null && player.FrozenActorLayer.FromID(actor.ActorID) != null;

		/// <summary>Seen now, or remembered in the frozen layer.</summary>
		public static bool Admits(OpenRA.Player player, Actor actor) =>
			Admits(actor.CanBeViewedByPlayer(player), Remembered(player, actor));

		/// <summary>Clamps the crate reserve radius. The learned knob (LA02 seam) is not on this base; the prior 25 is inside 15..40, so the default is unchanged.</summary>
		public static int ClampReserveRadius(int cells) => Math.Clamp(cells, ReserveRadiusMin, ReserveRadiusMax);
	}
}
