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
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// ARTY-TARGET-01 (USER P0, Opus): artillery squads take defences and structures first, at stand-off; a unit is a target only when it threatens the artillery.
	// c2: with no structure in range the artillery assaults the nearest seen or remembered defence, else structure, near its anchor; a unit squad target only when no structure is known.
	// Owner: Code Monkey. Reviewer: Sonnet.
	[TestFixture]
	public sealed class ArtyStructFirst01Test
	{
		static readonly WPos Origin = new WPos(0, 0, 0);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void WithTheSwitchANonThreateningUnitIsNeverPicked()
		{
			var units = new[] { new ArtyCandidate(7, new WPos(1000, 0, 0), ArtyKind.Unit, false) };
			Assert.That(ArtyMicroCA.PickTarget(Origin, 2000, units), Is.EqualTo(0), "without the switch the unit is the fallback");
			Assert.That(ArtyMicroCA.PickTarget(Origin, 2000, units, true), Is.EqualTo(-1), "with the switch a non-threatening unit is not picked");
		}

		[Test]
		public void WithTheSwitchAThreateningUnitIsStillPicked()
		{
			var units = new[] { new ArtyCandidate(7, new WPos(1000, 0, 0), ArtyKind.Unit, true) };
			Assert.That(ArtyMicroCA.PickTarget(Origin, 2000, units, true), Is.EqualTo(0));
		}

		[Test]
		public void WithTheSwitchAStructureInRangeBeatsAUnit()
		{
			var list = new[]
			{
				new ArtyCandidate(7, new WPos(500, 0, 0), ArtyKind.Unit, false),
				new ArtyCandidate(9, new WPos(1500, 0, 0), ArtyKind.Structure, false),
			};
			Assert.That(ArtyMicroCA.PickTarget(Origin, 2000, list, true), Is.EqualTo(1));
		}

		[Test]
		public void TheAssaultPicksADefenceBeforeANearerStructure()
		{
			var list = new[]
			{
				new ArtyCandidate(1, new WPos(1000, 0, 0), ArtyKind.Structure, false),
				new ArtyCandidate(2, new WPos(3000, 0, 0), ArtyKind.Defence, false),
			};
			Assert.That(ArtyMicroCA.PickStructure(Origin, list), Is.EqualTo(1));
		}

		[Test]
		public void TheAssaultFallsBackToTheNearestStructureWhenNoDefenceIsKnown()
		{
			var list = new[]
			{
				new ArtyCandidate(1, new WPos(3000, 0, 0), ArtyKind.Structure, false),
				new ArtyCandidate(2, new WPos(1000, 0, 0), ArtyKind.Structure, false),
			};
			Assert.That(ArtyMicroCA.PickStructure(Origin, list), Is.EqualTo(1));
		}

		[Test]
		public void TheAssaultNeverPicksAUnitAndReportsNothingWhenOnlyUnitsAreKnown()
		{
			var list = new[]
			{
				new ArtyCandidate(1, new WPos(1000, 0, 0), ArtyKind.Unit, true),
				new ArtyCandidate(2, new WPos(500, 0, 0), ArtyKind.Unit, false),
			};
			Assert.That(ArtyMicroCA.PickStructure(Origin, list), Is.EqualTo(-1));
		}

		[Test]
		public void TheStructuralAimIsAnExplicitAttackAfterAStandOffMove()
		{
			var states = Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Contain("PickTarget(pos, rangeLen, candidates, manager.ArtyStructFirstActive)"));
			Assert.That(states, Does.Contain("if (manager.ArtyStructFirstActive && (targetIsUnit || !owner.Target.IsValidFor(unit))"));
			Assert.That(states, Does.Contain("ArtyMicroCA.PickStructure(anchor, structures)"));
			Assert.That(states, Does.Contain("ArtyMicroCA.PlanApproach(pos, targetPos, rangeLen, rangeMargin, assaultDefences, defenceMargin)"));
			Assert.That(states, Does.Contain("new Order(\"Attack\", unit, Target.FromActor(actor), false)"));
		}

		[Test]
		public void TheStructureScanIsSharedPerSquadAndSkipsWalls()
		{
			var states = Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Contain("if (!structuresGathered)"), "the scan runs at most once per squad per think");
			Assert.That(states, Does.Contain("GatherStructures(owner, anchor);"));
			Assert.That(states, Does.Not.Contain("GatherStructures(owner, unit, anchor)"));
			Assert.That(states, Does.Contain("fa.TargetTypes.Overlaps(WallTargetType)"), "remembered walls are not targets");
			Assert.That(states, Does.Contain("e.GetEnabledTargetTypes().Overlaps(WallTargetType))"), "seen walls are not targets");
		}

		[Test]
		public void TheSwitchIsAnOwnRowOnlyWhileTheMicroIsOn()
		{
			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(squad, Does.Contain("public readonly bool ArtyStructFirstEnabled = false;"));
			Assert.That(squad, Does.Contain("public readonly int ArtyAssaultScanCells = 40;"));

			var ally = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Contain("internal bool ArtyStructFirstActive => ArtyMicroActive && Info.ArtyStructFirstEnabled && !ablateArtyStruct.Off(Player, AblationIds.ArtyStructFirst);"));
		}

		[Test]
		public void TheRowIsRegisteredWithItsOwnId()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.ArtyStructFirst.cs"), Does.Contain("ArtyStructFirst = \"arty_struct_first\""));
		}

		[Test]
		public void EveryArtilleryPersonalityThatRunsTheMicroAlsoArmsTheSwitch()
		{
			var ai = Source("packaging/ai.yaml");
			var micro = Regex.Matches(ai, @"^\t\tArtyMicroEnabled: true$", RegexOptions.Multiline).Count;
			var structFirst = Regex.Matches(ai, @"^\t\tArtyStructFirstEnabled: true$", RegexOptions.Multiline).Count;
			Assert.That(micro, Is.GreaterThan(0));
			Assert.That(structFirst, Is.EqualTo(micro), "each personality with the micro also gets the switch on");
		}
	}
}
