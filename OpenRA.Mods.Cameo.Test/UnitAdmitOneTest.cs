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
	/// CONSOLIDATE-03 P5 (b), row ub_admit_one (design 02 contradiction 1, addendum 3 item 4). The core focus names the types it lifts with its own bank bar; the fill's admission can still refuse a named type and the
	/// roll re-rolls to a cheaper unit. With the row on the core reads the same admission forecast, so a type the gates would refuse right now is not eligible and the next-dearest admissible type takes its place.
	/// The forecast is the pure UnitPickArbiter.Admit plus the same three gate terms as the fill loop; the row only narrows the eligible set and changes no order, queue or draw by itself.
	/// </summary>
	[TestFixture]
	public class UnitAdmitOneTest
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
		public void TheRowHasItsOwnAblationIdInItsOwnFile()
		{
			Assert.That(AblationIds.UbAdmitOne, Is.EqualTo("ub_admit_one"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.UbAdmitOne.cs"), Does.Contain("public const string UbAdmitOne = \"ub_admit_one\";"));
			Assert.That(System.Text.RegularExpressions.Regex.IsMatch(AblationIds.UbAdmitOne, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");
		}

		[Test]
		public void TheSwitchIsOffInCodeOnInTheGenericBlockAndAbsentFromClassic()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("public readonly bool UnitAdmitOne = false;"), "off in code: only the yaml arms it");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var generic = yaml.IndexOf("\tUnitBuilderBotModuleCA@generic:", StringComparison.Ordinal);
			var classic = yaml.IndexOf("\tUnitBuilderBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(generic, Is.GreaterThan(0));
			Assert.That(classic, Is.GreaterThan(generic));
			Assert.That(yaml[generic..classic], Does.Contain("\t\tUnitAdmitOne: true\n"));
			Assert.That(Count(yaml, "UnitAdmitOne:"), Is.EqualTo(1), "@generic only; no other block carries the row");
			Assert.That(yaml[classic..], Does.Not.Contain("UnitAdmitOne"), "classic keeps its old unit pick");
		}

		[Test]
		public void TheRowNeedsTheCoreFocusAndTheAblationLatch()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.AdmitOne.cs");
			Assert.That(part, Does.Contain("AblationLatch ablateAdmitOne;"));
			Assert.That(part, Does.Contain("bool AdmitOneOn => CoreFocusOn && Info.UnitAdmitOne && !ablateAdmitOne.Off(player, AblationIds.UbAdmitOne);"), "it narrows the core, so it needs the core focus");
		}

		[Test]
		public void TheFillHandsTheForecastInputsOverBeforeTheRollAndOnlyWhileTheRowIsOn()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var bank = unit.IndexOf("rollBankLeft = CoreFocusOn ? batch.BankLeft : 0;", StringComparison.Ordinal);
			var hand = unit.IndexOf("rollAdmit = AdmitOneOn ? new RollAdmitInputs(batch, held, depthQueue, unpaidQueued) : null;", StringComparison.Ordinal);
			var call = unit.IndexOf("var unit = roll ? ChooseRollUnit(queue, skip)", StringComparison.Ordinal);
			Assert.That(bank, Is.GreaterThan(0));
			Assert.That(hand, Is.GreaterThan(bank), "after the bank the core already reads");
			Assert.That(call, Is.GreaterThan(hand), "before the roll");
			Assert.That(Count(unit, "rollAdmit ="), Is.EqualTo(1), "one writer, on the fill's own line: the one roll call site reads fresh inputs, off = null");
			Assert.That(Count(unit, "ChooseRollUnit("), Is.EqualTo(2), "the definition and the one fill call: no other path can read stale inputs");
		}

		[Test]
		public void TheForecastIsTheFillsAdmissionAndTheFillsThreeGateTerms()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.AdmitOne.cs");

			// the fill: ONE Admit call (a4) and then the three gate terms
			Assert.That(Count(unit, "UnitPickArbiter.Admit("), Is.EqualTo(2), "the fill and RollCashAdmits (ub_affordable_pool keep mask, V08H join)");
			Assert.That(unit, Does.Contain("if (!batch.Affords(cost) || !ReserveAdmits(batch, cost)\n\t\t\t\t\t\t|| depthQueue && held > 0 && !CashLedger.UnitDepthAdmits(batch.BankLeft, unpaidQueued, cost))"));

			// the forecast: the same pure function (a settled pick is a roll and never the first build) and the same three terms, with the same inputs
			Assert.That(Count(part, "UnitPickArbiter.Admit("), Is.EqualTo(1));
			Assert.That(part, Does.Contain("UnitPickArbiter.Admit(true, false, s.Batch.BankLeft, fullCost, TechLeanOn, rollPoolMaxCost, s.Held, world.WorldTick, RollParameters.TechLeanStartTicks, RollParameters.TechLeanTopPct, RollParameters.TechLeanBankPct)"));
			Assert.That(part, Does.Contain("s.Batch.Affords(admission.GateCost) && ReserveAdmits(s.Batch, admission.GateCost)"));
			Assert.That(part, Does.Contain("&& !(s.DepthQueue && s.Held > 0 && !QueueDepthCA.CashAdmits(s.Batch.BankLeft, s.Unpaid, admission.GateCost));"));
			Assert.That(part, Does.Contain("if (rollAdmit is not { } s)\n\t\t\t\treturn true;"), "no inputs = admitted: the core keeps its own bank bar alone");
		}

		[Test]
		public void TheForecastOnlyReadsAndChangesNoOrderQueueOrDraw()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.AdmitOne.cs");
			foreach (var banned in new[] { "QueueOrder", "Order.", "StartProduction", "batch.Reserve", "Batch.Reserve", "rollOrdered", "RollDraw", "rng.", "skip.", ".Add(name", "counters." })
				Assert.That(part, Does.Not.Contain(banned), banned);

			// the only writes are the per-roll inputs (the fill's line) and the DEV log's own rate limiter
			Assert.That(Count(part, "rollAdmit ="), Is.EqualTo(0), "the partial only reads the inputs; the fill writes them");
		}

		[Test]
		public void TheCoreNarrowsTheEligibleSetAfterTheOldBankBarAndNeverAddsToIt()
		{
			var core = RepoFile(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs");
			var old = core.IndexOf("eligible[i] = settled[i] && CoreFocus.Affordable(price[i], rollBankLeft, RollParameters.CoreFocusBankPct);", StringComparison.Ordinal);
			var narrow = core.IndexOf("if (rollAdmit != null && eligible[i] && !R4Forecast(price[i]))", StringComparison.Ordinal);
			var pick = core.IndexOf("var picked = CoreFocus.Core(price, eligible, RollParameters.CoreFocusCount);", StringComparison.Ordinal);
			Assert.That(old, Is.GreaterThan(0));
			Assert.That(narrow, Is.GreaterThan(old), "the old bar first, the forecast on top of it");
			Assert.That(pick, Is.GreaterThan(narrow));
			Assert.That(Count(core, "eligible[i] = true"), Is.EqualTo(0), "the row never makes a type eligible");
			Assert.That(core, Does.Contain("eligible[i] = false;"));
			Assert.That(core, Does.Not.Contain("R4Forecast(price[i]) ||"), "the forecast is only ever an extra AND on the old bar");
		}

		[Test]
		public void TheDevRowIsCompiledOutOfRelease()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.AdmitOne.cs");
			Assert.That(part, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid AdmitOneLog("));
			Assert.That(Count(part, "AdmitOneLog("), Is.EqualTo(1), "only the definition lives in the partial; the core pass calls it");
			Assert.That(RepoFile(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs"), Does.Contain("AdmitOneLog(queue.Info.Type, pool, admitOneRemoved);"));
		}

		// The eligible set is what the row changes; the core is the dearest few of it. A refused dearest type hands its place to the next-dearest admissible one, and the core never grows past what the old bar allowed.
		[Test]
		public void ARefusedDearTypeHandsItsPlaceToTheNextDearestAdmissibleOne()
		{
			var price = new[] { 1200, 900, 600, 300, 100 };
			var oldBar = new[] { true, true, true, true, true };

			var before = CoreFocus.Core(price, oldBar, 3);
			Assert.That(before, Is.EquivalentTo(new[] { 0, 1, 2 }));

			// the forecast refuses the 1200 type (cannot afford it right now): it leaves the eligible set
			var narrowed = (bool[])oldBar.Clone();
			narrowed[0] = false;
			var after = CoreFocus.Core(price, narrowed, 3);
			Assert.That(after, Is.EquivalentTo(new[] { 1, 2, 3 }), "the next-dearest admissible type joins the core");
			Assert.That(after.All(i => oldBar[i]), Is.True, "and it was eligible under the old bar too");
			Assert.That(after, Does.Not.Contain(0), "the refused pick is not lifted");

			// every type refused: no core, the roll keeps its own weights
			Assert.That(CoreFocus.Core(price, new bool[5], 3), Is.Empty);
		}

		[Test]
		public void TheNarrowedSetIsASubsetOfTheOldEligibleSetForEveryMask()
		{
			var price = new[] { 800, 700, 500, 400, 250, 100 };
			for (var oldMask = 0; oldMask < 1 << price.Length; oldMask++)
			{
				var old = Enumerable.Range(0, price.Length).Select(i => (oldMask >> i & 1) == 1).ToArray();
				for (var refuseMask = 0; refuseMask < 1 << price.Length; refuseMask++)
				{
					var narrowed = Enumerable.Range(0, price.Length).Select(i => old[i] && (refuseMask >> i & 1) == 0).ToArray();
					var core = CoreFocus.Core(price, narrowed, 3);
					Assert.That(core.All(i => old[i]), Is.True, "oldMask " + oldMask + " refuse " + refuseMask);
					Assert.That(core.All(i => (refuseMask >> i & 1) == 0), Is.True, "a refused type is never in the core");
					Assert.That(core.Count, Is.EqualTo(Math.Min(3, narrowed.Count(e => e))));
				}
			}
		}
	}
}
