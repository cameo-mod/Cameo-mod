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
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PERF-BOTTICK-01 row ub_affordable_pool (Opus 11-26): the roll draws only among the members that pass the cash/afford check, with the weights of the whole pool; none passes = the
	// fill ends for the visit; off = identical. V08F4: about 150 rolls per started unit, nearly all refused for cash after the pick.
	[TestFixture]
	public sealed class UbAffordablePoolTest
	{
		const string Unit = "mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string RollBody(string u)
		{
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			return u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
		}

		[Test]
		public void TheKeptMembersCarryTheirWholePoolWeightsInPoolOrder()
		{
			var weights = new[] { 12.5, 3.0, 40.25, 7.0, 0.5 };
			var keep = new[] { true, false, true, false, true };
			var kept = AffordablePool.Count(keep);
			Assert.That(kept, Is.EqualTo(3));
			Assert.That(AffordablePool.Keep(weights, keep, kept), Is.EqualTo(new[] { 12.5, 40.25, 0.5 }), "the same weights, nothing renormalised, the pool order kept");
			Assert.That(AffordablePool.Keep(new[] { "a", "b", "c", "d", "e" }, keep, kept), Is.EqualTo(new[] { "a", "c", "e" }));

			// every member affordable: the same pool, the same weights, so the same units and the same draw
			var all = new[] { true, true, true, true, true };
			Assert.That(AffordablePool.Keep(weights, all, AffordablePool.Count(all)), Is.EqualTo(weights));
			Assert.That(UnitRoll.Quantize(AffordablePool.Keep(weights, all, 5)), Is.EqualTo(UnitRoll.Quantize(weights)));

			// none affordable: nothing to draw from
			var none = new bool[5];
			Assert.That(AffordablePool.Count(none), Is.Zero);
			Assert.That(AffordablePool.Keep(weights, none, 0), Is.Empty);
		}

		[Test]
		public void AnEmptyAffordablePoolEndsTheFillWithoutRevertingTheComposition()
		{
			var u = Source(Unit);
			var body = RollBody(u);
			Assert.That(body, Does.Contain("rollFirst = false;\n\t\t\trollNoneAffordable = false;"), "every roll starts clear");
			Assert.That(body, Does.Contain("if (kept == 0)\n\t\t\t\t{\n\t\t\t\t\trollNoneAffordable = true;\n\t\t\t\t\tRollEmptyLog(queue.Info.Type, \"none_affordable\");\n\t\t\t\t\treturn null;\n\t\t\t\t}"));

			// a null roll ends the queue's fill; "nothing affordable" is not an empty composition (the row-off path refuses each member, which leaves skip non-empty too)
			var rollPick = u.IndexOf("var unit = roll ? ChooseRollUnit(queue, skip)", StringComparison.Ordinal);
			var noCandidate = u.IndexOf("if (roll && unit == null)\n\t\t\t\t\t{", rollPick, StringComparison.Ordinal);
			var guard = u.IndexOf("if (skip.Count == 0 && !rollNoneAffordable && activeComposition != null && CompositionAppliesToCategory(activeComposition, queue.Info.Type))\n\t\t\t\t\t\t\tRevertToBaselineComposition();\n\n\t\t\t\t\t\tbreak;", noCandidate, StringComparison.Ordinal);
			Assert.That(noCandidate, Is.GreaterThan(rollPick));
			Assert.That(guard, Is.GreaterThan(noCandidate));
		}

		[Test]
		public void TheFilterUsesTheWholePoolWeightsAndTheSameCashTermsAsTheGateAfterThePick()
		{
			var u = Source(Unit);
			var body = RollBody(u);
			var weightsAt = body.IndexOf("var weights = UnitRoll.Weights(fractions, biases, counterPcts, RollParameters.FloorPct, boosts);", StringComparison.Ordinal);
			var filterAt = body.IndexOf("if (AffordablePoolOn && rollCash.Batch != null)", StringComparison.Ordinal);
			var quantizeAt = body.IndexOf("var units = UnitRoll.Quantize(weights);", StringComparison.Ordinal);
			Assert.That(weightsAt, Is.GreaterThan(0));
			Assert.That(filterAt, Is.GreaterThan(weightsAt), "the weights (floor, counter and share maxima) come from the whole pool");
			Assert.That(quantizeAt, Is.GreaterThan(filterAt), "the draw is over the kept members");
			foreach (var array in new[] { "pool", "weights", "fractions", "biases", "counterPcts", "boosts" })
				Assert.That(body, Does.Contain($"{array} = AffordablePool.Keep({array}, keep, kept)"), array + " is cut the same way, so the pick log stays aligned");

			// the filter's terms are the gate's terms: pay-as-you-go first build, tech lean, bank, reserve, depth cash
			Assert.That(u, Does.Contain("var paygo = roll && rollFirst && UnitRoll.FirstAdmits(batch.BankLeft);"));
			Assert.That(body, Does.Contain("rollFirst = !rollOrdered.Contains(pool[pick].Name);"));
			Assert.That(body, Does.Contain("var paygo = !rollOrdered.Contains(a.Name) && UnitRoll.FirstAdmits(batch.BankLeft);"));
			Assert.That(u, Does.Contain("var leaned = roll && !paygo && TechLeanOn && TechLean.Admits(fullCost, rollPoolMaxCost, held, world.WorldTick, RollParameters.RampTicks, batch.BankLeft, Info.TechLeanTopPct, Info.TechLeanBankPct);"));
			Assert.That(body, Does.Contain("if (!paygo && TechLeanOn && TechLean.Admits(fullCost, rollPoolMaxCost, rollCash.Held, world.WorldTick, RollParameters.RampTicks, batch.BankLeft, Info.TechLeanTopPct, Info.TechLeanBankPct))"));
			Assert.That(u, Does.Contain("if (!batch.Affords(cost) || !ReserveAdmits(batch, cost)\n\t\t\t\t\t\t|| depthQueue && held > 0 && !QueueDepthCA.CashAdmits(batch.BankLeft, unpaidQueued, cost))"));
			Assert.That(body, Does.Contain("return batch.Affords(cost) && ReserveAdmits(batch, cost)\n\t\t\t\t\t&& !(rollCash.DepthQueue && rollCash.Held > 0 && !QueueDepthCA.CashAdmits(batch.BankLeft, rollCash.UnpaidQueued, cost));"));
			Assert.That(u, Does.Contain("rollCash = new RollCash(batch, held, depthQueue, unpaidQueued);\n\t\t\t\t\tUbSubStart(0);"), "the cash state of this very pick");
		}

		[Test]
		public void OffInCodeAndUnarmedInTheYamlSoOffIsTodaysRoll()
		{
			var u = Source(Unit);
			Assert.That(AblationIds.UnitAffordablePool, Is.EqualTo("ub_affordable_pool"));
			Assert.That(u, Does.Contain("public readonly bool AffordablePool = false;"), "off in code");
			Assert.That(u, Does.Contain("bool AffordablePoolOn => UnitRollOn && Info.AffordablePool && !ablateAffordablePool.Off(player, AblationIds.UnitAffordablePool);"), "needs the roll, the switch and the row");
			Assert.That(Source("packaging/ai.yaml"), Does.Not.Contain("AffordablePool:"), "no yaml arms it in this commit");

			// off: the filter block is the only reader, and the composition guard reads a flag only the filter sets
			Assert.That(u.Split("AffordablePoolOn").Length - 1, Is.EqualTo(3), "the property, the filter and the DEV divergence log");
			Assert.That(u.Split("rollNoneAffordable = true;").Length - 1, Is.EqualTo(1));
		}
	}
}
