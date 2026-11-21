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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// PROD-IDLE (PLAN_03 section 3, "slots and cap from the projected state"): a unit cap that knows WHICH items it
	/// counts. A plain <see cref="IBotRequestPauseUnitProduction"/> vote stops every unit queue, so a bot parked at its
	/// army cap could not rebuild a harvester or an MCV, although the cap does not count them. A cap that implements
	/// this interface (in addition to the pause vote) lets the fair producer service keep serving the items it does not
	/// count. Legacy (flag-off) service ignores the interface and still stops on the plain vote.
	/// Read-only, no orders, no RNG.
	/// </summary>
	public interface IBotItemAwareUnitCap
	{
		/// <summary>True when producing <paramref name="actor"/> would add to the count this cap limits.</summary>
		bool CountsAgainstCap(ActorInfo actor);
	}

	public static class ItemAwareCapCA
	{
		/// <summary>
		/// True when one of the caps that are pausing right now counts the item. No pausing cap = never capped. An item
		/// the ruleset does not know (<paramref name="item"/> null) is treated as counted: fail closed, the legacy full
		/// stop is what such an item got before.
		/// </summary>
		public static bool CappedOut(IBotItemAwareUnitCap[] pausing, ActorInfo item)
		{
			if (pausing.Length == 0)
				return false;

			if (item == null)
				return true;

			foreach (var cap in pausing)
				if (cap.CountsAgainstCap(item))
					return true;

			return false;
		}
	}
}
