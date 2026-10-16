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
	/// REF-2 P1a (retained-refinery fallback, amended per Sol dispositions CE13F2BA and
	/// addendum 66578711): the world-free half of the feature — RefineryFallbackScan's
	/// pre-charged paged phase machine (provider collection, rank-set collection, paged
	/// sort, square/domain enumeration, resumable ranking, paged ordering, pick), the
	/// Chebyshev-square domain geometry, the relaxation-tier ladder, the ∃-dock fold, and
	/// the hold-reason / no-cancel predicates in BaseBuilderQueueEvalCA. Authored only:
	/// the qualification lane runs them (the amendment claim forbids executing tests here).
	/// </summary>
	[TestFixture]
	public sealed class RefineryFallbackTest
	{
		// ---- helpers -------------------------------------------------------

		static RefineryFallbackScan.Provider Prov(int x, int y, int reach, uint id = 1) =>
			new RefineryFallbackScan.Provider(new CPos(x, y), reach, id);

		static int Unrestricted(int tier) => int.MaxValue;

		/// The relaxation ladder the shipped snapshot uses: tier t bounds dist² at (t+1)².
		static int LinearCells(int tier) => tier >= 30 ? int.MaxValue : (tier + 1) * (tier + 1);

		/// Square-domain snapshot: providers enumerate their own Chebyshev squares.
		static RefineryFallbackScan.Snapshot Snap(
			IReadOnlyList<RefineryFallbackScan.Provider?> providers,
			IReadOnlyList<CPos> rankCells,
			Func<CPos, bool> inMap,
			Func<CPos, bool> legal,
			Func<int, int> tierBoundSq = null)
		{
			return new RefineryFallbackScan.Snapshot(
				() => providers.GetEnumerator(),
				() => rankCells.GetEnumerator(),
				() => Enumerable.Empty<CPos>().GetEnumerator(),
				false,
				tierBoundSq ?? Unrestricted,
				inMap,
				legal);
		}

		static RefineryFallbackScan.Snapshot WholeMapSnap(
			IEnumerable<CPos> mapCells,
			IReadOnlyList<CPos> rankCells,
			Func<CPos, bool> legal,
			Func<int, int> tierBoundSq = null)
		{
			var cells = mapCells.ToList();
			return new RefineryFallbackScan.Snapshot(
				() => Enumerable.Empty<RefineryFallbackScan.Provider?>().GetEnumerator(),
				() => rankCells.GetEnumerator(),
				() => cells.GetEnumerator(),
				true,
				tierBoundSq ?? Unrestricted,
				c => true,
				legal);
		}

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
					"poll {0} charged {1} units against a budget of {2}", i, scan.LastPollWork, budget);
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
				false,
				Unrestricted,
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
				false,
				Unrestricted,
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
			Assert.That(legalCalls.Distinct().Count(), Is.EqualTo(legalCalls.Count),
				"each overlapping-domain cell is evaluated once");
		}

		[Test]
		public void WholeMapDomainCoversProviderlessCells()
		{
			// No-RBA / distance-flag-false / BuildAnywhere legality: no provider squares,
			// the domain is the streamed map — a cell far from anything still places.
			var map = Enumerable.Range(0, 8).SelectMany(x => Enumerable.Range(0, 8).Select(y => new CPos(x, y)));
			var site_cell = new CPos(7, 7);
			var legal = new HashSet<CPos> { site_cell };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 16, 400, () => WholeMapSnap(map, new[] { new CPos(0, 0) }, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(site_cell));
		}

		// ---- ranking / ordering ---------------------------------------------

		[Test]
		public void GloballyNearestWinsNotEnumerationOrder()
		{
			// Legal cells at dist² 16 / 1 / 25 from the rank cell — the square walk visits
			// them in coordinate order, the pick must return the nearest regardless.
			var providers = new[] { Prov(4, 0, 5) };
			var ranks = new[] { new CPos(4, 0) };
			var legal = new HashSet<CPos> { new CPos(0, 0), new CPos(5, 0), new CPos(9, 0) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 128, 400, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(5, 0)), "dist² 1 beats 16 and 25 under the global order");
		}

		[Test]
		public void EqualRankBreaksByCellXY()
		{
			var providers = new[] { Prov(5, 5, 6) };
			var ranks = new[] { new CPos(100, 100) };
			var legal = new HashSet<CPos> { new CPos(5, 4), new CPos(5, 1), new CPos(1, 9) };
			var scan = new RefineryFallbackScan(50);

			var verdict = Drain(scan, 128, 400, () => Snap(providers, ranks, c => true, LegalOnly(legal)), out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(1, 9)), "equal rank: lowest X wins, then Y");
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

		// ---- relaxation ladder / exhaustion ---------------------------------

		[Test]
		public void TierLadderRelaxesToTheSiteTier()
		{
			// Legal only at (5,0), dist² 25 from the rank cell: tiers 0..3 (bounds 1,4,9,16)
			// drain NoSite; tier 4 (bound 25) admits it. The ladder repeats the scan at the
			// current tier — matching the maintainer's graduated-relaxation ruling.
			var providers = new[] { Prov(0, 0, 6) };
			var legal = new HashSet<CPos> { new CPos(5, 0) };
			var ranks = new[] { new CPos(0, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, ranks, c => true, LegalOnly(legal), LinearCells);

			for (var tier = 0; tier <= 3; tier++)
			{
				var drained = Drain(scan, 64, 400, () => snapshot, out var s);
				Assert.That(drained, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite), "tier {0} admits nothing", tier);
				Assert.That(s, Is.Null);
				Assert.That(scan.Tier, Is.EqualTo(tier));
				Assert.That(scan.UnrestrictedTier, Is.False);
				scan.AdvanceTier();
			}

			var verdict = Drain(scan, 64, 400, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(5, 0)));
			Assert.That(scan.Tier, Is.EqualTo(4));
		}

		[Test]
		public void RanklessCandidatesAdmitOnlyAtUnrestrictedTier()
		{
			// No declared resource cells at all → every legal cell ranks int.MaxValue.
			// A bounded tier rejects them; the fully-removed tier admits them —
			// "place wherever legal" only once the ladder has run out.
			var providers = new[] { Prov(0, 0, 2) };
			var legal = new HashSet<CPos> { new CPos(1, 0) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, Array.Empty<CPos>(), c => true, LegalOnly(legal),
				t => t == 0 ? 4 : int.MaxValue);

			var drained = Drain(scan, 64, 200, () => snapshot, out var s);
			Assert.That(drained, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(s, Is.Null);
			Assert.That(scan.UnrestrictedTier, Is.False);

			scan.AdvanceTier();
			var verdict = Drain(scan, 64, 200, () => snapshot, out var site);
			Assert.That(verdict, Is.EqualTo(RefineryFallbackScan.Verdict.Site));
			Assert.That(site, Is.EqualTo(new CPos(1, 0)));
		}

		[Test]
		public void UnrestrictedDrainIsTheExhaustionProof()
		{
			// Nothing legal anywhere in the domain; tier 0 bounded, tier 1 unrestricted —
			// only the unrestricted drain may carry UnrestrictedTier (the caller's signal
			// for the only permitted cancel).
			var providers = new[] { Prov(0, 0, 3) };
			var scan = new RefineryFallbackScan(50);
			var snapshot = Snap(providers, new[] { new CPos(0, 0) }, c => true, c => false,
				t => t == 0 ? 1 : int.MaxValue);

			var bounded = Drain(scan, 64, 400, () => snapshot, out _);
			Assert.That(bounded, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.UnrestrictedTier, Is.False, "a bounded drain is never the exhaustion proof");

			scan.AdvanceTier();
			var final = Drain(scan, 64, 400, () => snapshot, out _);
			Assert.That(final, Is.EqualTo(RefineryFallbackScan.Verdict.NoSite));
			Assert.That(scan.UnrestrictedTier, Is.True,
				"the unrestricted drain proves total-domain exhaustion — the cancel precondition");
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

		// ---- hold-reason / no-cancel predicates ------------------------------

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
	}
}
