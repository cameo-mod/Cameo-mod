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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-01 c7 (CONSOLIDATE_01_DESIGN.md section 4 row C7): the yaml arm of the base-build pipeline. BaseBuilderBotModuleCA@generic
	// arms the pipeline and its stage switches plus the defence cap moved from ArmyFirstBotModule@af02; @classic arms none; the af02 grant
	// is empty; every arm has its ablation row.
	[TestFixture]
	public class Consolidate01C7Test
	{
		static readonly string[] Arms = { "BbPipeline01", "BbAntiRushSet", "BbPassiveIncome", "BbExtraDefence", "CsCaptureUnlock" };

		static readonly (string Key, string Value)[] CapValues =
		{
			("DefenceCapBase", "3"), ("DefenceCapPerThreat", "6"), ("DefenceThreatSaturationPercent", "300"),
			("TurtleDefenceCapPercent", "200"), ("TurtleDefenceUntilTick", "27000"),
		};

		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
			"tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		static List<MiniYamlNode> TopLevel() => MiniYaml.FromString(Fixture("mcv_deploy_ai.yaml"), "ai").ToList();

		static MiniYamlNode PlayerNode(string key) =>
			TopLevel().Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		[Test]
		public void TheGenericBaseBuilderArmsThePipelineAndEveryStageSwitch()
		{
			var generic = PlayerNode("BaseBuilderBotModuleCA@generic");
			Assert.That(FieldOrNull(generic, "RequiresCondition"), Is.EqualTo("genericbot"));
			Assert.That(FieldOrNull(generic, "SwitchCondition"), Is.EqualTo("genericbot"));
			foreach (var arm in Arms)
				Assert.That(FieldOrNull(generic, arm), Is.EqualTo("true"), arm);
		}

		[Test]
		public void TheDefenceCapMovesFromTheAf02VoteIntoThePipelineWithTheSameValues()
		{
			var generic = PlayerNode("BaseBuilderBotModuleCA@generic");
			var af02 = PlayerNode("ArmyFirstBotModule@af02");
			foreach (var (key, value) in CapValues)
			{
				Assert.That(FieldOrNull(generic, key), Is.EqualTo(value), key);
				Assert.That(FieldOrNull(af02, key), Is.EqualTo(value), "the same values as the retired af02 cap: " + key);
			}
		}

		[Test]
		public void TheClassicBaseBuilderArmsNothing()
		{
			var classic = PlayerNode("BaseBuilderBotModuleCA@classic");
			Assert.That(FieldOrNull(classic, "RequiresCondition"), Is.EqualTo("classicbot"));
			foreach (var arm in Arms)
				Assert.That(FieldOrNull(classic, arm), Is.Null, arm);
			foreach (var (key, _) in CapValues)
				Assert.That(FieldOrNull(classic, key), Is.Null, key);
		}

		[Test]
		public void TheAf02GrantIsEmptySoAf02IsInertAndTheBaseModuleStillServesFransbot()
		{
			var difficulties = TopLevel().Single(n => n.Key == "^AIDifficulties").Value.Nodes;
			var grant = difficulties.Single(n => n.Key == "GrantConditionOnBotOwner@armyfirst02");
			Assert.That(FieldOrNull(grant, "Bots") ?? "", Is.Empty);
			Assert.That(FieldOrNull(PlayerNode("ArmyFirstBotModule"), "RequiresCondition"), Is.EqualTo("genericbot && army_first"));
		}

		[Test]
		public void NoContentPackArmsAPipelineSwitch()
		{
			var packs = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "source", "packs"), "ai.yaml", SearchOption.AllDirectories);
			Assert.That(packs, Is.Not.Empty);
			foreach (var pack in packs)
			{
				var text = File.ReadAllText(pack);
				foreach (var arm in Arms)
					Assert.That(text, Does.Not.Contain(arm), pack + ": " + arm);
			}
		}

		[Test]
		public void EveryArmHasItsAblationRow()
		{
			var rows = AblationSpec.RegistryIds();
			foreach (var row in new[] { AblationIds.BbPipeline01, AblationIds.BbAntiRushSet, AblationIds.BbPassiveIncome, AblationIds.BbExtraDefence,
				AblationIds.CsCaptureUnlock, AblationIds.Af2DefenceCap })
				Assert.That(rows, Does.Contain(row), row);
		}
	}
}
