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
		int huntRetryAt;

		internal bool LateEscalationActive => Stalemate01CA.Late(Info.LateEscalationEnabled, World.WorldTick, Info.LateEscalationStartTick)
			&& !ablateLateEscalation.Off(Player, AblationIds.StalemateLateEscalation);

		internal bool FinishModeActive => Stalemate01CA.Late(Info.FinishModeEnabled, World.WorldTick, Info.LateEscalationStartTick)
			&& !ablateFinishMode.Off(Player, AblationIds.StalemateFinishMode);

		int LateDelay(int reset) => LateEscalationActive
			? Stalemate01CA.Scale(reset, Stalemate01CA.SteppedPercent(World.WorldTick, Info.LateEscalationStartTick, Info.LateEscalationDelayPercent,
				Info.LateEscalationStepTicks, Info.LateEscalationStepPercent, Info.LateEscalationDelayFloorPercent))
			: reset;

		// STALEMATE-01 r5: no late launch-bar scaling (USER 11-24: big blob pushes, not frequent trickles).

		AblationLatch ablateBlobLeaderOrderOnly;

		/// <summary>BLOB-LAUNCH-01: the leader attacks only through an open team order (row bl_leader_order_only).</summary>
		internal bool BlobLeaderOrderOnlyActive => Info.BlobLeaderOrderOnly && !ablateBlobLeaderOrderOnly.Off(Player, AblationIds.BlobLeaderOrderOnly);

		/// <summary>BLOB-LAUNCH-01: the leader's own CreateAttackForce forms no wave (see BlobLaunch01CA.LeaderHolds; fallback included).</summary>
		bool BlobLeaderHolds(bool teamLaunch)
		{
			if (!BlobLeaderOrderOnlyActive || !teamLaunch || !TeamLeadIsLeader || !TeamLeadCadenceOn || TeamLeadOpenOrder() != null)
				return false;

			var now = World.WorldTick;
			var pool = teamLeadTeam?.Pool(now, Info.TeamLeadPoolMaxAgeTicks).Units ?? 0;
			var minUnits = BlobLaunch01CA.MinUnits(WaveMinActive ? WaveMinBars().Units : 0, pool);
			var since = BlobLaunch01CA.TicksSinceOrder(now, teamLeadTeam?.LastOrderTick ?? -1, initialAttackDelay);
			return BlobLaunch01CA.LeaderHolds(true, true, true, false, since, Info.BlobLeaderFallbackTicks, pool, minUnits);
		}

		/// <summary>Finish-mode hunt pick for a squad whose leader is <paramref name="leader"/>: null when none is reachable.</summary>
		internal CPos? FinishHuntCell(Actor leader)
		{
			var mobile = leader?.TraitOrDefault<Mobile>();
			if (mobile == null || World.WorldTick < huntRetryAt)
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
				// Frontrunner FIX: an unreachable cell is marked too, so it rotates behind the untried cells instead of heading every pick.
				if (!mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, from, cell))
				{
					huntAssigned[cell] = World.WorldTick;
					continue;
				}

				huntAssigned[cell] = World.WorldTick;
				return cell;
			}

			huntRetryAt = World.WorldTick + Stalemate01CA.HuntRetryTicks;
			return null;
		}
	}
}
