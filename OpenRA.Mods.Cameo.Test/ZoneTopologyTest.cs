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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ZoneTopologyTest
	{
		[Test]
		public void ConnectedComponentCollectsOnlyTheLinkedCells()
		{
			// A 3-cell row plus one disjoint cell: the component from (0,0) takes the row only.
			// The helper's contract is that the seed is already in `visited` (see its remark).
			var cells = new HashSet<CPos> { new(0, 0), new(1, 0), new(2, 0), new(5, 5) };
			var visited = new HashSet<CPos> { new(0, 0) };

			var comp = TacticalMapBotModule.ConnectedComponent(new CPos(0, 0), cells, visited);

			Assert.That(comp, Is.EquivalentTo(new[] { new CPos(0, 0), new CPos(1, 0), new CPos(2, 0) }));
			Assert.That(comp, Does.Not.Contain(new CPos(5, 5)), "a disjoint cell belongs to another component");
			Assert.That(visited.Count, Is.EqualTo(3), "visited marks exactly the collected cells");
		}

		[Test]
		public void ConnectedComponentReachesDiagonallyAdjacentCells()
		{
			// CVec.Directions is 8-connected; a diagonal chain is one component.
			var cells = new HashSet<CPos> { new(0, 0), new(1, 1), new(2, 2) };
			var comp = TacticalMapBotModule.ConnectedComponent(new CPos(0, 0), cells, new HashSet<CPos> { new(0, 0) });

			Assert.That(comp.Count, Is.EqualTo(3));
		}

		[Test]
		public void CentroidPicksTheMemberNearestTheGeometricCentre()
		{
			// Geometric centre (6,0) is not itself a member; (10,0) at distance 16 wins over (10,1) at 17.
			var cells = new List<CPos> { new(0, 0), new(10, 0), new(10, 1) };

			var rep = TacticalMapBotModule.Centroid(cells);

			Assert.That(rep, Is.EqualTo(new CPos(10, 0)));
			Assert.That(cells, Does.Contain(rep), "the representative is always a real member, not the mean point");
		}

		[Test]
		public void CentroidKeepsFirstOnATie()
		{
			// Centre (1,1): (0,0) and (0,2) tie at distance 2; the scan keeps the first-seen best.
			var cells = new List<CPos> { new(0, 0), new(0, 2), new(4, 1) };

			Assert.That(TacticalMapBotModule.Centroid(cells), Is.EqualTo(new CPos(0, 0)));
		}
	}
}
