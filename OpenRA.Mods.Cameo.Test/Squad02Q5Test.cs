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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q5 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 2, "Joining"; row sq_grouped_join under sq_launch_arbiter): the pool joins the gathering squad as a batch of at
	/// least the join floor, never a unit that walks to the rally alone (X2: every 100 ticks each seat sent its new 1-3 pool units across the map, median contact 3). After the push
	/// a WAVE-MIN group forms a squad of its own (Q4 ArmyReinforce); nothing joins alone.
	/// </summary>
	[TestFixture]
	public class Squad02Q5Test
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
		public void ABatchIsReadyAtTheJoinFloorAndNeverBelowOne()
		{
			Assert.That(TeamLeadCA.BatchReady(0, 3), Is.False, "nothing to join");
			Assert.That(TeamLeadCA.BatchReady(1, 3), Is.False);
			Assert.That(TeamLeadCA.BatchReady(2, 3), Is.False);
			Assert.That(TeamLeadCA.BatchReady(3, 3), Is.True, "the floor itself is a batch");
			Assert.That(TeamLeadCA.BatchReady(7, 3), Is.True);
			Assert.That(TeamLeadCA.BatchReady(1, 0), Is.True, "a zero floor counts as 1: a floor never lets nothing through");
			Assert.That(TeamLeadCA.BatchReady(0, 0), Is.False);
			Assert.That(TeamLeadCA.BatchReady(1, -5), Is.True);

			// The all-in floor (2) and the ordinary floor (3) come from AllInCA.JoinMinUnits, the floor a new squad needs.
			Assert.That(AllInCA.JoinMinUnits(false, 3, 2), Is.EqualTo(3));
			Assert.That(AllInCA.JoinMinUnits(true, 3, 2), Is.EqualTo(2));
			Assert.That(TeamLeadCA.BatchReady(2, AllInCA.JoinMinUnits(true, 3, 2)), Is.True, "an all-in order takes a pair");
			Assert.That(TeamLeadCA.BatchReady(2, AllInCA.JoinMinUnits(false, 3, 2)), Is.False);
		}

		// X2: production adds 1-3 units to the pool at every team check (100 ticks) while a rally order is open. The old rule merged them at once (every walk was 1-3 units); the
		// batch rule merges only a pool of at least the floor, so every walk is a group and no unit is lost.
		[Test]
		public void ProductionOfOneToThreeUnitsPerCheckNeverWalksAloneAndNothingIsLost()
		{
			const int floor = 3;
			var produced = 0;
			var merged = 0;
			var pool = 0;
			var walks = new List<int>();
			for (var check = 0; check < 40; check++)
			{
				var made = check % 3 + 1;
				produced += made;
				pool += made;
				if (TeamLeadCA.BatchReady(pool, floor))
				{
					walks.Add(pool);
					merged += pool;
					pool = 0;
				}
			}

			Assert.That(walks, Is.Not.Empty);
			Assert.That(walks, Has.All.GreaterThanOrEqualTo(floor), "no walk below the join floor");
			Assert.That(merged + pool, Is.EqualTo(produced), "every produced unit is either merged or still waiting at home");
			Assert.That(pool, Is.LessThan(floor), "what waits is below the floor");

			// The old rule, for contrast: every check merged what it had (median walk 2).
			var oldWalks = new List<int>();
			for (var check = 0; check < 40; check++)
				oldWalks.Add(check % 3 + 1);
			Assert.That(oldWalks, Has.Some.LessThan(floor), "X2: the old rule sent groups of 1-2 across the map");
		}

		[Test]
		public void TheBatchFloorIsWiredInTheMergeBehindItsRowAndTheOldMergeStaysWithItOff()
		{
			var merge = Between(RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs"), "bool TeamLeadMergeIntoGatherSquad(", "// A team Rush squad: not the stalemate breaker");
			Assert.That(merge, Does.Contain("if (ArmyGroupedJoinOn && !TeamLeadCA.BatchReady(joinable.Count, AllInCA.JoinMinUnits(order.AllIn, Info.TeamLeadJoinMinUnits, Info.TeamLeadAllInJoinMinUnits)))\n\t\t\t\treturn false;"),
				"row off = the guard is skipped and every joinable unit merges as before");

			// The guard sits after the empty-pool test and before any unit is moved or ordered.
			var guard = merge.IndexOf("ArmyGroupedJoinOn", StringComparison.Ordinal);
			Assert.That(guard, Is.GreaterThan(merge.IndexOf("joinable.Count == 0", StringComparison.Ordinal)));
			Assert.That(guard, Is.LessThan(merge.IndexOf("squad.Units.Add(u);", StringComparison.Ordinal)));
			Assert.That(guard, Is.LessThan(merge.IndexOf("bot.QueueOrder(", StringComparison.Ordinal)));

			// The caller stops at the same floor, so a pool below it neither merges nor forms a one-unit squad.
			var join = Between(RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs"), "void TeamLeadJoin(IBot bot, int now)", "// Where the order goes, from the leader's own view");
			Assert.That(join, Does.Contain("if (TeamLeadMergeIntoGatherSquad(bot, order))\n\t\t\t\t\treturn;\n\n\t\t\t\tif (unitsHangingAroundTheBase.Count(TeamLeadJoinable) < AllInCA.JoinMinUnits(order.AllIn, Info.TeamLeadJoinMinUnits, Info.TeamLeadAllInJoinMinUnits))\n\t\t\t\t\treturn;"));
			Assert.That(join, Does.Contain("ArmyReinforce(bot, order, now);"), "after the push the group is a WAVE-MIN squad of its own (Q4), never a merge");
		}

		[Test]
		public void TheRowIsRegisteredBehindTheOneLauncher()
		{
			Assert.That(AblationIds.SqGroupedJoin, Is.EqualTo("sq_grouped_join"));
			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(pipe, Does.Contain("AblationLatch ablateSqGroupedJoin;"));
			Assert.That(pipe, Does.Contain("internal bool ArmyGroupedJoinOn => ArmyLaunchArbiterOn && !ablateSqGroupedJoin.Off(Player, AblationIds.SqGroupedJoin);"));
		}
	}
}
