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

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// REF-TIER-01 (USER BINDING 11-26, Opus rulings; one switch Info.RefTierFloor, rows ref_tier_floor and ref_tier_personality): the refinery target is a floor for refinery demand, not a hold. The
	// planner lifts the zone caps (ExpansionPlannerBotModule.RefTierLift, which reads IBotRefTier); this file holds the queue manager's part: the interval outrank and the DEV verdict record.
	partial class BaseBuilderQueueManagerCA
	{
		// The economy gate's verdict with its interval wait outranked: the rungs before it (the first-war-factory wait) were decided by the caller, the one after it is the bounded first-copy yield, which
		// only exists from the 3rd refinery on (the gate's own rule: the yield never applies below the ref2 minimum of two). The yield clock is the gate's own (RefineryGateYieldSince).
		RefineryFieldGateCA.EconomyVerdict RefTierYieldVerdict(IEnumerable<ActorInfo> buildableThings, int count, int now, bool yardWithoutRefinery, bool factoryBuildable,
			bool factoryOwnedOrQueued, bool factoryWaitReleased)
		{
			var essential = count >= 2 && EssentialFirstCopyPending(buildableThings);
			if (!essential)
				baseBuilder.RefineryGateYieldSince = -1;
			else if (baseBuilder.RefineryGateYieldSince < 0)
				baseBuilder.RefineryGateYieldSince = now;

			// minIntervalTicks 0: the interval is outranked
			return RefineryFieldGateCA.Economy(true, count, yardWithoutRefinery, factoryBuildable, factoryOwnedOrQueued, factoryWaitReleased, now,
				baseBuilder.RefineryGateLastRiseTick, 0, essential, baseBuilder.RefineryGateYieldSince, baseBuilder.Info.RefineryEssentialYieldTicks);
		}

		// DEV only (compiled out of a release build with its call and arguments): the tier state, one record per CHANGE of (refineries, target, personality percent, radar, tech, anchor). A map with no claimable
		// field stays at below_no_anchor and writes nothing more. Nothing reads anything back.
		int refTierNextObserve;

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		internal void RefTierObserve()
		{
			var observers = PickObservers;
			if (observers.Length == 0)
				return;

			var now = world.WorldTick;
			if (now < refTierNextObserve)
				return;

			refTierNextObserve = now + 25;
			var target = baseBuilder.RefTierTarget;
			var count = baseBuilder.RefineryCountWithQueued();
			var anchors = baseBuilder.RefineryLawProvider()?.UnclaimedAnchorsInReach ?? 0;
			var reason = RefTier.Reason(baseBuilder.RefTierArmed, count, target, anchors);
			var detail = $"n_{count}|target_{target}|pct_{baseBuilder.RefTierPersonalityPct(baseBuilder.RefTierRadarStanding)}|radar_{(baseBuilder.RefTierRadarStanding ? 1 : 0)}|tech_{(baseBuilder.RefTierTechStanding ? 1 : 0)}|anchor_{(anchors > 0 ? 1 : 0)}";
			foreach (var o in observers)
				o.RefTierVerdict(player, now, reason, detail, target, count, Category);
		}
	}
}
