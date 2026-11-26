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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q8b (row sq_hunt_follow under sq_pipeline_02; Opus 2026-11-25): a pushed order that hunts moves on to the team's next hunt cell once the old one is explored, instead of
	/// idling on it until the order runs out. The rule is pure (TeamHuntCell.Follow); the wiring is pinned.
	/// </summary>
	[TestFixture]
	public class Squad02Q8bTest
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

		[Test]
		public void TheOrderMovesOnWhenTheTeamCellIsAnotherCell()
		{
			Assert.That(TeamHuntCell.Follow(false, 1000, 1150, 150), Is.True, "another cell, the minimum passed");
			Assert.That(TeamHuntCell.Follow(true, 1000, 5000, 150), Is.False, "the same cell: the old one is not explored yet");
		}

		[Test]
		public void TheMoveWaitsTheMinimumAfterThePushOrTheLastMove()
		{
			Assert.That(TeamHuntCell.Follow(false, 1000, 1149, 150), Is.False);
			Assert.That(TeamHuntCell.Follow(false, 1000, 1150, 150), Is.True);
			Assert.That(TeamHuntCell.Follow(false, 1000, 1000, 0), Is.True, "a minimum of 0 = at once");
			Assert.That(TeamHuntCell.Follow(false, 1000, 1000, -5), Is.True, "never negative");
		}

		[Test]
		public void ABadClockNeverMovesTheOrder()
		{
			Assert.That(TeamHuntCell.Follow(false, -1, 5000, 150), Is.False, "never pushed");
			Assert.That(TeamHuntCell.Follow(false, 6000, 5000, 150), Is.False, "a tick in the future");
		}

		[Test]
		public void ThePureRulesStayWorldFree()
		{
			var text = RepoFile(Bots + "Squads/TeamHuntCell.cs");
			foreach (var banned in new[] { "Random", "World", "Actor ", "CPos", "DateTime" })
				Assert.That(text, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void TheHuntFollowHooksInBeforeTheUnchangedHuntToHuntReturn()
		{
			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			var retarget = Between(pipe, "void ArmyRetargetEngaged(int now)", "void ArmyNoteStalemate(");
			var follow = retarget.IndexOf("if (!gone && targetId == 0 && ArmyHuntFollowMoves(order, target, now))\n\t\t\t\treturn;", StringComparison.Ordinal);
			var pinned = retarget.IndexOf("if ((!gone && targetId == 0) || (targetId != 0 && targetId == order.TargetActorId))\n\t\t\t\treturn;\n\n\t\t\torder.Retarget(target, targetId);", StringComparison.Ordinal);
			Assert.That(follow, Is.GreaterThan(retarget.IndexOf("if (!picked)", StringComparison.Ordinal)), "after the pick, so a hunt that finds nothing stays as before");
			Assert.That(pinned, Is.GreaterThan(follow), "the old hunt-to-hunt return and the actor retarget are unchanged and come after");
			Assert.That(retarget, Does.Contain("var gone = ArmyOrderTargetGone(order);"));
			Assert.That(retarget, Does.Contain("if (!gone && order.TargetActorId != 0)\n\t\t\t\treturn;"), "an order with a target actor never follows a cell");
		}

		[Test]
		public void TheRowNeedsTheTeamCellAndThePersistentOrder()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.HuntFollow.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqHuntFollow;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyHuntFollowOn => ArmyHuntOneCellOn && ArmyOrderEngagedOn && !ablateSqHuntFollow.Off(Player, AblationIds.SqHuntFollow);"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.Squad02HuntFollow.cs"), Does.Contain("public const string SqHuntFollow = \"sq_hunt_follow\";"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCAInfo.HuntFollow.cs"), Does.Contain("public readonly int ArmyHuntFollowMinTicks = 150;"));
		}

		[Test]
		public void TheMoveRetargetsTheOrderToTheCellAndRemembersTheTick()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.HuntFollow.cs");
			var moves = Between(wiring, "bool ArmyHuntFollowMoves(TeamLeadOrder order, WPos target, int now)", "\n\t}\n}");
			Assert.That(moves, Does.Contain("if (!ArmyHuntFollowOn || order.TargetActorId != 0)"));
			Assert.That(moves, Does.Contain("TeamHuntCell.Follow(cell == World.Map.CellContaining(order.Target), System.Math.Max(order.PushTick, order.HuntFollowTick), now, Info.ArmyHuntFollowMinTicks)"));
			Assert.That(moves.IndexOf("order.Retarget(target, 0);", StringComparison.Ordinal), Is.GreaterThan(moves.IndexOf("TeamHuntCell.Follow(", StringComparison.Ordinal)));
			Assert.That(moves, Does.Contain("order.HuntFollowTick = now;"));
			Assert.That(moves, Does.Contain("DiagnosticTeamEvent(\"team_hunt_follow\""));
			foreach (var banned in new[] { "FindActors", "FindClosestEnemy", "FrozenActorLayer", "World.Actors", "ActorsWithTrait", "CanBeViewedByPlayer", "Random" })
				Assert.That(wiring, Does.Not.Contain(banned), banned);

			Assert.That(RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs"), Does.Contain("public int HuntFollowTick = -1;"));
		}
	}
}
