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
	// CRATE-RESERVE-01 row (USER rule via Opus 11-24). Owner: Sonnet.
	public static partial class AblationIds
	{
		// Crates within the reserve radius of any own or allied construction yard are left alone while the bot has a yard or an MCV; every other crate is collected by a
		// cheap nearby idle unit away from known threat; with no yard and no MCV every crate is hunted by all idle units, safest first.
		public const string CrateReserveMcv = "crate_reserve_mcv";
	}
}
