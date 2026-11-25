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
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// UB-SUBSTEP-PERF-01 (Opus 11-25, DEV only): the UnitBuilder visit clock. Every clock read sits in a [Conditional("CAMEO_DEV_LOGGING")] helper, the ub_perf row is once per visit at 5 ms or more, and no clock value steers a decision.
	[TestFixture]
	public sealed class UbSubstepPerf01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static int CountOf(string text, string needle)
		{
			var n = 0;
			for (var at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
				n++;

			return n;
		}

		[Test]
		public void TheClockReadsAreOnlyInsideConditionalHelpers()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid UbSubStart(int slot) => ubSubStart[slot] = System.Diagnostics.Stopwatch.GetTimestamp();"));
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid UbSubStop(int slot) => ubSubAcc[slot] += System.Diagnostics.Stopwatch.GetTimestamp() - ubSubStart[slot];"));
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid UbVisitBegin()"));
			Assert.That(u, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid UbVisitEnd()"));
			Assert.That(CountOf(u, "System.Diagnostics.Stopwatch"), Is.EqualTo(3), "two GetTimestamp reads and one Frequency read, all inside the helpers");
		}

		[Test]
		public void TheVisitRowIsOncePerBotTickAndOnlyAtFiveMilliseconds()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("void IBotTick.BotTick(IBot bot)\n\t\t{\n\t\t\tUbVisitBegin();\n\t\t\tBotTickVisit(bot);\n\t\t\tUbVisitEnd();\n\t\t}"), "one visit = one BotTick call, the body moved unchanged to BotTickVisit");
			Assert.That(u, Does.Contain("if (total < 5000.0)\n\t\t\t\treturn;"), "a visit under 5 ms writes no row");
			Assert.That(u, Does.Contain("ub_perf,,,total_us={total:0};pick_us={Us(0):0};reject_us={Us(1):0};coverage_us={Us(2):0};refresh_us={Us(3):0}"), "the row carries the four sub-steps");
		}

		[Test]
		public void TheSubStepSitesWrapTheExistingStatementsWithoutChangingThem()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("\t\t\t\t\tUbSubStart(0);\n\t\t\t\t\tvar unit = roll ? ChooseRollUnit(queue, skip) : fromFloor ?? (buildRandom\n\t\t\t\t\t\t? ChooseRandomUnitToBuild(queue, excludeLimited, shares, skip)\n\t\t\t\t\t\t: ChooseUnitToBuild(queue, excludeLimited, saturated, shares, skip));\n\t\t\t\t\tUbSubStop(0);"));
			Assert.That(u, Does.Contain("\t\t\tUbSubStart(2);\n\t\t\tUpgradeScanOnce();\n\t\t\tUpgradeSettle(queuedIn);\n\t\t\tUbSubStop(2);"));
			Assert.That(u, Does.Contain("var queuedIn = new Dictionary<string, string>(StringComparer.Ordinal); // item name -> key of the exact queue instance holding it\n\t\t\tUbSubStart(3);"));
			Assert.That(u, Does.Contain("\t\t\t\t\t}\n\t\t\tUbSubStop(3);\n\n\t\t\tUbSubStart(2);"));
			Assert.That(u, Does.Contain("\t\t\tUbSubStart(3);\n\t\t\tvar queues = AIUtils.FindQueues(player, category)"));
			Assert.That(u, Does.Contain("\t\t\t\t.ToList();\n\t\t\tUbSubStop(3);\n\t\t\tif (queues.Count == 0)"));
		}

		[Test]
		public void TheRejectClockWrapsTheDecisionAndKeepsItsSignatureAndTheRollGate()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("string ShouldBuildReject(string name, bool ignoreUnitsToBuild, string queueCategory)\n\t\t{\n\t\t\tUbSubStart(1);\n\t\t\tvar rejectedBy = ShouldBuildRejectCore(name, ignoreUnitsToBuild, queueCategory);\n\t\t\tUbSubStop(1);\n\t\t\treturn rejectedBy;\n\t\t}"));
			Assert.That(u, Does.Contain("string ShouldBuildRejectCore(string name, bool ignoreUnitsToBuild, string queueCategory)\n\t\t{"));
			Assert.That(u, Does.Contain("var rejectedBy = ShouldBuildReject(a.Name, true, queue.Info.Type);\n\t\t\t\tif (rejectedBy != null)"), "the roll reject gate is unchanged");
		}

		[Test]
		public void TheClockNeverSteersADecision()
		{
			var u = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Not.Match(@"if \([^\n]*Ub(Sub|Visit)"), "no clock helper may appear in an if condition");
			Assert.That(u, Does.Not.Match(@"\?[^\n;]*UbSub"), "no clock helper may appear in a conditional expression");
		}
	}
}
