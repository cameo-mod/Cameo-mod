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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// STAGGER_DESIGN_06 tests J1-J9 (section 16): the heavy-work service, world free.
	[TestFixture]
	public class BotJobServiceTest
	{
		// A bounded-unit job that sums an array. Two input modes:
		//   L (live):     the job reads Source as it goes; a Source change bumps SourceVersion, which is its generation.
		//   S (snapshot): the job copies Source at BeginPass; a later change leaves the running pass alone and queues a re-run.
		sealed class SumJob : BotSlicedJob
		{
			public int[] Source;
			public long SourceVersion;
			public bool UseSnapshot;
			public bool Requested;
			public long PublishedSum = -1;
			public long PublishedVersion = -1;
			public int Index;

			int[] snap;
			long sum;
			long versionAtStart;

			public SumJob(BotJobSpec spec, int[] source, bool useSnapshot = false, bool requested = true)
				: base(spec)
			{
				Source = source;
				UseSnapshot = useSnapshot;
				Requested = requested;
			}

			public void Mutate(int index, int value)
			{
				Source[index] = value;
				SourceVersion++;
			}

			protected override bool HasWork => Requested;

			protected override void BeginPass()
			{
				Requested = false;
				Index = 0;
				sum = 0;
				versionAtStart = SourceVersion;
				snap = UseSnapshot ? (int[])Source.Clone() : null;
			}

			protected override int StepUnits(int budget)
			{
				var data = UseSnapshot ? snap : Source;
				var n = Math.Min(budget, data.Length - Index);
				for (var i = 0; i < n; i++)
					sum += data[Index++];

				return n;
			}

			protected override bool PassComplete => Index >= (UseSnapshot ? snap : Source).Length;

			protected override void Publish()
			{
				PublishedSum = sum;
				PublishedVersion = UseSnapshot ? versionAtStart : SourceVersion;
			}

			protected override long LiveGeneration => UseSnapshot ? 0 : SourceVersion;

			protected override bool SnapshotChanged => UseSnapshot && SourceVersion != versionAtStart;
		}

		// An endless job: always has work, never completes. Used to hold a visit at its caps.
		sealed class EndlessJob : BotSlicedJob
		{
			public EndlessJob(BotJobSpec spec)
				: base(spec) { }

			protected override bool HasWork => true;
			protected override void BeginPass() { }
			protected override int StepUnits(int budget) => budget;
			protected override bool PassComplete => false;
			protected override void Publish() { }
		}

		// Pending only while `On` is set: the on-demand filler of the spare-capacity fixtures.
		sealed class SwitchJob : BotSlicedJob
		{
			public bool On;

			public SwitchJob(BotJobSpec spec)
				: base(spec) { }

			protected override bool HasWork => On;
			protected override void BeginPass() { }
			protected override int StepUnits(int budget) => budget;
			protected override bool PassComplete => false;
			protected override void Publish() { }
		}

		// As SwitchJob, but every served pass completes at once: pending only on the visits where `On` is set.
		sealed class OneShotSwitchJob : BotSlicedJob
		{
			public bool On;

			public OneShotSwitchJob(BotJobSpec spec)
				: base(spec) { }

			protected override bool HasWork => On;
			protected override void BeginPass() { }
			protected override int StepUnits(int budget) => budget;
			protected override bool PassComplete => true;
			protected override void Publish() { }
		}

		static BotJobSpec Spec(string kind, int quota, int floor, int slotCap, int debtCap = 0, int rMax = 2, int backoff = 2, int work = 1000) =>
			new(kind, quota, floor, slotCap, debtCap, rMax, backoff, work);

		static int[] Range(int n) => Enumerable.Range(1, n).ToArray();

		static long Sum(IEnumerable<int> xs) => xs.Sum(x => (long)x);

		[Test]
		public void J1_DebtIsBoundedByDebtCapAndDropsAreCounted()
		{
			var set = new BotJobSet(100, new BotJobSeq());
			var job = new EndlessJob(Spec("scan", quota: 3, floor: 1, slotCap: 4, debtCap: 5));
			set.Add(job);

			// Elapsed 8: want = 24 + debt, the visit can spend at most SlotCap 4, so the shortfall is large.
			set.RunVisit(8);
			Assert.That(job.Debt, Is.EqualTo(5));
			Assert.That(job.Dropped, Is.EqualTo(15));
			set.RunVisit(8);
			Assert.That(job.Debt, Is.EqualTo(5), "debt never exceeds DebtCap");
			Assert.That(job.Dropped, Is.EqualTo(35), "want 29, used 4, debt 5: 20 more dropped");

			// With enough capacity the shortfall disappears: debt comes only from a credit shortfall.
			var calm = new BotJobSet(100, new BotJobSeq());
			var easy = new EndlessJob(Spec("scan", quota: 3, floor: 1, slotCap: 4, debtCap: 5));
			calm.Add(easy);
			calm.RunVisit(1);
			Assert.That(easy.Debt, Is.Zero);
			Assert.That(easy.Dropped, Is.Zero);
		}

		[Test]
		public void J2_IdleOpBanksNoDebtAndTheFloorCreatesNone()
		{
			var set = new BotJobSet(100, new BotJobSeq());
			var idle = new SwitchJob(Spec("idle", quota: 3, floor: 1, slotCap: 4, debtCap: 50));
			var onDemand = new EndlessJob(Spec("demand", quota: 0, floor: 2, slotCap: 2));
			set.Add(idle);
			set.Add(onDemand);

			for (var i = 0; i < 50; i++)
				set.RunVisit(8);

			Assert.That(idle.Debt, Is.Zero, "no pending work, no banked credit");
			Assert.That(idle.Dropped, Is.Zero);
			Assert.That(onDemand.Debt, Is.Zero, "the floor never creates debt");
			Assert.That(onDemand.Dropped, Is.Zero);

			idle.On = true;
			set.RunVisit(8);
			Assert.That(idle.UnitsServed, Is.LessThanOrEqualTo(4), "the first visit spends at most SlotCap; nothing was banked");
		}

		[Test]
		public void J3_OwnerLossAtEveryCycleOffsetTransfersAtTheBoundaryWithTheCursorIntact([Range(0, 3)] int offset)
		{
			var seq = new BotJobSeq();
			var registry = new BotSharedJobRegistry(seq);
			var source = Range(40);
			var job = (SumJob)registry.Subscribe("topology", gated: true, "A", () => new SumJob(Spec("topology", 0, 2, 2), source));
			registry.Subscribe("topology", true, "B", () => null);
			registry.Subscribe("topology", true, "C", () => null);

			var sets = new Dictionary<string, BotJobSet>();
			foreach (var seat in new[] { "A", "B", "C", "D" })
			{
				var name = seat;
				sets[name] = new BotJobSet(10, seq, () => registry.OwnedBy(name));
			}

			registry.TransferOwners(new[] { "A", "B", "C", "D" });
			Assert.That(registry.OwnerOf("topology", true), Is.EqualTo("A"));

			// Cycle 1: A visits at slot 0 only if it is alive; here A is defeated before slot `offset`.
			var roster = new[] { "A", "B", "C", "D" };
			var progressAtLoss = -1;
			for (var slot = 0; slot < 4; slot++)
			{
				if (slot == offset)
					progressAtLoss = job.Index;

				// A visit by a defeated owner never happens. Other seats' visits serve only what they own.
				var seat = roster[slot];
				if (seat == "A" && slot >= offset)
					continue;

				sets[seat].RunVisit(4);
			}

			var progressAfterCycle = job.Index;
			Assert.That(progressAfterCycle, Is.GreaterThanOrEqualTo(progressAtLoss));
			Assert.That(job.Index, Is.LessThan(source.Length), "the job pauses until the boundary and is not finished yet");

			// Boundary: A is gone, the owner transfers to B, nothing restarts.
			registry.TransferOwners(new[] { "B", "C", "D" });
			Assert.That(registry.OwnerOf("topology", true), Is.EqualTo("B"));
			Assert.That(registry.OwnerTransfers, Is.EqualTo(1));
			Assert.That(job.Index, Is.EqualTo(progressAfterCycle), "the cursor transfers intact");
			Assert.That(job.TotalRestarts, Is.Zero);

			for (var i = 0; i < 40 && job.Publishes == 0; i++)
				sets["B"].RunVisit(4);

			Assert.That(job.PublishedSum, Is.EqualTo(Sum(source)));
		}

		[Test]
		public void J3_NoSubscriberLeftDropsTheJobAndCountsIt()
		{
			var registry = new BotSharedJobRegistry(new BotJobSeq());
			registry.Subscribe("topology", true, "A", () => new SumJob(Spec("topology", 0, 1, 1), Range(5)));
			registry.TransferOwners(new[] { "A", "B" });
			registry.TransferOwners(new[] { "B" });
			Assert.That(registry.Count, Is.Zero);
			Assert.That(registry.DroppedJobs, Is.EqualTo(1));
		}

		[Test]
		public void J3_GatedAndUngatedBotsNeverShareAJobInstance()
		{
			var registry = new BotSharedJobRegistry(new BotJobSeq());
			var gated = registry.Subscribe("topology", true, "A", () => new SumJob(Spec("topology", 0, 1, 1), Range(5)));
			var ungated = registry.Subscribe("topology", false, "B", () => new SumJob(Spec("topology", 0, 1, 1), Range(5)));
			Assert.That(gated, Is.Not.SameAs(ungated));
			Assert.That(registry.Subscribe("topology", true, "C", () => null), Is.SameAs(gated));
		}

		[Test]
		public void J4a_AnLInputChangeMidJobRestartsOnceAndPublishesTheNewGeneration()
		{
			var source = Range(20);
			var job = new SumJob(Spec("flood", 0, 3, 3), source);
			var set = new BotJobSet(10, new BotJobSeq());
			set.Add(job);

			set.RunVisit(1);
			set.RunVisit(1);
			Assert.That(job.Publishes, Is.Zero);
			job.Mutate(15, 1000);

			for (var i = 0; i < 40 && job.Publishes == 0; i++)
				set.RunVisit(1);

			Assert.That(job.TotalRestarts, Is.EqualTo(1));
			Assert.That(job.Publishes, Is.EqualTo(1));
			Assert.That(job.PublishedSum, Is.EqualTo(Sum(source)), "equals the synchronous reference on the new generation");
			Assert.That(job.PublishedVersion, Is.EqualTo(1));
		}

		[Test]
		public void J4b_ContinuousInvalidationEndsStarvedAndPublishesNothing()
		{
			var source = Range(30);
			var job = new SumJob(Spec("flood", 0, 3, 3, rMax: 2, backoff: 3), source);
			var set = new BotJobSet(10, new BotJobSeq());
			set.Add(job);

			// A first, undisturbed publication gives a previous complete generation.
			for (var i = 0; i < 40 && job.Publishes == 0; i++)
				set.RunVisit(1);

			var previousSum = job.PublishedSum;
			var previousVersion = job.PublishedVersion;
			Assert.That(previousSum, Is.EqualTo(Sum(source)));

			job.Requested = true;
			for (var i = 0; i < 60; i++)
			{
				job.Mutate(0, source[0] + 1);
				set.RunVisit(1);
			}

			Assert.That(job.StarvedCount, Is.GreaterThanOrEqualTo(1));
			Assert.That(job.Publishes, Is.EqualTo(1), "a changing world publishes nothing after RMax restarts");
			Assert.That(job.PublishedSum, Is.EqualTo(previousSum), "the previous generation stays readable");
			Assert.That(job.PublishedVersion, Is.EqualTo(previousVersion));
		}

		[Test]
		public void J4b_AStarvedJobWaitsTheBackoffBeforeItIsQueuedAgain()
		{
			var source = Range(30);
			var job = new SumJob(Spec("flood", 0, 3, 3, rMax: 0, backoff: 3), source);
			var set = new BotJobSet(10, new BotJobSeq());
			set.Add(job);

			set.RunVisit(1);
			job.Mutate(0, 5);
			set.RunVisit(1);
			Assert.That(job.StarvedCount, Is.EqualTo(1));
			Assert.That(job.BackoffVisits, Is.EqualTo(3));

			var before = job.UnitsServed;
			for (var i = 0; i < 3; i++)
				set.RunVisit(1);

			Assert.That(job.UnitsServed, Is.EqualTo(before), "three backoff visits serve nothing");
			job.Requested = true;
			set.RunVisit(1);
			Assert.That(job.UnitsServed, Is.GreaterThan(before), "queued again after the backoff");
		}

		[Test]
		public void J4c_AnSInputChangeMidJobPublishesTheStartSnapshotThenRerunsWithTheNewOne()
		{
			var source = Range(12);
			var startSum = Sum(source);
			var job = new SumJob(Spec("scan", 0, 3, 3), source, useSnapshot: true);
			var set = new BotJobSet(10, new BotJobSeq());
			set.Add(job);

			set.RunVisit(1);
			job.Mutate(11, 500);
			for (var i = 0; i < 20 && job.Publishes == 0; i++)
				set.RunVisit(1);

			Assert.That(job.PublishedSum, Is.EqualTo(startSum), "the result is the function of the START snapshot");
			Assert.That(job.Pending, Is.True, "a re-run is queued with a fresh snapshot");
			for (var i = 0; i < 20 && job.Publishes < 2; i++)
				set.RunVisit(1);

			Assert.That(job.PublishedSum, Is.EqualTo(Sum(source)));
			Assert.That(job.TotalRestarts, Is.Zero, "an S input never restarts a pass");
		}

		[Test]
		public void J5a_EveryJobGetsItsFloorAtEveryVisit()
		{
			var set = new BotJobSet(6, new BotJobSeq());
			var jobs = Enumerable.Range(0, 3).Select(i => new EndlessJob(Spec("j" + i, 5, 2, 4, 20))).ToArray();
			foreach (var j in jobs)
				set.Add(j);

			for (var v = 1; v <= 25; v++)
			{
				set.RunVisit(3);
				foreach (var j in jobs)
					Assert.That(j.UnitsServed, Is.EqualTo(2 * v), "floor served in full at visit " + v);
			}
		}

		[Test]
		public void J5b_ACreditJobIsServedWithinJSpareVisitsWhenSpareAlternates()
		{
			// Spare alternates 1, 0, 1, 0: the filler is pending on every other visit and eats the spare.
			var set = new BotJobSet(3, new BotJobSeq());
			var a = new EndlessJob(Spec("a", 10, 1, 3, 100));
			var b = new EndlessJob(Spec("b", 10, 1, 3, 100));
			var filler = new OneShotSwitchJob(Spec("filler", 0, 1, 1));
			set.Add(a);
			set.Add(b);
			set.Add(filler);

			for (var v = 0; v < 4; v++)
			{
				filler.On = v % 2 == 1;
				set.RunVisit(4);
			}

			// Two visits had spare. Each floor job got 4 floor units over 4 visits; the credit units are the rest.
			var creditA = a.UnitsServed - 4;
			var creditB = b.UnitsServed - 4;
			Assert.That(creditA, Is.EqualTo(1));
			Assert.That(creditB, Is.EqualTo(1), "the rotation reaches the second credit job within J = 2 spare visits");
		}

		[Test]
		public void J5c_WhenTheFloorsFillTheVisitNoCreditIsServedAndTheCounterShowsIt()
		{
			var set = new BotJobSet(4, new BotJobSeq());
			var a = new EndlessJob(Spec("a", 8, 2, 6, 50));
			var b = new EndlessJob(Spec("b", 8, 2, 6, 50));
			set.Add(a);
			set.Add(b);

			for (var v = 1; v <= 10; v++)
			{
				set.RunVisit(4);
				Assert.That(a.UnitsServed, Is.EqualTo(2 * v));
				Assert.That(b.UnitsServed, Is.EqualTo(2 * v));
			}

			Assert.That(set.CreditStarvedVisits, Is.EqualTo(10));
			Assert.That(set.Cursor, Is.Zero, "a visit without spare never moves the cursor");
		}

		[Test]
		public void J6_AdmissionRejectsEachSyntheticFailure()
		{
			var good = new BotJobUnitProfile(10, false, false, false, false, false);
			Assert.That(BotUnitAdmission.Admit(good, 16), Is.Null);
			Assert.That(BotUnitAdmission.Admit(good with { WorstCaseCost = 17 }, 16), Does.StartWith("1"));
			Assert.That(BotUnitAdmission.Admit(good with { CostDependsOnMapOrActors = true }, 16), Does.StartWith("1"));
			Assert.That(BotUnitAdmission.Admit(good with { UnboundedNestedCall = true }, 16), Does.StartWith("2"));
			Assert.That(BotUnitAdmission.Admit(good with { UnboundedSort = true }, 16), Does.StartWith("3"));
			Assert.That(BotUnitAdmission.Admit(good with { UnboundedAllocation = true }, 16), Does.StartWith("3"));
			Assert.That(BotUnitAdmission.Admit(good with { PublishTouchesEveryCell = true }, 16), Does.StartWith("4"));
		}

		[Test]
		public void J7_AZeroQuotaJobMakesFloorProgressAtEveryVisitAndFinishesWithinTheBound()
		{
			const int work = 9;
			const int floor = 2;
			var job = new SumJob(Spec("lazy", 0, floor, floor, work: work), Range(work));
			var rival = new EndlessJob(Spec("rival", 50, 1, 6, 100));
			var set = new BotJobSet(floor + 1, new BotJobSeq());
			set.Add(job);
			set.Add(rival);

			var visits = 0;
			while (job.Publishes == 0 && visits < 100)
			{
				set.RunVisit(10);
				visits++;
			}

			Assert.That(visits, Is.LessThanOrEqualTo((work + floor - 1) / floor));
			Assert.That(job.PublishedSum, Is.EqualTo(Sum(Range(work))));
		}

		[Test]
		public void J7_TheMutantWithAZeroFloorStallsSoTheBoundAssertionCanFail()
		{
			var job = new SumJob(Spec("lazy", 0, 0, 2), Range(9));
			var set = new BotJobSet(4, new BotJobSeq());
			set.Add(job);
			for (var i = 0; i < 100; i++)
				set.RunVisit(10);

			Assert.That(job.Publishes, Is.Zero, "m_k = 0 and no quota: no service at all");
			Assert.That(BotJobLint.CheckFloors(new[] { job.Spec }, 4), Does.Contain("m_k >= 1"));
		}

		[Test]
		public void J8_TheFloorLintRefusesBadRulesAndAGoodSetServesEveryFloorUnderMaximumLoad()
		{
			Assert.That(BotJobLint.CheckFloors(new[] { Spec("a", 0, 3, 3), Spec("b", 0, 3, 3) }, 5), Does.Contain("sum"));
			Assert.That(BotJobLint.CheckFloors(new[] { Spec("a", 0, 4, 3) }, 8), Does.Contain("above SlotCap"));
			Assert.That(BotJobLint.CheckFloors(new[] { Spec("a", 0, 1, 9) }, 8), Does.Contain("above VisitCap"));
			Assert.That(BotJobLint.CheckFloors(new[] { Spec("a", 0, 0, 3) }, 8), Does.Contain("m_k >= 1"));

			// The equality case passes the lint and leaves no spare.
			var specs = new[] { Spec("a", 20, 2, 5, 40), Spec("b", 20, 3, 5, 40) };
			Assert.That(BotJobLint.CheckFloors(specs, 5), Is.Null);

			var set = new BotJobSet(5, new BotJobSeq());
			var jobs = specs.Select(s => new EndlessJob(s)).ToArray();
			foreach (var j in jobs)
				set.Add(j);

			for (var v = 1; v <= 30; v++)
			{
				set.RunVisit(12);
				Assert.That(jobs[0].UnitsServed, Is.EqualTo(2 * v));
				Assert.That(jobs[1].UnitsServed, Is.EqualTo(3 * v));
			}
		}

		[Test]
		public void J8_TheQuotaThroughputWarningFiresWhenTheQuotaCannotBeMetAtSomeN()
		{
			var specs = new[] { Spec("a", 2, 1, 8), Spec("b", 2, 1, 8) };
			Assert.That(BotJobLint.QuotaThroughputWarning(specs, 12, 1), Is.False);
			Assert.That(BotJobLint.QuotaThroughputWarning(specs, 12, 12), Is.True);
		}

		[Test]
		public void J9_VisitsToPublishStayWithinTheLatencyBoundForAStableAndAnInjectedRestartRun()
		{
			const int work = 10;
			const int floor = 3;
			var perPass = (work + floor - 1) / floor;

			var stable = new SumJob(Spec("scan", 0, floor, floor, rMax: 2, work: work), Range(work));
			var set = new BotJobSet(floor, new BotJobSeq());
			set.Add(stable);
			var visits = 0;
			while (stable.Publishes == 0 && visits < 100)
			{
				set.RunVisit(1);
				visits++;
			}

			Assert.That(visits, Is.LessThanOrEqualTo(perPass));

			// Two injected restarts (R_max = 2): the bound multiplies by R_max + 1.
			var source = Range(work);
			var injected = new SumJob(Spec("scan", 0, floor, floor, rMax: 2, work: work), source);
			var set2 = new BotJobSet(floor, new BotJobSeq());
			set2.Add(injected);
			visits = 0;
			var injections = 0;
			while (injected.Publishes == 0 && visits < 100)
			{
				if (injections < 2 && injected.Index > 0)
				{
					injected.Mutate(0, source[0]);
					injections++;
				}

				set2.RunVisit(1);
				visits++;
			}

			Assert.That(injected.TotalRestarts, Is.EqualTo(2));
			Assert.That(injected.Publishes, Is.EqualTo(1));
			Assert.That(visits, Is.LessThanOrEqualTo(perPass * (2 + 1)));
		}

		[Test]
		public void TheSameInputsGiveTheSameServiceOnEveryRun()
		{
			string Run()
			{
				var set = new BotJobSet(7, new BotJobSeq());
				var jobs = new List<EndlessJob>
				{
					new(Spec("a", 4, 1, 5, 9)),
					new(Spec("b", 3, 2, 6, 9)),
					new(Spec("c", 0, 1, 3)),
				};
				foreach (var j in jobs)
					set.Add(j);

				var trace = new List<string>();
				for (var v = 1; v <= 40; v++)
				{
					set.RunVisit(v % 5 + 1);
					trace.Add(string.Join(",", jobs.Select(j => j.UnitsServed + ":" + j.Debt + ":" + j.Dropped)));
				}

				return string.Join(";", trace);
			}

			Assert.That(Run(), Is.EqualTo(Run()));
		}
	}
}
