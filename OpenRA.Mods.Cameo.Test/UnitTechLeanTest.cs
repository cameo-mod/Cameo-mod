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
	/// TIER-MIX-01 (row ub_tech_lean). V08D 6v6: the unit roll rolled vehicles at 1033 growing to 1890 per unit while the vehicles built stayed at 900 to 990 (built/rolled 0.93 down to 0.50),
	/// because every unit order needs its FULL price in the bank and a refused pick is re-rolled among the cheaper rest (bank median 334 to 1164 from tick 5000). Only the first build of a type was
	/// pay-as-you-go. With the row on, a LATE pick from the TOP of its queue's pool, on an EMPTY queue, with a share of its price in the bank, gets the same admission. The weights are untouched.
	/// </summary>
	[TestFixture]
	public class UnitTechLeanTest
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

		[Test]
		public void AdmitsALateTopPickOnAnEmptyQueueWithAShareOfItsPriceInTheBank()
		{
			// The V08D vehicle case: pool top 1890, late tick, empty queue.
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 600, 50, 30), Is.True, "the pool's dearest type, 600 of 1850 in the bank (32%)");
			Assert.That(TechLean.Admits(1500, 1890, 0, 12000, 9000, 700, 50, 30), Is.True, "a 1500 pick of a 1890 pool: top half, 700 >= 30% of 1500");
			Assert.That(TechLean.Admits(1500, 1890, 0, 12000, 9000, 300, 50, 30), Is.False, "300 is below 30% of 1500 (450): still refused, falls to a cheaper unit as before");

			// The funding boundary: bankLeft x 100 >= cost x bankPct.
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 554, 50, 30), Is.False, "554 x 100 = 55400 < 1850 x 30 = 55500");
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 555, 50, 30), Is.True, "555 x 100 = 55500 >= 55500");

			// The top boundary: cost x 100 >= poolMax x topPct (925 is exactly half of 1850).
			Assert.That(TechLean.Admits(924, 1850, 0, 9000, 9000, 1000, 50, 30), Is.False);
			Assert.That(TechLean.Admits(925, 1850, 0, 9000, 9000, 1000, 50, 30), Is.True);
			Assert.That(TechLean.Admits(300, 1850, 0, 20000, 9000, 5000, 50, 30), Is.False, "a cheap pick of a dear pool is never lean, however rich the bank");

			// Late: the tick must have reached the start (UnitRollRampTicks).
			Assert.That(TechLean.Admits(1850, 1850, 0, 8999, 9000, 5000, 50, 30), Is.False);
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 5000, 50, 30), Is.True);
			Assert.That(TechLean.Admits(1850, 1850, 0, 0, 0, 5000, 50, 30), Is.True, "no ramp (start 0): late from the first tick");

			// One lean item per queue: the queue must hold nothing.
			Assert.That(TechLean.Admits(1850, 1850, 1, 9000, 9000, 5000, 50, 30), Is.False);
			Assert.That(TechLean.Admits(1850, 1850, 2, 9000, 9000, 5000, 50, 30), Is.False);

			// Nothing to admit: no price, no pool, no bank, row off.
			Assert.That(TechLean.Admits(0, 1850, 0, 9000, 9000, 5000, 50, 30), Is.False);
			Assert.That(TechLean.Admits(-5, 1850, 0, 9000, 9000, 5000, 50, 30), Is.False);
			Assert.That(TechLean.Admits(1850, 0, 0, 9000, 9000, 5000, 50, 30), Is.False);
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 0, 50, 30), Is.False, "an empty bank still waits (as FirstAdmits)");
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, -200, 50, 30), Is.False, "a bank in the red still waits");
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 5000, 0, 30), Is.False, "topPct 0 = row off");
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 5000, -10, 30), Is.False, "a negative topPct is off, not 'everything'");
		}

		[Test]
		public void BadYamlPercentsAreClampedNeverTurnedIntoNoFundingOrMoreThanTheWholePrice()
		{
			// topPct above 100 behaves as 100: only the dearest type of the pool.
			Assert.That(TechLean.Admits(1000, 1850, 0, 9000, 9000, 5000, 150, 30), Is.False);
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 5000, 150, 30), Is.True);

			// bankPct 0 or negative: any bank above 0 funds it (the same as the first-build rule); above 100 behaves as 100: the whole price.
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 1, 50, 0), Is.True);
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 1, 50, -10), Is.True);
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 1849, 50, 500), Is.False);
			Assert.That(TechLean.Admits(1850, 1850, 0, 9000, 9000, 1850, 50, 500), Is.True);

			// 64-bit math: the largest ints neither wrap nor throw.
			Assert.That(TechLean.Admits(int.MaxValue, int.MaxValue, 0, 9000, 9000, int.MaxValue, 100, 100), Is.True);
			Assert.That(TechLean.Admits(int.MaxValue, int.MaxValue, 0, int.MaxValue, 9000, int.MaxValue - 1, 100, 100), Is.False, "one short of the whole price");
		}

		[Test]
		public void ARefusedPickDoesNotLowerTheTopOfTheFillsPool()
		{
			Assert.That(TechLean.PoolMax(0, 999, 1500), Is.EqualTo(1500), "the first roll of a fill starts over, whatever the last fill left");
			Assert.That(TechLean.PoolMax(0, 0, 1850), Is.EqualTo(1850));
			Assert.That(TechLean.PoolMax(1, 1850, 1500), Is.EqualTo(1850), "after a refusal the pool lost the refused pick: the fill keeps its earlier top");
			Assert.That(TechLean.PoolMax(3, 1850, 2000), Is.EqualTo(2000), "a dearer type seen later still counts");
			Assert.That(TechLean.PoolMax(1, -5, 100), Is.EqualTo(100), "a bad previous value is clamped");
			Assert.That(TechLean.PoolMax(0, 0, -3), Is.EqualTo(0));

			// The point of it: the refused 1850 pick leaves the pool; the 1500 re-roll is judged against 1850, not against itself.
			var top = TechLean.PoolMax(1, 1850, 1500);
			Assert.That(TechLean.Admits(1500, top, 0, 12000, 9000, 700, 50, 30), Is.True, "still in the top half of the original pool");
			Assert.That(TechLean.Admits(800, top, 0, 12000, 9000, 700, 50, 30), Is.False, "a cheap re-roll of a pool whose top was refused is not lean");
		}

		[Test]
		public void TheRowHasItsIdItsInfoFieldsAndItsYamlKeysOnlyInTheGenericBlock()
		{
			Assert.That(AblationIds.UnitTechLean, Is.EqualTo("ub_tech_lean"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.UnitTechLean.cs"), Does.Contain("public const string UnitTechLean = \"ub_tech_lean\";"));

			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("public readonly int TechLeanTopPct = 0;"), "off in code: only the yaml arms it");
			Assert.That(unit, Does.Contain("public readonly int TechLeanBankPct = 30;"));
			Assert.That(unit, Does.Contain("AblationLatch ablateTechLean;"));
			Assert.That(unit, Does.Contain("bool TechLeanOn => UnitRollOn && Info.TechLeanTopPct > 0 && !ablateTechLean.Off(player, AblationIds.UnitTechLean);"), "needs the roll, an armed percent and the row");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var generic = yaml.IndexOf("\tUnitBuilderBotModuleCA@generic:", StringComparison.Ordinal);
			var classic = yaml.IndexOf("\tUnitBuilderBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(generic, Is.GreaterThan(0));
			Assert.That(classic, Is.GreaterThan(generic));
			var genericBlock = yaml[generic..classic];
			Assert.That(genericBlock, Does.Contain("\t\tTechLeanTopPct: 50\n"));
			Assert.That(genericBlock, Does.Contain("\t\tTechLeanBankPct: 30\n"));
			Assert.That(genericBlock, Does.Contain("\t\tUnitRoll: true\n"), "the lean rides on the unit roll of the same block");
			Assert.That(yaml[classic..], Does.Not.Contain("TechLean"), "classic keeps its old unit pick and its full-price gate");
		}

		[Test]
		public void OnlyTheAffordGateChangesAndTheFirstBuildPaygoAndTheFullReservationAreUntouched()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");

			// The first-build pay-as-you-go keeps its exact older text (UnitAll01Test pin), the lean line comes after it.
			const string Paygo = "var paygo = roll && rollFirst && UnitRoll.FirstAdmits(batch.BankLeft);\n\t\t\t\t\tvar cost = paygo ? 0 : fullCost;";
			Assert.That(unit, Does.Contain(Paygo));

			const string Leaned = "var leaned = roll && !paygo && TechLeanOn && TechLean.Admits(fullCost, rollPoolMaxCost, held, world.WorldTick, RollParameters.RampTicks, batch.BankLeft, RollParameters.TechLeanTopPct, RollParameters.TechLeanBankPct);\n\t\t\t\t\tif (leaned)\n\t\t\t\t\t\tcost = 0;";
			Assert.That(unit, Does.Contain(Leaned), "the lean only ever sets the gate cost to 0, for a ROLL pick (never a floor or random pick: rollPoolMaxCost is the last roll's) that is not already a first build (CM review of b73e77aa)");

			var paygoAt = unit.IndexOf(Paygo, StringComparison.Ordinal);
			var leanAt = unit.IndexOf(Leaned, StringComparison.Ordinal);
			var affordAt = unit.IndexOf("if (!batch.Affords(cost) || !ReserveAdmits(batch, cost)", StringComparison.Ordinal);
			var reserveAt = unit.IndexOf("batch.Reserve(queue, fullCost);", StringComparison.Ordinal);
			var logAt = unit.IndexOf("TechLeanLog(queue.Info.Type, name, fullCost, batch.BankLeft + fullCost, rollPoolMaxCost);", StringComparison.Ordinal);
			Assert.That(paygoAt, Is.GreaterThan(0));
			Assert.That(leanAt, Is.GreaterThan(paygoAt));
			Assert.That(affordAt, Is.GreaterThan(leanAt), "the lean is decided before the afford, reserve and depth gates read the cost");
			Assert.That(reserveAt, Is.GreaterThan(affordAt));
			Assert.That(logAt, Is.GreaterThan(reserveAt), "the DEV row is written only for an order that was started, after the full price is reserved");
			Assert.That(unit, Does.Contain("batch.Reserve(queue, fullCost);\n\t\t\t\t\tif (leaned)\n\t\t\t\t\t\tTechLeanLog("), "the order still reserves its FULL price");
			Assert.That(unit, Does.Contain("var held = queuedItems + batch.PendingOf(queue);"), "held = what the queue holds plus this pass's orders into it");

			// The weights are untouched: no lean term between the weights and the pick.
			// (P5 a3: the weights and the pick moved into the pick arbiter; the same check on its text, and the roll hands it the same arrays)
			var arbiter = RepoFile(Bots + "BotModuleLogic/UnitPickArbiter.cs");
			var weights = arbiter.IndexOf("var weights = UnitRoll.Weights(", StringComparison.Ordinal);
			var pick = arbiter.IndexOf("var pick = UnitRoll.Pick(", StringComparison.Ordinal);
			Assert.That(weights, Is.GreaterThan(0));
			Assert.That(pick, Is.GreaterThan(weights));
			Assert.That(arbiter.Substring(weights, pick - weights), Does.Not.Contain("TechLean"));
			Assert.That(unit, Does.Contain("UnitPickArbiter.Decide(fractions, biases, counterPcts, RollParameters.FloorPct, boosts, coreLift,"));
		}

		[Test]
		public void TheRollRecordsTheTopOfItsPoolOnlyWhileTheRowIsOnAndAfterThePoolIsFinal()
		{
			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			var start = unit.IndexOf("ActorInfo ChooseRollUnit(", StringComparison.Ordinal);
			var body = unit.Substring(start, unit.IndexOf("int rollDeficitTick", start, StringComparison.Ordinal) - start);

			var pool = body.IndexOf("var pool = items;", StringComparison.Ordinal);
			var lean = body.IndexOf("if (TechLeanOn)\n\t\t\t{\n\t\t\t\tvar poolMax = 0;", StringComparison.Ordinal);
			var strength = body.IndexOf("var strength = new int[UnitRoll.ProfileNames.Length];", StringComparison.Ordinal);
			Assert.That(pool, Is.GreaterThan(0));
			Assert.That(lean, Is.GreaterThan(pool), "after the pool is formed");
			Assert.That(strength, Is.GreaterThan(lean));
			Assert.That(body, Does.Contain("poolMax = Math.Max(poolMax, queue.GetProductionCost(a));"));
			Assert.That(body, Does.Contain("rollPoolMaxCost = TechLean.PoolMax(skip.Count, rollPoolMaxCost, poolMax);"), "skip.Count = what this fill has refused so far");

			// The older c3 pins on the same method still hold.
			Assert.That(body, Does.Contain("rollFirst = !rollOrdered.Contains(pool[pick].Name);"));
			Assert.That(body, Does.Not.Contain("savings"));
		}

		[Test]
		public void TheSeamIsWorldFreeAndTheDevRowIsCompiledOutOfRelease()
		{
			var seam = RepoFile(Bots + "BotModuleLogic/TechLean.cs");
			foreach (var banned in new[] { "Random", "World", "Actor", "Player", "FindActors" })
				Assert.That(seam, Does.Not.Contain(banned), banned);

			var unit = RepoFile(Bots + "UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid TechLeanLog(string queueType, string name, int price, int bankBefore, int poolMax) =>\n\t\t\tRollLog("));
			Assert.That(unit, Does.Contain(",ub_tech_lean,{name},{queueType},{price};{bankBefore};{poolMax}"));
		}
	}
}
