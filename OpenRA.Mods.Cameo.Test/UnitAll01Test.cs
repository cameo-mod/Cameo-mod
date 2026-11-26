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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.Test
{
	// UNIT-ALL-01 (USER 11-25, V07: no Mammoth, OI tank or Duelist ever built): the unit pick is ONE fraction roll over the listed UnitsToBuild types the queue can build now.
	// weight = fraction x personality bias x observed counter demand, floored at epsilon x mean; a listed type never ordered is picked first. Row ub_unit_roll, genericbot only.
	[TestFixture]
	public sealed class UnitAll01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static readonly string[] Names = ["a_rifle", "b_rocket", "c_jeep", "d_tank", "e_mammoth", "f_duelist", "g_artillery", "h_orca"];
		static readonly int[] Fractions = [40, 30, 20, 15, 1, 1, 5, 0];

		static double[] Roll(int[] bias = null, int[] counter = null, int floor = 5) =>
			UnitRoll.Weights(Fractions, bias ?? Enumerable.Repeat(100, Names.Length).ToArray(), counter ?? Enumerable.Repeat(100, Names.Length).ToArray(), floor);

		[Test]
		public void EveryListedTypeKeepsAPositiveWeightEvenWithAZeroFractionOrABiasOfZero()
		{
			var w = Roll();
			Assert.That(w.All(x => x > 0), Is.True, "floor > 0");
			var biasZero = Enumerable.Repeat(0, Names.Length).ToArray();
			Assert.That(Roll(biasZero).All(x => x > 0), Is.True, "even a zero bias leaves weight >= 1");
			Assert.That(UnitRoll.Weights(new[] { 0, 0 }, new[] { 100, 100 }, new[] { 100, 100 }, 5), Is.EqualTo(new double[] { 1, 1 }), "all-zero fractions roll equally");
		}

		[Test]
		public void TheFloorIsEpsilonOfTheMeanAndNeverLowersAHeavyType()
		{
			var w = Roll(floor: 10);
			var raw = Roll(floor: 0);
			// V08E: the floor is a share of the mean RAW weight (fraction x 100% x 100%) before the lift to 1; raw[7] holds that lift (0 -> 1), so the mean comes from the fractions
			var mean = Fractions.Sum() * 100.0 / Fractions.Length;
			Assert.That(w[7], Is.EqualTo(mean * 10 / 100).Within(1e-9), "the zero-fraction orca takes the floor");
			Assert.That(w[0], Is.EqualTo(raw[0]).Within(1e-9), "a heavy type is untouched");
			Assert.That(w[4], Is.GreaterThanOrEqualTo(mean * 10 / 100));
		}

		[Test]
		public void OverManyRollsEveryListedTypeIsPickedAndTheHeavyOnesLead()
		{
			var w = Roll();
			var rng = new Random(1234);
			var hits = new int[Names.Length];
			var q = UnitRoll.Quantize(w);
			var sum = UnitRoll.Sum(q);
			for (var i = 0; i < 40000; i++)
				hits[UnitRoll.Pick(q, rng.Next(sum))]++;

			for (var i = 0; i < Names.Length; i++)
				Assert.That(hits[i], Is.GreaterThan(0), Names[i]);

			Assert.That(hits[0], Is.GreaterThan(hits[3]));
			Assert.That(hits[3], Is.GreaterThan(hits[4]), "the 1-fraction mammoth is rare");
		}

		[Test]
		public void FirstOfEachBuildsEveryTypeOnceWhateverTheFractionsAre()
		{
			var built = new HashSet<string>();
			var w = Roll();
			var rng = new Random(7);
			var steps = 0;
			while (built.Count < Names.Length && steps < Names.Length)
			{
				var unbuilt = UnitRoll.Unbuilt(Names, built.Contains);
				var pool = unbuilt.Select(i => w[i]).ToArray();
				var pq = UnitRoll.Quantize(pool);
				var pick = unbuilt[UnitRoll.Pick(pq, rng.Next(UnitRoll.Sum(pq)))];
				built.Add(Names[pick]);
				steps++;
			}

			Assert.That(built.Count, Is.EqualTo(Names.Length), "one pick per type");
			Assert.That(steps, Is.EqualTo(Names.Length), "one at a time, no repeats before every type has a first");
		}

		[Test]
		public void PersonalityBiasMovesTheOddsAndTheRushRampGoesFromInfantryToVehicles()
		{
			var early = new Dictionary<string, int> { ["rush.anti_infantry"] = 160, ["rush.frontline"] = 70 };
			var late = new Dictionary<string, int> { ["rush.anti_infantry"] = 100, ["rush.frontline"] = 140 };
			Assert.That(UnitRoll.RoleBiasPct(early, late, "rush", "anti_infantry", 0, 9000), Is.EqualTo(160));
			Assert.That(UnitRoll.RoleBiasPct(early, late, "rush", "frontline", 0, 9000), Is.EqualTo(70));
			Assert.That(UnitRoll.RoleBiasPct(early, late, "rush", "frontline", 4500, 9000), Is.EqualTo(105), "halfway");
			Assert.That(UnitRoll.RoleBiasPct(early, late, "rush", "frontline", 99999, 9000), Is.EqualTo(140), "past the ramp");
			Assert.That(UnitRoll.RoleBiasPct(early, late, "rush", "frontline", 4500, 0), Is.EqualTo(70), "no ramp = early value");
			Assert.That(UnitRoll.RoleBiasPct(early, late, "turtle", "frontline", 0, 9000), Is.EqualTo(100), "another personality is neutral");
			Assert.That(UnitRoll.RoleBiasPct(early, late, "", "frontline", 0, 9000), Is.EqualTo(100), "no personality is neutral");
			Assert.That(UnitRoll.RoleBiasPct(early, null, "rush", "scout", 0, 9000), Is.EqualTo(100), "an unlisted role is neutral");

			var plain = Roll();
			var biased = Roll(bias: new[] { 100, 100, 100, 170, 100, 100, 170, 100 });
			Assert.That((double)biased[3] / biased.Sum(), Is.GreaterThan((double)plain[3] / plain.Sum()), "steamroller tank share rises");
		}

		[Test]
		public void CounterDemandOnlyRaisesTheBestAnswerAndStaysNeutralWithoutAnObservedEnemy()
		{
			Assert.That(UnitRoll.CounterPct(0, 0, 200), Is.EqualTo(100), "nothing observed: neutral");
			Assert.That(UnitRoll.CounterPct(500, 500, 200), Is.EqualTo(300));
			Assert.That(UnitRoll.CounterPct(250, 500, 200), Is.EqualTo(200));
			Assert.That(UnitRoll.CounterPct(0, 500, 200), Is.EqualTo(100), "a unit that cannot hurt it gets no boost");
			Assert.That(UnitRoll.DeficitPct(20, 0), Is.EqualTo(200));
			Assert.That(UnitRoll.DeficitPct(20, 20), Is.EqualTo(100));
			Assert.That(UnitRoll.DeficitPct(0, 0), Is.EqualTo(100));
		}

		[Test]
		public void TheRollIsWiredBehindTheSwitchAndTheAblationRowAndLeavesTheHardGatesAlone()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("bool UnitRollOn => switchesActive && Info.UnitRoll && FairService && !ablateUnitRoll.Off(player, AblationIds.UnitRoll);"));
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.UnitAll.cs"), Does.Contain("\"ub_unit_roll\""));

			// the roll replaces the layers: the fair loop takes the roll pick and skips the floors; compositions are not selected
			Assert.That(u, Does.Contain("var unit = roll ? ChooseRollUnit(queue, skip) : fromFloor ?? (buildRandom"));
			var rollPick = u.IndexOf("var unit = roll ? ChooseRollUnit(queue, skip)", StringComparison.Ordinal);
			var noCandidate = u.IndexOf("if (roll && unit == null)\n\t\t\t\t\t{", rollPick, StringComparison.Ordinal);
			var idleFloor = u.IndexOf("if (unit == null && held == 0 && IdleQueueFloorOn", rollPick, StringComparison.Ordinal);
			var mixFloor = u.IndexOf("if (unit == null && held == 0 && UnitMixOn", rollPick, StringComparison.Ordinal);
			Assert.That(noCandidate, Is.GreaterThan(rollPick), "an empty roll ends the queue's fill");
			Assert.That(idleFloor, Is.GreaterThan(noCandidate), "...before the idle floor");
			Assert.That(mixFloor, Is.GreaterThan(noCandidate), "...and before the category floor");
			Assert.That(u, Does.Contain("if (!UnitRollOn || RollProfileOn(UnitRoll.Composition))\n\t\t\t\t\tUpdateComposition();"));
			Assert.That(u, Does.Contain("if (!ignoreUnitsToBuild && !UnitRollOn && unitsToBuildShares != null"), "the roll's candidates are the listed table: no share-membership refusal");

			// the candidate set: the raw UnitsToBuild table, no harvesters, hard gates before the pick
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var end = u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal);
			var body = u.Substring(start, end - start);
			Assert.That(body, Does.Contain("var table = Info.UnitsToBuild;"));
			Assert.That(body, Does.Contain("a.HasTraitInfo<HarvesterInfo>()"), "harvesters are outside the roll (HARV-01)");
			// V08E re-pin: ded7416c keeps the reject reason for RollExclude; the gate is unchanged
			Assert.That(body, Does.Contain("var rejectedBy = ShouldBuildReject(a.Name, true, queue.Info.Type);\n\t\t\t\tif (rejectedBy != null)"), "delay, interval, limit, capturer and item-aware caps still apply");
			Assert.That(body, Does.Not.Contain("GetUnitsToBuildForCategory"), "the derived or composition table never replaces the yaml fractions");
			Assert.That(body, Does.Not.Contain("ChooseCounter").And.Not.Contain("ChooseWaveMixRole").And.Not.Contain("ChooseRoleDeficit"));
			Assert.That(body, Does.Contain("fractions[i] = RollParameters.Fraction(name);"));
			Assert.That(body, Does.Contain("UnitRoll.Weights(fractions, biases, counterPcts, RollParameters.FloorPct, boosts)"));
		}

		// Opus ruling 1: no kept gate may block a listed type forever. The aircraft caps are waived for the FIRST build of a type (both in the candidate filter and after the pick),
		// and a UnitLimits entry below 1 would ban a type outright, so none may exist for a listed type.
		[Test]
		public void TheFirstBuildOfATypeIsNeverBlockedByTheAircraftCapsOrAUnitLimit()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			Assert.That(body, Does.Contain("if (rollOrdered.Contains(a.Name) && !CanBuildMoreOfAircraft(a))"), "the cap filters only types already ordered once");
			Assert.That(u, Does.Contain("if (!(roll && rollFirst) && !CanBuildMoreOfAircraft(unit, aircraftPending))"), "and the post-pick admission waives it for a first pick");
			Assert.That(u, Does.Contain("if (switchesActive && Info.UnitRoll)\n\t\t\t\trollOrdered.Add(name);"), "a type counts as ordered once an order was emitted for it, by any path, even while the row is ablated");

			// a unit limit of 0 would be a permanent ban on a listed type; the shared yaml carries none for genericbot
			var generic = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes
				.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes;
			var limits = generic.SingleOrDefault(n => n.Key == "UnitLimits")?.Value.Nodes;
			if (limits != null)
				foreach (var limit in limits)
					Assert.That(int.Parse(limit.Value.Value), Is.GreaterThanOrEqualTo(1), limit.Key);

			// the content-pack fragments add rows to the same module: the merged table is what the roll reads
			var packs = Path.Combine(AppContext.BaseDirectory, "source", "packs");
			var checkedPacks = 0;
			foreach (var file in Directory.GetFiles(packs, "ai.yaml", SearchOption.AllDirectories))
			{
				var rows = MiniYaml.FromString(File.ReadAllText(file), file).FirstOrDefault(n => n.Key == "Player")?.Value.Nodes
					.FirstOrDefault(n => n.Key == "UnitBuilderBotModuleCA@generic")?.Value.Nodes
					.FirstOrDefault(n => n.Key == "UnitLimits")?.Value.Nodes;
				if (rows == null)
					continue;

				checkedPacks++;
				foreach (var limit in rows)
					Assert.That(int.Parse(limit.Value.Value), Is.GreaterThanOrEqualTo(1), file + " " + limit.Key);
			}

			TestContext.Out.WriteLine("pack fragments with UnitLimits: " + checkedPacks);
		}

		// Opus ruling 3: the counter term reads the observed enemy memory only.
		[Test]
		public void TheCounterTermReadsTheObservedEnemyMemoryOnly()
		{
			var c = Source("mods/CA/Traits/BotModules/BotModuleLogic/AdaptiveCounterProduction.cs");
			var start = c.IndexOf("public void ObserveFogOnly(", StringComparison.Ordinal);
			var end = c.IndexOf("public long CounterScore(", start, StringComparison.Ordinal);
			var fogOnly = c.Substring(start, end - start);
			Assert.That(fogOnly, Does.Contain("provider.TryGetEnemyComposition(out var sample)"));
			Assert.That(fogOnly, Does.Not.Contain("OmniscientSample"), "no world scan");

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("if (UnitRollOn)\n\t\t\t\tcounters.ObserveFogOnly(Info.AdaptiveObservationInterval, compositionProvider);\n\t\t\telse if (CounterWeight > 0)\n\t\t\t\tcounters.Observe("));
		}

		[Test]
		public void TheDevLogIsCompiledOutOfReleaseAndTheRowIsGenericbotOnly()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RollPickLog("));
			Assert.That(u, Does.Contain("world.WorldTick - last < 250"), "rate limited");

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(generic.Single(n => n.Key == "UnitRoll").Value.Value, Is.EqualTo("true"));
			foreach (var o in player.Where(n => n.Key.StartsWith("UnitBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "UnitBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key.StartsWith("UnitRoll", StringComparison.Ordinal)), Is.False, o.Key);
		}

		// USER refinement: the folded layers are mode bias profiles. Each multiplies, never gates; each has its own strength (yaml) and ablation row.
		[Test]
		public void EveryProfileIsAMultiplierWithItsOwnRowAndStrengthAndNoneCanZeroACandidate()
		{
			Assert.That(UnitRoll.ProfileNames, Is.EqualTo(new[] { "composition", "counter", "wavemix", "roledeficit", "opening", "learned" }));
			Assert.That(AblationIds.UnitRollProfileRows, Is.EqualTo(new[] { "ub_roll_composition", "ub_roll_counter", "ub_roll_wavemix", "ub_roll_roledeficit", "ub_roll_opening", "ub_roll_learned" }));
			Assert.That(AblationIds.UnitRollProfileRows.Distinct().Count(), Is.EqualTo(UnitRoll.ProfileNames.Length));

			Assert.That(UnitRoll.Boost(false, 150), Is.EqualTo(100), "trigger not holding: neutral");
			Assert.That(UnitRoll.Boost(true, 0), Is.EqualTo(100), "strength 0 = off");
			Assert.That(UnitRoll.Boost(true, 150), Is.EqualTo(250));
			Assert.That(UnitRoll.Scaled(200, 100), Is.EqualTo(200), "strength 100 = the term as is");
			Assert.That(UnitRoll.Scaled(200, 50), Is.EqualTo(150));
			Assert.That(UnitRoll.Scaled(200, 0), Is.EqualTo(100), "strength 0 = neutral");
			Assert.That(UnitRoll.Scaled(0, 100), Is.EqualTo(0), "a learned prior of 0 stays 0 here; the floor in Weights keeps the unit alive");
			Assert.That(UnitRoll.ShareBoost(5, 10, 100), Is.EqualTo(150));
			Assert.That(UnitRoll.ShareBoost(0, 10, 100), Is.EqualTo(100), "not in the composition: neutral");
			Assert.That(UnitRoll.ShareBoost(5, 0, 100), Is.EqualTo(100));

			// a zero-product candidate still rolls: the floor, not the profiles, decides the minimum
			var w = UnitRoll.Weights(new[] { 10, 10 }, new[] { 0, 100 }, new[] { 100, 100 }, 5);
			Assert.That(w[0], Is.GreaterThan(0));

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			foreach (var profile in new[] { "UnitRoll.RoleDeficit", "UnitRoll.Learned", "UnitRoll.Counter", "UnitRoll.WaveMix", "UnitRoll.Composition", "UnitRoll.Opening" })
				Assert.That(body, Does.Contain("strength[" + profile + "]"), profile);

			Assert.That(u, Does.Contain("!rollProfileLatches[profile].Off(player, AblationIds.UnitRollProfileRows[profile])"));
			Assert.That(u, Does.Contain("baseBuilder != null && baseBuilder.CanTrainOpeningDefense"), "the opening profile uses the opening-defence window of the base builder");
			Assert.That(u, Does.Contain("manager.WaveMixActive"), "the wavemix profile uses the ARMY-MIX-01 trigger");
		}

		// c3 (Opus 11-25): an unaffordable first build is pay-as-you-go, like TECHCHAIN-PAYGO: it queues with any bank above 0, no savings hold. Opus 11-25 (Sol FIX): one legal pool, the
		// first-build preference is a weight (FirstBoost), not an exclusion; the open-slot machinery is gone.
		[Test]
		public void TheFirstBuildOfATypeQueuesWithAnyBankAndIsAWeightNotAnExclusion()
		{
			Assert.That(UnitRoll.FirstAdmits(1), Is.True);
			Assert.That(UnitRoll.FirstAdmits(0), Is.False, "an empty bank still waits");

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("var paygo = roll && rollFirst && UnitRoll.FirstAdmits(batch.BankLeft);\n\t\t\t\t\tvar cost = paygo ? 0 : fullCost;"), "only the first build sees cost 0 at the afford and reserve gates");
			Assert.That(u, Does.Contain("batch.Reserve(queue, fullCost);"), "the order still reserves its full price");
			Assert.That(u, Does.Not.Contain("rollFirstOpen").And.Not.Contain("RollOpenFirst"), "no open-slot exclusion any more");

			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			Assert.That(body, Does.Contain("var pool = items;"), "one legal pool: every listed buildable type is in every roll");
			Assert.That(body, Does.Not.Contain("items.Where(a => rollOrdered.Contains(a.Name))").And.Not.Contain("!rollOrdered.Contains(a.Name)).ToList()"), "never-built types never exclude or select");
			Assert.That(body, Does.Contain("UnitRoll.FirstBoost(RollParameters.FirstBoost, world.WorldTick - rollAvailableSince[name], RollParameters.FirstRampTicks, RollParameters.FirstCap)"));
			Assert.That(body, Does.Contain("rollFirst = !rollOrdered.Contains(pool[pick].Name);"), "a pick is a first build when its type was never ordered");
			Assert.That(body, Does.Not.Contain("savings"), "no savings hold");
		}

		// Opus 11-25 rulings on Sol's FIX of 7eccbe9a: the first-build boost escalates, the numerics are double, overrides are validated.
		[Test]
		public void TheFirstBuildBoostDoublesPerRampAndStopsAtTheCap()
		{
			Assert.That(UnitRoll.FirstBoost(8, 0, 3000, 64), Is.EqualTo(8));
			Assert.That(UnitRoll.FirstBoost(8, 2999, 3000, 64), Is.EqualTo(8));
			Assert.That(UnitRoll.FirstBoost(8, 3000, 3000, 64), Is.EqualTo(16));
			Assert.That(UnitRoll.FirstBoost(8, 9000, 3000, 64), Is.EqualTo(64));
			Assert.That(UnitRoll.FirstBoost(8, 900000, 3000, 64), Is.EqualTo(64), "capped, no overflow");
			Assert.That(UnitRoll.FirstBoost(8, 9000, 0, 64), Is.EqualTo(8), "no ramp = the base value");
			Assert.That(UnitRoll.FirstBoost(0, 0, 3000, 64), Is.EqualTo(1), "never below neutral");

			// an unbuilt zero-fraction type with the boost still rolls, and the boost raises a type that was left behind
			var plain = UnitRoll.Weights(new[] { 40, 1 }, new[] { 100, 100 }, new[] { 100, 100 }, 5);
			var boosted = UnitRoll.Weights(new[] { 40, 1 }, new[] { 100, 100 }, new[] { 100, 100 }, 5, new[] { 1.0, 64.0 });
			Assert.That(boosted[1], Is.GreaterThan(plain[1] * 60));
			Assert.That(boosted[0], Is.EqualTo(plain[0]).Within(1e-9), "a built type keeps its weight");
		}

		// Opus 11-25 ruling on Sol's FIX of 686ca394: the deterministic 30-bit draw stays; weights are quantized to integers (sum <= 2^30) with a relative floor of total / 2^20, the draw is an
		// integer in [0, sum) walked over the cumulative sum. These fixtures use the REAL discrete draw, not a hand-picked double.
		static void AssertEveryCandidateOwnsAnInterval(double[] weights)
		{
			var q = UnitRoll.Quantize(weights);
			Assert.That(q.All(x => x >= 1), Is.True, "every candidate has at least 1 unit");
			Assert.That(UnitRoll.Sum(q), Is.LessThanOrEqualTo(1 << UnitRoll.DrawBits), "the sum fits the 30-bit draw");

			// exhaustive over the interval boundaries: the first and last draw of every candidate's interval land on it
			var cumulative = 0;
			for (var i = 0; i < q.Length; i++)
			{
				Assert.That(UnitRoll.Pick(q, cumulative), Is.EqualTo(i), "first draw of candidate " + i);
				cumulative += q[i];
				Assert.That(UnitRoll.Pick(q, cumulative - 1), Is.EqualTo(i), "last draw of candidate " + i);
			}
		}

		[Test]
		public void ExtremeWeightsKeepEveryCandidateReachableThroughTheRealDiscreteDraw()
		{
			AssertEveryCandidateOwnsAnInterval(new[] { 1e12, 1 });
			var q = UnitRoll.Quantize(new[] { 1e12, 1 });
			Assert.That(UnitRoll.Pick(q, UnitRoll.Sum(q) - 1), Is.EqualTo(1), "the last candidate is reachable at the largest draw");
			Assert.That(q[1], Is.GreaterThanOrEqualTo(1000), "the relative floor (total / 2^20) gives the tail about a thousand units");

			AssertEveryCandidateOwnsAnInterval(new[] { 1.0, 1e12, 1.0, 1e12, 1.0 });
			AssertEveryCandidateOwnsAnInterval(new[] { 3.0, 1.0, 2.0 });

			// the extreme override path of the table: every number at the maximum, floor 100% and more
			var w = UnitRoll.Weights(new[] { 1_000_000, 1_000_000, 1 }, new[] { 100_000, 100_000, 100 }, new[] { 100_000, 100_000, 100 }, 1_000_000, new[] { 64.0, 64.0, 64.0 });
			Assert.That(w.All(x => x >= 1 && double.IsFinite(x)), Is.True);
			AssertEveryCandidateOwnsAnInterval(w);

			// all-zero except the floor: equal, positive
			var z = UnitRoll.Weights(new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, 0);
			Assert.That(z, Is.EqualTo(new double[] { 1, 1, 1 }));
			var zq = UnitRoll.Quantize(z);
			Assert.That(zq.Distinct().Count(), Is.EqualTo(1), "equal weights give equal units");
			AssertEveryCandidateOwnsAnInterval(z);

			// garbage in (NaN, infinity, negative) never removes a candidate
			AssertEveryCandidateOwnsAnInterval(new[] { double.NaN, double.PositiveInfinity, -5, 2 });

			// the module draws exactly this way
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("var units = UnitRoll.Quantize(weights);"));
			Assert.That(u, Does.Contain("UnitRoll.Pick(units, BotRng.For(player, nameof(UnitBuilderBotModuleCA)).Next(UnitRoll.Sum(units)))"));
		}

		[Test]
		public void TableOverridesAreClampedToZeroAndTheMaximum()
		{
			var t = new UnitRollTable(new Dictionary<string, int> { ["a"] = 5 }, null, null, null, 5, 0);
			t.Override(UnitRollTable.FractionKey("a"), -50);
			Assert.That(t.Fraction("a"), Is.EqualTo(0), "negative = 0");
			t.Override(UnitRollTable.FractionKey("a"), int.MaxValue);
			Assert.That(t.Fraction("a"), Is.EqualTo(UnitRollTable.MaxValue), "clamped to the maximum");
			t.Override(UnitRollTable.FloorKey, int.MinValue);
			Assert.That(t.FloorPct, Is.EqualTo(0));
			t.Override(UnitRollTable.FirstBoostKey, 1_000_000_000);
			Assert.That(t.FirstBoost, Is.EqualTo(UnitRollTable.MaxValue));
			t.ClearOverrides();
			Assert.That(t.FirstBoost, Is.EqualTo(8), "the default table value");
			Assert.That(t.FirstRampTicks, Is.EqualTo(3000));
			Assert.That(t.FirstCap, Is.EqualTo(64));
			Assert.That(new UnitRollTable(new Dictionary<string, int> { ["a"] = -7 }, null, null, null, -1, -1).Fraction("a"), Is.EqualTo(0), "a bad yaml value is clamped too");
		}

		// UPGRADE-COVERAGE once-done registry (Opus 11-25; Sol FIX of 686ca394: only symmetric pairs form a set).
		[Test]
		public void TheOnceDoneRegistryCountsARepeatableAfterItsFirstCompletionAndOnlyASymmetricSetOnce()
		{
			var r = new UpgradeCompletions();
			r.Observe("upgrade_a", 100, new[] { "~faction", "!upgrade_a" }); // non-repeatable: only itself
			r.Observe("opt_x", 200, new[] { "!opt_y" });
			r.Observe("opt_y", 250, new[] { "!opt_x" });
			r.Observe("promo_elite", 300, null);

			Assert.That(r.IsDone("upgrade_a"), Is.False);
			Assert.That(r.Complete("upgrade_a"), Is.True, "first completion");
			Assert.That(r.Complete("upgrade_a"), Is.False, "a repeatable counts once");
			Assert.That(r.IsDone("upgrade_a"), Is.True);

			Assert.That(r.IsDone("opt_x") || r.IsDone("opt_y"), Is.False);
			r.Complete("opt_y");
			Assert.That(r.IsDone("opt_x"), Is.True, "any one option satisfies the whole symmetric set");
			Assert.That(r.Completed("opt_x"), Is.False, "but only the completed member counts as completed itself");
			Assert.That(r.Since("upgrade_a"), Is.EqualTo(100));
			Assert.That(r.Since("never_seen"), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void ADirectionalBlockerLinksNothing()
		{
			// A requires !B (it is only offered while B is absent) but B stays legal after A: completing A must not mark B done
			var r = new UpgradeCompletions();
			r.Observe("a_requires_not_b", 10, new[] { "!b_plain" });
			r.Observe("b_plain", 10, null);
			Assert.That(r.Complete("a_requires_not_b"), Is.True);
			Assert.That(r.IsDone("b_plain"), Is.False, "B is not part of a set with A");

			// the other direction: completing B does not mark A done either (A simply becomes unavailable through its prerequisite)
			var r2 = new UpgradeCompletions();
			r2.Observe("a_requires_not_b", 10, new[] { "!b_plain" });
			r2.Observe("b_plain", 10, null);
			r2.Complete("b_plain");
			Assert.That(r2.IsDone("a_requires_not_b"), Is.False);
		}

		// Sol FIX of 425f1fab: completion is decided from authoritative state at the service visits, keyed to the REAL grants of the item.
		static List<(string Name, string Source)> Settle(UpgradeCompletions r, int tick, string[] inProduction, Dictionary<string, string[]> grants, string[] owned, string[] offered = null,
			bool queueAlive = true, bool prerequisitesHeld = true, string queueKey = "q1", Func<string, bool> aliveKey = null) =>
			r.Settle(tick, inProduction.ToDictionary(n => n, _ => queueKey), n => grants.TryGetValue(n, out var g) ? g : [], new HashSet<string>(owned), () => new HashSet<string>(offered ?? []),
				key => aliveKey != null ? aliveKey(key) : queueAlive, (_, _) => prerequisitesHeld);

		[Test]
		public void AFastItemThatFinishesBetweenTwoVisitsIsStillFoundByItsGrant()
		{
			var r = new UpgradeCompletions();
			var grants = new Dictionary<string, string[]> { ["upgrade_fast"] = ["upgrade_fast"] };
			r.Started("upgrade_fast", 1000);

			// the item entered and finished between two visits: it was never seen in the queue, the grant is held
			var done = Settle(r, 1030, [], grants, ["upgrade_fast", "something_else"]);
			Assert.That(done, Is.EqualTo(new[] { ("upgrade_fast", UpgradeCompletions.SourceGrant) }));
			Assert.That(r.Completed("upgrade_fast"), Is.True);
			Assert.That(r.Source("upgrade_fast"), Is.EqualTo("grant"));
			Assert.That(r.AnyPending, Is.False);
			Assert.That(Settle(r, 1060, [], grants, ["upgrade_fast"]), Is.Empty, "once");
		}

		[Test]
		public void TheGrantIsResolvedFromTheItemsTraitsNotItsName()
		{
			var grants = new Dictionary<string, string[]> { ["upgrade_cabal"] = ["tech.cabal", "tech.cabal2"] };

			// holding something named like the item is NOT the grant
			var r = new UpgradeCompletions();
			r.Started("upgrade_cabal", 0);
			Assert.That(Settle(r, 50, [], grants, ["upgrade_cabal"]), Is.Empty, "the name alone proves nothing");
			Assert.That(r.Completed("upgrade_cabal"), Is.False);

			// holding any real grant is
			Assert.That(Settle(r, 80, [], grants, ["tech.cabal2"]), Is.EqualTo(new[] { ("upgrade_cabal", "grant") }));

			// still in production: not settled
			var r2 = new UpgradeCompletions();
			r2.Started("upgrade_cabal", 0);
			Assert.That(Settle(r2, 50, ["upgrade_cabal"], grants, ["tech.cabal"]), Is.Empty, "the grant of a running item is not held yet or is another item's");
			Assert.That(r2.AnyPending, Is.True);

			// a cancelled item (grant never held) is dropped once stale, never done
			var r3 = new UpgradeCompletions();
			r3.Started("upgrade_cabal", 0);
			Assert.That(Settle(r3, UpgradeCompletions.DefaultPendingTimeoutTicks + 1, [], grants, []), Is.Empty);
			Assert.That(r3.AnyPending, Is.False);
			Assert.That(r3.Completed("upgrade_cabal"), Is.False);
		}

		[Test]
		public void AConditionOnlyItemIsDoneByApproximationOnlyWhenItLeftTheQueueAndIsNoLongerOffered()
		{
			var grants = new Dictionary<string, string[]>(); // no prerequisite: condition-only

			// seen in the queue, then gone and not offered any more: approx
			var r = new UpgradeCompletions();
			r.Started("promo_cond", 0);
			Assert.That(Settle(r, 25, ["promo_cond"], grants, [], []), Is.Empty, "still in the queue");
			Assert.That(Settle(r, 50, [], grants, [], []), Is.EqualTo(new[] { ("promo_cond", UpgradeCompletions.SourceApprox) }));
			Assert.That(r.Source("promo_cond"), Is.EqualTo("approx"));

			// seen, gone, but still offered (cancelled, or a repeatable): NOT done, and dropped
			var r2 = new UpgradeCompletions();
			r2.Started("promo_cond", 0);
			Settle(r2, 25, ["promo_cond"], grants, [], ["promo_cond"]);
			Assert.That(Settle(r2, 50, [], grants, [], ["promo_cond"]), Is.Empty);
			Assert.That(r2.Completed("promo_cond"), Is.False);
			Assert.That(r2.AnyPending, Is.False);

			// ordered this visit and not queued yet: not judged
			var r3 = new UpgradeCompletions();
			r3.Started("promo_cond", 1000);
			Assert.That(Settle(r3, 1010, [], grants, [], []), Is.Empty);
			Assert.That(r3.AnyPending, Is.True);

			// a never-seen order is never done by approximation, even long after: it is dropped as not accepted
			Assert.That(Settle(r3, 1000 + UpgradeCompletions.DefaultPendingTimeoutTicks + 1, [], grants, [], []), Is.Empty);
			Assert.That(r3.AnyPending, Is.False);
			Assert.That(r3.Completed("promo_cond"), Is.False);
		}

		[Test]
		public void ADelayedOrderStaysReservedAndKeepsItsFirstIssueTickAndSeenBit()
		{
			var grants = new Dictionary<string, string[]>();
			var r = new UpgradeCompletions();
			r.Started("promo_cond", 100);
			Assert.That(r.IsPending("promo_cond"), Is.True, "reserved: the service adds pending names to the in-production set, so it is not offered again");
			Settle(r, 120, [], grants, [], ["promo_cond"]);

			// a second Started (a re-order) must not restart the clock
			r.Started("promo_cond", 150);
			Assert.That(Settle(r, 100 + UpgradeCompletions.DefaultPendingTimeoutTicks + 1, [], grants, [], ["promo_cond"]), Is.Empty);
			Assert.That(r.AnyPending, Is.False, "dropped by the FIRST issue tick, not the restarted one");

			// seen once, a later Started does not clear the Seen bit
			var r2 = new UpgradeCompletions();
			r2.Started("promo_cond", 0);
			Settle(r2, 10, ["promo_cond"], grants, [], []);
			r2.Started("promo_cond", 20);
			Assert.That(Settle(r2, 30, [], grants, [], []), Is.EqualTo(new[] { ("promo_cond", UpgradeCompletions.SourceApprox) }), "the Seen bit survived");

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			// V08E re-pin: 1ce52d20 put the reservation behind upg_once_registry (onceOn), as its own pin in TheOnceRegistryHasItsOwnToggleThatShipsOffAndTheDevRowsDoNotDependOnIt states
			Assert.That(u, Does.Contain("if (onceOn)\n\t\t\t\tforeach (var pendingName in upgradeDone.PendingNames)\n\t\t\t\t\tinProduction.Add(pendingName);"));
		}

		[Test]
		public void AQueueGlimpseBetweenTwoSettlesIsAlreadyTheSeenBitBoundToThatQueue()
		{
			var grants = new Dictionary<string, string[]>();

			// the queue showed the item at a visit between two 25-tick settles and it finished before the next settle: seen (bound to q1), left, not offered, queue alive: approx
			var r = new UpgradeCompletions();
			r.Started("promo_cond", 100);
			r.NoteQueued(new Dictionary<string, string> { ["promo_cond"] = "q1" });
			Assert.That(Settle(r, 125, [], grants, [], []), Is.EqualTo(new[] { ("promo_cond", UpgradeCompletions.SourceApprox) }));

			// without the glimpse it is a never-seen order: no approximation, only the timeout
			var r2 = new UpgradeCompletions();
			r2.Started("promo_cond", 100);
			Assert.That(Settle(r2, 125, [], grants, [], []), Is.Empty);
			Assert.That(r2.Completed("promo_cond"), Is.False);

			// the glimpse binds the exact queue: q1 dead = queue_lost even though another queue lives
			var r3 = new UpgradeCompletions();
			r3.Started("promo_cond", 100);
			r3.NoteQueued(new Dictionary<string, string> { ["promo_cond"] = "q1" });
			Assert.That(Settle(r3, 125, [], grants, [], [], aliveKey: key => key == "q2"), Is.Empty);
			Assert.That(r3.Released.Single(), Is.EqualTo(("promo_cond", "queue_lost")));

			// NoteQueued of an item that is not pending changes nothing, and a later glimpse in another queue moves the binding
			var r4 = new UpgradeCompletions();
			r4.NoteQueued(new Dictionary<string, string> { ["x"] = "q1" });
			Assert.That(r4.AnyPending, Is.False);
			r4.Started("promo_cond", 0);
			r4.NoteQueued(new Dictionary<string, string> { ["promo_cond"] = "q1" });
			r4.NoteQueued(new Dictionary<string, string> { ["promo_cond"] = "q2" });
			Assert.That(Settle(r4, 30, [], grants, [], [], aliveKey: key => key == "q2"), Is.EqualTo(new[] { ("promo_cond", UpgradeCompletions.SourceApprox) }));

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("upgradeDone.NoteQueued(queuedIn);\n\t\t\tif (world.WorldTick - upgradeSettleTick < 25)\n\t\t\t\treturn;"), "noted at every visit, before the 25-tick gate");
			Assert.That(u, Does.Contain("if (reason == \"timeout\")\n\t\t\t\tRollLog($\"{world.WorldTick},{Array.IndexOf(world.Players, player)},upg_unseen_timeout,{name},,{++upgradeUnseenTimeouts}\");"), "DEV counter of never-seen timeouts (Opus ruling: no approximation for them)");
		}

		[Test]
		public void TheDeadProducerIsTheExactQueueThatShowedTheItemNotAnyQueueOfTheCategory()
		{
			var grants = new Dictionary<string, string[]>();

			// two queues of the same category: the item was seen in q1; q1's producer dies, q2 survives; the item is no longer offered (a producer-specific list): NOT done
			var r = new UpgradeCompletions();
			r.Started("promo_cond", 0);
			Settle(r, 10, ["promo_cond"], grants, [], [], queueKey: "q1");
			Assert.That(Settle(r, 40, [], grants, [], [], aliveKey: key => key == "q2"), Is.Empty);
			Assert.That(r.Completed("promo_cond"), Is.False, "q2 being alive proves nothing about the producer that held the item");
			Assert.That(r.Released.Single(), Is.EqualTo(("promo_cond", "queue_lost")));

			// the same departure with q1 still alive is credible
			var r2 = new UpgradeCompletions();
			r2.Started("promo_cond", 0);
			Settle(r2, 10, ["promo_cond"], grants, [], [], queueKey: "q1");
			Assert.That(Settle(r2, 40, [], grants, [], [], aliveKey: key => key == "q1"), Is.EqualTo(new[] { ("promo_cond", UpgradeCompletions.SourceApprox) }));

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("bool UpgradeQueueAlive(string queueKey)"));
			Assert.That(u, Does.Contain("if (UpgradeQueueKey(queue) == queueKey)"));
			Assert.That(u, Does.Contain("queuedIn[queued.Item] = UpgradeQueueKey(queue);"));
		}

		[Test]
		public void AProducerOrPrerequisiteLossIsNotACompletion()
		{
			var grants = new Dictionary<string, string[]>();

			// seen in the queue, then the producer was sold: the offer stops, but it is not a completion
			var r = new UpgradeCompletions();
			r.Started("promo_cond", 0);
			Settle(r, 10, ["promo_cond"], grants, [], []);
			Assert.That(Settle(r, 40, [], grants, [], [], queueAlive: false), Is.Empty);
			Assert.That(r.Completed("promo_cond"), Is.False);
			Assert.That(r.AnyPending, Is.False);

			// a required prerequisite was lost: same
			var r2 = new UpgradeCompletions();
			r2.Started("promo_cond", 0);
			Settle(r2, 10, ["promo_cond"], grants, [], []);
			Assert.That(Settle(r2, 40, [], grants, [], [], prerequisitesHeld: false), Is.Empty);
			Assert.That(r2.Completed("promo_cond"), Is.False);

			// a granting item that left the queue without its grant (cancelled or lost) is dropped, never done
			var granting = new Dictionary<string, string[]> { ["upgrade_x"] = ["tech.x"] };
			var r3 = new UpgradeCompletions();
			r3.Started("upgrade_x", 0);
			Settle(r3, 10, ["upgrade_x"], granting, []);
			Assert.That(Settle(r3, 40, [], granting, []), Is.Empty);
			Assert.That(r3.Completed("upgrade_x"), Is.False);
			Assert.That(r3.AnyPending, Is.False);

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
						Assert.That(u, Does.Contain("bool UpgradePrerequisitesHeld(string name, ISet<string> owned)"));
		}

		[Test]
		public void PreExistingOrRestoredCompletionsAreFoundByTheInitScanOfRealGrants()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");

			// authoritative state: the prerequisites the player holds (ITechTreePrerequisite, the tech tree's own source) against the grants the item's traits provide (ITechTreePrerequisiteInfo)
			Assert.That(u, Does.Contain("world.ActorsWithTrait<ITechTreePrerequisite>()"));
			Assert.That(u, Does.Contain("info.TraitInfos<ITechTreePrerequisiteInfo>().SelectMany(t => t.Prerequisites(info))"), "the real grant, not the name");
			Assert.That(u, Does.Contain("void UpgradeScanOnce()\n\t\t{\n\t\t\tif (upgradeScanned)\n\t\t\t\treturn;"), "once");
			Assert.That(u, Does.Contain("if (ServedByUpgradeService(a) && UpgradeGrantsOf(a.Name).Any(owned.Contains) && upgradeDone.Complete(a.Name, UpgradeCompletions.SourceInit))"));
			Assert.That(u, Does.Contain("UpgradeScanOnce();\n\t\t\tUpgradeSettle(queuedIn);"), "both run before the service chooses");
			Assert.That(u, Does.Contain("upgradeDone.Started(item2.Name, world.WorldTick);"), "every ordered item is settled at the later visits");
			Assert.That(u, Does.Contain("upgradeDone.Settle(world.WorldTick, queuedIn, UpgradeGrantsOf, OwnedPrerequisites(), OfferedUpgradeNames, UpgradeQueueAlive, UpgradePrerequisitesHeld, Info.UpgradePendingTimeout)"));
			Assert.That(u, Does.Contain("upg_done_source={source}"), "the DEV row carries the source");

			// no ActorAdded guess
			Assert.That(u, Does.Not.Contain("UpgradeCompleted(Actor a)"));
			Assert.That(u, Does.Not.Contain("world.ActorAdded += UpgradeCompleted"));
			Assert.That(Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs"), Does.Not.Contain("ActorAdded"), "the DEV row reads the module's registry");
			Assert.That(Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs"), Does.Contain("AppendString(b, \"upg_done_source\", source)"));

			// the registry itself: a restored completion marks done at once, without ever having been seen buildable
			var r = new UpgradeCompletions();
			Assert.That(r.Complete("upgrade_conyard"), Is.True, "found at init");
			Assert.That(r.Source("upgrade_conyard"), Is.EqualTo("init"));
			Assert.That(r.IsDone("upgrade_conyard"), Is.True);
			Assert.That(r.Complete("upgrade_conyard"), Is.False, "once");
		}

		// ub_capturer_strict (Opus 11-25, V08E): rifle infantry carry Captures@DRIVER_KILL and a garrison capture; only a building class makes a capturer.
		static readonly BitSet<CaptureType> BuildingClasses = new("building", "defense", "tech");

		[Test]
		public void RifleInfantryWithGarrisonAndDriverKillCapturesIsNotACapturerButAnEngineerIs()
		{
			var driverKill = new BitSet<CaptureType>("driverless_vehicle", "vehicle");
			var garrison = new BitSet<CaptureType>("ra2garrison");
			var engineer = new BitSet<CaptureType>("building", "defense", "tech");
			var spy = new BitSet<CaptureType>("building");
			var carBomb = new BitSet<CaptureType>("bombcar");
			var husk = new BitSet<CaptureType>("husk");

			Assert.That(CapturerClass.IsStrict([driverKill, garrison], BuildingClasses), Is.False, "ra1_allies_rifleinfantry");
			Assert.That(CapturerClass.IsStrict([carBomb], BuildingClasses), Is.False);
			Assert.That(CapturerClass.IsStrict([husk, driverKill], BuildingClasses), Is.False);
			Assert.That(CapturerClass.IsStrict([engineer], BuildingClasses), Is.True, "an engineer");
			Assert.That(CapturerClass.IsStrict([spy], BuildingClasses), Is.True);
			Assert.That(CapturerClass.IsStrict([garrison, engineer], BuildingClasses), Is.True, "any one building trait is enough");
			Assert.That(CapturerClass.IsStrict([], BuildingClasses), Is.False);
		}

		[Test]
		public void TheStrictCapturerRuleIsUsedAtEveryCapOrDenominatorSiteAndClassicKeepsTheOldRule()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("bool CapturerStrictOn => switchesActive && Info.CapturerStrict && !ablateCapturerStrict.Off(player, AblationIds.CapturerStrict);"));
			Assert.That(u, Does.Contain("public readonly bool CapturerStrict = false;"), "off by default: classic and every other instance keep the old rule");
			Assert.That(u, Does.Contain("public readonly string[] CapturerCaptureTypes = { \"building\", \"defense\", \"tech\" };"));
			Assert.That(u, Does.Contain("var old = info.HasTraitInfo<CapturesInfo>();\n\t\t\tif (!old)\n\t\t\t\treturn false;\n\n\t\t\tif (!CapturerStrictOn)\n\t\t\t\treturn CapturerClass.CountsForCap(true, IsWorker(info));"), "off = exactly the old test");

			// the cap sites and the armed-infantry denominator all ask IsCapturer; no raw CapturesInfo test is left outside the classifier, the static unit-mix category and the static default
			var withoutClassifier = u.Replace("var old = info.HasTraitInfo<CapturesInfo>();", "").Replace("IsArmedInfantry(info, info.HasTraitInfo<CapturesInfo>())", "")
				.Replace("UnitMixCapturerOn ? IsCapturer(info) || IsWorker(info) : info.HasTraitInfo<CapturesInfo>();", "");
			Assert.That(withoutClassifier.Split(new[] { "HasTraitInfo<CapturesInfo>()" }, StringSplitOptions.None).Length - 1, Is.EqualTo(0), "every cap / denominator site goes through IsCapturer");
			Assert.That(u, Does.Contain("capturerUnitsCached++;"));
			Assert.That(u, Does.Contain("else if (IsArmedInfantryForCap(info))"));
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid CapturerClassLog("), "the per-type classification row is DEV only and written once");
			Assert.That(u, Does.Contain(",ub_capturer_class,{name},,{(worker ? 2 : strict ? 1 : 0)}"));

			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.Capturer.cs"), Does.Contain("\"ub_capturer_strict\""));

			// armed for genericbot only: the yaml switch sits in the generic module block, not in the classic one
			var generic = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes.Any(n => n.Key == "CapturerStrict" && n.Value.Value == "true"), Is.True);
			Assert.That(generic.Single(n => n.Key == "UnitBuilderBotModuleCA@classic").Value.Nodes.Any(n => n.Key == "CapturerStrict"), Is.False);
		}

		// ub_capturer_workers (Opus 11-25, MILITIA-CAP-01): the Harvester-trait peasant and militia carry a building Captures trait and are invisible to the capturer cap.
		[Test]
		public void AWorkerIsNeitherACapturerNorArmedInfantryForTheCapAndOffKeepsTheOldRule()
		{
			// pure seam: (capturer by the strict/old rule, worker taken out)
			Assert.That(CapturerClass.CountsForCap(true, false), Is.True, "an engineer stays a capturer");
			Assert.That(CapturerClass.CountsForCap(true, true), Is.False, "the militia / peasant: a building Captures trait but a worker");
			Assert.That(CapturerClass.CountsForCap(false, false), Is.False, "rifle infantry never was one");
			Assert.That(CapturerClass.CountsForCap(false, true), Is.False);

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("public readonly bool CapturerSkipsWorkers = false;"), "off by default: classic and every other instance keep today's rule");
			Assert.That(u, Does.Contain("bool CapturerWorkersOn => switchesActive && Info.CapturerSkipsWorkers && !ablateCapturerWorkers.Off(player, AblationIds.CapturerWorkers);"));
			Assert.That(u, Does.Contain("bool IsWorker(ActorInfo info) => CapturerWorkersOn && info.HasTraitInfo<HarvesterInfo>();"), "the switch short-circuits: off costs no trait lookup");
			Assert.That(u, Does.Contain("return CapturerClass.CountsForCap(strict, worker);"));
			Assert.That(u, Does.Contain("bool IsArmedInfantryForCap(ActorInfo info) => !IsWorker(info) && IsArmedInfantry(info, IsCapturer(info));"));
			Assert.That(u, Does.Contain("if (IsWorker(info))\n\t\t\t\t\tcontinue;\n\n\t\t\t\tif (IsCapturer(info))\n\t\t\t\t\tcapturerUnitsCached++;"), "a worker is in neither count");

			// no raw armed-infantry-with-IsCapturer test is left: every cap site (denominator, batch pending, the pick) goes through the worker-aware helper
			Assert.That(u, Does.Not.Contain("IsArmedInfantry(unit, IsCapturer(unit))"));
			Assert.That(u, Does.Not.Contain("IsArmedInfantry(actorInfo, IsCapturer(actorInfo))"));
			Assert.That(u, Does.Not.Contain("IsArmedInfantry(b, IsCapturer(b))"));
			Assert.That(u, Does.Contain("IsArmedInfantryForCap(b) && !skip.Contains(b.Name)"));
			Assert.That(u.Split(new[] { "IsArmedInfantryForCap(" }, StringSplitOptions.None).Length - 1, Is.EqualTo(5), "the definition, the denominator, two pending sites, the pick");

			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.Capturer.cs"), Does.Contain("\"ub_capturer_workers\""));
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.Capturer.cs"), Does.Contain("\"ub_unitmix_capturer\""));

			var generic = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes.Any(n => n.Key == "CapturerSkipsWorkers" && n.Value.Value == "true"), Is.True);
			Assert.That(generic.Single(n => n.Key == "UnitBuilderBotModuleCA@classic").Value.Nodes.Any(n => n.Key == "CapturerSkipsWorkers"), Is.False);
		}

		// ub_unitmix_capturer: the unit-mix census used the raw Captures trait, so every ^DefaultInfantry (Captures@CAPTURES: ra2garrison) fell in the None category.
		[Test]
		public void TheUnitMixCensusUsesTheCapRuleWhenItsOwnRowIsOnAndTheRawTraitOtherwise()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("public readonly bool UnitMixCapturerClass = false;"));
			Assert.That(u, Does.Contain("bool UnitMixCapturerOn => switchesActive && Info.UnitMixCapturerClass && !ablateUnitMixCapturer.Off(player, AblationIds.UnitMixCapturer);"));
			Assert.That(u, Does.Contain("var capturer = UnitMixCapturerOn ? IsCapturer(info) || IsWorker(info) : info.HasTraitInfo<CapturesInfo>();"), "off = the raw test of before");
			Assert.That(u, Does.Contain("&& !capturer && !info.HasTraitInfo<CarrierSlaveInfo>();"));
			Assert.That(u, Does.Contain(": IsArmedInfantry(info, capturer) ? UnitMixCategory.Infantry"));

			// the garrison-only Captures of the default infantry: strict says not a capturer, so the unit is infantry in the census
			var garrison = new BitSet<CaptureType>("ra2garrison");
			Assert.That(CapturerClass.CountsForCap(CapturerClass.IsStrict([garrison], BuildingClasses), false), Is.False);

			var generic = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes.Any(n => n.Key == "UnitMixCapturerClass" && n.Value.Value == "true"), Is.True);
			Assert.That(generic.Single(n => n.Key == "UnitBuilderBotModuleCA@classic").Value.Nodes.Any(n => n.Key == "UnitMixCapturerClass"), Is.False);
		}

		[Test]
		public void TheOnceRegistryHasItsOwnToggleThatShipsOffAndTheDevRowsDoNotDependOnIt()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.UpgradeQueue.cs"), Does.Contain("\"upg_once_registry\""));
			Assert.That(u, Does.Contain("public readonly bool UpgradeOnceRegistry = false;"), "ships OFF until it has a PASS");
			Assert.That(u, Does.Contain("bool UpgradeOnceOn => Info.UpgradeOnceRegistry && !ablateUpgradeOnce.Off(player, AblationIds.UpgradeOnceRegistry);"));

			// the three gameplay effects sit behind it; the tracking (Observe, Started, Settle) does not
			Assert.That(u, Does.Contain("if (onceOn && upgradeDone.IsDone(item.Name))"));
			Assert.That(u, Does.Contain("onceOn ? upgradeDone.Since(item.Name) : 0"));
			Assert.That(u, Does.Contain("if (onceOn)\n\t\t\t\tforeach (var pendingName in upgradeDone.PendingNames)"));
			Assert.That(u, Does.Contain("upgradeDone.Observe(item.Name, world.WorldTick, item.TraitInfoOrDefault<BuildableInfo>()?.Prerequisites);"));
			Assert.That(u, Does.Contain("upgradeDone.Started(item2.Name, world.WorldTick);"));
			Assert.That(u, Does.Contain("Info.UpgradePendingTimeout))"), "PendingTimeout is yaml");
			Assert.That(u, Does.Contain("public readonly int UpgradePendingTimeout = 300;"));
			Assert.That(u, Does.Contain("upg_pending_release,{name},,{reason}"), "every release is logged with its reason");

			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			Assert.That(dev, Does.Not.Contain("UpgradeOnce"), "the DEV coverage rows are independent of the toggle");

			// V08F (Opus 11-25): armed for genericbot only - the yaml switch sits in the generic block, not in the classic one
			var players = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			Assert.That(players.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes.Any(n => n.Key == "UpgradeOnceRegistry" && n.Value.Value == "true"), Is.True);
			Assert.That(players.Single(n => n.Key == "UnitBuilderBotModuleCA@classic").Value.Nodes.Any(n => n.Key == "UpgradeOnceRegistry"), Is.False);
		}

		// Opus 11-25: ub_pool_empty (DEV record only, no behaviour) + the ub_unit_roll OFF path stays the old one (classic byte-identical).
		[Test]
		public void TheEmptyPoolRowIsDevOnlyRecordOnlyAndNamesEveryExcludedTypeWithItsReason()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RollExclude("));
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RollEmptyLog("));
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RollCappedLog("));
			Assert.That(u, Does.Contain(",ub_pool_empty,,{queueType},{reason};{string.Join(\"|\", rollExcluded)}"));
			Assert.That(u, Does.Contain("world.WorldTick - last < 250"), "rate limited per queue");
			Assert.That(u, Does.Contain("RollExclude(a.Name, rejectedBy);"), "the reject reason of each excluded type");
			Assert.That(u, Does.Contain("RollEmptyLog(queue.Info.Type, rollExcluded.Count > 0 ? \"all_excluded\" : \"none_listed_buildable\");"));
			Assert.That(u, Does.Contain("cappedOut.Length > 0 && !queue.BuildableItems().Any(a => !CappedOut(a)))\n\t\t\t\t{\n\t\t\t\t\tRollCappedLog(queue.Info.Type);\n\t\t\t\t\tcontinue;\n\t\t\t\t}"), "the cap skip keeps its continue");
		}

		[Test]
		public void WithTheUnitRollOffTheOldChooserRunsAndNoRollTermIsReadOutsideTheRollMethods()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");

			// the old chooser is still the one the fair loop takes when the roll is off
			Assert.That(u, Does.Contain("var unit = roll ? ChooseRollUnit(queue, skip) : fromFloor ?? (buildRandom"));
			Assert.That(u, Does.Contain("var roll = UnitRollOn;"));
			Assert.That(u, Does.Contain("bool UnitRollOn => switchesActive && Info.UnitRoll && FairService && !ablateUnitRoll.Off(player, AblationIds.UnitRoll);"));

			// the roll's own state (table, availability stamps, pick ids, excluded list) is touched only inside the roll methods and DEV log methods
			string Body(string signature)
			{
				var start = u.IndexOf(signature, StringComparison.Ordinal);
				Assert.That(start, Is.GreaterThan(0), signature);
				var open = u.IndexOf('{', start);
				// V08E: an expression-bodied member (=> ...;) on the signature line ends at its ';' - it has no braces of its own
				var arrow = u.IndexOf("=>", start, StringComparison.Ordinal);
				var eol = u.IndexOf('\n', start);
				if (arrow >= 0 && arrow < eol && (open < 0 || arrow < open))
					return u.Substring(start, u.IndexOf(';', arrow) - start + 1);

				var depth = 0;
				for (var i = open; i < u.Length; i++)
				{
					if (u[i] == '{')
						depth++;
					else if (u[i] == '}' && --depth == 0)
						return u.Substring(start, i - start + 1);
				}

				return u.Substring(start);
			}

			var outside = u;
			foreach (var signature in new[] { "ActorInfo ChooseRollUnit(", "void RollPickLog(", "void RollStartLog(", "void RollEmptyLog(", "void RollCappedLog(", "int RollStrength(int profile)", "void RollExclude(" })
				outside = outside.Replace(Body(signature), "");

			// V08F gate fix, TIER-MIX-01 (row ub_tech_lean): the one roll term read outside the roll methods is the ramp of the tech-lean admission, and it sits behind
			// `roll &&`, so with the roll off it is never evaluated and the old chooser runs unchanged.
			const string TechLeanRamp = "var leaned = roll && !paygo && TechLeanOn && TechLean.Admits(fullCost, rollPoolMaxCost, held, world.WorldTick, RollParameters.RampTicks,";
			Assert.That(outside, Does.Contain(TechLeanRamp), "the tech-lean ramp is read only behind the roll switch");
			outside = outside.Replace(TechLeanRamp, "");

			foreach (var term in new[] { "RollParameters.", "rollAvailableSince[", "rollAvailableSince.", "rollPickIds.", "rollPickSeq", "rollExcluded.", "UnitRoll.Quantize", "UnitRoll.FirstBoost" })
			{
				// declarations and the property are the only other mentions
				var count = outside.Split(new[] { term }, StringSplitOptions.None).Length - 1;
				Assert.That(count, Is.LessThanOrEqualTo(term == "rollPickSeq" ? 1 : 0), term + " is read only by the roll methods");
			}

			// NotifyStarted keeps its old body plus the DEV-only call
			Assert.That(u, Does.Contain("RollStartLog(name, queueType);\n\n\t\t\tforeach (var observer in UnitObservers)"));
		}

		[Test]
		public void AmongNotDoneCandidatesTheOldestAvailableGoesFirstFreeOrPaid()
		{
			var items = new[]
			{
				new UpgradeServiceCA.Candidate("heavy_new", 1000, 90, 5000),
				new UpgradeServiceCA.Candidate("light_old", 1000, 10, 100),
				new UpgradeServiceCA.Candidate("free_new", 0, 0, 9000),
			};
			Assert.That(UpgradeServiceCA.Choose(items, new HashSet<string>(), false), Is.EqualTo(1), "the oldest paid item goes before a newer free one");
			Assert.That(UpgradeServiceCA.Choose(items, new HashSet<string> { "light_old" }, false), Is.EqualTo(0), "then the next oldest");
			Assert.That(UpgradeServiceCA.Choose(new[] { new UpgradeServiceCA.Candidate("paid", 500, 90, 10), new UpgradeServiceCA.Candidate("free", 0, 0, 10) }, new HashSet<string>(), false), Is.EqualTo(1), "free only breaks a tie in Since");
			Assert.That(UpgradeServiceCA.Choose(new[] { new UpgradeServiceCA.Candidate("a", 1, 5, 10), new UpgradeServiceCA.Candidate("b", 1, 9, 10) }, new HashSet<string>(), false), Is.EqualTo(1), "equal since and cost class: the weight decides");
		}

		[Test]
		public void APromotionQueueItemReachesDoneThroughTheRegistryAndTheServiceCoversEveryQueueType()
		{
			// a promotion is a free item of the Promotions queue: once seen buildable and completed it is done, and is not offered again
			var r = new UpgradeCompletions();
			r.Observe("promotion.elite", 50, null);
			Assert.That(r.Complete("promotion.elite"), Is.True);
			Assert.That(r.Completed("promotion.elite"), Is.True);
			Assert.That(r.IsDone("promotion.elite"), Is.True);

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("public readonly string[] UpgradeServiceQueues = { \"Promotions\", \"Upgrades\", \"Research\", \"Addons\" };"), "every upgrade-like queue type is served");
			Assert.That(u, Does.Contain("if (!UpgradeQueueServed(category))"), "BuildingAddons only with its toggle");
			Assert.That(u, Does.Contain("upgradeDone.Observe(item.Name, world.WorldTick, item.TraitInfoOrDefault<BuildableInfo>()?.Prerequisites);"));
			// V08E re-pin: 1ce52d20 gates the once-done skip on upg_once_registry (onceOn)
			Assert.That(u, Does.Contain("if (onceOn && upgradeDone.IsDone(item.Name))\n\t\t\t\t\t\t\t\tcontinue;"), "a done type is not started again");
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.UpgradeQueue.cs"), Does.Contain("\"upg_building_addons\""));
			Assert.That(u, Does.Contain("bool UpgradeQueueServed(string category) =>\n\t\t\tcategory != \"BuildingAddons\" || (Info.UpgradeBuildingAddons"));

			// the DEV rows carry the queue name for every queue type
			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			Assert.That(dev, Does.Contain("AppendString(b, \"queue\", queue)"));
		}

		[Test]
		public void TheUnitStartAndPoolRowsAreDevOnlyAndCarryThePickId()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RollStartLog("));
			Assert.That(u, Does.Contain(",ub_unit_start,{name},{queueType},{pickId}"));
			Assert.That(u, Does.Contain(",ub_pool,{pool[pick].Name},{queueType},{pickId};floor="));
			Assert.That(u, Does.Contain("RollStartLog(name, queueType);"), "every start path ends in NotifyStarted");
			Assert.That(u, Does.Contain("var pickId = ++rollPickSeq;"), "every pick gets an id, logged or not");
		}

		// Workhorse review of c888455f: the squad manager's wave hold is fed by the roll whatever the wavemix profile strength, and from every candidate the gates admit.
		[Test]
		public void TheWaveHoldFeedDoesNotDependOnTheWavemixProfileAndSeesEveryBuildableRole()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			var report = body.IndexOf("var queueRoles = ReportWaveRoles(queue.Info.Type, items, roles);", StringComparison.Ordinal);
			var empty = body.IndexOf("if (items.Count == 0)", StringComparison.Ordinal);
			var firstOpen = body.IndexOf("var pool = items;", StringComparison.Ordinal);
			Assert.That(report, Is.GreaterThan(0));
			Assert.That(report, Is.LessThan(empty), "reported even when nothing is buildable");
			Assert.That(report, Is.LessThan(firstOpen), "reported before the pool is formed: the whole candidate list");
			Assert.That(body, Does.Contain("strength[UnitRoll.WaveMix] > 0 ? RollMissingWaveRoles(queueRoles) : null"), "the profile only reads the report");
			Assert.That(u, Does.Contain("waveMixAvailable[queueType] = (world.WorldTick, queueRoles);"));
		}

		[Test]
		public void TheFogOnlyObservationDropsAnEarlierOmniscientMemoryOnItsFirstCall()
		{
			var c = Source("mods/CA/Traits/BotModules/BotModuleLogic/AdaptiveCounterProduction.cs");
			var start = c.IndexOf("public void ObserveFogOnly(", StringComparison.Ordinal);
			var body = c.Substring(start, c.IndexOf("public long CounterScore(", start, StringComparison.Ordinal) - start);
			Assert.That(body, Does.Contain("if (!fogOnly)"));
			Assert.That(body, Does.Contain("observedEnemyValue.Clear();"));
		}

		// Opus 11-25 (coverage scorer): one DEV row per completed unit, so "built" is exact and not a count.
		// Opus 11-25 (user, LEARN-MULT-01): every roll number sits in ONE table the learner can read and override per player.
		[Test]
		public void TheParameterTableOwnsEveryRollNumberAndALearnerCanOverrideIt()
		{
			var fractions = new Dictionary<string, int> { ["e1"] = 10, ["e3"] = 5 };
			var early = new Dictionary<string, int> { ["rush.frontline"] = 70 };
			var late = new Dictionary<string, int> { ["rush.frontline"] = 140 };
			var strengths = new Dictionary<string, int> { ["counter"] = 200, ["wavemix"] = 0 };
			var t = new UnitRollTable(fractions, early, late, strengths, 5, 9000);

			Assert.That(t.Fraction("e1"), Is.EqualTo(10));
			Assert.That(t.Fraction("zzz"), Is.EqualTo(0), "not listed");
			Assert.That(t.Strength("counter"), Is.EqualTo(200));
			Assert.That(t.Strength("opening"), Is.EqualTo(0), "absent = off");
			Assert.That(t.FloorPct, Is.EqualTo(5));
			Assert.That(t.RampTicks, Is.EqualTo(9000));
			Assert.That(t.BiasPct("rush", "frontline", 0), Is.EqualTo(70));
			Assert.That(t.BiasPct("rush", "frontline", 4500), Is.EqualTo(105));
			Assert.That(t.BiasPct("turtle", "frontline", 0), Is.EqualTo(100));

			// same answers as the pure function the table replaces
			foreach (var tick in new[] { 0, 1000, 4500, 9000, 99999 })
				Assert.That(t.BiasPct("rush", "frontline", tick), Is.EqualTo(UnitRoll.RoleBiasPct(early, late, "rush", "frontline", tick, 9000)), "tick " + tick);

			t.Override(UnitRollTable.FractionKey("e1"), 40);
			t.Override(UnitRollTable.StrengthKey("counter"), 50);
			t.Override(UnitRollTable.BiasKey("rush", "frontline"), 100);
			t.Override(UnitRollTable.BiasLateKey("rush", "frontline"), 100);
			t.Override(UnitRollTable.FloorKey, 12);
			t.Override(UnitRollTable.RampKey, 0);
			Assert.That(t.Fraction("e1"), Is.EqualTo(40));
			Assert.That(t.Fraction("e3"), Is.EqualTo(5), "the other values keep the yaml number");
			Assert.That(t.Strength("counter"), Is.EqualTo(50));
			Assert.That(t.FloorPct, Is.EqualTo(12));
			Assert.That(t.BiasPct("rush", "frontline", 4500), Is.EqualTo(100));
			Assert.That(t.Overrides, Has.Count.EqualTo(6));

			t.ClearOverrides();
			Assert.That(t.Fraction("e1"), Is.EqualTo(10), "clearing restores the yaml value");
			Assert.That(t.BiasPct("rush", "frontline", 4500), Is.EqualTo(105));
		}

		[Test]
		public void TheRollReadsItsNumbersOnlyThroughTheTable()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			foreach (var yaml in new[] { "Info.UnitRollBias", "Info.UnitRollBiasLate", "Info.UnitRollRampTicks", "Info.UnitRollFloorPct", "Info.UnitRollProfiles", "table[" })
				Assert.That(body, Does.Not.Contain(yaml), yaml + " is read through RollParameters, so a learner override applies");

			Assert.That(u, Does.Contain("RollParameters.Strength(UnitRoll.ProfileNames[profile])"));
			// V08E re-pin: 686ca394 added the FirstBoost arguments, still all read from Info
			Assert.That(u, Does.Contain("new UnitRollTable(Info.UnitsToBuild, Info.UnitRollBias, Info.UnitRollBiasLate, Info.UnitRollProfiles, Info.UnitRollFloorPct, Info.UnitRollRampTicks,\n\t\t\t\tInfo.UnitRollFirstBoost, Info.UnitRollFirstRampTicks, Info.UnitRollFirstCap)"));
		}

		// Opus/Haiku 11-25: DEV rows that show whether every available upgrade / promotion / research / add-on gets researched.
		[Test]
		public void TheUpgradeCoverageRowsAreDevOnlyRecordOnlyAndOncePerSeatAndType()
		{
			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			Assert.That(dev, Does.Contain("UpgradeQueues = new HashSet<string>(units.SelectMany(u => u.Info.UpgradeServiceQueues)"), "the served queues come from the seat's UnitBuilder");
			Assert.That(dev, Does.Contain("cov.UpgradeAvailable.TryAdd(name, queue.Info.Type)"), "once per seat and type");
			Assert.That(dev, Does.Contain("EmitUpgradeRow(seat, \"ub_upgrade_available\", name, queue.Info.Type)"));
			Assert.That(dev, Does.Contain("cov.UpgradeDone.Add(pair.Key)"));
			// V08E re-pin: ded7416c adds the completing registry's source as the last argument
			Assert.That(dev, Does.Contain("EmitUpgradeRow(seat, \"ub_upgrade_done\", pair.Key, pair.Value, cov.UpgradeRegistries.Select(r => r.Source(pair.Key)).FirstOrDefault(x => x != null));"));
			Assert.That(dev, Does.Contain("cov.UpgradeRegistries.Any(r => r.Completed(pair.Key))"), "the module's registry decides completion");

			// the row is written only for a type first seen available, so a plain unit or building never writes it
			Assert.That(dev, Does.Contain("foreach (var pair in cov.UpgradeAvailable)"));
		}

		// Workhorse c5 nits.
		[Test]
		public void TheC5NitsAreFixed()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");

			// (1) a unit leaving a transport enters the world twice: only the first entry of an actor id counts as built
			var built = u.IndexOf("void RollUnitBuilt(Actor a)", StringComparison.Ordinal);
			Assert.That(u.Substring(built, 1100), Does.Contain("if (!rollBuiltSeen.Add(a.ActorID))"));

			// (2)(3) the first-build open slot (dead-factory prune, slot bound by item) is gone: the first build is a weight in one pool
			Assert.That(u, Does.Not.Contain("rollFirstOpen"));
		}

		[Test]
		public void EveryCompletedUnitWritesAnUbUnitBuiltRowInDevBuildsOnly()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tworld.ActorAdded += RollUnitBuilt;\n#endif"));
			Assert.That(u, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tworld.ActorAdded -= RollUnitBuilt;\n#endif"), "unsubscribed on disposal");
			var start = u.IndexOf("void RollUnitBuilt(Actor a)", StringComparison.Ordinal);
			var body = u.Substring(start, 1100);
			Assert.That(body, Does.Contain("IsTraitDisabled"), "only the enabled module instance writes");
			Assert.That(body, Does.Contain("world.WorldTick == 0"), "the starting units are not completions");
			Assert.That(body, Does.Contain("a.Owner != player"));
			Assert.That(body, Does.Contain("HasTraitInfo<BuildingInfo>()"), "units, not buildings");
			Assert.That(body, Does.Contain("ub_unit_built,{a.Info.Name},,"), "tick,seat,ub_unit_built,type,,listed");
		}

		[Test]
		public void TheProfileStrengthsInYamlNameKnownProfilesAndStayNonNegative()
		{
			var generic = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes
				.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes;
			var profiles = generic.Single(n => n.Key == "UnitRollProfiles").Value.Nodes;
			foreach (var p in profiles)
			{
				Assert.That(UnitRoll.ProfileNames, Does.Contain(p.Key), p.Key);
				Assert.That(int.Parse(p.Value.Value), Is.GreaterThanOrEqualTo(0), p.Key);
			}

			Assert.That(profiles.Select(p => p.Key), Is.EquivalentTo(UnitRoll.ProfileNames), "every profile has a strength row");
		}

		[Test]
		public void TheBiasTableUsesKnownPersonalitiesAndCombatRolesOnly()
		{
			var generic = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes
				.Single(n => n.Key == "UnitBuilderBotModuleCA@generic").Value.Nodes;
			var personalities = generic.Single(n => n.Key == "UnitRollPersonalities").Value.Value.Split(',', StringSplitOptions.TrimEntries);
			Assert.That(personalities, Is.EquivalentTo(new[] { "rush", "turtle", "tech", "expansion", "steamroller", "guerrilla" }));
			foreach (var table in new[] { "UnitRollBias", "UnitRollBiasLate" })
				foreach (var row in generic.Single(n => n.Key == table).Value.Nodes)
				{
					var parts = row.Key.Split('.');
					Assert.That(parts, Has.Length.EqualTo(2), row.Key);
					Assert.That(personalities, Does.Contain(parts[0]), row.Key);
					Assert.That(BotUnitRole.CombatRoles, Does.Contain(parts[1]), row.Key);
					Assert.That(int.Parse(row.Value.Value), Is.GreaterThan(0), row.Key);
				}

			Assert.That(int.Parse(generic.Single(n => n.Key == "UnitRollRampTicks").Value.Value), Is.GreaterThan(0));
			Assert.That(int.Parse(generic.Single(n => n.Key == "UnitRollFloorPct").Value.Value), Is.GreaterThan(0), "epsilon > 0");
		}
	}
}
