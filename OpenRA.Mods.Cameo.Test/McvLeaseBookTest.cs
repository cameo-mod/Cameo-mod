#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P8 (a) (CONSOLIDATE_03_P8_DESIGN_01.md): the one MCV lease record. Same reads and writes as the old inflightMcvSites map; the DEV stream is compiled out of a release.
	[TestFixture]
	public sealed class McvLeaseBookTest
	{
		const string Dir = "mods/Cameo/Traits/BotModules/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void GrantReplacesAndReleaseEnds()
		{
			var book = new McvLeaseBook();
			book.Grant(7, 3, new CPos(40, 40), new CPos(10, 10), 100);
			book.Grant(9, 4, new CPos(60, 20), new CPos(12, 10), 110);
			Assert.That(book.Count, Is.EqualTo(2));
			Assert.That(book.Holds(7), Is.True);
			Assert.That(book.FieldOf(7), Is.EqualTo(3));
			Assert.That(book.FieldOf(8), Is.EqualTo(-1));

			book.Grant(7, 5, new CPos(70, 70), new CPos(10, 10), 200);
			Assert.That(book.Count, Is.EqualTo(2), "a re-grant replaces, it does not add");
			Assert.That(book.FieldOf(7), Is.EqualTo(5));

			Assert.That(book.Release(7), Is.True);
			Assert.That(book.Release(7), Is.False, "already released");
			Assert.That(book.Ids(), Is.EqualTo(new uint[] { 9 }));
		}

		[Test]
		public void TheCentresAreTheOldMapValuesAndTheAvoidListSkipsTheAskingMcv()
		{
			var book = new McvLeaseBook();
			book.Grant(1, 0, new CPos(1, 1), default, 0);
			book.Grant(2, 1, new CPos(2, 2), default, 0);
			book.Grant(3, 2, new CPos(3, 3), default, 0);
			Assert.That(book.Centers().OrderBy(c => c.X), Is.EqualTo(new[] { new CPos(1, 1), new CPos(2, 2), new CPos(3, 3) }));

			var avoid = new List<CPos> { new CPos(9, 9) };
			book.AddCentersExcept(2, avoid);
			Assert.That(avoid.Count, Is.EqualTo(3));
			Assert.That(avoid, Does.Contain(new CPos(9, 9)).And.Contain(new CPos(1, 1)).And.Contain(new CPos(3, 3)));
			Assert.That(avoid, Does.Not.Contain(new CPos(2, 2)), "the asking MCV's own site is not an avoid point");
		}

		[Test]
		public void AChangedCellRestartsTheMovementClock()
		{
			var book = new McvLeaseBook();
			book.Grant(1, 0, new CPos(50, 50), new CPos(5, 5), 100);
			book.Note(1, new CPos(5, 5), 400);
			Assert.That(book.Stalled(1, 1100, 1000), Is.True, "the same cell for 1000 ticks");
			Assert.That(book.Stalled(1, 1099, 1000), Is.False);

			book.Note(1, new CPos(6, 5), 1100);
			Assert.That(book.Stalled(1, 2000, 1000), Is.False, "it moved at 1100");
			Assert.That(book.Stalled(1, 2100, 1000), Is.True);
			Assert.That(book.Stalled(1, 9999, 0), Is.False, "0 = never");
			Assert.That(book.Stalled(2, 9999, 1000), Is.False, "no lease, no stall");
			Assert.That(McvLeaseBook.IsStalled(0, 750, 750), Is.True);
			Assert.That(McvLeaseBook.IsStalled(1, 750, 750), Is.False);
		}

		[Test]
		public void TheOldMapIsGoneAndEveryGrantReleaseAndReadGoesThroughTheBook()
		{
			var planner = Source(Dir + "ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Not.Contain("inflightMcvSites"));
			Assert.That(planner, Does.Contain("readonly McvLeaseBook mcvLeases = new();"));
			Assert.That(planner, Does.Contain("mcvLeases.Grant(mcv.ActorID, s.Index, s.Center, mcv.Location, tick);"));
			Assert.That(planner, Does.Contain("mcvLeases.AddCentersExcept(mcv.ActorID, avoid);"));
			Assert.That(planner, Does.Contain("Info.FieldCoverage && mcvLeases.Count > 0 && TakenByMcvSite(center, mcvLeases.Centers(), Info.ClaimRadiusCells)"));
			Assert.That(planner, Does.Contain("LeaseObserved((int)McvLeaseEnd.HolderGone, id, mcvLeases.FieldOf(id), default);\n\t\t\t\t\t\tmcvLeases.Release(id);"));

			var safe = Source(Dir + "ExpansionPlannerBotModule.McvSafe.cs");
			Assert.That(safe, Does.Not.Contain("inflightMcvSites"));
			Assert.That(safe, Does.Contain("LeaseObserved((int)McvLeaseEnd.Abort, mcv.ActorID, track.Field, default);\n\t\t\t\tmcvLeases.Release(mcv.ActorID);"));
		}

		[Test]
		public void TheDevStreamIsCompiledOutOfARelease()
		{
			var stream = Source(Dir + "McvLeaseStream.cs");
			Assert.That(stream, Does.Contain("#if CAMEO_DEV_LOGGING\nnamespace"));
			Assert.That(stream, Does.Contain("EX-LEASE-STREAM"));
			Assert.That(stream, Does.Contain("1099511628211UL"), "FNV-1a 64");

			var planner = Source(Dir + "ExpansionPlannerBotModule.cs");
			Assert.That(planner, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid LeaseObserved("));
			Assert.That(planner, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\treadonly McvLeaseStream mcvLeaseStream = new();"));
		}
	}
}
