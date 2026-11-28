#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// DEF-LANES-01 P1 (user 11-28): the per-lane attack memory, its decay, the extra budget and the coverage/lane split. Pure state, no world.
	[TestFixture]
	public class DefenseLanesTest
	{
		[Test]
		public void BucketsFollowTheBearing()
		{
			Assert.That(DefenseLanes.Bucket(10, 0, 8), Is.EqualTo(0));
			Assert.That(DefenseLanes.Bucket(10, 10, 8), Is.EqualTo(1));
			Assert.That(DefenseLanes.Bucket(0, 10, 8), Is.EqualTo(2));
			Assert.That(DefenseLanes.Bucket(-10, 0, 8), Is.EqualTo(4));
			Assert.That(DefenseLanes.Bucket(0, -10, 8), Is.EqualTo(6));
			Assert.That(DefenseLanes.Bucket(10, -1, 8), Is.EqualTo(0));
			Assert.That(DefenseLanes.Bucket(0, 0, 8), Is.EqualTo(0));
		}

		[Test]
		public void DirectionPointsBackIntoItsBucket()
		{
			for (var b = 0; b < 8; b++)
			{
				var (x, y) = DefenseLanes.Direction(b, 8, 20);
				Assert.That(DefenseLanes.Bucket(x, y, 8), Is.EqualTo(b));
			}
		}

		[Test]
		public void AttacksAndDamageRaiseTheLane()
		{
			var lanes = new DefenseLanes(8);
			Assert.That(lanes.Heaviest(), Is.EqualTo(-1));
			lanes.Hit(3, 100, 5000, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(3), Is.EqualTo(50));

			// The same attack (inside the window): no attack gain; the damage carry reaches one step.
			lanes.Hit(3, 200, 5000, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(3), Is.EqualTo(60));

			// A new attack after a quiet window.
			lanes.Hit(3, 450, 0, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(3), Is.EqualTo(110));
			lanes.Hit(5, 450, 0, 50, 10, 250, 1000);
			Assert.That(lanes.Heaviest(), Is.EqualTo(3));
			Assert.That(lanes.Total(), Is.EqualTo(160));

			// The cap.
			lanes.Hit(5, 451, 10000000, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(5), Is.EqualTo(1000));
		}

		[Test]
		public void QuietLanesDecay()
		{
			var lanes = new DefenseLanes(8);
			lanes.Hit(1, 0, 0, 1000, 0, 250, 1000);
			lanes.Decay(749, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(1000));
			lanes.Decay(750, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(900));
			lanes.Decay(2250, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(729));
			lanes.Decay(750 * 200, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(0));
			Assert.That(lanes.Heaviest(), Is.EqualTo(-1));
		}

		[Test]
		public void BudgetAndSplit()
		{
			Assert.That(DefenseLanes.ExtraPermille(0, 100, 1000), Is.EqualTo(0));
			Assert.That(DefenseLanes.ExtraPermille(50, 100, 1000), Is.EqualTo(150));
			Assert.That(DefenseLanes.ExtraPermille(5000, 100, 1000), Is.EqualTo(1000));

			// No extra budget = the factor unchanged; +100% budget halves it.
			Assert.That(DefenceLaneBudget.Apply(2000, 0), Is.EqualTo(2000));
			Assert.That(DefenceLaneBudget.Apply(2000, 1000), Is.EqualTo(1000));
			Assert.That(DefenceLaneBudget.Apply(1500, 500), Is.EqualTo(1000));

			// 50%: lane, coverage, lane, coverage.
			Assert.That(DefenseLanes.LaneTurn(0, 0, 50), Is.True);
			Assert.That(DefenseLanes.LaneTurn(1, 0, 50), Is.False);
			Assert.That(DefenseLanes.LaneTurn(1, 1, 50), Is.True);
			Assert.That(DefenseLanes.LaneTurn(0, 5, 0), Is.False);
			Assert.That(DefenseLanes.LaneTurn(5, 0, 100), Is.True);
		}

		[Test]
		public void TeamShare()
		{
			// Share 0 = own lanes only.
			Assert.That(DefenseLanes.TeamExtra(150, 800, 0, 1000), Is.EqualTo(150));
			Assert.That(DefenseLanes.TeamExtra(0, 800, 50, 1000), Is.EqualTo(400));
			Assert.That(DefenseLanes.TeamExtra(700, 800, 50, 1000), Is.EqualTo(1000));
			Assert.That(DefenseLanes.AllyLaneWins(0, 100, 0), Is.False);
			Assert.That(DefenseLanes.AllyLaneWins(0, 100, 50), Is.True);
			Assert.That(DefenseLanes.AllyLaneWins(50, 100, 50), Is.False);
			Assert.That(DefenseLanes.AllyLaneWins(49, 100, 50), Is.True);
		}

		[Test]
		public void UnseenAttackerFallsBackToTheBuildingBearing()
		{
			Assert.That(DefenseLanes.OriginBucket(true, -10, 0, 10, 0, 8), Is.EqualTo(4));
			Assert.That(DefenseLanes.OriginBucket(false, -10, 0, 10, 0, 8), Is.EqualTo(0));
			Assert.That(DefenseLanes.OriginBucket(false, -10, 0, 0, 0, 8), Is.EqualTo(-1));
			Assert.That(DefenseLanes.OriginBucket(true, 0, 0, 0, 0, 8), Is.EqualTo(0));
		}

		[Test]
		public void TheSplitIsPerEpisode()
		{
			var split = new LaneSplit();
			split.Observe(true);
			Assert.That(split.LaneTurn(50), Is.True);
			split.NotePlacement(true);
			Assert.That(split.LaneTurn(50), Is.False);
			split.NotePlacement(false);
			split.NotePlacement(false);
			split.NotePlacement(false);

			// Still the same episode: the counts stay.
			split.Observe(true);
			Assert.That(split.CoveragePlaced, Is.EqualTo(3));

			// Cold, then hot again: a new episode starts at 0 and the lane goes first.
			split.Observe(false);
			split.Observe(true);
			Assert.That(split.LanePlaced, Is.EqualTo(0));
			Assert.That(split.CoveragePlaced, Is.EqualTo(0));
			Assert.That(split.LaneTurn(50), Is.True);
		}

		[Test]
		public void TeamKeyIsTheSameForEveryMember()
		{
			Assert.That(TeamBrain.TeamKey(new[] { "Multi3", "Multi1", "Multi2" }), Is.EqualTo("Multi1"));
			Assert.That(TeamBrain.TeamKey(new[] { "Multi2", "Multi3", "Multi1" }), Is.EqualTo("Multi1"));
			Assert.That(TeamBrain.TeamKey(Array.Empty<string>()), Is.Null);
		}

		[Test]
		public void TheLaneBoardFindsTheHardestHitAlly()
		{
			var board = new TeamLaneBoard();
			var a = new DefenseLanes(8);
			var b = new DefenseLanes(8);
			var c = new DefenseLanes(8);
			board.Join("A", a, 10, 10, 100, 750);
			board.Join("B", b, 50, 10, 100, 750);
			board.Join("C", c, 90, 10, 100, 750);
			Assert.That(board.HeaviestAlly("A", out _, out _, out _), Is.False);
			Assert.That(board.MaxAllyTotal("A"), Is.EqualTo(0));

			a.Hit(1, 0, 0, 900, 0, 250, 1000);
			b.Hit(2, 0, 0, 300, 0, 250, 1000);
			c.Hit(5, 0, 0, 400, 0, 250, 1000);
			c.Hit(6, 0, 0, 100, 0, 250, 1000);

			// A never reads itself; C holds the heaviest ally lane and the largest ally total.
			Assert.That(board.HeaviestAlly("A", out var member, out var lane, out var weight), Is.True);
			Assert.That(member.Key, Is.EqualTo("C"));
			Assert.That(member.HomeX, Is.EqualTo(90));
			Assert.That(lane, Is.EqualTo(5));
			Assert.That(weight, Is.EqualTo(400));
			Assert.That(board.MaxAllyTotal("A"), Is.EqualTo(500));
			Assert.That(board.HeaviestAlly("C", out member, out _, out weight), Is.True);
			Assert.That(member.Key, Is.EqualTo("A"));
			Assert.That(weight, Is.EqualTo(900));

			// Every member decays by its own rate; a member that left is not read.
			board.Decay(750);
			Assert.That(c.Weight(5), Is.EqualTo(360));
			board.Leave("C");
			Assert.That(board.HeaviestAlly("A", out member, out _, out _), Is.True);
			Assert.That(member.Key, Is.EqualTo("B"));

			// Joining again replaces the entry in place.
			board.Join("B", new DefenseLanes(8), 50, 10, 100, 750);
			Assert.That(board.Members.Count, Is.EqualTo(2));
			Assert.That(board.HeaviestAlly("A", out _, out _, out _), Is.False);
		}

		[Test]
		public void TheBoardFoldsInKeyOrder()
		{
			// The same weights joined in two orders: the tie goes to the lowest key both times.
			foreach (var order in new[] { new[] { "C", "B", "A" }, new[] { "A", "C", "B" } })
			{
				var board = new TeamLaneBoard();
				foreach (var key in order)
				{
					var lanes = new DefenseLanes(8);
					lanes.Hit(3, 0, 0, 200, 0, 250, 1000);
					board.Join(key, lanes, 0, 0, 100, 750);
				}

				Assert.That(string.Join(",", System.Linq.Enumerable.Select(board.Members, m => m.Key)), Is.EqualTo("A,B,C"));
				Assert.That(board.HeaviestAlly("A", out var member, out _, out _), Is.True);
				Assert.That(member.Key, Is.EqualTo("B"));
			}
		}

		[Test]
		public void TheBrainSharesOneBoardPerTeam()
		{
			var world = (World)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(World));
			var red = TeamBrain.For(world, "Multi0");
			Assert.That(TeamBrain.For(world, "Multi0"), Is.SameAs(red));
			Assert.That(red.Board<TeamLaneBoard>(), Is.SameAs(TeamBrain.For(world, "Multi0").Board<TeamLaneBoard>()));
			Assert.That(TeamBrain.For(world, "Multi3").Board<TeamLaneBoard>(), Is.Not.SameAs(red.Board<TeamLaneBoard>()));
			Assert.That(red.Key, Is.EqualTo("Multi0"));
		}

		[Test]
		public void SaveRoundTripsAndDamagedTextRestoresNothing()
		{
			var lanes = new DefenseLanes(8);
			var split = new LaneSplit();
			split.Observe(true);
			lanes.Hit(3, 100, 5000, 50, 10, 250, 1000);
			lanes.Hit(6, 120, 25000, 50, 10, 250, 1000);
			split.NotePlacement(true);
			split.NotePlacement(false);
			var text = SavedLanes.Encode(lanes.Save(split));

			var loaded = new DefenseLanes(8);
			var loadedSplit = new LaneSplit();
			Assert.That(loaded.Restore(SavedLanes.Decode(text), loadedSplit), Is.True);
			for (var i = 0; i < 8; i++)
				Assert.That(loaded.Weight(i), Is.EqualTo(lanes.Weight(i)));

			Assert.That(loadedSplit.Hot, Is.True);
			Assert.That(loadedSplit.LanePlaced, Is.EqualTo(1));
			Assert.That(loadedSplit.CoveragePlaced, Is.EqualTo(1));

			// The restored memory continues exactly like the original: the attack window, the damage carry and the decay clock are all kept.
			lanes.Hit(3, 200, 5000, 50, 10, 250, 1000);
			loaded.Hit(3, 200, 5000, 50, 10, 250, 1000);
			lanes.Decay(1500, 100, 750);
			loaded.Decay(1500, 100, 750);
			for (var i = 0; i < 8; i++)
				Assert.That(loaded.Weight(i), Is.EqualTo(lanes.Weight(i)));

			Assert.That(SavedLanes.Decode(null), Is.Null);
			Assert.That(SavedLanes.Decode("not base64 !"), Is.Null);
			Assert.That(SavedLanes.Decode(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{broken"))), Is.Null);
			Assert.That(SavedLanes.Decode(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"Version\":99}"))), Is.Null);

			// Another bucket count restores nothing.
			var other = new DefenseLanes(12);
			Assert.That(other.Restore(SavedLanes.Decode(text), new LaneSplit()), Is.False);
			Assert.That(other.Total(), Is.EqualTo(0));
		}

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		// Sonnet note (c): off = no lanes. The code defaults are off, every entry point returns on the off check before it touches lane state,
		// the only arm is the genericbot planner block in ai.yaml, and the base builder leaves its factor alone at extra 0.
		[Test]
		public void OffMeansNoLanes()
		{
			var info = new DefenseCoveragePlannerInfo();
			Assert.That(info.LaneDefences, Is.False);
			Assert.That(info.LaneAllySharePercent, Is.EqualTo(0));

			var src = RepoFile("OpenRA.Mods.Cameo/Traits/BotModules/DefenseCoveragePlanner.Lanes.cs");
			Assert.That(Count(src, "if (!Info.LaneDefences || !LanesArmed)"), Is.EqualTo(3));
			Assert.That(Count(src, "new DefenseLanes("), Is.EqualTo(1));
			Assert.That(Count(src, "laneBoard.Join("), Is.EqualTo(1));
			Assert.That(src, Does.Contain("if (lanes == null || !LanesArmed)\n\t\t\t\treturn null;"));
			Assert.That(src, Does.Contain("if (world.IsReplay || !LanesArmed || "));
			foreach (var entry in new[] { "void IBotRespondToAttack.RespondToAttack(", "int IBotDefenceLaneBudget.ExtraDefencePermille", "int PickLane(" })
			{
				var at = src.IndexOf(entry, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThanOrEqualTo(0), entry);
				var guard = src.IndexOf("if (!Info.LaneDefences || !LanesArmed)", at, StringComparison.Ordinal);
				var firstState = src.IndexOf("RefreshLaneKnobs();", at, StringComparison.Ordinal);
				Assert.That(guard, Is.GreaterThan(at).And.LessThan(firstState), entry);
			}

			Assert.That(src, Does.Contain("bool LanesArmed => Info.LaneDefences && Info.Enabled && !IsTraitDisabled && !ablateLanes.Off(player, AblationIds.DefLanes);"));
			Assert.That(src, Does.Contain("bool LaneTeamArmed => laneAllyShare > 0 && !ablateLaneTeam.Off(player, AblationIds.DefLanesTeam);"));
			Assert.That(AblationIds.DefLanes, Is.EqualTo("def_lanes"));
			Assert.That(AblationIds.DefLanesTeam, Is.EqualTo("def_lanes_team"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			Assert.That(Count(yaml, "LaneDefences: true"), Is.EqualTo(1));
			Assert.That(Count(yaml, "LaneAllySharePercent:"), Is.EqualTo(1));
			var block = yaml.IndexOf("\tDefenseCoveragePlanner:\n\t\tEnabled: true\n\t\tRequiresCondition: genericbot\n", StringComparison.Ordinal);
			Assert.That(block, Is.GreaterThanOrEqualTo(0));
			var next = yaml.IndexOf("\n\tBaseFrontBackPlannerBotModule:", block, StringComparison.Ordinal);
			var arm = yaml.IndexOf("LaneDefences: true", StringComparison.Ordinal);
			Assert.That(arm, Is.GreaterThan(block).And.LessThan(next));

			for (var factor = 750; factor <= 3000; factor += 250)
				Assert.That(DefenceLaneBudget.Apply(factor, 0), Is.EqualTo(factor));
		}
	}
}
