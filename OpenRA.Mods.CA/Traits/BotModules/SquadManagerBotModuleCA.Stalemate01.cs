#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// STALEMATE-01 (USER BINDING 11-23): late escalation and the finish-mode hunt. One switch and one DEV ablation row each.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateLateEscalation, ablateFinishMode;
		List<CPos> huntCells;
		readonly Dictionary<CPos, int> huntAssigned = new();

		internal bool LateEscalationActive => Stalemate01CA.Late(Info.LateEscalationEnabled, World.WorldTick, Info.LateEscalationStartTick)
			&& !ablateLateEscalation.Off(Player, AblationIds.StalemateLateEscalation);

		internal bool FinishModeActive => Stalemate01CA.Late(Info.FinishModeEnabled, World.WorldTick, Info.LateEscalationStartTick)
			&& !ablateFinishMode.Off(Player, AblationIds.StalemateFinishMode);

		int LateDelay(int reset) => LateEscalationActive ? Stalemate01CA.Scale(reset, Info.LateEscalationDelayPercent) : reset;

		int LateBar(int bar) => LateEscalationActive ? Stalemate01CA.Scale(bar, Info.LateEscalationForcePercent) : bar;

		/// <summary>Finish-mode hunt pick for a squad whose leader is <paramref name="leader"/>: null when none is reachable.</summary>
		internal CPos? FinishHuntCell(Actor leader)
		{
			var mobile = leader?.TraitOrDefault<Mobile>();
			if (mobile == null)
				return null;

			if (huntCells == null)
			{
				huntCells = new List<CPos>();
				var b = World.Map.Bounds;
				foreach (var (x, y) in Stalemate01CA.Grid(b.Left, b.Top, b.Right, b.Bottom, Info.FinishHuntGridCells))
				{
					var cell = new MPos(x, y).ToCPos(World.Map);
					if (World.Map.Contains(cell))
						huntCells.Add(cell);
				}
			}

			var from = leader.Location;
			var candidates = new List<(bool Visible, int LastAssigned, long DistSq)>(huntCells.Count);
			foreach (var cell in huntCells)
				candidates.Add((Player.Shroud.IsVisible(cell), huntAssigned.TryGetValue(cell, out var t) ? t : -1, (cell - from).LengthSquared));

			var checks = 0;
			foreach (var i in Stalemate01CA.HuntOrder(candidates))
			{
				if (++checks > Info.FinishHuntMaxChecks)
					break;

				var cell = huntCells[i];
				if (!mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, from, cell))
					continue;

				huntAssigned[cell] = World.WorldTick;
				return cell;
			}

			return null;
		}
	}
}
