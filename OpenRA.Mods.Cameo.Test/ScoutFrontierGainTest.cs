#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// RM-01 SCOUT-FRONTIER-01 (design d33605f3): golden vectors for the pure gain class, the neutral-setting equivalence with the original
	// pick, the sector cap, the revisit memory, the fog pins on the source, the allocation pin and the cost scaling.
	[TestFixture]
	public sealed class ScoutFrontierGainTest
	{
		sealed class Field : IScoutFrontierField
		{
			public long[] StaleAt;
			public int[] Interest;
			public long[] DangerAt;
			public int[][] Next;
			public CPos[] Centers;
			public bool Grid;
			public long ValueCalls;

			public int Count => StaleAt.Length;
			public bool GridBacked => Grid;
			public long Stale(int i) => StaleAt[i];

			public long Value(int i)
			{
				ValueCalls++;
				return StaleAt[i] <= 0 ? 0 : StaleAt[i] * (1000 + Interest[i]);
			}

			public long Danger(int i) => DangerAt[i];
			public IReadOnlyList<int> Neighbors(int i) => Next[i];
			public CPos Center(int i) => Centers[i];
		}

		// Chain 0-1-2-3, centres 4 cells apart.
		static Field Chain(long[] stale)
		{
			var n = stale.Length;
			var next = new int[n][];
			var centers = new CPos[n];
			for (var i = 0; i < n; i++)
			{
				var list = new List<int>();
				if (i > 0)
					list.Add(i - 1);
				if (i < n - 1)
					list.Add(i + 1);
				next[i] = list.ToArray();
				centers[i] = new CPos(4 * i, 0);
			}

			return new Field { StaleAt = stale, Interest = new int[n], DangerAt = new long[n], Next = next, Centers = centers };
		}

		static ScoutFrontierGain.Tunables T(int weight = 500, int hops = 1, int vision = 5, int prefilter = 32, int cap = 1) =>
			new(weight, hops, vision, 1250, 750, 2, cap, prefilter);

		[Test]
		public void TheGainSumsTheStaleValueInsideTheVisionReach()
		{
			var f = Chain([10, 20, 30, 40]);
			var g = new ScoutFrontierGain();
			g.BeginCover(f.Count, 32);
			Assert.That(g.Gain(f, 1, T()), Is.EqualTo(60L * 1000), "regions 0,1,2 inside hops 1 and 5 cells");
			Assert.That(g.Gain(f, 1, T(hops: 2)), Is.EqualTo(60L * 1000), "region 3 is 8 cells from region 1: outside the vision");
			Assert.That(g.Gain(f, 1, T(hops: 2, vision: 9)), Is.EqualTo(100L * 1000), "wider vision reaches region 3 through two hops");
			Assert.That(g.Gain(f, 0, T()), Is.EqualTo(30L * 1000), "an end region sees itself and one neighbour");
		}

		[Test]
		public void ARegionAnotherScoutAlreadyCoversAddsNoGain()
		{
			var f = Chain([10, 20, 30, 40]);
			var g = new ScoutFrontierGain();
			g.BeginCover(f.Count, 32);
			g.Cover(f, 2, T());
			Assert.That(g.Gain(f, 0, T()), Is.EqualTo(10L * 1000), "region 1 is covered by the scout on region 2");
			g.BeginCover(f.Count, 32);
			Assert.That(g.Gain(f, 0, T()), Is.EqualTo(30L * 1000), "a new cover set forgets it");
		}

		[Test]
		public void TheHeadingBiasForwardBackAndNone()
		{
			var t = T();
			Assert.That(ScoutFrontierGain.HeadingPermille(true, new CVec(5, 0), new CVec(3, 1), t), Is.EqualTo(1250));
			Assert.That(ScoutFrontierGain.HeadingPermille(true, new CVec(5, 0), new CVec(-3, 1), t), Is.EqualTo(750));
			Assert.That(ScoutFrontierGain.HeadingPermille(true, new CVec(5, 0), new CVec(0, 4), t), Is.EqualTo(1000), "perpendicular");
			Assert.That(ScoutFrontierGain.HeadingPermille(false, new CVec(5, 0), new CVec(3, 1), t), Is.EqualTo(1000), "no previous heading");
		}

		[Test]
		public void TheCombinedScoreAppliesGainHeadingRevisitAndTheAdditiveTerms()
		{
			Assert.That(ScoutFrontierGain.Combine(1000, 200, 500, 1250, false, 2, 30, 5), Is.EqualTo(1340), "(1000+100) x 1.25 - 30 - 5");
			Assert.That(ScoutFrontierGain.Combine(1000, 200, 500, 1000, true, 2, 30, 5), Is.EqualTo(515), "(1000+100) / 2 - 35");
			Assert.That(ScoutFrontierGain.Combine(1000, 999999, 0, 1000, false, 2, 0, 0), Is.EqualTo(1000), "weight 0 ignores the gain");
		}

		[Test]
		public void AVisitThatGaveNoSightHalvesAndAnUnroutableTargetCoolsDown()
		{
			var m = new ScoutFrontierMemory();
			m.RecordVisit(3, 2500, 2600, 400, 1000, 100);
			Assert.That(m.Halved(3, 2700), Is.True, "still as stale as at the assignment: halved");
			Assert.That(m.CoolingDown(3, 1001), Is.False);
			m.RecordVisit(3, 2500, 10, 400, 1100, 100);
			Assert.That(m.Halved(3, 2700), Is.False, "seen since: forgotten");

			m.RecordVisit(5, 2500, 2500, 40, 2000, 100);
			Assert.That(m.CoolingDown(5, 2099), Is.True, "idle 40 ticks after the order, no sight: unroutable, retry in 100");
			Assert.That(m.CoolingDown(5, 2100), Is.False);
			Assert.That(m.Halved(5, 2500), Is.False, "the unroutable case is a cooldown, not a halving");
			m.Clear();
			Assert.That(m.Count, Is.EqualTo(0));
		}

		[Test]
		public void TheGainBeatsAnIsolatedStaleRegionAndTheTieGoesToTheLowerIndex()
		{
			// Regions 1 and 3 equally stale; 3's neighbour 2 is also stale, 1's neighbours 0 and 2... use a separate chain.
			var f = Chain([0, 100, 0, 0, 100, 100]);
			var g = new ScoutFrontierGain();
			g.BeginCover(f.Count, 32);
			var pick = g.Pick(f, new CPos(0, 0), false, CVec.Zero, new HashSet<int>(), null, 0, T(vision: 5));
			Assert.That(pick, Is.EqualTo(4), "region 4 has a stale neighbour (5): gain wins over the closer isolated region 1");
			Assert.That(g.LastGain, Is.GreaterThan(0));

			var tie = Chain([0, 0, 0, 0]);
			tie.StaleAt = [50, 50, 50, 50];
			g.BeginCover(tie.Count, 32);
			Assert.That(g.Pick(tie, new CPos(0, 0), false, CVec.Zero, new HashSet<int>(), null, 0, T(weight: 0)), Is.EqualTo(0), "all else equal: lowest index");
		}

		[Test]
		public void TheCooldownAndTheHalvingChangeThePick()
		{
			var f = Chain([100, 100, 0, 0]);
			var g = new ScoutFrontierGain();
			var m = new ScoutFrontierMemory();
			g.BeginCover(f.Count, 32);
			Assert.That(g.Pick(f, new CPos(0, 0), false, CVec.Zero, new HashSet<int>(), m, 0, T(weight: 0)), Is.EqualTo(0));
			m.RecordVisit(0, 100, 100, 10, 0, 100);
			g.BeginCover(f.Count, 32);
			Assert.That(g.Pick(f, new CPos(0, 0), false, CVec.Zero, new HashSet<int>(), m, 50, T(weight: 0)), Is.EqualTo(1), "region 0 is cooling down");
			Assert.That(g.Pick(f, new CPos(0, 0), false, CVec.Zero, new HashSet<int>(), m, 100, T(weight: 0)), Is.EqualTo(0), "cooldown over");
		}

		[Test]
		public void InAGridIndexSpaceRegionsNextToAnotherScoutsTargetAreSkipped()
		{
			var f = Chain([0, 100, 100, 100, 100]);
			f.Grid = true;
			var g = new ScoutFrontierGain();
			g.BeginCover(f.Count, 32);
			g.Cover(f, 2, T());
			var taken = new HashSet<int> { 2 };
			Assert.That(g.Pick(f, new CPos(0, 0), false, CVec.Zero, taken, null, 0, T(weight: 0)), Is.EqualTo(4), "1 and 3 are within one hop of the target on 2");

			f.Grid = false;
			g.BeginCover(f.Count, 32);
			g.Cover(f, 2, T());
			Assert.That(g.Pick(f, new CPos(0, 0), false, CVec.Zero, taken, null, 0, T(weight: 0)), Is.EqualTo(1), "zone-backed: a region is a sector, only the same region is taken");
		}

		[Test]
		public void WithTheNeutralSettingTheFrontierPickEqualsTheOriginalPickOver5000SeededInputs()
		{
			var rng = new Random(20261127);
			var g = new ScoutFrontierGain();
			for (var run = 0; run < 5000; run++)
			{
				var n = rng.Next(1, 40);
				var f = Chain(new long[n]);
				var taken = new HashSet<int>();
				for (var i = 0; i < n; i++)
				{
					f.StaleAt[i] = rng.Next(0, 4) == 0 ? 0 : rng.Next(1, 5000);
					f.Interest[i] = rng.Next(0, 6) == 0 ? rng.Next(0, 4000) : 0;
					f.DangerAt[i] = rng.Next(0, 5) == 0 ? rng.Next(0, 3000) : 0;
					if (rng.Next(0, 6) == 0)
						taken.Add(i);
				}

				var from = new CPos(rng.Next(0, 200), rng.Next(0, 200));
				var expected = -1;
				var best = long.MinValue;
				for (var i = 0; i < n; i++)
				{
					if (taken.Contains(i) || f.StaleAt[i] <= 0)
						continue;

					var score = f.StaleAt[i] * (1000 + f.Interest[i]) - f.DangerAt[i] - (f.Centers[i] - from).Length / 64;
					if (score > best || (score == best && expected >= 0 && i < expected))
					{
						expected = i;
						best = score;
					}
				}

				g.BeginCover(n, 64);
				var actual = g.Pick(f, from, false, CVec.Zero, taken, null, 0, T(weight: 0, prefilter: 64, cap: 0));
				Assert.That(actual, Is.EqualTo(expected), $"run {run}");
			}
		}

		[Test]
		public void APickAllocatesNothingAfterWarmUpAndItsCostDoesNotGrowWithTheRegionCount()
		{
			foreach (var n in new[] { 50, 500, 2000 })
			{
				var stale = new long[n];
				for (var i = 0; i < n; i++)
					stale[i] = 1 + (i * 37) % 2500;

				var f = Chain(stale);
				var g = new ScoutFrontierGain();
				var taken = new HashSet<int>();
				var t = T(prefilter: 32);
				g.BeginCover(n, 32);
				g.Pick(f, new CPos(0, 0), true, new CVec(3, 0), taken, null, 0, t);

				f.ValueCalls = 0;
				var before = GC.GetAllocatedBytesForCurrentThread();
				for (var k = 0; k < 20; k++)
				{
					g.BeginCover(n, 32);
					g.Cover(f, 7, t);
					g.Pick(f, new CPos(0, 0), true, new CVec(3, 0), taken, null, 0, t);
				}

				var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
				Assert.That(allocated, Is.EqualTo(0), $"{n} regions: no allocation per pick");

				// Value() calls beyond the one-per-region scan are bounded by the prefilter, not by the region count.
				var extra = f.ValueCalls / 20 - n;
				Assert.That(extra, Is.LessThanOrEqualTo(32 * 8), $"{n} regions: gain work is bounded by K");
			}
		}

		static string Source(string relative) =>
			File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/Cameo/Traits/BotModules", relative)).Replace("\r\n", "\n");

		[Test]
		public void TheGainClassIsFogHonestAndDeterministic()
		{
			var code = Source("ScoutFrontierGain.cs");
			foreach (var banned in new[] { "Actor", "Player", "World", "WinState", "TraitsImplementing<", "EnemyProfile", "System.Random", "BotRng", "Linq", "=>  new HashSet", "Dictionary<string" })
				Assert.That(code, Does.Not.Contain(banned), banned);

			Assert.That(code, Does.Not.Contain("using System.Linq"));
		}

		[Test]
		public void TheSwitchIsOffByDefaultAndTheOriginalPickBodyIsUntouched()
		{
			var scout = Source("ScoutBotModule.cs");
			Assert.That(scout, Does.Contain("public readonly bool ScoutFrontierEnabled = false;"));
			Assert.That(scout, Does.Contain("bool ScoutFrontierOn => Info.ScoutFrontierEnabled && !ablateScoutFrontier.Off(player, AblationIds.ScoutFrontier);"));
			Assert.That(scout, Does.Contain("var score = (long)stale * (1000 + interest(i)) - danger(i) - DistancePenalty(regions, from, i);"), "the off path formula");
			Assert.That(scout, Does.Contain("return PickScoutRegion(regions, from, taken,\n\t\t\t\t\ti => influence.Staleness[i]"));
			Assert.That(AblationSpecIds(), Does.Contain("scout_frontier"));
		}

		static IReadOnlyCollection<string> AblationSpecIds() => OpenRA.Mods.Cameo.Dev.AblationSpec.RegistryIds();
	}
}
