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
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// REF-2 P1a (retained-refinery fallback): the world-free half of the feature —
	/// RefineryFallbackScan's bounded enumeration, global-nearest ordering, pick-time
	/// revalidation and bounded epoch re-listing, plus the hold-reason and retention
	/// predicates in BaseBuilderQueueEvalCA. Authored only: the qualification lane runs
	/// them (the P1a claim forbids executing tests here).
	/// </summary>
	[TestFixture]
	public sealed class RefineryFallbackTest
	{
		// ---- helpers -------------------------------------------------------

		/// One provider whose "disk" is the given cells, pre-sorted as the snapshot
		/// contract requires (dist^2, X, Y from the provider centre).
		static RefineryFallbackScan.Snapshot SingleProvider(
			IEnumerable<CPos> cells,
			Func<CPos, bool> legal,
			Func<CPos, int> rank)
		{
			var disk = cells.ToArray();
			return new RefineryFallbackScan.Snapshot(
				providers: new[] { (new CPos(0, 0), 100) },
				disk: (center, reach) => disk,
				legal: legal,
				rank: c => (rank(c), 1));
		}

		static Func<CPos, bool> LegalAll() => _ => true;

		static Func<CPos, bool> LegalOnly(IReadOnlySet<CPos> allowed) => allowed.Contains;

		// Rank = squared distance to a fixed resource point, like the map-static field.
		static Func<CPos, int> RankFrom(CPos resource) =>
			c => (c - resource).LengthSquared;

		// ---- fallback ordering / cursor ------------------------------------

		[Test]
		public void NoPickBeforeFrontierDrains()
		{
			// Five cells, budget 2/poll — the pick must wait for the full enumeration
			// even though the best cell is enumerated in the first sweep.
			var cells = Enumerable.Range(0, 5).Select(i => new CPos(i, 0)).ToArray();
			var legal = new HashSet<CPos>(cells);
			var scan = new RefineryFallbackScan(50);
			var polled = 0;
			var snapshot = SingleProvider(cells, LegalOnly(legal), RankFrom(new CPos(0, 0)));

			CPos? site = null;
			var verdict = scan.Poll(2, 0, () => snapshot, out site);
			polled++;
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(site, Is.Null);

			while (verdict == RefineryFallbackScan.Verdict.Deferred)
			{
				verdict = scan.Poll(2, polled, () => snapshot, out site);
				polled++;
				Assert.That(polled, Is.LessThan(10), "bounded enumeration must terminate");
			}

			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(0, 0)));
		}

		[Test]
		public void GloballyNearestWinsNotEnumerationOrder()
		{
			// The first-enumerated legal cell is the FARTHEST from the resource; the
			// globally nearest must win even though it is enumerated last.
			var cells = new[] { new CPos(50, 0), new CPos(30, 0), new CPos(5, 0) };
			var legal = new HashSet<CPos>(cells);
			var scan = new RefineryFallbackScan(50);
			var snapshot = SingleProvider(cells, LegalOnly(legal), RankFrom(new CPos(0, 0)));

			var verdict = scan.Poll(1000, 0, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(5, 0)));
		}

		[Test]
		public void EqualRankBreaksByCellXY()
		{
			var cells = new[] { new CPos(5, 4), new CPos(5, 1), new CPos(1, 9) };
			var legal = new HashSet<CPos>(cells);
			var scan = new RefineryFallbackScan(50);
			var snapshot = SingleProvider(cells, LegalOnly(legal), _ => 7);

			var verdict = scan.Poll(1000, 0, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(1, 9)), "equal rank: lowest X wins, then Y");
		}

		[Test]
		public void PickTimeRevalidationSelectsNextBestStillLegal()
		{
			// Occupancy changes between enumeration and pick: the best cell goes
			// illegal at revalidation, so the second-best still-legal cell is picked.
			// The pass snapshot is frozen once taken — the mutable legality set models
			// the world changing underneath a live pass.
			var cells = new[] { new CPos(1, 0), new CPos(2, 0), new CPos(3, 0) };
			var legal = new HashSet<CPos>(cells);
			var scan = new RefineryFallbackScan(50);
			var snapshot = SingleProvider(cells, c => legal.Contains(c), RankFrom(new CPos(0, 0)));

			Assert.That(scan.Poll(2, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred),
				"the oversized disk materializes under charge before any cell enumerates");
			Assert.That(scan.Poll(2, 1, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred),
				"(1,0) enumerated legal — one legal cell per sweep at this budget");

			legal.Remove(new CPos(1, 0));

			Assert.That(scan.Poll(2, 2, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred),
				"(2,0) enumerated legal — (1,0) stays a candidate until pick-time revalidation");
			var verdict = scan.Poll(1000, 3, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(2, 0)), "the next-ranked still-legal cell wins after the best fails revalidation");
		}

		[Test]
		public void TwoProviderDomainsEnumerateOnceEach()
		{
			// Overlapping provider disks dedup: the shared cell is visited once, and the
			// pick still sees the global ranking across both disks.
			var a = new[] { new CPos(10, 0), new CPos(11, 0) };
			var b = new[] { new CPos(11, 0), new CPos(2, 0) };
			var legal = new HashSet<CPos>(a.Concat(b));
			var enumerated = new List<CPos>();
			var scan = new RefineryFallbackScan(50);
			var snapshot = new RefineryFallbackScan.Snapshot(
				new[] { (new CPos(0, 0), 10), (new CPos(40, 0), 10) },
				(center, reach) => center.X == 0 ? a : b,
				c => { enumerated.Add(c); return legal.Contains(c); },
				c => ((c - new CPos(0, 0)).LengthSquared, 1));

			var verdict = scan.Poll(1000, 0, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(2, 0)));
			Assert.That(enumerated.Distinct().Count(), Is.EqualTo(enumerated.Count), "each cell enumerated once across overlapping disks");
		}

		// ---- verdicts -------------------------------------------------------

		[Test]
		public void EmptyDomainVerdictsNoUniverse()
		{
			var scan = new RefineryFallbackScan(50);
			var verdict = scan.Poll(100, 0, () => SingleProvider(Array.Empty<CPos>(), LegalAll(), _ => 0), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.NoUniverse));
			Assert.That(site, Is.Null);
		}

		[Test]
		public void DrainedIllegalDomainVerdictsNoSite()
		{
			var cells = new[] { new CPos(1, 1), new CPos(2, 2) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = SingleProvider(cells, _ => false, _ => 0);
			var verdict = scan.Poll(100, 0, () => snapshot, out _);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
		}

		[Test]
		public void EpochRelistsUnderFreshSnapshot()
		{
			// Pass 1: the only candidate is illegal → NoSite and the epoch gate closes.
			// During the gate, polls report the consumed verdict without doing work.
			// At the epoch a fresh snapshot is taken — newly-legal cells are seen.
			var cell = new CPos(4, 0);
			var legal = new HashSet<CPos>();
			var scan = new RefineryFallbackScan(25);
			var snapshot = SingleProvider(new[] { cell }, LegalOnly(legal), RankFrom(new CPos(0, 0)));

			Assert.That(scan.Poll(100, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.Poll(100, 10, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.Epochs, Is.EqualTo(1), "the epoch gate holds until the epoch ticks");

			legal.Add(cell);   // occupancy changed between passes
			var verdict = scan.Poll(100, 26, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(cell));
			Assert.That(scan.Epochs, Is.EqualTo(2), "the re-list ran a second pass under the resampled snapshot");
		}

		[Test]
		public void OversizedDiskResumesAtCursor()
		{
			// A single provider disk larger than the per-sweep budget materializes under
			// charge, then enumeration resumes at the cursor on later sweeps — never
			// restarting at the disk head.
			var cells = Enumerable.Range(0, 10).Select(i => new CPos(i, 0)).ToArray();
			var legal = new HashSet<CPos>(cells);
			var enumerated = new List<CPos>();
			var scan = new RefineryFallbackScan(50);
			var snapshot = new RefineryFallbackScan.Snapshot(
				new[] { (new CPos(0, 0), 100) },
				(center, reach) => cells,
				c => { enumerated.Add(c); return legal.Contains(c); },
				c => ((c - new CPos(0, 0)).LengthSquared, 1));

			var verdict = scan.Poll(4, 0, () => snapshot, out _);   // disk(10) > budget — materialization charged
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			var first = enumerated.Count;

			verdict = scan.Poll(4, 1, () => snapshot, out _);
			Assert.That(enumerated.Count, Is.GreaterThan(first), "enumeration resumed at the cursor, not the disk head");

			while (verdict == RefineryFallbackScan.Verdict.Deferred)
				verdict = scan.Poll(4, 2, () => snapshot, out _);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
		}

		// ---- hold-reason / retention evals ----------------------------------

		[Test]
		public void FallbackHoldReasonDistinguishesVerdicts()
		{
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.Deferred, false),
				Is.EqualTo(BotQueueTransitionReason.RefineryScanDeferred));
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.NoUniverse, false),
				Is.EqualTo(BotQueueTransitionReason.NoRefineryClaim));
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.NoUniverse, true),
				Is.EqualTo(BotQueueTransitionReason.NoRefinerySite),
				"claims were offered — an empty domain after offered claims is the drained-scan case, not the no-claim case");
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.NoSite, true),
				Is.EqualTo(BotQueueTransitionReason.NoRefinerySite));
		}

		[Test]
		public void RefineryDefersOnlyUnderLaw()
		{
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Refinery), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(false, BuildingType.Refinery), Is.False,
				"classic path unchanged — a law-less refinery still fails over the ordinary budget");
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Building), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Defense), Is.False);
		}
	}
}
