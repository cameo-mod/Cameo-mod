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

namespace OpenRA.Mods.Cameo.Traits
{
	// STAGGER_DESIGN_06 section 10: the world-free heavy-work service. Heavy operations run as bounded units under a
	// guaranteed floor plus quota credit, with one input generation per published result and no Mixed publication.
	// Everything is integer state in a fixed job order (creation Seq): deterministic, no clock, no hash order.
	// This file is the BASE layer only (10.7); the adaptive layer is not built (D-5).
	public sealed class BotJobSpec
	{
		public readonly string Kind;

		// q_k: units per world tick of today's quota. 0 for an on-demand op.
		public readonly int Quota;

		// m_k >= 1: the service floor, units per visit while the op has pending work.
		public readonly int Floor;

		// SlotCap_k: the most units one visit may spend on this op (floor plus credit).
		public readonly int SlotCap;

		// DebtCap_k: the most unpaid credit carried to the next visit.
		public readonly int DebtCap;

		// R_max_k: restarts per publication attempt before the job ends Starved.
		public readonly int RMax;

		// StarveBackoff_k: visits a Starved job waits before it is queued again.
		public readonly int StarveBackoff;

		// W_k: the declared work bound of one pass, recorded by admission test A-5.
		public readonly int WorkBound;

		public BotJobSpec(string kind, int quota, int floor, int slotCap, int debtCap, int rMax, int starveBackoff, int workBound)
		{
			Kind = kind;
			Quota = quota;
			Floor = floor;
			SlotCap = slotCap;
			DebtCap = debtCap;
			RMax = rMax;
			StarveBackoff = starveBackoff;
			WorkBound = workBound;
		}
	}

	// One shared creation counter per scheduler, so the fixed job order is the same on every run.
	public sealed class BotJobSeq
	{
		long next;
		public long Next() => next++;
	}

	// A sliced job. The subclass supplies bounded units; this class owns the generation, restart and starvation rules
	// of 10.4 and the debt/drop accounting of 10.2. A published result is the function of ONE input generation.
	public abstract class BotSlicedJob
	{
		public readonly BotJobSpec Spec;
		public long Seq { get; internal set; }

		public bool Running { get; private set; }

		// Restarts within the current publication attempt.
		public int Restarts { get; private set; }
		public int BackoffVisits { get; private set; }

		public int Debt { get; internal set; }
		public long Dropped { get; internal set; }
		public long TotalRestarts { get; private set; }
		public long StarvedCount { get; private set; }
		public long Publishes { get; private set; }
		public long UnitsServed { get; private set; }

		long startGeneration;
		bool rerunPending;
		bool backedOffThisVisit;

		protected BotSlicedJob(BotJobSpec spec)
		{
			Spec = spec;
		}

		// Pending work: a queued request exists, or a pass is running.
		protected abstract bool HasWork { get; }

		// Copy the S (snapshot) inputs and reset the cursor. The job also resets its snapshot-change baseline here.
		protected abstract void BeginPass();

		// Run at most `budget` bounded units; return the units used (never more than the budget).
		protected abstract int StepUnits(int budget);

		protected abstract bool PassComplete { get; }

		// Bounded publish: a double-buffer swap. The previous complete generation stays readable until it runs.
		protected abstract void Publish();

		// The combined monotone generation of the L (live) inputs. O(1), no scan.
		protected virtual long LiveGeneration => 0;

		// True when an S input changed since BeginPass: the published result is stale and a re-run is queued.
		protected virtual bool SnapshotChanged => false;

		public bool Pending => !backedOffThisVisit && BackoffVisits == 0 && (Running || rerunPending || HasWork);

		// Once per visit, before any service: advance the starvation backoff and restart a pass whose L input moved.
		internal void VisitStart()
		{
			backedOffThisVisit = false;
			if (BackoffVisits > 0)
			{
				BackoffVisits--;
				backedOffThisVisit = true;
				return;
			}

			if (Running && LiveGeneration != startGeneration)
				Restart();
		}

		// Serve up to `units` units; returns the units used.
		internal int Serve(int units)
		{
			if (units <= 0 || backedOffThisVisit || BackoffVisits > 0)
				return 0;

			if (!Running)
			{
				if (!rerunPending && !HasWork)
					return 0;

				rerunPending = false;
				BeginPass();
				startGeneration = LiveGeneration;
				Running = true;
			}

			var used = Math.Min(units, Math.Max(0, StepUnits(units)));
			UnitsServed += used;
			if (PassComplete)
			{
				// The generation is re-read immediately before the publish; a moved L input restarts the pass.
				if (LiveGeneration != startGeneration)
					Restart();
				else
				{
					Publish();
					Publishes++;
					Running = false;
					Restarts = 0;
					if (SnapshotChanged)
						rerunPending = true;
				}
			}

			return used;
		}

