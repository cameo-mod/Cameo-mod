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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace OpenRA.Mods.CA.Traits
{
	// Decision arithmetic only. Callers must prove the selected commitment identity and
	// reconcile outstanding commitments before invoking this policy.
	public static class RecoverySpendPolicyCA
	{
		public enum Admission { General, OpeningOrEssential, SelectedRecovery }
		public enum EssentialLane { Power, Refinery, Production }
		public enum RecoveryLane { ConstructionMcv, Refinery, Harvester }

		// Slot availability is deliberately absent: a temporarily busy producer
		// must not turn a legal route into an illegal one or bypass the lease winner.
		public readonly record struct EssentialCandidate(EssentialLane Lane, uint Actor, int Queue, string Item);
		public readonly record struct RecoveryCandidate(RecoveryLane Lane, uint Actor, int Queue, string Item, int Cost);

		public static EssentialCandidate? SelectEssential(IEnumerable<EssentialCandidate> legalCandidates) =>
			legalCandidates.Where(x => x.Queue >= 0 && !string.IsNullOrEmpty(x.Item))
				.OrderBy(x => x.Lane).ThenBy(x => x.Actor).ThenBy(x => x.Queue)
				.ThenBy(x => x.Item, StringComparer.Ordinal).Select(x => (EssentialCandidate?)x).FirstOrDefault();

		public static RecoveryCandidate? SelectRecovery(IEnumerable<RecoveryCandidate> legalCandidates) =>
			legalCandidates.Where(x => x.Cost >= 0 && x.Queue >= 0 && !string.IsNullOrEmpty(x.Item))
				.OrderBy(x => x.Lane).ThenBy(x => x.Cost)
				.ThenBy(x => x.Item, StringComparer.Ordinal).ThenBy(x => x.Actor).ThenBy(x => x.Queue)
				.Select(x => (RecoveryCandidate?)x).FirstOrDefault();

		public static bool Admits(int available, int cost, int mobileFloor, int essentialFloor,
			int recoveryCharge, bool sameEssentialCommitment, Admission admission)
		{
			var remaining = (long)Math.Max(0, available) - Math.Max(0, cost);
			if (remaining < 0)
				return false;

			var essential = Math.Max(0, essentialFloor);
			if (admission == Admission.OpeningOrEssential)
				return true;
			if (admission == Admission.SelectedRecovery)
				return remaining >= essential;

			var floor = Math.Max(Math.Max(0, mobileFloor), essential);
			return remaining >= (long)floor + (sameEssentialCommitment ? 0 : Math.Max(0, recoveryCharge));
		}
	}

	// The player-scoped owner must persist this state and must settle a pending
	// reservation only after an authoritative refusal, queue transfer or terminal event.
	// This ledger is not a world trait and does not itself issue production orders.
	public readonly record struct RecoverySpendKey(uint Generation, uint Actor, int Queue, string Item);
	public readonly record struct RecoverySpendEntry(RecoverySpendKey Key, int Remaining, bool Queued);
	public sealed record RecoverySpendSnapshot(RecoverySpendEntry[] Entries, uint[] UsedGenerations,
		RecoverySpendKey? SelectedRecovery, (uint Actor, int Queue)? FloorLease, uint NextGeneration, bool Exhausted);

	public sealed class RecoverySpendLedgerCA
	{
		readonly Dictionary<RecoverySpendKey, int> pending = new();
		readonly Dictionary<RecoverySpendKey, int> inFlight = new();
		readonly HashSet<uint> usedGenerations = new();
		RecoverySpendKey? selectedRecovery;
		(uint Actor, int Queue)? floorLease;
		uint nextGeneration = 1;
		bool exhausted;

		public long CommittedCost => pending.Values.Concat(inFlight.Values).Aggregate(0L, (sum, cost) => sum + cost);
		public bool HasState => nextGeneration != 1 || exhausted || usedGenerations.Count != 0
			|| selectedRecovery != null || floorLease != null;
		public RecoverySpendEntry[] OutstandingEntries() => pending.Select(x => new RecoverySpendEntry(x.Key, x.Value, false))
			.Concat(inFlight.Select(x => new RecoverySpendEntry(x.Key, x.Value, true))).ToArray();

		public bool TryAllocateGeneration(out uint generation)
		{
			generation = 0;
			if (exhausted)
				return false;

			generation = nextGeneration;
			if (nextGeneration == uint.MaxValue)
				exhausted = true;
			else
				nextGeneration++;

			return true;
		}

		public void SelectRecovery(RecoverySpendKey? key) => selectedRecovery = key;

		public bool TryReserve(RecoverySpendKey key, int cash, int cost, int mobileFloor, int essentialFloor,
			int recoveryCharge, bool sameEssentialCommitment, RecoverySpendPolicyCA.Admission admission)
		{
			if (key.Generation == 0 || (!exhausted && key.Generation >= nextGeneration)
				|| string.IsNullOrEmpty(key.Item) || key.Queue < 0 || usedGenerations.Contains(key.Generation)
				|| admission == RecoverySpendPolicyCA.Admission.SelectedRecovery && selectedRecovery != key)
				return false;

			var uncommitted = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)cash - CommittedCost));
			if (!RecoverySpendPolicyCA.Admits(uncommitted, cost, mobileFloor, essentialFloor,
				recoveryCharge, sameEssentialCommitment, admission))
				return false;

			pending.Add(key, Math.Max(0, cost));
			usedGenerations.Add(key.Generation);
			return true;
		}

		// The caller must first prove this exact key identifies the accepted item.
		// A same-name observation or elapsed time cannot authorize a transfer.
		public bool TransferToQueue(RecoverySpendKey key, int remainingCost)
		{
			if (!pending.TryGetValue(key, out var reserved) || remainingCost < 0 || remainingCost > reserved)
				return false;

			pending.Remove(key);
			inFlight.Add(key, remainingCost);
			return true;
		}

		// Remaining cost can only fall for the exact tracked item.
		public bool ObserveRemaining(RecoverySpendKey key, int remainingCost)
		{
			if (!inFlight.TryGetValue(key, out var previous) || remainingCost < 0 || remainingCost > previous)
				return false;

			inFlight[key] = remainingCost;
			return true;
		}

		// Call only after authoritative refusal or an exact item's terminal event.
		public bool Settle(RecoverySpendKey key) => pending.Remove(key) || inFlight.Remove(key);

		public bool TryAcquireFloorLease(uint actor, int queue, bool slotAvailable,
			IEnumerable<(uint Actor, int Queue)> structurallyEligible)
		{
			if (floorLease != null || !slotAvailable)
				return false;

			var selected = structurallyEligible.OrderBy(x => x.Actor).ThenBy(x => x.Queue)
				.Select(x => ((uint Actor, int Queue)?)x).FirstOrDefault();
			if (selected == null || selected.Value != (actor, queue))
				return false;

			floorLease = selected;
			return true;
		}

		public bool ReleaseFloorLease(uint actor, int queue)
		{
			if (floorLease != (actor, queue))
				return false;

			floorLease = null;
			return true;
		}

		// Snapshot carries unresolved reservations and consumed generations across load.
		public RecoverySpendSnapshot Save() => new(
			pending.Select(x => new RecoverySpendEntry(x.Key, x.Value, false))
				.Concat(inFlight.Select(x => new RecoverySpendEntry(x.Key, x.Value, true)))
				.OrderBy(x => x.Key.Generation).ToArray(),
			usedGenerations.OrderBy(x => x).ToArray(), selectedRecovery, floorLease, nextGeneration, exhausted);

		static readonly JsonSerializerOptions SaveOptions = new() { IncludeFields = true };

		// Actual game-save wiring remains the responsibility of the player trait.
		public string SaveText() => JsonSerializer.Serialize(Save(), SaveOptions);

		public bool RestoreText(string text)
		{
			if (string.IsNullOrEmpty(text))
				return false;

			try
			{
				return Restore(JsonSerializer.Deserialize<RecoverySpendSnapshot>(text, SaveOptions));
			}
			catch (JsonException)
			{
				return false;
			}
		}

		public bool Restore(RecoverySpendSnapshot snapshot)
		{
			if (snapshot?.Entries == null || snapshot.UsedGenerations == null || snapshot.NextGeneration == 0
				|| snapshot.Exhausted && snapshot.NextGeneration != uint.MaxValue
				|| snapshot.UsedGenerations.Any(x => x == 0 || !snapshot.Exhausted && x >= snapshot.NextGeneration)
				|| snapshot.UsedGenerations.Distinct().Count() != snapshot.UsedGenerations.Length
				|| snapshot.Entries.Any(x => x.Remaining < 0 || x.Key.Queue < 0 || string.IsNullOrEmpty(x.Key.Item))
				|| snapshot.Entries.Select(x => x.Key.Generation).Distinct().Count() != snapshot.Entries.Length
				|| snapshot.Entries.Any(x => !snapshot.UsedGenerations.Contains(x.Key.Generation))
				|| snapshot.SelectedRecovery is { } selected && (selected.Generation == 0
					|| !snapshot.Exhausted && selected.Generation >= snapshot.NextGeneration
					|| selected.Queue < 0 || string.IsNullOrEmpty(selected.Item))
				|| snapshot.FloorLease is { } lease && lease.Queue < 0)
				return false;

			pending.Clear();
			inFlight.Clear();
			usedGenerations.Clear();
			foreach (var generation in snapshot.UsedGenerations)
				usedGenerations.Add(generation);
			foreach (var entry in snapshot.Entries)
				(entry.Queued ? inFlight : pending).Add(entry.Key, entry.Remaining);
			selectedRecovery = snapshot.SelectedRecovery;
			floorLease = snapshot.FloorLease;
			nextGeneration = snapshot.NextGeneration;
			exhausted = snapshot.Exhausted;
			return true;
		}
	}
}
