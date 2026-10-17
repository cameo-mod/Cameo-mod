using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// The saved player-level M13 record: seat identity, wave counter, base-danger
	// window, and the two component snapshots. A record with no active claim still
	// carries the cooldown list; no field may be invented from a tick or location.
	public sealed record M13PlayerSnapshotCA(int Version, string PlayerIdentity,
		long LastWaveId, long BaseDangerUntilTick,
		AttackLivenessSnapshotCA Liveness,
		TeamResponseBudgetSnapshotCA Responses);

	// The first armed player trait owns this object; disabled personalities retain it.
	// No static/team state and no randomness. All armed managers use one wave sequence.
	public sealed class M13PlayerStateCA
	{
		public AttackLivenessEvalCA Liveness { get; } = new();
		public TeamResponseBudgetCA Responses { get; } = new();
		public long LastWaveId { get; private set; }
		public bool EvidenceComplete { get; private set; } = true;
		public long BaseDangerUntilTick { get; private set; } = -1;

		// The save adapter's restore attempt marker. A legacy save (or a disabled owner)
		// leaves this false; the first armed tick then fails closed instead of running
		// on evidence that was never supplied.
		public bool RestoreSeen { get; private set; }

		public void NoteBaseDamage(int tick, int reviewTicks)
		{
			if (tick < 0 || reviewTicks <= 0)
				throw new ArgumentOutOfRangeException(nameof(tick));
			BaseDangerUntilTick = Math.Max(BaseDangerUntilTick, (long)tick + reviewTicks);
		}

		// Without restored interval and membership evidence, do not silently renew
		// a deadline or recycle a response slot. An adapter must supply both later.
		public void Invalidate() => EvidenceComplete = false;

		public long AllocateWave()
		{
			if (LastWaveId == long.MaxValue)
				throw new InvalidOperationException("M13 wave identity exhausted.");
			return ++LastWaveId;
		}

		// Exactly one compositional restore into a fresh instance. Both component
		// transports must accept the saved tick and the counter must dominate every
		// recorded wave; any rejection leaves EvidenceComplete false, which keeps a
		// half-restored clock or budget inert behind the manager's evidence gate.
		public bool TryRestore(int tick, string playerIdentity,
			M13PlayerSnapshotCA snapshot, long baseDangerWindow)
		{
			if (RestoreSeen)
				return false;
			RestoreSeen = true;
			var liveness = snapshot?.Liveness;
			var budget = snapshot?.Responses;
			var ok = snapshot != null && snapshot.Version == 1
				&& !string.IsNullOrEmpty(snapshot.PlayerIdentity)
				&& snapshot.PlayerIdentity == playerIdentity
				&& snapshot.LastWaveId >= 0
				&& snapshot.BaseDangerUntilTick >= -1
				&& (snapshot.BaseDangerUntilTick <= 0 || baseDangerWindow <= 0
					|| snapshot.BaseDangerUntilTick <= (long)tick + baseDangerWindow)
				&& Liveness.TryRestore(tick, liveness)
				&& Responses.TryRestore(tick, playerIdentity, budget)
				&& liveness != null
				&& snapshot.LastWaveId >= liveness.LastIssuedWave
				&& snapshot.LastWaveId >= liveness.PendingWave
				&& snapshot.LastWaveId >= liveness.LastObservedWave;
			if (!ok)
			{
				Invalidate();
				return false;
			}

			LastWaveId = snapshot.LastWaveId;
			BaseDangerUntilTick = snapshot.BaseDangerUntilTick;
			return true;
		}
	}
}
