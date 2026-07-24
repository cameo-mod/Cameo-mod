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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Renders a camera-relative nuclear exposure flash that brightens toward the blast and darkens the surrounding world.")]
	public class NuclearFlashRendererInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new NuclearFlashRenderer(); }
	}

	public sealed class NuclearFlashRenderer : RenderPostProcessPassBase, ITick
	{
		WPos position;
		Color color;
		int duration;
		int remainingTicks;
		float radius;
		float brightness;
		float darkness;

		public NuclearFlashRenderer()
			: base("nuclearflash", PostProcessPassType.AfterWorld) { }

		public void Enable(WPos position, Color color, int duration, float radius, float brightness, float darkness)
		{
			this.position = position;
			this.color = color;
			this.duration = Math.Max(1, duration);
			this.radius = radius;
			this.brightness = brightness;
			this.darkness = darkness;
			remainingTicks = this.duration;
		}

		void ITick.Tick(Actor self)
		{
			if (remainingTicks > 0)
				remainingTicks--;
		}

		protected override bool Enabled => remainingTicks > 0;

		protected override void PrepareRender(WorldRenderer wr, IShader shader)
		{
			var renderer = Game.Renderer;
			var downscale = renderer.WorldDownscaleFactor;
			var screen = wr.ScreenPxPosition(position);
			var topLeft = wr.Viewport.TopLeft;
			var fbWidth = (float)renderer.WorldFrameBufferSize.Width;
			var fbHeight = (float)renderer.WorldFrameBufferSize.Height;

			var x = (float)(screen.X - topLeft.X) * downscale;
			var y = (float)(screen.Y - topLeft.Y) * downscale;

			// Keep off-screen blasts directional without allowing a distant world position to move the
			// light so far away that no spill reaches the viewport.
			x = Math.Clamp(x, -fbWidth * 0.2f, fbWidth * 1.2f);
			y = Math.Clamp(y, -fbHeight * 0.2f, fbHeight * 1.2f);

			var linear = (float)remainingTicks / duration;
			var strength = linear * linear * (3f - 2f * linear);

			shader.SetVec("LightPosition", x, y);
			shader.SetVec("LightRadius", Math.Min(fbWidth, fbHeight) * radius);
			shader.SetVec("LightColor", color.R / 255f, color.G / 255f, color.B / 255f);
			shader.SetVec("Brightness", brightness * strength);
			shader.SetVec("Darkness", darkness * strength);
		}
	}
}
