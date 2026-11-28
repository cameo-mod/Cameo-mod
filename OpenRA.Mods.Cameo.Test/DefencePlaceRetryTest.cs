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
		const int Base = 250, Max = 6000;
		static readonly CPos A = new(18, 21);
		static readonly CPos B = new(19, 21);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		[Test]
		public void TheMaskRejectsAtOnceAndOnlyThatCellAndType()
		{
			var cells = new RejectedCells();
			Assert.That(cells.IsRejected("tower", A, 500), Is.False);
			Assert.That(cells.Reject("tower", A, 500, Base, Max), Is.EqualTo(Base));
			Assert.That(cells.IsRejected("tower", A, 500), Is.True);
			Assert.That(cells.IsRejected("tower", A, 500 + Base - 1), Is.True);
			Assert.That(cells.IsRejected("tower", A, 500 + Base), Is.False, "offered again after the time");
			Assert.That(cells.IsRejected("tower", B, 500), Is.False, "other cells stay open");
			Assert.That(cells.IsRejected("sam", A, 500), Is.False, "per type");
		}

		[Test]
		public void ABuildingOnTheCellForgetsItsRejection()
		{
			var cells = new RejectedCells();
			cells.Reject("tower", B, 300, Base, Max);
			cells.Confirm("sam", B);
			cells.Confirm("tower", A);
			Assert.That(cells.IsRejected("tower", B, 301), Is.True, "another type or another cell confirms nothing");
			cells.Confirm("tower", B);
			Assert.That(cells.IsRejected("tower", B, 301), Is.False);
			Assert.That(cells.RejectedCount, Is.EqualTo(0));
			Assert.That(cells.Reject("tower", B, 400, Base, Max), Is.EqualTo(Base), "its strikes are gone too");
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
		public void ARepeatedRequestIsNeverARejection()
		{
			// Sol/Sonnet FIX: the only rejection is the builder's own refusal (the mask at the emission point). Nothing is keyed by a producer,
			// no order is tracked as pending, and no time-delayed same-type request strikes a cell.
			var memory = Source("mods/Cameo/Traits/BotModules/RejectedCells.cs");
			Assert.That(memory, Does.Not.Contain("pending"));
			Assert.That(memory, Does.Not.Contain("Producer"));

			var contract = Source("mods/CA/Traits/BotModules/IBotDefensePlacementFeedback.cs");
			Assert.That(contract, Does.Not.Contain("BeginAttempt"));
			Assert.That(contract, Does.Not.Contain("AttemptIssued"));

			var retry = Source("mods/Cameo/Traits/BotModules/DefenseCoveragePlanner.Retry.cs");
			Assert.That(retry.Split("rejectedCells.Reject(").Length - 1, Is.EqualTo(1), "one rejection site: AttemptMasked");
		}

		[Test]
		public void OffMeansNoCallNoMaskAndTheGivenCheck()
		{
			// Source pins. The switch is off in code; a planner that does not track is never called by the base builder and its candidates are
			// not mask-filtered; and with the switch off, or nothing rejected, ChooseDefenseCell keeps the very delegate it was given.
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
			Assert.That(builder.Split("DefencePlacementFeedback()?.").Length - 1, Is.EqualTo(1), "one call: the masked refusal");

			// The failure budget and the cancel it leads to are untouched (the READY guard of NO-CANCEL-01 still stands in front of site 5).
			Assert.That(builder, Does.Contain("else if (++failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)"));
			Assert.That(builder, Does.Contain("baseBuilder.TraceNoCancel(NoCancelRowOn, \"CANCEL-UNPLACEABLE\", 5, currentBuilding.Item, queue.Actor);"));
		}
	}
}
