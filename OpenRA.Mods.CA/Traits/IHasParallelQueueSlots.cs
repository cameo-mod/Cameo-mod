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
	/// Implemented by production queues that can hold more than one item simultaneously (e.g. Zerg hatchery).
	/// Allows the AI to fill all available parallel slots instead of treating the queue as busy after one item.
	/// </summary>
	public interface IHasParallelQueueSlots
	{
		/// <summary>How many additional items may currently be queued.</summary>
		int AvailableSlots { get; }
	}
}
