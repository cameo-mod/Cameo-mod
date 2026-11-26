#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-BOTTICK-01 (Opus 11-26, exact, no switch): the counter score is cached until the observed enemy memory changes, and the DEV shadow check that compares it with the
	// untouched computation is compiled out of Release. Frontrunner review note on 810e2cfed: pin every mutation site's version bump.
	[TestFixture]
	public sealed class PerfBotTick01Test
	{
		const string Counter = "mods/CA/Traits/BotModules/BotModuleLogic/AdaptiveCounterProduction.cs";
		const string Unit = "mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static int CountOf(string text, string needle)
		{
			var n = 0;
			for (var at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
				n++;

			return n;
		}

		// The source as Release compiles it: every #if CAMEO_DEV_LOGGING ... #endif block removed (none of them has an #else).
		static string ReleaseView(string source) => Regex.Replace(source, @"#if CAMEO_DEV_LOGGING\n.*?#endif\n", "", RegexOptions.Singleline);

		[Test]
		public void EveryWriteToTheObservedMemoryBumpsTheVersion()
		{
			var s = Source(Counter);

			// the writes: two clears (ObserveFogOnly, Load), two smooths (Observe, ObserveFogOnly), one indexer store (Load); Smooth itself writes only the dictionary it is given
			Assert.That(CountOf(s, "observedEnemyValue.Clear()"), Is.EqualTo(2));
			Assert.That(CountOf(s, "Smooth(observedEnemyValue, sample);"), Is.EqualTo(2));
			Assert.That(CountOf(s, "observedEnemyValue["), Is.EqualTo(1));
			Assert.That(CountOf(s, "observedEnemyValue.Remove(") + CountOf(s, "observedEnemyValue.Add("), Is.Zero);
			Assert.That(s, Does.Contain("public IReadOnlyDictionary<string, int> ObservedEnemyValue => observedEnemyValue;"), "the only outside view is read-only");

			// each write is followed by the bump, and there are no other bumps
			Assert.That(s, Does.Contain("\t\t\tSmooth(observedEnemyValue, sample);\n\t\t\tobservedVersion++;\n\t\t}"), "Observe");
			Assert.That(s, Does.Contain("\t\t\t\tobservedEnemyValue.Clear();\n\t\t\t\tobservedVersion++;\n\t\t\t}"), "ObserveFogOnly: the first fog-only call's clear");
			Assert.That(s, Does.Contain("\t\t\t{\n\t\t\t\tSmooth(observedEnemyValue, sample);\n\t\t\t\tobservedVersion++;\n\t\t\t}"), "ObserveFogOnly: the smooth");
			Assert.That(s, Does.Contain("\t\t\t\t\tobservedEnemyValue[t[i]] = v[i];\n\n\t\t\t\tobservedVersion++;"), "Load: after the clear and the stores");
			Assert.That(CountOf(s, "observedVersion++;"), Is.EqualTo(4));

			// a version change empties the score cache before any lookup
			Assert.That(s, Does.Contain("if (scoreCacheVersion != observedVersion)\n\t\t\t{\n\t\t\t\tscoreCache.Clear();\n\t\t\t\tscoreCacheVersion = observedVersion;\n\t\t\t}\n\n\t\t\tif (scoreCache.TryGetValue(candidate.Name, out var cached))"));
		}

		[Test]
		public void TheShadowCheckIsDevOnly()
		{
			var s = Source(Counter);
			Assert.That(s, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\t\tif (++scoreShadowCount % ScoreShadowEvery == 0)"));
			Assert.That(s, Does.Contain("PERF-BOTTICK-01 SHADOW-MISMATCH counter-score"));

			var release = ReleaseView(s);
			foreach (var devOnly in new[] { "ScoreFresh", "scoreShadowCount", "ScoreShadowEvery", "SHADOW-MISMATCH" })
				Assert.That(release, Does.Not.Contain(devOnly), devOnly + " is compiled out of Release");

			Assert.That(release, Does.Contain("return scoreCache[candidate.Name] = ScoreCached(candidate);"), "Release scores through the cache");
		}

		[Test]
		public void ThePerRollScoringDoesNotAllocateANewListOrSortBuffer()
		{
			var s = Source(Counter);
			Assert.That(s, Does.Not.Contain("candidates.OrderBy("), "Choose sorts into a reused buffer");
			Assert.That(s, Does.Contain("while (at > 0 && string.CompareOrdinal(sorted[at - 1].Name, candidate.Name) > 0)"), "stable ordinal insertion: OrderBy's order");

			var u = Source(Unit);
			Assert.That(u, Does.Not.Contain("pool.Select(a => counters.CounterScore(a)).ToList()"));
			Assert.That(u, Does.Contain("scores = rollScores;\n\t\t\t\tscores.Clear();"));
		}
	}
}
