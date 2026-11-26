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
using OpenRA.Support;

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
			var draw = new UnitPickArbiter.DrawTrace(7, 3, 1, 42, "Vehicle", 2, "a:3|b:5", 8, 123456, 99, "b", true, 900, 1500, 800, "c:limit", "ground|air", 2, "deadbeef", "b@4000", UnitPickArbiter.RuleDraw);
			Assert.That(UnitPickArbiter.DrawDetail(draw),
				Is.EqualTo("ord=7;fill=3;att=1;prod=42;n=2;bins=a:3|b:5;bound=8;raw=123456;tot=99;first=1;pmb=900;pm=1500;bank=800;excl=c:limit;roles=ground|air;avail=2/deadbeef;availnew=b@4000;rule=R3_draw"));

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

		// The old inline admission of the fill loop (before step a4), kept here as the reference the pure function must equal.
		static (int Cost, bool Paygo, bool Leaned) OldAdmission(bool roll, bool first, int bankLeft, int fullCost, bool leanOn, int poolMax, int held, int tick, int start, int top, int bank)
		{
			var paygo = roll && first && UnitRoll.FirstAdmits(bankLeft);
			var cost = paygo ? 0 : fullCost;
			var leaned = roll && !paygo && leanOn && TechLean.Admits(fullCost, poolMax, held, tick, start, bankLeft, top, bank);
			if (leaned)
				cost = 0;

			return (cost, paygo, leaned);
		}

		[Test]
		public void AdmitEqualsTheOldInlineAdmissionOverTheWholeInputGrid()
		{
			var checkedCount = 0;
			foreach (var roll in new[] { false, true })
			foreach (var first in new[] { false, true })
			foreach (var bankLeft in new[] { -5, 0, 1, 40, 150, 10_000 })
			foreach (var fullCost in new[] { 0, 100, 500, 1500 })
			foreach (var leanOn in new[] { false, true })
			foreach (var poolMax in new[] { 0, 500, 1500 })
			foreach (var held in new[] { 0, 1 })
			foreach (var tick in new[] { 0, 6000 })
			foreach (var top in new[] { 0, 50, 100 })
			foreach (var bankPct in new[] { 0, 30, 100 })
			{
				var admission = UnitPickArbiter.Admit(roll, first, bankLeft, fullCost, leanOn, poolMax, held, tick, 3000, top, bankPct);
				var old = OldAdmission(roll, first, bankLeft, fullCost, leanOn, poolMax, held, tick, 3000, top, bankPct);
				Assert.That((admission.GateCost, admission.Paygo, admission.Leaned), Is.EqualTo(old), $"roll={roll} first={first} bank={bankLeft} full={fullCost} lean={leanOn} max={poolMax} held={held} tick={tick} top={top} bankPct={bankPct}");
				Assert.That(admission.Rule, Is.EqualTo(UnitPickArbiter.AdmitRule(old.Paygo, old.Leaned)));
				checkedCount++;
			}

			Assert.That(checkedCount, Is.EqualTo(2 * 2 * 6 * 4 * 2 * 3 * 2 * 2 * 3 * 3));
		}

		[Test]
		public void AdmitOnlyEverLowersTheGateCostToZeroAndANonRollPickAlwaysPaysTheFullPrice()
		{
			var firstBuild = UnitPickArbiter.Admit(true, true, 1, 700, true, 700, 0, 9000, 3000, 50, 30);
			Assert.That((firstBuild.GateCost, firstBuild.Paygo, firstBuild.Leaned, firstBuild.Rule), Is.EqualTo((0, true, false, UnitPickArbiter.RuleAdmitPaygo)), "pay-as-you-go comes first and the lean is then not asked");

			var emptyBank = UnitPickArbiter.Admit(true, true, 0, 700, false, 700, 0, 9000, 3000, 50, 30);
			Assert.That((emptyBank.GateCost, emptyBank.Rule), Is.EqualTo((700, UnitPickArbiter.RuleAdmitFull)), "an empty bank still waits");

			var lean = UnitPickArbiter.Admit(true, false, 300, 700, true, 700, 0, 9000, 3000, 50, 30);
			Assert.That((lean.GateCost, lean.Leaned, lean.Rule), Is.EqualTo((0, true, UnitPickArbiter.RuleAdmitLean)), "a late top pick with 30 percent of its price in the bank on an empty queue");

			var floorPick = UnitPickArbiter.Admit(false, true, 10_000, 700, true, 700, 0, 9000, 3000, 50, 30);
			Assert.That((floorPick.GateCost, floorPick.Paygo, floorPick.Leaned), Is.EqualTo((700, false, false)), "a floor or random pick is never a roll pick: full price");

			var early = UnitPickArbiter.Admit(true, false, 300, 700, true, 700, 0, 100, 3000, 50, 30);
			Assert.That(early.GateCost, Is.EqualTo(700), "before the lean start tick");
		}

		[Test]
		public void TheTraceHooksAreDevOnlyAndOnlyReadTheRng()
		{
			var part = RepoFile(Bots + "UnitBuilderBotModuleCA.PickTrace.cs");
			var hooks = Count(part, "\t\tvoid PickTrace") - Count(part, "\t\tvoid PickTraceWrite(") - Count(part, "\t\tvoid PickTraceFillRow(");
			Assert.That(hooks, Is.EqualTo(8), "roll start, fill start, empty, draw, none, refuse, accept, fill end");
			Assert.That(Count(part, "[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid PickTrace"), Is.EqualTo(8), "every hook call is compiled out of release");
			Assert.That(Count(part, "if (!Info.UnitPickTrace)\n\t\t\t\treturn;"), Is.EqualTo(8), "and every hook is off while the switch is off");

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
			var pick = body.IndexOf("var pick = decision.Pick;", StringComparison.Ordinal); // P5 a3: the draw itself is the arbiter's one call
			var first = body.IndexOf("rollFirst = !rollOrdered.Contains(pool[pick].Name);", StringComparison.Ordinal);
			var log = body.IndexOf("RollPickLog(queue.Info.Type, pool, weights, pick,", StringComparison.Ordinal);
			var trace = body.IndexOf("PickTraceDraw(queue, pool, units, pick, skip.Count, queueRoles);", StringComparison.Ordinal);
			var ret = body.IndexOf("return pool[pick];", StringComparison.Ordinal);
			Assert.That(pick, Is.GreaterThan(0));
			Assert.That(first, Is.GreaterThan(pick));
			Assert.That(log, Is.GreaterThan(first));
			Assert.That(trace, Is.GreaterThan(log), "the draw row reads the final first-copy flag, after the pick");
			Assert.That(ret, Is.GreaterThan(trace));

			// the roll-start hook captures the pool max and the known names BEFORE the roll writes either
			var rollStart = body.IndexOf("rollFirst = false;\n\t\t\tPickTraceRollStart();\n\t\t\tvar table = Info.UnitsToBuild;", StringComparison.Ordinal);
			Assert.That(rollStart, Is.GreaterThan(0));
			Assert.That(rollStart, Is.LessThan(body.IndexOf("rollAvailableSince.TryAdd(", StringComparison.Ordinal)));
			Assert.That(rollStart, Is.LessThan(body.IndexOf("rollPoolMaxCost = TechLean.PoolMax(", StringComparison.Ordinal)));

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

		// P5 a3: the R2/R3 steps moved into the arbiter; the old inline steps are replayed here on the same seed, so the picks and the RNG draw count must be identical.
		[Test]
		public void DecideMakesTheSamePicksAndTheSameRngCallsAsTheOldInlineSteps()
		{
			var fractions = new[] { 5, 3, 1, 8 };
			var biases = new[] { 100, 150, 80, 100 };
			var counter = new[] { 100, 100, 120, 100 };
			var boosts = new[] { 1.0, 1.0, 8.0, 1.0 };
			var oldRng = new MersenneTwister(100775);
			var newRng = new MersenneTwister(100775);
			for (var i = 0; i < 500; i++)
			{
				var units = UnitRoll.Quantize(UnitRoll.Weights(fractions, biases, counter, 5, boosts));
				var oldPick = UnitRoll.Pick(units, oldRng.Next(UnitRoll.Sum(units)));
				var decision = UnitPickArbiter.Decide(fractions, biases, counter, 5, boosts, null, newRng.Next);
				Assert.That(decision.Pick, Is.EqualTo(oldPick), "draw " + i);
				Assert.That(decision.Bound, Is.EqualTo(UnitRoll.Sum(units)));
				Assert.That(decision.Units, Is.EqualTo(units));
				Assert.That(newRng.TotalCount, Is.EqualTo(oldRng.TotalCount), "exactly one RNG call per roll, as before");
				Assert.That(newRng.Last, Is.EqualTo(oldRng.Last));
			}
		}

		[Test]
		public void TheCoreLiftIsTheLastStepOnTheWeightsAndTheDrawIsCalledOnceWithTheBound()
		{
			var fractions = new[] { 5, 3, 1 };
			var biases = new[] { 100, 100, 100 };
			var counter = new[] { 100, 100, 100 };
			var plain = UnitRoll.Weights(fractions, biases, counter, 5, null);
			double[] seen = null;
			var lifts = 0;
			var draws = 0;
			var bound = -1;
			var decision = UnitPickArbiter.Decide(fractions, biases, counter, 5, null,
				w =>
				{
					lifts++;
					seen = (double[])w.Clone();
					var lifted = (double[])w.Clone();
					lifted[2] *= 100;
					return lifted;
				},
				n =>
				{
					draws++;
					bound = n;
					return n - 1; // the last draw: the last candidate, which owns the end of the interval
				});

			Assert.That(seen, Is.EqualTo(plain), "the lift sees the roll's own weights (floor applied), nothing else came before it");
			Assert.That(lifts, Is.EqualTo(1));
			Assert.That(draws, Is.EqualTo(1));
			Assert.That(bound, Is.EqualTo(UnitRoll.Sum(decision.Units)));
			Assert.That(decision.Weights[2], Is.EqualTo(plain[2] * 100), "the draw used the lifted weights");
			Assert.That(decision.Pick, Is.EqualTo(2));
			Assert.That(decision.Rule, Is.EqualTo(UnitPickArbiter.RuleDraw));
		}

		[Test]
		public void ComposeBiasMultipliesThePresentTermsInTheFixedOrderAndSkipsTheAbsentOnes()
		{
			Assert.That(UnitPickArbiter.ComposeBias(130, null, null, null, null, null), Is.EqualTo(130));
			var terms = new int?[] { null, 0, 50, 100, 250, 100_000 };
			foreach (var d in terms)
				foreach (var l in terms)
					foreach (var w in terms)
						foreach (var c in terms)
							foreach (var o in terms)
							{
								var old = 130;
								if (d.HasValue)
									old = UnitRoll.MulPct(old, d.Value);
								if (l.HasValue)
									old = UnitRoll.MulPct(old, l.Value);
								if (w.HasValue)
									old = UnitRoll.MulPct(old, w.Value);
								if (c.HasValue)
									old = UnitRoll.MulPct(old, c.Value);
								if (o.HasValue)
									old = UnitRoll.MulPct(old, o.Value);

								Assert.That(UnitPickArbiter.ComposeBias(130, d, l, w, c, o), Is.EqualTo(old), $"{d} {l} {w} {c} {o}");
							}

			// the module hands over the terms in that order and computes none of the products itself
			var u = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("biases[i] = UnitPickArbiter.ComposeBias(RollParameters.BiasPct(rollPersonality, role, world.WorldTick), deficitTerm, learnedTerm, waveMixTerm, compositionTerm, openingTerm);"));
			var start = u.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = u.Substring(start, u.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);
			Assert.That(body, Does.Not.Contain("UnitRoll.MulPct("), "the multiply steps are the arbiter's");
			Assert.That(body, Does.Not.Contain("UnitRoll.Quantize("));
			Assert.That(body, Does.Not.Contain("UnitRoll.Pick("));
			Assert.That(body, Does.Not.Contain(".Next("), "ChooseRollUnit holds no random call of its own: the draw is the arbiter's one call");
		}

		// P5 a2 (addendum 1, user ruling 11-26): the tech-up demand multipliers are named table inputs; the prior = the Hans / yaml values, so phase (a) equality holds (N2: priors only, map and stage factor 1).
		[Test]
		public void TheCoreFocusAndTechLeanKeysDefaultToTheInfoDefaultsAndTheYamlValuesArePassedThrough()
		{
			// no arguments = the Info defaults (CoreFocusSharePct 0, Count 3, MaxPct 800, BankPct 30; TechLeanTopPct 0, BankPct 30)
			var t = new UnitRollTable(null, null, null, null, 5, 0);
			Assert.That((t.CoreFocusSharePct, t.CoreFocusCount, t.CoreFocusMaxPct, t.CoreFocusBankPct, t.TechLeanTopPct, t.TechLeanBankPct), Is.EqualTo((0, 3, 800, 30, 0, 30)));

			// the shipped Hans values: core share 50, count 3, cap x8, bank 30; lean 50 / 30
			var hans = new UnitRollTable(null, null, null, null, 5, 0, 8, 3000, 64, 50, 3, 800, 30, 50, 30);
			Assert.That((hans.CoreFocusSharePct, hans.CoreFocusCount, hans.CoreFocusMaxPct, hans.CoreFocusBankPct, hans.TechLeanTopPct, hans.TechLeanBankPct), Is.EqualTo((50, 3, 800, 30, 50, 30)));
			Assert.That(hans.FirstBoost, Is.EqualTo(8), "the older arguments keep their meaning");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			foreach (var line in new[] { "\t\tCoreFocusSharePct: 50\n", "\t\tCoreFocusCount: 3\n", "\t\tCoreFocusMaxPct: 800\n", "\t\tCoreFocusBankPct: 30\n", "\t\tTechLeanTopPct: 50\n", "\t\tTechLeanBankPct: 30\n" })
				Assert.That(yaml, Does.Contain(line), "the prior values this test pins are the @generic yaml values: " + line.Trim());
		}

		[Test]
		public void ALearnerOverrideReplacesAPriorAndClearingRestoresIt()
		{
			var t = new UnitRollTable(null, null, null, null, 5, 0, 8, 3000, 64, 50, 3, 800, 30, 50, 30);
			foreach (var (key, prior) in new[] { (UnitRollTable.CoreShareKey, 50), (UnitRollTable.CoreCountKey, 3), (UnitRollTable.CoreMaxKey, 800), (UnitRollTable.CoreBankKey, 30), (UnitRollTable.TechTopKey, 50), (UnitRollTable.TechBankKey, 30) })
			{
				t.Override(key, prior + 7);
				Assert.That(t.Overrides[key], Is.EqualTo(prior + 7), key);
				t.ClearOverride(key);
				Assert.That(t.Overrides.ContainsKey(key), Is.False, key);
			}

			t.Override(UnitRollTable.CoreShareKey, -9);
			Assert.That(t.CoreFocusSharePct, Is.EqualTo(0), "a bad learned value is clamped like every other key");
			t.Override(UnitRollTable.TechTopKey, 70);
			Assert.That(t.TechLeanTopPct, Is.EqualTo(70));
			t.ClearOverrides();
			Assert.That((t.CoreFocusSharePct, t.TechLeanTopPct), Is.EqualTo((50, 50)));
			Assert.That((UnitRollTable.CoreShareKey, UnitRollTable.CoreCountKey, UnitRollTable.CoreMaxKey, UnitRollTable.CoreBankKey, UnitRollTable.TechTopKey, UnitRollTable.TechBankKey),
				Is.EqualTo(("corefocus.share", "corefocus.count", "corefocus.max", "corefocus.bank", "techlean.top", "techlean.bank")), "the key names are the learner's stable contract (addendum 1)");
		}

		// P5 (b), addendum 3 item 6 (N3, Haiku): a table multiplier (the core lift, the only one the table feeds into the weights) may never touch a type that was not issued yet: its weight after the table step equals its weight
		// before it, so the first-build pull of a never-issued type and the mean floor stay as the roll made them. It also never lowers any weight (a lift only), and stays inside its cap, for every corefocus.* value.
		[Test]
		public void TheTableStepNeverChangesAnUnissuedWeightNorLowersAnyWeightForEveryCoreFocusValue()
		{
			var weights = new[] { 40.0, 10.0, 25.0, 1.0, 900.0, 5.0 };
			var settled = new[] { true, true, false, true, true, false };
			var prices = new[] { 100, 300, 500, 700, 1200, 60 };
			var steps = 0;
			foreach (var share in new[] { -5, 0, 30, 50, 99, 400 })
			foreach (var max in new[] { 0, 100, 250, 800, 1_000_000 })
			foreach (var count in new[] { 0, 1, 3, 9 })
			foreach (var bankPct in new[] { 0, 30, 100 })
			foreach (var bank in new[] { 0, 90, 400, 5000 })
			{
				var eligible = settled.Select((s, i) => s && CoreFocus.Affordable(prices[i], bank, bankPct)).ToArray();
				var core = new bool[weights.Length];
				foreach (var i in CoreFocus.Core(prices, eligible, count))
					core[i] = true;

				var multiplier = CoreFocus.MultiplierPct(weights, settled, core, share, max);
				var after = CoreFocus.Apply(weights, core, multiplier);
				var label = $"share={share} max={max} count={count} bankPct={bankPct} bank={bank}";
				Assert.That(multiplier, Is.InRange(100, Math.Max(100, max)), label);
				for (var i = 0; i < weights.Length; i++)
				{
					if (!settled[i])
						Assert.That(after[i], Is.EqualTo(weights[i]), "an unissued type keeps its weight: " + label + " i=" + i);

					if (!core[i])
						Assert.That(after[i], Is.EqualTo(weights[i]), "only the core moves: " + label + " i=" + i);

					Assert.That(after[i], Is.GreaterThanOrEqualTo(weights[i]), "a lift never lowers a weight: " + label + " i=" + i);
				}

				steps++;
			}

			Assert.That(steps, Is.EqualTo(6 * 5 * 4 * 3 * 4));
		}

		// P5 (b), addendum 3 item 3 (row ub_ramp_split as a table key, no switch): the lean's late start is its own key with the ramp as its prior.
		[Test]
		public void TheTechLeanStartIsTheRampUnlessALearnerSetsItAndTheBiasRampIsUntouched()
		{
			Assert.That(UnitRollTable.TechStartKey, Is.EqualTo("techlean.start"), "the key name is the learner's stable contract");
			var bias = new Dictionary<string, int> { ["p.role"] = 100 };
			var late = new Dictionary<string, int> { ["p.role"] = 200 };
			var t = new UnitRollTable(null, bias, late, null, 5, 1000, 8, 3000, 64, 50, 3, 800, 30, 50, 30);

			Assert.That(t.TechLeanStartTicks, Is.EqualTo(1000), "prior = the ramp: the same number, so behaviour is equal to before the split");
			t.Override(UnitRollTable.RampKey, 4000);
			Assert.That(t.TechLeanStartTicks, Is.EqualTo(4000), "a ramp override still moves the lean start, exactly as the single shared number did");

			t.Override(UnitRollTable.TechStartKey, 2500);
			Assert.That((t.TechLeanStartTicks, t.RampTicks), Is.EqualTo((2500, 4000)), "a learner can now separate the two");
			Assert.That(t.BiasPct("p", "role", 2000), Is.EqualTo(150), "the personality bias ramp reads the ramp only (100 -> 200 over 4000 ticks, halfway at 2000)");

			t.ClearOverride(UnitRollTable.TechStartKey);
			Assert.That(t.TechLeanStartTicks, Is.EqualTo(4000), "clearing restores the prior");
			t.Override(UnitRollTable.TechStartKey, -4);
			Assert.That(t.TechLeanStartTicks, Is.EqualTo(0), "clamped like every key");
		}

		[Test]
		public void CoreFocusIsReadByTheWeightsStepAndTechLeanByTheAdmissionStepOnly()
		{
			var u = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var core = RepoFile(Bots + "UnitBuilderBotModuleCA.CoreFocus.cs");

			// the module builds the table from the Info values (the prior)
			Assert.That(u, Does.Contain("Info.CoreFocusSharePct, Info.CoreFocusCount, Info.CoreFocusMaxPct, Info.CoreFocusBankPct, Info.TechLeanTopPct, Info.TechLeanBankPct);"));

			// R2: the core lift reads corefocus.* through the table, in the CoreFocus partial only
			Assert.That(core, Does.Contain("RollParameters.CoreFocusBankPct"));
			Assert.That(core, Does.Contain("RollParameters.CoreFocusCount"));
			Assert.That(core, Does.Contain("RollParameters.CoreFocusSharePct, RollParameters.CoreFocusMaxPct"));
			Assert.That(core, Does.Not.Contain("RollParameters.TechLean"), "N1: techlean.* is not a weight input");
			Assert.That(u, Does.Not.Contain("RollParameters.CoreFocus"), "N1: corefocus.* is read by the core lift only");

			// R4: the lean predicate reads techlean.* through the table, on the admission line only
			Assert.That(Count(u, "RollParameters.TechLeanTopPct"), Is.EqualTo(1));
			Assert.That(Count(u, "RollParameters.TechLeanBankPct"), Is.EqualTo(1));
			Assert.That(Count(u, "RollParameters.TechLeanStartTicks"), Is.EqualTo(1), "techlean.start is read on the admission line only (the ramp of the personality bias stays in the table's BiasPct)");
			Assert.That(u, Does.Contain("UnitPickArbiter.Admit(roll, rollFirst, batch.BankLeft, fullCost, TechLeanOn, rollPoolMaxCost, held, world.WorldTick, RollParameters.TechLeanStartTicks, RollParameters.TechLeanTopPct, RollParameters.TechLeanBankPct)"));
			Assert.That(u, Does.Not.Contain("RollParameters.RampTicks"), "the lean no longer reads the ramp directly");

			// the row switches stay on the yaml (a learned 0 cannot switch the row off), and no other read of the six Info values is left
			Assert.That(core, Does.Contain("bool CoreFocusOn => UnitRollOn && Info.CoreFocusSharePct > 0"));
			Assert.That(u, Does.Contain("bool TechLeanOn => UnitRollOn && Info.TechLeanTopPct > 0"));
			Assert.That(core, Does.Not.Contain("Info.CoreFocusCount"));
			Assert.That(core, Does.Not.Contain("Info.CoreFocusMaxPct"));
			Assert.That(core, Does.Not.Contain("Info.CoreFocusBankPct"));
			Assert.That(u, Does.Not.Contain("Info.TechLeanBankPct,"), "only the table construction passes it on, as the last argument");
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
