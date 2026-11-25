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

using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	// QUEUE-SPLIT-01 and REF-S2-01 (Opus rulings 11-25): the world reads of the two rules; the rules themselves are QueueStageSplit (pure).
	partial class BaseBuilderQueueManagerCA
	{
		const int SeparateDefenceQueueRefreshTicks = 250;
		int separateDefenceQueueTick = -SeparateDefenceQueueRefreshTicks;
		bool separateDefenceQueue;

		// This player owns a dedicated defence queue (in DefenseQueues only) AND a distinct building queue (in BuildingQueues only); a queue in both sets is Shared
		// (QueueStageSplit.Classify). Refreshed every 250 ticks (a queue appears with its producer); a faction whose defences share the building queue has none,
		// so it keeps today's order.
		bool SeparateDefenceQueue()
		{
			var now = world.WorldTick;
			if (now - separateDefenceQueueTick < SeparateDefenceQueueRefreshTicks)
				return separateDefenceQueue;

			separateDefenceQueueTick = now;
			var b = baseBuilder.Info;
			var owned = b.BuildingQueues.Concat(b.DefenseQueues).Distinct().Where(c => AIUtils.FindQueues(player, c).Any());
			separateDefenceQueue = QueueStageSplit.SplitActive(owned, b.BuildingQueues, b.DefenseQueues);
			return separateDefenceQueue;
		}

		// This manager's queue class (Shared = in both sets: no split for it, and it is a building queue for the S2 zone demand).
		QueueKind ThisQueueKind => QueueStageSplit.Classify(Category, baseBuilder.Info.BuildingQueues, baseBuilder.Info.DefenseQueues);

		int RefineriesInFlight()
		{
			var n = 0;
			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (kv.Value > 0 && baseBuilder.Info.RefineryTypes.Contains(kv.Key))
					n += kv.Value;

			return n;
		}

		bool ZoneDemandAtS2(IBotExpansionTargetProvider law) =>
			QueueStageSplit.ZoneDemandAtS2(baseBuilder.RefineryAtS2Armed, law != null && law.RefinerySpacingActive, ThisQueueKind == QueueKind.Defence,
				RefineriesInFlight());
	}
}
