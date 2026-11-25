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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-01 c6 (CONSOLIDATE_01_DESIGN.md 3.1, rules R7 and R9): the cash tiers of the staged planner as pure functions. L is the tier line
	/// (BotLimits MaximiseProductionCashRequirement, 0 = none). Below L the pipeline stops after S7 (essentials only), except that S9 may still run once a
	/// threat is seen. S8 (the production cycle) needs the bank at or above L. SUSTAINED-CASH (the bank held at or above L long enough) moves S8 above S7 and,
	/// while the cycle still offers a type, suppresses S9 and S10; S1, S2, S3, S4 and S5 stay above it (the user correction, pb_sustained_keeps_essentials).
	/// </summary>
	public static class BaseBuildTiersCA
	{
		// SUSTAINED order: S8 sits directly above S7, the rest is the normal order.
		static readonly BaseBuildStage[] SustainedOrder =
		[
			BaseBuildStage.S1Power,
			BaseBuildStage.S2ExpansionRefinery,
			BaseBuildStage.S3Opening,
			BaseBuildStage.S4AntiRush,
			BaseBuildStage.S5CoreOnce,
			BaseBuildStage.S6PassiveIncome,
			BaseBuildStage.S8ProductionCycle,
			BaseBuildStage.S7RefineryPerField,
			BaseBuildStage.S9ExtraDefence,
			BaseBuildStage.S10Fractions,
		];

		/// <summary>The stages in walk order: the normal order, or the SUSTAINED order while sustained cash production is active (it needs a tier line).</summary>
		public static IReadOnlyList<BaseBuildStage> Order(bool sustainedActive, int tierLine) =>
			sustainedActive && tierLine > 0 ? SustainedOrder : BaseBuildPlanCA.Stages;

		/// <summary>
		/// Whether the planner walks <paramref name="stage"/> at all.
		/// <paramref name="lowCashCut"/> = the R9 switch (pb_essentials_low_cash): below L only S1-S7 (and a threat-seen S9) run.
		/// <paramref name="cycleOffered"/> = S8 offered at least one type this sweep, i.e. the source ENUMERATED one (an exhausted cycle, every type at its limit, lets normal
		/// building resume). Enumerated, not admitted, on purpose: a cycle type that then fails A (reserve, power, delay) or is held by an admission vote still holds S9 and S10 back for that sweep, so
		/// SUSTAINED cash is saved for the production building instead of leaking into extras.
		/// </summary>
		public static bool Open(BaseBuildStage stage, int tierLine, int bank, bool lowCashCut, bool sustainedActive, bool threatSeen, bool cycleOffered)
		{
			var below = tierLine > 0 && bank < tierLine;
			var suppressed = sustainedActive && tierLine > 0 && cycleOffered;
			switch (stage)
			{
				case BaseBuildStage.S8ProductionCycle:
					return tierLine > 0 && !below;
				case BaseBuildStage.S9ExtraDefence:
					return (!lowCashCut || !below || threatSeen) && !suppressed;
				case BaseBuildStage.S10Fractions:
					return (!lowCashCut || !below) && !suppressed;
				default:
					return true;
			}
		}
	}
}
