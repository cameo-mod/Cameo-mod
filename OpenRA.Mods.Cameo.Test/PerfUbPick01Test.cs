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
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-UB-PICK-01 (Opus 11-26): V08G Multi4's unit builder spent 98% of its visits in the pick. HANS-PROFILE-01's FocusCore priced the whole pool with a live
	// GetProductionCost on every roll (outside the FIX 4 visit cost cache), and a refused pick re-rolls. The fix reads the price through Cost (the same integer).
	// Items 3 and 4 are DEV-only counters (roll work per pick in the ub_perf row) and a DEV slow-call line for the expansion planner (EXP-SLOW-01, 250 ms).
	[TestFixture]
	public sealed class PerfUbPick01Test
	{
		const string Bots = "mods/CA/Traits/BotModules/";
		const string Unit = Bots + "UnitBuilderBotModuleCA.cs";
		const string Planner = "mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs";

		static string SourceDir => Path.Combine(AppContext.BaseDirectory, "source");

		static string Source(string file) => File.ReadAllText(Path.Combine(SourceDir, file)).Replace("\r\n", "\n");

		static string ReleaseView(string source) => Regex.Replace(source, @"#if CAMEO_DEV_LOGGING\n.*?#endif\n", "", RegexOptions.Singleline);

		static int CountOf(string text, string needle)
		{
			var n = 0;
			for (var at = text.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
				n++;

			return n;
		}

		[Test]
		public void NoUnitBuilderPartialReadsTheCostLive()
		{
			var parts = Directory.GetFiles(Path.Combine(SourceDir, Bots), "UnitBuilderBotModuleCA*.cs");
			Assert.That(parts.Length, Is.GreaterThan(3), "the partials are packaged");
			Assert.That(parts.Select(Path.GetFileName), Does.Contain("UnitBuilderBotModuleCA.CoreFocus.cs"));
			foreach (var p in parts)
				Assert.That(File.ReadAllText(p), Does.Not.Contain("GetProductionCost("), Path.GetFileName(p) + ": every cost read goes through Cost (the visit cache)");

			Assert.That(Source(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs"), Does.Contain("price[i] = Cost(queue, pool[i]);"));
		}

		[Test]
		public void TheRollCountersAreDevOnlyAndSteerNothing()
		{
			var part = Source(Bots + "UnitBuilderBotModuleCA.PickCount.cs");
			var release = ReleaseView(part);
			Assert.That(release, Does.Not.Contain("int ubRolls"), "a release instance has no counter fields");
			Assert.That(release, Does.Not.Contain("UbPickCounts()"));
			Assert.That(CountOf(part, "[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]"), Is.EqualTo(3), "reset, pool and done are [Conditional]");

			var u = Source(Unit);
			Assert.That(u, Does.Contain("\t\t\tUbSubStart(4);\n\t\t\tUbPickCountReset();\n"), "the counters start with the visit clock");
			Assert.That(u, Does.Contain("refresh_us={Us(3):0}{UbPickCounts()}\");"), "appended to the existing ub_perf row");
			Assert.That(u, Does.Contain("\t\t\t\t\tUbSubStop(0);\n\t\t\t\t\tUbRollDone(roll, unit != null);\n"), "one count per pick call, after the pick clock");
			Assert.That(u, Does.Contain("\t\t\tUbRollPool(pool.Count + rollExcluded.Count, pool.Count);\n"));
			Assert.That(CountOf(u, "System.Diagnostics.Stopwatch"), Is.EqualTo(3), "no new clock read in the unit builder");
			Assert.That(u, Does.Not.Match(@"if \([^\n]*Ub(Roll|Pick)"), "no counter in an if condition");
			Assert.That(u, Does.Not.Match(@"\?[^\n;]*Ub(Roll|Pick)"), "no counter in a conditional expression");
		}

		[Test]
		public void ThePlannerSlowCallGuardWrapsTheUnchangedBodyAndIsDevOnly()
		{
			var p = Source(Planner);
			Assert.That(p, Does.Contain("void IBotTick.BotTick(IBot bot)\n\t\t{\n\t\t\tSlowCallBegin();\n\t\t\tBotTickVisit(bot);\n\t\t\tSlowCallEnd();\n\t\t}"), "one guard per BotTick call");
			Assert.That(p, Does.Contain("void BotTickVisit(IBot bot)\n\t\t{\n\t\t\t// LC4 (AI_REVIEW_FRANSOTTO P1c)"), "the body starts as before");

			foreach (var mark in new[] { "McvSafe", "Coverage", "Buildables", "Actors", "Fields", "Target", "Claims", "Mcv" })
				Assert.That(CountOf(p, "SlowCallMark(SlowMark" + mark + ");"), Is.EqualTo(1), mark + " is marked once");

			Assert.That(p, Does.Contain("\t\t\tSlowCallMark(SlowMarkClaims);\n\t\t\tif (Info.DriveMcvRequests)\n\t\t\t\tRequestMcv(bot);\n\t\t\tSlowCallMark(SlowMarkMcv);"));
			Assert.That(p, Does.Not.Match(@"if \([^\n]*SlowCall"), "no guard helper in an if condition");
			Assert.That(p, Does.Not.Contain("Stopwatch.GetTimestamp() - slow"), "the clock math lives in the partial");

			var guard = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.SlowCall.cs");
			Assert.That(CountOf(guard, "[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]"), Is.EqualTo(3), "begin, mark and end are [Conditional]");
			var release = ReleaseView(guard);
			Assert.That(release, Does.Not.Contain("Stopwatch"), "a release build reads no clock");
			Assert.That(release, Does.Not.Contain("slowMarks = new"), "and allocates nothing");
			Assert.That(guard, Does.Contain("const double SlowCallMs = 250.0;"));
			Assert.That(guard, Does.Contain("if (total < SlowCallMs)\n\t\t\t\treturn;"), "only calls of 250 ms or more write a line");
			Assert.That(guard, Does.Contain("Log.Write(\"debug\", $\"EXP-SLOW-01 player={player.ClientIndex} tick={world.WorldTick} ms={total:F1}"));
		}
	}
}
