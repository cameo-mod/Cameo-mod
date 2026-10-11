#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotCombatPredictorTest
	{
		static readonly BitSet<TargetableType> Ground = new("Ground");
		static readonly BitSet<TargetableType> Air = new("Air");
		static readonly BitSet<TargetableType> None = default;

		static BotUnitProfile Unit(string name, int hp, string armor, double dpt, Dictionary<string, int> versus = null,
			BitSet<TargetableType>? targets = null, BitSet<TargetableType>? hits = null)
		{
			var weapons = dpt > 0 ? new[] { new BotWeaponProfile(dpt, WDist.FromCells(5), hits ?? Ground, None, versus) } : [];
			return new BotUnitProfile(name, 100, hp, armor, 50, false, false, targets ?? Ground, weapons);
		}

		static BotUnitProfile Unit(string name, int hp, string armor, params BotWeaponProfile[] weapons)
		{
			return new BotUnitProfile(name, 100, hp, armor, 50, false, false, Ground, weapons);
		}

		static BotWeaponProfile Weapon(double dpt, Dictionary<string, int> versus = null, double effective = 0,
			BotWeaponModel model = null, double power = 1.0, double cycle = 1.0, int burst = 1, int mainDamage = 0)
		{
			return new BotWeaponProfile(dpt, WDist.FromCells(5), Ground, None, versus,
				effective, model, null, burst, power, cycle, mainDamage, default);
		}

		[Test]
		public void EqualForcesAreEven()
		{
			var p = BotCombatPredictor.Predict(10, 1000, 10, 1000);
			Assert.That(p.Ratio, Is.EqualTo(1).Within(1e-9));
		}

		[Test]
		public void SquareLawRewardsConcentration()
		{
			// Twice the units: twice the damage AND twice the HP -> four times the fighting strength.
			var tank = Unit("tank", 400, "Heavy", 2);
			var p = BotCombatPredictor.Predict(new[] { (tank, 10) }, new[] { (tank, 5) });
			Assert.That(p.Ratio, Is.EqualTo(4).Within(1e-9));
			Assert.That(p.OwnWins, Is.True);
			Assert.That(p.OwnSurvivingFraction, Is.EqualTo(System.Math.Sqrt(0.75)).Within(1e-9));
			Assert.That(p.EnemySurvivingFraction, Is.Zero);
		}

		[Test]
		public void VersusDecidesWhoWins()
		{
			// The rocket team costs the same and has less HP, but deals 300% to Heavy while the tank deals 25% to Light.
			var tank = Unit("tank", 600, "Heavy", 2, new Dictionary<string, int> { { "Light", 25 } });
			var rockets = Unit("rockets", 200, "Light", 2, new Dictionary<string, int> { { "Heavy", 300 } });
			var p = BotCombatPredictor.Predict(new[] { (rockets, 5) }, new[] { (tank, 5) });
			Assert.That(p.OwnWins, Is.True, $"ratio {p.Ratio}");
		}

		[Test]
		public void AGroundArmyCannotHurtAircraft()
		{
			var tank = Unit("tank", 600, "Heavy", 2);
			var jet = Unit("jet", 300, "Light", 2, targets: Air, hits: Ground);
			var p = BotCombatPredictor.Predict(new[] { (tank, 20) }, new[] { (jet, 1) });
			Assert.That(p.Ratio, Is.Zero);
			Assert.That(p.OwnWins, Is.False);
		}

		[Test]
		public void AnUnarmedEnemyIsCappedNotInfinite()
		{
			var tank = Unit("tank", 600, "Heavy", 2);
			var truck = Unit("truck", 500, "Light", 0);
			var p = BotCombatPredictor.Predict(new[] { (tank, 1) }, new[] { (truck, 3) });
			Assert.That(p.Ratio, Is.EqualTo(BotCombatPredictor.MaxRatio));
			Assert.That(p.OwnSurvivingFraction, Is.EqualTo(1));
		}

		[TestCase(40, 1, new[] { 5 }, 40)]
		[TestCase(40, 3, new[] { 5 }, 50)]
		[TestCase(40, 3, new[] { 2, 8 }, 50)]
		[TestCase(0, 1, new int[0], 1)]
		public void CycleIsBurstGapsPlusReload(int reload, int burst, int[] delays, int expected)
		{
			Assert.That(BotWeaponProfile.CycleTicks(reload, burst, delays), Is.EqualTo(expected));
		}

		// ---- Live per-actor stats (PREDICTOR-PARITY revised P1) ----
		// The live profile folds the shooter's firepower/reload modifiers into PowerScale/CycleScale
		// and the target's current HP/Armour into the unit fields. These tests pin that each such
		// term moves the prediction in the right direction; the actor enumeration itself is a
		// thin TraitsImplementing loop covered by build + boot gates.

		[Test]
		public void VeterancyFirepowerRaisesPrediction()
		{
			var veteran = Unit("veteran", 400, "Heavy", Weapon(8, power: 1.25));
			var regular = Unit("regular", 400, "Heavy", Weapon(8));
			var enemy = Unit("enemy", 400, "Heavy", Weapon(8));

			Assert.That(veteran.DamagePerTickAgainst(enemy),
				Is.EqualTo(regular.DamagePerTickAgainst(enemy) * 1.25).Within(1e-9));

			var rVet = BotCombatPredictor.Predict(new[] { (veteran, 5) }, new[] { (enemy, 5) });
			var rReg = BotCombatPredictor.Predict(new[] { (regular, 5) }, new[] { (enemy, 5) });
			Assert.That(rVet.Ratio, Is.GreaterThan(rReg.Ratio));
		}

		[Test]
		public void FasterReloadRaisesPrediction()
		{
			// IReloadModifier of 80% -> CycleScale 0.8 -> the same shot lands more often.
			var quick = Unit("quick", 400, "Heavy", Weapon(8, cycle: 0.8));
			var regular = Unit("regular", 400, "Heavy", Weapon(8));
			var enemy = Unit("enemy", 400, "Heavy", Weapon(8));

			Assert.That(quick.DamagePerTickAgainst(enemy),
				Is.EqualTo(regular.DamagePerTickAgainst(enemy) / 0.8).Within(1e-9));

			var rQuick = BotCombatPredictor.Predict(new[] { (quick, 5) }, new[] { (enemy, 5) });
			var rReg = BotCombatPredictor.Predict(new[] { (regular, 5) }, new[] { (enemy, 5) });
			Assert.That(rQuick.Ratio, Is.GreaterThan(rReg.Ratio));
		}

		[Test]
		public void GrantedArmamentAddsDamage()
		{
			// An upgrade/condition-granted armament appears as an extra weapon on the live profile.
			var granted = Unit("granted", 400, "Heavy", Weapon(8), Weapon(4));
			var regular = Unit("regular", 400, "Heavy", Weapon(8));
			var enemy = Unit("enemy", 400, "Heavy", Weapon(8));

			Assert.That(granted.DamagePerTickAgainst(enemy),
				Is.EqualTo(regular.DamagePerTickAgainst(enemy) + 4).Within(1e-9));

			var rGranted = BotCombatPredictor.Predict(new[] { (granted, 5) }, new[] { (enemy, 5) });
			var rReg = BotCombatPredictor.Predict(new[] { (regular, 5) }, new[] { (enemy, 5) });
			Assert.That(rGranted.Ratio, Is.GreaterThan(rReg.Ratio));
		}

		[Test]
		public void DamagedTargetPredictsBetter()
		{
			var own = Unit("own", 400, "Heavy", Weapon(8));
			var enemyFull = Unit("enemy", 400, "Heavy", Weapon(8));
			var enemyHurt = Unit("enemy", 200, "Heavy", Weapon(8));

			var rFull = BotCombatPredictor.Predict(new[] { (own, 5) }, new[] { (enemyFull, 5) });
			var rHurt = BotCombatPredictor.Predict(new[] { (own, 5) }, new[] { (enemyHurt, 5) });

			// Halving the enemy's HP halves its fighting strength (dps x hp) -> the ratio doubles.
			Assert.That(rFull.Ratio, Is.EqualTo(1).Within(1e-9));
			Assert.That(rHurt.Ratio, Is.EqualTo(2).Within(1e-9));
			Assert.That(rHurt.OwnWins, Is.True);
		}

		[Test]
		public void EffectiveFlagSwapsClassicTerm()
		{
			// Bit-identity: with the flag off the modelled value must not leak into the prediction;
			// with it on the modelled per-tick term replaces damage x versus.
			var model = new BotWeaponModel
			{
				IsModelled = true,
				DamageTotal = 400,
				EffectivePerShot = 400,
				EffReload = 25
			};
			var versus = new Dictionary<string, int> { { "Heavy", 100 } };
			var own = Unit("own", 400, "Heavy", Weapon(8, versus, model: model));
			var enemy = Unit("enemy", 400, "Heavy", Weapon(8));

			Assert.That(own.DamagePerTickAgainst(enemy, false), Is.EqualTo(8).Within(1e-9));
			Assert.That(own.DamagePerTickAgainst(enemy, true), Is.EqualTo(400 / 25.0).Within(1e-9));
		}

		[Test]
		public void ModelReadsTheTargetsArmourRow()
		{
			// A live target's observed armour picks its own Versus row from the model's
			// per-armour decomposition instead of the census average.
			var model = new BotWeaponModel
			{
				IsModelled = true,
				DamageTotal = 200,
				EffectivePerShot = 160,
				EffReload = 25,
				KByArmor = new Dictionary<string, double> { { "Heavy", 0.5 }, { "Light", 1.0 } },
				PctByArmor = new Dictionary<string, double>(),
				FoldedByArmor = new Dictionary<string, double>()
			};
			var own = Unit("own", 400, "Heavy", Weapon(1, model: model));
			var heavy = Unit("heavy", 400, "Heavy", Weapon(8));
			var light = Unit("light", 400, "Light", Weapon(8));

			Assert.That(model.EffectivePerShotAgainst("Heavy"), Is.EqualTo(100).Within(1e-9));
			Assert.That(model.EffectivePerShotAgainst("Light"), Is.EqualTo(200).Within(1e-9));
			Assert.That(model.EffectivePerShotAgainst(null), Is.EqualTo(160).Within(1e-9));
			Assert.That(own.DamagePerTickAgainst(light, true), Is.GreaterThan(own.DamagePerTickAgainst(heavy, true)));
		}

		[Test]
		public void UnmodelledWeaponFallsBackToClassic()
		{
			// The two weapons the pipeline cannot model must keep their classic term under the flag.
			var versus = new Dictionary<string, int> { { "Heavy", 50 } };
			var own = Unit("own", 400, "Heavy", Weapon(8, versus, effective: 0, model: null));
			var enemy = Unit("enemy", 400, "Heavy", Weapon(8));

			Assert.That(own.DamagePerTickAgainst(enemy, true), Is.EqualTo(4).Within(1e-9));
		}

		[Test]
		public void MultiDamageWarheadsSumUnderEffectiveOnly()
		{
			// Until W24 picks a main warhead the effective fallback sums every positive
			// damage warhead with its own Versus; the classic term stays main-only.
			var versus = new Dictionary<string, int> { { "Heavy", 100 } };
			var secondary = new Dictionary<string, int> { { "Heavy", 50 } };
			var terms = new BotWarheadTerm[]
			{
				new(8, versus, 80, default),
				new(4, secondary, 40, default)
			};
			var w = new BotWeaponProfile(8, WDist.FromCells(5), Ground, None, versus, 0, null, null, 1, 1.0, 1.0, 80, default, terms);
			var own = Unit("own", 400, "Heavy", w);
			var enemy = Unit("enemy", 400, "Heavy", Weapon(8));

			Assert.That(own.DamagePerTickAgainst(enemy, false), Is.EqualTo(8).Within(1e-9));
			Assert.That(own.DamagePerTickAgainst(enemy, true), Is.EqualTo(8 + 4 * 0.5).Within(1e-9));
		}
	}
}
