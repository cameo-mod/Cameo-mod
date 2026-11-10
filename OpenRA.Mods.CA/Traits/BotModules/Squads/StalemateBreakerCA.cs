#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

// BOTPASSIVE-FIX01 IMPL01: the pure admission core of the stalemate breaker (design _06 §1, _07 M1/M2/L1,
// _08 §1/§4/§5/N1/N3, _09 M1/M2/LD; Sonnet D09 L2/L5). No world access, no orders, no RNG.
// The world-facing wiring lives in SquadManagerBotModuleCA.Stalemate.cs.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>One drafted (eligible and transferable) own ground unit, captured at the attempt tick.</summary>
	public readonly record struct StalemateDraftUnitCA(uint ActorId, string Locomotor, string ActorType, int X, int Y, long Value);

	/// <summary>One legally known enemy CY or production building.</summary>
	public readonly record struct StalemateCandidateCA(uint ActorId, int OwnerIndex, bool Visible, int X, int Y);

	/// <summary>A per-candidate route answer for one group: Found = a non-empty (or zero-length) path; L after the EvalPath trim.</summary>
	public readonly record struct StalemateRouteCA(bool Found, int L, int MinSpeed);

	/// <summary>A locomotor group (design _06 §1.1 with _09 M2): integer centroid, representative and drafted members.</summary>
	public sealed class StalemateGroupCA
	{
		public string Locomotor;
		public long TotalValue;
		public int CenX, CenY;
		public uint RepId;
		public int RepX, RepY;
		public List<StalemateDraftUnitCA> Drafted = new();
		public int Stragglers;

		/// <summary>Lowest-ActorID drafted member of each distinct actor type (design _08 §5 B5), in type-ordinal order.</summary>
		public List<uint> TypeRepresentatives() => Drafted.GroupBy(m => m.ActorType, StringComparer.Ordinal)
			.OrderBy(g => g.Key, StringComparer.Ordinal).Select(g => g.Min(m => m.ActorId)).ToList();
	}

	/// <summary>The route facts the admission needs, answered by the engine in the game and by a fake in tests.</summary>
	public interface IStalemateRouteOracleCA
	{
		/// <summary>tc_g = Mobile.NearestCell(raw, legal, 1, 10) for the group's representative; null when no legal cell (target_unenterable).</summary>
		(int X, int Y)? ResolveTarget(StalemateGroupCA group, int rawX, int rawY);

		/// <summary>PathExistsForLocomotor(member locomotor, member cell, tc) - the terrain-only domain check.</summary>
		bool Connected(StalemateDraftUnitCA member, (int X, int Y) tc);

		/// <summary>One engine path from the representative to tc (the N2 call), trimmed, with the minimum speed over its cells
		/// for every type representative. Counts as one path.</summary>
		StalemateRouteCA Route(StalemateGroupCA group, (int X, int Y) tc, IReadOnlyList<uint> typeRepresentatives);
	}

	public sealed class StalemateGroupEvalCA
	{
		public StalemateGroupCA Group;
		public (int X, int Y) Tc;
		public int L;
		public int S;
		public List<StalemateDraftUnitCA> Reachable = new();
	}

	public sealed class StalemateCandidateEvalCA
	{
		public StalemateCandidateCA Candidate;

		/// <summary>"admit" or a per-candidate failure: unreachable, target_unenterable, route_too_long, speed_unknown, e_over_cap.</summary>
		public string Result;
		public List<StalemateGroupEvalCA> Groups = new();
		public StalemateBreakerPolicyCA.TimingCA Timing;
		public int ReachableCount;
		public long ReachableValue;
		public int UnreachableUnits;
		public uint LeaderId;
		public int LeaderL;
	}

	public sealed class StalemateAttemptCA
	{
		public StalemateCandidateEvalCA Admitted;
		public List<StalemateCandidateEvalCA> Failures = new();
		public int Paths;
		public int ReachChecks;
		public int CandidatesTried;
	}

	public static class StalemateBreakerCA
	{
		public const int MaxGroups = 3;
		public const int MaxDraft = 256;
		public const int StragglerCells = 32;
		public const int MaxTypesPerGroup = 16;
		public const int CandidatesPerAttempt = 4;
		public const int CandidatesPerOwner = 64;
		public const int PathsPerAttempt = 12;
		public const int WorldPathsPerTick = 16;
		public const int RetargetPaths = 3;
		public const int MaxRouteCells = 2048;
		public const int ExclusionTicks = 3000;
		public const int ExclusionMax = 448;
		public const int AttemptInterval = 150;
		public const int NoCandidateCooldown = 750;
		public const int WaveCooldown = 5000;
		public const int SeenFreshTicks = 750;
		public const int RallyRadiusCells = 5;
		public const int ArrivalRadiusCells = 8;
		public const int RallyReissueInterval = 150;
		public const int RallyReissueMinCells = 4;
		public const int DefenceRadiusCells = 20;
		public const int WaveSampleRecordInterval = 750;
		public const int HealthInterval = 250;

		/// <summary>floor(sum / n) for any sign.</summary>
		public static int FloorDiv(long sum, int n)
		{
			if (n <= 0)
				throw new ArgumentOutOfRangeException(nameof(n));
			var q = sum / n;
			if (sum % n != 0 && sum < 0)
				q--;
			return (int)q;
		}

		static long D2(int ax, int ay, int bx, int by)
		{
			long dx = ax - bx, dy = ay - by;
			return dx * dx + dy * dy;
		}

		/// <summary>
		/// Design _06 §1.1 + _09 M2 + _08 §5: group by locomotor, rank by total value (desc) then locomotor name (ordinal), keep K = 3.
		/// The centroid is taken once over every member of the group; rep = minimum squared distance to it, ties to the lowest ActorID;
		/// members farther than 32 cells from the centroid are stragglers (excluded, still in N0/V0). A group whose drafted set is empty
		/// (Sonnet D09 L2: bimodal) or holds more than 16 distinct types is dropped. Returns the kept groups in rank order.
		/// </summary>
		public static List<StalemateGroupCA> BuildGroups(IEnumerable<StalemateDraftUnitCA> draft,
			out int unitsExcludedGroups, out int stragglers, out int tooManyTypesGroups)
		{
			unitsExcludedGroups = 0;
			stragglers = 0;
			tooManyTypesGroups = 0;
			var byLoco = draft.OrderBy(u => u.ActorId)
				.GroupBy(u => u.Locomotor ?? "", StringComparer.Ordinal)
				.Select(g => (Locomotor: g.Key, Members: g.ToList(), Value: g.Sum(m => Math.Max(0, m.Value))))
				.OrderByDescending(g => g.Value).ThenBy(g => g.Locomotor, StringComparer.Ordinal)
				.ToList();

			var kept = new List<StalemateGroupCA>();
			for (var i = 0; i < byLoco.Count; i++)
			{
				var (loco, members, value) = byLoco[i];
				if (i >= MaxGroups)
				{
					unitsExcludedGroups += members.Count;
					continue;
				}

				var cenX = FloorDiv(members.Sum(m => (long)m.X), members.Count);
				var cenY = FloorDiv(members.Sum(m => (long)m.Y), members.Count);
				var rep = members.OrderBy(m => D2(m.X, m.Y, cenX, cenY)).ThenBy(m => m.ActorId).First();
				var limit = (long)StragglerCells * StragglerCells;
				var group = new StalemateGroupCA
				{
					Locomotor = loco, TotalValue = value, CenX = cenX, CenY = cenY,
					RepId = rep.ActorId, RepX = rep.X, RepY = rep.Y
				};

				foreach (var m in members)
				{
					if (D2(m.X, m.Y, cenX, cenY) > limit)
						group.Stragglers++;
					else
						group.Drafted.Add(m);
				}

				stragglers += group.Stragglers;
				if (group.Drafted.Count == 0)
					continue;

				if (group.Drafted.Select(m => m.ActorType).Distinct(StringComparer.Ordinal).Count() > MaxTypesPerGroup)
				{
					tooManyTypesGroups++;
					unitsExcludedGroups += group.Drafted.Count;
					continue;
				}

				kept.Add(group);
			}

			return kept;
		}

		/// <summary>Design _08 §4: visible before remembered, squared distance from the start cell, owner index, ActorID;
		/// at most 64 per owner (_06 §1.4).</summary>
		public static List<StalemateCandidateCA> OrderCandidates(IEnumerable<StalemateCandidateCA> candidates, (int X, int Y) start)
		{
			var ordered = candidates
				.GroupBy(c => c.ActorId).Select(g => g.OrderByDescending(c => c.Visible).First())
				.OrderBy(c => c.Visible ? 0 : 1)
				.ThenBy(c => D2(c.X, c.Y, start.X, start.Y))
				.ThenBy(c => c.OwnerIndex)
				.ThenBy(c => c.ActorId)
				.ToList();

			var perOwner = new Dictionary<int, int>();
			var result = new List<StalemateCandidateCA>();
			foreach (var c in ordered)
			{
				perOwner.TryGetValue(c.OwnerIndex, out var n);
				if (n >= CandidatesPerOwner)
					continue;
				perOwner[c.OwnerIndex] = n + 1;
				result.Add(c);
			}

			return result;
		}

		/// <summary>
		/// Design _09 M1: drop excluded candidates, then every candidate whose owner fails (clock OR force(owner)), BEFORE the 4-cut.
		/// forceLowOwners receives each failing owner once, in first-seen order.
		/// </summary>
		public static List<StalemateCandidateCA> FilterByTrigger(IEnumerable<StalemateCandidateCA> ordered,
			Func<uint, bool> excluded, bool clock, Func<int, bool> force, List<int> forceLowOwners)
		{
			var result = new List<StalemateCandidateCA>();
			var verdict = new Dictionary<int, bool>();
			foreach (var c in ordered)
			{
				if (excluded(c.ActorId))
					continue;

				if (!clock)
				{
					if (!verdict.TryGetValue(c.OwnerIndex, out var ok))
					{
						ok = force(c.OwnerIndex);
						verdict[c.OwnerIndex] = ok;
						if (!ok)
							forceLowOwners.Add(c.OwnerIndex);
					}

					if (!ok)
						continue;
				}

				result.Add(c);
			}

			return result;
		}

		/// <summary>Design _09 LD: only these failures enter the 3000-tick exclusion set.</summary>
		public static bool EntersExclusion(string reason) =>
			reason == "unreachable" || reason == "target_unenterable" || reason == "route_too_long";

		/// <summary>
		/// One candidate (design _08 §1, §5, _07 M1): resolve tc_g per group, connectivity per member, one path per enterable group.
		/// </summary>
		public static StalemateCandidateEvalCA EvaluateCandidate(StalemateCandidateCA candidate, IReadOnlyList<StalemateGroupCA> groups,
			int n0, long v0, IStalemateRouteOracleCA oracle, ref int paths, ref int reachChecks)
		{
			var eval = new StalemateCandidateEvalCA { Candidate = candidate };
			var enterable = 0;
			var routeTooLong = false;
			var speedUnknown = false;
			foreach (var g in groups)
			{
				var tc = oracle.ResolveTarget(g, candidate.X, candidate.Y);
				if (tc == null)
				{
					eval.UnreachableUnits += g.Drafted.Count;
					continue;
				}

				enterable++;
				var ge = new StalemateGroupEvalCA { Group = g, Tc = tc.Value };
				paths++;
				var route = oracle.Route(g, tc.Value, g.TypeRepresentatives());
				if (!route.Found)
				{
					eval.UnreachableUnits += g.Drafted.Count;
					continue;
				}

				if (route.L > MaxRouteCells)
				{
					routeTooLong = true;
					continue;
				}

				if (route.MinSpeed <= 0)
				{
					speedUnknown = true;
					continue;
				}

				ge.L = route.L;
				ge.S = route.MinSpeed;
				foreach (var m in g.Drafted)
				{
					reachChecks++;
					if (m.ActorId == g.RepId || oracle.Connected(m, tc.Value))
						ge.Reachable.Add(m);
					else
						eval.UnreachableUnits++;
				}

				if (ge.Reachable.Count > 0)
					eval.Groups.Add(ge);
			}

			if (enterable == 0)
				return Fail(eval, "target_unenterable");
			if (routeTooLong)
				return Fail(eval, "route_too_long");
			if (speedUnknown)
				return Fail(eval, "speed_unknown");
			if (eval.Groups.Count == 0)
				return Fail(eval, "unreachable");

			if (!StalemateBreakerPolicyCA.TryTiming(eval.Groups.Select(g => new StalemateBreakerPolicyCA.RouteCA(g.L, g.S)), out var timing))
				return Fail(eval, "e_over_cap");

			eval.Timing = timing;
			eval.ReachableCount = eval.Groups.Sum(g => g.Reachable.Count);
			eval.ReachableValue = eval.Groups.Sum(g => g.Reachable.Sum(m => Math.Max(0, m.Value)));
			if (!StalemateBreakerPolicyCA.Admit(true, true, false, n0, eval.ReachableCount, v0, eval.ReachableValue))
				return Fail(eval, "unreachable");

			eval.LeaderId = eval.Groups[0].Group.RepId;
			eval.LeaderL = eval.Groups[0].L;
			eval.Result = "admit";
			return eval;
		}

		static StalemateCandidateEvalCA Fail(StalemateCandidateEvalCA eval, string reason)
		{
			eval.Result = reason;
			eval.Groups.Clear();
			return eval;
		}

		/// <summary>One admission attempt over the filtered candidate list: the first 4 are tried, the first admit wins.</summary>
		public static StalemateAttemptCA RunAttempt(IReadOnlyList<StalemateCandidateCA> admissible, IReadOnlyList<StalemateGroupCA> groups,
			int n0, long v0, IStalemateRouteOracleCA oracle, int candidateLimit = CandidatesPerAttempt)
		{
			var attempt = new StalemateAttemptCA();
			foreach (var c in admissible.Take(candidateLimit))
			{
				attempt.CandidatesTried++;
				var paths = attempt.Paths;
				var checks = attempt.ReachChecks;
				var eval = EvaluateCandidate(c, groups, n0, v0, oracle, ref paths, ref checks);
				attempt.Paths = paths;
				attempt.ReachChecks = checks;
				if (eval.Result == "admit")
				{
					attempt.Admitted = eval;
					break;
				}

				attempt.Failures.Add(eval);
			}

			return attempt;
		}

		/// <summary>Design _08 N3: the next-ranked group with a survivor leads (its representative if alive, else its lowest-ActorID survivor).</summary>
		public static uint NextLeader(IReadOnlyList<(uint RepId, IReadOnlyList<uint> Members)> groupsInRank, Func<uint, bool> alive)
		{
			foreach (var g in groupsInRank)
			{
				if (alive(g.RepId))
					return g.RepId;
				foreach (var id in g.Members.OrderBy(i => i))
					if (alive(id))
						return id;
			}

			return 0;
		}

		public static string Cell((int X, int Y) c) =>
			c.X.ToString(CultureInfo.InvariantCulture) + "," + c.Y.ToString(CultureInfo.InvariantCulture);

		public static (int X, int Y) ParseCell(string s)
		{
			var p = (s ?? "").Split(',');
			if (p.Length != 2)
				throw new InvalidDataException("Malformed cell.");
			return (int.Parse(p[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
				int.Parse(p[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
		}
	}

	/// <summary>Seat-local candidate exclusion set (design _06 §1.4, _09 LD): actor id -> expiry tick, at most 448 entries, persisted when ON.</summary>
	public sealed class StalemateExclusionSetCA
	{
		readonly SortedDictionary<uint, int> expiry = new();

		public int Count => expiry.Count;

		public bool IsExcluded(uint id, int now) => expiry.TryGetValue(id, out var until) && now < until;

		public void Prune(int now)
		{
			foreach (var id in expiry.Where(kv => now >= kv.Value).Select(kv => kv.Key).ToList())
				expiry.Remove(id);
		}

		/// <summary>Adds only the three LD reasons. At the cap the entry expiring soonest (then lowest id) is evicted.</summary>
		public bool Add(uint id, int now, string reason)
		{
			if (!StalemateBreakerCA.EntersExclusion(reason))
				return false;
			Prune(now);
			if (!expiry.ContainsKey(id) && expiry.Count >= StalemateBreakerCA.ExclusionMax)
			{
				var evict = expiry.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
				expiry.Remove(evict);
			}

			expiry[id] = checked(now + StalemateBreakerCA.ExclusionTicks);
			return true;
		}

		public List<MiniYamlNode> Save() => expiry.Select(kv => new MiniYamlNode(
			FieldSaver.FormatValue(kv.Key), FieldSaver.FormatValue(kv.Value))).ToList();

		public static StalemateExclusionSetCA Load(MiniYaml data)
		{
			var set = new StalemateExclusionSetCA();
			if (data == null)
				return set;
			foreach (var n in data.Nodes)
				set.expiry[FieldLoader.GetValue<uint>("ExclusionId", n.Key)] = FieldLoader.GetValue<int>("ExclusionExpiry", n.Value.Value);
			if (set.expiry.Count > StalemateBreakerCA.ExclusionMax)
				throw new InvalidDataException("Exclusion set over its cap.");
			return set;
		}
	}

	/// <summary>
	/// BOTHEALTH H12 (Opus ruling, telemetry only): a squad in attack/move state is stuck when its leader cell moved &lt; 2 cells
	/// versus its leader cell 6 health samples (1500 ticks) earlier. One ring of the last 6 sampled cells per squad; a squad
	/// counts only with 6 samples of history; the ring is dropped when the squad is gone or leaves attack/move.
	/// </summary>
	public sealed class StalemateStuckTrackerCA<TKey>
	{
		public const int HistorySamples = 6;
		readonly Dictionary<TKey, Queue<(int X, int Y)>> rings = new();

		public int Tracked => rings.Count;

		public int Sample(IEnumerable<(TKey Key, int X, int Y)> attackMoveSquads)
		{
			var stuck = 0;
			var present = new HashSet<TKey>();
			foreach (var (key, x, y) in attackMoveSquads)
			{
				if (!present.Add(key))
					continue;
				if (!rings.TryGetValue(key, out var ring))
					rings[key] = ring = new Queue<(int X, int Y)>();
				if (ring.Count == HistorySamples)
				{
					var (ox, oy) = ring.Dequeue();
					long dx = x - ox, dy = y - oy;
					if (dx * dx + dy * dy < 4)
						stuck++;
				}

				ring.Enqueue((x, y));
			}

			foreach (var gone in rings.Keys.Where(k => !present.Contains(k)).ToList())
				rings.Remove(gone);
			return stuck;
		}
	}

	/// <summary>
	/// Design _06 §1.3 + _08 N1: the world admission rotation. P_world = 16 and an attempt reserves 12, so at most one admission runs per tick.
	/// A seat registers as pending at tick s; from tick s + 1 on, the winner of tick t is the pending seat with the lowest rank
	/// (index - t mod n) mod n among seats pending before t. The election is cached per tick, so seat tick order cannot change it,
	/// and each seat runs within n ticks of becoming eligible (deferral at most n - 1 ticks).
	/// </summary>
	public sealed class StalemateRotationCA
	{
		readonly SortedDictionary<int, int> pendingSince = new();
		int electionTick = int.MinValue;
		int electionWinner = -1;

		public bool IsPending(int seat) => pendingSince.ContainsKey(seat);

		public int PendingSince(int seat) => pendingSince.TryGetValue(seat, out var s) ? s : -1;

		public void MarkPending(int seat, int tick)
		{
			if (!pendingSince.ContainsKey(seat))
				pendingSince[seat] = tick;
		}

		public void Clear(int seat) => pendingSince.Remove(seat);

		public static int Rank(int seat, int tick, int seatCount) =>
			(int)((((long)seat - ((long)tick % seatCount)) % seatCount + seatCount) % seatCount);

		public bool IsWinner(int seat, int tick, int seatCount)
		{
			if (seatCount <= 0)
				return false;
			if (electionTick != tick)
			{
				electionTick = tick;
				electionWinner = -1;
				var best = int.MaxValue;
				foreach (var (s, since) in pendingSince)
				{
					if (since >= tick)
						continue;
					var r = Rank(s, tick, seatCount);
					if (r < best)
					{
						best = r;
						electionWinner = s;
					}
				}
			}

			return electionWinner == seat;
		}
	}
}
