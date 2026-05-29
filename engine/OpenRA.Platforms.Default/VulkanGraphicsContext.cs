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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Primitives;
using SDL2;
using Veldrid;
using Veldrid.SPIRV;
using Rectangle = OpenRA.Primitives.Rectangle;
using VeldridBuffer = Veldrid.DeviceBuffer;
using VeldridFramebuffer = Veldrid.Framebuffer;
using VeldridShader = Veldrid.Shader;
using VeldridTexture = Veldrid.Texture;

namespace OpenRA.Platforms.Default
{
	sealed class VulkanGraphicsContext : ThreadAffine, IGraphicsContext
	{
		readonly Sdl2PlatformWindow window;
		readonly CommandList commandList;
		readonly VulkanTexture whiteTexture;
		bool disposed;

		internal GraphicsDevice Device { get; }
		internal ResourceFactory Factory { get; }
		internal VeldridFramebuffer CurrentFramebuffer { get; private set; }
		internal VeldridTexture CurrentColorTarget => CurrentFramebuffer.ColorTargets[0].Target;
		internal OutputDescription CurrentOutputDescription => CurrentFramebuffer.OutputDescription;
		internal CommandList CommandList => commandList;
		internal BlendMode CurrentBlendMode { get; private set; } = BlendMode.None;
		internal bool DepthEnabled { get; private set; }
		internal bool ScissorEnabled { get; private set; }
		internal VulkanVertexBuffer CurrentVertexBuffer { get; private set; }
		internal VulkanIndexBuffer CurrentIndexBuffer { get; private set; }
		internal VulkanShader CurrentShader { get; private set; }
		internal Sampler PointSampler => Device.PointSampler;
		internal Sampler LinearSampler => Device.LinearSampler;
		internal VulkanTexture WhiteTexture => whiteTexture;

		public string GLVersion => $"Vulkan {Device.ApiVersion} ({Device.DeviceName})";
		public bool RequiresRenderTargetYFlip => false;

		public VulkanGraphicsContext(Sdl2PlatformWindow window, bool vsync)
		{
			this.window = window;

			var options = new GraphicsDeviceOptions(
				Game.Settings.Debug.DisplayDeveloperSettings,
				PixelFormat.D24_UNorm_S8_UInt,
				vsync,
				ResourceBindingModel.Improved,
				true,
				// Match Vulkan's native top-left framebuffer orientation. OpenRA's
				// UI/window coordinates are also top-left, so using native Vulkan
				// clip-space avoids the extra OpenGL-style render-target flip.
				false,
				false);

			var surfaceSize = window.SurfaceSize;
			var swapchainSource = CreateSwapchainSource(window.Window);
			var swapchain = new SwapchainDescription(swapchainSource, (uint)surfaceSize.Width, (uint)surfaceSize.Height, PixelFormat.D24_UNorm_S8_UInt, vsync, false);
			Device = GraphicsDevice.CreateVulkan(options, swapchain);
			Factory = Device.ResourceFactory;
			commandList = Factory.CreateCommandList();
			CurrentFramebuffer = Device.SwapchainFramebuffer;
			commandList.Begin();
			BindFramebuffer(null);

			whiteTexture = new VulkanTexture(this);
			whiteTexture.SetData([255, 255, 255, 255], 1, 1);
		}

		static SwapchainSource CreateSwapchainSource(IntPtr sdlWindow)
		{
			var info = default(SDL.SDL_SysWMinfo);
			SDL.SDL_VERSION(out info.version);
			if (SDL.SDL_GetWindowWMInfo(sdlWindow, ref info) == SDL.SDL_bool.SDL_FALSE)
				throw new InvalidOperationException($"Can not query SDL window system information. (Error: {SDL.SDL_GetError()})");

			return info.subsystem switch
			{
				SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS => SwapchainSource.CreateWin32(info.info.win.window, info.info.win.hinstance),
				SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11 => SwapchainSource.CreateXlib(info.info.x11.display, info.info.x11.window),
				SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND => SwapchainSource.CreateWayland(info.info.wl.display, info.info.wl.surface),
				SDL.SDL_SYSWM_TYPE.SDL_SYSWM_COCOA => SwapchainSource.CreateNSWindow(info.info.cocoa.window),
				SDL.SDL_SYSWM_TYPE.SDL_SYSWM_UIKIT => SwapchainSource.CreateUIView(info.info.uikit.window),
				SDL.SDL_SYSWM_TYPE.SDL_SYSWM_ANDROID => SwapchainSource.CreateAndroidSurface(info.info.android.window, IntPtr.Zero),
				_ => throw new PlatformNotSupportedException($"Cannot create a Vulkan swapchain source for SDL subsystem {info.subsystem}.")
			};
		}

		internal void VerifyOwnerThread() { VerifyThreadAffinity(); }

		internal void SetVertexBuffer(VulkanVertexBuffer buffer)
		{
			VerifyThreadAffinity();
			CurrentVertexBuffer = buffer;
			commandList.SetVertexBuffer(0, buffer.Buffer);
		}

		internal void SetIndexBuffer(VulkanIndexBuffer buffer)
		{
			VerifyThreadAffinity();
			CurrentIndexBuffer = buffer;
			commandList.SetIndexBuffer(buffer.Buffer, IndexFormat.UInt32);
		}

		internal void SetShader(VulkanShader shader)
		{
			VerifyThreadAffinity();
			CurrentShader = shader;
		}

		internal void BindFramebuffer(VulkanFrameBuffer framebuffer)
		{
			SetFramebuffer(framebuffer?.Framebuffer ?? Device.SwapchainFramebuffer);
		}

		internal void RestoreFramebuffer(VeldridFramebuffer framebuffer)
		{
			SetFramebuffer(framebuffer ?? Device.SwapchainFramebuffer);
		}

		void SetFramebuffer(VeldridFramebuffer framebuffer)
		{
			VerifyThreadAffinity();
			CurrentFramebuffer = framebuffer;
			commandList.SetFramebuffer(CurrentFramebuffer);
			commandList.SetFullViewport(0);
			commandList.SetFullScissorRect(0);
		}

