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
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// SPIKE-FRANS-01 (Opus, from the V05 spike RCA): DEV-only spike timers for every AI (the seat is the internal name: every AI prints as one
	/// player and client index), sub-timers inside FransGeneral.BotTick and the GC delta of each spike; plus one release switch,
	/// ScanSiteIntelCache, that builds each SiteIntel once per key per scan tick. The tests pin the wiring (source pins) and that the timers
	/// are compiled out of a release build.
	/// </summary>
	[TestFixture]
	public class SpikeFrans01Test
	{
		const string Frans = "OpenRA.Mods.Fransbot/Traits/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		[Test]
		public void TheScanSwitchIsOffInCodeAndOnForTheFransStack()
		{
			var general = RepoFile(Frans + "FransGeneralBotModule.cs");
			Assert.That(general, Does.Contain("public readonly bool ScanSiteIntelCache = false;"), "release switch, default off");
			Assert.That(RepoFile("mods/cameo/ai/fransbot.yaml"), Does.Contain("\t\tScanSiteIntelCache: true"), "armed in the Frans block, which only the Frans stack and the inc3 bots load");
			Assert.That(RepoFile("mods/cameo/ai/ai.yaml"), Does.Not.Contain("ScanSiteIntelCache"), "no per-personality line and never @classic");
		}

		[Test]
		public void TheSiteIntelCacheIsKeyedByEverythingTheResultDependsOnAndDiesWithTheTick()
		{
			var general = RepoFile(Frans + "FransGeneralBotModule.cs");
			Assert.That(general, Does.Contain("var key = (center, radiusCells, maximumContactAgeTicks, visiblePrimary?.ActorID ?? 0u);"), "centre, radius, age and primary actor");
			Assert.That(general, Does.Contain("if (siteIntelCacheTick != world.WorldTick || siteIntelCacheSnapshot != combatIntelService.SnapshotWorldTick)"), "one world tick and one sensor snapshot");
			Assert.That(general, Does.Contain("siteIntelCache.Clear();"));
			Assert.That(general, Does.Contain("if (!Info.ScanSiteIntelCache || !inMissionScan)\n\t\t\t\treturn BuildSiteIntelUncached(center, radiusCells, maximumContactAgeTicks, visiblePrimary, false);"), "switch off, or outside the scan = the old per-call build with the old sorts");
			Assert.That(general, Does.Contain("siteIntelCache.Clear();\n\t\t\tinMissionScan = true;\n\t\t\ttry\n\t\t\t{\n\t\t\t\tRebuildMissionBoard();\n\t\t\t}\n\t\t\tfinally\n\t\t\t{\n\t\t\t\tinMissionScan = false;\n\t\t\t\tsiteIntelCache.Clear();\n\t\t\t}"), "the cache lives only inside EnsureCurrentMissions (cleared at both ends, also on an exception): attack callbacks and order paths read live health and never see it");
			Assert.That(general, Does.Contain("\t\tvoid RebuildMissionBoard()\n\t\t{\n\t\t\tusing (Step(\"snapshot\"))"), "the board rebuild body");
			Assert.That(general, Does.Contain("presorted ? (IEnumerable<FransCombatIntelContact>)combatIntelService.EnemyCombatContacts : combatIntelService.EnemyCombatContacts.OrderBy(c => c.ActorId)"), "no second sort of a list already in actor-id order");
			Assert.That(general, Does.Contain("presorted && visiblePrimary == null ? actors.Values.ToArray() : actors.Values.OrderBy(a => a.ActorId).ToArray()"), "the final sort stays when a primary may add an id");
			Assert.That(general, Does.Contain("var built = BuildSiteIntelUncached(center, radiusCells, maximumContactAgeTicks, visiblePrimary, true);"));

			// The contact list is built in actor-id order, which the presorted path relies on.
			Assert.That(RepoFile(Frans + "FransCombatIntelBotModule.cs"), Does.Contain("foreach (var pair in enemyCombatMemory.OrderBy(p => p.Key))"));
		}

		[Test]
		public void EveryScanStepHasItsOwnTimerAndTheReportIsDevOnly()
		{
			var general = RepoFile(Frans + "FransGeneralBotModule.cs");
			foreach (var step in new[]
			{
				"ensure_missions", "strategic_assessment", "snapshot", "cleanup_defense", "spawn_probes", "mine_observation", "transport_losses",
				"defend_missions", "append_transport_secure", "append_secure", "append_beach_secure", "append_raid", "append_recon", "site_intel",
			})
				Assert.That(general, Does.Contain("Step(\"" + step + "\")"), "scan step " + step);

			Assert.That(general, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tscanSteps.Clear();"), "cleared at the start of every scan, DEV only");
			Assert.That(general, Does.Contain("ReportScanSpike(Stopwatch.GetTimestamp() - scanStart, scanGc);"));
			Assert.That(general, Does.Contain("[FRANS-SCAN-SPIKE][WT {world.WorldTick}] seat={player.InternalName}#{player.PlayerActor.ActorID} total={totalMs:0.0} ms contacts="));
			Assert.That(general, Does.Contain("if (totalMs >= FransBotLog.SpikeLogMilliseconds)"), "only a scan over the limit is logged");
			Assert.That(general, Does.Contain("#else\n\t\t\tpublic ScanStep(FransGeneralBotModule owner, string name)\n\t\t\t{\n\t\t\t}\n\n\t\t\tpublic void Dispose()\n\t\t\t{\n\t\t\t}\n#endif"), "a release build keeps an empty struct");
		}

		[Test]
		public void TheFransProfilerLogsOnlyAboveFiftyMillisecondsWithTheSeatAndIsAbsentFromARelease()
		{
			var log = RepoFile(Frans + "FransBotLog.cs");
			Assert.That(log, Does.Contain("public const double SpikeLogMilliseconds = 50.0;"));
			Assert.That(log, Does.Contain("const double ImmediateSpikeMilliseconds = SpikeLogMilliseconds;"));
			Assert.That(log, Does.Contain("seat={player?.InternalName}#{player?.PlayerActor?.ActorID} {label} = {elapsedMs:0.00} ms {gcMark.Since()}"), "every AI prints as the same player name");
			Assert.That(log, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\treturn new PerfScope(world, player, label);\n#else"), "a release build takes no timestamps");
			Assert.That(log, Does.Contain("\t\t\treturn default;\n#endif"));
			Assert.That(log, Does.Contain("public static GcMark Now() => new(GC.GetTotalPauseDuration().Ticks, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));"));
			Assert.That(RepoFile(Frans + "FransCombatIntelBotModule.cs"), Does.Contain("FransBotLog.Profile(world, player, \"FransCombatIntel.Refresh\")"), "the sensor refresh has its own timer");
		}

		[Test]
		public void ModularBotLogsEverySlowModuleCallAndEverySlowBotTickForEverySeat()
		{
			var bot = RepoFile("OpenRA.Mods.Cameo/Traits/ModularBot.cs");
			Assert.That(bot, Does.Contain("public readonly int ModuleSpikeLogMs = 50;"));
			Assert.That(bot, Does.Contain("RecordModuleCall(t, Stopwatch.GetTimestamp() - started, gcStart);"), "every module call, every AI, not only AI 0");
			Assert.That(bot, Does.Contain("[BOT-MODULE-SPIKE][WT {0}] seat={1}#{2} module={3} = {4:F1} ms {5}"));
			Assert.That(bot, Does.Contain("[BOT-TICK-SPIKE][WT {0}] seat={1}#{2} mode={3} total={4:F1} ms modules={5:F1} drain={6:F1} other={7:F1} worst: {8} {9}"), "the part of the tick that no module timer saw");
			Assert.That(bot, Does.Contain("ReportTickSpike(tickEnd - tickStart, tickEnd - drainStart, tickGcStart);"));
			Assert.That(bot, Does.Contain("\"BotModuleTiming player={0} tick={1} mode={2} seat={3}\""), "the window line names the seat");
			Assert.That(bot, Does.Contain("#else\n\t\t\tDrainOrders();\n#endif"), "a release build only drains");

			// Every timer sits inside the DEV block: the release loop is the plain call.
			var dev = bot.IndexOf("#if CAMEO_DEV_LOGGING\n\t\t\t\t\tfor (var i = 0; i < calls; i++)", StringComparison.Ordinal);
			var plain = bot.IndexOf("#else\n\t\t\t\t\tfor (var i = 0; i < calls; i++)\n\t\t\t\t\t\tt.BotTick(this);\n#endif", StringComparison.Ordinal);
			Assert.That(dev, Is.GreaterThan(0));
			Assert.That(plain, Is.GreaterThan(dev), "release = t.BotTick(this) only");
		}
	}
}
