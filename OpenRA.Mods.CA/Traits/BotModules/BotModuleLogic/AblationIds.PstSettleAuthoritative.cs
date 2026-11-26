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
	// CONSOLIDATE-03 P6 (b) row. Owner: Code Monkey (Opus ruling, option a).
	// Off = the upgrade settle reads the owned prerequisites from the bot's own scan (OwnedPrerequisites), as before.
	// On = the owned prerequisites come from the player's own TechTree (TechTree.HasPrerequisites, the engine's owner-only check).
	public static partial class AblationIds
	{
		public const string PstSettleAuthoritative = "pst_settle_authoritative";
	}
}
