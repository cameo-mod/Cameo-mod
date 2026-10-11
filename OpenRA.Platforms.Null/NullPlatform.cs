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
using OpenRA.Primitives;

namespace OpenRA.Platforms.Null
{
	/// <summary>
	/// Headless platform for bot-only matches (A/B, campaign, learning runs): selected with
	/// Game.Platform=Null. The engine loads it by file name (OpenRA.Platforms.Null.dll in the bin dir).
	/// It opens no window and creates no GPU textures: sprite sheets hand their pixel buffer to a
	/// texture that only remembers its size, so the buffer is released instead of uploaded (the
	/// default platform holds ~3.2 GiB of 2048x2048 sheets per match). The window reports itself
	/// suspended, which makes Game.Loop skip every RenderTick while LogicTick (world + UI) runs
	/// unchanged. Rendering never feeds the simulation, so the sim and its sync hashes are the
	/// same as with the default platform (verified by a same-seed replay comparison, see
	/// docs/LESSONS_LEARNED.md "Headless bot matches"). Fonts and the dummy sound engine are
	/// delegated to the default platform so text metrics stay real.
	/// </summary>
	public class NullPlatform : IPlatform
	{
		IPlatform defaultPlatform;

		IPlatform DefaultPlatform => defaultPlatform ??= Game.CreatePlatform("Default");

		public IPlatformWindow CreateWindow(
			Size size, WindowMode windowMode, float scaleModifier, int vertexBatchSize, int indexBatchSize, int videoDisplay, GLProfile profile)
		{
			Log.Write("graphics", "Null platform: headless window, rendering and textures disabled.");
			return new NullPlatformWindow(size);
		}

		public ISoundEngine CreateSound(string device)
		{
			// Always the dummy engine: a headless match never plays audio, whatever Sound.Device says.
			return DefaultPlatform.CreateSound("none");
		}

		public IFont CreateFont(byte[] data)
		{
			return DefaultPlatform.CreateFont(data);
		}
	}

	sealed class NullPlatformWindow : IPlatformWindow
	{
		readonly Size size;

		public NullPlatformWindow(Size size)
		{
			this.size = size;
			Context = new NullGraphicsContext();
		}

		public IGraphicsContext Context { get; }
		public Size NativeWindowSize => size;
		public Size EffectiveWindowSize => size;
		public float NativeWindowScale => 1f;
		public float EffectiveWindowScale => 1f;
		public Size SurfaceSize => size;
		public int DisplayCount => 1;
		public int CurrentDisplay => 0;
		public bool HasInputFocus => false;

		// Suspended = Game.Loop skips RenderTick entirely and keeps logic ticking.
		public bool IsSuspended => true;

		public GLProfile GLProfile => GLProfile.Modern;
		public GLProfile[] SupportedGLProfiles => [GLProfile.Modern];

		public event Action<float, float, float, float> OnWindowScaleChanged { add { } remove { } }

		public void PumpInput(IInputHandler inputHandler) { }
		public string GetClipboardText() { return ""; }
		public bool SetClipboardText(string text) { return false; }
		public bool TryOpenUrl(string url) { return false; }
		public void GrabWindowMouseFocus() { }
		public void ReleaseWindowMouseFocus() { }
		public IHardwareCursor CreateHardwareCursor(string name, Size size, byte[] data, int2 hotspot, bool pixelDouble) { return new NullHardwareCursor(); }
		public void SetHardwareCursor(IHardwareCursor cursor) { }
		public void SetWindowTitle(string title) { }
		public void SetRelativeMouseMode(bool mode) { }
		public void SetScaleModifier(float scale) { }

		public void Dispose()
		{
			Context.Dispose();
		}
	}

	sealed class NullHardwareCursor : IHardwareCursor
	{
		public void Dispose() { }
	}

	sealed class NullGraphicsContext : IGraphicsContext
	{
		public string GLVersion => "Null";

		public IVertexBuffer<T> CreateEmptyVertexBuffer<T>(int size) where T : struct { return new NullVertexBuffer<T>(); }
		public IVertexBuffer<T> CreateVertexBuffer<T>(T[] data, bool dynamic = true) where T : struct { return new NullVertexBuffer<T>(); }
		public T[] CreateVertices<T>(int size) where T : struct { return new T[size]; }
		public IIndexBuffer CreateIndexBuffer(uint[] indices) { return new NullIndexBuffer(); }
		public ITexture CreateTexture() { return new NullTexture(); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new NullFrameBuffer(s); }
		public IFrameBuffer CreateFrameBuffer(Size s, Color clearColor) { return new NullFrameBuffer(s); }
		public IShader CreateShader(IShaderBindings shaderBindings) { return new NullShader(); }
		public void EnableScissor(int x, int y, int width, int height) { }
		public void DisableScissor() { }
		public void Present() { }
		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices) { }
		public void DrawElements(int numIndices, int offset) { }
		public void Clear() { }
		public void EnableDepthBuffer() { }
		public void DisableDepthBuffer() { }
		public void ClearDepthBuffer() { }
		public void SetBlendMode(BlendMode mode) { }
		public void SetVSyncEnabled(bool enabled) { }
		public void Dispose() { }
	}

	sealed class NullVertexBuffer<T> : IVertexBuffer<T> where T : struct
	{
		public void Bind() { }
		public void SetData(T[] vertices, int length) { }
		public void SetData(ref T[] vertices, int length) { }
		public void SetData(T[] vertices, int offset, int start, int length) { }
		public void Dispose() { }
	}

	sealed class NullIndexBuffer : IIndexBuffer
	{
		public void Bind() { }
		public void Dispose() { }
	}

	sealed class NullShader : IShader
	{
		public void SetBool(string name, bool value) { }
		public void SetVec(string name, float x) { }
		public void SetVec(string name, float x, float y) { }
		public void SetVec(string name, float x, float y, float z) { }
		public void SetVec(string name, ReadOnlyMemory<float> vec, int length) { }
		public void SetVec(string name, float[] vec, int components, int count) { }
		public void SetTexture(string param, ITexture texture) { }
		public void SetMatrix(string param, float[] mtx) { }
		public void PrepareRender() { }
		public void Bind() { }
	}

	/// <summary>Keeps only the size; the pixel data is dropped so the caller's buffer can be collected.</summary>
	sealed class NullTexture : ITexture
	{
		public NullTexture() { }

		public NullTexture(Size size)
		{
			Size = size;
		}

		public Size Size { get; private set; }
		public TextureScaleFilter ScaleFilter { get; set; }

		public void SetData(byte[] colors, int width, int height) { Size = new Size(width, height); }
		public void SetSubData(byte[] colors, int xoffset, int yoffset, int width, int height) { }
		public void SetFloatData(float[] data, int width, int height) { Size = new Size(width, height); }
		public void SetDataFromReadBuffer(Rectangle rect) { }

		// Read-back (screenshots, a sheet re-buffering after release) gets blank pixels of the right size.
		public byte[] GetData() { return new byte[4 * Size.Width * Size.Height]; }

		public void Dispose() { }
	}

	sealed class NullFrameBuffer : IFrameBuffer
	{
		public NullFrameBuffer(Size size)
		{
			Texture = new NullTexture(size);
		}

		public ITexture Texture { get; }
		public void Bind() { }
		public void Unbind() { }
		public void EnableScissor(Rectangle rect) { }
		public void DisableScissor() { }
		public void Dispose() { }
	}
}
