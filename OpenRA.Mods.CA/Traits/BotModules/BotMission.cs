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

		// MissionCard identity: the stable strategic reason, deterministic from
		// (Type, TargetPlayer, RegionIndex). Providers re-derive missions every
		// situation rebuild, so re-published instances of the same underlying
		// mission share an id and attempts accumulate against it.
		public int MissionId => unchecked(((int)Type * 397) ^ (RegionIndex * 31) ^ ((TargetPlayer?.ClientIndex ?? -1) * 17));
	}

	// Attempt lifecycle, fransotto's MissionCard vocabulary. Denied precedes a
	// commit; Recover is a mid-attempt state; Success/Failed are terminal.
	public enum BotMissionAttemptState { Denied, Committed, Progressing, Stalled, Recover, Success, Failed }

	// Opt-in write-back: providers implementing this receive attempt outcomes the
	// squad layer resolved. Strategic-layer consumers (dormant shelf, failure
	// memory — the generalized form of the per-region siege memory) adopt it
	// without forcing every provider to implement it.
	public interface IBotMissionOutcomeSink
	{
		void MissionAttemptResolved(BotMission mission, int attempt, BotMissionAttemptState state);
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
