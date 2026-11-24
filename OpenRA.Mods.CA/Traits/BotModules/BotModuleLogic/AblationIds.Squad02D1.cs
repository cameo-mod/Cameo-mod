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
		// The push bar counts only the committed blob that has ARRIVED at the rally: WAVE-MIN units and value, without the "half of the team's idle army
		// must have arrived" term (BlobLaunch01CA.MinUnits), so units still at home never raise the bar. Info switch: TeamLeadBarArrivedOnlyEnabled.
		public const string TeamLeadBarArrivedOnly = "tl_bar_arrived_only";
	}
}
