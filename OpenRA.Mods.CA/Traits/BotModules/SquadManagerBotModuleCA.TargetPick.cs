#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 Q6 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 3): the world wiring of TargetPick. The order of every purpose is the pure TargetPick table; each tier is resolved
	// here from the primitives the old pickers used (FindClosestEnemy, FindHighValueTarget, FindFrozenEnemyTarget, the finish focus, FinishHuntCell), so a tier gives the same pick
	// it always gave and only the order and the composition are new. Behind the row sq_target_pick (under sq_pipeline_02); with it off no caller reaches this file.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqTargetPick;

		/// <summary>
		/// Q6 (row sq_target_pick): the squads' own pick (FindNewTarget), the leader's pick (TeamLeadPickTarget), the DF-4 raid retarget and the defence squads' target follow the
		/// TargetPick order of their purpose. Off = the old pickers, unchanged.
		/// </summary>
		internal bool ArmyTargetPickOn => ArmyPipelineOn && !ablateSqTargetPick.Off(Player, AblationIds.SqTargetPick);

		/// <summary>Raids pick as HARASS, every other ground squad as MAIN.</summary>
		internal static TargetPurpose TargetPurposeOf(SquadCAType type) =>
			type is SquadCAType.Guerrilla or SquadCAType.Harass ? TargetPurpose.Harass : TargetPurpose.Main;

		// What the bot may pick: an enemy it sees (the code base's own rule: IsPreferredObservedEnemyUnit; the legacy omniscient mode keeps its meaning).
		bool TargetObserved(Actor a) => !FoggedScans || IsNotHiddenUnit(a);

		// Artillery prefers buildings (Step 3): the global preference, or a squad that is half artillery or more.
		bool TargetPrefersStructures(SquadCA squad)
		{
			var artillery = 0;
			foreach (var u in squad.Units)
				if (IsArtilleryUnit(u.Actor))
					artillery++;

			return TargetPick.PrefersStructures(Info.PreferBuildingTargets, artillery, squad.Units.Count);
		}

		/// <summary>
		/// A squad's own pick: the tiers of its purpose in order, the first that resolves sets the squad's target. <paramref name="tryHighValue"/> is the caller's priority roll
		/// (a raid only), <paramref name="riskGated"/> applies the 6c pre-commit risk gate of the squad's value, <paramref name="allowed"/> restricts the tiers (DF-4).
		/// <paramref name="frozenNeedsFog"/>: the remembered-building tier needs fogged scans (the state pick's rule); DF-4 never had that guard and passes false.
		/// False = nothing known: the squad keeps its target (or none).
		/// </summary>
		internal bool PickSquadTarget(SquadCA squad, bool tryHighValue, bool riskGated, TargetTier allowed = TargetTier.All, bool frozenNeedsFog = true)
		{
			var source = squad.Units[0].Actor;
			var from = source.CenterPosition;
			var risk = riskGated ? SquadValueOf(squad) : -1;
			var structures = TargetPrefersStructures(squad);
			var purpose = TargetPurposeOf(squad.Type);
			if (!tryHighValue)
				allowed &= ~TargetTier.HighValue;

			var tier = TargetPick.Resolve(TargetPick.Order(purpose, allowed), t => t switch
			{
				TargetTier.Focus => TargetFocusFor(squad),
				TargetTier.SharedHarass => TargetSharedHarassFor(squad),
				TargetTier.HighValue => TargetHighValueFor(squad, from, risk),
				TargetTier.Visible => TargetVisibleFor(squad, source, risk, structures),
				TargetTier.Frozen => TargetFrozenFor(squad, from, risk, frozenNeedsFog),
				TargetTier.Hunt => TargetHuntFor(squad, source),
				_ => false
			});

			if (tier == null)
				return false;

			// The raid's own pick becomes the group's target, so the other raids of the team join it instead of picking another (X14).
			if (purpose == TargetPurpose.Harass && (tier == TargetTier.HighValue || tier == TargetTier.Visible) && squad.TargetActor != null)
				HarassPublish(squad.TargetActor);

#if CAMEO_DEV_LOGGING
			DiagnosticSquadEvent(squad, "target_pick", reason: purpose + ":" + tier);
#endif
			return true;
		}

		// The finish focus' nearest known building: seen now, else remembered. The focus is one per team and the leader refreshes it each team check (from the home position), so a
		// squad only READS it: a squad that refreshed it would pick the building nearest to itself and move the target under the leader.
		bool TargetFocusFor(SquadCA squad)
		{
			var team = teamLeadTeam;
			if (team == null || !TeamLeadFinishWeakestOn || !team.FocusHasTarget || team.FocusTargetId == 0)
				return false;

			var id = team.FocusTargetId;

			var actor = World.GetActorById(id);
			if (actor != null && !actor.IsDead && actor.IsInWorld && TargetObserved(actor))
			{
				squad.TargetActor = actor;
				return true;
			}

			var frozen = Player.FrozenActorLayer?.FromID(id);
			if (frozen != null && frozen.IsValid && !frozen.Hidden)
			{
				squad.Target = Target.FromFrozenActor(frozen);
				return true;
			}

			return false;
		}

		bool TargetSharedHarassFor(SquadCA squad)
		{
			var shared = HarassSharedTarget();
			if (shared == null)
				return false;

			squad.TargetActor = shared;
			return true;
		}

		/// <summary>The team's live joint harass target if this bot can see it, else null.</summary>
		internal Actor HarassSharedTarget()
		{
			var team = teamLeadTeam;
			if (team == null || !TeamLeadJointHarassOn || team.HarassTargetId == 0 || !TeamLeadCA.HarassLive(World.WorldTick, team.HarassTick, Info.TeamLeadHarassTicks))
				return null;

			var shared = World.GetActorById(team.HarassTargetId);
			return shared != null && !shared.IsDead && shared.IsInWorld && IsPreferredObservedEnemyUnit(shared) ? shared : null;
		}

		// A pick never replaces a live shared target, so the target does not flap between bots with different views.
		void HarassPublish(Actor pick)
		{
			var team = teamLeadTeam;
			if (team == null || !TeamLeadJointHarassOn || HarassSharedTarget() != null)
				return;

			team.HarassTargetId = pick.ActorID;
			team.HarassTick = World.WorldTick;
		}

		bool TargetHighValueFor(SquadCA squad, WPos from, int risk)
		{
			var target = risk >= 0 ? FindHighValueTarget(from, risk) : FindHighValueTarget(from);
			if (target == null)
				return false;

			squad.TargetActor = target;
			return true;
		}

		bool TargetVisibleFor(SquadCA squad, Actor source, int risk, bool structures)
		{
			var target = risk >= 0 ? FindClosestEnemy(source, risk, squad, structures) : FindClosestEnemy(source, squad, structures);
			if (target == null)
				return false;

			squad.TargetActor = target;
			return true;
		}

		// The remembered-building fallback of a state pick exists only under fogged scans (without them every enemy is a candidate of the visible tier); DF-4 reads the layer as it always did.
		bool TargetFrozenFor(SquadCA squad, WPos from, int risk, bool needsFog)
		{
			if (needsFog && !FoggedScans)
				return false;

			var frozen = FindFrozenEnemyTarget(from, risk, squad);
			if (frozen == null)
				return false;

			squad.Target = Target.FromFrozenActor(frozen);
			return true;
		}

		// STALEMATE-01 finish mode: late game, nothing seen and nothing remembered, the squad hunts the least recently visited unseen cell it can reach, unless the order hunts for the team.
		bool TargetHuntFor(SquadCA squad, Actor source)
		{
			if (!FinishModeActive || !Stalemate01CA.Hunts(squad.Type) || TeamLeadHuntsViaOrder(squad))
				return false;

			var cell = FinishHuntCell(source);
			if (cell == null)
				return false;

			squad.Target = Target.FromCell(World, cell.Value);
			squad.HuntCell = cell;
			return true;
		}

		/// <summary>
		/// The leader's pick of the order target (MAIN): the finish focus, the nearest enemy seen (the main-target seat first, no risk gate), a remembered building, then the one
		/// hunt cell the order may carry (late game with the finish mode, or an all-in with nothing known). The order is the TargetPick table; each tier is what
		/// TeamLeadPickTarget always did.
		/// </summary>
		bool TeamLeadPickMain(WPos from, int now, out WPos target, out uint actorId)
		{
			var picked = default(WPos);
			var pickedId = 0u;
			var hunts = (TeamLeadHuntViaOrderOn && FinishModeActive) || teamLeadAllInPicking || StalemateScoutHunts;
			var allowed = hunts ? TargetTier.All : TargetTier.All & ~TargetTier.Hunt;
			var tier = TargetPick.Resolve(TargetPick.Order(TargetPurpose.Main, allowed), t =>
			{
				switch (t)
				{
					case TargetTier.Focus:
						return TeamLeadFocusTarget(from, now, out picked, out pickedId);

					case TargetTier.Visible:
					{
						var mainTarget = EffectiveMainTarget();
						var seen = mainTarget != null ? FindClosestEnemy(World.Map.CellContaining(from), TeamLeadOpenRisk, mainTarget) : null;
						if (seen == null)
						{
							var source = unitsHangingAroundTheBase.Count > 0 ? unitsHangingAroundTheBase[0].Actor : activeUnits.FirstOrDefault(a => !a.IsDead && a.IsInWorld);
							seen = source != null ? FindClosestEnemy(source) : null;
						}

						if (seen == null)
							return false;

						picked = seen.CenterPosition;
						pickedId = seen.ActorID;
						return true;
					}

					case TargetTier.Frozen:
					{
						if (!FoggedScans || FindFrozenEnemyTarget(from, -1) is not { } frozen)
							return false;

						picked = frozen.CenterPosition;
						pickedId = frozen.ID;
						return true;
					}

					case TargetTier.Hunt:
					{
						// An all-in order hunts from a ground unit: the first pool unit may be an aircraft with no locomotor.
						var huntSource = teamLeadAllInPicking || StalemateScoutHunts ? TeamLeadHuntSample()
							: unitsHangingAroundTheBase.Count > 0 ? unitsHangingAroundTheBase[0].Actor : activeUnits.FirstOrDefault(a => !a.IsDead && a.IsInWorld);
						if (FinishHuntCell(huntSource) is not { } hunt)
							return false;

						picked = World.Map.CenterOfCell(hunt);
						pickedId = 0;
						return true;
					}

					default:
						return false;
				}
			});

			target = picked;
			actorId = pickedId;
			return tier != null;
		}

		/// <summary>
		/// A defence squad's target (DEFENCE): the nearest armed enemy the bot sees around <paramref name="anchor"/> (what is defended), else the enemy that reported the hit if it
		/// is seen. A harvester or a scout that happened to hit first no longer draws the guard away from the armed attackers. Null = nothing seen.
		/// </summary>
		internal Actor PickDefenceTarget(Actor reported, WPos anchor)
		{
			Actor picked = null;
			TargetPick.Resolve(TargetPick.Order(TargetPurpose.Defence), t =>
			{
				if (t == TargetTier.ArmedAttacker)
				{
					var near = World.FindActorsInCircle(anchor, WDist.FromCells(Info.DefendAllyAttackerScanCells))
						.Where(a => !a.IsDead && a.IsInWorld && IsPreferredEnemyUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>() && !a.Info.HasTraitInfo<HuskInfo>()
							&& !a.Info.HasTraitInfo<BuildingInfo>())
						.ToList();
					var seen = near.Select(a => new TargetSeen(a.ActorID, (a.CenterPosition - anchor).HorizontalLengthSquared, TargetObserved(a), true, false)).ToList();
					var i = TargetPick.Nearest(seen);
					if (i < 0)
						return false;

					picked = near[i];
					return true;
				}

				if (reported == null || reported.IsDead || !reported.IsInWorld || !TargetObserved(reported))
					return false;

				picked = reported;
				return true;
			});

			return picked;
		}
	}
}
