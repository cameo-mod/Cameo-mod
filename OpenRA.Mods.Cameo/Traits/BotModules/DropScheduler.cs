#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion


using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	internal readonly record struct DropScheduleState(long NextOpportunityTick, long NextRegularTick);

	internal readonly record struct DropScheduleVisit(DropScheduleState State, bool ScanOpportunity, bool DrawRegular)
	{
		// Consume the due draw even when A admitted a run. A never consumes a draw of its own.
		public bool TryRegular(int? regularRoll, int chancePercent, bool opportunityAdmitted)
		{
			if (DrawRegular != regularRoll.HasValue || regularRoll is < 0 or >= 100)
				throw new ArgumentException("A due regular visit requires exactly one roll in [0, 100).", nameof(regularRoll));

			return !opportunityAdmitted && regularRoll is int roll && roll < chancePercent;
		}
	}

	internal readonly record struct DropScheduler(int ChancePercent, int IntervalTicks, int InitialDelayTicks)
	{
		public const int OpportunityIntervalTicks = 25;

		public static DropScheduler ForTier(int rank)
		{
			if (rank < 0 || rank > 10)
				throw new ArgumentOutOfRangeException(nameof(rank));

			return new DropScheduler(50 + 5 * rank, 525 - 30 * rank, 675 - 60 * rank);
		}

		public DropScheduleState Start(int matchStartTick) =>
			new(matchStartTick, (long)matchStartTick + InitialDelayTicks);

		// The caller persists State and the bot RNG stream together. On a due visit,
		// consume one roll before committing either trigger. Late visits coalesce.
		public DropScheduleVisit Visit(DropScheduleState state, int worldTick)
		{
			var scan = worldTick >= state.NextOpportunityTick;
			var draw = worldTick >= state.NextRegularTick;
			return new DropScheduleVisit(
				new DropScheduleState(
					scan ? (long)worldTick + OpportunityIntervalTicks : state.NextOpportunityTick,
					draw ? (long)worldTick + IntervalTicks : state.NextRegularTick),
				scan, draw);
		}
	}
}
