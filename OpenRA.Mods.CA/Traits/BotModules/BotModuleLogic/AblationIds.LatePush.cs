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
	// LATEPUSH-01 P1 rows (design 01 D1 and D2, design 02 R-D).
	public static partial class AblationIds
	{
		// lp_age_min: the team attack minimum is the WAVE-MIN bar x age x map context x the learned multiplier, capped by what the team can send. Row off = the WAVE-MIN bars alone, as before.
		public const string LpAgeMin = "lp_age_min";

		// lp_commit: an order commits a share of the eligible army and the guard lends what that share needs. Defined with the guard floor ON (design 02 R-D); row off = the fixed lend share, as before.
		public const string LpCommit = "lp_commit";
	}
}
