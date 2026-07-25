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

using OpenRA.GameRules;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Warheads
{
	[Desc("Triggers the camera-relative nuclear exposure effect on the world actor.")]
	public class NuclearFlashEffectWarhead : Warhead
	{
		public readonly Color Color = Color.FromArgb(255, 255, 238, 184);
		public readonly int Duration = 40;
		public readonly float Radius = 0.55f;
		public readonly float Brightness = 1.15f;
		public readonly float Darkness = 0.4f;

		public override bool IsValidAgainst(Actor victim, Actor firedBy) => true;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			args.SourceActor.World.WorldActor.TraitOrDefault<NuclearFlashRenderer>()
				?.Enable(target.CenterPosition, Color, Duration, Radius, Brightness, Darkness);
		}
	}
}
