#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// Recon remains owned by ScoutBotModule; Secure is reserved for a later phase.
	public enum BotMissionType { Recon, Raid, Secure, Defend }

	public sealed class BotMission
	{
		public BotMissionType Type;
		public CPos Location;
		public Player TargetPlayer;
		public int RegionIndex;
		public int RequiredValue;
		public int Priority;

		// MissionCard id (docs/design/AI_MISSION_CARDS.md §2.1): allocated by the
		// mission's owner as a stable per-player integer; 0 = not yet allocated,
		// consumers fall back to IdentityKey until owner allocation lands (MC1).
		public int MissionId;

		// The identity key — deterministic from (Type, TargetPlayer, RegionIndex).
		// Providers re-derive missions every situation rebuild, so re-published
		// instances of the same underlying mission share the key; it is what the
		// owner allocates MissionId from and what consumers key attempts on.
		public int IdentityKey => unchecked(((int)Type * 397) ^ (RegionIndex * 31) ^ ((TargetPlayer?.ClientIndex ?? -1) * 17));

		public int EffectiveMissionId => MissionId != 0 ? MissionId : IdentityKey;
	}

	// Closed attempt/mission lifecycle (AI_MISSION_CARDS.md §2.2). Proposed,
	// Denied and Dormant are owner-side states; executors emit Committed onward
	// and must report exactly one terminal state (Succeeded, Failed, Abandoned)
	// per attempt.
	public enum BotMissionState { Proposed, Denied, Dormant, Committed, Progressing, Stalled, Recovering, Succeeded, Failed, Abandoned }

	// Closed transition reasons (AI_MISSION_CARDS.md §2.2); projects may extend
	// under their own prefix in serialized cards.
	public enum BotMissionReason { None, NoUnits, Unreachable, Undeployable, Reserved, Outmatched, TargetGone, Timeout, Stuck, Superseded, LostUnits, Done }

	// The return path (AI_MISSION_CARDS.md §2.3): executors report attempt
	// transitions; the owner alone decides what a failure means. Opt-in —
	// providers adopt without forcing every provider to implement it.
	public interface IBotMissionOutcomeSink
	{
		void Report(int missionId, int attemptId, BotMissionState state, BotMissionReason reason, int tick);
	}

	public sealed class BotMissionAssignment
	{
		public BotMissionType Type;
		public int RegionIndex;
		public bool Frozen;
	}

	public interface IBotMissionProvider
	{
		IReadOnlyList<BotMission> Missions { get; }
		void MissionTaken(BotMission mission);
	}

	public interface IBotMissionAssignmentProvider
	{
		BotMissionAssignment LastMissionAssignment { get; }
	}
}
