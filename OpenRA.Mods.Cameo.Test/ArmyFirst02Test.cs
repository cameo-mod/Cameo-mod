#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// ARMY-FIRST-02 (USER P0 11-23, Opus): the tier bots' army-first voter as the count-based redesign of the user rule "each
	// available building at least once, then army", the threat-scaled defence cap, and the repair cash floor (USER rule 11-23:
	// repair always on unless cash + resources < 1000, one toggle per state change). Pure decisions plus yaml mount pins.
	[TestFixture]
	public class ArmyFirst02Test
	{
		static readonly string[] Tiers =
		{
			"noob", "easiest", "veryeasy", "easy", "medium", "hard", "veryhard", "brutal", "challenger", "unbeatable", "cameogod",
		};

		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
			"tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string WithoutDevRegions(string text) => Regex.Replace(text, @"#if CAMEO_DEV_LOGGING.*?#endif", string.Empty, RegexOptions.Singleline);

		static List<MiniYamlNode> TopLevel() => MiniYaml.FromString(Fixture("mcv_deploy_ai.yaml"), "ai").ToList();

		static MiniYamlNode PlayerNode(string key) =>
			TopLevel().Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string Field(MiniYamlNode node, string key) => node.Value.Nodes.Single(n => n.Key == key).Value.Value.Trim();

		[Test]
		public void FirstCopyIsOnlyTheTypeWithNothingOwnedQueuedOrAdmitted()
		{
			Assert.That(ArmyFirstEval.IsFirstCopy(0, false), Is.True);
			Assert.That(ArmyFirstEval.IsFirstCopy(1, false), Is.False, "one owned or queued copy makes the next an extra");
			Assert.That(ArmyFirstEval.IsFirstCopy(0, true), Is.False, "an admitted order in flight is the first copy already");
		}

		[Test]
		public void DefenceIsAnArmedBuilding()
		{
			Assert.That(ArmyFirstEval.IsDefence(true, true), Is.True);
			Assert.That(ArmyFirstEval.IsDefence(true, false), Is.False, "walls and unarmed support are not defences");
			Assert.That(ArmyFirstEval.IsDefence(false, true), Is.False, "a mobile unit is army, not a defence");
		}

		[Test]
		public void DefenceCapScalesWithThreatAndTheTurtleBonus()
		{
			Assert.That(ArmyFirstEval.DefenceCap(-1, 6, 1.0, false, 200), Is.EqualTo(-1), "negative base = no cap");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 0.0, false, 200), Is.EqualTo(3), "quiet");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 0.5, false, 200), Is.EqualTo(6));
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 0.49, false, 200), Is.EqualTo(5), "rounded down");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 1.0, false, 200), Is.EqualTo(9), "full threat");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 7.0, false, 200), Is.EqualTo(9), "intensity clamps to 1");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, -1.0, false, 200), Is.EqualTo(3), "intensity clamps to 0");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 0.0, true, 200), Is.EqualTo(6), "turtle early-mid: 2x");
			Assert.That(ArmyFirstEval.DefenceCap(3, 6, 1.0, true, 200), Is.EqualTo(18));
			Assert.That(ArmyFirstEval.DefenceCap(3, -4, 1.0, false, 100), Is.EqualTo(3), "a negative per-threat term adds nothing");
		}

		[Test]
		public void CapHoldsAtOrOverTheCapOnly()
		{
			Assert.That(ArmyFirstEval.DefenceCapHolds(3, 2), Is.False);
			Assert.That(ArmyFirstEval.DefenceCapHolds(3, 3), Is.True);
			Assert.That(ArmyFirstEval.DefenceCapHolds(3, 9), Is.True);
			Assert.That(ArmyFirstEval.DefenceCapHolds(-1, 99), Is.False, "no cap never holds");
			Assert.That(ArmyFirstEval.DefenceCapHolds(0, 0), Is.True, "cap 0 holds every extra defence");
		}

		[Test]
		public void DecisionOrderFirstCopyThenCapThenVotes()
		{
			// The first copy passes even where the cap or the votes would hold (the core rule: never held by army preference).
			Assert.That(ArmyFirstEval.Af02Holds(true, true, true, true, true, true), Is.False);
			Assert.That(ArmyFirstEval.Af02Holds(true, true, false, false, false, true), Is.False);

			// An extra defence follows the cap, not the stock votes: under the cap it passes although the votes would hold.
			Assert.That(ArmyFirstEval.Af02Holds(true, false, true, true, false, true), Is.False);
			Assert.That(ArmyFirstEval.Af02Holds(true, false, true, true, true, false), Is.True);

			// Any other extra goes to the votes.
			Assert.That(ArmyFirstEval.Af02Holds(true, false, true, false, false, true), Is.True);
			Assert.That(ArmyFirstEval.Af02Holds(true, false, true, false, false, false), Is.False);

			// Rows off: first copy off -> a first copy is voted; cap off -> a defence is voted.
			Assert.That(ArmyFirstEval.Af02Holds(false, true, false, false, false, true), Is.True);
			Assert.That(ArmyFirstEval.Af02Holds(false, false, false, true, true, true), Is.True);
			Assert.That(ArmyFirstEval.Af02Holds(false, false, false, true, true, false), Is.False);
		}

		[Test]
		public void RepairIsOffBelowTheFloorOnlyAndTogglesOnce()
		{
			Assert.That(BaseRepairBotModule.BelowRepairFloor(1000, 999), Is.True);
			Assert.That(BaseRepairBotModule.BelowRepairFloor(1000, 1000), Is.False, "at the floor repair is on");
			Assert.That(BaseRepairBotModule.BelowRepairFloor(0, 0), Is.False, "floor 0 = no floor");

			Assert.That(BaseRepairBotModule.SuspendRepair(true, true, false), Is.True, "a running repair gets one switch-off");
			Assert.That(BaseRepairBotModule.SuspendRepair(true, true, true), Is.False, "never a second toggle while one is in flight");
			Assert.That(BaseRepairBotModule.SuspendRepair(true, false, false), Is.False, "nothing to switch off");
			Assert.That(BaseRepairBotModule.SuspendRepair(false, true, false), Is.False, "above the floor nothing is switched off");
		}

		[Test]
		public void TheTierVoterIsItsOwnInstanceOnItsOwnSwitch()
		{
			var module = PlayerNode("ArmyFirstBotModule@af02");
			Assert.That(Field(module, "RequiresCondition"), Is.EqualTo("genericbot && army_first_02"));
			Assert.That(Field(module, "ArmyReserveLowCash"), Is.EqualTo("0"));
			Assert.That(Field(module, "ArmyReserveHighCash"), Is.EqualTo("0"));
			Assert.That(Field(module, "MinArmyUnitsBeforeBuildings"), Is.EqualTo("0"));
			Assert.That(Field(module, "FirstCopyPasses"), Is.EqualTo("true"));
			Assert.That(Field(module, "DefenceCapBase"), Is.EqualTo("3"));
			Assert.That(Field(module, "DefenceCapPerThreat"), Is.EqualTo("6"));
			Assert.That(Field(module, "TurtleDefenceCapPercent"), Is.EqualTo("200"));
			Assert.That(Field(module, "TurtleDefenceUntilTick"), Is.EqualTo("27000"));
			Assert.That(Field(module, "AblationRow"), Is.EqualTo(OpenRA.Mods.CA.Traits.AblationIds.Af2ArmyFirst));

			// The DEFENCE-ZERO-FIX instance is unchanged (fransbot gate).
			Assert.That(Field(PlayerNode("ArmyFirstBotModule"), "RequiresCondition"), Is.EqualTo("genericbot && army_first"));
		}

		[Test]
		public void TheSwitchGrantIsEmptiedByConsolidate01AndNothingElseGrantsIt()
		{
			var difficulties = TopLevel().Single(n => n.Key == "^AIDifficulties").Value.Nodes;
			var grant = difficulties.Single(n => n.Key == "GrantConditionOnBotOwner@armyfirst02");
			Assert.That(Field(grant, "Condition"), Is.EqualTo("army_first_02"));
			// CONSOLIDATE-01 c7: the grant is emptied; the base-build pipeline replaced the af02 vote for the tiers (Consolidate01C7Test).
			Assert.That(grant.Value.Nodes.Single(n => n.Key == "Bots").Value.Value?.Trim() ?? "", Is.Empty, "no tier bot carries the af02 vote");
			Assert.That(difficulties.Count(n => n.Value.Nodes.Any(c => c.Key == "Condition" && c.Value.Value.Trim() == "army_first_02")),
				Is.EqualTo(1), "one grant: classic and the exploit personalities never get it");
		}

		[Test]
		public void TheSwitchListIsExactlyTheGenericbotLadder()
		{
			// Sonnet LOW-4: a typo would leave a tier silently without the voter. The switch list must equal the union of every
			// genericbot grant (the base tier grant plus the noob grant).
			var generic = TopLevel().Single(n => n.Key == "^AIDifficulties").Value.Nodes
				.Where(n => n.Value.Nodes.Any(c => c.Key == "Condition" && c.Value.Value.Trim() == "genericbot"))
				.SelectMany(n => Field(n, "Bots").Split(',').Select(x => x.Trim())).ToHashSet();
			Assert.That(generic, Is.EquivalentTo(Tiers));
		}

		[Test]
		public void RepairFloorIsOneThousandOnTheTierRepairOwner()
		{
			var repair = PlayerNode("BaseRepairBotModule");
			Assert.That(Field(repair, "RequiresCondition"), Is.EqualTo("genericbot"));
			Assert.That(Field(repair, "RepairMinCash"), Is.EqualTo("1000"));
		}

		[Test]
		public void NewLoggingIsDevOnlyAndTheVoterSendsNoOrders()
		{
			var module = Source("mods/Cameo/Traits/BotModules/ArmyFirstBotModule.cs");
			Assert.That(module, Does.Contain("AF2 {kind} item="));
			Assert.That(WithoutDevRegions(module), Does.Not.Contain("Log.Write"));
			Assert.That(WithoutDevRegions(module), Does.Not.Contain("CountAf2("), "the call site is inside the DEV region too");
			Assert.That(module, Does.Not.Contain("QueueOrder"), "a voter only answers; it never issues an order");
			Assert.That(module, Does.Not.Contain("CancelProduction"));
			Assert.That(module, Does.Not.Contain("[Sync]"));
			Assert.That(module, Does.Contain("ablateFirstCopy.Off(player, AblationIds.Af2FirstCopy)"));
			Assert.That(module, Does.Contain("ablateDefenceCap.Off(player, AblationIds.Af2DefenceCap)"));

			var repair = Source("mods/Cameo/Traits/BotModules/BaseRepairBotModule.cs");
			Assert.That(repair, Does.Contain("AF2 repair_off"));
			Assert.That(WithoutDevRegions(repair), Does.Not.Contain("Log.Write"));
			Assert.That(repair, Does.Contain("ablateRepairCash.Off(player, AblationIds.Af2RepairCash)"));
		}
	}
}
