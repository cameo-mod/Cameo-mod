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
	/// UNIT-ALL-01 (USER 11-25, V07: no Mammoth, OI tank or Duelist ever built): the unit pick is ONE fraction roll over the ai.yaml UnitsToBuild entries the queue can
	/// build now. weight = fraction x personality bias x counter demand (x role deficit x learned prior), floored at epsilon x the mean weight so every listed unit keeps a
	/// chance. A listed type this bot never ordered is picked first, one at a time. World-free integer math: no floats, deterministic on every client.
	/// </summary>
	public static class UnitRoll
	{
		/// <summary>The largest weight one candidate may take; keeps the cumulative sum inside an int roll.</summary>
		public const long WeightCap = 5_000_000;

		/// <summary>Percent bias of a role for a personality at a tick: the early value, moving linearly to the late value over rampTicks (late missing = stays early). 100 = neutral.</summary>
		public static int RoleBiasPct(IReadOnlyDictionary<string, int> early, IReadOnlyDictionary<string, int> late, string personality, string role, int tick, int rampTicks)
		{
			if (early == null || string.IsNullOrEmpty(personality) || string.IsNullOrEmpty(role))
				return 100;

			var key = personality + "." + role;
			var e = early.TryGetValue(key, out var ev) ? ev : 100;
			if (late == null || rampTicks <= 0 || !late.TryGetValue(key, out var l))
				return e;

			var t = Math.Clamp(tick, 0, rampTicks);
			return (int)(e + (long)(l - e) * t / rampTicks);
		}

		/// <summary>Counter demand in percent: 100 neutral, up to 100 + gainPct for the candidate that is the best answer to the observed enemy army (score / best score).</summary>
		public static int CounterPct(long score, long bestScore, int gainPct) =>
			bestScore <= 0 || score <= 0 ? 100 : 100 + (int)(gainPct * Math.Min(score, bestScore) / bestScore);

		/// <summary>Role-mix deficit term in percent: 100 neutral, up to 200 while the role has none of its target share.</summary>
		public static int DeficitPct(int targetPct, int actualPct) =>
			targetPct <= 0 || actualPct >= targetPct ? 100 : 100 + (int)(100L * (targetPct - actualPct) / targetPct);

		/// <summary>Percent product of two percent terms.</summary>
		public static int MulPct(int a, int b) => (int)Math.Clamp((long)a * b / 100, 0, 100_000);

		/// <summary>
		/// The weight of every candidate: fraction x bias% x counter%, then max(weight, floorPct% of the mean). Always at least 1, so a candidate can never drop out;
		/// all-zero fractions give equal weights.
		/// </summary>
		public static long[] Weights(IReadOnlyList<int> fraction, IReadOnlyList<int> biasPct, IReadOnlyList<int> counterPct, int floorPct)
		{
			var n = fraction.Count;
			var raw = new long[n];
			long sum = 0;
			for (var i = 0; i < n; i++)
			{
				raw[i] = Math.Min(WeightCap, (long)Math.Max(0, fraction[i]) * Math.Max(0, biasPct[i]) * Math.Max(0, counterPct[i]) / 100);
				sum += raw[i];
			}

			var floor = n == 0 ? 0 : sum / n * Math.Max(0, floorPct) / 100;
			for (var i = 0; i < n; i++)
				raw[i] = Math.Max(1, Math.Max(raw[i], floor));

			return raw;
		}

		public static long Total(IReadOnlyList<long> weights)
		{
			long total = 0;
			foreach (var w in weights)
				total += w;

			return total;
		}

		/// <summary>The index a roll in [0, Total) lands on; -1 for an empty list.</summary>
		public static int Pick(IReadOnlyList<long> weights, int roll)
		{
			for (var i = 0; i < weights.Count; i++)
			{
				roll -= (int)weights[i];
				if (roll < 0)
					return i;
			}

			return weights.Count - 1;
		}

		/// <summary>Indices of the candidates never ordered before (the first-of-each set).</summary>
		public static List<int> Unbuilt(IReadOnlyList<string> names, Func<string, bool> built)
		{
			var list = new List<int>();
			for (var i = 0; i < names.Count; i++)
				if (!built(names[i]))
					list.Add(i);

			return list;
		}

		/// <summary>The personality named by the first granted personality-X condition, or "". Order is the table order, so a bot with two grants is still deterministic.</summary>
		public static string PersonalityOf(IEnumerable<string> personalities, Func<string, bool> granted)
		{
			foreach (var p in personalities)
				if (granted(p))
					return p;

			return "";
		}
	}
}
