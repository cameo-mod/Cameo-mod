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
	/// The four learned estimates of one role in one match-up (USER 4-value rule, 2026-11-19): GLOBAL (every bot game), OWN FACTION (every
	/// game as that faction), ENEMY FACTION (every game against that faction), OBSERVED ENEMY STYLE (tactic/strategy inferred from what the
	/// bot sees, never read from the enemy). Each is a full-scale estimate; the observed-style value is 0 until that style was seen.
	/// </summary>
	public readonly record struct MixRoleValues(double Global, double OwnFaction, double EnemyFaction, double ObservedStyle);

	/// <summary>
	/// ARMY-MIX-01 learning hook: a provider (the DEV learner; nothing ships in a release build) that knows how much a role is worth in the
	/// current match-up. No provider, or a role it does not know, is neutral. It only ORDERS the roles the wave is missing; it never decides
	/// whether anything is built.
	/// </summary>
	public interface IBotMixLearning
	{
		bool TryRoleValues(string role, out MixRoleValues values);
	}

	/// <summary>The wait clock of a ready wave: when it started (-1 = not waiting) and whether the bounded wait already ran out for this wave.</summary>
	public readonly record struct WaveMixClock(int Since, bool Released)
	{
		public static readonly WaveMixClock Idle = new(-1, false);
	}

	/// <summary>ARMY-MIX-01 (USER P0, 2026-11-23): the World-free half. A wave mixes the available roles: at least min(3, available) distinct roles.</summary>
	public static class WaveMixCA
	{
		/// <summary>The roles an assault wave is mixed from, in the order used when nothing is learned: the tank mass first, then the counters.</summary>
		public static readonly string[] WaveRoles =
		[
			BotUnitRole.Frontline, BotUnitRole.AntiArmour, BotUnitRole.AntiInfantry, BotUnitRole.AntiAir, BotUnitRole.Artillery, BotUnitRole.Skirmisher
		];

		static readonly HashSet<string> WaveRoleSet = new(WaveRoles, StringComparer.Ordinal);

		/// <summary>The distinct wave roles among the given primary roles (null = unclassified, other taxonomy roles are ignored).</summary>
		public static HashSet<string> DistinctRoles(IEnumerable<string> primaryRoles)
		{
			var set = new HashSet<string>(StringComparer.Ordinal);
			foreach (var role in primaryRoles)
				if (role != null && WaveRoleSet.Contains(role))
					set.Add(role);

			return set;
		}

		/// <summary>The role count a wave needs: the wanted minimum, or every role there is when fewer are available. Never negative.</summary>
		public static int Needed(int minRoles, int availableRoles) => Math.Max(0, Math.Min(minRoles, availableRoles));

		/// <summary>
		/// The bounded wait for a missing role. Satisfied: no hold and the clock resets. Otherwise the first call starts the clock and
		/// every call holds until <paramref name="waitTicks"/> have passed; then the wave is RELEASED and stays released (no hold, the
		/// clock is not re-armed) until the role arrives or the wave launches (the caller then stores <see cref="WaveMixClock.Idle"/>). A
		/// wait that never ends would stall the bot behind a role it cannot get, and a clock that re-armed itself after the timeout
		/// would hold the wave again whenever a later launch gate returned first.
		/// </summary>
		public static (bool Hold, WaveMixClock Next) Step(int now, WaveMixClock clock, int waitTicks, bool satisfied)
		{
			if (satisfied || waitTicks <= 0)
				return (false, WaveMixClock.Idle);

			if (clock.Released)
				return (false, clock);

			if (clock.Since < 0)
				return (true, new WaveMixClock(now, false));

			return now - clock.Since < waitTicks ? (true, clock) : (false, new WaveMixClock(-1, true));
		}

		/// <summary>
		/// The available roles the pool lacks, best first: higher weight first (a learned value, 1 when nothing is known), then the fixed
		/// order of <see cref="WaveRoles"/>.
		/// </summary>
		public static List<string> MissingRoles(IEnumerable<string> available, ICollection<string> pool, Func<string, double> weight)
		{
			return available
				.Where(role => !pool.Contains(role))
				.Distinct(StringComparer.Ordinal)
				.OrderByDescending(role => weight(role))
				.ThenBy(role => Array.IndexOf(WaveRoles, role))
				.ThenBy(role => role, StringComparer.Ordinal)
				.ToList();
		}

		/// <summary>
		/// The first missing role not yet covered by a pick of this producer: one pick covers a role until <c>coveredUntil</c> (a unit
		/// takes time to build), so several queues in one cycle do not all build the same missing role.
		/// </summary>
		public static string NextMissing(IEnumerable<string> orderedMissing, Func<string, int> coveredUntil, int now)
		{
			foreach (var role in orderedMissing)
				if (coveredUntil(role) <= now)
					return role;

			return null;
		}

		/// <summary>
		/// The match-up value: the geometric mean (G x F_own x E x S)^(1/4) (USER 2026-11-19). A new observed-style value (not above 0)
		/// starts at the three-value result (G x F_own x E)^(1/3), so an unseen style changes nothing. A missing GLOBAL, OWN or ENEMY
		/// value (not above 0) is dropped from the mean instead of zeroing it; no value at all = 1 (neutral).
		/// </summary>
		public static double Matchup(MixRoleValues v)
		{
			var known = new List<double>(4);
			foreach (var x in new[] { v.Global, v.OwnFaction, v.EnemyFaction })
				if (x > 0)
					known.Add(x);

			if (known.Count == 0)
				return 1.0;

			var three = Math.Pow(known.Aggregate(1.0, (a, b) => a * b), 1.0 / known.Count);
			var style = v.ObservedStyle > 0 ? v.ObservedStyle : three;
			return Math.Pow(known.Aggregate(style, (a, b) => a * b), 1.0 / (known.Count + 1));
		}
	}
}
