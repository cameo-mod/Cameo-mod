#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// U1 (production service contract §3): the world-free decisions behind
	/// UnitBuilderBotModuleCA's fair-producer service — rotation resume over stable queue
	/// keys, ephemeral in-pass slot/cost reservations (an emitted StartProduction has not
	/// reached AllQueued inside the same decision batch), and the per-visit offer budget.
	/// Armed by the module's FairProducerService field behind SwitchCondition; every path
	/// here is deterministic and identical across clients for identical inputs.
	/// </summary>
	public static class UnitProducerServiceCA
	{
		/// <summary>
		/// The index in <paramref name="sortedKeys"/> at which service resumes: the entry
		/// AFTER <paramref name="lastKey"/>. An absent or null key (new map load, removed
		/// producer, save/load identity that no longer validates) resumes at the documented
		/// stable first key — never at an arbitrary position.
		/// </summary>
		public static int ResumeIndex(IReadOnlyList<string> sortedKeys, string lastKey)
		{
			if (sortedKeys.Count == 0)
				return 0;

			var at = -1;
			if (lastKey != null)
				for (var i = 0; i < sortedKeys.Count; i++)
					if (string.Equals(sortedKeys[i], lastKey, StringComparison.Ordinal))
					{
						at = i;
						break;
					}

			return (at + 1) % sortedKeys.Count;
		}

		/// <summary>
		/// Whether the producer may receive another order this pass: its free slots minus
		/// the orders already emitted to it in this decision batch.
		/// </summary>
		public static bool SlotAvailable(int freeSlots, int pendingThisPass) =>
			pendingThisPass < freeSlots;

		/// <summary>
		/// The bank a later pick in this batch may spend from — earlier emissions reserve
		/// their nominal cost even though the live bank has not moved yet.
		/// </summary>
		public static int BankAfterReserved(int bank, int reservedThisPass) =>
			bank - Math.Max(0, reservedThisPass);

		/// <summary>
		/// Producer offers this visit: bounded by the category's queue count and the armed
		/// per-category width (saturation/provider). Never zero while queues exist — a
		/// width of zero would silently starve a category.
		/// </summary>
		public static int OfferBudget(int queueCount, int maxQueues) =>
			Math.Min(queueCount, Math.Max(1, maxQueues));
	}
}
