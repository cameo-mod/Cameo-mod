#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// REF-GATE-V08F (Opus 11-25 after REF_PEAK_RCA_01 53f4dbdf). V08D: the survivors' refinery peaks were 1-3 because the stuck-park counter (12 re-plans of 250 ticks = 3000 ticks)
	// parked a wanted anchor for 3000 ticks, every 6000 ticks, although the building queue had never offered the refinery. Each item has its own Info switch and ablation row.
	[TestFixture]
	public sealed class RefGateV08fTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		const string Planner = "mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs";

		[Test]
		public void AStuckCountIgnoresReplansInWhichTheQueueNeverOfferedTheRefinery()
		{
			// off, or offered = the old counter exactly
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(false, false, true, 2, 2, 0, 3, 5), Is.EqualTo(ExpansionPlannerBotModule.AnchorStuckNext(true, 2, 2, 0, 3, 5)));
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, true, true, 2, 2, 0, 4, 5), Is.EqualTo((5, true)), "offered and nothing came of it: stuck");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, true, true, 2, 2, 1, 4, 5), Is.EqualTo((0, false)), "offered and in production: progress");

			// on and never offered: the count stands, never a park
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, false, true, 2, 2, 0, 4, 5), Is.EqualTo((4, false)), "not offered: waiting, not stuck");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, false, true, 2, 2, 0, 11, 12), Is.EqualTo((11, false)), "the 12th unoffered re-plan does not park");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, false, false, 2, 2, 0, 4, 5), Is.EqualTo((0, false)), "another anchor restarts");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, false, true, 3, 2, 0, 4, 5), Is.EqualTo((0, false)), "a refinery gained restarts");
			Assert.That(ExpansionPlannerBotModule.AnchorStuckEvaluated(true, false, true, 2, 2, 1, 4, 5), Is.EqualTo((0, false)), "one in production restarts");
		}

		[Test]
		public void TheOfferReachesThePlannerFromTheStageAndTheRowShipsOffAndIsArmedForTheGenericPlannerOnly()
		{
			var p = Source(Planner);
			Assert.That(p, Does.Contain("public readonly bool RefParkEvaluatedOnly = false;"), "off by default");
			Assert.That(p, Does.Contain("bool ParkEvaluatedOnlyOn => Info.RefParkEvaluatedOnly && RefDemandArmed && !ablateParkEvaluated.Off(player, AblationIds.RefParkEvaluatedOnly);"));
			Assert.That(p, Does.Contain("void IBotExpansionTargetProvider.RefineryPickOffered() => refineryOfferedSinceReplan = true;"));
			Assert.That(p, Does.Contain("var offered = refineryOfferedSinceReplan;\n\t\t\trefineryOfferedSinceReplan = false;\n\t\t\tvar (replans, parkAnchor) = AnchorStuckEvaluated(ParkEvaluatedOnlyOn, offered, anchorStuck.Anchor == anchor,"), "read and cleared at every re-plan that reaches the loop guard");

			Assert.That(Source("mods/CA/Traits/BotModules/IBotExpansionTargetProvider.cs"), Does.Contain("void RefineryPickOffered()\n\t\t{\n\t\t}"), "a default member: fakes need no change");
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs"), Does.Contain("if (pick == refinery)\n\t\t\t\t\tbaseBuilder.RefineryLawProvider()?.RefineryPickOffered();"), "only the refinery itself is the offer; a low-power pick (the power plant) is not");
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.RefGate.cs"), Does.Contain("\"ref_park_evaluated_only\""));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("ref_park_evaluated_only"));

			var planner = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(planner.Value.Nodes.Any(n => n.Key == "RefParkEvaluatedOnly" && n.Value.Value == "true"), Is.True);
		}

		[Test]
		public void ALossEndsWhenARefineryThatWasNotStandingBeforeStandsInRangeOfTheLostCell()
		{
			var lost = new CPos(10, 10);
			var losses = new System.Collections.Generic.Dictionary<CPos, int> { [lost] = 1000 };
			bool Within(CPos r, CPos l) => System.Math.Abs(r.X - l.X) <= 8 && System.Math.Abs(r.Y - l.Y) <= 8;

			Assert.That(ExpansionPlannerBotModule.ReplacedLosses(losses, new[] { new CPos(14, 12) }, 2000, Within), Is.EqualTo(new[] { lost }), "a new refinery in range replaces the loss");
			Assert.That(ExpansionPlannerBotModule.ReplacedLosses(losses, new[] { new CPos(10, 10) }, 2000, Within), Is.EqualTo(new[] { lost }), "rebuilt on the same cell");
			Assert.That(ExpansionPlannerBotModule.ReplacedLosses(losses, new[] { new CPos(40, 40) }, 2000, Within), Is.Empty, "a new refinery out of range does not");
			Assert.That(ExpansionPlannerBotModule.ReplacedLosses(losses, System.Array.Empty<CPos>(), 2000, Within), Is.Empty, "nothing appeared");
			Assert.That(ExpansionPlannerBotModule.ReplacedLosses(losses, new[] { new CPos(14, 12) }, 1000, Within), Is.Empty, "one that shows up in the update that recorded the loss is not a replacement");
		}

		[Test]
		public void TheRebuildRowShipsOffAndIsArmedForTheGenericPlannerBehindRefDemandOnly()
		{
			var p = Source(Planner);
			Assert.That(p, Does.Contain("public readonly bool RefRebuildLost = false;"), "off by default");
			Assert.That(p, Does.Contain("bool RebuildLostOn => Info.RefRebuildLost && RefDemandArmed && !ablateRebuildLost.Off(player, AblationIds.RefRebuildLost);"));
			Assert.That(p, Does.Contain("var appeared = RebuildLostOn && fieldGateLossTick.Count > 0 ? refineryCells.Where(c => !fieldGateSeenRefineries.Contains(c)).ToList() : null;"), "the appeared cells are read before the seen set is replaced");
			Assert.That(p, Does.Contain("RefineryFieldGateCA.Locked(tick, kv.Value, fieldGateThreatTick.GetValueOrDefault(kv.Key, -1), Info.RefineryLostLockTicks,\n\t\t\t\t\t\tInfo.RefineryLostQuietTicks, Info.RefineryLostMemoryTicks)"), "the lock and the quiet check are unchanged");
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("ref_rebuild_lost"));

			var planner = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == "ExpansionPlannerBotModule");
			Assert.That(planner.Value.Nodes.Any(n => n.Key == "RefRebuildLost" && n.Value.Value == "true"), Is.True);
		}

		[Test]
		public void TheS2ZoneDemandBranchRecordsItsHoldGateInDevOnlyUnderItsOwnRow()
		{
			const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";
			var stages = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Stages.cs");
			Assert.That(stages, Does.Contain("\t\t\t\tif (refinery != null)\n\t\t\t\t\tyield return refinery.Name;\n\t\t\t\telse\n\t\t\t\t\tS2RefineryHeldObserved(buildable); // ref_s2_hold_log, DEV only\n\t\t\t}\n\n\t\t\tif (law == null)"),
				"the log sits in the ZoneDemandAtS2 branch (the one followed by the law == null return)");
			var at = stages.IndexOf("void S2RefineryHeldObserved(", StringComparison.Ordinal);
			Assert.That(at, Is.GreaterThan(0));
			Assert.That(stages.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1), "compiled out of a release build");
			Assert.That(stages, Does.Contain("if (ablateS2HoldLog.Off(player, AblationIds.RefS2HoldLog))\n\t\t\t\treturn;\n\n\t\t\tRefineryHeldObserved(RefineryHoldGate(buildable));"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("ref_s2_hold_log"));
		}
	}
}
