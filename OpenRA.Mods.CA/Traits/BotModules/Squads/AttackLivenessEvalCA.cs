using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum AttackLivenessPhaseCA { Ineligible, EligibleWaiting, DispatchDue, DispatchPending, ObservedOffensiveActivity, Unsupported }
	// A receipt exists only when a fresh offensive wave actually resets the interval.
	// It describes public activity facts, never acceptance of the original dispatch order.
	public readonly record struct AttackLivenessProgressCA(int Tick, long WaveId, int OffensiveMembers,
		int ObservedCount, int PriorEligibleSinceTick, long PriorDeadlineTick);
	// Transport only. A manager adapter must also restore wave/member provenance.
	public sealed record AttackLivenessSnapshotCA(int Version, int LastTick, AttackLivenessPhaseCA Phase,
		int EligibleSinceTick, long ValueFloor, long DeadlineTick, int DispatchLead,
		int LastObservedOffensiveTick, int ObservedOffensiveCount, AttackSoftRestraintCA Restraints,
		long PendingWave, long LastObservedWave, long LastIssuedWave);
	[Flags]
	public enum AttackSoftRestraintCA
	{
		None = 0, ValueBar = 1, CountBar = 2, AttackDelay = 4, Composition = 8,
		Director = 16, DefendMission = 32, TeamResponse = 64, Staging = 128,
		FormationSync = 256, CombatPolicy = 512, Scheduler = 1024
	}

	// No target/reason/personality parameter can renew an already eligible interval.
	// Observed activity is not dispatch causality. No engine outcome or game-world work here.
	public sealed class AttackLivenessEvalCA
	{
		public AttackLivenessPhaseCA Phase { get; private set; }
		public int EligibleSinceTick { get; private set; } = -1;
		public long ValueFloor { get; private set; }
		public long DeadlineTick { get; private set; } = -1;
		public int LastObservedOffensiveTick { get; private set; } = -1;
		public int ObservedOffensiveCount { get; private set; }
		public AttackSoftRestraintCA Restraints { get; private set; }
		long pendingWave;
		long lastObservedWave;
		long lastIssuedWave;
		int dispatchLead;
		int lastTick = -1;
		bool restoreAttempted;
		bool restoreInvalid;

		public AttackLivenessSnapshotCA Capture()
		{
			if (restoreInvalid)
				throw new InvalidOperationException("Cannot capture an invalid restored liveness clock.");
			return new(1, lastTick, Phase, EligibleSinceTick, ValueFloor, DeadlineTick, dispatchLead,
				LastObservedOffensiveTick, ObservedOffensiveCount, Restraints,
				pendingWave, lastObservedWave, lastIssuedWave);
		}

		// Restore once into a fresh clock. A malformed save cannot buy a new deadline.
		public bool TryRestore(int tick, AttackLivenessSnapshotCA snapshot)
		{
			if (restoreAttempted || lastTick != -1)
				return false;
			restoreAttempted = true;
			restoreInvalid = true;
			Phase = AttackLivenessPhaseCA.Unsupported;
			if (tick < 0 || snapshot == null || snapshot.Version != 1
				|| snapshot.LastTick < -1 || snapshot.LastTick > tick || !Enum.IsDefined(snapshot.Phase)
				|| ((int)snapshot.Restraints & ~2047) != 0 || snapshot.DispatchLead < 0
				|| snapshot.PendingWave < 0 || snapshot.LastObservedWave < 0 || snapshot.LastIssuedWave < 0
				|| snapshot.PendingWave > snapshot.LastIssuedWave || snapshot.ObservedOffensiveCount < 0
				|| snapshot.ObservedOffensiveCount > snapshot.LastObservedWave
				|| snapshot.LastObservedOffensiveTick < -1 || snapshot.LastObservedOffensiveTick > snapshot.LastTick
				|| (snapshot.ObservedOffensiveCount == 0) != (snapshot.LastObservedWave == 0)
				|| (snapshot.ObservedOffensiveCount == 0) != (snapshot.LastObservedOffensiveTick == -1))
				return false;

			if (snapshot.EligibleSinceTick >= 0)
			{
				var duration = snapshot.DeadlineTick - snapshot.EligibleSinceTick;
				if (snapshot.EligibleSinceTick > snapshot.LastTick || snapshot.ValueFloor <= 0
					|| snapshot.DeadlineTick <= snapshot.EligibleSinceTick
					|| snapshot.DeadlineTick > (long)snapshot.EligibleSinceTick + int.MaxValue
					|| snapshot.DispatchLead >= duration
					|| snapshot.Phase == AttackLivenessPhaseCA.Ineligible
					|| snapshot.Phase == AttackLivenessPhaseCA.ObservedOffensiveActivity
					|| (snapshot.Phase != AttackLivenessPhaseCA.Unsupported
						&& ((snapshot.Phase == AttackLivenessPhaseCA.DispatchPending) != (snapshot.PendingWave != 0)))
					|| (snapshot.PendingWave != 0 && snapshot.PendingWave != snapshot.LastIssuedWave))
					return false;
			}
			else if (snapshot.EligibleSinceTick != -1 || snapshot.ValueFloor != 0
				|| snapshot.DeadlineTick != -1 || snapshot.PendingWave != 0
				|| (snapshot.Phase != AttackLivenessPhaseCA.Ineligible
					&& snapshot.Phase != AttackLivenessPhaseCA.ObservedOffensiveActivity
					&& snapshot.Phase != AttackLivenessPhaseCA.Unsupported)
				|| (snapshot.Phase == AttackLivenessPhaseCA.ObservedOffensiveActivity
					&& snapshot.ObservedOffensiveCount == 0))
				return false;

			if (snapshot.LastTick == -1 && (snapshot.LastIssuedWave != 0
				|| snapshot.ObservedOffensiveCount != 0 || snapshot.Phase != AttackLivenessPhaseCA.Ineligible))
				return false;
			EligibleSinceTick = snapshot.EligibleSinceTick;
			ValueFloor = snapshot.ValueFloor;
			DeadlineTick = snapshot.DeadlineTick;
			dispatchLead = snapshot.DispatchLead;
			LastObservedOffensiveTick = snapshot.LastObservedOffensiveTick;
			ObservedOffensiveCount = snapshot.ObservedOffensiveCount;
			Restraints = snapshot.Restraints;
			pendingWave = snapshot.PendingWave;
			lastObservedWave = snapshot.LastObservedWave;
			lastIssuedWave = snapshot.LastIssuedWave;
			Phase = snapshot.Phase;
			lastTick = tick;
			restoreInvalid = false;
			return true;
		}

		public void Observe(int tick, bool evidenceComplete, bool active, bool legalTarget,
			bool orderableRoster, long eligibleValue, long configuredFloor, int maxNoLaunchTicks,
			int dispatchLeadTicks, AttackSoftRestraintCA restraints, int firstWaveMaxTicks = 0)
		{
			CheckTick(tick);
			Restraints = restraints;
			if (restoreInvalid || !evidenceComplete || configuredFloor <= 0 || eligibleValue < 0
				|| maxNoLaunchTicks <= 0 || dispatchLeadTicks < 0 || dispatchLeadTicks >= maxNoLaunchTicks)
			{
				// Unsupported evidence cannot silently reset a previously established deadline.
				Phase = AttackLivenessPhaseCA.Unsupported;
				return;
			}
			var floor = EligibleSinceTick >= 0 ? ValueFloor : configuredFloor;
			if (!active || !legalTarget || !orderableRoster || eligibleValue < floor)
			{
				ClearInterval();
				Phase = AttackLivenessPhaseCA.Ineligible;
				return;
			}
			if (EligibleSinceTick < 0)
			{
				EligibleSinceTick = tick;
				ValueFloor = configuredFloor;
				DeadlineTick = (long)tick + IntervalCap(maxNoLaunchTicks, firstWaveMaxTicks, ObservedOffensiveCount);
				dispatchLead = dispatchLeadTicks;
			}
			Phase = pendingWave != 0 ? AttackLivenessPhaseCA.DispatchPending
				: tick >= DeadlineTick - dispatchLead ? AttackLivenessPhaseCA.DispatchDue
				: AttackLivenessPhaseCA.EligibleWaiting;
		}

		public bool NoteDispatchIntent(long waveId)
		{
			if (EligibleSinceTick < 0 || Phase == AttackLivenessPhaseCA.Unsupported
				|| waveId <= lastIssuedWave || pendingWave != 0)
				return false;
			pendingWave = waveId;
			lastIssuedWave = waveId;
			Phase = AttackLivenessPhaseCA.DispatchPending;
			return true;
		}

		public bool NoteDispatchFailed(long waveId)
		{
			if (waveId == 0 || waveId != pendingWave)
				return false;
			pendingWave = 0;
			// Resolving an old intent supplies no new eligibility evidence.
			// Only Observe with complete evidence may leave Unsupported.
			if (Phase != AttackLivenessPhaseCA.Unsupported)
				Phase = lastTick >= DeadlineTick - dispatchLead ? AttackLivenessPhaseCA.DispatchDue
					: AttackLivenessPhaseCA.EligibleWaiting;
			return true;
		}

		// Conservative monotonic dedup: an older wave observed after a newer one cannot renew
		// the deadline. No queued intent is needed or certified by these public activity facts.
		public bool ObserveOffensiveActivity(int tick, long waveId, int offensiveMembers,
			bool ownedOffensiveWave, bool actualOffensiveActivity, bool legalTarget, bool evidenceComplete)
			=> ObserveOffensiveActivity(tick, waveId, offensiveMembers, ownedOffensiveWave,
				actualOffensiveActivity, legalTarget, evidenceComplete, out _);

		public bool ObserveOffensiveActivity(int tick, long waveId, int offensiveMembers,
			bool ownedOffensiveWave, bool actualOffensiveActivity, bool legalTarget, bool evidenceComplete,
			out AttackLivenessProgressCA progress)
		{
			progress = default;
			CheckTick(tick);
			if (restoreInvalid || !evidenceComplete || !ownedOffensiveWave || !actualOffensiveActivity || !legalTarget
				|| offensiveMembers <= 0 || waveId <= 0 || waveId <= lastObservedWave)
				return false;
			LastObservedOffensiveTick = tick;
			ObservedOffensiveCount++;
			lastObservedWave = waveId;
			progress = new(tick, waveId, offensiveMembers, ObservedOffensiveCount,
				EligibleSinceTick, DeadlineTick);
			ClearInterval();
			Phase = AttackLivenessPhaseCA.ObservedOffensiveActivity;
			return true;
		}

		// First-wave arm (BV_m13_liveness): before this clock's first accepted wave the interval
		// deadline is capped at the tier's first-wave max. It can only shorten — it never renews
		// or extends an already created interval, and 0 disables it outright.
		public static int IntervalCap(int maxNoLaunchTicks, int firstWaveMaxTicks, long observedOffensiveCount) =>
			firstWaveMaxTicks > 0 && observedOffensiveCount == 0
				? Math.Min(maxNoLaunchTicks, firstWaveMaxTicks) : maxNoLaunchTicks;

		public static bool AbsoluteOverflow(int eligibleCount, int configuredMaxIdleUnits) =>
			eligibleCount > 0 && configuredMaxIdleUnits > 0 && eligibleCount >= configuredMaxIdleUnits;

		void ClearInterval()
		{
			EligibleSinceTick = -1;
			ValueFloor = 0;
			DeadlineTick = -1;
			pendingWave = 0;
		}

		void CheckTick(int tick)
		{
			if (tick < 0 || tick < lastTick)
				throw new ArgumentOutOfRangeException(nameof(tick));
			lastTick = tick;
		}
	}
}
