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

		// On the very first load the splash plays a short intro, then hands off to the normal randomized
		// loading screen for the rest of the load: FadingIn -> Holding -> FadingOut -> (swap image) ->
		// FadingInRandom -> Done. After Done the screen behaves like any normal loadscreen (full alpha,
		// isNewSession re-randomization). For non-splash loads we start at Done so none of this runs.
		enum IntroPhase { FadingIn, Holding, FadingOut, FadingInRandom, Done }

		// Fade-IN is wall-clock based and deliberately long: at startup the OS/compositor shows the freshly-
		// created window as black for the first ~1-2.5s before it presents our composited frames. A short fade
		// is entirely consumed inside that invisible cold-start window, so the splash appears to "pop" to full.
		// A longer fade keeps the tail ramping once the window is actually visible. Spec-robust: cold-start
		// latency is roughly OS-fixed (not CPU-scaled), and slower machines load longer => more fade time.
		// NOTE: this is ~2s longer than the *visible* fade-in. The first ~2s is spent inside the cold-start black,
		// so 4.0 here yields a ~2s visible ramp. For a literal 2s fade-in, set this to 2.0 (it will mostly pop).
		const double SplashFadeSeconds = 4.0;

		// Hold the fully-lit splash for a beat before retiring it.
		const double SplashHoldSeconds = 4.0;

		// Fade-OUT and the random screen's fade-IN happen after the window is already visible (no cold-start to
		// out-wait), so these are their true durations — a quick 0.5s each.
		const double TransitionFadeSeconds = 0.5;

		// Cap how much alpha can advance per painted frame so a single long blocking load between paints
		// can't skip a ramp in one step — keeps fades visibly stepping even if checkpoints are sparse.
		const float MaxAlphaStepPerFrame = 0.06f;

		readonly Stopwatch phaseTimer = new Stopwatch();
		IntroPhase phase = IntroPhase.Done;
		float introAlpha;

		// After the intro hands off to a random screen, the next >5s gap is the cold sprite-decode
		// (PrepareMap.LoadSprites, ~16s) within this SAME load — not a new session. Skip re-randomizing on it
		// so the just-faded-in screen rides through to the menu. Cleared after that one gap; genuine later
		// menu->game sessions (warm, fast) re-randomize normally.
		bool suppressNextReRandomize;

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

					// Kick off the splash intro state machine. Non-splash loads stay at Done (no intro).
					phase = IntroPhase.FadingIn;
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
			// which makes a fade visibly step ~once per 200ms. While any intro fade is in progress, repaint as
			// fast as loading checkpoints allow (up to 60 FPS) so the ramp is smooth; during the static hold and
			// once the intro is Done (or this isn't the splash) fall back to the lazy 5 FPS. We reimplement rather
			// than call base because this screen manages its own sheet and ignores the base's cached one.
			var fading = phase == IntroPhase.FadingIn || phase == IntroPhase.FadingOut || phase == IntroPhase.FadingInRandom;
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
			// and leaves the values Init() already chose untouched. Gated to Done so a long blocking load mid-intro
			// can't trip the gap check and yank the splash out from under the running transition.
			var isNewSession = phase == IntroPhase.Done && timeSinceLastDisplay.Elapsed.TotalSeconds > NewSessionGapSeconds;
			timeSinceLastDisplay.Restart();

			if (isNewSession)
			{
				if (suppressNextReRandomize)
				{
					// The first big gap after the intro is the cold sprite-decode in this same load, not a new
					// session — leave the just-faded-in screen alone. Clear so genuine later sessions re-randomize.
					suppressNextReRandomize = false;
				}
				else
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

			// Drive the splash intro. Each fade uses a wall-clock target (consistent duration across machines)
			// clamped by a per-frame cap (so a long blocking load between paints can't jump the ramp). Phases
			// advance on the alpha reaching its target; the timer restarts on each transition. Done => full alpha.
			var alpha = 1f;
			switch (phase)
			{
				case IntroPhase.FadingIn:
					if (!phaseTimer.IsRunning)
						phaseTimer.Start();

					introAlpha = Math.Min(RampTarget(SplashFadeSeconds), introAlpha + MaxAlphaStepPerFrame);
					alpha = introAlpha;
					if (introAlpha >= 1f)
						Advance(IntroPhase.Holding);
					break;

				case IntroPhase.Holding:
					alpha = 1f;
					if (phaseTimer.Elapsed.TotalSeconds >= SplashHoldSeconds)
						Advance(IntroPhase.FadingOut);
					break;

				case IntroPhase.FadingOut:
					introAlpha = Math.Max(1f - RampTarget(TransitionFadeSeconds), introAlpha - MaxAlphaStepPerFrame);
					alpha = introAlpha;
					if (introAlpha <= 0f)
					{
						// Splash is now black — retire it and swap to a random loading screen. The new sheet loads
						// on the next paint (ownSheet == null), which is fine since this frame draws at alpha 0.
						if (images != null && images.Length > 0)
						{
							ownSheet?.Dispose();
							ownSheet = null;
							currentImage = images[Game.CosmeticRandom.Next(images.Length)];
						}

						Advance(IntroPhase.FadingInRandom);
					}

					break;

				case IntroPhase.FadingInRandom:
					introAlpha = Math.Min(RampTarget(TransitionFadeSeconds), introAlpha + MaxAlphaStepPerFrame);
					alpha = introAlpha;
					if (introAlpha >= 1f)
					{
						phase = IntroPhase.Done;
						suppressNextReRandomize = true;
					}

					break;
			}

			// Fade by scaling the tint RGB (darkening toward black), NOT the tint alpha. The sprite shader uses
			// premultiplied-alpha blending (GL_ONE, GL_ONE_MINUS_SRC_ALPHA) and does c *= vTint, so reducing only
			// the alpha channel leaves the colour at full brightness over the black background — no visible fade.
			if (logo != null)
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos, 1f, new float3(alpha, alpha, alpha), 1f);

			// Tips belong to the loading screen, not the branding splash. Only draw them from the random screen
			// onward (FadingInRandom + Done) so the tip fades in exactly once with that screen — rather than
			// appearing over the splash, fading out with it, then fading the same tip back in.
			var showTips = phase == IntroPhase.FadingInRandom || phase == IntroPhase.Done;
			if (r.Fonts != null && messages.Length > 0 && showTips)
			{
				// Same reasoning for text: scale the colour toward black rather than dropping its alpha.
				var lit = (int)(255 * alpha);
				var textSize = r.Fonts["Bold"].Measure(text);
				r.Fonts["Bold"].DrawTextWithContrast(text,
					new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20),
					Color.FromArgb(lit, lit, lit), Color.Black, 2);
			}
		}

		// Wall-clock ramp [0,1] for the current phase, based on time since the phase began.
		float RampTarget(double seconds)
		{
			return (float)Math.Min(1.0, phaseTimer.Elapsed.TotalSeconds / seconds);
		}

		// Move to the next intro phase and restart the phase clock; introAlpha carries over as the ramp's start.
		void Advance(IntroPhase next)
		{
			phase = next;
			phaseTimer.Restart();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				ownSheet?.Dispose();

			base.Dispose(disposing);
		}
	}
}
