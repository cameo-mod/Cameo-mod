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
	// V07 (USER master replay 2026-11-24, via Opus). Every value is a named yaml parameter; none is read while its switch is false.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("PINCER-02 (release switch; the DEV row is tl_pincer_wide). On (needs TeamLeadPincerEnabled): any approach separation of TeamLeadPincerMinAngleDegrees to 180 counts, the staging search fans out to more angles when the preferred spread has no legal point, and the two-times-WAVE-MIN gate is read per arm (TeamLeadPincerArmPercent). Off: TEAM-PINCER-01 exactly as before.")]
		public readonly bool TeamLeadPincerWideEnabled = false;

		[Desc("PINCER-02: the smallest angle between the two approaches, as seen from the target, in degrees. Range 60-180; 90 is the user rule (90-180 counts).")]
		public readonly int TeamLeadPincerMinAngleDegrees = 90;

		[Desc("PINCER-02: each arm needs this percent of the WAVE-MIN bars (units and value, each arm on its own); 100 = a full WAVE-MIN per arm. Range 50-100. Below 100 the pincer fires with a smaller blob (the pair still holds at least twice the arm bar, so never less than this share of two waves).")]
		public readonly int TeamLeadPincerArmPercent = 75;
	}
}
