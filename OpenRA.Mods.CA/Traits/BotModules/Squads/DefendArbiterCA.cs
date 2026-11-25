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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>CONSOLIDATE-02 Step 4: the defend/recall priorities, highest first (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1).</summary>
	public enum DefendPriority : byte
	{
		/// <summary>The own base would fall: an own construction yard or production building is hit and the attack is worth more than the home defence and the pool.</summary>
		P0OwnBaseFalls,

		/// <summary>An own priority asset is hit (or a home threat is seen) and the base would not fall.</summary>
		P1OwnThreat,

		/// <summary>An ally would fall: the attack around it is worth more than its defence (Opus D2).</summary>
		P2AllyFalls,

		/// <summary>An ally is under a serious attack and would not fall.</summary>
		P3AllySerious,

		/// <summary>A friendly near this bot's units lost hit points (DEFEND-ALLY radius).</summary>
		P4LocalHit,

		/// <summary>Escort and preposition requests.</summary>
		P5Escort
	}

	/// <summary>One unit a defence answer may take: its index in the caller's list, its value and its squared distance to the attack.</summary>
	public readonly record struct DefendCandidate(int Index, int Value, long DistSq);

	/// <summary>
	/// CONSOLIDATE-02 Q3 (row sq_defend_arbiter under the master switch sq_pipeline_02): the one defend arbiter, pure. The entry points (RespondToAttack/ProtectOwn,
	/// DEFEND-ALLY, TEAM-DEFEND, the AggTeam detachment) stay as the event feeders; each asks this table which units it may take and how much it needs, so a raid never
	/// dissolves a gathering blob (X6), one harass never drains the pool (X7), and the ally answers follow one rule (X8, X9). No world access, no RNG.
	/// </summary>
	public static class DefendArbiterCA
	{
		/// <summary>Opus D2: a side would fall when the attack around it is worth more than its defence (no percent knob).</summary>
		public static bool Falls(long attackValue, long defenceValue) => attackValue > 0 && attackValue > Math.Max(0, defenceValue);

		/// <summary>The own answer: P0 when a core building (yard, production) is hit and the base would fall to it, else P1.</summary>
		public static DefendPriority Own(bool coreHit, long attackValue, long homeDefenceValue) =>
			coreHit && Falls(attackValue, homeDefenceValue) ? DefendPriority.P0OwnBaseFalls : DefendPriority.P1OwnThreat;

		/// <summary>The ally answer: P2 when the ally would fall, else P3.</summary>
		public static DefendPriority Ally(long attackValue, long allyDefenceValue) =>
			Falls(attackValue, allyDefenceValue) ? DefendPriority.P2AllyFalls : DefendPriority.P3AllySerious;

		/// <summary>
		/// The ladder (design 4.1 Step 4 with Opus D2):
		/// - the pool answers every priority;
		/// - a quiet Main squad (Idle) answers the own threat and every ally alarm (D2: home, idle and defence pools), never a local hit or an escort;
		/// - a gathering or pushed Main squad answers only P0 (own base falls) and P2 (the ally falls);
		/// - a busy Main squad (staging, marching outside the order) answers only P0;
		/// - the home guard answers the own priorities, local hits and escorts, and an ally alarm only while it is not fighting at home (the caller passes Busy then);
		/// - raids, attached squads and the air/naval/stealth domains keep their jobs.
		/// </summary>
		public static bool MayTake(DefendPriority priority, ArmyRole role, ArmyHolder holder)
		{
			switch (role)
			{
				case ArmyRole.Main:
					return holder switch
					{
						ArmyHolder.Pool => true,
						ArmyHolder.Quiet => priority is DefendPriority.P0OwnBaseFalls or DefendPriority.P1OwnThreat or DefendPriority.P2AllyFalls or DefendPriority.P3AllySerious,
						ArmyHolder.Gathering or ArmyHolder.Pushed => priority is DefendPriority.P0OwnBaseFalls or DefendPriority.P2AllyFalls,
						_ => priority == DefendPriority.P0OwnBaseFalls
					};

				case ArmyRole.DefenceHome:
					return priority is DefendPriority.P0OwnBaseFalls or DefendPriority.P1OwnThreat or DefendPriority.P4LocalHit or DefendPriority.P5Escort
						|| holder != ArmyHolder.Busy;

				default:
					return false;
			}
		}

		/// <summary>
		/// The value an answer needs: <paramref name="marginPercent"/> percent of the attack that the defence already there does not cover (rounded up). 0 = nothing
		/// more is needed. The margin is clamped to 100-400.
		/// </summary>
		public static long Need(long attackValue, long defenceThere, int marginPercent)
		{
			var open = Math.Max(0, attackValue) - Math.Max(0, defenceThere);
			if (open <= 0)
				return 0;

			var margin = Math.Clamp(marginPercent, 100, 400);
			return (open * margin + 99) / 100;
		}

		/// <summary>
		/// Only the share needed (P0 / P1 sizing): the candidates nearest to the attack first (then the lower index) until their value reaches <paramref name="need"/>.
		/// Returns the caller's indexes in that order. A need of 0 takes nothing; a need above the total takes everyone.
		/// </summary>
		public static List<int> PickNearest(IEnumerable<DefendCandidate> candidates, long need)
		{
			var result = new List<int>();
			if (need <= 0 || candidates == null)
				return result;

			var list = new List<DefendCandidate>(candidates);
			list.Sort((a, b) =>
			{
				var d = a.DistSq.CompareTo(b.DistSq);
				return d != 0 ? d : a.Index.CompareTo(b.Index);
			});

			long value = 0;
			foreach (var c in list)
			{
				if (value >= need)
					break;

				result.Add(c.Index);
				value += Math.Max(1, c.Value);
			}

			return result;
		}

		/// <summary>An attack the bot cannot see (an attacker in the fog) is answered by at most this many units, nearest first, instead of the whole pool.</summary>
		public static int BlindDraft(int draftable, int blindUnits) => Math.Clamp(blindUnits, 0, Math.Max(0, draftable));
	}
}
