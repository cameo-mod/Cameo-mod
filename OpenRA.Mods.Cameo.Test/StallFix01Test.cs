// STALL-FIX-01 (Sonnet; Opus order from STALL_ICECOLD_01_REPORT_02, FREEZE11 input; switch CI_refinery_stall_fix, default off).
// Ice Cold 4v4: a completed Nod refinery sat Held (RefineryScanDeferred) for ~33.8k ticks and blocked the whole building queue, because
// the budgeted terminal-tier fallback scan never produced Site or Exhausted. Trace: the terminal pass needs several polls (a 144x144 map
// is ~20k cells against a ~2000 budget, one poll per ~130 ticks while the queue is held) and every poll re-proves the coherence signature
// (any actor spawned or killed since the pass stamped it aborts the pass for a whole relist epoch) and every mobile cell change feeds
// the stale-mark set (capped, overflow aborts). On a busy 4v4 the pass cannot survive its own polls: an abort loop, not a cursor reset
// and not the stale-scan prune. The bound forces ONE poll to completion on a frozen world.
// The world-free scan is driven directly with synthetic snapshots; the world wiring (no headless world fixture) is pinned by source.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class StallFix01Test
	{
		const int Epoch = 50;
		const int Bound = 1500;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		static string Planner() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");

		static string Manager() => Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");

		static string Scan() => Source("mods/Cameo/Traits/BotModules/RefineryFallbackScan.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static int Count(string text, string needle)
		{
			var n = 0;
			for (var i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
				n++;
			return n;
		}

		// ---- synthetic world ----

		/// <summary>A churning world: Actors grows (a unit spawned), Dirt names cells whose occupancy changed.</summary>
		sealed class FakeWorld
		{
			public int Actors = 10;
			public readonly List<CPos> Providers = new() { new CPos(5, 5) };
			public int LegalX = -1, LegalY = -1;
			public int Width = 80, Height = 80;
			public int Legal(CPos c) => c.X == LegalX && c.Y == LegalY ? 1 : 0;
		}

		static IEnumerator<RefineryFallbackScan.CoherenceAtom> AtomsOf(FakeWorld w)
		{
			yield return new RefineryFallbackScan.CoherenceAtom(0, 0, 1, 0, null);
			for (var i = 0; i < w.Actors; i++)
				yield return new RefineryFallbackScan.CoherenceAtom(1, (uint)i, 0, 0, null);
		}

		static int Unrestricted(int tier) => int.MaxValue;

		static int LinearGap(int tier) => tier >= 30 ? int.MaxValue : tier + 1;

		static RefineryFallbackScan.Snapshot Snap(FakeWorld w, Func<int, int> tierBound = null, bool providers = true)
		{
			var cells = Enumerable.Range(0, w.Width).SelectMany(x => Enumerable.Range(0, w.Height).Select(y => new CPos(x, y))).ToList();
			var provs = providers
				? w.Providers.Select((p, i) => (RefineryFallbackScan.Provider?)new RefineryFallbackScan.Provider(p, 3, (uint)(i + 1))).ToList()
				: new List<RefineryFallbackScan.Provider?>();
			var width = w.Width;
			var height = w.Height;
			return new RefineryFallbackScan.Snapshot(
				() => provs.GetEnumerator(),
				() => new List<CPos> { new CPos(0, 0) }.GetEnumerator(),
				() => cells.GetEnumerator(),
				tierBound ?? Unrestricted,
				new[] { new CVec(0, 0) },
				() => AtomsOf(w),
				() => AtomsOf(w),
				c => c.X >= 0 && c.Y >= 0 && c.X < width && c.Y < height ? c.Y * width + c.X : -1,
				i => new CPos(i % width, i / width),
				2,
				c => true,
				c => w.Legal(c) == 1);
		}

		static RefineryFallbackScan NewScan(int start = 0) => new(Epoch) { ForceStartTick = start };

		// The ordinary per-queue-sweep budget: ConstantBudget + this world's sweep atoms.
		const int NormalBudget = 2000;

		// ---- the trace: the budgeted terminal pass cannot finish under churn ----

		[Test]
		public void ControlOnAStaticWorldTheBudgetedTerminalPassFinishesAfterSeveralPolls()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			var polls = 0;
			RefineryFallbackScan.Verdict v;
			do
			{
				v = scan.Poll(NormalBudget, polls * 10, () => Snap(w), out _);
				polls++;
			}
			while (v == RefineryFallbackScan.Verdict.Deferred && polls < 200);

			Assert.That(v, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(polls, Is.GreaterThan(2), "6400 cells against a 2000 budget needs several polls — that is what leaves the pass exposed to churn");
		}

		[Test]
		public void TheStallAnActorSpawnedBetweenPollsAbortsEveryTerminalPassSoItNeverCompletes()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			for (var i = 0; i < 400; i++)
			{
				w.Actors++;
				var v = scan.Poll(NormalBudget, i * 10, () => Snap(w), out _);
				Assert.That(v, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred), $"poll {i}");
			}

			Assert.That(scan.Epochs, Is.GreaterThan(20), "the pass restarts every epoch and is torn each time");
			Assert.That(scan.TerminalTier, Is.True);
		}

		[Test]
		public void TheStallAMarkedCellStormOverflowsTheStaleSetAndAbortsTheDrain()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			var tick = 0;
			for (var i = 0; i < 300; i++)
			{
				// Unit movement: many distinct cell updates between two polls, no actor spawned or killed.
				for (var k = 0; k < 400; k++)
					scan.MarkDirtyCell(new CPos((i * 7 + k * 13) % w.Width, (i * 11 + k * 17) % w.Height));

				var v = scan.Poll(NormalBudget, tick, () => Snap(w), out _);
				tick += 10;
				Assert.That(v, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred), $"poll {i}");
			}
		}

		// ---- forced completion ----

		[Test]
		public void ForcedPollCompletesWithExhaustedWhereTheBudgetedPathStalls()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			for (var i = 0; i < 30; i++)
			{
				w.Actors++;
				Assert.That(scan.Poll(NormalBudget, i * 10, () => Snap(w), out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			}

			w.Actors++;
			var verdict = scan.PollForced(30, 300, () => Snap(w), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(site, Is.Null);
			Assert.That(scan.CertificateCurrent(), Is.True, "the forced verdict carries its own certificate on the unchanged world");
			Assert.That(scan.LastPollWork, Is.GreaterThan(NormalBudget), "one poll did the whole 6400-cell map");
		}

		[Test]
		public void ForcedPollFromTheMiddleOfAPassWithAStampedSignatureStillCompletes()
		{
			// A pass half-run under the old world: its stamped signature is stale by the time the force fires.
			var w = new FakeWorld();
			var scan = NewScan();
			scan.Poll(NormalBudget, 0, () => Snap(w), out _);
			w.Actors += 5;

			var verdict = scan.PollForced(30, 10, () => Snap(w), out _);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted), "the forced pass restarts fresh instead of tearing on the old stamp");
		}

		[Test]
		public void ForcedPollUnderACellStormStillCompletes()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			for (var i = 0; i < 20; i++)
			{
				for (var k = 0; k < 400; k++)
					scan.MarkDirtyCell(new CPos((i * 7 + k * 13) % w.Width, (i * 11 + k * 17) % w.Height));

				scan.Poll(NormalBudget, i * 10, () => Snap(w), out _);
			}

			var verdict = scan.PollForced(30, 500, () => Snap(w), out _);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
		}

		[Test]
		public void ForcedPollReturnsTheSiteWhenALegalCellExists()
		{
			var w = new FakeWorld { LegalX = 70, LegalY = 61 };
			var scan = NewScan();
			for (var i = 0; i < 30; i++)
			{
				w.Actors++;
				Assert.That(scan.Poll(NormalBudget, i * 10, () => Snap(w), out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			}

			var verdict = scan.PollForced(30, 300, () => Snap(w), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(70, 61)));
			Assert.That(scan.CertificateCurrent(), Is.False, "a Site verdict never carries a cancellation certificate");
		}

		[Test]
		public void ForcedPollLeavesTheBoundedTiersAndRunsTheTerminalTierDirectly()
		{
			// Tier 0 is a bounded, provider-square pass under the linear ladder; the force must put the scan on the terminal tier.
			var w = new FakeWorld();
			var scan = NewScan();
			Assert.That(scan.Poll(NormalBudget, 0, () => Snap(w, LinearGap), out _), Is.EqualTo(RefineryFallbackScan.Verdict.NoSite).Or.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(scan.TerminalTier, Is.False);

			var verdict = scan.PollForced(30, 5, () => Snap(w, LinearGap), out _);
			Assert.That(scan.Tier, Is.GreaterThanOrEqualTo(30));
			Assert.That(scan.TerminalTier, Is.True);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
		}

		[Test]
		public void ForcedPollWithNoProviderStillOnlyHoldsNeverExhausted()
		{
			// 6722A484: an empty provider domain never certifies. The force must not turn that into a cancel authority.
			var w = new FakeWorld();
			var scan = NewScan();
			var verdict = scan.PollForced(30, 0, () => Snap(w, providers: false), out var site);
			Assert.That(verdict, Is.Not.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(verdict, Is.Not.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.Null);
			Assert.That(scan.CertificateCurrent(), Is.False);
		}

		static IEnumerator<RefineryFallbackScan.CoherenceAtom> TornAtoms()
		{
			throw new InvalidOperationException("collection modified");
			#pragma warning disable CS0162
			yield break;
			#pragma warning restore CS0162
		}

		[Test]
		public void AForcedPollThatCannotCompleteStaysDeferredHoldsAndIsNotRetriedBeforeTheNextEpoch()
		{
			// Defence in depth: if a forced pass ever tears (an enumeration that keeps throwing), the verdict is the honest
			// Deferred hold, never a certificate, and the expensive forced poll does not repeat on every sweep.
			var scan = NewScan(0);
			var torn = new RefineryFallbackScan.Snapshot(
				() => new List<RefineryFallbackScan.Provider?> { new RefineryFallbackScan.Provider(new CPos(5, 5), 3, 1) }.GetEnumerator(),
				() => new List<CPos>().GetEnumerator(),
				() => new List<CPos> { new CPos(0, 0) }.GetEnumerator(),
				Unrestricted,
				new[] { new CVec(0, 0) },
				TornAtoms,
				TornAtoms,
				c => 0,
				i => new CPos(0, 0),
				2,
				c => true,
				c => false);

			Assert.That(scan.ForceDue(Bound, 2000), Is.True);
			var verdict = scan.PollForced(30, 2000, () => torn, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(site, Is.Null);
			Assert.That(scan.CertificateCurrent(), Is.False);
			Assert.That(scan.ReplayingConclusiveVerdict(2001), Is.False, "an aborted forced pass is not a conclusive verdict");
			Assert.That(scan.ForceDue(Bound, 2001), Is.False, "no second forced pass inside the same epoch");
			Assert.That(scan.ForceDue(Bound, 2000 + Epoch), Is.True, "forced again at the next epoch");
		}

		// ---- the cancel rule is unchanged: only Exhausted with a current certificate ----

		[Test]
		public void AForcedExhaustedCertificateGoesStaleOnAnyActorOrCellChangeAfterIssue()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Assert.That(scan.PollForced(30, 0, () => Snap(w), out _), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.CertificateCurrent(), Is.True);

			w.Actors++;
			Assert.That(scan.CertificateCurrent(), Is.False, "an atom drift fails the certificate closed");
			w.Actors--;
			Assert.That(scan.CertificateCurrent(), Is.True, "restoring the world restores the stamped atoms");
			scan.MarkDirtyCell(new CPos(3, 3));
			Assert.That(scan.CertificateCurrent(), Is.False, "a covered event after issue fails the certificate closed");
		}

		[Test]
		public void AForcedExhaustedReplayInsideItsEpochStillRequiresTheCurrentCertificate()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			scan.PollForced(30, 100, () => Snap(w), out _);
			w.Actors++;

			var replay = scan.Poll(NormalBudget, 101, () => Snap(w), out _);
			Assert.That(replay, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted), "the verdict replays inside the epoch");
			Assert.That(scan.CertificateCurrent(), Is.False, "but the replay is not a cancel authority once the world moved");
		}

		[Test]
		public void NoVerdictOtherThanExhaustedEverCarriesACertificate()
		{
			// Site (legal cell exists), no-provider NoSite, and a mid-pass Deferred: none is certificate-current.
			var site = new FakeWorld { LegalX = 70, LegalY = 61 };
			var s1 = NewScan();
			Assert.That(s1.PollForced(30, 0, () => Snap(site), out _), Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(s1.CertificateCurrent(), Is.False);

			var empty = new FakeWorld();
			var s2 = NewScan();
			s2.PollForced(30, 0, () => Snap(empty, providers: false), out _);
			Assert.That(s2.CertificateCurrent(), Is.False);

			var s3 = NewScan();
			Assert.That(s3.Poll(NormalBudget, 0, () => Snap(empty), out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(s3.CertificateCurrent(), Is.False);
		}

		[Test]
		public void TheManagersCancelRuleIsUntouchedAndStillRequiresExhaustedPlusACurrentCertificate()
		{
			var m = Manager();
			Assert.That(m, Does.Not.Contain("ForceAfter"), "STALL-FIX-01 does not touch the queue manager");
			Assert.That(m, Does.Not.Contain("PollForced"));
			Assert.That(Count(m, "refineryExhausted = true;"), Is.EqualTo(1));
			Assert.That(Regex.IsMatch(m, @"if \(fallback == RefineryFallbackOutcome\.Exhausted\)\s+refineryExhausted = true;"), Is.True);

			var exhausted = Section(m, "if (refineryExhausted)", "REF-2 P1a (retained-refinery): a completed refinery is NEVER");
			Assert.That(exhausted.IndexOf("CertificateAuthorizes(", StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0));
			Assert.That(exhausted.IndexOf("CertificateAuthorizes(", StringComparison.Ordinal),
				Is.LessThan(exhausted.IndexOf("Order.CancelProduction", StringComparison.Ordinal)), "the certificate check precedes the cancel order");
			Assert.That(Count(exhausted, "Order.CancelProduction"), Is.EqualTo(1));
			Assert.That(exhausted, Does.Contain("exhaustion certificate stale"));

			// The only other cancel on the refinery-law branch would be a regression: no cancel outside the certificate arm of this block.
			var lawBlock = Section(m, "if (refineryDefers)", "REF-2 P1a (retained-refinery): a completed refinery is NEVER");
			Assert.That(Count(lawBlock, "Order.CancelProduction"), Is.EqualTo(1));
		}

		[Test]
		public void TheForcedBranchNeverNamesACancelAndTheScanFileNeverNamesOne()
		{
			var forced = Section(Planner(), "if (scan.ForceDue(", "switch (verdict)");
			Assert.That(forced, Does.Not.Contain("Cancel"));
			Assert.That(forced, Does.Not.Contain("QueueOrder"));
			Assert.That(forced, Does.Not.Contain("Verdict.Exhausted"), "the verdict still maps through the existing switch only — the forced branch rewrites nothing");
			Assert.That(forced, Does.Not.Contain("Outcome.Exhausted"));
			Assert.That(Count(forced, "verdict = "), Is.EqualTo(2), "exactly two assignments: PollForced in the forced branch, the original Poll in the else");
			Assert.That(forced, Does.Contain("verdict = scan.PollForced("));
			Assert.That(Scan(), Does.Not.Contain("CancelProduction"));
		}

		// ---- the bound ----

		[Test]
		public void TheBoundIsNotDueBeforeNTicksAndDueAtAndAfterN()
		{
			var scan = NewScan(100);
			Assert.That(scan.ForceDue(Bound, 100), Is.False);
			Assert.That(scan.ForceDue(Bound, 100 + Bound - 1), Is.False, "one tick short");
			Assert.That(scan.ForceDue(Bound, 100 + Bound), Is.True, "exactly N ticks");
			Assert.That(scan.ForceDue(Bound, 100 + Bound + 1), Is.True);
			Assert.That(scan.ForceDue(Bound, 100 + 10 * Bound), Is.True);
		}

		[TestCase(0)]
		[TestCase(-1)]
		[TestCase(int.MinValue)]
		public void AZeroOrNegativeBoundNeverForces(int bound)
		{
			var scan = NewScan(0);
			foreach (var now in new[] { 0, 1, Bound, 100000, int.MaxValue / 2 })
				Assert.That(scan.ForceDue(bound, now), Is.False, $"bound {bound} at tick {now}");
		}

		[Test]
		public void TheClockStartsAtTheScansFirstTickNotAtTickZero()
		{
			// The module sets ForceStartTick when the scan is created: a scan born at tick 30000 is not due at tick 30001.
			var scan = NewScan(30000);
			Assert.That(scan.ForceDue(Bound, 30001), Is.False);
			Assert.That(scan.ForceDue(Bound, 30000 + Bound), Is.True);
		}

		[Test]
		public void ADeferredScanIsForcedAtTheBoundThenReplaysItsVerdictAndForcesAgainOnlyOncePerEpoch()
		{
			var w = new FakeWorld();
			var scan = NewScan(1000);
			var forcedAt = new List<int>();
			var verdicts = new List<RefineryFallbackScan.Verdict>();
			for (var now = 1000; now < 1000 + Bound + 5 * Epoch + 40; now += 10)
			{
				w.Actors++;
				RefineryFallbackScan.Verdict v;
				if (scan.ForceDue(Bound, now))
				{
					forcedAt.Add(now);
					v = scan.PollForced(30, now, () => Snap(w), out _);
				}
				else
					v = scan.Poll(NormalBudget, now, () => Snap(w), out _);

				verdicts.Add(v);
			}

			Assert.That(forcedAt, Is.EqualTo(new[] { 2500, 2550, 2600, 2650, 2700, 2750 }), "first at start + N exactly, then once per relist epoch");
			Assert.That(verdicts.Take(Bound / 10).All(v => v == RefineryFallbackScan.Verdict.Deferred), Is.True, "Deferred only before the bound");
			Assert.That(verdicts.Skip(Bound / 10).All(v => v == RefineryFallbackScan.Verdict.Exhausted), Is.True, "from the force on the verdict is Exhausted, never Deferred");
		}

		[Test]
		public void AConclusiveVerdictInsideItsEpochIsNeverForcedAgain()
		{
			var w = new FakeWorld();
			var scan = NewScan(0);
			scan.PollForced(30, 5000, () => Snap(w), out _);
			Assert.That(scan.ForcedPolls, Is.EqualTo(1));
			for (var now = 5001; now < 5000 + Epoch; now++)
				Assert.That(scan.ForceDue(Bound, now), Is.False, $"tick {now}");

			Assert.That(scan.ForceDue(Bound, 5000 + Epoch), Is.True, "after the epoch the relist is forced fresh too");
		}

		[Test]
		public void AnAbortedPassIsNotConclusiveSoTheForceFiresInsideItsWait()
		{
			var w = new FakeWorld();
			var scan = NewScan(0);
			scan.Poll(NormalBudget, 0, () => Snap(w), out _);
			w.Actors++;
			scan.Poll(NormalBudget, 10, () => Snap(w), out _);

			Assert.That(scan.ReplayingConclusiveVerdict(11), Is.False);
			Assert.That(scan.ForceDue(Bound, Bound + 11), Is.True);
		}

		[Test]
		public void ANaturalExhaustedInsideItsEpochIsNotForcedEvenPastTheBound()
		{
			var w = new FakeWorld();
			var scan = NewScan(-2000);
			while (scan.Poll(NormalBudget, 0, () => Snap(w), out _) == RefineryFallbackScan.Verdict.Deferred)
			{
			}

			Assert.That(scan.ReplayingConclusiveVerdict(1), Is.True);
			Assert.That(scan.ForceDue(Bound, 10), Is.False, "elapsed 2010 >= 1500 but the conclusive verdict is still in force");
			Assert.That(scan.ForceDue(Bound, Epoch), Is.True, "past the epoch it re-lists, forced");
		}

		// ---- CI off = identical ----

		[Test]
		public void TheInfoDefaultIsOffAndTheYamlOnValueIs1500()
		{
			var info = (ExpansionPlannerBotModuleInfo)Activator.CreateInstance(typeof(ExpansionPlannerBotModuleInfo), true);
			var field = typeof(ExpansionPlannerBotModuleInfo).GetField("RefineryFallbackForceAfterTicks", BindingFlags.Public | BindingFlags.Instance);
			Assert.That(field, Is.Not.Null);
			Assert.That(field.IsInitOnly, Is.True);
			Assert.That(field.GetValue(info), Is.EqualTo(0), "off by default");
			Assert.That(Planner(), Does.Contain("public readonly int RefineryFallbackForceAfterTicks = 0;"));
		}

		[Test]
		public void WithTheBoundOffThePollSequenceIsIdenticalToThePlainBudgetedPoll()
		{
			// The module's off path: ForceDue(0, ...) is consulted and never true, so every poll is the plain Poll.
			var plainWorld = new FakeWorld();
			var gatedWorld = new FakeWorld();
			var plain = NewScan();
			var gated = NewScan();
			for (var i = 0; i < 300; i++)
			{
				plainWorld.Actors += i % 3 == 0 ? 1 : 0;
				gatedWorld.Actors += i % 3 == 0 ? 1 : 0;
				var now = i * 10;
				var a = plain.Poll(NormalBudget, now, () => Snap(plainWorld), out var sa);
				Assert.That(gated.ForceDue(0, now), Is.False);
				var b = gated.Poll(NormalBudget, now, () => Snap(gatedWorld), out var sb);
				Assert.That(b, Is.EqualTo(a), $"poll {i}");
				Assert.That(sb, Is.EqualTo(sa));
				Assert.That(gated.LastPollWork, Is.EqualTo(plain.LastPollWork));
				Assert.That(gated.Epochs, Is.EqualTo(plain.Epochs));
				Assert.That(gated.Tier, Is.EqualTo(plain.Tier));
			}

			Assert.That(gated.ForcedPolls, Is.EqualTo(0));
		}

		[Test]
		public void TheModuleForcesOnlyThroughTheInfoSwitchAndKeepsTheOriginalPollInItsElseBranch()
		{
			var p = Planner();
			Assert.That(Count(p, "scan.ForceDue(Info.RefineryFallbackForceAfterTicks, now)"), Is.EqualTo(1), "the bound reads the Info switch");
			Assert.That(Count(p, "RefineryFallbackForceAfterTicks"), Is.EqualTo(5), "declaration, the comment, the ForceDue call, the evidence line's bound and the R1 cancel gate");
			Assert.That(Regex.IsMatch(p, @"ForceDue\(\s*\d"), Is.False, "no literal bound");

			var poll = Section(p, "if (scan.ForceDue(", "switch (verdict)");
			Assert.That(poll, Does.Contain("\t\t\telse\n\t\t\t\tverdict = scan.Poll(budget, now,\n\t\t\t\t\t() => FallbackSnapshot(actorInfo, bi, producer, distanceToBaseIsImportant), out site);"));

			// The three verdict arms after the branch are the pre-existing ones.
			var arms = Section(p, "switch (verdict)", "bool IBotExpansionTargetProvider.RefineryFallbackCertificateCurrent");
			Assert.That(arms, Does.Contain("case RefineryFallbackScan.Verdict.Site:"));
			Assert.That(arms, Does.Contain("case RefineryFallbackScan.Verdict.Exhausted:\n\t\t\t\t\treturn RefineryFallbackOutcome.Exhausted;"));
			Assert.That(arms, Does.Contain("if (!scan.TerminalTier)\n\t\t\t\t\t\tscan.AdvanceTier();\n\t\t\t\t\treturn RefineryFallbackOutcome.Deferred;"));
		}

		[Test]
		public void TheScansStartClockIsSetWhereTheScanIsCreatedSoThePruneDropsBothTogether()
		{
			var p = Planner();
			Assert.That(p, Does.Contain("fallbackScans[key] = scan = new RefineryFallbackScan(Info.RefineryFallbackRelistEpochTicks) { ForceStartTick = now };"));
			Assert.That(Count(p, "ForceStartTick"), Is.EqualTo(2), "creation plus the evidence line");

			// PruneFallbackScans itself is unchanged: it removes scans, and the clock lives on the scan.
			var prune = Section(p, "void PruneFallbackScans(int now)", "// ECON-A-FIX (R6): the claim surface's three states");
			Assert.That(prune, Does.Not.Contain("Force"));
			Assert.That(Count(prune, "fallbackScans.Remove(key);"), Is.EqualTo(2));
		}

		[Test]
		public void TheEvidenceLineIsWrittenOnlyInsideTheForcedBranch()
		{
			var p = Planner();
			Assert.That(Count(p, "STALL-FIX-01 refinery fallback scan"), Is.EqualTo(0), "the line text lives in the pure ForcedLine formatter");
			Assert.That(Count(Scan(), "STALL-FIX-01 refinery fallback scan"), Is.EqualTo(1));
			Assert.That(Count(p, "RefineryFallbackScan.ForcedLine("), Is.EqualTo(1));
			var forced = Section(p, "if (scan.ForceDue(", "else\n\t\t\t\tverdict = scan.Poll(");
			Assert.That(forced, Does.Contain("RefineryFallbackScan.ForcedLine("));
			Assert.That(forced, Does.Contain("Log.Write(\"debug\", forced);"));
			Assert.That(forced, Does.Not.Contain("Random"));
			Assert.That(forced, Does.Not.Contain("Owner"));
		}

		[Test]
		public void TheScanFileStaysWorldFreeAndIntegerOnly()
		{
			var scan = Scan();
			Assert.That(scan, Does.Not.Contain("Random"));
			Assert.That(scan, Does.Not.Contain("double"));
			Assert.That(scan, Does.Not.Contain("float"));
			Assert.That(scan, Does.Not.Contain("Player"));
		}

		// ---- R1: the cancel needs TWO Exhausted verdicts at least one relist epoch apart ----

		static RefineryFallbackScan.Verdict Force(RefineryFallbackScan scan, FakeWorld w, int now, bool providers = true) =>
			scan.PollForced(30, now, () => Snap(w, providers: providers), out _);

		[Test]
		public void R1TheFirstForcedExhaustedOnlyHoldsAndTheSecondOneAnEpochLaterAuthorizes()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Assert.That(Force(scan, w, 1000), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.CertificateCurrent(), Is.True, "the first verdict has a current certificate");
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.False, "but one Exhausted does not authorize the cancel");
			Assert.That(scan.ExhaustedChain, Is.EqualTo(1));
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(1000));

			Assert.That(Force(scan, w, 1000 + Epoch), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.ExhaustedChain, Is.EqualTo(2));
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.True, "the second Exhausted, exactly one epoch after the first, with a current certificate");
		}

		[Test]
		public void R1TwoExhaustedLessThanAnEpochApartDoNotAuthorizeAndALaterThirdAgainstTheFirstDoes()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Force(scan, w, 1000);
			Force(scan, w, 1000 + Epoch - 1);
			Assert.That(scan.CertificateCurrent(), Is.True);
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.False, "one tick short of the epoch");
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(1000), "the record keeps the chain's FIRST verdict");

			Force(scan, w, 1000 + Epoch + 7);
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.True, "a later verdict is a separate proof against the first");
		}

		[Test]
		public void R1AnExhaustedThenASiteClearsTheRecordSoTheNextExhaustedIsAFirstAgain()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Force(scan, w, 1000);
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(1000));

			w.LegalX = 70;
			w.LegalY = 61;
			Assert.That(Force(scan, w, 1000 + Epoch), Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(int.MinValue), "a Site clears the first-Exhausted record");
			Assert.That(scan.ExhaustedChain, Is.EqualTo(0));

			w.LegalX = -1;
			w.LegalY = -1;
			Assert.That(Force(scan, w, 1000 + 2 * Epoch), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.False, "this Exhausted is the first of a new chain");
			Assert.That(scan.ExhaustedChain, Is.EqualTo(1));
		}

		[Test]
		public void R1ADrainedNonExhaustedPassClearsTheRecord()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Force(scan, w, 1000);

			// No provider: the pass drains NoSite (never certifies) — breaks the chain.
			Assert.That(Force(scan, w, 1000 + Epoch, providers: false), Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(int.MinValue));
			Force(scan, w, 1000 + 2 * Epoch);
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.False, "first of a new chain after the NoSite");
		}

		[Test]
		public void R1AnAbortedPassAlsoClearsTheRecord()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Force(scan, w, 1000);
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(1000));

			var torn = new RefineryFallbackScan.Snapshot(
				() => new List<RefineryFallbackScan.Provider?> { new RefineryFallbackScan.Provider(new CPos(5, 5), 3, 1) }.GetEnumerator(),
				() => new List<CPos>().GetEnumerator(),
				() => new List<CPos> { new CPos(0, 0) }.GetEnumerator(),
				Unrestricted,
				new[] { new CVec(0, 0) },
				TornAtoms,
				TornAtoms,
				c => 0,
				i => new CPos(0, 0),
				2,
				c => true,
				c => false);
			Assert.That(scan.PollForced(30, 1000 + Epoch, () => torn, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(scan.FirstExhaustedTick, Is.EqualTo(int.MinValue), "an aborted pass clears it too");
		}

		[Test]
		public void R1ADroppedScanTakesTheRecordWithIt()
		{
			// PruneFallbackScans / the Site path drop the scan object: the record lives on it, so a replacement scan starts a new chain.
			var w = new FakeWorld();
			var old = NewScan();
			Force(old, w, 1000);
			Force(old, w, 1000 + Epoch);
			Assert.That(old.CancelCertificateCurrent(Bound), Is.True);

			var fresh = NewScan(1000);
			Assert.That(fresh.FirstExhaustedTick, Is.EqualTo(int.MinValue));
			Force(fresh, w, 1000 + 2 * Epoch);
			Assert.That(fresh.CancelCertificateCurrent(Bound), Is.False, "the replacement scan's first Exhausted only holds");
		}

		[Test]
		public void R1TheSecondVerdictStillNeedsACurrentCertificate()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Force(scan, w, 1000);
			Force(scan, w, 1000 + Epoch);
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.True);

			w.Actors++;
			Assert.That(scan.ExhaustedConfirmed, Is.True, "the gate is satisfied ...");
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.False, "... but an atom drift fails the certificate closed");
			w.Actors--;
			scan.MarkDirtyCell(new CPos(2, 2));
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.False, "and so does a covered event after issue");
		}

		[Test]
		public void R1AReplayedVerdictInsideTheEpochIsNotASecondVerdictButANaturalReListIs()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Force(scan, w, 1000);
			for (var now = 1001; now < 1000 + Epoch; now += 7)
			{
				Assert.That(scan.Poll(NormalBudget, now, () => Snap(w), out _), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted), "replay");
				Assert.That(scan.ExhaustedChain, Is.EqualTo(1), "a replay is not a fresh proof");
				Assert.That(scan.CancelCertificateCurrent(Bound), Is.False);
			}

			// Past the epoch the budgeted pass re-lists; on a static world it completes and is the second proof.
			var t = 1000 + Epoch;
			RefineryFallbackScan.Verdict v;
			do
				v = scan.Poll(NormalBudget, t, () => Snap(w), out _);
			while (v == RefineryFallbackScan.Verdict.Deferred);
			Assert.That(v, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.ExhaustedChain, Is.EqualTo(2));
			Assert.That(scan.CancelCertificateCurrent(Bound), Is.True);
		}

		[Test]
		public void R1TheModuleLoopCancelsOnlyAtTheSecondForcedExhaustedAnEpochAfterTheFirst()
		{
			// The module's CI-on sequence as the manager drives it: poll every ~130 ticks; on Exhausted the cancel arm asks
			// CancelCertificateCurrent in the same sweep. Epoch 50 here, so the second force comes at the first poll >= 50 later.
			var w = new FakeWorld();
			var scan = NewScan(1000);
			var firstExhausted = -1;
			var cancelAt = -1;
			var cancels = 0;
			for (var now = 1000; now < 1000 + Bound + 1500; now += 130)
			{
				w.Actors++;
				var v = scan.ForceDue(Bound, now) ? scan.PollForced(30, now, () => Snap(w), out _) : scan.Poll(NormalBudget, now, () => Snap(w), out _);
				if (v != RefineryFallbackScan.Verdict.Exhausted)
					continue;

				firstExhausted = firstExhausted < 0 ? now : firstExhausted;
				if (scan.CancelCertificateCurrent(Bound))
				{
					cancels++;
					cancelAt = cancelAt < 0 ? now : cancelAt;
				}
			}

			Assert.That(firstExhausted, Is.GreaterThanOrEqualTo(1000 + Bound), "no Exhausted before the bound");
			Assert.That(cancelAt, Is.GreaterThan(firstExhausted), "never at the first Exhausted");
			Assert.That(cancelAt - scan.FirstExhaustedTick, Is.GreaterThanOrEqualTo(Epoch));
			Assert.That(cancels, Is.GreaterThanOrEqualTo(1));
		}

		[Test]
		public void R1WithTheSwitchOffTheCancelCheckIsExactlyTheCertificateCheck()
		{
			var w = new FakeWorld();
			var scan = NewScan();
			Assert.That(scan.CancelCertificateCurrent(0), Is.EqualTo(scan.CertificateCurrent()));
			Force(scan, w, 1000);
			Assert.That(scan.CertificateCurrent(), Is.True);
			Assert.That(scan.CancelCertificateCurrent(0), Is.True, "OFF: one proven Exhausted with a current certificate cancels, as before");
			Assert.That(scan.CancelCertificateCurrent(-5), Is.True);
			w.Actors++;
			Assert.That(scan.CancelCertificateCurrent(0), Is.False);
			Assert.That(scan.CancelCertificateCurrent(0), Is.EqualTo(scan.CertificateCurrent()));

			// A natural budgeted Exhausted (the only kind OFF ever sees) behaves the same.
			var w2 = new FakeWorld();
			var natural = NewScan();
			while (natural.Poll(NormalBudget, 0, () => Snap(w2), out _) == RefineryFallbackScan.Verdict.Deferred)
			{
			}

			Assert.That(natural.CancelCertificateCurrent(0), Is.True);
			Assert.That(natural.CancelCertificateCurrent(Bound), Is.False, "CI on: the first Exhausted of the chain only holds, forced or not");
		}

		[Test]
		public void R1TheModuleCancelArmReadsTheGateThroughTheInfoSwitchAndTheManagerIsUntouched()
		{
			var p = Planner();
			var arm = Section(p, "bool IBotExpansionTargetProvider.RefineryFallbackCertificateCurrent", "void IBotExpansionTargetProvider.RefinerySiteCommitted");
			Assert.That(arm, Does.Contain("scan.CancelCertificateCurrent(Info.RefineryFallbackForceAfterTicks)"));
			Assert.That(arm, Does.Not.Contain("scan.CertificateCurrent()"));
			Assert.That(Manager(), Does.Not.Contain("CancelCertificateCurrent"));
			Assert.That(Manager(), Does.Contain("law.RefineryFallbackCertificateCurrent(queue.Actor, itemName)"));
			Assert.That(Scan(), Does.Contain("CertificateCurrent() && (forceAfterTicks <= 0 || ExhaustedConfirmed)"));
		}

		// ---- R3: the forced Site's distance from the base rides the telemetry line (record only) ----

		[Test]
		public void R3TheForcedSiteLineCarriesItsChebyshevAndSquaredDistanceFromTheBase()
		{
			var line = RefineryFallbackScan.ForcedLine(2, "td_nod_tiberiumrefinery", 77, 1530, 1500, 1, RefineryFallbackScan.Verdict.Site,
				new CPos(70, 61), new CPos(33, 40), 6400, 3, 0, false, 41234);
			Assert.That(line, Does.Contain("STALL-FIX-01 refinery fallback scan td_nod_tiberiumrefinery on producer 77 forced after 1530 deferred ticks (bound 1500, force #1): Site at 70,61"));
			Assert.That(line, Does.Contain("base distance 37 cells (sq 1810) from 33,40"));
			Assert.That(line, Does.EndWith("at tick 41234"));
		}

		[Test]
		public void R3AnExhaustedLineCarriesTheChainAndTheGateNotADistance()
		{
			var line = RefineryFallbackScan.ForcedLine(2, "x", 1, 1500, 1500, 2, RefineryFallbackScan.Verdict.Exhausted, null, new CPos(1, 1), 99, 4, 2, true, 5000);
			Assert.That(line, Does.Contain("Exhausted, work 99, epochs 4, exhausted chain 2, cancel gate satisfied at tick 5000"));
			Assert.That(line, Does.Not.Contain("base distance"));
			Assert.That(RefineryFallbackScan.ForcedLine(2, "x", 1, 1500, 1500, 1, RefineryFallbackScan.Verdict.Exhausted, null, null, 1, 1, 1, false, 1),
				Does.Contain("cancel gate not satisfied"));
		}

		[TestCase(33, 40, 70, 61, 37, 1810L)]
		[TestCase(70, 61, 33, 40, 37, 1810L)]
		[TestCase(5, 5, 5, 5, 0, 0L)]
		[TestCase(-3, 8, 4, -2, 10, 149L)]
		public void R3TheDistanceIsAnIntegerChebyshevPlusASquaredLongAndSymmetric(int ax, int ay, int bx, int by, int cheb, long sq)
		{
			Assert.That(RefineryFallbackScan.ChebyshevCells(new CPos(ax, ay), new CPos(bx, by)), Is.EqualTo(cheb));
			Assert.That(RefineryFallbackScan.SquaredCells(new CPos(ax, ay), new CPos(bx, by)), Is.EqualTo(sq));
		}

		[Test]
		public void R3TheDistanceIsRecordOnlyTheModulePassesItToTheFormatterAndNothingReadsItBack()
		{
			var p = Planner();
			Assert.That(p, Does.Contain("baseCenter ?? producer.Location"));
			Assert.That(Count(p, "ChebyshevCells("), Is.EqualTo(0), "the planner makes no distance decision");
			Assert.That(Count(Scan(), "ChebyshevCells("), Is.EqualTo(2), "definition plus the formatter's single use");
			Assert.That(Count(Scan(), "SquaredCells("), Is.EqualTo(2));
		}

		// ---- R2: the forced poll's cost on a synthetic 144x144 world ----

		/// <summary>144x144 map, 70% blocked terrain, 3000 occupied cells, 1000 actors (about 1.5 atoms each), 25 providers with
		/// reach 12, 300 rank cells. Legal = a 4x3 footprint clear of terrain and occupancy (12 table lookups) inside a provider square
		/// or, with the open region, anywhere in it.</summary>
		sealed class BigWorld
		{
			public const int N = 144;
			public readonly bool[] Blocked = new bool[N * N];
			public readonly HashSet<CPos> Occupied = new();
			public readonly List<CPos> Providers = new();
			public readonly List<CPos> Ranks = new();
			public int Actors = 1000;
			readonly bool openRegion;

			public BigWorld(bool legalRegion)
			{
				openRegion = legalRegion;
				var rng = 12345u;
				int Next()
				{
					rng = rng * 1664525u + 1013904223u;
					return (int)(rng >> 8) & 0xffff;
				}

				for (var i = 0; i < N * N; i++)
					Blocked[i] = Next() % 100 < 70;
				for (var i = 0; i < 3000; i++)
					Occupied.Add(new CPos(Next() % N, Next() % N));
				for (var i = 0; i < 25; i++)
					Providers.Add(new CPos(10 + (i % 5) * 25, 10 + (i / 5) * 25));
				for (var i = 0; i < 300; i++)
					Ranks.Add(new CPos(Next() % N, Next() % N));
				if (legalRegion)
				{
					for (var x = 100; x < 140; x++)
						for (var y = 100; y < 140; y++)
						{
							Blocked[y * N + x] = false;
							Occupied.Remove(new CPos(x, y));
						}
				}
			}

			public bool Legal(CPos c)
			{
				for (var dx = 0; dx < 4; dx++)
				{
					for (var dy = 0; dy < 3; dy++)
					{
						var x = c.X + dx;
						var y = c.Y + dy;
						if (x >= N || y >= N || Blocked[y * N + x] || Occupied.Contains(new CPos(x, y)))
							return false;
					}
				}

				if (openRegion && c.X >= 100 && c.Y >= 100)
					return true;

				foreach (var p in Providers)
					if (Math.Abs(p.X - c.X) <= 12 && Math.Abs(p.Y - c.Y) <= 12)
						return true;

				return false;
			}
		}

		static IEnumerator<RefineryFallbackScan.CoherenceAtom> BigAtoms(BigWorld w)
		{
			yield return new RefineryFallbackScan.CoherenceAtom(0, 0, 1, 0, null);
			for (var i = 0; i < w.Actors; i++)
			{
				yield return new RefineryFallbackScan.CoherenceAtom(1, (uint)i, 0, 0, null);
				if ((i & 1) == 0)
					yield return new RefineryFallbackScan.CoherenceAtom(2, (uint)i, 0, 0, null);
			}
		}

		static RefineryFallbackScan.Snapshot BigSnap(BigWorld w, List<CPos> cells)
		{
			var provs = w.Providers.Select((p, i) => (RefineryFallbackScan.Provider?)new RefineryFallbackScan.Provider(p, 12, (uint)(i + 1))).ToList();
			return new RefineryFallbackScan.Snapshot(
				() => provs.GetEnumerator(),
				() => w.Ranks.GetEnumerator(),
				() => cells.GetEnumerator(),
				Unrestricted,
				new[] { new CVec(0, 0), new CVec(1, 0), new CVec(2, 0), new CVec(3, 0), new CVec(0, 1), new CVec(1, 1), new CVec(2, 1), new CVec(3, 1), new CVec(0, 2), new CVec(1, 2), new CVec(2, 2), new CVec(3, 2) },
				() => BigAtoms(w),
				() => BigAtoms(w),
				c => c.X >= 0 && c.Y >= 0 && c.X < BigWorld.N && c.Y < BigWorld.N ? c.Y * BigWorld.N + c.X : -1,
				i => new CPos(i % BigWorld.N, i / BigWorld.N),
				6,
				c => true,
				w.Legal);
		}

		static (double Median, double Max, RefineryFallbackScan.Verdict Verdict, int Work) TimeForced(BigWorld w, int runs)
		{
			var cells = Enumerable.Range(0, BigWorld.N).SelectMany(y => Enumerable.Range(0, BigWorld.N).Select(x => new CPos(x, y))).ToList();
			var times = new List<double>();
			var verdict = RefineryFallbackScan.Verdict.Deferred;
			var work = 0;
			for (var i = 0; i < runs + 3; i++)
			{
				var scan = NewScan();
				var sw = System.Diagnostics.Stopwatch.StartNew();
				verdict = scan.PollForced(30, 0, () => BigSnap(w, cells), out _);
				sw.Stop();
				work = scan.LastPollWork;
				if (i >= 3)
					times.Add(sw.Elapsed.TotalMilliseconds);
			}

			times.Sort();
			return (times[times.Count / 2], times[times.Count - 1], verdict, work);
		}

		[Test]
		public void R2TheForcedPollOnASynthetic144x144WorldIsWithinTwentyMillisecondsMedian()
		{
			// Exhausted-class world: no legal cell anywhere (70% blocked terrain, no open region).
			var w = new BigWorld(false);
			var (median, max, verdict, work) = TimeForced(w, 15);
			var line = $"R2 forced poll 144x144 Exhausted-class: median {median:F2} ms, max {max:F2} ms, verdict {verdict}, work units {work}, cells {BigWorld.N * BigWorld.N}, atoms ~{w.Actors * 3 / 2}";
			TestContext.Out.WriteLine(line);
			Console.WriteLine(line);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(work, Is.GreaterThan(BigWorld.N * BigWorld.N), "one poll examined the whole map");
			Assert.That(median, Is.LessThanOrEqualTo(20.0), "Opus budget for one forced poll");
		}

		[Test]
		public void R2TheForcedPollWithAManyLegalCellsWorstCaseIsRecordedAndBoundedGenerously()
		{
			// Site-class worst case: a 40x40 open region = ~1.4k legal cells to rank (12 footprint tiles x 300 rank cells each).
			var w = new BigWorld(true);
			var (median, max, verdict, work) = TimeForced(w, 9);
			var line = $"R2 forced poll 144x144 Site-class (open 40x40 region): median {median:F2} ms, max {max:F2} ms, verdict {verdict}, work units {work}";
			TestContext.Out.WriteLine(line);
			Console.WriteLine(line);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(median, Is.LessThanOrEqualTo(500.0), "generous: recorded in the notes, not a budget");
		}

		// ---- switch yaml wiring (as CH) ----

		[Test]
		public void TheSwitchGroupIsACapabilityWithNoNeedsAndSetsOnlyTheForceBound()
		{
			var yaml = Fixture("increment_switches.yaml");
			Assert.That(yaml, Does.Contain("\n  CI_refinery_stall_fix: capability "));
			var needs = Section(yaml, "\nneeds:", "\neffect_classes:");
			Assert.That(needs, Does.Not.Contain("CI_refinery_stall_fix"), "independent of the CD..CH group");

			var groups = yaml.Substring(yaml.IndexOf("\ngroups:", StringComparison.Ordinal));
			var start = groups.IndexOf("\n  CI_refinery_stall_fix:", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(groups.IndexOf("\n  CH_retreat_rally:", StringComparison.Ordinal)), "the new letter follows CH");
			var group = groups.Substring(start).TrimEnd();
			Assert.That(group, Does.Contain("ExpansionPlannerBotModule:\n      RefineryFallbackForceAfterTicks: 1500"));
			Assert.That(Regex.Matches(group, @"\n      \w+:").Count, Is.EqualTo(1), "sets exactly one field");
			Assert.That(group, Does.Not.Contain("SquadManagerBotModuleCA"));
			Assert.That(group, Does.Not.Contain("MasterAiBotModule"));
			Assert.That(Regex.Matches(yaml, @"\n\s+RefineryFallbackForceAfterTicks:").Count, Is.EqualTo(1), "no other group sets the bound");
		}
	}
}
