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
	/// <summary>A squad that takes part in the order: <paramref name="Key"/> is its total-order id (the actor id of its first unit), never a reference.</summary>
	public readonly record struct ArmSquad(uint Key, int Units, long Value);

	/// <summary>The arm (0 or 1) a squad walks to.</summary>
	public readonly record struct ArmPlace(uint Key, int Arm);

	/// <summary><paramref name="Units"/> of the squad <paramref name="Key"/> leave it for the arm <paramref name="ToArm"/> (the rest stay on the squad's own arm).</summary>
	public readonly record struct ArmSplit(uint Key, int Units, int ToArm);

	/// <summary>The two arms of a pincer: who goes where, and the one squad that is divided between them (if any).</summary>
	public sealed class ArmPlan
	{
		public ArmPlan(IReadOnlyList<ArmPlace> places, ArmSplit? split, int units0, long value0, int units1, long value1)
		{
			Places = places;
			Split = split;
			Units0 = units0;
			Value0 = value0;
			Units1 = units1;
			Value1 = value1;
		}

		public IReadOnlyList<ArmPlace> Places { get; }

		public ArmSplit? Split { get; }

		public int Units0 { get; }

		public long Value0 { get; }

		public int Units1 { get; }

		public long Value1 { get; }
	}

	/// <summary>
	/// CONSOLIDATE-02 Q7 (row sq_pincer_push under the master switch sq_pipeline_02): the arms of a pincer decided at PUSH time, pure (design 4.1 Step 2). The blob that stands
	/// at the rally is shared out between two arms so that BOTH hold the arm bar (units and value). Squads go whole to the lighter arm (heaviest first); when no whole-squad
	/// share holds both bars (three seats, one squad each, 2:1), the biggest squad of the heavier arm is DIVIDED: some of its units move to the lighter arm. Integer only, a
	/// total order on the squad key, no RNG: the same blob gives the same arms.
	/// </summary>
	public static class PincerArms
	{
		/// <summary>An arm's floor: the arm bar, and never below the join floor (an arm of one or two units is not an arm).</summary>
		public static int FloorUnits(int armBarUnits, int joinMinUnits) => Math.Max(Math.Max(0, armBarUnits), Math.Max(1, joinMinUnits));

		/// <summary>An arm holds its bar: units AND value.</summary>
		public static bool Holds(int units, long value, int barUnits, long barValue) => units >= barUnits && value >= barValue;

		/// <summary>
		/// The two arms, or null when no share of this blob (whole squads, then one division) puts both arms at the bar - the order then goes as the single blob it is.
		/// </summary>
		public static ArmPlan Balance(IReadOnlyList<ArmSquad> squads, int barUnits, long barValue)
		{
			var live = new List<ArmSquad>(squads.Count);
			foreach (var s in squads)
				if (s.Units > 0)
					live.Add(s);

			if (live.Count == 0)
				return null;

			// Heaviest first; the key makes the order total.
			live.Sort((a, b) =>
			{
				var byValue = b.Value.CompareTo(a.Value);
				if (byValue != 0)
					return byValue;

				var byUnits = b.Units.CompareTo(a.Units);
				return byUnits != 0 ? byUnits : a.Key.CompareTo(b.Key);
			});

			var arm = new int[live.Count];
			var units = new int[2];
			var value = new long[2];
			for (var i = 0; i < live.Count; i++)
			{
				var to = Lighter(units, value);
				arm[i] = to;
				units[to] += live[i].Units;
				value[to] += live[i].Value;
			}

			ArmSplit? split = null;
			if (!(Holds(units[0], value[0], barUnits, barValue) && Holds(units[1], value[1], barUnits, barValue)))
			{
				split = Divide(live, arm, units, value, barUnits, barValue, out var movedUnits, out var movedValue);
				if (split == null)
					return null;

				var donor = 1 - split.Value.ToArm;
				units[donor] -= movedUnits;
				value[donor] -= movedValue;
				units[split.Value.ToArm] += movedUnits;
				value[split.Value.ToArm] += movedValue;
			}

			var places = new List<ArmPlace>(live.Count);
			for (var i = 0; i < live.Count; i++)
				places.Add(new ArmPlace(live[i].Key, arm[i]));

			places.Sort((a, b) => a.Key.CompareTo(b.Key));
			return new ArmPlan(places, split, units[0], value[0], units[1], value[1]);
		}

		// The lighter arm by value, then by units, then arm 0.
		static int Lighter(int[] units, long[] value) =>
			value[1] < value[0] || (value[1] == value[0] && units[1] < units[0]) ? 1 : 0;

		// One division: the biggest squad of the heavier arm (by value; then the other arm) that can still bring both arms to the bar gives some of its units to the other arm.
		// Among the shares that hold both bars the most even one wins (then the fewest units moved).
		static ArmSplit? Divide(List<ArmSquad> live, int[] arm, int[] units, long[] value, int barUnits, long barValue, out int movedUnits, out long movedValue)
		{
			movedUnits = 0;
			movedValue = 0;
			var heavyFirst = 1 - Lighter(units, value);

			for (var pass = 0; pass < 2; pass++)
			{
				var donor = pass == 0 ? heavyFirst : 1 - heavyFirst;
				var taker = 1 - donor;

				var candidates = new List<int>();
				for (var i = 0; i < live.Count; i++)
					if (arm[i] == donor && live[i].Units >= 2)
						candidates.Add(i);

				candidates.Sort((x, y) =>
				{
					var byUnits = live[y].Units.CompareTo(live[x].Units);
					return byUnits != 0 ? byUnits : live[x].Key.CompareTo(live[y].Key);
				});

				foreach (var i in candidates)
				{
					var s = live[i];
					var bestK = 0;
					var bestDiff = long.MaxValue;
					long bestValue = 0;
					for (var k = 1; k < s.Units; k++)
					{
						var moved = s.Value * k / s.Units;
						var donorUnits = units[donor] - k;
						var donorValue = value[donor] - moved;
						var takerUnits = units[taker] + k;
						var takerValue = value[taker] + moved;
						if (!Holds(donorUnits, donorValue, barUnits, barValue) || !Holds(takerUnits, takerValue, barUnits, barValue))
							continue;

						var diff = Math.Abs(donorValue - takerValue);
						if (diff < bestDiff)
						{
							bestK = k;
							bestDiff = diff;
							bestValue = moved;
						}
					}

					if (bestK > 0)
					{
						movedUnits = bestK;
						movedValue = bestValue;
						return new ArmSplit(s.Key, bestK, taker);
					}
				}
			}

			return null;
		}
	}
}
