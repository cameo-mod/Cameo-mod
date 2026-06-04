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

using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Provides a deterministic visual offset derived from this actor's map cell.",
		"Use this to break up grid-aligned effects while keeping gameplay placement unchanged.")]
	public class DeterministicCellOffsetInfo : TraitInfo, Requires<IOccupySpaceInfo>
	{
		public readonly WVec MinOffset = WVec.Zero;
		public readonly WVec MaxOffset = WVec.Zero;

		[Desc("Extra value mixed into the cell hash so different effect families can use different offsets.")]
		public readonly int Salt = 0;

		public override object Create(ActorInitializer init) { return new DeterministicCellOffset(init.Self, this); }
	}

	public class DeterministicCellOffset
	{
		public readonly WVec Offset;

		public DeterministicCellOffset(Actor self, DeterministicCellOffsetInfo info)
		{
			var cell = self.Location;
			var xHash = Hash(cell.X, cell.Y, info.Salt);
			var yHash = Hash(cell.X, cell.Y, info.Salt ^ 0x6D2B79F5);
			var zHash = Hash(cell.X, cell.Y, info.Salt ^ 0x1B873593);

			Offset = new WVec(
				Range(xHash, info.MinOffset.X, info.MaxOffset.X),
				Range(yHash, info.MinOffset.Y, info.MaxOffset.Y),
				Range(zHash, info.MinOffset.Z, info.MaxOffset.Z));
		}

		static uint Hash(int x, int y, int salt)
		{
			unchecked
			{
				var h = 2166136261u;
				h = (h ^ (uint)x) * 16777619u;
				h = (h ^ (uint)y) * 16777619u;
				h = (h ^ (uint)salt) * 16777619u;
				h ^= h >> 16;
				h *= 2246822519u;
				h ^= h >> 13;
				h *= 3266489917u;
				h ^= h >> 16;
				return h;
			}
		}

		static int Range(uint hash, int min, int max)
		{
			if (max <= min)
				return min;

			return min + (int)(hash % (uint)(max - min + 1));
		}
	}
}
