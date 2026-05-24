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
		const double NewSessionGapSeconds = 15.0;

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

			if (logo != null)
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos);

			if (r.Fonts != null && messages.Length > 0)
			{
				var textSize = r.Fonts["Bold"].Measure(text);
				r.Fonts["Bold"].DrawTextWithContrast(text,
					new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20),
					Color.White, Color.Black, 2);
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
