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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// B4 OBSERVED-STYLE phase d (DESIGN_OBSERVED_STYLE_02 sections 1-4, ADD1-ADD4 unchanged): the integer classifier, IBotObservedStyle, the enable constant and the exact
	// C = 0 branch of the matchup value. The constant stays false: nothing here changes behaviour.
	// OBS-STYLE-PROVIDER-01: the B4 phase d tests (f14b8dac6 ObservedStyleD4Test) of the classifier and the table; StyleMatchup is not ported (LA02 aggregates).
	[TestFixture]
	public sealed class ObservedStyleProviderClassifierTest
	{
		const string Dir = "OpenRA.Mods.Cameo/Traits/BotModules/ObservedStyle/";
		static readonly string[] PureFiles = { "StyleClassifier.cs", "StyleTable.cs" };

		static string Source(string file)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, file)).Replace("\r\n", "\n");
		}

		static string Code(string file) => Regex.Replace(Regex.Replace(Source(Dir + file), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\n]*", string.Empty);

		static StyleReading Reading(int confidence, params int[] posterior)
		{
			var r = new StyleReading();
			r.Set(posterior.Length == 0 ? StyleReading.UniformPermille : posterior, confidence, 0);
			return r;
		}

		// ---- the enable constant and the interface -------------------------------------------------------------------------------------------------------

		[Test]
		public void TheEnableConstantIsFalseAndAppearsOnce()
		{
			Assert.That(ObservedStyleSettings.Enabled, Is.False);
			var code = Code("StyleReading.cs");
			Assert.That(Regex.Matches(code, @"\bEnabled\b").Count, Is.EqualTo(1));
			Assert.That(code, Does.Contain("public static readonly bool Enabled = false;"));
		}

		[Test]
		public void ThePureClassifierFilesCannotReachTheForbiddenInputs()
		{
			string[] forbidden =
			{
				"EnemyProfile", "WinState", ".Alive", "IBotCurrentPersonality", "BotPersonalityController", "PinnedPersonalities", "TraitsImplementing<", "World.Actors", "FrozenActorLayer",
				".Owner.PlayerName", "FactionName", "Faction.InternalName", "Player.Name", "Remembered(", "OpenRA.Player", "personality", "TypeName", "Random", "DateTime", "Stopwatch",
			};

			foreach (var file in PureFiles)
			{
				var code = Code(file);
				foreach (var f in forbidden)
					Assert.That(code, Does.Not.Contain(f), $"{file} must not contain '{f}'");

				Assert.That(Regex.IsMatch(code, @"(?<![A-Za-z0-9_.])(Actor|Player|World)\s+[a-z_]\w*\s*[,)]"), Is.False, $"{file}: no Actor, Player or World parameter");
			}

			foreach (var t in new[] { typeof(StyleClassifier), typeof(StyleTable) })
				foreach (var method in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
					foreach (var p in method.GetParameters())
						Assert.That(p.ParameterType == typeof(OpenRA.Player) || p.ParameterType == typeof(Actor) || p.ParameterType == typeof(World), Is.False, $"{t.Name}.{method.Name}({p.Name})");
		}

		// ---- section 4: C = 0 is exactly base3 -----------------------------------------------------------------------------------------------------------

		// ---- the classifier --------------------------------------------------------------------------------------------------------------------------------

		static FogStyleActor Seen(uint id, int tick) => new FogStyleActor { ActorId = id, Cell = new CPos(10, 10), Value = 1000, LastSeenTick = tick, Building = true, BaseBuilding = true };

		// For class c: the most likely bucket of every informative feature under the table, coverage 10 (everything seen recently).
		static int[] ClassFeatures(StyleTable table, int cls)
		{
			var f = Enumerable.Repeat(StyleFeatures.Unknown, StyleFeatures.Count).ToArray();
			for (var i = 0; i < StyleFeatures.Count; i++)
			{
				if (!table.Informative(i))
					continue;

				var best = 0;
				for (var b = 1; b < StyleTable.Buckets; b++)
					if (table.Ll(cls, i, b) > table.Ll(cls, i, best))
						best = b;

				f[i] = best;
			}

			f[StyleFeatures.CoverageIndex] = 10;
			return f;
		}

		static StyleReading Run(StyleClassifier c, int[] features, int fromTick, int updates, Func<int, FogStyleActor[]> actorsAt)
		{
			var tracker = new StyleTracker();
			tracker.Update(actorsAt(fromTick), ReadOnlySpan<CPos>.Empty, fromTick);
			for (var i = 0; i < updates; i++)
			{
				var tick = fromTick + i * 75;
				c.Update(features, actorsAt(tick), tracker, tick);
			}

			return c.Reading;
		}

		[Test]
		public void AColdEnemyIsThePriorWithConfidenceZero()
		{
			var table = StyleTable.HandPrior;
			var c = new StyleClassifier(table);

			// The hidden-actor case at classifier level: an empty fog table gives all features Unknown and no actors, so no evidence and no sighting.
			var tracker = new StyleTracker();
			var unknown = Enumerable.Repeat(StyleFeatures.Unknown, StyleFeatures.Count).ToArray();
			for (var tick = 0; tick < 20000; tick += 75)
				c.Update(unknown, ReadOnlySpan<FogStyleActor>.Empty, tracker, tick);

			Assert.That(c.Reading.ConfidencePermille, Is.Zero);
			Assert.That(c.Reading.PosteriorPermille.ToArray(), Is.EqualTo(StyleReading.UniformPermille));

			// A first sighting is not yet mature: still the prior, C = 0.
			var f = ClassFeatures(table, 0);
			var c2 = new StyleClassifier(table);
			var t2 = new StyleTracker();
			t2.Update(new[] { Seen(1, 100) }, ReadOnlySpan<CPos>.Empty, 100);
			c2.Update(f, new[] { Seen(1, 100) }, t2, 100);
			Assert.That(c2.Reading.ConfidencePermille, Is.Zero);
			Assert.That(c2.Reading.PosteriorPermille.ToArray(), Is.EqualTo(StyleReading.UniformPermille));
		}

		[Test]
		public void EachClassProfileIsRecognisedAsItsOwnStyleWithPositiveConfidence()
		{
			var table = StyleTable.HandPrior;
			for (var cls = 0; cls < StyleReading.Classes; cls++)
			{
				var features = ClassFeatures(table, cls);
				var c = new StyleClassifier(table);
				var reading = Run(c, features, 0, 120, t => new[] { Seen(1, t) });
				var p = reading.PosteriorPermille.ToArray();
				Assert.That(Array.IndexOf(p, p.Max()), Is.EqualTo(cls), $"{StyleReading.StyleNames[cls]}: {string.Join(",", p)}");
				Assert.That(reading.ConfidencePermille, Is.GreaterThan(0), StyleReading.StyleNames[cls]);
			}
		}

		[Test]
		public void PosteriorsAreAlwaysAValidDistributionAndTheClassifierIsDeterministic()
		{
			var table = StyleTable.HandPrior;
			var rng = new Random(99);
			var a = new StyleClassifier(table);
			var b = new StyleClassifier(table);
			var ta = new StyleTracker();
			var tb = new StyleTracker();
			var actors = new[] { Seen(1, 0) };
			ta.Update(actors, ReadOnlySpan<CPos>.Empty, 0);
			tb.Update(actors, ReadOnlySpan<CPos>.Empty, 0);
			for (var i = 0; i < 400; i++)
			{
				var tick = i * 75;
				var f = Enumerable.Range(0, StyleFeatures.Count).Select(_ => rng.Next(-1, 20)).ToArray();
				a.Update(f, actors, ta, tick);
				b.Update(f, actors, tb, tick);

				var p = a.Reading.PosteriorPermille.ToArray();
				Assert.That(p.Sum(), Is.EqualTo(1000));
				Assert.That(p.Min(), Is.GreaterThanOrEqualTo(StyleClassifier.MinPosterior));
				Assert.That(p.Max(), Is.LessThanOrEqualTo(1000 - 5 * 5));
				Assert.That(a.Reading.ConfidencePermille, Is.InRange(0, 1000));
				Assert.That(b.Reading.PosteriorPermille.ToArray(), Is.EqualTo(p));
				Assert.That(b.Reading.ConfidencePermille, Is.EqualTo(a.Reading.ConfidencePermille));
			}
		}

		[Test]
		public void ConfidenceFadesOnePercentPerSecondOfStaleSightingAndIsGoneAfterHundredSeconds()
		{
			var table = StyleTable.HandPrior;
			var f = ClassFeatures(table, 2);
			var c = new StyleClassifier(table);
			var tracker = new StyleTracker();
			tracker.Update(new[] { Seen(1, 0) }, ReadOnlySpan<CPos>.Empty, 0);
			for (var tick = 0; tick <= 6000; tick += 75)
				c.Update(f, new[] { Seen(1, tick) }, tracker, tick);

			var fresh = c.Reading.ConfidencePermille;
			Assert.That(fresh, Is.GreaterThan(300));

			// Same snapshot, but the newest sighting is 50 s (1250 ticks) old: about half the confidence remains.
			c.Update(f, new[] { Seen(1, 6075 - 1250) }, tracker, 6075);
			Assert.That(c.Reading.ConfidencePermille, Is.EqualTo(fresh * 500 / 1000).Within(fresh / 20 + 2));

			// 100 s old: nothing left.
			c.Update(f, new[] { Seen(1, 6150 - 2500) }, tracker, 6150);
			Assert.That(c.Reading.ConfidencePermille, Is.Zero);
		}

		[Test]
		public void TheFixedPointTablesMatchTheirReferenceValues()
		{
			var flags = BindingFlags.NonPublic | BindingFlags.Static;
			var exp = (int[])typeof(StyleClassifier).GetField("ExpQ16", flags).GetValue(null);
			for (var k = 0; k < exp.Length; k++)
				Assert.That(exp[k], Is.EqualTo((int)Math.Round(Math.Exp(-0.05 * k) * 65536)).Within(2), $"exp[{k}]");

			var h = (int[])typeof(StyleClassifier).GetField("NegPLogP", flags).GetValue(null);
			Assert.That(new[] { h[1], h[5], h[100], h[167], h[500], h[995] }, Is.EqualTo(new[] { 6908, 26492, 230259, 298890, 346574, 4987 }));
			Assert.That(h[0], Is.Zero);
			Assert.That(h[1000], Is.Zero);

			var step = (long)typeof(StyleClassifier).GetField("DecayStepQ30", flags).GetRawConstantValue();
			Assert.That(step, Is.EqualTo((long)Math.Round(Math.Pow(2, -25.0 / 6000) * (1 << 30))).Within(2));
			var ratio = (long)typeof(StyleClassifier).GetField("ExpRatioQ30", flags).GetRawConstantValue();
			Assert.That(ratio, Is.EqualTo((long)Math.Round(Math.Exp(-0.05) * (1 << 30))).Within(2));
		}

		[Test]
		public void TheHandPriorTableIsStableAndCarriesNoPersonalityName()
		{
			var table = StyleTable.HandPrior;
			Assert.That(table.Version, Does.Match("^[0-9A-F]{8}$"));
			Assert.That(StyleTable.HandPrior, Is.SameAs(table));
			for (var f = 0; f < StyleFeatures.Count; f++)
				for (var c = 0; c < StyleReading.Classes; c++)
					for (var b = 0; b < StyleTable.Buckets; b++)
						Assert.That(table.Ll(c, f, b), Is.InRange(StyleTable.Floor, 0));

			// F10 is a confidence input and the faction-tracking shares are left out: no evidence from them.
			foreach (var name in new[] { "f10_coverage", "f10_since_building", "f7_air_share", "f7_naval_share", "f9_harv_per_ref", "f9_ref_per_yard" })
				Assert.That(table.Informative(Array.IndexOf(StyleFeatures.Names, name)), Is.False, name);
		}

		// ---- the DEV row ----------------------------------------------------------------------------------------------------------------------------------
	}
}
