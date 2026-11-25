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

		static long[] Roll(int[] bias = null, int[] counter = null, int floor = 5) =>
			UnitRoll.Weights(Fractions, bias ?? Enumerable.Repeat(100, Names.Length).ToArray(), counter ?? Enumerable.Repeat(100, Names.Length).ToArray(), floor);

		[Test]
		public void EveryListedTypeKeepsAPositiveWeightEvenWithAZeroFractionOrABiasOfZero()
		{
			var w = Roll();
			Assert.That(w.All(x => x > 0), Is.True, "floor > 0");
			var biasZero = Enumerable.Repeat(0, Names.Length).ToArray();
			Assert.That(Roll(biasZero).All(x => x > 0), Is.True, "even a zero bias leaves weight >= 1");
			Assert.That(UnitRoll.Weights(new[] { 0, 0 }, new[] { 100, 100 }, new[] { 100, 100 }, 5), Is.EqualTo(new long[] { 1, 1 }), "all-zero fractions roll equally");
		}

		[Test]
		public void TheFloorIsEpsilonOfTheMeanAndNeverLowersAHeavyType()
		{
			var w = Roll(floor: 10);
			var raw = Roll(floor: 0);
			var mean = raw.Sum() / raw.Length;
			Assert.That(w[7], Is.EqualTo(mean * 10 / 100), "the zero-fraction orca takes the floor");
			Assert.That(w[0], Is.EqualTo(raw[0]), "a heavy type is untouched");
			Assert.That(w[4], Is.GreaterThanOrEqualTo(mean * 10 / 100));
		}

		[Test]
		public void OverManyRollsEveryListedTypeIsPickedAndTheHeavyOnesLead()
		{
			var w = Roll();
			var rng = new Random(1234);
			var hits = new int[Names.Length];
			var total = (int)UnitRoll.Total(w);
			for (var i = 0; i < 40000; i++)
				hits[UnitRoll.Pick(w, rng.Next(total))]++;

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
				var pick = unbuilt[UnitRoll.Pick(pool, rng.Next((int)UnitRoll.Total(pool)))];
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
			var noCandidate = u.IndexOf("if (roll && unit == null)\n\t\t\t\t\t\tbreak;", rollPick, StringComparison.Ordinal);
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
			Assert.That(body, Does.Contain("fractions[i] = table[name];"));
			Assert.That(body, Does.Contain("UnitRoll.Weights(fractions, biases, counterPcts, Info.UnitRollFloorPct)"));
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
			Assert.That(u, Does.Contain("if (UnitRollOn)\n\t\t\t\trollOrdered.Add(name);"), "a type counts as ordered once an order was emitted for it, by any path");

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

		// c3 (Opus 11-25): an unaffordable first-of-each is pay-as-you-go, like TECHCHAIN-PAYGO: it queues with any bank above 0, at most one open per queue, no savings hold.
		[Test]
		public void TheFirstBuildOfATypeQueuesWithAnyBankAndOnlyOneIsOpenPerQueue()
		{
			Assert.That(UnitRoll.FirstAdmits(1), Is.True);
			Assert.That(UnitRoll.FirstAdmits(0), Is.False, "an empty bank still waits");

			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("var paygo = roll && rollFirst && UnitRoll.FirstAdmits(batch.BankLeft);\n\t\t\t\t\tvar cost = paygo ? 0 : fullCost;"), "only the first-of-each sees cost 0 at the afford and reserve gates");
			Assert.That(u, Does.Contain("batch.Reserve(queue, fullCost);"), "the order still reserves its full price");
			Assert.That(u, Does.Contain("rollFirstOpen[queue.Actor.ActorID + \"|\" + queue.Info.Type] = (name, world.WorldTick);"));

			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			Assert.That(body, Does.Contain("open.Tick == world.WorldTick || queue.AllQueued().Any(i => i.Item == open.Name)"), "open while queued, and for the rest of the pass that ordered it");
			Assert.That(body, Does.Contain("items = items.Where(a => rollOrdered.Contains(a.Name)).ToList();"), "while one is open the other never-built types wait; ordinary types still roll");
			Assert.That(body, Does.Not.Contain("savings"), "no savings hold");
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
