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
	// CONSOLIDATE-03 P4.
	public static partial class AblationIds
	{
		// The emergency power picks count the plants in production (Info PwInflightEmergency). Off = the raw surplus, as before.
		public const string PwInflightEmergency = "pw_inflight_emergency";

		// The power admission test counts the plants in production (Info PwInflightAdmit). Off = the raw surplus, as before.
		public const string PwInflightAdmit = "pw_inflight_admit";
	}
}
