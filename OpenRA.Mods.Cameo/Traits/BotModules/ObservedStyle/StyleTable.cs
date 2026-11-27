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
using System.Security.Cryptography;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4 OBSERVED-STYLE phase d (DESIGN_OBSERVED_STYLE_02 section 3a): the bucketed log-likelihood table LL[class][feature][bucket] in permille of a nat, plus the class
	/// log priors. The runtime table is the HAND PRIOR below: per feature one centre bucket per class and one shared spread, LL = -500 (b - centre)^2 / sigma^2, floored.
	/// It is a reviewed constant written before any label join exists; nothing in it was fitted, tuned or checked against a true label. The label-blind EM of phase c
	/// produces the successor tables (a new Version sha8); the loader for them is not part of this phase. Class order = <see cref="StyleReading.StyleNames"/>.
	/// Features not listed carry no evidence (LL 0): F10 is a confidence input, and the air and naval shares, harvester ratios and the unit value of F1 are left out
	/// because they track faction rosters more than style.
	/// </summary>
	internal sealed class StyleTable
	{
		public const int Buckets = 16;

		/// <summary>Lowest per-bucket log-likelihood (permille of a nat) so one feature can never exclude a class by itself.</summary>
		public const int Floor = -4000;

		// name, sigma^2 x 100, centre bucket for rush, tech, turtle, expansion, steamroller, guerrilla (the 3d signatures).
		static readonly (string Feature, int Sigma2x100, int[] Centres)[] Spec =
		{
			("f1_first_out_tick", 225, new[] { 5, 7, 7, 6, 7, 5 }),
			("f2_army_pm_m3", 225, new[] { 10, 8, 8, 8, 9, 9 }),
			("f2_army_pm_m10", 225, new[] { 10, 11, 10, 11, 12, 10 }),
			("f3_prod_per_tech", 225, new[] { 12, 9, 10, 11, 11, 11 }),
			("f3_first_tech_tick", 144, new[] { 8, 6, 7, 7, 7, 8 }),
			("f4_yards", 100, new[] { 1, 1, 1, 3, 2, 2 }),
			("f4_refineries", 100, new[] { 2, 2, 2, 3, 3, 2 }),
			("f4_second_yard_tick", 144, new[] { 8, 7, 7, 6, 7, 8 }),
			("f4_clusters", 100, new[] { 1, 1, 1, 3, 2, 2 }),
			("f5_defence_share", 400, new[] { 1, 3, 7, 3, 2, 2 }),
			("f5_defence_count", 100, new[] { 2, 3, 4, 3, 2, 2 }),
			("f6_max_tech_value", 100, new[] { 5, 7, 5, 5, 6, 5 }),
			("f7_inf_share", 225, new[] { 4, 2, 3, 3, 1, 5 }),
			("f7_veh_share", 225, new[] { 3, 4, 3, 4, 6, 2 }),
			("f7_mean_unit_value", 100, new[] { 6, 6, 6, 6, 7, 5 }),
			("f8_groups_3000", 100, new[] { 2, 1, 0, 1, 1, 3 }),
			("f8_group_median", 144, new[] { 3, 2, 0, 3, 4, 2 }),
			("f8_group_max", 144, new[] { 4, 2, 0, 3, 5, 2 }),
			("f8_gone_count", 100, new[] { 1, 1, 0, 1, 1, 3 }),
		};

		static StyleTable handPrior;

		/// <summary>LL[(class * StyleFeatures.Count + feature) * Buckets + bucket], permille of a nat.</summary>
		readonly int[] ll;

		/// <summary>Class log priors, permille of a nat (uniform: all 0).</summary>
		readonly int[] logPrior = new int[StyleReading.Classes];

		StyleTable(int[] ll)
		{
			this.ll = ll;
			Version = Hash(ll);
		}

		/// <summary>sha8 (first 8 hex digits, upper case) of the table content; written to every DEV row so a reading names the table it came from.</summary>
		public string Version { get; }

		public static StyleTable HandPrior => handPrior ??= BuildHandPrior();

		public int LogPrior(int cls) => logPrior[cls];

		public int Ll(int cls, int feature, int bucket) => ll[(cls * StyleFeatures.Count + feature) * Buckets + bucket];

		/// <summary>True when feature <paramref name="feature"/> carries evidence in this table (some class has a non-zero entry).</summary>
		public bool Informative(int feature)
		{
			for (var c = 0; c < StyleReading.Classes; c++)
				for (var b = 0; b < Buckets; b++)
					if (Ll(c, feature, b) != 0)
						return true;

			return false;
		}

		static StyleTable BuildHandPrior()
		{
			var table = new int[StyleReading.Classes * StyleFeatures.Count * Buckets];
			foreach (var (feature, sigma2x100, centres) in Spec)
			{
				var f = Array.IndexOf(StyleFeatures.Names, feature);
				if (f < 0)
					throw new InvalidOperationException("unknown style feature " + feature);

				for (var c = 0; c < StyleReading.Classes; c++)
				{
					for (var b = 0; b < Buckets; b++)
					{
						var d = b - centres[c];
						table[(c * StyleFeatures.Count + f) * Buckets + b] = Math.Max(Floor, -(500 * d * d * 100 / sigma2x100));
					}
				}
			}

			return new StyleTable(table);
		}

		static string Hash(int[] values)
		{
			var bytes = new byte[values.Length * 4];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			var digest = SHA256.HashData(bytes);
			return Convert.ToHexString(digest, 0, 4);
		}
	}
}
