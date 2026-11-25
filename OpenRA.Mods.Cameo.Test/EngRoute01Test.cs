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
	// ENG-ROUTE-01 (V08 RCA 8c37f899): one EngineerBotModule SafeRoute visit took 2549 ms (an uncapped path search with a FindActorsInCircle per cell). The search is bounded: an
	// expansion budget per call (overrun = the route is unsafe for this visit), a cap on searches per world tick, and a danger cache that lives several ticks. Row er_route_budget.
	[TestFixture]
	public sealed class EngRoute01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Engineer() => Source("mods/Cameo/Traits/BotModules/EngineerBotModule.cs");

		static string SafeRouteBody(string e)
		{
			var start = e.IndexOf("List<CPos> SafeRoute(Actor capturer, Actor target, string caller = \"\")", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			return e.Substring(start, e.IndexOf("// CA parent verbatim, kept for MaxExposedRouteCells < 0", start, StringComparison.Ordinal) - start);
		}

		[Test]
		public void TheSearchCountsItsCostEvaluationsAndAbandonsItselfPastTheBudget()
		{
			var body = SafeRouteBody(Engineer());
			Assert.That(body, Does.Contain("var limit = budget ? Info.SafeRouteExpansionBudget : 0;"), "no budget when the switch or the row is off: the old unbounded search");
			Assert.That(body, Does.Contain("evaluations++;\n\t\t\t\t\t\tif (limit > 0 && evaluations > limit)\n\t\t\t\t\t\t\tthrow new RouteBudgetExceeded();"));
			Assert.That(body, Does.Contain("catch (RouteBudgetExceeded)"));
			Assert.That(body, Does.Contain("RouteBudgetOverruns++;"));
			var catchAt = body.IndexOf("catch (RouteBudgetExceeded)", StringComparison.Ordinal);
			Assert.That(body.IndexOf("return null;", catchAt, StringComparison.Ordinal), Is.GreaterThan(catchAt), "an overrun counts as unsafe: no route");
			Assert.That(body, Does.Contain("return Danger(capturer, loc);"), "the real cost is unchanged");
		}

		[Test]
		public void AtMostNSearchesRunPerWorldTickAndADeferredOneIsUnsafeForTheVisit()
		{
			var body = SafeRouteBody(Engineer());
			Assert.That(body, Does.Contain("if (budget && Info.SafeRouteCallsPerTick > 0)"));
			Assert.That(body, Does.Contain("if (routeCallsTick != world.WorldTick)"));
			Assert.That(body, Does.Contain("if (routeCalls >= Info.SafeRouteCallsPerTick)"));
			Assert.That(body, Does.Contain("RouteCallsDeferred++;"));
			Assert.That(body.IndexOf("routeCalls++;", StringComparison.Ordinal), Is.GreaterThan(body.IndexOf("RouteCallsDeferred++;", StringComparison.Ordinal)));

			var e = Engineer();
			Assert.That(e, Does.Contain("SafeRoute(a, target, \"recheck\")"));
			Assert.That(e, Does.Contain("SafeRoute(capturer, t, \"rank\")"));
			Assert.That(e, Does.Contain("SafeRoute(capturer, target, \"gate\")"));
		}

		[Test]
		public void TheDangerCacheLivesSeveralTicksOnlyWithTheSwitchOn()
		{
			var e = Engineer();
			Assert.That(e, Does.Contain("if (world.WorldTick - dangerCacheTick >= (RouteBudgetOn ? Math.Max(1, Info.DangerCacheTicks) : 1))"), "1 tick (the old cache) when off");
			Assert.That(e, Does.Contain("bool RouteBudgetOn => Info.SafeRouteBudget && Info.MaxExposedRouteCells >= 0 && !ablateRouteBudget.Off(player, AblationIds.EngRouteBudget);"));
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.EngRoute.cs"), Does.Contain("\"er_route_budget\""));
		}

		[Test]
		public void TheDevTimerAndCountersAreCompiledOutOfRelease()
		{
			var body = SafeRouteBody(Engineer());
			Assert.That(body, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tvar watch = System.Diagnostics.Stopwatch.StartNew();\n#endif"));
			Assert.That(body, Does.Contain("evaluations={evaluations}"), "the expansion counter is logged");
			Assert.That(body, Does.Contain("caller={caller}"), "with the caller name");
			Assert.That(body, Does.Contain("watch.ElapsedMilliseconds >= 50"));
		}

		[Test]
		public void OnlyGenericbotsEngineerModuleArmsTheSwitchAndNoOtherModuleDoes()
		{
			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var engineer = player.Single(n => n.Key == "EngineerBotModule").Value.Nodes;
			Assert.That(engineer.Single(n => n.Key == "SafeRouteBudget").Value.Value, Is.EqualTo("true"));
			Assert.That(int.Parse(engineer.Single(n => n.Key == "SafeRouteExpansionBudget").Value.Value), Is.InRange(500, 5000));
			Assert.That(int.Parse(engineer.Single(n => n.Key == "SafeRouteCallsPerTick").Value.Value), Is.InRange(1, 10));
			Assert.That(int.Parse(engineer.Single(n => n.Key == "DangerCacheTicks").Value.Value), Is.InRange(1, 25));
			Assert.That(int.Parse(engineer.Single(n => n.Key == "MaxExposedRouteCells").Value.Value), Is.GreaterThanOrEqualTo(0), "the budget needs the live planner");
			Assert.That(player.Count(n => n.Key.StartsWith("EngineerBotModule", StringComparison.Ordinal)), Is.EqualTo(1), "classic runs CaptureManagerBotModuleCA, not this module");
		}
	}
}
