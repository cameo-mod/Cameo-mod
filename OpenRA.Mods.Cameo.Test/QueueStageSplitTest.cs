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
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// QUEUE-SPLIT-01 (row bb_queue_split) and REF-S2-01 (row bb_refinery_s2), Opus rulings 2026-11-25 after the V08C RCA: a defence-only stage never returns
	// early on the building queue, the defence queue never runs the refinery stages, and unmet refinery zone demand is served at S2.
	[TestFixture]
	public sealed class QueueStageSplitTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		sealed class AllFeasible : IBaseBuildFeasibility
		{
			public bool StageAdmits(BaseBuildCandidate candidate) => true;

			public bool Feasible(BaseBuildCandidate candidate, out string admitted)
			{
				admitted = candidate.Name;
				return true;
			}

			public bool TryClaim(BaseBuildCandidate candidate, string admitted) => true;
		}

		// A ground defence is offered at S4 and a refinery at S7; nothing else is open.
		static BaseBuildSnapshot Snap(bool split, bool defenceQueue) => new()
		{
			Bank = 5000,
			Owned = new Dictionary<string, int>(),
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			DefenceQueue = defenceQueue,
			QueueSplit = split,
			StageSource = stage => stage == BaseBuildStage.S4AntiRush ? new[] { "pillbox" } : stage == BaseBuildStage.S7RefineryPerField ? new[] { "proc" } : null,
		};

		static string Pick(BaseBuildSnapshot s) => BaseBuildPlanCA.Plan(s, new AllFeasible(), new MersenneTwister(1)).Name;

		[Test]
		public void ADefenceQueuePickNeverBlocksABuildingQueuePick()
		{
			Assert.That(Pick(Snap(false, false)), Is.EqualTo("pillbox"), "no split: S4 returns first, as before (the V08C starvation)");
			Assert.That(Pick(Snap(true, false)), Is.EqualTo("proc"), "split: the building queue skips S4 and reaches the refinery");
			Assert.That(Pick(Snap(true, true)), Is.EqualTo("pillbox"), "split: the defence queue keeps its defence stage");
		}

		[Test]
		public void TheDefenceQueueNeverRunsTheRefineryStages()
		{
			var s = Snap(true, true);
			s.StageSource = stage => stage == BaseBuildStage.S7RefineryPerField || stage == BaseBuildStage.S2ExpansionRefinery ? new[] { "proc" } : null;
			Assert.That(BaseBuildPlanCA.Plan(s, new AllFeasible(), new MersenneTwister(1)).IsNone, Is.True);
		}

		[Test]
		public void TheStageMatrix()
		{
			foreach (var stage in Enum.GetValues<BaseBuildStage>())
				Assert.That(QueueStageSplit.Allowed(stage, false, true) && QueueStageSplit.Allowed(stage, false, false), Is.True, "no split = every stage, " + stage);

			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S4AntiRush, true, false), Is.False);
			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S9ExtraDefence, true, false), Is.False);
			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S5CoreOnce, true, false), Is.True);
			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S2ExpansionRefinery, true, true), Is.False);
			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S7RefineryPerField, true, true), Is.False);
			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S6PassiveIncome, true, true), Is.False);
			Assert.That(QueueStageSplit.Allowed(BaseBuildStage.S10Fractions, true, true), Is.True, "the defence queue rolls its listed defences");
		}

		[Test]
		public void ZoneDemandIsServedAtS2BeforeTheOpeningAndAntiRushStages()
		{
			Assert.That(QueueStageSplit.ZoneDemandAtS2(true, true, false, 0), Is.True);
			Assert.That(QueueStageSplit.ZoneDemandAtS2(false, true, false, 0), Is.False, "switch off");
			Assert.That(QueueStageSplit.ZoneDemandAtS2(true, false, false, 0), Is.False, "REF-DEMAND not active");
			Assert.That(QueueStageSplit.ZoneDemandAtS2(true, true, true, 0), Is.False, "never on the defence queue");
			Assert.That(QueueStageSplit.ZoneDemandAtS2(true, true, false, 1), Is.False, "one refinery in flight per player");

			var order = BaseBuildPlanCA.Stages.ToList();
			Assert.That(order.IndexOf(BaseBuildStage.S2ExpansionRefinery), Is.LessThan(order.IndexOf(BaseBuildStage.S3Opening)));
			Assert.That(order.IndexOf(BaseBuildStage.S2ExpansionRefinery), Is.LessThan(order.IndexOf(BaseBuildStage.S4AntiRush)));

			// The ref1 -> war factory -> ref2 order and the interval stay inside the wanted-refinery helper the S2 branch calls.
			var stages = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Stages.cs");
			Assert.That(stages, Does.Contain("else if (ZoneDemandAtS2(law))\n\t\t\t{"));
			Assert.That(stages, Does.Contain("if (baseBuilder.YardWithoutRefinery())"), "the expansion refinery first, unchanged");
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.QueueSplit.cs"), Does.Contain("law != null && law.RefinerySpacingActive"));
		}

		[Test]
		public void TheWiringIsBehindTheSwitches()
		{
			Assert.That(Source(Dir + "BaseBuildPlanCA.cs"), Does.Contain("if (!QueueStageSplit.Allowed(stage, snapshot.QueueSplit, snapshot.DefenceQueue))\n\t\t\t\t\tcontinue;"));
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs"), Does.Contain("QueueSplit = baseBuilder.QueueSplitArmed && SeparateDefenceQueue(),"));
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs"), Does.Contain("o.RefineryHeld(player, world.WorldTick, gate, refineries, Category);"), "the row carries the queue");
			Assert.That(Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs"), Does.Contain("AiMatchLogWriter.AppendString(b, \"queue\", queue);"));

			// bb_skip (Opus 11-25): the listed types the walk did not start, with the stage that returned; DEV only.
			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("SkipsObserved(buildable, snapshot, pick); // bb_skip, DEV only"));
			var at = p1.IndexOf("void SkipsObserved(", StringComparison.Ordinal);
			Assert.That(p1.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1));
			Assert.That(Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs"), Does.Contain("ObserverRow(owner, \"bb_skip\", b =>"));
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsThemAndTheRowsAreRegistered()
		{
			// V08D (Opus ruling A 11-25): BbQueueSplit ships OFF until the shared-queue classification fix (BuildingAddons is in both sets); REF-S2 stays armed.
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "BbQueueSplit"), Is.EqualTo("false"));
			foreach (var key in new[] { "BbQueueSplit", "BbRefineryAtS2" })
			{
				if (key == "BbRefineryAtS2")
					Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), key), Is.EqualTo("true"), key);
				Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), key), Is.Null, key);
			}

			Assert.That(AblationIds.BbQueueSplit, Is.EqualTo("bb_queue_split"));
			Assert.That(AblationIds.BbRefineryAtS2, Is.EqualTo("bb_refinery_s2"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbQueueSplit));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbRefineryAtS2));
			Assert.That(new BaseBuilderBotModuleCAInfo().BbQueueSplit, Is.False);
			Assert.That(new BaseBuilderBotModuleCAInfo().BbRefineryAtS2, Is.False);
			Assert.That(new BaseBuildSnapshot().QueueSplit, Is.False);
		}
	}
}
