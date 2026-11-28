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
	/// <summary>
	/// DEF-LANES-01: extra static-defence budget from remembered attack lanes, in permille on top of the DEF-RATIO budget (0 = none; 500 = half as much
	/// again). The base builder divides its RATIO-2X factor by (1000 + extra) / 1000. A provider that is off or disabled returns 0, so the factor is
	/// unchanged. Providers live in OpenRA.Mods.Cameo and must not be referenced by name here.
	/// </summary>
	public interface IBotDefenceLaneBudget
	{
		int ExtraDefencePermille { get; }
	}

	public static partial class AblationIds
	{
		// DEF-LANES-01 row. Owner: Fable. Off = no lane memory, no extra budget, no lane placement.
		public const string DefLanes = "def_lanes";
	}

	public static class DefenceLaneBudget
	{
		/// <summary>The RATIO-2X factor (permille) after the lane budget: factor / (1 + extra). Extra 0 returns the factor unchanged.</summary>
		public static int Apply(int factorPermille, int extraPermille)
		{
			if (extraPermille <= 0)
				return factorPermille;

			return (int)((long)factorPermille * 1000 / (1000 + extraPermille));
		}
	}
}
