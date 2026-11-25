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
	public enum ArtyKind
	{
		Defence = 0,
		Structure = 1,
		Unit = 2,
	}

	/// <summary>One enemy the artillery unit can see: where it is, what it is, and whether it threatens the artillery unit.</summary>
	public readonly struct ArtyCandidate
	{
		public readonly uint Id;
		public readonly WPos Pos;
		public readonly ArtyKind Kind;

		/// <summary>A hostile unit that is closing in (inside the kite radius) or whose weapon already reaches the artillery unit.</summary>
		public readonly bool Threatens;

		public ArtyCandidate(uint id, WPos pos, ArtyKind kind, bool threatens)
		{
			Id = id;
			Pos = pos;
			Kind = kind;
			Threatens = threatens;
		}
	}

	/// <summary>An enemy defence: its position and how far its weapon reaches (cells as WDist length).</summary>
	public readonly struct ArtyDefence
	{
		public readonly WPos Pos;
		public readonly int ReachLen;

		public ArtyDefence(WPos pos, int reachLen)
		{
			Pos = pos;
			ReachLen = reachLen;
		}
	}

	public enum ArtyApproachKind
	{
		/// <summary>Already in range of the approach target: hold and fire.</summary>
		Hold,

		/// <summary>Move (not AttackMove) to <see cref="ArtyApproach.Point"/>, the firing position.</summary>
		Move,

		/// <summary>Every firing position is covered by an enemy defence the unit cannot outrange: stay put.</summary>
		Blocked,
	}

	public readonly struct ArtyApproach
	{
		public readonly ArtyApproachKind Kind;
		public readonly WPos Point;

		/// <summary>The position the firing point was computed against (the squad target, or the defence that covered an earlier firing point).</summary>
		public readonly WPos Against;

		public ArtyApproach(ArtyApproachKind kind, WPos point, WPos against)
		{
			Kind = kind;
			Point = point;
			Against = against;
		}
	}

	/// <summary>
	/// ARTY-01: the pure decision half of artillery micro (World-free, so tests drive it directly). Rules (USER P0):
	/// (a) target priority defences, then other structures, then units - a unit only when no structure is in range or it
	/// threatens the artillery unit; (b) stand-off: fire from the unit's maximum range minus a small margin and never walk
	/// into the reach of a known defence; (c) kite: when a hostile unit it outranges closes in, back off to outrange it again.
	/// Every function is deterministic: ties break on distance, then on the candidate id.
	/// </summary>
	public static class ArtyMicroCA
	{
		const int ApproachRetargets = 4;

		/// <summary>
		/// Index of the candidate to attack from <paramref name="from"/>, or -1 when none is in range. ARTY-TARGET-01: with
		/// <paramref name="structFirst"/> a unit that does not threaten the artillery is never picked, so only a threat, a defence or a structure is attacked.
		/// </summary>
		public static int PickTarget(WPos from, int rangeLen, IReadOnlyList<ArtyCandidate> candidates, bool structFirst = false)
		{
			var rangeSq = (long)rangeLen * rangeLen;
			var threat = -1;
			var structure = -1;
			var unit = -1;
			for (var i = 0; i < candidates.Count; i++)
			{
				var c = candidates[i];
				if ((c.Pos - from).HorizontalLengthSquared > rangeSq)
					continue;

				if (c.Kind == ArtyKind.Unit)
				{
					if (c.Threatens)
						threat = Closer(from, candidates, threat, i, false);
					else if (!structFirst)
						unit = Closer(from, candidates, unit, i, false);
				}
				else
					structure = Closer(from, candidates, structure, i, true);
			}

			if (threat >= 0)
				return threat;

			return structure >= 0 ? structure : unit;
		}

		/// <summary>
		/// ARTY-TARGET-01 (c2): the structure an artillery squad assaults when nothing structural is in range - the nearest defence to the
		/// assault point, else the nearest other structure. Units are never picked. Index into <paramref name="candidates"/>, or -1.
		/// </summary>
		public static int PickStructure(WPos assault, IReadOnlyList<ArtyCandidate> candidates) => PickStructure(assault, candidates, _ => true);

		/// <summary>ARTY-TARGET-01 (c4): as above, but only entries for which <paramref name="usable"/> is true may be picked.</summary>
		public static int PickStructure(WPos assault, IReadOnlyList<ArtyCandidate> candidates, Func<int, bool> usable)
		{
			var best = -1;
			for (var i = 0; i < candidates.Count; i++)
			{
				if (candidates[i].Kind != ArtyKind.Unit && usable(i))
					best = Closer(assault, candidates, best, i, true);
			}

			return best;
		}

		// Lower kind first (only for structures), then nearer, then lower id.
		static int Closer(WPos from, IReadOnlyList<ArtyCandidate> list, int best, int challenger, bool byKind)
		{
			if (best < 0)
				return challenger;

			var a = list[best];
			var b = list[challenger];
			if (byKind && a.Kind != b.Kind)
				return b.Kind < a.Kind ? challenger : best;

			var da = (a.Pos - from).HorizontalLengthSquared;
			var db = (b.Pos - from).HorizontalLengthSquared;
			if (db != da)
				return db < da ? challenger : best;

			return b.Id < a.Id ? challenger : best;
		}

		/// <summary>
		/// The point on the line from <paramref name="target"/> to <paramref name="unit"/> that is <c>rangeLen - marginLen</c> from the
		/// target, i.e. the nearest position that still outranges whatever the target's own reach is. Null when the unit is already
		/// within <paramref name="rangeLen"/> (it can fire) or the range is too small to stand off at.
		/// </summary>
		public static WPos? FiringPoint(WPos unit, WPos target, int rangeLen, int marginLen)
		{
			var want = rangeLen - marginLen;
			if (want <= 0)
				return null;

			var away = unit - target;
			var distance = away.HorizontalLength;
			if (distance <= rangeLen)
				return null;

			return target + new WVec(
				(int)((long)away.X * want / distance),
				(int)((long)away.Y * want / distance),
				0);
		}

		/// <summary>Index of the first defence whose reach (plus margin) covers <paramref name="p"/>, or -1.</summary>
		public static int CoveringDefence(WPos p, IReadOnlyList<ArtyDefence> defences, int marginLen)
		{
			for (var i = 0; i < defences.Count; i++)
			{
				var reach = (long)defences[i].ReachLen + marginLen;
				if ((defences[i].Pos - p).HorizontalLengthSquared <= reach * reach)
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Where an artillery unit should go to engage <paramref name="target"/>. The firing point is moved onto a covering defence
		/// (fire at that one first) until it is clear of every known defence; after a few retargets it holds rather than walking in.
		/// </summary>
		public static ArtyApproach PlanApproach(WPos unit, WPos target, int rangeLen, int rangeMarginLen,
			IReadOnlyList<ArtyDefence> defences, int defenceMarginLen)
		{
			var against = target;
			for (var i = 0; i < ApproachRetargets; i++)
			{
				var point = FiringPoint(unit, against, rangeLen, rangeMarginLen);
				if (point == null)
					return new ArtyApproach(ArtyApproachKind.Hold, unit, against);

				var cover = CoveringDefence(point.Value, defences, defenceMarginLen);
				if (cover < 0)
					return new ArtyApproach(ArtyApproachKind.Move, point.Value, against);

				// Already standing on that defence's doorstep: do not step to a firing point beyond it, keep the cover target.
				against = defences[cover].Pos;
			}

			return new ArtyApproach(ArtyApproachKind.Blocked, unit, against);
		}

		/// <summary>
		/// Back-off to outrange a defence that already reaches the unit: the point <c>rangeLen - marginLen</c> from the defence along the
		/// line from the defence through the unit. Null when the unit is not inside the defence's reach (plus margin) or cannot outrange it.
		/// </summary>
		public static WPos? DefenceBackoff(WPos unit, ArtyDefence defence, int rangeLen, int rangeMarginLen, int defenceMarginLen)
		{
			var want = rangeLen - rangeMarginLen;
			var reach = (long)defence.ReachLen + defenceMarginLen;
			if (want <= defence.ReachLen + defenceMarginLen)
				return null;

			var away = unit - defence.Pos;
			var distance = away.HorizontalLength;
			if (distance <= 0 || (long)distance * distance > reach * reach)
				return null;

			return defence.Pos + new WVec(
				(int)((long)away.X * want / distance),
				(int)((long)away.Y * want / distance),
				0);
		}

		/// <summary>
		/// The stand-off distance to keep from a hostile unit: its own reach plus the margin, or null when the artillery unit does not
		/// outrange it (nothing to kite with; the normal engage behaviour applies).
		/// </summary>
		public static int? KiteStandoff(int ownRangeLen, int threatRangeLen, int marginLen)
		{
			return ownRangeLen > threatRangeLen ? threatRangeLen + marginLen : (int?)null;
		}

		/// <summary>
		/// The waypoint a kiting unit runs to: its own position continued directly away from the threat so the gap grows to
		/// <paramref name="standoffLen"/>, never by more than <paramref name="maxStepLen"/> in one order. Null when the unit is already
		/// at or beyond the stand-off, or the two positions coincide.
		/// </summary>
		public static WPos? KitePoint(WPos unit, WPos threat, int standoffLen, int maxStepLen)
		{
			var away = unit - threat;
			var distance = away.HorizontalLength;
			if (distance <= 0 || distance >= standoffLen)
				return null;

			var step = Math.Min(standoffLen - distance, Math.Max(1, maxStepLen));
			return unit + new WVec(
				(int)((long)away.X * step / distance),
				(int)((long)away.Y * step / distance),
				0);
		}
	}
}
