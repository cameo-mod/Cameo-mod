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
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q8 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Steps 2-3; rows sq_all_in_late and sq_hunt_one_cell under sq_pipeline_02): the all-in has one input list (cap, huge, won,
	/// the breaker's stalemate signal of Q4, and the late-escalation step), the breaker stays inert, and the team hunts one unseen cell at a time. The rules are pure; the wiring is pinned.
	/// </summary>
	[TestFixture]
	public class Squad02Q8Test
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

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		// ---- the late step ----

		[Test]
		public void TheLateStepFiresAfterTheConfiguredNumberOfFullSteps()
		{
			Assert.That(AllInCA.WhyLate, Is.EqualTo("late"));
			Assert.That(AllInCA.LateStep(39999, 25000, 5000, 3), Is.False, "two full steps");
			Assert.That(AllInCA.LateStep(40000, 25000, 5000, 3), Is.True, "the third step is complete");
			Assert.That(AllInCA.LateStep(90000, 25000, 5000, 3), Is.True, "and it stays on");
			Assert.That(AllInCA.LateStep(24999, 25000, 5000, 3), Is.False, "before the start tick");
			Assert.That(AllInCA.LateStep(25000, 25000, 5000, 1), Is.False, "no step has passed at the start tick");
			Assert.That(AllInCA.LateStep(30000, 25000, 5000, 1), Is.True);
		}

		[Test]
		public void TheLateStepIsOffForNoStepsOrNoStepLength()
		{
			Assert.That(AllInCA.LateStep(90000, 25000, 5000, 0), Is.False, "0 steps = off");
			Assert.That(AllInCA.LateStep(90000, 25000, 5000, -2), Is.False);
			Assert.That(AllInCA.LateStep(90000, 25000, 0, 3), Is.False, "no step length = no steps (the cadence does not step either)");
			Assert.That(AllInCA.LateStep(90000, 25000, -5000, 3), Is.False);
		}

		// The default of three steps is the cadence reaching its floor: the same clock (SteppedPercent) that paces the attack delay commits the army.
		[Test]
		public void TheDefaultStepCountIsWhereTheCadenceReachesItsFloor()
		{
			Assert.That(Stalemate01CA.SteppedPercent(39999, 25000, 50, 5000, 75, 20), Is.EqualTo(27), "two steps: 50 -> 37 -> 27");
			Assert.That(Stalemate01CA.SteppedPercent(40000, 25000, 50, 5000, 75, 20), Is.EqualTo(20), "three steps: the floor");
			Assert.That(AllInCA.LateStep(40000, 25000, 5000, 3), Is.True);
			Assert.That(AllInCA.LateStep(39999, 25000, 5000, 3), Is.False);
		}

		// ---- the one hunt cell ----

		[Test]
		public void TheTeamCellIsKeptWhileUnexploredAndWithinTheHold()
		{
			Assert.That(TeamHuntCell.Keep(true, false, 1000, 1000, 2250), Is.True, "taken this tick");
			Assert.That(TeamHuntCell.Keep(true, false, 1000, 3250, 2250), Is.True, "the edge is inside");
			Assert.That(TeamHuntCell.Keep(true, false, 1000, 3251, 2250), Is.False, "held too long: nobody reached it");
			Assert.That(TeamHuntCell.Keep(true, true, 1000, 1500, 2250), Is.False, "explored (seen or reached): the hunt moves on");
			Assert.That(TeamHuntCell.Keep(false, false, 1000, 1500, 2250), Is.False, "no cell");
			Assert.That(TeamHuntCell.Keep(true, false, -1, 1500, 2250), Is.False, "never taken");
			Assert.That(TeamHuntCell.Keep(true, false, 2000, 1500, 2250), Is.False, "from the future");
			Assert.That(TeamHuntCell.Keep(true, false, 1000, 1001, 0), Is.True, "a hold of 0 is clamped to 1 tick");
			Assert.That(TeamHuntCell.Keep(true, false, 1000, 1002, 0), Is.False);
		}

		[Test]
		public void AnAskerAtTheCellHasReachedIt()
		{
			Assert.That(TeamHuntCell.Reached(0, 4), Is.True);
			Assert.That(TeamHuntCell.Reached(64, 4), Is.True, "8 cells = twice the arrival radius: the edge is inside");
			Assert.That(TeamHuntCell.Reached(65, 4), Is.False);
			Assert.That(TeamHuntCell.Reached(1, 0), Is.False, "no radius: only the cell itself");
			Assert.That(TeamHuntCell.Reached(0, 0), Is.True);
		}

		[Test]
		public void ThePureRulesTouchNoWorldAndNoRandom()
		{
			var pure = RepoFile(Bots + "Squads/TeamHuntCell.cs");
			Assert.That(pure, Does.Not.Contain("Random"));
			Assert.That(pure, Does.Not.Contain("World"));
			Assert.That(pure, Does.Not.Contain("Actor "));
		}

		// ---- the rows and the wiring ----

		[Test]
		public void TheRowsAreRegisteredAndWiredBehindTheMasterRow()
		{
			Assert.That(AblationIds.SqAllInLate, Is.EqualTo("sq_all_in_late"));
			Assert.That(AblationIds.SqHuntOneCell, Is.EqualTo("sq_hunt_one_cell"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInFold.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqAllInLate, ablateSqHuntOneCell;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyHuntOneCellOn => ArmyPipelineOn && !ablateSqHuntOneCell.Off(Player, AblationIds.SqHuntOneCell);"));

			// The late step needs the one launcher and the late escalation it counts the steps of, and starts no earlier than the all-in itself.
			var late = Between(wiring, "internal bool ArmyAllInLateOn(int now)", "/// <summary>Q8 (row sq_hunt_one_cell)");
			Assert.That(late, Does.Contain("ArmyLaunchArbiterOn && LateEscalationActive && !ablateSqAllInLate.Off(Player, AblationIds.SqAllInLate)"));
			Assert.That(late, Does.Contain("&& now >= Info.TeamLeadAllInStartTick"));
			Assert.That(late, Does.Contain("AllInCA.LateStep(now, Info.ArmyAllInLateStartTick >= 0 ? Info.ArmyAllInLateStartTick : Info.LateEscalationStartTick, Info.LateEscalationStepTicks, Info.ArmyAllInLateSteps);"));

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.Pipeline02.cs");
			Assert.That(info, Does.Contain("public readonly int ArmyAllInLateSteps = 3;"));
			Assert.That(info, Does.Contain("public readonly int ArmyAllInLateStartTick = -1;"), "-1 = the escalation start, as before");
			Assert.That(AllInCA.LateStep(24000, 9000, 5000, 3), Is.True, "yaml 9000 + 3 x 5000 = fires at 24000, before the 30000 decisive cut");
			Assert.That(AllInCA.LateStep(23999, 9000, 5000, 3), Is.False);
			Assert.That(info, Does.Contain("public readonly int ArmyHuntCellHoldTicks = 2250;"));
		}

		[Test]
		public void TheLateStepIsTheLastAllInInputAndTheNamedTriggersKeepTheirName()
		{
			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			var why = Between(v07, "string TeamLeadAllInWhy(", "// Fog-honest, like TeamLeadKnownBuildings");
			var late = why.IndexOf("if (!atCap && !huge && !won && ArmyAllInLateOn(now))\n\t\t\t\treturn AllInCA.WhyLate;", StringComparison.Ordinal);
			var named = why.IndexOf("return AllInCA.Why(atCap, huge, won)", StringComparison.Ordinal);
			Assert.That(late, Is.GreaterThan(0));
			Assert.That(named, Is.GreaterThan(late), "the named triggers are read after the late step, and never hidden by it (it only answers when none of them fired)");
			Assert.That(why.IndexOf("var won = false;", StringComparison.Ordinal), Is.LessThan(late), "the late step reads the cap/huge/won results");
			Assert.That(why, Does.Contain("?? (ArmyLaunchArbiterOn && AllInCA.StalemateSignal(now, team.StalemateSignalTick, Info.ArmyStalemateSignalTicks) ? AllInCA.WhyStalemate : null);"),
				"the Q4 stalemate input is unchanged");
		}

		[Test]
		public void TheBreakerStaysInertUnderTheLauncherRow()
		{
			var stalemate = RepoFile(Bots + "SquadManagerBotModuleCA.Stalemate.cs");
			var hook = stalemate.IndexOf("if (ArmyLaunchArbiterOn)\n\t\t\t{\n\t\t\t\tArmyNoteStalemate(t);", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThan(0));
			Assert.That(hook, Is.LessThan(stalemate.IndexOf("StalemateCommit(bot, seat, t, admitted, pool, n0, v0);", StringComparison.Ordinal)), "before the commit: the breaker never launches");

			// The late step is not a launcher either: it only marks the order the leader publishes anyway.
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInFold.cs");
			Assert.That(wiring, Does.Not.Contain("CreateAttackForce"));
			Assert.That(wiring, Does.Not.Contain("StalemateCommit"));
			Assert.That(wiring, Does.Not.Contain("LaunchSourceRecord"));
		}

		[Test]
		public void TheHuntAsksTheTeamBoardFirstAndFallsBackToThePlainPick()
		{
			var logic = RepoFile(Bots + "SquadManagerBotModuleCA.Stalemate01.cs");
			Assert.That(logic, Does.Contain("internal CPos? FinishHuntCell(Actor leader, bool ownPick = false)"));
			var hook = logic.IndexOf("if (!ownPick && ArmyHuntOneCellOn)\n\t\t\t\treturn TeamHuntCellFor(leader);", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThan(0));
			Assert.That(hook, Is.LessThan(logic.IndexOf("var mobile = leader?.TraitOrDefault<Mobile>();", StringComparison.Ordinal)), "the hook comes first, the plain pick is the old body");
			Assert.That(logic.Split("huntAssigned[cell] = World.WorldTick;").Length - 1, Is.EqualTo(2), "the plain pick still marks a picked and an unreachable cell");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.AllInFold.cs");
			var ask = Between(wiring, "CPos? TeamHuntCellFor(Actor leader)", "\n\t}\n}");
			var lines = new[]
			{
				"if (mobile == null || team == null)\n\t\t\t\treturn FinishHuntCell(leader, true);",
				"team.HuntCell is { } shared",
				"TeamHuntCell.Keep(true, Player.Shroud.IsVisible(shared) || TeamHuntCell.Reached((leader.Location - shared).LengthSquared, Info.FinishHuntArriveCells), team.HuntTick, now,",
				"mobile.PathFinder.PathExistsForLocomotor(mobile.Locomotor, leader.Location, shared) ? shared : FinishHuntCell(leader, true);",
				"var picked = FinishHuntCell(leader, true);",
				"team.HuntCell = cell;",
				"team.HuntTick = now;"
			};

			var last = -1;
			foreach (var line in lines)
			{
				var at = ask.IndexOf(line, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThan(last), line);
				last = at;
			}

			// Fog-honest: the bot's own vision only; no enemy actor is read to choose the cell.
			foreach (var banned in new[] { "FindActors", "FindClosestEnemy", "FrozenActorLayer", "World.Actors", "CanBeViewedByPlayer", "ActorsWithTrait" })
				Assert.That(ask, Does.Not.Contain(banned), banned);

			// A cell this asker cannot walk to is not taken from the team: the board is written only after the plain pick.
			Assert.That(ask.IndexOf("team.HuntCell = cell;", StringComparison.Ordinal), Is.GreaterThan(ask.IndexOf("var picked = FinishHuntCell(leader, true);", StringComparison.Ordinal)));
		}

		[Test]
		public void TheTeamBoardCarriesTheHuntCell()
		{
			var board = RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs");
			var team = Between(board, "public int StalemateSignalTick = -1;", "public int FocusSeat = -1;");
			Assert.That(team, Does.Contain("public CPos? HuntCell;"));
			Assert.That(team, Does.Contain("public int HuntTick = -1;"));
		}

		[Test]
		public void TheOtherHuntCallersAreUnchanged()
		{
			// Every caller keeps its call (the hook is inside FinishHuntCell), so the order's own hunt, a raider's hunt and a squad the order left all draw the same team cell.
			var ground = RepoFile(Bots + "Squads/States/GroundStatesCA.cs");
			Assert.That(ground, Does.Contain("owner.SquadManager.FinishHuntCell(owner.Units.First().Actor)"));
			var pick = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			Assert.That(pick, Does.Contain("var cell = FinishHuntCell(source);"));
			Assert.That(pick, Does.Contain("if (FinishHuntCell(huntSource) is not { } hunt)"));
		}
	}
}