		internal void ClearColor(Color color)
		{
			commandList.ClearColorTarget(0, new RgbaFloat(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
		}

		internal void SubmitAndRestart(bool waitForIdle)
		{
			VerifyThreadAffinity();
			commandList.End();
			Device.SubmitCommands(commandList);
			if (waitForIdle)
				Device.WaitForIdle();

			commandList.Begin();
			commandList.SetFramebuffer(CurrentFramebuffer);
			commandList.SetFullViewport(0);
			if (!ScissorEnabled)
				commandList.SetFullScissorRect(0);
		}

		internal void UpdateBuffer(VeldridBuffer buffer, uint offset, IntPtr source, uint size)
		{
			VerifyThreadAffinity();
			commandList.UpdateBuffer(buffer, offset, source, size);
		}

		internal void UpdateBuffer<T>(VeldridBuffer buffer, uint offset, T[] source) where T : struct
		{
			VerifyThreadAffinity();
			var handle = GCHandle.Alloc(source, GCHandleType.Pinned);
			try
			{
				commandList.UpdateBuffer(buffer, offset, handle.AddrOfPinnedObject(), (uint)(Marshal.SizeOf<T>() * source.Length));
			}
			finally
			{
				handle.Free();
			}
		}

		internal void CopyReadBufferToTexture(VulkanTexture texture, Rectangle rect)
		{
			VerifyThreadAffinity();
			texture.EnsureTexture(rect.Width, rect.Height, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.RenderTarget);
			commandList.CopyTexture(CurrentColorTarget, (uint)rect.X, (uint)rect.Y, 0, 0, 0,
				texture.NativeTexture, 0, 0, 0, 0, 0, (uint)rect.Width, (uint)rect.Height, 1, 1);
		}

		internal void PrepareDraw(PrimitiveType primitiveType, bool indexed, int count, int startOrOffset)
		{
			VerifyThreadAffinity();
			if (CurrentShader == null)
				throw new InvalidOperationException("No Vulkan shader is bound.");

			CurrentShader.BindForDraw(primitiveType);
			if (indexed)
				commandList.DrawIndexed((uint)count, 1, (uint)(startOrOffset / sizeof(uint)), 0, 0);
			else
				commandList.Draw((uint)count, 1, (uint)startOrOffset, 0);
		}

		public IVertexBuffer<T> CreateEmptyVertexBuffer<T>(int size) where T : struct
		{
			VerifyThreadAffinity();
			return new VulkanVertexBuffer<T>(this, size, true);
		}

		public IVertexBuffer<T> CreateVertexBuffer<T>(T[] data, bool dynamic = true) where T : struct
		{
			VerifyThreadAffinity();
			return new VulkanVertexBuffer<T>(this, data, dynamic);
		}

		public T[] CreateVertices<T>(int size) where T : struct
		{
			VerifyThreadAffinity();
			return new T[size];
		}

		public IIndexBuffer CreateIndexBuffer(uint[] indices)
		{
			VerifyThreadAffinity();
			return new VulkanIndexBuffer(this, indices);
		}

		public ITexture CreateTexture()
		{
			VerifyThreadAffinity();
			return new VulkanTexture(this);
		}

		public IFrameBuffer CreateFrameBuffer(Size s) { return CreateFrameBuffer(s, Color.FromArgb(0)); }

		public IFrameBuffer CreateFrameBuffer(Size s, Color clearColor)
		{
			VerifyThreadAffinity();
			return new VulkanFrameBuffer(this, s, clearColor);
		}

		public IShader CreateShader(IShaderBindings shaderBindings)
		{
			VerifyThreadAffinity();
			return new VulkanShader(this, shaderBindings);
		}

		public void EnableScissor(int x, int y, int width, int height)
		{
			VerifyThreadAffinity();
			ScissorEnabled = true;

			var windowSize = window.EffectiveWindowSize;
			var surfaceSize = window.SurfaceSize;
			if (windowSize != surfaceSize)
			{
				var windowScale = window.EffectiveWindowScale;
				x = (int)Math.Round(windowScale * x);
				y = (int)Math.Round(windowScale * y);
				width = (int)Math.Round(windowScale * width);
				height = (int)Math.Round(windowScale * height);
			}

			SetTopLeftScissor(x, y, width, height);
		}

		internal void EnableFramebufferScissor(Rectangle rect)
		{
			VerifyThreadAffinity();
			ScissorEnabled = true;
			SetTopLeftScissor(rect.X, rect.Y, Math.Max(rect.Width, 0), Math.Max(rect.Height, 0));
		}

		void SetTopLeftScissor(int x, int y, int width, int height)
		{
			width = Math.Max(width, 0);
			height = Math.Max(height, 0);

			var fbWidth = (int)CurrentFramebuffer.Width;
			var fbHeight = (int)CurrentFramebuffer.Height;
			var left = Math.Clamp(x, 0, fbWidth);
			var top = Math.Clamp(y, 0, fbHeight);
			var right = Math.Clamp(x + width, 0, fbWidth);
			var bottom = Math.Clamp(y + height, 0, fbHeight);

			width = Math.Max(right - left, 0);
			height = Math.Max(bottom - top, 0);

			commandList.SetScissorRect(0, (uint)left, (uint)top, (uint)width, (uint)height);
		}

		public void DisableScissor()
		{
			VerifyThreadAffinity();
			ScissorEnabled = false;
			commandList.SetFullScissorRect(0);
		}

		public void Present()
		{
			VerifyThreadAffinity();
			commandList.End();
			Device.SubmitCommands(commandList);
			Device.SwapBuffers();

			var surfaceSize = window.SurfaceSize;
			if (surfaceSize.Width > 0 && surfaceSize.Height > 0 &&
				(Device.SwapchainFramebuffer.Width != surfaceSize.Width || Device.SwapchainFramebuffer.Height != surfaceSize.Height))
				Device.ResizeMainWindow((uint)surfaceSize.Width, (uint)surfaceSize.Height);

			CurrentFramebuffer = Device.SwapchainFramebuffer;
			commandList.Begin();
			BindFramebuffer(null);
		}

		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices)
		{
			PrepareDraw(pt, false, numVertices, firstVertex);
		}

		public void DrawElements(int numIndices, int offset)
		{
			PrepareDraw(PrimitiveType.TriangleList, true, numIndices, offset);
		}

		public void Clear()
		{
			VerifyThreadAffinity();
			BindFramebuffer(null);
			commandList.ClearColorTarget(0, RgbaFloat.Black);
			commandList.ClearDepthStencil(1f);
		}

		public void EnableDepthBuffer()
		{
			VerifyThreadAffinity();
			DepthEnabled = true;
			commandList.ClearDepthStencil(1f);
		}

		public void DisableDepthBuffer()
		{
			VerifyThreadAffinity();
			DepthEnabled = false;
		}

		public void ClearDepthBuffer()
		{
			VerifyThreadAffinity();
			commandList.ClearDepthStencil(1f);
		}

		public void SetBlendMode(BlendMode mode)
		{
			VerifyThreadAffinity();
			CurrentBlendMode = mode;
		}

		public void SetVSyncEnabled(bool enabled)
		{
			VerifyThreadAffinity();
			Device.SyncToVerticalBlank = enabled;
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			Device.WaitForIdle();
			whiteTexture.Dispose();
			commandList.Dispose();
			Device.Dispose();
		}
	}

	abstract class VulkanVertexBuffer : IDisposable
	{
		public abstract VeldridBuffer Buffer { get; }
		public abstract void Dispose();
	}

	sealed class VulkanVertexBuffer<T> : VulkanVertexBuffer, IVertexBuffer<T> where T : struct
	{
		static readonly int VertexSize = Marshal.SizeOf<T>();
		readonly VulkanGraphicsContext context;
		VeldridBuffer buffer;
		int capacity;
		bool disposed;

		public override VeldridBuffer Buffer => buffer;

		public VulkanVertexBuffer(VulkanGraphicsContext context, int size, bool dynamic)
		{
			this.context = context;
			Create(size, dynamic);
			if (size > 0)
				SetData(new T[size], size);
		}

		public VulkanVertexBuffer(VulkanGraphicsContext context, T[] data, bool dynamic)
		{
			this.context = context;
			Create(data.Length, dynamic);
			SetData(data, data.Length);
		}

		void Create(int size, bool dynamic)
		{
			capacity = Math.Max(1, size);
			var usage = BufferUsage.VertexBuffer | (dynamic ? BufferUsage.Dynamic : 0);
			buffer = context.Factory.CreateBuffer(new BufferDescription((uint)(VertexSize * capacity), usage));
		}

		void EnsureCapacity(int size)
		{
			if (size <= capacity)
				return;

			context.Device.DisposeWhenIdle(buffer);
			Create(size, true);
		}

