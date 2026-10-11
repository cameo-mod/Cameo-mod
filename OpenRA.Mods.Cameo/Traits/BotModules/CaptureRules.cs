#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Capture-domain decisions extracted pure from <see cref="EngineerBotModule.QueueCaptureOrders"/> (hotspot
	/// #10) and shared with <see cref="GarrisonContestBotModule"/> — the shard-tier and ally-claim rules are the
	/// same BF-2/TC-2e contract on both. Free of World/Actor so every branch is unit-testable; behaviour is
	/// bit-identical to the inline conditions, including the lazily-drawn transport roll (<see cref="ShouldRollForTransport"/>
	/// runs before the draw; the existing WantsTransport check after it).
	/// </summary>
	public static class CaptureRules
	{
		/// <summary>An in-flight capture is superseded when an outranking ally claims its target cell.
		/// Only capture jobs carry a target worth superseding; a null target has nothing to claim, so the
		/// claim check is lazy — it dereferences the target's position and must not run on a null target.</summary>
		public static bool SupersedesCapture(bool isCaptureJob, bool hasTarget, Func<bool> targetClaimed) =>
			isCaptureJob && hasTarget && targetClaimed();

		/// <summary>The BF-2 shard tier: in-shard targets sort first. Shard size 1 makes every tier 0 (the no-op).</summary>
		public static int CaptureShardTier(int shardRank, int shardSize, int targetShard)
		{
			if (shardSize <= 1)
				return 0;

			return targetShard == shardRank ? 0 : 1;
		}

		/// <summary>Whether the transport roll is even drawn — the gate BEFORE LocalRandom.Next is touched, so a
		/// chance of 0 or an escort-eligible target leaves the random sequence exactly as classic's.</summary>
		public static bool ShouldRollForTransport(int transportChancePct, bool escortEligible) =>
			transportChancePct > 0 && !escortEligible;

		/// <summary>Whether the priority (targeted) pass runs this tick — the PriorityCaptureChance roll.</summary>
		public static bool UsesPriorityPass(int roll, int priorityChancePct) => roll < priorityChancePct;

		/// <summary>The per-engineer open-target filter: not full, not dormant, not escort-blocked. The escort
		/// check stays lazy — it can run a defence scan, so it must not evaluate for full/dormant targets.</summary>
		public static bool IsOpenTarget(bool targetFull, bool dormant, Func<bool> blockedByEscort) =>
			!targetFull && !dormant && !blockedByEscort();
	}
}
