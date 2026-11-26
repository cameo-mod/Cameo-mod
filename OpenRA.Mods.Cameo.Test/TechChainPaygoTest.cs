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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// TECHCHAIN-PAYGO-01 (switch bb_techchain_paygo, Opus ruling A 2026-11-25): test A6 of the staged planner is waived for an S3/S5 tech-chain start
	// with the bank above 0 and no tech-chain item in flight. Every other start keeps the full-cost test; the switch off keeps A6 unchanged.
	// The pure rule (TechChainPaygo) is tested directly, the world wiring by pins, the arm by the yaml.
	[TestFixture]
	public sealed class TechChainPaygoTest
	{
		const string PipelineFile = "mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		[Test]
		public void AnS3OrS5TechChainStartRidesWithAnyBankAboveZero()
		{
			foreach (var stage in new[] { BaseBuildStage.S3Opening, BaseBuildStage.S5CoreOnce })
			{
				Assert.That(TechChainPaygo.Waives(stage, 1, 0), Is.True, stage.ToString());
				Assert.That(TechChainPaygo.Waives(stage, 312, 0), Is.True, stage.ToString());
			}
		}

		[Test]
		public void AnEmptyOrNegativeBankOrATechChainItemInFlightKeepsTheFullCostTest()
		{
			foreach (var stage in new[] { BaseBuildStage.S3Opening, BaseBuildStage.S5CoreOnce })
			{
				Assert.That(TechChainPaygo.Waives(stage, 0, 0), Is.False, stage + " bank 0");
				Assert.That(TechChainPaygo.Waives(stage, -50, 0), Is.False, stage + " bank < 0");
				Assert.That(TechChainPaygo.Waives(stage, 5000, 1), Is.False, stage + " one tech-chain item in flight");
			}
		}

		[Test]
		public void EveryOtherStageKeepsTheFullCostTest()
		{
			foreach (var stage in Enum.GetValues<BaseBuildStage>())
			{
				if (stage == BaseBuildStage.S3Opening || stage == BaseBuildStage.S5CoreOnce)
					continue;

				Assert.That(TechChainPaygo.StageWaives(stage), Is.False, stage.ToString());
				Assert.That(TechChainPaygo.Waives(stage, 5000, 0), Is.False, stage.ToString());
			}
		}

		[Test]
		public void TheHoldRowIsRateLimitedPerNameTo750Ticks()
		{
			Assert.That(TechChainPaygo.HoldLogIntervalTicks, Is.EqualTo(750));
			Assert.That(TechChainPaygo.HoldLogDue(-1, 0), Is.True, "first row");
			Assert.That(TechChainPaygo.HoldLogDue(100, 849), Is.False);
			Assert.That(TechChainPaygo.HoldLogDue(100, 850), Is.True);
		}

		[Test]
		public void TheWaiverIsOnlyReachedWhenTheFullCostTestFailsForAnArmedTechChainStart()
		{
			var src = Source(PipelineFile);
			Assert.That(src, Does.Contain("if (!CashLedger.AdmitBuilding(s.Bank, startCost, CashLedger.BuildingReserve(c.Stage, s.ProductionCashReserve)).Ok)"), "A6 is still the first test (P2 (a): the ledger admission = Affordable)");
			Assert.That(src, Does.Contain("var techChainStart = baseBuilder.Info.TechChainTypes.Contains(start.Name);"));
			// BUILD-ALL-B: the listed first-copy waiver (bb_build_all_listed) is tried first; with it off the paygo test is the old one.
			Assert.That(src, Does.Contain("if (!listedWaived && (!techChainStart || !(baseBuilder.TechChainPaygoArmed && TechChainPaygo.Waives(c.Stage, s.Bank, TechChainInFlight()))))"),
				"switch off or a non-tech start = the old A6 refusal");
			Assert.That(src, Does.Contain("if (techChainStart || c.Stage == BaseBuildStage.S5CoreOnce)\n\t\t\t\t\t\tTechChainA6HoldRecord(start.Name, c.Stage, s.Bank, startCost);\n\n\t\t\t\t\treturn false;"));
		}

		[Test]
		public void TheHoldRowIsDevOnly()
		{
			var src = Source(PipelineFile);
			var record = src.IndexOf("void TechChainA6HoldRecord(", StringComparison.Ordinal);
			Assert.That(record, Is.GreaterThan(0));
			Assert.That(src.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", record, StringComparison.Ordinal), Is.InRange(record - 80, record - 1));
			Assert.That(src, Does.Contain("pipeline_a6_hold name={1} stage={2} bank={3} cost={4}"));
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegistered()
		{
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "BbTechChainPaygo"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "BbTechChainPaygo"), Is.Null);
			Assert.That(AblationIds.BbTechChainPaygo, Is.EqualTo("bb_techchain_paygo"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbTechChainPaygo));

			var packs = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "source", "packs"), "ai.yaml", SearchOption.AllDirectories);
			Assert.That(packs, Is.Not.Empty);
			foreach (var pack in packs)
				Assert.That(File.ReadAllText(pack), Does.Not.Contain("BbTechChainPaygo"), pack);
		}

		[Test]
		public void TheSwitchIsOffByDefault()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.BbTechChainPaygo, Is.False);
		}
	}
}
