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
	// NO-CANCEL-01.
	public static partial class AblationIds
	{
		// A building in production is never cancelled for a choice: the six removal sites withhold unless the item is READY with an unplaceable proof (Info BbNoCancel). Off = the six sites as before.
		public const string BbNoCancel = "bb_no_cancel";
	}
}
