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
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// STALEMATE-SCOUT-01 (row ss_scout_push; USER decision D5 on audit A7). A push that is ready with no known target never waits at home: it scouts the unscouted or stale regions of the tactical map
	// (IBotScoutCellProvider, the scout module's region memory) and attacks what it finds; the plain-grid hunt (FinishHuntCell) stays the fallback. Only an unreachable target excuses it.
	// Fog: the figures are the bot's own region memory, its shroud and its own units; no enemy actor, trait or order stream is read. No per-tick work: the pick runs when the leader
	// publishes (or a hunting squad asks), the DEV rows only when the writer exists.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSsScoutPush;
		IBotScoutCellProvider[] scoutCellProviders;
		readonly Dictionary<CPos, int> scoutRejected = new();
		int scoutLastChecks;
		bool scoutLastExhausted;
		string scoutLastSource = "none";
		CPos? scoutHuntCell;
		int scoutHuntTick = -1;
		int scoutIdleLogTick = -1000;

		/// <summary>ss_scout_push: a ready push with no known target scouts the stale regions (needs the one launcher; the leader's pick is the only caller).</summary>
		internal bool StalemateScoutOn => ArmyLaunchArbiterOn && Info.StalemateScoutEnabled && !ablateSsScoutPush.Off(Player, AblationIds.SsScoutPush);

		/// <summary>The leader's pick may end in a hunt cell from the first ready push (StalemateScoutStartTick), not only in the late game or an all-in.</summary>
		internal bool StalemateScoutHunts => StalemateScoutCA.Hunts(false, StalemateScoutOn, World.WorldTick, Info.StalemateScoutStartTick);

		// FinishHuntCell, row on: the best reachable stale region of the tactical map, as a cell the leader's locomotor can stand on. Null = none (the grid pick follows).
		CPos? StalemateScoutCell(Actor leader, Mobile mobile)
		{
			scoutLastChecks = 0;
			scoutLastExhausted = false;
			scoutLastSource = "none";
			scoutCellProviders ??= Player.PlayerActor.TraitsImplementing<IBotScoutCellProvider>().ToArray();
			var map = World.Map;
			var now = World.WorldTick;
			var from = leader.Location;
			var resolved = default(CPos);

			bool Legal(CPos c) => map.Contains(c) && mobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;
			bool Accept(CPos center)
			{
				if (scoutRejected.TryGetValue(center, out var refused) && StalemateScoutCA.Rejected(refused, now))
					return false;

				var cell = mobile.NearestCell(center, Legal, 1, 3);
				if (Legal(cell) && mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, from, cell))
				{
					resolved = cell;
					return true;
				}

				scoutRejected[center] = now;
				return false;
			}

			foreach (var provider in scoutCellProviders)
			{
				if (!provider.IsTraitEnabled())
					continue;

				var found = provider.TryPickScoutCell(from, Info.FinishHuntMaxChecks, Accept, out _, out var checks, out var exhausted);
				scoutLastChecks = System.Math.Max(scoutLastChecks, checks);
				scoutLastExhausted |= exhausted;
				if (!found)
					continue;

				scoutLastSource = "map";
				huntAssigned[resolved] = now;
				return resolved;
			}

			return null;
		}

		// The leader published nothing for lack of a target: with the row on the DEV row says why (unreachable = the excused case).
		void StalemateScoutIdle(bool picked, int now, bool allIn, int blobUnits, long blobValue)
		{
			if (picked || !StalemateScoutOn || DiagnosticWriter == null || now - scoutIdleLogTick < 250)
				return;

			scoutIdleLogTick = now;
			var verdict = StalemateScoutCA.Classify(false, false, scoutLastChecks, scoutLastExhausted);
			StalemateScoutRecord("scout_idle", TeamLeadOpenOrder(), "verdict=" + verdict.ToString().ToLowerInvariant() + " excused=" + (StalemateScoutCA.Excused(verdict) ? 1 : 0)
				+ " allin=" + (allIn ? 1 : 0) + " blob=" + blobUnits + "/" + blobValue + " checked=" + scoutLastChecks);
		}

		// The leader published an order: a hunt cell (scouting) or a real target (attack, with whether the last scouted cell was reached).
		void StalemateScoutPublished(TeamLeadOrder order, uint targetId, WPos target, int now)
		{
			if (!StalemateScoutOn || DiagnosticWriter == null)
				return;

			var cell = World.Map.CellContaining(target);
			if (targetId == 0)
			{
				var prev = scoutHuntCell is { } p ? " prev=" + p.X + "," + p.Y + " prev_reached=" + (Player.Shroud.IsVisible(p) ? 1 : 0) : "";
				scoutHuntCell = cell;
				scoutHuntTick = now;
				StalemateScoutRecord("scout_order", order, "verdict=scout src=" + scoutLastSource + " cell=" + cell.X + "," + cell.Y + prev);
				return;
			}

			if (scoutHuntCell is { } hunted)
			{
				StalemateScoutRecord("scout_found", order, "verdict=attack target=" + targetId + " since=" + (now - scoutHuntTick) + " cell_seen=" + (Player.Shroud.IsVisible(hunted) ? 1 : 0));
				scoutHuntCell = null;
			}
		}

		// One shape for every row: the all-in reason, the order state, the eligible vs the committed force.
		void StalemateScoutRecord(string evt, TeamLeadOrder order, string detail)
		{
			var now = World.WorldTick;
			var (eligibleUnits, eligibleValue) = teamLeadTeam != null ? teamLeadTeam.Eligible(now, Info.TeamLeadPoolMaxAgeTicks) : (0, 0L);
			var (committedUnits, committedValue) = order != null ? order.CommittedTotals() : (0, 0L);
			var arrivedUnits = order != null ? order.ArrivedTotals().Units : 0;
			var state = order == null ? "none" : order.GatherOnly ? "gather" : order.Pushed ? "pushed" : "rally";
			DiagnosticTeamEvent(evt, "seq=" + (order?.Seq ?? 0) + " why=" + (order != null && order.AllIn ? order.AllInWhy : "-") + " state=" + state
				+ " eligible=" + eligibleUnits + "/" + eligibleValue + " committed=" + committedUnits + "/" + committedValue + " arrived=" + arrivedUnits + " " + detail);
		}
	}
}
