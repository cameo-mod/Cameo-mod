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
	/// REF-2 P1a (retained-refinery fallback, amended per Sol dispositions CE13F2BA,
	/// addendum 66578711, certificate per BF5B8247 option (c) and 6722A484 exact-state
	/// ruling): the world-free half of the feature — RefineryFallbackScan's pre-charged
	/// paged phase machine (provider collection, rank-set collection, paged sort,
	/// square/whole-map domain enumeration, resumable footprint-gap ranking, paged
	/// ordering, pick), the Chebyshev-square domain geometry, the relaxation-tier ladder
	/// whose terminal tier is the only certifying pass, the coherence sweep's pairwise
	/// atom proof, the selective dirty-set repair, the ∃-dock fold, and the
	/// hold-reason / no-cancel predicates in BaseBuilderQueueEvalCA. Authored only:
	/// the qualification lane runs them (the amendment claim forbids executing tests here).
	/// </summary>
	[TestFixture]
	public sealed class RefineryFallbackTest
	{
		// ---- helpers -------------------------------------------------------

		// Returns Provider? so new[] { Prov(...) } infers Provider?[] — the Snapshot
		// helpers take IReadOnlyList<Provider?> and there is no covariance over value types.
		static RefineryFallbackScan.Provider? Prov(int x, int y, int reach, uint id = 1) =>
			new RefineryFallbackScan.Provider(new CPos(x, y), reach, id);

		/// A single-tile footprint keeps the pre-amend topLeft-distance tests meaningful
		/// under the footprint-gap metric: with one tile the gap IS the topLeft's
		/// Chebyshev distance to the nearest rank cell.
		static readonly IReadOnlyList<CVec> OneTile = new[] { new CVec(0, 0) };

		/// Bounded-but-wide tier bound: admits any realistic gap yet stays NON-terminal,
		/// so the scan enumerates the provider-square domain (the terminal tier's
		/// unconditional domain is the whole-map stream instead).
		static int BoundedWide(int tier) => 1000;

		static int Unrestricted(int tier) => int.MaxValue;

		/// The relaxation ladder the shipped snapshot uses: tier t admits footprint-gap ≤ t+1.
		static int LinearGap(int tier) => tier >= 30 ? int.MaxValue : tier + 1;

		static IEnumerator<RefineryFallbackScan.CoherenceAtom> AtomsOf(IReadOnlyList<RefineryFallbackScan.CoherenceAtom> atoms) =>
			atoms.GetEnumerator();

		static IReadOnlyList<RefineryFallbackScan.CoherenceAtom> FixedAtoms() =>
			new[] { new RefineryFallbackScan.CoherenceAtom(0, 0, 42, 0, null) };

		/// Row-major index helpers for a rectangular test map — the same index space the
		/// shipped FallbackMapIndex declares (terminal enumeration is monotone over it).
		static (Func<CPos, int>, Func<int, CPos>) IndexFns(int width, int height)
		{
			Func<CPos, int> mapIndex = c => c.X >= 0 && c.Y >= 0 && c.X < width && c.Y < height ? c.Y * width + c.X : -1;
			Func<int, CPos> indexCell = i => new CPos(i % width, i / width);
			return (mapIndex, indexCell);
		}

		/// Square-domain snapshot: providers enumerate their own Chebyshev squares on
		/// bounded tiers; the whole-map stream is supplied anyway so a tier bound that
		/// reaches int.MaxValue still has its terminal domain.
		static RefineryFallbackScan.Snapshot Snap(
			IReadOnlyList<RefineryFallbackScan.Provider?> providers,
			IReadOnlyList<CPos> rankCells,
			Func<CPos, bool> inMap,
			Func<CPos, bool> legal,
			Func<int, int> tierBound = null,
			IReadOnlyList<CVec> footprint = null,
			IReadOnlyList<CPos> wholeMap = null,
			Func<IEnumerator<RefineryFallbackScan.CoherenceAtom>> atoms = null)
		{
			var map = wholeMap ?? Array.Empty<CPos>();
			var w = map.Count > 0 ? map.Max(c => c.X) + 1 : 0;
			var h = map.Count > 0 ? map.Max(c => c.Y) + 1 : 0;
			var (mapIndex, indexCell) = IndexFns(w, h);
			atoms ??= () => AtomsOf(FixedAtoms());
			return new RefineryFallbackScan.Snapshot(
				() => providers.GetEnumerator(),
				() => rankCells.GetEnumerator(),
				() => map.AsEnumerable().GetEnumerator(),
				tierBound ?? BoundedWide,
				footprint ?? OneTile,
				atoms,
				atoms,
				mapIndex,
				indexCell,
				2,
				inMap,
				legal);
		}

		/// Whole-map snapshot: the terminal tier's declared domain is the streamed map;
		/// providers still feed the cert gate (no provider, no certificate — 6722A484).
		static RefineryFallbackScan.Snapshot WholeMapSnap(
			IEnumerable<CPos> mapCells,
			IReadOnlyList<RefineryFallbackScan.Provider?> providers,
			IReadOnlyList<CPos> rankCells,
			Func<CPos, bool> legal,
			Func<int, int> tierBound = null,
			IReadOnlyList<CVec> footprint = null,
			Func<IEnumerator<RefineryFallbackScan.CoherenceAtom>> atoms = null,
			int readRadius = 2)
		{
			var cells = mapCells.ToList();
			var w = cells.Max(c => c.X) + 1;
			var h = cells.Max(c => c.Y) + 1;
			var (mapIndex, indexCell) = IndexFns(w, h);
			atoms ??= () => AtomsOf(FixedAtoms());
			return new RefineryFallbackScan.Snapshot(
				() => providers.GetEnumerator(),
				() => rankCells.GetEnumerator(),
				() => cells.GetEnumerator(),
				tierBound ?? Unrestricted,
				footprint ?? OneTile,
				atoms,
				atoms,
				mapIndex,
				indexCell,
				readRadius,
				c => true,
				legal);
		}

		static IEnumerable<CPos> RectMap(int width, int height) =>
			Enumerable.Range(0, width).SelectMany(x => Enumerable.Range(0, height).Select(y => new CPos(x, y)));

		static Func<CPos, bool> LegalOnly(IReadOnlySet<CPos> allowed) => allowed.Contains;

		/// Polls until the scan resolves; asserts every poll's charged work stays within
		/// its budget — the AMEND-A contract that no phase runs uncharged atomics.
		static RefineryFallbackScan.Verdict Drain(RefineryFallbackScan scan, int budget, int maxPolls,
			Func<RefineryFallbackScan.Snapshot> snapshot, out CPos? site, int now0 = 0)
		{
			site = null;
			for (var i = 0; i < maxPolls; i++)
			{
				var verdict = scan.Poll(budget, now0 + i, snapshot, out site);
				Assert.That(scan.LastPollWork, Is.LessThanOrEqualTo(budget),
					$"poll {i} charged {scan.LastPollWork} units against a budget of {budget}");
				if (verdict != RefineryFallbackScan.Verdict.Deferred)
					return verdict;
			}

			Assert.Fail("the scan did not resolve inside the poll bound");
			return default;
		}

		// ---- AMEND-A: real pre-charged work in every phase ------------------

		[Test]
		public void BudgetOneChargesExactlyOneStepPerPollAcrossAllPhases()
		{
			// 3 provider-scan steps (one filtered null), 6 rank cells needing a real
			// multi-step mergesort, two reach-1 squares (9 cells each), ranking and pick —
			// every unit lands inside a charged step, so at budget 1 each poll does exactly
			// one bounded operation and the total poll count exceeds the input sizes.
			var providers = new RefineryFallbackScan.Provider?[] { Prov(0, 0, 1, 1), null, Prov(20, 0, 1, 2) };
			var ranks = new[] { new CPos(0, 0), new CPos(20, 0), new CPos(40, 0), new CPos(-10, 0), new CPos(7, 0), new CPos(3, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, ranks, c => true, c => true);

			var polls = 0;
			CPos? site;
			while (true)
			{
				var verdict = scan.Poll(1, polls, () => snapshot, out site);
				Assert.That(scan.LastPollWork, Is.EqualTo(1),
					"budget 1 must buy exactly one charged step — never an uncharged atomic");
				polls++;
				if (verdict != RefineryFallbackScan.Verdict.Deferred)
				{
					Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
					break;
				}

				Assert.That(polls, Is.LessThan(500), "the paged pass must still terminate");
			}

			// Provider collection (4 enumerator steps) + rank collection (7) + the 18-cell
			// domain enumeration alone exceed this count — sorting/ranking/picking add more.
			Assert.That(polls, Is.GreaterThan(3 + 6 + 18),
				"collection, enumeration and sorting work must consume real charged steps");
		}

		[Test]
		public void LargeDomainCannotCompleteInOneSmallPoll()
		{
			// A 21x21 provider square (441 cells) at budget 8: a single poll must stop
			// mid-pass and resume — no uncharged whole-disk materialization remains.
			var providers = new[] { Prov(0, 0, 10) };
			var ranks = new[] { new CPos(3, 3) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, ranks, c => true, c => true);

			var first = scan.Poll(8, 0, () => snapshot, out _);
			Assert.That(first, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred),
				"a 441-cell domain cannot drain inside an 8-unit budget");
			Assert.That(scan.LastPollWork, Is.LessThanOrEqualTo(8));
		}

		[Test]
		public void ZeroBudgetPollNeverFabricsAVerdict()
		{
			// A fresh scan polled with no budget must not replay the initial verdict
			// placeholder — an unearned terminal verdict is a cancel with zero cells
			// scanned. Deferred is honest.
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(new[] { Prov(0, 0, 2) }, new[] { new CPos(0, 0) }, c => true, c => true);

			Assert.That(scan.Poll(0, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred),
				"no work may claim an empty domain");
			Assert.That(scan.LastPollWork, Is.EqualTo(0));

			// After a real drained pass the same zero-budget poll DOES replay the real verdict.
			var verdict = Drain(scan, 64, 400, () => snapshot, out _);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(scan.Poll(0, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Site),
				"a consumed verdict replays; a fabricated one does not");
		}

		[Test]
		public void PickedSiteConsumesThePass()
		{
			// A delivered site ends the pass: a later Poll must not drain stale leftover
			// candidates — it replays Site inside the epoch and re-lists after it.
			var providers = new[] { Prov(0, 0, 3) };
			var legal = new HashSet<CPos> { new CPos(1, 0), new CPos(2, 0), new CPos(3, 0) };
			var ranks = new[] { new CPos(0, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, ranks, c => true, LegalOnly(legal));

			Assert.That(Drain(scan, 64, 400, () => snapshot, out var site), Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(1, 0)));

			legal.Remove(new CPos(2, 0));
			Assert.That(scan.Poll(64, 0, () => snapshot, out var replay), Is.EqualTo(RefineryFallbackScan.Verdict.Site),
				"the delivered verdict replays inside the epoch — no stale second pick");
			Assert.That(replay, Is.EqualTo(new CPos(1, 0)));
			Assert.That(scan.LastPollWork, Is.EqualTo(0));
		}

		[Test]
		public void MutatedProviderEnumerationRestartsBoundedly()
		{
			// A world mutation tearing the live collection restarts it in place — the pass
			// still completes deterministically on the retaken snapshot.
			var providers = new RefineryFallbackScan.Provider?[] { Prov(0, 0, 1, 1), Prov(10, 0, 1, 2), Prov(20, 0, 1, 3) };
			var enumCalls = 0;
			Func<IEnumerator<RefineryFallbackScan.Provider?>> source = () =>
			{
				var which = enumCalls++;
				return FlakyProviders(providers, which == 0 ? 1 : int.MaxValue);
			};

			var snapshot = new RefineryFallbackScan.Snapshot(
				source,
				() => new[] { new CPos(0, 0) }.AsEnumerable().GetEnumerator(),
				() => Enumerable.Empty<CPos>().GetEnumerator(),
				BoundedWide,
				OneTile,
				() => AtomsOf(FixedAtoms()),
				() => AtomsOf(FixedAtoms()),
				c => -1,
				i => CPos.Zero,
				2,
				c => true,
				c => true);

			var scan = new RefineryFallbackScan(50);
			var verdict = Drain(scan, 16, 200, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(enumCalls, Is.EqualTo(2), "one torn enumeration, one clean restart");
		}

		static IEnumerator<RefineryFallbackScan.Provider?> FlakyProviders(IReadOnlyList<RefineryFallbackScan.Provider?> items, int failAt)
		{
			var i = 0;
			foreach (var p in items)
			{
				if (i++ == failAt)
					throw new InvalidOperationException("world mutated mid-enumeration");
				yield return p;
			}
		}

		[Test]
		public void MutationsBeyondRestartBoundDeferThePass()
		{
			// Every collection tears — after the bounded restart count the pass aborts to a
			// waiting state rather than churning work it cannot spend.
			var providers = new RefineryFallbackScan.Provider?[] { Prov(0, 0, 1, 1), Prov(10, 0, 1, 2) };
			var snapshot = new RefineryFallbackScan.Snapshot(
				() => FlakyProviders(providers, 1),
				() => new[] { new CPos(0, 0) }.AsEnumerable().GetEnumerator(),
				() => Enumerable.Empty<CPos>().GetEnumerator(),
				BoundedWide,
				OneTile,
				() => AtomsOf(FixedAtoms()),
				() => AtomsOf(FixedAtoms()),
				c => -1,
				i => CPos.Zero,
				2,
				c => true,
				c => true);

			var scan = new RefineryFallbackScan(100);
			Assert.That(scan.Poll(64, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(scan.Epochs, Is.EqualTo(1), "the aborted pass waits inside its epoch");
			Assert.That(scan.Poll(64, 50, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(scan.LastPollWork, Is.EqualTo(0), "a waiting poll spends nothing");
		}

		// ---- AMEND-C + addendum 66578711: complete square/paged domain -------

		[Test]
		public void ChebyshevCornerOutsideEuclideanDiskIsEnumerated()
		{
			// Provider reach 3: the square corner (3,3) sits at Euclidean distance √18 ≈ 4.24 —
			// OUTSIDE the radius-3 FindTilesInAnnulus disk the first revision used. The legal
			// domain is Chebyshev adjacency (Building.cs:267-273), so the corner must be found.
			var providers = new[] { Prov(0, 0, 3) };
			var corner = new CPos(3, 3);
			var legal = new HashSet<CPos> { corner };
			var ranks = new[] { new CPos(9, 9) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 64, 200, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(corner), "the Chebyshev corner is inside the declared domain");
		}

		[Test]
		public void DomainExceedsAnnulusMaxWithoutTruncation()
		{
			// Reach 55 > MaximumTileSearchRange - 1 = 49: the annulus path silently clamped;
			// the coordinate square still enumerates the corner (54,54) — addendum 66578711.
			var providers = new[] { Prov(0, 0, 55) };
			var far = new CPos(54, 54);
			var legal = new HashSet<CPos> { far };
			var ranks = new[] { new CPos(0, 0) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 512, 400, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(far), "cells beyond the annulus cap stay in the domain");
		}

		[Test]
		public void OffMapSquareCellsAreClippedByTheInMapPrefilter()
		{
			// A provider at the map corner: off-map square cells never reach legality —
			// the prefilter rejects them inside the same charged enumeration step.
			var providers = new[] { Prov(0, 0, 3) };
			var legalCalls = new List<CPos>();
			Func<CPos, bool> legal = c =>
			{
				legalCalls.Add(c);
				return false;
			};

			var scan = new RefineryFallbackScan(50);
			var verdict = Drain(scan, 8, 400,
				() => Snap(providers, new[] { new CPos(0, 0) }, c => c.X >= 0 && c.Y >= 0, legal), out _);

			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(legalCalls.Count, Is.GreaterThan(0));
			Assert.That(legalCalls.All(c => c.X >= 0 && c.Y >= 0), Is.True,
				"no off-map cell may reach the legality predicate");
		}

		[Test]
		public void OverlappingProviderSquaresDeduplicate()
		{
			// Two overlapping squares: shared cells reach legality exactly once.
			var providers = new[] { Prov(0, 0, 2, 1), Prov(2, 0, 2, 2) };
			var legalCalls = new List<CPos>();
			Func<CPos, bool> legal = c =>
			{
				legalCalls.Add(c);
				return true;
			};

			var scan = new RefineryFallbackScan(50);
			var verdict = Drain(scan, 32, 400,
				() => Snap(providers, new[] { new CPos(4, 0) }, c => true, legal), out var site);

			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(legalCalls.Distinct().Count(), Is.EqualTo(legalCalls.Count - 1),
				"each overlapping-domain cell is evaluated once during enumeration");
			Assert.That(legalCalls.Count(c => c == site.Value), Is.EqualTo(2),
				"the only second evaluation is the winner's pick-time revalidation");
		}

		[Test]
		public void WholeMapDomainCoversProviderlessCells()
		{
			// The terminal tier's unconditional domain is the whole map — a cell far from
			// every provider still places when the injected legality says legal.
			var map = RectMap(8, 8);
			var siteCell = new CPos(7, 7);
			var legal = new HashSet<CPos> { siteCell };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 16, 400,
				() => WholeMapSnap(map, new[] { Prov(0, 0, 1) }, new[] { new CPos(0, 0) }, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(siteCell));
		}

		// ---- ranking / ordering (footprint-gap metric, 80646D1B) ------------

		[Test]
		public void GloballyNearestWinsNotEnumerationOrder()
		{
			// Legal cells at Chebyshev gap 4 / 1 / 5 from the rank cell (single-tile
			// footprint — gap equals the topLeft's Chebyshev distance): the square walk
			// visits them in coordinate order, the pick must return the nearest regardless.
			var providers = new[] { Prov(4, 0, 5) };
			var ranks = new[] { new CPos(4, 0) };
			var legal = new HashSet<CPos> { new CPos(0, 0), new CPos(5, 0), new CPos(9, 0) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 128, 400, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(5, 0)), "gap 1 beats 4 and 5 under the global order");
		}

		[Test]
		public void EqualRankBreaksByCellXY()
		{
			// A nearer candidate still rules over the tie pair; the equal-gap tie-break
			// itself is covered by EqualGapBreaksByCellXY.
			var providers = new[] { Prov(0, 0, 4) };
			var ranks = new[] { new CPos(0, 0) };
			var legal = new HashSet<CPos> { new CPos(3, 0), new CPos(0, 3), new CPos(2, 2) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 128, 400, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(2, 2)),
				"gap 2 wins over the two gap-3 cells — the nearest admissible still rules");
		}

		[Test]
		public void EqualGapBreaksByCellXY()
		{
			// The tie-break itself: (0,3) and (3,0) sit at identical Chebyshev gap 3 —
			// the argmin takes the lower X, then the lower Y.
			var providers = new[] { Prov(0, 0, 4) };
			var ranks = new[] { new CPos(0, 0) };
			var legal = new HashSet<CPos> { new CPos(3, 0), new CPos(0, 3) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 128, 400, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(0, 3)), "equal rank: lowest X wins, then Y");
		}

		[Test]
		public void FootprintGapMeasuresEveryTileNotTheTopLeft()
		{
			// 80646D1B: footprint {(0,0),(4,0)} — a candidate whose TOP-LEFT is farther but
			// whose trailing tile touches the rank cell must beat a topLeft-nearer cell.
			// A: topLeft (6,0), tiles reach (10,0) = the rank cell itself → gap 0.
			// B: topLeft (9,0), tiles (9,0)/(13,0) → gap 1. TopLeft distance says B (1 < 4);
			// the footprint gap says A — the law's flush convention.
			var providers = new[] { Prov(6, 0, 8) };
			var ranks = new[] { new CPos(10, 0) };
			var footprint = new[] { new CVec(0, 0), new CVec(4, 0) };
			var legal = new HashSet<CPos> { new CPos(6, 0), new CPos(9, 0) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 256, 400,
				() => Snap(providers, ranks, c => true, LegalOnly(legal), footprint: footprint), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(6, 0)),
				"a footprint tile ON the resource cell (gap 0) beats a topLeft-nearer candidate");
		}

		[Test]
		public void PickTimeRevalidationSelectsNextBestStillLegal()
		{
			// The best cell goes illegal between enumeration and pick: the next-ranked
			// still-legal cell wins — the frozen pass list revalidates at pick time.
			var providers = new[] { Prov(0, 0, 4) };
			var ranks = new[] { new CPos(0, 0) };
			var legal = new HashSet<CPos> { new CPos(1, 0), new CPos(2, 0), new CPos(3, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, ranks, c => true, c => legal.Contains(c));

			// Advance the pass until enumeration has seen at least the best cell, then
			// flip it illegal before the pick phase can revalidate it.
			var polls = 0;
			while (scan.Poll(8, polls++, () => snapshot, out _).Equals(RefineryFallbackScan.Verdict.Deferred))
				if (polls > 3)
				{
					legal.Remove(new CPos(1, 0));
					break;
				}

			var verdict = Drain(scan, 8, 400, () => snapshot, out var site, polls);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.Not.EqualTo(new CPos(1, 0)),
				"the cell that went illegal mid-pass loses the pick at revalidation");
		}

		// ---- relaxation ladder / exhaustion certificate ---------------------

		[Test]
		public void TierLadderRelaxesToTheSiteTier()
		{
			// Legal only at (5,0), Chebyshev gap 5 from the rank cell: tiers 0..3 (bounds
			// 1..4) drain NoSite; tier 4 (bound 5) admits it. The ladder repeats the scan
			// at the current tier — matching the maintainer's graduated-relaxation ruling.
			var providers = new[] { Prov(0, 0, 6) };
			var legal = new HashSet<CPos> { new CPos(5, 0) };
			var ranks = new[] { new CPos(0, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, ranks, c => true, LegalOnly(legal), LinearGap);

			for (var tier = 0; tier <= 3; tier++)
			{
				var drained = Drain(scan, 64, 400, () => snapshot, out var s);
				Assert.That(drained, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite), $"tier {tier} admits nothing");
				Assert.That(s, Is.Null);
				Assert.That(scan.Tier, Is.EqualTo(tier));
				Assert.That(scan.TerminalTier, Is.False);
				scan.AdvanceTier();
			}

			var verdict = Drain(scan, 64, 400, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(5, 0)));
			Assert.That(scan.Tier, Is.EqualTo(4));
		}

		[Test]
		public void RanklessCandidatesAdmitOnlyAtTerminalTier()
		{
			// No declared resource cells at all → every legal cell ranks int.MaxValue.
			// A bounded tier rejects them; the fully-removed terminal tier admits them —
			// "place wherever legal" only once the ladder has run out. The terminal tier's
			// domain is the whole map, so the site need not sit in a provider square.
			var map = RectMap(6, 6).ToList();
			var legal = new HashSet<CPos> { new CPos(1, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = WholeMapSnap(map, new[] { Prov(0, 0, 1) }, Array.Empty<CPos>(), LegalOnly(legal),
				t => t == 0 ? 4 : int.MaxValue);

			var drained = Drain(scan, 64, 200, () => snapshot, out var s);
			Assert.That(drained, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(s, Is.Null);
			Assert.That(scan.TerminalTier, Is.False);

			scan.AdvanceTier();
			var verdict = Drain(scan, 64, 200, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(1, 0)));
		}

		[Test]
		public void TerminalDrainIsTheOnlyExhaustionProof()
		{
			// Nothing legal anywhere; tier 0 bounded (provider squares), tier 1 terminal
			// (whole map). Only the coherent whole-map drain may report Exhausted and only
			// a still-current certificate survives the cancel arm's live re-verification.
			var map = RectMap(6, 6).ToList();
			var atoms = new List<RefineryFallbackScan.CoherenceAtom> { new RefineryFallbackScan.CoherenceAtom(0, 0, 42, 0, null) };
			Func<IEnumerator<RefineryFallbackScan.CoherenceAtom>> atomsSrc = () => AtomsOf(atoms);
			var scan = new RefineryFallbackScan(50);
			var snapshot = WholeMapSnap(map, new[] { Prov(0, 0, 1) }, new[] { new CPos(0, 0) }, c => false,
				t => t == 0 ? 1 : int.MaxValue, atoms: atomsSrc);

			var bounded = Drain(scan, 64, 400, () => snapshot, out _);
			Assert.That(bounded, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.TerminalTier, Is.False, "a bounded drain is never the exhaustion proof");
			Assert.That(scan.CertificateCurrent(), Is.False, "no certificate issued on a bounded drain");

			scan.AdvanceTier();
			var final = Drain(scan, 64, 400, () => snapshot, out _);
			Assert.That(final, Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.TerminalTier, Is.True,
				"the coherent whole-map drain proves total-domain exhaustion — the cancel precondition");
			Assert.That(scan.CertificateCurrent(), Is.True,
				"the certificate stands while the live atom sequence still matches pairwise");

			// A covered event between issue and cancel stales the certificate (dirty epoch).
			scan.MarkDirtyCell(new CPos(0, 0));
			Assert.That(scan.CertificateCurrent(), Is.False,
				"a post-issue dirty event fails the live re-verification closed");
		}

		[Test]
		public void AtomDriftFailsCertificateVerification()
		{
			// 6722A484 exact state: the certificate's stamped atom list is compared
			// pairwise against a live re-enumeration — ANY drift fails closed.
			var map = RectMap(4, 4).ToList();
			var atoms = new List<RefineryFallbackScan.CoherenceAtom> { new RefineryFallbackScan.CoherenceAtom(0, 0, 42, 0, null) };
			Func<IEnumerator<RefineryFallbackScan.CoherenceAtom>> atomsSrc = () => AtomsOf(atoms);
			var scan = new RefineryFallbackScan(50);
			var snapshot = WholeMapSnap(map, new[] { Prov(0, 0, 1) }, new[] { new CPos(0, 0) }, c => false,
				atoms: atomsSrc);

			Assert.That(Drain(scan, 64, 400, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Exhausted));
			Assert.That(scan.CertificateCurrent(), Is.True);

			atoms.Add(new RefineryFallbackScan.CoherenceAtom(5, 0, 1, 1, null));
			Assert.That(scan.CertificateCurrent(), Is.False,
				"an appended live atom is a pairwise length mismatch — no collision surface exists");

			atoms.RemoveAt(atoms.Count - 1);
			atoms[0] = new RefineryFallbackScan.CoherenceAtom(0, 0, 43, 0, null);
			Assert.That(scan.CertificateCurrent(), Is.False, "a value drift in one field fails closed");
		}

		[Test]
		public void NoProviderTerminalDrainNeverCertifies()
		{
			// 6722A484: an empty provider domain is never an exhaustion proof — the
			// whole-map drain reports the honest drained verdict and no certificate exists.
			var map = RectMap(4, 4).ToList();
			var scan = new RefineryFallbackScan(50);
			var snapshot = WholeMapSnap(map, Array.Empty<RefineryFallbackScan.Provider?>(),
				new[] { new CPos(0, 0) }, c => false);

			var verdict = Drain(scan, 64, 400, () => snapshot, out _);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.TerminalTier, Is.True);
			Assert.That(scan.CertificateCurrent(), Is.False,
				"no provider scanned — Held/Deferred semantics, zero cancellation authority");
		}

		[Test]
		public void DirtyMarkDuringEnumerateRepairsAndFindsTheCell()
		{
			// A covered event dirties an already-enumerated cell; the terminal pass's
			// selective repair re-evaluates exactly that index before draining — the
			// now-legal cell joins the candidates and wins the pick.
			var map = RectMap(6, 6).ToList();
			var dirtied = new CPos(1, 1);
			var legal = new HashSet<CPos>();
			var scan = new RefineryFallbackScan(50);
			var snapshot = WholeMapSnap(map, new[] { Prov(0, 0, 1) }, new[] { new CPos(1, 1) },
				c => legal.Contains(c), readRadius: 1);

			// Run a few terminal polls so the enumeration frontier passes the target cell,
			// then make it legal and mark its index dirty — the repair pass must catch it.
			var polls = 0;
			while (polls < 4 && scan.Poll(8, polls, () => snapshot, out _).Equals(RefineryFallbackScan.Verdict.Deferred))
				polls++;

			legal.Add(dirtied);
			scan.MarkDirtyCell(dirtied);

			var verdict = Drain(scan, 64, 400, () => snapshot, out var site, polls);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(dirtied),
				"the dirtied already-enumerated cell was re-examined before the pass drained");
		}

		[Test]
		public void PostEnumerateDirtyMarkAbortsTheTerminalPass()
		{
			// Evidence dirtied after the whole-map drain — while ranking or picking —
			// cannot be repaired in place: the pass aborts to Deferred and re-lists on a
			// later epoch; the certificate never issues over mixed-time evidence.
			var map = RectMap(6, 6).ToList();
			var legal = new HashSet<CPos> { new CPos(1, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = WholeMapSnap(map, new[] { Prov(0, 0, 1) }, new[] { new CPos(0, 0) },
				LegalOnly(legal), readRadius: 1);

			// Charged-step arithmetic at budget 24: poll 1 = sweep (2) + provider collect
			// (2) + rank collect (2) + sort (1) + 17 enumerated cells — mid-Enumerate.
			// Poll 2 = sweep (2) + the remaining 19 cells + drain-check (1) + 2 rank
			// steps — lands inside Rank. The mark then hits a post-enumerate phase.
			Assert.That(scan.Poll(24, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));
			Assert.That(scan.Poll(24, 1, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.Deferred));

			scan.MarkDirtyCell(new CPos(0, 0));

			// The abort verdict is Deferred — never Exhausted, never a stale pick.
			var aborted = scan.Poll(64, 2, () => snapshot, out _);
			Assert.That(aborted, Is.EqualTo(RefineryFallbackScan.Verdict.Deferred),
				"post-enumerate evidence churn aborts the pass — mixed-time is never certified");
			Assert.That(scan.CertificateCurrent(), Is.False);

			var verdict = Drain(scan, 64, 400, () => snapshot, out var site, 500);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(1, 0)), "the re-listed pass completes cleanly");
		}

		[Test]
		public void EmptyDomainVerdictsNoUniverse()
		{
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(Array.Empty<RefineryFallbackScan.Provider?>(),
				new[] { new CPos(0, 0) }, c => true, c => true);

			var verdict = Drain(scan, 32, 100, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.NoUniverse));
			Assert.That(site, Is.Null);
		}

		[Test]
		public void EpochRelistsUnderFreshSnapshot()
		{
			// Pass 1: the only candidate is illegal → NoSite and the epoch gate closes.
			// A poll inside the gate replays the consumed verdict with zero work; a poll
			// past it re-lists under a fresh snapshot and sees the now-legal cell.
			var providers = new[] { Prov(0, 0, 3) };
			var cell = new CPos(1, 0);
			var legal = new HashSet<CPos>();
			var scan = new RefineryFallbackScan(25);
			var snapshot = Snap(providers, new[] { new CPos(0, 0) }, c => true, LegalOnly(legal));

			Assert.That(Drain(scan, 64, 200, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.Poll(64, 0, () => snapshot, out _), Is.EqualTo(RefineryFallbackScan.Verdict.NoSite),
				"a tick inside the epoch gate replays the drained verdict");
			Assert.That(scan.LastPollWork, Is.EqualTo(0));
			Assert.That(scan.Epochs, Is.EqualTo(1), "the epoch gate holds until the epoch ticks");

			legal.Add(cell);   // occupancy changed between passes
			var verdict = Drain(scan, 64, 200, () => snapshot, out var site, 500);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(cell));
			Assert.That(scan.Epochs, Is.EqualTo(2));
		}

		// ---- determinism / budget equivalence -------------------------------

		[Test]
		public void SmallAndLargeBudgetsPickIdentically()
		{
			var providers = new[] { Prov(0, 0, 3), Prov(8, 0, 3) };
			var ranks = new[] { new CPos(2, 2) };
			var legal = new HashSet<CPos> { new CPos(0, 0), new CPos(2, 2), new CPos(4, 4), new CPos(8, 0) };

			var scanSmall = new RefineryFallbackScan(50);
			var scanLarge = new RefineryFallbackScan(50);
			var verdictSmall = Drain(scanSmall, 3, 2000, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var siteSmall);
			var verdictLarge = Drain(scanLarge, 100000, 4, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var siteLarge);

			Assert.That(verdictSmall, Is.EqualTo(verdictLarge));
			Assert.That(siteSmall, Is.EqualTo(siteLarge),
				"paging changes only how the work is spread across sweeps, never the outcome");
		}

		// ---- AMEND-B: the ∃-dock fold ----------------------------------------

		[Test]
		public void AnyDockClearIsExistential()
		{
			Assert.That(BaseBuilderQueueEvalCA.AnyDockClear(new[]
			{
				BaseBuilderQueueEvalCA.DockAccess.NoExit,
				BaseBuilderQueueEvalCA.DockAccess.Clear,
				BaseBuilderQueueEvalCA.DockAccess.OnResource
			}), Is.True, "one clear dock suffices — the law's rule");

			Assert.That(BaseBuilderQueueEvalCA.AnyDockClear(new[]
			{
				BaseBuilderQueueEvalCA.DockAccess.OnResource,
				BaseBuilderQueueEvalCA.DockAccess.NoExit
			}), Is.False, "a cell fails only when EVERY dock is blocked");

			Assert.That(BaseBuilderQueueEvalCA.AnyDockClear(Enumerable.Empty<BaseBuilderQueueEvalCA.DockAccess>()),
				Is.False, "no dock offsets means no reachable dock");
		}

		[Test]
		public void AnyDockClearStopsAtFirstClear()
		{
			var evaluated = 0;
			IEnumerable<BaseBuilderQueueEvalCA.DockAccess> Seq()
			{
				evaluated++;
				yield return BaseBuilderQueueEvalCA.DockAccess.Clear;
				evaluated++;
				yield return BaseBuilderQueueEvalCA.DockAccess.NoExit;
			}

			Assert.That(BaseBuilderQueueEvalCA.AnyDockClear(Seq()), Is.True);
			Assert.That(evaluated, Is.EqualTo(1), "the fold never evaluates past the first Clear");
		}

		// ---- hold-reason / protection predicates -----------------------------

		[Test]
		public void FallbackHoldReasonDistinguishesVerdicts()
		{
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.Deferred, false),
				Is.EqualTo(BotQueueTransitionReason.RefineryScanDeferred));
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.Exhausted, false),
				Is.EqualTo(BotQueueTransitionReason.RefineryDomainExhausted),
				"the proven-exhaustion verdict carries its own reason — it precedes the only permitted cancel");
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.Exhausted, true),
				Is.EqualTo(BotQueueTransitionReason.RefineryDomainExhausted));
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.NoUniverse, false),
				Is.EqualTo(BotQueueTransitionReason.NoRefineryClaim));
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.NoUniverse, true),
				Is.EqualTo(BotQueueTransitionReason.NoRefinerySite),
				"claims were offered — an empty domain after offered claims is the drained-scan case, not the no-claim case");
			Assert.That(BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(RefineryFallbackOutcome.NoSite, true),
				Is.EqualTo(BotQueueTransitionReason.NoRefinerySite));
		}

		[Test]
		public void RetainedHighValueItemCoversOnlyTheRefineryClass()
		{
			var types = new HashSet<string> { "proc", "nref" };
			Assert.That(BaseBuilderQueueEvalCA.RetainedHighValueItem(types, "proc"), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.RetainedHighValueItem(types, "nref"), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.RetainedHighValueItem(types, "powr"), Is.False,
				"cheap fast items still cancel — the class is refinery-scoped pending the G3 ruling");
			Assert.That(BaseBuilderQueueEvalCA.RetainedHighValueItem(types, "atek"), Is.False,
				"radar/tech scope is the pending G3 ruling — not retained yet");
		}

		[Test]
		public void RefineryDefersRemainsTheLawScopedQuery()
		{
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Refinery), Is.True);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(false, BuildingType.Refinery), Is.False,
				"the law-scoped query is unchanged — G2's no-law retention is decided at the call site");
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Building), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.RefineryDefers(true, BuildingType.Defense), Is.False);
		}

		[Test]
		public void PlacementCancelProtectionIsTheSharedVerdictTable()
		{
			// E1283A6D/D831BBCB: proven cheap-fast → Allow; proven protected →
			// RequireCertificate; anything else → Unresolvable (fails closed). Floors and
			// resolved cost/ticks are accepted but unused this iteration.
			Assert.That(
				BaseBuilderQueueEvalCA.EvaluatePlacementCancelProtection(null, true, true, null, null, null, null),
				Is.EqualTo(PlacementCancelVerdict.Allow), "cheap-fast wins first");
			Assert.That(
				BaseBuilderQueueEvalCA.EvaluatePlacementCancelProtection(null, false, true, 0, -1, null, null),
				Is.EqualTo(PlacementCancelVerdict.Allow), "a sentinel -1 duration never blocks Allow");
			Assert.That(
				BaseBuilderQueueEvalCA.EvaluatePlacementCancelProtection(null, true, false, null, null, null, null),
				Is.EqualTo(PlacementCancelVerdict.RequireCertificate));
			Assert.That(
				BaseBuilderQueueEvalCA.EvaluatePlacementCancelProtection(null, false, false, 500, 40, 600, 50),
				Is.EqualTo(PlacementCancelVerdict.Unresolvable),
				"unknown membership is protected — floors stay unused this iteration");
		}
	}
}
