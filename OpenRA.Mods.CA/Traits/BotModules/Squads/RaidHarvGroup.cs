#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>The four answers of the raid launch decision.</summary>
	public enum RaidLaunch
	{
		/// <summary>No active ally: the seat raids on its own package.</summary>
		Solo,

		/// <summary>A valid team-leader joint assignment exists and the group is route-ready: the seat's squads take the shared target.</summary>
		Join,

		/// <summary>No live shared target and the routed group reaches the requirement: the seat proposes the target to the team leader (the leader alone approves).</summary>
		Propose,

		/// <summary>Nothing launches.</summary>
		Hold,
	}

	/// <summary>One seat's route-ready offer for ONE target: published only after the seat's own explored-only route search to that target succeeded and its waypoint risk gate passed with the offered value.</summary>
	public readonly record struct RaidContribution(int SeatKey, uint TargetId, int Value, int Units, int EtaTicks, int Tick);

	/// <summary>
	/// A seat's proposal of a raid target to the team leader (delta 02 section 3). It carries the target's id as a name to compare and the PROPOSER's last observation (cell and tick) as a hint, never a reference the leader dereferences: the
	/// leader approves by comparing the routed value for that id with the requirement, and every member resolves the actor through its own visibility check before it reads anything of it.
	/// </summary>
	public readonly record struct RaidProposal(int ProposerSeat, uint TargetId, int Req, CPos LastSeenCell, int LastSeenTick, int Tick);

	/// <summary>
	/// RAID-HARV-01 delta 02 section 3 (design 02 R3): the fail-closed allied branch and the routed, committed group value. With an active ally the ONLY launches are a valid team-leader joint assignment (Join) or a proposal the leader may
	/// approve (Propose); Solo exists only without active allies. The sufficiency measure is the ROUTED value of the fresh contributions for THIS target, not the fresh total raid value of the seats.
	/// </summary>
	public static class RaidHarvGroup
	{
		/// <summary>
		/// An active ally of this seat: another seat, an ally, a combatant (not a spectator) and not defeated. The same test the team-leader rally code applies to its members
		/// (<c>p.NonCombatant || p.WinState != WinState.Undefined || (p != Player &amp;&amp; relationship != Ally)</c> negated); a source pin keeps the two equal. A live human ally counts.
		/// </summary>
		public static bool ActiveAlly(bool isSelf, bool isAlly, bool nonCombatant, bool defeated) => !isSelf && isAlly && !nonCombatant && !defeated;

		/// <summary>
		/// The routed value for one target: the sum of the FRESH contributions for that target id, at most one per seat (the latest wins), each with at least <paramref name="minUnits"/> ACTORS (the join minimum is a count, never a value)
		/// and a positive value. A stale contribution (older than <paramref name="maxAgeTicks"/>, or from the future) is not counted.
		/// </summary>
		public static long RoutedValue(IReadOnlyList<RaidContribution> contributions, uint targetId, int now, int maxAgeTicks, int minUnits)
		{
			if (contributions == null)
				return 0;

			var latest = new Dictionary<int, RaidContribution>();
			foreach (var c in contributions)
			{
				if (c.TargetId != targetId || c.Value <= 0 || c.Units < minUnits || c.Units <= 0 || c.Tick > now || now - c.Tick > maxAgeTicks)
					continue;

				if (!latest.TryGetValue(c.SeatKey, out var held) || c.Tick >= held.Tick)
					latest[c.SeatKey] = c;
			}

			long sum = 0;
			foreach (var c in latest.Values)
				sum += c.Value;

			return sum;
		}

		/// <summary>
		/// The launch decision. <paramref name="routedValue"/> is <see cref="RoutedValue"/> for the target; <paramref name="req"/> the requirement of <see cref="RaidHarvPackage.Required"/>; <paramref name="ownContributionReady"/> says this seat has
		/// its own route-ready contribution. Invariant: with <paramref name="hasActiveAllies"/> the answer is never <see cref="RaidLaunch.Solo"/>; the joint row off with allies = <see cref="RaidLaunch.Hold"/> (fail closed).
		/// </summary>
		public static RaidLaunch Decide(bool hasActiveAllies, bool jointOn, bool assignmentApproved, long routedValue, int req, bool ownContributionReady)
		{
			if (req <= 0 || !ownContributionReady || routedValue < req)
				return RaidLaunch.Hold;

			if (!hasActiveAllies)
				return RaidLaunch.Solo;

			if (!jointOn)
				return RaidLaunch.Hold;

			return assignmentApproved ? RaidLaunch.Join : RaidLaunch.Propose;
		}

		/// <summary>A proposal is fresh when it exists, is not from the future and is at most <paramref name="maxAgeTicks"/> old.</summary>
		public static bool ProposalFresh(RaidProposal? proposal, int now, int maxAgeTicks) => proposal is { } p && p.Tick <= now && now - p.Tick <= maxAgeTicks;

		/// <summary>
		/// The leader's re-approval of a live raid (Opus ruling 3 of P2b): before the joint assignment lapses (TeamLeadHarassTicks), the leader writes its tick again while it is the leader, the assignment is the very target it approved, that
		/// assignment is still live, and the fresh contributions for the target (the running raids' heartbeats and the route-ready offers) still reach the requirement it approved. It compares ids and values only; a lapse without a refresh
		/// releases the raids (Seq bump, Stop, normal state).
		/// </summary>
		public static bool LeaderRefreshes(bool isLeader, uint approvedTarget, uint assignedTarget, bool assignmentLive, long routedValue, int approvedReq) =>
			isLeader && approvedTarget != 0 && approvedTarget == assignedTarget && assignmentLive && approvedReq > 0 && routedValue >= approvedReq;

		/// <summary>The team leader's approval of a proposal (Propose to Join): a fresh proposal whose routed value reaches the requirement. Only the leader writes the assignment; a member never does.</summary>
		public static bool LeaderApproves(bool isLeader, bool proposalFresh, long routedValue, int req) => isLeader && proposalFresh && req > 0 && routedValue >= req;
	}
}
