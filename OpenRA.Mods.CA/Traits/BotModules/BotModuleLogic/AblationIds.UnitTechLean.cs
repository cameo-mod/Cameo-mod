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
	// V08F row (Opus 11-25, TIER-MIX-01).
	public static partial class AblationIds
	{
		// TIER-MIX-01: a late pick from the top of its queue's pool, on an empty queue with a share of its price in the bank, is admitted pay-as-you-go like the first build of a type
		// (the full-price cash gate otherwise re-rolls it to a cheaper unit). Row off = the full price is needed up front, as before.
		public const string UnitTechLean = "ub_tech_lean";
	}
}
