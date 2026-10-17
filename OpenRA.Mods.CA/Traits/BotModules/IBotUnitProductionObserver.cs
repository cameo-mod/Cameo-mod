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
	/// Record-only observer of the unit builders' production decisions (WBL-P1
	/// conversion diagnostics). Implemented by a world trait in OpenRA.Mods.Cameo
	/// (the player telemetry log); this assembly must not reference it by name.
	/// The observer must never feed a decision: the unit builders ignore
	/// everything about it, so an absent observer leaves behaviour bit-identical.
	/// </summary>
	public interface IBotUnitProductionObserver
	{
		/// <summary>
		/// Another module asked the unit builder for <paramref name="actor"/> and the
		/// request entered the builder's pending list (<see cref="IBotRequestUnitProduction"/>).
		/// </summary>
		void UnitProductionRequested(Player owner, int tick, string actor);

		/// <summary>
		/// The unit builder issued a StartProduction order for <paramref name="actor"/>
		/// on a queue of category <paramref name="queueType"/> — the request was accepted.
		/// </summary>
		void UnitProductionStarted(Player owner, int tick, string actor, string queueType);

		/// <summary>
		/// A queued request died inside the builder without an order: the policy gate
		/// refused it or no queue could produce it. <paramref name="reason"/> is the
		/// call-site label (should_build, no_queue); per-queue hold reasons come from
		/// the telemetry poll, not this seam.
		/// </summary>
		void UnitProductionSkipped(Player owner, int tick, string actor, string reason);
	}
}
