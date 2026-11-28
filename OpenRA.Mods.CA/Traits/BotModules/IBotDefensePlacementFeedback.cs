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
	/// DEF-PLACE-RETRY-01 (DEF-LANES P3a): the authoritative refusal of a cell a defence-placement advisor chose. An advisor that also
	/// implements this and tracks attempts gets its candidate cells filtered by the base builder's reservation mask, and is told when that
	/// mask still refused the chosen cell at the emission point. That refusal is the only rejection: a queue that asks again for the same
	/// type is NOT one (its order may still be in flight, and a producer id can stand for more than one queue). An advisor that does not
	/// track (the switch off or the row def_place_retry ablated) is never called. Advisors live in another assembly and must not be
	/// referenced by name here.
	/// </summary>
	public interface IBotDefensePlacementFeedback
	{
		/// <summary>False = no call below is made and the advisor's candidate cells are not filtered by the reservation mask.</summary>
		bool TracksAttempts { get; }

		/// <summary>The reservation mask refused the advised cell at the emission point (no order went out). producer is for the log only.</summary>
		void AttemptMasked(uint producer, string type, CPos cell);
	}

	public static partial class AblationIds
	{
		// DEF-PLACE-RETRY-01 row. Owner: Fable. Off = the advisor keeps no memory of failed cells and its candidates are not mask-filtered.
		public const string DefPlaceRetry = "def_place_retry";
	}
}
