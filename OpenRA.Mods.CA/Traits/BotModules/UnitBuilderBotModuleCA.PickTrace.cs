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
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P5 step (a) (design 02 4c19e5f9 + addendum 2): the DEV equivalence trace of the unit pick, unit_pick_trace.csv (own file, no header, all seats append; compiled out of release: [Conditional] drops the calls
	// and their arguments). Record only: no behaviour, no new random draw (the RNG's own draw counter and last raw value are READ, never advanced), nothing here is read by a decision. Rows, columns tick,seat,event,type,queue,detail:
	//   ub_fill_start  a queue pass begins (producer actor id, the RNG draw counter).
	//   ub_draw        one roll that drew: the integer bins (UnitRoll.Quantize), the bound, the raw RNG value, the RNG draw counter, the legal-set refusals, the wave roles reported, rollPoolMaxCost, rollBankLeft, the first-copy flag, rollAvailableSince.
	//   ub_empty       a roll with no candidate left (the all-excluded or none-listed fill).
	//   ub_fill        the outcome after a draw or a null roll: accept (order started, order sequence) or refuse (the post-draw gate), the deciding rule, the batch bank / reserved / left, the cap headroom, the gate cost and the full price.
	//   ub_fill_end    the pass ended: orders emitted, the RNG draw counter and last raw value.
	// An old and a new build on the same seed with the same switches must write the same rows (the trace diff is the proof of an equivalence refactor).
	public partial class UnitBuilderBotModuleCA
	{
		int pickTraceFill, pickTraceDraw, pickTraceOrder, pickTraceFillOrders;

		void PickTraceWrite(string ev, string name, string queueType, string detail)
		{
			var hub = world.WorldActor.TraitOrDefault<CameoLogHub>();
			if (hub == null || !hub.Sink.Enabled)
				return;

			hub.Sink.InfantryRejectRow(Array.IndexOf(world.Players, player), "unit_pick_trace.csv", $"{world.WorldTick},{Array.IndexOf(world.Players, player)},{ev},{name},{queueType},{detail}", world.WorldTick);
		}

		static string PickTraceRoles(HashSet<string> roles) => roles == null ? "-" : string.Join("|", roles.OrderBy(r => r, StringComparer.Ordinal));

		static string PickTraceExcluded(List<string> excluded) => excluded.Count == 0 ? "-" : string.Join("|", excluded);

		MersenneTwister PickTraceRng() => BotRng.For(player, nameof(UnitBuilderBotModuleCA));

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceFillStart(ProductionQueue queue)
		{
			if (!Info.UnitPickTrace)
				return;

			pickTraceFill++;
			pickTraceFillOrders = 0;
			PickTraceWrite("ub_fill_start", "", queue.Info.Type, UnitPickArbiter.FillEndDetail(new UnitPickArbiter.FillEndTrace(pickTraceFill, queue.Actor.ActorID, 0, PickTraceRng().TotalCount, PickTraceRng().Last)));
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceEmpty(ProductionQueue queue, int refused, HashSet<string> queueRoles)
		{
			if (!Info.UnitPickTrace)
				return;

			PickTraceWrite("ub_empty", "", queue.Info.Type, UnitPickArbiter.EmptyDetail(new UnitPickArbiter.EmptyTrace(
				pickTraceFill, refused, queue.Actor.ActorID, PickTraceExcluded(rollExcluded), PickTraceRoles(queueRoles), PickTraceRng().TotalCount, UnitPickArbiter.RuleEmptyPool)));
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceDraw(ProductionQueue queue, List<ActorInfo> pool, int[] units, int pick, int refused, HashSet<string> queueRoles)
		{
			if (!Info.UnitPickTrace)
				return;

			var rng = PickTraceRng();
			var avail = UnitPickArbiter.Avail(rollAvailableSince);
			PickTraceWrite("ub_draw", pool[pick].Name, queue.Info.Type, UnitPickArbiter.DrawDetail(new UnitPickArbiter.DrawTrace(
				++pickTraceDraw, pickTraceFill, refused, queue.Actor.ActorID, queue.Info.Type, pool.Count,
				UnitPickArbiter.Bins(pool.Select(a => a.Name).ToList(), units), UnitRoll.Sum(units), rng.Last, rng.TotalCount, pool[pick].Name, rollFirst, rollPoolMaxCost, rollBankLeft,
				PickTraceExcluded(rollExcluded), PickTraceRoles(queueRoles), rollAvailableSince.Count, UnitPickArbiter.Digest(avail), UnitPickArbiter.RuleDraw)));
		}

		// A null roll that ends the fill (no candidate left): the fill row with result none.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceNone(ProductionQueue queue, int refused, int held, ProducerServiceBatch batch)
		{
			if (!Info.UnitPickTrace)
				return;

			PickTraceFillRow(queue, refused, UnitPickArbiter.ResultNone, UnitPickArbiter.RuleNoPick, "no_candidate", null, held, -1, -1, batch, 0);
		}

		// A post-draw refusal: the gate that said no (ShouldBuildReject, headroom, capturer, aircraft, afford_cash); gateCost / fullCost are -1 before the cost is known.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceRefuse(ProductionQueue queue, int refused, string why, string name, int held, int gateCost, int fullCost, ProducerServiceBatch batch)
		{
			if (!Info.UnitPickTrace)
				return;

			PickTraceFillRow(queue, refused, UnitPickArbiter.ResultRefuse, UnitPickArbiter.RuleGate, why, name, held, gateCost, fullCost, batch, 0);
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceAccept(ProductionQueue queue, int refused, string name, int held, int gateCost, int fullCost, bool paygo, bool leaned, ProducerServiceBatch batch)
		{
			if (!Info.UnitPickTrace)
				return;

			pickTraceFillOrders++;
			PickTraceFillRow(queue, refused, UnitPickArbiter.ResultAccept, UnitPickArbiter.AdmitRule(paygo, leaned), "order", name, held, gateCost, fullCost, batch, ++pickTraceOrder);
		}

		void PickTraceFillRow(ProductionQueue queue, int refused, string result, string rule, string why, string name, int held, int gateCost, int fullCost, ProducerServiceBatch batch, int orderSeq) =>
			PickTraceWrite("ub_fill", name ?? "", queue.Info.Type, UnitPickArbiter.FillDetail(new UnitPickArbiter.FillTrace(
				pickTraceFill, refused, queue.Actor.ActorID, queue.Info.Type, result, rule, why, name ?? "-", held, gateCost, fullCost,
				batch.Bank, batch.ReservedCash, batch.BankLeft, UnitPickArbiter.HeadroomText(batch.LimitHeadroom, name), rollFirst, orderSeq)));

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PickTraceFillEnd(ProductionQueue queue)
		{
			if (!Info.UnitPickTrace)
				return;

			PickTraceWrite("ub_fill_end", "", queue.Info.Type, UnitPickArbiter.FillEndDetail(new UnitPickArbiter.FillEndTrace(
				pickTraceFill, queue.Actor.ActorID, pickTraceFillOrders, PickTraceRng().TotalCount, PickTraceRng().Last)));
		}
	}
}
