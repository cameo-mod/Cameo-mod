#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// STALEMATE-01 (USER BINDING 11-23: no stalemates). Pure decisions for late escalation and the finish-mode hunt.
	public static class Stalemate01CA
	{
		/// <summary>A tick-gated arm: on from startTick onwards.</summary>
		public static bool Late(bool enabled, int now, int startTick) => enabled && now >= startTick;

		/// <summary>Scales a positive delay or launch bar by percent (clamped 0..100), never below 1; zero or negative (= no bar) stays as is.</summary>
		public static int Scale(int value, int percent) =>
			value <= 0 ? value : Math.Max(1, (int)((long)value * Math.Clamp(percent, 0, 100) / 100));

		/// <summary>Only attack squads hunt (Sonnet MED): base defenders (Protection), harassers and support types keep their own roles.</summary>
		public static bool Hunts(SquadCAType type) => type == SquadCAType.Rush || type == SquadCAType.Guerrilla;

		/// <summary>After a pick that found no reachable cell, the next one waits this long (Sonnet LOW: no repeated path queries).</summary>
		public const int HuntRetryTicks = 75;

		/// <summary>The hunting squad's leader is within arriveCells of its hunt cell.</summary>
		public static bool Arrived(long distSq, int arriveCells) => distSq <= (long)arriveCells * arriveCells;

		/// <summary>Hunt grid in map coordinates: every step cells inside the bounds, offset half a step from the edges.</summary>
		public static List<(int X, int Y)> Grid(int left, int top, int right, int bottom, int step)
		{
			var cells = new List<(int X, int Y)>();
			if (step <= 0 || right <= left || bottom <= top)
				return cells;

			for (var y = top + step / 2; y < bottom; y += step)
				for (var x = left + step / 2; x < right; x += step)
					cells.Add((x, y));

			return cells;
		}

		/// <summary>Hunt candidates in try order: cells the bot cannot see now (fog-honest), least recently assigned first (-1 = never),
		/// then nearest, then index. Visible cells are skipped: the bot already knows nothing is there.</summary>
		public static IEnumerable<int> HuntOrder(IReadOnlyList<(bool Visible, int LastAssigned, long DistSq)> candidates) =>
			Enumerable.Range(0, candidates.Count)
				.Where(i => !candidates[i].Visible)
				.OrderBy(i => candidates[i].LastAssigned)
				.ThenBy(i => candidates[i].DistSq)
				.ThenBy(i => i);
	}
}
