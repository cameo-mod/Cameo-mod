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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// MCV-REBUILD-01 (Opus 11-24, V04 RCA): no yard + no MCV starts an MCV at once; a failed external request frees its slot quickly. Each row is genericbot only.
	[TestFixture]
	public sealed class McvRebuild01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void AnMcvIsNeededOnlyWithNoYardNoMcvAndNoneQueued()
		{
			Assert.That(McvRebuildCA.NeedsMcv(0, 0, false), Is.True);
			Assert.That(McvRebuildCA.NeedsMcv(1, 0, false), Is.False, "a yard stands");
			Assert.That(McvRebuildCA.NeedsMcv(0, 1, false), Is.False, "an MCV exists");
			Assert.That(McvRebuildCA.NeedsMcv(0, 0, true), Is.False, "one is already queued");
		}

		[Test]
		public void TheFastDropShortensOnlyWhenArmed()
		{
			Assert.That(McvRebuildCA.DropLimit(false, 250, 25), Is.EqualTo(250));
			Assert.That(McvRebuildCA.DropLimit(true, 250, 25), Is.EqualTo(25));
			Assert.That(McvRebuildCA.DropLimit(true, 250, 0), Is.EqualTo(1), "never below one sweep");
			Assert.That(McvRebuildCA.DropLimit(true, 20, 500), Is.EqualTo(20), "never longer than the base limit");
		}

		[Test]
		public void TheRebuildRunsBeforeEveryPauseVoteAndGateAndSkipsTheUnitGates()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var call = u.IndexOf("if (McvRebuildOn)\n\t\t\t\tTryRebuildMcv(bot);", StringComparison.Ordinal);
			Assert.That(call, Is.GreaterThan(0));
			Assert.That(call, Is.LessThan(u.IndexOf("ServeUpgradeQueues(bot);", call, StringComparison.Ordinal)));
			Assert.That(call, Is.LessThan(u.IndexOf("rp.PauseUnitProduction", call, StringComparison.Ordinal)));

			var start = u.IndexOf("void TryRebuildMcv(IBot bot)", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("void ServeUpgradeQueues(IBot bot)", start, StringComparison.Ordinal) - start);
			foreach (var gate in new[] { "ShouldBuild", "UnitDelays", "UnitIntervals", "SetUnitInterval", "ProductionMinCashRequirement", "GetUnitsToBuildForCategory", "PauseUnitProduction" })
				Assert.That(body, Does.Not.Contain(gate), $"the MCV start bypasses {gate}");

			Assert.That(body, Does.Contain("Order.StartProduction(best.Actor, bestItem.Name, 1)"));
			Assert.That(body, Does.Contain("McvRebuildCA.NeedsMcv(yards, mcvs, queued)"));
			Assert.That(body, Does.Contain("#if CAMEO_DEV_LOGGING"), "the no-factory log is DEV only");
		}

		[Test]
		public void TheExternalRequestDropUsesTheShortLimitUnderTheRow()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("else if (++intent.FailedSweeps >= RequestDropSweeps)"));
			Assert.That(u, Does.Not.Contain("++intent.FailedSweeps >= RequestDropFailedSweeps)"));
		}

		[Test]
		public void BothRowsAreRegisteredAndOnlyGenericbotArmsThem()
		{
			var ids = Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.McvRebuild.cs");
			Assert.That(ids, Does.Contain("\"mcv_rebuild_first\""));
			Assert.That(ids, Does.Contain("\"pb_request_slot\""));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var armed = player.Where(n => n.Key.StartsWith("UnitBuilderBotModuleCA@", StringComparison.Ordinal)
				&& n.Value.Nodes.Any(x => x.Key == "McvRebuildFirst" && x.Value.Value == "true")).ToArray();
			Assert.That(armed.Length, Is.EqualTo(1));
			Assert.That(armed[0].Key, Is.EqualTo("UnitBuilderBotModuleCA@generic"));
			Assert.That(armed[0].Value.Nodes.Single(x => x.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			Assert.That(armed[0].Value.Nodes.Single(x => x.Key == "RequestSlotFastDrop").Value.Value, Is.EqualTo("true"));
		}
	}
}
