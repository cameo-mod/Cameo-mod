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
	// PROD-IDLE G1 rows (PROD-IDLE_PLAN_03 section 5, PLAN_04). Owner: Frontrunner. Other owners add their own file.
	public static partial class AblationIds
	{
		public const string FairProducer = "pi_fair_producer";
		public const string ArmyRatio = "pi_army_ratio";
		public const string Overflow = "pi_overflow";
		public const string Lean = "pi_lean";
		public const string DemandCapturers = "pi_demand_capturers";
		public const string RoleMix = "pi_role_mix";
		public const string DerivedWeights = "pi_derived_weights";
		public const string QueueDepth = "pi_queue_depth";
		public const string Recovery = "pi_recovery";
	}
}
