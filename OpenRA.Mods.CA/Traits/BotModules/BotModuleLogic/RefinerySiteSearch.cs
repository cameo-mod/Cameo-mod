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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// REFSCAN D-5 (design _12 3be85dba + ERRATUM_01 9c33a59b + IMPL_DOC_NOTES_01 ac106581; TREATMENT arm only):
	/// the site-first anytime refinery search. One episode per law refinery item in a building queue: it
	/// searches while the item is in production under a per-visit unit cap, keeps the K best legal sites and
	/// reserves the best one (mask + anchor), and at READY places from the bot module tick — live recheck,
	/// next-best, then the deadline phase — so nothing on the READY-to-placed path waits on WaitTicks, a park,
	/// the force or a relist. Synced bot code: integer ticks and units, ActorID / (d2, X, Y) / score orders and
	/// one keyed RNG stream with counted draws. Owned by <see cref="BaseBuilderBotModuleCA"/>, pumped from its
	/// BotTick before the queue managers run. Inert (no episode, empty mask) unless the law provider publishes
	/// <see cref="RefinerySiteSearchSettings.Enabled"/>.
	/// </summary>
	public sealed class RefinerySiteSearch
	{
		// Units of one O-3 or R chunk inside the O-3 : R phase (1 : 1, D-3).
		const int ChunkUnits = 16;

		// Units the O-3 : R phase runs before the O-1 + O-2 pass restarts (anchors and claims are re-peeked).
		const int O3RPhaseVisits = 4;

		// Before READY a full K-list only revalidates; the stream runs at most once per this many ticks. This build
		// has no STAGGER, so a visit is every tick (V = 1) and the D-3 units were sized for one visit per N ticks.
		const int FullListSearchIntervalTicks = 25;

		sealed class Candidate
		{
			public CPos Cell;
			public RefinerySiteScore Score;
			public RefineryAnchorClaim? Claim;
			public int FoundTick;

			// REF-DEMAND-01: the site is legal only at the fallback spacing (its zone's full-spacing pass found none).
			public bool SpacingFallback;
		}

		sealed class Episode
		{
			public long Key;
			public Actor Producer;
			public ProductionQueue Queue;
			public ProductionItem Item;
			public string ItemName;
			public ActorInfo ActorInfo;
			public BuildingInfo Bi;
			public List<WVec> DockOffsets;
			public int OpenTick;

			public readonly List<Candidate> Best = new();
			public int Rotate;
			public CPos? MaskCell;

			// Candidate stream state.
			public bool InO3R;
			public int O3RUnits;
			public bool NextChunkIsO3 = true;
			public List<RefineryAnchorClaim> Anchors;
			public int AnchorIndex;
			public List<CPos> Annulus;
			public int AnnulusIndex;
			public HashSet<CPos> Zone0;
			public HashSet<CPos> Zone1;
			public bool FallbackExhausted;

			// REF-DEMAND-01: sites legal only at the fallback spacing wait here, never in Best, until the strict (full spacing) search has finished
			// with no site; StrictDone is set at that point and cleared when the next pass starts.
			public readonly List<Candidate> Relaxed = new();
			public bool StrictDone;

			// READY timeline (sec 4).
			public int? ReadyTick;
			public int? ReadyPrime;
			public bool DemandHold;
			public bool HandoffDone;
			public CPos? HandoffBest0;
			public Candidate Outstanding;
			public int OrderTick;
			public RefineryPlacedVia OutstandingVia;
			public bool OutstandingAfterVLast;
			public int Rejections;
			public bool HeldAtVLast;
			public int? VLast;
			public bool McvOverlap;
			public int? FirstFind;
			public bool SelfCancelled;
			public readonly Dictionary<CPos, int> LastIllegal = new();

			public int Units;
			public long Draws;
			public BotCooldown FullListSearchCooldown;
		}

		readonly BaseBuilderBotModuleCA baseBuilder;
		readonly World world;
		readonly Player player;
		readonly IResourceLayer resourceLayer;
		readonly RefineryReservationMask mask = new();
		readonly List<Episode> episodes = new();
		readonly Dictionary<ProductionItem, Episode> byItem = new();
		readonly Dictionary<CPos, long> anchorHeldBy = new();
		readonly HashSet<CPos> watched = new();
		readonly HashSet<CPos> dirty = new();
		readonly List<(CPos Center, int Reach)> providers = new();
		int providersTick = -1;
		(uint, string) providersFor;
		long nextKey;
		bool wired;
		bool watchStale;
		MersenneTwister rng;

		// REF-DEMAND-01: the law of the running Pump — the spacing source of every legality check made in it.
		IBotExpansionTargetProvider pumpLaw;

		public RefinerySiteSearch(BaseBuilderBotModuleCA baseBuilder, World world, Player player, IResourceLayer resourceLayer)
		{
			this.baseBuilder = baseBuilder;
			this.world = world;
			this.player = player;
			this.resourceLayer = resourceLayer;
		}

		/// <summary>Open episodes (tests and DEV records).</summary>
		public int OpenEpisodes => episodes.Count;

		/// <summary>True iff an open episode owns this queue item — the queue sweep must not place it.</summary>
		public bool Owns(ProductionItem item) => item != null && byItem.ContainsKey(item);

		/// <summary>
		/// F2: the mask check for a placement that is not an episode's own (the queue sweep's emission point and
		/// the engine MCV deploy veto, D-6). True iff a footprint cell is reserved by any episode.
		/// </summary>
		public bool Masked(ActorInfo building, CPos topLeft)
		{
			if (mask.IsEmpty || building == null)
				return false;

			var bi = building.TraitInfoOrDefault<BuildingInfo>();
			return bi != null ? mask.Blocks(bi.Tiles(topLeft), RefineryReservationMask.None) : mask.Blocks(new[] { topLeft }, RefineryReservationMask.None);
		}

		public void Dispose()
		{
			if (!wired)
				return;

			world.ActorMap.CellUpdated -= CellChanged;
			world.Map.CustomTerrain.CellEntryChanged -= CellChanged;
			if (resourceLayer != null)
				resourceLayer.CellChanged -= ResourceChanged;

			wired = false;
		}

		void Wire()
		{
			if (wired)
				return;

			wired = true;
			world.ActorMap.CellUpdated += CellChanged;
			world.Map.CustomTerrain.CellEntryChanged += CellChanged;
			if (resourceLayer != null)
				resourceLayer.CellChanged += ResourceChanged;
		}

		// Sec 3.4: the per-cell watch over every K-list member's footprint marks it dirty, O(1) per event.
		void CellChanged(CPos cell)
		{
			if (watched.Contains(cell))
				dirty.Add(cell);
		}

		void ResourceChanged(CPos cell, string type) => CellChanged(cell);

		/// <summary>
		/// One bot visit (BaseBuilderBotModuleCA.BotTick, before the queue managers). With the law off or the
		/// search disabled every open episode ends LAW OFF and releases its reservation in this same tick.
		/// </summary>
		public void Pump(IBot bot, IBotExpansionTargetProvider law)
		{
			pumpLaw = law;
			var settings = law?.RefinerySiteSearch ?? default;
			if (law == null || !settings.Enabled)
			{
				for (var i = episodes.Count - 1; i >= 0; i--)
					End(episodes[i], RefineryEpisodeEnd.LawOff);

				dirty.Clear();
				return;
			}

			Wire();
			rng ??= BotRng.For(player, "RefinerySiteSearch");
			var now = world.WorldTick;
			BindNewItems(law, now);

			foreach (var e in episodes.ToList())
				Visit(bot, law, settings, e, now);

			dirty.Clear();
			if (watchStale)
			{
				watched.Clear();
				foreach (var e in episodes)
					foreach (var c in e.Best)
						foreach (var t in e.Bi.Tiles(c.Cell))
							watched.Add(t);

				watchStale = false;
			}
		}

		// Sec 2 / 2.1: an episode opens when a refinery item is first seen in a building queue (queue order =
		// admission order, so binding is FIFO per producer and name), keyed by a per-player sequence. Each
		// episode binds one item instance by reference; its end follows that instance. After a load every
		// queued refinery opens a fresh episode the same way (nothing is saved).
		void BindNewItems(IBotExpansionTargetProvider law, int now)
		{
			// The planner's crawl link and front defence are not refinery placements (the queue sweep classes them
			// BaseCrawl / Defense before the refinery test), so a same-named want is never an episode.
			var link = law.WantedLinkBuilding;
			var front = law.WantedFrontDefence;
			foreach (var category in baseBuilder.Info.BuildingQueues.OrderBy(c => c, StringComparer.Ordinal))
			{
				foreach (var queue in AIUtils.FindQueues(player, category))
				{
					foreach (var item in queue.AllQueued())
					{
						if (!baseBuilder.Info.RefineryTypes.Contains(item.Item) || byItem.ContainsKey(item)
							|| item.Item == link || item.Item == front)
							continue;

						if (!world.Map.Rules.Actors.TryGetValue(item.Item, out var ai) || ai.TraitInfoOrDefault<BuildingInfo>() is not { } bi)
							continue;

						var e = new Episode
						{
							Key = nextKey++,
							Producer = queue.Actor,
							Queue = queue,
							Item = item,
							ItemName = item.Item,
							ActorInfo = ai,
							Bi = bi,
							DockOffsets = ai.TraitInfos<DockHostInfo>().Select(d => d.DockOffset).DefaultIfEmpty(WVec.Zero).ToList(),
							OpenTick = now,
						};

						episodes.Add(e);
						byItem[item] = e;
						Dev(e, now, "open", "");
					}
				}
			}
		}

		void Visit(IBot bot, IBotExpansionTargetProvider law, RefinerySiteSearchSettings settings, Episode e, int now)
		{
			if (e.Producer.IsDead || !e.Producer.IsInWorld || e.Producer.Owner != player)
			{
				End(e, RefineryEpisodeEnd.ProducerLost);
				return;
			}

			if (!e.Queue.AllQueued().Any(i => ReferenceEquals(i, e.Item)))
			{
				// The item left its queue: an accepted PlaceBuilding removes it (Placed); a cancel removes it
				// without one of our orders in flight (Cancelled; the certified no-site cancel is its own end).
				End(e, e.SelfCancelled ? RefineryEpisodeEnd.CancelledNoSite
					: e.Outstanding != null ? RefineryEpisodeEnd.Placed : RefineryEpisodeEnd.Cancelled);
				return;
			}

			if (e.SelfCancelled)
				return;

			// Sec 4.1 / D-1 (a): t_R is the Done flip; a demand-bound item's clock runs from max(t_R, deploy).
			if (e.Item.Done && e.ReadyTick == null)
			{
				e.ReadyTick = now;
				Dev(e, now, "done", "");
			}

			var demand = baseBuilder.ExpansionPrebuildEnabled ? baseBuilder.DemandForQueuedItem(e.ItemName, e.Producer) : null;
			if (e.ReadyTick != null && e.ReadyPrime == null)
			{
				if (demand != null && !demand.Deployed)
					e.DemandHold = true;
				else
				{
					e.ReadyPrime = now;
					Dev(e, now, "ready", $"tR={e.ReadyTick} hold={(e.DemandHold ? 1 : 0)}");
					ExpirePrefetchedRelaxed(e, now);
				}
			}

			var ready = e.ReadyPrime != null;
			var units = ready && e.Best.Count == 0 && e.Outstanding == null ? settings.DeadlineVisitUnits : settings.SearchVisitUnits;
			var handoffVisit = ready && !e.HandoffDone;
			if (handoffVisit)
				e.HandoffBest0 = e.Best.Count > 0 ? e.Best[0].Cell : null;

			e.FallbackExhausted = false;
			Revalidate(law, e, now, ref units);

			if (!ready)
			{
				if (e.Best.Count < RefinerySiteSearchLogic.K || e.FullListSearchCooldown.Ready(now))
				{
					e.FullListSearchCooldown.Start(now, FullListSearchIntervalTicks);
					Search(law, settings, e, demand, now, ref units, false);
				}

				UpdateMask(e);
				return;
			}

			var latency = settings.OrderLatencyTicks;
			if (e.Outstanding != null && now >= e.OrderTick + latency)
			{
				// Sec 4.2 step 3: still queued (Done) after t_order + L_o*, so the order was rejected at application.
				e.Rejections++;
				e.Best.Remove(e.Outstanding);
				Dev(e, now, "reject", $"cell={e.Outstanding.Cell}");
				e.Outstanding = null;
				watchStale = true;
			}

			// Sec 4.2 step 4: the duplicate guard — no re-issue before t_order + L_o*.
			if (e.Outstanding == null)
			{
				if (!TryPlace(bot, law, settings, e, demand, now, handoffVisit ? null : RefineryPlacedVia.DeadlinePhase))
				{
					// Sec 4.3: the deadline phase — the search runs on at the deadline cap; a find places at once.
					units = Math.Max(units, settings.DeadlineVisitUnits);
					Search(law, settings, e, demand, now, ref units, true);
					if (!TryPlace(bot, law, settings, e, demand, now, RefineryPlacedVia.DeadlinePhase)
						&& e.FallbackExhausted && RefineryDemand.RelaxedGate(e.StrictDone, e.Best.Count, e.Relaxed.Count) != RefineryDemand.RelaxedStep.Deferred
						&& baseBuilder.ManagerFor(e.Queue.Info.Type) is { } qm
						&& qm.EpisodeCertifiedCancel(bot, e.Queue, e.Item))
					{
						// Sec 3.6: the only cancel authority, the _08 certificate with two verdicts.
						e.SelfCancelled = true;
						Dev(e, now, "cancel", "nosite");
					}
				}
			}

			e.HandoffDone = true;

			// Sec 4.3 / E-2: v_last is the last visit with v + L_o* <= t_R' + 75, from the real visit schedule;
			// "held a reserved site at v_last" = an order outstanding when that visit ends.
			if (RefinerySiteSearchLogic.IsOrderingVisit(now, e.ReadyPrime.Value, latency))
			{
				e.VLast = now;
				e.HeldAtVLast = e.Outstanding != null;
			}

			UpdateMask(e);
		}

		// Sec 4.2 step 1: live recheck of the K-list in order; the first legal member is ordered this visit.
		bool TryPlace(IBot bot, IBotExpansionTargetProvider law, RefinerySiteSearchSettings settings, Episode e,
			ExpansionDemand demand, int now, RefineryPlacedVia? via)
		{
			while (e.Best.Count > 0)
			{
				var c = e.Best[0];
				e.Units++;
				if (!LegalFor(e, c))
				{
					Drop(e, c, now);
					continue;
				}

				var qm = baseBuilder.ManagerFor(e.Queue.Info.Type);
				if (qm == null)
					return false;

				qm.EmitEpisodePlacement(bot, e.Queue, e.Item, c.Cell, c.Claim, demand);
				e.Outstanding = c;
				e.OrderTick = now;
				e.OutstandingVia = via ?? (e.HandoffBest0 == c.Cell ? RefineryPlacedVia.ReservedHit : RefineryPlacedVia.NextBest);
				e.OutstandingAfterVLast = !RefinerySiteSearchLogic.IsOrderingVisit(now, e.ReadyPrime.Value, settings.OrderLatencyTicks);
				e.LastIllegal.TryGetValue(c.Cell, out var tlo);
				Dev(e, now, "order", $"cell={c.Cell} score={c.Score} vfind={c.FoundTick} tlo={(tlo > 0 ? tlo : e.OpenTick)} "
					+ $"via={e.OutstandingVia} late={(e.OutstandingAfterVLast ? 1 : 0)}");
				return true;
			}

			return false;
		}

		// Sec 3.4 / N-b: Best[0], then dirty members, then one rotated member — each one unit.
		void Revalidate(IBotExpansionTargetProvider law, Episode e, int now, ref int units)
		{
			if (e.Best.Count == 0)
				return;

			var check = new List<Candidate> { e.Best[0] };
			if (dirty.Count > 0)
				foreach (var c in e.Best.Skip(1))
					if (e.Bi.Tiles(c.Cell).Any(dirty.Contains))
						check.Add(c);

			if (e.Best.Count > 1)
			{
				var r = e.Best[1 + (e.Rotate++ % (e.Best.Count - 1))];
				if (!check.Contains(r))
					check.Add(r);
			}

			foreach (var c in check)
			{
				if (units <= 0)
					break;

				if (ReferenceEquals(c, e.Outstanding))
					continue;

				units--;
				e.Units++;
				if (!LegalFor(e, c))
					Drop(e, c, now);
			}
		}

		void Drop(Episode e, Candidate c, int now)
		{
			// Sec 8 / T-15b: a reserved site lost to an own construction yard is the mcv-overlap cross-check.
			if (e.Best.Count > 0 && ReferenceEquals(e.Best[0], c))
				foreach (var t in e.Bi.Tiles(c.Cell))
					foreach (var a in world.ActorMap.GetActorsAt(t))
						if (a.Owner == player && baseBuilder.Info.ConstructionYardTypes.Contains(a.Info.Name))
							e.McvOverlap = true;

			e.LastIllegal[c.Cell] = now;
			e.Best.Remove(c);
			watchStale = true;
		}

		// Sec 3.1 / 3.4: LegalFor(e, c) = Legal(c) && !MaskBlocks(c, footprint, e), and a class-0 site's anchor is
		// not the reserved anchor of another episode.
		bool LegalFor(Episode e, Candidate c)
		{
			if (c.Claim is { } claim && anchorHeldBy.TryGetValue(claim.Anchor, out var holder) && holder != e.Key)
				return false;

			return LegalCell(e, c.Cell) && SpacingAdmits(c.Cell, c.SpacingFallback);
		}

		// REF-DEMAND-01 (row ref_demand): the site's friendly-spacing tier; always allowed with the spacing off (no law, classic, switch off).
		int SpacingTier(CPos cell) =>
			pumpLaw != null && pumpLaw.RefinerySpacingActive ? pumpLaw.RefinerySiteSpacingTier(cell) : RefineryDemand.SpacingAllowed;

		bool SpacingAdmits(CPos cell, bool fallback)
		{
			var tier = SpacingTier(cell);
			return tier == RefineryDemand.SpacingAllowed || (fallback && tier == RefineryDemand.SpacingFallbackOnly);
		}

		bool LegalCell(Episode e, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!BaseBuilderQueueEvalCA.RefinerySiteLegal(world, player, resourceLayer, e.ActorInfo, e.Bi, e.Producer, true,
					e.DockOffsets, baseBuilder.ResourceMapModule?.Info.ValuableResourceTypes, cell))
				return false;

			// FIX-1 (587A0D62): the candidate's dock cells are checked too, so a site whose dock falls on another
			// episode's reserved footprint or dock is refused before that episode's building exists.
			return mask.IsEmpty || !mask.BlocksSite(e.Bi.Tiles(cell), DockCells(e, cell), e.Key);
		}

		// The dock cells of a site at this top-left cell: the same cells UpdateMask reserves and LegalCell checks.
		List<CPos> DockCells(Episode e, CPos cell)
		{
			var cells = new List<CPos>(e.DockOffsets.Count);
			foreach (var off in e.DockOffsets)
				cells.Add(world.Map.CellContaining(world.Map.CenterOfCell(cell) + e.Bi.CenterOffset(world) + off));

			return cells;
		}

		void Offer(Episode e, CPos cell, RefinerySiteScore score, RefineryAnchorClaim? claim, int now, bool spacingFallback = false)
		{
			var c = new Candidate { Cell = cell, Score = score, Claim = claim, FoundTick = now, SpacingFallback = spacingFallback };
			if (!spacingFallback)
				EvictRelaxedForStrict(e, cell, now);

			if (spacingFallback)
			{
				// Deferred: a relaxed site never enters Best before the strict search is exhausted (PromoteRelaxed).
				RefinerySiteSearchLogic.InsertBest(e.Relaxed, c, x => x.Score, x => x.Cell);
				return;
			}

			if (RefinerySiteSearchLogic.InsertBest(e.Best, c, x => x.Score, x => x.Cell) < 0)
				return;

			e.FirstFind ??= now;
			watchStale = true;
		}

		// REF-PREFETCH-FRESH (row ref_prefetch_fresh): a relaxed site promoted into Best before READY lives on a certificate that may be stale when the
		// building is done. At the READY flip every such member is dropped and the candidate stream restarts its pass (the same step the pass end takes), so
		// a relaxed site is only used again after a certificate drawn from the current board (PromoteRelaxed). Strict members and an issued order stay.
		void ExpirePrefetchedRelaxed(Episode e, int now)
		{
			if (!baseBuilder.RefPrefetchFreshArmed)
				return;

			var expired = RefinerySiteSearchLogic.TakeRelaxed(e.Best, x => x.SpacingFallback, x => ReferenceEquals(x, e.Outstanding));
			if (expired.Count == 0)
				return;

			e.InO3R = false;
			e.Anchors = null;
			watchStale = true;
			Dev(e, now, "relaxed_expired", $"n={expired.Count} first={expired[0].Cell}");
		}

		// REF-PREFETCH-FRESH: a strict offer outranks every relaxed one, whatever the score classes say (a relaxed claim site is class 0, a strict widened site class 1).
		// The relaxed members leave Best and wait in Relaxed again; they come back through PromoteRelaxed only when no strict site stands.
		void EvictRelaxedForStrict(Episode e, CPos cell, int now)
		{
			if (!baseBuilder.RefPrefetchFreshArmed || e.Best.Count == 0)
				return;

			var evicted = RefinerySiteSearchLogic.TakeRelaxed(e.Best, x => x.SpacingFallback, x => ReferenceEquals(x, e.Outstanding));
			if (evicted.Count == 0)
				return;

			foreach (var r in evicted)
				RefinerySiteSearchLogic.InsertBest(e.Relaxed, r, x => x.Score, x => x.Cell);

			watchStale = true;
			Dev(e, now, "relaxed_outranked", $"n={evicted.Count} strict={cell}");
		}

		// REF-DEMAND-01: the exhaustive strict certificate arrived (the paged scan drained its whole domain; random samples and the visit budget never count). With no strict site the legal relaxed ones enter Best now; with a strict site they are dropped.
		void PromoteRelaxed(Episode e, int now)
		{
			e.StrictDone = true;
			if (RefineryDemand.RelaxedGate(true, e.Best.Count, e.Relaxed.Count) == RefineryDemand.RelaxedStep.Promote)
				foreach (var c in e.Relaxed)
					if (LegalFor(e, c) && RefinerySiteSearchLogic.InsertBest(e.Best, c, x => x.Score, x => x.Cell) >= 0)
					{
						e.FirstFind ??= now;
						watchStale = true;
					}

			e.Relaxed.Clear();
		}

		// Sec 3.3: the cyclic candidate stream — the O-1 + O-2 pass (anchors re-peeked at pass start, each
		// annulus in (d2, X, Y) order), then O-3 : R at 1 : 1 for a bounded number of visits, then again.
		void Search(IBotExpansionTargetProvider law, RefinerySiteSearchSettings settings, Episode e, ExpansionDemand demand,
			int now, ref int units, bool deadline)
		{
			// Every iteration charges a unit or moves the stream to a charging step; the guard is a backstop only.
			var guard = 0;
			while (units > 0 && guard++ < 65536)
			{
				if (!e.InO3R)
				{
					if (!StepClaims(law, e, demand, now, ref units))
					{
						e.InO3R = true;
						e.O3RUnits = 0;
					}

					continue;
				}

				if (e.O3RUnits >= O3RPhaseVisits * settings.SearchVisitUnits)
				{
					e.InO3R = false;
					e.Anchors = null;
					continue;
				}

				var chunk = Math.Min(units, ChunkUnits);
				if (e.NextChunkIsO3)
					StepFallback(law, settings, e, now, chunk, deadline);
				else
					StepSamples(law, e, now, chunk);

				e.NextChunkIsO3 = !e.NextChunkIsO3;
				units -= chunk;
				e.Units += chunk;
				e.O3RUnits += chunk;
			}
		}

		// O-1 + O-2: one charged step (an anchor's preparation, or one annulus cell). False = the pass is done.
		bool StepClaims(IBotExpansionTargetProvider law, Episode e, ExpansionDemand demand, int now, ref int units)
		{
			if (e.Anchors == null)
			{
				// O-1: the ECON-A R6 claim the demand reserved at admission (tier 0), then O-2 in the ladder's order.
				e.Anchors = new List<RefineryAnchorClaim>();
				e.Relaxed.Clear();
				e.StrictDone = false;
				CPos? o1 = null;
				if (demand?.ReservedClaim is { } reserved && law.RefineryAnchorReserved(reserved.Anchor, demand))
				{
					e.Anchors.Add(reserved);
					o1 = reserved.Anchor;
				}

				var requestRef = demand?.Mcv ?? BaseBuilderQueueEvalCA.FirstRequestedRefinery(baseBuilder.RequestedRefineries);
				CPos? near = requestRef != null && baseBuilder.RequestedRefineries.TryGetValue(requestRef, out var request)
					? request.ConyardLoc : null;
				foreach (var claim in law.PeekRefineryClaims(near))
					if (claim.Anchor != o1)
						e.Anchors.Add(claim);

				e.AnchorIndex = 0;
				e.Annulus = null;
				units--;
				e.Units++;
				return e.Anchors.Count > 0;
			}

			if (e.AnchorIndex >= e.Anchors.Count)
				return false;

			var anchor = e.Anchors[e.AnchorIndex];
			var tier = e.AnchorIndex;

			// A full K-list whose worst member beats any site this tier can give skips the rest of the pass.
			if (e.Best.Count >= RefinerySiteSearchLogic.K
				&& e.Best[^1].Score.CompareTo(new RefinerySiteScore(0, tier, 0, 0, int.MinValue, int.MinValue)) < 0)
			{
				e.AnchorIndex = e.Anchors.Count;
				return false;
			}

			if (e.Annulus == null)
			{
				e.Annulus = world.Map.FindTilesInAnnulus(anchor.Anchor, 0, law.ExpansionTargetClaimRadius)
					.OrderBy(c => (c - anchor.Anchor).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y).ToList();
				e.AnnulusIndex = 0;
				e.Zone0 = e.Zone1 = null;
				var cost = 1 + e.Annulus.Count / 16;
				if (anchor.ResourceCells != null && anchor.ResourceCells.Count > 0)
				{
					e.Zone0 = Dilate(anchor.ResourceCells);
					e.Zone1 = Dilate(e.Zone0);
					e.Zone1.ExceptWith(e.Zone0);
					cost += anchor.ResourceCells.Count / 16;
				}

				units -= cost;
				e.Units += cost;
				return true;
			}

			if (e.AnnulusIndex >= e.Annulus.Count)
			{
				e.AnchorIndex++;
				e.Annulus = null;
				return true;
			}

			var cell = e.Annulus[e.AnnulusIndex++];
			units--;
			e.Units++;
			var d2 = (long)(cell - anchor.Anchor).LengthSquared;
			if (e.Best.Count >= RefinerySiteSearchLogic.K
				&& e.Best[^1].Score.CompareTo(new RefinerySiteScore(0, tier, 0, d2, int.MinValue, int.MinValue)) < 0)
			{
				// Annulus cells come in d2 order: nothing later on this anchor can enter the K-list.
				e.AnnulusIndex = e.Annulus.Count;
				return true;
			}

			if (anchorHeldBy.TryGetValue(anchor.Anchor, out var holder) && holder != e.Key)
			{
				e.AnnulusIndex = e.Annulus.Count;
				return true;
			}

			if (!LegalCell(e, cell))
			{
				e.LastIllegal[cell] = now;
				return true;
			}

			// REF-DEMAND-01: a cell within the spacing of a friendly refinery is refused; one legal only at the fallback spacing scores after
			// every full-spacing site of the same claim (gap + 2), so the zone retries at the fallback spacing only when the full one gives none.
			var spacingTier = SpacingTier(cell);
			if (spacingTier == RefineryDemand.SpacingRefused)
			{
				e.LastIllegal[cell] = now;
				return true;
			}

			// The law's gap rule (QM LawRefineryPlacement): gap 0 or 1 only; an anchor with no resource cells is
			// the plain nearest-placeable case and scores as gap 0.
			var gap = 0;
			if (e.Zone0 != null)
			{
				gap = BaseBuilderQueueEvalCA.FootprintGap(e.Bi.Tiles(cell).ToList(), e.Zone0, e.Zone1);
				if (gap < 0 || gap > 1)
					return true;
			}

			var relaxed = spacingTier == RefineryDemand.SpacingFallbackOnly;
			Offer(e, cell, new RefinerySiteScore(0, tier, relaxed ? gap + 2 : gap, d2, cell.X, cell.Y), anchor, now, relaxed);
			return true;
		}

		// O-3: the widened scan's own paged (rank, X, Y) pass, one budgeted poll per chunk. Its Exhausted verdict
		// is the certificate the deadline phase may cancel on.
		void StepFallback(IBotExpansionTargetProvider law, RefinerySiteSearchSettings settings, Episode e, int now, int chunk, bool deadline)
		{
			var budget = Math.Min(chunk, settings.OrderBuildUnits);
			var outcome = law.PollRefineryFallbackSite(e.Producer, e.ItemName, true, budget, deadline, out var site);
			if (outcome == RefineryFallbackOutcome.Exhausted)
			{
				e.FallbackExhausted = true;

				// The exhaustive strict certificate: the paged scan walked its whole domain and found no legal cell at all.
				PromoteRelaxed(e, now);
			}

			if (outcome != RefineryFallbackOutcome.Site || site is not { } cell)
				return;

			// The scan hands out a fallback-spacing site only after its strict candidates are drained; it waits in Relaxed like every relaxed site.
			var tier = SpacingTier(cell);
			if (!LegalCell(e, cell) || tier == RefineryDemand.SpacingRefused)
			{
				e.LastIllegal[cell] = now;
				return;
			}

			var relaxedSite = tier == RefineryDemand.SpacingFallbackOnly;
			Offer(e, cell, new RefinerySiteScore(1, 0, law.RefineryResourceGap(e.ItemName, cell), 0, cell.X, cell.Y), null, now, relaxedSite);

			// The scan hands out a fallback-spacing cell only after every strict candidate of its domain is drained: that is the certificate.
			if (relaxedSite)
				PromoteRelaxed(e, now);
		}

		// R: seeded samples — a provider uniformly from the ActorID-ordered list, then a cell uniformly in its
		// reach square. Every draw is counted; the provider list is an S-class snapshot taken per chunk.
		void StepSamples(IBotExpansionTargetProvider law, Episode e, int now, int chunk)
		{
			// The provider snapshot is taken once per tick per (producer, item) and shared by every chunk.
			if (providersTick != now || providersFor != (e.Producer.ActorID, e.ItemName))
			{
				law.RefinerySearchProviders(e.Producer, e.ItemName, providers);
				providersTick = now;
				providersFor = (e.Producer.ActorID, e.ItemName);
			}

			if (providers.Count == 0)
				return;

			// N-1 (587A0D62): one sample per charged unit.
			for (var n = 0; n < chunk; n++)
			{
				var p = providers[rng.Next(providers.Count)];
				var cell = new CPos(p.Center.X + rng.Next(-p.Reach, p.Reach + 1), p.Center.Y + rng.Next(-p.Reach, p.Reach + 1));
				e.Draws += 3;
				if (!LegalCell(e, cell) || SpacingTier(cell) != RefineryDemand.SpacingAllowed)
				{
					e.LastIllegal[cell] = now;
					continue;
				}

				Offer(e, cell, new RefinerySiteScore(1, 0, law.RefineryResourceGap(e.ItemName, cell), 0, cell.X, cell.Y), null, now);
			}
		}

		// Sec 3.4: the mask covers the reserved site (the outstanding order's cell, else Best[0]) — its footprint
		// and dock cells — and its class-0 anchor is held against every other episode.
		void UpdateMask(Episode e)
		{
			var site = e.Outstanding ?? (e.Best.Count > 0 ? e.Best[0] : null);
			if (site?.Cell == e.MaskCell && site != null)
				return;

			foreach (var a in anchorHeldBy.Where(kv => kv.Value == e.Key).Select(kv => kv.Key).ToList())
				anchorHeldBy.Remove(a);

			if (site == null)
			{
				mask.Release(e.Key);
				e.MaskCell = null;
				return;
			}

			var cells = e.Bi.Tiles(site.Cell).ToList();
			cells.AddRange(DockCells(e, site.Cell));

			mask.Set(e.Key, cells);
			e.MaskCell = site.Cell;
			if (site.Claim is { } claim && !anchorHeldBy.ContainsKey(claim.Anchor))
				anchorHeldBy[claim.Anchor] = e.Key;
		}

		void End(Episode e, RefineryEpisodeEnd end)
		{
			var now = world.WorldTick;
			var facts = new RefineryEpisodeFacts
			{
				ReadyTick = e.ReadyPrime,
				EndTick = now,
				End = end,
				EverFound = e.FirstFind != null,
				OrderIssuedAfterVLast = e.OutstandingAfterVLast,
				HeldAtVLast = e.HeldAtVLast,
				Rejections = e.Rejections,
				McvOverlap = e.McvOverlap,
				Via = e.Outstanding != null ? e.OutstandingVia : RefineryPlacedVia.None,
			};

			var cls = RefinerySiteSearchLogic.Classify(facts);
			Dev(e, now, "end", $"end={end} cls={cls} tR={e.ReadyTick?.ToString() ?? "-"} tRp={e.ReadyPrime?.ToString() ?? "-"} "
				+ $"order={(e.Outstanding != null ? e.OrderTick.ToString() : "-")} placed={(end == RefineryEpisodeEnd.Placed ? now.ToString() : "-")} "
				+ $"vlast={e.VLast?.ToString() ?? "-"} vfind={e.Outstanding?.FoundTick.ToString() ?? "-"} first={e.FirstFind?.ToString() ?? "-"} "
				+ $"hold={(e.DemandHold ? 1 : 0)} rej={e.Rejections} units={e.Units} draws={e.Draws}");

			mask.Release(e.Key);
			foreach (var a in anchorHeldBy.Where(kv => kv.Value == e.Key).Select(kv => kv.Key).ToList())
				anchorHeldBy.Remove(a);

			byItem.Remove(e.Item);
			episodes.Remove(e);
			watchStale = true;
		}

		static HashSet<CPos> Dilate(IEnumerable<CPos> cells)
		{
			var result = new HashSet<CPos>(cells);
			foreach (var c in cells)
				for (var dy = -1; dy <= 1; dy++)
					for (var dx = -1; dx <= 1; dx++)
						result.Add(new CPos(c.X + dx, c.Y + dy));

			return result;
		}

		// Sec 8: the per-event DEV record (DEV build only; the official build carries no dev logging).
		void Dev(Episode e, int now, string evt, string fields)
		{
#if CAMEO_DEV_LOGGING
			Log.Write("debug", $"AI ({player.ClientIndex}): REFSCAN ep={e.Key} {evt} item={e.ItemName} producer={e.Producer.ActorID} tick={now} {fields}");
#endif
		}
	}
}
