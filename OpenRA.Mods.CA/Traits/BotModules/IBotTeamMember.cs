#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TC-1 (AI_DEEP_RESEARCH.md §11, AI_ARCHITECTURE.md §12.17): one allied bot's
	/// broadcast on the team blackboard — what it would say on voice chat, as
	/// fog-honest scalars refreshed once per situation snapshot. An immutable
	/// value carrier: own-side readings plus the DI-1 pacing wave, never an
	/// enumeration of enemy actors.
	/// </summary>
	public class TeamBroadcast
	{
		/// <summary>Tick of the snapshot that produced this broadcast; 0 = never published.</summary>
		public readonly int SnapshotTick;

		/// <summary>The bot's own army value — an own-side scalar, no enemy knowledge.</summary>
		public readonly int OwnArmyValue;

		/// <summary>
		/// Urgency as 0 = Normal, 1 = Pressured, 2 = Emergency — the Cameo
		/// <c>BotUrgency</c> ordinals carried as an int because that enum lives in
		/// an assembly this one cannot reference.
		/// </summary>
		public readonly int UrgencyLevel;

		/// <summary>The DI-1 Director's tension in [0,100], forwarded verbatim.</summary>
		public readonly int DirectorTension;

		/// <summary>The DI-1 Director's wave phase, forwarded verbatim.</summary>
		public readonly DirectorPhase DirectorPhase;

		/// <summary>The enemy this bot is currently committed to, or null.</summary>
		public readonly Player MainTarget;

		/// <summary>True while the bot is under pressure (UrgencyLevel &gt;= 1) and wants help.</summary>
		public readonly bool RequestsDefence;

		/// <summary>
		/// Where help is needed — the bot's own base centre while under pressure;
		/// <see cref="WPos.Zero"/> when none (not pressured, or no base stands).
		/// </summary>
		public readonly WPos DefendPosition;

		public TeamBroadcast(int snapshotTick, int ownArmyValue, int urgencyLevel, int directorTension,
			DirectorPhase directorPhase, Player mainTarget, bool requestsDefence, WPos defendPosition)
		{
			SnapshotTick = snapshotTick;
			OwnArmyValue = ownArmyValue;
			UrgencyLevel = urgencyLevel;
			DirectorTension = directorTension;
			DirectorPhase = directorPhase;
			MainTarget = mainTarget;
			RequestsDefence = requestsDefence;
			DefendPosition = defendPosition;
		}

		/// <summary>What an absent, disabled or never-snapshotted provider publishes.</summary>
		public static readonly TeamBroadcast Empty =
			new(0, 0, 0, 0, DirectorPhase.BuildUp, null, false, WPos.Zero);
	}

	/// <summary>
	/// TC-1 (AI_DEEP_RESEARCH.md §11, AI_ARCHITECTURE.md §12.17): the publish end
	/// of the team blackboard. Every bot of a match runs on the host
	/// (AI_ARCHITECTURE.md §1.1), so allied bots read each other's PlayerActor
	/// traits directly — an unsynced blackboard, no sync or network work, and no
	/// cheat: only own-side scalars and ally-published data cross it. Publish-only:
	/// nothing consumes it yet; TC-2 adds the consumers (synchronised attack
	/// windows, defend-request answering, expansion-claim deconfliction, role-split
	/// bias, human-ally beacons). Providers live in OpenRA.Mods.Cameo and must not
	/// be referenced by name from this assembly; absent or disabled providers
	/// publish <see cref="TeamBroadcast.Empty"/> — never a behaviour change.
	/// </summary>
	public interface IBotTeamMember
	{
		/// <summary>The bot's latest snapshot broadcast; <see cref="TeamBroadcast.Empty"/> when none.</summary>
		TeamBroadcast Broadcast { get; }
	}

	/// <summary>The TC-1 summary of what a bot's allies published.</summary>
	public sealed class TeamBlackboardSummary
	{
		/// <summary>Allied bots whose broadcast joined this summary.</summary>
		public int AlliedBots;

		/// <summary>Sum of allied <see cref="TeamBroadcast.OwnArmyValue"/>.</summary>
		public int TotalArmyValue;

		/// <summary>Highest allied <see cref="TeamBroadcast.DirectorTension"/>.</summary>
		public int MaxTension;

		/// <summary>True while at least one ally's Director wave is at Climax.</summary>
		public bool AnyClimax;

		/// <summary>Allies currently asking for defence.</summary>
		public int DefendRequests;

		/// <summary>
		/// Size of the largest group of broadcasts sharing the same non-null
		/// <see cref="TeamBroadcast.MainTarget"/> — allies committed to the same enemy.
		/// </summary>
		public int SharedTargetCount;
	}

	/// <summary>
	/// TC-1 (AI_ARCHITECTURE.md §12.17): reads the allied half of the team
	/// blackboard. The caller's own broadcast is deliberately NOT folded in — a
	/// bot reads its own scalars directly, so the summary answers "what is the
	/// rest of the team doing". Fog-honest: <see cref="World.Players"/> filtered
	/// to allied bots plus their published broadcasts — the same seam
	/// AlliedCommitments already uses — never an actor enumeration. Pure
	/// aggregation in <see cref="Aggregate"/> keeps the logic testable without a
	/// World.
	/// </summary>
	public static class TeamBlackboard
	{
		/// <summary>Fold broadcasts into one summary; null or empty input yields all zeros.</summary>
		public static TeamBlackboardSummary Aggregate(IEnumerable<TeamBroadcast> broadcasts)
		{
			var summary = new TeamBlackboardSummary();
			if (broadcasts == null)
				return summary;

			var sharedTargets = new Dictionary<Player, int>();
			foreach (var broadcast in broadcasts)
			{
				if (broadcast == null)
					continue;

				summary.AlliedBots++;
				summary.TotalArmyValue += broadcast.OwnArmyValue;
				summary.MaxTension = Math.Max(summary.MaxTension, broadcast.DirectorTension);
				if (broadcast.DirectorPhase == DirectorPhase.Climax)
					summary.AnyClimax = true;
				if (broadcast.RequestsDefence)
					summary.DefendRequests++;
				if (broadcast.MainTarget != null)
					sharedTargets[broadcast.MainTarget] = sharedTargets.GetValueOrDefault(broadcast.MainTarget) + 1;
			}

			foreach (var count in sharedTargets.Values)
				summary.SharedTargetCount = Math.Max(summary.SharedTargetCount, count);

			return summary;
		}

		/// <summary>
		/// Read every allied bot's latest broadcast — allies only, never the caller:
		/// the caller's own state is already on its own snapshot. Host-side and
		/// unsynced by design (every bot runs on the host); an ally without an
		/// enabled provider contributes nothing, so a 1v1 yields a zeroed summary.
		/// </summary>
		public static TeamBlackboardSummary Collect(Player me)
		{
			if (me?.World == null)
				return new TeamBlackboardSummary();

			var broadcasts = new List<TeamBroadcast>();
			foreach (var p in me.World.Players.Where(p => p != me && p.IsBot && me.IsAlliedWith(p)))
			{
				var member = p.PlayerActor?.TraitsImplementing<IBotTeamMember>().FirstEnabledTraitOrDefault();
				if (member?.Broadcast != null)
					broadcasts.Add(member.Broadcast);
			}

			return Aggregate(broadcasts);
		}
	}
}
