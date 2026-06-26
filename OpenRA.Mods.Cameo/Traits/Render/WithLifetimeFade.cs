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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Rolls a lifetime, removes the actor when it expires (like KillsSelf with RemoveInstead),",
		"and fades the actor's sprites in at spawn and out just before removal so it does not pop",
		"abruptly. Intended for short-lived cosmetic actors such as ground fire. Use INSTEAD of",
		"KillsSelf (the lifetime is rolled here so the fade-out can be timed against it).")]
	public class WithLifetimeFadeInfo : TraitInfo
	{
		[Desc("Lifetime in ticks before the actor is removed. Two values pick a random range per actor.")]
		public readonly ImmutableArray<int> Duration = [125, 250];

		[Desc("Ticks to fade in from transparent at spawn (5 ticks ~= 200ms at the default game speed).")]
		public readonly int FadeInTicks = 5;

		[Desc("Ticks to fade out to transparent before removal.")]
		public readonly int FadeOutTicks = 5;

		public override object Create(ActorInitializer init) { return new WithLifetimeFade(init.Self, this); }
	}

	public class WithLifetimeFade : INotifyAddedToWorld, ITick, IRenderModifier
	{
		readonly WithLifetimeFadeInfo info;
		readonly int lifetime;
		int age;

		public WithLifetimeFade(Actor self, WithLifetimeFadeInfo info)
		{
			this.info = info;

			// Rolled on the synced RNG (mirrors KillsSelf) so the lifetime is deterministic in MP.
			lifetime = OpenRA.Mods.Common.Util.RandomInRange(self.World.SharedRandom, info.Duration);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (lifetime == 0)
				self.World.AddFrameEndTask(w => self.Dispose());
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld || self.IsDead)
				return;

			if (++age >= lifetime)
				self.World.AddFrameEndTask(w => { if (!self.IsDead) self.Dispose(); });
		}

		// Render-only: the alpha ramp is cosmetic and never affects the simulation.
		float CurrentAlpha()
		{
			var a = 1f;
			if (info.FadeInTicks > 0 && age < info.FadeInTicks)
				a = (float)age / info.FadeInTicks;

			var remaining = lifetime - age;
			if (info.FadeOutTicks > 0 && remaining < info.FadeOutTicks)
				a = Math.Min(a, (float)remaining / info.FadeOutTicks);

			return Math.Clamp(a, 0f, 1f);
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			var a = CurrentAlpha();
			if (a >= 1f)
				return r;

			return Fade(r, a);
		}

		// Unlike WithAlpha this also fades IsDecoration renderables: the ground-fire overlay is a
		// decoration, so skipping decorations would leave it un-faded.
		static IEnumerable<IRenderable> Fade(IEnumerable<IRenderable> r, float a)
		{
			foreach (var renderable in r)
			{
				if (renderable is IModifyableRenderable m)
					yield return m.WithAlpha(a);
				else
					yield return renderable;
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
