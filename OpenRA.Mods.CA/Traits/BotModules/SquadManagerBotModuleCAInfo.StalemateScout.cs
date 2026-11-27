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
	// STALEMATE-SCOUT-01 (USER decision D5 on audit A7). Read only behind the one launcher, off in code and in classic.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("STALEMATE-SCOUT-01 (row ss_scout_push; needs the one launcher and the team leader): a push that is ready with no known target scouts the unscouted or stale regions of the tactical map (the scout module's region memory),",
			"then attacks what it finds, instead of waiting at home. Only a target that is genuinely unreachable excuses a push that does not attack. The plain-grid hunt stays the fallback. Off in code and classic.")]
		public readonly bool StalemateScoutEnabled = false;

		[Desc("STALEMATE-SCOUT-01: the earliest tick at which a ready push with no known target scouts (0 = from the first ready push).")]
		public readonly int StalemateScoutStartTick = 0;
	}
}
