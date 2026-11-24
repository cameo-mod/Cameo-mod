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
	/// PINCER-02 (V07, USER 2026-11-24, master replay: "no pincer was seen"). Pure rules, no world access; the wiring is SquadManagerBotModuleCA.V07.cs and
	/// the TEAM-PINCER-01 code of SquadManagerBotModuleCA.TeamLead.cs. Behind the switch TeamLeadPincerWideEnabled (DEV row tl_pincer_wide):
	/// 1. any approach separation of 90-180 degrees counts (measured between the two real staging points as seen from the target; no exact opposite sides);
	/// 2. the staging search fans out: when the preferred spread has no legal staging point the search widens to more angles on both sides and takes the first
	///    legal PAIR in rank order, instead of giving the pincer up when one side is blocked;
	/// 3. the two-times-WAVE-MIN gate is read per arm: each arm (its own units AND value) must hold the arm bar, which is an <c>ArmPercent</c> share of the WAVE-MIN bars
	///    (100 = a full WAVE-MIN per arm; the default is lower so the pincer fires more often; the pair together still holds at least twice the arm bar).
	/// </summary>
	public static class PincerWideCA
	{
		/// <summary>The smallest approach separation the rule accepts, whatever the yaml says (a narrower pair is one approach, not a pincer).</summary>
		public const int MinAngleFloor = 60;

		/// <summary>The lowest arm share of the WAVE-MIN bars the rule accepts, whatever the yaml says.</summary>
		public const int MinArmPercent = 50;

		/// <summary>
		/// The angle at <paramref name="target"/> between the bearings to <paramref name="a"/> and <paramref name="b"/>, 0-180 degrees. Integer only
		/// (WAngle.ArcTan), so every client computes the same value. A point on top of the target has no bearing: 0.
		/// </summary>
		public static int SeparationDegrees(WPos target, WPos a, WPos b)
		{
			var da = a - target;
			var db = b - target;
			if (da.HorizontalLengthSquared == 0 || db.HorizontalLengthSquared == 0)
				return 0;

			var angleA = WAngle.ArcTan(da.Y, da.X).Angle;
			var angleB = WAngle.ArcTan(db.Y, db.X).Angle;
			var diff = Math.Abs(angleA - angleB) % 1024;
			if (diff > 512)
				diff = 1024 - diff;

			return diff * 360 / 1024;
		}

		/// <summary>The separation counts as a pincer: at least <paramref name="minDegrees"/> (never below <see cref="MinAngleFloor"/>); 180 is the opposite side, any value between counts.</summary>
		public static bool AngleOk(int separationDegrees, int minDegrees) => separationDegrees >= Math.Clamp(minDegrees, MinAngleFloor, 180);

		/// <summary>One arm's bar: <paramref name="percent"/> percent (50-100) of the WAVE-MIN bar, rounded up; a bar of 0 or less stays 0 (no bar).</summary>
		public static int ArmBar(int bar, int percent) =>
			bar <= 0 ? 0 : (int)Math.Max(1L, ((long)bar * Math.Clamp(percent, MinArmPercent, 100) + 99) / 100);

		/// <summary>
		/// The combined idle blob can field two arms: twice the join floor and, when the WAVE-MIN tier applies, twice the arm bars. At 100 percent this is
		/// TeamLeadCA.PincerBlob exactly.
		/// </summary>
		public static bool PincerBlob(int units, long value, int floorUnits, bool waveMinLimits, int waveMinUnits, int waveMinValue, int armPercent) =>
			TeamLeadCA.BlobReady(units, value, 2 * Math.Max(1, floorUnits), waveMinLimits, 2 * ArmBar(waveMinUnits, armPercent), 2 * ArmBar(waveMinValue, armPercent));

		/// <summary>Both arms hold the arm bar on their own: units and value of each arm (no bar = holds).</summary>
		public static bool ArmsHold(int units0, long value0, int units1, long value1, int waveMinUnits, int waveMinValue, int armPercent)
		{
			var unitBar = ArmBar(waveMinUnits, armPercent);
			var valueBar = ArmBar(waveMinValue, armPercent);
			return units0 >= unitBar && value0 >= valueBar && units1 >= unitBar && value1 >= valueBar;
		}

		/// <summary>
		/// The staging candidates for one side of the target, best first: the TEAM-PINCER-01 list for the yaml spread (so a free map gives the same points as before),
		/// then wider and narrower approach angles (50-120 degrees round from the line target-to-home) at the full stand-off and pulled 4 cells in.
		/// </summary>
		public static List<WPos> Candidates(WPos from, WPos target, int side, int standOffCells, int spreadDegrees)
		{
			var result = TeamLeadCA.PincerStaging(from, target, side, standOffCells, spreadDegrees);
			var toHome = from - target;
			var length = toHome.HorizontalLength;
			if (length <= 0)
				return result;

			var spread = Math.Clamp(spreadDegrees, 45, 90);
			var known = new HashSet<(int Angle, int Pull)>();
			foreach (var (swing, pull) in new[] { (0, 0), (-15, 0), (-25, 0), (0, 4), (-15, 4), (-25, 4) })
				known.Add((Math.Max(30, spread + swing), pull));

			var standOff = Math.Max(4, standOffCells);
			foreach (var (half, pull) in new[] { (90, 0), (80, 0), (100, 0), (60, 0), (110, 0), (50, 0), (120, 0), (90, 4), (80, 4), (100, 4) })
			{
				if (!known.Add((half, pull)))
					continue;

				var radius = (long)Math.Max(4, standOff - pull) * TeamLeadCA.CellUnits;
				var line = new WVec((int)(toHome.X * radius / length), (int)(toHome.Y * radius / length), 0);
				var cell = target + line.Rotate(WRot.FromYaw(WAngle.FromDegrees((side < 0 ? -1 : 1) * half)));
				result.Add(new WPos(cell.X, cell.Y, 0));
			}

			return result;
		}

		/// <summary>
		/// The first pair of staging points, one per side, in rank order (the sum of the two candidate ranks first, so a near-default pair beats a far-fetched one),
		/// whose real separation round the target passes <see cref="AngleOk"/> and that stand at least <paramref name="minCellsApart"/> apart. A resolver
		/// returns the legal, reachable point for a candidate index or null; each is asked at most once. False = no pair.
		/// </summary>
		public static bool PickPair(WPos target, int countA, Func<int, WPos?> resolveA, int countB, Func<int, WPos?> resolveB, int minDegrees, int minCellsApart,
			out WPos a, out WPos b, out int separationDegrees)
		{
			a = b = default;
			separationDegrees = 0;
			var cachedA = new WPos?[countA];
			var cachedB = new WPos?[countB];
			var doneA = new bool[countA];
			var doneB = new bool[countB];
			var apart = (long)Math.Max(1, minCellsApart) * TeamLeadCA.CellUnits;

			WPos? PointA(int i)
			{
				if (!doneA[i])
				{
					cachedA[i] = resolveA(i);
					doneA[i] = true;
				}

				return cachedA[i];
			}

			WPos? PointB(int j)
			{
				if (!doneB[j])
				{
					cachedB[j] = resolveB(j);
					doneB[j] = true;
				}

				return cachedB[j];
			}

			for (var rank = 0; rank <= countA + countB - 2; rank++)
			{
				for (var i = Math.Max(0, rank - (countB - 1)); i <= Math.Min(rank, countA - 1); i++)
				{
					var pa = PointA(i);
					if (pa == null)
						continue;

					var pb = PointB(rank - i);
					if (pb == null)
						continue;

					var angle = SeparationDegrees(target, pa.Value, pb.Value);
					if (!AngleOk(angle, minDegrees) || (pa.Value - pb.Value).HorizontalLengthSquared < apart * apart)
						continue;

					a = pa.Value;
					b = pb.Value;
					separationDegrees = angle;
					return true;
				}
			}

			return false;
		}
	}
}
