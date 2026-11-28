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
	/// DEF-PLACE-RETRY-01 (DEF-LANES P3a): what became of a cell a defence-placement advisor chose. An advisor that also implements this
	/// and tracks attempts is told, per production queue (the producer actor) and defence type, when an attempt starts, when the base
	/// builder's reservation mask refused the chosen cell and when the place order went out. With that it can stop offering a cell that is
	/// never built on. An advisor that does not track (the switch off or the row def_place_retry ablated) is never called and the base
	/// builder runs exactly as before. Advisors live in another assembly and must not be referenced by name here.
	/// </summary>
	public interface IBotDefensePlacementFeedback
	{
		/// <summary>False = no call below is made and the advisor's candidate cells are not filtered by the reservation mask.</summary>
		bool TracksAttempts { get; }

		/// <summary>The base builder is about to ask for a cell for this queue's ready <paramref name="type"/>.</summary>
		void BeginAttempt(uint producer, string type);

		/// <summary>An explicit rejection: the reservation mask refused the advised cell at the emission point (no order went out).</summary>
		void AttemptMasked(uint producer, string type, CPos cell);

		/// <summary>The place order for the advised cell went out. Not yet a placement: the building may still fail to appear.</summary>
		void AttemptIssued(uint producer, string type, CPos cell);
	}

	public static partial class AblationIds
	{
		// DEF-PLACE-RETRY-01 row. Owner: Fable. Off = the advisor keeps no memory of failed cells and its candidates are not mask-filtered.
		public const string DefPlaceRetry = "def_place_retry";
	}
}
