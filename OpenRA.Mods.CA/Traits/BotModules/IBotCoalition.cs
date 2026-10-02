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
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TC-3 (AI_ARCHITECTURE.md §12.18): the team phase folded from the blackboard.
	/// </summary>
	public enum CoalitionPhase
	{
		/// <summary>No shared push is formed and nobody is in emergency.</summary>
		BuildUp,

		/// <summary>A shared MainTarget exists and the wave is synchronized (any member at Climax).</summary>
		Push,

		/// <summary>At least one member is at emergency urgency and asking for defence.</summary>
		Defend,
	}

	/// <summary>
	/// One elected rescue: which client index answers which defend request.
	/// </summary>
	public sealed class CoalitionRescueAssignment
	{
		/// <summary>ClientIndex of the broadcast that published the defend request.</summary>
		public readonly int RequesterClientIndex;

		/// <summary>Where the requester wants help (its own base centre while pressured).</summary>
		public readonly WPos DefendPosition;

		/// <summary>ClientIndex of the elected responder — the nearest free ally by ArmyCentroid.</summary>
		public readonly int ResponderClientIndex;

		public CoalitionRescueAssignment(int requesterClientIndex, WPos defendPosition, int responderClientIndex)
		{
			RequesterClientIndex = requesterClientIndex;
			DefendPosition = defendPosition;
			ResponderClientIndex = responderClientIndex;
		}
	}

	/// <summary>
	/// TC-3 (AI_ARCHITECTURE.md §12.18): the coalition general's output — a pure
	/// publication computed by every team member from the same broadcast set, so
	/// all members hold the identical directive without an electable command unit.
	/// Consumers read it as demand bias only; orders stay with existing owners.
	/// </summary>
	public sealed class CoalitionDirective
	{
		/// <summary>The enemy the team pushes, or null. Voted by broadcast count.</summary>
		public readonly Player MainTarget;

		/// <summary>Team phase for this snapshot.</summary>
		public readonly CoalitionPhase Phase;

		/// <summary>One elected responder per allied defend request (empty when none).</summary>
		public readonly List<CoalitionRescueAssignment> RescueAssignments;

		/// <summary>Allied spawn anchors in Voronoi sector order: ClientIndex -> anchor.</summary>
		public readonly Dictionary<int, WPos> SectorAnchors;

		public static readonly CoalitionDirective Empty = new(null, CoalitionPhase.BuildUp, new List<CoalitionRescueAssignment>(), new Dictionary<int, WPos>());

		public CoalitionDirective(Player mainTarget, CoalitionPhase phase, List<CoalitionRescueAssignment> rescueAssignments, Dictionary<int, WPos> sectorAnchors)
		{
			MainTarget = mainTarget;
			Phase = phase;
			RescueAssignments = rescueAssignments;
			SectorAnchors = sectorAnchors;
		}
	}

	/// <summary>
	/// The deterministic fold. Every member calls this with the same inputs
	/// (own broadcast + every allied broadcast) and computes the identical
	/// directive — the hivemind is the function, not a unit.
	/// </summary>
	public static class CoalitionFold
	{
		/// <summary>
		/// Fold own + allied broadcasts into the team directive. Deterministic:
		/// ordering is by broadcast content (votes, distance, ClientIndex), never
		/// by enumeration order. Null/empty input degrades to own-only behaviour —
		/// a solo bot's directive is its own broadcast.
		/// </summary>
		public static CoalitionDirective Compute(TeamBroadcast own, IEnumerable<TeamBroadcast> allies)
		{
			var all = new List<TeamBroadcast>();
			if (own != null)
				all.Add(own);
			if (allies != null)
				all.AddRange(allies.Where(b => b != null));

			if (all.Count == 0)
				return CoalitionDirective.Empty;

			// MainTarget: the target carried by the most broadcasts; ties break to the
			// highest DirectorTension publisher, then the lowest ClientIndex.
			var votes = all.Where(b => b.MainTarget != null)
				.GroupBy(b => b.MainTarget)
				.Select(g => (Target: g.Key, Count: g.Count(), MaxTension: g.Max(b => b.DirectorTension), MinIndex: g.Min(b => b.ClientIndex)))
				.OrderByDescending(v => v.Count)
				.ThenByDescending(v => v.MaxTension)
				.ThenBy(v => v.MinIndex)
				.ToList();
			var mainTarget = votes.Count > 0 ? votes[0].Target : null;

			// Rescue: one elected responder per defend request — the nearest free
			// ally by ArmyCentroid. A requester does not rescue (it is under attack);
			// a Zero centroid (old-version ally or no army) cannot be elected.
			var requesters = all.Where(b => b.RequestsDefence && b.DefendPosition != WPos.Zero)
				.OrderBy(b => b.ClientIndex)
				.ToList();
			var requesterIds = new HashSet<int>(requesters.Select(b => b.ClientIndex));
			var freePool = all.Where(b => !requesterIds.Contains(b.ClientIndex) && b.ArmyCentroid != WPos.Zero)
				.ToList();
			var rescue = new List<CoalitionRescueAssignment>();
			foreach (var req in requesters)
			{
				var responder = freePool
					.OrderBy(b => (b.ArmyCentroid - req.DefendPosition).LengthSquared)
					.ThenBy(b => b.ClientIndex)
					.FirstOrDefault();
				if (responder != null)
					rescue.Add(new CoalitionRescueAssignment(req.ClientIndex, req.DefendPosition, responder.ClientIndex));
			}

			// Phase: Defend overrides Push; Push needs a target and a synchronized wave.
			CoalitionPhase phase;
			if (rescue.Count > 0 || all.Any(b => b.UrgencyLevel >= 2 && b.RequestsDefence))
				phase = CoalitionPhase.Defend;
			else if (mainTarget != null && all.Any(b => b.DirectorPhase == DirectorPhase.Climax))
				phase = CoalitionPhase.Push;
			else
				phase = CoalitionPhase.BuildUp;

			// Sectors: the Voronoi anchors are the allied spawn positions — public map
			// data, published once via the broadcast field SpawnPoint.
			var anchors = all.Where(b => b.SpawnPoint != WPos.Zero)
				.GroupBy(b => b.ClientIndex)
				.ToDictionary(g => g.Key, g => g.First().SpawnPoint);

			return new CoalitionDirective(mainTarget, phase, rescue, anchors);
		}
	}

	/// <summary>
	/// TC-3 publish seam: the bot's master answers its latest folded directive.
	/// Consumers read it as demand bias; <see cref="CoalitionDirective.Empty"/>
	/// degrades every consumer to today's behaviour (1v1, no provider, flag off).
	/// </summary>
	public interface IBotCoalition
	{
		/// <summary>The bot's latest coalition directive; Empty when none or disabled.</summary>
		CoalitionDirective Coalition { get; }
	}

	/// <summary>
	/// TC-3 optional provider: the publisher's expansion assist — a claim that
	/// wants a bodyguard (a thin army contesting a field). Implemented by the
	/// expansion planner; absent = no assist requests.
	/// </summary>
	public interface IBotExpansionAssistProvider
	{
		/// <summary>The field the publisher wants escorted, or null.</summary>
		WPos? ExpansionAssistTarget { get; }
	}
}
