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
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// INFANTRY-REJECT-LOG-01 (DEV only): the reject log is bookkeeping that never feeds a decision. The aggregator prints one line per queue type per
	/// period, and the unit builder feeds it only under CAMEO_DEV_LOGGING (official builds: the feed is compiled to a constant false).
	/// </summary>
	[TestFixture]
	public class InfantryRejectLog01Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		[Test]
		public void ALineIsPrintedOncePerPeriodWithTheCountsAndTheCountsReset()
		{
			var log = new InfantryRejectLogCA();
			log.Reject("Infantry", "delay");
			log.Reject("Infantry", "delay");
			log.Reject("Infantry", "afford_cash");
			log.Pass("Infantry", true);
			log.Floor("Infantry", false);

			Assert.That(log.TryFlush("Infantry", 0, out var first), Is.True);
			Assert.That(first, Does.Contain("t=0 q=Infantry passes=1 idle=1 floor_ran=1 floor_found=0"));
			Assert.That(first, Does.Contain(" delay=2 "));
			Assert.That(first, Does.Contain(" afford_cash=1 "));
			Assert.That(first, Does.Contain(" no_candidate=0"));

			// Inside the period nothing is printed, and the counts were cleared by the first line.
			log.Pass("Infantry", false);
			Assert.That(log.TryFlush("Infantry", InfantryRejectLogCA.PeriodTicks - 1, out _), Is.False);
			Assert.That(log.TryFlush("Infantry", InfantryRejectLogCA.PeriodTicks, out var second), Is.True);
			Assert.That(second, Does.Contain("passes=1 idle=0 floor_ran=0 floor_found=0"));
			Assert.That(second, Does.Contain(" delay=0 "));
		}

		[Test]
		public void NothingIsPrintedBeforeAPassEndedAndQueueTypesAreKeptApart()
		{
			var log = new InfantryRejectLogCA();
			log.Reject("RAInfantry", "limit");

			Assert.That(log.TryFlush("RAInfantry", 0, out _), Is.False, "no pass has ended yet");
			log.Pass("RAInfantry", false);

			Assert.That(log.TryFlush("Infantry", 0, out _), Is.False, "another queue type has no pass");
			Assert.That(log.TryFlush("RAInfantry", 0, out var line), Is.True);
			Assert.That(line, Does.Contain("q=RAInfantry").And.Contain(" limit=1 "));
		}

		[Test]
		public void TheFeedIsCompiledOutOfOfficialBuildsAndTheCountsStayOutOfTheDecisions()
		{
			var builder = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");

			Assert.That(builder, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\tbool InfantryRejectLogOn => world.WorldActor.TraitOrDefault<CameoLogHub>()?.Sink.Enabled == true;\n#else\n\t\tbool InfantryRejectLogOn => false;\n#endif"),
				"release: the feed is a constant false, so no reject is counted");
			Assert.That(builder, Does.Contain("var rejectLog = InfantryRejectLogOn ? infantryRejectLog : null;"));
			Assert.That(builder, Does.Contain("rejectLog.TryFlush(queue.Info.Type, world.WorldTick, out var line)"));
			Assert.That(builder, Does.Contain("\"infantry_reject.csv\""), "the row goes to the DEV sink's context file");
			Assert.That(builder, Does.Contain("string ShouldBuildReject(string name, bool ignoreUnitsToBuild, string queueCategory)"));
			Assert.That(builder, Does.Contain("bool ShouldBuild(string name, bool ignoreUnitsToBuild, string queueCategory = null) =>\n\t\t\tShouldBuildReject(name, ignoreUnitsToBuild, queueCategory) == null;"),
				"ShouldBuild keeps its bool meaning");
			Assert.That(builder, Does.Contain("var refusedBy = ShouldBuildReject(name, baselineTried || fromIdleFloor, queue.Info.Type);"));
		}
	}
}
