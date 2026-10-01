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
	// refinery — the far edge of the SAME home field, so refineries stacked there. The filter is
	// field-level: a field counts as served when an own refinery stands within the served radius of
	// the field's resource centre, no matter how far a candidate cell sits from the refinery itself.
	[TestFixture]
	public class RefinerySpreadTest
	{
		// Stand-in for ResourceMapBotModule.FindClosestIndiceFromCPos().ResourceCellsCenter.
		static CPos FieldCenter(CPos c) => c.Y < 20 ? new CPos(10, 10) : new CPos(40, 30);

		[Test]
		public void UnservedFieldIsPreferred()
		{
			var homeField = new[] { new CPos(10, 10), new CPos(11, 10), new CPos(12, 10) };
			var farField = new[] { new CPos(40, 30), new CPos(41, 30) };
			var refineries = new List<CPos> { new(10, 12) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(
				homeField.Concat(farField), FieldCenter, refineries, 10).ToList();

			Assert.That(picked, Is.EquivalentTo(farField),
				"cells of the field the refinery already serves must not be sampled again");
		}

		[Test]
		public void FarEdgeOfServedFieldStillCountsAsServed()
		{
			// The v1 bug shape: a candidate cell far from the refinery but on the SAME field's index
			// centre must still be dropped — cell distance alone cannot see that.
			var sameFieldFarEdge = new[] { new CPos(19, 15) }; // 11+ cells from the refinery, centre (10,10)
			var otherField = new[] { new CPos(40, 30) };
			var refineries = new List<CPos> { new(10, 12) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(
				sameFieldFarEdge.Concat(otherField), FieldCenter, refineries, 10).ToList();

			Assert.That(picked, Is.EquivalentTo(otherField),
				"(19,15) is far from the refinery but on its field — only the second field may remain");
		}

		[Test]
		public void EverythingServedKeepsAllCandidates()
		{
			var cells = new[] { new CPos(10, 10), new CPos(12, 10) };
			var refineries = new List<CPos> { new(10, 11) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(cells, FieldCenter, refineries, 10).ToList();

			Assert.That(picked, Is.EquivalentTo(cells),
				"with every candidate's field served the caller's ordering must see the full set");
		}

		[Test]
		public void NoRefineryYetKeepsAllCandidates()
		{
			var cells = new[] { new CPos(10, 10), new CPos(40, 30) };

			var picked = BaseBuilderBotModuleCA.PreferUnservedResourceCells(cells, FieldCenter, new List<CPos>(), 10).ToList();

			Assert.That(picked, Is.EquivalentTo(cells), "first refinery placement is unchanged");
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

		// UT-4 (AI_ARCHITECTURE.md 12.13): the TechRush<->Expansion axis scales the appetite —
		// neutral keeps the base count, the Expansion pole adds the bonus, the TechRush pole
		// subtracts (floor 1: never zero appetite).
		[Test]
		public void NeutralAxisKeepsTheBaseCount()
		{
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(3, 50, 2, 1), Is.EqualTo(3),
				"neutral = flag-off behaviour verbatim");
		}

		[Test]
		public void ExpansionPoleWidensTheAppetite()
		{
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(3, 100, 2, 1), Is.EqualTo(5));
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(3, 75, 2, 1), Is.EqualTo(4),
				"half-lean rounds to half the bonus");
		}

		[Test]
		public void TechRushPoleSlimsTheAppetite()
		{
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(3, 0, 2, 1), Is.EqualTo(2));
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(3, 25, 2, 1), Is.EqualTo(3),
				"a half-lean of minus-one rounds back to base");
		}

		[Test]
		public void TheFloorIsOneYard()
		{
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(1, 0, 2, 5), Is.EqualTo(1),
				"a full TechRush lean never zeroes the appetite");
			Assert.That(ExpansionPlannerBotModule.EffectiveMcvTargetCount(3, -10, 2, 1), Is.EqualTo(2),
				"out-of-contract axes clamp defensively");
		}
	}
}
