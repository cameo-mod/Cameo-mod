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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// REFSCAN D-5 (design _12 3be85dba + ERRATUM_01 9c33a59b): the rules knobs the site search reads,
	/// published by the refinery-law provider. Default = search off (CONTROL and every provider-less bot).
	/// </summary>
	public readonly struct RefinerySiteSearchSettings
	{
		public readonly bool Enabled;
		public readonly int SearchVisitUnits;
		public readonly int DeadlineVisitUnits;
		public readonly int OrderBuildUnits;
		public readonly int EpisodeBindGraceTicks;
		public readonly int OrderLatencyTicks;

		public RefinerySiteSearchSettings(bool enabled, int searchVisitUnits, int deadlineVisitUnits, int orderBuildUnits,
			int episodeBindGraceTicks, int orderLatencyTicks)
		{
			Enabled = enabled;
			SearchVisitUnits = searchVisitUnits;
			DeadlineVisitUnits = deadlineVisitUnits;
			OrderBuildUnits = orderBuildUnits;
			EpisodeBindGraceTicks = episodeBindGraceTicks;
			OrderLatencyTicks = orderLatencyTicks;
		}
	}

	/// <summary>
	/// REFSCAN sec 3.2: the score S(c) = (class, tier, gap, d2, X, Y), lower is better, a total order.
	/// Class 0 serves an offered law anchor (tier = the anchor's position in the O-1 + Peek order, gap 0 or 1,
	/// d2 = squared distance to the anchor); class 1 is a widened site (tier 0, gap = the resource gap rank, d2 0).
	/// </summary>
	public readonly struct RefinerySiteScore : IComparable<RefinerySiteScore>, IEquatable<RefinerySiteScore>
	{
		public readonly int Class;
		public readonly int Tier;
		public readonly int Gap;
		public readonly long D2;
		public readonly int X;
		public readonly int Y;

		public RefinerySiteScore(int cls, int tier, int gap, long d2, int x, int y)
		{
			Class = cls;
			Tier = tier;
			Gap = gap;
			D2 = d2;
			X = x;
			Y = y;
		}

		public int CompareTo(RefinerySiteScore o)
		{
			var c = Class.CompareTo(o.Class);
			if (c != 0)
				return c;

			c = Tier.CompareTo(o.Tier);
			if (c != 0)
				return c;

			c = Gap.CompareTo(o.Gap);
			if (c != 0)
				return c;

			c = D2.CompareTo(o.D2);
			if (c != 0)
				return c;

			c = X.CompareTo(o.X);
			return c != 0 ? c : Y.CompareTo(o.Y);
		}

		public bool Equals(RefinerySiteScore o) => CompareTo(o) == 0;
		public override bool Equals(object obj) => obj is RefinerySiteScore o && Equals(o);
		public override int GetHashCode() => HashCode.Combine(Class, Tier, Gap, D2, X, Y);
		public override string ToString() => $"({Class},{Tier},{Gap},{D2},{X},{Y})";
	}

	/// <summary>How an episode ended (sec 2.1). <see cref="Open"/> = not ended.</summary>
	public enum RefineryEpisodeEnd { Open, Placed, Cancelled, CancelledNoSite, ProducerLost, LawOff, GameEnd }

	/// <summary>Which member the placing order used (sec 4.2 / 4.3), for the class.</summary>
	public enum RefineryPlacedVia { None, ReservedHit, NextBest, DeadlinePhase }

	/// <summary>The per-event facts the sec 8 class is decided from.</summary>
	public struct RefineryEpisodeFacts
	{
		/// <summary>t_R' (hold-adjusted READY tick), null while not READY.</summary>
		public int? ReadyTick;
		public int EndTick;
		public RefineryEpisodeEnd End;

		/// <summary>A legal site was ever found (first_find exists).</summary>
		public bool EverFound;

		/// <summary>The placing (last) order was issued at a visit after v_last.</summary>
		public bool OrderIssuedAfterVLast;

		/// <summary>At v_last the episode held a reserved legal site: an order outstanding (E-2).</summary>
		public bool HeldAtVLast;
		public int Rejections;
		public bool McvOverlap;
		public RefineryPlacedVia Via;
	}

	/// <summary>
	/// REFSCAN D-5: the pure, World-free half of the site search — constants, the ordering-visit test,
	/// the sec 4.4 class precedence (with ruling C3 / D1), lints L-R and L-C, the two-verdict gate and the
	/// K-best list. Tests drive every branch directly.
	/// </summary>
	public static class RefinerySiteSearchLogic
	{
		/// <summary>K, the size of the best-so-far list (sec 3.4).</summary>
		public const int K = 4;

		/// <summary>D, the READY-to-placed bound in ticks (U-1).</summary>
		public const int DeadlineTicks = 75;

		/// <summary>The STAGGER_06 bounds lint L-R is evaluated over (sec 4.4): N = 1..12, and a re-pack V = 23.</summary>
		public const int LintMaxBots = 12;
		public const int LintRepackV = 23;

		/// <summary>
		/// Sec 4.3: an ordering visit is one whose order can still land by the deadline,
		/// <c>v + L_o* &lt;= t_R' + 75</c>. v_last is the last visit for which this holds, taken from the actual
		/// visit schedule (the caller evaluates it at each real visit, never from a fixed V).
		/// </summary>
		public static bool IsOrderingVisit(int visit, int readyTick, int latency) =>
			visit + latency <= readyTick + DeadlineTicks;

		/// <summary>
		/// Sec 4.4 precedence, first match wins: censored, SITELESS, UNPLACED, late-first, late-site,
		/// mcv-overlap, rejected-retry, deadline-phase, next-best, reserved-hit. Ruling C3 / D1
		/// (3cd19a63): an episode that ends before t_R' is PRE_READY_END (reported, not gated) and is
		/// evaluated first, unless the no-site cancel rule ended it (then SITELESS). DemandHold is a flag
		/// the caller reports beside the class, never a class.
		/// </summary>
		public static string Classify(in RefineryEpisodeFacts f)
		{
			if (f.End == RefineryEpisodeEnd.Open)
				return "open";

			if (f.ReadyTick == null || f.EndTick < f.ReadyTick.Value)
				return f.End == RefineryEpisodeEnd.CancelledNoSite ? "SITELESS" : "PRE_READY_END";

			if (f.End == RefineryEpisodeEnd.GameEnd && f.EndTick < f.ReadyTick.Value + DeadlineTicks)
				return "censored";

			if (f.End != RefineryEpisodeEnd.Placed)
				return f.EverFound ? "UNPLACED" : "SITELESS";

			// E-2 / E-4: the class depends only on the placing order and v_last — late-site only when a
			// reserved site was held at v_last (an order outstanding) and its rejection was seen after it.
			if (f.OrderIssuedAfterVLast)
				return f.HeldAtVLast ? "late-site" : "late-first";

			if (f.McvOverlap)
				return "mcv-overlap";

			if (f.Rejections > 0)
				return "rejected-retry";

			return f.Via switch
			{
				RefineryPlacedVia.DeadlinePhase => "deadline-phase",
				RefineryPlacedVia.NextBest => "next-best",
				_ => "reserved-hit",
			};
		}

		/// <summary>True for the classes the sec 8 gate fails on TREATMENT (the latency maximum is checked separately).</summary>
		public static bool GateFailureClass(string cls) =>
			cls == "SITELESS" || cls == "UNPLACED" || cls == "late-first";

		/// <summary>
		/// Lint L-R (sec 4.4): <c>2 * (V + L_o*) &lt;= 75</c> for V = N, N = 1..12, and for a re-pack
		/// V = 23. Returns every failing case by name; empty = pass.
		/// </summary>
		public static List<string> LatencyLintFailures(int latency)
		{
			var failed = new List<string>();
			for (var n = 1; n <= LintMaxBots; n++)
				if (2 * (n + latency) > DeadlineTicks)
					failed.Add("N=" + n);

			if (2 * (LintRepackV + latency) > DeadlineTicks)
				failed.Add("re-pack V=" + LintRepackV);

			return failed;
		}

		/// <summary>Lint L-C (sec 3.6): the search may not run with the two-verdict cancel off.</summary>
		public static bool CancelKnobLintFails(bool searchEnabled, bool requireTwoVerdicts) =>
			searchEnabled && !requireTwoVerdicts;

		/// <summary>
		/// Sec 3.6 (F1): the cancel needs two Exhausted verdicts when the new knob is on OR the force is on —
		/// CONTROL (force 1500, knob false) and TREATMENT (force 0, knob true) both keep the two-verdict rule.
		/// </summary>
		public static bool CancelRequiresTwoVerdicts(bool knob, int forceAfterTicks) =>
			knob || forceAfterTicks > 0;

		/// <summary>
		/// Sec 3.4: insert a candidate into the K-best list (ascending score, unique cells). Returns the
		/// index it took, or -1 when it was a duplicate cell or did not make the top K.
		/// </summary>
		public static int InsertBest<T>(List<T> best, T candidate, Func<T, RefinerySiteScore> score, Func<T, CPos> cell)
		{
			var s = score(candidate);
			var c = cell(candidate);
			for (var i = 0; i < best.Count; i++)
			{
				if (cell(best[i]) != c)
					continue;

				// The same cell scored again: keep the better score only.
				if (score(best[i]).CompareTo(s) <= 0)
					return -1;

				best.RemoveAt(i);
				break;
			}

			var at = best.Count;
			for (var i = 0; i < best.Count; i++)
			{
				if (s.CompareTo(score(best[i])) < 0)
				{
					at = i;
					break;
				}
			}

			if (at >= K)
				return -1;

			best.Insert(at, candidate);
			if (best.Count > K)
				best.RemoveAt(best.Count - 1);

			return at;
		}
	}

	/// <summary>
	/// REFSCAN sec 3.4 (F2, F3): the per-cell owner map over each episode's reserved site (Best[0]'s
	/// footprint plus its dock cells). <see cref="Blocks"/> is true iff a footprint cell maps to an
	/// episode other than the caller; the owning episode is exempt from its own mask, and a caller that
	/// is not an episode passes <see cref="None"/>, so every reserved cell blocks it.
	/// </summary>
	public sealed class RefineryReservationMask
	{
		public const long None = -1;

		readonly Dictionary<CPos, long> owner = new();
		readonly Dictionary<long, List<CPos>> cellsOf = new();

		public int Count => owner.Count;

		public bool IsEmpty => owner.Count == 0;

		public bool TryOwner(CPos cell, out long episode) => owner.TryGetValue(cell, out episode);

		/// <summary>Replace the episode's reserved cells. A cell another episode already holds stays with it.</summary>
		public void Set(long episode, IEnumerable<CPos> cells)
		{
			Release(episode);
			var held = new List<CPos>();
			foreach (var c in cells)
			{
				if (owner.ContainsKey(c))
					continue;

				owner[c] = episode;
				held.Add(c);
			}

			cellsOf[episode] = held;
		}

		/// <summary>Release every cell the episode holds (same tick as the episode end, sec 2.1).</summary>
		public void Release(long episode)
		{
			if (!cellsOf.TryGetValue(episode, out var held))
				return;

			foreach (var c in held)
				if (owner.TryGetValue(c, out var o) && o == episode)
					owner.Remove(c);

			cellsOf.Remove(episode);
		}

		public bool Blocks(IEnumerable<CPos> footprint, long episode)
		{
			if (owner.Count == 0)
				return false;

			foreach (var c in footprint)
				if (owner.TryGetValue(c, out var o) && o != episode)
					return true;

			return false;
		}
	}
}
