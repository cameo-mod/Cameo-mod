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
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// REF-2 P1a (retained-refinery fallback, Sol disposition 5FB81294): one hold episode's bounded
	/// widened-site search, run while a completed law-path refinery waits at a queue's head for a
	/// legal cell. The episode owns a lazy frontier over the declared provider-reach snapshot
	/// (frozen at each pass start): per-provider disks in deterministic provider order, cells in
	/// increasing-distance order per provider, deduped across providers. A cell becomes a candidate
	/// only under the SAME legality predicates an ordinary refinery placement uses, and is ranked by
	/// dist^2 to the nearest declared resource cell. No site is ever picked early: the frontier must
	/// drain and the exact global (rank, X, Y) order be established before the pick phase revalidates
	/// and returns the best still-legal cell. A consumed pass waits one epoch, then re-enumerates a
	/// fresh snapshot so occupancy and reach are re-sampled. World-free: providers, legality, rank
	/// and the clock are injected, so the whole state machine is directly testable.
	/// </summary>
	public sealed class RefineryFallbackScan
	{
		/// <summary>
		/// The frozen per-pass inputs. <see cref="Providers"/> are the reach-domain sources in
		/// deterministic order; <see cref="Disk"/> materializes one provider's cells sorted by
		/// (dist^2, X, Y) from its center; <see cref="Legal"/> is the full engine-legality predicate;
		/// <see cref="Rank"/> returns (dist^2 to the nearest declared resource cell, evaluation
		/// count) — the evaluation count is charged to the sweep budget so ranking work is honest.
		/// Rebuilt at each epoch so a re-list re-samples occupancy and reach.
		/// </summary>
		public readonly struct Snapshot
		{
			public readonly IReadOnlyList<(CPos Center, int ReachCells)> Providers;
			public readonly Func<CPos, int, IReadOnlyList<CPos>> Disk;
			public readonly Func<CPos, bool> Legal;
			public readonly Func<CPos, (int DistSq, int Evals)> Rank;

			public Snapshot(IReadOnlyList<(CPos Center, int ReachCells)> providers,
				Func<CPos, int, IReadOnlyList<CPos>> disk,
				Func<CPos, bool> legal,
				Func<CPos, (int DistSq, int Evals)> rank)
			{
				Providers = providers;
				Disk = disk;
				Legal = legal;
				Rank = rank;
			}
		}

		/// <summary>The per-sweep verdict.</summary>
		public enum Verdict
		{
			/// <summary>A still-legal site was picked under the established global order.</summary>
			Site,

			/// <summary>Enumeration or pick revalidation continues — the budget was spent mid-pass.</summary>
			Deferred,

			/// <summary>The pass drained and every candidate failed pick-time revalidation.</summary>
			NoSite,

			/// <summary>The pass enumerated no cells — the declared reach domain was empty.</summary>
			NoUniverse
		}

		enum Phase { WaitForEpoch, Scanning, Picking }

		readonly int relistEpochTicks;
		Snapshot snapshot;
		Phase phase;
		int providerIndex;
		IReadOnlyList<CPos> providerCells;
		int cellIndex;
		int enumeratedThisPass;
		readonly HashSet<CPos> visited = new();
		readonly List<(CPos Cell, int Rank)> candidates = new();
		List<(CPos Cell, int Rank)> ordered;
		int pickIndex;
		int relistAt = int.MinValue;
		Verdict waitingVerdict = Verdict.NoUniverse;

		public RefineryFallbackScan(int relistEpochTicks)
		{
			this.relistEpochTicks = relistEpochTicks;
			phase = Phase.WaitForEpoch;
		}

		/// <summary>Epochs run so far (the initial pass counts as one).</summary>
		public int Epochs { get; private set; }

		/// <summary>
		/// One sweep's bounded work: at most <paramref name="cellBudget"/> cell-units of frontier
		/// enumeration, provider-disk materialization, rank evaluation and pick revalidation.
		/// <paramref name="snapshotAtPassStart"/> supplies the frozen inputs at each pass start and is
		/// only called then — a scan in progress never re-reads its providers, and a waiting epoch
		/// touches nothing.
		/// </summary>
		public Verdict Poll(int cellBudget, int now, Func<Snapshot> snapshotAtPassStart, out CPos? site)
		{
			site = null;
			var work = 0;

			if (phase == Phase.WaitForEpoch)
			{
				if (now < relistAt)
					return waitingVerdict;

				StartPass(snapshotAtPassStart());
			}

			if (phase == Phase.Scanning)
			{
				while (work < cellBudget)
				{
					if (providerCells == null)
					{
						if (providerIndex >= snapshot.Providers.Count)
						{
							// Frontier drained — the ordering guarantee is now established: sort the
							// whole candidate set once by (rank, X, Y). No pick happened before this.
							ordered = candidates
								.OrderBy(c => c.Rank)
								.ThenBy(c => c.Cell.X)
								.ThenBy(c => c.Cell.Y)
								.ToList();
							phase = Phase.Picking;
							break;
						}

						var p = snapshot.Providers[providerIndex++];
						providerCells = snapshot.Disk(p.Center, p.ReachCells);
						cellIndex = 0;

						// Materialization counts against the sweep's cell budget — an oversized
						// disk charges its size and the pass resumes at its first cell next sweep.
						work += providerCells.Count;
						if (providerCells.Count == 0)
							providerCells = null;
					}
					else
					{
						var cell = providerCells[cellIndex++];
						work++;
						enumeratedThisPass++;
						if (cellIndex >= providerCells.Count)
							providerCells = null;

						if (!visited.Add(cell) || !snapshot.Legal(cell))
							continue;

						var rank = snapshot.Rank(cell);
						work += rank.Evals;
						candidates.Add((cell, rank.DistSq));
					}
				}
			}

			if (phase == Phase.Picking)
			{
				while (work < cellBudget && pickIndex < ordered.Count)
				{
					var c = ordered[pickIndex++];
					work++;

					// Pick-time revalidation: occupancy, reach and docks may have changed while the
					// frontier enumerated — the global order holds, only legality is re-checked.
					if (snapshot.Legal(c.Cell))
					{
						site = c.Cell;
						return Verdict.Site;
					}
				}

				if (pickIndex < ordered.Count)
					return Verdict.Deferred;

				// Pass consumed under its snapshot — schedule the bounded re-list epoch and report
				// the verdict honestly: this describes a drained scan, never a live empty-reach proof.
				waitingVerdict = enumeratedThisPass == 0 ? Verdict.NoUniverse : Verdict.NoSite;
				relistAt = now + relistEpochTicks;
				phase = Phase.WaitForEpoch;
				return waitingVerdict;
			}

			return Verdict.Deferred;
		}

		void StartPass(Snapshot s)
		{
			snapshot = s;
			providerIndex = 0;
			providerCells = null;
			cellIndex = 0;
			enumeratedThisPass = 0;
			visited.Clear();
			candidates.Clear();
			ordered = null;
			pickIndex = 0;
			phase = Phase.Scanning;
			Epochs++;
		}
	}
}
