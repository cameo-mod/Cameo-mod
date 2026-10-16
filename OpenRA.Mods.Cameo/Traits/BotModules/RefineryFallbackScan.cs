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
	/// DISPOSITION_2026-10-16_refinery_p1a_b9_amend_sol, certificate per
	/// DECISION_2026-10-16_refinery_certificate_mechanism_sol BF5B8247 option (c) and
	/// ruling A399083F §2): one hold episode's bounded widened-site search, run while a
	/// completed law-path refinery waits at a queue's head for a legal cell.
	///
	/// Every pass runs as an explicit phase machine — collect providers, collect rank cells,
	/// sort the rank set, enumerate the declared domain, rank the legal cells, then pick —
	/// and EVERY step charges its budget unit BEFORE the work runs. No uncharged atomics
	/// hide behind a MoveNext or a ToList: enumeration yields one actor/cell/window
	/// position per step, sorting is a resumable mergesort, nearest-resource ranking is a
	/// resumable two-sided scan, and the final selection is a resumable argmin over the
	/// exact global (rank, X, Y) order — a pick never happens before the ordering guarantee
	/// is established. Declared atomics are: one provider prep per enumerated actor
	/// (footprint-proportioned — bounded by rules constants, not counts), one engine
	/// legality call per domain cell or pick (the identical predicate the preferred offer
	/// stage already spends per attempt), one per-actor signature atom per poll of a
	/// terminal pass, and the rank-sort scratch allocation (a single GC array per pass,
	/// charged once). A world mutation that invalidates a live enumeration restarts that
	/// collection, bounded per pass; a pass that cannot complete defers to the next epoch.
	/// A consumed pass waits one epoch, then re-enumerates a fresh snapshot. World-free:
	/// providers, the domain stream, legality, rank cells, the footprint, the coherence
	/// atoms and the map index are injected, so the whole machine is directly testable.
	///
	/// COHERENT-CURRENT CERTIFICATE (BF5B8247 option c): the terminal relaxation tier is
	/// the ONLY tier that may certify exhaustion, and its declared domain is the WHOLE MAP —
	/// provider squares are supersets that serve the preferred (bounded) tiers only; an
	/// empty provider domain is never an exhaustion proof. Two coherent-state mechanisms
	/// guard the pass:
	///
	/// 1. COHERENCE SWEEP — every poll of a terminal pass first re-collects the injected
	///    coherence atoms (scalar flags; per-actor ownership, alliance, in-world state;
	///    per-trait conditional buildable-area set, provider-readiness and replaceable
	///    state; one atom per rank cell) in a single same-tick sweep and compares them
	///    PAIRWISE to the pass's stamped sequence — exact values and rule-set reference
	///    identity, never a hash fold (6722A484). Any difference tears the pass
	///    (Deferred, epoch re-list): the evidence was mixed-time.
	/// 2. SELECTIVE DIRTY SET — every covered evented input (occupancy via the ActorMap
	///    influence layer — every IOccupySpace change including mobile moves; resource
	///    cells via the resource layer; terrain overrides via CustomTerrain's
	///    CellEntryChanged — the layer BuildableTerrainLayer writes through, so terrain
	///    mutation is evented after all) marks already-enumerated topLeft indices inside
	///    a declared read radius. The terminal enumerate phase repairs the stale set one
	///    index per charged step after the domain drains; a certificate issues only when
	///    the domain is complete AND the stale set is empty. Events dirtying evidence
	///    after enumeration (Rank/Pick) abort the pass — the certificate never issues
	///    over mixed-time evidence either. An EMPTY PROVIDER DOMAIN never certifies
	///    (6722A484): no-provider holds Deferred, never Exhausted.
	///
	/// The verdict carries {stamped atoms, dirty-epoch}; the caller's cancel arm
	/// re-verifies both against the live world via <see cref="CertificateCurrent"/> — a
	/// stale verdict replay, an atom drift or a single post-issue event fails closed.
	///
	/// FOOTPRINT-GAP METRIC (REVIEW_2026-10-16_refinery_resource_gap_metric_sol 80646D1B):
	/// candidate ranking and tier admission use the law's own adjacency convention — the
	/// minimum Chebyshev distance over every (footprint tile, resource cell) pair, the same
	/// semantics RefineryFlushFields applies (a "gap of 1" means the footprint stands one
	/// cell from a resource cell). A raw top-left distance is NOT the objective: on a 4x4
	/// footprint it reverses the intended preference (top-left (0,0) footprint-flush loses
	/// to (4,2) two cells off under top-left distance). Tier bound t admits gap ≤ t+1.
	/// </summary>
	public sealed class RefineryFallbackScan
	{
		/// <summary>
		/// 6722A484 (exact deterministic state — not hash collision): one coherence atom on
		/// the decisive path. <see cref="Kind"/> discriminates the payload the provider
		/// binds: a scalar atom (world/flag scalars packed into <see cref="A"/>), an actor
		/// atom (id in <see cref="Id"/>, ownership/alliance in <see cref="A"/>, in-world and
		/// dead state bits in <see cref="B"/>), a per-trait atom (owner actor id in
		/// <see cref="Id"/>, trait ordinal in <see cref="A"/>, the legality-relevant state
		/// bit in <see cref="B"/> — conditional buildable-area types and replaceable types
		/// travel in <see cref="Ref"/> as the rules' OWN set instance, compared by reference
		/// so a conditional toggle changes the observed value), and a rank atom (cell X in
		/// <see cref="A"/>, Y in <see cref="B"/>). Equality is pairwise over every field —
		/// no fold exists that could collide, and nothing sums so nothing overflows.
		/// </summary>
		public readonly struct CoherenceAtom : IEquatable<CoherenceAtom>
		{
			public CoherenceAtom(byte kind, uint id, int a, int b, object @ref)
			{
				Kind = kind;
				Id = id;
				A = a;
				B = b;
				Ref = @ref;
			}

			public readonly byte Kind;
			public readonly uint Id;
			public readonly int A;
			public readonly int B;
			public readonly object Ref;

			public bool Equals(CoherenceAtom other) =>
				Kind == other.Kind && Id == other.Id && A == other.A && B == other.B
					&& ReferenceEquals(Ref, other.Ref);

			public override bool Equals(object obj) => obj is CoherenceAtom other && Equals(other);
			public override int GetHashCode() => (int)(Kind ^ Id ^ (uint)A ^ (uint)B);
		}

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
		/// <see cref="RankCellSource"/> yields the declared resource cells (duplicates are
		/// deduped scan-side). <see cref="WholeMapCells"/> streams every map cell — the
		/// terminal tier's unconditional domain, used by NO other tier.
		/// <see cref="Footprint"/> is the item's footprint as relative cell offsets — the
		/// gap metric measures tile-to-resource Chebyshev distance over all of them.
		/// <see cref="TierBound"/> maps a relaxation tier to its Chebyshev footprint-gap
		/// bound: a legal cell is a candidate only while its nearest-resource gap is within
		/// the current tier's bound; int.MaxValue declares the terminal (unrestricted) tier.
		/// <see cref="CoherenceSource"/> yields the coherence atoms a terminal pass collects
		/// once per poll — scalar flags, one atom per scanned actor or trait, one per rank
		/// cell — compared EXACTLY (value fields plus reference identity), never hashed:
		/// DECISION_2026-10-16_refinery_dependency_audit_sol 6722A484 requires exact
		/// deterministic state, not a fold that could collide. Null disables signature
		/// tracking (a certificate then can never validate: <see cref="LiveAtoms"/> is the
		/// cancel-time recompute of the identical sequence and null fails closed).
		/// <see cref="MapIndex"/> maps a cell to its whole-map ordinal (-1 outside);
		/// <see cref="IndexCell"/> inverts it; <see cref="ReadRadius"/> declares how far a
		/// topLeft's legality may read (footprint extent + provider adjacency + dock reach)
		/// so evented changes can dirty exactly the already-enumerated cells that could
		/// have consulted the changed cell. <see cref="InMap"/> is the playable-bounds
		/// prefilter. <see cref="Legal"/> is the full engine legality predicate — the same
		/// atomic check the preferred offer path spends per attempt; each call sits inside
		/// one charged step. Rebuilt at each epoch so a re-list re-samples occupancy and
		/// reach.
		/// </summary>
		public readonly struct Snapshot
		{
			public Snapshot(
				Func<IEnumerator<Provider?>> providerSource,
				Func<IEnumerator<CPos>> rankCellSource,
				Func<IEnumerator<CPos>> wholeMapCells,
				Func<int, int> tierBound,
				IReadOnlyList<CVec> footprint,
				Func<IEnumerator<CoherenceAtom>> coherenceSource,
				Func<IEnumerator<CoherenceAtom>> liveAtoms,
				Func<CPos, int> mapIndex,
				Func<int, CPos> indexCell,
				int readRadius,
				Func<CPos, bool> inMap,
				Func<CPos, bool> legal)
			{
				ProviderSource = providerSource;
				RankCellSource = rankCellSource;
				WholeMapCells = wholeMapCells;
				TierBound = tierBound;
				Footprint = footprint;
				CoherenceSource = coherenceSource;
				LiveAtoms = liveAtoms;
				MapIndex = mapIndex;
				IndexCell = indexCell;
				ReadRadius = readRadius;
				InMap = inMap;
				Legal = legal;
			}

			public readonly Func<IEnumerator<Provider?>> ProviderSource;
			public readonly Func<IEnumerator<CPos>> RankCellSource;
			public readonly Func<IEnumerator<CPos>> WholeMapCells;
			public readonly Func<int, int> TierBound;
			public readonly IReadOnlyList<CVec> Footprint;
			public readonly Func<IEnumerator<CoherenceAtom>> CoherenceSource;
			public readonly Func<IEnumerator<CoherenceAtom>> LiveAtoms;
			public readonly Func<CPos, int> MapIndex;
			public readonly Func<int, CPos> IndexCell;
			public readonly int ReadRadius;
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

			/// <summary>The pass drained and every candidate failed pick-time revalidation — mixed evidence, never an emptiness proof.</summary>
			NoSite,

			/// <summary>A non-terminal pass enumerated no domain cells — the declared reach domain was empty.</summary>
			NoUniverse,

			/// <summary>
			/// A399083F §2 / BF5B8247(c): the terminal tier's whole-map pass drained under a
			/// coherent signature with an empty stale set and found no legal cell — the ONLY
			/// verdict that carries a cancellation certificate, and still valid only while
			/// <see cref="CertificateCurrent"/> reports the live world unchanged.
			/// </summary>
			Exhausted
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

		// Enumerate — provider squares on bounded tiers; the whole map unconditionally on
		// the terminal tier, whose frontier records how far into the index space the pass
		// has examined so evented changes can dirty exactly the already-evaluated cells.
		int providerIndex;
		bool providerArmed;
		int diskDx, diskDy;
		IEnumerator<CPos> domainEnum;
		bool wholeMapDone;
		int domainCells;
		int frontier;
		readonly HashSet<CPos> visited = new();
		readonly HashSet<CPos> legalSet = new();
		readonly List<CPos> legalCells = new();
		readonly SortedSet<int> stale = new();

		// Per-poll coherence sweep (terminal tier only): the exact atom sequence collected
		// in one same-tick pass — scalar flags, per-actor/per-trait state, rank-set content —
		// compared pairwise against the pass's stamped list; the first completed sweep
		// stamps it and a pairwise mismatch tears the pass (mixed-time evidence never
		/// certifies). A torn atom enumeration restarts bounded per poll.
		IEnumerator<CoherenceAtom> sweepEnum;
		readonly List<CoherenceAtom> sweepAtoms = new();
		readonly List<CoherenceAtom> passAtoms = new();
		bool sweepDone;
		int sweepRestarts;
		bool hasSignature;

		// The issued certificate: the stamped atom list rides passAtoms (cleared on the
		// next pass — the cert check also requires the exhausted verdict to still stand)
		// plus the dirty epoch at drain. Any covered event — or a pairwise atom drift —
		// stales it.
		long certDirtyEpoch;
		bool certIssued;
		long dirtyEpoch;

		// Rank — resumable per-footprint-tile two-sided scan over the X-sorted rank set;
		// the gap is the minimum Chebyshev distance over every (tile, resource) pair.
		int rankIndex;
		int rankTileIndex;
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

		// Relaxation ladder (maintainer ruling 2026-10-16): the footprint-gap constraint
		// relaxes tier-by-tier — each pass admits only candidates within TierBound(tier);
		// a drained tier advances immediately and re-passes under the wider bound; the
		// final tier (int.MaxValue) is the fully-removed constraint — the ONLY tier that
		/// may certify exhaustion, reported via <see cref="TerminalTier"/>.
		int tier;
		int maxRankGap;

		int relistAt = int.MinValue;

		// Placeholder only — reachable solely while relistAt is set (a real consumed or
		// aborted pass verdict); Deferred is the safe-direction default.
		Verdict waitingVerdict = Verdict.Deferred;
		CPos pickedCell;

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

		/// <summary>True once the latest pass ran at the terminal tier — the fully-removed
		/// constraint whose domain is the whole map and the only tier that may certify.</summary>
		public bool TerminalTier => maxRankGap == int.MaxValue;

		/// <summary>
		/// The provider's dirty channel: a covered legality input changed at
		/// <paramref name="changed"/> (an occupancy cell via the ActorMap influence layer,
		/// a resource cell via the resource layer, a terrain override via CustomTerrain's
		/// CellEntryChanged — the subscriber supplies the cell either way). The
		/// dirty epoch always advances so a certificate issued later in this tick sequence
		/// cannot outlive the event; on a live terminal pass the already-enumerated topLefts
		/// within the declared read radius mark for selective repair — everything else is
		/// examined in order anyway or will abort at the next poll's coherence gate.
		/// </summary>
		public void MarkDirtyCell(CPos changed)
		{
			dirtyEpoch++;
			if (!TerminalTier || frontier == 0 || phase == Phase.WaitForEpoch || snapshot.MapIndex == null)
				return;

			var r = Math.Max(0, snapshot.ReadRadius);
			for (var dy = -r; dy <= r; dy++)
			{
				for (var dx = -r; dx <= r; dx++)
				{
					var idx = snapshot.MapIndex(new CPos(changed.X + dx, changed.Y + dy));
					if (idx >= 0 && idx < frontier)
						stale.Add(idx);
				}
			}
		}

		/// <summary>
		/// BF5B8247(c) / A399083F §5: the cancel arm's live re-verification. True only while
		/// the terminal pass's certificate stands: issued by a coherent drain AND the live
		/// world still matches — the dirty epoch unmoved since issue AND the provider's
		/// live atom re-enumeration still pairwise-equal to the stamped sequence. Any
		/// drift, any covered event, a replayed-but-stale verdict or a missing coverage
		/// hook fails closed to false.
		/// </summary>
		public bool CertificateCurrent()
		{
			if (!certIssued || phase != Phase.WaitForEpoch || waitingVerdict != Verdict.Exhausted)
				return false;

			if (dirtyEpoch != certDirtyEpoch || snapshot.LiveAtoms == null)
				return false;

			// Exact re-read: the live atom sequence must equal the stamped list pairwise —
			// no folding, so no collision can pass; a torn live enumeration fails closed.
			try
			{
				using var live = snapshot.LiveAtoms();
				var i = 0;
				while (live.MoveNext())
				{
					if (i >= passAtoms.Count || !live.Current.Equals(passAtoms[i]))
						return false;

					i++;
				}

				return i == passAtoms.Count;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}

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
		/// factory runs once per pass start and does no enumeration itself. On the terminal
		/// tier every poll first completes the same-tick signature sweep; an uncompletable
		/// sweep holds the pass (the certificate requires coherent state).
		/// </summary>
		public Verdict Poll(int cellBudget, int now, Func<Snapshot> snapshotAtPassStart, out CPos? site)
		{
			site = null;
			var work = 0;

			if (cellBudget <= 0)
			{
				LastPollWork = 0;
				// A zero-budget poll performs no work — it may only replay a verdict a
				// completed or aborted pass actually produced (relistAt set), never the
				// initial placeholder: a fresh scan reports mid-pass, not an empty domain.
				if (phase == Phase.WaitForEpoch && relistAt != int.MinValue)
				{
					site = ReplaySite();
					return waitingVerdict;
				}

				return Verdict.Deferred;
			}

			if (phase == Phase.WaitForEpoch)
			{
				if (now < relistAt)
				{
					LastPollWork = work;
					site = ReplaySite();
					return waitingVerdict;
				}

				work++;
				StartPass(snapshotAtPassStart());
			}

			// The signature sweep is per-poll state: it must complete inside this poll (the
			// world does not mutate inside a manager sweep, so a one-poll fold is coherent;
			// a fold spanning polls would certify a mixed-time state that never existed).
			sweepDone = false;
			sweepAtoms.Clear();
			sweepRestarts = 0;
			sweepEnum?.Dispose();
			sweepEnum = null;

			while (work < cellBudget && phase != Phase.WaitForEpoch)
			{
				if (TerminalTier)
				{
					// Evidence dirtied after the terminal enumerate drained — the ranked and
					// picked sets are mixed-time and cannot be repaired in place: abort.
					if (stale.Count > 0 && (phase == Phase.Rank || phase == Phase.Pick))
					{
						AbortPass(now);
						continue;
					}

					// Every terminal poll first re-proves coherence — a drifted signature
					// aborts the pass inside SweepStep.
					if (!sweepDone)
					{
						work++;
						SweepStep(now);
						continue;
					}
				}

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
						EnumerateStep(now);
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
								// The pick consumes the pass — leaving phase at Pick would let a later
								// Poll keep draining stale candidates. Site replays idempotently (with
								// its cell) inside the epoch; a poll past it re-lists fresh.
								DisposeCursors();
								waitingVerdict = Verdict.Site;
								pickedCell = site.Value;
								relistAt = now + relistEpochTicks;
								phase = Phase.WaitForEpoch;
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
			if (phase == Phase.WaitForEpoch)
			{
				site = ReplaySite();
				return waitingVerdict;
			}

			return Verdict.Deferred;
		}

		// A replayed Site carries its delivered cell; every other verdict has no payload.
		CPos? ReplaySite() => waitingVerdict == Verdict.Site ? pickedCell : null;

		enum PickResult { Continue, Picked, Drained }

		// One coherence atom per step: the source yields the scalar atom first, then one
		// atom per scanned actor or trait, then one per rank cell. The first completed
		// sweep of a pass stamps passAtoms; every later sweep must equal it pairwise —
		// any drift aborts (6722A484 exact state, no hash collision surface). A torn live
		// enumeration restarts this poll's sweep in place, bounded.
		void SweepStep(int now)
		{
			if (sweepEnum == null)
				sweepEnum = snapshot.CoherenceSource != null ? snapshot.CoherenceSource() : EmptyAtoms();

			bool more;
			try
			{
				more = sweepEnum.MoveNext();
			}
			catch (InvalidOperationException)
			{
				sweepEnum.Dispose();
				sweepEnum = null;
				sweepAtoms.Clear();
				if (++sweepRestarts > MaxCollectRestarts)
					AbortPass(now);
				return;
			}

			if (!more)
			{
				sweepEnum.Dispose();
				sweepEnum = null;
				sweepDone = true;
				if (!hasSignature)
				{
					passAtoms.Clear();
					passAtoms.AddRange(sweepAtoms);
					hasSignature = true;
				}
				else if (!AtomsEqual(sweepAtoms, passAtoms))
				{
					AbortPass(now);
				}

				return;
			}

			sweepAtoms.Add(sweepEnum.Current);
		}

		static bool AtomsEqual(IReadOnlyList<CoherenceAtom> a, IReadOnlyList<CoherenceAtom> b)
		{
			if (a.Count != b.Count)
				return false;

			for (var i = 0; i < a.Count; i++)
				if (!a[i].Equals(b[i]))
					return false;

			return true;
		}

		static IEnumerator<CoherenceAtom> EmptyAtoms()
		{
			yield break;
		}

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

		void EnumerateStep(int now)
		{
			if (TerminalTier)
			{
				// The terminal tier's declared domain is the WHOLE MAP — unconditionally,
				/// even when provider domains exist (they are supersets for the preferred
				// tiers only, never a substitute for the final proof).
				if (!wholeMapDone)
				{
					domainEnum ??= snapshot.WholeMapCells();
					if (!domainEnum.MoveNext())
					{
						domainEnum.Dispose();
						domainEnum = null;
						wholeMapDone = true;
					}
					else
					{
						var cell = domainEnum.Current;
						domainCells++;
						var idx = snapshot.MapIndex != null ? snapshot.MapIndex(cell) : -1;
						frontier = idx >= 0 ? Math.Max(frontier, idx + 1) : frontier + 1;
						EvaluateTerminalCell(cell);
					}

					return;
				}

				// Selective repair: exactly the already-enumerated cells an event dirtied
				// are re-examined, one index per charged step. New marks may arrive while
				// repair drains — the phase completes only on an empty set.
				if (stale.Count > 0)
				{
					var idx = stale.Min;
					stale.Remove(idx);
					if (snapshot.IndexCell != null)
						EvaluateRepairCell(snapshot.IndexCell(idx));
					return;
				}

				// A399083F §2 / BF5B8247(c): the coherent drain — every map cell examined
				// under an atom sequence that stayed pairwise-current the whole pass, no
				// dirtied evidence outstanding, and none legal. 6722A484: an empty PROVIDER
				// domain is never an exhaustion proof — with no declared reach domain there
				// is nothing to exhaust, so the pass drains to an honest hold verdict and
				// the epoch re-lists (providers may appear). A domain that streamed ZERO
				// cells (domainCells == 0) is not examined evidence either — fail closed to
				// the same hold. The certificate rides the verdict; the caller's cancel arm
				// re-verifies it live before cancelling.
				if (legalCells.Count == 0 && providers.Count > 0 && domainCells > 0)
				{
					certDirtyEpoch = dirtyEpoch;
					certIssued = true;
					DisposeCursors();
					waitingVerdict = Verdict.Exhausted;
					relistAt = now + relistEpochTicks;
					phase = Phase.WaitForEpoch;
					return;
				}

				phase = Phase.Rank;
				return;
			}

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

				EvaluateDomainCell(cell);
				return;
			}

			phase = Phase.Rank;
		}

		void EvaluateDomainCell(CPos cell)
		{
			if (!snapshot.InMap(cell))
				return;

			domainCells++;
			if (visited.Add(cell) && snapshot.Legal(cell) && legalSet.Add(cell))
				legalCells.Add(cell);
		}

		// Terminal enumeration: the whole-map stream yields each index once, so no dedupe —
		// legal dedupe lives in legalSet (a repair re-eval of a still-legal cell adds nothing).
		void EvaluateTerminalCell(CPos cell)
		{
			if (!snapshot.InMap(cell))
				return;

			if (snapshot.Legal(cell) && legalSet.Add(cell))
				legalCells.Add(cell);
		}

		void EvaluateRepairCell(CPos cell)
		{
			if (!snapshot.InMap(cell))
				return;

			if (snapshot.Legal(cell) && legalSet.Add(cell))
				legalCells.Add(cell);
		}

		// One bounded step: either one halving of a tile's X lower-bound search or one
		// rank-cell evaluation in the two-sided expansion that follows it. The candidate's
		// gap is the minimum Chebyshev distance over every footprint tile (80646D1B — the
		/// law's flush convention, not top-left distance).
		void RankStep()
		{
			if (rankIndex >= legalCells.Count)
			{
				phase = Phase.Pick;
				return;
			}

			var footprint = snapshot.Footprint ?? (IReadOnlyList<CVec>)Array.Empty<CVec>();
			if (rankTileIndex >= footprint.Count)
			{
				// Tier admission: only cells within the current bound join the pick order.
				// int.MaxValue rank (no rank cells at all) qualifies only at the terminal
				// tier — the fully-removed constraint.
				var cell = legalCells[rankIndex];
				if (rankBest <= maxRankGap)
					candidates.Add((cell, rankBest));

				rankIndex++;
				boundDone = false;
				rankLo = 0;
				rankHi = rankCells.Count;
				side = 0;
				scanIdx = 0;
				rankTileIndex = 0;
				rankBest = int.MaxValue;
				return;
			}

			var tile = legalCells[rankIndex] + footprint[rankTileIndex];
			if (!boundDone)
			{
				if (rankLo < rankHi)
				{
					var mid = (rankLo + rankHi) >> 1;
					if (rankCells[mid].X < tile.X)
						rankLo = mid + 1;
					else
						rankHi = mid;
					return;
				}

				boundDone = true;
				side = 0;
				scanIdx = rankLo;
				return;
			}

			if (side == 0)
			{
				if (scanIdx < rankCells.Count)
				{
					var r = rankCells[scanIdx];
					var dx = r.X - tile.X;
					if (dx >= rankBest)
					{
						side = 1;
						scanIdx = rankLo - 1;
					}
					else
					{
						var dy = r.Y - tile.Y;
						rankBest = Math.Min(rankBest, Chebyshev(dx, dy));
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
				var dx = tile.X - r.X;
				if (dx < rankBest)
				{
					var dy = r.Y - tile.Y;
					rankBest = Math.Min(rankBest, Chebyshev(dx, dy));
					scanIdx--;
					return;
				}
			}

			// This tile's scan drained — advance to the next footprint offset with the
			// global best carried over (it only ever tightens the prune bound).
			rankTileIndex++;
			boundDone = false;
			rankLo = 0;
			rankHi = rankCells.Count;
			side = 0;
			scanIdx = 0;
		}

		static int Chebyshev(int dx, int dy)
		{
			var ax = dx < 0 ? -dx : dx;
			var ay = dy < 0 ? -dy : dy;
			return ax > ay ? ax : ay;
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
			maxRankGap = Math.Max(0, s.TierBound(tier));
			providers.Clear();
			seenRank.Clear();
			rankCells.Clear();
			visited.Clear();
			legalSet.Clear();
			legalCells.Clear();
			stale.Clear();
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
			frontier = 0;
			hasSignature = false;
			passAtoms.Clear();
			sweepAtoms.Clear();
			certIssued = false;
			rankIndex = 0;
			rankTileIndex = 0;
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

		// The pass could not finish under coherent inputs (torn collections beyond the
		// restart bound, a drifted signature, or post-enumerate evidence churn) — defer a
		// whole epoch rather than certify or cancel over mixed-time evidence. Deferred is
		// the honest verdict: nothing was enumerated or proven.
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
			sweepEnum?.Dispose();
			sweepEnum = null;
		}

		static int CompareXy(CPos a, CPos b)
		{
			var d = a.X.CompareTo(b.X);
			return d != 0 ? d : a.Y.CompareTo(b.Y);
		}
	}
}
