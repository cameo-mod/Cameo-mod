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
	/// PUSHBAR-RCA-01 (row sq_guard_lend). V08D 6v6 (83a0cf03): 3 team orders, 2 cancelled for no reports after 500 ticks, 0 team_push. The seats' home guards (Protection squads)
	/// held 56 to 126 of the team's units against 3 to 16 in the pool and the quiet wave squads, so an order found at most 3 joinable pool units per seat. A quiet guard lends its
	/// spare units to the open, unpushed order, and the lendable units count in the idle army the leader's ready test measures.
	/// </summary>
	[TestFixture]
	public class Squad02GuardLendTest
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

		[Test]
		public void AGuardIsQuietOnlyWithNoTaskAtAll()
		{
			Assert.That(GuardLend.Quiet(false, false, false, false), Is.True);
			Assert.That(GuardLend.Quiet(true, false, false, false), Is.False, "a target: it is fighting or answering");
			Assert.That(GuardLend.Quiet(false, true, false, false), Is.False, "it holds a rally for a predicted attack");
			Assert.That(GuardLend.Quiet(false, false, true, false), Is.False, "a live protection request");
			Assert.That(GuardLend.Quiet(false, false, false, true), Is.False, "a perceived or predicted threat stands against the base");
			Assert.That(GuardLend.Quiet(true, true, true, true), Is.False);
		}

		[Test]
		public void TheGuardLendsItsShareInABatchAndNeverEmpties()
		{
			// The two quiet guards of V08D at tick 16250: Multi0 19 units, Multi2 14 units (all joinable, 50 percent, floor 3).
			Assert.That(GuardLend.Units(19, 19, 50, 3), Is.EqualTo(9));
			Assert.That(GuardLend.Units(14, 14, 50, 3), Is.EqualTo(7));

			// Only joinable units are lent (artillery, fire support, units away from the base stay).
			Assert.That(GuardLend.Units(19, 4, 50, 3), Is.EqualTo(4));
			Assert.That(GuardLend.Units(19, 2, 50, 3), Is.EqualTo(0), "below the join floor a squad cannot form: nothing is lent");

			// A small guard keeps its units: 4 units at 50 percent is 2, below the floor.
			Assert.That(GuardLend.Units(4, 4, 50, 3), Is.EqualTo(0));
			Assert.That(GuardLend.Units(6, 6, 50, 3), Is.EqualTo(3));

			// Rows off and bad values.
			Assert.That(GuardLend.Units(19, 19, 0, 3), Is.EqualTo(0), "0 percent = nothing lent");
			Assert.That(GuardLend.Units(0, 5, 50, 3), Is.EqualTo(0));
			Assert.That(GuardLend.Units(19, 0, 50, 3), Is.EqualTo(0));
			Assert.That(GuardLend.Units(19, 19, -10, 3), Is.EqualTo(0));
			Assert.That(GuardLend.Units(5, 5, 50, 0), Is.EqualTo(2), "a zero floor counts as 1: a floor never lets nothing through");

			// The guard keeps at least one unit, whatever the percent.
			Assert.That(GuardLend.MaxPercent, Is.EqualTo(90));
			for (var guard = 1; guard <= 60; guard++)
				for (var pct = 0; pct <= 300; pct += 10)
					Assert.That(GuardLend.Units(guard, guard, pct, 1), Is.LessThan(guard), "guard " + guard + " pct " + pct);

			Assert.That(GuardLend.Units(int.MaxValue, int.MaxValue, 90, 1), Is.GreaterThan(0), "no overflow on a huge guard");
		}

		[Test]
		public void TheRowHasItsIdItsSwitchAndItsDefault()
		{
			Assert.That(AblationIds.SqGuardLend, Is.EqualTo("sq_guard_lend"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.Squad02GuardLend.cs"), Does.Contain("public const string SqGuardLend = \"sq_guard_lend\";"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCAInfo.GuardLend.cs"), Does.Contain("public readonly int ArmyGuardLendPercent = 50;"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqGuardLend;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyGuardLendOn => ArmyLaunchArbiterOn && Info.ArmyGuardLendPercent > 0 && !ablateSqGuardLend.Off(Player, AblationIds.SqGuardLend);"),
				"behind the one launcher, off at 0 percent, off with the DEV row");

			// Classic and every block without the pipeline play as before: the Info switch is the pipeline's, never set in @classic.
			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(yaml[classic..], Does.Not.Contain("ArmyGuardLendPercent"));
		}

		[Test]
		public void OnlyAQuietUntaskedProtectionGuardLendsAndItNeverRunsAnEnemyScan()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			var pick = Between(wiring, "bool ArmyGuardPick(out SquadCA guard)", "// The member's idle army (TeamLeadBlob)");

			Assert.That(pick, Does.Contain("guard = GetSquadOfType(SquadCAType.Protection);"));
			Assert.That(pick, Does.Contain("ReferenceEquals(guard, m13ResponseSquad) || ReferenceEquals(guard, m13IntentSquad)"), "the liveness response keeps its job");
			Assert.That(pick, Does.Contain("is not (UnitsForProtectionIdleState or UnitsForProtectionAttackState)"), "a fleeing or other guard keeps every unit");
			Assert.That(pick, Does.Contain("GuardLend.Quiet(guard.IsTargetValid, TryGetProtectionRally(out _), SelectProtectionRequest() != null, threatened)"));
			Assert.That(pick, Does.Contain("threatPredictionProviders.Any(p => p.PerceivedBaseThreat || p.PredictedThreats.Count > 0)"), "the same threat test as the guard's own release");
			Assert.That(pick, Does.Contain("TeamLeadJoinable(u) && (u.Actor.Location - initialBaseCenter).LengthSquared <= baseRadius"), "ground, armed, at the base");
			Assert.That(pick, Does.Contain("GuardLend.Units(guard.Units.Count, armyGuardLend.Count, Info.ArmyGuardLendPercent, Info.TeamLeadJoinMinUnits)"));

			foreach (var banned in new[] { "FindClosestEnemy", "FindActorsInCircle", "World.Actors", "ActorsWithTrait", "FrozenActorLayer", "Random" })
				Assert.That(wiring, Does.Not.Contain(banned), "the blob count runs this on every team check: no scan, no RNG (" + banned + ")");
		}

		[Test]
		public void TheLendTakesFromTheGuardPutsInThePoolAndIsAWriterGuardedRecord()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			var lend = wiring[wiring.IndexOf("void ArmyGuardLendTo(TeamLeadOrder order)", StringComparison.Ordinal)..];

			Assert.That(lend, Does.Contain("if (!ArmyGuardLendOn || order.Pushed || !ArmyGuardPick(out var guard))\n\t\t\t\treturn;"), "an unpushed order only");
			Assert.That(lend.IndexOf("guard.Units.Remove(u);", StringComparison.Ordinal), Is.LessThan(lend.IndexOf("unitsHangingAroundTheBase.Add(u);", StringComparison.Ordinal)));
			Assert.That(lend, Does.Contain("n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);"));
			Assert.That(lend, Does.Contain("if (DiagnosticWriter != null)\n\t\t\t\tDiagnosticTeamEvent(\"army_guard_lend\""), "DEV only, no string built in a release build");
			Assert.That(lend, Does.Not.Contain("DismissSquad"), "the guard is never dissolved");
			Assert.That(lend, Does.Not.Contain("ChangeState"), "the guard's state is untouched");

			var blob = Between(wiring, "void ArmyGuardBlob(ref int units, ref long value)", "// TeamLeadJoin, for an order that has not pushed");
			Assert.That(blob, Does.Contain("if (!ArmyGuardLendOn || !ArmyGuardPick(out _))\n\t\t\t\treturn;"));
			Assert.That(blob, Does.Contain("value += UnitValue(u.Actor);"));
		}

		[Test]
		public void TheHooksSitBeforeTheBarsAndTheOlderTextIsUnchanged()
		{
			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");

			// Join: after the pool is cleaned, before the merge and the pool-size bars; an after-push (persistent) order returns before it.
			var join = Between(tl, "void TeamLeadJoin(IBot bot, int now)", "// Where the order goes, from the leader's own view");
			var reinforce = join.IndexOf("ArmyReinforce(bot, order, now);", StringComparison.Ordinal);
			var clean = join.IndexOf("unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));", StringComparison.Ordinal);
			var hook = join.IndexOf("ArmyGuardLendTo(order);", StringComparison.Ordinal);
			var merge = join.IndexOf("if (TeamLeadJoinMergeOn && (order.GatherOnly || order.Extending || order.AllIn))", StringComparison.Ordinal);
			var floor = join.IndexOf("< AllInCA.JoinMinUnits(order.AllIn, teamLeadIsLeader && !order.GatherOnly && !order.Extending ? Info.TeamLeadLeaderMinUnits : Info.TeamLeadJoinMinUnits", StringComparison.Ordinal);
			Assert.That(reinforce, Is.GreaterThanOrEqualTo(0));
			Assert.That(clean, Is.GreaterThan(reinforce));
			Assert.That(hook, Is.GreaterThan(clean));
			Assert.That(merge, Is.GreaterThan(hook));
			Assert.That(floor, Is.GreaterThan(merge));
			Assert.That(join, Does.Contain("(!AllInCA.JoinsAgain(order.AllIn, teamLeadJoinedSeq, order.Seq) && !order.GatherOnly && !order.Extending)"), "an attack order is still joined once");

			// Blob: the lendable units are added last, no scan in the blob body.
			var blob = Between(tl, "(int Units, long Value) TeamLeadBlob()", "bool TeamLeadWaveSquad(SquadCA s)");
			Assert.That(blob.IndexOf("ArmyGuardBlob(ref units, ref value);", StringComparison.Ordinal), Is.LessThan(blob.IndexOf("return (units, value);", StringComparison.Ordinal)));
			Assert.That(blob.IndexOf("ArmyGuardBlob(ref units, ref value);", StringComparison.Ordinal), Is.GreaterThan(blob.IndexOf("value += SquadValueOf(s);", StringComparison.Ordinal)));
			Assert.That(blob, Does.Not.Contain("TeamLeadRaiderInContact"));
			Assert.That(blob, Does.Not.Contain("FindClosestEnemy"));

			// The release path of the guard is untouched: the one release trigger stays ShouldReleaseDefenders.
			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(sm, Does.Contain("internal bool ShouldReleaseDefenders(bool quiet)"));
			Assert.That(sm, Does.Not.Contain("ArmyGuardLend"));
		}
	}
}
