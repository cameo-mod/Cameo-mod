#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P7 (b), row sq_defend_one (design 01 contradiction 2: two ally-defend copies on the same event; Opus review change 4: ONE implementation, keep the 25-cell ally radius, all allies respond,
	/// the ready-turret placement hook stays a named input, the DEV row records which old copy supplied which part). Pure and engine-free.
	/// The two copies: DEFEND-ALLY (DefendAllyRespond, copy 1: a local hit near the responders; pool units inside the radius, the DEFEND-ALLY draft limit, one response cooldown) and TEAM-DEFEND (TeamDefendTick, copy 2:
	/// the team alarm; every teammate but the victim's bot; the whole pool or the Q4b share). The merged rule keeps the radius and the draft limit of copy 1 for the alarm too, the own-base guard, the home guard and the
	/// squad redirect of copy 2, and one cooldown for both, so a single attack is answered by one draft.
	/// </summary>
	public static class DefendOne
	{
		public const string SourceAlarm = "alarm";
		public const string CopyDefendAlly = "DefendAlly";
		public const string CopyTeamDefend = "TeamDefend";

		/// <summary>A pool unit as the pick sees it: its place in the caller's list, its actor id (the stable tie-break) and its squared distance to the attacked friendly.</summary>
		public readonly record struct Candidate(int Index, uint ActorId, long DistanceSquared);

		/// <summary>The candidates inside the ally-defence radius (inclusive, as DefendAllyCA.InRadius), nearest to the attacked friendly first, equal distance by lower actor id: the caller's list indices.</summary>
		public static List<int> InRadiusNearestFirst(IReadOnlyList<Candidate> candidates, long radiusSquared) =>
			candidates.Where(c => c.DistanceSquared <= radiusSquared)
				.OrderBy(c => c.DistanceSquared).ThenBy(c => c.ActorId)
				.Select(c => c.Index)
				.ToList();

		/// <summary>One draft per attack: the local scan and the team alarm share one response cooldown, so the alarm drafts only when no response is still cooling down.</summary>
		public static bool MayDraft(int now, int nextResponseTick) => now >= nextResponseTick;

		/// <summary>The DEV row (sq_defend_one): which old copy supplied which part of the merged rule, and what it did.</summary>
		public static string Detail(string source, int pool, int inRadius, int drafted, bool cooldownSkip) =>
			"src=" + source + ";radius=" + CopyDefendAlly + ";limit=" + CopyDefendAlly + ";order=nearest_victim;squads=" + CopyTeamDefend + ";guard=" + CopyTeamDefend
			+ ";pool=" + pool + ";in_radius=" + inRadius + ";far_kept=" + (pool - inRadius) + ";drafted=" + drafted + ";cooldown_skip=" + (cooldownSkip ? 1 : 0);
	}
}
