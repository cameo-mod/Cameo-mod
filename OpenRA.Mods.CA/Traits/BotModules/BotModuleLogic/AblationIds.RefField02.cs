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
	// REF-FIELD-02 rows (USER PRIORITY 11-24: refinery spam). Owner: Workhorse.
	public static partial class AblationIds
	{
		// rf_field_gate off = no per-field refinery cap and no lost-refinery lock in the expansion planner's claims, as before.
		public const string RefineryFieldGate = "rf_field_gate";

		// rf_economy_gate off = the economy rung's refinery pick has no war-factory step, interval or essential yield, as before.
		public const string RefineryEconomyGate = "rf_economy_gate";
	}
}
