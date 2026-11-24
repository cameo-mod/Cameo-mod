#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_02 s2, _03, _04, _05): the pure 2x mobile/static rule. Pure state, no world.
	[TestFixture]
	public class MobileStaticRatioTest
	{
		[Test]
		public void TheConstantsAreTheRuledValues()
		{
			Assert.That(MobileStaticRatioCA.Ratio, Is.EqualTo(2), "the user's 2x, fixed at every tier");
			Assert.That(MobileStaticRatioCA.RecordTicks, Is.EqualTo(3));
			Assert.That(MobileStaticRatioCA.SnapshotMaxAgeTicks, Is.EqualTo(300));
			Assert.That(MobileStaticRatioCA.StoragePct, Is.EqualTo(50));
		}

		[Test]
		public void AdmitsAtExactlyTwiceTheStaticValuePlusTheNewDefence()
		{
			Assert.That(MobileStaticRatioCA.Admits(1200, 0, 600), Is.True, "M = 2 x (0 + 600)");
			Assert.That(MobileStaticRatioCA.Admits(1199, 0, 600), Is.False, "one below");
			Assert.That(MobileStaticRatioCA.Admits(0, 0, 600), Is.False, "no army: no defence start");
			Assert.That(MobileStaticRatioCA.Admits(5000, 1900, 600), Is.True, "5000 = 2 x (1900 + 600)");
			Assert.That(MobileStaticRatioCA.Admits(4999, 1900, 600), Is.False);
		}

		[Test]
		public void NegativeInputsClampAndHugeValuesNeverAdmitOrThrow()
		{
			Assert.That(MobileStaticRatioCA.Admits(0, -50, -10), Is.True, "negatives read as 0: 0 >= 0");
			Assert.That(MobileStaticRatioCA.Admits(long.MaxValue, long.MaxValue, 1), Is.False, "overflowing need is never admitted");
			Assert.That(MobileStaticRatioCA.Admits(long.MaxValue, long.MaxValue / 2, 1), Is.False, "2 x need overflows: never admitted");
		}

		[Test]
		public void StageOneOrderIsInertAdmitFirstCopyThenNeedsFloat()
		{
			Assert.That(MobileStaticRatioCA.Stage1(false, 0, 99999, 600, true), Is.EqualTo(RatioVerdict.Inert), "no measure: inert even for a first copy");
			Assert.That(MobileStaticRatioCA.Stage1(true, 1200, 0, 600, false), Is.EqualTo(RatioVerdict.Admit));
			Assert.That(MobileStaticRatioCA.Stage1(true, 1200, 0, 600, true), Is.EqualTo(RatioVerdict.Admit),
				"a first copy that satisfies the ratio is a plain Admit: an exempt start is always BELOW the ratio");
			Assert.That(MobileStaticRatioCA.Stage1(true, 0, 0, 600, true), Is.EqualTo(RatioVerdict.ExemptFirst), "first copy below the ratio");
			Assert.That(MobileStaticRatioCA.Stage1(true, 0, 0, 600, false), Is.EqualTo(RatioVerdict.NeedsFloat), "a further copy below the ratio");
		}

		[Test]
		public void StageFourExemptsWhileFloatingAndHoldsOtherwise()
		{
			Assert.That(MobileStaticRatioCA.Float(true), Is.EqualTo(RatioVerdict.ExemptFloat));
			Assert.That(MobileStaticRatioCA.Float(false), Is.EqualTo(RatioVerdict.Hold));
		}

		[Test]
		public void DecideComposesTheStagesAndOnlyAFurtherCopyBelowTheRatioReachesFloat()
		{
			// ratio satisfied: floats are irrelevant
			Assert.That(MobileStaticRatioCA.Decide(true, 1200, 0, 600, false, false), Is.EqualTo(RatioVerdict.Admit));
			// first copy: exempt without consulting float
			Assert.That(MobileStaticRatioCA.Decide(true, 0, 0, 600, true, false), Is.EqualTo(RatioVerdict.ExemptFirst));
			// further copy: float decides
			Assert.That(MobileStaticRatioCA.Decide(true, 0, 0, 600, false, true), Is.EqualTo(RatioVerdict.ExemptFloat));
			Assert.That(MobileStaticRatioCA.Decide(true, 0, 0, 600, false, false), Is.EqualTo(RatioVerdict.Hold));
			// unmeasured: never a hold
			Assert.That(MobileStaticRatioCA.Decide(false, 0, 0, 600, false, false), Is.EqualTo(RatioVerdict.Inert));

			Assert.That(MobileStaticRatioCA.Holds(RatioVerdict.Hold), Is.True);
			foreach (var v in new[] { RatioVerdict.Inert, RatioVerdict.Admit, RatioVerdict.ExemptFirst, RatioVerdict.ExemptFloat, RatioVerdict.NeedsFloat })
				Assert.That(MobileStaticRatioCA.Holds(v), Is.False, v.ToString());
		}

		[Test]
		public void TheStorageLegIsFiftyPercentOfAPositiveCapacity()
		{
			Assert.That(MobileStaticRatioCA.StorageFloating(499, 1000), Is.False);
			Assert.That(MobileStaticRatioCA.StorageFloating(500, 1000), Is.True);
			Assert.That(MobileStaticRatioCA.StorageFloating(1000, 1000), Is.True);
			Assert.That(MobileStaticRatioCA.StorageFloating(500, 0), Is.False, "no capacity: no storage leg");
			Assert.That(MobileStaticRatioCA.StorageFloating(0, 0), Is.False);
		}

		[Test]
		public void TheBankLegIsTheTierBarAndAZeroBarNeverApplies()
		{
			Assert.That(MobileStaticRatioCA.BankOverBar(3999, 4000), Is.False, "bar - 1");
			Assert.That(MobileStaticRatioCA.BankOverBar(4000, 4000), Is.True, "bar");
			Assert.That(MobileStaticRatioCA.BankOverBar(999999, 0), Is.False, "bar 0 (switch off or classic): the bank leg never applies");
		}

		sealed class UnitSide : IBotMobileSpendReserve
		{
			public bool UnitSideSaturated { get; set; }

			public int MobileSpendFloor => 0;

			public int PaidUnitSharePct => 0;
		}

		// USER 11-22 (Opus T1): (all unit queues full AND bank >= bar) OR storage >= 50%; the storage leg stands alone.
		[TestCase(true, false, false, ExpectedResult = true, TestName = "storage alone floats, units can still absorb")]
		[TestCase(true, true, true, ExpectedResult = true, TestName = "all legs")]
		[TestCase(false, true, true, ExpectedResult = true, TestName = "bank over the bar with a saturated unit side")]
		[TestCase(false, true, false, ExpectedResult = false, TestName = "bank over the bar but units can still absorb")]
		[TestCase(false, false, true, ExpectedResult = false, TestName = "saturated unit side without the bank")]
		[TestCase(false, false, false, ExpectedResult = false, TestName = "nothing floats")]
		public bool IsFloatingIsStorageAloneOrABankOverTheBarWithASaturatedUnitSide(bool storage, bool bank, bool unitsSaturated) =>
			MobileStaticRatioCA.IsFloating(storage, bank, new UnitSide { UnitSideSaturated = unitsSaturated });

		[Test]
		public void NoProviderMeansTheUnitSideNeverReadsSaturatedButStorageStillFloats()
		{
			Assert.That(MobileStaticRatioCA.IsFloating(false, true, null), Is.False);
			Assert.That(MobileStaticRatioCA.IsFloating(true, false, null), Is.True);
		}

		[Test]
		public void TheDefaultProviderAnswerIsNotSaturated()
		{
			IBotMobileSpendReserve plain = new PlainReserve();
			Assert.That(plain.UnitSideSaturated, Is.False, "the interface default: no float from the unit side");
		}

		sealed class PlainReserve : IBotMobileSpendReserve
		{
			public int MobileSpendFloor => 0;

			public int PaidUnitSharePct => 0;
		}

		[Test]
		public void AFreshSnapshotIsUsableUpToThreeHundredTicksOld()
		{
			Assert.That(MobileStaticRatioCA.MeasureUsable(true, 1000, 1000), Is.True);
			Assert.That(MobileStaticRatioCA.MeasureUsable(true, 1000, 1300), Is.True, "age 300");
			Assert.That(MobileStaticRatioCA.MeasureUsable(true, 1000, 1301), Is.False, "age 301: stale, so the rule is inert");
			Assert.That(MobileStaticRatioCA.MeasureUsable(false, 1000, 1000), Is.False, "no snapshot yet");
			Assert.That(MobileStaticRatioCA.MeasureUsable(true, 1000, 999), Is.False, "a snapshot from the future (a clock reset) is not usable");
		}

		[Test]
		public void ARecordedStartCountsForThreeTicksThenExpires()
		{
			var r = new MobileStaticRatioCA();
			r.Record(1000, "gun", 500);
			Assert.That(r.Recorded(1000), Is.EqualTo(500));
			r.Record(1001, "sam", 300);
			Assert.That(r.Recorded(1001), Is.EqualTo(800), "both inside the window");
			Assert.That(r.Recorded(1002), Is.EqualTo(800));
			Assert.That(r.Recorded(1003), Is.EqualTo(300), "the first start is 3 ticks old and expired");
			Assert.That(r.Recorded(1004), Is.EqualTo(0));
			Assert.That(r.Recorded(1004), Is.EqualTo(0), "stable once pruned");

			r.Record(2000, "gun", 400);
			Assert.That(r.Recorded(1999), Is.EqualTo(0), "an entry from the future (a clock reset) is dropped");
		}

		[Test]
		public void TwoDefencesCannotBothPassOneOldSnapshot()
		{
			var r = new MobileStaticRatioCA();
			const long mobile = 2400;
			const long staticLive = 0;

			Assert.That(MobileStaticRatioCA.Admits(mobile, staticLive + r.Recorded(500), 1000), Is.True, "first defence of 1000 passes: 2400 >= 2000");
			r.Record(500, "gun", 1000);
			Assert.That(MobileStaticRatioCA.Admits(mobile, staticLive + r.Recorded(500), 1000), Is.False, "the same sweep: the second sees the first (2400 < 4000)");
		}

		[Test]
		public void TheDoubleCountWindowIsAConservativeOvercountNeverALooserTest()
		{
			// _03 s2: for up to 3 ticks a started defence is in the queue term AND the record term. S only grows, so the test only tightens.
			var r = new MobileStaticRatioCA();
			r.Record(100, "gun", 600);
			var once = 600L;
			var twice = 600L + r.Recorded(101);
			Assert.That(twice, Is.EqualTo(2 * once));
			Assert.That(MobileStaticRatioCA.Admits(1800, twice, 300), Is.False);
			Assert.That(MobileStaticRatioCA.Admits(1800, once, 300), Is.True, "without the overlap this would pass: the overlap is the safe side");
			Assert.That(r.Recorded(103), Is.EqualTo(0), "and it is gone after 3 ticks");
		}

		[Test]
		public void TheFirstCopyLedgerNeedsNeitherAnOwnedNorAQueuedNorAnEarlierStart()
		{
			var r = new MobileStaticRatioCA();
			Assert.That(r.IsFirstCopy("gun", false), Is.True);
			Assert.That(r.IsFirstCopy("gun", true), Is.False, "one already owned or queued");

			r.Record(10, "gun", 600);
			Assert.That(r.EverStarted("gun"), Is.True);
			Assert.That(r.IsFirstCopy("gun", false), Is.False, "started once, then lost: not a first copy again");
			Assert.That(r.IsFirstCopy("sam", false), Is.True, "another type is still a first copy");
			Assert.That(r.EverStarted(null), Is.False);
			Assert.That(r.IsFirstCopy(null, false), Is.True, "a null type name never counts as started");
		}

		[Test]
		public void TheFirstCopyIsAdmittedBelowTheRatioAndTheSecondIsHeld()
		{
			var r = new MobileStaticRatioCA();
			const long mobile = 0;

			var first = MobileStaticRatioCA.Decide(true, mobile, 0, 600, r.IsFirstCopy("gun", false), false);
			Assert.That(first, Is.EqualTo(RatioVerdict.ExemptFirst));
			r.Record(10, "gun", 600);

			var second = MobileStaticRatioCA.Decide(true, mobile, 600, 600, r.IsFirstCopy("gun", true), false);
			Assert.That(second, Is.EqualTo(RatioVerdict.Hold), "the further copy waits for the army");
		}

		// ---- source pins (R2/R3): the wiring cannot drift from the reviewed shape ----

		static string Source(string file) =>
			File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Builder() => Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");

		static string RatioRegion(string builder)
		{
			var a = builder.IndexOf("// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_04/05) ---", StringComparison.Ordinal);
			var b = builder.IndexOf("ActorInfo ChooseBuildingToBuild(ProductionQueue queue)", StringComparison.Ordinal);
			Assert.That(a, Is.GreaterThanOrEqualTo(0), "the ratio helper region exists");
			Assert.That(b, Is.GreaterThan(a));
			return builder.Substring(a, b - a);
		}

		[Test]
		public void TheAblationRowAndTheSwitchGateTheRule()
		{
			Assert.That(AblationIds.Ratio2x, Is.EqualTo("army_first_02_ratio"));
			var region = RatioRegion(Builder());
			Assert.That(region, Does.Contain("bool RatioActive() => baseBuilder.Info.MobileStaticRatioEnabled && !ablateRatio.Off(player, AblationIds.Ratio2x);"));
			Assert.That(Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs"), Does.Contain("public readonly bool MobileStaticRatioEnabled = false;"), "off by default");
		}

		[Test]
		public void TheGateHoldsBeforeEveryClaimAndRecordsOnlyAtTheEmittedOrder()
		{
			var b = Builder();
			var gate = b.IndexOf("var ratioVerdict = RatioGate(item, out var ratioDefence);", StringComparison.Ordinal);
			var afterReserve = b.IndexOf("// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_04/05): a static-defence start must keep", StringComparison.Ordinal);
			var firstClaim = b.IndexOf("lawWants.TryClaimCrawlLink(queue.Actor, item.Name", StringComparison.Ordinal);
			var refReservation = b.IndexOf("BaseBuilderQueueEvalCA.RefineryReservationAdmits(", StringComparison.Ordinal);
			var mobileBlock = b.IndexOf("BaseBuilderQueueEvalCA.MobileSpendBlocks(playerResources.GetCashAndResources()", StringComparison.Ordinal);
			var emitted = b.IndexOf("queuedAt[queue.Actor.ActorID] = (item.Name, world.WorldTick);", StringComparison.Ordinal);
			var record = b.IndexOf("baseBuilder.StaticRatio.Record(world.WorldTick, item.Name, ValuedCost(item));", StringComparison.Ordinal);

			Assert.That(new[] { gate, afterReserve, firstClaim, refReservation, mobileBlock, emitted, record }, Has.All.GreaterThanOrEqualTo(0));
			Assert.That(mobileBlock, Is.LessThan(gate), "after the cash gate and the TF-2 reservation");
			Assert.That(gate, Is.LessThan(refReservation), "before the refinery reservation claim");
			Assert.That(gate, Is.LessThan(firstClaim), "before the crawl-link claim");
			Assert.That(record, Is.GreaterThan(emitted), "the start is recorded only after the order is emitted");

			var holdAt = b.IndexOf("if (MobileStaticRatioCA.Holds(ratioVerdict))", StringComparison.Ordinal);
			Assert.That(holdAt, Is.GreaterThanOrEqualTo(0));
			var holdBlock = b.Substring(holdAt, 160);
			Assert.That(holdBlock, Does.Contain("pendingDemandPick = default;"), "a hold drops the pending demand pick like the TF-2 veto");
			Assert.That(holdBlock, Does.Contain("return false;"));
		}

		[Test]
		public void TheFractionLoopSkipsAHeldDefenceInsteadOfLosingTheSweep()
		{
			var b = Builder();
			var skip = b.IndexOf("if (MobileStaticRatioCA.Holds(RatioGate(actor, out _)))", StringComparison.Ordinal);
			var build = b.IndexOf("// Lets build this", StringComparison.Ordinal);
			var lowPower = b.IndexOf("if (BaseBuilderQueueEvalCA.LowPowerBlocked(playerPower != null", StringComparison.Ordinal);
			Assert.That(skip, Is.GreaterThan(lowPower));
			Assert.That(build, Is.GreaterThan(skip));
		}

		[Test]
		public void TheNewCodeNeverCancelsOrOrdersAndAddsNoVoterOrNewScalingConstant()
		{
			var region = RatioRegion(Builder());
			var pure = Source("mods/CA/Traits/BotModules/BotModuleLogic/MobileStaticRatioCA.cs");
			var seam = Source("mods/CA/Traits/BotModules/IBotMobileStaticValues.cs");
			foreach (var text in new[] { region, pure, seam })
			{
				Assert.That(text, Does.Not.Contain("CancelProduction"), "a held defence is retried, never cancelled");
				Assert.That(text, Does.Not.Contain("QueueOrder"));
				Assert.That(text, Does.Not.Contain("[Sync]"), "no synced state");
				Assert.That(text, Does.Not.Contain("IBotRequestPauseBuildingProduction"), "no building pause voter");
				Assert.That(text, Does.Not.Contain("FloatCash"), "the fixed float bar was dropped");
				Assert.That(text, Does.Not.Contain("ScalingNetIncomePerTick"));
				Assert.That(text, Does.Not.Contain("BankSaturationThreshold"), "the raw tier bar is read, not the unit builder's switched threshold");
				Assert.That(text, Does.Not.Contain("UnitProductionSaturationCash"));
			}

			Assert.That(region, Does.Contain("botLimits?.Info.BankSaturationCash ?? 0"));
			Assert.That(region, Does.Contain("bool IsFloating()"), "one shared float helper (TECHFIRST-01 calls it too)");
			Assert.That(region, Does.Contain("return MobileStaticRatioCA.IsFloating("), "the definition lives in the pure class only");
			Assert.That(region, Does.Contain("verdict = MobileStaticRatioCA.Float(IsFloating());"));
			Assert.That(region, Does.Not.Contain("BotRng"), "no RNG: the verdict is a pure function of the measure");
		}

		[Test]
		public void TheDefenceClassificationIsTheInfoTwinOfTheSituationOwnersIsDefence()
		{
			var situation = Source("mods/Cameo/Traits/BotModules/BotSituation.cs");
			Assert.That(situation, Does.Contain("bool IsDefence(Actor a) => IsBuilding(a) && (a.Info.HasTraitInfo<AttackBaseInfo>() ||"));
			Assert.That(situation, Does.Contain("a.GetEnabledTargetTypes().Overlaps(Info.DefenceTargetTypes));"));
			Assert.That(situation, Does.Contain("info != null && info.HasTraitInfo<BuildingInfo>()"));
			Assert.That(situation, Does.Contain("&& (info.HasTraitInfo<AttackBaseInfo>() || info.TraitInfos<ITargetableInfo>().Any(t => t.GetTargetTypes().Overlaps(Info.DefenceTargetTypes)));"));
			Assert.That(situation, Does.Contain("long IBotMobileStaticValues.MobileValue => Situation?.OwnArmyValue ?? 0;"), "the measure is the snapshot's own army value, no new scan");
		}

		[Test]
		public void TheUnitSideSaturationIsCachedAndTheDefaultMeansNoFloat()
		{
			var unit = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("tick - unitSideSaturatedTick < FeedbackTime"), "never a per-call scan");
			Assert.That(Source("mods/CA/Traits/BotModules/IBotMobileSpendReserve.cs"), Does.Contain("bool UnitSideSaturated => false;"));
		}
	}
}
