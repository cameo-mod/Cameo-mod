#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class BotLimitsInfo : ConditionalTraitInfo
	{
		public readonly int ProductionTypeLimit = 1;
		public readonly int RefineryLimit = 4;
		public readonly int ConstructionYardLimit = 1;

		public readonly int BuildingDelayModifier = 100;
		public readonly int BuildingIntervalModifier = 100;

		public readonly int HarvesterLimit = 8;

		public readonly int UnitDelayModifier = 100;
		public readonly int UnitIntervalModifier = 100;

		public readonly int InitialAttackDelay = 0;

		[Desc("M13 maximum continuously eligible no-launch interval. Used only by the default-off M13 consumer.")]
		public readonly int AttackLivenessMaxNoLaunchTicks = 7500;
		[Desc("M13 first-wave cap (BV_m13_liveness): until the shared clock records its first accepted",
			"offensive wave, an eligible interval's deadline is eligibleSince + min(this,",
			"AttackLivenessMaxNoLaunchTicks). 0 = disabled.")]
		public readonly int AttackLivenessFirstWaveMaxTicks = 0;
		[Desc("M13 tempo min wave floor (BV_m13_liveness): raises the ordinary launch gate's unit-count",
			"bar only; MaxIdleUnits overflow, ValueOnlyAttackLaunch and the liveness deadline path are",
			"untouched. 0 = off.")]
		public readonly int TempoMinWaveUnits = 0;
		[Desc("M13 non-renewing response expiry from first admission; a delay on the difficulty line.")]
		public readonly int TeamResponseMaxLeaseTicks = 1500;
		[Desc("M13 response re-admission cooldown for the same claim episode.")]
		public readonly int TeamResponseRearmCooldownTicks = 750;
		[Desc("M13 concurrent responses per bot. Capacity invariant: exactly one at every tier.")]
		public readonly int TeamResponseLeaseLimit = 1;

		[Desc("Prioritize the first barracks before the first refinery for configured factions.")]
		public readonly bool PrioritizeBarracksBeforeRefinery = false;

		[Desc("Ticks the same personality candidate must persist before this difficulty switches to it. Negative disables switching.")]
		public readonly int PersonalityReactionDelay = 7500;

		[Desc("Cash above which the base builder adds more production structures to spend what it earns.",
			"Overrides BaseBuilderBotModuleCA.NewProductionCashThreshold. Negative: use the module's value.")]
		public readonly int NewProductionCashThreshold = -1;

		[Desc("Cash above which the unit builder fills every unit queue in one pass.",
			"Overrides UnitBuilderBotModuleCA.MaximiseProductionCashRequirement. Negative: use the module's value.")]
		public readonly int MaximiseProductionCashRequirement = -1;

		[Desc("M13 tempo (BV_m13_liveness): cash+resources at or above this tier bar saturate the bank — the armed",
			"unit builder stops honouring MaximiseProductionCashRequirement, fills up to two free queues per category",
			"per cycle, and retries a share-rejected pick once without the share test. 0 = off.")]
		public readonly int BankSaturationCash = 0;

		[Desc("Percent (0-100) of mobile combat picks the unit builder may spend on COUNTERS to the enemy army it",
			"has observed (through fog when the master AI observes through fog). 0 disables adaptive counters.")]
		public readonly int AdaptiveCounterWeight = 0;

		[Desc("S1b (ENGINEER-CAP-10PCT, maintainer 2026-10-12): live capturer units (engineers and kin)",
			"at most this percent of live armed infantry. Flat 10 on every tier — CapturerLimitPctCeiling",
			"clamps anything higher, so a difficulty may only go lower. Negative: use the module's value.")]
		public readonly int CapturerInfantryLimitPct = -1;

		[Desc("S1b: minimum ticks between capturer completions; on the difficulty line (longer pacing",
			"on easier tiers), skipped while a known capturable-target allowance remains. 0 = off.",
			"Negative: use the module's value.")]
		public readonly int EngineerCompletionIntervalTicks = -1;

		[Desc("Self-preservation (AI_DEEP_RESEARCH.md §7, Zero-K's lesson): a ground squad using the combat predictor",
			"retreats when its predicted strength ratio against the enemies it sees falls below this percent. Higher",
			"tiers value their units more. Maintainer 2026-09-28: never suicide units.")]
		public readonly int RetreatRatioPct = 50;

		public override object Create(ActorInitializer init) { return new BotLimits(init.Self, this); }
	}

	public class BotLimits : ConditionalTrait<BotLimitsInfo>
	{
		public BotLimits(Actor self, BotLimitsInfo info) : base(info)
		{
		}
	}
}
