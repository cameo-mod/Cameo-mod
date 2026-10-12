#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Read-only view of the gates a unit-builder module currently enforces, exposed
	/// for record-only diagnostics (WBL-P1 conversion diagnostics). Each module
	/// answers with its own semantics — floor adjustments, reservation voters,
	/// saturation policy, and per-type limits differ between the CA and Frans
	/// builders — so a poll reads enforced truth instead of duplicating internals.
	/// Implementations must be cheap, allocation-light, and side-effect free: they
	/// are queried by observers that must never feed back into the decision.
	/// </summary>
	public interface IBotUnitProductionGateInfo
	{
		/// <summary>
		/// Cash+resources below which the module refuses ordinary unit production,
		/// already including any personality or state adjustment the module applies
		/// (e.g. a steamroller lead, a harvester-debt or opening floor). 0 = no floor.
		/// </summary>
		int UnitProductionCashFloor { get; }

		/// <summary>
		/// Bank level at/above which the module stops sweeping unit production
		/// categories. 0 = the module never stops unit production for bank
		/// saturation (Frans: the economic state is a spending diagnosis, not a stop).
		/// </summary>
		int UnitProductionSaturationCash { get; }

		/// <summary>
		/// Trait-type names of the reservation voters whose pause requests the module
		/// currently honors (an enabled <see cref="IBotRequestPauseUnitProduction"/>
		/// voting <c>PauseUnitProduction</c> starves every ordinary unit queue, even
		/// where the module still runs an internal exception such as opening
		/// defense). Ordinal-sorted; empty when nobody is holding the cash.
		/// </summary>
		string[] UnitProductionReservedBy { get; }

		/// <summary>
		/// True when the module's own per-type limits currently forbid producing
		/// <paramref name="actor"/> — the caller supplies this seat's owned/queued
		/// counts; which count a module consults and whether a positive limit is a
		/// stop or a composition target is the module's semantics. A missing limit
		/// entry is never a block.
		/// </summary>
		bool UnitProductionLimited(string actor, int owned, int queued);
	}
}
