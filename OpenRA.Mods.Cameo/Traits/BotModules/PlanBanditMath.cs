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
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Tier-3 (fleet orders 2026-10-03): pooled Thompson-sampling bandits with a safety floor, over
	/// continuous EL engagement rewards (`total_milli`, [-1000, 1000]). Pure functions so tests can
	/// drive them without a World.
	///
	/// Model: unknown-mean unknown-variance normal likelihood. The posterior for the arm mean is a
	/// Student-t (df = n-1, loc = sample mean, scale^2 = s^2/n) — the exact conjugate update for a
	/// normal likelihood, which a Bernoulli Beta cannot express for continuous signed rewards.
	///
	/// Partial pooling: a child scope's stats are shrunk toward its parent scope's stats by treating
	/// the parent as pseudo-data capped at PriorCount weight, merged with the standard parallel
	/// (Chan et al.) variance combination. Child evidence dominates as n grows.
	///
	/// Safety floor: after Thompson ranking, any arm whose lower confidence bound
	/// (mean - LcbZ * standard error, once it has >= MinEvidence samples) sits below MinSafetyLcb is
	/// excluded. If every arm is excluded the choice falls back to the arm with the highest LCB —
	/// the floor prefers the least-bad known quantity over exploration.
	/// </summary>
	public struct PlanBanditArmStats
	{
		public long N;
		public double Mean;
		public double M2;

		public static PlanBanditArmStats FromObservation(double x)
		{
			return new PlanBanditArmStats { N = 1, Mean = x, M2 = 0 };
		}

		/// <summary>Welford add of a single observation.</summary>
		public void Add(double x)
		{
			N++;
			var delta = x - Mean;
			Mean += delta / N;
			M2 += delta * (x - Mean);
		}

		/// <summary>Exponential discount of retained evidence (sliding-window approximation).
		/// factor in (0,1]; factor 1 keeps everything.</summary>
		public void Decay(double factor)
		{
			if (factor >= 1)
				return;

			Mean *= factor;
			M2 *= factor * factor;
			N = (long)Math.Floor(N * factor + 0.5);
		}

		/// <summary>Chan et al. merge of two sample stats into one.</summary>
		public static PlanBanditArmStats Merge(PlanBanditArmStats a, PlanBanditArmStats b)
		{
			if (a.N == 0)
				return b;
			if (b.N == 0)
				return a;

			var n = a.N + b.N;
			var delta = b.Mean - a.Mean;
			var mean = (a.N * a.Mean + b.N * b.Mean) / n;
			var m2 = a.M2 + b.M2 + delta * delta * a.N * b.N / n;
			return new PlanBanditArmStats { N = n, Mean = mean, M2 = m2 };
		}

		/// <summary>The same stats re-weighted to at most `weight` pseudo-observations — used to shrink
		/// a parent scope toward a prior so it cannot dominate a child's real evidence.</summary>
		public PlanBanditArmStats Downweighted(long weight)
		{
			if (N <= weight)
				return this;

			var scale = (double)weight / N;
			return new PlanBanditArmStats { N = weight, Mean = Mean, M2 = M2 * scale };
		}
	}

	public static class PlanBanditMath
	{
		/// <summary>Prior when an arm has no evidence anywhere in the pool: neutral engagement score.</summary>
		public const double PriorMean = 0;
		public const double PriorSd = 250;

		/// <summary>Standard error of the arm mean (0 when fewer than 2 samples — falls back to PriorSd).</summary>
		public static double StandardError(PlanBanditArmStats s)
		{
			if (s.N < 2)
				return PriorSd;

			var variance = s.M2 / (s.N - 1);
			return Math.Sqrt(Math.Max(variance, 0) / s.N);
		}

		/// <summary>Lower confidence bound used by the safety floor.</summary>
		public static double LowerConfidenceBound(PlanBanditArmStats s, double z)
		{
			if (s.N < 2)
				return PriorMean - z * PriorSd;

			return s.Mean - z * StandardError(s);
		}

		/// <summary>Partial pooling: merge the child's stats with the parent's downweighted to at most
		/// `priorCount` pseudo-observations. parentCount of 0 or priorCount of 0 disables pooling.</summary>
		public static PlanBanditArmStats Pool(PlanBanditArmStats child, PlanBanditArmStats parent, long priorCount)
		{
			if (priorCount <= 0 || parent.N == 0)
				return child;

			return PlanBanditArmStats.Merge(child, parent.Downweighted(priorCount));
		}

		/// <summary>Thompson draw for an arm's mean under the Student-t posterior. Consumes `df + 1`
		/// Box-Muller gaussians; `uniforms` must supply enough values (bounded by MaxDrawAttempts at
		/// the call site). n&lt;2 draws from the neutral prior.</summary>
		public static double SampleMean(PlanBanditArmStats s, IEnumerator<double> uniforms)
		{
			double loc, scale;
			long df;
			if (s.N < 2)
			{
				loc = PriorMean;
				scale = PriorSd;
				df = 1;
			}
			else
			{
				loc = s.Mean;
				scale = StandardError(s);
				df = s.N - 1;
				if (df > 64)
					return loc + scale * NextGaussian(uniforms); // t_64 ≈ N(0,1) within a fraction of a milli
			}

			// T = Z / sqrt(V/df), V ~ chi^2_df = sum of df squared gaussians.
			var z = NextGaussian(uniforms);
			double v = 0;
			for (var i = 0; i < df; i++)
			{
				var g = NextGaussian(uniforms);
				v += g * g;
			}

			return loc + scale * z / Math.Sqrt(v / df);
		}

		static double NextGaussian(IEnumerator<double> uniforms)
		{
			var u1 = Next(uniforms);
			var u2 = Next(uniforms);
			if (u1 <= 0)
				u1 = 1e-12;

			return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
		}

		static double Next(IEnumerator<double> uniforms)
		{
			if (!uniforms.MoveNext())
				throw new InvalidOperationException("PlanBanditMath: uniform stream exhausted");

			return uniforms.Current;
		}

		/// <summary>
		/// The arm choice: sort candidate names, Thompson-sample each pooled arm, take the best draw,
		/// then enforce the safety floor — a sampled winner whose OWN-scope evidence (the n in `evidence`,
		/// not the pooled count) is at least minEvidence and whose pooled LCB is below minSafetyLcb is
		/// blocked; the floor then returns the max-LCB arm, the least-bad known quantity. Returns null
		/// only when there are no candidates.
		/// </summary>
		public static string ChooseArm(IReadOnlyDictionary<string, PlanBanditArmStats> pooled,
			IReadOnlyDictionary<string, long> evidence,
			double lcbZ, long minEvidence, double minSafetyLcb, IEnumerable<double> uniformStream)
		{
			if (pooled == null || pooled.Count == 0)
				return null;

			using var uniforms = uniformStream.GetEnumerator();
			string bestName = null;
			var bestDraw = double.NegativeInfinity;
			foreach (var name in pooled.Keys.OrderBy(k => k, StringComparer.Ordinal))
			{
				var draw = SampleMean(pooled[name], uniforms);
				if (draw > bestDraw)
				{
					bestDraw = draw;
					bestName = name;
				}
			}

			if (bestName == null)
				return null;

			var winner = pooled[bestName];
			var ownEvidence = evidence != null && evidence.TryGetValue(bestName, out var n) ? n : 0;
			if (ownEvidence >= minEvidence && LowerConfidenceBound(winner, lcbZ) < minSafetyLcb)
			{
				// Winner below the safety floor — pick the arm with the highest LCB instead.
				string floorName = null;
				var floorLcb = double.NegativeInfinity;
				foreach (var pair in pooled)
				{
					var lcb = LowerConfidenceBound(pair.Value, lcbZ);
					if (lcb > floorLcb)
					{
						floorLcb = lcb;
						floorName = pair.Key;
					}
				}

				return floorName;
			}

			return bestName;
		}
	}
}
