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
	// TURRET-NOHOLD-01 (row turret_no_hold, USER 11-24 turret rule): a READY ground turret is never held. With no legal cell inside its weapon range it takes the legal cell of the attacked
	// building's cluster nearest the attackers (never closer than the safe gap), and with none the normal own-base placement runs. Off = the old hold, text pinned. The world is not driven here
	// (no placement fixture exists); the pure picker is tested and the wiring is source-pinned.
	[TestFixture]
	public sealed class TurretNoHold01Test
	{
		const long Cell = 1024;
		const long Range = 6 * Cell;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// Whitespace-flattened text: the pins read statements, not the indentation.
		static string Flat(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

		static List<(long X, long Y)> Row(params int[] xs) => xs.Select(x => (x * Cell, 0L)).ToList();

		static long MinSafe => TurretSupportCA.MinSafe(Range, 4 * Cell);

		[Test]
		public void TheIdealCellIsBlockedThePickHoldsAndTheFallbackTakesTheNearestUsableCell()
		{
			// Attackers at x = 20; the legal cells of the attacked building's cluster all stand 12 or more cells away: out of range, so Pick holds.
			var legal = Row(0, 2, 4, 6, 8);
			Assert.That(TurretSupportCA.Pick(legal, (20 * Cell, 0), (0, 0), Range, MinSafe), Is.EqualTo(-1), "the old rule holds");

			var i = TurretSupportCA.FallbackPick(legal, (20 * Cell, 0), (0, 0), MinSafe);
			Assert.That(legal[i].X, Is.EqualTo(8 * Cell), "the cell nearest the attackers: no wait");
		}

		[Test]
		public void SoloOneVersusOneTheOwnBaseClusterIsUsedWhenNothingIsInRange()
		{
			// Own yard at x = 0 attacked from the east at x = 30 by artillery out of every legal cell's range.
			var legal = Row(-3, -2, -1, 0, 1, 2, 3);
			Assert.That(TurretSupportCA.Pick(legal, (30 * Cell, 0), (0, 0), Range, MinSafe), Is.EqualTo(-1));
			Assert.That(legal[TurretSupportCA.FallbackPick(legal, (30 * Cell, 0), (0, 0), MinSafe)].X, Is.EqualTo(3 * Cell), "the most forward own cell");
		}

		[Test]
		public void AlliedPlacementOffLeavesOnlyOwnCellsAndTheFallbackStillPicksOneOfThem()
		{
			// The lobby option is off: the engine placement check leaves only own cells, far from the attacked ally at x = 40.
			var ownOnly = Row(0, 5, 10, 15);
			Assert.That(TurretSupportCA.Pick(ownOnly, (46 * Cell, 0), (40 * Cell, 0), Range, MinSafe), Is.EqualTo(-1));
			Assert.That(ownOnly[TurretSupportCA.FallbackPick(ownOnly, (46 * Cell, 0), (40 * Cell, 0), MinSafe)].X, Is.EqualTo(15 * Cell));

			// And with no legal cell at all the picker says -1, so the normal own-base placement runs (no hold, no fabricated cell).
			Assert.That(TurretSupportCA.FallbackPick(new List<(long X, long Y)>(), (46 * Cell, 0), (40 * Cell, 0), MinSafe), Is.EqualTo(-1));
		}

		[Test]
		public void TheFallbackNeverStandsNextToTheEnemy()
		{
			// Every legal cell is inside the safe gap (< 3.6 cells here: MinSafe = min(4 cells, 3.6 cells)): nothing is usable.
			Assert.That(MinSafe, Is.EqualTo(3600L * Cell / 1000));
			Assert.That(TurretSupportCA.FallbackPick(Row(9, 10, 11), (10 * Cell, 0), (0, 0), MinSafe), Is.EqualTo(-1));

			// One cell outside the gap is taken.
			var legal = Row(5, 6, 9, 10, 11);
			Assert.That(legal[TurretSupportCA.FallbackPick(legal, (10 * Cell, 0), (0, 0), MinSafe)].X, Is.EqualTo(6 * Cell), "the nearest cell that keeps the gap");
		}

		[Test]
		public void TiesGoToTheCellNearerTheAnchorThenTheLowerIndex()
		{
			// Two cells equally far from the attackers (north and south of the row); the one nearer the anchor wins.
			var legal = new List<(long X, long Y)> { (0, 10 * Cell), (0, -10 * Cell), (0, -10 * Cell) };
			Assert.That(TurretSupportCA.FallbackPick(legal, (0, 0), (0, -12 * Cell), MinSafe), Is.EqualTo(1), "nearer the anchor; of two equal cells the lower index");
		}

		// Sol FIX 923e10bb: a cell reserved by a refinery search (PlacementMasked) never wins an armed support search. The support cell skips the normal placement, so a masked winner used to
		// be nulled at the final mask check with no second try.
		static Func<int, bool> Masked(params int[] indices)
		{
			var set = new HashSet<int>(indices);
			return set.Contains;
		}

		[Test]
		public void TheBestCellIsMaskedAndTheNextUsableCellWins()
		{
			var legal = Row(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
			var attackers = (10 * Cell, 0L);
			Assert.That(TurretSupportCA.PickUnmasked(legal, Masked(), attackers, (0, 0), Range, MinSafe), Is.EqualTo(4), "nothing masked = the plain Pick (x = 4, exactly the range)");
			Assert.That(TurretSupportCA.PickUnmasked(legal, Masked(4), attackers, (0, 0), Range, MinSafe), Is.EqualTo(5), "the best is masked: the next usable cell, as an ORIGINAL index");
			Assert.That(TurretSupportCA.PickUnmasked(legal, Masked(4, 5), attackers, (0, 0), Range, MinSafe), Is.EqualTo(6));

			// The fallback cluster: the same rule.
			var cluster = Row(0, 2, 4, 6, 8);
			var far = (20 * Cell, 0L);
			Assert.That(TurretSupportCA.FallbackPickUnmasked(cluster, Masked(), far, (0, 0), MinSafe), Is.EqualTo(4));
			Assert.That(TurretSupportCA.FallbackPickUnmasked(cluster, Masked(4), far, (0, 0), MinSafe), Is.EqualTo(3));
		}

		[Test]
		public void EveryCellMaskedFallsBackToTheNormalPlacement()
		{
			var legal = Row(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
			Assert.That(TurretSupportCA.PickUnmasked(legal, _ => true, (10 * Cell, 0), (0, 0), Range, MinSafe), Is.EqualTo(-1), "no in-range cell left: the armed caller goes on to the fallback cluster");
			Assert.That(TurretSupportCA.FallbackPickUnmasked(legal, _ => true, (10 * Cell, 0), (0, 0), MinSafe), Is.EqualTo(-1), "nothing left there either: (null, false) = the normal own-base placement, no hold");
			Assert.That(TurretSupportCA.FallbackPickUnmasked(new List<(long X, long Y)>(), _ => false, (10 * Cell, 0), (0, 0), MinSafe), Is.EqualTo(-1));
		}

		[Test]
		public void AStableReservationGivesTheSamePickEveryTimeAndLiftingItRestoresTheBest()
		{
			var legal = Row(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
			var attackers = (10 * Cell, 0L);
			for (var i = 0; i < 5; i++)
				Assert.That(TurretSupportCA.PickUnmasked(legal, Masked(4), attackers, (0, 0), Range, MinSafe), Is.EqualTo(5), "the same reservation, the same cell on every pass");

			// A reservation on a cell that would not have won changes nothing.
			Assert.That(TurretSupportCA.PickUnmasked(legal, Masked(0, 1, 12), attackers, (0, 0), Range, MinSafe), Is.EqualTo(4));

			// The mask is read in the ORIGINAL numbering however many cells before it are masked.
			var seen = new List<int>();
			TurretSupportCA.PickUnmasked(legal, i => { seen.Add(i); return i == 4; }, attackers, (0, 0), Range, MinSafe);
			Assert.That(seen, Is.EqualTo(Enumerable.Range(0, legal.Count).ToList()), "each cell asked once, in order");

			// The reservation ends: the best cell is back.
			Assert.That(TurretSupportCA.PickUnmasked(legal, Masked(), attackers, (0, 0), Range, MinSafe), Is.EqualTo(4));
		}

		[Test]
		public void TheOldHoldTextIsKeptWhenTheRowIsOffAndTheFallbackNeverHolds()
		{
			var q = Flat(Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs"));
			var chooser = q.Substring(q.IndexOf("(CPos? Cell, bool Hold) ChooseSupportTurretCell(", StringComparison.Ordinal));
			chooser = chooser.Substring(0, chooser.IndexOf("// TURRET-NOHOLD-01: a READY ground turret skips", StringComparison.Ordinal));

			Assert.That(chooser, Does.Contain("if (index >= 0) return (legal[index], false); if (!TurretNoHoldOn) return (null, true);"), "row off = the old hold");
			Assert.That(chooser, Does.Contain("var fallback = TurretSupportCA.FallbackPickUnmasked(clusterPositions, i => baseBuilder.PlacementMasked(actorInfo.Name, cluster[i]), (focus.Attackers.X, focus.Attackers.Y), (focus.Anchor.X, focus.Anchor.Y), minSafe);"));
			Assert.That(chooser, Does.Contain("var index = TurretNoHoldOn ? TurretSupportCA.PickUnmasked(positions, i => baseBuilder.PlacementMasked(actorInfo.Name, legal[i]), (focus.Attackers.X, focus.Attackers.Y), (focus.Anchor.X, focus.Anchor.Y), range, minSafe) : TurretSupportCA.Pick(positions, (focus.Attackers.X, focus.Attackers.Y), (focus.Anchor.X, focus.Anchor.Y), range, minSafe);"),
				"armed: masked cells never win; off: the plain Pick");
			Assert.That(chooser, Does.Contain("return (fallback >= 0 ? (CPos?)cluster[fallback] : null, false);"), "row on = a cell or the normal placement, never a hold");
			Assert.That(chooser, Does.Contain("AdvisorLegalCells(actorInfo, true, producer, world.Map.CellContaining(focus.Anchor))"), "the same engine placement check around the attacked building");
			Assert.That(Count(chooser, ", true);"), Is.EqualTo(1), "exactly one hold return (the row-off one)");

			// The READY watchdog call sites keep their text: the watchdog stays the backstop for the row-off path.
			Assert.That(q, Does.Contain("if (supportHold && ReadyWatchdogOn && !BaseFix02CA.HoldExpired("));
			Assert.That(q, Does.Contain("if (frontBackHold && !(ReadyWatchdogOn && BaseFix02CA.HoldExpired("));
		}

		[Test]
		public void TheOutpostAdvisorHoldIsSkippedOnlyForAReadyGroundTurretWhenTheRowIsOn()
		{
			var q = Flat(Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs"));
			Assert.That(q, Does.Contain("bool TurretNoHoldSkipsHold(ActorInfo actorInfo) => TurretNoHoldOn && actorInfo.HasTraitInfo<AttackBaseInfo>() && IsGroundDefence(actorInfo);"));
			Assert.That(q, Does.Contain("if (pick.Hold && !TurretNoHoldSkipsHold(actorInfo)) { frontBackHold = true;"), "the outpost defence site");
			Assert.That(q, Does.Contain("if (pick.Hold) { frontBackHold = true;"), "the radar/production/valuable site keeps the plain hold");
			Assert.That(q, Does.Contain("AblationLatch ablateTurretNoHold;"));
			Assert.That(q, Does.Contain("bool TurretNoHoldOn => baseBuilder.TurretNoHoldArmed && !ablateTurretNoHold.Off(player, AblationIds.TurretNoHold);"));
		}

		[Test]
		public void TheRowIsRegisteredOffInCodeAndInYaml()
		{
			Assert.That(AblationIds.TurretNoHold, Is.EqualTo("turret_no_hold"));
			var module = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(module, Does.Contain("public readonly bool TurretNoHold = false;"));
			Assert.That(module, Does.Contain("public bool TurretNoHoldArmed => Info.TurretNoHold && switchesActive;"));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "TurretNoHold").Value.Value, Is.EqualTo("true"), "armed by the playtest B2 ARM (generic only)");
			foreach (var o in player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key == "TurretNoHold"), Is.False, o.Key);
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;
	}
}
