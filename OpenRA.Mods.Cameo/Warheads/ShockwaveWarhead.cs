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
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Warheads
{
	[Desc("Registers a screen-space shockwave-lens distortion at the impact position. Purely visual, no damage.")]
	public class ShockwaveWarhead : Warhead
	{
		[Desc("Scale multiplier on the renderer's MaxRadius and Strength.")]
		public readonly float Scale = 1f;

		[Desc("Number of render frames the ring expands and fades over. Sub-second expand-and-vanish.")]
		public readonly int FadeFrames = 24;

		[Desc("Number of render frames to ease the intensity in over. 0 = instant on.")]
		public readonly int FadeInFrames = 0;

		public override bool IsValidAgainst(Actor victim, Actor firedBy) => true;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (!Game.Settings.Graphics.Shockwave)
				return;

			// Use target.CenterPosition, not args.ImpactPosition: the latter is only populated by real
			// projectiles. Superweapons detonate via the projectile-less WeaponInfo.Impact(target, firedBy)
			// overload, which leaves ImpactPosition at (0,0,0). CreateEffectWarhead uses CenterPosition too.
			args.SourceActor.World.WorldActor.TraitOrDefault<ShockwaveDistortionRenderer>()
				?.RegisterShockwave(target.CenterPosition, Scale, FadeFrames, FadeInFrames);
		}
	}
}
