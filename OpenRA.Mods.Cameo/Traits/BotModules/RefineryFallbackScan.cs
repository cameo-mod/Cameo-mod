#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// REF-2 P1a (retained-refinery fallback, Sol disposition 5FB81294, amended per
	/// DISPOSITION_2026-10-16_refinery_p1a_b9_amend_sol): one hold episode's bounded
	/// widened-site search, run while a completed law-path refinery waits at a queue's head
	/// for a legal cell.
	///
	/// Every pass runs as an explicit phase machine — collect providers, collect rank cells,
	/// sort the rank set, enumerate the declared domain, rank the legal cells, then pick —
	/// and EVERY step charges its budget unit BEFORE the work runs. No uncharged atomics
	/// hide behind a MoveNext or a ToList: enumeration yields one actor/cell/window
	/// position per step, sorting is a resumable mergesort, nearest-resource ranking is a
	/// resumable two-sided scan, and the final selection is a resumable argmin over the
	/// exact global (rank, X, Y) order — a pick never happens before the ordering guarantee
	/// is established. Three atomics are declared rather than paged: one provider prep per
	/// enumerated actor (footprint-proportioned — bounded by rules constants, not counts),
	/// one engine legality call per domain cell or pick (the identical predicate the
	/// preferred offer stage already spends per attempt), and the rank-sort scratch
	/// allocation (a single GC array per pass, charged once). A world mutation that
	/// invalidates a live enumeration restarts that collection, bounded per pass; a pass
	/// that cannot complete defers to the next epoch. A consumed pass waits one epoch,
	/// then re-enumerates a fresh snapshot. World-free: providers, the domain stream,
	/// legality, rank cells and the map-bounds prefilter are injected, so the whole
	/// machine is directly testable.
	/// </summary>
	public sealed class RefineryFallbackScan
	{
		/// <summary>One declared reach-disk provider, in deterministic source order.</summary>
		public readonly struct Provider
		{
			public Provider(CPos center, int reachCells, uint id)
			{
				Center = center;
				ReachCells = reachCells;
				Id = id;
			}

			public readonly CPos Center;

			/// <summary>Chebyshev reach of this provider's disk domain.</summary>
			public readonly int ReachCells;
			public readonly uint Id;
		}

		/// <summary>
		/// The frozen per-pass inputs — enumeration factories, not materialized data, so every
		/// preparation step pages under the sweep budget:
		/// <see cref="ProviderSource"/> yields one entry PER SCANNED ACTOR (null when the
		/// declared filter rejects it), so a filtered-out stretch still charges one bounded
		/// step per actor and no filtering hides inside a single MoveNext.
		/// <see cref="WholeMapDomain"/> declares the unrestricted domain — used when the
		/// caller's legality cannot reject on base distance (the flag is false, or the item
		/// has no RequiresBuildableArea and IsCloseEnoughToBase therefore can never fail) —
		/// and <see cref="WholeMapCells"/> streams every map cell for it.
		/// <see cref="RankCellSource"/> yields the declared resource cells (duplicates are
		/// deduped scan-side). <see cref="InMap"/> is the playable-bounds prefilter.
		/// <see cref="Legal"/> is the full engine legality predicate — the same atomic check
		/// the preferred offer path spends per attempt; each call sits inside one charged step.
		/// <see cref="TierBoundSq"/> maps a relaxation tier to its squared-distance bound:
		/// a legal cell is a candidate only while its nearest-resource dist² is within the
		/// current tier's bound; int.MaxValue declares the unrestricted final tier.
		/// Rebuilt at each epoch so a re-list re-samples occupancy and reach.
		/// </summary>
		public readonly struct Snapshot
		{
			public Snapshot(
				Func<IEnumerator<Provider?>> providerSource,
				Func<IEnumerator<CPos>> rankCellSource,
				Func<IEnumerator<CPos>> wholeMapCells,
				bool wholeMapDomain,
				Func<int, int> tierBoundSq,
				Func<CPos, bool> inMap,
				Func<CPos, bool> legal)
			{
				ProviderSource = providerSource;
				RankCellSource = rankCellSource;
				WholeMapCells = wholeMapCells;
				WholeMapDomain = wholeMapDomain;
				TierBoundSq = tierBoundSq;
				InMap = inMap;
				Legal = legal;
			}

			public readonly Func<IEnumerator<Provider?>> ProviderSource;
			public readonly Func<IEnumerator<CPos>> RankCellSource;
			public readonly Func<IEnumerator<CPos>> WholeMapCells;
			public readonly bool WholeMapDomain;
			public readonly Func<int, int> TierBoundSq;
			public readonly Func<CPos, bool> InMap;
			public readonly Func<CPos, bool> Legal;
		}

		/// <summary>The per-sweep verdict.</summary>
		public enum Verdict
		{
			/// <summary>A still-legal site was picked under the established global order.</summary>
			Site,

			/// <summary>Collection, enumeration, ranking or picking continues — the budget was spent mid-pass.</summary>
			Deferred,

			/// <summary>The pass drained and every candidate failed pick-time revalidation.</summary>
			NoSite,

			/// <summary>The pass enumerated no domain cells — the declared reach domain was empty.</summary>
			NoUniverse
		}

		enum Phase { WaitForEpoch, CollectProviders, CollectRankCells, SortRankCells, Enumerate, Rank, Pick }

		/// <summary>
		/// A resumable bottom-up mergesort: every Step performs exactly one micro-op (one run
		/// advance, one comparison-write, or one copy-back) so ordering work pages under the
		/// same sweep budget as everything else. The single scratch allocation happens at
		/// construction and is charged as the sort's one declared memory step.
		/// </summary>
		sealed class PagedMerge<T>
		{
			readonly List<T> a;
			readonly T[] scratch;
			readonly Comparison<T> cmp;
			readonly int n;
			int width = 1;
			int run;
			int i, j, hi, k;
			bool merging, copying;
			int cb;

			public PagedMerge(List<T> a, Comparison<T> cmp)
			{
				this.a = a;
				this.cmp = cmp;
				n = a.Count;
				scratch = new T[n];
				Done = n < 2;
			}

			public bool Done { get; private set; }

			/// <summary>One bounded micro-step. Returns false once the list is sorted.</summary>
			public bool Step()
			{
				if (Done)
					return false;

				if (merging)
				{
					var leftEnd = run + width;
					if (i < leftEnd && j < hi)
						scratch[k++] = cmp(a[i], a[j]) <= 0 ? a[i++] : a[j++];
					else if (i < leftEnd)
						scratch[k++] = a[i++];
					else if (j < hi)
						scratch[k++] = a[j++];
					else
					{
						merging = false;
						copying = true;
						cb = run;
					}

					return true;
				}

				if (copying)
				{
					a[cb] = scratch[cb];
					if (++cb >= k)
					{
						copying = false;
						run += 2 * width;
					}

					return true;
				}

				// Advance to the next mergeable run pair; a solo trailing run needs no merge.
				if (run + width >= n)
				{
					run += 2 * width;
					if (run >= n)
					{
						width <<= 1;
						run = 0;
						if (width >= n)
						{
							Done = true;
							return false;
						}
					}

					return true;
				}

				i = run;
				j = run + width;
				hi = Math.Min(run + 2 * width, n);
				k = run;
				merging = true;
				return true;
			}
		}

		const int MaxCollectRestarts = 8;

		readonly int relistEpochTicks;
		Snapshot snapshot;
		Phase phase;

		// CollectProviders
		IEnumerator<Provider?> providerEnum;
		readonly List<Provider> providers = new();
		int collectRestarts;

		// CollectRankCells + SortRankCells
		IEnumerator<CPos> rankEnum;
		readonly HashSet<CPos> seenRank = new();
		readonly List<CPos> rankCells = new();
		PagedMerge<CPos> rankSort;

		// Enumerate
		int providerIndex;
		bool providerArmed;
		int diskDx, diskDy;
		IEnumerator<CPos> domainEnum;
		bool wholeMapDone;
		int domainCells;
		readonly HashSet<CPos> visited = new();
		readonly List<CPos> legalCells = new();

		// Rank — resumable per-candidate two-sided scan over the X-sorted rank set
		int rankIndex;
		int rankLo, rankHi;
		bool boundDone;
		int side;
		int scanIdx;
		int rankBest;
		readonly List<(CPos Cell, int Rank)> candidates = new();

		// Pick — resumable argmin under the exact global (rank, X, Y) order
		int pickScanIndex;
		int pickBestIndex;
		int pickBestRank, pickBestX, pickBestY;

		// Relaxation ladder (maintainer ruling 2026-10-16): the resource-distance constraint
		// relaxes tier-by-tier — each pass admits only candidates within TierBoundSq(tier);
		// a drained tier advances immediately and re-passes under the wider bound; the final
		// tier (int.MaxValue) is the fully-removed constraint. A drain there, or an empty
		// domain, is the proven total-domain exhaustion that authorizes the only permitted
		// cancel — reported via <see cref="UnrestrictedTier"/> for the caller to arbitrate.
		int tier;
		int maxRankSq;

		int relistAt = int.MinValue;
		Verdict waitingVerdict = Verdict.NoUniverse;

		public RefineryFallbackScan(int relistEpochTicks)
		{
			this.relistEpochTicks = relistEpochTicks;
			phase = Phase.WaitForEpoch;
		}

		/// <summary>Epochs run so far (the initial pass counts as one).</summary>
		public int Epochs { get; private set; }

		/// <summary>The charged work units of the most recent Poll — always ≤ its budget.</summary>
		public int LastPollWork { get; private set; }

		/// <summary>The relaxation tier the most recent pass enumerated under (0-based).</summary>
		public int Tier => tier;

		/// <summary>True once the latest pass ran at the unrestricted tier — the fully-removed constraint.</summary>
		public bool UnrestrictedTier => maxRankSq == int.MaxValue;

		/// <summary>
		/// Advance the relaxation ladder after a drained pass — the next Poll re-passes under
		/// the wider bound immediately (the constraint changed, not the world: no epoch wait).
		/// Call only after a drained verdict; mid-pass it is a no-op against a live cursor.
		/// </summary>
		public void AdvanceTier()
		{
			if (phase == Phase.WaitForEpoch)
			{
				tier++;
				relistAt = int.MinValue;
			}
		}

		/// <summary>
		/// One sweep's bounded work: at most <paramref name="cellBudget"/> charged steps of
		/// provider collection, rank-set collection/sorting, domain enumeration, ranking and
		/// pick revalidation — each charged BEFORE the bounded step runs. The snapshot
		/// factory runs once per pass start and does no enumeration itself.
		/// </summary>
		public Verdict Poll(int cellBudget, int now, Func<Snapshot> snapshotAtPassStart, out CPos? site)
		{
			site = null;
			var work = 0;

			if (cellBudget <= 0)
			{
				LastPollWork = 0;
				return phase == Phase.WaitForEpoch ? waitingVerdict : Verdict.Deferred;
			}

			if (phase == Phase.WaitForEpoch)
			{
				if (now < relistAt)
				{
					LastPollWork = work;
					return waitingVerdict;
				}

				work++;
				StartPass(snapshotAtPassStart());
			}

			while (work < cellBudget && phase != Phase.WaitForEpoch)
			{
				switch (phase)
				{
					case Phase.CollectProviders:
						work++;
						CollectProvidersStep(now);
						break;

					case Phase.CollectRankCells:
						work++;
						CollectRankCellsStep(now);
						break;

					case Phase.SortRankCells:
						work++;
						rankSort ??= new PagedMerge<CPos>(rankCells, CompareXy);
						if (!rankSort.Step())
						{
							rankSort = null;
							phase = Phase.Enumerate;
						}

						break;

					case Phase.Enumerate:
						work++;
						EnumerateStep();
						break;

					case Phase.Rank:
						work++;
						RankStep();
						break;

					case Phase.Pick:
						work++;
						switch (PickStep(now, out site))
						{
							case PickResult.Picked:
								LastPollWork = work;
								return Verdict.Site;
							case PickResult.Drained:
								LastPollWork = work;
								return waitingVerdict;
						}

						break;
				}
			}

			LastPollWork = work;
			return phase == Phase.WaitForEpoch ? waitingVerdict : Verdict.Deferred;
		}

		enum PickResult { Continue, Picked, Drained }

		// One actor per step: the source yields an entry for every scanned actor (null when
		// the declared filter rejects it), so a rejected stretch still charges bounded work.
		void CollectProvidersStep(int now)
		{
			providerEnum ??= snapshot.ProviderSource();
			bool more;
			try
			{
				more = providerEnum.MoveNext();
			}
			catch (InvalidOperationException)
			{
				// The live world enumeration was torn by a mutation — restart the
				// collection, bounded per pass; beyond that the pass defers a whole epoch.
				providerEnum.Dispose();
				providerEnum = null;
				providers.Clear();
				if (++collectRestarts > MaxCollectRestarts)
					AbortPass(now);
				return;
			}

			if (!more)
			{
				providerEnum.Dispose();
				providerEnum = null;
				phase = Phase.CollectRankCells;
				return;
			}

			// The source yields one entry per scanned actor — null marks a filtered-out
			// actor; the per-step charge already covers its bounded prep inside the source.
			var p = providerEnum.Current;
			if (p.HasValue)
				providers.Add(p.Value);
		}

		void CollectRankCellsStep(int now)
		{
			rankEnum ??= snapshot.RankCellSource();
			bool more;
			try
			{
				more = rankEnum.MoveNext();
			}
			catch (InvalidOperationException)
			{
				rankEnum.Dispose();
				rankEnum = null;
				seenRank.Clear();
				rankCells.Clear();
				if (++collectRestarts > MaxCollectRestarts)
					AbortPass(now);
				return;
			}

			if (!more)
			{
				rankEnum.Dispose();
				rankEnum = null;
				phase = Phase.SortRankCells;
				return;
			}

			var c = rankEnum.Current;
			if (seenRank.Add(c))
				rankCells.Add(c);
		}

		void EnumerateStep()
		{
			if (providerIndex < providers.Count)
			{
				var p = providers[providerIndex];
				if (!providerArmed)
				{
					providerArmed = true;
					diskDx = -p.ReachCells;
					diskDy = -p.ReachCells;
					return;
				}

				if (diskDy > p.ReachCells)
				{
					providerIndex++;
					providerArmed = false;
					return;
				}

				// The Chebyshev square IS the declared disk domain — every window position is
				// a domain cell, no shape filter needed. (A Euclidean FindTilesInAnnulus disk
				// would silently drop corner cells of the reach superset the legality pad
				// requires: |cell - provider|inf ≤ extent + Adjacent + footprintExtent.)
				var cell = new CPos(p.Center.X + diskDx, p.Center.Y + diskDy);
				if (++diskDx > p.ReachCells)
				{
					diskDx = -p.ReachCells;
					diskDy++;
				}

				EvaluateCell(cell);
				return;
			}

			if (wholeMapDone || !snapshot.WholeMapDomain)
			{
				phase = Phase.Rank;
				return;
			}

			// The unrestricted domain — one map cell per step.
			domainEnum ??= snapshot.WholeMapCells();
			if (!domainEnum.MoveNext())
			{
				domainEnum.Dispose();
				domainEnum = null;
				wholeMapDone = true;
				phase = Phase.Rank;
				return;
			}

			EvaluateCell(domainEnum.Current);
		}

		void EvaluateCell(CPos cell)
		{
			if (!snapshot.InMap(cell))
				return;

			domainCells++;
			if (visited.Add(cell) && snapshot.Legal(cell))
				legalCells.Add(cell);
		}

		// One bounded step: either one halving of the X lower-bound search or one
		// nearest-resource evaluation in the two-sided expansion that follows it.
		void RankStep()
		{
			if (rankIndex >= legalCells.Count)
			{
				phase = Phase.Pick;
				return;
			}

			var cell = legalCells[rankIndex];
			if (!boundDone)
			{
				if (rankLo < rankHi)
				{
					var mid = (rankLo + rankHi) >> 1;
					if (rankCells[mid].X < cell.X)
						rankLo = mid + 1;
					else
						rankHi = mid;
					return;
				}

				boundDone = true;
				side = 0;
				scanIdx = rankLo;
				rankBest = int.MaxValue;
				return;
			}

			if (side == 0)
			{
				if (scanIdx < rankCells.Count)
				{
					var r = rankCells[scanIdx];
					var dx = r.X - cell.X;
					var d2 = dx * dx;
					if (d2 >= rankBest)
					{
						side = 1;
						scanIdx = rankLo - 1;
					}
					else
					{
						var dy = r.Y - cell.Y;
						rankBest = Math.Min(rankBest, d2 + dy * dy);
						scanIdx++;
					}

					return;
				}

				side = 1;
				scanIdx = rankLo - 1;
				return;
			}

			if (scanIdx >= 0)
			{
				var r = rankCells[scanIdx];
				var dx = cell.X - r.X;
				var d2 = dx * dx;
				if (d2 < rankBest)
				{
					var dy = cell.Y - r.Y;
					rankBest = Math.Min(rankBest, d2 + dy * dy);
					scanIdx--;
					return;
				}
			}

			// Tier admission: only cells within the current bound join the pick order.
			// int.MaxValue rank (no rank cells at all) qualifies only at the unrestricted
			// tier — the fully-removed constraint.
			if (rankBest <= maxRankSq)
				candidates.Add((cell, rankBest));

			rankIndex++;
			boundDone = false;
			rankLo = 0;
			rankHi = rankCells.Count;
		}

		// Resumable argmin under the exact global (rank, X, Y) order — the ordering
		// guarantee is established by completing the scan over every candidate, with no
		// atomic sort; one charged revalidation runs per established minimum.
		PickResult PickStep(int now, out CPos? site)
		{
			site = null;
			if (candidates.Count == 0)
			{
				FinishPass(now);
				return PickResult.Drained;
			}

			if (pickScanIndex < candidates.Count)
			{
				var c = candidates[pickScanIndex++];
				if (pickBestIndex < 0
					|| c.Rank < pickBestRank
					|| (c.Rank == pickBestRank && (c.Cell.X < pickBestX || (c.Cell.X == pickBestX && c.Cell.Y < pickBestY))))
				{
					pickBestIndex = pickScanIndex - 1;
					pickBestRank = c.Rank;
					pickBestX = c.Cell.X;
					pickBestY = c.Cell.Y;
				}

				return PickResult.Continue;
			}

			var pick = candidates[pickBestIndex].Cell;
			candidates[pickBestIndex] = candidates[candidates.Count - 1];
			candidates.RemoveAt(candidates.Count - 1);
			pickScanIndex = 0;
			pickBestIndex = -1;

			if (snapshot.Legal(pick))
			{
				site = pick;
				return PickResult.Picked;
			}

			return PickResult.Continue;
		}

		void StartPass(Snapshot s)
		{
			snapshot = s;
			maxRankSq = Math.Max(0, s.TierBoundSq(tier));
			providers.Clear();
			seenRank.Clear();
			rankCells.Clear();
			visited.Clear();
			legalCells.Clear();
			candidates.Clear();
			providerEnum = null;
			rankEnum = null;
			rankSort = null;
			collectRestarts = 0;
			providerIndex = 0;
			providerArmed = false;
			domainEnum = null;
			wholeMapDone = false;
			domainCells = 0;
			rankIndex = 0;
			boundDone = false;
			rankLo = 0;
			rankHi = 0;
			side = 0;
			scanIdx = 0;
			rankBest = int.MaxValue;
			pickScanIndex = 0;
			pickBestIndex = -1;
			pickBestRank = 0;
			pickBestX = 0;
			pickBestY = 0;
			phase = Phase.CollectProviders;
			Epochs++;
		}

		// The pass could not finish its collection under the bounded restarts — defer a
		// whole epoch rather than churn work it cannot spend. Deferred is the honest
		// verdict: nothing was enumerated or proven.
		void AbortPass(int now)
		{
			DisposeCursors();
			waitingVerdict = Verdict.Deferred;
			relistAt = now + relistEpochTicks;
			phase = Phase.WaitForEpoch;
		}

		// Pass consumed under its snapshot — schedule the bounded re-list epoch and report
		// the verdict honestly: this describes a drained scan, never a live empty-reach proof.
		void FinishPass(int now)
		{
			DisposeCursors();
			waitingVerdict = domainCells == 0 ? Verdict.NoUniverse : Verdict.NoSite;
			relistAt = now + relistEpochTicks;
			phase = Phase.WaitForEpoch;
		}

		void DisposeCursors()
		{
			providerEnum?.Dispose();
			providerEnum = null;
			rankEnum?.Dispose();
			rankEnum = null;
			domainEnum?.Dispose();
			domainEnum = null;
		}

		static int CompareXy(CPos a, CPos b)
		{
			var d = a.X.CompareTo(b.X);
			return d != 0 ? d : a.Y.CompareTo(b.Y);
		}
	}
}
