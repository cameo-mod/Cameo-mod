#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// DEF-PLACE-RETRY-01 (DEF-LANES P3a; RCA in DEF_LANES_P3_DESIGN_NOTE): the planner offered the same best-scoring cell again and again while
	// it was never built on, until the base builder's failure budget cancelled a finished defence (seed 100775, tick 1267, on master too).
	// With the switch on the planner (1) only sees candidate cells that also pass the builder's reservation mask, and (2) skips a cell after
	// the builder's authoritative refusal of it (the mask at the emission point). A repeated request of a queue is never read as a rejection.
	// Off (PlaceRetryMemory false, the planner off, or the row def_place_retry): TracksAttempts is false, the base builder does not ask the
	// mask for the candidates, makes no call, and ChooseDefenseCell uses the canPlace it was given.
	public partial class DefenseCoveragePlanner : IBotDefensePlacementFeedback
	{
		AblationLatch ablatePlaceRetry;
		readonly RejectedCells rejectedCells = new();
		IBotLearnedKnobs placeRetryKnobs;
		bool placeRetryKnobsResolved;

		bool PlaceRetryOn => Info.PlaceRetryMemory && Info.Enabled && !IsTraitDisabled && !ablatePlaceRetry.Off(player, AblationIds.DefPlaceRetry);

		bool IBotDefensePlacementFeedback.TracksAttempts => PlaceRetryOn;

		// The learned knob def_place.reject_base scales the prior; no provider or no stored knob = the yaml prior.
		int RejectBaseTicks()
		{
			if (!placeRetryKnobsResolved)
			{
				placeRetryKnobs = player.PlayerActor.TraitsImplementing<IBotLearnedKnobs>().FirstOrDefault();
				placeRetryKnobsResolved = true;
			}

			return RefineryLearn.Resolve(placeRetryKnobs, "def_place.reject_base", Info.PlaceRejectBaseTicks, 25, 3000);
		}

		void IBotDefensePlacementFeedback.AttemptMasked(uint producer, string type, CPos cell)
		{
			if (!PlaceRetryOn)
				return;

			var ticks = rejectedCells.Reject(type, cell, world.WorldTick, RejectBaseTicks(), Info.PlaceRejectMaxTicks);
#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI {player.InternalName}: DEF-PLACE-RETRY reject {type} at {cell} (reservation mask) for {ticks} ticks " +
				$"producer {producer} at tick {world.WorldTick}");
#else
			_ = ticks;
			_ = producer;
#endif
		}

		// The candidate filter of ChooseDefenseCell: the given check, minus the cells that are rejected for this type now.
		Func<CPos, bool> WithoutRejected(ActorInfo defense, Func<CPos, bool> canPlace)
		{
			if (!PlaceRetryOn || rejectedCells.RejectedCount == 0)
				return canPlace;

			var type = defense.Name;
			var tick = world.WorldTick;
			return cell => !rejectedCells.IsRejected(type, cell, tick) && canPlace(cell);
		}

		// A building of ours appeared (the world's actor-added event): a cell that now holds the type is no longer rejected for it.
		void ConfirmPlaced(Actor building)
		{
			if (Info.PlaceRetryMemory && rejectedCells.RejectedCount > 0)
				rejectedCells.Confirm(building.Info.Name, building.Location);
		}
	}
}
