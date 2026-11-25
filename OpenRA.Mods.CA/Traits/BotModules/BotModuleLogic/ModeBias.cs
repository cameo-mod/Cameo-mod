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
using OpenRA.Support;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// MODE-BIAS-01 (user ruling 2026-11-25): the pure part of the mode bias profiles. A profile is an extra building list plus a milli multiplier on those types'
	/// BuildingFractions weights, active only while the list's own trigger holds. It is never a gate: every entry keeps a positive weight. Integer math and the
	/// synced random only, so lockstep stays deterministic.
	/// </summary>
	public static class ModeBias
	{
		public const string Essentials = "essentials";
		public const string TechChain = "techchain";
		public const string AntiRush = "antirush";
		public const string Opening = "opening";

		/// <summary>1000 = neutral (no change of the weight).</summary>
		public const int Neutral = 1000;

		/// <summary>The default cap of the composed multiplier of all active profiles on one type.</summary>
		public const int DefaultCap = 20000;

		// A weight of the ordered roll is clamped here so the total of any realistic table stays inside an int.
		const int MaxWeight = 1_000_000;

		/// <summary>One more profile's strength folded into the composed multiplier: current x strength / 1000 in long math, never below 1, never above the cap.</summary>
		public static int Combine(int current, int strength, int cap) =>
			(int)Math.Clamp((long)Math.Max(1, current) * Math.Max(1, strength) / Neutral, 1, Math.Max(1, cap));

		/// <summary>The knob milli of a type times its bias milli: max(1, knob x bias / 1000) in long math (an int result).</summary>
		public static int CombineMilli(int knobMilli, int biasMilli) =>
			(int)Math.Clamp((long)knobMilli * biasMilli / Neutral, 1, int.MaxValue);

		/// <summary>
		/// The indices 0..n-1 in a weighted order without replacement, lazily: one draw per index reached, r = Next(total of the remaining weights), the index whose
		/// cumulative weight range holds r. A weight below 1 counts as 1, so every index appears exactly once (the floor chance).
		/// </summary>
		public static IEnumerable<int> WeightedIndexOrder(IReadOnlyList<int> weights, MersenneTwister rng)
		{
			var remaining = new List<int>(weights.Count);
			var w = new List<int>(weights.Count);
			long total = 0;
			for (var i = 0; i < weights.Count; i++)
			{
				var clamped = Math.Clamp(weights[i], 1, MaxWeight);
				remaining.Add(i);
				w.Add(clamped);
				total += clamped;
			}

			while (remaining.Count > 0)
			{
				var r = rng.Next((int)total);
				var at = 0;
				while (at < remaining.Count - 1 && r >= w[at])
				{
					r -= w[at];
					at++;
				}

				var picked = remaining[at];
				total -= w[at];
				remaining.RemoveAt(at);
				w.RemoveAt(at);
				yield return picked;
			}
		}

		/// <summary>The BuildingFractions entries (ordinal key order first, as the uniform shuffle starts) in the weighted order of <paramref name="biasMilli"/>, lazily.</summary>
		public static IEnumerable<KeyValuePair<string, int>> WeightedOrder(IEnumerable<KeyValuePair<string, int>> fractions, Func<string, int> biasMilli, MersenneTwister rng)
		{
			var items = fractions.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToList();
			var weights = new int[items.Count];
			for (var i = 0; i < weights.Length; i++)
				weights[i] = biasMilli(items[i].Key);

			foreach (var index in WeightedIndexOrder(weights, rng))
				yield return items[index];
		}
	}

	/// <summary>
	/// MODE-BIAS-01: the strength table of one player's mode bias profiles (milli, 1000 = neutral) and the cap of the composed multiplier. Initialised from the Info
	/// fields; the planner reads strengths only through <see cref="Get"/>. <see cref="TrySet"/> is the override hook a later learner calls per player (nothing does yet).
	/// </summary>
	public sealed class ModeBiasParams
	{
		readonly Dictionary<string, int> strength = new(StringComparer.Ordinal);

		public ModeBiasParams(int essentials, int techChain, int antiRush, int opening, int cap)
		{
			strength[ModeBias.Essentials] = Math.Max(1, essentials);
			strength[ModeBias.TechChain] = Math.Max(1, techChain);
			strength[ModeBias.AntiRush] = Math.Max(1, antiRush);
			strength[ModeBias.Opening] = Math.Max(1, opening);
			Cap = Math.Max(1, cap);
		}

		/// <summary>The cap of the composed multiplier on one type.</summary>
		public int Cap { get; private set; }

		/// <summary>The strength of a profile; an unknown id is neutral.</summary>
		public int Get(string profileId) => strength.TryGetValue(profileId, out var milli) ? milli : ModeBias.Neutral;

		/// <summary>Overrides the strength of a known profile (clamped to at least 1). False = unknown id, nothing changed.</summary>
		public bool TrySet(string profileId, int milli)
		{
			if (!strength.ContainsKey(profileId))
				return false;

			strength[profileId] = Math.Max(1, milli);
			return true;
		}

		/// <summary>The composed multiplier of the given active profiles on one type: the product of their strengths (/1000 per extra factor), within [1, Cap].</summary>
		public int Compose(IEnumerable<string> activeProfileIds)
		{
			var m = ModeBias.Neutral;
			foreach (var id in activeProfileIds)
				m = ModeBias.Combine(m, Get(id), Cap);

			return m;
		}
	}
}
