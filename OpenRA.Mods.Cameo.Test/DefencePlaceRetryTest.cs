#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// DEF-PLACE-RETRY-01 (DEF-LANES P3a): the rejected-cell memory (pure) and source pins of its wiring.
	[TestFixture]
	public class DefencePlaceRetryTest
	{
		const int Grace = 5, Base = 250, Max = 6000;
		static readonly CPos A = new(18, 21);
		static readonly CPos B = new(19, 21);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		[Test]
		public void AnOrderThatProducedNoBuildingRejectsItsCellOnTheNextAttempt()
		{
			var cells = new RejectedCells();
			Assert.That(cells.Begin(9, "tower", 1171, Grace, Base, Max), Is.Null, "nothing pending");
			cells.Issue(9, "tower", A, 1171);
			Assert.That(cells.IsRejected("tower", A, 1172), Is.False, "an issued order is not a rejection");

			// The same queue asks again 48 ticks later (the tick-1219 repeat of seed 100775): the cell is rejected for 250 ticks.
			Assert.That(cells.Begin(9, "tower", 1219, Grace, Base, Max), Is.EqualTo(A));
			Assert.That(cells.IsRejected("tower", A, 1219), Is.True);
			Assert.That(cells.IsRejected("tower", A, 1219 + Base - 1), Is.True);
			Assert.That(cells.IsRejected("tower", A, 1219 + Base), Is.False, "offered again after the time");
			Assert.That(cells.IsRejected("tower", B, 1219), Is.False, "other cells stay open");
			Assert.That(cells.IsRejected("sam", A, 1219), Is.False, "per type");
			Assert.That(cells.PendingCount, Is.EqualTo(0));
		}

		[Test]
		public void AskingAgainInsideTheGraceTimeIsPendingNotARejection()
		{
			var cells = new RejectedCells();
			cells.Issue(9, "tower", A, 100);
			Assert.That(cells.Begin(9, "tower", 100 + Grace - 1, Grace, Base, Max), Is.Null);
			Assert.That(cells.IsRejected("tower", A, 104), Is.False);
			Assert.That(cells.PendingCount, Is.EqualTo(1), "still pending");
		}

		[Test]
		public void AnotherQueueOrAnotherTypeIsNotARejection()
		{
			var cells = new RejectedCells();
			cells.Issue(9, "tower", A, 100);
			Assert.That(cells.Begin(10, "tower", 200, Grace, Base, Max), Is.Null, "another production queue");
			Assert.That(cells.Begin(9, "sam", 200, Grace, Base, Max), Is.Null, "another type");
			Assert.That(cells.IsRejected("tower", A, 200), Is.False);
		}

		[Test]
		public void AConfirmedBuildingClearsThePendingOrderAndTheStrikes()
		{
			var cells = new RejectedCells();
			cells.Issue(9, "tower", A, 100);
			cells.Confirm("tower", A);
			Assert.That(cells.PendingCount, Is.EqualTo(0));
			Assert.That(cells.Begin(9, "tower", 200, Grace, Base, Max), Is.Null, "the next tower of the same queue is a new attempt");

			// A rejected cell that later does get its building is forgotten.
			cells.Reject("tower", B, 300, Base, Max);
			Assert.That(cells.IsRejected("tower", B, 301), Is.True);
			cells.Confirm("tower", B);
			Assert.That(cells.IsRejected("tower", B, 301), Is.False);
			Assert.That(cells.RejectedCount, Is.EqualTo(0));

			// A building of another type or on another cell confirms nothing.
			cells.Issue(9, "tower", A, 400);
			cells.Confirm("sam", A);
			cells.Confirm("tower", B);
			Assert.That(cells.PendingCount, Is.EqualTo(1));
		}

		[Test]
		public void TheMaskRejectsAtOnce()
		{
			var cells = new RejectedCells();
			Assert.That(cells.Reject("tower", A, 500, Base, Max), Is.EqualTo(Base));
			Assert.That(cells.IsRejected("tower", A, 500), Is.True);
		}

		[TestCase(1, 250)]
		[TestCase(2, 500)]
		[TestCase(3, 1000)]
		[TestCase(5, 4000)]
		[TestCase(6, 6000)]
		[TestCase(40, 6000)]
		[TestCase(0, 250)]
		public void TheSkipTimeDoublesWithEveryRejectionUpToTheCap(int strikes, int expected)
		{
			Assert.That(RejectedCells.Duration(strikes, Base, Max), Is.EqualTo(expected));
		}

		[Test]
		public void RepeatedRejectionsOfOneCellBackOff()
		{
			var cells = new RejectedCells();
			Assert.That(cells.Reject("tower", A, 0, Base, Max), Is.EqualTo(250));
			Assert.That(cells.Reject("tower", A, 300, Base, Max), Is.EqualTo(500));
			Assert.That(cells.Reject("tower", A, 900, Base, Max), Is.EqualTo(1000));
			Assert.That(cells.IsRejected("tower", A, 1899), Is.True);
			Assert.That(cells.IsRejected("tower", A, 1900), Is.False);
			Assert.That(RejectedCells.Duration(1, 0, Max), Is.EqualTo(1), "a learned base of 0 still skips one tick");
		}

		[Test]
		public void OldRejectionsAreForgotten()
		{
			// Sixteen cells rejected at tick 0; long after their time plus the cap, a new rejection prunes them.
			var cells = new RejectedCells();
			for (var x = 0; x < 16; x++)
				cells.Reject("tower", new CPos(x, 0), 0, Base, Max);

			Assert.That(cells.RejectedCount, Is.EqualTo(16));
			cells.Reject("tower", A, Base + Max + 1, Base, Max);
			Assert.That(cells.RejectedCount, Is.EqualTo(1));
			Assert.That(cells.Reject("tower", new CPos(0, 0), Base + Max + 2, Base, Max), Is.EqualTo(Base), "its strikes are gone too");
		}

		[Test]
		public void TheRowIdIsFixed()
		{
			Assert.That(AblationIds.DefPlaceRetry, Is.EqualTo("def_place_retry"));
		}

		[Test]
		public void OffMeansNoCallAndTheGivenCheck()
		{
			// Source pins. The switch is off in code; a planner that does not track is never called by the base builder; and with the
			// switch off, or nothing rejected, ChooseDefenseCell keeps the very delegate it was given.
			var planner = Source("mods/Cameo/Traits/BotModules/DefenseCoveragePlanner.cs");
			Assert.That(planner, Does.Contain("public readonly bool PlaceRetryMemory = false;"));
			Assert.That(planner, Does.Contain("canPlace = WithoutRejected(defense, canPlace);"));

			var retry = Source("mods/Cameo/Traits/BotModules/DefenseCoveragePlanner.Retry.cs");
			Assert.That(retry, Does.Contain("bool IBotDefensePlacementFeedback.TracksAttempts => PlaceRetryOn;"));
			Assert.That(retry, Does.Contain("if (!PlaceRetryOn || rejectedCells.RejectedCount == 0)\n\t\t\t\treturn canPlace;").Or.Contain("if (!PlaceRetryOn || rejectedCells.RejectedCount == 0)\r\n\t\t\t\treturn canPlace;"));
			Assert.That(retry, Does.Contain("!ablatePlaceRetry.Off(player, AblationIds.DefPlaceRetry)"));

			var builder = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(builder, Does.Contain("var tracks = advisor is IBotDefensePlacementFeedback feedback && feedback.TracksAttempts;"));
			Assert.That(builder, Does.Contain("&& (!tracks || !baseBuilder.PlacementMasked(actorInfo.Name, cell)));"));
			Assert.That(builder, Does.Contain("return advisor is IBotDefensePlacementFeedback feedback && feedback.TracksAttempts ? feedback : null;"));
			Assert.That(builder.Split("DefencePlacementFeedback()?.").Length - 1, Is.EqualTo(2), "one masked call, one issued call");

			// The failure budget and the cancel it leads to are untouched (the READY guard of NO-CANCEL-01 still stands in front of site 5).
			Assert.That(builder, Does.Contain("else if (++failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)"));
			Assert.That(builder, Does.Contain("baseBuilder.TraceNoCancel(NoCancelRowOn, \"CANCEL-UNPLACEABLE\", 5, currentBuilding.Item, queue.Actor);"));
		}
	}
}
