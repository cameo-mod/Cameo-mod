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

// A10 RETREAT-RALLY (design DRAFT02): the pure decisions. Where a retreating ground squad goes: score friendly armies, apply the guards,
// the rally phase machine and its ends, the fallback base cell on the enemy-to-own-base axis. No World access, no RNG, no clock: the
// squad manager (SquadManagerBotModuleCA.RetreatRally.cs) feeds it measured values. Used only while RetreatRallyOn is true.
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public static class RetreatRallyPolicyCA
	{
		public const int CellUnits = 1024;

		/// <summary>The scores are permille (0-1000).</summary>
		public const int Full = 1000;

		/// <summary>The squad counts as arrived within the destination radius plus this slack (a Move ends on a cell, not on the line).</summary>
		public const int ArrivalSlackCells = 2;

		/// <summary>At most this many ranked destinations are put through the world guards (G1/G2/reachability) in one decision.</summary>
		public const int MaxGuardChecks = 6;

		/// <summary>A9 registry mirror: every A10 parameter, its yaml key and its documented inclusive range (equal to the Desc ranges; a test pins it).</summary>
		public static readonly (string Key, int Min, int Max)[] Ranges =
		{
			("AggTeamRallyMaxDistanceCells", 10, 200),
			("AggTeamRallyWeightDistance", 0, 100),
			("AggTeamRallyWeightStrength", 0, 100),
			("AggTeamRallyWeightNeed", 0, 100),
			("AggTeamRallyStrengthTargetPercent", 50, 600),
			("AggTeamRallyNeedFightPoints", 0, 1000),
			("AggTeamRallyNeedDefendPoints", 0, 1000),
			("AggTeamRallyNeedMarchPoints", 0, 1000),
			("AggTeamRallyNeedIdlePoints", 0, 1000),
			("AggTeamRallyAllyPenaltyPoints", 0, 500),
			("AggTeamRallyMinScore", 0, 1000),
			("AggTeamRallyMinJoinRatioPercent", 50, 300),
			("AggTeamRallyDangerScanRadiusCells", 6, 40),
			("AggTeamRallyDestRadiusCells", 4, 40),
			("AggTeamRallyAwayGainCells", 0, 20),
			("AggTeamRallyMinSquadValue", 0, 20000),
			("AggTeamRallyMinCommitTicks", 0, 3000),
			("AggTeamRallyRescoreTicks", 25, 1000),
			("AggTeamRallySwitchMarginPercent", 0, 200),
			("AggTeamRallyMaxTicks", 100, 6000),
			("AggTeamRallySupportMaxTicks", 0, 6000),
			("AggTeamRallyCooldownTicks", 0, 6000),
			("AggTeamRallyRetreatMarkTicks", 50, 3000),
			("AggTeamRallyMaxHops", 0, 3),
			("AggTeamRallyEpisodeResetTicks", 100, 12000),
			("AggTeamRallyBlacklistTicks", 0, 30000),
			("AggTeamRallyFallbackDepthCells", -20, 20),
			("AggTeamRallyHumanAllies", 0, 1),
		};

		/// <summary>The first range violation as a message, or null. The three score weights must not all be zero.</summary>
		public static string RangeError(Func<string, int> value)
		{
			foreach (var (key, min, max) in Ranges)
			{
				var v = value(key);
				if (v < min || v > max)
					return $"{key} = {v} is outside its documented range {min}..{max}.";
			}

			if (value("AggTeamRallyWeightDistance") + value("AggTeamRallyWeightStrength") + value("AggTeamRallyWeightNeed") <= 0)
				return "The AggTeamRallyWeight* values must not all be zero.";

			return null;
		}

		// ---- candidates and scoring ----

		/// <summary>What a destination needs from the retreating squad (the order is the points order: fight, defend, march, idle).</summary>
		public enum RallyNeed { Idle, March, Defend, Fight }

		/// <summary>What the squad does on arrival: support an army (AttackMove onto its target) or merge into the pool at an own idle squad.</summary>
		public enum RallyAction { Support, Merge }

		/// <summary>The tunable numbers of the score, resolved by the caller (A9).</summary>
		public readonly record struct Params(int MaxDistanceCells, int WeightDistance, int WeightStrength, int WeightNeed, int StrengthTargetPercent,
			int NeedFightPoints, int NeedDefendPoints, int NeedMarchPoints, int NeedIdlePoints, int AllyPenaltyPoints, int MinScore, int MinSquadValue);

		/// <summary>
		/// One possible destination with the facts the caller measured. A destination is an own or allied-bot Rush/Guerrilla squad, or an open
		/// ally defence request (<see cref="DefenceRequest"/>, no squad). Distance in whole cells.
		/// </summary>
		public readonly record struct Candidate(string OwnerKey, int SquadId, bool Ally, bool Idle, bool DefenceRequest, RallyNeed Need,
			long SquadValue, long Strength, int DistanceCells, bool Retreating, bool Rallying, bool Marked, bool CoolingDown, bool Blacklisted,
			bool IsBreaker, bool IsSelf, bool OtherState);

		/// <summary>The stable identity of a destination: owner key + squad id (a request: the requester key + "#req").</summary>
		public static string KeyOf(Candidate c) => c.DefenceRequest ? c.OwnerKey + "#req" : c.OwnerKey + "#" + c.SquadId;

		/// <summary>What the squad does at this destination: a defence request is supported; an own idle squad is merged into; every other army is supported.</summary>
		public static RallyAction ActionFor(Candidate c) => !c.DefenceRequest && c.Idle ? RallyAction.Merge : RallyAction.Support;

		/// <summary>Need points of a need class (the parameter table).</summary>
		public static int NeedPoints(RallyNeed need, in Params p) => need switch
		{
			RallyNeed.Fight => p.NeedFightPoints,
			RallyNeed.Defend => p.NeedDefendPoints,
			RallyNeed.March => p.NeedMarchPoints,
			_ => p.NeedIdlePoints
		};

		/// <summary>
		/// The first exclusion reason of a candidate, or null. Order: self, breaker, retreating, rallying (no chains), marked (M2), cooldown (G5),
		/// blacklist (G6), allied idle (M3), an unusable state, value below the floor (squads only), distance. Distance from MaxDistanceCells
		/// on is excluded (the distance term is then zero anyway; fail closed).
		/// </summary>
		public static string Exclusion(in Candidate c, in Params p)
		{
			if (c.IsSelf)
				return "self";
			if (c.IsBreaker)
				return "breaker";
			if (c.Retreating)
				return "retreating";
			if (c.Rallying)
				return "rallying";
			if (c.Marked)
				return "marked";
			if (c.CoolingDown)
				return "cooldown";
			if (c.Blacklisted)
				return "blacklist";
			if (c.Idle && c.Ally && !c.DefenceRequest)
				return "idle_ally";
			if (c.OtherState)
				return "state";
			if (!c.DefenceRequest && c.SquadValue < p.MinSquadValue)
				return "value";
			if (c.DistanceCells < 0 || c.DistanceCells >= p.MaxDistanceCells)
				return "distance";

			return null;
		}

		/// <summary>Distance term, permille: 1000 at distance 0 falling linearly to 0 at MaxDistanceCells.</summary>
		public static long DistanceTerm(int distanceCells, int maxDistanceCells) =>
			maxDistanceCells <= 0 ? 0 : Math.Max(0L, Full - (long)Math.Max(0, distanceCells) * Full / maxDistanceCells);

		/// <summary>Strength term, permille, in long arithmetic: the destination strength against the retreating squad's value times the target percent. A squad of no value is always matched (1000).</summary>
		public static long StrengthTerm(long strength, long retreatingValue, int targetPercent)
		{
			var denominator = Math.Max(0L, retreatingValue) * Math.Max(0, targetPercent);
			if (denominator <= 0)
				return Full;
			if (strength <= 0)
				return 0;
			return Math.Min((long)Full, strength * 100000L / denominator);
		}

		/// <summary>The score 0-1000: weighted mean of the distance, strength and need terms, minus the ally penalty, floored at 0.</summary>
		public static int Score(in Candidate c, long retreatingValue, in Params p)
		{
			var wd = Math.Max(0, p.WeightDistance);
			var ws = Math.Max(0, p.WeightStrength);
			var wn = Math.Max(0, p.WeightNeed);
			var sum = (long)wd + ws + wn;
			if (sum <= 0)
				return 0;

			var need = Math.Min((long)Full, Math.Max(0, NeedPoints(c.Need, p)));
			var mean = (wd * DistanceTerm(c.DistanceCells, p.MaxDistanceCells) + ws * StrengthTerm(c.Strength, retreatingValue, p.StrengthTargetPercent) + wn * need) / sum;
			var score = mean - (c.Ally ? Math.Max(0, p.AllyPenaltyPoints) : 0);
			return (int)Math.Max(0L, Math.Min((long)Full, score));
		}

		/// <summary>The decision: a destination (Found) or the reason the squad goes to the base.</summary>
		public readonly record struct Choice(bool Found, Candidate Pick, int Score, int Best, string Why, int Considered, int MarkedRejects);

		/// <summary>
		/// The best destination. Candidates are excluded (Exclusion), scored and ranked (score desc, distance asc, own before ally, owner key, squad id: never
		/// enumeration-order dependent); ranked ones below MinScore end the search; the others go through <paramref name="worldGuard"/> (G1/G2/reachability:
		/// null = passes, else the reject reason) until one passes, at most MaxGuardChecks. Not found: Why is min_score, the first guard reject, or the
		/// exclusion reason of the nearest excluded candidate (none without any).
		/// </summary>
		public static Choice Choose(IEnumerable<Candidate> candidates, long retreatingValue, in Params p, Func<Candidate, string> worldGuard = null)
		{
			var ranked = new List<(Candidate C, int Score)>();
			string excludedWhy = null;
			var excludedDistance = int.MaxValue;
			var marked = 0;
			if (candidates != null)
			{
				foreach (var c in candidates)
				{
					var reason = Exclusion(c, p);
					if (reason != null)
					{
						if (reason == "marked")
							marked++;
						if (reason != "self" && reason != "breaker" && (excludedWhy == null || c.DistanceCells < excludedDistance))
						{
							excludedWhy = reason;
							excludedDistance = c.DistanceCells;
						}

						continue;
					}

					ranked.Add((c, Score(c, retreatingValue, p)));
				}
			}

			var order = ranked.OrderByDescending(r => r.Score).ThenBy(r => r.C.DistanceCells).ThenBy(r => r.C.Ally)
				.ThenBy(r => r.C.OwnerKey, StringComparer.Ordinal).ThenBy(r => r.C.SquadId).ToList();
			var best = order.Count > 0 ? order[0].Score : 0;
			if (order.Count == 0)
				return new Choice(false, default, 0, 0, excludedWhy ?? "none", 0, marked);

			string firstReject = null;
			var checks = 0;
			foreach (var (c, score) in order)
			{
				if (score < p.MinScore || checks >= MaxGuardChecks)
					break;

				checks++;
				var reject = worldGuard?.Invoke(c);
				if (reject == null)
					return new Choice(true, c, score, best, null, order.Count, marked);

				firstReject ??= reject;
			}

			return new Choice(false, default, 0, best, firstReject ?? "min_score", order.Count, marked);
		}

		// ---- guards ----

		/// <summary>G1: the fight at the destination is lost when the ratio (retreating squad plus friends there against the visible enemies) is below the bar. Not for the merge destination, and only with enemies in sight.</summary>
		public static bool Losing(double ratio, int minJoinRatioPercent, bool enemiesVisible, bool mergeDestination) =>
			!mergeDestination && enemiesVisible && ratio * 100 < minJoinRatioPercent;

		/// <summary>L6 march_flee: below the agg-core flee ratio the squad does not walk on through a fight.</summary>
		public static bool MarchFlee(double ratio, int fleeRatioPercent) => ratio * 100 < fleeRatioPercent;

		/// <summary>G2: the destination lies behind the pursuers when it is not at least gainCells farther from them than the squad is. No pursuer in sight skips the rule. Distances in world units.</summary>
		public static bool BehindPursuers(bool pursuersVisible, long destinationToPursuers, long squadToPursuers, int gainCells) =>
			pursuersVisible && destinationToPursuers < squadToPursuers + (long)Math.Max(0, gainCells) * CellUnits;

		/// <summary>G3/G8: the base answers first. recall (RecallAll) or home (a squad that defends, an active defence plan, a home threat). Null = no block.</summary>
		public static string HomeBlock(bool recallAll, bool squadDefends, bool planActive, bool homeThreat) =>
			recallAll ? "recall" : squadDefends || planActive || homeThreat ? "home" : null;

		/// <summary>G4: the hops of the episode in force: zero once resetTicks passed since the last rally event (lastTick &lt; 0 = never).</summary>
		public static int EffectiveHops(int hops, int lastRallyTick, int now, int resetTicks) =>
			hops > 0 && lastRallyTick >= 0 && now - lastRallyTick < resetTicks ? hops : 0;

		/// <summary>G4: no further rally once the episode reached MaxHops (0 = never).</summary>
		public static bool HopsExhausted(int hops, int lastRallyTick, int now, int maxHops, int resetTicks) =>
			EffectiveHops(hops, lastRallyTick, now, resetTicks) >= maxHops;

		/// <summary>G5/G6: a timestamp that blocks until (exclusive) the given tick. 0 ticks blocks nothing.</summary>
		public static int BlockedUntil(int now, int ticks) => now + Math.Max(0, ticks);

		public static bool Blocked(int untilTick, int now) => now < untilTick;

		/// <summary>G10 (M2): the squad began a flight within windowTicks of now (since &lt; 0 = never).</summary>
		public static bool IsMarked(int sinceTick, int now, int windowTicks) => sinceTick >= 0 && now - sinceTick < windowTicks;

		/// <summary>G7: the destination is locked for minCommitTicks from the pick.</summary>
		public static bool LockHeld(int pickTick, int now, int minCommitTicks) => now - pickTick < minCommitTicks;

		/// <summary>G7: a better destination replaces the current one only after the lock and with the margin (new >= old x (100 + margin) / 100, integer); a destination that became invalid is replaced at once.</summary>
		public static bool MaySwitch(int pickTick, int now, int minCommitTicks, int newScore, int oldScore, int marginPercent, bool destinationInvalid)
		{
			if (destinationInvalid)
				return true;
			if (LockHeld(pickTick, now, minCommitTicks))
				return false;

			return newScore >= (long)oldScore * (100 + Math.Max(0, marginPercent)) / 100;
		}

		public static bool RescoreDue(int lastRescoreTick, int now, int rescoreTicks) => now - lastRescoreTick >= Math.Max(1, rescoreTicks);

		// ---- geometry ----

		/// <summary>The point radiusCells in front of the destination on the line destination -> squad; a squad already inside the radius stays where it is.</summary>
		public static WPos ApproachPoint(WPos squad, WPos destination, int radiusCells)
		{
			var delta = squad - destination;
			var length = delta.HorizontalLength;
			var radius = (long)Math.Max(0, radiusCells) * CellUnits;
			if (length <= radius || length <= 0)
				return squad;

			return new WPos(
				destination.X + (int)(delta.X * radius / length),
				destination.Y + (int)(delta.Y * radius / length),
				squad.Z);
		}

		/// <summary>True within the destination radius plus the arrival slack. Distance in world units.</summary>
		public static bool Arrived(long distance, int destRadiusCells) => distance <= ((long)Math.Max(0, destRadiusCells) + ArrivalSlackCells) * CellUnits;

		/// <summary>
		/// A4.4 fallback cell: on the line enemy -> own anchor, depthCells from the anchor (positive = toward the enemy), clamped, and the first reachable
		/// one while the depth halves down to the anchor itself. enemy == anchor or depth 0 = the anchor. Null = none reachable. Integer math only.
		/// </summary>
		public static (int X, int Y)? AxisCell(int ownX, int ownY, int enemyX, int enemyY, int depthCells,
			Func<int, int, (int X, int Y)> clamp = null, Func<int, int, bool> reachable = null)
		{
			long dx = ownX - enemyX, dy = ownY - enemyY;
			var length = ISqrt(dx * dx + dy * dy);
			var depth = length == 0 ? 0 : depthCells;
			while (true)
			{
				var x = length == 0 ? ownX : (int)(ownX - dx * depth / length);
				var y = length == 0 ? ownY : (int)(ownY - dy * depth / length);
				(int X, int Y) cell = clamp != null ? clamp(x, y) : (x, y);
				if (reachable == null || reachable(cell.X, cell.Y))
					return cell;
				if (depth == 0)
					return null;

				depth /= 2;
			}
		}

		static long ISqrt(long value)
		{
			if (value <= 0)
				return 0;

			var root = (long)Math.Sqrt(value);
			while (root * root > value)
				root--;
			while ((root + 1) * (root + 1) <= value)
				root++;
			return root;
		}

		// ---- the rally phase machine ----

		public enum RallyPhase { March, Support, Merge, End }

		public enum RallyEnd { None, MergedPool, TargetGone, Expired, AbortLosing, Detached, Dead }

		/// <summary>What the squad becomes after an end: DismissPool (units to the pool, A3 mechanism), Idle, or the legacy flight with the rally refused (M4).</summary>
		public enum RallyNext { None, DismissPool, Idle, FleeBaseOnly }

		/// <summary>M4: the next state of every end. An expiry during the march and every losing end run the legacy flight at once.</summary>
		public static RallyNext NextState(RallyEnd end, RallyPhase phaseAtEnd) => end switch
		{
			RallyEnd.MergedPool => RallyNext.DismissPool,
			RallyEnd.TargetGone => RallyNext.Idle,
			RallyEnd.Expired => phaseAtEnd == RallyPhase.March ? RallyNext.FleeBaseOnly : RallyNext.Idle,
			RallyEnd.AbortLosing => RallyNext.FleeBaseOnly,
			_ => RallyNext.None
		};

		/// <summary>The telemetry spelling of an end.</summary>
		public static string EndName(RallyEnd end) => end switch
		{
			RallyEnd.MergedPool => "merged_pool",
			RallyEnd.TargetGone => "target_gone",
			RallyEnd.Expired => "expired",
			RallyEnd.AbortLosing => "abort_losing",
			RallyEnd.Detached => "detached",
			RallyEnd.Dead => "dead",
			_ => "none"
		};

		/// <summary>The telemetry spelling of the phase an end happened in (DRAFT02 section 6: march or support). A merge happens at the end of the march leg.</summary>
		public static string PhaseName(RallyPhase phase) => phase switch
		{
			RallyPhase.Support => "support",
			_ => "march"
		};

		/// <summary>One tick's facts for the phase machine. Losing = march_flee, or G1 at arrival; DestInvalid = the destination squad is gone, retreating, rallying or an ally turned idle; TargetEnded = its fight or request is over; DestLaunched = an own idle destination started an attack.</summary>
		public readonly record struct StepInput(RallyPhase Phase, RallyAction Action, int TicksSinceStart, int TicksInSupport, bool Arrived, bool Losing,
			bool DestInvalid, bool TargetEnded, bool DestLaunched, int MaxTicks, int SupportMaxTicks);

		public readonly record struct StepResult(RallyPhase Phase, RallyAction Action, RallyEnd End);

		/// <summary>
		/// March -> Support | Merge -> End. Order: losing and an invalid destination end it (abort_losing); an own idle destination that launched turns the
		/// action to Support; a Support destination whose fight is over ends it (target_gone); on arrival Merge ends (merged_pool) and Support begins
		/// (SupportMaxTicks 0 = ends on arrival, expired); the caps end the march (expired -> base) and the support (expired -> Idle).
		/// </summary>
		public static StepResult Step(in StepInput i)
		{
			if (i.Phase == RallyPhase.End)
				return new StepResult(RallyPhase.End, i.Action, RallyEnd.None);

			if (i.Losing || i.DestInvalid)
				return new StepResult(i.Phase, i.Action, RallyEnd.AbortLosing);

			var action = i.Action == RallyAction.Merge && i.DestLaunched ? RallyAction.Support : i.Action;
			if (action == RallyAction.Support && i.TargetEnded)
				return new StepResult(i.Phase, action, RallyEnd.TargetGone);

			if (i.Phase == RallyPhase.March)
			{
				if (i.Arrived)
				{
					if (action == RallyAction.Merge)
						return new StepResult(RallyPhase.Merge, action, RallyEnd.MergedPool);

					return new StepResult(RallyPhase.Support, action, i.SupportMaxTicks <= 0 ? RallyEnd.Expired : RallyEnd.None);
				}

				return new StepResult(RallyPhase.March, action, i.TicksSinceStart >= i.MaxTicks ? RallyEnd.Expired : RallyEnd.None);
			}

			return new StepResult(i.Phase, action, i.TicksInSupport >= i.SupportMaxTicks ? RallyEnd.Expired : RallyEnd.None);
		}
	}
}
