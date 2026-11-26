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

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q9 (row sq_states_micro under sq_pipeline_02; design 4.1 Step 5): the squad state machines are micro only. A team MAIN squad's Idle state holds (the order is the only launcher),
	/// a lost target takes a LOCAL enemy only (else the squad reports the target done and stands idle), and Flee reports Fled. The state classes only ask the module; the rules sit behind the row.
	/// The hooks are insert-only: every older pin on the state code (bounded retarget, keeps going, hand-off, launch sources) stays as it was.
	/// </summary>
	[TestFixture]
	public class Squad02Q9Test
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
			Assert.That(start, Is.GreaterThanOrEqualTo(0), "missing: " + from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), "missing after it: " + to);
			return text.Substring(start, end - start);
		}

		static string Ground() => RepoFile(Bots + "Squads/States/GroundStatesCA.cs");

		[Test]
		public void TheRowNeedsTheOneLauncherAndTheTargetPick()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.StatesMicro.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqStatesMicro;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyStatesMicroOn => ArmyLaunchArbiterOn && ArmyTargetPickOn && !ablateSqStatesMicro.Off(Player, AblationIds.SqStatesMicro);"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.Squad02StatesMicro.cs"), Does.Contain("public const string SqStatesMicro = \"sq_states_micro\";"));
		}

		[Test]
		public void OnlyAMainSquadOfTheTeamHoldsAndHarassKeepsItsQuorumLaunch()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.StatesMicro.cs");
			Assert.That(wiring, Does.Contain("internal bool ArmyIdleHolds(SquadCA squad) => ArmyStatesMicroOn && TeamLeadTeamSquad(squad);"));
			Assert.That(wiring, Does.Contain("internal bool ArmyLocalOnly(SquadCA squad) => ArmyStatesMicroOn && TeamLeadTeamSquad(squad) && !TeamLeadKeepsGoing(squad) && !TeamLeadBoundsRetarget(squad);"),
				"a squad that follows its order or is already bounded to its fight keeps that older rule");

			// TeamLeadTeamSquad is the Rush main squad: no response, breaker or m13 squad, and no raider.
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(team, Does.Contain("bool TeamLeadTeamSquad(SquadCA s) => s.Type == SquadCAType.Rush && !s.M13Response"));
			foreach (var banned in new[] { "Harass", "Guerrilla", "Artillery" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void TheLocalPickLooksOnlyAtTheScanRadiusAroundTheFirstUnit()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.StatesMicro.cs");
			var pick = Between(wiring, "internal bool ArmyLocalPick(SquadCA squad)", "/// <summary>Q9 report");
			Assert.That(pick, Does.Contain("if (squad.Units.Count == 0)\n\t\t\t\treturn false;"));
			Assert.That(pick, Does.Contain("FindClosestEnemy(squad.Units[0].Actor, WDist.FromCells(Info.AttackScanRadius), squad);"));
			Assert.That(pick, Does.Contain("squad.TargetActor = near;"));
			foreach (var banned in new[] { "Random", "FindActors", "World.Actors", "ActorsWithTrait", "FrozenActorLayer" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void TheIdleStateHoldsBeforeItsOwnLaunchPick()
		{
			var idle = Between(Ground(), "class GroundUnitsIdleStateCA", "void AggressionTick(");
			var valid = idle.IndexOf("if (!owner.IsValid)\n\t\t\t\treturn;", StringComparison.Ordinal);
			var hold = idle.IndexOf("if (owner.SquadManager.ArmyIdleHolds(owner))\n\t\t\t\treturn;", StringComparison.Ordinal);
			var pick = idle.IndexOf("if (!owner.IsTargetValid && !FindNewTarget(owner, true, riskCheck: true))", StringComparison.Ordinal);
			Assert.That(valid, Is.GreaterThanOrEqualTo(0));
			Assert.That(hold, Is.GreaterThan(valid), "after the validity guard");
			Assert.That(pick, Is.GreaterThan(hold), "before the proactive pick: no launch of its own (L09 is not a launcher for a team squad)");
		}

		[Test]
		public void TheAttackMoveStateTakesALocalPickOrReportsDoneBeforeTheOlderRetarget()
		{
			var move = Between(Ground(), "class GroundUnitsAttackMoveStateCA", "class GroundUnitsAttackState ");
			var hook = move.IndexOf("if (!owner.IsTargetValid && owner.SquadManager.ArmyLocalOnly(owner) && !owner.SquadManager.ArmyLocalPick(owner))\n\t\t\t{\n\t\t\t\towner.SquadManager.ArmyNoteTargetDone(owner);\n\t\t\t\towner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);\n\t\t\t\treturn;\n\t\t\t}", StringComparison.Ordinal);
			var older = move.IndexOf("if (!owner.IsTargetValid || !CheckReachability(leader.Actor, owner.World.Map.CellContaining(owner.Target.CenterPosition)))", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThanOrEqualTo(0));
			Assert.That(older, Is.GreaterThan(hook), "the older retarget block is unchanged and comes after");

			// The older bounded and keeps-going rules are kept as they were (TeamGather02).
			Assert.That(move, Does.Contain("var bounded = owner.SquadManager.TeamLeadBoundsRetarget(owner);"));
			Assert.That(move, Does.Contain("if (!owner.SquadManager.TeamLeadGoOn(owner, leader.Actor))"));
		}

		[Test]
		public void TheAttackStateTakesALocalPickOrReportsDoneBeforeItsOwnFarPick()
		{
			var attack = Between(Ground(), "class GroundUnitsAttackState ", "class GroundUnitsSiegeRetreatStateCA");
			var local = attack.IndexOf("if (!owner.IsTargetValid && owner.SquadManager.ArmyLocalOnly(owner))", StringComparison.Ordinal);
			var pick = attack.IndexOf("if (!owner.SquadManager.ArmyLocalPick(owner))", StringComparison.Ordinal);
			var done = attack.IndexOf("owner.SquadManager.ArmyNoteTargetDone(owner);", pick, StringComparison.Ordinal);
			var idle = attack.IndexOf("owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);", done, StringComparison.Ordinal);
			var older = attack.IndexOf("if (!owner.IsTargetValid && !FindNewTarget(owner))", StringComparison.Ordinal);
			Assert.That(local, Is.GreaterThanOrEqualTo(0));
			Assert.That(pick, Is.GreaterThan(local));
			Assert.That(done, Is.GreaterThan(pick), "no local enemy: done");
			Assert.That(idle, Is.GreaterThan(done), "and idle");
			Assert.That(older, Is.GreaterThan(idle), "the older far pick (and its flee) stays after the hook");
			Assert.That(attack, Does.Contain("if (!owner.IsTargetValid && owner.SquadManager.TeamLeadKeepsGoing(owner))"));
			Assert.That(attack, Does.Contain("if (!owner.IsTargetValid && owner.SquadManager.TeamLeadBoundsRetarget(owner))"));
		}

		[Test]
		public void EveryHandOffToIdleOnALostTargetReportsDone()
		{
			// The local-only hook, the bounded retarget and the pushed blob's go-on each end in Idle on a lost target; all three report through the one row-gated call (3 per state).
			var ground = Ground();
			var move = Between(ground, "class GroundUnitsAttackMoveStateCA", "class GroundUnitsAttackState ");
			var attack = Between(ground, "class GroundUnitsAttackState ", "class GroundUnitsSiegeRetreatStateCA");
			Assert.That(System.Text.RegularExpressions.Regex.Matches(move, "ArmyNoteTargetDone\\(owner\\);").Count, Is.EqualTo(3), "AttackMove: local-only, keeps going, bounded");
			Assert.That(System.Text.RegularExpressions.Regex.Matches(attack, "ArmyNoteTargetDone\\(owner\\);").Count, Is.EqualTo(3), "Attack: bounded, keeps going, local-only");
			Assert.That(move, Does.Contain("if (!owner.SquadManager.TeamLeadGoOn(owner, leader.Actor))\n\t\t\t\t\t\t{\n\t\t\t\t\t\t\towner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);\n\t\t\t\t\t\t\towner.SquadManager.ArmyNoteTargetDone(owner);\n\t\t\t\t\t\t\treturn;"));
			Assert.That(move, Does.Contain("else if (bounded)\n\t\t\t\t\t\t{\n\t\t\t\t\t\t\towner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);\n\t\t\t\t\t\t\towner.SquadManager.ArmyNoteTargetDone(owner);\n\t\t\t\t\t\t\treturn;"), "after the unchanged hand-off line (TeamGather02 pins it)");
			Assert.That(attack, Does.Contain("owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);\n\t\t\t\towner.SquadManager.ArmyNoteTargetDone(owner);\n\t\t\t\treturn;\n\t\t\t}\n\n\t\t\t// TEAM-GATHER-02 c2/c3 (gt_bounded_retarget): the pushed blob keeps going"));
			Assert.That(attack, Does.Contain("stand idle, the team pass or the hunt via order takes over; the blob never flees.\n\t\t\t\towner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);\n\t\t\t\towner.SquadManager.ArmyNoteTargetDone(owner);\n\t\t\t\treturn;"));
		}

		[Test]
		public void ASquadInAnAttackStateIsNeverAQuietWaveSquadSoItsHooksRun()
		{
			// Reach premise (CM review): ArmyPrepare handles a quiet team squad before Update; a squad in AttackMove/Attack is not quiet, so TeamLeadPrepare returns false and it updates.
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(team, Does.Contain("if (s.Type == SquadCAType.Rush)\n\t\t\t\treturn AggTeamIsQuietState(state) && !AggTeamSquadDefends(s);"), "a Rush wave squad is a quiet one that does not defend");
			Assert.That(team, Does.Contain("if (!TeamLeadOn || !TeamLeadWaveSquad(s))\n\t\t\t\treturn false;"), "not a wave squad: TeamLeadPrepare does not take it");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.AggTeam.cs"), Does.Contain("internal static bool AggTeamIsQuietState(IState state) => state is GroundUnitsIdleStateCA;"));
			var module = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			var prepare = module.IndexOf("if (TeamLeadPrepare(s))", StringComparison.Ordinal);
			var update = module.IndexOf("s.Update();", prepare, StringComparison.Ordinal);
			Assert.That(prepare, Is.GreaterThanOrEqualTo(0));
			Assert.That(update, Is.GreaterThan(prepare), "a squad the prepare step leaves goes on to its state update");
		}

		[Test]
		public void TheFlightReportsFledAndStillDismissesItsUnits()
		{
			var flee = Between(Ground(), "class GroundUnitsFleeStateCA", "class HarasserUnitsIdleStateCA");
			var deactivate = flee.Substring(flee.IndexOf("public void Deactivate(SquadCA owner)", StringComparison.Ordinal));
			var handoff = deactivate.IndexOf("if (handoff)\n\t\t\t\treturn;", StringComparison.Ordinal);
			var note = deactivate.IndexOf("owner.SquadManager.ArmyNoteFled(owner);", StringComparison.Ordinal);
			var dismiss = deactivate.IndexOf("owner.SquadManager.DismissSquad(owner);", StringComparison.Ordinal);
			Assert.That(handoff, Is.GreaterThanOrEqualTo(0));
			Assert.That(note, Is.GreaterThan(handoff), "a rally hand-off is not a flight end");
			Assert.That(dismiss, Is.GreaterThan(note), "the dismissal (the hand-off of the units to the pool) is unchanged");
		}

		[Test]
		public void TheReportsAreDevRecordsOnly()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.StatesMicro.cs");
			var done = Between(wiring, "internal void ArmyNoteTargetDone(SquadCA squad)", "/// <summary>Q9 report to the plan (DEV record): the squad's flight");
			Assert.That(done, Does.Contain("if (ArmyStatesMicroOn && TeamLeadTeamSquad(squad) && DiagnosticWriter != null)\n\t\t\t\tDiagnosticTeamEvent(\"army_target_done\""), "row-gated, a team main squad only, writer-guarded");
			var fled = Between(wiring, "internal void ArmyNoteFled(SquadCA squad)", "\n\t}\n}");
			Assert.That(fled, Does.Contain("if (ArmyStatesMicroOn && DiagnosticWriter != null)\n\t\t\t\tDiagnosticTeamEvent(\"army_fled\""));
			foreach (var banned in new[] { "ChangeState", "TargetActor =", "DismissSquad" })
			{
				Assert.That(done, Does.Not.Contain(banned), banned);
				Assert.That(fled, Does.Not.Contain(banned), banned);
			}
		}
	}
}
