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
	/// LEARN-ALL-02 P1a: the DEV step. One bounded step in log space per axis per game. Pure and world-free; the release never calls it. The result is
	/// quantized back to permille, so a snapshot written from it is integers only. tools/ai/fit_learn_all.py mirrors this exactly (shared golden vectors).
	/// </summary>
	public static class LearnAllStep
	{
		/// <summary>The step size in log space per unit of signal, and the bound on one step. Small by rule: the bot drifts game by game.</summary>
		public const double Rate = 0.04;
		public const double MaxLogStep = 0.08;

		/// <summary>
		/// One step of one estimate. <paramref name="signal"/> is the game's signed, bounded outcome in [-1, 1] for this axis (the caller divides the update budget
		/// across enemy seats). null / NaN / infinity = no evidence: the previous estimate is kept as it is (never zero, never an invented outcome).
		/// </summary>
		public static int Next(int current, double? signal)
		{
			var cur = LearnAllMath.ClampStore(current);
			if (signal is not double s || double.IsNaN(s) || double.IsInfinity(s))
				return cur;

			var step = Math.Clamp(Rate * Math.Clamp(s, -1.0, 1.0), -MaxLogStep, MaxLogStep);
			return LearnAllMath.ClampStore((int)Math.Round(cur * Math.Exp(step), MidpointRounding.AwayFromZero));
		}
	}

	/// <summary>One enemy seat of a game: the faction it played, the observed-style bucket the bot read from it (null = none qualified) and its weight (per-player counting).</summary>
	public readonly record struct EnemyObservation(string Faction, string Style, int Weight = 1);

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
	/// The release only reads a table (<see cref="Resolve"/>); <see cref="Apply"/> is called by the DEV learner and its python mirror.
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

			var main = axes.Enemy(mainTargetFaction);
			int enemyEff;
			if (enemies == null || enemies.Count == 0)
				enemyEff = main;
			else
			{
				var per = new int[enemies.Count];
				for (var i = 0; i < per.Length; i++)
					per[i] = axes.Enemy(enemies[i]);

				enemyEff = LearnAllMath.TeamEnemy(per, main);
			}

			var s = style != null && axes.Style.TryGetValue(style, out var sv) ? sv : 0;
			return LearnAllMath.Resolve(axes.Global, axes.Own(ownFaction), enemyEff, s);
		}

		/// <summary>
		/// One game's update of one knob for one personality (exactly one step per axis per game). Returns false and changes nothing when this game id was
		/// already applied (idempotent), or when there is no evidence for the knob (<paramref name="signal"/> null/NaN/infinite: every estimate is kept,
		/// but the game id is not consumed so a later evidence-bearing application of the same game is still possible). The update budget of the game is
		/// divided across the enemy observations by weight; F_own and the style buckets take the whole signal.
		/// New keys start from the pre-step values: F_own and E from G, a style from the three-axis result of that match-up.
		/// </summary>
		public bool Apply(string gameId, string personality, string knob, string ownFaction, IReadOnlyList<EnemyObservation> enemies, double? signal)
		{
			if (gameId == null || knob == null)
				return false;

			if (signal is not double s || double.IsNaN(s) || double.IsInfinity(s))
				return false;

			if (appliedGames.Contains(AppliedKey(gameId, personality, knob)))
				return false;

			var axes = Ensure(personality ?? AnyPersonality, knob);
			var pre = axes.Clone();

			axes.Global = LearnAllStep.Next(pre.Global, s);
			if (ownFaction != null)
				axes.OwnFaction[ownFaction] = LearnAllStep.Next(pre.Own(ownFaction), s);

			if (enemies != null && enemies.Count > 0)
			{
				long totalWeight = 0;
				foreach (var e in enemies)
					totalWeight += Math.Max(0, e.Weight);

				if (totalWeight > 0)
				{
					// Per faction: the shares of the seats that played it add up (a faction seen twice is stepped once with its summed share).
					var share = new SortedDictionary<string, double>(StringComparer.Ordinal);
					var styleShare = new SortedDictionary<string, double>(StringComparer.Ordinal);
					foreach (var e in enemies)
					{
						if (e.Weight <= 0)
							continue;

						var part = (double)e.Weight / totalWeight;
						if (e.Faction != null)
							share[e.Faction] = share.GetValueOrDefault(e.Faction) + part;
						if (e.Style != null)
							styleShare[e.Style] = styleShare.GetValueOrDefault(e.Style) + part;
					}

					foreach (var kv in share)
						axes.EnemyFaction[kv.Key] = LearnAllStep.Next(pre.Enemy(kv.Key), s * kv.Value);

					foreach (var kv in styleShare)
					{
						var seed = pre.Style.TryGetValue(kv.Key, out var cur)
							? cur
							: LearnAllMath.SeedStyle(pre.Global, pre.Own(ownFaction), pre.Enemy(MainFaction(enemies)));
						axes.Style[kv.Key] = LearnAllStep.Next(seed, s * kv.Value);
					}
				}
			}

			appliedGames.Add(AppliedKey(gameId, personality, knob));
			return true;
		}

		/// <summary>
		/// One game closes: the personality's own table steps first (a new personality copies the global values as they were BEFORE this game), then the
		/// global ("any") table steps with the same signal, so every game moves the global estimate once and its own personality once. Returns true when
		/// at least one table changed.
		/// </summary>
		public bool ApplyGame(string gameId, string personality, string knob, string ownFaction, IReadOnlyList<EnemyObservation> enemies, double? signal)
		{
			var changed = false;
			if (!string.IsNullOrEmpty(personality) && personality != AnyPersonality)
				changed |= Apply(gameId, personality, knob, ownFaction, enemies, signal);

			changed |= Apply(gameId, AnyPersonality, knob, ownFaction, enemies, signal);
			return changed;
		}

		/// <summary>The faction of the heaviest enemy seat (first on a tie), the one a style seed is measured against.</summary>
		static string MainFaction(IReadOnlyList<EnemyObservation> enemies)
		{
			string best = null;
			var weight = int.MinValue;
			foreach (var e in enemies)
			{
				if (e.Weight > weight)
				{
					weight = e.Weight;
					best = e.Faction;
				}
			}

			return best;
		}

		static string AppliedKey(string gameId, string personality, string knob) => gameId + "|" + (personality ?? AnyPersonality) + "|" + knob;

		/// <summary>The axes to update: the personality's own, created from a copy of the global ("any") axes when this personality is new (never from 1000).</summary>
		LearnKnobAxes Ensure(string personality, string knob)
		{
			if (!personalities.TryGetValue(personality, out var table))
				personalities[personality] = table = new SortedDictionary<string, LearnKnobAxes>(StringComparer.Ordinal);

			if (table.TryGetValue(knob, out var axes))
				return axes;

			table[knob] = axes = personalities.TryGetValue(AnyPersonality, out var any) && any.TryGetValue(knob, out var g)
				? g.Clone()
				: new LearnKnobAxes();
			return axes;
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

		public void MarkApplied(string key) => appliedGames.Add(key);
	}
}
