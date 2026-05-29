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

namespace OpenRA.Graphics
{
	// Persistent cross-map sprite cache. When supplied to a SpriteCache, holds SheetBuilders
	// and resolved sprite atlases alive across map transitions so that subsequent loads can
	// skip file I/O and atlas packing for sprites already cached.
	public sealed class SpriteCachePool : IDisposable
	{
		readonly int bgraSheetSize;
		readonly int indexedSheetSize;
		readonly int bgraSheetMargin;
		readonly int indexedSheetMargin;

		readonly Dictionary<SheetType, SheetBuilder> sheetBuilders = [];

		// The CacheKey identifies the logical transformation applied to a sprite. It is either the AdjustFrame
		// delegate itself (for callers without captured state) or a stable value type provided by the caller
		// (e.g. a ValueTuple of the captured fields) so equivalent reservations across map loads compare equal.
		public readonly Dictionary<
			(string Filename, int FrameIndex, bool Premultiplied, object CacheKey),
			Sprite> ResolvedSprites = [];

		// Total number of frames in each file. Lets cross-map checks determine "all needed frames cached?"
		// without opening the file again, even for reservations that wanted all frames (Frames == null).
		public readonly Dictionary<string, int> FrameCounts = [];

		public SpriteCachePool(int bgraSheetSize, int indexedSheetSize, int bgraSheetMargin = 1, int indexedSheetMargin = 1)
		{
			this.bgraSheetSize = bgraSheetSize;
			this.indexedSheetSize = indexedSheetSize;
			this.bgraSheetMargin = bgraSheetMargin;
			this.indexedSheetMargin = indexedSheetMargin;
		}

		public SheetBuilder GetOrCreateSheetBuilder(SheetType type)
		{
			if (sheetBuilders.TryGetValue(type, out var sb))
				return sb;

			sb = type switch
			{
				SheetType.BGRA => new SheetBuilder(SheetType.BGRA, bgraSheetSize, bgraSheetMargin),
				SheetType.Indexed => new SheetBuilder(SheetType.Indexed, indexedSheetSize, indexedSheetMargin),
				_ => throw new ArgumentException($"Unknown sheet type {type}", nameof(type))
			};

			sheetBuilders[type] = sb;
			return sb;
		}

		public IReadOnlyDictionary<SheetType, SheetBuilder> SheetBuilders => sheetBuilders;

		public void Dispose()
		{
			foreach (var sb in sheetBuilders.Values)
				sb.Dispose();

			sheetBuilders.Clear();
			ResolvedSprites.Clear();
			FrameCounts.Clear();
		}
	}
}
