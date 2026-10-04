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

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModuleLogic;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class CombatVetoEvalTest
	{
		static readonly BitSet<TargetableType> Ground = new("Ground");
		static readonly BitSet<TargetableType> None = default;

		static BotUnitProfile Unit(string name, int hp, string armor, double dpt, int speed = 50)
		{
			var weapons = dpt > 0 ? new[] { new BotWeaponProfile(dpt, WDist.FromCells(5), Ground, None, null) } : [];
			return new BotUnitProfile(name, 100, hp, armor, speed, false, false, Ground, weapons);
		}

		sealed class StubPriors : IBotEngagementPriors
		{
			readonly int milli;
			public StubPriors(int milli) { this.milli = milli; }
			public int CorrectionMilli(BotUnitProfile attacker, BotUnitProfile target) => milli;
		}

		sealed class StubExponent : IBotEngagementPriors
		{
			readonly int alpha;
			public StubExponent(int alpha) { this.alpha = alpha; }
			public int CorrectionMilli(BotUnitProfile attacker, BotUnitProfile target) => 1000;
			public int AttritionExponentMilli => alpha;
		}

		sealed class StubCombatVeto : IBotCombatVeto, IDisabledTrait
		{
			public int Calls;
			public StubCombatVeto(bool disabled) { IsTraitDisabled = disabled; }
			public bool IsTraitDisabled { get; }
			public bool VetoEngage(SquadCA squad, IReadOnlyList<Actor> enemies, bool alreadyCommitted, out string reason) { Calls++; reason = "stub"; return true; }
			public bool VetoLaunch(IReadOnlyList<Actor> force, CPos targetCell, out string reason) { Calls++; reason = "stub"; return true; }
			public bool VetoFlee(SquadCA squad, IReadOnlyList<Actor> pursuers, out string reason) { Calls++; reason = "stub"; return true; }
		}

		[Test]
		public void NullPriorsMatchesThePredictor()
		{
			// The veto's damage assembly with no priors must reproduce the one authority's numbers — never a fork.
			var tank = Unit("tank", 400, "Heavy", 2);
			var weak = Unit("weak", 100, "Light", 1);
			var own = new[] { (tank, 10) };
			var foes = new[] { (tank, 3), (weak, 5) };
			var p = CombatVetoEval.Predict(own, foes, null);
			var q = BotCombatPredictor.Predict(own, foes);
			Assert.That(p.Ratio, Is.EqualTo(q.Ratio).Within(1e-9));
		}

		[Test]
		public void EngageHysteresisHoldsACommittedSquad()
		{
			// Enter at <50, exit (for an already-committed squad) only below 35.
			Assert.That(CombatVetoEval.EngageVetoed(45, alreadyCommitted: false, 50, 35), Is.True);
			Assert.That(CombatVetoEval.EngageVetoed(45, alreadyCommitted: true, 50, 35), Is.False);
			Assert.That(CombatVetoEval.EngageVetoed(30, alreadyCommitted: true, 50, 35), Is.True);
			Assert.That(CombatVetoEval.EngageVetoed(60, alreadyCommitted: false, 50, 35), Is.False);
		}

		[Test]
		public void PredictedLossVetoesTheAttack()
		{
			// 3 tanks attacking 20 tanks: ratio far under the 50% engage line -> vetoed.
			var tank = Unit("tank", 400, "Heavy", 2);
			var ratio = CombatVetoEval.Predict(new[] { (tank, 3) }, new[] { (tank, 20) }, null).Ratio;
			var ratioPct = (int)(ratio * 100);
			Assert.That(ratioPct, Is.LessThan(50));
			Assert.That(CombatVetoEval.EngageVetoed(ratioPct, alreadyCommitted: false, 50, 35), Is.True);
		}

		[Test]
		public void WinningAttackIsAllowed()
		{
			var tank = Unit("tank", 400, "Heavy", 2);
			var ratio = CombatVetoEval.Predict(new[] { (tank, 20) }, new[] { (tank, 3) }, null).Ratio;
			var ratioPct = (int)(ratio * 100);
			Assert.That(CombatVetoEval.EngageVetoed(ratioPct, alreadyCommitted: false, 50, 35), Is.False);
		}

		[Test]
		public void PriorsShiftTheVerdict()
		{
			// An even fight, but the fitted file says the attacker's deliveries underperform (50% correction):
			// the same forces now predict a loss. Null priors do not.
			var tank = Unit("tank", 400, "Heavy", 2);
			var forces = new[] { (tank, 10) };
			var neutral = CombatVetoEval.Predict(forces, forces, null).Ratio;
			var corrected = CombatVetoEval.Predict(forces, forces, new StubPriors(500)).Ratio;
			Assert.That(neutral, Is.EqualTo(1).Within(1e-9));
			Assert.That(corrected, Is.LessThan(neutral));
		}

		[Test]
		public void AttritionExponentWarpsTheRatio()
		{
			// The fitter's AttritionExponentMilli applies as ratio^alpha on the aggregate, fractions
			// re-derived; alpha>1000 sharpens a predicted win, alpha<1000 dampens it, 1000 is inert.
			var tank = Unit("tank", 400, "Heavy", 2);
			var own = new[] { (tank, 12) };
			var foes = new[] { (tank, 10) };
			var square = CombatVetoEval.Predict(own, foes, null).Ratio;
			var sharpened = CombatVetoEval.Predict(own, foes, new StubExponent(1500));
			var dampened = CombatVetoEval.Predict(own, foes, new StubExponent(500));
			var inert = CombatVetoEval.Predict(own, foes, new StubExponent(1000));
			Assert.That(square, Is.GreaterThan(1));
			Assert.That(sharpened.Ratio, Is.EqualTo(Math.Pow(square, 1.5)).Within(1e-9));
			Assert.That(dampened.Ratio, Is.EqualTo(Math.Sqrt(square)).Within(1e-9));
			Assert.That(inert.Ratio, Is.EqualTo(square).Within(1e-9));
			Assert.That(sharpened.OwnSurvivingFraction, Is.EqualTo(Math.Sqrt(1 - 1 / sharpened.Ratio)).Within(1e-9));
			Assert.That(sharpened.EnemySurvivingFraction, Is.EqualTo(0));
		}

		[Test]
		public void FasterPursuersOutrunTheRetreat()
		{
			var slow = Unit("slow", 400, "Heavy", 2, speed: 40);
			var fast = Unit("fast", 100, "Light", 3, speed: 120);
			Assert.That(CombatVetoEval.CannotOutrun(new[] { (slow, 5) }, new[] { (fast, 5) }, 100), Is.True);
			Assert.That(CombatVetoEval.CannotOutrun(new[] { (fast, 5) }, new[] { (slow, 5) }, 100), Is.False);
		}

		[Test]
		public void NoPursuersCannotOutrun()
		{
			var slow = Unit("slow", 400, "Heavy", 2, speed: 40);
			Assert.That(CombatVetoEval.CannotOutrun(new[] { (slow, 5) }, new (BotUnitProfile, int)[0], 100), Is.False);
		}

		[Test]
		public void DisabledCombatVetoProviderIsNotConsulted()
		{
			var disabled = new StubCombatVeto(disabled: true);
			var enabled = new StubCombatVeto(disabled: false);
			var vetoes = new IBotCombatVeto[] { disabled, enabled };
			Assert.That(SquadManagerBotModuleCA.EnabledCombatVetoes(vetoes).ToArray(), Is.EqualTo(new[] { enabled }));

			var consulted = 0;
			foreach (var veto in SquadManagerBotModuleCA.EnabledCombatVetoes(vetoes))
			{
				if (veto.VetoEngage(null, Array.Empty<Actor>(), false, out _))
					consulted++;
				if (veto.VetoLaunch(Array.Empty<Actor>(), CPos.Zero, out _))
					consulted++;
				if (veto.VetoFlee(null, Array.Empty<Actor>(), out _))
					consulted++;
			}

			Assert.That(consulted, Is.EqualTo(3));
			Assert.That(disabled.Calls, Is.Zero);
			Assert.That(enabled.Calls, Is.EqualTo(3));
		}
	}
}
