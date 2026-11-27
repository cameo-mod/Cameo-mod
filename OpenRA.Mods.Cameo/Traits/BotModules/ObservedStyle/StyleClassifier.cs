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

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4 OBSERVED-STYLE phase d (DESIGN_OBSERVED_STYLE_02 sections 2 and 3, _01 sections 3 and 4): the naive-Bayes style classifier of ONE (viewer, enemy) pair. Pure and
	/// integer: no clock, no random draw, no floating point, no world access, no allocation after construction. Input = the bucketed features of
	/// <see cref="StyleFeatures"/>, the allowlisted actor list (only for the newest sighting tick) and the tracker (first-seen tick); the table is a parameter.
	///
	///   L_c(t)   = d(dt) L_c(t-dt) + w x sum over observed informative features of LL[c][f][bucket_f]      (d = 2^(-dt/H), H = 6000 ticks, in 25-tick steps)
	///   p        = softmax(log prior_c + L_c), fixed-point exp table, each posterior clamped to 5..995 permille (sum exactly 1000)
	///   C        = (1 - H_norm(p)) x coverage(F10) x maturity(min(1, observed ticks / 3000)) x staleness(1 - 1% per second since the newest sighting)
	///   cold     : no sighting yet or no maturity -> p = uniform prior, C = 0.
	///
	/// w = 10 permille per update is a fixed placeholder (the design's "fitter may shrink"): with the 75-tick update cadence the decay memory is about 116 updates, so a
	/// unit weight would count one steady snapshot 116 times and drive every reading to the clamp; w = 1 - d makes the steady state weigh a snapshot about once. It was not
	/// chosen with a label (DESIGN section 3 "Not allowed").
	/// </summary>
	internal sealed class StyleClassifier
	{
		/// <summary>Half-life of the evidence (ticks), design placeholder.</summary>
		public const int HalfLifeTicks = 6000;

		/// <summary>Observed time at which maturity reaches 1 (ticks), design placeholder.</summary>
		public const int MaturityTicks = 3000;

		/// <summary>Evidence weight per update, permille (see the class summary).</summary>
		public const int WeightPermille = 10;

		/// <summary>Game ticks per second (OpenRA normal speed); 1% confidence per second = 10 permille per 25 ticks.</summary>
		public const int TicksPerSecond = 25;

		public const int MinPosterior = 5;

		const int DecayStepTicks = 25;

		// 2^(-25/6000) in Q30.
		const long DecayStepQ30 = 1070645210;

		// exp(-k x 0.05) in Q16, k = 0..ExpSteps; the step is 50 permille of a nat = 50000 units of L (1e-6 nat).
		const int ExpSteps = 200;
		const int ExpStepUnits = 50000;
		const long ExpRatioQ30 = 1021374817; // exp(-0.05) in Q30
		static readonly int[] ExpQ16 = BuildExp();

		// -p ln p x 1e6 for p = i / 1000, and ln 6 x 1e6.
		static readonly int[] NegPLogP = BuildNegPLogP();
		const long LnClassesE6 = 1791759;

		readonly StyleTable table;
		readonly long[] score = new long[StyleReading.Classes];
		readonly long[] evidence = new long[StyleReading.Classes];
		readonly int[] posterior = new int[StyleReading.Classes];
		readonly StyleReading reading = new();
		int lastTick = -1;

		public StyleClassifier(StyleTable table)
		{
			this.table = table;
		}

		/// <summary>The current reading (a stable object, updated in place). Cold start until the first mature update.</summary>
		public StyleReading Reading => reading;

		/// <summary>Version (sha8) of the table the reading comes from.</summary>
		public string TableVersion => table.Version;

		static int[] BuildExp()
		{
			var t = new int[ExpSteps + 1];
			var q30 = 1L << 30;
			for (var k = 0; k <= ExpSteps; k++)
			{
				t[k] = (int)((q30 + (1 << 13)) >> 14);
				q30 = (q30 * ExpRatioQ30) >> 30;
			}

			return t;
		}

		static int[] BuildNegPLogP()
		{
			var t = new int[1001];
			for (var i = 1; i < 1000; i++)
				t[i] = (int)Math.Round(-(i / 1000.0) * Math.Log(i / 1000.0) * 1e6);

			return t;
		}

		/// <summary>Updates the reading from this snapshot. <paramref name="features"/> is the extractor's output for the same snapshot.</summary>
		public void Update(ReadOnlySpan<int> features, ReadOnlySpan<FogStyleActor> actors, StyleTracker tracker, int tick)
		{
			var dt = lastTick < 0 ? 0 : Math.Max(0, tick - lastTick);
			lastTick = tick;

			// Forget: d(dt) in 25-tick steps (capped: after H x 8 the memory is gone anyway).
			var steps = Math.Min(dt / DecayStepTicks, 8 * HalfLifeTicks / DecayStepTicks);
			for (var s = 0; s < steps; s++)
				for (var c = 0; c < StyleReading.Classes; c++)
					evidence[c] = (evidence[c] * DecayStepQ30) >> 30;

			// Evidence of this snapshot.
			var informative = 0;
			for (var f = 0; f < features.Length && f < StyleFeatures.Count; f++)
			{
				var bucket = features[f];
				if (bucket == StyleFeatures.Unknown || !table.Informative(f))
					continue;

				bucket = Math.Min(bucket, StyleTable.Buckets - 1);
				informative++;
				for (var c = 0; c < StyleReading.Classes; c++)
					evidence[c] += (long)table.Ll(c, f, bucket) * WeightPermille;
			}

			// Maturity and the staleness of what the viewer last saw.
			var first = tracker.FirstSeenTick;
			var maturity = first < 0 || informative == 0 ? 0 : (int)Math.Min(1000, (tick - first) * 1000L / MaturityTicks);
			if (maturity <= 0)
			{
				reading.Set(StyleReading.UniformPermille, 0, tick);
				return;
			}

			Posterior();

			var newest = int.MinValue;
			for (var i = 0; i < actors.Length; i++)
				if (actors[i].LastSeenTick > newest)
					newest = actors[i].LastSeenTick;

			var unseen = newest == int.MinValue ? int.MaxValue / 2 : Math.Max(0, tick - newest);
			var stale = (int)Math.Max(0, 1000 - unseen * 10L / TicksPerSecond);
			var coverage = features[StyleFeatures.CoverageIndex] == StyleFeatures.Unknown ? 0 : Math.Min(1000, features[StyleFeatures.CoverageIndex] * 100);

			var confidence = (int)((1000L - NormalisedEntropy()) * coverage / 1000 * maturity / 1000 * stale / 1000);
			reading.Set(posterior, confidence, tick);
		}

		void Posterior()
		{
			long max = long.MinValue;
			for (var c = 0; c < StyleReading.Classes; c++)
			{
				score[c] = evidence[c] + table.LogPrior(c) * 1000L;
				if (score[c] > max)
					max = score[c];
			}

			long sum = 0;
			for (var c = 0; c < StyleReading.Classes; c++)
			{
				var k = (int)Math.Min(ExpSteps, (max - score[c]) / ExpStepUnits);
				expQ[c] = ExpQ16[k];
				sum += expQ[c];
			}

			var total = 0;
			var top = 0;
			for (var c = 0; c < StyleReading.Classes; c++)
			{
				posterior[c] = (int)Math.Max(MinPosterior, expQ[c] * 1000 / sum);
				total += posterior[c];
				if (posterior[c] > posterior[top])
					top = c;
			}

			// The clamp may have added mass: take the difference off the largest class so the sum is exactly 1000.
			posterior[top] -= total - 1000;
		}

		readonly long[] expQ = new long[StyleReading.Classes];

		int NormalisedEntropy()
		{
			long h = 0;
			for (var c = 0; c < StyleReading.Classes; c++)
				h += NegPLogP[Math.Clamp(posterior[c], 0, 1000)];

			return (int)Math.Min(1000, h * 1000 / LnClassesE6);
		}
	}
}
