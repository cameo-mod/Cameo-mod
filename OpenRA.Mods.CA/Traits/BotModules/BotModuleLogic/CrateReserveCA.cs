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

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>CRATE-RESERVE-01: what the crate pickup does this scan.</summary>
	public enum CrateMode
	{
		/// <summary>A yard stands (an MCV may exist): reserved crates are left alone, others are collected.</summary>
		Normal,

		/// <summary>No yard and no MCV: hunt every crate, one unit each, spare units search unexplored areas.</summary>
		Hunt,

		/// <summary>No yard but an MCV: the hunt is over, every unit escorts the MCV until it deploys.</summary>
		Escort,
	}

	// CRATE-RESERVE-01. Pure decisions, no state.
	public static class CrateReserveCA
	{
		/// <summary>No own construction yard and no own MCV: the bot needs an MCV from a crate (an allied yard does not count).</summary>
		public static bool NeedsMcv(int ownYards, int ownMcvs) => ownYards <= 0 && ownMcvs <= 0;

		public static CrateMode ModeFor(int ownYards, int ownMcvs) =>
			ownYards > 0 ? CrateMode.Normal : ownMcvs > 0 ? CrateMode.Escort : CrateMode.Hunt;

		/// <summary>A crate this many cells (or fewer) from a yard is reserved for the MCV chance.</summary>
		public static bool Reserved(int cellsToNearestYard, int radiusCells) => cellsToNearestYard <= radiusCells;

		/// <summary>Whether the crate may be collected: with no yard and no MCV every crate is hunted; otherwise only crates outside the reserve and outside known threat.</summary>
		public static bool Eligible(bool needMcv, bool reserved, bool threatened) => needMcv || (!reserved && !threatened);

		/// <summary>An escorting unit is re-ordered only when it has fallen farther than the radius behind the MCV.</summary>
		public static bool NeedsEscortOrder(int cellsFromMcv, int radiusCells) => cellsFromMcv > radiusCells;

		/// <summary>
		/// Greedy distinct assignment: repeatedly the globally closest unassigned (unit, target) pair, so every target gets at most one unit and every unit at most one target. Ties go to the
		/// lower unit index (the caller orders units cheapest first) then the lower target index. Returns (unit index, target index) pairs.
		/// </summary>
		public static List<(int Unit, int Target)> AssignDistinct(IReadOnlyList<(int X, int Y)> units, IReadOnlyList<(int X, int Y)> targets)
		{
			var result = new List<(int, int)>();
			var unitTaken = new bool[units.Count];
			var targetTaken = new bool[targets.Count];
			for (var n = Math.Min(units.Count, targets.Count); n > 0; n--)
			{
				long best = long.MaxValue;
				var bu = -1;
				var bt = -1;
				for (var u = 0; u < units.Count; u++)
				{
					if (unitTaken[u])
						continue;

					for (var t = 0; t < targets.Count; t++)
					{
						if (targetTaken[t])
							continue;

						long dx = units[u].X - targets[t].X;
						long dy = units[u].Y - targets[t].Y;
						var d = dx * dx + dy * dy;
						if (d < best)
						{
							best = d;
							bu = u;
							bt = t;
						}
					}
				}

				if (bu < 0)
					break;

				unitTaken[bu] = true;
				targetTaken[bt] = true;
				result.Add((bu, bt));
			}

			return result;
		}
	}
}
