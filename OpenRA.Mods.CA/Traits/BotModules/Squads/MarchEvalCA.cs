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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// Hotspot #2 extraction (2026-10-04): the pure decisions inside
	/// GroundUnitsAttackMoveStateCA.Tick / IssueFormationOrders that were NOT already
	/// covered by SquadMicroEvalCA (hold/push hysteresis, pull-back point, micro
	/// budget) — the stuck detector's counter truth table and escalation order, the
	/// route-parameter pick, waypoint advance, the leader-wait latch, formation
	/// bucketing, the axis projection, and the rear-stall tracker.
	/// Bit-identical; all world-facing reads stay in the caller.
	/// </summary>
	public static class MarchEvalCA
	{
		/// <summary>A rear frontline member holding still for this many squad ticks
		/// grants the wider stalled-lead allowance (fransbot chokepoint rule).</summary>
		public const int StalledRearTicks = 25;

		/// <summary>
		/// The stuck detector's counter update. stop∧wait suspects stuck members;
		/// stop∧¬wait suspects a blocked leader — UNLESS a make-way just ended
		/// (the -1 sentinel), which escalates straight to the kick counter for the
		/// rest of this march; ¬stop∧¬wait clears both; ¬stop∧wait deliberately
		/// leaves both untouched (a moving leader on a strung-out march is progress,
		/// the leader-wait latch holds the line instead).
		/// </summary>
		public static (int MakeWay, int KickStuck) StuckCountersNext(
			bool leaderStopped, bool leaderWait, bool makeWayJustEnded,
			int makeWayPossibility, int kickStuckPossibility)
		{
			if (leaderStopped && leaderWait)
				return (makeWayPossibility, kickStuckPossibility + 1);

			if (leaderStopped)
				return makeWayJustEnded
					? (makeWayPossibility, kickStuckPossibility + 1)
					: (makeWayPossibility + 1, kickStuckPossibility);

			if (!leaderWait)
				return (0, 0);

			return (makeWayPossibility, kickStuckPossibility);
		}

		/// <summary>Which remediation arms first — make-way takes precedence: when both
		/// counters cross their maxima the same tick the cheaper fix runs first and the
		/// kick counter keeps accumulating (escalation path, not reset).</summary>
		public static MarchStuckAction StuckActionFor(int makeWayPossibility, int kickStuckPossibility,
			int maxMakeWay, int maxKickStuck)
		{
			if (makeWayPossibility >= maxMakeWay)
				return MarchStuckAction.MakeWay;

			return kickStuckPossibility >= maxKickStuck ? MarchStuckAction.KickStuck : MarchStuckAction.None;
		}

		/// <summary>
		/// The indirect-route parameters for this squad type and target. Harassers take
		/// their configured count, guerrillas a fixed three; other types sometimes take
		/// the wide seven-route indirect plan by chance — the roll is lazy (Func) because
		/// the original only draws LocalRandom inside the chance gate, and an eager draw
		/// would desync the shared stream when the gate is off.
		/// </summary>
		public static (int MaxRoutes, bool UseIndirect) RouteParams(
			SquadCAType type, int harassRouteCount, int indirectRouteChance, Func<int> nextRoll)
		{
			if (type == SquadCAType.Harass)
				return (harassRouteCount, false);

			if (type == SquadCAType.Guerrilla)
				return (3, false);

			if (indirectRouteChance > 0 && nextRoll() < indirectRouteChance)
				return (7, true);

			return (2, false);
		}

		/// <summary>A waypoint is consumed when the leader is within four cells of it,
		/// or after 625 ticks regardless — a waypoint a slow squad cannot reach cannot
		/// stall the march forever.</summary>
		public static bool AdvanceWaypoint(long distanceSquaredToWaypoint, int tick, int lastWaypointUpdateTick)
		{
			return distanceSquaredToWaypoint < 16 || tick > lastWaypointUpdateTick + 625;
		}

		/// <summary>The leader-wait latch's arm half (AR-S hysteresis): a member
		/// trailing beyond the wait radius stops the leader, unless a kick episode is
		/// driving it.</summary>
		public static bool LeaderWaitLatches(bool leaderWaitCheck, bool kickStuckActive)
		{
			return leaderWaitCheck && !kickStuckActive;
		}

		/// <summary>The latch's hold half: the leader keeps waiting while any member
		/// remains beyond the release band and no kick episode is driving. Releasing
		/// on the narrower band is the hysteresis — the old per-tick Stop/AttackMove
		/// alternation repathed the leader every squad tick.</summary>
		public static bool LeaderWaitHolds(bool anyBeyondReleaseRadius, bool kickStuckActive)
		{
			return anyBeyondReleaseRadius && !kickStuckActive;
		}

		/// <summary>Formation bucketing with the file's precedence — a member holding
		/// frontline AND anti_air rows leads; scout and anti_air claim before the
		/// trailing residual; unroled members trail.</summary>
		public static MarchBucket BucketFor(IReadOnlySet<string> roles)
		{
			if (roles == null || roles.Count == 0)
				return MarchBucket.Trailing;

			if (roles.Contains(BotUnitRole.Frontline))
				return MarchBucket.Frontline;

			if (roles.Contains(BotUnitRole.Scout))
				return MarchBucket.Scout;

			return roles.Contains(BotUnitRole.AntiAir) ? MarchBucket.AntiAir : MarchBucket.Trailing;
		}

		/// <summary>
		/// Distance-to-go along the march axis, projected in long — WVec.Dot returns int
		/// and overflows beyond ~45 cells of span, which is most real routes, so the
		/// multiply is widened before the dot and divided after.
		/// </summary>
		public static long AxisRemaining(long dx, long dy, long dz, long ax, long ay, long az, long axisLen)
		{
			return (dx * ax + dy * ay + dz * az) / axisLen;
		}

		/// <summary>
		/// The rear-stall tracker: same position ticks the stall up (and watches HP for
		/// under-fire — a stalled rear losing HP is taking fire in the chokepoint);
		/// movement resets it. prevHp -1 is the unprimed sentinel: the first observed
		/// tick can never flag under-fire.
		/// </summary>
		public static (int StallTicks, int PrevHp, bool UnderFire) RearStallNext(
			bool samePosition, int stallTicks, bool hasHealth, int hp, int prevHp)
		{
			if (!samePosition)
				return (0, -1, false);

			var ticks = stallTicks + 1;
			if (!hasHealth)
				return (ticks, prevHp, false);

			var underFire = prevHp >= 0 && hp < prevHp;
			return (ticks, hp, underFire);
		}

		/// <summary>A stalled rear grants the wider stalled-lead allowance once the stall
		/// reaches <see cref="StalledRearTicks"/>; the normal lead resumes the tick it moves.</summary>
		public static long LeadForStall(int stallTicks, long normalLead, long stalledLead)
		{
			return stallTicks >= StalledRearTicks ? stalledLead : normalLead;
		}

		/// <summary>The march axis must span at least two cells — a frontline centroid
		/// sitting on the route target has no direction to hold formation along.</summary>
		public static bool AxisUsable(long axisLengthSquared)
		{
			var min = (long)WDist.FromCells(2).Length;
			return axisLengthSquared >= min * min;
		}
	}

	public enum MarchStuckAction
	{
		None,
		MakeWay,
		KickStuck
	}

	public enum MarchBucket
	{
		Trailing,
		Frontline,
		Scout,
		AntiAir
	}
}
