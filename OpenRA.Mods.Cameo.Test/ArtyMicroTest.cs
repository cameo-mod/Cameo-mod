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
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>ARTY-01 (USER P0): the pure artillery decisions - target priority, stand-off, defence avoidance, kiting.</summary>
	[TestFixture]
	public class ArtyMicroTest
	{
		const int Cell = 1024;

		static WPos P(int cellsX, int cellsY = 0) => new(cellsX * Cell, cellsY * Cell, 0);

		static ArtyCandidate C(uint id, ArtyKind kind, int cellsX, bool threatens = false, int cellsY = 0) =>
			new(id, P(cellsX, cellsY), kind, threatens);

		static List<ArtyCandidate> L(params ArtyCandidate[] c) => [.. c];

		[Test]
		public void DefenceBeatsStructureBeatsUnitEvenWhenFarther()
		{
			var list = L(C(1, ArtyKind.Unit, 3), C(2, ArtyKind.Structure, 8), C(3, ArtyKind.Defence, 14));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, list), Is.EqualTo(2), "the defence, though it is the farthest in range");

			list.RemoveAt(2);
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, list), Is.EqualTo(1), "then the structure over the nearer unit");
		}

		[Test]
		public void UnitOnlyWhenNoStructureInRangeOrItThreatens()
		{
			var list = L(C(1, ArtyKind.Structure, 20), C(2, ArtyKind.Unit, 5));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, list), Is.EqualTo(1), "the structure is out of range, so the unit is the target");

			var inRange = L(C(1, ArtyKind.Structure, 10), C(2, ArtyKind.Unit, 5));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, inRange), Is.EqualTo(0), "a structure in range outranks a calm unit");

			var threat = L(C(1, ArtyKind.Structure, 10), C(2, ArtyKind.Unit, 5, threatens: true));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, threat), Is.EqualTo(1), "a threatening unit is shot first");
		}

		[Test]
		public void NothingInRangeIsMinusOneAndTheRangeEdgeIsInclusive()
		{
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, L(C(1, ArtyKind.Defence, 16))), Is.EqualTo(-1));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, L(C(1, ArtyKind.Defence, 15))), Is.EqualTo(0));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, L()), Is.EqualTo(-1));
		}

		[Test]
		public void TiesBreakOnKindThenDistanceThenId()
		{
			// Same kind: nearer wins, then the lower id.
			var near = L(C(9, ArtyKind.Defence, 12), C(4, ArtyKind.Defence, 6));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, near), Is.EqualTo(1));

			var same = L(C(9, ArtyKind.Structure, 7, cellsY: 0), C(4, ArtyKind.Structure, 7, cellsY: 0));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, same), Is.EqualTo(1), "equal distance: lower id");

			// Order of the input list does not matter.
			var reversed = L(C(4, ArtyKind.Defence, 6), C(9, ArtyKind.Defence, 12));
			Assert.That(ArtyMicroCA.PickTarget(P(0), 15 * Cell, reversed), Is.EqualTo(0));
		}

		[Test]
		public void FiringPointIsRangeMinusMarginFromTheTargetOnTheLineToTheUnit()
		{
			var point = ArtyMicroCA.FiringPoint(P(30), P(0), 15 * Cell, Cell);
			Assert.That(point, Is.Not.Null);
			Assert.That(point.Value, Is.EqualTo(P(14)));

			var diagonal = ArtyMicroCA.FiringPoint(P(30, 40), P(0), 25 * Cell, Cell);
			// (30, 40) is the 3-4-5 bearing: 24 cells out is (14.4, 19.2) cells.
			Assert.That(diagonal.Value.X, Is.InRange((int)(14.4 * Cell) - 2, (int)(14.4 * Cell) + 2));
			Assert.That(diagonal.Value.Y, Is.InRange((int)(19.2 * Cell) - 2, (int)(19.2 * Cell) + 2));
		}

		[Test]
		public void FiringPointIsNullWhenAlreadyInRangeOrTheRangeIsTooSmall()
		{
			Assert.That(ArtyMicroCA.FiringPoint(P(15), P(0), 15 * Cell, Cell), Is.Null, "inside range: hold and fire");
			Assert.That(ArtyMicroCA.FiringPoint(P(5), P(0), 15 * Cell, Cell), Is.Null);
			Assert.That(ArtyMicroCA.FiringPoint(P(30), P(0), Cell, Cell), Is.Null, "no room to stand off");
		}

		[Test]
		public void ApproachMovesToTheFiringPointWhenNoDefenceCoversIt()
		{
			var plan = ArtyMicroCA.PlanApproach(P(30), P(0), 15 * Cell, Cell, [], 2 * Cell);
			Assert.That(plan.Kind, Is.EqualTo(ArtyApproachKind.Move));
			Assert.That(plan.Point, Is.EqualTo(P(14)));
			Assert.That(plan.Against, Is.EqualTo(P(0)));

			var inRange = ArtyMicroCA.PlanApproach(P(10), P(0), 15 * Cell, Cell, [], 2 * Cell);
			Assert.That(inRange.Kind, Is.EqualTo(ArtyApproachKind.Hold));
		}

		[Test]
		public void ApproachNeverWalksIntoADefencesReach()
		{
			// The target's own firing point (14 cells out) sits 4 cells from a short-range defence, inside its reach (7 + 2).
			var defence = new ArtyDefence(P(10), 7 * Cell);
			var plan = ArtyMicroCA.PlanApproach(P(30), P(0), 15 * Cell, Cell, [defence], 2 * Cell);
			Assert.That(plan.Kind, Is.EqualTo(ArtyApproachKind.Move));
			Assert.That(plan.Against, Is.EqualTo(P(10)), "the covering defence becomes the approach target");
			Assert.That(plan.Point, Is.EqualTo(P(24)), "14 cells from the defence, outside its reach");
			Assert.That(ArtyMicroCA.CoveringDefence(plan.Point, [defence], 2 * Cell), Is.EqualTo(-1));
		}

		[Test]
		public void ApproachHoldsWhenEveryFiringPointIsCoveredByADefenceItCannotOutrange()
		{
			// A defence that reaches 20 cells covers a firing point 14 cells from itself, whichever way it is approached.
			var plan = ArtyMicroCA.PlanApproach(P(30), P(0), 15 * Cell, Cell, [new ArtyDefence(P(10), 20 * Cell)], 2 * Cell);
			Assert.That(plan.Kind, Is.EqualTo(ArtyApproachKind.Blocked));
		}

		[Test]
		public void DefenceBackoffReturnsToMaximumRangeOnlyWhenInsideTheReach()
		{
			var defence = new ArtyDefence(P(0), 8 * Cell);
			Assert.That(ArtyMicroCA.DefenceBackoff(P(5), defence, 15 * Cell, Cell, 2 * Cell), Is.EqualTo(P(14)));
			Assert.That(ArtyMicroCA.DefenceBackoff(P(12), defence, 15 * Cell, Cell, 2 * Cell), Is.Null, "outside reach + margin");
			Assert.That(ArtyMicroCA.DefenceBackoff(P(5), defence, 10 * Cell, Cell, 2 * Cell), Is.Null, "cannot outrange it: no backoff");
			Assert.That(ArtyMicroCA.DefenceBackoff(P(0), defence, 15 * Cell, Cell, 2 * Cell), Is.Null, "coincident positions: no direction");
		}

		[Test]
		public void KiteOnlyWhenOutrangingAndStandsOffAtTheThreatsReachPlusMargin()
		{
			Assert.That(ArtyMicroCA.KiteStandoff(15 * Cell, 8 * Cell, 2 * Cell), Is.EqualTo(10 * Cell));
			Assert.That(ArtyMicroCA.KiteStandoff(8 * Cell, 8 * Cell, 2 * Cell), Is.Null);
			Assert.That(ArtyMicroCA.KiteStandoff(7 * Cell, 8 * Cell, 2 * Cell), Is.Null);
		}

		[Test]
		public void KitePointRunsDirectlyAwayAndCapsTheStep()
		{
			Assert.That(ArtyMicroCA.KitePoint(P(3), P(0), 10 * Cell, 6 * Cell), Is.EqualTo(P(9)), "capped at one 6-cell step");
			Assert.That(ArtyMicroCA.KitePoint(P(3), P(0), 10 * Cell, 20 * Cell), Is.EqualTo(P(10)), "stops at the stand-off");
			Assert.That(ArtyMicroCA.KitePoint(P(11), P(0), 10 * Cell, 6 * Cell), Is.Null, "already beyond it");
			Assert.That(ArtyMicroCA.KitePoint(P(3), P(3), 10 * Cell, 6 * Cell), Is.Null, "no direction");
			Assert.That(ArtyMicroCA.KitePoint(P(0, 3), P(0), 10 * Cell, 6 * Cell), Is.EqualTo(P(0, 9)), "works on the other axis too");
		}

		// --- wiring pins: the switch defaults, the six personalities and the Classic instance --------------------------------------

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		[Test]
		public void BothSwitchesDefaultOffAndAreOnInSixPersonalitiesNotClassic()
		{
			var info = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(info, Does.Contain("public readonly bool ArtyMicroEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly bool DefendAllyEnabled = false;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			var personalities = yaml[..classic];
			Assert.That(CountOf(personalities, "\t\tArtyMicroEnabled: true"), Is.EqualTo(6));
			Assert.That(CountOf(personalities, "\t\tDefendAllyEnabled: true"), Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("ArtyMicroEnabled"), "@classic keeps the defaults");
			Assert.That(yaml[classic..], Does.Not.Contain("DefendAllyEnabled"));
		}

		[Test]
		public void TheArtilleryStateKeepsTheLegacyLoopBehindTheSwitchAndTheRowIsRegistered()
		{
			var state = RepoFile("OpenRA.Mods.CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");
			Assert.That(state, Does.Contain("if (owner.SquadManager.ArtyMicroActive)"));
			Assert.That(state, Does.Contain("LegacyUnitOrder(owner, u, anchor);"));

			var manager = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(manager, Does.Contain("Info.ArtyMicroEnabled && !ablateArty.Off(Player, AblationIds.ArtyMicro)"));
			Assert.That(manager, Does.Contain("Info.DefendAllyEnabled && !ablateDefendAlly.Off(Player, AblationIds.DefendAlly)"));
			Assert.That(AblationIds.ArtyMicro, Is.EqualTo("ay_arty_micro"));
			Assert.That(AblationIds.DefendAlly, Is.EqualTo("da_defend_ally"));
		}

		static int CountOf(string text, string needle)
		{
			var count = 0;
			var at = 0;
			while ((at = text.IndexOf(needle, at, StringComparison.Ordinal)) >= 0)
			{
				count++;
				at += needle.Length;
			}

			return count;
		}
	}
}
