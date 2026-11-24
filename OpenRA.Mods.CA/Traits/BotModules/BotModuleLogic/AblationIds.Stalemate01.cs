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
	// STALEMATE-01 rows (USER BINDING 11-23: no stalemates, games end decisively). Owner: Workhorse.
	// st_late_escalation off = no late attack-delay / launch-bar scaling. st_finish_mode off = a squad with nothing visible or remembered holds, as before.
	public static partial class AblationIds
	{
		public const string StalemateLateEscalation = "st_late_escalation";
		public const string StalemateFinishMode = "st_finish_mode";

		// BLOB-LAUNCH-01 (USER 11-24): bl_leader_order_only off = the team leader may form its own wave outside a team order, as before.
		public const string BlobLeaderOrderOnly = "bl_leader_order_only";
	}
}
