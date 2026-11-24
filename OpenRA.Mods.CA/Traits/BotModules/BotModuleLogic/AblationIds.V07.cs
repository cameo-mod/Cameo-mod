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
	// V07 rows (USER master replay 2026-11-24, via Opus). Owner: Frontrunner. Off = the pre-change behaviour of every bot, byte for byte (the Info switch is the
	// release control, the row the DEV ablation).
	public static partial class AblationIds
	{
		// PINCER-02: any approach separation of 90-180 degrees counts, the staging search fans out to more angles, and the pincer gate is read per arm.
		// Info switch: TeamLeadPincerWideEnabled.
		public const string TeamLeadPincerWide = "tl_pincer_wide";
	}
}
