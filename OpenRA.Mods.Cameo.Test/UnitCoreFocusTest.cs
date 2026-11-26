#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// HANS-PROFILE-01 row 2 (ub_core_focus). Hans' top 3 unit types are 48 to 64 percent of his unit requests; the build-everything roll spreads over all of them. Once a type has been built
	/// once, the 3 dearest types of a queue that the bank can pay for (the core) get a weight multiplier so that they hold about the target share of the weight of the types built once.
	/// A bias only: only the core's weights change; every other weight, never-built types included, stays as the roll made it; nothing is removed from the pool.
	/// </summary>
	[TestFixture]
	public class UnitCoreFocusTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static bool[] Mask(int n, params int[] on)
		{
			var m = new bool[n];
			foreach (var i in on)
				m[i] = true;

			return m;
		}

		static bool[] All(int n) => Enumerable.Repeat(true, n).ToArray();

		[Test]
		public void AffordableNeedsABankAndTheShareOfThePrice()
		{
			Assert.That(CoreFocus.Affordable(1000, 300, 30), Is.True, "300 x 100 = 30000 >= 1000 x 30");
			Assert.That(CoreFocus.Affordable(1000, 299, 30), Is.False);
			Assert.That(CoreFocus.Affordable(1000, 0, 30), Is.False, "an empty bank affords nothing");
			Assert.That(CoreFocus.Affordable(1000, -50, 30), Is.False);
			Assert.That(CoreFocus.Affordable(0, 500, 30), Is.False, "no price, no core member");
			Assert.That(CoreFocus.Affordable(-5, 500, 30), Is.False);
			Assert.That(CoreFocus.Affordable(1000, 1, 0), Is.True, "bankPct 0 = any bank above 0");
			Assert.That(CoreFocus.Affordable(1000, 1, -10), Is.True, "a negative percent is 0, not a free pass for an empty bank");
			Assert.That(CoreFocus.Affordable(1000, 999, 500), Is.False, "above 100 behaves as 100: the whole price");
			Assert.That(CoreFocus.Affordable(1000, 1000, 500), Is.True);
			Assert.That(CoreFocus.Affordable(int.MaxValue, int.MaxValue, 100), Is.True, "64-bit math");
			Assert.That(CoreFocus.Affordable(int.MaxValue, int.MaxValue - 1, 100), Is.False);
		}

		[Test]
		public void TheCoreIsTheDearestEligibleTypesWithAStableOrder()
		{
			var price = new[] { 500, 1200, 900, 1200, 300 };
			Assert.That(CoreFocus.Core(price, All(5), 3), Is.EqualTo(new[] { 1, 3, 2 }), "dearest first, equal prices keep the lower index first");
			Assert.That(CoreFocus.Core(price, Mask(5, 0, 2, 3, 4), 3), Is.EqualTo(new[] { 3, 2, 0 }), "an ineligible type (not built once, or not affordable) is never in the core");
			Assert.That(CoreFocus.Core(price, Mask(5, 0, 2, 3, 4), 10), Is.EqualTo(new[] { 3, 2, 0, 4 }), "fewer eligible than the count: all of them");
			Assert.That(CoreFocus.Core(new[] { 500, 1200, 0, 1200, 300 }, All(5), 3), Is.EqualTo(new[] { 1, 3, 0 }), "a type with no price is skipped");
			Assert.That(CoreFocus.Core(price, All(5), 0), Is.Empty, "count 0 = no core");
			Assert.That(CoreFocus.Core(price, All(5), -1), Is.Empty);
			Assert.That(CoreFocus.Core(new int[0], new bool[0], 3), Is.Empty);
			Assert.That(CoreFocus.Core(price, Mask(5), 3), Is.Empty, "nothing eligible");
		}

		[Test]
		public void TheMultiplierLiftsTheCoreToTheTargetShareOfTheSettledWeight()
		{
			// 3 core + 6 rest, all weight 100: the core holds 33%; 50% needs x2.
			var w = Enumerable.Repeat(100.0, 9).ToArray();
			var core = Mask(9, 0, 1, 2);
			Assert.That(CoreFocus.SharePct(w, All(9), core), Is.EqualTo(33));
			Assert.That(CoreFocus.MultiplierPct(w, All(9), core, 50, 800), Is.EqualTo(200));
			var lifted = CoreFocus.Apply(w, core, 200);
			Assert.That(lifted, Is.EqualTo(new[] { 200.0, 200, 200, 100, 100, 100, 100, 100, 100 }));
			Assert.That(CoreFocus.SharePct(lifted, All(9), core), Is.EqualTo(50));

			// The multiplier rounds down to a whole percent (the target is "about").
			Assert.That(CoreFocus.MultiplierPct(Enumerable.Repeat(100.0, 7).ToArray(), All(7), Mask(7, 0, 1, 2), 50, 800), Is.EqualTo(133));

			// A core that already holds the share is left alone: the row only ever leans toward the core.
			Assert.That(CoreFocus.MultiplierPct(new[] { 300.0, 300, 300, 10, 10, 10 }, All(6), Mask(6, 0, 1, 2), 50, 800), Is.EqualTo(100));
		}

		[Test]
		public void NeverBuiltTypesAreNotCountedAndTheirFirstBuildPullIsUntouched()
		{
			// Two never-built types carry a huge first-build weight; they are outside the share, so the multiplier is the same as without them.
			var w = Enumerable.Repeat(100.0, 9).Concat(new[] { 5000.0, 5000.0 }).ToArray();
			var settled = Mask(11, 0, 1, 2, 3, 4, 5, 6, 7, 8);
			var core = Mask(11, 0, 1, 2);
			Assert.That(CoreFocus.MultiplierPct(w, settled, core, 50, 800), Is.EqualTo(200));
			var lifted = CoreFocus.Apply(w, core, 200);
			Assert.That(lifted[9], Is.EqualTo(5000.0), "a first-build weight is never touched");
			Assert.That(lifted[10], Is.EqualTo(5000.0));

			// A settled-only core with no settled rest has nothing to lean away from.
			Assert.That(CoreFocus.MultiplierPct(new[] { 100.0, 100, 100, 6400 }, Mask(4, 0, 1, 2), Mask(4, 0, 1, 2), 50, 800), Is.EqualTo(100));
		}

		[Test]
		public void TheMultiplierIsCappedAndBadInputsAreOff()
		{
			var w = new[] { 1.0, 1, 1, 1000, 1000, 1000 };
			var core = Mask(6, 0, 1, 2);
			Assert.That(CoreFocus.MultiplierPct(w, All(6), core, 50, 800), Is.EqualTo(800), "capped by maxPct");
			Assert.That(CoreFocus.MultiplierPct(w, All(6), core, 50, 50), Is.EqualTo(100), "a cap below 100 never lowers the core: no lift");
			Assert.That(CoreFocus.MultiplierPct(w, All(6), core, 0, 800), Is.EqualTo(100), "share 0 = row off");
			Assert.That(CoreFocus.MultiplierPct(w, All(6), core, -5, 800), Is.EqualTo(100));

			var v = new[] { 100.0, 100, 100, 200, 200, 200 };
			Assert.That(CoreFocus.MultiplierPct(v, All(6), core, 99, 800), Is.EqualTo(800));
			Assert.That(CoreFocus.MultiplierPct(v, All(6), core, 150, 800), Is.EqualTo(800), "a share above 99 behaves as 99 (there is always a rest to compare with)");

			// Garbage weights count as 0; no core weight or no rest weight = no lift.
			Assert.That(CoreFocus.MultiplierPct(new[] { 0.0, double.NaN, 0, 100, 100, 100 }, All(6), core, 50, 800), Is.EqualTo(100));
			Assert.That(CoreFocus.MultiplierPct(new[] { 100.0, 100, 100, 0, 0, 0 }, All(6), core, 50, 800), Is.EqualTo(100));
			Assert.That(CoreFocus.MultiplierPct(new[] { 100.0, 100, 100 }, All(3), All(3), 50, 800), Is.EqualTo(100), "every settled type is core: nothing to lean away from");
			Assert.That(CoreFocus.MultiplierPct(new[] { double.PositiveInfinity, 100, 100, 100 }, All(4), Mask(4, 0), 50, 800), Is.EqualTo(100), "an infinite weight is garbage, not the whole share");
			Assert.That(CoreFocus.SharePct(new double[] { 0, 0 }, All(2), Mask(2, 0)), Is.EqualTo(0));
		}

		[Test]
		public void OnlyTheCoreChangesAndAMultiplierOf100OrLessChangesNothing()
		{
			// Core x2: ONLY the core changes. The small types are NOT lifted to a floor of the new mean (CM review of b6e34a887): their weights stay exactly as the roll made them.
			var lifted = CoreFocus.Apply(new[] { 1000.0, 10, 1 }, Mask(3, 0), 200);
			Assert.That(lifted, Is.EqualTo(new[] { 2000.0, 10, 1 }));

			// 100 or less: the weights come back as they were, the floor is not re-applied.
			Assert.That(CoreFocus.Apply(new[] { 1000.0, 10, 1 }, Mask(3, 0), 100), Is.EqualTo(new[] { 1000.0, 10, 1 }));
			Assert.That(CoreFocus.Apply(new[] { 1000.0, 10, 1 }, Mask(3, 0), 0), Is.EqualTo(new[] { 1000.0, 10, 1 }));
			Assert.That(CoreFocus.Apply(new[] { 1000.0, 10, 1 }, Mask(3, 0), -50), Is.EqualTo(new[] { 1000.0, 10, 1 }));
			Assert.That(CoreFocus.Apply(new double[0], new bool[0], 200), Is.Empty);

			// The weight cap of the roll holds, and every candidate still owns a drawable interval after the integer quantisation.
			var capped = CoreFocus.Apply(new[] { 1e12, 1 }, Mask(2, 0), 800);
			Assert.That(capped[0], Is.EqualTo(UnitRoll.WeightCap));
			Assert.That(capped[1], Is.EqualTo(1.0), "the rest is untouched");
			var weights = UnitRoll.Weights(new[] { 5, 5, 5, 5, 5, 5, 5, 5, 5 }, Enumerable.Repeat(100, 9).ToArray(), Enumerable.Repeat(100, 9).ToArray(), 5);
			var focused = CoreFocus.Apply(weights, Mask(9, 0, 1, 2), 800);
			var units = UnitRoll.Quantize(focused);
			Assert.That(units.All(u => u >= 1), Is.True, "no candidate drops out of the roll");
			Assert.That(UnitRoll.Sum(units), Is.LessThanOrEqualTo(1 << UnitRoll.DrawBits));
			for (var i = 3; i < 9; i++)
				Assert.That(focused[i], Is.EqualTo(weights[i]), "a type outside the core is exactly as the roll made it");
		}

		[Test]
		public void TheRowHasItsIdItsInfoFieldsAndItsYamlKeysOnlyInTheGenericBlock()
		{
			Assert.That(AblationIds.UnitCoreFocus, Is.EqualTo("ub_core_focus"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.UnitCoreFocus.cs"), Does.Contain("public const string UnitCoreFocus = \"ub_core_focus\";"));

			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("public readonly int CoreFocusSharePct = 0;"), "off in code: only the yaml arms it");
			Assert.That(unit, Does.Contain("public readonly int CoreFocusCount = 3;"));
			Assert.That(unit, Does.Contain("public readonly int CoreFocusMaxPct = 800;"));
			Assert.That(unit, Does.Contain("public readonly int CoreFocusBankPct = 30;"));

			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs");
			Assert.That(part, Does.Contain("AblationLatch ablateCoreFocus;"));
			Assert.That(part, Does.Contain("bool CoreFocusOn => UnitRollOn && Info.CoreFocusSharePct > 0 && !ablateCoreFocus.Off(player, AblationIds.UnitCoreFocus);"), "needs the roll, an armed share and the row");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var generic = yaml.IndexOf("\tUnitBuilderBotModuleCA@generic:", StringComparison.Ordinal);
			var classic = yaml.IndexOf("\tUnitBuilderBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(generic, Is.GreaterThan(0));
			Assert.That(classic, Is.GreaterThan(generic));
			var genericBlock = yaml[generic..classic];
			Assert.That(genericBlock, Does.Contain("\t\tCoreFocusSharePct: 50\n"));
			Assert.That(genericBlock, Does.Contain("\t\tCoreFocusCount: 3\n"));
			Assert.That(genericBlock, Does.Contain("\t\tCoreFocusMaxPct: 800\n"));
			Assert.That(genericBlock, Does.Contain("\t\tCoreFocusBankPct: 30\n"));
			Assert.That(genericBlock, Does.Contain("\t\tUnitRoll: true\n"), "the row rides on the unit roll of the same block");
			Assert.That(yaml[classic..], Does.Not.Contain("CoreFocus"), "classic keeps its old unit pick");
		}

		[Test]
		public void TheLiftIsTheLastStepOnTheWeightsAndTheRollSignatureIsUntouched()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var start = unit.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = unit.Substring(start, unit.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);

			var weights = body.IndexOf("var weights = UnitRoll.Weights(fractions, biases, counterPcts, RollParameters.FloorPct, boosts);", StringComparison.Ordinal);
			var lift = body.IndexOf("if (CoreFocusOn)\n\t\t\t\tweights = FocusCore(queue, pool, weights);", StringComparison.Ordinal);
			var quantize = body.IndexOf("var units = UnitRoll.Quantize(weights);", StringComparison.Ordinal);
			var log = body.IndexOf("RollPickLog(queue.Info.Type, pool, weights, pick,", StringComparison.Ordinal);
			Assert.That(weights, Is.GreaterThan(0));
			Assert.That(lift, Is.GreaterThan(weights), "after the roll's own weights (floor, boosts, counters, profiles)");
			Assert.That(quantize, Is.GreaterThan(lift), "before the integer draw");
			Assert.That(log, Is.GreaterThan(quantize), "the pick log shows the weights the draw used");
			Assert.That(body, Does.Not.Contain("rollOrdered.Add"), "ordering coverage is not touched here");

			// The call and the signature the older pins read stay as they were.
			Assert.That(unit, Does.Contain("var unit = roll ? ChooseRollUnit(queue, skip) : fromFloor ?? (buildRandom"));

			// The bank is read before the roll, from the pass's own batch, and only while the row is on.
			var bank = unit.IndexOf("rollBankLeft = CoreFocusOn ? batch.BankLeft : 0;", StringComparison.Ordinal);
			var call = unit.IndexOf("var unit = roll ? ChooseRollUnit(queue, skip)", StringComparison.Ordinal);
			Assert.That(bank, Is.GreaterThan(0));
			Assert.That(call, Is.GreaterThan(bank));

			// The helper reads the same ordering the first-build boost reads, the queue's own price, and the yaml values.
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs");
			Assert.That(part, Does.Contain("settled[i] = rollOrdered.Contains(pool[i].Name);"));
			Assert.That(part, Does.Contain("price[i] = queue.GetProductionCost(pool[i]);"));
			Assert.That(part, Does.Contain("CoreFocus.Affordable(price[i], rollBankLeft, RollParameters.CoreFocusBankPct)"));
			Assert.That(part, Does.Contain("CoreFocus.Core(price, eligible, RollParameters.CoreFocusCount)"));
			Assert.That(part, Does.Contain("CoreFocus.MultiplierPct(weights, settled, core, RollParameters.CoreFocusSharePct, RollParameters.CoreFocusMaxPct)"));
			Assert.That(part, Does.Contain("CoreFocus.Apply(weights, core, multiplier)"));
		}

		[Test]
		public void TheSeamIsWorldFreeAndTheDevRowIsCompiledOutOfRelease()
		{
			var seam = RepoFile(Bots + "BotModuleLogic/CoreFocus.cs");
			foreach (var banned in new[] { "Random", "World", "Actor", "Player", "FindActors" })
				Assert.That(seam, Does.Not.Contain(banned), banned);

			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs");
			Assert.That(part, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid CoreFocusLog(string queueType, List<ActorInfo> pool, List<int> core, int multiplier, int shareBefore, int shareAfter)"));
			Assert.That(part, Does.Contain(",ub_core_focus,,{queueType},{multiplier};core={string.Join(\"|\", names)};{shareBefore};{shareAfter}"));

			// Opus 11-26: the DEV churn count of the core set (changes per 1000 ticks), recorded for every roll that names a set, an empty set included, and compiled out of release.
			Assert.That(part, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid CoreFocusChurn(string queueType, List<ActorInfo> pool, List<int> core)"));
			Assert.That(part, Does.Contain(",ub_core_churn,,{queueType},{state.Changes};{now - state.Start};core={key}"));
			Assert.That(part, Does.Contain("if (now - state.Start >= 1000)"), "one row per queue type per 1000 ticks");
			Assert.That(part, Does.Contain("names.Sort(StringComparer.Ordinal);"), "a set, not an order: a price tie that reorders is no change");
			Assert.That(part, Does.Contain("var picked = CoreFocus.Core(price, eligible, RollParameters.CoreFocusCount);\n\t\t\tCoreFocusChurn(queue.Info.Type, pool, picked);\n\t\t\tif (picked.Count == 0)"), "counted before the empty-set return");
		}
	}
}
