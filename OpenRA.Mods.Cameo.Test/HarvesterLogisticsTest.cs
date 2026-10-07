#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// SPEC_2026-10-05_econ_logistics Part B: the pure rules behind BU_harvester_logistics.
	[TestFixture]
	public class HarvesterLogisticsTest
	{
		[Test]
		public void ReservationScalesWithFieldCellsAndHonoursCap()
		{
			Assert.That(HarvesterLogistics.Reservation(0, 4, 4), Is.EqualTo(0));
			Assert.That(HarvesterLogistics.Reservation(3, 4, 4), Is.EqualTo(1));
			Assert.That(HarvesterLogistics.Reservation(16, 4, 4), Is.EqualTo(4));
			Assert.That(HarvesterLogistics.Reservation(17, 4, 4), Is.EqualTo(4));
			Assert.That(HarvesterLogistics.Reservation(17, 4, 0), Is.EqualTo(5));
			Assert.That(HarvesterLogistics.Reservation(17, 4, -1), Is.EqualTo(5));
		}

		[Test]
		public void ReservationNeverDividesByZero()
		{
			Assert.That(HarvesterLogistics.Reservation(50, 0, 4), Is.EqualTo(0));
			Assert.That(HarvesterLogistics.Reservation(50, -3, 4), Is.EqualTo(0));
		}

		[Test]
		public void MarginBlocksBelowTheBandAndPassesAtIt()
		{
			Assert.That(HarvesterLogistics.MarginPasses(100, 124, 25), Is.False);
			Assert.That(HarvesterLogistics.MarginPasses(100, 125, 25), Is.True);
			Assert.That(HarvesterLogistics.MarginPasses(100, 90, 25), Is.False);
			Assert.That(HarvesterLogistics.MarginPasses(200, 260, 25), Is.True);
		}

		[Test]
		public void MarginAcceptsAnyNonEmptyFieldFromADepletedOne()
		{
			Assert.That(HarvesterLogistics.MarginPasses(0, 0, 25), Is.False);
			Assert.That(HarvesterLogistics.MarginPasses(0, 5, 25), Is.True);
			Assert.That(HarvesterLogistics.MarginPasses(-1, 5, 25), Is.True);
		}
	}
}
