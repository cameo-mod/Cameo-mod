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

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// ENGLOG-NRE-FIX: an in-world actor without IOccupySpace has no position (Actor.Location and Actor.CenterPosition both
	// dereference the null OccupiesSpace). PB01-C29-s1-DZFon crashed on one in EngagementLogBotModule.Capture.
	[TestFixture]
	public sealed class EngageLogNoPositionTest
	{
		sealed class StubSpace : IOccupySpace
		{
			public WPos CenterPosition => WPos.Zero;
			public CPos TopLeft { get; init; }
			public (CPos Cell, SubCell SubCell)[] OccupiedCells() => new[] { (TopLeft, SubCell.FullCell) };
		}

		[Test]
		public void NoOccupySpaceMeansNoCell()
		{
			Assert.That(EngagementLogBotModule.TryCell(null, out var cell), Is.False);
			Assert.That(cell, Is.EqualTo(CPos.Zero));
		}

		[Test]
		public void OccupySpaceGivesItsTopLeft()
		{
			Assert.That(EngagementLogBotModule.TryCell(new StubSpace { TopLeft = new CPos(7, 9) }, out var cell), Is.True);
			Assert.That(cell, Is.EqualTo(new CPos(7, 9)));
		}

		[TestCase(true)]
		[TestCase(false)]
		public void AttackerHasPosRoundTrips(bool hasPos)
		{
			var ev = new EngagementHitEvent { SubjectType = "fact", AttackerType = "e3", AttackerInWorld = true, AttackerHasPos = hasPos };
			var back = (EngagementHitEvent)BotEvent.Load(ev.Save().Value);
			Assert.That(back.AttackerHasPos, Is.EqualTo(hasPos));
			Assert.That(back.AttackerInWorld, Is.True);
		}
	}
}
