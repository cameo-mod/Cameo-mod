using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// Transport only: the player coordinator must restore this once before admission,
	// and separately persist/restore the actual drafted membership.
	public sealed class TeamResponseBudgetSnapshotCA
	{
		public int Version { get; }
		public string PlayerIdentity { get; }
		public int LastTick { get; }
		public string ActiveKey { get; }
		public string Owner { get; }
		public int StartedTick { get; }
		public long ExpiresTick { get; }
		public IReadOnlyList<KeyValuePair<string, long>> Cooldowns { get; }

		public TeamResponseBudgetSnapshotCA(int version, string playerIdentity, int lastTick,
			string activeKey, string owner, int startedTick, long expiresTick,
			IReadOnlyList<KeyValuePair<string, long>> cooldowns)
		{
			Version = version;
			PlayerIdentity = playerIdentity;
			LastTick = lastTick;
			ActiveKey = activeKey;
			Owner = owner;
			StartedTick = startedTick;
			ExpiresTick = expiresTick;
			Cooldowns = cooldowns;
		}
	}

	public enum TeamResponseReleaseCA { None, Cleared, Stale, RequesterInactive, ReserveLost, Emergency, Expired }

	// One instance belongs to the player coordinator, shared by all personality managers.
	// This owns admission accounting only; the manager must release actual membership too.
	public sealed class TeamResponseBudgetCA
	{
		public string ActiveKey { get; private set; }
		public string Owner { get; private set; }
		public int StartedTick { get; private set; } = -1;
		public long ExpiresTick { get; private set; } = -1;
		const int MaximumCooldownClaims = 128;
		const int MaximumIdentityLength = 128;
		const int MaximumClaimLength = 512;
		bool restoreAttempted;
		bool admissionEvidenceComplete = true;
		readonly Dictionary<string, long> cooldowns = new(StringComparer.Ordinal);
		readonly List<string> expiredCooldowns = new(MaximumCooldownClaims);
		int lastTick = -1;

		public static bool HasSpare(long assaultValue, int assaultCount, long draftValue, int draftCount,
			long normalValueBar, int normalCountBar, bool residualRolesReady) =>
			assaultValue >= 0 && assaultCount >= 0 && draftValue >= 0 && draftCount > 0
			&& draftCount <= assaultCount && draftValue <= assaultValue && normalValueBar >= 0
			&& normalCountBar >= 0 && residualRolesReady
			&& assaultValue - draftValue >= normalValueBar && assaultCount - draftCount >= normalCountBar;

		public bool TryAcquire(int tick, string owner, string key, int leaseTicks, bool spare)
		{
			CheckTick(tick);
			PruneCooldowns(tick);
			if (!admissionEvidenceComplete || !spare || leaseTicks <= 0
				|| !BoundedIdentity(owner) || !BoundedClaim(key)
				|| ActiveKey != null || cooldowns.Count >= MaximumCooldownClaims || cooldowns.ContainsKey(key))
				return false;
			ActiveKey = key;
			Owner = owner;
			StartedTick = tick;
			ExpiresTick = (long)tick + leaseTicks;
			return true;
		}

		// A broadcast/rally/personality update cannot extend ExpiresTick.
		public TeamResponseReleaseCA Evaluate(int tick, bool evidenceFresh, bool contested,
			bool requesterActive, bool reserveIntact, bool emergency)
		{
			CheckTick(tick);
			if (ActiveKey == null)
				return TeamResponseReleaseCA.None;
			if (emergency) return TeamResponseReleaseCA.Emergency;
			if (!evidenceFresh) return TeamResponseReleaseCA.Stale;
			if (!requesterActive) return TeamResponseReleaseCA.RequesterInactive;
			if (!contested) return TeamResponseReleaseCA.Cleared;
			if (!reserveIntact) return TeamResponseReleaseCA.ReserveLost;
			return tick >= ExpiresTick ? TeamResponseReleaseCA.Expired : TeamResponseReleaseCA.None;
		}

		public bool Release(int tick, string owner, int cooldownTicks)
		{
			CheckTick(tick);
			if (ActiveKey == null || Owner != owner || cooldownTicks < 0)
				return false;
			// Admission reserves space for this cooldown; other managers cannot admit
			// while active. Never evict an unexpired claim to make rotation rearm it.
			cooldowns[ActiveKey] = (long)tick + cooldownTicks;
			ActiveKey = Owner = null;
			StartedTick = -1;
			ExpiresTick = -1;
			return true;
		}

		public TeamResponseBudgetSnapshotCA Capture(string playerIdentity)
		{
			if (!admissionEvidenceComplete || !BoundedIdentity(playerIdentity))
				throw new InvalidOperationException("Cannot capture an unsupported response budget.");
			var entries = new List<KeyValuePair<string, long>>(cooldowns);
			entries.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
			return new TeamResponseBudgetSnapshotCA(1, playerIdentity, lastTick, ActiveKey, Owner,
				StartedTick, ExpiresTick, entries.ToArray());
		}

		// Exactly one restore into a fresh instance. Rejecting corrupt/missing evidence
		// must not silently give that bot a new slot or erase a claim's cooldown.
		public bool TryRestore(int tick, string playerIdentity, TeamResponseBudgetSnapshotCA snapshot)
		{
			if (restoreAttempted || lastTick != -1)
				return false;
			restoreAttempted = true;
			admissionEvidenceComplete = false;
			if (tick < 0 || !BoundedIdentity(playerIdentity) || snapshot == null
				|| snapshot.Version != 1 || snapshot.PlayerIdentity != playerIdentity
				|| snapshot.LastTick < -1 || snapshot.LastTick > tick || snapshot.Cooldowns == null
				|| snapshot.Cooldowns.Count > MaximumCooldownClaims)
				return false;

			var active = snapshot.ActiveKey != null;
			if (active)
			{
				if (!BoundedClaim(snapshot.ActiveKey) || !BoundedIdentity(snapshot.Owner)
					|| snapshot.StartedTick < 0 || snapshot.StartedTick > snapshot.LastTick
					|| snapshot.ExpiresTick <= snapshot.StartedTick
					|| snapshot.ExpiresTick > (long)snapshot.StartedTick + int.MaxValue
					|| snapshot.Cooldowns.Count >= MaximumCooldownClaims)
					return false;
			}
			else if (snapshot.Owner != null || snapshot.StartedTick != -1 || snapshot.ExpiresTick != -1)
				return false;

			var restored = new Dictionary<string, long>(StringComparer.Ordinal);
			foreach (var entry in snapshot.Cooldowns)
			{
				if (!BoundedClaim(entry.Key) || entry.Key == snapshot.ActiveKey || entry.Value < 0
					|| snapshot.LastTick < 0 || entry.Value > (long)snapshot.LastTick + int.MaxValue
					|| !restored.TryAdd(entry.Key, entry.Value))
					return false;
			}

			foreach (var entry in restored)
				if (entry.Value > tick)
					cooldowns.Add(entry.Key, entry.Value);
			ActiveKey = snapshot.ActiveKey;
			Owner = snapshot.Owner;
			StartedTick = snapshot.StartedTick;
			ExpiresTick = snapshot.ExpiresTick;
			lastTick = tick;
			admissionEvidenceComplete = true;
			return true;
		}

		static bool BoundedIdentity(string value) => !string.IsNullOrEmpty(value) && value.Length <= MaximumIdentityLength;
		static bool BoundedClaim(string value) => !string.IsNullOrEmpty(value) && value.Length <= MaximumClaimLength;

		void PruneCooldowns(int tick)
		{
			expiredCooldowns.Clear();
			foreach (var entry in cooldowns)
				if (tick >= entry.Value)
					expiredCooldowns.Add(entry.Key);
			foreach (var key in expiredCooldowns)
				cooldowns.Remove(key);
		}

		void CheckTick(int tick)
		{
			if (tick < 0 || tick < lastTick)
				throw new ArgumentOutOfRangeException(nameof(tick));
			lastTick = tick;
		}
	}
}
