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
using OpenRA.Support;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P1 (CONSOLIDATE_03_P1_DESIGN_01.md): the named inputs of the ONE building-pick arbiter. Each binding user rule keeps its memory name here; none may be
	/// deleted without a user ruling. The stage order and the walk are BaseBuildPlanCA's; this names who admitted a pick and who refused a candidate.
	/// </summary>
	public enum BuildingRule
	{
		None = 0,

		// S1: power short now (the A5 power swap rides on every stage).
		Power,

		// S2 (bb_refinery_s2): the yard / zone refinery. Memory: designer-expansion-refinery.
		ExpansionRefinery,

		// S3 (opening_standard_order). Memory: designer-opening-build-order.
		Opening,

		// S4 (bb_antirush_set; P1 (b) early_def_learn): the early anti-rush set, EarlyDefencePlan. Memory: designer-early-defence-all-bots-2026-11-24.
		EarlyDefence,

		// S5, first copies, in reason order: a listed BuildingFractions entry (bb_build_all_listed), a capture unlock (cs_capture_unlock), a tech-depth first copy.
		// Memories: designer-build-everything-bias-only-2026-11-25, designer-each-building-once-then-army, designer-tech-progression-never-blocked.
		FirstCopyListed,
		FirstCopyCaptureUnlock,
		FirstCopyTechDepth,

		// S6 (bb_passive_income). Memory: designer-each-building-once-then-army (passive income to the build limit).
		PassiveIncome,

		// S7: the refinery per field. Memory: designer-refinery-spacing.
		RefineryPerField,

		// S8 (pb_cycle_production, pb_sustained_cash_production, bb_s8_rejected_fallthrough, bb_s8_cash_hold: an eligible factory waiting only for cash holds this
		// building queue's S10 for at most S8HoldMaxTicks). Memory: designer-production-always-busy.
		ProductionCycle,

		// S9 (bb_extra_defence, af2_defence_cap, RATIO-2X): the scaled defence after a seen threat. Memory: designer-building-defence-limits-2026-11-24.
		ScaledDefence,

		// S10: the BuildingFractions roll (share cap, intervals, mode bias). Memory: designer-build-everything-bias-only-2026-11-25.
		Fractions,
	}

	/// <summary>Why the walk refused a stage or a candidate.</summary>
	public enum BuildingReason
	{
		// A closed stage (no candidate name): QUEUE-SPLIT keeps it off this queue; bb_s8_cash_hold holds S10 for a factory waiting only for cash; the cash tiers close it
		// (below the tier line / S8 under the line); the S8 offer holds it.
		QueueSplit,
		S8CashHold,
		TierCut,
		CycleHold,

		// A refused candidate: its stage caps (no test spent), A1-A6, A1-A6 on cash only while it takes the S8 cash hold, the A7 claim, or the same type failed A earlier
		// this sweep.
		StageCap,
		Infeasible,
		S8CashOnly,
		ClaimRefused,
		AlreadyFailed,

		// The K feasibility tests are spent: the walk stops with no pick.
		TestCap,
	}

	/// <summary>One refusal: the candidate (null for a closed stage or the K stop), the stage, why, and the named rule of that stage.</summary>
	public readonly record struct BuildingRefusal(string Name, BaseBuildStage Stage, BuildingReason Reason, BuildingRule Rule);

	/// <summary>The arbiter's answer: the pick (BaseBuildPick, unchanged) and the named rule that decided it (None = no pick).</summary>
	public readonly record struct BuildingPickDecision(BaseBuildPick Pick, BuildingRule Decider);

	/// <summary>
	/// CONSOLIDATE-03 P1 (a): the ONE building-pick arbiter. It returns the winner, the deciding rule and (when asked) every refusal of the walk. The walk itself is
	/// BaseBuildPlanCA.Plan, unchanged: the same stages, the same tests, the same K bound, the same random draws. Pure, no World.
	/// </summary>
	public static class BuildingPickArbiter
	{
		/// <summary>The pick and its deciding rule. <paramref name="refusals"/> null = no recording (release); else every refusal is appended in walk order.</summary>
		public static BuildingPickDecision Decide(BaseBuildSnapshot snapshot, IBaseBuildFeasibility feasibility, MersenneTwister rng, List<BuildingRefusal> refusals)
		{
			var pick = BaseBuildPlanCA.Plan(snapshot, feasibility, rng, refusals);
			return new BuildingPickDecision(pick, pick.IsNone ? BuildingRule.None : RuleOf(pick.Stage, snapshot, pick.Candidate));
		}

		/// <summary>The named rule of a stage. S5 names the reason of <paramref name="name"/>: listed (a BuildingFractions entry with a weight), capture unlock, else tech depth.</summary>
		public static BuildingRule RuleOf(BaseBuildStage stage, BaseBuildSnapshot s, string name)
		{
			switch (stage)
			{
				case BaseBuildStage.S1Power: return BuildingRule.Power;
				case BaseBuildStage.S2ExpansionRefinery: return BuildingRule.ExpansionRefinery;
				case BaseBuildStage.S3Opening: return BuildingRule.Opening;
				case BaseBuildStage.S4AntiRush: return BuildingRule.EarlyDefence;
				case BaseBuildStage.S5CoreOnce: return FirstCopyRule(s, name);
				case BaseBuildStage.S6PassiveIncome: return BuildingRule.PassiveIncome;
				case BaseBuildStage.S7RefineryPerField: return BuildingRule.RefineryPerField;
				case BaseBuildStage.S8ProductionCycle: return BuildingRule.ProductionCycle;
				case BaseBuildStage.S9ExtraDefence: return BuildingRule.ScaledDefence;
				case BaseBuildStage.S10Fractions: return BuildingRule.Fractions;
				default: return BuildingRule.None;
			}
		}

		static BuildingRule FirstCopyRule(BaseBuildSnapshot s, string name)
		{
			if (name == null)
				return BuildingRule.FirstCopyListed;

			if (s.Fractions != null)
				foreach (var kv in s.Fractions)
					if (kv.Key == name && kv.Value > 0)
						return BuildingRule.FirstCopyListed;

			return s.CaptureUnlockOf != null && s.CaptureUnlockOf(name) ? BuildingRule.FirstCopyCaptureUnlock : BuildingRule.FirstCopyTechDepth;
		}

		/// <summary>Records why <paramref name="stage"/> is closed to the walk, if it is: the same pure tests the walk runs next, in the same order. No-op when open.</summary>
		public static void NoteClosed(List<BuildingRefusal> refusals, BaseBuildStage stage, BaseBuildSnapshot s, bool cycleOffered)
		{
			if (!QueueStageSplit.Allowed(stage, s.QueueSplit, s.DefenceQueue))
			{
				refusals.Add(new BuildingRefusal(null, stage, BuildingReason.QueueSplit, RuleOf(stage, s, null)));
				return;
			}

			if (stage == BaseBuildStage.S10Fractions && BaseBuildPlanCA.S8CashHoldActive(s))
			{
				refusals.Add(new BuildingRefusal(null, stage, BuildingReason.S8CashHold, RuleOf(stage, s, null)));
				return;
			}

			var holds = BaseBuildTiersCA.CycleHolds(cycleOffered, s.S8RejectedFallthrough);
			if (BaseBuildTiersCA.Open(stage, s.TierLine, s.Bank, s.LowCashCut, s.SustainedActive, s.DefenceThreatSeen, holds))
				return;

			// Closed only because the S8 offer holds it = the cycle hold; closed even without the hold = the cash tiers.
			var reason = holds && BaseBuildTiersCA.Open(stage, s.TierLine, s.Bank, s.LowCashCut, s.SustainedActive, s.DefenceThreatSeen, false)
				? BuildingReason.CycleHold : BuildingReason.TierCut;
			refusals.Add(new BuildingRefusal(null, stage, reason, RuleOf(stage, s, null)));
		}

		/// <summary>DEV rows: the first <paramref name="max"/> refusals as "stage:name:reason" joined by ';' (name '-' for a closed stage), then "+n" for the rest.</summary>
		public static string Summary(IReadOnlyList<BuildingRefusal> refusals, int max)
		{
			if (refusals == null || refusals.Count == 0)
				return "";

			var b = new System.Text.StringBuilder();
			for (var i = 0; i < refusals.Count && i < max; i++)
			{
				var r = refusals[i];
				if (i > 0)
					b.Append(';');

				b.Append(r.Stage).Append(':').Append(r.Name ?? "-").Append(':').Append(r.Reason);
			}

			if (refusals.Count > max)
				b.Append(";+").Append(refusals.Count - max);

			return b.ToString();
		}

		/// <summary>Records a refused candidate. No-op when <paramref name="refusals"/> is null (release).</summary>
		public static void Refuse(List<BuildingRefusal> refusals, BaseBuildSnapshot s, BaseBuildCandidate candidate, BuildingReason reason)
		{
			refusals?.Add(new BuildingRefusal(candidate.Name, candidate.Stage, reason, RuleOf(candidate.Stage, s, candidate.Name)));
		}
	}
}
