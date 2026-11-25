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
	// BUILD-ALL-B (user ruling 2026-11-25).
	public static partial class AblationIds
	{
		// The base builder never queues a terrain foundation (D2k concrete; FoundationExclusion.IsFoundation) on any path (Info BbNoFoundation). Off = as before.
		public const string BbNoFoundation = "bb_no_foundation";
	}
}
