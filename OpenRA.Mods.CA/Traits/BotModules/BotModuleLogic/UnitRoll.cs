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
	/// chance. A listed type this bot never ordered is picked first, one at a time. The roll itself is world-free integer math (the role-mix shares arrive as doubles and are rounded to whole percent at the edge), deterministic on every client.
	/// </summary>
	public static class UnitRoll
	{
		/// <summary>The largest weight one candidate may take; keeps the cumulative sum inside an int roll.</summary>
		public const double WeightCap = 1e12;

		/// <summary>The resolution of the uniform draw: the roll is Next(RollResolution) / RollResolution, scaled by the total weight in double (no int cast of a weight).</summary>
		public const int RollResolution = 1 << 30;

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

		/// <summary>
		/// The mode bias profiles (UNIT-ALL-01 refinement): each folded layer is a named profile, active only while its existing trigger holds. A profile multiplies the weight, never gates,
		/// and has its own yaml strength (UnitRollProfiles; 0 = off) and ablation row (AblationIds.UnitRollProfileRows, same order).
		/// </summary>
		public static readonly string[] ProfileNames = ["composition", "counter", "wavemix", "roledeficit", "opening", "learned"];

		public const int Composition = 0, Counter = 1, WaveMix = 2, RoleDeficit = 3, Opening = 4, Learned = 5;

		/// <summary>A boost profile: 100 + strength while its trigger holds for the candidate, else neutral.</summary>
		public static int Boost(bool active, int strengthPct) => active && strengthPct > 0 ? 100 + strengthPct : 100;

		/// <summary>A strength-scaled term: 100 + (term - 100) x strength / 100, never below 0 (strength 100 = the term as is).</summary>
		public static int Scaled(int termPct, int strengthPct) => (int)Math.Max(0, 100 + (long)(termPct - 100) * Math.Max(0, strengthPct) / 100);

		/// <summary>A composition's pull on a unit: 100 + strength x share / best share of that composition (best share 0 = neutral).</summary>
		public static int ShareBoost(int share, int bestShare, int strengthPct) =>
			share <= 0 || bestShare <= 0 || strengthPct <= 0 ? 100 : 100 + (int)((long)strengthPct * Math.Min(share, bestShare) / bestShare);

		/// <summary>c3, pay-as-you-go: the FIRST build of a listed type does not wait for its full price (OpenRA pays progressively), like TECHCHAIN-PAYGO; it needs a bank above 0 and the other gates (reserve) are tested at cost 0. No savings hold.</summary>
		public static bool FirstAdmits(int bankLeft) => bankLeft > 0;

		/// <summary>Percent product of two percent terms.</summary>
		public static int MulPct(int a, int b) => (int)Math.Clamp((long)a * b / 100, 0, 100_000);

		/// <summary>
		/// The weight of every candidate, in double: fraction x bias% x counter% x boost, then max(weight, floorPct% of the mean). Always at least 1, so a candidate can never drop out
		/// (the floor stays positive for every candidate); all-zero fractions give equal weights. boost = the first-build multiplier of a never-built type (null = 1 for all).
		/// </summary>
		public static double[] Weights(IReadOnlyList<int> fraction, IReadOnlyList<int> biasPct, IReadOnlyList<int> counterPct, int floorPct, IReadOnlyList<double> boost = null)
		{
			var n = fraction.Count;
			var raw = new double[n];
			double sum = 0;
			for (var i = 0; i < n; i++)
			{
				var b = boost == null || double.IsNaN(boost[i]) || boost[i] < 1 ? 1.0 : Math.Min(boost[i], 1e6);
				raw[i] = Math.Min(WeightCap, (double)Math.Max(0, fraction[i]) * Math.Max(0, biasPct[i]) * Math.Max(0, counterPct[i]) / 100.0 * b);
				sum += raw[i];
			}

			var floor = n == 0 ? 0 : sum / n * Math.Max(0, floorPct) / 100.0;
			for (var i = 0; i < n; i++)
				raw[i] = Math.Max(1.0, Math.Max(raw[i], floor));

			return raw;
		}

		public static double Total(IReadOnlyList<double> weights)
		{
			double total = 0;
			foreach (var w in weights)
				total += w;

			return total;
		}

		/// <summary>The index a roll in [0, Total) lands on, by cumulative sum in double (no cast, no wrap); the last candidate when the roll reaches the end; -1 for an empty list.</summary>
		public static int Pick(IReadOnlyList<double> weights, double roll)
		{
			for (var i = 0; i < weights.Count; i++)
			{
				roll -= weights[i];
				if (roll < 0)
					return i;
			}

			return weights.Count - 1;
		}

		/// <summary>
		/// The first-build weight multiplier of a type that is buildable but never built: baseBoost, doubled for every full rampTicks it has stayed unbuilt, capped at cap (never below 1).
		/// Coverage stays near-certain because the pull grows until the type is built.
		/// </summary>
		public static double FirstBoost(int baseBoost, int unbuiltTicks, int rampTicks, int cap)
		{
			var steps = rampTicks > 0 ? Math.Clamp(unbuiltTicks / rampTicks, 0, 30) : 0;
			return Math.Min(Math.Max(1, cap), Math.Max(1, baseBoost) * (double)(1L << steps));
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
