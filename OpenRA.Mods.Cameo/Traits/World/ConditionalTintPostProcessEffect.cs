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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Conditionally applies a global tint post-process pass.")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class ConditionalTintPostProcessEffectInfo : ConditionalTraitInfo
	{
		public readonly float Red = 1f;
		public readonly float Green = 1f;
		public readonly float Blue = 1f;
		public readonly float Ambient = 1f;

		public override object Create(ActorInitializer init) { return new ConditionalTintPostProcessEffect(this); }
	}

	public class ConditionalTintPostProcessEffect : ConditionalTrait<ConditionalTintPostProcessEffectInfo>, IRenderPostProcessPass, INotifyActorDisposing
	{
		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<RenderPostProcessPassVertex> buffer;

		public ConditionalTintPostProcessEffect(ConditionalTintPostProcessEffectInfo info)
			: base(info)
		{
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new RenderPostProcessPassShaderBindings("tint"));
			var vertices = new RenderPostProcessPassVertex[]
			{
				new(-1, -1),
				new(1, -1),
				new(1, 1),
				new(1, 1),
				new(-1, 1),
				new(-1, -1)
			};

			buffer = renderer.CreateVertexBuffer(vertices, false);
		}

		PostProcessPassType IRenderPostProcessPass.Type => PostProcessPassType.AfterActors;
		bool IRenderPostProcessPass.Enabled => !IsTraitDisabled;

		void IRenderPostProcessPass.Draw(WorldRenderer wr)
		{
			shader.SetTexture("SourceTexture", Game.Renderer.GetRenderBufferSnapshot());
			shader.SetVec("Tint", Info.Ambient * Info.Red, Info.Ambient * Info.Green, Info.Ambient * Info.Blue);
			shader.PrepareRender();
			renderer.DrawBatch(buffer, shader, 0, 6, PrimitiveType.TriangleList);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			buffer.Dispose();
		}
	}
}
