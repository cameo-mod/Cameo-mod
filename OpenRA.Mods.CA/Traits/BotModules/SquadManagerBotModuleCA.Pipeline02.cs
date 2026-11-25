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

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 (CONSOLIDATE_02_SQUAD_DESIGN.md 4): the world wiring of the army plan. Everything here is read only behind Info.SquadPipeline02Enabled (default false,
	// never set in @classic) and its DEV rows: with the switch off no snapshot is built, no role is read and no order changes, so the bot plays exactly as before.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablatePipeline02;
		AblationLatch ablateSqRoles;
		int armyPlanNextTick;
		ArmyPlan armyPlan = ArmyPlan.Empty;
		readonly List<ArmyPlanSquad> armyPlanSquads = new();

		/// <summary>The master switch of the squad pipeline: the Info switch and the DEV row sq_pipeline_02.</summary>
		internal bool ArmyPipelineOn => Info.SquadPipeline02Enabled && !ablatePipeline02.Off(Player, AblationIds.SqPipeline02);

		/// <summary>Q2 Step 1 (row sq_roles): one role per unit, the harass cap by value, attached squads follow the main squad.</summary>
		internal bool ArmyRolesOn => ArmyPipelineOn && !ablateSqRoles.Off(Player, AblationIds.SqRoles);

		/// <summary>The last Step 1 result (Empty until the first plan pass, and always with the switch off).</summary>
		internal ArmyPlan ArmyPlanNow => armyPlan;

		internal static ArmySquadKind ArmySquadKindOf(SquadCAType type) => type switch
		{
			SquadCAType.Rush => ArmySquadKind.Rush,
			SquadCAType.Guerrilla or SquadCAType.Harass => ArmySquadKind.Raid,
			SquadCAType.Protection => ArmySquadKind.Protection,
			SquadCAType.Artillery or SquadCAType.FireSupport or SquadCAType.Support => ArmySquadKind.Attached,
			_ => ArmySquadKind.Domain
		};

		/// <summary>Where the squad stands: gathering for the team order, a pushed team squad, quiet (Idle), or busy.</summary>
		internal static ArmyHolder ArmyHolderOf(SquadCA s)
		{
			var state = s.FuzzyStateMachine?.CurrentState;
			if (state is TeamLeadGatherStateCA)
				return ArmyHolder.Gathering;
			if (AggTeamIsQuietState(state))
				return ArmyHolder.Quiet;
			return s.Type == SquadCAType.Rush && s.TeamPushSeq > 0 ? ArmyHolder.Pushed : ArmyHolder.Busy;
		}

		/// <summary>Once per ArmyPlanTicks (switch ON only): the snapshot and Step 1, then the attached squads follow the main squad.</summary>
		void ArmyPlanTick()
		{
			if (!ArmyRolesOn || !BotPeriod.CarryDue(ref armyPlanNextTick, World.WorldTick, Info.ArmyPlanTicks))
				return;

			armyPlanSquads.Clear();
			for (var i = 0; i < Squads.Count; i++)
			{
				var s = Squads[i];
				if (!s.IsValid)
					continue;

				armyPlanSquads.Add(new ArmyPlanSquad(i, ArmySquadKindOf(s.Type), ArmyHolderOf(s), s.Units.Count, SquadValueOf(s)));
			}

			long poolValue = 0;
			var poolUnits = 0;
			foreach (var u in unitsHangingAroundTheBase)
			{
				if (unitCannotBeOrdered(u.Actor))
					continue;

				poolUnits++;
				poolValue += UnitValue(u.Actor);
			}

			armyPlan = ArmyPlanCA.Assign(armyPlanSquads, poolUnits, poolValue, Info.HarassCapPercent, HarassMinOn ? Info.HarassMinUnits : 1);
			ArmyAttachParents();

#if CAMEO_DEV_LOGGING
			if (DiagnosticWriter != null)
			{
				var p = armyPlan;
				DiagnosticTeamEvent("army_plan", "main=" + p.MainUnits + "/" + p.MainValue + " harass=" + p.HarassUnits + "/" + p.HarassValue + " cap=" + (p.HarassCapValue == long.MaxValue ? -1 : p.HarassCapValue)
					+ " attached=" + p.AttachedUnits + "/" + p.AttachedValue + " defence=" + p.DefenceUnits + "/" + p.DefenceValue + " other=" + p.OtherUnits
					+ " demoted=" + p.DemotedSmall + "," + p.DemotedOverCap + " main_squad=" + p.MainSquadKey);
			}
#endif
		}

		/// <summary>
		/// The seat's main squad, measured now (the plan cache may predate a wave formed this tick): the Rush squad of the team order with the most value, else the most
		/// valuable Rush squad. Null = no Rush squad.
		/// </summary>
		SquadCA ArmyMainSquad()
		{
			SquadCA best = null;
			var bestInOrder = false;
			long bestValue = -1;
			foreach (var s in Squads)
			{
				if (!s.IsValid || s.Type != SquadCAType.Rush)
					continue;

				var holder = ArmyHolderOf(s);
				var inOrder = holder is ArmyHolder.Gathering or ArmyHolder.Pushed;
				var value = SquadValueOf(s);
				if (best != null && !(inOrder && !bestInOrder) && !(inOrder == bestInOrder && value > bestValue))
					continue;

				best = s;
				bestInOrder = inOrder;
				bestValue = value;
			}

			return best;
		}

		/// <summary>
		/// The parent an orphaned attached squad takes when a wave forms: with sq_roles on the seat's main squad (never just the newest squad, design L18), else the old
		/// rule (the new wave). Null = keep the current parent.
		/// </summary>
		SquadCA ArmyAttachParent(SquadCA newWave)
		{
			if (ArmyRolesOn && ArmyMainSquad() is { } main)
				return main;

			return newWave.IsValid ? newWave : null;
		}

		// The artillery and support squads follow the main squad; the fire support follows the biggest artillery squad (its old rule), else the main squad.
		void ArmyAttachParents()
		{
			var main = ArmyMainSquad();
			if (main == null)
				return;

			SquadCA artillery = null;
			foreach (var s in Squads)
				if (s.IsValid && s.Type == SquadCAType.Artillery && (artillery == null || s.Units.Count > artillery.Units.Count))
					artillery = s;

			foreach (var s in Squads)
			{
				if (!s.IsValid || s.Type is not (SquadCAType.Artillery or SquadCAType.Support or SquadCAType.FireSupport))
					continue;

				var want = s.Type == SquadCAType.FireSupport && artillery != null ? artillery : main;
				var parent = s.Parent;
				if (ReferenceEquals(parent, want))
					continue;

				var parentIsMain = parent != null && (s.Type == SquadCAType.FireSupport ? parent.Type == SquadCAType.Artillery : parent.Type == SquadCAType.Rush);
				if (ArmyPlanCA.AttachedKeepsParent(parent is { IsValid: true }, parentIsMain, parent?.Units.Count ?? 0, want.Units.Count))
					continue;

				s.Parent = want;
				DiagnosticSquadEvent(s, "army_attach", "parent=" + want.Type + " units=" + want.Units.Count);
			}
		}

		/// <summary>
		/// Q2 (sq_roles, Opus D5): the raider cap by VALUE. The seat's ground combat value is the pool plus every Rush, raid and home-guard squad plus the candidate; the raid value
		/// is its guerrilla and harass squads. True = the candidate may join a raid squad.
		/// </summary>
		bool HarassCapAllowsByValue(Actor candidate)
		{
			long total = 0;
			foreach (var u in unitsHangingAroundTheBase)
				total += UnitValue(u.Actor);

			long raid = 0;
			var raiders = 0;
			foreach (var q in Squads)
			{
				var kind = ArmySquadKindOf(q.Type);
				if (kind is not (ArmySquadKind.Rush or ArmySquadKind.Raid or ArmySquadKind.Protection))
					continue;

				var value = SquadValueOf(q);
				total += value;
				if (kind == ArmySquadKind.Raid)
				{
					raid += value;
					raiders += q.Units.Count;
				}
			}

			var candidateValue = candidate != null ? UnitValue(candidate) : 0;
			return ArmyPlanCA.HarassAllowsByValue(raid, raiders, total + candidateValue, candidateValue, Info.HarassCapPercent, HarassMinOn ? Info.HarassMinUnits : 0);
		}
	}
}
