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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// CRATE-RESERVE-01 (USER rule via Opus 11-24): crates near a yard are reserved while the bot has a yard or an MCV; all others are collected; no yard and no MCV hunts every crate.
	[TestFixture]
	public sealed class CrateReserve01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void AnMcvIsNeededOnlyWithNoOwnYardAndNoOwnMcv()
		{
			Assert.That(CrateReserveCA.NeedsMcv(0, 0), Is.True);
			Assert.That(CrateReserveCA.NeedsMcv(1, 0), Is.False);
			Assert.That(CrateReserveCA.NeedsMcv(0, 1), Is.False);
		}

		[TestCase(0, true)]
		[TestCase(25, true)]
		[TestCase(26, false)]
		public void ACrateWithinTheRadiusOfAYardIsReserved(int cells, bool reserved) =>
			Assert.That(CrateReserveCA.Reserved(cells, 25), Is.EqualTo(reserved));

		[Test]
		public void TheEligibilityFollowsTheThreeRules()
		{
			// has a yard or an MCV: only crates outside the reserve and outside known threat
			Assert.That(CrateReserveCA.Eligible(false, true, false), Is.False, "reserved");
			Assert.That(CrateReserveCA.Eligible(false, false, true), Is.False, "inside known enemy threat");
			Assert.That(CrateReserveCA.Eligible(false, false, false), Is.True, "far from the base and safe: collected actively");
			// no yard and no MCV: every crate, reserved and threatened ones included
			Assert.That(CrateReserveCA.Eligible(true, true, true), Is.True);
			Assert.That(CrateReserveCA.Eligible(true, true, false), Is.True);
			Assert.That(CrateReserveCA.Eligible(true, false, true), Is.True);
		}

		[Test]
		public void TheScanIsGatedAndHuntsFastWithNoYardAndNoMcv()
		{
			var c = Source("mods/Cameo/Traits/BotModules/CratePickupBotModule.cs");
			Assert.That(c, Does.Contain("if (ReserveOn && TryReservedScan(bot, clock))\n\t\t\t\treturn;"));
			Assert.That(c, Does.Contain("bool ReserveOn => Info.CrateReserveMcv && switchesActive && !ablateReserve.Off(player, AblationIds.CrateReserveMcv);"));
			Assert.That(c, Does.Contain("mode != CrateMode.Normal ? Math.Min(Info.ScanForCratesInterval, Info.HuntScanInterval) : Info.ScanForCratesInterval"));
			Assert.That(c, Does.Contain("CrateReserveCA.Eligible(needMcv, reserved, threatened)"));
			Assert.That(c, Does.Contain("var collector = CheapCollector(idleUnits, cell);"), "normal mode: one cheap collector per remembered crate");
			Assert.That(c, Does.Contain("CrateReserveCA.AssignDistinct("), "hunt mode: one unit per crate, distinct crates");
			Assert.That(c, Does.Contain("reservedCrateCells.Contains(location)"), "the path avoids reserved crates");
		}

		[Test]
		public void TheModeFollowsYardAndMcv()
		{
			Assert.That(CrateReserveCA.ModeFor(1, 0), Is.EqualTo(CrateMode.Normal));
			Assert.That(CrateReserveCA.ModeFor(2, 1), Is.EqualTo(CrateMode.Normal));
			Assert.That(CrateReserveCA.ModeFor(0, 0), Is.EqualTo(CrateMode.Hunt));
			Assert.That(CrateReserveCA.ModeFor(0, 1), Is.EqualTo(CrateMode.Escort), "the moment an MCV exists the hunt stops and the escort starts");
			Assert.That(CrateReserveCA.ModeFor(0, 3), Is.EqualTo(CrateMode.Escort));
		}

		[Test]
		public void TheHuntSendsOneDistinctNearestUnitToEachCrate()
		{
			var units = new[] { (0, 0), (10, 0), (20, 0) };
			var crates = new[] { (19, 1), (1, 1) };
			var pairs = CrateReserveCA.AssignDistinct(units, crates);
			Assert.That(pairs.Count, Is.EqualTo(2), "one unit per crate: two crates, two units");
			Assert.That(pairs.Select(p => p.Target).Distinct().Count(), Is.EqualTo(2), "distinct crates");
			Assert.That(pairs.Select(p => p.Unit).Distinct().Count(), Is.EqualTo(2), "distinct units");
			Assert.That(pairs, Does.Contain((2, 0)), "the unit at x=20 takes the crate at x=19");
			Assert.That(pairs, Does.Contain((0, 1)), "the unit at x=0 takes the crate at x=1");
		}

		[Test]
		public void SpareUnitsGetDifferentSearchCellsAndTiesGoToTheCheaperUnit()
		{
			var spare = new[] { (0, 0), (0, 1), (0, 2) };
			var cells = new[] { (30, 30), (40, 40), (50, 50) };
			var pairs = CrateReserveCA.AssignDistinct(spare, cells);
			Assert.That(pairs.Select(p => p.Target).Distinct().Count(), Is.EqualTo(3), "each spare unit searches a different place");
			// equal distance: the lower index (the caller sorts cheapest first) wins
			Assert.That(CrateReserveCA.AssignDistinct(new[] { (0, 0), (0, 0) }, new[] { (5, 5) })[0].Unit, Is.EqualTo(0));
			Assert.That(CrateReserveCA.AssignDistinct(new (int, int)[0], cells), Is.Empty);
			Assert.That(CrateReserveCA.AssignDistinct(spare, new (int, int)[0]), Is.Empty);
		}

		[Test]
		public void TheEscortOrdersOnlyAUnitThatFellBehindAndTheHuntEndsInTheEscortMode()
		{
			Assert.That(CrateReserveCA.NeedsEscortOrder(5, 4), Is.True);
			Assert.That(CrateReserveCA.NeedsEscortOrder(4, 4), Is.False);
			var c = Source("mods/Cameo/Traits/BotModules/CratePickupBotModule.cs");
			var escort = c.IndexOf("if (mode == CrateMode.Escort)", StringComparison.Ordinal);
			Assert.That(escort, Is.GreaterThan(0));
			Assert.That(escort, Is.LessThan(c.IndexOf("foreach (var cell in crateMemory.Keys)", escort, StringComparison.Ordinal)), "the escort returns before any crate is considered: the hunt has stopped");
			Assert.That(c, Does.Contain("bot.QueueOrder(new Order(\"Move\", unit, Target.FromCell(world, mcv.Location), false));"), "the escort order: every free unit moves to the MCV");
			Assert.That(c, Does.Contain("leases?.Release(collector, LeaseOwner);"), "hunting collectors are released when the MCV appears");
		}

		[Test]
		public void TheReserveRuleWorksFromTheCrateMemoryNotTheWholeMap()
		{
			var c = Source("mods/Cameo/Traits/BotModules/CratePickupBotModule.cs");
			var scan = c.IndexOf("bool TryReservedScan(IBot bot, (int VisitTick, int Elapsed, bool InVisit) clock)", StringComparison.Ordinal);
			var body = c.Substring(scan, c.IndexOf("IEnumerable<Actor> EligibleUnits(", scan, StringComparison.Ordinal) - scan);
			Assert.That(body, Does.Not.Contain("ActorsHavingTrait<Crate>"), "no whole-map crate read in any reserve mode");
			Assert.That(body, Does.Contain("foreach (var cell in crateMemory.Keys)"));
			Assert.That(c, Does.Contain("if (!crate.CanBeViewedByPlayer(player))\n\t\t\t\t\tcontinue;\n\n\t\t\t\tpresent.Add(crate.Location);\n\t\t\t\tcrateMemory[crate.Location] = now;"), "an unseen crate neither enters the memory nor counts as present");
			Assert.That(c, Does.Contain("(player.Shroud.IsVisible(cell) && !present.Contains(cell)) || now - seen > Info.CrateMemoryTicks"), "forgotten when seen gone, or after the memory time");
			Assert.That(c, Does.Contain("if (ReserveOn)\n\t\t\t\tUpdateCrateMemory();"), "refreshed on its own cadence, before the pickup cooldown");
		}

		[Test]
		public void TheRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.CrateReserve.cs"), Does.Contain("\"crate_reserve_mcv\""));
			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var crate = player.Single(n => n.Key == "CratePickupBotModule").Value.Nodes;
			Assert.That(crate.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(crate.Single(n => n.Key == "CrateReserveMcv").Value.Value, Is.EqualTo("true"));
		}
	}
}
