#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// ARMY-BRAIN-01 S1 (ARMY_BRAIN_DESIGN_02.md, 3.1 - 3.4): the fight model, the multipliers and the slots. Pure, no world.
	[TestFixture]
	public class ArmyBrainFormulaTest
	{
		static readonly BitSet<TargetableType> Ground = new("Ground");
		static readonly BitSet<TargetableType> Air = new("Air");
		static readonly BitSet<TargetableType> None = default;

		static BotUnitProfile Unit(string name, int cost, int hp, double dpt, int rangeCells, int speed,
			BitSet<TargetableType>? hits = null, BitSet<TargetableType>? targets = null, bool aircraft = false)
		{
			var weapons = dpt > 0 ? new[] { new BotWeaponProfile(dpt, WDist.FromCells(rangeCells), hits ?? Ground, None, null) } : [];
			return new BotUnitProfile(name, cost, hp, "None", speed, aircraft, false, targets ?? Ground, weapons);
		}

		static List<(BotUnitProfile Unit, double Count)> Side(params (BotUnitProfile Unit, double Count)[] units) => units.ToList();

		[Test]
		public void FreeFireIsTheRangeAdvantageOverTheTargetSpeed()
		{
			var artillery = Unit("arty", 1000, 200, 5, 8, 40);
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			Assert.That(ArmyFightModel.FreeFireTicks(artillery, tank, 250), Is.EqualTo(3 * 1024 / 64.0));
			Assert.That(ArmyFightModel.FreeFireTicks(tank, artillery, 250), Is.EqualTo(0), "the shorter range has no free fire");
			Assert.That(ArmyFightModel.FreeFireTicks(artillery, tank, 20), Is.EqualTo(20), "capped");
			Assert.That(ArmyFightModel.FreeFireTicks(tank, tank, 250), Is.EqualTo(0), "equal range");
			Assert.That(ArmyFightModel.FreeFireTicks(artillery, tank, 0), Is.EqualTo(0), "cap 0 switches the term off");
		}

		[Test]
		public void FreeFireIsTheCapAgainstAnImmobileOrHelplessTarget()
		{
			var artillery = Unit("arty", 1000, 200, 5, 8, 40);
			var turret = Unit("turret", 600, 600, 8, 6, 0);
			var helicopter = Unit("heli", 1200, 200, 6, 5, 120, Ground, Air, true);
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			Assert.That(ArmyFightModel.FreeFireTicks(artillery, turret, 250), Is.EqualTo(250), "a defence cannot close the gap");
			Assert.That(ArmyFightModel.FreeFireTicks(helicopter, tank, 250), Is.EqualTo(250), "the tank cannot answer at all");
			Assert.That(ArmyFightModel.FreeFireTicks(tank, helicopter, 250), Is.EqualTo(0), "no weapon reaches");
		}

		[Test]
		public void AUnitThatCannotTargetTheEnemyDestroysNothing()
		{
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			var helicopter = Unit("heli", 1200, 200, 6, 5, 120, Ground, Air, true);
			var outcome = ArmyFightModel.Evaluate(Side((tank, 5)), Side((helicopter, 3)), 250);
			Assert.That(outcome.Destroyed, Is.EqualTo(0));
			Assert.That(outcome.Lost, Is.GreaterThan(0));

			var reverse = ArmyFightModel.Evaluate(Side((helicopter, 3)), Side((tank, 5)), 250);
			Assert.That(reverse.Lost, Is.EqualTo(0), "helicopters counter tanks that cannot shoot air");
			Assert.That(reverse.Destroyed, Is.EqualTo(5 * 800).Within(1e-6));
		}

		[Test]
		public void DamageGoesOnlyToWhatCanBeHit()
		{
			// The tank cannot hit the helicopter, so all of its damage goes to the enemy tank instead of being wasted on the air unit.
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			var enemyTank = Unit("etank", 800, 400, 5, 5, 64);
			var helicopter = Unit("heli", 1200, 200, 0, 5, 120, Ground, Air, true);
			var alone = ArmyFightModel.Evaluate(Side((tank, 4)), Side((enemyTank, 2)), 0);
			var withAir = ArmyFightModel.Evaluate(Side((tank, 4)), Side((enemyTank, 2), (helicopter, 2)), 0);
			Assert.That(withAir.Destroyed, Is.EqualTo(alone.Destroyed).Within(1e-6), "the unarmed air unit nobody can hit is not destroyed");
			Assert.That(withAir.Lost, Is.EqualTo(alone.Lost).Within(1e-6));
		}

		[Test]
		public void WhatIsLeftUnderAnAttackerItCannotHitIsLost()
		{
			// The tanks beat the enemy tanks, but the armed helicopters they cannot shoot at finish them.
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			var enemyTank = Unit("etank", 800, 400, 5, 5, 64);
			var gunship = Unit("heli", 1200, 200, 6, 5, 120, Ground, Air, true);
			var outcome = ArmyFightModel.Evaluate(Side((tank, 6)), Side((enemyTank, 2), (gunship, 1)), 0);
			Assert.That(outcome.Destroyed, Is.EqualTo(2 * 800).Within(1e-6), "the helicopter survives");
			Assert.That(outcome.Lost, Is.EqualTo(6 * 800).Within(1e-6));

			// An anti-air escort that can hit the helicopter changes that.
			var antiAir = Unit("aa", 800, 300, 6, 6, 60, Air);
			var escorted = ArmyFightModel.Evaluate(Side((tank, 6), (antiAir, 2)), Side((enemyTank, 2), (gunship, 1)), 0);
			Assert.That(escorted.Destroyed, Is.EqualTo(2 * 800 + 1200).Within(1e-6));
			Assert.That(escorted.Lost, Is.LessThan(6 * 800));
		}

		[Test]
		public void AnEmptySideGivesNoOutcome()
		{
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			Assert.That(ArmyFightModel.Evaluate(Side(), Side((tank, 2)), 250), Is.EqualTo(default(ArmyFightModel.Outcome)));
			Assert.That(ArmyFightModel.Evaluate(Side((tank, 2)), Side(), 250), Is.EqualTo(default(ArmyFightModel.Outcome)));
		}

		[Test]
		public void OutrangingADefenceGainsMoreThanTheSameDamageAtShortRange()
		{
			// Same cost, hit points and damage; only the range differs. The turret outranges the short one and is outranged by the long one.
			var turret = Unit("turret", 600, 800, 10, 6, 0);
			var longRange = Unit("arty", 1000, 200, 6, 9, 40);
			var shortRange = Unit("brawler", 1000, 200, 6, 4, 40);
			var enemy = Side((turret, 3));
			var own = Side();
			var art = ArmyFightModel.Gain(longRange, own, enemy, 0, 2000, 8, 250);
			var brawl = ArmyFightModel.Gain(shortRange, own, enemy, 0, 2000, 8, 250);
			Assert.That(art, Is.GreaterThan(brawl));
		}

		[Test]
		public void ACandidateThatCanHitTheEnemyGainsMoreThanOneThatCannot()
		{
			var helicopter = Unit("heli", 1200, 200, 6, 5, 120, Ground, Air, true);
			var antiAir = Unit("aa", 800, 300, 6, 6, 60, Air);
			var tank = Unit("tank", 800, 300, 6, 6, 60);
			var enemy = Side((helicopter, 4));
			Assert.That(ArmyFightModel.Gain(antiAir, Side(), enemy, 0, 2000, 8, 250),
				Is.GreaterThan(ArmyFightModel.Gain(tank, Side(), enemy, 0, 2000, 8, 250)));
		}

		[Test]
		public void TheGainIsMarginalOnTheCurrentArmy()
		{
			// The candidate is merged into its own entry, and the gain is the change against the given base result.
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			var enemyTank = Unit("etank", 800, 400, 5, 5, 64);
			var own = Side((tank, 2));
			var enemy = Side((enemyTank, 4));
			var baseNet = ArmyFightModel.Evaluate(own, enemy, 250).Net;
			var gain = ArmyFightModel.Gain(tank, own, enemy, baseNet, 1600, 8, 250);
			var expected = (ArmyFightModel.Evaluate(Side((tank, 4)), enemy, 250).Net - baseNet) / (2 * 800.0);
			Assert.That(gain, Is.EqualTo(expected).Within(1e-9));
			Assert.That(own, Has.Count.EqualTo(1), "the army list is not changed");
			Assert.That(own[0].Count, Is.EqualTo(2));
		}

		[Test]
		public void ThePairCacheDoesNotChangeTheResult()
		{
			var artillery = Unit("arty", 1000, 200, 5, 8, 40);
			var tank = Unit("tank", 800, 400, 5, 5, 64);
			var turret = Unit("turret", 600, 800, 10, 6, 0);
			var helicopter = Unit("heli", 1200, 200, 6, 5, 120, new BitSet<TargetableType>("Ground", "Air"), Air, true);
			var own = Side((artillery, 2), (tank, 3));
			var enemy = Side((turret, 2), (helicopter, 1), (tank, 2));
			var cache = new ArmyPairCache(250);
			var plain = ArmyFightModel.Evaluate(own, enemy, 250);
			Assert.That(ArmyFightModel.Evaluate(own, enemy, 250, cache), Is.EqualTo(plain));
			Assert.That(ArmyFightModel.Evaluate(own, enemy, 250, cache), Is.EqualTo(plain), "second call, from the cache");
		}

		[TestCase(2000, 100, 8, 8)]
		[TestCase(2000, 2000, 8, 1)]
		[TestCase(2000, 5000, 8, 1)]
		[TestCase(2000, 700, 8, 3)]
		[TestCase(2000, 0, 8, 1)]
		[TestCase(2000, 100, 0, 1)]
		public void TheTestStepIsCapped(int stepCost, int unitCost, int maxCount, int expected)
		{
			Assert.That(ArmyFightModel.StepCount(stepCost, unitCost, maxCount), Is.EqualTo(expected));
		}

		static int[] Multipliers(double[] gains, int gain = 60, int min = 50, int max = 300, int band = 250)
		{
			var percents = new int[gains.Length];
			ArmyFightModel.Multipliers(gains, gain, min, max, band, percents);
			return percents;
		}

		[Test]
		public void EqualGainsAreNeutral()
		{
			Assert.That(Multipliers([3.5, 3.5, 3.5, 3.5]), Is.All.EqualTo(100));
			Assert.That(Multipliers([0, 0, 0]), Is.All.EqualTo(100));
			Assert.That(Multipliers([7]), Is.All.EqualTo(100), "one candidate");
			Assert.That(Multipliers([]), Is.Empty);
		}

		[Test]
		public void TheMultiplierFollowsTheRankNotTheSize()
		{
			// gain 60: the best gets +60, the worst -60, the middle stays; a huge gain gets no more than the top rank.
			Assert.That(Multipliers([1, 2, 3], min: 10), Is.EqualTo(new[] { 40, 100, 160 }));
			Assert.That(Multipliers([1, 2, 3000], min: 10), Is.EqualTo(new[] { 40, 100, 160 }));
			Assert.That(Multipliers([-5, -1, 4], min: 10), Is.EqualTo(new[] { 40, 100, 160 }), "negative gains rank the same way");
		}

		[Test]
		public void TheMultiplierIsClampedAndNeverZero()
		{
			Assert.That(Multipliers([1, 2, 3]), Is.EqualTo(new[] { 50, 100, 160 }), "minimum 50");
			Assert.That(Multipliers([1, 2, 3], gain: 500, min: 50, max: 300), Is.EqualTo(new[] { 50, 100, 300 }));
			Assert.That(Multipliers([1, 2, 3], gain: 500, min: 1, max: 300).Min(), Is.GreaterThan(0));
		}

		[Test]
		public void TiesShareARank()
		{
			Assert.That(Multipliers([1, 1, 3], min: 10), Is.EqualTo(new[] { 70, 70, 160 }));
		}

		[Test]
		public void NearlyEqualGainsStayNearNeutral()
		{
			// spread 1 over a mean size of 100.5 with a band of 250 permille: 4% of the full swing.
			Assert.That(Multipliers([100, 101]), Is.EqualTo(new[] { 98, 102 }));
			Assert.That(Multipliers([100, 101], band: 0), Is.EqualTo(new[] { 50, 160 }), "band 0 = no dead band");
		}

		[Test]
		public void PercentileIsTheNearestRankBelow()
		{
			Assert.That(ArmySlots.Percentile([10, 20, 30, 40, 50], 0), Is.EqualTo(10));
			Assert.That(ArmySlots.Percentile([50, 10, 40, 20, 30], 50), Is.EqualTo(30));
			Assert.That(ArmySlots.Percentile([10, 20, 30, 40, 50], 75), Is.EqualTo(40));
			Assert.That(ArmySlots.Percentile([10, 20, 30, 40, 50], 100), Is.EqualTo(50));
			Assert.That(ArmySlots.Percentile([], 50), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void SlotsComeFromTheCutPoints()
		{
			var slots = new ArmySlots(frontHpPerCost: 500, backRange: 7000, fastSpeed: 90);
			Assert.That(slots.Classify(new ArmySlotFacts(600, 5000, 50, false, true)), Is.EqualTo(ArmySlot.Front));
			Assert.That(slots.Classify(new ArmySlotFacts(200, 8000, 40, false, true)), Is.EqualTo(ArmySlot.Back));
			Assert.That(slots.Classify(new ArmySlotFacts(200, 5000, 100, false, true)), Is.EqualTo(ArmySlot.Fast));
			Assert.That(slots.Classify(new ArmySlotFacts(200, 5000, 50, false, true)), Is.EqualTo(ArmySlot.Line));
			Assert.That(slots.Classify(new ArmySlotFacts(600, 8000, 50, false, true)), Is.EqualTo(ArmySlot.Line), "durable and long-ranged: neither front nor back");
			Assert.That(slots.Classify(new ArmySlotFacts(600, 8000, 100, true, true)), Is.EqualTo(ArmySlot.Air));
			Assert.That(slots.Classify(new ArmySlotFacts(600, 0, 100, false, false)), Is.EqualTo(ArmySlot.Support));
		}

		[Test]
		public void TheCutPointsDoNotDependOnTheOrderOfThePopulation()
		{
			var population = new[]
			{
				ArmySlotFacts.Of(Unit("rifle", 100, 50, 1, 4, 56)),
				ArmySlotFacts.Of(Unit("tank", 800, 400, 5, 6, 64)),
				ArmySlotFacts.Of(Unit("heavy", 1500, 1200, 7, 5, 40)),
				ArmySlotFacts.Of(Unit("arty", 1000, 200, 5, 9, 40)),
				ArmySlotFacts.Of(Unit("buggy", 500, 150, 2, 4, 120)),
				ArmySlotFacts.Of(Unit("truck", 500, 200, 0, 0, 80)),
				ArmySlotFacts.Of(Unit("heli", 1200, 200, 6, 5, 150, Ground, Air, true)),
			};

			var a = ArmySlots.From(population, 60, 75, 75);
			var b = ArmySlots.From(population.Reverse(), 60, 75, 75);
			Assert.That((b.FrontHpPerCost, b.BackRange, b.FastSpeed), Is.EqualTo((a.FrontHpPerCost, a.BackRange, a.FastSpeed)));

			// The same cut points classify every type, whatever subset a faction can build.
			Assert.That(a.Classify(population[3]), Is.EqualTo(ArmySlot.Back));
			Assert.That(a.Classify(population[2]), Is.EqualTo(ArmySlot.Front));
			Assert.That(a.Classify(population[4]), Is.EqualTo(ArmySlot.Fast));
			Assert.That(a.Classify(population[5]), Is.EqualTo(ArmySlot.Support));
			Assert.That(a.Classify(population[6]), Is.EqualTo(ArmySlot.Air));
		}

		[Test]
		public void TheRowIdIsFixed()
		{
			Assert.That(AblationIds.ArmyBrainFormula, Is.EqualTo("ab_formula"));
		}
	}
}
