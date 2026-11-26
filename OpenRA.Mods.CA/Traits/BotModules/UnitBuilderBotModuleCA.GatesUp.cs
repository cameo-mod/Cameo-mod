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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P5 (b), row ub_gates_up (design 02 contradiction 2, addendum 3 item 5; scored on distributions, admission timing, starts and refusals per fill).
	// Today the roll's legal set (R1) already holds the gates that need no batch: the share/delay/interval/limit/capturer/item-aware-cap gate (ShouldBuildReject) and the aircraft cap of a type already ordered once. The three gates
	// that read the pass's BATCH still run after the draw: the in-pass limit headroom, the capturer cap with the batch's pending capturers, and the aircraft caps with the batch's pending aircraft. A pick they refuse is
	// skipped and the roll is redrawn, but it was already in the pool: it set the mean floor, the core, the draw bound and rollPoolMaxCost (the lean's dearest price) before it was refused.
	// With the row on the fill hands the batch over and R1 asks the same three gates, in the same order and with the same inputs, before the draw; the post-draw gates stay in the fill (they then find nothing to refuse).
	// NOT distribution-equal: the pool, the floors, the bound and the RNG stream change. The gates are the ones the fill runs (LimitHeadroomAdmits, CapturerCapAdmitsWithPending, PendingAircraftDelta + CanBuildMoreOfAircraft,
	// the first build of a type waived from the aircraft cap); the row holds no state beyond the per-roll inputs and one list for the DEV row.
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablateGatesUp;

		/// <summary>ub_gates_up: the row is on and not ablated; it is a filter of the roll's legal set, so it needs the roll.</summary>
		bool GatesUpOn => UnitRollOn && Info.UnitGatesUp && !ablateGatesUp.Off(player, AblationIds.UbGatesUp);

		// Set by the fill before each roll while the row is on (the pass's batch), null otherwise: R1 then runs no batch gate.
		ProducerServiceBatch rollGateBatch;

		// The types the row removed from the legal set on the current roll, with the gate that refused them (cleared by every roll).
		readonly List<(string Name, string Why)> rollGated = new();

		/// <summary>
		/// The first batch gate that refuses a candidate right now, or null: the same three gates, in the order and with the inputs the fill runs after the draw (headroom, capturer with the pending capturers,
		/// aircraft with the pending aircraft, waived for the first build of a type). Always null while the fill handed no batch over (row off).
		/// </summary>
		string GatesUpRefusal(ActorInfo candidate)
		{
			var batch = rollGateBatch;
			if (batch == null)
				return null;

			if (!LimitHeadroomAdmits(batch, candidate.Name))
				return "headroom";

			if (!CapturerCapAdmitsWithPending(candidate, batch))
				return "capturer";

			// The aircraft caps never block the FIRST build of a listed type (the same waiver as in the fill); a delta of 0 was already asked by R1's own aircraft filter.
			if (rollOrdered.Contains(candidate.Name))
			{
				var isAircraft = candidate.HasTraitInfo<AircraftInfo>();
				var isAirToAir = isAircraft && Info.AirToAirUnits.Contains(candidate.Name);
				var pending = UnitProducerServiceCA.PendingAircraftDelta(isAircraft, isAirToAir, Info.MaintainAirSuperiority, batch);
				if (pending != 0 && !CanBuildMoreOfAircraft(candidate, pending))
					return "aircraft";
			}

			return null;
		}

		/// <summary>
		/// R1 asks this per candidate (after its own filters): true = the row removed it. The removal is remembered for the DEV row and for the empty-pool rule below.
		/// </summary>
		bool GatesUpRemoves(ActorInfo candidate)
		{
			var why = GatesUpRefusal(candidate);
			if (why == null)
				return false;

			rollGated.Add((candidate.Name, why));
			RollExclude(candidate.Name, why);
			return true;
		}

		/// <summary>
		/// F1 (the fill's rule): only a genuinely EMPTY pick may revert the composition; a pick whose candidates were refused on gates (skip not empty) must not. A candidate the row removed is a refused
		/// candidate, so when the row leaves the legal set empty it joins the fill's skip set, and the fill then ends this queue's pass without touching the composition, as it did when the post-draw gate refused them.
		/// </summary>
		void GatesUpMarkRefused(HashSet<string> skip)
		{
			foreach (var (name, _) in rollGated)
				skip.Add(name);
		}

		readonly Dictionary<string, int> gatesUpLogTick = new(StringComparer.Ordinal);

		// ub_gates_up (DEV, record only): at most one unit_roll.csv row per queue type per 250 ticks while the row removed a type from the legal set:
		// tick,seat,ub_gates_up,,queue,types removed this roll;gated=name:gate|name:gate. The ub_draw row's excl= field shows the same names with the same gate names.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void GatesUpLog(string queueType)
		{
			if (rollGated.Count == 0 || gatesUpLogTick.TryGetValue(queueType, out var last) && world.WorldTick - last < 250)
				return;

			gatesUpLogTick[queueType] = world.WorldTick;
			var names = new List<string>(rollGated.Count);
			foreach (var (name, why) in rollGated)
				names.Add(name + ":" + why);

			RollLog($"{world.WorldTick},{Array.IndexOf(world.Players, player)},ub_gates_up,,{queueType},{rollGated.Count};gated={string.Join("|", names)}");
		}
	}
}
