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
	// REF2-FIRST-01 (USER BINDING 11-26, Opus rulings; one switch Info.Ref2First, rows ref2_before_radar, ref2_before_prod2, ref2_floor). The one precedence gate of the second refinery:
	// while ref2 is due (Ref2First.Due) a NEW radar provider (TECH exempt) or a 2nd or later production building of a role (RUSH exempt) is not offered, and ref2 outranks the refinery
	// interval wait (not the first-war-factory wait). Nothing in flight is cancelled. The planner (ExpansionPlannerBotModule.EcoFloorLift) lifts the zone cap while fewer than two own
	// refineries stand, which is what makes a reachable field claimable for ref2 even where the law says adequate.
	partial class BaseBuilderQueueManagerCA
	{
		int ref2DueTick = -1;
		bool ref2DueValue;
		HashSet<string> ref2AirRole;

		bool Ref2AnyArmed => baseBuilder.Ref2RadarArmed || baseBuilder.Ref2Prod2Armed || baseBuilder.Ref2FloorArmed;

		// Once per tick per queue manager; the module's DueSince / Finished latches are shared by every queue and written only on a change.
		bool Ref2Due(Func<string, bool> buildable)
		{
			var now = world.WorldTick;
			if (ref2DueTick == now)
				return ref2DueValue;

			ref2DueTick = now;
			ref2DueValue = ComputeRef2Due(buildable, now, false);
			return ref2DueValue;
		}

		// Row ref2_stall_release, the stall clock sample (Haiku FIX on 7611e2cb4, Opus ruling): once per bot visit, ahead of the queue managers' rotation, while the row is armed and the gate has not
		// finished (Ref2First.SampleWindow). Single writer of the clock: the module's BotTick calls it on one building manager; the pick-time evaluations (Ref2Due) never touch the clock. Cheap by
		// construction: two flag reads and one tick compare on most visits, one evaluation per Ref2First.SampleTicks ticks, loops over frozen sets instead of LINQ, one cached delegate.
		int ref2NextSample;
		Func<string, bool> ref2SampleBuildable;

		public void Ref2StallSample()
		{
			var now = world.WorldTick;
			if (now < ref2NextSample)
				return;

			if (!Ref2First.SampleWindow(baseBuilder.Ref2StallArmed, baseBuilder.Ref2Finished, baseBuilder.Ref2DueSince, now, baseBuilder.Info.Ref2FirstBoundTicks))
				return;

			ref2NextSample = now + Ref2First.SampleTicks;
			ref2SampleBuildable ??= Ref2BuildableInAnyQueue;
			ComputeRef2Due(ref2SampleBuildable, now, true);
		}

		// The sample asks the whole building side, not one queue: a refinery buildable in any building queue of the module counts (the pick-time path keeps its per-queue view).
		bool Ref2BuildableInAnyQueue(string name)
		{
			foreach (var category in baseBuilder.Info.BuildingQueues)
				foreach (var queue in AIUtils.FindQueues(player, category))
					foreach (var item in queue.BuildableItems())
						if (item.Name == name)
							return true;

			return false;
		}

		bool ComputeRef2Due(Func<string, bool> buildable, int now, bool sampleStall)
		{
			if (!Ref2AnyArmed)
				return false;

			var b = baseBuilder.Info;
			var count = baseBuilder.RefineryCountWithQueued();
			if (baseBuilder.Ref2Finished)
			{
				Ref2Observed("not_due", "finished", count);
				return false;
			}

			if (count >= Ref2First.RefineryTarget)
			{
				baseBuilder.Ref2Finished = true;
				Ref2Observed("not_due", "two_refineries", count);
				return false;
			}

			if (Ref2First.BoundExpired(baseBuilder.Ref2DueSince, now, b.Ref2FirstBoundTicks))
			{
				baseBuilder.Ref2Finished = true;
				Ref2Observed("bound_expired", "since_" + baseBuilder.Ref2DueSince, count);
				return false;
			}

			ActorInfo refinery = null;
			foreach (var name in b.RefineryTypes)
			{
				if (buildable(name) && world.Map.Rules.Actors.TryGetValue(name, out var info))
				{
					refinery = info;
					break;
				}
			}

			// The law counts the claimable anchors (the planner's cap lift makes a field with room claimable). Without a law provider there is no field verdict and no planner cap lift either,
			// so nothing can be claimed for ref2: no field = not due (Haiku FIX on REF2-FIRST-01; a hold for a refinery that cannot be placed would only delay the radar and the 2nd production).
			var law = baseBuilder.RefineryLawProvider();
			var fieldUnderCap = law != null && law.UnclaimedAnchorsInReach > 0;
			var maxed = refinery != null && baseBuilder.HasMaxRefineriesFor(refinery);
			var due = Ref2First.Due(true, false, count, refinery != null, maxed, fieldUnderCap);
			if (due && baseBuilder.Ref2DueSince < 0)
				baseBuilder.Ref2DueSince = now;

			// Row ref2_stall_release (Opus 11-26): ref2 is startable (due, and the first-war-factory wait is over) but no refinery has started within Ref2StallTicks: the downstream blocker is
			// not ours to wait for (claim, site, spacing, reach, a refinery-pick gate or cash), so the hold ends for good and the old order runs. The clock is consecutive startable SAMPLES (one per
			// Ref2First.SampleTicks ticks from Ref2StallSample, never from the pick-time path, so a not-startable spell cannot go unseen); the 3000-tick bound above stays the outer cap. A refinery
			// that starts makes the count reach two before this point, so the gate finishes through the two_refineries branch instead.
			if (sampleStall && baseBuilder.Ref2StallArmed)
			{
				var factoryBuildable = false;
				var factoryBanned = false;
				foreach (var factory in b.VehiclesFactoryTypes)
				{
					factoryBuildable |= buildable(factory);
					if (!factoryBanned)
						factoryBanned = NoSpaceBanned(factory);
				}

				var factoryOwnedOrQueued = baseBuilder.VehicleFactoryOwnedOrQueued();
				var factoryWaitReleased = RefineryFieldGateCA.FactoryWaitReleased(factoryBanned,
					baseBuilder.RefineryGateFactoryPlacementFailed, now, baseBuilder.RefineryGateFirstStoodTick, b.RefineryFactoryWaitTicks);
				var startable = Ref2First.Startable(due, factoryBuildable, factoryOwnedOrQueued, factoryWaitReleased);
				baseBuilder.Ref2StartableSince = Ref2First.StartableSince(baseBuilder.Ref2StartableSince, startable, now);
				if (Ref2First.StallExpired(baseBuilder.Ref2StartableSince, now, b.Ref2StallTicks))
				{
					baseBuilder.Ref2Finished = true;

					// DEV only (the argument is compiled out with the call): the verdict of the downstream blocker the V08G game must name.
					Ref2Observed("stall_released",
						$"startable_since_{baseBuilder.Ref2StartableSince}|due_since_{baseBuilder.Ref2DueSince}|eco_{lastRefineryEconomyVerdict}|anchors_{law?.UnclaimedAnchorsInReach}|standing_{AIUtils.CountActorByCommonName(baseBuilder.RefineryBuildings)}|queued_{count - AIUtils.CountActorByCommonName(baseBuilder.RefineryBuildings)}|factory_{(factoryOwnedOrQueued ? "owned" : factoryWaitReleased ? "released" : "none")}",
						count);
					return false;
				}
			}

			Ref2Observed(due ? "due" : "not_due", due ? "ref2_wanted" : refinery == null ? "refinery_not_buildable" : maxed ? "refinery_max" : "no_field_with_room", count);
			return due;
		}

		// REF2-FIRST-01 (row ref2_verdict), DEV only: compiled out of a release build together with its arguments; the observer dedupes. Nothing reads anything back.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void Ref2Observed(string verdict, string detail, int refineries)
		{
			foreach (var o in PickObservers)
				o.Ref2Verdict(player, world.WorldTick, verdict, detail, refineries, Category);
		}

		string Ref2Personality()
		{
			if (!pipelinePersonalityResolved)
			{
				pipelinePersonality = player.PlayerActor.TraitsImplementing<IBotCurrentPersonality>().FirstOrDefault();
				pipelinePersonalityResolved = true;
			}

			return pipelinePersonality?.CurrentPersonality;
		}

		// The role of a production type: barracks, vehicle factory, naval yard, or the rest (air); the count is owned plus in production, as the opening order counts.
		int Ref2RoleCount(string name)
		{
			var b = baseBuilder.Info;
			if (b.BarracksTypes.Contains(name))
				return OwnedOrQueuedCount(b.BarracksTypes);

			if (b.VehiclesFactoryTypes.Contains(name))
				return OwnedOrQueuedCount(b.VehiclesFactoryTypes);

			if (b.NavalProductionTypes.Contains(name))
				return OwnedOrQueuedCount(b.NavalProductionTypes);

			ref2AirRole ??= new HashSet<string>(b.ProductionTypes.Except(b.BarracksTypes).Except(b.VehiclesFactoryTypes).Except(b.NavalProductionTypes));
			return OwnedOrQueuedCount(ref2AirRole);
		}

		/// <summary>True = this NEW start is not offered now: a radar provider (row ref2_before_radar) or a 2nd or later production building (row ref2_before_prod2) while ref2 is due.</summary>
		bool Ref2Holds(ActorInfo info, Func<string, bool> buildable)
		{
			if (!Ref2AnyArmed)
				return false;

			var isRadar = baseBuilder.Ref2RadarArmed && IsRadarProvider(info);
			var isProduction = baseBuilder.Ref2Prod2Armed && baseBuilder.Info.ProductionTypes.Contains(info.Name);
			if (!isRadar && !isProduction)
				return false;

			var due = Ref2Due(buildable);
			if (!due)
				return false;

			var personality = Ref2Personality();
			var heldRadar = Ref2First.HoldsRadar(due, isRadar, personality);
			var heldProd2 = !heldRadar && Ref2First.HoldsSecondProduction(due, isProduction, isProduction ? Ref2RoleCount(info.Name) : 0, personality);
			if (heldRadar || heldProd2)
				Ref2Observed(heldRadar ? "held_radar" : "held_prod2", info.Name, baseBuilder.RefineryCountWithQueued());

			return heldRadar || heldProd2;
		}

		// Row ref2_floor: a due ref2 passes the economy gate's interval wait (the first-war-factory wait and the first-copy yield are untouched; the yield applies from the third refinery on).
		bool Ref2OutranksInterval(IEnumerable<ActorInfo> buildableThings, int refineryCount)
		{
			if (!baseBuilder.Ref2FloorArmed)
				return false;

			var names = new HashSet<string>(buildableThings.Select(a => a.Name));
			return Ref2First.OutranksIntervalWait(Ref2Due(names.Contains), refineryCount);
		}
	}
}
