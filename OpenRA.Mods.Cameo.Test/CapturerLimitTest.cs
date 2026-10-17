#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// S1b (ENGINEER-CAP-10PCT, maintainer 2026-10-12): engineers are a support unit, not combat —
	// live capturers <= 10% of armed infantry (floor 2), +1 per KNOWN uncaptured capturable target
	// (bounded +4), at most one in production, and a difficulty-line interval between completions
	// that is skipped while the allowance remains. The forensics 30:30 becomes 3+allowance:30.
	[TestFixture]
	public sealed class CapturerLimitTest
	{
		static bool Admits(int pct, int capturers, int armed, int minUnits = 2, int allowanceMax = 4,
			int knownTargets = 0, int queued = 0, int ticksSince = -1, int interval = 0) =>
			UnitBuilderBotModuleCA.CapturerCapAdmits(pct, capturers, armed, minUnits, allowanceMax,
				knownTargets, queued, ticksSince, interval);

		[Test]
		public void ThirtyGrenadiersAdmitAtMostThreePlusAllowance()
		{
			// 10% of 30 armed infantry -> base cap 3 -> at most 3 live engineers, plus allowance.
			for (var engineers = 0; engineers < 3; engineers++)
				Assert.That(Admits(10, engineers, 30), Is.True);
			Assert.That(Admits(10, 3, 30), Is.False);
			Assert.That(Admits(10, 3, 30, knownTargets: 2), Is.True, "+2 known targets -> cap 5");
			Assert.That(Admits(10, 5, 30, knownTargets: 2), Is.False);
		}

		[Test]
		public void TheAllowanceIsBoundedAtFour()
		{
			// 9 known targets still only add 4: cap = 3 + 4 = 7.
			Assert.That(Admits(10, 6, 30, knownTargets: 9), Is.True);
			Assert.That(Admits(10, 7, 30, knownTargets: 9), Is.False);
			Assert.That(Admits(10, 7, 30, knownTargets: 9, allowanceMax: 0), Is.False,
				"a zero-configured bound means no allowance at all");
		}

		[Test]
		public void ZeroInfantryKeepsTheFloorOfTwo()
		{
			Assert.That(Admits(10, 0, 0), Is.True);
			Assert.That(Admits(10, 1, 0), Is.True);
			Assert.That(Admits(10, 2, 0), Is.False);
			Assert.That(Admits(10, 3, 0, knownTargets: 2), Is.True,
				"the allowance stacks on the floor: cap = 2 + 2");
			Assert.That(Admits(10, 4, 0, knownTargets: 2), Is.False);
		}

		[Test]
		public void TheCeilingClampsAnyHigherPercent()
		{
			// A legacy or hand-set pct above 10 computes at the ceiling: still cap 3 on 30 infantry.
			Assert.That(Admits(49, 2, 30), Is.True);
			Assert.That(Admits(49, 3, 30), Is.False);
			Assert.That(Admits(80, 3, 30), Is.False);
			Assert.That(Admits(100, 10, 30), Is.False);
		}

		[Test]
		public void OneCapturerInProductionBlocksTheNext()
		{
			// Under-cap, under allowance — a queued or building engineer still blocks the next.
			Assert.That(Admits(10, 0, 30, queued: 1), Is.False);
			Assert.That(Admits(10, 0, 30, knownTargets: 4, queued: 1), Is.False,
				"the in-production rule is unconditional, even while free targets remain");
			Assert.That(Admits(10, 0, 30, queued: 0), Is.True);
		}

		[Test]
		public void TheCompletionIntervalPacesExceptDuringTheAllowance()
		{
			Assert.That(Admits(10, 0, 30, ticksSince: 100, interval: 1200), Is.False,
				"inside the interval the next completion may not be started");
			Assert.That(Admits(10, 0, 30, ticksSince: 1200, interval: 1200), Is.True,
				"the interval elapses at equality");
			Assert.That(Admits(10, 0, 30, ticksSince: 100, interval: 1200, knownTargets: 1), Is.True,
				"skipped while the allowance is above 0");
			Assert.That(Admits(10, 0, 30, ticksSince: -1, interval: 1200), Is.True,
				"no completion yet — the first engineer never waits");
			Assert.That(Admits(10, 0, 30, ticksSince: 5, interval: 0), Is.True,
				"interval 0 disables pacing");
		}

		[Test]
		public void ZeroAndNegativePercentDisableTheCap()
		{
			// Classic (switchesActive false) reads 0 and preserves the pre-cap mix entirely:
			// no ratio, no allowance, no production pacing.
			Assert.That(Admits(0, 100, 30, queued: 5, ticksSince: 1, interval: 1200), Is.True);
			Assert.That(Admits(-1, 100, 30), Is.True);
		}

		// F1 (lead ruling 2026-10-12): the off-cap early return is hoisted above the army
		// scan — CapturerCapActive gates the whole check, so classic never pays the walk.

		[Test]
		public void AnOffCapIsNotActive()
		{
			Assert.That(UnitBuilderBotModuleCA.CapturerCapActive(0), Is.False,
				"classic reads 0 — the caller short-circuits before the scan");
			Assert.That(UnitBuilderBotModuleCA.CapturerCapActive(-1), Is.False);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapActive(10), Is.True);
			Assert.That(UnitBuilderBotModuleCA.CapturerCapActive(80), Is.True);
		}

		[Test]
		public void TheOffCapInvariantStillAdmits()
		{
			// The hoist relies on CapturerCapAdmits <= 0 already being a pass — pinned so the
			// early return can never change the outcome, only the work spent reaching it.
			Assert.That(Admits(0, 100, 30), Is.True);
			Assert.That(Admits(-1, 100, 30), Is.True);
		}

		// The allowance source: EngineerBotModule's fog-fair known-target count.

		[Test]
		public void OnlyNeutralTechAndPriorityEnemyTargetsFeedTheAllowance()
		{
			Assert.That(EngineerBotModule.CountsTowardCapturerAllowance(true, false, true), Is.True,
				"neutral tech building");
			Assert.That(EngineerBotModule.CountsTowardCapturerAllowance(false, true, true), Is.True,
				"PriorityCapturableActorTypes enemy structure");
			Assert.That(EngineerBotModule.CountsTowardCapturerAllowance(false, false, true), Is.False,
				"an ordinary enemy structure does not count");
			Assert.That(EngineerBotModule.CountsTowardCapturerAllowance(true, true, false), Is.False,
				"a configured capturable whitelist still gates the count");
		}
	}
}
