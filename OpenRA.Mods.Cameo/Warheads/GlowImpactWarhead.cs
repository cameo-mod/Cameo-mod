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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Warheads
{
	[Desc("Registers a screen-space glow flash at the impact position. Purely visual, no damage.")]
	public class GlowImpactWarhead : Warhead
	{
		[Desc("Color of the glow.")]
		public readonly Color Color = Color.FromArgb(255, 255, 102, 0);

		[Desc("Scale multiplier on the renderer's GlowRadius and GlowIntensity.")]
		public readonly float Scale = 1f;

		[Desc("Number of render frames to fade out over. 0 = single-frame flash.")]
		public readonly int FadeFrames = 90;

		[Desc("Number of render frames to fade in over. 0 = instant on.")]
		public readonly int FadeInFrames = 0;

		public override bool IsValidAgainst(Actor victim, Actor firedBy) => true;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (!Game.Settings.Graphics.LaserGlow)
				return;

			args.SourceActor.World.WorldActor.TraitOrDefault<GlowRenderer>()
				?.RegisterGlow(args.ImpactPosition, args.ImpactPosition, Color, Scale, FadeFrames, FadeInFrames);
		}
	}
}
