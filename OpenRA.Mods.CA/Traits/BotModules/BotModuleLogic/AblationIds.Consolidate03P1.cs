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
	// CONSOLIDATE-03 P1 (b), the building-pick arbiter's behaviour rows (CONSOLIDATE_03_P1_DESIGN_01.md).
	public static partial class AblationIds
	{
		// With listed-only first copies (bb_build_all_listed) S5 still offers the capture-unlock types after the listed roll (Info BbS5CaptureReason). Off = S5 stops
		// after the roll, so cs_capture_unlock never fires on the staged path, as before.
		public const string BbS5CaptureReason = "bb_s5_capture_reason";

		// The early anti-rush count = clamp(prior x M(map) x L) (Info EarlyDefLearn). Off = the prior AntiRushBaseMin..Max, as before.
		public const string EarlyDefLearn = "early_def_learn";
	}
}
