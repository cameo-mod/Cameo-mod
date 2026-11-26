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
			ref2DueValue = ComputeRef2Due(buildable, now);
			return ref2DueValue;
		}

		bool ComputeRef2Due(Func<string, bool> buildable, int now)
		{
			if (!Ref2AnyArmed || baseBuilder.Ref2Finished)
				return false;

			var b = baseBuilder.Info;
			var count = baseBuilder.RefineryCountWithQueued();
			if (count >= Ref2First.RefineryTarget || Ref2First.BoundExpired(baseBuilder.Ref2DueSince, now, b.Ref2FirstBoundTicks))
			{
				baseBuilder.Ref2Finished = true;
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

			// The law counts the claimable anchors (the planner's cap lift makes a field with room claimable); without a law the old placement search has no field verdict, so only buildable + max apply.
			var law = baseBuilder.RefineryLawProvider();
			var fieldUnderCap = law == null || law.UnclaimedAnchorsInReach > 0;
			var due = Ref2First.Due(true, false, count, refinery != null, refinery != null && baseBuilder.HasMaxRefineriesFor(refinery), fieldUnderCap);
			if (due && baseBuilder.Ref2DueSince < 0)
				baseBuilder.Ref2DueSince = now;

			return due;
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
			return Ref2First.HoldsRadar(due, isRadar, personality)
				|| Ref2First.HoldsSecondProduction(due, isProduction, isProduction ? Ref2RoleCount(info.Name) : 0, personality);
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
