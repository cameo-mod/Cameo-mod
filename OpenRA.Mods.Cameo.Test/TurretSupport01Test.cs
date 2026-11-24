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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// TEAM-DEFEND-02 turrets (USER 11-24 via Opus): a ready turret is placed at its maximum weapon range from the current attackers, on the defended side, never next to the enemy.
	// It works SOLO (1v1, own base attacked: the primary case) and in TEAM (an ally's base attacked). Same placement code for both.
	[TestFixture]
	public sealed class TurretSupport01Test
	{
		const long Cell = 1024;
		const long Range = 6 * Cell;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// Legal cells on one row, x in cells.
		static List<(long X, long Y)> Row(params int[] xs) => xs.Select(x => (x * Cell, 0L)).ToList();

		static long MinSafe => TurretSupportCA.MinSafe(Range, 4 * Cell);

		[Test]
		public void SoloOwnBaseAttackedTheTurretStandsAtExactlyMaxRangeOnTheOwnSide()
		{
			// Own building at x = 0, the attackers at x = 10 (east). Every cell of the row from 0 to 14 is legal.
			var legal = Row(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
			var i = TurretSupportCA.Pick(legal, (10 * Cell, 0), (0, 0), Range, MinSafe);
			Assert.That(legal[i].X, Is.EqualTo(4 * Cell), "10 - 4 = 6 cells = exactly the range, on the defended side of the attackers");
		}

		[Test]
		public void TheTurretNeverStandsNextToTheEnemyOrOutOfRange()
		{
			var legal = Row(9, 10, 11); // 1 cell from the attackers: inside the safety gap
			Assert.That(TurretSupportCA.Pick(legal, (10 * Cell, 0), (0, 0), Range, MinSafe), Is.EqualTo(-1), "next to the enemy: hold");
			Assert.That(TurretSupportCA.Pick(Row(0, 1, 2, 3), (10 * Cell, 0), (0, 0), Range, MinSafe), Is.EqualTo(-1), "every legal cell is out of range: hold");
		}

		[Test]
		public void TeamAllyBaseAttackedWithBuildingNextToAnAllyOnTheTurretGoesToTheAllyFront()
		{
			// Own yard far west, the allied building at x = 30, the attackers at x = 36. Cells around the ally are legal (the lobby option is on).
			var legal = Row(0, 5, 10, 20, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34);
			var i = TurretSupportCA.Pick(legal, (36 * Cell, 0), (30 * Cell, 0), Range, MinSafe);
			Assert.That(legal[i].X, Is.EqualTo(30 * Cell), "at the ally, exactly the range from the attackers");
		}

		[Test]
		public void TeamWithBuildingNextToAnAllyOffOnlyOwnCellsAreLegalAndTheBestOfThemIsTaken()
		{
			// The option is off: legality (the engine placement check) leaves only own cells, here ending at x = 31 because the ally front is 5 cells from the own yard.
			var legal = Row(0, 5, 10, 20, 28, 29, 30, 31);
			var i = TurretSupportCA.Pick(legal, (36 * Cell, 0), (30 * Cell, 0), Range, MinSafe);
			Assert.That(legal[i].X, Is.EqualTo(30 * Cell), "the nearest legal cell toward the ally that is in range");

			var farLegal = Row(0, 5, 10, 20);
			Assert.That(TurretSupportCA.Pick(farLegal, (36 * Cell, 0), (30 * Cell, 0), Range, MinSafe), Is.EqualTo(-1), "no legal cell in range: hold");
		}

		[Test]
		public void WhenNoDefendedSideCellIsUsableAnyInRangeCellIsTheFallback()
		{
			// The only legal cell lies beyond the anchor from the attackers' side view (farther from the anchor than from the attackers).
			var legal = Row(14);
			Assert.That(TurretSupportCA.Pick(legal, (10 * Cell, 0), (0, 0), Range, MinSafe), Is.EqualTo(0));
		}

		[Test]
		public void TheDefendedSideBeatsACellCloserToExactlyTheRangeOnTheEnemySide()
		{
			// Attackers at x = 10, anchor at x = 0; a cell at x = 16 is exactly 6 from the attackers but on the enemy side; x = 4 also 6 on the defended side. Equal distance: the defended side wins pass 1.
			var legal = Row(16, 4);
			Assert.That(legal[TurretSupportCA.Pick(legal, (10 * Cell, 0), (0, 0), Range, MinSafe)].X, Is.EqualTo(4 * Cell));
		}

		[Test]
		public void TheFocusIsTheBuildingWithTheMostAttackersAndALoneScoutDoesNotCount()
		{
			Assert.That(TurretSupportCA.Focus([(7u, 1)], 2), Is.EqualTo(-1), "a lone scout");
			Assert.That(TurretSupportCA.Focus([(7u, 2), (3u, 5), (9u, 5)], 2), Is.EqualTo(1), "most attackers, the lower id on a tie");
			Assert.That(TurretSupportCA.Focus([], 2), Is.EqualTo(-1));
		}

		[Test]
		public void TheCentroidIsTheMeanPosition() =>
			Assert.That(TurretSupportCA.Centroid([(0, 0), (1000, 2000), (2000, 4000)]), Is.EqualTo((1000L, 2000L)));

		[Test]
		public void TheHookRunsBeforeTheNormalPlacementAndUsesTheEnginePlacementCheck()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			var hook = q.IndexOf("ChooseSupportTurretCell(actorInfo, queue.Actor)", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThan(0));
			Assert.That(hook, Is.LessThan(q.IndexOf("(location, baseCenterKeepsFailing, actorVariant) = ChooseBuildLocation(currentBuilding.Item", StringComparison.Ordinal)));
			Assert.That(q, Does.Contain("if (supportCell != null)\n\t\t\t\t\t\tlocation = supportCell;\n\t\t\t\t\telse if (advisedDefense != null)"));

			// a hold keeps the READY item queued, bounded by the READY watchdog (the turret is then placed at a base cell)
			Assert.That(q, Does.Contain("if (supportHold && !(ReadyWatchdogOn && BaseFix02CA.HoldExpired("));

			// legality is the engine's own check, which already honours the lobby build-next-to-an-ally option; the attack picture counts own AND allied buildings (solo = own only)
			var chooser = q.Substring(q.IndexOf("(CPos? Cell, bool Hold) ChooseSupportTurretCell(", StringComparison.Ordinal));
			Assert.That(chooser, Does.Contain("AdvisorLegalCells(actorInfo, true, producer, world.Map.CellContaining(focus.Attackers))"), "the legal-cell gate");
			var legalCells = q.Substring(q.IndexOf("List<CPos> AdvisorLegalCells(", StringComparison.Ordinal));
			Assert.That(legalCells, Does.Contain("bi.IsCloseEnoughToBase(world, player, actorInfo, producer, cell)"), "the engine near-base rule honours the lobby ally option");
			Assert.That(q, Does.Contain("a.Owner == player || player.RelationshipWith(a.Owner) == PlayerRelationship.Ally"));
			Assert.That(q, Does.Contain("player.RelationshipWith(enemy.Owner) != PlayerRelationship.Enemy"));
			Assert.That(q, Does.Contain("!enemy.CanBeViewedByPlayer(player)"), "observation only");
		}

		[Test]
		public void OnlyGenericbotArmsTheRowAndTheRowIsRegistered()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.EarlyDef.cs"), Does.Contain("\"tm_ally_turret_support\""));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "TeamTurretSupport").Value.Value, Is.EqualTo("true"));
			foreach (var o in player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "TeamTurretSupport"), Is.False, o.Key);
		}
	}
}
