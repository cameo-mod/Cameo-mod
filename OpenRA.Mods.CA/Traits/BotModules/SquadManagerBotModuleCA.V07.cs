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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// V07 (USER master replay 2026-11-24, via Opus). One switch + one DEV row per item; each is read only behind its own switch, so with it off
	// the bot plays exactly as before.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablatePincerWide;

		// PINCER-02: read by the leader (it plans the prongs and decides the push); the members follow the order's own prong data.
		internal bool TeamLeadPincerWideOn => TeamLeadPincerOn && Info.TeamLeadPincerWideEnabled && !ablatePincerWide.Off(Player, AblationIds.TeamLeadPincerWide);

		// PINCER-02: a pincer that is due to push collapses into the single blob unless BOTH arms hold the arm bar (units and value of each arm on its own).
		bool TeamLeadPincerArmCollapse(TeamLeadOrder order, int now)
		{
			if (!WaveMinActive)
				return false;

			var (minUnits, minValue) = WaveMinBars();
			var (value0, _) = order.Totals(0);
			var (value1, _) = order.Totals(1);
			var units0 = order.ProngUnits(0);
			var units1 = order.ProngUnits(1);
			if (PincerWideCA.ArmsHold(units0, value0, units1, value1, minUnits, minValue, Info.TeamLeadPincerArmPercent))
				return false;

			order.PincerCollapsed = true;
			DiagnosticTeamEvent("team_pincer_collapse", "seq=" + order.Seq + " why=arm arms=" + units0 + "/" + value0 + "," + units1 + "/" + value1
				+ " bars=" + PincerWideCA.ArmBar(minUnits, Info.TeamLeadPincerArmPercent) + "/" + PincerWideCA.ArmBar(minValue, Info.TeamLeadPincerArmPercent)
				+ " age=" + (now - order.IssueTick));
			return true;
		}

		AblationLatch ablateAllIn;
		AblationLatch ablateSoloLead;

		// ALL-IN-01: set only while the leader picks the target of an all-in order (TeamLeadPickTarget then hunts when it knows no target).
		bool teamLeadAllInPicking;

		// The living seats of this bot's team at its last team check (1 = it leads alone: a 1v1 or the last survivor); for the DEV records.
		int teamLeadAliveSeats;

		// ALL-IN-01: read by the leader (it decides the trigger and marks the order); the members follow the order's own AllIn flag, so a member needs no switch of its own.
		internal bool TeamLeadAllInOn => TeamLeadOn && Info.TeamLeadAllInEnabled && !ablateAllIn.Off(Player, AblationIds.TeamLeadAllIn);

		// SOLO-LEAD: asked by the team check before the bot counts as engaged, so it does not read TeamLeadOn: a team of ONE living bot leads itself.
		bool TeamLeadSoloOn => TeamLeadActive && Info.TeamLeadSoloEnabled && !ablateSoloLead.Off(Player, AblationIds.TeamLeadSoloLead);

		// ALL-IN-01 (members and leader): the open team order is an all-in order. False whenever no order carries the flag, i.e. with the switch off.
		internal bool TeamLeadAllInOrderOpen => TeamLeadOpenOrder() is { AllIn: true };

		// ALL-IN-01 (leader only, once per team check from TeamLeadPublish): the trigger that fired - a member's idle army at its tier limit, the team idle army
		// at TeamLeadAllInIdleUnits, or the team has nearly won - or null. The nearly-won scan (the enemy structures the bot sees or remembers) runs only past
		// the start tick, with a blob that could publish at all, and only when no cheaper trigger fired.
		string TeamLeadAllInWhy(TeamLeadTeam team, int now)
		{
			if (!TeamLeadAllInOn)
				return null;

			// An open order that is already all-in, or only gathers at its rally, needs no trigger: no scan for it.
			if (TeamLeadOpenOrder() is { } open && (open.AllIn || open.GatherOnly))
				return null;

			var blob = team.Pool(now, Info.TeamLeadPoolMaxAgeTicks);
			var atCap = team.AnyAtCap(now, Info.TeamLeadPoolMaxAgeTicks);
			var huge = AllInCA.IdleHuge(blob.Units, Info.TeamLeadAllInIdleUnits);
			var won = false;
			if (!atCap && !huge && now >= Info.TeamLeadAllInStartTick)
			{
				var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);
				if (TeamLeadCA.BlobReady(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue))
				{
					var enemyValue = TeamLeadKnownEnemyStructures(out var enemyStructures);
					won = AllInCA.NearlyWon(now, Info.TeamLeadAllInStartTick, blob.Value, enemyValue, enemyStructures, Info.TeamLeadAllInWinPercent);
				}
			}

			return AllInCA.Why(atCap, huge, won);
		}

		// Fog-honest, like TeamLeadKnownBuildings: the enemy structures (walls aside) the bot sees now or remembers in its frozen-actor layer, and their summed cost.
		long TeamLeadKnownEnemyStructures(out int count)
		{
			long value = 0;
			var n = 0;
			var counted = new HashSet<uint>();
			foreach (var a in World.ActorsHavingTrait<Building>())
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null || !TeamLeadFocusEnemy(a.Owner) || a.Info.HasTraitInfo<LineBuildInfo>() || !a.CanBeViewedByPlayer(Player)
					|| !counted.Add(a.ActorID))
					continue;

				n++;
				value += a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			}

			var layer = Player.FrozenActorLayer;
			if (layer != null)
				foreach (var fa in layer.FrozenActorsInRegion(World.Map.AllCells))
				{
					if (!fa.IsValid || fa.Hidden || !TeamLeadFocusEnemy(fa.Owner) || !fa.Info.HasTraitInfo<BuildingInfo>() || fa.Info.HasTraitInfo<LineBuildInfo>() || !counted.Add(fa.ID))
						continue;

					n++;
					value += fa.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
				}

			count = n;
			return value;
		}

		// ALL-IN-01: a ground unit to path-check the hunt cells for: the first Mobile unit of the pool or the quiet wave squads, else of the active units.
		Actor TeamLeadHuntSample() =>
			TeamLeadPincerSample() ?? activeUnits.FirstOrDefault(a => !a.IsDead && a.IsInWorld && a.TraitOrDefault<Mobile>() != null);

		// ALL-IN-01 DEV records (the writer is null in a release build).
		void TeamLeadAllInRecord(string evt, TeamLeadOrder order, string detail)
		{
			if (DiagnosticWriter == null)
				return;

			DiagnosticTeamEvent(evt, "seq=" + order.Seq + " " + detail + " seats=" + teamLeadAliveSeats);
		}

		// The release of an all-in order: how many team units still stand idle at home right now (the pools the members last published).
		void TeamLeadAllInPushRecord(TeamLeadOrder order, int now)
		{
			if (DiagnosticWriter == null || !order.AllIn || order.AllInPushLogged)
				return;

			order.AllInPushLogged = true;
			var (units, value) = teamLeadTeam.Pool(now, Info.TeamLeadPoolMaxAgeTicks);
			var (committed, arrived) = order.Totals();
			TeamLeadAllInRecord("all_in_push", order, "why=" + order.AllInWhy + " age=" + (now - order.IssueTick) + " committed=" + committed + " arrived=" + arrived
				+ " idle_home=" + units + "/" + value + " pincer=" + (order.Pincer ? 1 : 0));
		}

		// PINCER-02 DEV record (the writer is null in a release build): a pincer was published or released, with the approach angle of its two staging points.
		void TeamLeadPincerRecord(string evt, TeamLeadOrder order, string detail)
		{
			if (DiagnosticWriter == null)
				return;

			var angle = PincerWideCA.SeparationDegrees(order.Target, order.ProngRally(0), order.ProngRally(1));
			DiagnosticTeamEvent(evt, "seq=" + order.Seq + " angle=" + angle + " wide=" + (TeamLeadPincerWideOn ? 1 : 0) + " " + detail);
		}
	}
}
