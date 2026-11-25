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
	// CONSOLIDATE-02 D1 stop-gap (Opus 2026-11-24, CONSOLIDATE_02_SQUAD_DESIGN.md X1/X11). Owner: Frontrunner.
	public static partial class AblationIds
	{
		// The push bar counts the blob that has ARRIVED at the rally: at least WAVE-MIN and TeamLeadBarArrivedPercent of the units COMMITTED to the order, without the
		// "half of the team's idle army must have arrived" term (BlobLaunch01CA.MinUnits), so units still at home never raise the bar; at the bar timeout the arrived
		// blob pushes at plain WAVE-MIN instead of the order being dissolved. Info switch: TeamLeadBarArrivedOnlyEnabled.
		public const string TeamLeadBarArrivedOnly = "tl_bar_arrived_only";
	}
}