		public void Bind()
		{
			context.VerifyOwnerThread();
			context.SetVertexBuffer(this);
		}

		public void SetData(T[] vertices, int length) { SetData(vertices, 0, 0, length); }
		public void SetData(ref T[] vertices, int length) { SetData(vertices, 0, 0, length); }

		public void SetData(T[] vertices, int offset, int start, int length)
		{
			context.VerifyOwnerThread();
			EnsureCapacity(start + length);
			var handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
			try
			{
				context.UpdateBuffer(buffer, (uint)(VertexSize * start), IntPtr.Add(handle.AddrOfPinnedObject(), VertexSize * offset), (uint)(VertexSize * length));
			}
			finally
			{
				handle.Free();
			}
		}

		public override void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			context.Device.DisposeWhenIdle(buffer);
		}
	}

	sealed class VulkanIndexBuffer : IIndexBuffer
	{
		readonly VulkanGraphicsContext context;
		readonly VeldridBuffer buffer;
		bool disposed;

		public VeldridBuffer Buffer => buffer;

		public VulkanIndexBuffer(VulkanGraphicsContext context, uint[] indices)
		{
			this.context = context;
			buffer = context.Factory.CreateBuffer(new BufferDescription((uint)(sizeof(uint) * indices.Length), BufferUsage.IndexBuffer));
			context.UpdateBuffer(buffer, 0, indices);
		}

		public void Bind()
		{
			context.VerifyOwnerThread();
			context.SetIndexBuffer(this);
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			context.Device.DisposeWhenIdle(buffer);
		}
	}

	sealed class VulkanTexture : ITextureInternal
	{
		readonly VulkanGraphicsContext context;
		VeldridTexture texture;
		TextureView view;
		PixelFormat format;
		bool disposed;
		TextureScaleFilter scaleFilter;

		public uint ID => 0;
		public Size Size { get; private set; }
		public int Version { get; private set; }
		public VeldridTexture NativeTexture => texture;
		public TextureView View => view;
		public Sampler Sampler => scaleFilter == TextureScaleFilter.Linear ? context.LinearSampler : context.PointSampler;
		public TextureScaleFilter ScaleFilter
		{
			get => scaleFilter;
			set
			{
				context.VerifyOwnerThread();
				if (scaleFilter == value)
					return;

				scaleFilter = value;
				Version++;
			}
		}

		public VulkanTexture(VulkanGraphicsContext context) { this.context = context; }

