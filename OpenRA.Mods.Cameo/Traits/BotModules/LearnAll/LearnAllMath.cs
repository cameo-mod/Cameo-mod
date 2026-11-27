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
using System.Collections.Generic;
using System.Numerics;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// LEARN-ALL-02 P1a: the integer half of the learning contract, shared by the release reader and the DEV learner. A learned estimate is a positive
	/// full-scale value in PERMILLE (1000 = 1.0). The decision value is the geometric mean (G x F_own x E_eff x S_obs)^(1/4) of the four axes
	/// (USER 4-value rule, 2026-11-19), taken in exact integer arithmetic (BigInteger, floor root, round to nearest) so it is identical on every
	/// machine and safe for the simulation. An unseen style (S_obs = 0) resolves to the exact three-axis result (G x F_own x E_eff)^(1/3).
	/// </summary>
	public static class LearnAllMath
	{
		public const int One = 1000;

		/// <summary>The store domain of one estimate. A store clamp only keeps the number sane; it is no gameplay multiplier (the consumer clamps once, per knob).</summary>
		public const int StoreMin = 50;
		public const int StoreMax = 20000;

		public static int ClampStore(int permille) => Math.Clamp(permille, StoreMin, StoreMax);

		/// <summary>Geometric mean of positive permille values: round(n-th root of the product). An empty list is neutral (1000). Values are clamped to the store domain first.</summary>
		public static int GeoMean(ReadOnlySpan<int> values)
		{
			if (values.Length == 0)
				return One;

			if (values.Length == 1)
				return ClampStore(values[0]);

			var product = BigInteger.One;
			foreach (var v in values)
				product *= ClampStore(v);

			return RootRounded(product, values.Length);
		}

		public static int GeoMean(int a, int b) => GeoMean([a, b]);
		public static int GeoMean(int a, int b, int c) => GeoMean([a, b, c]);
		public static int GeoMean(int a, int b, int c, int d) => GeoMean([a, b, c, d]);

		/// <summary>The decision value of one knob in one match-up. S_obs &lt;= 0 means "no observation-based style bucket qualified": the exact three-axis result.</summary>
		public static int Resolve(int global, int ownFaction, int enemyEff, int observedStyle) =>
			observedStyle <= 0 ? GeoMean(global, ownFaction, enemyEff) : GeoMean(global, ownFaction, enemyEff, observedStyle);

		/// <summary>The first value of a style bucket: the prior three-axis result, so a freshly seen style preserves the decision value until it learns.</summary>
		public static int SeedStyle(int global, int ownFaction, int enemyEff) => GeoMean(global, ownFaction, enemyEff);

		/// <summary>
		/// TEAM GAMES (USER 2026-11-14): the enemy value is the geometric mean of (the geometric mean over ALL enemy players' faction values) and (the current
		/// main-target enemy faction value). One enemy: both parts are that enemy's value, so the result is exactly it.
		/// </summary>
		public static int TeamEnemy(ReadOnlySpan<int> perEnemyPlayerFactionValues, int mainTargetValue)
		{
			if (perEnemyPlayerFactionValues.Length == 0)
				return ClampStore(mainTargetValue);

			return GeoMean(GeoMean(perEnemyPlayerFactionValues), mainTargetValue);
		}

		/// <summary>round(P^(1/n)) for a positive integer P: the floor root by bisection, then rounded to the nearest integer root (half rounds up).</summary>
		static int RootRounded(BigInteger product, int n)
		{
			// The root of a product of values in [StoreMin, StoreMax] lies in the same range.
			int lo = StoreMin, hi = StoreMax;
			while (lo < hi)
			{
				var mid = lo + ((hi - lo + 1) >> 1);
				if (BigInteger.Pow(mid, n) <= product)
					lo = mid;
				else
					hi = mid - 1;
			}

			if (lo >= StoreMax)
				return StoreMax;

			// Round to the nearest integer root: P^(1/n) >= lo + 1/2  <=>  2^n * P >= (2*lo + 1)^n (a root exactly on the half rounds up).
			return (product << n) >= BigInteger.Pow(2 * lo + 1, n) ? lo + 1 : lo;
		}
	}

	/// <summary>
	/// The four axes of one knob in one personality's table: G (every game), F_own by own faction, E by enemy faction, S by observed style. Absent keys are
	/// not stored: F_own and E read as G, an absent style reads as "unseen" (three-axis result).
	/// </summary>
	public sealed class LearnKnobAxes
	{
		public int Global = LearnAllMath.One;
		public readonly SortedDictionary<string, int> OwnFaction = new(StringComparer.Ordinal);
		public readonly SortedDictionary<string, int> EnemyFaction = new(StringComparer.Ordinal);
		public readonly SortedDictionary<string, int> Style = new(StringComparer.Ordinal);

		public int Own(string faction) => faction != null && OwnFaction.TryGetValue(faction, out var v) ? v : Global;
		public int Enemy(string faction) => faction != null && EnemyFaction.TryGetValue(faction, out var v) ? v : Global;

		/// <summary>
		/// The enemy value E_eff of one match-up, the ONE owner for the runtime reader and the DEV trainer (Opus 2026-11-27, no pull toward global): one entry per enemy
		/// PLAYER and the main target. With a main target it is TeamEnemy(per player, main); with NO main target (null, empty, or a Random pick, whose public faction is "")
		/// it is the all-enemy aggregate alone; with no enemies either it is the global value. A real faction the table has not learned yet reads as the global value (new values start at G).
		/// </summary>
		public int EnemyEff(IReadOnlyList<string> enemies, string mainTarget)
		{
			var hasMain = !string.IsNullOrEmpty(mainTarget);
			if (enemies == null || enemies.Count == 0)
				return hasMain ? Enemy(mainTarget) : Global;

			var per = new int[enemies.Count];
			for (var i = 0; i < per.Length; i++)
				per[i] = Enemy(enemies[i]);

			return hasMain ? LearnAllMath.TeamEnemy(per, Enemy(mainTarget)) : LearnAllMath.GeoMean(per);
		}

		public LearnKnobAxes Clone()
		{
			var c = new LearnKnobAxes { Global = Global };
			foreach (var kv in OwnFaction)
				c.OwnFaction[kv.Key] = kv.Value;
			foreach (var kv in EnemyFaction)
				c.EnemyFaction[kv.Key] = kv.Value;
			foreach (var kv in Style)
				c.Style[kv.Key] = kv.Value;
			return c;
		}
	}

	/// <summary>
	/// LEARN-ALL-02 P1a: the learned table of every own personality (USER 2026-11-19: each own personality keeps its own table under the same rule).
	/// The "any" table is the global one; a personality seen for the first time starts as a copy of it (new values start at the global value). Nothing
	/// ever resets: a balance change, a missing signal or a rebuild keeps every estimate, and updates only move values by bounded log steps.
	/// The release only READS a table (<see cref="Resolve"/>); the update half lives in OpenRA.Mods.Cameo.Dev (DEV build only) and its python mirror, so no training code is compiled into a release build.
	/// </summary>
	public sealed class LearnTable
	{
		public const string AnyPersonality = "any";

		readonly SortedDictionary<string, SortedDictionary<string, LearnKnobAxes>> personalities = new(StringComparer.Ordinal);
		readonly SortedSet<string> appliedGames = new(StringComparer.Ordinal);

		public IReadOnlyDictionary<string, SortedDictionary<string, LearnKnobAxes>> Personalities => personalities;
		public IReadOnlyCollection<string> AppliedGames => appliedGames;

		public bool HasPersonality(string personality) => personalities.ContainsKey(personality ?? AnyPersonality);

		/// <summary>The axes of a knob for a personality; the global ("any") table when that personality has none yet; null when nothing is learned (neutral).</summary>
		public LearnKnobAxes Find(string personality, string knob)
		{
			if (knob == null)
				return null;

			if (personality != null && personalities.TryGetValue(personality, out var own) && own.TryGetValue(knob, out var axes))
				return axes;

			return personalities.TryGetValue(AnyPersonality, out var any) && any.TryGetValue(knob, out var g) ? g : null;
		}

		/// <summary>
		/// The decision value in permille for one knob in one match-up. Nothing learned = 1000 (neutral). <paramref name="enemies"/> are the enemy players'
		/// factions (team game: all of them) and <paramref name="mainTargetFaction"/> the current main target; <paramref name="style"/> is the observed-style
		/// bucket of the main target (null/unseen = the exact three-axis result).
		/// </summary>
		public int Resolve(string personality, string knob, string ownFaction, IReadOnlyList<string> enemies, string mainTargetFaction, string style)
		{
			var axes = Find(personality, knob);
			if (axes == null)
				return LearnAllMath.One;

			var enemyEff = axes.EnemyEff(enemies, mainTargetFaction);
			var s = style != null && axes.Style.TryGetValue(style, out var sv) ? sv : 0;
			return LearnAllMath.Resolve(axes.Global, axes.Own(ownFaction), enemyEff, s);
		}

		/// <summary>Load-side builder for the snapshot reader: installs the axes of one knob verbatim (clamped to the store domain), replacing nothing else.</summary>
		public LearnKnobAxes Install(string personality, string knob)
		{
			var key = personality ?? AnyPersonality;
			if (!personalities.TryGetValue(key, out var table))
				personalities[key] = table = new SortedDictionary<string, LearnKnobAxes>(StringComparer.Ordinal);

			if (!table.TryGetValue(knob, out var axes))
				table[knob] = axes = new LearnKnobAxes();

			return axes;
		}

		public bool IsApplied(string key) => appliedGames.Contains(key);

		public void MarkApplied(string key) => appliedGames.Add(key);
	}
}
