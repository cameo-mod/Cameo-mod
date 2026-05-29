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
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Spawn a sprite that pulsates between scales during its lifetime.")]
	public class CreateEffectPulsateWarhead : CreateEffectWarhead
	{
		[Desc("Scale at the start of the animation.")]
		public readonly float StartScale = 0.1f;

		[Desc("Scale reached at the apex of the pulse.")]
		public readonly float PeakScale = 1f;

		[Desc("Scale at the end of the pulse (defaults to StartScale).")]
		public readonly float EndScale = 0.1f;

		[Desc("Ticks spent scaling from StartScale to PeakScale.")]
		public readonly int GrowTicks = 8;

		[Desc("Ticks to remain at PeakScale before shrinking.")]
		public readonly int HoldTicks = 0;

		[Desc("Ticks spent scaling from PeakScale to EndScale.")]
		public readonly int ShrinkTicks = 8;

		[Desc("If true, repeats the grow/hold/shrink cycle until the animation ends.")]
		public readonly bool Loop = false;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid)
				return;

			var firedBy = args.SourceActor;
			var pos = target.CenterPosition;
			var world = firedBy.World;
			var actorAtImpact = ImpactActors ? ActorTypeAtImpact(world, pos, firedBy) : ImpactActorType.None;

			if (actorAtImpact == ImpactActorType.Invalid)
				return;

			if (actorAtImpact == ImpactActorType.None && !IsValidAgainstTerrain(world, pos))
				return;

			var explosion = Explosions.RandomOrDefault(world.LocalRandom);
			if (Image != null && explosion != null)
			{
				if (Inaccuracy.Length > 0)
					pos += WVec.FromPDF(world.SharedRandom, 2) * Inaccuracy.Length / 1024;

				if (ForceDisplayAtGroundLevel)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					pos -= new WVec(0, 0, dat.Length);
				}

				var palette = ExplosionPalette;
				if (UsePlayerPalette)
					palette += firedBy.Owner.InternalName;

				world.AddFrameEndTask(w => w.Add(new PulsatingSpriteEffect(
					pos, w, Image, explosion, palette,
					StartScale, PeakScale, EndScale,
					GrowTicks, HoldTicks, ShrinkTicks, Loop)));
			}

			var impactSound = ImpactSounds.RandomOrDefault(world.LocalRandom);
			if (impactSound != null && world.LocalRandom.Next(0, 100) < ImpactSoundChance
				&& (AudibleThroughFog || (!world.ShroudObscures(pos) && !world.FogObscures(pos))))
				Game.Sound.Play(SoundType.World, impactSound, pos, Volume);
		}
	}
}
