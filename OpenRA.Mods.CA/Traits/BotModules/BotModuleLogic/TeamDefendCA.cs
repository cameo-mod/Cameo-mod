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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// TEAM-DEFEND, unit side (V07, USER 2026-11-24: "when an ally is attacked every teammate sends idle, home and defence units"). Pure rules, no world access; the
	/// wiring is SquadManagerBotModuleCA.V07Defend.cs. Behind the switch TeamLeadDefendTeamEnabled (DEV row tl_defend_team): the bot that sees a serious attack on an own
	/// or allied actor raises ONE alarm on the team board; every other teammate answers it with its pool units, its idle squads and its idle home-guard squad; a blob that
	/// has already pushed keeps its order unless the ally would die (the attack is worth more than its defence). Observation only: the attackers are what the bot sees.
	/// </summary>
	public static class TeamDefendCA
	{
		/// <summary>The team alarm: when, whose actor was attacked and where, the attacker to answer, and what each side is worth around the victim.</summary>
		public readonly record struct Alarm(int Tick, string VictimKey, uint VictimId, WPos VictimPos, uint AttackerId, WPos AttackerPos, long AttackValue, long DefenceValue);

		/// <summary>The attack is worth an alarm: the armed enemies around the victim cost at least <paramref name="minAttackValue"/> (a lone scout is not an alarm).</summary>
		public static bool Serious(long attackValue, int minAttackValue) => attackValue > 0 && attackValue >= Math.Max(1, minAttackValue);

		/// <summary>The ally would die: the attack is worth more than <paramref name="diePercent"/> percent (25-400) of its defence.</summary>
		public static bool WouldDie(long attackValue, long defenceValue, int diePercent) => attackValue * 100 > defenceValue * Math.Clamp(diePercent, 25, 400);

		/// <summary>The alarm is still in force: raised at or before <paramref name="now"/>, no older than <paramref name="ttlTicks"/>.</summary>
		public static bool Active(int now, int alarmTick, int ttlTicks) => now >= alarmTick && now - alarmTick <= Math.Max(1, ttlTicks);

		/// <summary>
		/// A new alarm takes the board when there is none in force, when it is about the same seat (a refresh with the newest numbers), or when its attack is worth more than
		/// the one in force. Otherwise the alarm in force stays.
		/// </summary>
		public static bool Replaces(Alarm? current, Alarm next, int now, int ttlTicks) =>
			current == null || !Active(now, current.Value.Tick, ttlTicks) || current.Value.VictimKey == next.VictimKey || next.AttackValue > current.Value.AttackValue;

		/// <summary>
		/// A squad answers the alarm: a free one (idle, staging or gathering without a hold on it) always; a blob that already pushed only when the ally would die.
		/// Everything else keeps its job (a squad that is marching or fighting is not recalled).
		/// </summary>
		public static bool SquadResponds(bool free, bool pushedBlob, bool wouldDie) => free || (pushedBlob && wouldDie);
	}
}
