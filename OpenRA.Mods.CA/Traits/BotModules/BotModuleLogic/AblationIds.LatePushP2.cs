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
	// LATEPUSH-01 P2 rows (design 01 D3 to D5, user decision audit A6).
	public static partial class AblationIds
	{
		// lp_army_cap: the age-scaled team minimum is capped at the learned share of the eligible army (prior 800 permille). Row off = the fixed P1 cap.
		public const string LpArmyCap = "lp_army_cap";

		// lp_all_in_publish: an open all-in publishes on the eligible army and the guard claims all of it. Row off = the blob test and the old lend, as before.
		public const string LpAllInPublish = "lp_all_in_publish";

		// lp_reinforce: one reinforcement per seat and order, as one group. Row off = a WAVE-MIN group every pass, as before.
		public const string LpReinforce = "lp_reinforce";

		// lp_handover: a personality switch hands the pushed squads to the open order. Row off = the squads dissolve, as before.
		public const string LpHandover = "lp_handover";
	}
}
