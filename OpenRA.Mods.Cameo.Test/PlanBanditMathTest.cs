#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// Tier-3 (fleet orders 2026-10-03): the pure bandit math — pooling, the Student-t Thompson draw,
	// the safety floor, decay — plus the learned-file parser and the pooling chain.
	[TestFixture]
	public class PlanBanditMathTest
	{
		static PlanBanditArmStats Stats(long n, double mean, double m2) => new() { N = n, Mean = mean, M2 = m2 };

		static IEnumerable<double> UniformsOf(params double[] values)
		{
			foreach (var v in values)
				yield return v;
			while (true)
				yield return 0.5;
		}

		[Test]
		public void WelfordTracksMeanAndVariance()
		{
			var s = new PlanBanditArmStats();
			foreach (var x in new[] { 100.0, -200.0, 50.0, 250.0 })
				s.Add(x);

			Assert.That(s.N, Is.EqualTo(4));
			Assert.That(s.Mean, Is.EqualTo(50.0).Within(1e-9));
			Assert.That(s.M2, Is.EqualTo(105000.0).Within(1e-6), "sum of squared deviations");
		}

		[Test]
		public void MergeMatchesOnePassOverAllSamples()
		{
			var a = new PlanBanditArmStats();
			var b = new PlanBanditArmStats();
			var both = new PlanBanditArmStats();
			foreach (var x in new[] { 10.0, 20.0, 30.0 })
			{
				a.Add(x);
				both.Add(x);
			}

			foreach (var x in new[] { -5.0, -15.0 })
			{
				b.Add(x);
				both.Add(x);
			}

			var merged = PlanBanditArmStats.Merge(a, b);
			Assert.That(merged.N, Is.EqualTo(both.N));
			Assert.That(merged.Mean, Is.EqualTo(both.Mean).Within(1e-9));
			Assert.That(merged.M2, Is.EqualTo(both.M2).Within(1e-6));
		}

		[Test]
		public void DownweightKeepsMeanAndCapsCount()
		{
			var s = Stats(40, 120, 90000).Downweighted(8);
			Assert.That(s.N, Is.EqualTo(8));
			Assert.That(s.Mean, Is.EqualTo(120).Within(1e-9));
			Assert.That(s.M2, Is.EqualTo(90000 * 8.0 / 40).Within(1e-6), "variance evidence scales with the weight");
			Assert.That(Stats(4, 120, 9000).Downweighted(8).N, Is.EqualTo(4), "never inflates real evidence");
		}

		[Test]
		public void PoolingShrinksChildTowardParent()
		{
			var child = Stats(4, -100, 40000);
			var parent = Stats(50, 300, 200000);
			var pooled = PlanBanditMath.Pool(child, parent, 8);

			Assert.That(pooled.N, Is.EqualTo(12));
			Assert.That(pooled.Mean, Is.EqualTo((4 * -100 + 8 * 300) / 12.0).Within(1e-9),
				"8 pseudo-observations of the parent pull the sparse child toward +300, not to it");
		}

		[Test]
		public void LcbIsMeanMinusZTimesStandardError()
		{
			// sd^2 = 40000/3, SE = sqrt(sd^2/4) = sqrt(10000/3) ~ 57.735.
			var s = Stats(4, 100, 40000);
			Assert.That(PlanBanditMath.LowerConfidenceBound(s, 1.64), Is.EqualTo(100 - 1.64 * 57.735).Within(0.01));
			Assert.That(PlanBanditMath.LowerConfidenceBound(Stats(1, 500, 0), 1.64),
				Is.EqualTo(0 - 1.64 * PlanBanditMath.PriorSd).Within(1e-9), "sub-2-n evidence reads the neutral prior");
		}

		[Test]
		public void ThompsonDrawIsDeterministicForAUniformStream()
		{
			var pooled = new Dictionary<string, PlanBanditArmStats>
			{
				["a"] = Stats(10, 100, 40000),
				["b"] = Stats(10, 0, 40000),
				["c"] = Stats(0, 0, 0)
			};

			var evidence = new Dictionary<string, long> { ["a"] = 10, ["b"] = 10, ["c"] = 0 };
			double[] stream() => new double[2000].Fill(0.37);
			var first = PlanBanditMath.ChooseArm(pooled, evidence, 1.64, 4, -250, UniformsOf(stream()));
			var second = PlanBanditMath.ChooseArm(pooled, evidence, 1.64, 4, -250, UniformsOf(stream()));
			Assert.That(first, Is.EqualTo(second), "the same uniform stream must pick the same arm");
			Assert.That(first, Is.Not.Null);
		}

		[Test]
		public void SafetyFloorBlocksAnEvidencedLoser()
		{
			// `bad`: n=4, mean=-100, huge spread — a lucky draw (t~4.45) lifts it to ~+1119 so it WINS the
			// Thompson sample, but its LCB (-100 - 1.64 x 274 ~ -549) is below the -250 floor, so the floor
			// blocks it and the max-LCB arm `ok` is returned instead.
			var pooled = new Dictionary<string, PlanBanditArmStats>
			{
				["bad"] = Stats(4, -100, 900000),
				["ok"] = Stats(30, 0, 10000)
			};
			var evidence = new Dictionary<string, long> { ["bad"] = 4, ["ok"] = 30 };

			var choice = PlanBanditMath.ChooseArm(pooled, evidence, 1.64, 4, -250,
				UniformsOf(1e-9, 0.0, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.9999, 0.5));
			Assert.That(choice, Is.EqualTo("ok"), "a lucky draw below the safety floor must not be chosen");
		}

		[Test]
		public void SafetyFloorIgnoresUnevidencedArms()
		{
			// `aa`: n=2 (< MinEvidence 4), mean=-900, huge spread — visited first (sorted), the lucky-draw
			// stream lifts it to ~+2770 and it wins the Thompson sample. Its LCB is far below -250, but the
			// floor only binds at n >= MinEvidence, so the sparse arm is returned.
			var pooled = new Dictionary<string, PlanBanditArmStats>
			{
				["aa"] = Stats(2, -900, 900000),
				["ok"] = Stats(30, 0, 10000)
			};
			var evidence = new Dictionary<string, long> { ["aa"] = 2, ["ok"] = 30 };

			var choice = PlanBanditMath.ChooseArm(pooled, evidence, 1.64, 4, -250,
				UniformsOf(1e-9, 0.0, 0.5, 0.5, 0.9999, 0.5));
			Assert.That(choice, Is.EqualTo("aa"), "exploration is not punished without evidence");
		}

		[Test]
		public void DecayRegressesTowardThePrior()
		{
			var s = Stats(20, 400, 80000);
			s.Decay(0.5);
			Assert.That(s.N, Is.EqualTo(10));
			Assert.That(s.Mean, Is.EqualTo(200).Within(1e-9), "the mean moves halfway toward neutral 0");
			Assert.That(s.M2, Is.EqualTo(20000).Within(1e-6), "m2 shrinks with the square of the discount");

			s.Decay(0.1);
			Assert.That(s.N, Is.EqualTo(1), "floor(N x factor + 0.5) lets evidence fade out");
		}

		[Test]
		public void LearnedParsesBanditScopesAndArms()
		{
			var learned = PlanBanditLearned.Parse(MiniYaml.FromString(@"
BotPlanBandits:
	Personality@any:
		rush: 20 100.5 40000
		turtle: 12 -50 30000
	Personality@td_gdi__vs__td_nod:
		rush: 3 250 9000
	Plan@family_td:
		press: 8 30 12000
", "learned"));

			Assert.That(learned.ScopeCount, Is.EqualTo(3));
			var rush = learned.At("Personality", "rush", "td_gdi__vs__td_nod");
			Assert.That(rush.N, Is.EqualTo(3));
			Assert.That(rush.Mean, Is.EqualTo(250).Within(1e-9));
			Assert.That(rush.M2, Is.EqualTo(9000).Within(1e-6));
			Assert.That(learned.At("Plan", "press", "family_td").N, Is.EqualTo(8));
			Assert.That(learned.At("Personality", "missing", "any").N, Is.EqualTo(0));
		}

		[Test]
		public void LearnedIgnoresMalformedRows()
		{
			var learned = PlanBanditLearned.Parse(MiniYaml.FromString(@"
BotPlanBandits:
	Personality@any:
		good: 5 100 40000
		bad_count: x y z
		negative_n: -3 50 1000
		two_fields: 5 100
	Personality@:
		orphan: 5 100 40000
	Other:
	Personality@other_scope:
		rush: 7 -20 8000
", "learned"));

			var arms = learned.At("Personality", "good", "any");
			Assert.That(arms.N, Is.EqualTo(5));
			Assert.That(learned.At("Personality", "bad_count", "any").N, Is.EqualTo(0));
			Assert.That(learned.At("Personality", "negative_n", "any").N, Is.EqualTo(0));
			Assert.That(learned.At("Personality", "two_fields", "any").N, Is.EqualTo(0));
			Assert.That(learned.At("Personality", "orphan", "").N, Is.EqualTo(0), "empty scope never parses");
		}

		[Test]
		public void PooledChainsMatchupToAny()
		{
			var learned = PlanBanditLearned.Parse(MiniYaml.FromString(@"
BotPlanBandits:
	Personality@any:
		rush: 40 50 200000
	Personality@family_td:
		rush: 20 100 80000
	Personality@td_gdi:
		rush: 10 150 40000
	Personality@td_gdi__vs__td_nod:
		rush: 5 200 10000
", "learned"));

			// matchup n=5 pooled with each parent capped at 8 pseudo-observations:
			// merge order in Pooled is matchup -> faction -> family -> any.
			var expected = Stats(5, 200, 10000);
			expected = PlanBanditArmStats.Merge(expected, Stats(10, 150, 40000).Downweighted(8));
			expected = PlanBanditArmStats.Merge(expected, Stats(20, 100, 80000).Downweighted(8));
			expected = PlanBanditArmStats.Merge(expected, Stats(40, 50, 200000).Downweighted(8));

			var pooled = learned.Pooled("Personality", "rush", "td_gdi", "td_nod", 8);
			Assert.That(pooled.N, Is.EqualTo(expected.N));
			Assert.That(pooled.Mean, Is.EqualTo(expected.Mean).Within(1e-9));
			Assert.That(pooled.M2, Is.EqualTo(expected.M2).Within(1e-6));
		}

		[Test]
		public void PooledWithNoMatchupFallsBackToParents()
		{
			var learned = PlanBanditLearned.Parse(MiniYaml.FromString(@"
BotPlanBandits:
	Personality@any:
		turtle: 16 80 60000
", "learned"));

			var pooled = learned.Pooled("Personality", "turtle", "ra1_allies", "ra1_soviet", 8);
			Assert.That(pooled.N, Is.EqualTo(8), "an unseen matchup reads the downweighted global pool only");
			Assert.That(pooled.Mean, Is.EqualTo(80).Within(1e-9));
		}

		[Test]
		public void EvidenceCountsOwnScopePlaysNotRollUp()
		{
			var learned = PlanBanditLearned.Parse(MiniYaml.FromString(@"
BotPlanBandits:
	Personality@any:
		rush: 40 50 200000
	Personality@td_gdi:
		rush: 10 150 40000
	Personality@td_gdi__vs__td_nod:
		rush: 5 200 10000
", "learned"));

			Assert.That(learned.EvidenceN("Personality", "rush", "td_gdi", "td_nod"), Is.EqualTo(5),
				"the matchup scope's own count, not the pooled/roll-up total");
			Assert.That(learned.EvidenceN("Personality", "rush", "td_gdi", "ra1_allies"), Is.EqualTo(10),
				"an unseen matchup reads the own-faction count");
			Assert.That(learned.EvidenceN("Personality", "turtle", "td_gdi", "td_nod"), Is.EqualTo(0));
		}

		[Test]
		public void EmptyYamlParsesEmpty()
		{
			var learned = PlanBanditLearned.Parse(MiniYaml.FromString("BotPlanBandits:", "learned"));
			Assert.That(learned.ScopeCount, Is.EqualTo(0));
			Assert.That(learned.Pooled("Personality", "rush", "td_gdi", "td_nod", 8).N, Is.EqualTo(0));
		}
	}

	static class ArrayFill
	{
		public static double[] Fill(this double[] a, double v)
		{
			for (var i = 0; i < a.Length; i++)
				a[i] = v;
			return a;
		}
	}
}
