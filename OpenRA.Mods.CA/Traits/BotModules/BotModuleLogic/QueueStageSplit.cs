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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>How a queue name sits in the BuildingQueues and DefenseQueues sets. Shared = in both: it keeps the legacy single walk (S4 and S9 intact).</summary>
	public enum QueueKind { Other, Building, Defence, Shared }

	// QUEUE-SPLIT-01 (user correction 11-25, Opus): defences and buildings use separate queues and must not influence each other. A defence-only stage never
	// returns early on the building queue (it starved refineries and S5 in V08C), and the defence queue never runs the refinery or economy stages (the V08C
	// not_buildable rows). A player whose defences share the building queue has no split and keeps today's order.
	public static class QueueStageSplit
	{
		/// <summary>Opus ruling 11-25: in the defence set only = Defence (dedicated), in the building set only = Building, in both = Shared, in neither = Other.</summary>
		public static QueueKind Classify(string name, ISet<string> buildingSet, ISet<string> defenceSet)
		{
			var b = buildingSet != null && buildingSet.Contains(name);
			var d = defenceSet != null && defenceSet.Contains(name);
			return b && d ? QueueKind.Shared : d ? QueueKind.Defence : b ? QueueKind.Building : QueueKind.Other;
		}

		/// <summary>Separate walks only when the player owns a dedicated defence queue AND a distinct building queue; a Shared or Other queue never counts for either.</summary>
		public static bool SplitActive(IEnumerable<string> ownedQueueNames, ISet<string> buildingSet, ISet<string> defenceSet)
		{
			var owned = ownedQueueNames.Select(n => Classify(n, buildingSet, defenceSet)).ToList();
			return owned.Contains(QueueKind.Defence) && owned.Contains(QueueKind.Building);
		}

		/// <summary>The stages that only ever offer defences: the anti-rush set (S4) and the extra defences (S9).</summary>
		public static bool DefenceOnly(BaseBuildStage stage) => stage == BaseBuildStage.S4AntiRush || stage == BaseBuildStage.S9ExtraDefence;

		/// <summary>Whether a queue walks a stage. No split = every stage. Split: the defence queue walks S4, S9 and the S10 roll (its listed defences); the
		/// building queue walks every stage but S4 and S9.</summary>
		public static bool Allowed(BaseBuildStage stage, bool split, bool defenceQueue)
		{
			if (!split)
				return true;

			return defenceQueue ? DefenceOnly(stage) || stage == BaseBuildStage.S10Fractions : !DefenceOnly(stage);
		}

		/// <summary>REF-S2-01: S2 also serves unmet refinery zone demand when armed, REF-DEMAND's spacing law is active, the queue is not a defence queue and no
		/// refinery of the player is in flight (one at a time). The economy gate (ref1, war factory, ref2, interval) stays inside the wanted-refinery helper.</summary>
		public static bool ZoneDemandAtS2(bool armed, bool refDemandActive, bool defenceQueue, int refineriesInFlight) =>
			armed && refDemandActive && !defenceQueue && refineriesInFlight <= 0;
	}
}
