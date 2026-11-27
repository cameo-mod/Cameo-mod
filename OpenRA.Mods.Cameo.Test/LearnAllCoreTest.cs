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
using NUnit.Framework;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// LEARN-ALL-02 P1a: the integer contract (G x F_own x E_eff x S_obs)^(1/4), the exact three-axis fallback, the bounded log step,
	// no-evidence = keep, never reset, per-personality tables that start from the global values, one step per game (idempotent), every seat
	// of a game combined in that one step. The step/trainer lives in OpenRA.Mods.Cameo.Dev (DEV build only); this fixture drives it.
	[TestFixture]
	public sealed class LearnAllCoreTest
	{
		static SeatObservation Seat(string personality, string own, string enemy, string style, string main, double? signal) =>
			new(personality, own, [new EnemyObservation(enemy, style)], main, signal);

		static IReadOnlyList<SeatObservation> One(string personality, string own, string enemy, double? signal, string style = null) =>
			[Seat(personality, own, enemy, style, enemy, signal)];

		[Test]
		public void TheDecisionValueIsTheFourthRootOfTheFourAxes()
		{
			Assert.That(LearnAllMath.Resolve(1000, 1000, 1000, 1000), Is.EqualTo(1000));
			Assert.That(LearnAllMath.Resolve(1000, 1000, 1000, 2000), Is.EqualTo(1189), "(1000^3 x 2000)^(1/4) = 1189.2");
			Assert.That(LearnAllMath.Resolve(500, 1000, 2000, 4000), Is.EqualTo(1414), "(4e12)^(1/4) = 1414.2");
			Assert.That(LearnAllMath.Resolve(1000, 1000, 1000, 16000), Is.EqualTo(2000), "exact fourth root");
		}

		[Test]
		public void AnUnseenStyleIsTheExactThreeAxisResult()
		{
			Assert.That(LearnAllMath.Resolve(1000, 800, 1250, 0), Is.EqualTo(1000), "(1e9)^(1/3) exactly");
			Assert.That(LearnAllMath.Resolve(1000, 1200, 800, 0), Is.EqualTo(986), "(9.6e8)^(1/3) = 986.49 rounds to the nearest root");
			Assert.That(LearnAllMath.Resolve(1000, 1200, 800, -5), Is.EqualTo(986), "a non-positive style is 'unseen'");
		}

		[Test]
		public void AFreshStyleIsSeededFromTheThreeAxisResultSoTheDecisionDoesNotJump()
		{
			var seed = LearnAllMath.SeedStyle(1000, 1200, 800);
			Assert.That(seed, Is.EqualTo(986));
			var before = LearnAllMath.Resolve(1000, 1200, 800, 0);
			var after = LearnAllMath.Resolve(1000, 1200, 800, seed);
			Assert.That(Math.Abs(after - before), Is.LessThanOrEqualTo(1), "seeded style = same decision value (to a rounding permille)");
		}

		[Test]
		public void ATeamEnemyIsTheMeanOfAllEnemyPlayersAndTheMainTarget()
		{
			Assert.That(LearnAllMath.TeamEnemy([1000, 4000], 2000), Is.EqualTo(2000));
			Assert.That(LearnAllMath.TeamEnemy([1000, 4000], 1000), Is.EqualTo(1414), "sqrt(2000 x 1000)");
			Assert.That(LearnAllMath.TeamEnemy([800], 800), Is.EqualTo(800), "one enemy is exactly its own value");
			Assert.That(LearnAllMath.TeamEnemy([], 1500), Is.EqualTo(1500));
			var eleven = new int[11];
			Array.Fill(eleven, 20000);
			Assert.That(LearnAllMath.TeamEnemy(eleven, 20000), Is.EqualTo(20000), "a 6v6 product does not overflow");
		}

		[Test]
		public void TheStepIsBoundedInLogSpaceAndQuantizedToPermille()
		{
			Assert.That(LearnAllStep.Next(1000, 1.0), Is.EqualTo(1041));
			Assert.That(LearnAllStep.Next(1000, -1.0), Is.EqualTo(961));
			Assert.That(LearnAllStep.Next(1000, 5.0), Is.EqualTo(1041), "the signal is clipped to [-1, 1]");
			Assert.That(LearnAllStep.Next(20000, 1.0), Is.EqualTo(20000), "the store clamp holds");
			Assert.That(LearnAllStep.Next(50, -1.0), Is.EqualTo(50));
			Assert.That(LearnAllStep.Next(10, null), Is.EqualTo(50), "an out-of-domain stored value is clamped, never zero");
		}

		[Test]
		public void NoEvidenceKeepsThePreviousEstimateAndInventsNoOutcome()
		{
			Assert.That(LearnAllStep.Next(1234, null), Is.EqualTo(1234));
			Assert.That(LearnAllStep.Next(1234, double.NaN), Is.EqualTo(1234), "an undefined denominator is no evidence, not zero");
			Assert.That(LearnAllStep.Next(1234, double.PositiveInfinity), Is.EqualTo(1234));

			var table = new LearnTable();
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", One("rush", "allies", "soviet", 1.0)), Is.True);
			var kept = table.Resolve("rush", "k", "allies", ["soviet"], "soviet", null);
			Assert.That(LearnAllTrainer.ApplyGame(table, "g2", "k", One("rush", "allies", "soviet", null)), Is.False);
			Assert.That(LearnAllTrainer.ApplyGame(table, "g2", "k", One("rush", "allies", "soviet", double.NaN)), Is.False);
			Assert.That(table.Resolve("rush", "k", "allies", ["soviet"], "soviet", null), Is.EqualTo(kept));
			Assert.That(table.AppliedGames, Does.Not.Contain("g2|rush|k"), "a no-evidence call does not consume the game id");
			Assert.That(LearnAllTrainer.ApplyGame(table, "g2", "k", One("rush", "allies", "soviet", 1.0)), Is.True, "evidence later in the same game still counts once");
		}

		[Test]
		public void NothingLearnedIsNeutral()
		{
			var table = new LearnTable();
			Assert.That(table.Resolve("rush", "unknown", "allies", ["soviet"], "soviet", "turtle"), Is.EqualTo(1000));
			Assert.That(table.Resolve(null, null, null, null, null, null), Is.EqualTo(1000));
		}

		[Test]
		public void OneGameStepsEveryAxisOnceAndNewKeysStartFromTheirPrior()
		{
			var table = new LearnTable();
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", One("any", "allies", "soviet", 1.0, "boom")), Is.True);
			var axes = table.Find("any", "k");
			Assert.That(axes.Global, Is.EqualTo(1041));
			Assert.That(axes.Own("allies"), Is.EqualTo(1041), "F_own started at G (1000) and took one step");
			Assert.That(axes.Enemy("soviet"), Is.EqualTo(1041), "E started at G and took one step");
			Assert.That(axes.Style["boom"], Is.EqualTo(1041), "S_obs started at the three-axis result (1000) and took one step");
		}

		[Test]
		public void ASeededStyleStartsFromTheThreeAxisResultNotFromG()
		{
			var table = new LearnTable();
			var axes = table.Install("any", "k");
			axes.Global = 1000;
			axes.OwnFaction["allies"] = 1200;
			axes.EnemyFaction["soviet"] = 800;
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", One("any", "allies", "soviet", 0.0, "boom")), Is.True);
			Assert.That(table.Find("any", "k").Style["boom"], Is.EqualTo(986), "seed (1000 x 1200 x 800)^(1/3), zero signal = the seed itself");
		}

		[Test]
		public void TheStyleSeedIsThePreStepTeamEnemyWithTheActualMainTarget()
		{
			static LearnTable Preset()
			{
				var t = new LearnTable();
				var a = t.Install("any", "k");
				a.EnemyFaction["nod"] = 1000;
				a.EnemyFaction["allies"] = 4000;
				return t;
			}

			EnemyObservation[] both = [new("nod", "boom"), new("allies", "boom")];
			var onNod = Preset();
			LearnAllTrainer.ApplyGame(onNod, "g1", "k", [new SeatObservation("any", "gdi", both, "nod", 0.0)]);
			Assert.That(onNod.Find("any", "k").Style["boom"], Is.EqualTo(LearnAllMath.GeoMean(1000, 1000, 1414)), "E_eff = geo(geo(1000, 4000), nod 1000) = 1414");

			var onAllies = Preset();
			LearnAllTrainer.ApplyGame(onAllies, "g1", "k", [new SeatObservation("any", "gdi", both, "allies", 0.0)]);
			Assert.That(onAllies.Find("any", "k").Style["boom"], Is.EqualTo(LearnAllMath.GeoMean(1000, 1000, 2828)), "the seed follows the main target the decision used");
		}

		[Test]
		public void TheUpdateBudgetIsDividedAcrossEnemySeatsByWeight()
		{
			var table = new LearnTable();
			var enemies = new EnemyObservation[] { new("soviet", null, 1), new("soviet", null, 1), new("allies", null, 2) };
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", [new SeatObservation("any", "gdi", enemies, "allies", 1.0)]), Is.True);
			var axes = table.Find("any", "k");
			Assert.That(axes.Enemy("soviet"), Is.EqualTo(1020), "half the budget: exp(0.04 x 0.5)");
			Assert.That(axes.Enemy("allies"), Is.EqualTo(1020));
			Assert.That(axes.Global, Is.EqualTo(1041), "G and F_own take the whole signal");
		}

		[Test]
		public void EverySeatOfAGameCountsInOneStepNotOnlyTheFirst()
		{
			var table = new LearnTable();
			var seats = new[] { Seat("rush", "gdi", "nod", null, "nod", 1.0), Seat("turtle", "allies", "nod", null, "nod", -1.0), Seat("rush", "gdi", "nod", null, "nod", 1.0) };
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", seats), Is.True);
			Assert.That(table.Find("rush", "k").Global, Is.EqualTo(1041), "both rush seats agree: one step, not two");
			Assert.That(table.Find("turtle", "k").Global, Is.EqualTo(961), "the second personality is not dropped by a first-seat game key");
			Assert.That(table.Find("any", "k").Global, Is.EqualTo(1013), "global = mean(+1, -1, +1) = 1/3: 1000 x exp(0.04/3)");
			Assert.That(table.Find("any", "k").Own("gdi"), Is.EqualTo(1041));
			Assert.That(table.Find("any", "k").Own("allies"), Is.EqualTo(961));
			Assert.That(table.AppliedGames, Is.EquivalentTo(new[] { "g1|any|k", "g1|rush|k", "g1|turtle|k" }));
		}

		[Test]
		public void TheSeatOrderAndANoEvidenceSeatDoNotChangeTheResult()
		{
			var a = Seat("rush", "gdi", "nod", "boom", "nod", 0.3);
			var b = Seat("rush", "allies", "soviet", "rush", "soviet", -0.7);
			var c = Seat("turtle", "gdi", "nod", "boom", "nod", 0.9);
			var dead = Seat("rush", "soviet", "nod", null, "nod", double.NaN);

			var t1 = new LearnTable();
			LearnAllTrainer.ApplyGame(t1, "g1", "k", [a, b, c]);
			var t2 = new LearnTable();
			LearnAllTrainer.ApplyGame(t2, "g1", "k", [c, dead, b, a]);
			Assert.That(LearnAllSnapshotWriter.Write(t2), Is.EqualTo(LearnAllSnapshotWriter.Write(t1)));
		}

		[Test]
		public void ANewPersonalityStartsFromTheGlobalValuesAsTheyWereBeforeThatGame()
		{
			var table = new LearnTable();
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", One("rush", "gdi", "nod", 1.0)), Is.True);
			Assert.That(table.Find("any", "k").Global, Is.EqualTo(1041));
			Assert.That(table.Find("rush", "k").Global, Is.EqualTo(1041));

			Assert.That(LearnAllTrainer.ApplyGame(table, "g2", "k", One("turtle", "gdi", "nod", -1.0)), Is.True);
			Assert.That(table.Find("turtle", "k").Global, Is.EqualTo(1000), "turtle copied the global 1041, then stepped down once (not 1000 -> 961)");
			Assert.That(table.Find("any", "k").Global, Is.EqualTo(1000), "the global moved once as well");
			Assert.That(table.Find("rush", "k").Global, Is.EqualTo(1041), "another personality's table is untouched");
		}

		[Test]
		public void APersonalityWithoutATableReadsTheGlobalOne()
		{
			var table = new LearnTable();
			LearnAllTrainer.ApplyGame(table, "g1", "k", One("rush", "gdi", "nod", 1.0));
			Assert.That(table.Resolve("never_seen", "k", "gdi", ["nod"], "nod", null), Is.EqualTo(table.Resolve("any", "k", "gdi", ["nod"], "nod", null)));
		}

		[Test]
		public void AGameIsAppliedOnlyOnce()
		{
			var table = new LearnTable();
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", One("rush", "gdi", "nod", 1.0)), Is.True);
			var once = table.Resolve("rush", "k", "gdi", ["nod"], "nod", null);
			Assert.That(LearnAllTrainer.ApplyGame(table, "g1", "k", One("rush", "gdi", "nod", 1.0)), Is.False, "same game id again = skipped");
			Assert.That(table.Resolve("rush", "k", "gdi", ["nod"], "nod", null), Is.EqualTo(once));
		}

		[Test]
		public void AGameIdOutsideTheKeyCharsetIsRefusedLikeThePythonFitter()
		{
			var table = new LearnTable();
			foreach (var bad in new[] { "", "g 1", "g:1", "g|1", "g\n1", "g1\n", "g/1" })
				Assert.That(LearnAllTrainer.ApplyGame(table, bad, "k", One("rush", "gdi", "nod", 1.0)), Is.False, "game id '" + bad + "'");

			Assert.That(table.Personalities, Is.Empty, "a refused id changes nothing");
			Assert.That(LearnAllTrainer.ApplyGame(table, "g-1.a_B", "k", One("rush", "gdi", "nod", 1.0)), Is.True);
		}

		[Test]
		public void EstimatesNeverResetAndDriftGameByGame()
		{
			var table = new LearnTable();
			for (var i = 0; i < 10; i++)
				Assert.That(LearnAllTrainer.ApplyGame(table, "g" + i, "k", One("rush", "gdi", "nod", 1.0)), Is.True);

			var learned = table.Find("rush", "k").Global;
			Assert.That(learned, Is.GreaterThan(1400), "ten small steps accumulate");

			// A game with no evidence for the knob (the balance changed, nothing was exposed) leaves everything as it is.
			LearnAllTrainer.ApplyGame(table, "g10", "k", One("rush", "gdi", "nod", null));
			Assert.That(table.Find("rush", "k").Global, Is.EqualTo(learned));

			// There is no API that sets a table back to 1000 or pulls it toward G; the only mutation of a value is a bounded step.
			var after = table.Find("rush", "k").Global;
			LearnAllTrainer.ApplyGame(table, "g11", "k", One("rush", "gdi", "nod", -1.0));
			Assert.That(Math.Abs(table.Find("rush", "k").Global - after), Is.LessThanOrEqualTo(after * 0.0834), "one game moves a value by at most exp(0.08) - 1");
		}

		[Test]
		public void ATeamResolveUsesAllEnemiesAndTheMainTarget()
		{
			var table = new LearnTable();
			var axes = table.Install("any", "k");
			axes.EnemyFaction["soviet"] = 1000;
			axes.EnemyFaction["allies"] = 4000;
			Assert.That(table.Resolve("any", "k", "gdi", ["soviet", "allies"], "allies", null), Is.EqualTo(LearnAllMath.Resolve(1000, 1000, 2828, 0)),
				"E_eff = sqrt(geomean(1000, 4000) x 4000) = sqrt(2000 x 4000) = 2828");
		}

		[Test]
		public void TheReleaseSourcesReadOnlyAndHoldNoTrainingOrWriteCode()
		{
			// The pure core has no world, file or clock access; the step / trainer / snapshot writer live only in OpenRA.Mods.Cameo.Dev (built with CameoDevLogging=true).
			foreach (var file in new[] { "LearnAllMath.cs", "LearnAllSnapshot.cs", "LearnAllBotModule.cs" })
			{
				var code = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "source", "mods/Cameo/Traits/BotModules/LearnAll", file)).Replace("\r\n", "\n");
				foreach (var banned in new[] { "LearnAllStep", "LearnAllTrainer", "ApplyGame", "Rate =", "MaxLogStep", "Math.Exp", "Write(", "Clear()", "DateTime", "Random" })
					Assert.That(code, Does.Not.Contain(banned), file + " must not contain " + banned);
			}

			var math = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "source", "mods/Cameo/Traits/BotModules/LearnAll/LearnAllMath.cs"));
			Assert.That(math, Does.Not.Contain("System.IO"));
		}
	}
}
