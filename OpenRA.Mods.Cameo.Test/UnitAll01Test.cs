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
			var mean = raw.Sum() / raw.Length;
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
			var total = UnitRoll.Total(w);
			for (var i = 0; i < 40000; i++)
				hits[UnitRoll.Pick(w, rng.NextDouble() * total)]++;

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
				var pick = unbuilt[UnitRoll.Pick(pool, rng.NextDouble() * UnitRoll.Total(pool))];
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
			Assert.That(body, Does.Contain("ShouldBuildReject(a.Name, true, queue.Info.Type) != null"), "delay, interval, limit, capturer and item-aware caps still apply");
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

		[Test]
		public void ExtremeWeightsKeepEveryCandidateReachableAndNothingWraps()
		{
			// the last candidate stays reachable next to a huge one (no int cast of a sum, no wrap)
			var w = UnitRoll.Weights(new[] { 1_000_000, 1_000_000, 1 }, new[] { 100_000, 100_000, 100 }, new[] { 100_000, 100_000, 100 }, 1_000_000, new[] { 64.0, 64.0, 64.0 });
			Assert.That(w.All(x => x > 0 && !double.IsNaN(x) && !double.IsInfinity(x)), Is.True);
			var total = UnitRoll.Total(w);
			Assert.That(total, Is.GreaterThan(0));
			Assert.That(UnitRoll.Pick(w, total - 1e-6), Is.EqualTo(2), "a roll at the very end lands on the last candidate");
			Assert.That(UnitRoll.Pick(w, 0), Is.EqualTo(0));
			Assert.That(UnitRoll.Pick(w, total * 2), Is.EqualTo(2), "a roll past the end falls to the last candidate, never an index error");

			// all-zero except the floor: equal, positive
			var z = UnitRoll.Weights(new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, new[] { 0, 0, 0 }, 0);
			Assert.That(z, Is.EqualTo(new double[] { 1, 1, 1 }));
			Assert.That(UnitRoll.Pick(z, 2.5), Is.EqualTo(2));

			// the uniform draw: the top of the resolution maps below the total
			var top = (UnitRoll.RollResolution - 1) / (double)UnitRoll.RollResolution * total;
			Assert.That(UnitRoll.Pick(w, top), Is.InRange(0, 2));
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

		// UPGRADE-COVERAGE once-done registry (Opus 11-25).
		[Test]
		public void TheOnceDoneRegistryCountsARepeatableAfterItsFirstCompletionAndAnExclusiveSetOnce()
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
			Assert.That(r.IsDone("opt_x"), Is.True, "any one option satisfies the whole exclusive set");

			Assert.That(r.Complete("never_seen"), Is.False, "a type never seen buildable (a unit, a building) marks nothing");
			Assert.That(r.IsDone("never_seen"), Is.False);
			Assert.That(r.Since("upgrade_a"), Is.EqualTo(100));
			Assert.That(r.Since("never_seen"), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void AmongNotDoneCandidatesTheOldestAvailableGoesFirstAfterFreeItems()
		{
			var items = new[]
			{
				new UpgradeServiceCA.Candidate("heavy_new", 1000, 90, 5000),
				new UpgradeServiceCA.Candidate("light_old", 1000, 10, 100),
				new UpgradeServiceCA.Candidate("free_new", 0, 0, 9000),
			};
			Assert.That(UpgradeServiceCA.Choose(items, new HashSet<string>(), false), Is.EqualTo(2), "free first");
			Assert.That(UpgradeServiceCA.Choose(items, new HashSet<string> { "free_new" }, false), Is.EqualTo(1), "the older one before the heavier weight");
			Assert.That(UpgradeServiceCA.Choose(new[] { new UpgradeServiceCA.Candidate("a", 1, 5, 10), new UpgradeServiceCA.Candidate("b", 1, 9, 10) }, new HashSet<string>(), false), Is.EqualTo(1), "equal since: the weight decides");
		}

		[Test]
		public void APromotionQueueItemReachesDoneThroughTheRegistryAndTheServiceCoversEveryQueueType()
		{
			// a promotion is a free item of the Promotions queue: once seen buildable and completed it is done, and is not offered again
			var r = new UpgradeCompletions();
			r.Observe("promotion.elite", 50, null);
			Assert.That(r.Complete("promotion.elite"), Is.True);
			Assert.That(r.IsDone("promotion.elite"), Is.True);

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("public readonly string[] UpgradeServiceQueues = { \"Promotions\", \"Upgrades\", \"Research\", \"Addons\" };"), "every upgrade-like queue type is served");
			Assert.That(u, Does.Contain("if (!UpgradeQueueServed(category))"), "BuildingAddons only with its toggle");
			Assert.That(u, Does.Contain("upgradeDone.Observe(item.Name, world.WorldTick, item.TraitInfoOrDefault<BuildableInfo>()?.Prerequisites);"));
			Assert.That(u, Does.Contain("if (upgradeDone.IsDone(item.Name))\n\t\t\t\t\t\t\t\tcontinue;"), "a done type is not started again");
			Assert.That(u, Does.Contain("world.ActorAdded += UpgradeCompleted;"));
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
			Assert.That(u, Does.Contain("new UnitRollTable(Info.UnitsToBuild, Info.UnitRollBias, Info.UnitRollBiasLate, Info.UnitRollProfiles, Info.UnitRollFloorPct, Info.UnitRollRampTicks)"));
		}

		// Opus/Haiku 11-25: DEV rows that show whether every available upgrade / promotion / research / add-on gets researched.
		[Test]
		public void TheUpgradeCoverageRowsAreDevOnlyRecordOnlyAndOncePerSeatAndType()
		{
			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			Assert.That(dev, Does.Contain("UpgradeQueues = new HashSet<string>(units.SelectMany(u => u.Info.UpgradeServiceQueues)"), "the served queues come from the seat's UnitBuilder");
			Assert.That(dev, Does.Contain("cov.UpgradeAvailable.TryAdd(name, queue.Info.Type)"), "once per seat and type");
			Assert.That(dev, Does.Contain("EmitUpgradeRow(seat, \"ub_upgrade_available\", name, queue.Info.Type)"));
			Assert.That(dev, Does.Contain("cov.UpgradeDone.Add(name)"));
			Assert.That(dev, Does.Contain("EmitUpgradeRow(seat, \"ub_upgrade_done\", name, queue)"));
			Assert.That(dev, Does.Contain("world.ActorAdded += UpgradeDoneSafe;"));
			Assert.That(dev, Does.Contain("Fail(e, \"ub_upgrade_done\")"), "a fault ends the channel, never the game");

			// the row is written only for a type first seen available, so a plain unit or building never writes it
			var done = dev.IndexOf("void UpgradeDoneSafe(", StringComparison.Ordinal);
			Assert.That(dev.Substring(done, 900), Does.Contain("cov.UpgradeAvailable.TryGetValue(name, out var queue)"));
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
