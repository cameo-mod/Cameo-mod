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

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-03 P5 (b), row ub_gates_up (design 02 contradiction 2, addendum 3 item 5). The three gates that read the pass's batch (in-pass limit headroom, capturer cap with pending capturers, aircraft caps with
	/// pending aircraft) run after the draw today: a pick they refuse is redrawn but was already in the pool, the mean floor, the core and the lean's dearest price. With the row on R1 asks the same gates before the draw.
	/// NOT distribution-equal: the tests below pin the wiring and show exactly when the result differs.
	/// </summary>
	[TestFixture]
	public class UnitGatesUpTest
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

		static string RollBody(string unit)
		{
			var start = unit.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			return unit.Substring(start, unit.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
		}

		[Test]
		public void TheRowHasItsOwnAblationIdInItsOwnFile()
		{
			Assert.That(AblationIds.UbGatesUp, Is.EqualTo("ub_gates_up"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.UbGatesUp.cs"), Does.Contain("public const string UbGatesUp = \"ub_gates_up\";"));
			Assert.That(System.Text.RegularExpressions.Regex.IsMatch(AblationIds.UbGatesUp, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");
		}

		[Test]
		public void TheSwitchIsOffInCodeOnInTheGenericBlockAndAbsentFromClassic()
		{
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.cs"), Does.Contain("public readonly bool UnitGatesUp = false;"), "off in code: only the yaml arms it");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var generic = yaml.IndexOf("\tUnitBuilderBotModuleCA@generic:", StringComparison.Ordinal);
			var classic = yaml.IndexOf("\tUnitBuilderBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(generic, Is.GreaterThan(0));
			Assert.That(classic, Is.GreaterThan(generic));
			Assert.That(yaml[generic..classic], Does.Contain("\t\tUnitGatesUp: true\n"));
			Assert.That(Count(yaml, "UnitGatesUp:"), Is.EqualTo(1), "@generic only; no other block carries the row");
			Assert.That(yaml[classic..], Does.Not.Contain("UnitGatesUp"), "classic keeps its old unit pick");
		}

		[Test]
		public void TheRowFiltersTheRollsLegalSetSoItNeedsTheRollAndTheLatch()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.GatesUp.cs");
			Assert.That(part, Does.Contain("AblationLatch ablateGatesUp;"));
			Assert.That(part, Does.Contain("bool GatesUpOn => UnitRollOn && Info.UnitGatesUp && !ablateGatesUp.Off(player, AblationIds.UbGatesUp);"));
		}

		[Test]
		public void TheFillHandsTheBatchOverBeforeTheRollAndOnlyWhileTheRowIsOn()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var admit = unit.IndexOf("rollAdmit = AdmitOneOn ?", StringComparison.Ordinal);
			var hand = unit.IndexOf("rollGateBatch = GatesUpOn ? batch : null;", StringComparison.Ordinal);
			var call = unit.IndexOf("var unit = roll ? ChooseRollUnit(queue, skip)", StringComparison.Ordinal);
			Assert.That(admit, Is.GreaterThan(0));
			Assert.That(hand, Is.GreaterThan(admit));
			Assert.That(call, Is.GreaterThan(hand), "before the roll");
			Assert.That(Count(unit, "rollGateBatch ="), Is.EqualTo(1), "one writer, on the fill's own line; off = null = R1 runs no batch gate");
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.GatesUp.cs"), Does.Not.Contain("rollGateBatch ="), "the partial only reads the handed-over batch");
		}

		[Test]
		public void TheLegalSetAsksTheBatchGatesAfterItsOwnFiltersAndBeforeItAddsTheCandidate()
		{
			var body = RollBody(RepoFile(Bots + "UnitBuilderBotModuleCA.cs"));
			var reject = body.IndexOf("var rejectedBy = ShouldBuildReject(a.Name, true, queue.Info.Type);", StringComparison.Ordinal);
			var aircraft = body.IndexOf("if (rollOrdered.Contains(a.Name) && !CanBuildMoreOfAircraft(a))", StringComparison.Ordinal);
			var gates = body.IndexOf("if (GatesUpRemoves(a))\n\t\t\t\t\tcontinue;", StringComparison.Ordinal);
			var add = body.IndexOf("items.Add(a);", StringComparison.Ordinal);
			Assert.That(reject, Is.GreaterThan(0), "the cash-free share/delay/interval/limit/capturer/item-cap gate is already in R1");
			Assert.That(aircraft, Is.GreaterThan(reject));
			Assert.That(gates, Is.GreaterThan(aircraft), "the batch gates run last, on candidates R1's own filters let through");
			Assert.That(add, Is.GreaterThan(gates));
			Assert.That(Count(body, "GatesUpRemoves("), Is.EqualTo(1));
			Assert.That(body.IndexOf("rollGated.Clear();", StringComparison.Ordinal), Is.GreaterThan(body.IndexOf("rollExcluded.Clear();", StringComparison.Ordinal)).And.LessThan(reject), "cleared once per roll, before the loop");
			Assert.That(body, Does.Contain("GatesUpLog(queue.Info.Type);"), "one DEV row per roll, after the loop");
		}

		[Test]
		public void AnEmptyLegalSetThatTheRowEmptiedCountsAsRefusedSoTheCompositionIsNotReverted()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var body = RollBody(unit);
			var emptyLog = body.IndexOf("RollEmptyLog(queue.Info.Type, rollExcluded.Count > 0 ? \"all_excluded\" : \"none_listed_buildable\");", StringComparison.Ordinal);
			var mark = body.IndexOf("GatesUpMarkRefused(skip);\n\t\t\t\treturn null;", StringComparison.Ordinal);
			Assert.That(emptyLog, Is.GreaterThan(0));
			Assert.That(mark, Is.GreaterThan(emptyLog), "after the empty row, in the empty-pool branch only");
			Assert.That(Count(body, "GatesUpMarkRefused("), Is.EqualTo(1));

			// F1 (the fill): only a genuinely empty pick (skip empty) may revert the composition; the mark makes the gated names refused ones
			Assert.That(unit, Does.Contain("if (skip.Count == 0 && activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type))\n\t\t\t\t\t\t\tRevertToBaselineComposition();"));
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.GatesUp.cs");
			Assert.That(part, Does.Contain("skip.Add(name);"));
			Assert.That(Count(part, "skip."), Is.EqualTo(1), "the only touch of the skip set");
		}

		[Test]
		public void TheGatesAreTheFillsThreeBatchGatesInTheFillsOrderWithTheFillsInputs()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.GatesUp.cs");

			// the fill keeps its post-draw gates (they find nothing to refuse once the row is on)
			var fillHeadroom = unit.IndexOf("if (!LimitHeadroomAdmits(batch, name))", StringComparison.Ordinal);
			var fillCapturer = unit.IndexOf("if (!CapturerCapAdmitsWithPending(unit, batch))", StringComparison.Ordinal);
			var fillAircraft = unit.IndexOf("if (!(roll && rollFirst) && !CanBuildMoreOfAircraft(unit, aircraftPending))", StringComparison.Ordinal);
			Assert.That(fillHeadroom, Is.GreaterThan(0));
			Assert.That(fillCapturer, Is.GreaterThan(fillHeadroom));
			Assert.That(fillAircraft, Is.GreaterThan(fillCapturer));
			Assert.That(unit, Does.Contain("UnitProducerServiceCA.PendingAircraftDelta(isAircraft, isAirToAir,\n\t\t\t\t\t\tInfo.MaintainAirSuperiority, batch);"));

			// R1: the same functions, the same order, the same inputs, and the same first-build waiver
			var headroom = part.IndexOf("if (!LimitHeadroomAdmits(batch, candidate.Name))\n\t\t\t\treturn \"headroom\";", StringComparison.Ordinal);
			var capturer = part.IndexOf("if (!CapturerCapAdmitsWithPending(candidate, batch))\n\t\t\t\treturn \"capturer\";", StringComparison.Ordinal);
			var waiver = part.IndexOf("if (rollOrdered.Contains(candidate.Name))", StringComparison.Ordinal);
			var delta = part.IndexOf("UnitProducerServiceCA.PendingAircraftDelta(isAircraft, isAirToAir, Info.MaintainAirSuperiority, batch);", StringComparison.Ordinal);
			var cap = part.IndexOf("if (pending != 0 && !CanBuildMoreOfAircraft(candidate, pending))\n\t\t\t\t\treturn \"aircraft\";", StringComparison.Ordinal);
			Assert.That(headroom, Is.GreaterThan(0));
			Assert.That(capturer, Is.GreaterThan(headroom));
			Assert.That(waiver, Is.GreaterThan(capturer), "the aircraft caps never block the first build of a type");
			Assert.That(delta, Is.GreaterThan(waiver));
			Assert.That(cap, Is.GreaterThan(delta));
			Assert.That(part, Does.Contain("var isAirToAir = isAircraft && Info.AirToAirUnits.Contains(candidate.Name);"));
			Assert.That(part, Does.Contain("if (batch == null)\n\t\t\t\treturn null;"), "no batch = no gate");

			// the reason names are the fill's own, so a refused row and an excluded row add up in the scoring
			foreach (var why in new[] { "headroom", "capturer", "aircraft" })
				Assert.That(unit, Does.Contain("rejectLog.Reject(queue.Info.Type, \"" + why + "\");"), why);
		}

		[Test]
		public void TheRowOnlyReadsAndChangesNoOrderQueueDrawOrBatchState()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.GatesUp.cs");
			foreach (var banned in new[] { "QueueOrder", "Order.", "StartProduction", ".Reserve(", "SpendLimitHeadroom", "rollOrdered.Add", "RollDraw", "rng.", "PendingCapturers++", "PendingAircraft++", "counters." })
				Assert.That(part, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void TheDevRowIsCompiledOutOfRelease()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.GatesUp.cs");
			Assert.That(part, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid GatesUpLog(string queueType)"));
			Assert.That(part, Does.Contain(",ub_gates_up,,{queueType},{rollGated.Count};gated={string.Join(\"|\", names)}"));
			Assert.That(part, Does.Contain("world.WorldTick - last < 250"), "rate limited");
		}

		// The pick that the post-draw gate refuses is redrawn over the rest, so when the floor does not bind the result is the same distribution; when the floor binds, the mean (and so the floor) of the pool changes.
		[Test]
		public void TheFloorlessPoolGivesTheSameDistributionAndABindingFloorDoesNot()
		{
			// types: 0 and 1 normal, 2 a tiny fraction; type 0 is the one a batch gate refuses
			var fractions = new[] { 100, 100, 1 };
			var bias = new[] { 100, 100, 100 };
			var counter = new[] { 100, 100, 100 };

			double[] Pool(int floorPct, params int[] keep)
			{
				var w = UnitRoll.Weights(keep.Select(i => fractions[i]).ToArray(), keep.Select(i => bias[i]).ToArray(), keep.Select(i => counter[i]).ToArray(), floorPct);
				return w;
			}

			double OldShare(int type, int floorPct)
			{
				// draw over all three; if the refused type 0 is drawn, redraw over the rest (the refused type has left the pool)
				var all = Pool(floorPct, 0, 1, 2);
				var rest = Pool(floorPct, 1, 2);
				var idx = type - 1;
				var direct = all[type] / all.Sum();
				var viaRedraw = all[0] / all.Sum() * rest[idx] / rest.Sum();
				return direct + viaRedraw;
			}

			double NewShare(int type, int floorPct) => Pool(floorPct, 1, 2)[type - 1] / Pool(floorPct, 1, 2).Sum();

			// no floor: removing a type before the draw and redrawing after it are the same distribution
			Assert.That(NewShare(1, 0), Is.EqualTo(OldShare(1, 0)).Within(1e-12));
			Assert.That(NewShare(2, 0), Is.EqualTo(OldShare(2, 0)).Within(1e-12));

			// a binding floor: the refused type inflated the mean, so the tiny type was floored higher; the row changes the distribution
			Assert.That(NewShare(2, 50), Is.Not.EqualTo(OldShare(2, 50)).Within(1e-6), "NOT distribution-equal with a binding floor");
			Assert.That(OldShare(2, 50), Is.GreaterThan(NewShare(2, 50)), "the tiny type was favoured by the refused type's weight in the mean");
			Assert.That(OldShare(1, 50) + OldShare(2, 50), Is.EqualTo(1).Within(1e-12));
			Assert.That(NewShare(1, 50) + NewShare(2, 50), Is.EqualTo(1).Within(1e-12));
		}

		[Test]
		public void ARefusedTypeNoLongerSetsTheLeansDearestPrice()
		{
			// old: the refused 1500 type is in the first roll's pool, the re-roll keeps that top (the refused pick has left the pool)
			var oldFirst = TechLean.PoolMax(0, 0, 1500);
			var oldReroll = TechLean.PoolMax(1, oldFirst, 900);
			Assert.That(oldReroll, Is.EqualTo(1500));

			// new: it was never in the pool, so the first roll's top is the dearest type that can be built
			Assert.That(TechLean.PoolMax(0, 0, 900), Is.EqualTo(900));
		}
	}
}
