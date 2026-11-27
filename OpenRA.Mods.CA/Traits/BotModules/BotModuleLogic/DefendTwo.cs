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

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TEAM-DEFEND-02 (design pending/TEAM_DEFEND_02_DESIGN_01.md): the pure, engine-free rules of the two-tier ally defence's T2 (the team alarm). Integer math only.
	/// Total need = the presence the team wants at the victim (margin x the SEEN attack) minus the defence already there; each seat takes the share of what is still missing that its spare value earns among the answerers (a team ledger of
	/// published spare and committed values, no central allocator); a unit that already arrived is counted once (in the defence there), never again in the commit.
	/// </summary>
	public static class DefendTwo
	{
		public const int MinMarginPercent = 100;
		public const int MaxMarginPercent = 400;

		/// <summary>A ledger entry older than this many ticks (two send passes of the default cadence) is ignored.</summary>
		public const int LedgerMaxAgeTicks = 100;

		/// <summary>
		/// The value the team sends in total: <paramref name="marginPercent"/> percent of the seen attack, rounded up, MINUS the defence already there, never below 0 (so the total presence at the victim is the margin x the attackers).
		/// Not (attack - defence) x margin: that under-sends exactly when defence is present. No attack seen = 0 (a blind alarm uses <see cref="BlindNeed"/>).
		/// </summary>
		public static long Need(long attackValue, long defenceThere, int marginPercent)
		{
			if (attackValue <= 0)
				return 0;

			var margin = Math.Clamp(marginPercent, MinMarginPercent, MaxMarginPercent);
			var presence = ((decimal)attackValue * margin + 99) / 100;
			var total = (long)Math.Floor(presence);
			return Math.Max(0, total - Math.Max(0, defenceThere));
		}

		/// <summary>The need of a blind alarm (a base asset hit by an attacker nobody sees): the blind value minus the defence already there, never below 0.</summary>
		public static long BlindNeed(long blindValue, long defenceThere) => Math.Max(0, Math.Max(0, blindValue) - Math.Max(0, defenceThere));

		/// <summary>An alarm without an attacker identity: the damaged location is all it carries.</summary>
		public static bool IsBlind(uint attackerId) => attackerId == 0;

		/// <summary>A ledger entry is usable: published at or before <paramref name="now"/> and no older than <paramref name="maxAgeTicks"/>.</summary>
		public static bool Fresh(int now, int tick, int maxAgeTicks) => tick >= 0 && now >= tick && now - tick <= Math.Max(0, maxAgeTicks);

		/// <summary>
		/// The value one seat sends in this pass: min(its spare, ceil(remaining x its spare / the spare of all fresh answerers)) with remaining = need - what the whole team has already committed (never below 0).
		/// A seat with no spare, or no answerer value at all, sends nothing. The quota never exceeds the remaining need (own spare is part of the total), so seats passing one after the other, each publishing its commit right after its send, cannot
		/// together send more than the need plus the one atomic squad of the last step.
		/// </summary>
		public static long Quota(long need, long committedTotal, long ownSpare, long totalSpare)
		{
			if (ownSpare <= 0 || totalSpare <= 0)
				return 0;

			var remaining = Math.Max(0, need - Math.Max(0, committedTotal));
			if (remaining == 0)
				return 0;

			var total = Math.Max(totalSpare, ownSpare);
			var share = (long)Math.Ceiling((decimal)remaining * ownSpare / total);
			return Math.Min(ownSpare, share);
		}

		/// <summary>
		/// The value a seat has on ally defence that is still on its way: only units FARTHER than the attacker-scan radius from the victim count. A unit inside that radius has arrived and is already in the alarm's defence value; counting it again
		/// would subtract it twice.
		/// </summary>
		public static long CommitValue(IEnumerable<(long Value, long DistanceSquared)> units, long scanRadiusSquared)
		{
			long sum = 0;
			foreach (var (value, distanceSquared) in units)
				if (distanceSquared > scanRadiusSquared)
					sum += Math.Max(0, value);

			return sum;
		}

		/// <summary>
		/// Which alarm takes the board. A BLIND refresh never replaces a still-active SEEN alarm of the same victim (it would erase the seen attack value); in every other case the rule is TeamDefendCA.Replaces (a seen alarm replaces a blind one,
		/// a same-victim refresh takes the newest numbers, a larger attack takes the board).
		/// </summary>
		public static bool Replaces(TeamDefendCA.Alarm? current, TeamDefendCA.Alarm next, int now, int ttlTicks)
		{
			if (current is { } c && IsBlind(next.AttackerId) && !IsBlind(c.AttackerId) && c.VictimKey == next.VictimKey && TeamDefendCA.Active(now, c.Tick, ttlTicks))
				return false;

			return TeamDefendCA.Replaces(current, next, now, ttlTicks);
		}

		/// <summary>The DEV row (team_defend_two) of one send pass.</summary>
		public static string Detail(string victim, bool blind, long need, long committed, long spare, long totalSpare, long quota, int pool, int drafted, int squads, int blobs, bool seen) =>
			"victim=" + victim + ";blind=" + (blind ? 1 : 0) + ";seen=" + (seen ? 1 : 0) + ";need=" + need + ";committed=" + committed + ";spare=" + spare + ";total_spare=" + totalSpare
			+ ";quota=" + quota + ";pool=" + pool + ";drafted=" + drafted + ";squads=" + squads + ";blobs=" + blobs;
	}
}
