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
	public static partial class AblationIds
	{
		// TURRET-NOHOLD-01: a READY ground turret is never held for the support cell or the front/back advisor; it falls back to the attacked building's legal cluster, then the own base.
		// Row off = the old hold (a turret with no in-range cell waits for the READY watchdog).
		public const string TurretNoHold = "turret_no_hold";
	}
}
