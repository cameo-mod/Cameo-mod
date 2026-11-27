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

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P5 (b), row ub_admit_one (design 02 contradiction 1, addendum 3 item 4; scored on ub_core_churn, afford_cash refusals per fill, redraws). The core focus (R2) names the types it lifts with its own bank bar
	// (a share of the price); the fill's admission (R4: paygo, tech lean, full price, then the afford, reserve and depth-cash gates) can still refuse a type the core named, and the roll then re-rolls to a cheaper unit.
	// With the row on the core reads the admission forecast too: a settled type that the gates would refuse right now is not eligible for the core, and the core takes the next-dearest admissible type in its place.
	// It only NARROWS the eligible set (every type it can name was eligible under the old bank bar too, so it never adds a type the old bar refused), asks the same pure UnitPickArbiter.Admit and the same three gate
	// terms the fill loop uses, changes no order, no queue and no draw by itself, and holds no state of its own beyond the per-roll inputs. The fill hands over its batch, held count and depth state on each roll.
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablateAdmitOne;

		/// <summary>ub_admit_one: the row is on and not ablated; it needs the core focus it narrows.</summary>
		bool AdmitOneOn => CoreFocusOn && Info.UnitAdmitOne && !ablateAdmitOne.Off(player, AblationIds.UbAdmitOne);

		/// <summary>What the fill's admission reads on one roll: the pass's batch (bank and reservations), what the queue already holds, whether the depth-cash gate applies, the unpaid queued cost.</summary>
		readonly record struct RollAdmitInputs(ProducerServiceBatch Batch, int Held, bool DepthQueue, long Unpaid);

		// Set by the fill before each roll while the row is on, null otherwise (the core then uses its own bank bar alone).
		RollAdmitInputs? rollAdmit;

		/// <summary>True when the fill's admission list would start a settled (already ordered) pick of this full price right now: the admission, then the afford, reserve and depth-cash gates, as in the fill loop.</summary>
		bool R4Forecast(int fullCost)
		{
			if (rollAdmit is not { } s)
				return true;

			var admission = UnitPickArbiter.Admit(true, false, s.Batch.BankLeft, fullCost, TechLeanOn, rollPoolMaxCost, s.Held, world.WorldTick, RollParameters.TechLeanStartTicks, RollParameters.TechLeanTopPct, RollParameters.TechLeanBankPct);
			return s.Batch.Affords(admission.GateCost) && ReserveAdmits(s.Batch, admission.GateCost)
				&& !(s.DepthQueue && s.Held > 0 && !QueueDepthCA.CashAdmits(s.Batch.BankLeft, s.Unpaid, admission.GateCost));
		}

		// The types the forecast removed from the core on the current roll (cleared by every core pass, so no allocation).
		readonly List<int> admitOneRemoved = new();

		readonly Dictionary<string, int> admitOneLogTick = new(StringComparer.Ordinal);

		// ub_admit_one (DEV, record only): at most one unit_roll.csv row per queue type per 250 ticks while the row removed a type from the core: tick,seat,ub_admit_one,,queue,types removed this roll;the core now.
		// "Removed" = the old bank bar named it, the admission forecast refused it: the refused pick the core used to lift.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void AdmitOneLog(string queueType, List<ActorInfo> pool, List<int> removed)
		{
			if (removed.Count == 0 || admitOneLogTick.TryGetValue(queueType, out var last) && world.WorldTick - last < 250)
				return;

			admitOneLogTick[queueType] = world.WorldTick;
			var names = new List<string>(removed.Count);
			foreach (var i in removed)
				names.Add(pool[i].Name);

			RollLog($"{world.WorldTick},{Array.IndexOf(world.Players, player)},ub_admit_one,,{queueType},{removed.Count};refused={string.Join("|", names)}");
		}
	}
}
