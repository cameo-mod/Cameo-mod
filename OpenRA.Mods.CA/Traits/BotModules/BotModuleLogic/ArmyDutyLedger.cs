#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P7 (army allocation and engagement), phase (a): the duties a squad-manager unit can be claimed for, in the rank order of design 01 (5e100b94) with the five required changes of the Opus
	/// review (CONSOLIDATE_03_P7_REVIEW_OPUS_01.md). The rank is the target order of phase (b): a higher rank may take units only from a lower one, equal ranks never. Phase (a) only RECORDS (write-only ledger, DEV only):
	/// no decision reads it, so nothing here can change a game.
	/// </summary>
	public enum ArmyDuty
	{
		None = 0,

		/// <summary>R1/R3: the own base is defended (R1 when the core definition <see cref="ArmyDutyLedger.IsCoreUnderAttack"/> holds, R3 otherwise; phase (a) records R3 for every own-base claim).</summary>
		OwnBaseDefend,

		/// <summary>R2: ONE team-defend implementation, an ally's base is attacked (25-cell radius, all allies).</summary>
		AllyDefend,

		/// <summary>R4: units lent out of the home guard (guard-lend); the lend row carries the return tick.</summary>
		GuardLend,

		/// <summary>R5: team order, push or join of the gather squad.</summary>
		TeamPush,

		/// <summary>R5: team order, a pincer arm.</summary>
		PincerArm,

		/// <summary>R5: team order, all-in.</summary>
		AllIn,

		/// <summary>R5 (Opus change 1): the stalemate breaker is an attack order.</summary>
		Stalemate,

		/// <summary>R5: the M13 attack-liveness wave.</summary>
		AttackLiveness,

		/// <summary>R6: raid / harass (the harass cap).</summary>
		Raid,

		/// <summary>R7: idle pool, reserve, preposition.</summary>
		Idle,
	}

	/// <summary>
	/// The write-only unit -> duty ledger (design 01 section 3/4): every claim and release of a unit by a squad-manager duty is written here by a DEV-only hook, with the tick, the duty, the caller method and the reason.
	/// Pure and engine-free (units are actor ids), so it is unit-testable. It also derives the baseline counters of design 01 section 4(a): steals, draft-backs of lent units, double ownership found by the heartbeat.
	/// </summary>
	public sealed class ArmyDutyLedger
	{
		/// <summary>A claim over an entry of a different duty younger than this many ticks counts as a steal (design 01: "within N ticks").</summary>
		public const int StealWindowTicks = 750;

		/// <summary>A unit that a defend duty claims within this many ticks of being lent counts as a draft-back of a lent unit.</summary>
		public const int LendWindowTicks = 3000;

		public enum ClaimKind
		{
			/// <summary>The unit had no entry.</summary>
			New,

			/// <summary>The same duty claimed it again (a refresh).</summary>
			Same,

			/// <summary>A higher rank took it from a lower one: what the target order allows.</summary>
			TakeLower,

			/// <summary>An equal or lower rank (a different duty) took it: forbidden by the target order (a violation).</summary>
			TakeEqualOrHigher,
		}

		public readonly record struct Entry(ArmyDuty Duty, int Since, string Reason, string Caller);

		public readonly record struct ClaimResult(ClaimKind Kind, Entry Previous, bool Steal, bool DraftBack);

		readonly Dictionary<uint, Entry> byUnit = new();
		readonly Dictionary<uint, int> lentAt = new();

		public int Claims { get; private set; }
		public int Releases { get; private set; }
		public int Steals { get; private set; }
		public int Violations { get; private set; }
		public int DraftBacks { get; private set; }
		public int HeartbeatDoubles { get; private set; }
		public int Pruned { get; private set; }

		public int Count => byUnit.Count;

		/// <summary>The rank of a duty in the target order of design 01 with Opus' changes (1 = highest). None = 0 (no claim).</summary>
		public static int Rank(ArmyDuty duty) => duty switch
		{
			ArmyDuty.OwnBaseDefend => 3,
			ArmyDuty.AllyDefend => 2,
			ArmyDuty.GuardLend => 4,
			ArmyDuty.TeamPush => 5,
			ArmyDuty.PincerArm => 5,
			ArmyDuty.AllIn => 5,
			ArmyDuty.Stalemate => 5,
			ArmyDuty.AttackLiveness => 5,
			ArmyDuty.Raid => 6,
			ArmyDuty.Idle => 7,
			_ => 0,
		};

		/// <summary>
		/// R1 "own base falls (core)", written down (Opus note): the yard, a production building or a refinery is under attack by a force at or above the observed threat threshold. Only observed values go in.
		/// Phase (a) does not use it for any decision; phase (b) (sq_rank_enforce) ranks an own-base claim R1 when it holds, R3 otherwise.
		/// </summary>
		public static bool IsCoreUnderAttack(bool yardHit, bool productionHit, bool refineryHit, long observedAttackerValue, long observedThreshold) =>
			(yardHit || productionHit || refineryHit) && observedAttackerValue >= observedThreshold;

		public bool TryGet(uint unit, out Entry entry) => byUnit.TryGetValue(unit, out entry);

		/// <summary>Records the claim of one unit by a duty and returns what it displaced (the previous entry, the kind, whether it counts as a steal or a draft-back).</summary>
		public ClaimResult Claim(uint unit, ArmyDuty duty, int tick, string reason, string caller)
		{
			Claims++;
			var steal = false;
			var draftBack = false;
			var kind = ClaimKind.New;
			Entry previous = default;
			if (byUnit.TryGetValue(unit, out previous) && previous.Duty != ArmyDuty.None)
			{
				if (previous.Duty == duty)
					kind = ClaimKind.Same;
				else
				{
					kind = Rank(duty) < Rank(previous.Duty) ? ClaimKind.TakeLower : ClaimKind.TakeEqualOrHigher;
					if (tick - previous.Since <= StealWindowTicks)
					{
						steal = true;
						Steals++;
					}

					if (kind == ClaimKind.TakeEqualOrHigher)
						Violations++;
				}
			}

			if ((duty == ArmyDuty.OwnBaseDefend || duty == ArmyDuty.AllyDefend) && lentAt.TryGetValue(unit, out var lent))
			{
				if (tick - lent <= LendWindowTicks)
				{
					draftBack = true;
					DraftBacks++;
				}

				lentAt.Remove(unit);
			}

			byUnit[unit] = new Entry(duty, kind == ClaimKind.Same ? previous.Since : tick, reason, caller);
			return new ClaimResult(kind, previous, steal, draftBack);
		}

		/// <summary>Records that a unit left its duty (back to the idle pool, dismissed, dead). Returns false when it had no entry.</summary>
		public bool Release(uint unit, int tick, out Entry previous)
		{
			Releases++;
			return byUnit.Remove(unit, out previous);
		}

		/// <summary>
		/// A unit goes back to the idle pool (R7): its old duty ends and it holds Idle since this tick. This is a hand-back, not a take: it is never a steal or a violation. Returns false when it had no entry.
		/// </summary>
		public bool ToPool(uint unit, int tick, string reason, string caller, out Entry previous)
		{
			Releases++;
			Claims++;
			var had = byUnit.TryGetValue(unit, out previous);
			byUnit[unit] = new Entry(ArmyDuty.Idle, tick, reason, caller);
			return had;
		}

		/// <summary>Drops the entries of units the predicate calls gone (dead, out of the world), so the held count and later claims are not distorted by a stale entry. Returns how many were dropped.</summary>
		public int Prune(Func<uint, bool> gone)
		{
			var dropped = byUnit.Keys.Where(gone).ToList();
			foreach (var id in dropped)
			{
				byUnit.Remove(id);
				lentAt.Remove(id);
			}

			Pruned += dropped.Count;
			return dropped.Count;
		}

		/// <summary>A guard-lend: the unit leaves the home guard for the pool; a later defend claim inside <see cref="LendWindowTicks"/> is a draft-back.</summary>
		public void NoteLend(uint unit, int tick) => lentAt[unit] = tick;

		/// <summary>The heartbeat (ReconcileSquadLeasesCore) found and repaired this many units owned twice.</summary>
		public void NoteHeartbeatDoubles(int count) => HeartbeatDoubles += Math.Max(0, count);

		/// <summary>FNV-1a of the unit ids in the given order, 8 hex digits: an order-sensitive digest of the idle pool (the trace's pool-order hash).</summary>
		public static string PoolHash(IEnumerable<uint> ids)
		{
			var sb = new StringBuilder();
			foreach (var id in ids)
				sb.Append(id.ToString(CultureInfo.InvariantCulture)).Append(',');

			return BotRng.Fnv1a(sb.ToString()).ToString("x8", CultureInfo.InvariantCulture);
		}

		/// <summary>The sorted unit ids as "1|2|3" ('|' separated: the CSV detail column never holds a comma).</summary>
		public static string IdList(IEnumerable<uint> ids) => string.Join("|", ids.OrderBy(i => i).Select(i => i.ToString(CultureInfo.InvariantCulture)));

		/// <summary>The previous duties as "Raid:3|Idle:1" (sorted by duty; "-" when empty): the offline analysis classifies a violation by the duty pair, not by the count.</summary>
		public static string DutyHistogram(IReadOnlyDictionary<ArmyDuty, int> duties) =>
			duties.Count == 0 ? "-" : string.Join("|", duties.OrderBy(kv => kv.Key).Select(kv => kv.Key + ":" + kv.Value.ToString(CultureInfo.InvariantCulture)));

		/// <summary>One trace detail for a claim batch: the duty, its rank, the caller, the reason, the kind histogram, the displaced duties, the sorted ids and the pool-order digest.</summary>
		public static string ClaimDetail(ArmyDuty duty, string caller, string reason, IReadOnlyDictionary<ClaimKind, int> kinds, IReadOnlyDictionary<ArmyDuty, int> displaced, int steals, int draftBacks, IEnumerable<uint> ids, string poolHash) =>
			string.Create(CultureInfo.InvariantCulture,
				$"duty={duty};rank={Rank(duty)};caller={caller};reason={reason};new={kinds.GetValueOrDefault(ClaimKind.New)};same={kinds.GetValueOrDefault(ClaimKind.Same)};take={kinds.GetValueOrDefault(ClaimKind.TakeLower)};viol={kinds.GetValueOrDefault(ClaimKind.TakeEqualOrHigher)};from={DutyHistogram(displaced)};steals={steals};draftbacks={draftBacks};units={IdList(ids)};pool={poolHash}");

		/// <summary>One trace detail for a release batch: the caller, the reason, the previous duties (histogram by duty name), the sorted ids and the pool-order digest.</summary>
		public static string ReleaseDetail(string caller, string reason, IReadOnlyDictionary<ArmyDuty, int> previous, IEnumerable<uint> ids, string poolHash) =>
			string.Create(CultureInfo.InvariantCulture,
				$"caller={caller};reason={reason};prev={DutyHistogram(previous)};units={IdList(ids)};pool={poolHash}");

		/// <summary>The baseline counters of design 01 section 4(a) as one detail (no time-to-first-defender here: that needs the threat start, a (b) input).</summary>
		public string CountersDetail() => string.Create(CultureInfo.InvariantCulture,
			$"claims={Claims};releases={Releases};steals={Steals};violations={Violations};draftbacks={DraftBacks};heartbeat_doubles={HeartbeatDoubles};pruned={Pruned};held={Count}");
	}
}
