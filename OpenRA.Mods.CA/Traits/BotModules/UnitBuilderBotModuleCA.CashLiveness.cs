#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CASH-LIVENESS-01 row cl_stale_release (CashLiveness.cs): the unit-production pause votes are released once stale. A cap (IBotItemAwareUnitCap,
	// e.g. BotGlobalUnitBudget) is a live state and never stale: while one is among the holding votes the clock does not run.
	public partial class UnitBuilderBotModuleCA
	{
		AblationLatch ablateStaleRelease;
		int voteHoldSince = -1;
		int refineryHoldSince = -1;

		bool StaleReleaseOn => switchesActive && Info.CashLivenessStaleRelease && !ablateStaleRelease.Off(player, AblationIds.ClStaleRelease);

		/// <summary>The general pause votes (every vote but the base builder's, one clock for the set): released when stale. Row off = no clock, no work
		/// (Opus 11-27); a row turned on later starts its clock at the next visit.</summary>
		bool StaleVoteReleased(bool held, IBotRequestPauseUnitProduction baseBuilderPause)
		{
			if (!StaleReleaseOn)
				return false;

			// Under the fair service the caps are not part of `held` (they stop only the items they count); under the legacy service they are.
			var capInHold = false;
			if (held && !FairService)
				foreach (var rp in requestPause)
					if (!ReferenceEquals(rp, baseBuilderPause) && rp is IBotItemAwareUnitCap && rp.IsTraitEnabled() && rp.PauseUnitProduction)
					{
						capInHold = true;
						break;
					}

			return CashLiveness.Released(ref voteHoldSince, held && !capInHold, world.WorldTick, Info.StaleHoldMaxTicks,
				playerResources.GetCashAndResources(), Info.StaleHoldSpareCash);
		}

		/// <summary>The base builder's refinery pause: released when stale (a bot that banks StaleHoldSpareCash for StaleHoldMaxTicks and still has no
		/// refinery is not getting one by holding its units).</summary>
		bool StaleRefineryReleased(bool held)
		{
			if (!StaleReleaseOn)
				return false;

			return CashLiveness.Released(ref refineryHoldSince, held, world.WorldTick, Info.StaleHoldMaxTicks,
				playerResources.GetCashAndResources(), Info.StaleHoldSpareCash);
		}
	}
}
