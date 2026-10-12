using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public static class M13LaunchPolicyCA
	{
		// Preserve legacy integer arithmetic exactly while off. M13's count valve is
		// the configured absolute limit, independent of the provider's value target.
		public static int MaxIdleUnits(bool enabled, int configured, int baseValue, int targetValue) =>
			enabled ? configured : Math.Max(1, (int)((long)configured * targetValue / baseValue));

		// Call only with the factual assault roster and a currently legal target.
		// Soft holds (including scaled bars and delay) cannot override this decision.
		public static bool Urgent(bool enabled, int eligibleCount, int configuredMaxIdleUnits,
			bool legalTarget, AttackLivenessPhaseCA phase) =>
			enabled && legalTarget && eligibleCount > 0 && phase != AttackLivenessPhaseCA.Unsupported
			&& (AttackLivenessEvalCA.AbsoluteOverflow(eligibleCount, configuredMaxIdleUnits)
				|| phase == AttackLivenessPhaseCA.DispatchDue);

		public static bool TryCommit(bool enabled, int legalDraftCount, long legalDraftValue,
			int configuredMaxIdleUnits, bool legalTarget, AttackLivenessEvalCA clock, Action commit)
		{
			if (!Urgent(enabled, legalDraftCount, configuredMaxIdleUnits, legalTarget, clock.Phase)
				|| legalDraftValue < 0 || (!AttackLivenessEvalCA.AbsoluteOverflow(legalDraftCount, configuredMaxIdleUnits)
					&& legalDraftValue < clock.ValueFloor))
				return false;
			commit();
			return true;
		}

		// AbsoluteOverflow commits may fire while the clock already has a pending intent
		// (Urgent allows any phase except Unsupported). Only the wave whose intent the
		// clock accepted may carry the intent marker and own the caller's pending slot —
		// an overflow wave must not clobber the earlier intent's timeout bookkeeping.
		public static bool TryMarkDispatchIntent(AttackLivenessEvalCA clock, long waveId) =>
			clock.NoteDispatchIntent(waveId);
	}
}
