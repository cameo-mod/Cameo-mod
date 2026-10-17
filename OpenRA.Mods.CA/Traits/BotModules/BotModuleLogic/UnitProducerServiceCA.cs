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
using System.Linq;

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

		/// <summary>
		/// P4 combat-infantry floor pick order: highest combat value per unit cost first,
		/// ordinal name as the deterministic tiebreak — the same inputs on every client
		/// produce the same pick.
		/// </summary>
		public static IEnumerable<string> CombatValueOrder(IEnumerable<(string Name, double Strength, int Cost)> candidates) =>
			candidates
				.OrderByDescending(c => c.Strength / Math.Max(1, c.Cost))
				.ThenBy(c => c.Name, StringComparer.Ordinal)
				.Select(c => c.Name);
	}

	/// <summary>
	/// U1: the ephemeral decision-batch reservation ledger (Sol F1) — created at the top
	/// of each BotTick decision pass, shared by the external-request scan and every
	/// category visit, then discarded. Emitted orders spend from one bank and one set of
	/// per-producer slots so no call site can oversubscribe what another already claimed.
	/// NOT durable state: nothing survives into the next pass — that ledger is U2's.
	/// </summary>
	public sealed class ProducerServiceBatch
	{
		/// <summary>The bank observed at batch creation — reservations draw it down.</summary>
		public readonly int Bank;

		/// <summary>Nominal cost of every order emitted in this batch.</summary>
		public int ReservedCash;

		readonly Dictionary<object, int> pending = new();

		/// <summary>Per-name cap headroom computed lazily per unit (live census minus
		/// the limit on first sight), shared by every call site in the batch.</summary>
		public readonly Dictionary<string, int> LimitHeadroom = new(StringComparer.Ordinal);

		/// <summary>In-pass emissions that live counts cannot see yet.</summary>
		public int PendingAircraft, PendingAirToAir, PendingArmedInfantry, PendingCapturers;

		public ProducerServiceBatch(int bank) => Bank = bank;

		/// <summary>What remains spendable after this batch's emissions.</summary>
		public int BankLeft => UnitProducerServiceCA.BankAfterReserved(Bank, ReservedCash);

		public bool Affords(int cost) => BankLeft >= Math.Max(0, cost);

		/// <summary>Live free slots minus what this batch already emitted to that producer —
		/// keyed by queue instance, so same-actor/same-type siblings reserve independently.</summary>
		public int SlotsFree(object queue, int liveFreeSlots) =>
			liveFreeSlots - pending.GetValueOrDefault(queue);

		public int PendingOf(object queue) => pending.GetValueOrDefault(queue);

		/// <summary>Record an emitted order: spends bank and one producer slot.</summary>
		public void Reserve(object queue, int cost)
		{
			ReservedCash += Math.Max(0, cost);
			pending[queue] = PendingOf(queue) + 1;
		}
	}
}
