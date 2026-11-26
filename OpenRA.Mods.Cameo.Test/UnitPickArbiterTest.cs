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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-03 P5 step (a) (design 02 4c19e5f9 + addendum 2): the unit pick's decision record and DEV equivalence trace. The formatters are pure; the hooks in the module only READ state
	/// (the RNG's draw counter and last raw value, never advance it), are compiled out of release, and sit at fixed places so that an old and a new build write the same rows.
	/// </summary>
	[TestFixture]
	public class UnitPickArbiterTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		[Test]
		public void BinsAreTheLosslessOrderedIntegerUnits()
		{
			Assert.That(UnitPickArbiter.Bins(["a", "b", "c"], [3, 5, 1048576]), Is.EqualTo("a:3|b:5|c:1048576"));
			Assert.That(UnitPickArbiter.Bins([], []), Is.EqualTo(string.Empty));
		}

		[Test]
		public void TheDigestIsTheBotRngSaltHashAsEightHexDigits()
		{
			Assert.That(UnitPickArbiter.Digest("a:3|b:5"), Is.EqualTo(BotRng.Fnv1a("a:3|b:5").ToString("x8")));
			Assert.That(UnitPickArbiter.Digest("x"), Has.Length.EqualTo(8));
			Assert.That(UnitPickArbiter.Digest(null), Is.EqualTo(UnitPickArbiter.Digest(string.Empty)));
		}

		[Test]
		public void TheAvailableSinceTableIsWrittenInOrdinalNameOrder()
		{
			var since = new Dictionary<string, int> { ["e2"] = 900, ["E1"] = 100, ["e1"] = 50 };
			Assert.That(UnitPickArbiter.Avail(since), Is.EqualTo("E1:100|e1:50|e2:900"), "ordinal: capitals first, so the text does not depend on the insertion order or the culture");
		}

		[Test]
		public void TheRowFormatsAreFixedAndCarryNoComma()
		{
			var draw = new UnitPickArbiter.DrawTrace(7, 3, 1, 42, "Vehicle", 2, "a:3|b:5", 8, 123456, 99, "b", true, 1500, 800, "c:limit", "ground|air", 2, "deadbeef", UnitPickArbiter.RuleDraw);
			Assert.That(UnitPickArbiter.DrawDetail(draw),
				Is.EqualTo("ord=7;fill=3;att=1;prod=42;n=2;bins=a:3|b:5;bound=8;raw=123456;tot=99;first=1;pm=1500;bank=800;excl=c:limit;roles=ground|air;avail=2/deadbeef;rule=R3_draw"));

			var empty = new UnitPickArbiter.EmptyTrace(3, 0, 42, "Vehicle", "-", "-", 99, UnitPickArbiter.RuleEmptyPool);
			Assert.That(UnitPickArbiter.EmptyDetail(empty), Is.EqualTo("fill=3;att=0;prod=42;excl=-;roles=-;tot=99;rule=R1_empty"));

			var fill = new UnitPickArbiter.FillTrace(3, 1, 42, "Vehicle", UnitPickArbiter.ResultRefuse, UnitPickArbiter.RuleGate, "afford_cash", "b", 1, 600, 1200, 2000, 500, 1500, "2", false, 0);
			Assert.That(UnitPickArbiter.FillDetail(fill),
				Is.EqualTo("fill=3;att=1;prod=42;res=refuse;rule=R4_gate;why=afford_cash;held=1;gate=600;full=1200;bank=2000;reserved=500;left=1500;headroom=2;first=0;order=0"));

			Assert.That(UnitPickArbiter.FillEndDetail(new UnitPickArbiter.FillEndTrace(3, 42, 2, 101, 777)), Is.EqualTo("fill=3;prod=42;orders=2;tot=101;last=777"));

			foreach (var text in new[] { UnitPickArbiter.DrawDetail(draw), UnitPickArbiter.EmptyDetail(empty), UnitPickArbiter.FillDetail(fill) })
				Assert.That(text, Does.Not.Contain(","), "the row is a comma separated file: the detail column must stay one field");
		}

		[Test]
		public void TheHeadroomIsTextOrNotAvailable()
		{
			var headroom = new Dictionary<string, int> { ["a"] = -1, ["b"] = 3 };
			Assert.That(UnitPickArbiter.HeadroomText(headroom, "a"), Is.EqualTo("-1"));
			Assert.That(UnitPickArbiter.HeadroomText(headroom, "b"), Is.EqualTo("3"));
			Assert.That(UnitPickArbiter.HeadroomText(headroom, "z"), Is.EqualTo("na"));
			Assert.That(UnitPickArbiter.HeadroomText(headroom, null), Is.EqualTo("na"));
		}

		[Test]
		public void TheAdmissionRuleNamesKeepTheOldOrder()
		{
			Assert.That(UnitPickArbiter.AdmitRule(true, true), Is.EqualTo(UnitPickArbiter.RuleAdmitPaygo), "pay-as-you-go first build comes before the lean");
			Assert.That(UnitPickArbiter.AdmitRule(false, true), Is.EqualTo(UnitPickArbiter.RuleAdmitLean));
			Assert.That(UnitPickArbiter.AdmitRule(false, false), Is.EqualTo(UnitPickArbiter.RuleAdmitFull));
		}

		[Test]
		public void TheTraceHooksAreDevOnlyAndOnlyReadTheRng()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.PickTrace.cs");
			var hooks = Count(part, "\t\tvoid PickTrace") - Count(part, "\t\tvoid PickTraceWrite(") - Count(part, "\t\tvoid PickTraceFillRow(");
			Assert.That(hooks, Is.EqualTo(7), "fill start, empty, draw, none, refuse, accept, fill end");
			Assert.That(Count(part, "[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid PickTrace"), Is.EqualTo(7), "every hook is compiled out of release");
			Assert.That(Count(part, "if (!Info.UnitPickTrace)\n\t\t\t\treturn;"), Is.EqualTo(7), "and every hook is off while the switch is off");

			foreach (var advance in new[] { ".Next(", ".NextFloat(", ".NextUint(", ".NextUlong(", ".PickWeighted(", ".ShuffleInPlace(" })
				Assert.That(part, Does.Not.Contain(advance), "the trace never advances the RNG stream (" + advance + ")");

			Assert.That(part, Does.Contain("PickTraceRng().TotalCount"));
			Assert.That(part, Does.Contain("rng.Last"));
			Assert.That(part, Does.Contain("\"unit_pick_trace.csv\""), "its own file: the existing unit_roll.csv analysers are untouched");
		}

		[Test]
		public void TheHooksSitAtTheirFixedPlacesAndTheDrawLinesAreUntouched()
		{
			var u = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);

			// the draw lines the older pins read are as before; the trace comes after them
			var pick = body.IndexOf("var pick = UnitRoll.Pick(units, BotRng.For(player, nameof(UnitBuilderBotModuleCA)).Next(UnitRoll.Sum(units)));", StringComparison.Ordinal);
			var first = body.IndexOf("rollFirst = !rollOrdered.Contains(pool[pick].Name);", StringComparison.Ordinal);
			var log = body.IndexOf("RollPickLog(queue.Info.Type, pool, weights, pick,", StringComparison.Ordinal);
			var trace = body.IndexOf("PickTraceDraw(queue, pool, units, pick, skip.Count, queueRoles);", StringComparison.Ordinal);
			var ret = body.IndexOf("return pool[pick];", StringComparison.Ordinal);
			Assert.That(pick, Is.GreaterThan(0));
			Assert.That(first, Is.GreaterThan(pick));
			Assert.That(log, Is.GreaterThan(first));
			Assert.That(trace, Is.GreaterThan(log), "the draw row reads the final first-copy flag, after the pick");
			Assert.That(ret, Is.GreaterThan(trace));

			// the empty row comes after the wave roles are reported and before the empty log
			var empty = body.IndexOf("PickTraceEmpty(queue, skip.Count, queueRoles);", StringComparison.Ordinal);
			Assert.That(empty, Is.GreaterThan(body.IndexOf("var queueRoles = ReportWaveRoles(", StringComparison.Ordinal)));
			Assert.That(empty, Is.LessThan(body.IndexOf("RollEmptyLog(queue.Info.Type", StringComparison.Ordinal)));

			// the fill: start after the per-queue set-up, one refusal row per post-draw gate (5), the accept row before the order, the end row after the loop
			var fillStart = u.IndexOf("PickTraceFillStart(queue);", StringComparison.Ordinal);
			Assert.That(fillStart, Is.GreaterThan(u.IndexOf("var baselineTried = false;", StringComparison.Ordinal)));
			Assert.That(fillStart, Is.LessThan(u.IndexOf("var unit = roll ? ChooseRollUnit(queue, skip)", StringComparison.Ordinal)));
			Assert.That(Count(u, "PickTraceRefuse(queue, skip.Count, "), Is.EqualTo(5), "ShouldBuildReject, headroom, capturer, aircraft, afford_cash");
			Assert.That(Count(u, "PickTraceNone(queue, skip.Count, held, batch);"), Is.EqualTo(1));
			Assert.That(u, Does.Contain("PickTraceAccept(queue, skip.Count, name, held, cost, fullCost, paygo, leaned, batch);\n\t\t\t\t\tSetUnitInterval(name);\n\t\t\t\t\tbot.QueueOrder(Order.StartProduction(queue.Actor, name, 1));"));
			Assert.That(u.IndexOf("PickTraceFillEnd(queue);", StringComparison.Ordinal), Is.GreaterThan(u.IndexOf("PickTraceAccept(", StringComparison.Ordinal)));
			Assert.That(u.IndexOf("PickTraceFillEnd(queue);", StringComparison.Ordinal), Is.LessThan(u.IndexOf("INFANTRY-REJECT-LOG-01 (DEV only): this queue's pass ended", StringComparison.Ordinal)));
		}

		[Test]
		public void TheSwitchIsOffInCodeAndOnInTheGenericBlock()
		{
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.cs"), Does.Contain("public readonly bool UnitPickTrace = false;"));
			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			Assert.That(Count(yaml, "\t\tUnitPickTrace: true\n"), Is.EqualTo(1));
			Assert.That(Count(yaml, "UnitPickTrace:"), Is.EqualTo(1), "@generic only; the classic block never traces");
		}
	}
}
