#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */

namespace OpenRA.Mods.CA.Traits
{
	// QUEUE-SPLIT-01 and REF-S2-01 (Opus rulings 11-25).
	public static partial class AblationIds
	{
		// With a separate defence queue the building-queue walk skips the defence-only stages (S4, S9) and the defence-queue walk runs only the defence stages
		// (S4, S9, S10), never the refinery or economy stages (Info BbQueueSplit). Off = every queue walks every stage as before.
		public const string BbQueueSplit = "bb_queue_split";

		// With REF-DEMAND armed, unmet refinery zone demand is served at S2 on the building queue, one refinery in flight per bot (Info BbRefineryAtS2). Off = S2 as before.
		public const string BbRefineryAtS2 = "bb_refinery_s2";
	}
}
