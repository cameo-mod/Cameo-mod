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
	// PERF-UB-PICK-01 (Opus 11-26, DEV only): how much work the unit roll does per pick, appended to the ub_perf row (UbVisitEnd) of the visit.
	// rolls = ChooseRollUnit calls of the visit, picks = the rolls that returned a unit (a refused pick re-rolls, so rolls >= picks),
	// examined = the listed buildable types the rolls looked at (the legal pool plus every exclusion, the skip set included), pool = the legal pool sizes summed,
	// examined_per_pick = examined / picks (examined when no roll picked). Counters only: nothing here is read by a decision, and a release build has no fields and no calls.
	public partial class UnitBuilderBotModuleCA
	{
#if CAMEO_DEV_LOGGING
		int ubRolls, ubPicks, ubExamined, ubPooled;
#endif

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void UbPickCountReset()
		{
#if CAMEO_DEV_LOGGING
			ubRolls = ubPicks = ubExamined = ubPooled = 0;
#endif
		}

		// One roll's legal pool, counted where ChooseRollUnit has built it.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void UbRollPool(int examined, int pooled)
		{
#if CAMEO_DEV_LOGGING
			ubExamined += examined;
			ubPooled += pooled;
#endif
		}

		// One pick call of the fill; only the roll's calls count.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void UbRollDone(bool roll, bool picked)
		{
#if CAMEO_DEV_LOGGING
			if (!roll)
				return;

			ubRolls++;
			if (picked)
				ubPicks++;
#endif
		}

#if CAMEO_DEV_LOGGING
		string UbPickCounts() =>
			$";rolls={ubRolls};picks={ubPicks};examined={ubExamined};pool={ubPooled};examined_per_pick={(ubPicks > 0 ? (double)ubExamined / ubPicks : ubExamined):0.0}";
#endif
	}
}
