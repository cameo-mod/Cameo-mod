using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum AttackLivenessPhaseCA { Ineligible, EligibleWaiting, DispatchDue, DispatchPending, VerifiedDispatch, Unsupported }
	[Flags]
	public enum AttackSoftRestraintCA
	{
		None = 0, ValueBar = 1, CountBar = 2, AttackDelay = 4, Composition = 8,
		Director = 16, DefendMission = 32, TeamResponse = 64, Staging = 128,
		FormationSync = 256, CombatPolicy = 512, Scheduler = 1024
	}

	// No target/reason/personality parameter can renew an already eligible interval.
	// Dispatch intent is separate from reviewed outcome evidence; no game-world work here.
	public sealed class AttackLivenessEvalCA
	{
		public AttackLivenessPhaseCA Phase { get; private set; }
		public int EligibleSinceTick { get; private set; } = -1;
		public long ValueFloor { get; private set; }
		public long DeadlineTick { get; private set; } = -1;
		public int LastVerifiedLaunchTick { get; private set; } = -1;
		public int VerifiedLaunchCount { get; private set; }
		public AttackSoftRestraintCA Restraints { get; private set; }
		long pendingWave;
		long lastVerifiedWave;
		long lastIssuedWave;
		int dispatchLead;
		int lastTick = -1;

		public void Observe(int tick, bool evidenceComplete, bool active, bool legalTarget,
			bool orderableRoster, long eligibleValue, long configuredFloor, int maxNoLaunchTicks,
			int dispatchLeadTicks, AttackSoftRestraintCA restraints)
		{
			CheckTick(tick);
			Restraints = restraints;
			if (!evidenceComplete || configuredFloor <= 0 || eligibleValue < 0
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
				DeadlineTick = (long)tick + maxNoLaunchTicks;
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

		public bool ObserveDispatch(int tick, long waveId, int offensiveMembers,
			bool ownedOffensiveWave, bool actualOffensiveActivity, bool legalTarget, bool evidenceComplete)
		{
			CheckTick(tick);
			if (!evidenceComplete || !ownedOffensiveWave || !actualOffensiveActivity || !legalTarget
				|| offensiveMembers <= 0 || waveId == 0 || waveId != pendingWave || waveId <= lastVerifiedWave)
				return false;
			LastVerifiedLaunchTick = tick;
			VerifiedLaunchCount++;
			lastVerifiedWave = waveId;
			ClearInterval();
			Phase = AttackLivenessPhaseCA.VerifiedDispatch;
			return true;
		}

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
