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
	/// The enemy air capability as this bot has SEEN it: a remembered enemy aircraft or a
	/// remembered enemy aircraft-producing structure. Implemented by Cameo's master AI from
	/// its fog memory (S2, FINDINGS_2026-10-12: blind AA spam — samsites rose thousands of
	/// ticks before the first enemy air appeared, six of them against an opponent who never
	/// fielded air). Returns false when the bot is not observing through fog; the caller
	/// then keeps its ungated behaviour, which is what the `classic` bot deliberately runs.
	///
	/// E1 (lead ruling 2026-10-12): the sighting LATCHES per enemy player — once an enemy's
	/// air capability was fog-fairly seen, the observation stays true against that player
	/// for the rest of the match even after the memory expires; only a world load resets.
	/// </summary>
	public interface IBotEnemyAirIntelProvider
	{
		bool TryGetEnemyAirIntel(out bool airThreatObserved);

		/// <summary>The per-enemy view of the same gate: "air capability observed against
		/// THIS enemy player" — the latched answer, or a fresh fog scan that latches on hit.</summary>
		bool TryGetEnemyAirIntel(Player enemy, out bool airThreatObserved);
	}
}
