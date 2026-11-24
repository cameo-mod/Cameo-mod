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
	// PROD-BUSY-01 (USER P0 11-23: "never stop producing; keep all queues busy at all times"). Pure decisions, one switch each.
	public static class ProdBusyCA
	{
		// Factories served per category per pass: every one (the offer budget clamps to the queue count) while the switch is on,
		// else the old saturation/provider width.
		public static int QueuesPerCategory(bool allFactories, bool saturated, int providerWidth) =>
			allFactories ? int.MaxValue : ProductionSaturationCA.QueuesPerCategory(saturated, providerWidth);

		// Whether the unit sweep ends after this category. Switch on: only when what is left of the bank (net of this pass's
		// orders) is under the production cash floor (the reserve exception). Switch off: the old MaximiseProductionCash break.
		public static bool StopCategorySweep(bool allCategories, bool saturated, long cashAndResources, int maximiseCash,
			long bankLeft, int cashFloor) =>
			allCategories ? bankLeft < cashFloor : ProductionSaturationCA.StopCategorySweep(saturated, cashAndResources, maximiseCash);

		// The bank a base-builder producer sees: net of this sweep's unpaid starts while the switch is on, else the live bank.
		public static int SweepBank(bool allProducers, int cashAndResources, int orderedThisSweep) =>
			allProducers ? cashAndResources - orderedThisSweep : cashAndResources;

		// Whether an earlier start in this base-builder sweep holds the next producer of the same category.
		public static bool QueuedThisSweepBlocks(bool allProducers, bool queuedThisSweep) => queuedThisSweep && !allProducers;

		// Whether the next base-builder sweep comes after the short (active) delay.
		public static bool ShortSweepDelay(bool active, bool idleSweep) => active || idleSweep;
	}
}
