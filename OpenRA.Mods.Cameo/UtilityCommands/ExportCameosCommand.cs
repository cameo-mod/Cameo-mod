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
using System.Linq;
using System.Text.RegularExpressions;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Exports the production cameo (icon sequence) of every buildable actor to an
	// individual PNG in one ModData preload, for the external tech-tree website.
	// Each cameo is rendered with the actor's declared Buildable.IconPalette (so
	// d2kunit/ra2/windows256/... cameos get their correct colours, not chrome).
	// File names match the website extractor's icon_basename(): lowercased actor
	// name with any non [a-z0-9._-] run collapsed to '_', plus ".png".
	sealed class ExportCameosCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--export-cameos";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("[OUTPUT-DIR]",
			"Export every buildable actor's cameo icon to a PNG (default dir 'cameos').")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var rules = modData.DefaultRules;

			var outDir = args.Length > 1 ? args[1] : "cameos";
			Directory.CreateDirectory(outDir);

			var palettes = LoadPalettes(modData, rules);
			palettes.TryGetValue("chrome", out var chrome);

			// A representative tileset for sprite resolution (cameos are tileset-independent;
			// CameoSpriteSequence only varies by tileset for explicit TilesetFilenames).
			var tileset = modData.DefaultTerrainInfo.Keys
				.FirstOrDefault(t => t.Equals("TEMPERAT", StringComparison.OrdinalIgnoreCase))
				?? modData.DefaultTerrainInfo.Keys.First();

			using var sequences = new SequenceSet(modData.ModFiles, modData, tileset, null);
			sequences.LoadSprites();
			var haveImage = new HashSet<string>(sequences.Images, StringComparer.Ordinal);

			int ok = 0, missing = 0, failed = 0;
			foreach (var actorInfo in rules.Actors)
			{
				if (actorInfo.Key.StartsWith(ActorInfo.AbstractActorPrefix))
					continue;

				var buildable = actorInfo.Value.TraitInfoOrDefault<BuildableInfo>();
				if (buildable == null)
					continue;

				var render = actorInfo.Value.TraitInfoOrDefault<RenderSpritesInfo>();
				var image = (render != null ? render.GetImage(actorInfo.Value, null) : actorInfo.Key).ToLowerInvariant();
				var iconSeq = string.IsNullOrEmpty(buildable.Icon) ? "icon" : buildable.Icon;

				if (!haveImage.Contains(image) || !sequences.HasSequence(image, iconSeq))
				{
					missing++;
					continue;
				}

				var palName = string.IsNullOrEmpty(buildable.IconPalette) ? "chrome" : buildable.IconPalette;
				var palette = palettes.GetValueOrDefault(palName) ?? chrome;

				try
				{
					var sprite = sequences.GetSequence(image, iconSeq).GetSprite(0);
					var png = SpriteToPng(sprite, palette);
					if (png == null)
					{
						failed++;
						continue;
					}

					png.Save(Path.Combine(outDir, Canonical(actorInfo.Key)));
					ok++;
				}
				catch (Exception)
				{
					failed++;
				}
			}

			Console.Error.WriteLine($"export-cameos: wrote {ok} cameos to {outDir} ({missing} without an icon sequence, {failed} failed)");
		}

		// Resolve every named palette the cameos might reference (chrome, d2kunit, ra2,
		// windows256, ra2cameo, ...) to a concrete palette. File palettes are read
		// directly; player-color palettes resolve to their base palette (cameos are
		// drawn neutral, so the un-remapped base is the right colour set).
		static Dictionary<string, ImmutablePalette> LoadPalettes(ModData modData, Ruleset rules)
		{
			var worldInfo = rules.Actors[SystemActors.World];

			var filePalettes = new Dictionary<string, ImmutablePalette>(StringComparer.Ordinal);
			foreach (var ti in worldInfo.TraitInfos<TraitInfo>())
			{
				if (ti is not IProvidesCursorPaletteInfo provides)
					continue;

				// PaletteFromFileInfo/PaletteFromPngInfo et al. expose the palette name as a `Name` field.
				if (ti.GetType().GetField("Name")?.GetValue(ti) is not string name || string.IsNullOrEmpty(name))
					continue;
				if (filePalettes.ContainsKey(name))
					continue;

				try { filePalettes[name] = provides.ReadPalette(modData.DefaultFileSystem); }
				catch (Exception) { }
			}

			// player-color palette prefix -> base palette name
			var playerBase = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var ti in worldInfo.TraitInfos<PlayerColorPaletteInfo>())
				if (!string.IsNullOrEmpty(ti.BaseName) && !playerBase.ContainsKey(ti.BaseName))
					playerBase[ti.BaseName] = ti.BasePalette;

			var resolved = new Dictionary<string, ImmutablePalette>(filePalettes, StringComparer.Ordinal);
			foreach (var kv in playerBase)
			{
				if (resolved.ContainsKey(kv.Key))
					continue;
				var baseName = kv.Value;
				var depth = 0;
				while (baseName != null && depth++ < 8 && !filePalettes.ContainsKey(baseName))
					baseName = playerBase.GetValueOrDefault(baseName);
				if (baseName != null && filePalettes.TryGetValue(baseName, out var pal))
					resolved[kv.Key] = pal;
			}

			return resolved;
		}

		static string Canonical(string actorName) =>
			Regex.Replace(actorName.ToLowerInvariant(), "[^a-z0-9._-]+", "_") + ".png";

		// Sheet.cs / Util.cs pack indexed sprites' bytes at this offset within each RGBA
		// quad for a given logical TextureChannel (Red=0, Green=1, Blue=2, Alpha=3) - the
		// channel enum value is NOT the byte offset (see Util.ChannelMasks: "our channel
		// order is nuts"). Using the enum value directly reads the wrong sprite's data for
		// Red/Blue-channel sprites (Green/Alpha happen to be self-mapped).
		static readonly int[] ChannelByteOffset = { 2, 1, 0, 3 };

		// Crop a single sprite out of its (shared) sheet into a standalone PNG.
		static Png SpriteToPng(Sprite sprite, IPalette palette)
		{
			var b = sprite.Bounds;
			if (b.Width <= 0 || b.Height <= 0)
				return null;

			var sheet = sprite.Sheet;
			var data = sheet.GetData();
			var stride = 4 * sheet.Size.Width;

			if (sheet.Type == SheetType.Indexed)
			{
				if (palette == null)
					return null;

				// Each sheet pixel packs four sprites' palette indices (one per channel).
				var ch = ChannelByteOffset[(int)sprite.Channel];
				var plane = new byte[b.Width * b.Height];
				for (var y = 0; y < b.Height; y++)
					for (var x = 0; x < b.Width; x++)
						plane[y * b.Width + x] = data[(b.Top + y) * stride + 4 * (b.Left + x) + ch];

				var palColors = new Color[Palette.Size];
				for (var i = 0; i < Palette.Size; i++)
					palColors[i] = palette.GetColor(i);

				return new Png(plane, SpriteFrameType.Indexed8, b.Width, b.Height, palColors);
			}

			// BGRA sheet: copy the sub-rectangle directly.
			var bgra = new byte[b.Width * b.Height * 4];
			for (var y = 0; y < b.Height; y++)
				Array.Copy(data, (b.Top + y) * stride + 4 * b.Left, bgra, y * b.Width * 4, b.Width * 4);

			return new Png(bgra, SpriteFrameType.Bgra32, b.Width, b.Height);
		}
	}
}
