// IMPL02-AGG-TEAM: pure, deterministic predicates for A4 (join an attack intent) and A5 (home reserve, max-strength push).
// No world access; the squad manager and the rendezvous state feed these. Used only while AggTeamEnabled is true.
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public static class AggTeamPolicyCA
	{
		public const int CellUnits = 1024;

		/// <summary>One intent with its publisher key (the participant key for an ally, the own key for an own intent).</summary>
		public readonly record struct Candidate(string Key, TeamAttackIntent Intent);

		/// <summary>The point <paramref name="backCells"/> cells back from the target along the publisher's approach (clamped to the publisher).</summary>
		public static WPos RendezvousPoint(WPos target, WPos publisherPos, int backCells)
		{
			var offset = publisherPos - target;
			var distance = offset.HorizontalLength;
			var back = (long)Math.Max(0, backCells) * CellUnits;
			if (distance <= 0 || distance <= back)
				return publisherPos;
			return target + new WVec((int)(offset.X * back / distance), (int)(offset.Y * back / distance), 0);
		}

		/// <summary>Ticks to cover the straight-line distance, never negative.</summary>
		public static int TravelTicks(WPos from, WPos to, int ticksPerCell)
		{
			var cells = (long)(to - from).HorizontalLength / CellUnits;
			return (int)Math.Min(int.MaxValue / 2, cells * Math.Max(0, ticksPerCell));
		}

		/// <summary>Build the intent a publisher squad at <paramref name="squadPos"/> attacking <paramref name="target"/> would publish.</summary>
		public static TeamAttackIntent BuildIntent(WPos squadPos, WPos target, int now, long value, uint targetActorId, int backCells, int ticksPerCell)
		{
			var rendezvous = RendezvousPoint(target, squadPos, backCells);
			var etaTick = now + TravelTicks(squadPos, target, ticksPerCell);
			var rendezvousTick = now + TravelTicks(squadPos, rendezvous, ticksPerCell);
			return new TeamAttackIntent(target, rendezvous, rendezvousTick, etaTick, now, value, targetActorId);
		}

		/// <summary>An intent is joinable while it is fresh (not older than maxAge) and its ETA is not further in the past than grace.</summary>
		public static bool IntentLive(TeamAttackIntent intent, int now, int maxAgeTicks, int graceTicks) =>
			intent != null && now - intent.PublishTick <= maxAgeTicks && now - intent.EtaTick <= graceTicks;

		/// <summary>
		/// The intent an idle squad at <paramref name="squadPos"/> joins: live, with its rendezvous within the join range; the nearest wins, ties go to the
		/// larger publisher squad, then the lower ordinal key. Null when none qualifies. Deterministic (never enumeration-order dependent).
		/// </summary>
		public static Candidate? ChooseIntent(IEnumerable<Candidate> candidates, WPos squadPos, int now, int maxAgeTicks, int graceTicks, int joinRangeCells)
		{
			if (candidates == null)
				return null;
			var range = (long)Math.Max(0, joinRangeCells) * CellUnits;
			var rangeSquared = range * range;
			Candidate? best = null;
			long bestDistance = 0;
			foreach (var c in candidates)
			{
				if (c.Intent == null || !IntentLive(c.Intent, now, maxAgeTicks, graceTicks))
					continue;
				var distance = (long)(c.Intent.Rendezvous - squadPos).HorizontalLengthSquared;
				if (distance > rangeSquared)
					continue;
				if (best == null || Better(c, distance, best.Value, bestDistance))
				{
					best = c;
					bestDistance = distance;
				}
			}

			return best;
		}

		static bool Better(Candidate c, long distance, Candidate best, long bestDistance)
		{
			if (distance != bestDistance)
				return distance < bestDistance;
			if (c.Intent.Value != best.Intent.Value)
				return c.Intent.Value > best.Intent.Value;
			return string.CompareOrdinal(c.Key, best.Key) < 0;
		}

		/// <summary>
		/// Start together: a joiner starts once it is assembled and the publisher has reached the rendezvous, or - never waiting forever - once
		/// waitTicks have passed since the later of the rendezvous tick and its own join tick.
		/// </summary>
		public static bool RendezvousCommit(int now, int assembledPercent, int assemblePercent, int rendezvousTick, int joinTick, int waitTicks)
		{
			if (assembledPercent >= assemblePercent && now >= rendezvousTick)
				return true;
			return now >= Math.Max(rendezvousTick, joinTick) + Math.Max(0, waitTicks);
		}

		/// <summary>Combat value the personality keeps at home: the reserve share of the own combat value. Zero when no value or no share.</summary>
		public static long ReserveValue(long ownCombatValue, int reserveSharePercent)
		{
			if (ownCombatValue <= 0 || reserveSharePercent <= 0)
				return 0;
			return ownCombatValue * Math.Min(100, reserveSharePercent) / 100;
		}

		/// <summary>A squad (or draft) leaves home only without a home block, and only while what stays home still covers the reserve.</summary>
		public static bool MayLeaveHome(long homeValue, long leavingValue, long reserveValue, bool homeBlocked) =>
			!homeBlocked && homeValue - leavingValue >= reserveValue;

		/// <summary>The pool launches when what it holds above the reserve (the reserve is first covered by quiet squads) reaches the minimum.</summary>
		public static bool ExcessLaunchDue(long poolValue, long quietSquadValue, long reserveValue, int minExcessValue, bool homeBlocked)
		{
			if (homeBlocked || poolValue <= 0)
				return false;
			var need = Math.Max(0, reserveValue - Math.Max(0, quietSquadValue));
			return poolValue - need >= Math.Max(1, minExcessValue);
		}

		/// <summary>
		/// Which pool members stay home: the strongest are drafted first (ordered by value desc, then id), a unit is drafted only while what is left
		/// still covers <paramref name="need"/>. Returns the kept ids.
		/// </summary>
		public static HashSet<uint> KeepForReserve(IEnumerable<(uint Id, int Value)> pool, long need)
		{
			var keep = new HashSet<uint>();
			var ordered = pool.OrderByDescending(u => u.Value).ThenBy(u => u.Id).ToList();
			var remaining = ordered.Sum(u => (long)u.Value);
			foreach (var u in ordered)
			{
				if (remaining - u.Value >= need)
					remaining -= u.Value;
				else
					keep.Add(u.Id);
			}

			return keep;
		}

		/// <summary>The max-strength push is due when the army trigger or the late-tick trigger fires (a value of 0 disables its trigger).</summary>
		public static bool PushDue(long ownArmyValue, int now, int pushArmyValue, int pushLateTick) =>
			(pushArmyValue > 0 && ownArmyValue >= pushArmyValue) || (pushLateTick > 0 && now >= pushLateTick);

		/// <summary>The reserve in force: zero during a push, else the share of the own value.</summary>
		public static long EffectiveReserve(bool pushActive, long ownCombatValue, int reserveSharePercent) =>
			pushActive ? 0 : ReserveValue(ownCombatValue, reserveSharePercent);

		/// <summary>The reserve in force with the finishing push (A7): zero during a push, the finish minimum (never above the share) during the all-in, else the share.</summary>
		public static long EffectiveReserve(bool pushActive, bool finishActive, long ownCombatValue, int reserveSharePercent, int finishMinReservePercent) =>
			EffectiveReserve(pushActive, ownCombatValue, finishActive ? Math.Min(reserveSharePercent, finishMinReservePercent) : reserveSharePercent);

		// ---- A7: the finishing push ----

		/// <summary>
		/// One step of the all-in latch. Enter when an enemy seat has no production left, or when own + allied army value (at least minArmyValue)
		/// reaches enterPercent of a known, non-zero enemy army value; once active, hold until the ratio falls below the (never higher) exit percent.
		/// </summary>
		public static bool FinishStep(bool active, long ownArmy, long alliedArmy, long enemyArmy, int enterPercent, int exitPercent, int minArmyValue, bool enemyWithoutProduction)
		{
			if (enemyWithoutProduction)
				return true;
			var ours = Math.Max(0, ownArmy) + Math.Max(0, alliedArmy);
			if (enemyArmy <= 0 || ours < minArmyValue)
				return false;
			var percent = active ? Math.Min(exitPercent, enterPercent) : enterPercent;
			return ours * 100 >= enemyArmy * percent;
		}

		/// <summary>
		/// A7 trigger (Workhorse C3): a seat is without production when at least minKnownBuildings of its remembered buildings are counted and none of them is a CY or a
		/// production building. A key building always counts; any other building counts only when it is neither a wall nor armed (walls and defences prove nothing
		/// about the base behind them).
		/// </summary>
		public static bool SeatLacksProduction(IEnumerable<(bool Wall, bool Armed, bool Key)> buildings, int minKnownBuildings)
		{
			if (buildings == null)
				return false;
			int counted = 0, key = 0;
			foreach (var b in buildings)
			{
				if (b.Key)
				{
					counted++;
					key++;
				}
				else if (!b.Wall && !b.Armed)
					counted++;
			}

			return counted >= minKnownBuildings && key == 0;
		}

		/// <summary>The forced launch draft (Workhorse C4) is born with at least the dispatch floor (the A3 merge floor), so a squad is never created only to be merged back.</summary>
		public static bool LaunchMeetsFloor(int draftUnits, long draftValue, int minUnits, int minValue) => draftUnits >= minUnits && draftValue >= minValue;

		// ---- A3 merge (Opus ruling a): a quiet remnant below the dispatch floor goes back to the pool ----

		/// <summary>A squad below the dispatch floor in unit count or in value is a remnant.</summary>
		public static bool IsTinyRemnant(int units, long value, int minUnits, int minValue) => units < minUnits || value < minValue;

		/// <summary>A remnant that has stayed one for mergeTicks (first seen at sinceTick, -1 = never) is merged into the pool, where the next attack force picks it up.</summary>
		public static bool TinyMergeDue(bool tiny, int sinceTick, int now, int mergeTicks) => tiny && sinceTick >= 0 && now - sinceTick >= Math.Max(0, mergeTicks);

		// ---- A8: target value scoring ----

		/// <summary>One target candidate: its cost, the expected defence around it, its class weight, whether it is a CY/production/economy building, its distance and its seat's visible army.</summary>
		public readonly record struct TargetCandidate(uint Id, int Value, int Defence, int ClassWeightPercent, bool KeyOrEconomy, long DistanceSquared, long SeatArmy);

		public static int DistanceCells(long distanceSquared) => (int)((long)Math.Sqrt(Math.Max(0, distanceSquared)) / CellUnits);

		/// <summary>score = value x class weight x (1 + weak-seat bonus) x distance factor / (expected defence + 1). Integer, overflow-safe for game values.</summary>
		public static long TargetScore(TargetCandidate c, int distanceHalfCells, int weakSeatBonusPercent, long strongestSeatArmy)
		{
			var half = Math.Max(1, distanceHalfCells);
			var factorPermille = 1000L * half / (half + DistanceCells(c.DistanceSquared));
			var weak = strongestSeatArmy > 0 ? Math.Max(0, weakSeatBonusPercent) * Math.Max(0, strongestSeatArmy - c.SeatArmy) / strongestSeatArmy : 0;
			return Math.Max(1L, c.Value) * Math.Max(0, c.ClassWeightPercent) * (100 + weak) * factorPermille / (Math.Max(0, c.Defence) + 1);
		}

		/// <summary>
		/// The target to attack. An undefended CY, production or economy building within reach is always taken before any defended target (the best
		/// such building by score); otherwise the best score wins. Ties go to the nearer, then the lower id. Null when empty.
		/// </summary>
		public static uint? PickTarget(IEnumerable<TargetCandidate> candidates, int distanceHalfCells, int weakSeatBonusPercent, int reachCells)
		{
			var list = candidates?.ToList();
			if (list == null || list.Count == 0)
				return null;
			var strongest = list.Max(c => c.SeatArmy);
			var undefended = list.Where(c => c.KeyOrEconomy && c.Defence <= 0 && DistanceCells(c.DistanceSquared) <= reachCells).ToList();
			var pool = undefended.Count > 0 ? undefended : list;
			return pool.OrderByDescending(c => TargetScore(c, distanceHalfCells, weakSeatBonusPercent, strongest))
				.ThenBy(c => c.DistanceSquared).ThenBy(c => c.Id).First().Id;
		}
	}
}
