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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// MODE-BIAS-01 (user ruling 2026-11-25): the extra building lists also become mode bias profiles, a milli weight on those types in the BuildingFractions roll
	// while the list's trigger holds, never a gate. The pure order and the combine are tested directly, the planner with a plain snapshot, the world wiring by
	// pins, the arm by the yaml.
	[TestFixture]
	public sealed class ModeBiasTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		static readonly string[] Names = ["a", "b", "c", "d", "e"];

		static KeyValuePair<string, int>[] Fractions() => Names.Select(n => new KeyValuePair<string, int>(n, 10)).ToArray();

		static BaseBuildSnapshot Snap(Func<string, int> bias) => new()
		{
			Bank = 5000,
			Owned = new Dictionary<string, int>(),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = Fractions(),
			BiasMilli = bias,
		};

		static List<string> Candidates(BaseBuildSnapshot s, BaseBuildStage stage, int seed) =>
			BaseBuildPlanCA.StageCandidates(stage, s, new MersenneTwister(seed)).Select(c => c.Name).ToList();

		[Test]
		public void TheWeightedOrderIsDeterministicAndYieldsEveryEntryOnce()
		{
			var weights = new[] { 1000, 1, 5000, 1, 1000 };
			var a = new MersenneTwister(42);
			var b = new MersenneTwister(42);
			var first = ModeBias.WeightedIndexOrder(weights, a).ToList();
			var second = ModeBias.WeightedIndexOrder(weights, b).ToList();
			Assert.That(first, Is.EqualTo(second), "one seed, one order");
			Assert.That(first.OrderBy(i => i), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }), "every entry exactly once, the weight-1 ones included");
			Assert.That(a.Next(1000000), Is.EqualTo(b.Next(1000000)), "the same number of draws");

			for (var seed = 0; seed < 50; seed++)
				Assert.That(ModeBias.WeightedIndexOrder(new[] { 0, -5, 1, 1_000_000 }, new MersenneTwister(seed)).OrderBy(i => i), Is.EqualTo(new[] { 0, 1, 2, 3 }), "a weight below 1 counts as 1");
		}

		[Test]
		public void TheWeightedOrderIsLazyOneDrawPerEntryReached()
		{
			var rng = new MersenneTwister(9);
			var reference = new MersenneTwister(9);
			var taken = ModeBias.WeightedIndexOrder(new[] { 1000, 1000, 1000, 1000 }, rng).Take(1).ToList();
			Assert.That(taken, Has.Count.EqualTo(1));
			reference.Next(4000);
			Assert.That(rng.Next(1000000), Is.EqualTo(reference.Next(1000000)), "one draw for the first entry only");
		}

		[Test]
		public void AHeavyEntryComesFirstFarMoreOften()
		{
			var firsts = new int[5];
			for (var seed = 1; seed <= 600; seed++)
				firsts[ModeBias.WeightedIndexOrder(new[] { 1000, 1000, 5000, 1000, 1000 }, new MersenneTwister(seed)).First()]++;

			// Expected 5000/9000 = 333 of 600 for the heavy entry, 67 for each other one.
			var others = firsts.Where((_, i) => i != 2).ToArray();
			Assert.That(firsts[2], Is.GreaterThan(2 * others.Max()), string.Join(",", firsts));
			Assert.That(others.Min(), Is.GreaterThan(0), "the floor: the others still come first sometimes");
		}

		[Test]
		public void TheFractionsOrderIsSortedOrdinalThenWeighted()
		{
			var reversed = Names.Reverse().Select(n => new KeyValuePair<string, int>(n, 10)).ToArray();
			var order = ModeBias.WeightedOrder(reversed, _ => 1000, new MersenneTwister(3)).Select(kv => kv.Key).ToList();
			var byIndex = ModeBias.WeightedIndexOrder(new[] { 1000, 1000, 1000, 1000, 1000 }, new MersenneTwister(3)).Select(i => Names[i]).ToList();
			Assert.That(order, Is.EqualTo(byIndex), "the same indices over the ordinal key order");
		}

		[Test]
		public void WithNoBiasTheRollIsTheOldShuffle()
		{
			foreach (var stage in new[] { BaseBuildStage.S10Fractions, BaseBuildStage.S5CoreOnce })
			{
				for (var seed = 0; seed < 20; seed++)
				{
					var expected = Fractions().OrderBy(kv => kv.Key, StringComparer.Ordinal).Shuffle(new MersenneTwister(seed)).Select(kv => kv.Key).ToList();
					Assert.That(Candidates(Snap(null), stage, seed), Is.EqualTo(expected), stage + " seed " + seed);
				}
			}
		}

		[Test]
		public void WithABiasTheRollIsTheWeightedOrderAndEveryEntryStaysACandidate()
		{
			var s = Snap(name => name == "c" ? 5000 : 1000);
			foreach (var stage in new[] { BaseBuildStage.S10Fractions, BaseBuildStage.S5CoreOnce })
			{
				var names = Candidates(s, stage, 11);
				Assert.That(names.OrderBy(n => n, StringComparer.Ordinal), Is.EqualTo(Names), stage + ": the floor, nothing is gated out");

				// S5 weighs by the combined weight (base fraction x milli, BUILD-ALL-B); S10 by the mode bias milli alone.
				var weights = stage == BaseBuildStage.S5CoreOnce ? new[] { 10000, 10000, 50000, 10000, 10000 } : new[] { 1000, 1000, 5000, 1000, 1000 };
				var expected = ModeBias.WeightedIndexOrder(weights, new MersenneTwister(11)).Select(i => Names[i]).ToList();
				Assert.That(names, Is.EqualTo(expected), stage.ToString());
			}
		}

		[Test]
		public void TheS5OrderUsesTheCombinedWeightAHigherWeightGoesFirst()
		{
			var s = Snap(null);
			s.Fractions = new[] { new KeyValuePair<string, int>("low", 10), new KeyValuePair<string, int>("high", 10) };
			s.FractionMilli = name => name == "high" ? 100000 : 1000; // personality x demand
			Assert.That(BaseBuildPlanCA.CombinedWeight(s, "high", 10), Is.GreaterThan(BaseBuildPlanCA.CombinedWeight(s, "low", 10)));
			Assert.That(BaseBuildPlanCA.CombinedWeight(s, "none", 0), Is.EqualTo(1000), "the floor: a base of 0 counts as 1, so the type keeps a chance");
			var highFirst = 0;
			for (var seed = 0; seed < 50; seed++)
				if (Candidates(s, BaseBuildStage.S5CoreOnce, seed)[0] == "high")
					highFirst++;

			Assert.That(highFirst, Is.GreaterThan(45), "weight 1,000,000 against 10,000 leads in nearly every roll");
		}

		[Test]
		public void TheBiasWidensTheShareCap()
		{
			var s = Snap(null);
			s.Owned = new Dictionary<string, int> { ["a"] = 2 };
			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "a", 10), Is.False, "2 of 10 is over a 10% share");
			s.BiasMilli = name => name == "a" ? 2000 : 1000;
			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "a", 10), Is.True, "2000 milli doubles the share");
			s.BiasMilli = _ => 0;
			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "b", 10), Is.True, "a type with no copies always passes");
			s.Owned = new Dictionary<string, int> { ["a"] = 1 };
			Assert.That(BaseBuildPlanCA.FractionStageAdmits(s, "a", 10), Is.False, "a bias of 0 counts as 1 milli");
		}

		[Test]
		public void TheCombineClampsToOneAndTheCap()
		{
			Assert.That(ModeBias.Combine(ModeBias.Neutral, 2000, 20000), Is.EqualTo(2000));
			Assert.That(ModeBias.Combine(2000, 2000, 20000), Is.EqualTo(4000), "x strength / 1000 per extra factor");
			Assert.That(ModeBias.Combine(15000, 5000, 20000), Is.EqualTo(20000), "the cap");
			Assert.That(ModeBias.Combine(int.MaxValue, int.MaxValue, 20000), Is.EqualTo(20000), "long math, no overflow");
			Assert.That(ModeBias.Combine(1000, 0, 20000), Is.EqualTo(1), "never below 1");
			Assert.That(ModeBias.Combine(1000, -7, 20000), Is.EqualTo(1));
			Assert.That(ModeBias.Combine(1000, 5000, 0), Is.EqualTo(1), "a cap below 1 still leaves 1");

			Assert.That(ModeBias.CombineMilli(500, 2000), Is.EqualTo(1000));
			Assert.That(ModeBias.CombineMilli(1000, 0), Is.EqualTo(1), "never below 1");
			Assert.That(ModeBias.CombineMilli(0, 5000), Is.EqualTo(1));
			Assert.That(ModeBias.CombineMilli(int.MaxValue, int.MaxValue), Is.EqualTo(int.MaxValue), "long math, clamped to an int");
		}

		[Test]
		public void TheParamsStartAtTheInfoValuesAndALearnerCanOverrideThem()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			var p = new ModeBiasParams(info.MbEssentialsStrength, info.MbTechChainStrength, info.MbAntiRushStrength, info.MbOpeningStrength, info.MbStrengthCap);
			Assert.That(new[] { p.Get(ModeBias.Essentials), p.Get(ModeBias.TechChain), p.Get(ModeBias.AntiRush), p.Get(ModeBias.Opening) }, Is.EqualTo(new[] { 2000, 2000, 2000, 2000 }), "the Info defaults");
			Assert.That(p.Cap, Is.EqualTo(20000));
			Assert.That(p.Get("unknown"), Is.EqualTo(ModeBias.Neutral));

			Assert.That(p.Compose(new[] { ModeBias.Essentials }), Is.EqualTo(2000));
			Assert.That(p.Compose(new[] { ModeBias.Essentials, ModeBias.TechChain }), Is.EqualTo(4000));
			Assert.That(p.Compose(Array.Empty<string>()), Is.EqualTo(ModeBias.Neutral));

			Assert.That(p.TrySet(ModeBias.Essentials, 3000), Is.True);
			Assert.That(p.Get(ModeBias.Essentials), Is.EqualTo(3000));
			Assert.That(p.Compose(new[] { ModeBias.Essentials }), Is.EqualTo(3000), "the multiplier ModeBias computes follows the override");
			Assert.That(p.Compose(new[] { ModeBias.Essentials, ModeBias.TechChain }), Is.EqualTo(6000));
			Assert.That(p.Get(ModeBias.TechChain), Is.EqualTo(2000), "the other profiles keep theirs");

			Assert.That(p.TrySet(ModeBias.Opening, -4), Is.True);
			Assert.That(p.Get(ModeBias.Opening), Is.EqualTo(1), "clamped to at least 1");
			Assert.That(p.TrySet("unknown", 5000), Is.False);
			Assert.That(p.Get("unknown"), Is.EqualTo(ModeBias.Neutral));

			var q = new ModeBiasParams(9000, 9000, 9000, 9000, 20000);
			Assert.That(q.Compose(new[] { ModeBias.Essentials, ModeBias.TechChain, ModeBias.AntiRush }), Is.EqualTo(20000), "the composed cap");
		}

		[Test]
		public void TheWorldWiringIsBehindTheSwitches()
		{
			var plan = Source(Dir + "BaseBuildPlanCA.cs");
			Assert.That(plan, Does.Contain("public Func<string, int> BiasMilli;"));
			Assert.That(plan, Does.Contain("? ModeBias.WeightedOrder(s.Fractions, s.BiasMilli, rng)"));
			Assert.That(plan, Does.Contain(": s.Fractions.OrderBy(kv => kv.Key, StringComparer.Ordinal).Shuffle(rng);"), "the old path is unchanged");
			Assert.That(plan, Does.Contain("milli = ModeBias.CombineMilli(milli, s.BiasMilli(name));"));

			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("s.BiasMilli = ModeBiasFor(buildable, s);"));

			var wiring = Source(Dir + "BaseBuilderQueueManagerCA.ModeBias.cs");
			Assert.That(wiring, Does.Contain("if (!(baseBuilder.MbEssentialsArmed || baseBuilder.MbTechChainArmed || baseBuilder.MbAntiRushArmed || baseBuilder.MbOpeningArmed))\n\t\t\t\treturn null;"));
			Assert.That(wiring, Does.Contain("if (baseBuilder.MbEssentialsArmed && EssentialsOn && line > 0 && s.Bank < line)"), "the low-cash hold's trigger");
			Assert.That(wiring, Does.Contain("!EssentialsLowCashCA.Holds(true, s.Bank, line,"), "the low-cash hold's type set");
			Assert.That(wiring, Does.Contain("b.TechChainTypes.Contains(a.Name) && BaseBuildPlanCA.CountOf(s, a.Name) == 0"), "techchain = the open tech steps only");
			Assert.That(wiring, Does.Contain("if (baseBuilder.MbAntiRushArmed && AntiRushSetOn && AntiRushWantedNow())"), "no anti-rush bias without the S4 set");
			Assert.That(wiring, Does.Contain("return AntiRushCA.Wants(playerBuildings.Any(a => b.ProductionTypes.Contains(a.Info.Name)), have, size);"), "S4's wanting test");
			Assert.That(wiring, Does.Contain("if (baseBuilder.MbOpeningArmed && s.OpeningActive)"));
			Assert.That(wiring, Does.Contain("return values.Compose(ids);"));
			Assert.That(wiring, Does.Contain("if (profiles.Count == 0)\n\t\t\t\treturn null;"), "null while no trigger holds");

			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			foreach (var name in new[] { "MbEssentials", "MbTechChain", "MbAntiRush", "MbOpening" })
				Assert.That(module, Does.Contain("public bool " + name + "Armed => Info." + name + " && switchesActive && !ablate" + name + ".Off(player, AblationIds." + name + ");"), name);

			Assert.That(module, Does.Contain("public ModeBiasParams ModeBiasValues => modeBiasValues ??= new ModeBiasParams(Info.MbEssentialsStrength, Info.MbTechChainStrength, Info.MbAntiRushStrength, Info.MbOpeningStrength, Info.MbStrengthCap);"));
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsThemAndTheRowsAreRegistered()
		{
			foreach (var field in new[] { "MbEssentials", "MbTechChain", "MbAntiRush", "MbOpening" })
			{
				Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), field), Is.EqualTo("true"), field);
				Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), field), Is.Null, field);
			}

			Assert.That(AblationIds.MbEssentials, Is.EqualTo("mb_essentials"));
			Assert.That(AblationIds.MbTechChain, Is.EqualTo("mb_techchain"));
			Assert.That(AblationIds.MbAntiRush, Is.EqualTo("mb_antirush"));
			Assert.That(AblationIds.MbOpening, Is.EqualTo("mb_opening"));
			var ids = AblationSpec.RegistryIds();
			Assert.That(ids, Does.Contain(AblationIds.MbEssentials));
			Assert.That(ids, Does.Contain(AblationIds.MbTechChain));
			Assert.That(ids, Does.Contain(AblationIds.MbAntiRush));
			Assert.That(ids, Does.Contain(AblationIds.MbOpening));
		}

		[Test]
		public void TheSwitchesAreOffByDefault()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.MbEssentials, Is.False);
			Assert.That(info.MbTechChain, Is.False);
			Assert.That(info.MbAntiRush, Is.False);
			Assert.That(info.MbOpening, Is.False);
			Assert.That(info.MbStrengthCap, Is.EqualTo(20000));
			Assert.That(new BaseBuildSnapshot().BiasMilli, Is.Null);
		}
	}
}
