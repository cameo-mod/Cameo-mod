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
		[Desc("Color of the glow. Ignored when UsePlayerColor is true.")]
		public readonly Color Color = Color.FromArgb(255, 255, 102, 0);

		[Desc("Use the firing player's color instead of Color.")]
		public readonly bool UsePlayerColor = false;

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

			// args.ImpactPosition is (0,0,0) for projectile-less superweapon detonations (chronoshift,
			// nukes); fall back to the target centre in that case, like HeatDistortionWarhead does.
			var pos = args.ImpactPosition != WPos.Zero ? args.ImpactPosition : target.CenterPosition;

			var color = UsePlayerColor && args.SourceActor != null ? args.SourceActor.Owner.Color : Color;

			args.SourceActor.World.WorldActor.TraitOrDefault<GlowRenderer>()
				?.RegisterGlow(pos, pos, color, Scale, FadeFrames, FadeInFrames);
		}
	}
}
