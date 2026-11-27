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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P7 (b), row sq_defend_one (design 01 contradiction 2, Opus review change 4). With the row on the team alarm answers like DEFEND-ALLY: it drafts the pool units inside the ally-defence radius of the attacked
	// friendly (nearest first), as many as the DEFEND-ALLY draft limit allows, and only when no response is cooling down (the local scan and the alarm share defendAllyNextResponse), so one attack is answered by one draft.
	// Everything else of TeamDefendTick stays (the victim's own bot does not answer, the own-base guard, the home guard, the squad redirect with the arbiter priority and the would-die rule). The ready-turret placement reads
	// the team's alarm: this row never writes or clears it (it only reads the alarm it is handed), so that hook keeps its input. Row off = TeamDefendTick drafts as before (the whole pool, or the Q4b share under the arbiter).
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateDefendOne;

		/// <summary>sq_defend_one: both ally-defend copies are on (the row merges them), the row is on and not ablated.</summary>
		bool DefendOneOn => DefendAllyActive && Info.DefendOneEnabled && !ablateDefendOne.Off(Player, AblationIds.SqDefendOne);

		/// <summary>The pool units the team alarm drafts under the row: inside the ally-defence radius of the victim, nearest first, up to the DEFEND-ALLY draft limit, none while a response cools down.</summary>
		List<UnitWposWrapper> DefendOnePick(List<UnitWposWrapper> draftable, TeamDefendCA.Alarm alarm, int now)
		{
			var radius = WDist.FromCells(Info.DefendAllyRadiusCells).Length;
			var candidates = new List<DefendOne.Candidate>(draftable.Count);
			for (var i = 0; i < draftable.Count; i++)
				candidates.Add(new DefendOne.Candidate(i, draftable[i].Actor.ActorID, (draftable[i].Actor.CenterPosition - alarm.VictimPos).HorizontalLengthSquared));

			var inside = DefendOne.InRadiusNearestFirst(candidates, (long)radius * radius);
			var cooling = !DefendOne.MayDraft(now, defendAllyNextResponse);
			var limit = cooling ? 0 : DefendDraftLimit(inside.Count, false, Info, utilityAxesProviders);
			var picked = inside.Take(limit).Select(i => draftable[i]).ToList();
			if (picked.Count > 0)
				defendAllyNextResponse = now + Math.Max(1, Info.DefendAllyCooldownTicks);

			if (DiagnosticWriter != null)
				DiagnosticTeamEvent("sq_defend_one", DefendOne.Detail(DefendOne.SourceAlarm, draftable.Count, inside.Count, picked.Count, cooling && inside.Count > 0));

			return picked;
		}
	}
}
