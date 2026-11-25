#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// MCV-REBUILD-01. Pure decisions, no state.
	public static class McvRebuildCA
	{
		/// <summary>The player has no construction yard, no MCV and no MCV in any queue.</summary>
		public static bool NeedsMcv(int yards, int mcvs, bool queued) => yards <= 0 && mcvs <= 0 && !queued;

		/// <summary>The failed sweeps after which a waiting external request is dropped: the short limit when the fast drop is armed, otherwise the base limit.</summary>
		public static int DropLimit(bool fastArmed, int baseLimit, int fastLimit) => fastArmed ? System.Math.Min(baseLimit, System.Math.Max(1, fastLimit)) : baseLimit;
	}
}