		void Restart()
		{
			TotalRestarts++;
			if (++Restarts > Spec.RMax)
			{
				// Starved: nothing is published, the previous generation stays readable, the job waits StarveBackoff visits.
				Running = false;
				Restarts = 0;
				StarvedCount++;
				BackoffVisits = Spec.StarveBackoff;
				return;
			}

			BeginPass();
			startGeneration = LiveGeneration;
		}
	}

	// One bot's jobs: its own plus the shared jobs it currently owns (10.3). RunVisit is called once per visit.
	public sealed class BotJobSet
	{
		readonly List<BotSlicedJob> own = [];
		readonly BotJobSeq seq;
		readonly Func<IEnumerable<BotSlicedJob>> shared;
		int cursor;

		public readonly int VisitCap;
		public long CreditStarvedVisits { get; private set; }
		public long Visits { get; private set; }
		public long UnitsSpent { get; private set; }

		public BotJobSet(int visitCap, BotJobSeq seq, Func<IEnumerable<BotSlicedJob>> shared = null)
		{
			VisitCap = visitCap;
			this.seq = seq;
			this.shared = shared;
		}

		public int Cursor => cursor;

		public void Add(BotSlicedJob job)
		{
			job.Seq = seq.Next();
			own.Add(job);
		}

		// 10.2 and 10.5: two passes per visit in the fixed creation order. Pass 1 serves every pending job its floor.
		// Pass 2 shares only the spare capacity among jobs whose credit exceeds their floor, in rotation.
		public int RunVisit(int elapsed)
		{
			Visits++;
			var jobs = (shared == null ? own : own.Concat(shared())).OrderBy(j => j.Seq).ToList();
			foreach (var j in jobs)
				j.VisitStart();

			var count = jobs.Count;
			var want = new int[count];
			var budget = new int[count];
			var spent = new int[count];
			var pendingAtStart = new bool[count];
			var remaining = VisitCap;

			// Pass 1: floors.
			for (var i = 0; i < count; i++)
			{
				var j = jobs[i];
				pendingAtStart[i] = j.Pending;
				want[i] = j.Spec.Quota > 0 ? j.Spec.Quota * Math.Max(0, elapsed) + j.Debt : 0;
				if (!pendingAtStart[i])
					continue;

				budget[i] = Math.Min(Math.Max(want[i], j.Spec.Floor), j.Spec.SlotCap);
				var grant = Math.Min(Math.Min(j.Spec.Floor, budget[i]), remaining);
				var used = j.Serve(grant);
				spent[i] += used;
				remaining -= used;
			}

			// Pass 2: credit, only from spare capacity.
			var spare = remaining;
			var creditAvail = new int[count];
			var runnable = 0;
			for (var i = 0; i < count; i++)
			{
				if (!pendingAtStart[i] || !jobs[i].Pending)
					continue;

				creditAvail[i] = Math.Max(0, budget[i] - spent[i]);
				if (creditAvail[i] > 0)
					runnable++;
			}

			if (spare >= 1)
			{
				var lastServed = -1;
				if (runnable > 0)
				{
					for (var k = 0; k < count && spare > 0; k++)
					{
						var i = (cursor + k) % count;
						if (creditAvail[i] <= 0 || !jobs[i].Pending)
							continue;

						var perJobCap = Math.Max(1, jobs[i].Spec.SlotCap / runnable);
						var grant = Math.Min(Math.Min(creditAvail[i], perJobCap), spare);
						var used = jobs[i].Serve(grant);
						spent[i] += used;
						spare -= used;
						if (used > 0)
							lastServed = i;
					}
				}

				// The cursor moves only on a visit that had spare capacity.
				cursor = count == 0 ? 0 : (lastServed >= 0 ? lastServed + 1 : cursor + 1) % count;
			}
			else if (runnable > 0)
				CreditStarvedVisits++;

			// Debt and drops: only from a credit shortfall of a job that is still pending.
			var total = 0;
			for (var i = 0; i < count; i++)
			{
				var j = jobs[i];
				total += spent[i];
				if (j.Spec.Quota <= 0 || !pendingAtStart[i])
				{
					j.Debt = 0;
					continue;
				}

				if (!j.Pending)
				{
					j.Debt = 0;
					continue;
				}

				var shortfall = Math.Max(0, want[i] - spent[i]);
				j.Debt = Math.Min(shortfall, j.Spec.DebtCap);
				j.Dropped += Math.Max(0, shortfall - j.Spec.DebtCap);
			}

			UnitsSpent += total;
			return total;
		}
	}

