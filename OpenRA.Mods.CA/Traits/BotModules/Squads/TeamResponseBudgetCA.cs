using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
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
			if (!spare || leaseTicks <= 0 || string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(key)
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
