#region Copyright & License Information

/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.LoadScreens
{
	public sealed class FitImageLoadScreen : SheetLoadScreen
	{
		const double NewSessionGapSeconds = 5.0;

		// Fade the splash in from the initially black screen on the very first load.
		// Wall-clock based, and deliberately long: at startup the OS/compositor shows the freshly-created
		// window as black for the first ~1-2.5s before it presents our composited frames. A short fade is
		// entirely consumed inside that invisible cold-start window, so the splash appears to "pop" to full.
		// Making the fade comfortably longer than the cold-start period means its tail is still ramping once
		// the window actually becomes visible. This is spec-robust: cold-start latency is roughly OS-fixed
		// (not CPU-scaled), and slower machines load longer => the splash is shown longer => more fade time.
		const double SplashFadeSeconds = 4.0;

		// Cap how much alpha can advance per painted frame so a single long blocking load between paints
		// can't skip the whole ramp in one step — keeps the fade visibly stepping even if checkpoints are sparse.
		const float MaxAlphaStepPerFrame = 0.06f;

		readonly Stopwatch splashFadeTimer = new Stopwatch();
		float splashAlpha;

		float2 scale;
		float2 logoPos;
		Sprite logo;

		Sheet lastSheet;
		int lastDensity;
		Size lastResolution;

		// Owned sheet — we bypass the base class's private sheet cache so we can swap images per session.
		string[] images;
		string splashImage;
		string currentImage;
		Sheet ownSheet;

		static bool splashShown;

		readonly Stopwatch timeSinceLastDisplay = new Stopwatch();
		readonly Stopwatch sinceLastPaint = new Stopwatch();

		[FluentReference]
		const string Loading = "loadscreen-loading";
		string[] messages = Array.Empty<string>();
		string text;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			if (info.TryGetValue("SplashImage", out var splash))
				splashImage = splash.Trim();

			if (info.TryGetValue("Image", out var raw))
			{
				images = raw.Contains(',')
					? raw.Split(',').Select(x => x.Trim()).ToArray()
					: new[] { raw };

				// Prevent the base class from loading + caching its own sheet; we manage one ourselves.
				info.Remove("Image");

				// Show splash on the very first load ever; fall back to random if none defined or already shown.
				if (!splashShown && splashImage != null)
				{
					currentImage = splashImage;
					splashShown = true;
				}
				else
					currentImage = images[Game.CosmeticRandom.Next(images.Length)];
			}

			messages = FluentProvider.GetMessage(Loading).Split('$').Select(x => x.Trim()).ToArray();

			if (messages.Length > 0)
				text = messages.Random(Game.CosmeticRandom);
		}

		public override void Display()
		{
			if (Game.Renderer == null)
				return;

			// The base SheetLoadScreen caps load-screen repaints at 5 FPS to avoid stealing CPU from loading,
			// which makes the fade visibly step ~once per 200ms. While the splash is still fading in, repaint as
			// fast as loading checkpoints allow (up to 60 FPS) so the ramp is smooth; once it reaches full alpha
			// (or this isn't the splash) fall back to the lazy 5 FPS. We reimplement rather than call base because
			// this screen manages its own sheet and ignores the base's cached one.
			var fading = splashImage != null && currentImage == splashImage && splashAlpha < 1f;
			var minInterval = fading ? 1 / 60.0 : 0.2;
			if (sinceLastPaint.IsRunning && sinceLastPaint.Elapsed.TotalSeconds < minInterval)
				return;

			sinceLastPaint.Restart();

			Game.Renderer.BeginUI();
			DisplayInner(Game.Renderer, null, 1);
			Game.Renderer.EndFrame(new NullInputHandler());
		}

		public override void DisplayInner(Renderer r, Sheet s, int density)
		{
			// Re-randomize when DisplayInner hasn't fired for a while — i.e. the player was at the menu between loads.
			// On the very first call Elapsed is zero (stopwatch never started), so this correctly evaluates to false
			// and leaves the values Init() already chose untouched.
			var isNewSession = timeSinceLastDisplay.Elapsed.TotalSeconds > NewSessionGapSeconds;
			timeSinceLastDisplay.Restart();

			if (isNewSession)
			{
				if (messages.Length > 0)
					text = messages.Random(Game.CosmeticRandom);

				if (images != null && images.Length > 0)
				{
					var pick = images[Game.CosmeticRandom.Next(images.Length)];

					// Only churn the sheet when the pick actually changes — same image = zero I/O, zero GPU upload.
					if (pick != currentImage)
					{
						// Dispose first to keep peak GPU memory at exactly 1 sheet (no brief 2-sheet overlap).
						ownSheet?.Dispose();
						ownSheet = null;
						currentImage = pick;
					}
				}
			}

			if (ownSheet == null && currentImage != null)
			{
				using (var stream = ModData.DefaultFileSystem.Open(Platform.ResolvePath(currentImage)))
				{
					ownSheet = new Sheet(SheetType.BGRA, stream);
					ownSheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
				}
			}

			var sheet = ownSheet ?? s;
			if (sheet == null)
				return;

			if (sheet != lastSheet || density != lastDensity)
			{
				lastSheet = sheet;
				lastDensity = density;

				var rect = new Rectangle(0, 0, sheet.Size.Width, sheet.Size.Height);
				scale = new float2(r.Resolution.Width / (float)sheet.Size.Width,
					(float)r.Resolution.Height / (float)sheet.Size.Height);

				logo = scale.X > scale.Y
					? new Sprite(sheet, rect, TextureChannel.RGBA, scale.Y)
					: new Sprite(sheet, rect, TextureChannel.RGBA, scale.X);

				// Force logo position recompute now that the sheet (and therefore size) may have changed.
				lastResolution = default;
			}

			if (r.Resolution != lastResolution)
			{
				lastResolution = r.Resolution;

				logoPos = scale.X > scale.Y
					? new float2((r.Resolution.Width - sheet.Size.Width * scale.Y) / 2, 0)
					: new float2(0, (-sheet.Size.Height * scale.X + r.Resolution.Height) / 2);
			}

			// While the splash is the current image, ramp alpha from 0 to 1 so it fades up from the black screen.
			// Wall-clock target gives a consistent fade duration across machines; the per-frame cap prevents a
			// long blocking load between paints from jumping straight to full (see field comments above).
			var alpha = 1f;
			if (splashImage != null && currentImage == splashImage)
			{
				if (!splashFadeTimer.IsRunning)
					splashFadeTimer.Start();

				var target = (float)Math.Min(1.0, splashFadeTimer.Elapsed.TotalSeconds / SplashFadeSeconds);
				splashAlpha = Math.Min(target, splashAlpha + MaxAlphaStepPerFrame);
				alpha = splashAlpha;
			}

			// Fade by scaling the tint RGB (darkening toward black), NOT the tint alpha. The sprite shader uses
			// premultiplied-alpha blending (GL_ONE, GL_ONE_MINUS_SRC_ALPHA) and does c *= vTint, so reducing only
			// the alpha channel leaves the colour at full brightness over the black background — no visible fade.
			if (logo != null)
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos, 1f, new float3(alpha, alpha, alpha), 1f);

			if (r.Fonts != null && messages.Length > 0)
			{
				// Same reasoning for text: scale the colour toward black rather than dropping its alpha.
				var lit = (int)(255 * alpha);
				var textSize = r.Fonts["Bold"].Measure(text);
				r.Fonts["Bold"].DrawTextWithContrast(text,
					new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20),
					Color.FromArgb(lit, lit, lit), Color.Black, 2);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				ownSheet?.Dispose();

			base.Dispose(disposing);
		}
	}
}
