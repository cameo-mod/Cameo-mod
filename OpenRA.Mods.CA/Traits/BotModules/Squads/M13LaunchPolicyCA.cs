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
	}
}
