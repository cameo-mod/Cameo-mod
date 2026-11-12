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
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// IMPL01-HOTFIX02 regression (SMOKE-ARM6-ON-01, 2026-11-08). Every ON seat reported urgency=emergency from the MCV
	// deploy (tick 35) to tick ~785. The undeployed MCV (Mobile + BaseBuilding) sat in the switch-ON home set as a CY,
	// and the deploy transform disposed its id, which StepHomeProductionLoss counted as a home loss. The fix has two
	// parts: the home set takes Building actors only, and a vanished id counts only in a step whose DeathsCost rose.
	[TestFixture]
	public sealed class StalemateHomeLossDeathGateTest
	{
		static readonly (int X, int Y) Start = (10, 10);

		static Dictionary<uint, (int X, int Y, bool Cy)> Live(params (uint Id, int X, int Y, bool Cy)[] actors)
		{
			var live = new Dictionary<uint, (int X, int Y, bool Cy)>();
			foreach (var a in actors)
				live[a.Id] = (a.X, a.Y, a.Cy);
			return live;
		}

		[Test]
		public void McvDeployIsNotAHomeLoss()
		{
			// Even if an MCV record reaches the state (defence in depth behind the Building-only set), its id
			// vanishing in a deploy step has no death, so the window stays empty.
			var state = new StalemateUrgencyState();
			Assert.That(state.StepHomeProductionLoss(10, Live((1, 10, 10, true)), Start, 750, false), Is.False);
			Assert.That(state.StepHomeProductionLoss(35, Live((2, 10, 10, true)), Start, 750, false), Is.False, "deploy transform");
			Assert.That(state.StepHomeProductionLoss(250, Live((2, 10, 10, true)), Start, 750, false), Is.False);
			Assert.That(state.HomeProductionLossTicks, Is.Empty);
		}

		[Test]
		public void SellingIsNotAHomeLossAndIsNotCountedLater()
		{
			var state = new StalemateUrgencyState();
			state.StepHomeProductionLoss(25, Live((1, 10, 10, true), (2, 14, 10, false)), Start, 750, false);
			Assert.That(state.StepHomeProductionLoss(50, Live((1, 10, 10, true)), Start, 750, false), Is.False, "barracks sold");
			// A later unrelated death must not resurrect the sold barracks as a loss.
			Assert.That(state.StepHomeProductionLoss(75, Live((1, 10, 10, true)), Start, 750, true), Is.False);
			Assert.That(state.HomeProductionLossTicks, Is.Empty);
		}

		[Test]
		public void DestroyedCyIsAHomeLossForTheWholeWindow()
		{
			var state = new StalemateUrgencyState();
			state.StepHomeProductionLoss(25, Live((1, 10, 10, true), (2, 14, 10, false)), Start, 750, false);
			Assert.That(state.StepHomeProductionLoss(50, Live((2, 14, 10, false)), Start, 750, true), Is.True, "CY destroyed");
			Assert.That(state.StepHomeProductionLoss(800, Live((2, 14, 10, false)), Start, 750, false), Is.True);
			Assert.That(state.StepHomeProductionLoss(801, Live((2, 14, 10, false)), Start, 750, false), Is.False);
		}

		[Test]
		public void DestroyedHomeBarracksIsALossButAFarOneIsNot()
		{
			var state = new StalemateUrgencyState();
			state.StepHomeProductionLoss(25, Live((1, 10, 10, true), (2, 14, 10, false), (3, 80, 80, false)), Start, 750, false);
			Assert.That(state.StepHomeProductionLoss(50, Live((1, 10, 10, true), (2, 14, 10, false)), Start, 750, true), Is.False, "far outpost");
			Assert.That(state.StepHomeProductionLoss(75, Live((1, 10, 10, true)), Start, 750, true), Is.True, "home barracks");
		}

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		[Test]
		public void SwitchOnHomeSetIsBuildingOnlyAndTheCallPassesTheDeathDelta()
		{
			var text = Source("mods/Cameo/Traits/BotModules/BotSituation.cs");
			var start = text.IndexOf("// BOTPASSIVE F4 Emergency' (switch ON).", StringComparison.Ordinal);
			var end = text.IndexOf("// BOTPASSIVE F3/F4 Urgency' at a snapshot", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			Assert.That(end, Is.GreaterThan(start));
			var body = text.Substring(start, end - start);
			Assert.That(body, Does.Contain("Info.LossWindowTicks, delta > 0);"), "the home-loss step must be gated on the DeathsCost delta");
			var local = body.Substring(body.IndexOf("IEnumerable<Actor> LiveProductionAndCy() =>", StringComparison.Ordinal));
			Assert.That(local, Does.Contain("HasTraitInfo<BuildingInfo>()"), "the MCV (Mobile + BaseBuilding) must not enter the home set");
		}
	}
}
