#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// Cross-manager restore verdict over plain scalars gathered from every armed
	// manager. Consistent = restored membership matches the shared record exactly;
	// DeadPendingIntent = the pending wave's intent squad did not survive the save —
	// a legitimate dispatch failure the clock resolves once; Inconsistent = any
	// corruption, so all restored evidence stays UNKNOWN and admission stays closed.
	public enum M13MembershipVerdictCA { Consistent, DeadPendingIntent, Inconsistent }

	public sealed record M13MembershipSnapshotCA(
		long MaxSquadWave,
		int IntentFlagCount,
		long IntentWave,
		long PendingWave,
		long LastIssuedWave,
		long LastWaveId,
		int ObservedWithoutWave,
		int CorruptSquads,
		int ResponseFlagCount,
		int ResponseSquadCount,
		string BudgetActiveKey,
		string BudgetOwner,
		string ResponseOwnerIdentity,
		string RecomputedResponseKey);

	public static class M13RestoreMembershipCA
	{
		public static M13MembershipVerdictCA Evaluate(M13MembershipSnapshotCA s)
		{
			if (s == null || s.CorruptSquads > 0
				|| s.MaxSquadWave > s.LastWaveId
				|| s.IntentFlagCount > 1
				|| s.ObservedWithoutWave > 0
				|| s.ResponseFlagCount > 1
				|| s.ResponseSquadCount > 1)
				return M13MembershipVerdictCA.Inconsistent;

			// A recorded pending wave needs its intent squad. When that squad is gone
			// the dispatch simply failed; when no wave is pending a flag is corrupt.
			if (s.PendingWave != 0 && s.IntentFlagCount == 0)
				return M13MembershipVerdictCA.DeadPendingIntent;
			if (s.PendingWave == 0 && s.IntentFlagCount != 0)
				return M13MembershipVerdictCA.Inconsistent;
			if (s.IntentFlagCount == 1
				&& (s.IntentWave <= 0 || s.IntentWave != s.PendingWave || s.IntentWave > s.LastIssuedWave))
				return M13MembershipVerdictCA.Inconsistent;

			// An active budget claim needs exactly one live flagged protection squad
			// held by the manager whose identity owns the lease and whose retained
			// request reproduces the claim key. No key may be invented.
			if (s.BudgetActiveKey != null
				&& (s.ResponseFlagCount != 1 || s.ResponseSquadCount != 1
					|| s.BudgetOwner == null || s.BudgetOwner != s.ResponseOwnerIdentity
					|| s.RecomputedResponseKey == null
					|| s.RecomputedResponseKey != s.BudgetActiveKey))
				return M13MembershipVerdictCA.Inconsistent;
			if (s.BudgetActiveKey == null && (s.ResponseFlagCount != 0 || s.ResponseSquadCount != 0))
				return M13MembershipVerdictCA.Inconsistent;

			return M13MembershipVerdictCA.Consistent;
		}
	}
}
