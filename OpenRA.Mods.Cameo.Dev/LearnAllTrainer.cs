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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Dev
{
	/// <summary>
	/// LEARN-ALL-02 P1: the DEV step. One bounded step in log space per axis per game, quantized back to permille. DEV build only (this assembly is
	/// built with CameoDevLogging=true); the release assembly holds the read side only. tools/ai/fit_learn_all.py mirrors this exactly (golden vectors).
	/// </summary>
	public static class LearnAllStep
	{
		/// <summary>The step size in log space per unit of signal, and the bound on one step. Small by rule: the bot drifts game by game.</summary>
		public const double Rate = 0.04;
		public const double MaxLogStep = 0.08;

		/// <summary>
		/// One step of one estimate. <paramref name="signal"/> is the game's signed, bounded outcome in [-1, 1] for this axis. null / NaN / infinity = no
		/// evidence: the previous estimate is kept as it is (never zero, never an invented outcome).
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

	/// <summary>One enemy seat of a game: the faction it played and its weight (per-player counting). The observed style is NOT per enemy: see <see cref="SeatObservation.MainTargetStyle"/>.</summary>
	public readonly record struct EnemyObservation(string Faction, int Weight = 1);

	/// <summary>
	/// One bot seat's view of one game for one knob: its personality, own faction, the enemy seats it observed, the AUTHORITATIVE main-target faction
	/// (the input the decision used, not derived from the seats), the observed-style bucket the bot read from the MAIN TARGET owner (null = unseen) and the
	/// game's bounded signed outcome for the knob (null = no evidence). Orchestrator ruling 2026-11-27: S_obs in a team game is the main target owner's observed
	/// style ONLY and takes the WHOLE step (never split by enemy weight); unseen = the three-axis result. E_eff is unchanged (all enemy players per player + the main target).
	/// </summary>
	public readonly record struct SeatObservation(string Personality, string OwnFaction, IReadOnlyList<EnemyObservation> Enemies, string MainTarget, string MainTargetStyle, double? Signal);

	public static class LearnAllTrainer
	{
		const double Scale = 1e9;

		/// <summary>A game id is part of a MiniYaml key (game|personality|knob): the same charset as tools/ai/fit_learn_all.py (GAME_ID).</summary>
		static readonly Regex GameIdPattern = new("^[A-Za-z0-9_.\\-]+\\z", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		/// <summary>
		/// One game closes for one knob: ALL eligible bot seats of the game are combined deterministically, then every table takes exactly one step per axis.
		/// Targets: each distinct personality present (its own table, a new one starts from the global values as they were BEFORE this game), then the
		/// global ("any") table with every seat. A seat with no evidence (null/NaN/infinite signal) contributes nothing. Contributions are summed as
		/// integers (1e-9 quantized) so the result does not depend on seat order. Returns true when at least one table stepped; a (game, table, knob)
		/// that was already applied is skipped (idempotent).
		/// </summary>
		public static bool ApplyGame(LearnTable table, string gameId, string knob, IReadOnlyList<SeatObservation> seats)
		{
			if (gameId == null || !GameIdPattern.IsMatch(gameId) || knob == null || seats == null)
				return false;

			var valid = seats.Where(s => s.Signal is double d && !double.IsNaN(d) && !double.IsInfinity(d)).ToList();
			if (valid.Count == 0)
				return false;

			var targets = valid.Select(s => s.Personality).Where(p => !string.IsNullOrEmpty(p) && p != LearnTable.AnyPersonality)
				.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();
			targets.Add(LearnTable.AnyPersonality);

			var changed = false;
			foreach (var target in targets)
			{
				var key = gameId + "|" + target + "|" + knob;
				if (table.IsApplied(key))
					continue;

				var group = target == LearnTable.AnyPersonality ? valid : valid.Where(s => s.Personality == target).ToList();
				Step(table, target, knob, group);
				table.MarkApplied(key);
				changed = true;
			}

			return changed;
		}

		static long Q(double x) => (long)(Math.Sign(x) * Math.Floor(Math.Abs(x) * Scale + 0.5));

		static void Add(SortedDictionary<string, (long Sum, int N)> map, string key, double contribution)
		{
			map.TryGetValue(key, out var cur);
			map[key] = (cur.Sum + Q(contribution), cur.N + 1);
		}

		static double Mean((long Sum, int N) a) => a.Sum / (a.N * Scale);

		static void Step(LearnTable table, string personality, string knob, List<SeatObservation> seats)
		{
			var existed = table.Personalities.TryGetValue(personality, out var own) && own.ContainsKey(knob);
			LearnKnobAxes pre;
			if (existed)
				pre = own[knob].Clone();
			else if (personality != LearnTable.AnyPersonality && table.Personalities.TryGetValue(LearnTable.AnyPersonality, out var any) && any.TryGetValue(knob, out var g))
				pre = g.Clone();
			else
				pre = new LearnKnobAxes();

			var axes = table.Install(personality, knob);
			if (!existed)
				Copy(pre, axes);

			long globalSum = 0;
			var ownMap = new SortedDictionary<string, (long Sum, int N)>(StringComparer.Ordinal);
			var enemyMap = new SortedDictionary<string, (long Sum, int N)>(StringComparer.Ordinal);
			var styleMap = new SortedDictionary<string, (long Sum, int N)>(StringComparer.Ordinal);
			var styleSeeds = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);

			foreach (var seat in seats)
			{
				var s = seat.Signal.Value;
				globalSum += Q(s);
				if (seat.OwnFaction != null)
					Add(ownMap, seat.OwnFaction, s);

				var enemies = (seat.Enemies ?? []).Where(e => e.Weight > 0).ToList();
				var total = 0L;
				foreach (var e in enemies)
					total += e.Weight;

				if (total == 0)
					continue;

				var share = new SortedDictionary<string, double>(StringComparer.Ordinal);
				foreach (var e in enemies)
				{
					if (e.Faction != null)
						share[e.Faction] = share.GetValueOrDefault(e.Faction) + (double)e.Weight / total;
				}

				foreach (var kv in share)
					Add(enemyMap, kv.Key, s * kv.Value);

				if (seat.MainTargetStyle != null)
				{
					// The WHOLE style step goes to the main target owner's observed style. The seed is the prior three-axis result of THIS seat's match-up, from the
					// pre-step axes: the team enemy value over the seats it observed and its authoritative main target.
					var seed = LearnAllMath.SeedStyle(pre.Global, pre.Own(seat.OwnFaction), SeatEnemyEff(pre, enemies, seat.MainTarget));
					Add(styleMap, seat.MainTargetStyle, s);
					if (!styleSeeds.TryGetValue(seat.MainTargetStyle, out var list))
						styleSeeds[seat.MainTargetStyle] = list = [];
					list.Add(seed);
				}
			}

			axes.Global = LearnAllStep.Next(pre.Global, globalSum / (seats.Count * Scale));
			foreach (var kv in ownMap)
				axes.OwnFaction[kv.Key] = LearnAllStep.Next(pre.Own(kv.Key), Mean(kv.Value));
			foreach (var kv in enemyMap)
				axes.EnemyFaction[kv.Key] = LearnAllStep.Next(pre.Enemy(kv.Key), Mean(kv.Value));
			foreach (var kv in styleMap)
			{
				var current = pre.Style.TryGetValue(kv.Key, out var cur) ? cur : LearnAllMath.GeoMean(styleSeeds[kv.Key].ToArray());
				axes.Style[kv.Key] = LearnAllStep.Next(current, Mean(kv.Value));
			}
		}

		/// <summary>The enemy value of one seat's match-up: TeamEnemy(per observed enemy seat, main target), all from the pre-step axes. No main target = the mean of the seats.</summary>
		internal static int SeatEnemyEff(LearnKnobAxes pre, List<EnemyObservation> enemies, string mainTarget)
		{
			var per = enemies.Select(e => pre.Enemy(e.Faction)).ToArray();
			var main = mainTarget != null ? pre.Enemy(mainTarget) : LearnAllMath.GeoMean(per);
			return per.Length == 0 ? (mainTarget != null ? main : pre.Global) : LearnAllMath.TeamEnemy(per, main);
		}

		static void Copy(LearnKnobAxes from, LearnKnobAxes to)
		{
			to.Global = from.Global;
			foreach (var kv in from.OwnFaction)
				to.OwnFaction[kv.Key] = kv.Value;
			foreach (var kv in from.EnemyFaction)
				to.EnemyFaction[kv.Key] = kv.Value;
			foreach (var kv in from.Style)
				to.Style[kv.Key] = kv.Value;
		}
	}

	/// <summary>The deterministic text of a table (DEV/tests only; the release never writes a snapshot): sorted personalities, knobs and keys, tabs, "\n", invariant integers.</summary>
	public static class LearnAllSnapshotWriter
	{
		public static string Write(LearnTable table, bool includeApplied = true)
		{
			var sb = new StringBuilder();
			sb.Append(LearnAllSnapshot.Root).Append(":\n");
			sb.Append("\tVersion: ").Append(LearnAllSnapshot.Version.ToString(CultureInfo.InvariantCulture)).Append('\n');
			if (includeApplied && table.AppliedGames.Count > 0)
			{
				sb.Append("\tApplied:\n");
				foreach (var key in table.AppliedGames)
					sb.Append("\t\t").Append(key).Append(":\n");
			}

			foreach (var (personality, knobs) in table.Personalities)
			{
				foreach (var (knob, axes) in knobs)
				{
					sb.Append("\tKnob@").Append(personality).Append("__").Append(knob).Append(":\n");
					sb.Append("\t\tGlobal: ").Append(axes.Global.ToString(CultureInfo.InvariantCulture)).Append('\n');
					foreach (var kv in axes.OwnFaction)
						sb.Append("\t\tOwn@").Append(kv.Key).Append(": ").Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
					foreach (var kv in axes.EnemyFaction)
						sb.Append("\t\tEnemy@").Append(kv.Key).Append(": ").Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
					foreach (var kv in axes.Style)
						sb.Append("\t\tStyle@").Append(kv.Key).Append(": ").Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
				}
			}

			return sb.ToString();
		}
	}
}
