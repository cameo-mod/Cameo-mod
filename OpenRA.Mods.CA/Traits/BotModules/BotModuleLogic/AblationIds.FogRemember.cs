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
	// FOG-REMEMBER-01 row (Orchestrator GO 11-28, user priority). Owner: Frontrunner.
	public static partial class AblationIds
	{
		// Generic-bot engineers judge an unseen candidate or defender by its frozen record (owner, validity, capture eligibility) instead of the live actor.
		// Off = the FOG-ENGCRATE scans: only seen or remembered actors are admitted, read live. Needs fog_engcrate on.
		public const string FogRemember = "fog_remember";
	}
}
