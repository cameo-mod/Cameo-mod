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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// ALLIN-TRIGGER-01 (USER 2026-11-25, designer-dynamic-all-in): the team all-in is DYNAMIC, it fires when the team is ahead. The nine ahead triggers, in the order they are tried
	/// (the cap, idle army, nearly-won, stalemate and late-tick inputs are the existing ones of AllInCA and stay as they are).
	/// </summary>
	public enum AheadTrigger : byte
	{
		/// <summary>The enemy's last known construction yard is dead and no MCV is remembered (it cannot rebuild).</summary>
		Yard,

		/// <summary>No enemy production building is known any more although some was seen.</summary>
		Production,

		/// <summary>The enemy economy is gone: the refineries seen are dead and no harvester is known.</summary>
		Economy,

		/// <summary>An enemy player is defeated and the team has the numbers; fires again for every further defeat.</summary>
		Defeat,

		/// <summary>The team destroyed at least twice the value it lost (the observable proxy of "twice the points").</summary>
		Trade,

		/// <summary>A big battle was just won: a large enemy value destroyed in a short window while the team army stands.</summary>
		Battle,

		/// <summary>The team army is at least 1.5 times the enemy army the bot sees or remembers plus its defences.</summary>
		Army,

		/// <summary>The enemy is busy at an ally (the ally asks for defence, the home is quiet) and its own base is thinly defended: counter all-in.</summary>
		Counter,

		/// <summary>An own superweapon is ready: the all-in goes in with the strike.</summary>
		Super,
	}

	/// <summary>
	/// Pure rules of the ahead triggers (no world access; the wiring is SquadManagerBotModuleCA.AllInAhead.cs). Observation only: every input is what the bot saw, remembered, or its own
	/// side's counters; never an enemy score or an enemy's internal state. A blind bot (nothing known of the enemy) fires none of the army, defence or structure rules.
	/// </summary>
	public static class AllInAhead
	{
		/// <summary>A ratio of a gain against no loss at all (the percent value the DEV rows show for it).</summary>
		public const int RatioCap = 100000;

		/// <summary>The all-in reason of each trigger, index = <see cref="AheadTrigger"/>. Every name starts with <see cref="WhyPrefix"/>.</summary>
		public static readonly string[] Names =
		{
			"ahead_yard", "ahead_production", "ahead_economy", "ahead_defeat", "ahead_trade", "ahead_battle", "ahead_army", "ahead_counter", "ahead_super"
		};

		public const string WhyPrefix = "ahead_";

		public static string Why(AheadTrigger trigger) => Names[(int)trigger];

		/// <summary>The all-in reason of an order was set by one of the ahead triggers (the only ones the de-escalation may end).</summary>
		public static bool IsAheadWhy(string why) => why != null && why.StartsWith(WhyPrefix, StringComparison.Ordinal);

		/// <summary>gain / loss in percent (a gain against no loss = <see cref="RatioCap"/>, no gain = 0; never above the cap).</summary>
		public static long RatioPct(long gain, long loss) =>
			loss <= 0 ? (gain > 0 ? RatioCap : 0) : Math.Min(RatioCap, gain * 100 / loss);

		/// <summary>1: the team destroyed at least <paramref name="minKills"/> and <paramref name="pct"/> percent of what it lost (a percent of 0 or less = off).</summary>
		public static bool Trade(long kills, long losses, long minKills, int pct) =>
			pct > 0 && kills > 0 && kills >= minKills && kills * 100 >= Math.Max(1, losses) * pct;

		/// <summary>
		/// 2: more enemy players are defeated (scoreboard) than the all-in already answered, and the team has the numbers: more living seats than the enemy side, or an army of at least
		/// <paramref name="armyPct"/> percent of the enemy army it knows.
		/// </summary>
		public static bool Defeat(int defeated, int handled, int teamSeats, int enemySeats, long ownArmy, long enemyArmy, int armyPct) =>
			defeated > handled && (teamSeats > enemySeats || (enemyArmy > 0 && ownArmy * 100 >= enemyArmy * Math.Max(1, armyPct)));

		/// <summary>3: the team army is at least <paramref name="pct"/> percent of the enemy army plus defences the bot knows (nothing known = no).</summary>
		public static bool Army(long ownArmy, long enemyArmy, long enemyDefence, int pct) =>
			pct > 0 && ownArmy > 0 && enemyArmy + enemyDefence > 0 && ownArmy * 100 >= (enemyArmy + enemyDefence) * pct;

		/// <summary>
		/// 4-6: <paramref name="crippled"/> of the <paramref name="living"/> enemy seats lost what the trigger names (yard and MCV, production, economy), at least
		/// <paramref name="pct"/> percent of them (one of one, one of two at 50).
		/// </summary>
		public static bool Majority(int crippled, int living, int pct) => crippled > 0 && living > 0 && crippled * 100 >= living * Math.Max(1, pct);

		/// <summary>7: inside the window the team destroyed at least <paramref name="minKills"/> and <paramref name="pct"/> percent of what it lost, and its army still stands (is at least what the window cost).</summary>
		public static bool Battle(long windowKills, long windowLosses, long minKills, int pct, long ownArmy) =>
			pct > 0 && windowKills > 0 && windowKills >= minKills && windowKills * 100 >= Math.Max(1, windowLosses) * pct && ownArmy > 0 && ownArmy >= windowLosses;

		/// <summary>8: an ally asks for defence, nothing threatens the own home, the enemy base is known, and the team army is at least <paramref name="pct"/> percent of the defences the bot knows there.</summary>
		public static bool Counter(bool allyAttacked, bool homeThreat, long ownArmy, long enemyDefence, int knownBuildings, int pct) =>
			allyAttacked && !homeThreat && knownBuildings > 0 && pct > 0 && ownArmy > 0 && ownArmy * 100 >= enemyDefence * pct;

		/// <summary>The team army is below <paramref name="pct"/> percent of the enemy army it knows (the enemy army unknown = not behind).</summary>
		public static bool Behind(long ownArmy, long enemyArmy, int pct) => enemyArmy > 0 && ownArmy * 100 < enemyArmy * Math.Max(1, pct);

		/// <summary>The exchange of the window goes against the team: it lost at least <paramref name="minLoss"/> and killed less than <paramref name="pct"/> percent of that.</summary>
		public static bool TradeBehind(long windowKills, long windowLosses, long minLoss, int pct) =>
			windowLosses > 0 && windowLosses >= minLoss && windowKills * 100 < windowLosses * Math.Max(1, pct);
	}

	/// <summary>
	/// What the bot saw of each enemy seat the highest it ever was (yards, production buildings, refineries): "dead" is only said of something that was seen alive and is gone now. Observed means
	/// the enemy sightings of the bot (BotFogMemory: seen or remembered, forgotten when the place is seen empty); an enemy that never showed a yard is not "yardless", it is unknown.
	/// </summary>
	public sealed class AheadLedger
	{
		readonly Dictionary<int, (int Yards, int Production, int Refineries)> high = new();

		public void Note(int seat, int yards, int production, int refineries)
		{
			high.TryGetValue(seat, out var h);
			high[seat] = (Math.Max(h.Yards, yards), Math.Max(h.Production, production), Math.Max(h.Refineries, refineries));
		}

		/// <summary>A yard was seen, none is known now, and no MCV is remembered: the seat cannot rebuild.</summary>
		public bool YardDead(int seat, int yards, int mcvs) => high.TryGetValue(seat, out var h) && h.Yards > 0 && yards == 0 && mcvs == 0;

		/// <summary>A production building was seen and none is known now.</summary>
		public bool ProductionDead(int seat, int production) => high.TryGetValue(seat, out var h) && h.Production > 0 && production == 0;

		/// <summary>A refinery was seen and neither a refinery nor a harvester is known now.</summary>
		public bool EconomyDead(int seat, int refineries, int harvesters) => high.TryGetValue(seat, out var h) && h.Refineries > 0 && refineries == 0 && harvesters == 0;
	}

	/// <summary>The team's cumulative kill and loss value, sampled every scan, so a window (the last N ticks) can be read: kills and losses gained inside it.</summary>
	public sealed class AheadWindow
	{
		readonly List<(int Tick, long Kills, long Losses)> samples = new();

		/// <summary>Add the totals of <paramref name="now"/>; samples older than <paramref name="keepTicks"/> go, except the newest of them (the baseline of the longest window).</summary>
		public void Add(int now, long kills, long losses, int keepTicks)
		{
			if (samples.Count > 0 && samples[samples.Count - 1].Tick == now)
				samples[samples.Count - 1] = (now, kills, losses);
			else
				samples.Add((now, kills, losses));

			while (samples.Count > 2 && samples[1].Tick <= now - keepTicks)
				samples.RemoveAt(0);
		}

		/// <summary>Kills and losses gained over the last <paramref name="windowTicks"/> (before the history spans the window: since the first sample). Never negative.</summary>
		public (long Kills, long Losses) Delta(int now, int windowTicks)
		{
			if (samples.Count == 0)
				return (0, 0);

			var last = samples[samples.Count - 1];
			var baseline = samples[0];
			for (var i = samples.Count - 1; i >= 0; i--)
				if (samples[i].Tick <= now - windowTicks)
				{
					baseline = samples[i];
					break;
				}

			return (Math.Max(0, last.Kills - baseline.Kills), Math.Max(0, last.Losses - baseline.Losses));
		}
	}
}
