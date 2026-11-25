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
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	// HARASS-CAP-01 (USER P0, V02 RCA 2026-11-24): the raiders must not eat the blob. Three switches, each with its own DEV row:
	// the cap (tl_harass_cap), the minimum raid (tl_harass_min) and, in SquadManagerBotModuleCA.TeamLead.cs, the raider recall (tl_raider_recall).
	// The pure rules live in HarassCapCA; this file only measures the seat and acts on the answers. Owner: Frontrunner.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateHarassCap;
		AblationLatch ablateHarassMin;
		AblationLatch ablateRaiderRecall;

		// The tick a quiet raid squad was first seen below the minimum raid (key gone = it is big enough or busy).
		readonly Dictionary<SquadCA, int> raidSmallSince = new();

		/// <summary>The raider cap is in force (ALL game; the Info switch with the DEV row).</summary>
		internal bool HarassCapOn => Info.HarassCapEnabled && !ablateHarassCap.Off(Player, AblationIds.TeamLeadHarassCap);

		/// <summary>The minimum raid is in force (ALL game).</summary>
		internal bool HarassMinOn => Info.HarassMinEnabled && !ablateHarassMin.Off(Player, AblationIds.TeamLeadHarassMin);

		/// <summary>The raider recall is in force: team feature engaged, the switch and row on, the late-escalation start reached.</summary>
		internal bool TeamLeadRaiderRecallOn => TeamLeadOn
			&& HarassCapCA.RecallActive(Info.TeamLeadRaiderRecallEnabled && !ablateRaiderRecall.Off(Player, AblationIds.TeamLeadRaiderRecall), World.WorldTick, Info.LateEscalationStartTick);

		/// <summary>
		/// One more unit may join a guerrilla/harass squad (true when the cap is off). The seat's combat units are its pool plus every squad's units
		/// (support squads aside) plus the candidate; the raiders are the units of its guerrilla and harass squads. A refused unit falls through to the
		/// next branch of the assignment, which ends in the idle pool = the blob.
		/// </summary>
		bool HarassCapAllows(Actor candidate = null)
		{
			if (!HarassCapOn)
				return true;

			// CONSOLIDATE-02 Q2 (sq_roles, Opus D5): under the squad pipeline the cap is measured by army VALUE, not by unit count.
			if (ArmyRolesOn)
				return HarassCapAllowsByValue(candidate);

			var total = unitsHangingAroundTheBase.Count + 1;
			var raiders = 0;
			foreach (var q in Squads)
			{
				if (q.Type == SquadCAType.Support)
					continue;

				var n = q.Units.Count;
				total += n;
				if (q.Type is SquadCAType.Guerrilla or SquadCAType.Harass)
					raiders += n;
			}

			return HarassCapCA.Allows(raiders, total, Info.HarassCapPercent, HarassMinOn ? Info.HarassMinUnits : 0);
		}

		/// <summary>
		/// A quiet guerrilla/harass squad below the minimum raid does not update (no launch): a guerrilla squad merges into a bigger quiet one with room,
		/// otherwise it holds, and after HarassMinHoldTicks its units go to the idle pool. True = the squad was handled and must not update this pass.
		/// </summary>
		bool HarassMinHolds(SquadCA s)
		{
			if (!s.IsValid || s.Type is not (SquadCAType.Guerrilla or SquadCAType.Harass) || !HarassMinOn)
				return false;

			if (!AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState))
			{
				raidSmallSince.Remove(s);
				return false;
			}

			var units = s.Units.Count;
			if (units >= Info.HarassMinUnits)
			{
				raidSmallSince.Remove(s);
				return false;
			}

			var now = World.WorldTick;
			if (!raidSmallSince.TryGetValue(s, out var since))
			{
				if (raidSmallSince.Count > Squads.Count)
					PruneRaidSmallSince();

				raidSmallSince[s] = since = now;
			}

			var target = s.Type == SquadCAType.Guerrilla ? HarassMergeTarget(s) : null;
			switch (HarassCapCA.RaidDecision(units, Info.HarassMinUnits, now - since, Info.HarassMinHoldTicks, target != null))
			{
				case HarassCapCA.RaidAction.Merge:
					DiagnosticSquadEvent(s, "raid_merge", "units=" + units + " into=" + target.Units.Count);
					RetreatRallyDetach(s);
					ResolveMissionAttempt(s, BotMissionAttemptState.Released, BotMissionReasons.Superseded);
					target.Units.AddRange(s.Units);
					s.Units.Clear();
					raidSmallSince.Remove(s);
					return true;

				case HarassCapCA.RaidAction.ToPool:
					DiagnosticSquadEvent(s, "raid_pool", "units=" + units + " held=" + (now - since));
					DismissSquad(s);
					raidSmallSince.Remove(s);
					return true;

				case HarassCapCA.RaidAction.Hold:
					return true;

				default:
					return false;
			}
		}

		void PruneRaidSmallSince()
		{
			List<SquadCA> gone = null;
			foreach (var key in raidSmallSince.Keys)
				if (!Squads.Contains(key))
					(gone ??= new List<SquadCA>()).Add(key);

			if (gone != null)
				foreach (var key in gone)
					raidSmallSince.Remove(key);
		}

		// The biggest other quiet guerrilla squad that can take all of this squad's units (the bigger of two equals - the earlier in the list - takes the other).
		SquadCA HarassMergeTarget(SquadCA s)
		{
			SquadCA best = null;
			var sIndex = Squads.IndexOf(s);
			for (var i = 0; i < Squads.Count; i++)
			{
				var t = Squads[i];
				if (ReferenceEquals(t, s) || t.Type != SquadCAType.Guerrilla || !t.IsValid || !AggTeamIsQuietState(t.FuzzyStateMachine?.CurrentState))
					continue;

				if (t.Units.Count + s.Units.Count > Info.MaxGuerrillaSize || t.Units.Count < s.Units.Count || (t.Units.Count == s.Units.Count && i > sIndex))
					continue;

				if (best == null || t.Units.Count > best.Units.Count)
					best = t;
			}

			return best;
		}
	}
}
