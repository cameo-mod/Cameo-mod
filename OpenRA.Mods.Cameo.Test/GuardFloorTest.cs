#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-03 P7 (b), row sq_guard_floor (design 01 R4 guard minimum; Opus review change 2): the minimum is learned (personality prior, per-player override), the lend takes only the units above it, an open all-in
	/// drops it to 0, and the lend-return window is a learnable input of the same table.
	/// </summary>
	[TestFixture]
	public class GuardFloorTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		[Test]
		public void TheFloorIsAPercentOfTheGuardRoundedUpAndNeverMoreThanTheGuard()
		{
			Assert.That(GuardFloor.Units(20, 60), Is.EqualTo(12));
			Assert.That(GuardFloor.Units(7, 60), Is.EqualTo(5), "4.2 rounds up");
			Assert.That(GuardFloor.Units(10, 0), Is.EqualTo(0));
			Assert.That(GuardFloor.Units(10, 100), Is.EqualTo(10));
			Assert.That(GuardFloor.Units(10, 250), Is.EqualTo(10), "clamped to 100 percent");
			Assert.That(GuardFloor.Units(10, -5), Is.EqualTo(0));
			Assert.That(GuardFloor.Units(0, 60), Is.EqualTo(0));
			Assert.That(GuardFloor.Units(-3, 60), Is.EqualTo(0));
		}

		[Test]
		public void AnOpenAllInDropsTheFloorToZeroUnlessItIsEnforcedForAnOwnBaseDefence()
		{
			Assert.That(GuardFloor.FloorUnits(20, 60, false, false), Is.EqualTo(12));
			Assert.That(GuardFloor.FloorUnits(20, 60, true, false), Is.EqualTo(0), "the dynamic all-in rule");
			Assert.That(GuardFloor.FloorUnits(20, 60, true, true), Is.EqualTo(12), "enforced for an own-base defence (R1/R3): the floor stays");
			Assert.That(GuardFloor.FloorUnits(20, 60, false, true), Is.EqualTo(12));
		}

		[Test]
		public void TheLendTakesOnlyTheUnitsAboveTheFloorAndStillNeedsTheJoinFloor()
		{
			// guard 20, turtle floor 12 -> 8 spare: a planned lend of 10 becomes 8
			Assert.That(GuardFloor.Lend(10, 20, 12, 3), Is.EqualTo(8));
			// the plan is smaller than the spare: the plan stands
			Assert.That(GuardFloor.Lend(5, 20, 12, 3), Is.EqualTo(5));
			// the spare is below the join floor: a squad could not form, nothing is lent
			Assert.That(GuardFloor.Lend(10, 20, 18, 3), Is.EqualTo(0));
			// a floor above the guard leaves no spare
			Assert.That(GuardFloor.Lend(10, 5, 9, 1), Is.EqualTo(0));
			// no floor (all-in): the plan is the lend
			Assert.That(GuardFloor.Lend(10, 20, 0, 3), Is.EqualTo(10));
			// nothing planned, nothing lent; the join floor is at least 1
			Assert.That(GuardFloor.Lend(0, 20, 0, 0), Is.EqualTo(0));
			Assert.That(GuardFloor.Lend(1, 20, 0, 0), Is.EqualTo(1));
			Assert.That(GuardFloor.Lend(-4, 20, 0, 1), Is.EqualTo(0));
		}

		[Test]
		public void AnAllInLendsUpToTheMostAGuardEverLendsAndTheFloorNeverRaisesTheLend()
		{
			Assert.That(GuardFloor.AllInLendPercent, Is.EqualTo(GuardLend.MaxPercent));
			var planned = GuardLend.Units(20, 20, 50, 3);
			Assert.That(planned, Is.EqualTo(10));
			var allIn = GuardLend.Units(20, 20, GuardFloor.AllInLendPercent, 3);
			Assert.That(allIn, Is.EqualTo(18), "90 percent of 20");
			Assert.That(GuardFloor.Lend(allIn, 20, GuardFloor.FloorUnits(20, 60, true, false), 3), Is.EqualTo(18));
			Assert.That(GuardFloor.Lend(planned, 20, GuardFloor.FloorUnits(20, 60, false, false), 3), Is.LessThanOrEqualTo(planned), "narrows, never adds");

			// for every guard size and floor the floor row never lends more than the plain lend share
			for (var guard = 0; guard <= 40; guard++)
				for (var pct = 0; pct <= 100; pct += 10)
				{
					var plain = GuardLend.Units(guard, guard, 50, 3);
					var floored = GuardFloor.Lend(plain, guard, GuardFloor.FloorUnits(guard, pct, false, false), 3);
					Assert.That(floored, Is.LessThanOrEqualTo(plain), "guard " + guard + " floor " + pct);
					Assert.That(guard - floored, Is.GreaterThanOrEqualTo(Math.Min(guard, GuardFloor.Units(guard, pct))).Or.EqualTo(guard), "the guard keeps its floor");
				}
		}

		[Test]
		public void AnAllInShareIsEvaluatedEvenWhenTheOrdinaryShareMissesTheJoinMinimum()
		{
			// guard 4, join minimum 3: the ordinary 50 percent share is 2 (< 3), so the plain lend is 0 ...
			var plain = GuardLend.Units(4, 4, 50, 3);
			Assert.That(plain, Is.EqualTo(0));

			// ... but the open all-in share (90 percent) is 3, and with the all-in floor of 0 the floor row lends all 3. The wiring therefore must ask the row BEFORE rejecting the zero.
			var allIn = GuardLend.Units(4, 4, GuardFloor.AllInLendPercent, 3);
			Assert.That(allIn, Is.EqualTo(3));
			Assert.That(GuardFloor.Lend(allIn, 4, GuardFloor.FloorUnits(4, 60, true, false), 3), Is.EqualTo(3));

			// without an all-in the zero stays a zero: a planned 0 never becomes a lend, with or without a floor
			Assert.That(GuardFloor.Lend(plain, 4, GuardFloor.FloorUnits(4, 0, false, false), 3), Is.EqualTo(0));
			Assert.That(GuardFloor.Lend(plain, 4, GuardFloor.FloorUnits(4, 60, false, false), 3), Is.EqualTo(0));
		}

		[Test]
		public void TheReturnWindowIsDueOnceItHasRun()
		{
			Assert.That(GuardFloor.ReturnDue(3100, 100, 3000), Is.True);
			Assert.That(GuardFloor.ReturnDue(3099, 100, 3000), Is.False);
			Assert.That(GuardFloor.ReturnDue(9999, 100, 0), Is.False, "0 = no return");
			Assert.That(GuardFloor.ReturnDue(9999, -1, 3000), Is.False, "no lend recorded");
		}

		[Test]
		public void TheTableIsThePriorUntilALearnerOverridesItAndEveryValueIsClamped()
		{
			var t = new GuardFloorTable(60, 3000);
			Assert.That(t.FloorPct, Is.EqualTo(60));
			Assert.That(t.ReturnTicks, Is.EqualTo(3000));

			t.Override(GuardFloorTable.PercentKey, 35);
			t.Override(GuardFloorTable.ReturnKey, 1200);
			Assert.That(t.FloorPct, Is.EqualTo(35));
			Assert.That(t.ReturnTicks, Is.EqualTo(1200));
			Assert.That(t.Overrides.Count, Is.EqualTo(2));

			t.Override(GuardFloorTable.PercentKey, 400);
			t.Override(GuardFloorTable.ReturnKey, 999999);
			Assert.That(t.FloorPct, Is.EqualTo(100));
			Assert.That(t.ReturnTicks, Is.EqualTo(GuardFloorTable.MaxReturnTicks));
			t.Override(GuardFloorTable.PercentKey, -7);
			Assert.That(t.FloorPct, Is.EqualTo(0));

			t.ClearOverride(GuardFloorTable.PercentKey);
			Assert.That(t.FloorPct, Is.EqualTo(60), "clearing restores the prior");
			t.ClearOverrides();
			Assert.That(t.ReturnTicks, Is.EqualTo(3000));
			Assert.That(t.Overrides, Is.Empty);

			Assert.That(new GuardFloorTable(-20, -1).FloorPct, Is.EqualTo(0), "a bad prior is clamped too");
			Assert.That(new GuardFloorTable(-20, -1).ReturnTicks, Is.EqualTo(0));
		}

		[Test]
		public void TheRowHasItsOwnAblationIdInfoSwitchOffInCodeAndPriorsPerPersonality()
		{
			Assert.That(AblationIds.SqGuardFloor, Is.EqualTo("sq_guard_floor"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.SqGuardFloor.cs"), Does.Contain("public const string SqGuardFloor = \"sq_guard_floor\";"));
			Assert.That(Regex.IsMatch(AblationIds.SqGuardFloor, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.GuardFloor.cs");
			Assert.That(info, Does.Contain("public readonly bool GuardFloorEnabled = false;"), "off in code");
			Assert.That(info, Does.Contain("public readonly int GuardFloorPercent = 0;"));
			Assert.That(info, Does.Contain("public readonly int GuardFloorReturnTicks = 3000;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Count(yaml[..classic], "\n\t\tGuardFloorEnabled: true\n"), Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("GuardFloor"), "classic untouched");

			// the prior per personality: turtle high, rush and steamroller low (Opus review change 2)
			int Prior(string block)
			{
				var start = yaml.IndexOf("\tSquadManagerBotModuleCA@" + block + ":", StringComparison.Ordinal);
				Assert.That(start, Is.GreaterThan(0), block);
				var m = Regex.Match(yaml[start..], "\n\t\tGuardFloorPercent: (\\d+)\n");
				Assert.That(m.Success, Is.True, block);
				return int.Parse(m.Groups[1].Value);
			}

			Assert.That(Prior("turtle"), Is.GreaterThan(Prior("tech")));
			Assert.That(Prior("turtle"), Is.GreaterThan(Prior("rush")));
			Assert.That(Prior("tech"), Is.GreaterThan(Prior("rush")));
			Assert.That(Prior("turtle"), Is.GreaterThan(Prior("steamroller")));
			Assert.That(Prior("rush"), Is.LessThanOrEqualTo(Prior("guerrilla")));
			Assert.That(Prior("steamroller"), Is.LessThanOrEqualTo(Prior("guerrilla")));
			Assert.That(Count(yaml, "GuardFloorPercent:"), Is.EqualTo(6));
		}

		[Test]
		public void TheLendIsNarrowedByTheFloorAfterTheLendShareAndOnlyWhileTheRowIsOn()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			var share = wiring.IndexOf("var lend = GuardLend.Units(guard.Units.Count, armyGuardLend.Count, Info.ArmyGuardLendPercent, Info.TeamLeadJoinMinUnits);", StringComparison.Ordinal);
			var floor = wiring.IndexOf("if (GuardFloorOn)\n\t\t\t\tlend = GuardFloorLend(guard, lend);", StringComparison.Ordinal);
			var empty = wiring.IndexOf("if (lend <= 0)", StringComparison.Ordinal);
			Assert.That(wiring, Does.Not.Contain("lend > 0 && GuardFloorOn"), "asked before a zero is rejected: an open all-in share is evaluated although the ordinary share misses the join minimum (Sol FIX_STATIC on db515678)");
			var cut = wiring.IndexOf("armyGuardLend.RemoveRange(lend, armyGuardLend.Count - lend);", StringComparison.Ordinal);
			Assert.That(share, Is.GreaterThan(0));
			Assert.That(floor, Is.GreaterThan(share), "after the share, so the row can only narrow it");
			Assert.That(empty, Is.GreaterThan(floor));
			Assert.That(cut, Is.GreaterThan(empty), "the list is cut to the narrowed lend");
			Assert.That(Count(wiring, "GuardFloorLend("), Is.EqualTo(1));

			var part = RepoFile(Bots + "SquadManagerBotModuleCA.GuardFloor.cs");
			Assert.That(part, Does.Contain("AblationLatch ablateGuardFloor;"));
			Assert.That(part, Does.Contain("internal bool GuardFloorOn => ArmyGuardLendOn && Info.GuardFloorEnabled && !ablateGuardFloor.Off(Player, AblationIds.SqGuardFloor);"), "needs guard-lend");
			Assert.That(part, Does.Contain("guardFloorTable ??= new GuardFloorTable(Info.GuardFloorPercent, Info.GuardFloorReturnTicks)"), "the prior is the yaml; a learner overrides the table");
			Assert.That(part, Does.Contain("var allIn = TeamLeadOpenOrder() is { AllIn: true };"));
			Assert.That(part, Does.Contain("var floor = GuardFloor.FloorUnits(count, GuardFloorParameters.FloorPct, allIn, false);"), "the floor is the learned percent; 0 during an all-in");
			Assert.That(part, Does.Contain("GuardFloor.Lend(plannedLend, count, floor, Info.TeamLeadJoinMinUnits)"));

			// nothing but the lend count changes: no unit moves, no state, no order, no draw
			foreach (var banned in new[] { "guard.Units.Remove", "unitsHangingAroundTheBase", "QueueOrder", "ChangeState", "DismissSquad", "Random" })
				Assert.That(part, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void TheSeamIsWorldFreeAndTheGuardLendPinsStillHold()
		{
			var seam = RepoFile(Bots + "Squads/GuardFloor.cs");
			foreach (var banned in new[] { "Random", "World", "Actor", "Player", "FindActors" })
				Assert.That(seam, Does.Not.Contain(banned), banned);

			// the existing guard-lend wiring text is untouched: the share line, the quiet test, the banned scans
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			foreach (var banned in new[] { "FindClosestEnemy", "FindActorsInCircle", "World.Actors", "ActorsWithTrait", "FrozenActorLayer", "Random" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);
		}
	}
}