	// 10.3: shared jobs (for example a topology keyed by locomotor). The owner is recomputed at every cycle boundary:
	// the first live roster member, in slot order, that subscribes. State is world-level, so ownership transfers
	// intact. Gated and ungated bots never share an instance: the key includes the gate flag.
	public sealed class BotSharedJobRegistry
	{
		sealed class Entry
		{
			public BotSlicedJob Job;
			public string Kind;
			public bool Gated;
			public readonly List<string> Subscribers = [];
			public string Owner;
		}

		readonly List<Entry> entries = [];
		readonly BotJobSeq seq;

		public long DroppedJobs { get; private set; }
		public long OwnerTransfers { get; private set; }

		public BotSharedJobRegistry(BotJobSeq seq)
		{
			this.seq = seq;
		}

		public int Count => entries.Count;

		public BotSlicedJob Subscribe(string kind, bool gated, string seat, Func<BotSlicedJob> create)
		{
			var e = entries.Find(x => x.Kind == kind && x.Gated == gated);
			if (e == null)
			{
				e = new Entry { Job = create(), Kind = kind, Gated = gated };
				e.Job.Seq = seq.Next();
				entries.Add(e);
			}

			if (!e.Subscribers.Contains(seat))
				e.Subscribers.Add(seat);

			return e.Job;
		}

		public void Unsubscribe(string kind, bool gated, string seat)
		{
			var e = entries.Find(x => x.Kind == kind && x.Gated == gated);
			e?.Subscribers.Remove(seat);
		}

		// Called at every cycle boundary with the live roster seats in slot order.
		public void TransferOwners(IReadOnlyList<string> liveRosterInSlotOrder)
		{
			for (var i = entries.Count - 1; i >= 0; i--)
			{
				var e = entries[i];
				var owner = liveRosterInSlotOrder.FirstOrDefault(s => e.Subscribers.Contains(s));
				if (owner == null)
				{
					// No subscriber is left: the job is dropped and counted.
					entries.RemoveAt(i);
					DroppedJobs++;
					continue;
				}

				if (owner != e.Owner)
				{
					if (e.Owner != null)
						OwnerTransfers++;

					e.Owner = owner;
				}
			}
		}

		public string OwnerOf(string kind, bool gated) => entries.Find(x => x.Kind == kind && x.Gated == gated)?.Owner;

		public IEnumerable<BotSlicedJob> OwnedBy(string seat) =>
			entries.Where(e => e.Owner == seat).Select(e => e.Job).ToList();
	}

	// 10.2 (J8) and 10.5 (L-H): the rules-load lint, applied to the specs a bot can own (its own plus every shared job
	// it subscribes to, so a transfer after a re-pack never breaks it).
	public static class BotJobLint
	{
		// Returns null when the rules pass, otherwise the reason the rules set is refused.
		public static string CheckFloors(IEnumerable<BotJobSpec> specs, int visitCap)
		{
			var list = specs.ToList();
			foreach (var s in list)
			{
				if (s.Floor < 1)
					return $"job '{s.Kind}' has a floor of {s.Floor}; every op needs m_k >= 1";

				if (s.Floor > s.SlotCap)
					return $"job '{s.Kind}' has floor {s.Floor} above SlotCap {s.SlotCap}";

				if (s.SlotCap > visitCap)
					return $"job '{s.Kind}' has SlotCap {s.SlotCap} above VisitCap {visitCap}";
			}

			var sum = list.Sum(s => s.Floor);
			return sum > visitCap ? $"the floors sum to {sum}, above VisitCap {visitCap}" : null;
		}

		// L-H, a warning (not a refusal): true when the quota throughput of 10.2 cannot be met at this N.
		public static bool QuotaThroughputWarning(IEnumerable<BotJobSpec> specs, int visitCap, int n) =>
			specs.Sum(s => Math.Max(s.Floor, Math.Min(s.Quota * n, s.SlotCap))) > visitCap;
	}

	// 10.1: admission test A-5, as the rejecting function the J6 test exercises. The profile fields come from the
	// per-op register row (the audit, plus the DEV microbenchmark of the S0 and T6 games).
	public readonly record struct BotUnitProfile(
		int WorstCaseCost, bool CostDependsOnMapOrActors, bool UnboundedNestedCall, bool UnboundedAllocation,
		bool UnboundedSort, bool PublishTouchesEveryCell);

	public static class BotUnitAdmission
	{
		// Returns null when admitted, otherwise the rule that rejects the unit.
		public static string Admit(BotUnitProfile p, int maxUnitCost)
		{
			if (p.CostDependsOnMapOrActors || p.WorstCaseCost > maxUnitCost)
				return "1: the unit's worst-case cost has no constant bound";

			if (p.UnboundedNestedCall)
				return "2: the unit calls an unbounded callee";

			if (p.UnboundedAllocation || p.UnboundedSort)
				return "3: the unit allocates or sorts without a constant bound";

			if (p.PublishTouchesEveryCell)
				return "4: the publish step touches every cell";

			return null;
		}
	}
}
