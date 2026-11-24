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
	// Owner: Code Monkey. Reviewer: Sonnet.
	[TestFixture]
	public sealed class ArtyStructFirst01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[TestCase(true, true, false)]
		[TestCase(true, false, true)]
		[TestCase(false, true, true)]
		[TestCase(false, false, true)]
		public void ASquadTargetThatIsAUnitIsRefusedOnlyWhenTheSwitchIsOn(bool structFirst, bool targetIsUnit, bool usable) =>
			Assert.That(ArtyMicroCA.SquadTargetUsable(structFirst, targetIsUnit), Is.EqualTo(usable));

		[Test]
		public void TheStepFourGuardRefusesAUnitSquadTargetAndHoldsAtTheAnchor()
		{
			var states = Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Contain("var targetIsUnit = owner.Target.Type == TargetType.Actor && owner.Target.Actor != null && !owner.Target.Actor.Info.HasTraitInfo<BuildingInfo>();"));
			Assert.That(states, Does.Contain("if (!ArtyMicroCA.SquadTargetUsable(manager.ArtyStructFirstActive, targetIsUnit))"));
			Assert.That(states, Does.Contain("if ((anchor - pos).HorizontalLengthSquared > (long)rangeMargin * rangeMargin && manager.TryConsumeMicroActions())\n\t\t\t\t\t\towner.Bot.QueueOrder(new Order(\"Move\", unit, Target.FromPos(anchor), false));"),
				"a unit squad target holds: a Move to the anchor, never an Attack or an approach");
		}

		[Test]
		public void TheSwitchIsAnOwnRowOnlyWhileTheMicroIsOn()
		{
			var squad = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(squad, Does.Contain("public readonly bool ArtyStructFirstEnabled = false;"));

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
