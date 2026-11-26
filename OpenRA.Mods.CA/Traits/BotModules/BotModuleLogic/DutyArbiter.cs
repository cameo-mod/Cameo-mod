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
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P7 (b), the pure policy seam of the rank rows (design 01 section 3, Opus review changes 3 and 5): rank gate, all-in claim rule with its refusal list, own-base rank (R1 or R3), and the observed-only
	/// ETA / risk eligibility. NOT wired into a decision yet: rows sq_rank_enforce, sq_all_in_ledger and sq_eta_risk wait for the baseline counters of phase (a) (Opus: run the baseline for one game before arming any
	/// (b) row), because the violations they remove are exactly what those counters measure. Pure, engine-free: no world access, no RNG, so each rule is unit-testable and the wiring afterwards is a few lines.
	/// </summary>
	public static class DutyArbiter
	{
		/// <summary>The all-in takes only from R4 and below (guard lend, team orders, raid, idle); never from R1-R3 (own base, ally defence).</summary>
		public const int AllInHighestTakenRank = 4;

		/// <summary>
		/// The rank gate: a claim may take a unit from a holder only when its rank is strictly higher (a smaller number); equal ranks never take from each other. No holder, or the same duty (a refresh), is not a take.
		/// <paramref name="coreUnderAttack"/> ranks an own-base claim R1 instead of R3 (ArmyDutyLedger.IsCoreUnderAttack); a holder's own-base duty is R3.
		/// </summary>
		public static bool MayTake(ArmyDuty claimant, ArmyDuty holder, bool coreUnderAttack = false)
		{
			if (holder == ArmyDuty.None || claimant == holder)
				return true;

			return EffectiveRank(claimant, coreUnderAttack) < ArmyDutyLedger.Rank(holder);
		}

		/// <summary>The rank of a claim: the ledger rank, except that an own-base defence of a core under attack is R1.</summary>
		public static int EffectiveRank(ArmyDuty duty, bool coreUnderAttack) =>
			duty == ArmyDuty.OwnBaseDefend && coreUnderAttack ? 1 : ArmyDutyLedger.Rank(duty);

		/// <summary>The all-in claim rule (Opus change 3): it claims everything at R4-R7, including the other team orders it replaces, and never a unit held at R1-R3.</summary>
		public static bool AllInMayTake(ArmyDuty holder) => holder == ArmyDuty.None || ArmyDutyLedger.Rank(holder) >= AllInHighestTakenRank;

		/// <summary>The units an all-in must leave alone, each with the rank that holds it (the refusal the sq_all_in_ledger row logs): "unit:R2". Sorted by unit id.</summary>
		public static List<string> AllInRefusals(IEnumerable<(uint Unit, ArmyDuty Holder)> units) =>
			units.Where(u => !AllInMayTake(u.Holder)).OrderBy(u => u.Unit)
				.Select(u => u.Unit + ":R" + ArmyDutyLedger.Rank(u.Holder)).ToList();

		/// <summary>A threat as the planner may read it (Opus change 5): it must be SEEN now or REMEMBERED (scout memory with fading certainty). A threat that is neither is an unseen actor and is never an input.</summary>
		public readonly record struct ObservedThreat(uint Id, long Value, bool Visible, bool Remembered);

		/// <summary>
		/// The risk of a path or a response: the summed value of the observed threats. An input that is neither visible nor remembered means an unseen actor was read: that is a bug in the caller, so it throws instead of
		/// silently counting a position the bot cannot know (the unit test of change 5 fails on it).
		/// </summary>
		public static long ObservedRisk(IEnumerable<ObservedThreat> threats)
		{
			long risk = 0;
			foreach (var t in threats)
			{
				if (!t.Visible && !t.Remembered)
					throw new InvalidOperationException("threat " + t.Id + " is neither seen nor remembered: an unseen actor must never be read");

				risk += Math.Max(0, t.Value);
			}

			return risk;
		}

		/// <summary>Eligibility for a draft by ETA and risk, not nearest only: the unit can be there in time and the observed risk on the way is not above the bar.</summary>
		public static bool Eligible(int etaTicks, int maxEtaTicks, long observedRisk, long maxRisk) =>
			etaTicks >= 0 && etaTicks <= maxEtaTicks && observedRisk <= maxRisk;
	}
}
