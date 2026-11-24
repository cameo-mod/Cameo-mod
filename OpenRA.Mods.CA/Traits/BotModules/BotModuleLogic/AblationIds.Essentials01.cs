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
	// ESSENTIALS-01 row. Owner: Sonnet.
	// pb_essentials_low_cash off = below the tier MaximiseProductionCash line the base builder admits extras again (the pre-ESSENTIALS rule).
	public static partial class AblationIds
	{
		public const string EssentialsLowCash = "pb_essentials_low_cash";
		public const string ExpansionRefinerySafety = "pb_expansion_refinery_safety"; // off = the expansion refinery goes first whether or not the new yard is under threat
		public const string SustainedCashProduction = "pb_sustained_cash_production"; // off = a long-lasting bank above the line no longer forces the next building to be production
		public const string CycleProduction = "pb_cycle_production"; // off = the old production rungs (overflow, scaling, threshold) only
	}
}
