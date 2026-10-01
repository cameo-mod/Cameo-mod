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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// EX-2 follow-up (AI_ARCHITECTURE.md 12.13): DriveRefineries means "every field in reach gets a
	// refinery", but the placement fallback only sampled the resource cells farthest from the newest
	// refinery — the far edge of the SAME home field, so refineries stacked there. With an expansion
	// planner mounted the sample now prefers cells no own refinery covers yet (spread to new ground);
	// with everything served it falls back to the old candidate set unchanged.
	[TestFixture]
	public class RefinerySpreadTest
	{
		[Test]
		public void UnservedGroundIsPreferred()
		{
			var homeField = new[] { new CPos(10, 10), new CPos(11, 10), new CPos(12, 10) };
			var farField = new[] { new CPos(40, 30), new CPos(41, 30) };
			var refineries = new List<CPos> { new(10, 12) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(
				homeField.Concat(farField), refineries, 10).ToList();

			Assert.That(picked, Is.EquivalentTo(farField),
				"cells the home refinery already serves must not be sampled again");
		}

		[Test]
		public void EverythingServedKeepsAllCandidates()
		{
			var cells = new[] { new CPos(10, 10), new CPos(12, 10) };
			var refineries = new List<CPos> { new(10, 12), new(12, 12) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(cells, refineries, 10).ToList();

			Assert.That(picked, Is.EquivalentTo(cells),
				"with every candidate served the caller's ordering must see the full set");
		}

		[Test]
		public void NoRefineryYetKeepsAllCandidates()
		{
			var cells = new[] { new CPos(10, 10), new CPos(40, 30) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(cells, new List<CPos>(), 10).ToList();

			Assert.That(picked, Is.EquivalentTo(cells), "first refinery placement is unchanged");
		}

		[Test]
		public void EdgeCellJustOutsideRadiusQualifies()
		{
			var cells = new[] { new CPos(10, 10), new CPos(10, 21) };
			var refineries = new List<CPos> { new(10, 10) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(cells, refineries, 10).ToList();

			Assert.That(picked, Is.EqualTo(new[] { new CPos(10, 21) }),
				"11 cells out is beyond the 10-cell served radius; the same-field cell is not");
		}
	}

	// Greedy expansion (AI_ARCHITECTURE.md 12.13): with DriveMcvRequests the planner asks for a
	// construction MCV itself while a far field is free — the engine module alone waits for its
	// 4000-cash trigger. The gate must hold the cash reserve, require a far free field, and stop
	// once the target yard+MCV count is covered (including queued ones).
	[TestFixture]
	public class GreedyMcvRequestTest
	{
		[Test]
		public void RequestsWhileFarFieldFreeAndReserved()
		{
			Assert.That(ExpansionPlannerBotModule.ShouldRequestMcv(3000, 1500, true, 1, 3), Is.True,
				"one yard, cash over reserve, a far field free -> build the second MCV now");
		}

		[Test]
		public void NoFarFieldNoRequest()
		{
			Assert.That(ExpansionPlannerBotModule.ShouldRequestMcv(9000, 1500, false, 1, 3), Is.False,
				"every free field inside crawl reach is the building line's job, not an MCV's");
		}

		[Test]
		public void ReserveIsAHardFloor()
		{
			Assert.That(ExpansionPlannerBotModule.ShouldRequestMcv(1499, 1500, true, 1, 3), Is.False);
			Assert.That(ExpansionPlannerBotModule.ShouldRequestMcv(1500, 1500, true, 1, 3), Is.True);
		}

		[Test]
		public void TargetCountIsARealCap()
		{
			Assert.That(ExpansionPlannerBotModule.ShouldRequestMcv(9000, 1500, true, 3, 3), Is.False,
				"three yards/MCVs including queued already covers the appetite");
			Assert.That(ExpansionPlannerBotModule.ShouldRequestMcv(9000, 1500, true, 2, 3), Is.True);
		}
	}
}