		public void EnsureTexture(int width, int height, PixelFormat pixelFormat, TextureUsage usage)
		{
			context.VerifyOwnerThread();
			if (texture != null && Size.Width == width && Size.Height == height && format == pixelFormat && (texture.Usage & usage) == usage)
				return;

			if (texture != null)
			{
				context.Device.DisposeWhenIdle(view);
				context.Device.DisposeWhenIdle(texture);
			}

			Size = new Size(width, height);
			format = pixelFormat;
			texture = context.Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, pixelFormat, usage));
			view = context.Factory.CreateTextureView(texture);
			Version++;
		}

		public void SetData(byte[] colors, int width, int height)
		{
			context.VerifyOwnerThread();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException($"Non-power-of-two array {width}x{height}");

			EnsureTexture(width, height, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.RenderTarget);
			context.Device.UpdateTexture(texture, colors, 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);
		}

		public void SetFloatData(float[] data, int width, int height)
		{
			context.VerifyOwnerThread();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException($"Non-power-of-two array {width}x{height}");

			EnsureTexture(width, height, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled);
			context.Device.UpdateTexture(texture, data, 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);
		}

		public void SetDataFromReadBuffer(Rectangle rect)
		{
			context.VerifyOwnerThread();
			if (!Exts.IsPowerOf2(rect.Width) || !Exts.IsPowerOf2(rect.Height))
				throw new InvalidDataException($"Non-power-of-two rectangle {rect.Width}x{rect.Height}");

			context.CopyReadBufferToTexture(this, rect);
		}

		public byte[] GetData()
		{
			context.VerifyOwnerThread();
			if (texture == null)
				return [];
			if (format != PixelFormat.B8_G8_R8_A8_UNorm)
				throw new InvalidOperationException("Only BGRA8 Vulkan textures can be read back through ITexture.GetData().");

			var data = new byte[4 * Size.Width * Size.Height];
			var staging = context.Factory.CreateTexture(TextureDescription.Texture2D((uint)Size.Width, (uint)Size.Height, 1, 1, format, TextureUsage.Staging));
			context.SubmitAndRestart(true);
			context.CommandList.CopyTexture(texture, staging);
			context.SubmitAndRestart(true);

			var mapped = context.Device.Map(staging, MapMode.Read);
			unsafe
			{
				fixed (byte* dstBase = data)
				{
					var srcBase = (byte*)mapped.Data;
					var rowBytes = Size.Width * 4;
					for (var y = 0; y < Size.Height; y++)
						Buffer.MemoryCopy(srcBase + y * mapped.RowPitch, dstBase + y * rowBytes, rowBytes, rowBytes);
				}
			}

			context.Device.Unmap(staging);
			staging.Dispose();
			return data;
		}

		public void SetEmpty(int width, int height)
		{
			context.VerifyOwnerThread();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException($"Non-power-of-two array {width}x{height}");

			EnsureTexture(width, height, PixelFormat.B8_G8_R8_A8_UNorm, TextureUsage.Sampled | TextureUsage.RenderTarget);
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			if (view != null)
				context.Device.DisposeWhenIdle(view);
			if (texture != null)
				context.Device.DisposeWhenIdle(texture);
		}
	}

	sealed class VulkanFrameBuffer : IFrameBuffer
	{
		readonly VulkanGraphicsContext context;
		readonly Color clearColor;
		readonly VulkanTexture texture;
		readonly VeldridTexture depthTexture;
		readonly VeldridFramebuffer framebuffer;
		VeldridFramebuffer previousFramebuffer;
		bool disposed;
		bool scissored;

		public VeldridFramebuffer Framebuffer => framebuffer;
		public ITexture Texture => texture;

		public VulkanFrameBuffer(VulkanGraphicsContext context, Size size, Color clearColor)
		{
			this.context = context;
			this.clearColor = clearColor;
			if (!Exts.IsPowerOf2(size.Width) || !Exts.IsPowerOf2(size.Height))
				throw new InvalidDataException($"Frame buffer size ({size.Width}x{size.Height}) must be a power of two");

			texture = new VulkanTexture(context);
			texture.SetEmpty(size.Width, size.Height);
			depthTexture = context.Factory.CreateTexture(TextureDescription.Texture2D((uint)size.Width, (uint)size.Height, 1, 1, PixelFormat.D24_UNorm_S8_UInt, TextureUsage.DepthStencil));
			framebuffer = context.Factory.CreateFramebuffer(new FramebufferDescription(depthTexture, [texture.NativeTexture]));
		}

		public void Bind()
		{
			context.VerifyOwnerThread();
			previousFramebuffer = context.CurrentFramebuffer;
			context.BindFramebuffer(this);
			context.ClearColor(clearColor);
			context.ClearDepthBuffer();
		}

		public void Unbind()
		{
			if (scissored)
				throw new InvalidOperationException("Attempting to unbind FrameBuffer with an active scissor region.");

			context.VerifyOwnerThread();
			context.RestoreFramebuffer(previousFramebuffer);
			previousFramebuffer = null;
		}

		public void EnableScissor(Rectangle rect)
		{
			context.VerifyOwnerThread();
			context.EnableFramebufferScissor(rect);
			scissored = true;
		}

		public void DisableScissor()
		{
			context.VerifyOwnerThread();
			context.DisableScissor();
			scissored = false;
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			texture.Dispose();
			context.Device.DisposeWhenIdle(framebuffer);
			context.Device.DisposeWhenIdle(depthTexture);
		}
	}

	sealed class VulkanShader : IShader, IDisposable
	{
		static readonly string[] TextureNames =
		[
			"Texture0", "Texture1", "Texture2", "Texture3", "Texture4", "Texture5", "Texture6", "Texture7",
			"Palette", "ColorShifts", "DiffuseTexture", "WorldTexture", "VortexTexture"
		];

		readonly VulkanGraphicsContext context;
		readonly IShaderBindings bindings;
		readonly VeldridShader[] shaders;
		readonly ResourceLayout resourceLayout;
		readonly VeldridBuffer uniformBuffer;
		readonly float[] uniforms = new float[128 * 4];
		readonly Dictionary<string, int> textureSlots = [];
		readonly VulkanTexture[] textures = new VulkanTexture[TextureNames.Length];
		readonly int[] textureVersions = new int[TextureNames.Length];
		readonly Dictionary<string, Pipeline> pipelines = [];
		ResourceSet resourceSet;
		bool resourceSetDirty = true;
		bool uniformsDirty = true;
		bool disposed;

		public VulkanShader(VulkanGraphicsContext context, IShaderBindings bindings)
		{
			this.context = context;
			this.bindings = bindings;
			for (var i = 0; i < TextureNames.Length; i++)
			{
				textureSlots[TextureNames[i]] = i;
				textures[i] = context.WhiteTexture;
				textureVersions[i] = -1;
			}

			uniformBuffer = context.Factory.CreateBuffer(new BufferDescription((uint)(uniforms.Length * sizeof(float)), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

			var elements = new ResourceLayoutElementDescription[1 + TextureNames.Length * 2];
			elements[0] = new ResourceLayoutElementDescription("OpenRAParams", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment);
			for (var i = 0; i < TextureNames.Length; i++)
			{
				elements[1 + i * 2] = new ResourceLayoutElementDescription(TextureNames[i], ResourceKind.TextureReadOnly, ShaderStages.Vertex | ShaderStages.Fragment);
				elements[2 + i * 2] = new ResourceLayoutElementDescription(TextureNames[i] + "Sampler", ResourceKind.Sampler, ShaderStages.Vertex | ShaderStages.Fragment);
			}

			resourceLayout = context.Factory.CreateResourceLayout(new ResourceLayoutDescription(elements));

			var vertex = VulkanShaderSources.GetVertexShader(bindings.VertexShaderName, context.Device.IsDepthRangeZeroToOne);
			var fragment = VulkanShaderSources.GetFragmentShader(bindings.FragmentShaderName);
			var vertexBytes = SpirvCompilation.CompileGlslToSpirv(vertex, bindings.VertexShaderName + ".vulkan.vert", ShaderStages.Vertex, GlslCompileOptions.Default).SpirvBytes;
			var fragmentBytes = SpirvCompilation.CompileGlslToSpirv(fragment, bindings.FragmentShaderName + ".vulkan.frag", ShaderStages.Fragment, GlslCompileOptions.Default).SpirvBytes;
			shaders =
			[
				context.Factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexBytes, "main")),
				context.Factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentBytes, "main"))
			];
		}

		void SetSlot(int slot, float x, float y = 0, float z = 0, float w = 0)
		{
			var offset = slot * 4;
			uniforms[offset] = x;
			uniforms[offset + 1] = y;
			uniforms[offset + 2] = z;
			uniforms[offset + 3] = w;
			uniformsDirty = true;
		}

		void SetSlotComponent(int slot, int component, float value)
		{
			uniforms[slot * 4 + component] = value;
			uniformsDirty = true;
		}

		static bool TryParseArrayIndex(string name, string prefix, out int index)
		{
			index = 0;
			if (!name.StartsWith(prefix + "[", StringComparison.Ordinal) || !name.EndsWith("]", StringComparison.Ordinal))
				return false;

			return int.TryParse(name.AsSpan(prefix.Length + 1, name.Length - prefix.Length - 2), out index) && index >= 0 && index < 16;
		}

		public void SetBool(string name, bool value)
		{
			SetVec(name, value ? 1f : 0f);
		}

		public void SetVec(string name, float x)
		{
			context.VerifyOwnerThread();
			switch (name)
			{
				case "PaletteRows": SetSlotComponent(0, 3, x); break;
				case "DepthTextureScale": SetSlotComponent(1, 3, x); break;
				case "EnableDepthPreview": SetSlotComponent(3, 2, x); break;
				case "EnablePixelArtScaling": SetSlotComponent(3, 3, x); break;
				case "Blend": SetSlotComponent(4, 3, x); break;
				case "BeamCount": SetSlotComponent(110, 0, x); break;
				case "DistortionCount": SetSlotComponent(110, 0, x); break;
				case "Time": SetSlotComponent(110, 1, x); break;
				case "From": SetSlotComponent(110, 2, x); break;
				case "To": SetSlotComponent(110, 3, x); break;
				case "Scale": SetSlotComponent(111, 0, x); break;
				default:
					if (TryParseArrayIndex(name, "GlowIntensities", out var gi))
						SetSlotComponent(78 + gi, 0, x);
					else if (TryParseArrayIndex(name, "GlowRadii", out var gr))
						SetSlotComponent(78 + gr, 1, x);
					else if (TryParseArrayIndex(name, "DistortionRadii", out var dr))
						SetSlotComponent(94 + dr, 2, x);
					else if (TryParseArrayIndex(name, "DistortionStrengths", out var ds))
						SetSlotComponent(94 + ds, 3, x);
					break;
			}
		}

		public void SetVec(string name, float x, float y)
		{
			context.VerifyOwnerThread();
			switch (name)
			{
				case "Scroll": SetSlot(0, x, y, uniforms[2], uniforms[3]); SetSlot(6, uniforms[24], uniforms[25], x, y); break;
				case "p1": SetSlot(1, x, y, uniforms[6], uniforms[7]); break;
				case "p2": SetSlot(2, x, y, uniforms[10], uniforms[11]); break;
				case "DepthPreviewParams": SetSlot(3, x, y, uniforms[14], uniforms[15]); break;
				case "Palettes": SetSlot(5, x, y, uniforms[22], uniforms[23]); break;
				case "Pos": SetSlot(6, x, y, uniforms[26], uniforms[27]); break;
				default:
					if (TryParseArrayIndex(name, "BeamStarts", out var bs))
						SetSlot(30 + bs, x, y, uniforms[(30 + bs) * 4 + 2], uniforms[(30 + bs) * 4 + 3]);
					else if (TryParseArrayIndex(name, "BeamEnds", out var be))
						SetSlot(46 + be, x, y, uniforms[(46 + be) * 4 + 2], uniforms[(46 + be) * 4 + 3]);
					else if (TryParseArrayIndex(name, "DistortionCenters", out var dc))
						SetSlot(94 + dc, x, y, uniforms[(94 + dc) * 4 + 2], uniforms[(94 + dc) * 4 + 3]);
					break;
			}
		}

		public void SetVec(string name, float x, float y, float z)
		{
			context.VerifyOwnerThread();
			switch (name)
			{
				case "Scroll": SetSlot(0, x, y, z, uniforms[3]); SetSlot(6, uniforms[24], uniforms[25], x, y); break;
				case "p1": SetSlot(1, x, y, z, uniforms[7]); break;
				case "p2": SetSlot(2, x, y, z, uniforms[11]); break;
				case "Color": SetSlot(4, x, y, z, uniforms[19]); break;
				case "Tint": SetSlot(4, x, y, z, uniforms[19]); break;
				default:
					if (TryParseArrayIndex(name, "GlowColors", out var gc))
						SetSlot(62 + gc, x, y, z, uniforms[(62 + gc) * 4 + 3]);
					break;
			}
		}

		public void SetVec(string name, float[] vec, int length)
		{
			context.VerifyOwnerThread();
			if (vec == null || length <= 0)
				return;

			var x = length > 0 ? vec[0] : 0;
			var y = length > 1 ? vec[1] : 0;
			var z = length > 2 ? vec[2] : 0;
			var w = length > 3 ? vec[3] : 0;
			switch (name)
			{
				case "LightDirection": SetSlot(18, x, y, z, w); break;
				case "AmbientLight": SetSlot(19, x, y, z, w); break;
				case "DiffuseLight": SetSlot(20, x, y, z, w); break;
				default:
					if (length == 1)
						SetVec(name, x);
					else if (length == 2)
						SetVec(name, x, y);
					else
						SetVec(name, x, y, z);
					break;
			}
		}

		public void SetVec(string name, float[] vec, int components, int count)
		{
			context.VerifyOwnerThread();
			for (var i = 0; i < count; i++)
			{
				var offset = i * components;
				var x = components > 0 ? vec[offset] : 0;
				var y = components > 1 ? vec[offset + 1] : 0;
				var z = components > 2 ? vec[offset + 2] : 0;
				var w = components > 3 ? vec[offset + 3] : 0;
				SetSlot(i, x, y, z, w);
			}
		}

		public void SetTexture(string param, ITexture texture)
		{
			context.VerifyOwnerThread();
			if (texture == null || !textureSlots.TryGetValue(param, out var slot))
				return;

			if (texture is not VulkanTexture vulkanTexture)
				throw new InvalidOperationException($"Texture `{param}` was not created by the Vulkan graphics context.");

			if (!ReferenceEquals(textures[slot], vulkanTexture))
			{
				textures[slot] = vulkanTexture;
				resourceSetDirty = true;
			}
		}

		public void SetMatrix(string param, float[] mtx)
		{
			context.VerifyOwnerThread();
			if (mtx == null || mtx.Length != 16)
				throw new InvalidDataException("Invalid 4x4 matrix");

			var baseSlot = param switch
			{
				"View" => 10,
				"TransformMatrix" => 14,
				_ => -1
			};

			if (baseSlot < 0)
				return;

			for (var i = 0; i < 16; i++)
				uniforms[baseSlot * 4 + i] = mtx[i];
			uniformsDirty = true;
		}

		public void PrepareRender()
		{
			context.VerifyOwnerThread();
			UpdateResourcesIfNeeded();
		}

		public void Bind()
		{
			context.VerifyOwnerThread();
			context.SetShader(this);
		}

		void UpdateResourcesIfNeeded()
		{
			if (uniformsDirty)
			{
				context.UpdateBuffer(uniformBuffer, 0, uniforms);
				uniformsDirty = false;
			}

			for (var i = 0; i < textures.Length; i++)
			{
				var version = textures[i].Version;
				if (textureVersions[i] == version)
					continue;

				textureVersions[i] = version;
				resourceSetDirty = true;
			}

			if (!resourceSetDirty)
				return;

			var resources = new BindableResource[1 + textures.Length * 2];
			resources[0] = uniformBuffer;
			for (var i = 0; i < textures.Length; i++)
			{
				resources[1 + i * 2] = textures[i].View;
				resources[2 + i * 2] = textures[i].Sampler;
			}

			resourceSet?.Dispose();
			resourceSet = context.Factory.CreateResourceSet(new ResourceSetDescription(resourceLayout, resources));
			resourceSetDirty = false;
		}

		internal void BindForDraw(PrimitiveType primitiveType)
		{
			UpdateResourcesIfNeeded();

			var key = $"{context.CurrentOutputDescription.GetHashCode()}|{context.CurrentBlendMode}|{context.DepthEnabled}|{context.ScissorEnabled}|{primitiveType}";
			if (!pipelines.TryGetValue(key, out var pipeline))
			{
				var vertexLayout = CreateVertexLayout();
				var shaderSet = new ShaderSetDescription([vertexLayout], shaders);
				var rasterizer = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, context.ScissorEnabled);
				var depth = context.DepthEnabled ? DepthStencilStateDescription.DepthOnlyLessEqual : DepthStencilStateDescription.Disabled;
				var blend = new BlendStateDescription(RgbaFloat.White, [BlendAttachment(context.CurrentBlendMode)]);
				var description = new GraphicsPipelineDescription(blend, depth, rasterizer, Topology(primitiveType), shaderSet, [resourceLayout], context.CurrentOutputDescription, ResourceBindingModel.Improved);
				pipeline = context.Factory.CreateGraphicsPipeline(description);
				pipelines.Add(key, pipeline);
			}

			context.CommandList.SetPipeline(pipeline);
			context.CommandList.SetGraphicsResourceSet(0, resourceSet);
		}

		VertexLayoutDescription CreateVertexLayout()
		{
			var elements = new VertexElementDescription[bindings.Attributes.Length];
			for (var i = 0; i < bindings.Attributes.Length; i++)
			{
				var a = bindings.Attributes[i];
				elements[i] = new VertexElementDescription(a.Name, VertexElementSemantic.TextureCoordinate, Format(a.Type, a.Components), (uint)a.Offset);
			}

			return new VertexLayoutDescription((uint)bindings.Stride, elements);
		}

		static PrimitiveTopology Topology(PrimitiveType pt)
		{
			return pt switch
			{
				PrimitiveType.PointList => PrimitiveTopology.PointList,
				PrimitiveType.LineList => PrimitiveTopology.LineList,
				PrimitiveType.TriangleList => PrimitiveTopology.TriangleList,
				_ => throw new NotImplementedException()
			};
		}

		static VertexElementFormat Format(ShaderVertexAttributeType type, int components)
		{
			return (type, components) switch
			{
				(ShaderVertexAttributeType.Float, 1) => VertexElementFormat.Float1,
				(ShaderVertexAttributeType.Float, 2) => VertexElementFormat.Float2,
				(ShaderVertexAttributeType.Float, 3) => VertexElementFormat.Float3,
				(ShaderVertexAttributeType.Float, 4) => VertexElementFormat.Float4,
				(ShaderVertexAttributeType.UInt, 1) => VertexElementFormat.UInt1,
				(ShaderVertexAttributeType.UInt, 2) => VertexElementFormat.UInt2,
				(ShaderVertexAttributeType.UInt, 3) => VertexElementFormat.UInt3,
				(ShaderVertexAttributeType.UInt, 4) => VertexElementFormat.UInt4,
				(ShaderVertexAttributeType.Int, 1) => VertexElementFormat.Int1,
				(ShaderVertexAttributeType.Int, 2) => VertexElementFormat.Int2,
				(ShaderVertexAttributeType.Int, 3) => VertexElementFormat.Int3,
				(ShaderVertexAttributeType.Int, 4) => VertexElementFormat.Int4,
				_ => throw new InvalidDataException("Invalid Vulkan vertex attribute format")
			};
		}

		static BlendAttachmentDescription BlendAttachment(BlendMode mode)
		{
			return mode switch
			{
				BlendMode.None => BlendAttachmentDescription.Disabled,
				BlendMode.Alpha => Blend(true, BlendFactor.One, BlendFactor.InverseSourceAlpha),
				BlendMode.Additive => Blend(true, BlendFactor.One, BlendFactor.One),
				BlendMode.Subtractive => Blend(true, BlendFactor.One, BlendFactor.One, BlendFunction.ReverseSubtract),
				BlendMode.Multiply => Blend(true, BlendFactor.DestinationColor, BlendFactor.InverseSourceAlpha),
				BlendMode.Multiplicative => Blend(true, BlendFactor.Zero, BlendFactor.SourceColor),
				BlendMode.DoubleMultiplicative => Blend(true, BlendFactor.DestinationColor, BlendFactor.SourceColor),
				BlendMode.LowAdditive => Blend(true, BlendFactor.DestinationColor, BlendFactor.One),
				BlendMode.Screen => Blend(true, BlendFactor.SourceColor, BlendFactor.InverseSourceColor),
				BlendMode.Translucent => Blend(true, BlendFactor.DestinationColor, BlendFactor.InverseDestinationColor),
				_ => BlendAttachmentDescription.Disabled
			};
		}

		static BlendAttachmentDescription Blend(bool enabled, BlendFactor src, BlendFactor dst, BlendFunction func = BlendFunction.Add)
		{
			return new BlendAttachmentDescription(enabled, src, dst, func, src, dst, BlendFunction.Add);
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			foreach (var p in pipelines.Values)
				p.Dispose();
			resourceSet?.Dispose();
			resourceLayout.Dispose();
			uniformBuffer.Dispose();
			foreach (var shader in shaders)
				shader.Dispose();
		}
	}

	static class VulkanShaderSources
	{
		static readonly string[] TextureNames =
		[
			"Texture0", "Texture1", "Texture2", "Texture3", "Texture4", "Texture5", "Texture6", "Texture7",
			"Palette", "ColorShifts", "DiffuseTexture", "WorldTexture", "VortexTexture"
		];

		static string Header(bool depthRangeZeroToOne = false)
		{
			var sb = new StringBuilder("""
#version 450
layout(set = 0, binding = 0) uniform OpenRAParams { vec4 U[128]; };

""");
			for (var i = 0; i < TextureNames.Length; i++)
			{
				var binding = 1 + i * 2;
				sb.Append("layout(set = 0, binding = ").Append(binding).Append(") uniform texture2D ").Append(TextureNames[i]).AppendLine(";");
				sb.Append("layout(set = 0, binding = ").Append(binding + 1).Append(") uniform sampler ").Append(TextureNames[i]).AppendLine("Sampler;");
			}

			sb.AppendLine();
			sb.AppendLine($$"""
vec4 OpenRAClipPosition(vec4 position)
{
	// OpenRA's shared renderer math already maps UI/window coordinates to the
	// top-left Vulkan framebuffer convention. Vulkan depth is 0..1, so remap
	// the OpenGL-style -1..1 depth range when required by the device.
{{(depthRangeZeroToOne ? "\tposition.z = (position.z + position.w) * 0.5;" : "")}}
	return position;
}

""");
			return sb.ToString();
		}

		public static string GetVertexShader(string name, bool depthRangeZeroToOne)
		{
			return Header(depthRangeZeroToOne) + name switch
			{
				"combined" => CombinedVertex,
				"model" => ModelVertex,
				"postprocess" => PostProcessVertex,
				"postprocess_textured" => PostProcessTexturedVertex,
				_ => throw new InvalidDataException($"No Vulkan vertex shader is defined for `{name}`.")
			};
		}

		public static string GetFragmentShader(string name)
		{
			return Header() + name switch
			{
				"combined" => CombinedFragment,
				"model" => ModelFragment,
				"postprocess_flash" => PostProcessFlashFragment,
				"postprocess_tint" => PostProcessTintFragment,
				"postprocess_menufade" => PostProcessMenuFadeFragment,
				"postprocess_chronoshift" => PostProcessChronoshiftFragment,
				"postprocess_glow" => PostProcessGlowFragment,
				"postprocess_heat_distortion" => PostProcessHeatDistortionFragment,
				"postprocess_textured_sonic" => PostProcessTexturedSonicFragment,
				"postprocess_textured_vortex" => PostProcessTexturedVortexFragment,
				_ => throw new InvalidDataException($"No Vulkan fragment shader is defined for `{name}`.")
			};
		}

		const string CombinedVertex = """
layout(location = 0) in vec3 aVertexPosition;
layout(location = 1) in vec4 aVertexTexCoord;
layout(location = 2) in uint aVertexAttributes;
layout(location = 3) in vec4 aVertexTint;
layout(location = 0) out vec4 vTexCoord;
layout(location = 1) flat out float vTexPalette;
layout(location = 2) flat out vec4 vChannelMask;
layout(location = 3) flat out uint vChannelSampler;
layout(location = 4) flat out uint vChannelType;
layout(location = 5) flat out vec4 vDepthMask;
layout(location = 6) flat out uint vDepthSampler;
layout(location = 7) out vec4 vTint;

vec4 SelectChannelMask(uint x)
{
	switch (x)
	{
		case 7u: return vec4(0.0, 0.0, 0.0, 1.0);
		case 5u: return vec4(0.0, 0.0, 1.0, 0.0);
		case 3u: return vec4(0.0, 1.0, 0.0, 0.0);
		case 2u: return vec4(1.0, 1.0, 1.0, 1.0);
		case 1u: return vec4(1.0, 0.0, 0.0, 0.0);
		default: return vec4(0.0, 0.0, 0.0, 0.0);
	}
}

void main()
{
	vec3 scroll = U[0].xyz;
	vec3 p1 = U[1].xyz;
	vec3 p2 = U[2].xyz;
	float paletteRows = U[0].w;
	gl_Position = OpenRAClipPosition(vec4((aVertexPosition - scroll) * p1 + p2, 1.0));
	vTexCoord = aVertexTexCoord;
	vChannelType = aVertexAttributes & 0x07u;
	vChannelMask = SelectChannelMask(vChannelType);
	vDepthMask = SelectChannelMask((aVertexAttributes >> 3) & 0x07u);
	vChannelSampler = (aVertexAttributes >> 6) & 0x07u;
	vDepthSampler = (aVertexAttributes >> 9) & 0x07u;
	vTexPalette = float(aVertexAttributes >> 16) / paletteRows;
	vTint = aVertexTint;
}
""";

		const string CombinedFragment = """
layout(location = 0) in vec4 vTexCoord;
layout(location = 1) flat in float vTexPalette;
layout(location = 2) flat in vec4 vChannelMask;
layout(location = 3) flat in uint vChannelSampler;
layout(location = 4) flat in uint vChannelType;
layout(location = 5) flat in vec4 vDepthMask;
layout(location = 6) flat in uint vDepthSampler;
layout(location = 7) in vec4 vTint;
layout(location = 0) out vec4 fragColor;

vec3 rgb2hsv(vec3 c)
{
	vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	vec4 p = c.g < c.b ? vec4(c.bg, K.wz) : vec4(c.gb, K.xy);
	vec4 q = c.r < p.x ? vec4(p.xyw, c.r) : vec4(c.r, p.yzx);
	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
	vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float srgb2linear(float c) { return c <= 0.04045 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4); }
vec4 srgb2linear(vec4 c) { return c.a * vec4(srgb2linear(c.r / c.a), srgb2linear(c.g / c.a), srgb2linear(c.b / c.a), 1.0); }
float linear2srgb(float c) { return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055; }
vec4 linear2srgb(vec4 c) { return c.a * vec4(linear2srgb(c.r / c.a), linear2srgb(c.g / c.a), linear2srgb(c.b / c.a), 1.0); }

vec2 Size(uint samplerIndex)
{
	switch (samplerIndex)
	{
		case 7u: return vec2(textureSize(sampler2D(Texture7, Texture7Sampler), 0));
		case 6u: return vec2(textureSize(sampler2D(Texture6, Texture6Sampler), 0));
		case 5u: return vec2(textureSize(sampler2D(Texture5, Texture5Sampler), 0));
		case 4u: return vec2(textureSize(sampler2D(Texture4, Texture4Sampler), 0));
		case 3u: return vec2(textureSize(sampler2D(Texture3, Texture3Sampler), 0));
		case 2u: return vec2(textureSize(sampler2D(Texture2, Texture2Sampler), 0));
		case 1u: return vec2(textureSize(sampler2D(Texture1, Texture1Sampler), 0));
		default: return vec2(textureSize(sampler2D(Texture0, Texture0Sampler), 0));
	}
}

vec4 Sample(uint samplerIndex, vec2 pos)
{
	switch (samplerIndex)
	{
		case 7u: return texture(sampler2D(Texture7, Texture7Sampler), pos);
		case 6u: return texture(sampler2D(Texture6, Texture6Sampler), pos);
		case 5u: return texture(sampler2D(Texture5, Texture5Sampler), pos);
		case 4u: return texture(sampler2D(Texture4, Texture4Sampler), pos);
		case 3u: return texture(sampler2D(Texture3, Texture3Sampler), pos);
		case 2u: return texture(sampler2D(Texture2, Texture2Sampler), pos);
		case 1u: return texture(sampler2D(Texture1, Texture1Sampler), pos);
		default: return texture(sampler2D(Texture0, Texture0Sampler), pos);
	}
}

vec4 PaletteSample(vec2 p) { return texture(sampler2D(Palette, PaletteSampler), p); }
vec4 ColorShiftSample(vec2 p) { return texture(sampler2D(ColorShifts, ColorShiftsSampler), p); }

vec4 SamplePalettedBilinear(uint samplerIndex, vec2 coords, vec2 textureSize)
{
	vec2 texPos = (coords * textureSize) - vec2(0.5);
	vec2 interp = fract(texPos);
	vec2 tl = (floor(texPos) + vec2(0.5)) / textureSize;
	vec2 px = 1.0 / textureSize;
	vec4 x1 = Sample(samplerIndex, tl);
	vec4 x2 = Sample(samplerIndex, tl + vec2(px.x, 0.0));
	vec4 x3 = Sample(samplerIndex, tl + vec2(0.0, px.y));
	vec4 x4 = Sample(samplerIndex, tl + px);
	vec4 c1 = PaletteSample(vec2(dot(x1, vChannelMask), vTexPalette));
	vec4 c2 = PaletteSample(vec2(dot(x2, vChannelMask), vTexPalette));
	vec4 c3 = PaletteSample(vec2(dot(x3, vChannelMask), vTexPalette));
	vec4 c4 = PaletteSample(vec2(dot(x4, vChannelMask), vTexPalette));
	return mix(mix(c1, c2, interp.x), mix(c3, c4, interp.x), interp.y);
}

vec4 ColorShift(vec4 c, float p)
{
	vec4 range = ColorShiftSample(vec2(0.25, p));
	vec4 shift = ColorShiftSample(vec2(0.75, p));
	vec3 hsv = rgb2hsv(srgb2linear(c).rgb);
	if (hsv.r > range.r && range.g >= hsv.r)
		c = linear2srgb(vec4(hsv2rgb(vec3(hsv.r + shift.r, clamp(hsv.g + shift.g, 0.0, 1.0), hsv.b * clamp(shift.b, 0.0, 1.0))), c.a));
	return c;
}

void main()
{
	bool enableDepthPreview = U[3].z != 0.0;
	bool enablePixelArtScaling = U[3].w != 0.0;
	float depthTextureScale = U[1].w;
	vec2 depthPreviewParams = U[3].xy;
	vec2 coords = vTexCoord.st;
	bool isPaletted = (vChannelType & 0x01u) != 0u;
	bool isColor = vChannelType == 0u;
	vec4 c = vec4(0.0);

	if (enablePixelArtScaling)
	{
		vec2 textureSize = Size(vChannelSampler);
		vec2 vUv = coords.st * textureSize;
		vec2 offset = fract(vUv);
		vec2 pixelsPerTexel = vec2(1.0 / dFdx(vUv.x), 1.0 / dFdy(vUv.y));
		float ik = 1.43;
		vec2 interp = clamp(offset * ik * pixelsPerTexel, 0.0, 0.5) + clamp((offset - 1.0) * ik * pixelsPerTexel + 0.5, 0.0, 0.5);
		coords = (floor(coords.st * textureSize) + interp) / textureSize;
		if (isPaletted)
			c = SamplePalettedBilinear(vChannelSampler, coords, textureSize);
	}

	if (!(enablePixelArtScaling && isPaletted))
	{
		vec4 x = Sample(vChannelSampler, coords);
		vec2 p = vec2(dot(x, vChannelMask), vTexPalette);
		if (isPaletted)
			c = PaletteSample(p);
		else if (isColor)
			c = vTexCoord;
		else
			c = x;
	}

	if (c.a == 0.0)
		discard;
	if (!isPaletted && vTexPalette > 0.0)
		c = ColorShift(c, vTexPalette);

	float depth = gl_FragCoord.z;
	if (length(vDepthMask) > 0.0)
	{
		vec4 y = Sample(vDepthSampler, vTexCoord.pq);
		depth = depth + depthTextureScale * dot(y, vDepthMask);
	}
	gl_FragDepth = depth;

	if (enableDepthPreview)
	{
		float intensity = 1.0 - clamp(depthPreviewParams.x * depth - 0.5 * depthPreviewParams.x - depthPreviewParams.y + 0.5, 0.0, 1.0);
		fragColor = vec4(vec3(intensity), 1.0);
	}
	else
	{
		if (vTint.a < 0.0)
			c = vec4(vTint.rgb, -vTint.a);
		else if (vTint.a > 1.0)
			c = vec4(clamp(c.rgb + vTint.rgb * (vTint.a - 1.0), 0.0, 1.0), c.a);
		else
			c *= vTint;
		fragColor = c;
	}
}
""";

		const string ModelVertex = """
layout(location = 0) in vec3 aVertexPosition;
layout(location = 1) in vec4 aVertexTexCoord;
layout(location = 2) in vec2 aVertexTexMetadata;
layout(location = 0) out vec4 vTexCoord;
layout(location = 1) out vec4 vChannelMask;
layout(location = 2) out vec4 vNormalsMask;

vec4 DecodeMask(float x)
{
	if (x > 0.0)
		return (x > 0.5) ? vec4(1,0,0,0) : vec4(0,1,0,0);
	else
		return (x < -0.5) ? vec4(0,0,0,1) : vec4(0,0,1,0);
}

void main()
{
	mat4 view = mat4(U[10], U[11], U[12], U[13]);
	mat4 transform = mat4(U[14], U[15], U[16], U[17]);
	gl_Position = OpenRAClipPosition(view * transform * vec4(aVertexPosition, 1.0));
	vTexCoord = aVertexTexCoord;
	vChannelMask = DecodeMask(aVertexTexMetadata.s);
	vNormalsMask = DecodeMask(aVertexTexMetadata.t);
}
""";

		const string ModelFragment = """
layout(location = 0) in vec4 vTexCoord;
layout(location = 1) in vec4 vChannelMask;
layout(location = 2) in vec4 vNormalsMask;
layout(location = 0) out vec4 fragColor;

void main()
{
	float paletteRows = U[0].w;
	vec2 palettes = U[5].xy;
	vec4 lightDirection = U[18];
	vec3 ambientLight = U[19].xyz;
	vec3 diffuseLight = U[20].xyz;
	vec4 x = texture(sampler2D(DiffuseTexture, DiffuseTextureSampler), vTexCoord.st);
	vec4 color = texture(sampler2D(Palette, PaletteSampler), vec2(dot(x, vChannelMask), (palettes.x + 0.5) / paletteRows));
	if (color.a < 0.01)
		discard;

	vec4 y = texture(sampler2D(DiffuseTexture, DiffuseTextureSampler), vTexCoord.pq);
	vec4 normal = (2.0 * texture(sampler2D(Palette, PaletteSampler), vec2(dot(y, vNormalsMask), (palettes.y + 0.5) / paletteRows)) - 1.0);
	vec3 intensity = ambientLight + diffuseLight * max(dot(normal, lightDirection), 0.0);
	fragColor = vec4(intensity * color.rgb, color.a);
}
""";

		const string PostProcessVertex = """
layout(location = 0) in vec2 aVertexPosition;
void main()
{
	gl_Position = OpenRAClipPosition(vec4(aVertexPosition, 0.0, 1.0));
}
""";

		const string PostProcessTexturedVertex = """
layout(location = 0) in vec2 aVertexPosition;
layout(location = 1) in vec2 aVertexTexCoord;
layout(location = 0) out vec2 vTexCoord;
void main()
{
	vec2 pos = U[6].xy;
	vec2 scroll = U[6].zw;
	vec2 p1 = U[1].xy;
	vec2 p2 = U[2].xy;
	gl_Position = OpenRAClipPosition(vec4((aVertexPosition + pos - scroll) * p1 + p2, 0.0, 1.0));
	vTexCoord = aVertexTexCoord;
}
""";

		const string PostProcessFlashFragment = """
layout(location = 0) out vec4 fragColor;
void main()
{
	vec4 c = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy), 0);
	fragColor = mix(c, vec4(U[4].rgb, c.a), U[4].w);
}
""";

		const string PostProcessTintFragment = """
layout(location = 0) out vec4 fragColor;
void main()
{
	vec4 c = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy), 0);
	fragColor = vec4(U[4].rgb, c.a) * c;
}
""";

		const string PostProcessChronoshiftFragment = """
layout(location = 0) out vec4 fragColor;
void main()
{
	vec4 c = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy), 0);
	float lum = 0.5 * (min(c.r, min(c.g, c.b)) + max(c.r, max(c.g, c.b)));
	fragColor = mix(c, vec4(lum, lum, lum, c.a), U[4].w);
}
""";

		const string PostProcessMenuFadeFragment = """
layout(location = 0) out vec4 fragColor;
vec4 ColorForEffect(float effect, vec4 c)
{
	if (effect > 1.5)
	{
		float lum = 0.5 * (min(c.r, min(c.g, c.b)) + max(c.r, max(c.g, c.b)));
		return vec4(lum, lum, lum, c.a);
	}
	if (effect > 0.5)
		return vec4(0.0, 0.0, 0.0, c.a);
	return c;
}
void main()
{
	vec4 c = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy), 0);
	fragColor = mix(ColorForEffect(U[110].w, c), ColorForEffect(U[110].z, c), U[4].w);
}
""";

		const string PostProcessGlowFragment = """
#define MAX_BEAMS 16
layout(location = 0) out vec4 fragColor;
float segmentDist(vec2 p, vec2 a, vec2 b)
{
	vec2 ab = b - a;
	float t = clamp(dot(p - a, ab) / max(dot(ab, ab), 0.0001), 0.0, 1.0);
	return length(p - (a + t * ab));
}
void main()
{
	vec4 c = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy), 0);
	vec3 rgb = c.rgb;
	int count = int(U[110].x);
	for (int i = 0; i < MAX_BEAMS; ++i)
	{
		if (i >= count)
			break;
		float d = segmentDist(gl_FragCoord.xy, U[30 + i].xy, U[46 + i].xy);
		float r = U[78 + i].y;
		float glow = U[78 + i].x * exp(-d * d / (r * r));
		vec3 contrib = U[62 + i].xyz * glow;
		rgb = rgb + contrib * (1.0 - rgb);
	}
	fragColor = vec4(rgb, c.a);
}
""";

		const string PostProcessHeatDistortionFragment = """
#define MAX_DISTORTIONS 16
layout(location = 0) out vec4 fragColor;
void main()
{
	vec2 fc = gl_FragCoord.xy;
	vec2 sz = vec2(textureSize(sampler2D(WorldTexture, WorldTextureSampler), 0));
	int count = int(U[110].x);
	vec2 offset = vec2(0.0);
	for (int i = 0; i < MAX_DISTORTIONS; ++i)
	{
		if (i >= count)
			break;
		float r = U[94 + i].z;
		float d = distance(fc, U[94 + i].xy);
		float falloff = exp(-d * d / (r * r));
		float s = U[94 + i].w * falloff;
		float phase = fc.y * 0.06 - U[110].y * 4.0;
		offset.x += sin(phase + fc.x * 0.02) * s;
		offset.y += cos(phase) * s * 0.35;
	}
	vec2 maxCoord = sz - vec2(1.0);
	ivec2 sampleCoord = ivec2(clamp(fc + offset, vec2(0.0), maxCoord));
	fragColor = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), sampleCoord, 0);
}
""";

		const string PostProcessTexturedSonicFragment = """
layout(location = 0) in vec2 vTexCoord;
layout(location = 0) out vec4 fragColor;
void main()
{
	if (dot(vTexCoord, vTexCoord) >= 1.0)
		discard;
	fragColor = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy + U[111].x * vTexCoord), 0);
}
""";

		const string PostProcessTexturedVortexFragment = """
layout(location = 0) in vec2 vTexCoord;
layout(location = 0) out vec4 fragColor;
void main()
{
	vec4 vtx = texture(sampler2D(VortexTexture, VortexTextureSampler), vTexCoord.xy);
	vec2 delta = (vtx.bg - 0.5) * 256.0;
	float frac = 16.0 * vtx.r + 0.0625;
	if (vtx.r > 0.055)
		discard;
	fragColor = texelFetch(sampler2D(WorldTexture, WorldTextureSampler), ivec2(gl_FragCoord.xy + delta), 0) * vec4(frac, frac, frac, 1.0);
}
""";
	}
}



